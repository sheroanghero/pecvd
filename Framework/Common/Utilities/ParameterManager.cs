using Aitex.Common.Util;
using Aitex.Core.UI.MVVM;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Xml;
using System.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Aitex.Core.RT.SCCore;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace MECF.Framework.Common.Utilities
{
    //单选属性
    [Serializable]
    public class SelectionProperty : ViewModelBase
    {
        private string _Name;
        [XmlAttribute("Name")]
        public string Name
        {
            get { return _Name; }
            set { _Name = value; this.InvokePropertyChanged(nameof(Name)); }
        }

        private string _Display;
        [XmlAttribute("Display")]
        public string Display
        {
            get
            {
                return string.IsNullOrEmpty(_Display) ? Name : _Display;
            }
            set
            {
                _Display = value;
                this.InvokePropertyChanged(nameof(Display));
            }
        }

        private string _CurrentValue;
        [XmlAttribute("CurrentValue")]
        public string CurrentValue
        {
            get { return _CurrentValue; }
            set { _CurrentValue = value; this.InvokePropertyChanged(nameof(CurrentValue)); }
        }

        private ObservableCollection<string> _Values = new ObservableCollection<string>();
        [XmlArray("Values")]
        [XmlArrayItem("string")]
        public ObservableCollection<string> Values
        {
            get { return _Values; }
            set { _Values = value; this.InvokePropertyChanged(nameof(Values)); }
        }
    }

    public class ParameterManager
    {
        //private static string _ConfigPathName = @"c:\Furnace\Parameter.txt";
        //private static string _ConfigPathName = PathManager.GetCfgDir().Replace("UI", "RT") + "Parameter.xml";
        private static volatile Parameter instance;
        private static object locker = new Object();

        private static string _blockNumber = "4Block";

        public static string BlockNumber
        {
            get => _blockNumber;
            set
            {
                _blockNumber = value;


            }
        }
        //public static bool IsInitialize { get; set; } = false;

        private static bool _isInitialize;

        public static bool IsInitialize
        {
            get => _isInitialize;
            set
            {
                _isInitialize = value;
            }
        }

        public static List<SCConfigItem> ConfigItemList { get; set; }

        public static void InitializeBlock()
        {
            if (ConfigItemList == null || ConfigItemList.Count == 0)
            {
                ConfigItemList = QueryDataClient.Instance.Service.GetConfigItemList();
            }
            int[] blockNumbers = new int[] { 6, 30, 30, 6 };
            switch (instance.NumberOfBlocks)
            {
                case 2:
                    blockNumbers = new int[] { 6, 30 };
                    break;
                case 3:
                    blockNumbers = new int[] { 6, 30, 6 };
                    break;
                case 4:
                    blockNumbers = new int[] { 6, 30, 30, 6 };
                    break;
                default:
                    break;
            }
            for (int i = 0; i < instance.NumberOfBlocks; i++)
            {
                ParameterManager.Instance.Blocks[i].Modules.Clear();
                for (int j = 0; j < blockNumbers[i]; j++)
                {
                    ParameterManager.Instance.Blocks[i].Modules.Add(CreateDeviceModule(i + 1, j));
                }
                ParameterManager.Instance.Blocks[i].UseModuleNumber = ParameterManager.Instance.Blocks[i].Modules.ToList().Where(x => x.NameList.CurrentValue != "Reserved").Count() - 1;
            }
            CreateDispense();
            //   CreateDispenseDrainDefault();

        }



        //private static string GetPropertyStr()
        //{
        //    string retstr = "System.SetUp.4Block.Block";
        //    switch (ParameterManager.Instance.NumberOfBlocks)
        //    {
        //        case 2:
        //            retstr = "System.SetUp.2Block.Block";
        //            break;
        //        case 3:
        //            retstr = "System.SetUp.3Block.Block";
        //            break;
        //        case 4:
        //            retstr = "System.SetUp.4Block.Block";
        //            break;
        //        default:
        //            break;
        //    }
        //    return retstr;
        //}

        /// <summary>
        /// 需要特别注意3block的block3==4block的block4
        /// </summary>
        /// <param name="noId"></param>
        /// <returns></returns>
        private static DeviceModule CreateDeviceModule(int blockId, int moduleId)
        {
            string propertyStr = "System.SetUp.Block";// GetPropertyStr();
            string noId = $"{blockId}.{moduleId}";
            if (instance.NumberOfBlocks == 3 && blockId == 3)
            {
                noId = $"{blockId + 1}.{moduleId}";
            }
            var moduleName = (string)QueryDataClient.Instance.Service.GetConfig(($"{propertyStr}{noId}.Name"));
            string parameter = ConfigItemList.Where(x => x.PathName == ($"{propertyStr}{noId}.Name")).FirstOrDefault()?.Parameter;
            DeviceModule deviceModule = new DeviceModule($"{blockId}-{moduleId}", moduleId.ToString(), moduleName);
            deviceModule.NameList.CurrentValue = moduleName;
            deviceModule.ParameterName = moduleName;
            if (parameter != null && parameter != "")
            {
                var listStr = parameter.Split(';');
                foreach (var item in listStr)
                {
                    deviceModule.NameList.Values.Add(item);
                }
            }
            //SetSCToModule(ref deviceModule);
            return deviceModule;
        }


        public Dictionary<string, string> GetScValue(string root)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            for (int i = 0; i < instance.NumberOfBlocks; i++)
            {
                foreach (var block in Instance.Blocks)
                {
                    block.GetScValue($"{root}Block{block.ID}.");
                }

            }


            //result[$"{root}.StartSlot"] = StartSlot.ToString();

            //for (int i = 0; i < 6; i++)
            //{
            //    //result.Join()
            //    //ArmAxisList.Add(new ArmAxis());
            //}

            return result;
        }

        //private static void SetSCToModule(ref DeviceModule deviceModule)
        //{
        //    string propertyStr = GetPropertyStr();
        //    deviceModule.Name = deviceModule.NameList.CurrentValue;
        //    foreach (var item in typeof(DeviceModule).GetProperties())
        //    {
        //        if (item.Name == "ID" || item.Name == "Name" || item.Name == "ModuleID" || item.Name == "ParentID") continue;
        //        var scPath = $"{propertyStr}{deviceModule.ModuleID.Replace("-", ".")}.{item.Name}";
        //        if (item.PropertyType == typeof(string))
        //        {
        //            item.SetValue(deviceModule, (string)QueryDataClient.Instance.Service.GetConfig(scPath));
        //        }
        //        else if (item.PropertyType == typeof(SelectionProperty))
        //        {
        //            var SelectionProperty = new SelectionProperty();
        //            SelectionProperty.CurrentValue = (string)QueryDataClient.Instance.Service.GetConfig(scPath);
        //            string strparameter = ConfigItemList.Where(x => x.PathName == scPath).FirstOrDefault()?.Parameter;
        //            if (strparameter != null && strparameter != "")
        //            {
        //                var strList = strparameter.Split(';').ToList();
        //                strList.ForEach(x => SelectionProperty.Values.Add(x));
        //            }
        //            item.SetValue(deviceModule, SelectionProperty);
        //        }
        //        else if (item.PropertyType == typeof(double))
        //        {
        //            item.SetValue(deviceModule, (double)QueryDataClient.Instance.Service.GetConfig(scPath));
        //        }
        //        else if (item.PropertyType == typeof(int))
        //        {
        //            var getValue = QueryDataClient.Instance.Service.GetConfig(scPath);
        //            if (getValue != null)
        //                item.SetValue(deviceModule, (int)QueryDataClient.Instance.Service.GetConfig(scPath));
        //        }
        //        else if (item.PropertyType == typeof(SpinerModule))
        //        {
        //            if (IsSpinModule(deviceModule.Name))
        //            {
        //                SpinerModule spinerModule = new SpinerModule();
        //                var spinerModuleStr = $"{propertyStr}{deviceModule.ModuleID.Replace("-", ".")}.";
        //                spinerModule.ArmCount = ((int)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}ArmCount")).ToString();
        //                spinerModule.ShakeArmNo = ((int)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}ShakeArmNo")).ToString();
        //                spinerModule.UpperLimitSpinRotationSpeed = ((int)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}UpperLimitSpinSpeed")).ToString();
        //                spinerModule.UpperLimitAccelerationSpeed = ((int)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}UpperLimitAcceleration")).ToString();
        //                spinerModule.SpinOffSpeed = ((int)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}SpinOutSpeed")).ToString();
        //                spinerModule.SpinOffTime = ((int)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}SpinOutTime")).ToString();
        //                NozzleChanger nozzleChanger = new NozzleChanger();
        //                nozzleChanger.IsNozzleChangerEnable = (string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleChanger");
        //                nozzleChanger.MotorPulseRate = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleChangerMotorPulseRate")).ToString();
        //                nozzleChanger.MaxSpeed = ((int)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleChangerMaxSpeed")).ToString();
        //                nozzleChanger.SetNozzleChangerDataList((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleChangerData"));
        //                spinerModule.NozzleChanger = nozzleChanger;
        //                NozzleArmInformat nozzleArmInformat1 = new NozzleArmInformat();
        //                nozzleArmInformat1.IsNozzleArmEnable = (string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1");
        //                nozzleArmInformat1.ArmDriveFormat = (string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1ArmFormat");
        //                nozzleArmInformat1.NozzleForm = (string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1Format");
        //                nozzleArmInformat1.MotorPulsesRate = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1MotorPulseRate")).ToString();
        //                nozzleArmInformat1.StartPosition = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1StartPosition")).ToString();
        //                nozzleArmInformat1.EndPosition = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1EndPosition")).ToString();
        //                nozzleArmInformat1.StartCupPosition = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1StartCupPosition")).ToString();
        //                nozzleArmInformat1.EndCupPosition = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1EndCupPosition")).ToString();

        //                nozzleArmInformat1.Home = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1RcpHome"));
        //                nozzleArmInformat1.DmyDisp = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1RcpDmyDisp"));
        //                nozzleArmInformat1.Standby1 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1RcpStandby1"));
        //                nozzleArmInformat1.Standby2 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1RcpStandby2"));
        //                nozzleArmInformat1.Begin = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1RcpBegin"));
        //                nozzleArmInformat1.Center = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1RcpCenter"));
        //                nozzleArmInformat1.End = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1RcpEnd"));
        //                nozzleArmInformat1.Dispense1 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1RcpDispense1"));
        //                nozzleArmInformat1.Dispense2 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1RcpDispense2"));
        //                nozzleArmInformat1.Dispense3 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1RcpDispense3"));
        //                nozzleArmInformat1.Dispense4 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1RcpDispense4"));
        //                nozzleArmInformat1.Dispense5 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm1RcpDispense5"));

        //                NozzleArmInformat nozzleArmInformat2 = new NozzleArmInformat();
        //                nozzleArmInformat2.IsNozzleArmEnable = (string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2");
        //                nozzleArmInformat2.ArmDriveFormat = (string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2ArmFormat");
        //                nozzleArmInformat2.NozzleForm = (string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2Format");
        //                nozzleArmInformat2.MotorPulsesRate = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2MotorPulseRate")).ToString();
        //                nozzleArmInformat2.StartPosition = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2StartPosition")).ToString();
        //                nozzleArmInformat2.EndPosition = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2EndPosition")).ToString();
        //                nozzleArmInformat2.StartCupPosition = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2StartCupPosition")).ToString();
        //                nozzleArmInformat2.EndCupPosition = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2EndCupPosition")).ToString();

        //                nozzleArmInformat2.Home = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2RcpHome"));
        //                nozzleArmInformat2.DmyDisp = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2RcpDmyDisp"));
        //                nozzleArmInformat2.Standby1 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2RcpStandby1"));
        //                nozzleArmInformat2.Standby2 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2RcpStandby2"));
        //                nozzleArmInformat2.Begin = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2RcpBegin"));
        //                nozzleArmInformat2.Center = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2RcpCenter"));
        //                nozzleArmInformat2.End = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2RcpEnd"));
        //                nozzleArmInformat2.Dispense1 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2RcpDispense1"));
        //                nozzleArmInformat2.Dispense2 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2RcpDispense2"));
        //                nozzleArmInformat2.Dispense3 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2RcpDispense3"));
        //                nozzleArmInformat2.Dispense4 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2RcpDispense4"));
        //                nozzleArmInformat2.Dispense5 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm2RcpDispense5"));



        //                NozzleArmInformat nozzleArmInformat3 = new NozzleArmInformat();
        //                nozzleArmInformat3.IsNozzleArmEnable = (string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3");
        //                nozzleArmInformat3.ArmDriveFormat = (string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3ArmFormat");
        //                nozzleArmInformat3.NozzleForm = (string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3Format");
        //                nozzleArmInformat3.MotorPulsesRate = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3MotorPulseRate")).ToString();
        //                nozzleArmInformat3.StartPosition = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3StartPosition")).ToString();
        //                nozzleArmInformat3.EndPosition = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3EndPosition")).ToString();
        //                nozzleArmInformat3.StartCupPosition = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3StartCupPosition")).ToString();
        //                nozzleArmInformat3.EndCupPosition = ((double)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3EndCupPosition")).ToString();



        //                nozzleArmInformat3.Home = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3RcpHome"));
        //                nozzleArmInformat3.DmyDisp = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3RcpDmyDisp"));
        //                nozzleArmInformat3.Standby1 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3RcpStandby1"));
        //                nozzleArmInformat3.Standby2 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3RcpStandby2"));
        //                nozzleArmInformat3.Begin = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3RcpBegin"));
        //                nozzleArmInformat3.Center = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3RcpCenter"));
        //                nozzleArmInformat3.End = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3RcpEnd"));
        //                nozzleArmInformat3.Dispense1 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3RcpDispense1"));
        //                nozzleArmInformat3.Dispense2 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3RcpDispense2"));
        //                nozzleArmInformat3.Dispense3 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3RcpDispense3"));
        //                nozzleArmInformat3.Dispense4 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3RcpDispense4"));
        //                nozzleArmInformat3.Dispense5 = new TransferArmRecipePositionData((string)QueryDataClient.Instance.Service.GetConfig($"{spinerModuleStr}NozzleArm3RcpDispense5"));


        //                spinerModule.NozzleArmInformat1 = nozzleArmInformat1;
        //                spinerModule.NozzleArmInformat2 = nozzleArmInformat2;
        //                item.SetValue(deviceModule, spinerModule);
        //            }
        //        }
        //    }
        //    var transferArmstr = (string)QueryDataClient.Instance.Service.GetConfig(($"{propertyStr}{deviceModule.ModuleID.Replace("-", "_")}_TransferArm").Replace('_', '.'));
        //    if (transferArmstr != null && transferArmstr != "")
        //    {
        //        int index = int.Parse(deviceModule.ModuleID.Split('-')[0]) - 1;
        //        ParameterManager.Instance.Blocks[index].TransferArmAccessModules = GetArmAccessModule(transferArmstr);
        //    }

        //}

        public static ObservableCollection<string> GetArmAccessModule(string str)
        {
            ObservableCollection<string> strList = new ObservableCollection<string>();
            if (BlockNumber == "3Block")
            {
                if (str.Contains("4-"))
                {
                    str = str.Replace("4-", "3-");
                }
            }
            var strlist = str.Split(';');
            foreach (var item in strlist)
            {
                strList.Add(item);
            }
            return strList;
        }


        public static string[] COTDispenseNozzleStrs = new string[] { "Resist1", "Resist2" , "Resist3","Resist4", "Resist5", "Resist6", "Resist7", "Resist8",
        "RRC1","RRC2","Test1","Test2","E.B.R.1","E.B.R.2","R1 Filter Air Vent","R2 Filter Air Vent","R3 Filter Air Vent","R4 Filter Air Vent",
        "TANK Press","","Back Rinse1","Back Rinse2","Solvent Bath","","Auto Damper","Back Rinse1+2","Drain Case Clean","EXH Duct Clean"};

        public static string[] DEVDispenseNozzleStrs = new string[] { "DEV Solution1", "DEV Solution2" , "DEV Solution3", "DEV Solution4", "", "", "",
        "DEV Nozzle1 Air Vent","DEV Nozzle2 Air Vent","","","Rinse1","Rinse2","Filter1 Vent","Filter2 Vent","Filter3 Vent","Filter4 Vent",
        "Tank Press","AD Promoter","Back Rinse1","Water Shield","Exhaust","Auto Damper","N2 Blow","Nozzle1 Rinse","Nozzle2 Rinse",
        "PDD Nozzle1","PDD Nozzle2","N2 Splay（DEV）","N3 Splay（Rinse）","Bypass"};

        public static string[] ADHDispenseNozzleStrs = new string[] { "HMDS" };



        public static void CreateDispense()
        {
            ParameterManager.Instance.SpinerDeviceModule.Clear();
            if (ParameterManager.Instance.Blocks[1] != null)
            {
                ParameterManager.Instance.Blocks[1].Modules.ToList().ForEach(x =>
                {
                    if (IsSpinModule(x.Name)) ParameterManager.Instance.SpinerDeviceModule.Add(x);
                });
                ParameterManager.Instance.Blocks[1].Modules.ToList().ForEach(x =>
                {
                    if (IsSpinModule(x.Name) || IsADHModule(x.Name)) ParameterManager.Instance.DispenseDeviceModule.Add(x);
                });

            }
            if (ParameterManager.Instance.NumberOfBlocks > 2 && ParameterManager.Instance.Blocks[2] != null)
            {
                ParameterManager.Instance.Blocks[2].Modules.ToList().ForEach(x =>
                {
                    if (IsSpinModule(x.Name)) ParameterManager.Instance.SpinerDeviceModule.Add(x);
                });
                ParameterManager.Instance.Blocks[2].Modules.ToList().ForEach(x =>
                {
                    if (IsSpinModule(x.Name) || IsADHModule(x.Name)) ParameterManager.Instance.DispenseDeviceModule.Add(x);
                });
            }

        }

        //private static void CreateDispenseDrainDefault()
        //{
        //    ParameterManager.Instance.ModuleDispenseConfigList = new Dictionary<string, Dictionary<string, DispenseConfig>>();
        //    for (int i = 0; i < ParameterManager.Instance.DispenseDeviceModule.Count; i++)
        //    {
        //        string strFileDispense = QueryDataClient.Instance.Service.GetConfigFileDispenseByModule(ParameterManager.Instance.DispenseDeviceModule[i].ID);
        //        Dictionary<string, DispenseConfig> dispenseConfigList = new Dictionary<string, DispenseConfig>();
        //        if (strFileDispense != null)
        //        {
        //            string[] listDispense = strFileDispense.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        //            Dictionary<string, string> dictDispense = new Dictionary<string, string>();
        //            for (int x = 0; x < listDispense.Length; x++)
        //            {
        //                string[] sigDispense = listDispense[x].Split(':');
        //                dictDispense.Add(sigDispense[0], sigDispense[1]);
        //            }
        //            switch (ParameterManager.Instance.DispenseDeviceModule[i].Name)
        //            {
        //                case "COT":
        //                    for (int j = 0; j < COTDispenseNozzleStrs.Length; j++)
        //                    {
        //                        if (COTDispenseNozzleStrs[j] != "")
        //                        {
        //                            dispenseConfigList.Add(COTDispenseNozzleStrs[j], new DispenseConfig(dictDispense[COTDispenseNozzleStrs[j]]));
        //                        }
        //                    }
        //                    break;
        //                case "DEV":
        //                    for (int j = 0; j < DEVDispenseNozzleStrs.Length; j++)
        //                    {
        //                        if (DEVDispenseNozzleStrs[j] != "")
        //                        {
        //                            dispenseConfigList.Add(DEVDispenseNozzleStrs[j], new DispenseConfig(dictDispense[DEVDispenseNozzleStrs[j]]));
        //                        }
        //                    }
        //                    break;
        //                default:
        //                    break;
        //            }
        //        }
        //        else
        //        {
        //            switch (ParameterManager.Instance.DispenseDeviceModule[i].Name)
        //            {
        //                case "COT":
        //                    for (int j = 0; j < COTDispenseNozzleStrs.Length; j++)
        //                    {
        //                        if (COTDispenseNozzleStrs[j] != "")
        //                        {
        //                            dispenseConfigList.Add(COTDispenseNozzleStrs[j], new DispenseConfig());
        //                        }
        //                    }
        //                    break;
        //                case "DEV":

        //                    for (int j = 0; j < DEVDispenseNozzleStrs.Length; j++)
        //                    {
        //                        if (DEVDispenseNozzleStrs[j] != "")
        //                        {
        //                            dispenseConfigList.Add(DEVDispenseNozzleStrs[j], new DispenseConfig());
        //                        }
        //                    }
        //                    break;
        //                default:
        //                    break;
        //            }
        //        }
        //        ParameterManager.Instance.ModuleDispenseConfigList.Add(ParameterManager.Instance.DispenseDeviceModule[i].ID, dispenseConfigList);
        //    }
        //}

        public static bool IsSpinModule(string moduleName)
        {
            bool isSpin = false;
            string[] spinMdules = new string[] { "COT", "BCT", "TCT", "DEV", "NTD" };
            if (spinMdules.Contains(moduleName))
            {
                isSpin = true;
            }
            return isSpin;
        }

        public static bool IsADHModule(string moduleName)
        {
            bool ret = false;
            string[] spinMdules = new string[] { "ADH" };
            if (spinMdules.Contains(moduleName))
            {
                ret = true;
            }
            return ret;
        }


        public static Parameter Instance
        {
            get
            {
                lock (locker)
                {
                    if (instance == null)
                    {
                        instance = new Parameter(BlockNumber);
                        InitializeBlock();
                    }
                    return instance;
                }
            }
        }

        public static void SaveParameter(object pa)
        {
            //StreamWriter stream = null;
            //try
            //{
            //    stream = new StreamWriter(_ConfigPathName);
            //    XmlSerializer xmlSerializer = new XmlSerializer(typeof(Parameter));
            //    xmlSerializer.Serialize(stream, pa);
            //    stream.Flush();
            //}
            //finally
            //{
            //    if (stream != null)
            //    {
            //        stream.Close();
            //        stream.Dispose();
            //    }
            //}

        }

        //private static object GetParameter()
        //{
        //StreamReader stream = null;
        //try
        //{
        //    stream = new StreamReader(_ConfigPathName);
        //    XmlSerializer xmlSerializer1 = new XmlSerializer(typeof(Parameter));
        //    var pa = xmlSerializer1.Deserialize(stream);
        //    return pa;
        //}
        //finally
        //{
        //    if (stream != null)
        //    {
        //        stream.Close();
        //        stream.Dispose();
        //    }
        //}
        //}


    }

    [Serializable]
    public class Parameter
    {
        public Parameter() { InitParameter(); }

        public Dictionary<string, Dictionary<string, DispenseConfig>> ModuleDispenseConfigList { get; set; } = new Dictionary<string, Dictionary<string, DispenseConfig>>();

        public ObservableCollection<DeviceModule> SpinerDeviceModule { get; set; } = new ObservableCollection<DeviceModule>();

        public ObservableCollection<DeviceModule> DispenseDeviceModule { get; set; } = new ObservableCollection<DeviceModule>();

        public Parameter(string blockNumber)
        {
            _NumberOfBlocks = BlockStrToInt(blockNumber);
            CreateBlocks();
            InitParameter();
        }

        private static int BlockStrToInt(string blockNumber)
        {
            var number = 4;
            switch (blockNumber)
            {
                case "4Block":
                    number = 4;
                    break;
                case "3Block":
                    number = 3;
                    break;
                case "2Block":
                    number = 2;
                    break;
                default:
                    break;
            }

            return number;
        }

        [XmlArray("Blocks")]
        [XmlArrayItem("DeviceBlock")]
        public ObservableCollection<DeviceBlock> Blocks { get; set; } = new ObservableCollection<DeviceBlock>();
        private int _NumberOfBlocks;
        [XmlAttribute("_NumberOfBlocks")]
        public int NumberOfBlocks
        {
            get { return _NumberOfBlocks; }
            set
            {
                if (value < 1 || value > 4) throw new Exception("value should between 1 and 4");
                if (_NumberOfBlocks != value)
                {
                    _NumberOfBlocks = value;
                    CreateBlocks();
                    InitParameter();
                }
            }
        }



        public void CreateBlocks()
        {
            Blocks.Clear();
            switch (NumberOfBlocks)
            {
                case 2:
                    Blocks.Add(new DeviceBlock(1) { Name = "CSB" });
                    Blocks.Add(new DeviceBlock(2) { Name = "PRB1" });
                    break;
                case 3:
                    Blocks.Add(new DeviceBlock(1) { Name = "CSB" });
                    Blocks.Add(new DeviceBlock(2) { Name = "PRB1" });
                    Blocks.Add(new DeviceBlock(3) { Name = "IFB" });
                    break;
                case 4:
                    Blocks.Add(new DeviceBlock(1) { Name = "CSB" });
                    Blocks.Add(new DeviceBlock(2) { Name = "PRB1" });
                    Blocks.Add(new DeviceBlock(3) { Name = "PRB2" });
                    Blocks.Add(new DeviceBlock(4) { Name = "IFB" });
                    break;
                default:
                    break;
            }
        }

        public void InitParameter()
        {
            GetMonitorEditData();
            //NumberOfBlocks = 1;
            //InitSystemParameter();
            //InitTransferOutOrInOrder();
            GetDataCollection();
        }

        private void GetMonitorEditData()
        {
            string strFileMonitorEdit = (string)QueryDataClient.Instance.Service.GetConfig("System.SetUp.MonitorEdit");
            if (strFileMonitorEdit != null && strFileMonitorEdit != "")
            {
                MonitorData.Clear();
                string[] listMonitorEdit = strFileMonitorEdit.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < listMonitorEdit.Length; i++)
                {
                    MonitorData.Add(new MonitorEditData(listMonitorEdit[i]));
                }
            }
        }

        private void GetDataCollection()
        {
            DataCollection.Clear();
            DataCollection.Add(new DataCollecion()
            {
                MouleName = "Coater",
                MeasureData = new ObservableCollection<string>(new List<string>() { "Spin rotation speed", "Solvent bath flow" })
            });
        }


        //public void SaveMonitorEditData()
        //{
        //    if (MonitorData != null && MonitorData.Count > 0)
        //    {
        //        InvokeClient.Instance.Service.DoOperation("SetConfig", new object[] { "Save", "MonitorEdit", MonitorEdittDataToString(MonitorData) });
        //    }
        //}

        //private string MonitorEdittDataToString(ObservableCollection<MonitorEditData> monitorData)
        //{
        //    StringBuilder stringBuilder = new StringBuilder();
        //    foreach (var item in monitorData)
        //    {
        //        stringBuilder.AppendLine($"{item.ToString()}");
        //    }
        //    return stringBuilder.ToString();
        //}


        [XmlArray("SystemParameter")]
        [XmlArrayItem("SelectionProperty")]
        public ObservableCollection<SelectionProperty> SystemParameter { get; set; } = new ObservableCollection<SelectionProperty>();


        //public void InitSystemParameter()
        //{
        //    SystemParameter.Add(new SelectionProperty() { Name = "WaferExistInRCV", Values = { "OK", "Cancel" }, CurrentValue = "OK" });
        //    SystemParameter.Add(new SelectionProperty() { Name = "DesignationOfLotEndForJob", Values = { "All Lot", "Last Lot" }, CurrentValue = "All Lot" });
        //    SystemParameter.Add(new SelectionProperty() { Name = "DefaultScreenForRunButton", Values = { "Lot Start", "Job Start", "Wafer Flow Recipe Registrtation" }, CurrentValue = "Lot Start" });
        //    SystemParameter.Add(new SelectionProperty() { Name = "CupWashingAlarm", Values = { "Valid", "Invalid" }, CurrentValue = "Valid" });
        //    SystemParameter.Add(new SelectionProperty() { Name = "EISInitializeCondition", Values = { "Unconditionally", "ConfigrmARMSignal" }, CurrentValue = "Unconditionally" });
        //}
        [XmlArray("TransferOutOrInOrder")]
        [XmlArrayItem("SelectionProperty")]
        public ObservableCollection<SelectionProperty> TransferOutOrInOrder { get; set; } = new ObservableCollection<SelectionProperty>();

        //private void InitTransferOutOrInOrder()
        //{
        //    TransferOutOrInOrder.Add(new SelectionProperty() { Name = "Sender-TransferOutOrder", Values = { "Top", "Bottom" }, CurrentValue = "" });
        //    TransferOutOrInOrder.Add(new SelectionProperty() { Name = "Reciver-TransferInOrder", Values = { "Top", "Bottom", "Slot Correspondence" }, CurrentValue = "" });
        //    TransferOutOrInOrder.Add(new SelectionProperty() { Name = "UniCass-TransferOutOrder", Values = { "Top", "Bottom" }, CurrentValue = "" });
        //    TransferOutOrInOrder.Add(new SelectionProperty() { Name = "PickUp-TransferInOrder", Values = { "Top", "Bottom" }, CurrentValue = "" });
        //    TransferOutOrInOrder.Add(new SelectionProperty() { Name = "Reject-TransferInOrder", Values = { "Top", "Bottom" }, CurrentValue = "" });
        //}

        public ObservableCollection<MonitorEditData> MonitorData { get; set; } = new ObservableCollection<MonitorEditData>();

        public ObservableCollection<DataCollecion> DataCollection { get; set; } = new ObservableCollection<DataCollecion>();

        public Dictionary<string, object> GetScValue(string root)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (var item in Blocks)
            {
                if (NumberOfBlocks == 3 && item.ID == "3")
                {
                    result.AddRange(item.GetScValue($"{root}Block4."));
                }
                else
                {
                    result.AddRange(item.GetScValue($"{root}Block{item.ID}."));
                }
            }
            foreach (var item in Blocks)
            {
                item.UseModuleNumber = item.Modules.ToList().Where(x => x.NameList.CurrentValue != "Reserved").Count() - 1;
            }
            return result;
        }
    }

    [Serializable]
    public class DeviceBlock
    {
        [XmlElement("ID")]
        public string ID { get; set; }
        [XmlElement("Name")]
        public string Name { get; set; }
        [XmlElement("BlockID")]
        public SelectionProperty BlockID { get; set; } = new SelectionProperty() { Name = "BlockID", Values = { "Cassette Block", "Proccess Block", "Interface Block" }, CurrentValue = "" };
        public ObservableCollection<string> ModuleIDs { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<string> TransferArmCanAccessModules { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<string> TransferArmAccessModules { get; set; } = new ObservableCollection<string>();
        //public ModuleArm ModuleArm { get; set; }
        [XmlArray("Modules")]
        [XmlArrayItem("DeviceModule")]
        public ObservableCollection<DeviceModule> Modules { get; set; } = new ObservableCollection<DeviceModule>();

        private int _NumberOfModules;
        [XmlAttribute("_NumberOfModules")]
        public int NumberOfModules
        {
            get { return _NumberOfModules; }
            set
            {
                if (value < 0 || value > 32) throw new Exception("value should between 0 and 32");
                _NumberOfModules = value;
                //  SetModules();
            }
        }

        public int UseModuleNumber { get; set; }

        //private void SetModules()
        //{
        //    int count = NumberOfModules - Modules.Count;
        //    if (count > 0)
        //    {
        //        for (int i = 0; i < count; i++)
        //        {
        //            string id = (Modules.Count + 1).ToString();
        //            Modules.Add(new DeviceModule(id, BlockID.CurrentValue));
        //        }
        //    }
        //    else if (count < 0)
        //    {
        //        for (int i = 0; i < (-count); i++)
        //        {
        //            Modules.RemoveAt(Modules.Count - 1);
        //        }
        //    }
        //}

        public DeviceBlock()
        {
        }
        public DeviceBlock(int id)
        {
            ID = id.ToString();
        }

        public ArmPosition AccessHomePosition { get; set; } = new ArmPosition();

        public ArmPosition WaferCheckPosition { get; set; } = new ArmPosition();


        public Dictionary<string, object> GetScValue(string root)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (var item in Modules)
            {
                result.AddRange(item.GetScValue($"{root}{item.ModuleID}."));
            }
            result.AddRange(AccessHomePosition.GetScValue($"{root}AccessHomePosition."));
            result.AddRange(WaferCheckPosition.GetScValue($"{root}WaferCheckPosition."));
            return result;
        }
    }


    [Serializable]
    public class DeviceModule
    {
        [XmlElement("ID")]
        public string ID { get; set; }
        [XmlElement("Name")]
        public string Name { get; set; }
        public string ParentID { get; set; }
        [XmlAttribute("ModuleID")]
        public string ModuleID { get; set; }

        [XmlAttribute("ParameterName")]
        public string ParameterName { get; set; }

        public SelectionProperty NameList { get; set; } = new SelectionProperty() { Name = "NameList", Values = { }, CurrentValue = "CPL" };
        public SelectionProperty SYNEXC { get; set; } = new SelectionProperty() { Name = "SYNCEXC", Values = { "Invalid", "1-2", "2-3", "1-2&2-3" }, CurrentValue = "" };
        [XmlAttribute("MateriCount")]
        public int MaterialCount { get; set; }
        [XmlAttribute("StartSlot")]
        public int StartSlot { get; set; }
        public SelectionProperty AGVConnect { get; set; } = new SelectionProperty() { Name = "AGVConnect", Values = { "Connect", "Disconnect" } };
        public SpinerModule ModuleSpiner { get; set; } = new SpinerModule();

        public ObservableCollection<SelectionProperty> TransferArmPosition { get; set; } = new ObservableCollection<SelectionProperty>();


        public ObservableCollection<ArmAxis> ArmAxisList { get; set; } = new ObservableCollection<ArmAxis>();
        public SelectionProperty MappingType { get; set; } = new SelectionProperty() { Name = "Arm Up", Values = { "Arm Up", "Arm Receive", "Cassette Down" } };

        public int WaferThickness { get; set; }

        public int PositiveTolerance { get; set; }

        public int NegativeTolerance { get; set; }

        public int FirstSlotposition { get; set; }

        public SelectionProperty SlotEditingType { get; set; } = new SelectionProperty() { Name = "Fix", Values = { "Fix", "Option" } };

        public int SlotPitch { get; set; }

        public int MappingStartPosition { get; set; }

        public int mappingEndPosition { get; set; }

        public int SlotNo { get; set; }

        public TransferArmPosition ParameterTransferArmPosition { get; set; } = new TransferArmPosition();

        public MonitorEditData MonitorEidtData { get; set; }

        public DeviceModule() { }
        public DeviceModule(string id, string moduleID, string name)
        {
            ID = id;
            ModuleID = moduleID;
            Name = name;
            for (int i = 0; i < 6; i++)
            {
                ArmAxisList.Add(new ArmAxis());
            }
            //NameList.CurrentValue = name;
        }

        public void Save()
        {

        }

        public Dictionary<string, object> GetScValue(string root)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result[$"{root}Name"] = NameList.CurrentValue;
            result[$"{root}SYNEXC"] = SYNEXC.CurrentValue;
            result[$"{root}MaterialCount"] = MaterialCount;
            result[$"{root}StartSlot"] = StartSlot;
            result[$"{root}AGVConnect"] = AGVConnect.CurrentValue;
            if (ModuleID == "0" && ArmAxisList != null && ArmAxisList.Count == 6)
            {
                result.AddRange(ArmAxisList[0].GetScValue($"{root}X1axis."));
                result.AddRange(ArmAxisList[1].GetScValue($"{root}X2axis."));
                result.AddRange(ArmAxisList[2].GetScValue($"{root}X3axis."));
                result.AddRange(ArmAxisList[3].GetScValue($"{root}Yaxis."));
                result.AddRange(ArmAxisList[4].GetScValue($"{root}Zaxis."));
                result.AddRange(ArmAxisList[5].GetScValue($"{root}Raxis."));
            }
            if (ParameterManager.IsSpinModule(Name))
            {
                result.AddRange(ModuleSpiner.GetScValue(root));
            }
            if (ModuleID != "0")
            {
                result.AddRange(ParameterTransferArmPosition.GetScValue($"{root}TransferArmPosition."));
            }
            if (NameList.CurrentValue == "COT" || NameList.CurrentValue == "DEV" || NameList.CurrentValue == "ADH")
            {
                result[$"{root}DispenseConfig"] = DispenseConfigListToString(ParameterManager.Instance.ModuleDispenseConfigList[ID]);
            }
            return result;
        }

        private string DispenseConfigListToString(Dictionary<string, DispenseConfig> dispenseConfigList)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var item in dispenseConfigList.Keys)
            {
                stringBuilder.AppendLine($"{item}:{dispenseConfigList[item].ToString()}");
            }
            return stringBuilder.ToString();
        }
    }

    public class ArmAxis
    {
        public int SpeedInitial { get; set; }
        public int SpeedMovementbetweenmod { get; set; }
        public int SpeedReceive { get; set; }
        public int SpeedSend { get; set; }
        public int SpeedMapping { get; set; }
        public int SpeedMaintenance { get; set; }
        public int SpeedInching { get; set; }
        public string MultiMovementbetweenmod { get; set; }
        public double PassMovementbetweenmod { get; set; }
        public double PassReceive { get; set; }
        public double PassSpeedSend { get; set; }
        public double PassSpeedMapping { get; set; }

        public Dictionary<string, object> GetScValue(string root)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result[$"{root}SpeedInitial"] = SpeedInitial;
            result[$"{root}SpeedMovementbetweenmod"] = SpeedMovementbetweenmod;
            result[$"{root}SpeedReceive"] = SpeedReceive;
            result[$"{root}SpeedSend"] = SpeedSend;
            result[$"{root}SpeedMapping"] = SpeedMapping;
            result[$"{root}SpeedMaintenance"] = SpeedMaintenance;
            result[$"{root}SpeedInching"] = SpeedInching;
            result[$"{root}MultiMovementbetweenmod"] = MultiMovementbetweenmod;
            result[$"{root}PassMovementbetweenmod"] = PassMovementbetweenmod;
            result[$"{root}PassReceive"] = PassReceive;
            result[$"{root}PassSpeedSend"] = PassSpeedSend;
            result[$"{root}PassSpeedMapping"] = PassSpeedMapping;
            return result;
        }
    }





    public class ModuleArm
    {


        //public DataTable TransferArmControlData { get; set; } = new DataTable("TransferArmControlData");

        //public ObservableCollection<TransferArmInteraction> TransferArmInteraction { get; set; } = new ObservableCollection<TransferArmInteraction>();

    }

    public class SpinerModule
    {
        public string ArmCount { get; set; }
        public string ShakeArmNo { get; set; }
        //public string RinseNozzleSpec { get; set; }
        public string UpperLimitSpinSpeed { get; set; }
        public string UpperLimitAcceleration { get; set; }
        public string SpinOutSpeed { get; set; }
        public string SpinOutTime { get; set; }
        public string Unit { get; set; }
        public NozzleChanger NozzleChanger { get; set; } = new NozzleChanger();
        public NozzleArmInformat NozzleArmInformat1 { get; set; } = new NozzleArmInformat();
        public NozzleArmInformat NozzleArmInformat2 { get; set; } = new NozzleArmInformat();

        public Dictionary<string, object> GetScValue(string root)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result[$"{root}ArmCount"] = ArmCount;
            result[$"{root}ShakeArmNo"] = ShakeArmNo;
            result[$"{root}UpperLimitSpinSpeed"] = UpperLimitSpinSpeed;
            result[$"{root}UpperLimitAcceleration"] = UpperLimitAcceleration;
            result[$"{root}SpinOutSpeed"] = SpinOutSpeed;
            result[$"{root}SpinOutTime"] = SpinOutTime;
            // result[$"{root}Unit"] = Unit;
            result.AddRange(NozzleChanger.GetScValue($"{root}NozzleChanger."));
            result.AddRange(NozzleArmInformat1.GetScValue($"{root}NozzleArmInformat1."));
            result.AddRange(NozzleArmInformat2.GetScValue($"{root}NozzleArmInformat2."));
            return result;
        }

    }
    public class NozzleChanger : ICloneable
    {
        public string IsNozzleChangerEnable { get; set; }
        public string MotorPulseRate { get; set; }
        public string MaxSpeed { get; set; }

        public void SetNozzleChangerDataList(string value)
        {
            NozzleChangerData.Clear();
            var strList = value.Split(';');
            for (int i = 0; i < strList.Length; i++)
            {
                var NozzleChangerStrList = strList[i].Split(',');
                NozzleChangerData.Add(new NozzleChangerData()
                {
                    ID = (i + 1).ToString(),
                    IsNozzleChangerEnable = NozzleChangerStrList[0],
                    Position = NozzleChangerStrList[1],
                    NozzleSensorNo = NozzleChangerStrList[2],
                    NozzleOffSet = NozzleChangerStrList[3]
                });
            }
        }

        public string NozzleChangerDataToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var item in NozzleChangerData)
            {
                stringBuilder.Append(item.ToString());
                stringBuilder.Append(";");
            }
            if (stringBuilder.Length > 0) stringBuilder.Remove(stringBuilder.Length - 1, 1);
            return stringBuilder.ToString();
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public object DeepClone()
        {
            return new NozzleChanger()
            {
                IsNozzleChangerEnable = this.IsNozzleChangerEnable,
                MotorPulseRate = this.MotorPulseRate,
                MaxSpeed = this.MaxSpeed,
                NozzleChangerData = new ObservableCollection<NozzleChangerData>(this.NozzleChangerData.Select(x => x.DeepClone()).Cast<NozzleChangerData>())
            };
        }

        public ObservableCollection<NozzleChangerData> NozzleChangerData { get; set; } = new ObservableCollection<NozzleChangerData>();

        public Dictionary<string, object> GetScValue(string root)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result[$"{root}IsNozzleChangerEnable"] = IsNozzleChangerEnable;
            result[$"{root}MotorPulseRate"] = MotorPulseRate;
            result[$"{root}MaxSpeed"] = MaxSpeed;
            result[$"{root}Data"] = NozzleChangerDataToString();
            return result;
        }
    }
    public class NozzleChangerData : ICloneable
    {
        public string ID { get; set; }
        public string IsNozzleChangerEnable { get; set; }
        public string Position { get; set; }
        public string NozzleSensorNo { get; set; }
        public string NozzleOffSet { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public object DeepClone()
        {
            return new NozzleChangerData()
            {
                ID = this.ID,
                IsNozzleChangerEnable = this.IsNozzleChangerEnable,
                Position = this.Position,
                NozzleSensorNo = this.NozzleSensorNo,
                NozzleOffSet = this.NozzleOffSet
            };
        }

        public override string ToString()
        {
            string rst = $"{IsNozzleChangerEnable},{Position},{NozzleSensorNo},{NozzleOffSet}";
            return rst.ToString();
        }

    }
    public class NozzleArmInformat : ICloneable
    {
        public string IsNozzleArmEnable { get; set; }
        public string ArmDriveFormat { get; set; }
        public string NozzleForm { get; set; }
        public string MotorPulsesRate { get; set; }
        public string StartPosition { get; set; }
        public string EndPosition { get; set; }
        public string StartCupPosition { get; set; }
        public string EndCupPosition { get; set; }
        public TransferArmRecipePositionData Home { get; set; }
        public TransferArmRecipePositionData DmyDisp { get; set; }
        public TransferArmRecipePositionData Standby1 { get; set; }
        public TransferArmRecipePositionData Standby2 { get; set; }
        public TransferArmRecipePositionData Begin { get; set; }
        public TransferArmRecipePositionData Center { get; set; }
        public TransferArmRecipePositionData End { get; set; }
        public TransferArmRecipePositionData Dispense1 { get; set; }
        public TransferArmRecipePositionData Dispense2 { get; set; }
        public TransferArmRecipePositionData Dispense3 { get; set; }
        public TransferArmRecipePositionData Dispense4 { get; set; }
        public TransferArmRecipePositionData Dispense5 { get; set; }

        public Dictionary<string, object> GetScValue(string root)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result[$"{root}IsNozzleArmEnable"] = IsNozzleArmEnable;
            result[$"{root}ArmDriveFormat"] = ArmDriveFormat;
            result[$"{root}NozzleForm"] = NozzleForm;
            result[$"{root}MotorPulsesRate"] = MotorPulsesRate;
            result[$"{root}StartPosition"] = double.Parse(StartPosition).ToString("f2");
            result[$"{root}EndPosition"] = double.Parse(EndPosition).ToString("f2");
            result[$"{root}StartCupPosition"] = double.Parse(StartCupPosition).ToString("f2");
            result[$"{root}EndCupPosition"] = double.Parse(EndCupPosition).ToString("f2");
            if (Home != null) result[$"{root}Home"] = Home.ToString();
            if (DmyDisp != null) result[$"{root}DmyDisp"] = DmyDisp.ToString();
            if (Standby1 != null) result[$"{root}Standby1"] = Standby1.ToString();
            if (Standby2 != null) result[$"{root}Standby2"] = Standby2.ToString();
            if (Begin != null) result[$"{root}Begin"] = Begin.ToString();
            if (Center != null) result[$"{root}Center"] = Center.ToString();
            if (End != null) result[$"{root}End"] = End.ToString();
            if (Dispense1 != null) result[$"{root}Dispense1"] = Dispense1.ToString();
            if (Dispense2 != null) result[$"{root}Dispense2"] = Dispense2.ToString();
            if (Dispense3 != null) result[$"{root}Dispense3"] = Dispense3.ToString();
            if (Dispense4 != null) result[$"{root}Dispense4"] = Dispense4.ToString();
            if (Dispense5 != null) result[$"{root}Dispense5"] = Dispense5.ToString();
            return result;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public object DeepClone()
        {
            return new NozzleArmInformat()
            {
                IsNozzleArmEnable = this.IsNozzleArmEnable,
                ArmDriveFormat = this.ArmDriveFormat,
                NozzleForm = this.NozzleForm,
                MotorPulsesRate = this.MotorPulsesRate,
                StartPosition = this.StartPosition,
                EndPosition = this.EndPosition,
                StartCupPosition = this.StartCupPosition,
                EndCupPosition = this.EndCupPosition,

                Home = (TransferArmRecipePositionData)this.Home.Clone(),
                DmyDisp = (TransferArmRecipePositionData)this.DmyDisp.Clone(),
                Standby1 = (TransferArmRecipePositionData)this.Standby1.Clone(),
                Standby2 = (TransferArmRecipePositionData)this.Standby2.Clone(),
                Begin = (TransferArmRecipePositionData)this.Begin.Clone(),
                Center = (TransferArmRecipePositionData)this.Center.Clone(),
                End = (TransferArmRecipePositionData)this.End.Clone(),
                Dispense1 = (TransferArmRecipePositionData)this.Dispense1.Clone(),
                Dispense2 = (TransferArmRecipePositionData)this.Dispense2.Clone(),
                Dispense3 = (TransferArmRecipePositionData)this.Dispense3.Clone(),
                Dispense4 = (TransferArmRecipePositionData)this.Dispense4.Clone(),
                Dispense5 = (TransferArmRecipePositionData)this.Dispense5.Clone(),
            };
        }
    }
    public class TransferArmInteraction
    {
        public string AxisName { get; set; }
        public ObservableCollection<TransferArmInteractionData> TransferArmInteractionData { get; set; } = new ObservableCollection<TransferArmInteractionData>();
    }

    public class TransferArmInteractionData
    {
        public string TranserAxisName { get; set; }
        public string From { get; set; }
        public string To { get; set; }
    }

    public class TransferArmRecipePositionData : ICloneable
    {

        public TransferArmRecipePositionData()
        { }

        public TransferArmRecipePositionData(string value)
        {
            if (!(value == null || value == ""))
            {
                var strList = value.Split(';');
                Acceleration = double.Parse(strList[0]).ToString("f2");
                Deceleration = double.Parse(strList[1]).ToString("f2");
                TopSpeed = double.Parse(strList[2]).ToString("f2");
                Position = double.Parse(strList[3]).ToString("f2");
                HeightPositionSelection = strList[4];
                var offsetList = strList[5].Split(',');
                Nozzle1Offset = double.Parse(offsetList[0]).ToString("f2");
                Nozzle2Offset = double.Parse(offsetList[1]).ToString("f2");
                Nozzle3Offset = double.Parse(offsetList[2]).ToString("f2");
                Nozzle4Offset = double.Parse(offsetList[3]).ToString("f2");
                Nozzle5Offset = double.Parse(offsetList[4]).ToString("f2");
                Nozzle6Offset = double.Parse(offsetList[5]).ToString("f2");
                Nozzle7Offset = double.Parse(offsetList[6]).ToString("f2");
                Nozzle8Offset = double.Parse(offsetList[7]).ToString("f2");
            }
        }

        public string Acceleration { get; set; }
        public string Deceleration { get; set; }
        public string TopSpeed { get; set; }
        public string Position { get; set; }
        public string HeightPositionSelection { get; set; }
        public string Nozzle1Offset { get; set; }
        public string Nozzle2Offset { get; set; }
        public string Nozzle3Offset { get; set; }
        public string Nozzle4Offset { get; set; }
        public string Nozzle5Offset { get; set; }
        public string Nozzle6Offset { get; set; }
        public string Nozzle7Offset { get; set; }
        public string Nozzle8Offset { get; set; }
        public string Units { get; set; }

        public override string ToString()
        {
            string rtn = $"{Acceleration};{Deceleration};{TopSpeed};{Position};{HeightPositionSelection};" +
                $"{Nozzle1Offset},{Nozzle2Offset},{Nozzle3Offset},{Nozzle4Offset}," +
                $"{Nozzle5Offset},{Nozzle6Offset},{Nozzle7Offset},{Nozzle8Offset}";
            return rtn.ToString();
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class DispenseConfig
    {
        public string Name { get; set; }
        public bool IsChecked { get; set; }
        public string DummyDispense { get; set; }
        public string DispenseMechanismName { get; set; }
        public string DispenseMechanismData { get; set; }
        public string FlowAmountSensor { get; set; }
        public FlowPumpData FlowPumpData { get; set; } = new FlowPumpData();
        public FlowSensorData FlowSensorData { get; set; } = new FlowSensorData();
        public string AutoSupplySystemSupply { get; set; }
        public AutoSupplySystem AutoSupplySystem { get; set; } = new AutoSupplySystem();
        public string AutoSupplySystemDrain { get; set; }
        public DispenseDrainData DispenseDrain { get; set; } = new DispenseDrainData();

        public DispenseConfig(string value)
        {
            CreateDispenseConfig(value);
        }

        public DispenseConfig()
        {
            CreateDispenseConfig("");
        }

        public void CreateDispenseConfig(string value)
        {
            if (value == null || value == "")
            {
                this.IsChecked = false;
                this.DummyDispense = "Yes";
                this.FlowAmountSensor = "-";
                this.DispenseMechanismName = "No Conn";
                this.AutoSupplySystemSupply = "-";
                this.AutoSupplySystemDrain = "-";
                this.FlowPumpData = new FlowPumpData();
                this.FlowSensorData = new FlowSensorData();
            }
            else
            {

                string[] listStr = value.Split('，');
                if (listStr.Length == 12)
                {
                    Name = listStr[0];
                    IsChecked = bool.Parse(listStr[1]);
                    DummyDispense = listStr[2];
                    DispenseMechanismName = listStr[3];
                    DispenseMechanismData = listStr[4];
                    FlowAmountSensor = listStr[5];
                    FlowPumpData = new FlowPumpData(listStr[6]);
                    FlowSensorData = new FlowSensorData(listStr[7]);
                    AutoSupplySystemSupply = listStr[8];
                    //AutoSupplySystem = new AutoSupplySystem(listStr[9]);
                    if (listStr[10] == "Drain pump")
                    {
                        AutoSupplySystemDrain = "Drain Pump";
                    }
                    else
                    {
                        AutoSupplySystemDrain = listStr[10];
                    }
                    //DispenseDrain = new DispenseDrainData(listStr[11]);
                }
                else
                {
                    Name = listStr[0];
                    IsChecked = bool.Parse(listStr[1]);
                    DummyDispense = listStr[2];
                    DispenseMechanismName = listStr[3];
                    DispenseMechanismData = listStr[4];
                    FlowAmountSensor = listStr[5];
                    FlowPumpData = new FlowPumpData(listStr[6]);
                    FlowSensorData = new FlowSensorData(listStr[7]);
                    AutoSupplySystemSupply = listStr[8];
                    if (listStr[9] == "Drain pump")
                    {
                        AutoSupplySystemDrain = "Drain Pump";
                    }
                    else
                    {
                        AutoSupplySystemDrain = listStr[9];
                    }
                   
                    //  DispenseDrain = new DispenseDrainData(listStr[10]);
                }
            }
        }


        public override string ToString()
        {
            string[] rtn = new string[]
                {
                 Name,
                 IsChecked.ToString(),
                 DummyDispense,
                 DispenseMechanismName,
                 DispenseMechanismData,
                 FlowAmountSensor,
                 FlowPumpData.ToString(),
                 FlowSensorData.ToString(),
                 AutoSupplySystemSupply,
                // AutoSupplySystem.ToString(),
                 AutoSupplySystemDrain
               // , DispenseDrain.ToString()
                 };
            return String.Join("，", rtn);
        }
    }
    public class FlowPumpData : ICloneable
    {
        public string PumpCapacity { get; set; }
        public string PassOperation { get; set; }
        public string PluseCount { get; set; }
        public string SpareReload { get; set; }
        public string DispenseWorkControlMethod { get; set; }
        public string AlarmValue { get; set; }
        public string StopValue { get; set; }
        public string Calibration { get; set; }

        public FlowPumpData()
        {
            PumpCapacity = "8.0";
            PassOperation = "Off";
            PluseCount = "0";
            SpareReload = "0";
            DispenseWorkControlMethod = "Total Dispense Count";
            AlarmValue = "1000";
            StopValue = "1000";
            Calibration = "1.00";
        }

        public FlowPumpData(string value)
        {
            var rtn = value.Split('/');
            PumpCapacity = rtn[0];
            PassOperation = rtn[1];
            PluseCount = rtn[2];
            SpareReload = rtn[3];
            DispenseWorkControlMethod = rtn[4];
            AlarmValue = rtn[5];
            StopValue = rtn[6];
            Calibration = rtn[7];
        }

        public override string ToString()
        {
            string[] strList = new string[]
            {
                PumpCapacity,PassOperation,PluseCount,SpareReload,DispenseWorkControlMethod,
                AlarmValue,StopValue,Calibration
            };
            return String.Join("/", strList);
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public object DeepClone()
        {
            return new FlowPumpData()
            {
                PumpCapacity = this.PumpCapacity,
                PassOperation = this.PassOperation,
                PluseCount = this.PassOperation,
                SpareReload = this.SpareReload,
                DispenseWorkControlMethod = this.DispenseWorkControlMethod,
                AlarmValue = this.AlarmValue,
                StopValue = this.StopValue,
                Calibration = this.Calibration
            };
        }
    }
    public class FlowSensorData : ICloneable
    {
        public string LiquidType { get; set; }
        public string SamplingCompleteDelayTime { get; set; }
        public string CheckTiming { get; set; }
        public string DummyDispenseFlowMonitoring { get; set; }
        public string HMDSValue { get; set; }
        public string Calibration { get; set; }
        public string AlarmRangeUpper { get; set; }
        public string AlarmRangeLower { get; set; }
        public string StopRangeUpper { get; set; }
        public string StopRangeLower { get; set; }
        public string DispenseTime { get; set; }
        public string N2Time { get; set; }
        public string N2Value { get; set; }

        public FlowSensorData()
        {
            LiquidType = "Solution";
            SamplingCompleteDelayTime = "1.0";
            CheckTiming = "None";
            DummyDispenseFlowMonitoring = "Yes";
            HMDSValue = "0";
            Calibration = "1.00";
            AlarmRangeUpper = "50.00";
            AlarmRangeLower = "50.00";
            StopRangeUpper = "50.00";
            StopRangeLower = "50.00";
            DispenseTime = "5.0";
            N2Time = "10.0";
            N2Value = "10";
        }

        public FlowSensorData(string value)
        {
            var rtn = value.Split('/');
            LiquidType = rtn[0];
            SamplingCompleteDelayTime = double.Parse(rtn[1]).ToString("F1");
            CheckTiming = rtn[2];
            DummyDispenseFlowMonitoring = rtn[3];
            HMDSValue = rtn[4];
            Calibration = rtn[5];
            AlarmRangeUpper = rtn[6];
            AlarmRangeLower = rtn[7];
            StopRangeUpper = rtn[8];
            StopRangeLower = rtn[9];
            if (rtn.Length > 10)
            {
                DispenseTime = double.Parse(rtn[10]).ToString("F1"); ;
                N2Time = double.Parse(rtn[11]).ToString("F1"); ;
            }
            else
            {
                DispenseTime = "5.0";
                N2Time = "10.0";
            }
            if (rtn.Length > 12)
            {
                N2Value = double.Parse(rtn[12]).ToString("F1"); ;
            }
            else
            {
                N2Value = "10";
            }

        }

        public override string ToString()
        {
            string[] strList = new string[]
            {
                LiquidType,SamplingCompleteDelayTime,CheckTiming,DummyDispenseFlowMonitoring,
                HMDSValue,Calibration,AlarmRangeUpper,AlarmRangeLower,StopRangeUpper,StopRangeLower,DispenseTime,N2Time,N2Value
            };
            return String.Join("/", strList);
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public object DeepClone()
        {
            return new FlowSensorData()
            {
                LiquidType = this.LiquidType,
                SamplingCompleteDelayTime = this.SamplingCompleteDelayTime,
                CheckTiming = this.CheckTiming,
                DummyDispenseFlowMonitoring = this.DummyDispenseFlowMonitoring,
                HMDSValue = this.HMDSValue,
                Calibration = this.Calibration,
                AlarmRangeUpper = this.AlarmRangeUpper,
                AlarmRangeLower = this.AlarmRangeLower,
                StopRangeUpper = this.StopRangeUpper,
                StopRangeLower = this.StopRangeLower,
                DispenseTime = this.DispenseTime,
                N2Time = this.N2Time,
                N2Value = this.N2Value
            };
        }
    }
    public class AutoSupplySystem : ICloneable
    {
        public string Name { get; set; }
        public string Mode { get; set; }
        public string LiquidSupplySource { get; set; }
        public string SupplyTime { get; set; }
        public string VacuumTime { get; set; }
        public string WaitingTime { get; set; }
        public string SupplyDelayTime { get; set; }
        public string PurgeTime { get; set; }
        public string LERecoverTime { get; set; }
        public string LESensor { get; set; }
        public string BubblingTime { get; set; }
        public string DrainTime { get; set; }

        public AutoSupplySystem()
        {
            Name = "Solvent(1)";
            Mode = "Auto";
            LiquidSupplySource = "Invalid";
            SupplyTime = "1.0";
            VacuumTime = "0.0";
            WaitingTime = "0.0";
            SupplyDelayTime = "0.0";
            PurgeTime = "99";
            LERecoverTime = "0.0";
            LESensor = "0";
            BubblingTime = "0.0";
            DrainTime = "0.0";
        }

        public AutoSupplySystem(string value)
        {
            var rtn = value.Split('/');
            Name = rtn[0];
            Mode = rtn[1];
            LiquidSupplySource = rtn[2];
            SupplyTime = double.Parse(rtn[3]).ToString("F1");
            VacuumTime = double.Parse(rtn[4]).ToString("F1");
            WaitingTime = double.Parse(rtn[5]).ToString("F1");
            SupplyDelayTime = double.Parse(rtn[6]).ToString("F1");
            PurgeTime = double.Parse(rtn[7]).ToString("F1");
            LERecoverTime = double.Parse(rtn[8]).ToString("F1");
            LESensor = rtn[9];
            BubblingTime = double.Parse(rtn[10]).ToString("F1");
            DrainTime = double.Parse(rtn[11]).ToString("F1");
        }

        public override string ToString()
        {
            string[] strList = new string[]
           {
               Name,Mode,LiquidSupplySource,SupplyTime,VacuumTime,WaitingTime,SupplyDelayTime,
               PurgeTime,LERecoverTime,LESensor,BubblingTime,DrainTime
           };
            return String.Join("/", strList);
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public object DeepClone()
        {
            return new AutoSupplySystem()
            {
                Name = this.Name,
                Mode = this.Mode,
                LiquidSupplySource = this.LiquidSupplySource,
                SupplyTime = this.SupplyTime,
                VacuumTime = this.VacuumTime,
                WaitingTime = this.WaitingTime,
                SupplyDelayTime = this.SupplyDelayTime,
                PurgeTime = this.PurgeTime,
                LERecoverTime = this.LERecoverTime,
                LESensor = this.LESensor,
                BubblingTime = this.BubblingTime,
                DrainTime = this.DrainTime,
            };
        }

        public Dictionary<string, object> GetScValue(string root)
        {
            Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
            keyValuePairs.Add($"{root}Mode", Mode);
            keyValuePairs.Add($"{root}LiquidSupplySource", LiquidSupplySource);
            keyValuePairs.Add($"{root}SupplyTime", SupplyTime);

            keyValuePairs.Add($"{root}VacuumTime", VacuumTime);
            keyValuePairs.Add($"{root}WaitingTime", WaitingTime);
            keyValuePairs.Add($"{root}SupplyDelayTime", SupplyDelayTime);

            keyValuePairs.Add($"{root}PurgeTime", PurgeTime);
            keyValuePairs.Add($"{root}LERecoverTime", LERecoverTime);
            keyValuePairs.Add($"{root}LESensor", LESensor);

            keyValuePairs.Add($"{root}BubblingTime", BubblingTime);
            keyValuePairs.Add($"{root}DrainTime", DrainTime);
            return keyValuePairs;
        }
    }

    public class DispenseDrainData : ICloneable
    {
        public string Name { get; set; }
        public string AutoDrainMode { get; set; }
        public string LiquidSupplySource { get; set; }
        public string DrainingTime { get; set; }

        public DispenseDrainData()
        {
            Name = "NMP";
            AutoDrainMode = "Auto";
            LiquidSupplySource = "Invalid";
            DrainingTime = "60.0";
        }

        public DispenseDrainData(string value)
        {
            var rtn = value.Split('/');
            Name = rtn[0];
            AutoDrainMode = rtn[1];
            LiquidSupplySource = rtn[2];
            DrainingTime = double.Parse(rtn[3]).ToString("F1");
        }

        public override string ToString()
        {
            string[] strList = new string[]
          {
              Name,AutoDrainMode,LiquidSupplySource,DrainingTime
            };
            return String.Join("/", strList);
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public object DeepClone()
        {
            return new DispenseDrainData()
            {
                Name = this.Name,
                AutoDrainMode = this.AutoDrainMode,
                LiquidSupplySource = this.LiquidSupplySource,
                DrainingTime = this.DrainingTime
            };
        }

        public Dictionary<string, object> GetScValue(string root)
        {
            Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
            keyValuePairs.Add($"{root}Name", Name);
            keyValuePairs.Add($"{root}AutoDrainMode", AutoDrainMode);
            keyValuePairs.Add($"{root}LiquidSupplySource", LiquidSupplySource);
            keyValuePairs.Add($"{root}DrainingTime", DrainingTime);
            return keyValuePairs;
        }

    }

    public class MonitorEditData : ICloneable
    {
        public string IsValid { get; set; }
        public string Module { get; set; }
        public string MeasDateName { get; set; }
        public string RecipeName { get; set; }
        public string TempControlForm { get; set; }
        public string ControllerName { get; set; }
        public string Node { get; set; }
        public string PortNo { get; set; }
        public string ChannelNo { get; set; }
        public string TunForm { get; set; }
        public string TunDestNode { get; set; }
        public string TunDestSensorID { get; set; }
        public string ControlForm { get; set; }
        public string InitialData { get; set; }
        public string OverTemp { get; set; }
        public string SettlingDetermineTime { get; set; }
        public string SettlingTimeOut { get; set; }
        public string UsePointOffset { get; set; }
        public string StartMonitorForm { get; set; }
        public string StartInvalidTime { get; set; }
        public string ProcessMonitorForm { get; set; }
        public string ProcessDeterminationTime { get; set; }
        public string ProcessValue { get; set; }
        public string PIDSetForm { get; set; }
        public string PIDPParam { get; set; }
        public string PIDIParam { get; set; }
        public string PIDDParam { get; set; }
        public string OffsetSetForm { get; set; }
        public string OffsetVal { get; set; }
        public string AConstantSetForm { get; set; }
        public string AConstantVal { get; set; }
        public string RangeMinimum { get; set; }
        public string RangeMaximum { get; set; }
        public string UsePointCtrlSettleTime { get; set; }
        public string UsePointCtrlSourceBand { get; set; }
        public string UsePointCtrlLearnBand { get; set; }
        public string UsePointUseBand { get; set; }

        public MonitorEditData()
        {
            CreateMonitorEditData();
        }

        public MonitorEditData(string data)
        {
            CreateMonitorEditData(data);
        }

        private void CreateMonitorEditData()
        {
            CreateMonitorEditData("");
        }

        private void CreateMonitorEditData(string data)
        {
            if (data == "")
            {
                IsValid = "Valid";
                Module = "";
                MeasDateName = "Cup Temperature";
                RecipeName = "Local";
                TempControlForm = "Temp Control ON";
                ControllerName = "THC Controller";
                PortNo = "8";
                ChannelNo = "0";
                TunForm = "No Sync";
                TunDestNode = "";
                TunDestSensorID = "2";
                ControlForm = "Standard";
                InitialData = "23.00";
                OverTemp = "180.00";
                SettlingDetermineTime = "5.0";
                SettlingTimeOut = "600.0";
                UsePointOffset = "0.00";
                StartMonitorForm = "Normal";
                StartInvalidTime = "0.0";
                ProcessMonitorForm = "Monitor";
                ProcessDeterminationTime = "0.0";
                ProcessValue = "0.0";
                PIDSetForm = "Manual Setting";
                PIDPParam = "1.00";
                PIDIParam = "25.00";
                PIDDParam = "5.00";
                OffsetSetForm = "Set";
                OffsetVal = "0.00";
                AConstantSetForm = "Set";
                AConstantVal = "0.00";
                RangeMinimum = "50.0";
                RangeMaximum = "200.0";
                UsePointCtrlSettleTime = "0.0";
                UsePointCtrlSourceBand = "0.00";
                UsePointCtrlLearnBand = "0.00";
                UsePointUseBand = "0.00";
            }
            else
            {
                int index = 0;
                var rtn = data.Split('/');
                IsValid = rtn[index++];
                Module = rtn[index++];
                MeasDateName = rtn[index++];
                RecipeName = rtn[index++];
                TempControlForm = rtn[index++];
                ControllerName = rtn[index++];
                if (ControllerName == "TCH Controller") ControllerName = "THC Controller";
               PortNo = rtn[index++];
                ChannelNo = rtn[index++];
                TunForm = rtn[index++];
                TunDestNode = rtn[index++];
                TunDestSensorID = rtn[index++];
                ControlForm = rtn[index++];
                InitialData = rtn[index++];
                OverTemp = rtn[index++];
                SettlingDetermineTime = rtn[index++];
                SettlingTimeOut = rtn[index++];
                UsePointOffset = rtn[index++];
                StartMonitorForm = rtn[index++];
                StartInvalidTime = rtn[index++];
                ProcessMonitorForm = rtn[index++];
                ProcessDeterminationTime = rtn[index++];
                ProcessValue = rtn[index++];
                PIDSetForm = rtn[index++];
                PIDPParam = rtn[index++];
                PIDIParam = rtn[index++];
                PIDDParam = rtn[index++];
                OffsetSetForm = rtn[index++];
                OffsetVal = rtn[index++];
                AConstantSetForm = rtn[index++];
                AConstantVal = rtn[index++];
                RangeMinimum = rtn[index++];
                RangeMaximum = rtn[index++];
                UsePointCtrlSettleTime = rtn[index++];
                UsePointCtrlSourceBand = rtn[index++];
                UsePointCtrlLearnBand = rtn[index++];
                UsePointUseBand = rtn[index++];
            }
        }

        public override string ToString()
        {
            string[] strList = new string[]
          {
            IsValid,Module,MeasDateName,RecipeName,TempControlForm, ControllerName,PortNo,ChannelNo,TunForm,TunDestNode,TunDestSensorID,ControlForm,
            InitialData,OverTemp,SettlingDetermineTime,SettlingTimeOut,UsePointOffset,StartMonitorForm,StartInvalidTime,ProcessMonitorForm,
            ProcessDeterminationTime,ProcessValue,PIDSetForm, PIDPParam,PIDIParam,PIDDParam,OffsetSetForm,OffsetVal,AConstantSetForm,
            AConstantVal,RangeMinimum,RangeMaximum,UsePointCtrlSettleTime,UsePointCtrlSourceBand,UsePointCtrlLearnBand,UsePointUseBand
        };
            return String.Join("/", strList);
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

    }

    public class DataCollecion
    {
        public string MouleName { get; set; }
        public ObservableCollection<string> MeasureData { get; set; } = new ObservableCollection<string>();
    }

    public class SystemParameter
    {
        public string WaferExistInRCV { get; set; }
        public string AutomaticRegistrationEditor { get; set; }
        public string DesignationOfLotEnd { get; set; }
        public string DefaultScreenForRunButton { get; set; }
        public string CupWashingAlarm { get; set; }
        public string EISInitializeCondition { get; set; }
    }




    public class TransferArmPosition
    {
        public string TransferMode { get; set; }
        public double StartOffset { get; set; }
        public double EndOffset { get; set; }
        public int SlowSpeedRate { get; set; }

        public TransferArmPosition()
        {
            PincetteList.Clear();
            PincetteList.Add("pincette1", new Pincette());
            PincetteList.Add("pincette2", new Pincette());
            PincetteList.Add("pincette3", new Pincette());
        }

        public Dictionary<string, Pincette> PincetteList { get; set; } = new Dictionary<string, Pincette>();

        public Dictionary<string, object> GetScValue(string root)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result[$"{root}Zaxis.TransferMode"] = TransferMode;
            result[$"{root}Zaxis.StartOffset"] = StartOffset;
            result[$"{root}Zaxis.EndOffset"] = EndOffset;
            result[$"{root}Zaxis.SlowSpeedRate"] = SlowSpeedRate;
            result.AddRange(PincetteList["pincette1"].GetScValue($"{root}pincette1."));
            result.AddRange(PincetteList["pincette2"].GetScValue($"{root}pincette2."));
            result.AddRange(PincetteList["pincette3"].GetScValue($"{root}pincette3."));
            return result;
        }
    }

    public class Pincette
    {
        public double XaxisReceive { get; set; }
        public double XaxisSend { get; set; }
        public double YaxisReceive { get; set; }
        public double YaxisSend { get; set; }
        public double ZaxisReceive { get; set; }
        public double ZaxisSend { get; set; }
        public double RaxisReceive { get; set; }
        public double RaxisSend { get; set; }
        public double StrokeZaxisReceive { get; set; }
        public double StrokeZaxisSend { get; set; }
        public int ZaxisReceivePer { get; set; }
        public int ZaxisSendPer { get; set; }


        public Dictionary<string, object> GetScValue(string root)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result[$"{root}XaxisReceive"] = XaxisReceive;
            result[$"{root}XaxisSend"] = XaxisSend;
            result[$"{root}YaxisReceive"] = YaxisReceive;
            result[$"{root}YaxisSend"] = YaxisSend;
            result[$"{root}ZaxisReceive"] = ZaxisReceive;
            result[$"{root}ZaxisSend"] = ZaxisSend;
            result[$"{root}RaxisReceive"] = RaxisReceive;
            result[$"{root}RaxisSend"] = RaxisSend;
            result[$"{root}StrokeZaxisReceive"] = StrokeZaxisReceive;
            result[$"{root}StrokeZaxisSend"] = StrokeZaxisSend;
            result[$"{root}ZaxisReceivePer"] = ZaxisReceivePer;
            result[$"{root}ZaxisSendPer"] = ZaxisSendPer;
            return result;
        }

    }

    public class ArmPosition
    {
        public int X1 { get; set; }

        public int X2 { get; set; }

        public int X3 { get; set; }

        public Dictionary<string, object> GetScValue(string root)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result.Add($"{root}X1", X1);
            result.Add($"{root}X2", X2);
            result.Add($"{root}X3", X3);
            return result;
        }
    }
}
