using Aitex.Common.Util;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SCCore;
using MECF.Framework.Common.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace MECF.Framework.Common.FAServices
{

    /// <summary>
    /// 所有ID 的命名规则：
    ///
    /// Module(1-399) + (Type+Unit)(1-999) + Parameter (1-9999)
    ///
    /// 最小 1 001 0001  = 10010001
    ///
    /// 最大 399 999 9999 = 3 999 999 999
    ///
    /// "PPP" 一部分的就是按参数  module=1, type=1, unit=1, parameter="AAA"
    ///   
    /// 
    /// </summary>
    ///
    ///
    public class VIDGenerator2
    {
        public string Type { get; set; }

        public string SourceFileName { get; set; }

        public List<VIDItem> VIDList = new List<VIDItem>();

        public Dictionary<string, int> _moduleIndex = new Dictionary<string, int>();
        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> moduleTypeParamIndex = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();

        public Dictionary<string, int> _index = new Dictionary<string, int>();

        private string _defaultPathFile;

        private string _type;

        public VIDGenerator2(string type, string defaultPathFile)
        {
            _type = type;
            _defaultPathFile = defaultPathFile;
        }

        public void Initialize()
        {
            XmlDocument xml = new XmlDocument();

            try
            {

                EnumLoop<ModuleName>.ForEach((item) =>
                {
                    moduleTypeParamIndex[item.ToString()] = new Dictionary<string, Dictionary<string, int>>();
                });

                if (!File.Exists(_defaultPathFile))
                    return;

                xml.Load(_defaultPathFile);
                VIDList = CustomXmlSerializer.Deserialize<List<VIDItem>>(xml.OuterXml);
                foreach (var VIDItem in VIDList)
                {
                    //_moduleTypeIndex[VIDItem.Type] = VIDItem.TypeIndex;
                    //_moduleTypeUnitIndex[VIDItem.Unit] = VIDItem.UnitIndex;
                    //_parameterIndex[VIDItem.Parameter] = VIDItem.ParameterIndex;

                    if (!moduleTypeParamIndex.ContainsKey(VIDItem.Module))
                    {
                        moduleTypeParamIndex[VIDItem.Module] = new Dictionary<string, Dictionary<string, int>>();
                    }

                    if (!moduleTypeParamIndex[VIDItem.Module].ContainsKey(VIDItem.Type + VIDItem.Unit))
                    {
                        moduleTypeParamIndex[VIDItem.Module][VIDItem.Type + VIDItem.Unit] = new Dictionary<string, int>();
                    }

                    moduleTypeParamIndex[VIDItem.Module][VIDItem.Type + VIDItem.Unit][VIDItem.Parameter] = VIDItem.Index;

                    _index[VIDItem.Name] = VIDItem.Index;
                }
                //XmlNodeList itemNodes = xml.SelectNodes("DataItems/DataItem");
                //foreach (var itemNode in itemNodes)
                //{
                //    XmlElement element = itemNode as XmlElement;
                //    if (element == null)
                //        continue;

                //    string name = element.GetAttribute("name").Trim();
                //    string index = element.GetAttribute("index").Trim();

                //    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(index) || (index.Length != 9 && index.Length != 8))
                //        continue;

                //    var item = ParseName(name);
                //    _moduleTypeIndex[$"{item.Item1}.{item.Item2}"] = int.Parse(index.Substring(index.Length - 7, 2));
                //    _moduleTypeUnitIndex[$"{item.Item1}.{item.Item2}.{item.Item3}"] = int.Parse(index.Substring(index.Length - 5, 2));
                //    _parameterIndex[$"{item.Item1}.{item.Item2}.{item.Item3}.{item.Item4}"] =
                //        int.Parse(index.Substring(index.Length - 3, 3));

                //    if (!_max.ContainsKey($"{item.Item1}"))
                //        _max[$"{item.Item1}"] = 0;
                //    _max[$"{item.Item1}"] =
                //        Math.Max(_moduleTypeIndex[$"{item.Item1}.{item.Item2}"], _max[$"{item.Item1}"]);

                //    if (!_max.ContainsKey($"{item.Item1}.{item.Item2}"))
                //        _max[$"{item.Item1}.{item.Item2}"] = 0;
                //    _max[$"{item.Item1}.{item.Item2}"] =
                //        Math.Max(_moduleTypeUnitIndex[$"{item.Item1}.{item.Item2}.{item.Item3}"], _max[$"{item.Item1}.{item.Item2}"]);

                //    if (!_max.ContainsKey($"{item.Item1}.{item.Item2}.{item.Item3}"))
                //        _max[$"{item.Item1}.{item.Item2}.{item.Item3}"] = 0;
                //    _max[$"{item.Item1}.{item.Item2}.{item.Item3}"] =
                //        Math.Max(_parameterIndex[$"{item.Item1}.{item.Item2}.{item.Item3}.{item.Item4}"], _max[$"{item.Item1}.{item.Item2}.{item.Item3}"]);

                //    _index[name] = int.Parse(index);
                //}

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            EnumLoop<ModuleName>.ForEach((item) =>
            {
                _moduleIndex[item.ToString()] = ((int)item) + 1;
            });

        }


        public Tuple<string, string, string, string> ParseName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            string module = ModuleName.System.ToString();
            string type = "";
            string unit = "";
            string parameter = "";

            string[] names = name.Split('.');

            if (names.Length >= 1)
            {
                parameter = names[names.Length - 1];
                if (names.Length >= 2)
                {
                    if (_moduleIndex.ContainsKey(names[0]))
                    {
                        module = names[0];

                        if (names.Length >= 3)
                        {
                            unit = names[names.Length - 2];

                            if (names.Length >= 4)
                            {
                                for (int j = 1; j < names.Length - 2; j++)
                                {
                                    type += names[j];
                                    if (j != names.Length - 3)
                                        type += ".";
                                }
                            }
                        }
                    }
                    else
                    {
                        //module = module;

                        unit = names[names.Length - 2];

                        if (names.Length >= 3)
                        {
                            for (int j = 0; j < names.Length - 2; j++)
                            {
                                type += names[j];
                                if (j != names.Length - 3)
                                    type += ".";
                            }
                        }
                    }
                }
            }

            return Tuple.Create(module, type, unit, parameter);
        }

        public void GenerateId(List<VIDItem> dataList)
        {
            List<VIDItem> newList = new List<VIDItem>();
            foreach (var data in dataList)
            {
                if (!_index.ContainsKey(data.Name))
                    newList.Add(data);
            }

            if (newList.Count > 0)
            {
                AssignNewId(newList);
                VIDList = VIDList.Concat(newList).OrderBy(x => x.Index).ToList();
                CustomXmlSerializer.Serialize(VIDList, _defaultPathFile);
            }
        }

        private void AssignNewId(List<VIDItem> dataList)
        {
            if (dataList.Count == 0)
                return;
            dataList = dataList.OrderBy(x => x.Name).ToList();
            foreach (var data in dataList)
            {
                data.Name = data.Name;

                if (!moduleTypeParamIndex.ContainsKey(data.Module))
                {
                    moduleTypeParamIndex[data.Module] = new Dictionary<string, Dictionary<string, int>>();
                    moduleTypeParamIndex[data.Module][data.Type + data.Unit] = new Dictionary<string, int>();

                    data.ModuleIndex = moduleTypeParamIndex.Keys.ToList().FindIndex(x => x == data.Module) + 1;
                    data.TypeIndex = 1;
                    data.UnitIndex = 1;
                    data.ParameterIndex = 1;

                    moduleTypeParamIndex[data.Module][data.Type + data.Unit][data.Parameter] = int.Parse($"{ data.ModuleIndex}{data.TypeIndex.ToString().PadLeft(3, '0')}{ data.ParameterIndex.ToString().PadLeft(4, '0')}");
                    data.Index = moduleTypeParamIndex[data.Module][data.Type + data.Unit][data.Parameter];
                    continue;
                }

                if (!moduleTypeParamIndex[data.Module].ContainsKey(data.Type + data.Unit))
                {
                    moduleTypeParamIndex[data.Module][data.Type + data.Unit] = new Dictionary<string, int>();

                    data.ModuleIndex = moduleTypeParamIndex.Keys.ToList().FindIndex(x => x == data.Module) + 1;
                    data.TypeIndex = moduleTypeParamIndex[data.Module].Count;
                    data.UnitIndex = 1;
                    data.ParameterIndex = 1;

                    moduleTypeParamIndex[data.Module][data.Type + data.Unit][data.Parameter] = int.Parse($"{ data.ModuleIndex}{data.TypeIndex.ToString().PadLeft(3, '0')}{ data.ParameterIndex.ToString().PadLeft(4, '0')}");
                    data.Index = moduleTypeParamIndex[data.Module][data.Type + data.Unit][data.Parameter];
                    continue;
                }


                moduleTypeParamIndex[data.Module][data.Type + data.Unit][data.Parameter] = moduleTypeParamIndex[data.Module][data.Type + data.Unit].Last().Value + 1;
                data.Index = moduleTypeParamIndex[data.Module][data.Type + data.Unit][data.Parameter];

            }



            //XmlDocument xml = new XmlDocument();

            //try
            //{
            //    xml.Load(_defaultPathFile);

            //    XmlNode itemNodes = xml.SelectSingleNode("DataItems");
            //    itemNodes.RemoveAll();

            //    Dictionary<int, string> orderedName = _index.OrderBy(o => o.Value).ToDictionary(p => p.Value, o => o.Key);

            //    foreach (var name in orderedName)
            //    {

            //        XmlElement subNode = xml.CreateElement("DataItem");
            //        subNode.SetAttribute("name", name.Value);
            //        subNode.SetAttribute("index", name.Key.ToString());
            //        itemNodes.AppendChild(subNode);
            //    }

            //    xml.Save(_defaultPathFile);
            //}
            //catch (Exception ex)
            //{
            //    LOG.Write(ex);
            //}

        }

    }

    public class VIDManager2 : Singleton<VIDManager2>
    {

        public void Initialize(string equipName, bool enableGem300Events = false, bool needReGenerateGemModelXml = false)
        {
            if (!System.Diagnostics.Debugger.IsAttached)
                return;

            //SVID
            var svid = new VIDGenerator2("SVID", $"{PathManager.GetCfgDir().Replace("\\bin\\Debug", "")}VIDs\\_SVID.xml");
            svid.Initialize();
            svid.GenerateId(Singleton<DataManager>.Instance.VidDataList);
            ExportSvid(svid.VIDList, true, true);

            //ECID
            var ecid = new VIDGenerator2("ECID", $"{PathManager.GetCfgDir().Replace("\\bin\\Debug", "")}VIDs\\_ECID.xml");
            ecid.Initialize();
            ecid.GenerateId(SystemConfigManager.Instance.VidConfigList);
            ExportEcid(ecid.VIDList);

            //CEID
            List<VIDItem> ceids = new List<VIDItem>();
            foreach (var eventItem in Singleton<EventManager>.Instance.VidEventList)
            {
                if (UniversalEvents.UniversalEventsDictionary.ContainsKey(eventItem.Name))
                {
                    ceids.Add(UniversalEvents.UniversalEventsDictionary[eventItem.Name]);
                }
                else if (enableGem300Events && Gem300Events.Gem300EventsDictionary.ContainsKey(eventItem.Name))
                {
                    ceids.Add(Gem300Events.Gem300EventsDictionary[eventItem.Name]);
                }
            }
            ceids = ceids.OrderBy(x => x.Index).ToList();
            CustomXmlSerializer.Serialize(ceids, $"{PathManager.GetCfgDir().Replace("\\bin\\Debug", "")}VIDs\\_CEID.xml");
            ExportCeid(ceids);

            //ALID
            var alid = new VIDGenerator2("ALID", $"{PathManager.GetCfgDir().Replace("\\bin\\Debug", "")}VIDs\\_ALID.xml");
            alid.Initialize();
            alid.GenerateId(Singleton<EventManager>.Instance.VidAlarmList);
            ExportAlid(alid.VIDList);

            //DVID, to be designed
            Dictionary<string, VIDItem> dvids = new Dictionary<string, VIDItem>();
            foreach (var eventItem in ceids)
            {
                if (eventItem.LinkableVid == null)
                    continue;
                foreach (var LinkableVid in eventItem.LinkableVid)
                {
                    string dvidName = ((DataVariables.DataName)LinkableVid).ToString();
                    if (DataVariables.DataVariablesDictionary.ContainsKey(dvidName))
                        dvids[dvidName] = DataVariables.DataVariablesDictionary[dvidName];
                }
            }

            var dvidList = dvids.Values.ToList().OrderBy(x => x.Index).ToList();
            CustomXmlSerializer.Serialize(dvidList, $"{PathManager.GetCfgDir().Replace("\\bin\\Debug", "")}VIDs\\_DVID.xml");
            ExportDvid(dvidList);

            //ReGenerate GemModel.Xml
            if (needReGenerateGemModelXml)
                ReGenerateGemModelXml(svid.VIDList, ecid.VIDList, dvidList, ceids, alid.VIDList, equipName);
        }

        private List<VIDItem> OriginalSvids = new List<VIDItem>()
        {
            new VIDItem(){Name = "AlarmsEnabled",Index = 1,DataType = "List"},
            new VIDItem(){Name = "AlarmsSet",Index = 2,DataType = "List"},
            new VIDItem(){Name = "Clock",Index = 3,DataType = "Ascii"},
            new VIDItem(){Name = "ControlState",Index = 4,DataType = "U4"},
            new VIDItem(){Name = "EventsEnabled",Index = 5,DataType = "List"},
            new VIDItem(){Name = "PPExecName",Index = 6,DataType = "Ascii"},
            new VIDItem(){Name = "PreviousProcessState",Index = 7,DataType = "U1"},
            new VIDItem(){Name = "ProcessState",Index = 8,DataType = "U1"},
            new VIDItem(){Name = "SpoolCountActual",Index = 9,DataType = "U4"},
            new VIDItem(){Name = "SpoolCountTotal",Index = 10,DataType = "U4"},
            new VIDItem(){Name = "SpoolFullTime",Index = 11,DataType = "Ascii"},
            new VIDItem(){Name = "SpoolStartTime",Index = 12,DataType = "Ascii"},
            new VIDItem(){Name = "SpoolState",Index = 13,DataType = "Ascii"},
            new VIDItem(){Name = "SpoolSubstate",Index = 14,DataType = "Ascii"},
        };

        private List<VIDItem> OriginalEcids = new List<VIDItem>()
        {
            new VIDItem(){Name = "EstablishCommunicationsTimeout",Index = 2000,DataType = "U2",Description = "2"},
            new VIDItem(){Name = "MaxSpoolTransmit",Index = 2001,DataType = "U4",Description = "100"},
            new VIDItem(){Name = "OverWriteSpool",Index = 2003,DataType = "Boolean",Description = "FALSE"},
            new VIDItem(){Name = "MaxSpoolCapacity",Index = 2005,DataType = "U4",Description = "100"},
            new VIDItem(){Name = "SpoolEnabled",Index = 2006,DataType = "Boolean",Description = "False"},
            new VIDItem(){Name = "TimeFormat",Index = 2007,DataType = "U1",Description = "0"},

        };

        private void ExportAlid(List<VIDItem> dataList, bool defaultPath = true, bool createNewFile = false)
        {
            var lists = dataList.OrderBy(x => x.ModuleIndex).ThenBy(x => x.Name).ToList();
            bool? result = defaultPath;
            string savePath = $"{PathManager.GetCfgDir().Replace("\\bin\\Debug", "")}VIDs\\Equipment_VIDs_{DateTime.Now:yyyyMMdd}.xlsx";

            if (!defaultPath)
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.DefaultExt = ".xlsx"; // Default file extension 
                dlg.FileName = $"Equipment_VIDs_{DateTime.Now:yyyyMMdd_HHmmss}";
                dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
                result = dlg.ShowDialog();// Show open file dialog box
                savePath = dlg.FileName;
            }

            if (result == true)
            {
                System.Data.DataSet ds = new System.Data.DataSet();
                ds.Tables.Add(new System.Data.DataTable("ALID(Alarm ID)"));
                ds.Tables[0].Columns.Add("ALID");
                ds.Tables[0].Columns.Add("Name");
                ds.Tables[0].Columns.Add("Description");
                ds.Tables[0].Columns.Add("AlarmSet");
                ds.Tables[0].Columns.Add("AlarmClear");

                Dictionary<int, int> IdDictionary = new Dictionary<int, int>();
                foreach (var item in lists)
                {
                    var row = ds.Tables[0].NewRow();

                    if (!IdDictionary.ContainsKey(item.ModuleIndex))
                        IdDictionary[item.ModuleIndex] = 0;

                    row[0] = item.Index;
                    row[1] = item.Name;
                    if (string.IsNullOrEmpty(item.Description))
                    {
                        var arr = item.Name.Split('.');
                        for (int i = 0; i < arr.Length; i++)
                            row[2] += $"{arr[i]} ";
                    }
                    else
                    {
                        row[2] = item.Description;
                    }
                    row[3] = $"10{item.ModuleIndex.ToString().PadLeft(3, '0')}01";
                    row[4] = $"20{item.ModuleIndex.ToString().PadLeft(3, '0')}01";
                    ds.Tables[0].Rows.Add(row);
                }

                if (!ExcelHelper.ExportToExcel(savePath, ds, out string reason, createNewFile))
                {
                    LOG.Write($"Export failed, {reason}");
                    return;
                }

                LOG.Write($"Export succeed, file save as {savePath}");
            }
        }

        private void ExportCeid(List<VIDItem> dataList, bool defaultPath = true, bool createNewFile = false)
        {
            var lists = dataList.OrderBy(x => x.Index).ToList();
            bool? result = defaultPath;
            string savePath = $"{PathManager.GetCfgDir().Replace("\\bin\\Debug", "")}VIDs\\Equipment_VIDs_{DateTime.Now:yyyyMMdd}.xlsx";

            if (!defaultPath)
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.DefaultExt = ".xlsx"; // Default file extension 
                dlg.FileName = $"Equipment_VIDs_{DateTime.Now:yyyyMMdd_HHmmss}";
                dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
                result = dlg.ShowDialog();// Show open file dialog box
                savePath = dlg.FileName;
            }

            if (result == true)
            {
                System.Data.DataSet ds = new System.Data.DataSet();
                ds.Tables.Add(new System.Data.DataTable("CEID(Collection Events)"));
                ds.Tables[0].Columns.Add("CEID");
                ds.Tables[0].Columns.Add("Name");
                ds.Tables[0].Columns.Add("LinkableVID");
                ds.Tables[0].Columns.Add("Description");

                Dictionary<int, int> IdDictionary = new Dictionary<int, int>();
                foreach (var item in lists)
                {
                    var row = ds.Tables[0].NewRow();

                    if (!IdDictionary.ContainsKey(item.ModuleIndex))
                        IdDictionary[item.ModuleIndex] = 0;

                    row[0] = item.Index;
                    row[1] = item.Name;
                    if (item.LinkableVid != null)
                    {
                        string LinkableVidDescription = String.Empty;
                        for (int i = 0; i < item.LinkableVid.Length; i++)
                        {
                            LinkableVidDescription += $"{(DataVariables.DataName)item.LinkableVid[i]} = {item.LinkableVid[i]} \r\n";
                        }
                        row[2] = LinkableVidDescription;
                    }

                    if (string.IsNullOrEmpty(item.Description))
                    {
                        var arr = item.Name.Split('.');
                        for (int i = 0; i < arr.Length; i++)
                            row[3] += $"{arr[i]} ";
                    }
                    else
                    {
                        row[3] = item.Description;
                    }
                    ds.Tables[0].Rows.Add(row);
                }

                if (!ExcelHelper.ExportToExcel(savePath, ds, out string reason, createNewFile))
                {
                    LOG.Write($"Export failed, {reason}");
                    return;
                }

                LOG.Write($"Export succeed, file save as {savePath}");
            }
        }

        private void ExportEcid(List<VIDItem> dataList, bool defaultPath = true, bool createNewFile = false)
        {
            var lists = dataList.OrderBy(x => x.Index).ToList();
            bool? result = defaultPath;
            string savePath = $"{PathManager.GetCfgDir().Replace("\\bin\\Debug", "")}VIDs\\Equipment_VIDs_{DateTime.Now:yyyyMMdd}.xlsx";

            if (!defaultPath)
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.DefaultExt = ".xlsx"; // Default file extension 
                dlg.FileName = $"Equipment_VIDs_{DateTime.Now:yyyyMMdd_HHmmss}";
                dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
                result = dlg.ShowDialog();// Show open file dialog box
                savePath = dlg.FileName;
            }

            if (result == true)
            {
                System.Data.DataSet ds = new System.Data.DataSet();
                ds.Tables.Add(new System.Data.DataTable("ECID(Equipment Constant)"));
                ds.Tables[0].Columns.Add("ECID");
                ds.Tables[0].Columns.Add("Name");
                ds.Tables[0].Columns.Add("Format");
                ds.Tables[0].Columns.Add("Description");

                Dictionary<int, int> IdDictionary = new Dictionary<int, int>();
                foreach (var item in lists)
                {
                    var row = ds.Tables[0].NewRow();

                    if (!IdDictionary.ContainsKey(item.ModuleIndex))
                        IdDictionary[item.ModuleIndex] = 0;

                    row[0] = item.Index;
                    row[1] = item.Name;
                    row[2] = VIDItemType2GemDataType(item.DataType);
                    if (string.IsNullOrEmpty(item.Description))
                    {
                        var arr = item.Name.Split('.');
                        for (int i = 0; i < arr.Length; i++)
                            row[3] += $"{arr[i]} ";
                    }
                    else
                    {
                        row[3] = item.Description;
                    }
                    ds.Tables[0].Rows.Add(row);
                }

                if (!ExcelHelper.ExportToExcel(savePath, ds, out string reason, createNewFile))
                {
                    LOG.Write($"Export failed, {reason}");
                    return;
                }

                LOG.Write($"Export succeed, file save as {savePath}");
            }
        }

        private void ExportSvid(List<VIDItem> dataList, bool defaultPath = true, bool createNewFile = false)
        {
            var lists = dataList.OrderBy(x => x.Index).ToList();
            bool? result = defaultPath;
            string savePath = $"{PathManager.GetCfgDir().Replace("\\bin\\Debug", "")}VIDs\\Equipment_VIDs_{DateTime.Now:yyyyMMdd}.xlsx";

            if (!defaultPath)
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.DefaultExt = ".xlsx"; // Default file extension 
                dlg.FileName = $"Chamber_Status_Variable_{DateTime.Now:yyyyMMdd_HHmmss}";
                dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
                result = dlg.ShowDialog();// Show open file dialog box
                savePath = dlg.FileName;
            }

            if (result == true)
            {
                System.Data.DataSet ds = new System.Data.DataSet();

                ds.Tables.Add(new System.Data.DataTable("SVID(Status Variable)"));
                ds.Tables[0].Columns.Add("SVID");
                ds.Tables[0].Columns.Add("Name");
                ds.Tables[0].Columns.Add("Format");
                ds.Tables[0].Columns.Add("Description");

                Dictionary<int, int> IdDictionary = new Dictionary<int, int>();

                foreach (var item in lists)
                {
                    var row = ds.Tables[0].NewRow();

                    if (!IdDictionary.ContainsKey(item.ModuleIndex))
                        IdDictionary[item.ModuleIndex] = 0;

                    row[0] = item.Index;
                    row[1] = item.Name;
                    row[2] = VIDItemType2GemDataType(item.DataType);
                    if (string.IsNullOrEmpty(item.Description))
                    {
                        var arr = item.Name.Split('.');
                        for (int i = 0; i < arr.Length; i++)
                            row[3] += $"{arr[i]} ";
                    }
                    else
                    {
                        row[3] = item.Description;
                    }

                    ds.Tables[0].Rows.Add(row);
                }

                if (!ExcelHelper.ExportToExcel(savePath, ds, out string reason, createNewFile))
                {
                    LOG.Write($"Export failed, {reason}");
                    return;
                }

                LOG.Write($"Export succeed, file save as {savePath}");
            }
        }

        private void ExportDvid(List<VIDItem> dataList, bool defaultPath = true, bool createNewFile = false)
        {
            var lists = dataList.OrderBy(x => x.Index).ToList();
            bool? result = defaultPath;
            string savePath = $"{PathManager.GetCfgDir().Replace("\\bin\\Debug", "")}VIDs\\Equipment_VIDs_{DateTime.Now:yyyyMMdd}.xlsx";

            if (!defaultPath)
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.DefaultExt = ".xlsx"; // Default file extension 
                dlg.FileName = $"Data_Variable_{DateTime.Now:yyyyMMdd_HHmmss}";
                dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
                result = dlg.ShowDialog();// Show open file dialog box
                savePath = dlg.FileName;
            }

            if (result == true)
            {
                System.Data.DataSet ds = new System.Data.DataSet();

                ds.Tables.Add(new System.Data.DataTable("DVID(Data Variable)"));
                ds.Tables[0].Columns.Add("DVID");
                ds.Tables[0].Columns.Add("Name");
                ds.Tables[0].Columns.Add("Format");
                ds.Tables[0].Columns.Add("Description");

                Dictionary<int, int> IdDictionary = new Dictionary<int, int>();

                foreach (var item in lists)
                {
                    var row = ds.Tables[0].NewRow();

                    if (!IdDictionary.ContainsKey(item.ModuleIndex))
                        IdDictionary[item.ModuleIndex] = 0;

                    row[0] = item.Index;
                    row[1] = item.Name;
                    row[2] = VIDItemType2GemDataType(item.DataType);
                    if (string.IsNullOrEmpty(item.Description))
                    {
                        var arr = item.Name.Split('.');
                        for (int i = 0; i < arr.Length; i++)
                            row[3] += $"{arr[i]} ";
                    }
                    else
                    {
                        row[3] = item.Description;
                    }

                    ds.Tables[0].Rows.Add(row);
                }

                if (!ExcelHelper.ExportToExcel(savePath, ds, out string reason, createNewFile))
                {
                    LOG.Write($"Export failed, {reason}");
                    return;
                }

                LOG.Write($"Export succeed, file save as {savePath}");
            }
        }

        private void ReGenerateGemModelXml(List<VIDItem> Svids, List<VIDItem> Ecids, List<VIDItem> Dvids, List<VIDItem> Ceids, List<VIDItem> Alids, string equipName)
        {
            XmlDocument xml = new XmlDocument();
            string _defaultPathFile = $"{PathManager.GetCfgDir().Replace("\\bin\\Debug", "")}{equipName}GemModel.xml";
            try
            {
                xml.Load(_defaultPathFile);

                if (Svids != null)
                {
                    XmlNode itemNodes = xml.SelectSingleNode("Equipment/StatusVariables");
                    itemNodes.RemoveAll();

                    foreach (var svid in OriginalSvids)
                    {
                        XmlElement subNode = xml.CreateElement("SVID");
                        subNode.SetAttribute("id", svid.Index.ToString());
                        subNode.SetAttribute("valueType", svid.DataType);
                        subNode.SetAttribute("logicalName", svid.Name);
                        subNode.SetAttribute("value", "");
                        subNode.SetAttribute("eventTrigger", "");
                        subNode.SetAttribute("units", "");
                        subNode.SetAttribute("description", svid.Description);
                        subNode.SetAttribute("isArray", "false");
                        itemNodes.AppendChild(subNode);
                    }

                    foreach (var svid in Svids)
                    {
                        XmlElement subNode = xml.CreateElement("SVID");
                        subNode.SetAttribute("id", svid.Index.ToString());
                        subNode.SetAttribute("valueType", VIDItemType2GemDataType(svid.DataType));
                        subNode.SetAttribute("logicalName", svid.Name);
                        subNode.SetAttribute("value", "");
                        subNode.SetAttribute("eventTrigger", "");
                        subNode.SetAttribute("units", "");
                        subNode.SetAttribute("description", svid.Description);
                        subNode.SetAttribute("isArray", "false");
                        itemNodes.AppendChild(subNode);
                    }
                }

                if (Ecids != null)
                {
                    XmlNode itemNodes = xml.SelectSingleNode("Equipment/EquipmentConstants");
                    itemNodes.RemoveAll();

                    foreach (var ecid in OriginalEcids)
                    {
                        XmlElement subNode = xml.CreateElement("ECID");
                        subNode.SetAttribute("id", ecid.Index.ToString());
                        subNode.SetAttribute("valueType", ecid.DataType);
                        subNode.SetAttribute("logicalName", ecid.Name);
                        subNode.SetAttribute("value", ecid.Description);
                        subNode.SetAttribute("min", "0");
                        subNode.SetAttribute("max", "100");
                        subNode.SetAttribute("eventTrigger", "");
                        subNode.SetAttribute("units", "");
                        subNode.SetAttribute("description", "");
                        subNode.SetAttribute("isArray", "false");
                        itemNodes.AppendChild(subNode);
                    }

                    foreach (var ecid in Ecids)
                    {
                        XmlElement subNode = xml.CreateElement("ECID");
                        subNode.SetAttribute("id", ecid.Index.ToString());
                        subNode.SetAttribute("valueType", VIDItemType2GemDataType(ecid.DataType));
                        subNode.SetAttribute("logicalName", ecid.Name);
                        subNode.SetAttribute("value", "");
                        subNode.SetAttribute("min", "");
                        subNode.SetAttribute("max", "");
                        subNode.SetAttribute("eventTrigger", "");
                        subNode.SetAttribute("units", "");
                        subNode.SetAttribute("description", ecid.Description);
                        subNode.SetAttribute("isArray", "false");
                        itemNodes.AppendChild(subNode);
                    }
                }

                if (Dvids != null)
                {
                    XmlNode itemNodes = xml.SelectSingleNode("Equipment/DataVariables");
                    itemNodes.RemoveAll();

                    foreach (var dvids in Dvids)
                    {
                        if (dvids.Index < 500)
                            continue;
                        XmlElement subNode = xml.CreateElement("DVID");
                        subNode.SetAttribute("id", dvids.Index.ToString());
                        subNode.SetAttribute("valueType", VIDItemType2GemDataType(dvids.DataType));
                        subNode.SetAttribute("logicalName", dvids.Name);
                        subNode.SetAttribute("value", "");
                        subNode.SetAttribute("eventTrigger", "");
                        subNode.SetAttribute("description", dvids.Description);
                        subNode.SetAttribute("isArray", "false");
                        itemNodes.AppendChild(subNode);
                    }
                }

                var moduleAlarmEventsDictionary = new Dictionary<int, string>();
                if (Alids != null)
                {
                    XmlNode itemNodes = xml.SelectSingleNode("Equipment/Alarms");
                    itemNodes.RemoveAll();

                    foreach (var alid in Alids)
                    {
                        XmlElement subNode = xml.CreateElement("ALID");
                        subNode.SetAttribute("id", alid.Index.ToString());
                        subNode.SetAttribute("logicalName", alid.Name);
                        subNode.SetAttribute("description", alid.Description);
                        subNode.SetAttribute("category", "EquipmentStatusWarning");
                        subNode.SetAttribute("enabled", "false");
                        subNode.SetAttribute("eventSet", $"10{alid.ModuleIndex.ToString().PadLeft(3, '0')}01");
                        subNode.SetAttribute("eventClear", $"20{alid.ModuleIndex.ToString().PadLeft(3, '0')}01");
                        itemNodes.AppendChild(subNode);
                        moduleAlarmEventsDictionary[alid.ModuleIndex] = alid.Module;
                    }
                }

                if (Ceids != null)
                {
                    int reportID = 0;
                    Dictionary<string, int> reportDictionary = new Dictionary<string, int>();

                    XmlNode RPTIDNodes = xml.SelectSingleNode("Equipment/DataCollections/RPTIDs");
                    RPTIDNodes.RemoveAll();

                    foreach (var ceid in Ceids)
                    {
                        XmlElement RPTID = xml.CreateElement("RPTID");
                        if (ceid.LinkableVid != null)
                        {
                            string reportKey = string.Join(",", ceid.LinkableVid);
                            if (!reportDictionary.ContainsKey(reportKey))
                            {
                                reportID += 1;
                                reportDictionary[reportKey] = reportID;
                                RPTID.SetAttribute("id", reportID.ToString());
                                RPTID.SetAttribute("logicalName", $"DefaultDefinedReport_{reportID}");

                                foreach (var vid in ceid.LinkableVid)
                                {
                                    XmlElement ReportVariable = xml.CreateElement("ReportVariable");
                                    ReportVariable.SetAttribute("id", vid.ToString());
                                    ReportVariable.SetAttribute("varType", vid < 500 ? "StatusVariable" : "DataVariable");
                                    ReportVariable.SetAttribute("logicalName", ((DataVariables.DataName)vid).ToString());
                                    RPTID.AppendChild(ReportVariable);
                                }

                                RPTIDNodes.AppendChild(RPTID);
                            }
                        }
                    }

                    XmlNode CEIDNodes = xml.SelectSingleNode("Equipment/DataCollections/CEIDs");
                    CEIDNodes.RemoveAll();

                    foreach (var ceid in Ceids)
                    {
                        XmlElement CEID = xml.CreateElement("CEID");
                        CEID.SetAttribute("id", ceid.Index.ToString());
                        CEID.SetAttribute("logicalName", ceid.Name);
                        CEID.SetAttribute("description", ceid.Description);
                        CEID.SetAttribute("enabled", "true");
                        if (ceid.LinkableVid != null)
                        {
                            string reportKey = string.Join(",", ceid.LinkableVid);
                            XmlElement RPTID = xml.CreateElement("RPTID");
                            RPTID.SetAttribute("id", reportDictionary[reportKey].ToString());
                            RPTID.SetAttribute("logicalName", $"DefaultDefinedReport_{reportDictionary[reportKey]}");
                            foreach (var vid in ceid.LinkableVid)
                            {
                                XmlElement ReportVariable = xml.CreateElement("ReportVariable");
                                ReportVariable.SetAttribute("id", vid.ToString());
                                ReportVariable.SetAttribute("varType", vid < 500 ? "StatusVariable" : "DataVariable");
                                ReportVariable.SetAttribute("logicalName", ((DataVariables.DataName)vid).ToString());
                                RPTID.AppendChild(ReportVariable);
                            }
                            CEID.AppendChild(RPTID);
                        }
                        CEIDNodes.AppendChild(CEID);
                    }

                    foreach (var alarmEvent in moduleAlarmEventsDictionary)
                    {
                        XmlElement EventSet = xml.CreateElement("CEID");
                        EventSet.SetAttribute("id", $"10{alarmEvent.Key.ToString().PadLeft(3, '0')}01");
                        EventSet.SetAttribute("logicalName", $"__SYSTEM__10{alarmEvent.Key.ToString().PadLeft(3, '0')}01__ALARMSET");
                        EventSet.SetAttribute("description", $"Alarm Set : {alarmEvent.Value} Error");
                        EventSet.SetAttribute("enabled", "true");
                        CEIDNodes.AppendChild(EventSet);

                        XmlElement EventClear = xml.CreateElement("CEID");
                        EventClear.SetAttribute("id", $"20{alarmEvent.Key.ToString().PadLeft(3, '0')}01");
                        EventClear.SetAttribute("logicalName", $"__SYSTEM__20{alarmEvent.Key.ToString().PadLeft(3, '0')}01__ALARMCLEAR");
                        EventClear.SetAttribute("description", $"Alarm Clear : {alarmEvent.Value} Error");
                        EventClear.SetAttribute("enabled", "true");
                        CEIDNodes.AppendChild(EventClear);
                    }


                }


                xml.Save(_defaultPathFile);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        string VIDItemType2GemDataType(string VIDItemType)
        {
            switch (VIDItemType.Replace("System.", ""))
            {
                case "List":
                    return "List";
                case "I1":
                    return "I1";
                case "I4":
                    return "I4";
                case "U1":
                    return "U1";
                case "U2":
                    return "U2";
                case "U4":
                    return "U4";
                case "Int":
                case "Integer":
                    return "I4";
                case "Bool":
                case "Boolean":
                    return "Boolean";
                case "Float":
                case "Single":
                case "Double":
                case "F8":
                    return "F8";
                case "Binary":
                    return "Binary";
                default:
                    return "Ascii";
            }
        }

    }
}
