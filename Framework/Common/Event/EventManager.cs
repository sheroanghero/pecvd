using Aitex.Common.Util;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using Aitex.Core.WCF;
using MECF.Framework.Common.Event;
using MECF.Framework.Common.FAServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Xml;

namespace Aitex.Core.RT.Event
{
    public class EventManager : ICommonEvent
    {
        public EventService Service
        {
            get { return _eventService; }
        }
        public event Action<EventItem> FireEvent;
        public event Action<EventItem> OnAlarmEvent;
        public event Action<EventItem> OnEvent;

        FixSizeQueue<EventItem> _eventQueue;
        List<EventItem> _alarmList;
        PeriodicJob _eventJob;

        EventDBWriter _eventDB;
        EventLogWriter _writerToLog;
        EventMailWriter _writerToMail;
        EventService _eventService;
        ServiceHost _eventServiceHost;


        Dictionary<string, EventItem> _eventDic = new Dictionary<string, EventItem>();

        private const string INFORMATION_EVENT = "INFORMATION_EVENT";
        private const string WARNING_EVENT = "WARNING_EVENT";
        private const string ALARM_EVENT = "ALARM_EVENT";

        private object _locker = new object();

        private string _localAlarmEventConfigFile;
        private string _localAlarmEventDataFile = PathManager.GetCfgDir() + "_AlarmEventDefine.xml";
        private string _localAlarmEventDataBackupFile = PathManager.GetCfgDir() + "_AlarmEventDefine.xml.bak";
        public Dictionary<string, Dictionary<string, EventItem>> AlarmDic { get; private set; }//不同的表，每个表里面有相应的alarm项
        public string CurrentAlarmTable { get; set; } = "0";//default table 0
        private string _defaultAlarmTable = "0";
        private object _alarmDicLocker = new object();


        public List<VIDItem> VidEventList
        {
            get
            {
                List<VIDItem> result = new List<VIDItem>();
                foreach (var dataItem in _eventDic)
                {
                    result.Add(new VIDItem()
                    {
                        DataType = "",
                        Description = dataItem.Value.Description,
                        Index = 0,
                        Name = dataItem.Key,
                        Unit = "",
                    });
                }

                return result;
            }
        }

        public List<VIDItem> VidAlarmList
        {
            get
            {
                List<VIDItem> result = new List<VIDItem>();
                lock (_alarmDicLocker)
                {
                    foreach (var dataItem in AlarmDic[_defaultAlarmTable].Values)
                    {
                        result.Add(new VIDItem()
                        {
                            DataType = "",
                            Description = dataItem.Description,
                            Index = 0,
                            Name = dataItem.EventEnum,
                            Unit = "",
                        });
                    }
                }

                return result;
            }
        }

