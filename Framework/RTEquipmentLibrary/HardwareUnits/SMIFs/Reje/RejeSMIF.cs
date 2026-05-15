using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.SMIFs.Reje
{
    public class RejeSMIF : SMIFBase, IConnection
    {
        private string _address = "";
        public string Address { get { return _address; } }
        public override bool IsConnected { get; set; }

        //public bool 
        public bool Connect()
        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }

        public string PortStatus { get; set; } = "Closed";

        private RejeSMIFConnection _connection;
        public RejeSMIFConnection Connection
        {
            get { return _connection; }
        }

        public List<IOResponse> IOResponseList { get; set; } = new List<IOResponse>();
        public int Axis { get; private set; }
        public override bool IsIdle { get; set; }
        public override bool IsHomed { get; set; }
        public override bool IsPodPresent { get; set; }
        public override bool IsArmRetract { get; set; }
        public override bool IsAlarm { get; set; }

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

        private object _locker = new object();

        private bool _enableLog;
        private string _errorCode = "";

        private DeviceTimer _QueryTimer = new DeviceTimer();
        private int _QueryInterval = 1000;

        private string _scRoot;

        public RejeSMIF(string Module, string Name, string scRoot)
        {
            //_enableLog = SC.GetValue<bool>($"SMIF.{Name}.EnableLogMessage");
            _scRoot = scRoot;
            _connection = new RejeSMIFConnection(this, _scRoot);
            _connection.EnableLog(_enableLog);

            base.Module = Module;
            base.Name = Name;

            //_lstMonitorHandler.AddLast(new RejeSMIFLoadCassetteHeandler(this));
            //_lstMonitorHandler.AddLast(new RejeSMIFUnLoadCassetteHeandler(this));
            //_lstMonitorHandler.AddLast(new RejeSMIFORGSHHeandler(this));
            //_lstMonitorHandler.AddLast(new RejeSMIFResetHeandler(this));
            //_lstMonitorHandler.AddLast(new RejeSMIFSetHomePositionHeandler(this,1000));
            _lstMonitorHandler.AddLast(new RejeSMIFGetStatusHandler(this));

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }
            else
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connect failed");
            }


            _QueryTimer.Start(_QueryInterval);

            _thread = new PeriodicJob(1000, OnTimer, $"{Name} MonitorHandler", true);
        }

        private bool OnTimer()
        {
            try
            {
                //return true;
                _connection.MonitorTimeout();

                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        if (string.IsNullOrEmpty(_scRoot))
                        {
                            _connection.SetPortAddress(SC.GetStringValue($"{Module}.DeviceAddress"));
                        }
                        else
                        {
                            _connection.SetPortAddress(SC.GetStringValue($"{_scRoot}.{Module}.{Name}.DeviceAddress"));
                        }
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}");
                        }
                        else
                        {
                            //_lstHandler.AddLast(new BrooksSMIFQueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new BrooksSMIFSetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
                        }
                    }
                    return true;
                }

                HandlerBase handler = null;
                if (!_connection.IsBusy)
                {
                    lock (_locker)
                    {
                        if (_lstHandler.Count == 0)
                        {
                            foreach (var monitorHandler in _lstMonitorHandler)
                            {
                                _lstHandler.AddLast(monitorHandler);
                            }

                        }

                        if (_lstHandler.Count > 0)
                        {
                            handler = _lstHandler.First.Value;

                            _lstHandler.RemoveFirst();
                        }
                    }

                    if (handler != null)
                    {
                        _connection.Execute(handler);
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        public override bool HomeSmif(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new RejeSMIFHomeHeandler(this));
            }
            return true;
        }


        public bool Stop(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new RejeSMIFStopHeandler(this));
                _lstHandler.AddLast(new RejeSMIFORGSHHeandler(this));
            }
            return true;
        }

        public override bool LoadCassette(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new RejeSMIFLoadCassetteHeandler(this));
            }
            return true;
        }

        public override bool UnloadCassette(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new RejeSMIFUnLoadCassetteHeandler(this));
            }
            return true;
        }

        public void OnAbs(string absMsg)
        {
            try
            {
                string absContext = absMsg.Split('/')[1].Replace(";", "").Replace("\r", "");

                NoteError(absContext, 1);


            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public void OnNak(string absMsg)
        {
            try
            {
                string absContext = absMsg.Split('/')[1].Replace(";", "").Replace("\r", "");

                NoteError(absContext, 2);
                //EV.Notify($"{Name}{absContext}");

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }



        }




        internal void OnEventArrived(string eventData)
        {
            if (eventData == "ONMGV")
            {
                ONMGVComplete = true;
            }
            if (eventData == "MENTE")
            {
                MENTEComlete = true;
            }
            if (eventData == "TEACH")
            {
                TEACHComlete = true;
            }
            if (eventData == "STOP_")
            {
                STOPComlete = true;
            }
            if (eventData == "ORGJZ")
            {
                ORGJZComlete = true;
            }
            if (eventData == "ORGJL")
            {
                ORGJLComlete = true;
            }
            if (eventData == "DOROP")
            {
                DOROPComlete = true;
            }
            if (eventData == "DORCL")
            {
                DORCLComlete = true;
            }
            if (eventData == "HOME_")
            {
                HOMEComlete = true;
            }
            if (eventData == "STAGE")
            {
                STAGEComlete = true;
            }
            if (eventData == "TEACH")
            {
                TEACHComlete = true;
            }
            if (eventData == "MAPDO")
            {
                MAPDOComlete = true;
            }
            if (eventData == "ORGSH")
            {
                ORGSHComlete = true;
            }
            if (eventData == "CLDMP")
            {
                CLDMPComlete = true;
            }
            if (eventData == "RESET")
            {
                RESETComlete = true;
            }
        }

        #region Properties
        /// <summary>
        /// 设置 onLine模式完成
        /// </summary>
        public bool ONMGVComplete { get; private set; }

        /// <summary>
        /// 设置维护模式完成
        /// </summary>
        public bool MENTEComlete { get; private set; }

        /// <summary>
        /// 设置示教模式完成
        /// </summary>
        public bool TEACHComlete { get; private set; }

        /// <summary>
        /// 执行紧急停止完成
        /// </summary>
        public bool STOPComlete { get; private set; }
        /// <summary>
        /// 移动到Stage位置完成
        /// </summary>
        public bool ORGJZComlete { get; private set; }

        /// <summary>
        /// 执行SMIFPod Base打开完成
        /// </summary>
        public bool ORGJLComlete { get; private set; }





        /// <summary>
        /// 执行SMIFPod Base关闭完成
        /// </summary>
        public bool DOROPComlete { get; private set; }

        /// <summary>
        ///执行SMIFPod Base打开完成
        /// </summary>
        public bool DORCLComlete { get; private set; }

        /// <summary>
        /// 上移到HOME位置
        /// </summary>
        public bool HOMEComlete { get; private set; }

        /// <summary>
        /// 执行 Z 轴下移到 stage 位置
        /// </summary>
        public bool STAGEComlete { get; private set; }

        /// <summary>
        /// 执行 Mapping 动作完成
        /// </summary>
        public bool MAPDOComlete { get; private set; }


        /// <summary>
        /// 系统搜索原点.系统重新上电时先进行搜索原点动作完成
        /// </summary>
        public bool ORGSHComlete { get; private set; }

        /// <summary>
        /// 打开 SMIFPod 并 MAP 扫描晶圆状态完成
        /// </summary>
        public bool CLDMPComlete { get; private set; }

        /// <summary>
        /// 重置完成
        /// </summary>
        public bool RESETComlete { get; private set; }
        #endregion

        #region Note Functions
        private R_TRIG _trigWarningMessage = new R_TRIG();


        public void NoteError(string errorData, int MsgType)
        {
            if (errorData != null)
            {
                _trigWarningMessage.CLK = true;
                if (_trigWarningMessage.Q)
                {
                    if (MsgType == 1)
                    {
                        EV.PostWarningLog(Module, $"{Module} error, {(_ASBMessageDict.ContainsKey(errorData) ? _ASBMessageDict[errorData] : errorData)}");
                    }
                    else if (MsgType == 2)
                    {
                        EV.PostWarningLog(Module, $"{Module} error, {(_NAKMessageDict.ContainsKey(errorData) ? _NAKMessageDict[errorData] : errorData)}");
                    }
                    else
                    {
                        EV.PostWarningLog(Module, $"{Module} error, {(_ErrorMessageDict.ContainsKey(errorData) ? _ErrorMessageDict[errorData] : errorData)}");
                    }
                }
            }
        }


        private static Dictionary<string, string> _ASBMessageDict = new Dictionary<string, string>()
            {
                {"COMER","Instruction content is incorrect" },
                {"ZDALM","Z axis alram" },
                {"LDALM","L axis alram" },
                {"ZCRLER","Z axis control system. alram" },
                {"LCRLER","L axis control system. alram" },
                {"LPOSI","SMIF pod bottom switch not change" },
                {"PROTS","Wafer out" },
                 {"PODNG","SMIF pod current status not change" },
                {"TIMOT","Action timeout alarm" },
                {"WFCNG","Place more than 2 wafers" },
                 {"PITNG","Pitch value Settings do not match" },
            };

        private static Dictionary<string, string> _NAKMessageDict = new Dictionary<string, string>()
            {
                {"CMDCOMERER","Serial port error" },
                {"CBUSY","Opeation is busy" },
                {"ORGYT","Origin not searched" },
                {"ZDALM","Z axis alram" },
                {"LDALM","L axis alram" },
                {"ZCRLER","Z axis control system. alram" },
                {"LCRLER","L axis control system. alram" },
                {"LPOSI","SMIF pod bottom switch status error " },
                {"PROTS","Wafer out" },
                {"SMILG","SMIF pod position error" },
                {"MODNG","System mode does not match" },
                {"MAPNE","Map class number does not exist" },
                {"MAPUD","No MAP scan was" },
                {"ZPOSI","Z axis Out of place" },
                {"NOPAR","Wafer parameters are not set" },
            };

        private static Dictionary<string, string> _ErrorMessageDict = new Dictionary<string, string>()
            {
                {"10","Z axis control system error" },
                {"70","L axis control system error" },
                {"11","Z axis speed setting abnormal" },
                {"71","L axis speed setting abnormal" },
                {"12","Z axis specified number is out of range" },
                {"72","L axis specified number is out of range" },
                {"13","Z axis reach the limit" },
                {"73","L axis reach the limit" },
                {"14","Z axis forced to stop" },
                {"74","L axis forced to stop" },
                {"18","Z axis Number does not exist " },
                {"78","L axis Number does not exist " },
                {"19","Z axis system error" },
                {"79","L axis system error" },
                {"1E","Z axis other axis alram" },
                {"7E","L axis axis alram" },
                {"22","L axis motor error" },
                {"032","Wafer out" },
                {"33","L axis position error" },
                {"34","SMIF pod  position error" },
                {"42","Z axis motor error" },
                {"43","Action timeout alarm" },
                {"62","Instruction content is incorrect" },
                {"63","Abnormal communication" },
            };

        #endregion
    }
}