        public EventManager()
        {

        }
        public void Initialize(string commonEventListXmlFile, string localEventListXmlFile, bool needCreateService = true, bool needSaveDB = true, bool needMailOut = false)
        {
            Initialize(commonEventListXmlFile, needCreateService, needSaveDB, needMailOut, localEventListXmlFile);
        }
        public void Initialize(string commonEventListXmlFile, bool needCreateService = true, bool needSaveDB = true, bool needMailOut = false, string localEventListXmlFile = null)
        {
            if (needSaveDB)
            {
                _eventDB = new EventDBWriter();
                try
                {
                    _eventDB.Initialize();
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }
            }
            _writerToLog = new EventLogWriter();

            if (needMailOut)
            {
                _writerToMail = new EventMailWriter();
            }

            _eventService = new EventService();
            if (needCreateService)
            {
                try
                {
                    _eventServiceHost = new ServiceHost(_eventService);
                    _eventServiceHost.Open();
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("创建Event服务失败，" + ex.Message);
                }
            }

            _eventQueue = new FixSizeQueue<EventItem>(1000);
            _alarmList = new List<EventItem>(1000);
            AlarmDic = new Dictionary<string, Dictionary<string, EventItem>>();
            AlarmDic.Add(_defaultAlarmTable, new Dictionary<string, EventItem>());
            _eventJob = new PeriodicJob(100, this.PeriodicRun, "EventPeriodicJob", true);

            try
            {
                EventDefine eventList = CustomXmlSerializer.Deserialize<EventDefine>(new FileInfo(commonEventListXmlFile));

                foreach (var item in eventList.Items)
                    _eventDic[item.EventEnum] = item;

                _localAlarmEventConfigFile = localEventListXmlFile;
                if (!string.IsNullOrEmpty(_localAlarmEventConfigFile))
                {
                    lock (_alarmDicLocker)
                    {
                        BuildItems(_localAlarmEventConfigFile);
                    }

                    BackupAndRecoverDataFile();

                    CustomData();

                    GenerateDataFile();
                }
            }
            catch (ArgumentNullException)
            {
                throw new ApplicationException("初始化EventManager没有设置Event列表文件");
            }
            catch (FileNotFoundException ex)
            {
                throw new ApplicationException("没有找到Event列表文件，" + ex.Message);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("EventDefine文件格式不对，" + commonEventListXmlFile + ",\r\n" + ex.Message);
            }

            Subscribe(new EventItem(INFORMATION_EVENT, EventType.EventUI_Notify, EventLevel.Information));
            Subscribe(new EventItem(WARNING_EVENT, EventType.EventUI_Notify, EventLevel.Warning));
            Subscribe(new EventItem(ALARM_EVENT, EventType.EventUI_Notify, EventLevel.Alarm));

            OP.Subscribe($"System.UpdateAlarmTable", (string cmd, object[] args) =>
            {
                if (args != null && args.Length > 0 && args[0] is Dictionary<string, Dictionary<string, EventItem>>)
                {
                    var updateAlarmTable = args[0] as Dictionary<string, Dictionary<string, EventItem>>;
                    lock (_alarmDicLocker)
                    {
                        foreach (var table in updateAlarmTable.Keys)
                        {
                            foreach (var key in updateAlarmTable[table].Keys)
                            {
                                if (AlarmDic != null && AlarmDic.ContainsKey(table) && AlarmDic[table].ContainsKey(key))
                                {
                                    if (updateAlarmTable[table][key] is EventItem)
                                    {
                                        AlarmDic[table][key] = (updateAlarmTable[table][key] as EventItem).Clone();
                                    }
                                }
                            }
                        }
                    }

                    GenerateDataFile();
                }
                return true;
            });

            OP.Subscribe($"System.SetAlarmTableID", (out string reason, int time, object[] param) =>
            {
                reason = string.Empty;
                CurrentAlarmTable = param[0].ToString();
                return true;
            });

            DATA.Subscribe($"System.AlarmTableID", () => CurrentAlarmTable);

            EV.InnerEventManager = this;
        }

        public void SubscribeOperationAndData()
        {
            OP.Subscribe("System.ResetAlarm", InvokeResetAlarm);

            DATA.Subscribe("System.ActiveAlarm", () =>
            {
                //return AlarmDic[CurrentAlarmTable].Values.ToList().FindAll(x => !x.IsAcknowledged);
                return EV.GetAlarmEvent();
            });

            DATA.Subscribe("System.HasActiveAlarm", () =>
            {
                //return AlarmDic[CurrentAlarmTable].Values.ToList().FirstOrDefault(x => !x.IsAcknowledged) != null;
                return EV.GetAlarmEvent().FirstOrDefault(x => !x.IsAcknowledged) != null;
            });
        }
        private void BuildItems(string xmlFile)
        {
            XmlDocument xml = new XmlDocument();

            try
            {
                xml.Load(xmlFile);

                XmlNodeList nodeAlarmTables = xml.SelectNodes("Alarms/AlarmTable");
                foreach (XmlElement AlarmTable in nodeAlarmTables)
                {
                    BuildAlarmConfigs(string.IsNullOrEmpty(AlarmTable.GetAttribute("Name")) ? "0" : AlarmTable.GetAttribute("Name"), AlarmTable as XmlElement, true);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        private void BuildAlarmConfigs(string alarmTable, XmlElement configElement, bool isCheckDuplicated)
        {
            XmlNodeList nodeAlarmList = configElement.SelectNodes("AlarmCategory");
            if (!AlarmDic.ContainsKey(alarmTable))
                AlarmDic.Add(alarmTable, new Dictionary<string, EventItem>());
            foreach (XmlElement nodeCategory in nodeAlarmList)
            {
                XmlNodeList nodeAlarms = nodeCategory.SelectNodes("Alarm");
                foreach (XmlElement nodeAlarm in nodeAlarms)
                {
                    EventItem item = new EventItem()
                    {
                        EventEnum = nodeAlarm.GetAttribute("Name"),
                        DisplayName = string.IsNullOrEmpty(nodeAlarm.GetAttribute("DisplayName")) ? nodeAlarm.GetAttribute("Name") : nodeAlarm.GetAttribute("DisplayName"),
                        Id = int.Parse(nodeAlarm.GetAttribute("Id")),
                        Source = nodeAlarm.GetAttribute("Source"),
                        Description = nodeAlarm.GetAttribute("Description"),
                        Explaination = nodeAlarm.GetAttribute("Explaination"),
                        Solution = nodeAlarm.GetAttribute("Solution"),
                        Category = !string.IsNullOrEmpty(nodeCategory.GetAttribute("Name")) ? nodeCategory.GetAttribute("Name") : "",
                        Bypass = !string.IsNullOrEmpty(nodeAlarm.GetAttribute("Bypass")) ? bool.Parse(nodeAlarm.GetAttribute("Bypass")) : false,
                        Editable = !string.IsNullOrEmpty(nodeAlarm.GetAttribute("Editable")) ? bool.Parse(nodeAlarm.GetAttribute("Editable")) : true,
                        AutoRecovery = !string.IsNullOrEmpty(nodeAlarm.GetAttribute("AutoRecovery")) ? bool.Parse(nodeAlarm.GetAttribute("AutoRecovery")) : false,
                        Group = !string.IsNullOrEmpty(nodeAlarm.GetAttribute("Group")) ?
                          (EventGroup)Enum.Parse(typeof(EventGroup), nodeAlarm.GetAttribute("Group")) : EventGroup.Group0,
                        Type = !string.IsNullOrEmpty(nodeAlarm.GetAttribute("Type")) ?
                          (EventType)Enum.Parse(typeof(EventType), nodeAlarm.GetAttribute("Type")) : EventType.EventUI_Notify,
                        Level = !string.IsNullOrEmpty(nodeAlarm.GetAttribute("Level")) ?
                          (EventLevel)Enum.Parse(typeof(EventLevel), nodeAlarm.GetAttribute("Level")) : EventLevel.Alarm,
                        Action = !string.IsNullOrEmpty(nodeAlarm.GetAttribute("Action")) ?
                          (EventAction)Enum.Parse(typeof(EventAction), nodeAlarm.GetAttribute("Action")) : EventAction.Clear,
                    };

                    if (isCheckDuplicated && AlarmDic[alarmTable].ContainsKey(item.EventEnum))
                    {
                        LOG.Error("Duplicated Alarm item, " + item.EventEnum);
                        continue;
                    }

                    AlarmDic[alarmTable][item.EventEnum] = item;
                }
            }
        }
        private void BackupAndRecoverDataFile()
        {
            try
            {
                if (File.Exists(_localAlarmEventDataFile) && IsXmlFileLoadable(_localAlarmEventDataFile))
                {
                    File.Copy(_localAlarmEventDataFile, _localAlarmEventDataBackupFile, true);
                }
                else if (File.Exists(_localAlarmEventDataBackupFile) && IsXmlFileLoadable(_localAlarmEventDataBackupFile))
                {
                    File.Copy(_localAlarmEventDataBackupFile, _localAlarmEventDataFile, true);
                    LOG.Write($"Restore alarm configs from {_localAlarmEventDataBackupFile} to {_localAlarmEventDataFile}");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        private void CustomData()
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            try
            {
                if (File.Exists(_localAlarmEventDataFile))
                {
                    XmlDocument xmlData = new XmlDocument();
                    xmlData.Load(_localAlarmEventDataFile);

                    XmlNodeList nodeAlarmTables = xmlData.SelectNodes("Alarms/AlarmTable");
                    foreach (XmlElement AlarmTable in nodeAlarmTables)
                    {
                        BuildAlarmConfigs(string.IsNullOrEmpty(AlarmTable.GetAttribute("Name")) ? "0" : AlarmTable.GetAttribute("Name"), AlarmTable as XmlElement, false);
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        private void GenerateDataFile()
        {
            try
            {
                lock (_alarmDicLocker)
                {
                    Serialize(_localAlarmEventDataFile, AlarmDic.Keys.ToList());
                }

                if (File.Exists(_localAlarmEventDataFile) && IsXmlFileLoadable(_localAlarmEventDataFile))
                {
                    File.Copy(_localAlarmEventDataFile, _localAlarmEventDataBackupFile, true);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }
        private bool IsXmlFileLoadable(string file)
        {
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(file);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }
        private void Serialize(string pathFile, List<string> alarmTables)
        {
            XmlDocument xmldoc = new XmlDocument();

            if (!File.Exists(pathFile))
            {
                XmlDocument xml = new XmlDocument();
                XmlDeclaration dec = xml.CreateXmlDeclaration("1.0", "utf-8", null);
                xml.AppendChild(dec);
                XmlElement root = xml.CreateElement("Alarms");
                xml.AppendChild(root);
                xml.Save(pathFile);
            }

            xmldoc.Load(pathFile);
            XmlNode rootNode = xmldoc.SelectSingleNode("Alarms");
            rootNode.RemoveAll();
            foreach (var table in alarmTables)
            {
                XmlElement eventTable = xmldoc.CreateElement("AlarmTable");
                eventTable.SetAttribute("Name", table);
                rootNode.AppendChild(eventTable);
            }
            foreach (var table in alarmTables)
            {
                XmlNode table0Node = rootNode.SelectSingleNode($"./AlarmTable[@Name='{table}']");

                List<string> categorys = new List<string>();
                foreach (var dataItem in AlarmDic[table].Values)
                {
                    if (!categorys.Contains(dataItem.Category))
                        categorys.Add(dataItem.Category);
                }
                foreach (var category in categorys)
                {
                    XmlElement cat = xmldoc.CreateElement("AlarmCategory");
                    cat.SetAttribute("Name", category);
                    table0Node.AppendChild(cat);
                }

                if (AlarmDic[table].Values != null)
                {
                    foreach (var alarm in AlarmDic[table].Values)
                    {
                        XmlNode categoryNodes = table0Node.SelectSingleNode($"./AlarmCategory[@Name='{alarm.Category}']");
                        XmlElement subNode = xmldoc.CreateElement("Alarm");
                        subNode.SetAttribute("Name", alarm.EventEnum.ToString());
                        subNode.SetAttribute("DisplayName", string.IsNullOrEmpty(alarm.DisplayName.ToString()) ? alarm.EventEnum.ToString() : alarm.DisplayName.ToString());
                        subNode.SetAttribute("Id", alarm.Id.ToString());
                        subNode.SetAttribute("Source", alarm.Source.ToString());
                        subNode.SetAttribute("Description", alarm.Description.ToString());
                        subNode.SetAttribute("Solution", alarm.Solution.ToString());
                        subNode.SetAttribute("Explaination", alarm.Explaination.ToString());
                        subNode.SetAttribute("Action", alarm.Action.ToString());
                        subNode.SetAttribute("Bypass", alarm.Bypass.ToString());
                        subNode.SetAttribute("Group", alarm.Group.ToString());
                        subNode.SetAttribute("AutoRecovery", alarm.AutoRecovery.ToString());
                        subNode.SetAttribute("Level", alarm.Level.ToString());
                        subNode.SetAttribute("Editable", alarm.Editable.ToString());
                        subNode.SetAttribute("Type", alarm.Type.ToString());
                        subNode.SetAttribute("Category", alarm.Category.ToString());

                        categoryNodes.AppendChild(subNode);
                    }
                }
            }

            xmldoc.Save(pathFile);
        }
        //生成AlarmEventDefine.xml
        public void Serialize()
        {
            var alid = new VIDGenerator("ALID", $"{PathManager.GetCfgDir().Replace("\\bin\\Debug", "")}VIDs\\_ALID.xml");
            alid.Initialize();
            alid.GenerateId(VidAlarmList);

            foreach (var dataItem in AlarmDic[CurrentAlarmTable].Values)
            {
                var item = alid.VIDList.SingleOrDefault(x => x.Name == dataItem.EventEnum);
                if (item != null)
                    dataItem.Id = item.Index;
            }

            string defaultPathFile = $"{PathManager.GetCfgDir().Replace("\\bin\\Debug", "")}AlarmEventDefine.xml";
            Serialize(defaultPathFile, new List<string>() { _defaultAlarmTable });
        }
        private bool InvokeResetAlarm(string arg1, object[] arg2)
        {
            ClearAlarmEvent();

            if (arg2 != null && arg2.Length >= 2)
            {
                var item = AlarmDic[CurrentAlarmTable].Values.ToList().FirstOrDefault(x => x.Source == (string)arg2[0] && x.EventEnum == (string)arg2[1]) as AlarmEventItem;
                if (item != null)
                {
                    item.Reset();
                }
            }
            return true;
        }

        public void Terminate()
        {
            if (_eventJob != null)
            {
                _eventJob.Stop();
                _eventJob = null;
            }

            if (_eventServiceHost != null)
            {
                _eventServiceHost.Close();
                _eventServiceHost = null;
            }
        }

        public void WriteEvent(string eventName)
        {
            if (!_eventDic.ContainsKey(eventName))
            {
                LOG.Write("Event name not registered, " + eventName);
                return;
            }

            WriteEvent(_eventDic[eventName].Source, eventName);
        }

        public void WriteEvent(string module, string eventName, string message)
        {
            if (!_eventDic.ContainsKey(eventName))
            {
                LOG.Write("Event name not registered, " + eventName);
                return;
            }

            EventItem item = _eventDic[eventName].Clone();
            item.Source = module;
            item.Description = message;
            item.OccuringTime = DateTime.Now;

            _eventQueue.Enqueue(item);

            if (item.Level == EventLevel.Alarm || item.Level == EventLevel.Warning)
            {
                lock (_locker)
                {
                    if (_alarmList.Count == _alarmList.Capacity)
                        _alarmList.RemoveAt(0);
                    _alarmList.Add(item);
                }

                if (OnAlarmEvent != null)
                    OnAlarmEvent(item);
            }

            if (OnEvent != null)
            {
                OnEvent(item);
            }

            _writerToLog.WriteEvent(item);

            //WriteEvent(eventName);
        }

        public void WriteEvent(string eventName, SerializableDictionary<string, string> dvid)
        {
            if (!_eventDic.ContainsKey(eventName))
            {
                LOG.Write("Event name not registered, " + eventName);
                return;
            }
            WriteEvent(_eventDic[eventName].Source, eventName, dvid);
        }

        public void WriteEvent(string eventName, SerializableDictionary<string, object> dvid)
        {
            if (!_eventDic.ContainsKey(eventName))
            {
                LOG.Error("Event name not registered, " + eventName);
                return;
            }
            EventItem item = _eventDic[eventName].Clone(dvid);
            item.OccuringTime = DateTime.Now;

            ProceedReceivedEvent(item);
        }

        public void WriteEvent(string module, string eventName, params object[] args)
        {
            EventItem item = _eventDic[eventName].Clone();
            item.Source = module;
            if (_eventDic[eventName].Description == null)
            {
                return;
            }
            item.Description = string.Format(_eventDic[eventName].Description, args);
            if (!string.IsNullOrEmpty(_eventDic[eventName].GlobalDescription_en))
                item.GlobalDescription_en = string.Format(_eventDic[eventName].GlobalDescription_en, args);
            if (!string.IsNullOrEmpty(_eventDic[eventName].GlobalDescription_zh))
                item.GlobalDescription_zh = string.Format(_eventDic[eventName].GlobalDescription_zh, args);
            item.OccuringTime = DateTime.Now;

            _eventQueue.Enqueue(item);

            if (item.Level == EventLevel.Alarm || item.Level == EventLevel.Warning)
            {
                lock (_locker)
                {
                    if (_alarmList.Count == _alarmList.Capacity)
                        _alarmList.RemoveAt(0);
                    _alarmList.Add(item);
                }

                if (OnAlarmEvent != null)
                    OnAlarmEvent(item);
            }

            if (OnEvent != null)
            {
                OnEvent(item);
            }

            _writerToLog.WriteEvent(item);
        }

        public void WriteEvent(string module, string eventName, SerializableDictionary<string, string> dvid, params object[] args)
        {
            EventItem item = _eventDic[eventName].Clone();
            item.Source = module;
            item.Description = string.Format(_eventDic[eventName].Description, args);
            if (!string.IsNullOrEmpty(_eventDic[eventName].GlobalDescription_en))
                item.GlobalDescription_en = string.Format(_eventDic[eventName].GlobalDescription_en, args);
            if (!string.IsNullOrEmpty(_eventDic[eventName].GlobalDescription_zh))
                item.GlobalDescription_zh = string.Format(_eventDic[eventName].GlobalDescription_zh, args);
            item.OccuringTime = DateTime.Now;
            item.DVID = dvid;

            _eventQueue.Enqueue(item);

            if (item.Level == EventLevel.Alarm || item.Level == EventLevel.Warning)
            {
                lock (_locker)
                {
                    if (_alarmList.Count == _alarmList.Capacity)
                        _alarmList.RemoveAt(0);
                    _alarmList.Add(item);
                }

                if (OnAlarmEvent != null)
                    OnAlarmEvent(item);
            }

            if (OnEvent != null)
            {
                OnEvent(item);
            }

            _writerToLog.WriteEvent(item);
        }

        private void ProceedReceivedEvent(EventItem item)
        {
            _eventQueue.Enqueue(item);

            if (item.Level == EventLevel.Alarm || item.Level == EventLevel.Warning)
            {
                lock (_locker)
                {
                    if (_alarmList.Count == _alarmList.Capacity)
                        _alarmList.RemoveAt(0);
                    _alarmList.Add(item);
                }

                if (OnAlarmEvent != null)
                    OnAlarmEvent(item);
            }

            if (OnEvent != null)
            {
                OnEvent(item);
            }

            _writerToLog.WriteEvent(item);
        }

        public void PostNotificationMessage(string message)
        {
            var eventItem = new EventItem()
            {
                Type = EventType.UIMessage_Notify,
                Description = message,
                OccuringTime = DateTime.Now,
            };
            _eventQueue.Enqueue(eventItem);

            _writerToLog.WriteEvent(eventItem);
        }

        public void PostPopDialogMessage(EventLevel level, string title, string message)
        {
            var eventItem = new EventItem()
            {
                Type = EventType.Dialog_Nofity,
                Description = title,
                Explaination = message,
                OccuringTime = DateTime.Now,
                Level = level,
            };
            _eventQueue.Enqueue(eventItem);
            _writerToLog.WriteEvent(eventItem);
        }

        public void PostKickoutMessage(string message)
        {
            var eventItem = new EventItem()
            {
                Type = EventType.KickOut_Notify,
                Description = message,
                OccuringTime = DateTime.Now,
                Level = EventLevel.Information
            };
            _eventQueue.Enqueue(eventItem);
            _writerToLog.WriteEvent(eventItem);
        }

        public void PostSoundMessage(string message)
        {
            var eventItem = new EventItem()
            {
                Type = EventType.Sound_Notify,
                Description = message,
                OccuringTime = DateTime.Now,
            };
            _eventQueue.Enqueue(eventItem);
            _writerToLog.WriteEvent(eventItem);
        }


        bool PeriodicRun()
        {
            EventItem ev;

            while (_eventQueue.TryDequeue(out ev))
            {
                try
                {
                    if (_eventDB != null)
                        _eventDB.WriteEvent(ev);

                    //_writerToLog.WriteEvent(ev);

                    if (_writerToMail != null)
                        _writerToMail.WriteEvent(ev);

                    if (_eventService != null)
                        _eventService.FireEvent(ev);

                    if (FireEvent != null)
                        FireEvent(ev);
                }
                catch (Exception ex)
                {
                    LOG.Error("Failed to post event", ex);
                }
            }

            return true;
        }


        public List<EventItem> GetAlarmEvent()
        {
            return _alarmList.ToList();
        }

        public void ClearAlarmEvent()
        {
            _alarmList.Clear();
        }


        public List<EventItem> QueryDBEvent(string sql)
        {
            return _eventDB.QueryDBEvent(sql);

        }

        public void Subscribe(EventItem item)
        {
            if (_eventDic.ContainsKey(item.EventEnum))
            {
                return;
            }

            _eventDic[item.EventEnum] = item;

            if (item is AlarmEventItem && AlarmDic.ContainsKey(_defaultAlarmTable) && !AlarmDic[_defaultAlarmTable].ContainsKey(item.EventEnum))
            {
                AlarmDic[_defaultAlarmTable][item.EventEnum] = item;
            }
        }

        public void PostInfoLog(string module, string message)
        {
            WriteEvent(module, INFORMATION_EVENT, message);
        }

        public void PostWarningLog(string module, string message)
        {
            WriteEvent(module, WARNING_EVENT, message);
        }

        public void PostAlarmLog(string module, string message)
        {
            WriteEvent(module, ALARM_EVENT, message);
        }
        //只有通过AlarmEventDefine.xml定义的warning，alarm可以通过此方法抛出
        public void PostAlarmDefineLog(string module, string name, string additionalDescription)
        {
            if (!AlarmDic[_defaultAlarmTable].ContainsKey(name))
            {
                LOG.Write("Event name not registered, " + name);
                return;
            }

            var newItem = AlarmDic[_defaultAlarmTable][name].Clone();
            if (!string.IsNullOrEmpty(additionalDescription))
                newItem.Description += $" {additionalDescription}";
            newItem.OccuringTime = DateTime.Now;
            newItem.Source = module;
            if (newItem.Bypass)
                LOG.Write("Bypass event: " + name);
            else
                ProceedReceivedEvent(newItem);
        }
        public void ClearAlarmEvent(string name)
        {
            var alarm = _alarmList.ToList().FirstOrDefault(x => x.EventEnum == name);
            if (alarm != null)
            {
                lock (_locker)
                {
                    _alarmList.Remove(alarm);
                }
            }
        }
    }
}
