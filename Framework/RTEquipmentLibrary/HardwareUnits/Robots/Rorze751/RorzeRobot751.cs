using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using EventType = Aitex.Core.RT.Event.EventType;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.Rorze
{
    public class RorzeRobot751 : RobotBaseDevice, IConnection
    {
       
        private bool isSimulatorMode;
        private string _scRoot;
        public RROpModeEnum CurrentOpMode { get; set; }
        public bool IsOrgshCompleted { get; set; }
        public bool IsCmdProcessing { get; set; }
        public RROpStatusEnum CurrentOpStatus { get; set; }
        public int CurrentOperationSpeed { get; set; } //0:Normal, 1-K Set Speed
        public string CurrentIdCodeForErrorController { get; set; }     

        public bool _diEms { get; set; }
        public bool _diTempStop { get; set; }
        public bool _diVacuumPressure { get; set; }
        public bool _diAirSourcePressure { get; set; }
        public bool _diZaxisFan { get; set; }
        public bool _diUpperArmFan { get; set;}
        public bool _diLowerArmFan { get; set; }
        public bool _diUpperFinger1ArmWaferExistence1 { get; set; }
        public bool _diUpperFinger1ArmWaferExistence2 { get; set; }
        public bool _diLowerArmWaferExistence1 { get; set; }
        public bool _diLowerArmWaferExistence2 { get; set; }
        public bool _diTPEms { get; set; }
        public bool _diDeadmanSwitch { get; set; }
        public bool _diModeKey { get; set; }


        public bool IsEnableSeqNo { get; private set; }
        public bool IsEnableCheckSum { get; private set; }
        public int CurrentSeqNo { get; set; }

        public string PortName;

        internal void OnEventReceived(string rawMessage)
        {
            try
            {
                string eventname = rawMessage.Split('.')[1].Split(':')[0];
                string[] data = rawMessage.Split('.')[1].Split(':')[1].Split('/');

                switch (eventname)
                {
                    case "STAT":
                        ParseSTATStatus(data);
                        break;
                    case "GPIO":
                        ParseDioStatus(data);
                        break;


                }
            }
            catch(Exception ex)
            {
                LOG.Write(ex);
            }


        }

        private string _address;
        private bool _enableLog;
        private RorzeRobot751Connection _connection;
        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        public string Address => _address;
        private PeriodicJob _thread;
        private object _locker = new object();
        private LinkedList<HandlerBase> _lstHandlers = new LinkedList<HandlerBase>();
        private IoSensor _diRobotReady = null;  //Normal ON
        private IoSensor _diRobotBlade1WaferOn = null;   //Off when wafer present
        private IoSensor _diRobotBlade2WaferOn = null;
        private IoSensor _diRobotError = null; //Normal ON
        private IoSensor _diTPinUse = null;

        private IoTrigger _doRobotHold = null; // Normal ON
        public ModuleName CurrentInteractiveModule { get; private set; }
        public bool IsConnected => throw new NotImplementedException();

        public bool IsGrippedBlade1 { get; private set; }
        public bool IsGrippedBlade2 { get; private set; }
        public bool IsPermittedInterlock1 { get; private set; }
        public bool IsPermittedInterlock2 { get; private set; }
        public bool IsPermittedInterlock3 { get; private set; }
        public bool IsPermittedInterlock4 { get; private set; }
        public bool IsPermittedInterlock5 { get; private set; }
        public bool IsPermittedInterlock6 { get; private set; }
        public bool IsPermittedInterlock7 { get; private set; }
        public bool IsPermittedInterlock8 { get; private set; }

        public float CurrentThetaPosition { get; private set; }
        public float CurrentExtensionPosition { get; private set; }
        public float CurrentArm1Position { get; private set; }
        public float CurrentArm2Position { get; private set; }
        public float CurrentZPosition { get; private set; }

        public float CommandThetaPosition { get; private set; }
        public float CommandExtensionPosition { get; private set; }
        public float CommandArm1Position { get; private set; }
        public float CommandArm2Position { get; private set; }
        public float CommandZPosition { get; private set; }
        public int SpeedLevel { get; private set; }
        public int SpeedLevelSetting { get; private set; }

        public string ReadMemorySpec { get; private set; }
        public string ReadTransferStation { get; private set; }
        public int ReadSlotNumber { get; private set; }
        public string ReadArmPosture { get; private set; }
        public RobotArmEnum ReadBladeNo { get; private set; }        
        public float ReadThetaPosition { get; private set; }
        public float ReadExtensionPosition { get; private set; }
        public float ReadArm1Position { get; private set; }
        public float ReadArm2Position { get; private set; }
        public float ReadZPosition { get; private set; }
        public Dictionary<string, string> ReadStationItemValues { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, string> ReadStationItemContents { get; private set; } = new Dictionary<string, string>();

        public string ReadParameterType { get; private set; }
        public string ReadParameterNo { get; private set; }
        public string ReadParameterValue { get; private set; }

        public bool IsManipulatorBatteryLow { get; private set; }
        public bool IsCommandExecutionReady { get; private set; }
        public bool IsServoON { get; private set; }
        public bool IsErrorOccurred { get; private set; }
        public bool IsControllerBatteryLow { get; private set; }

        public bool IsCheckInterlockWaferPresenceOnBlade1 { get; private set; }
        public bool IsCheckInterlockWaferPresenceOnBlade2 { get; private set; }

        public bool IsCheckInterlockPAOp { get; private set; }
        public bool IsCheckInterlockPAWaferStatus { get; private set; }
        public bool IsCheckInterlockPAWaferStatusByCCD { get; private set; }

        public string RobotSystemVersion { get; private set; }
        public string RobotSoftwareVersion { get; private set; }

        public string ReadMappingTransferStation { get; private set; }
        public int ReadMappingSlotNumbers { get; private set; }
        public string ReadSlotMap { get; private set; }
        public int RobotBodyNumber { get; private set; }
      
        private DateTime _dtActionStart;
        public RobotArmEnum chcekingArm { get; set; }
       
        public bool ParseComplete;
        public Dictionary<string, float> ReadMappingCalibrationResult { get; private set; }



        public RorzeRobot751(string module, string name, string scRoot, IoSensor[] dis, IoTrigger[] dos) : base(module, name)
        {
            Module = module;
            Name = name;

            isSimulatorMode = SC.ContainsItem("System.IsSimulatorMode") ? SC.GetValue<bool>("System.IsSimulatorMode") : false;
            _scRoot = scRoot;
            _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            RobotBodyNumber = SC.GetValue<int>($"{_scRoot}.{Name}.BodyNumber");

            SpeedLevelSetting = SC.GetValue<int>($"{_scRoot}.{Name}.SpeedLevel");
            _connection = new RorzeRobot751Connection(this, _address);
            _connection.EnableLog(_enableLog);
           

            if (dis != null && dis.Length >= 5)
            {
                _diRobotReady = dis[0];
                _diRobotBlade1WaferOn = dis[1];
                _diRobotBlade2WaferOn = dis[2];
                _diRobotError = dis[3];
                _diTPinUse = dis[4];
                _diRobotError.OnSignalChanged += _diRobotError_OnSignalChanged;
                _diTPinUse.OnSignalChanged += _diTPinUse_OnSignalChanged;
            }
            if (dos != null && dos.Length >= 1)
            {
                _doRobotHold = dos[0];
            }

            ConnectionManager.Instance.Subscribe($"{Name}", _connection);
            _thread = new PeriodicJob(1, OnTimer, $"{_scRoot}.{Name} MonitorHandler", true);
        



            ResetPropertiesAndResponses();
            RegisterSpecialData();
            RegisterAlarm();

        }

        public void HandlerMotion(string command, string[] pdata)
        {
            switch (command)
            {
                case "MMAP":
                    try
                    {
                        
                        StringBuilder sb = new StringBuilder();
                        for (int i = 6; i < pdata.Length; i++)
                        {
                            string value = pdata[i].Substring(3);
                            switch (value)
                            {
                                case "--":
                                    sb.Append("0");
                                    break;
                                case "OK":
                                    sb.Append("1");
                                    break;
                                case "CW":
                                    sb.Append("2");
                                    break;
                                case "DW":
                                    sb.Append("W");
                                    break;
                            }
                        }
                        ReadSlotMap = sb.ToString();
                        NotifySlotMapResult(CurrentInteractiveModule, ReadSlotMap);
                        return;
                    }
                    catch (Exception ex)
                    {
                        LOG.Write(ex);
                        
                    }
                    break;
                default:
                    break;
            }
        }

        private void RegisterAlarm()
        {           

        }

        public void NotifyAlarmByErrorCode(string errorcode)
        {
            EV.Notify($"{Name}Error{errorcode}");
        }
        private void _diTPinUse_OnSignalChanged(IoSensor arg1, bool arg2)
        {
            SetMaintenanceMode(!arg1.Value);
        }

        private void _diRobotError_OnSignalChanged(IoSensor arg1, bool arg2)
        {
            if (arg1.Value == false)
            {
                lock (_locker)
                {
                    _lstHandlers.AddLast(new RorzeRobot751ReadHandler(this, "STAT"));
                }
                OnError("RobotError");
            }
        }

        private void ResetPropertiesAndResponses()
        {

        }

        private void RegisterSpecialData()
        {
            DATA.Subscribe($"{Module}.{Name}.CurrentArm1Position", () => CurrentArm1Position);
            DATA.Subscribe($"{Module}.{Name}.CurrentArm2Position", () => CurrentArm2Position);
            DATA.Subscribe($"{Module}.{Name}.CurrentExtensionPosition", () => CurrentExtensionPosition);
            DATA.Subscribe($"{Module}.{Name}.CurrentThetaPosition", () => CurrentThetaPosition);
            DATA.Subscribe($"{Module}.{Name}.CurrentZPosition", () => CurrentZPosition);

            DATA.Subscribe($"{Module}.{Name}.IsManipulatorBatteryLow", () => IsManipulatorBatteryLow);
            DATA.Subscribe($"{Module}.{Name}.IsCommandExecutionReady", () => IsCommandExecutionReady);
            DATA.Subscribe($"{Module}.{Name}.IsServoON", () => IsServoON);
            DATA.Subscribe($"{Module}.{Name}.IsErrorOccurred", () => IsErrorOccurred);
            DATA.Subscribe($"{Module}.{Name}.IsControllerBatteryLow", () => IsControllerBatteryLow);
            DATA.Subscribe($"{Module}.{Name}.IsWaferPresenceOnBlade1", () => IsWaferPresenceOnBlade1);
            DATA.Subscribe($"{Module}.{Name}.IsWaferPresenceOnBlade2", () => IsWaferPresenceOnBlade2);

            DATA.Subscribe($"{Module}.{Name}.ErrorCode", () => ErrorCode);
            DATA.Subscribe($"{Module}.{Name}.IsGrippedBlade1", () => IsGrippedBlade1);
            DATA.Subscribe($"{Module}.{Name}.IsGrippedBlade2", () => IsGrippedBlade2);
            DATA.Subscribe($"{Module}.{Name}.IsPermittedInterlock1", () => IsPermittedInterlock1);
            DATA.Subscribe($"{Module}.{Name}.IsPermittedInterlock2", () => IsPermittedInterlock2);
            DATA.Subscribe($"{Module}.{Name}.IsPermittedInterlock3", () => IsPermittedInterlock3);
            DATA.Subscribe($"{Module}.{Name}.IsPermittedInterlock4", () => IsPermittedInterlock4);
            DATA.Subscribe($"{Module}.{Name}.IsPermittedInterlock5", () => IsPermittedInterlock5);
            DATA.Subscribe($"{Module}.{Name}.IsPermittedInterlock6", () => IsPermittedInterlock6);
            DATA.Subscribe($"{Module}.{Name}.IsPermittedInterlock7", () => IsPermittedInterlock7);
            DATA.Subscribe($"{Module}.{Name}.IsPermittedInterlock8", () => IsPermittedInterlock8);

            DATA.Subscribe($"{Module}.{Name}.RobotSpeed", () => SpeedLevelSetting.ToString());
            DATA.Subscribe($"{Name}.RobotSpeed", () =>
            {               
                return SpeedLevelSetting.ToString();
            });
            OP.Subscribe("SetSpeed", InvokeSetSpeed);

        }

        private bool OnTimer()
        {
            try
            {

                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstHandlers.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                            //_lstHandler.AddLast(new RobotHirataR4QueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new RobotHirataR4SetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
                        }
                    }
                    return true;
                }

                HandlerBase handler = null;

                lock (_locker)
                {
                    if (!_connection.IsBusy)
                    {
                            if (_lstHandlers.Count > 0)
                            {
                                handler = _lstHandlers.First.Value;
                                ExecuteHandler(handler);
                                _lstHandlers.RemoveFirst();
                            }
                    }
                    else
                    {
                            _connection.MonitorTimeout();

                            _trigCommunicationError.CLK = _connection.IsCommunicationError;
                            if (_trigCommunicationError.Q)
                            {
                                _lstHandlers.Clear();
                                //EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                                OnError($"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                                //_trigActionDone.CLK = true;
                            }
                    }
                }
                
            }

            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        public bool Connect()
        {
            return _connection.Connect();
        }

        public bool Disconnect()
        {
            return _connection.Disconnect();
        }

        public override bool IsReady()
        {
            //if (_diRobotReady!=null && !_diRobotReady.Value) 
            //    return false;
            if (_diRobotError != null && !_diRobotError.Value)
                return false;
            if (_diTPinUse != null && !_diTPinUse.Value)
                return false;
            return RobotState == RobotStateEnum.Idle && !IsBusy && fsm.CheckExecuted();
        }

        public bool ParseReadData(string _command, string[] rdata)
        {
            try
            {
                switch(_command)
                {
                    case "STAT":
                        rdata[0] = rdata[0].Split(':')[1];
                        ParseSTATStatus(rdata);
                        break;
                    case "GPIO":
                        rdata[0] = rdata[0].Split(':')[1];
                        ParseDioStatus(rdata);
                        break;
                    case "GMAP":
                        ParseSlotMap(rdata);
                        break;
                    case "GCHK":
                        ParseWaferPresence(rdata);
                        break;
                    case "EXST":
                        ParseWaferIsPresence(rdata);
                        break;


                }


                //if (_command == "RSLV")      //Read the speed level
                //{
                //    return (rdata.Length == 1 && ParseSpeedLevel(rdata[0]));
                //}
                //if (_command == "RPOS")   //Reference current postion
                //{
                //    return (rdata.Length > 1 && ParsePositionData(rdata));

                //}
                //if (_command == "RSTP")   //Reference registered position, read the save postion for station
                //{
                //    return (rdata.Length > 6 && ParseRegisteredPositionData(rdata));

                //}
                //if (_command == "RSTR")   //Reference station item value
                //{
                //    return (rdata.Length == 4 && ParseStationData(rdata));
                //}
                //if (_command == "RPRM")  //Reference the parameter values of the specified unit
                //{
                //    return (rdata.Length == 3 && ParseParameterData(rdata));
                //}
                //if (_command == "RMSK")  //Reference the interlock information
                //{
                //    return (rdata.Length == 1 && ParseInterlockInfo(rdata));

                //}
                //if (_command == "RVER")  //Reference the software version
                //{
                //    return (rdata.Length == 2 && ParseSoftwareVersion(rdata));
                //}
                //if (_command == "RMAP")  //Reference the slot map
                //{
                //    return (rdata.Length > 2 && ParseSlotMap(rdata));
                //}
                //if (_command == "RMPD")  //reference the mapping data
                //{
                //    return (rdata.Length > 1 && ParseMappingData(rdata));
                //}
                //if (_command == "RMCA")   // Reference the mapping calibration result
                //{
                //    return (rdata.Length > 1 && ParseMappingCalibrationResult(rdata));
                //}

                //if (_command == "RALN") // Reference the alignment result
                //{
                //    return true;
                //}
                //if (_command == "RACA") // Reference calibration result for alignment
                //{
                //    return true;
                //}
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return true;
            }
        }

        private void ParseDioStatus(string[] rdata)
        {
            var byte1=Convert.ToByte(rdata[0].Substring(12, 2),16);
            var byte2 = Convert.ToByte(rdata[0].Substring(10, 2),16);
            IsWaferPresenceOnBlade1 = ((byte2 >> 2) & 0x01) == 1 && ((byte2 >> 3) & 0x01) == 1;
            IsWaferPresenceOnBlade2 = ((byte1 >> 0) & 0x01) == 1 && ((byte1 >> 1) & 0x01) == 1;        
        }
        
        public bool ParseSTATStatus(string[] status)
        {
            try
            {
                
                CurrentOpMode = (RROpModeEnum)int.Parse(status[0].Substring(0,1));
                IsOrgshCompleted = status[0][1] == '1';
                IsCmdProcessing = status[0][2] == '1';
                CurrentOpStatus = (RROpStatusEnum)int.Parse(status[0].Substring(3, 1));
                CurrentOperationSpeed = int.Parse(status[0].Substring(4, 1));
                CurrentIdCodeForErrorController = status[1].Substring(0, 2);
                CurrentIdCodeForErrorController = status[1].Substring(2, 2);

                return true;

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return true;
            }
        }
        public bool ParseWaferPresence(string[] data)
        {
            try
            {

                var str = data[0].Split(':')[1];
                IsWaferPresenceOnBlade2 = int.Parse(str.Substring(0, 1)) == 0 ? false:true ;
                IsWaferPresenceOnBlade1 = int.Parse(str.Substring(1, 1)) == 0 ? false : true;

                return true;

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return true;
            }
        }
        public bool ParseWaferIsPresence(string[] data)
        {
            try
            {
                var str = data[0].Split(':')[1];
                if (chcekingArm == RobotArmEnum.Lower)
                {
                    IsWaferPresenceOnBlade1 = int.Parse(str.Substring(0, 1)) == 1 ? true : false;
                }
                else if (chcekingArm == RobotArmEnum.Upper)
                { 
                    IsWaferPresenceOnBlade2 = int.Parse(str.Substring(0, 1)) == 1 ? true : false; 
                }
                ParseComplete = true;
                return true;

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return true;
            }
        }
        public bool ParseSpeedLevel(string speedlevel)
        {
            try
            {
                int level = Convert.ToInt32(speedlevel);
                if (level < 1 || level > 3) return false;
                SpeedLevel = level;
                return true;

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        public bool ParsePositionData(string[] pdata)
        {
            try
            {
                if (pdata[0] == "R")
                {
                    CommandThetaPosition = Convert.ToSingle(pdata[1]) / 1000;
                    CommandExtensionPosition = Convert.ToSingle(pdata[2]) / 1000;
                    CommandArm1Position = Convert.ToSingle(pdata[3]) / 1000;
                    CommandArm2Position = Convert.ToSingle(pdata[4]) / 1000;
                    CommandZPosition = Convert.ToSingle(pdata[5]) / 1000;
                    return true;
                }
                if (pdata[0] == "F")
                {
                    CurrentThetaPosition = Convert.ToSingle(pdata[1]) / 1000;
                    PositionAxis1 = CurrentThetaPosition;
                    CurrentExtensionPosition = Convert.ToSingle(pdata[2]) / 1000;
                    PositionAxis2 = CurrentExtensionPosition;
                    CurrentArm1Position = Convert.ToSingle(pdata[3]) / 1000;
                    PositionAxis3 = CurrentArm1Position;
                    CurrentArm2Position = Convert.ToSingle(pdata[4]) / 1000;
                    PositionAxis4 = CurrentArm2Position;
                    CurrentZPosition = Convert.ToSingle(pdata[5]) / 1000;
                    PositionAxis5 = CurrentZPosition;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        public bool ParseRegisteredPositionData(string[] pdata)
        {
            return true;
        }

        public bool ParseStationData(string[] pdata)
        {
            try
            {
                ReadMemorySpec = pdata[0];
                ReadTransferStation = pdata[1];
                if (ReadStationItemValues.ContainsKey(pdata[2]))
                    ReadStationItemValues.Remove(pdata[2]);
                ReadStationItemValues.Add(pdata[2], pdata[3]);
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        public bool ParseParameterData(string[] pdata)
        {
            try
            {
                ReadParameterType = pdata[0];
                ReadParameterNo = pdata[1];
                ReadParameterValue = pdata[2];
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        public bool ParseInterlockInfo(string[] pdata)
        {
            try
            {
                int intdata = Convert.ToInt16(pdata[0]);
                IsCheckInterlockWaferPresenceOnBlade1 = (intdata & 0x1) == 0;
                IsCheckInterlockWaferPresenceOnBlade2 = (intdata & 0x2) == 0;
                IsCheckInterlockPAOp = (intdata & 0x10) == 0;
                IsCheckInterlockPAWaferStatus = (intdata & 0x20) == 0;
                IsCheckInterlockPAWaferStatusByCCD = (intdata & 0x40) == 0;
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        public bool ParseSoftwareVersion(string[] pdata)
        {
            try
            {
                RobotSystemVersion = pdata[0];
                RobotSoftwareVersion = pdata[1];
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        public bool ParseSlotMap(string[] pdata)
        {
            //            n = 0 : Wafer does not exist.
            //1 : Wafer exists.
            //2 : Thickness abnormal(thick)
            //3 : Cross
            //4 : Bow / Lift
            //7 : Multiple wafers
            //8 : Thickness abnormal(thin)
            //9 : Mapping failure
            try
            {                
                ReadSlotMap = pdata[0].Replace("2","W").Replace("3","2").Replace("4","?").Replace("7","W").Replace("8","?").Replace("9","?");
                NotifySlotMapResult(CurrentInteractiveModule, ReadSlotMap);
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        public bool ParseMappingData(string[] pdata)
        {
            try
            {
                ReadMappingTransferStation = pdata[0];

                List<string> lstupdata = new List<string>();
                List<string> lstdowndata = new List<string>();
                for (int i = 0; i < (pdata.Length - 1) / 2; i++)
                {
                    lstupdata.Add(pdata[2 * i + 1].Remove(0, 3));
                    lstdowndata.Add(pdata[2 * i + 2]);
                }
                ReadMappingDownData = lstdowndata.ToArray();
                ReadMappingUpData = lstupdata.ToArray();
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        public bool ParseMappingCalibrationResult(string[] pdata)
        {
            try
            {
                ReadMappingTransferStation = pdata[0];
                ReadMappingCalibrationResult.Clear();
                ReadMappingCalibrationResult.Add("LowestLaySlotPosition", Convert.ToInt32(pdata[1]) / 1000);
                ReadMappingCalibrationResult.Add("HighestLaySlotPosition", Convert.ToInt32(pdata[2]) / 1000);
                ReadMappingCalibrationResult.Add("WaferWidth", Convert.ToInt32(pdata[3]) / 1000);
                ReadMappingCalibrationResult.Add("ThreshhholdValueofDoubleInsertion", Convert.ToInt32(pdata[4]) / 1000);
                ReadMappingCalibrationResult.Add("ThreshhholdValueofSlantingInsertion1", Convert.ToInt32(pdata[5]) / 1000);
                ReadMappingCalibrationResult.Add("ThreshhholdValueofSlantingInsertion2", Convert.ToInt32(pdata[6]) / 1000);
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }


        protected override bool fClear(object[] param)
        {
            lock (_locker)
            {
                _lstHandlers.Clear();
                _connection.ForceClear();
                _lstHandlers.AddLast(new RorzeRobot751MotionHandler(this, "CCLR", "E"));
                _lstHandlers.AddLast(new RorzeRobot751ReadHandler(this, "STAT"));
            }
            return true;
        }
       
        protected override bool fStartReadData(object[] param)
        {
            if (param.Length < 1) return false;
            string readcommand = param[0].ToString();
            switch (readcommand)
            {
                case "CurrentStatus":
                    lock (_locker)
                    {
                        _lstHandlers.AddLast(new RorzeRobot751ReadHandler(this, "STAT"));
                    }
                    break;
                case "SignalStatus":
                    lock (_locker)
                    {
                        _lstHandlers.AddLast(new RorzeRobot751ReadHandler(this, "GPIO"));
                    }
                    break;
                case "CheckWaferIsPresence":
                    {
                        _dtActionStart = DateTime.Now;
                        ParseComplete = false;
                        IsBusy = true;
                        RobotArmEnum arm = chcekingArm = (RobotArmEnum)param[1];
                        int interval =Convert.ToInt16(param[2]);
                        switch (arm)
                        {
                            case RobotArmEnum.Lower:
                                _lstHandlers.AddLast(new RorzeRobot751ReadHandler(this, "EXST", $"(2,{interval})"));
                                break;
                            case RobotArmEnum.Upper:
                                _lstHandlers.AddLast(new RorzeRobot751ReadHandler(this, "EXST", $"(1,{interval})"));
                                break;
                        }
                    }
                    break;
                case "CheckWaferPresence":
                    {
                        RobotArmEnum arm = chcekingArm = (RobotArmEnum)param[1];
                        switch (arm)
                        {
                            case RobotArmEnum.Lower:
                                _lstHandlers.AddLast(new RorzeRobot751ReadHandler(this, "GCHK", "(2)"));
                                break;
                            case RobotArmEnum.Upper:
                                _lstHandlers.AddLast(new RorzeRobot751ReadHandler(this, "GCHK", "(1)"));
                                break;
                            case RobotArmEnum.Both:
                                _lstHandlers.AddLast(new RorzeRobot751ReadHandler(this, "GCHK", "(3)"));
                                break;
                        }
                    }
                    break;
                default:
                    break;
            }
            return true;
        }

        protected override bool fMonitorReadData(object[] param)
        {
            var readParameter = CurrentParamter[0].ToString();
            switch (readParameter)
            {
                case "CurrentStatus":
                    {
                        IsBusy = false;
                    }
                    break;
                case "SignalStatus":
                    {
                        IsBusy = false;
                    }
                    break;
                case "CheckWaferPresence":
                    {
                        IsBusy = false;
                    }
                    break;
                case "WaferSize":
                    IsBusy = false;
                    if (_setSizeArm == RobotArmEnum.Lower || _setSizeArm == RobotArmEnum.Blade1)
                        WaferManager.Instance.UpdateWaferSize(RobotModuleName, 0, _setSize);
                    if (_setSizeArm == RobotArmEnum.Upper || _setSizeArm == RobotArmEnum.Upper)
                        WaferManager.Instance.UpdateWaferSize(RobotModuleName, 1, _setSize);
                    if (_setSizeArm == RobotArmEnum.Both)
                    {
                        WaferManager.Instance.UpdateWaferSize(RobotModuleName, 0, _setSize);
                        WaferManager.Instance.UpdateWaferSize(RobotModuleName, 1, _setSize);
                    }
                    Size = _setSize;
                    break;
                case "CheckWaferIsPresence":
                    {
                       
                        if (!ParseComplete)
                        {
                            if (DateTime.Now - _dtActionStart < TimeSpan.FromSeconds(30))
                            {
                                //OnError("CheckWaferIsPresence timeout");
                                return false;
                            }
                            else
                            {
                                ParseComplete = false;
                                OnError("CheckWaferIsPresence timeout");
                                return true;
                            }
                        }
                        else
                        {
                            ParseComplete = false;
                            IsBusy = false;
                        }
                    }
                    break;
            }



            return true;
        }
        private void ExecuteHandler(HandlerBase handler)
        {

            _connection.Execute(handler);

        }
        private string Checksum(byte[] bytes)
        {
            int sum = 0;
            foreach (byte code in bytes)
            {
                sum += code;
            }
            string hex = String.Format("{0:X2}", sum % 256);
            return hex;
        }

        protected override bool fStartSetParameters(object[] param)
        {
            try
            {
                string strParameter;
                string setcommand = param[0].ToString();
                _setParameter = setcommand;
                switch (setcommand)
                {
                    case "RobotSpeed":   // SSPD Set the motion speed
                        int speedlevel = Convert.ToInt32(param[2]);
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new RorzeRobot751SetHandler(this, "SSPD", $"(0,{speedlevel})"));
                        }
                        if (SC.ContainsItem($"{_scRoot}.{Name}.SpeedLevel"))
                        {
                            SC.SetItemValue($"{_scRoot}.{Name}.SpeedLevel", Convert.ToInt32(speedlevel));
                            SpeedLevelSetting = SC.GetValue<int>($"{_scRoot}.{Name}.SpeedLevel");
                        }



                        break;
                    case "TransferSpeedLevel":    //SSLV Select the transfer speed level
                        string sslvlevel = param[1].ToString();

                        int nSpeed = 100;
                        if (sslvlevel == "1") nSpeed = 100;
                        if (sslvlevel == "2") nSpeed = 50;
                        if (sslvlevel == "3") nSpeed = 10;

                        if (!"123".Contains(sslvlevel))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + sslvlevel);
                            return false;
                        }
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new RorzeRobot751SetHandler(this, "SSPD", $"(0,{nSpeed})"));
                        }
                        if (SC.ContainsItem($"{_scRoot}.{Name}.SpeedLevel"))
                        {
                            SC.SetItemValue($"{_scRoot}.{Name}.SpeedLevel", Convert.ToInt32(sslvlevel));
                            SpeedLevelSetting = SC.GetValue<int>($"{_scRoot}.{Name}.SpeedLevel");
                        }
                        break;

                    case "WaferSize":
                        _setSizeArm = (RobotArmEnum)param[1];
                        _setSize = (WaferSize)param[2];
                        break;
                }
            }
            catch (Exception)
            {
                string reason = "";
                if (param != null)
                {
                    foreach (var para in param)
                    {
                        reason += para.ToString() + ",";
                    }
                }
                EV.PostAlarmLog(Name, "Set command parameter invalid:" + reason);
                return false;
            }
            return true;
        }
        private string _setParameter;
        private RobotArmEnum _setSizeArm;
        private WaferSize _setSize;

        protected override bool fMonitorSetParamter(object[] param)
        {
            IsBusy = false;
            switch (_setParameter)
            {
                case "WaferSize":
                    if (_setSizeArm == RobotArmEnum.Lower || _setSizeArm == RobotArmEnum.Blade1)
                        WaferManager.Instance.UpdateWaferSize(RobotModuleName, 0, _setSize);
                    if (_setSizeArm == RobotArmEnum.Upper || _setSizeArm == RobotArmEnum.Upper)
                        WaferManager.Instance.UpdateWaferSize(RobotModuleName, 1, _setSize);
                    if (_setSizeArm == RobotArmEnum.Both)
                    {
                        WaferManager.Instance.UpdateWaferSize(RobotModuleName, 0, _setSize);
                        WaferManager.Instance.UpdateWaferSize(RobotModuleName, 1, _setSize);
                    }
                    Size = _setSize;
                    break;
            }



            return true;
        }
        protected override bool fStartTransferWafer(object[] param)
        {
            return false;
        }

        protected override bool fStartUnGrip(object[] param)
        {
            lock (_locker)
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                switch(arm)
                {
                    case RobotArmEnum.Lower:
                        _lstHandlers.AddLast(new RorzeRobot751MotionHandler(this, "UCLM", "(2)"));
                        break;
                    case RobotArmEnum.Upper:
                        _lstHandlers.AddLast(new RorzeRobot751MotionHandler(this, "UCLM", "(1)"));
                        break;
                    case RobotArmEnum.Both:
                        _lstHandlers.AddLast(new RorzeRobot751MotionHandler(this, "UCLM", "(3)"));
                        break;

                }
            }
            return true;
        }

        protected override bool fStartGrip(object[] param)
        {
            lock (_locker)
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                switch (arm)
                {
                    case RobotArmEnum.Lower:
                        _lstHandlers.AddLast(new RorzeRobot751MotionHandler(this, "CLMP", "(2)"));
                        break;
                    case RobotArmEnum.Upper:
                        _lstHandlers.AddLast(new RorzeRobot751MotionHandler(this, "CLMP", "(1)"));
                        break;
                    case RobotArmEnum.Both:
                        _lstHandlers.AddLast(new RorzeRobot751MotionHandler(this, "CLMP", "(3)"));
                        break;

                }
            }
            return true;
        }
        protected override bool fStartInit(object[] param)
        {
            SpeedLevelSetting = SC.GetValue<int>($"{_scRoot}.{Name}.SpeedLevel");
            lock (_locker)
            {
                if (_doRobotHold != null)
                {
                    _doRobotHold.SetTrigger(true, out _);
                    Thread.Sleep(100);
                }
                _lstHandlers.AddLast(new RorzeRobot751SetHandler(this, "EVNT", "(0,1)"));
                _lstHandlers.AddLast(new RorzeRobot751MotionHandler(this, "INIT"));
                _lstHandlers.AddLast(new RorzeRobot751SetHandler(this, "MODE","(1,0)"));

                _lstHandlers.AddLast(new RorzeRobot751MotionHandler(this, "ORGN","(0,0)"));
                _lstHandlers.AddLast(new RorzeRobot751SetHandler(this, "SSPD", $"(0,{SpeedLevelSetting})"));

            }
            return true;
        }
        protected override bool fStartHome(object[] param)
        {
            SpeedLevelSetting = SC.GetValue<int>($"{_scRoot}.{Name}.SpeedLevel");
            lock (_locker)
            {
                if (_doRobotHold != null)
                {
                    _doRobotHold.SetTrigger(true, out _);
                    Thread.Sleep(100);
                }
                //_lstHandlers.AddLast(new SR100RobotMotionHandler(this, "CSOL", "F,1,0"));
                _lstHandlers.AddLast(new RorzeRobot751SetHandler(this, "EVNT", "(0,1)"));
                _lstHandlers.AddLast(new RorzeRobot751SetHandler(this, "INIT"));
                _lstHandlers.AddLast(new RorzeRobot751SetHandler(this, "MODE", "(1,0)"));

                _lstHandlers.AddLast(new RorzeRobot751MotionHandler(this, "ORGN", "(0,0)"));
                _lstHandlers.AddLast(new RorzeRobot751SetHandler(this, "SSPD", $"(0,{SpeedLevelSetting})"));
            }
            return true;
        }

        protected override bool fStartGoTo(object[] param)
        {
            try
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                if (ModuleHelper.IsLoadPort(module))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
                    if (lp != null)
                        lp.NoteTransferStart();
                }
                BladeTarget = module;
                Blade1Target = module;
                Blade2Target = module;
                
                int slot = (int)param[2] + 1;

                int TrsSt = GetStationsName(module);

                int nArm = 0;
                if (arm == RobotArmEnum.Lower)
                    nArm = 2;
                if (arm == RobotArmEnum.Upper)
                    nArm = 1;
                if (arm == RobotArmEnum.Both)
                    nArm = 3;
                if (nArm == 0)
                    return false;
    
                RobotPostionEnum postype = (RobotPostionEnum)param[3];
             
                string strCmd = string.Empty;
                int Id = 1;
               
                if (postype == RobotPostionEnum.PickExtend)
                {
                    Id = 1;
                    strCmd = "EXTD";
                    MoveInfo = new RobotMoveInfo()
                    {
                        ArmTarget = arm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                        BladeTarget = (arm == RobotArmEnum.Lower ? RobotArm.ArmA.ToString() : RobotArm.ArmB.ToString()) + "." + module.ToString(),
                    };
                }
                if (postype == RobotPostionEnum.PickRetracted)
                {
                    Id = 1;
                    strCmd = "HOME";
                    
                }  
                if (postype == RobotPostionEnum.PlaceExtend)
                {
                    Id = 3;
                    strCmd = "EXTD";
                    MoveInfo = new RobotMoveInfo()
                    {
                        ArmTarget = arm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                        BladeTarget = (arm == RobotArmEnum.Lower ? RobotArm.ArmA.ToString() : RobotArm.ArmB.ToString()) + "." + module.ToString(),
                    };
                }
                if (postype == RobotPostionEnum.PlaceRetract)
                {
                    Id = 1;
                    strCmd = "HOME";
                }
                if (postype == RobotPostionEnum.PickReady)
                {
                    Id = 1;
                    strCmd = "HOME";

                }
                if (postype == RobotPostionEnum.PlaceReady)
                {
                    Id = 2;
                    strCmd = "HOME";
                }
                string strpara = $"{Id},{nArm},{TrsSt},{slot}";
               
                //if (postype == RobotPostionEnum.PickReady)
                //{
                //    strCmd = "MGT1";    
                //    var Speed1 = (int)param[4];
                //    var ZDistince = (int)param[5];
                //    strpara = $"{nArm},{TrsSt},{slot},{Speed1},{ZDistince}";
                //}
                //if (postype == RobotPostionEnum.PlaceReady)
                //{
                //    strCmd = "MPT1";
                //    var falg1 = (int)param[4];
                //    var falg2 = (int)param[5];
                //    var Speed1 = (int)param[6];
                //    var Speed2 = (int)param[7];
                //    var ZDistince = (int)param[8];
                //    strpara = $"{nArm},{TrsSt},{slot},{falg1},{falg2},{Speed1},{Speed2},{ZDistince}";
                //}
                lock (_locker)
                {
                    lock (_locker)
                    {
                        if (_lstHandlers.Count == 0 && !_connection.IsBusy)
                        {
                            ExecuteHandler(new RorzeRobot751MotionHandler(this, strCmd, $"({strpara})"));
                        }
                        else
                        {
                            _lstHandlers.AddLast(new RorzeRobot751MotionHandler(this, strCmd, $"({strpara})"));
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        protected override bool fGoToComplete(object[] param)
        {
            try
            {
                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                ModuleName sourcemodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());
                int SourceslotIndex = (int)CurrentParamter[2];
                RobotPostionEnum postype = (RobotPostionEnum)CurrentParamter[3];
                switch (postype)
                {
                    //case RobotPostionEnum.PickExtend:
                    case RobotPostionEnum.PickRetracted:
                        BladeTarget = ModuleName.System;
                        Blade1Target = ModuleName.System;
                        Blade2Target = ModuleName.System;
                        MoveInfo = new RobotMoveInfo()
                        {
                            ArmTarget = arm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                            BladeTarget = (arm == RobotArmEnum.Lower ? RobotArm.ArmA.ToString() : RobotArm.ArmB.ToString()) + "." + ModuleName.System,
                        };
                        if (arm == RobotArmEnum.Lower)
                        {
                            WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                            UpdateThicknessType(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                            //if (isSimulatorMode || GetWaferState(arm) == RobotArmWaferStateEnum.Present)
                            //    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                            //else
                            //    OnError("Wafer detect error");
                        }
                        if (arm == RobotArmEnum.Upper)
                        {
                            WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                            UpdateThicknessType(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                            //if (isSimulatorMode || GetWaferState(arm) == RobotArmWaferStateEnum.Present)
                            //    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                            //else
                            //    OnError("Wafer detect error");
                        }
                        if (arm == RobotArmEnum.Both)
                        {
                            WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                            UpdateThicknessType(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                            WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                            UpdateThicknessType(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                            //if (isSimulatorMode || GetWaferState(arm) == RobotArmWaferStateEnum.Present)
                            //{
                            //    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                            //    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                            //}
                            //else
                            //    OnError("Wafer detect error");
                        }
                        break;
                    //case RobotPostionEnum.PlaceExtend:
                    case RobotPostionEnum.PlaceRetract:
                        BladeTarget = ModuleName.System;
                        Blade1Target = ModuleName.System;
                        Blade2Target = ModuleName.System;
                        MoveInfo = new RobotMoveInfo()
                        {
                            ArmTarget = arm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                            BladeTarget = (arm == RobotArmEnum.Lower ? RobotArm.ArmA.ToString() : RobotArm.ArmB.ToString()) + "." + ModuleName.System,
                        };
                        if (arm == RobotArmEnum.Lower)
                        {
                            WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, SourceslotIndex);
                            UpdateThicknessType(RobotModuleName, 0, sourcemodule, SourceslotIndex);
                            //if (isSimulatorMode || GetWaferState(arm) == RobotArmWaferStateEnum.Absent)
                            //    WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, SourceslotIndex);
                            //else
                            //    OnError("Wafer detect error");
                        }
                        if (arm == RobotArmEnum.Upper)
                        {
                            WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, SourceslotIndex);
                            UpdateThicknessType(RobotModuleName, 1, sourcemodule, SourceslotIndex);
                            //if (isSimulatorMode || GetWaferState(arm) == RobotArmWaferStateEnum.Absent)
                            //    WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, SourceslotIndex);
                            //else
                            //    OnError("Wafer detect error");
                        }
                        if (arm == RobotArmEnum.Both)
                        {
                            WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, SourceslotIndex);
                            UpdateThicknessType(RobotModuleName, 0, sourcemodule, SourceslotIndex);
                            WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, SourceslotIndex);
                            UpdateThicknessType(RobotModuleName, 1, sourcemodule, SourceslotIndex);
                            //if (isSimulatorMode || GetWaferState(arm) == RobotArmWaferStateEnum.Absent)
                            //{
                            //    WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, SourceslotIndex);
                            //    WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, SourceslotIndex);
                            //}
                            //else
                            //    OnError("Wafer detect error");
                        }
                        break;
                        case RobotPostionEnum.PickReady:
                        case RobotPostionEnum.PlaceReady:
                        {
                            BladeTarget = ModuleName.System;
                            Blade1Target = ModuleName.System;
                            Blade2Target = ModuleName.System;
                            if (arm == RobotArmEnum.Lower)
                            {
                                MoveInfo = new RobotMoveInfo()
                                {
                                    ArmTarget = RobotArm.ArmA,
                                    BladeTarget = RobotArm.ArmA+"."+ ModuleName.System.ToString(),
                                };
                            }
                            if (arm == RobotArmEnum.Upper)
                            {
                                MoveInfo = new RobotMoveInfo()
                                {   
                                    ArmTarget = RobotArm.ArmB,
                                    BladeTarget = RobotArm.ArmB + "." + ModuleName.System.ToString(),
                                };
                            }
                        }
                        break;


                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return base.fGoToComplete(param);
        }
        protected override bool fStop(object[] param)
        {
            lock (_locker)
            {
                if (_doRobotHold != null)
                    _doRobotHold.SetTrigger(false, out _);
                _lstHandlers.Clear();
                _connection.ForceClear();
                //ExecuteHandler(new SR100RobotMotionHandler(this, "CSTP", "E"));
            }
            return true; ;
        }

        protected override bool fStartMove(object[] param)
        {
            try
            {
                string strCmd = param[0].ToString();
                string strpara = string.Empty;
                for (int i = 1; i < param.Length; i++)
                {
                    if (i == 1)
                        strpara += param[i].ToString();
                    else
                        strpara += "," + param[i].ToString();
                }
                lock (_locker)
                {
                    //_lstHandlers.AddLast(new SR100RobotMotionHandler(this, strCmd, strpara));
                    //_lstHandlers.AddLast(new SR100RobotReadHandler(this, "RPOS", "F"));
                }
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        private int GetStationsName(ModuleName module)
        {
            try
            {
                if (ModuleHelper.IsLoadPort(module))
                {
                    var infopadindex = SC.GetStringValue($"CarrierInfo.{module}ThicknessType") =="THICK"? "0" :"1";
                    //int infopadindex = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString()).InfoPadCarrierIndex;
                    return SC.GetValue<int>($"CarrierInfo.{module}Station{infopadindex}");
                }
                int nvalue = SC.GetValue<int>($"CarrierInfo.{module}Station");
                return nvalue;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return 0;
            }
        }
        private bool UpdateThicknessType(ModuleName moduleform , int moduleformslot,ModuleName moduleto,int moduletoslot)
        {
            try
            {lock(this)
                { 
                if (!ModuleHelper.IsLoadPort(moduleto))
                {
                    string type = null;
                    if (ModuleHelper.IsRobot(moduleform))
                    {
                        if (moduleformslot == 0)
                            type = SC.GetStringValue($"CarrierInfo.LowerThicknessType");
                        else type = SC.GetStringValue($"CarrierInfo.UpperThicknessType");

                        SC.SetItemValueFromString($"CarrierInfo.{moduleto}ThicknessType",type);
                    }
                    else
                    {
                        type = SC.GetStringValue($"CarrierInfo.{moduleform}ThicknessType");
                        if (ModuleHelper.IsRobot(moduleto))
                        {
                            if (moduletoslot == 0)
                                SC.SetItemValueFromString($"CarrierInfo.LowerThicknessType", type);
                            else SC.SetItemValueFromString($"CarrierInfo.UpperThicknessType", type);
                        }
                        else SC.SetItemValueFromString($"CarrierInfo.{moduleto}ThicknessType", type);
                    }
                }
                if (!ModuleHelper.IsLoadPort(moduleform))
                {
                    if (ModuleHelper.IsRobot(moduleform))
                    {
                        if (moduleformslot == 0)
                            SC.SetItemValueFromString($"CarrierInfo.LowerThicknessType", "NONE");
                        else SC.SetItemValueFromString($"CarrierInfo.UpperThicknessType", "NONE");
                    }
                    else SC.SetItemValueFromString($"CarrierInfo.{moduleform}ThicknessType", "NONE");
                }
                }
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        private int GetSlotsNumber(ModuleName module)
        {
            try
            {
                if (ModuleHelper.IsLoadPort(module))
                {
                    return DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString()).ValidSlotsNumber;

                }
                return SC.GetValue<int>($"CarrierInfo.{module}SlotsNumber");
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return -1;
            }
        }
        private bool _isNeedMappignData
        {
            get
            {
                if (SC.ContainsItem($"Robot.{RobotModuleName}.NeedReadMapData"))
                    return SC.GetValue<bool>($"Robot.{RobotModuleName}.NeedReadMapData");
                return true;
            }
        }
        protected override bool fStartMapWafer(object[] param)
        {
            try
            {
                //RobotArmEnum pickarm = (RobotArmEnum)param[0];
                ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[0].ToString());
                BladeTarget = module;
                Blade1Target = module;
                Blade2Target = module;
                int slotsNumber = GetSlotsNumber(module);
                if (slotsNumber == -1)
                {
                    EV.PostAlarmLog("Robot", "Invalid mapping paramter slots number");
                    return false;
                }
                //int slot = 25;// (int)param[2];
                string strpara = $"({GetStationsName(module)},0,0)";
                lock (_locker)
                {
                    CurrentInteractiveModule = module;
                    _lstHandlers.AddLast(new RorzeRobot751MotionHandler(this, "WMAP", strpara));
                    _lstHandlers.AddLast(new RorzeRobot751ReadHandler(this, "GMAP"));
                }
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }


        protected override bool fStartSwapWafer(object[] param)
        {
            try
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                BladeTarget = module;
                Blade1Target = module;
                Blade2Target = module;
                MoveInfo = new RobotMoveInfo()
                {
                    ArmTarget = arm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = (arm == RobotArmEnum.Lower ? RobotArm.ArmA.ToString() : RobotArm.ArmB.ToString()) + "." + module,
                };
                if (ModuleHelper.IsLoadPort(module))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
                    if (lp != null)
                        lp.NoteTransferStart();
                }

                int slot = (int)param[2] + 1;
              
                int TrsSt = GetStationsName(module);
                

                int nArm = 0;
                if (arm == RobotArmEnum.Lower)
                    nArm = 2;
                if (arm == RobotArmEnum.Upper)
                    nArm = 1;
                if (nArm == 0) 
                    return false;


                string strpara = $"({nArm},{TrsSt},{slot})";
                
                lock (_locker)
                {
                    if (_lstHandlers.Count == 0 && !_connection.IsBusy)
                    {
                        ExecuteHandler(new RorzeRobot751MotionHandler(this, "EXCH", strpara));
                    }
                    else
                    {
                        _lstHandlers.AddLast(new RorzeRobot751MotionHandler(this, "EXCH", strpara));
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        protected override bool fSwapComplete(object[] param)
        {
            RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
            ModuleName sourcemodule;
            if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
            int Sourceslotindex;
            if (!int.TryParse(CurrentParamter[2].ToString(), out Sourceslotindex)) return false;
            //int delayCount = 0;
            BladeTarget = ModuleName.System;
            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;
            MoveInfo = new RobotMoveInfo()
            {
                ArmTarget = arm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = (arm == RobotArmEnum.Lower ? RobotArm.ArmA.ToString() : RobotArm.ArmB.ToString()) + "." + ModuleName.System,
            };
            if (arm == RobotArmEnum.Lower)
            {
                //WaferManager.Instance.WaferMoved(sourcemodule, Sourceslotindex, RobotModuleName, 0);
                //WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);

                //while (!isSimulatorMode && !(GetWaferState(RobotArmEnum.Lower) == RobotArmWaferStateEnum.Present
                //        && GetWaferState(RobotArmEnum.Upper) == RobotArmWaferStateEnum.Absent))
                //{
                //    delayCount++;
                //    Thread.Sleep(50);
                //    LOG.Write($"{RobotModuleName} delay {delayCount} time to detect wafer");
                //    if (delayCount > 100)
                //    {
                //        OnError("Wafer detect error");
                //        return true;
                //    }
                //}
                WaferManager.Instance.WaferMoved(sourcemodule, Sourceslotindex, RobotModuleName, 0);
                UpdateThicknessType(sourcemodule, Sourceslotindex, RobotModuleName, 0);
                WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);
                UpdateThicknessType(RobotModuleName, 1, sourcemodule, Sourceslotindex);

            }
            if (arm == RobotArmEnum.Upper)
            {
                //WaferManager.Instance.WaferMoved(sourcemodule, Sourceslotindex, RobotModuleName, 1);
                //WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                //delayCount = 0;
                //while (!isSimulatorMode && !(GetWaferState(RobotArmEnum.Upper) == RobotArmWaferStateEnum.Present &&
                //    GetWaferState(RobotArmEnum.Lower) == RobotArmWaferStateEnum.Absent))
                //{
                //    delayCount++;
                //    Thread.Sleep(50);
                //    LOG.Write($"{RobotModuleName} delay {delayCount} time to detect wafer");
                //    if (delayCount > 100)
                //    {
                //        OnError("Wafer detect error");
                //        return true;
                //    }
                //}
                WaferManager.Instance.WaferMoved(sourcemodule, Sourceslotindex, RobotModuleName, 1);
                UpdateThicknessType(sourcemodule, Sourceslotindex, RobotModuleName, 1);
                WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                UpdateThicknessType(RobotModuleName, 0, sourcemodule, Sourceslotindex);

            }
            return base.fSwapComplete(param);
        }


        protected override bool fStartPlaceWafer(object[] param)
        {
            try
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                if (ModuleHelper.IsLoadPort(module))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
                    if (lp != null)
                        lp.NoteTransferStart();
                }
                BladeTarget = module;
                Blade1Target = module;
                Blade2Target = module;
                MoveInfo = new RobotMoveInfo()
                {
                    ArmTarget = arm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = (arm == RobotArmEnum.Lower ? RobotArm.ArmA.ToString() : RobotArm.ArmB.ToString()) + "." + module,
                };
                int slot = (int)param[2] + 1;
               
                int TrsSt = GetStationsName(module);
                

                int nArm = 0;
                if (arm == RobotArmEnum.Lower)
                    nArm = 2;
                if (arm == RobotArmEnum.Upper)
                    nArm = 1;
                if (arm == RobotArmEnum.Both)
                    nArm = 3;
                if (nArm == 0)
                    return false;


                string strpara = $"{nArm},{TrsSt},{slot}";
               
                lock (_locker)
                {
                    if (_lstHandlers.Count == 0 && !_connection.IsBusy)
                    {
                        ExecuteHandler(new RorzeRobot751MotionHandler(this, "UNLD", $"({strpara})"));
                    }
                    else
                    {
                        _lstHandlers.AddLast(new RorzeRobot751MotionHandler(this, "UNLD", $"({strpara})"));
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        protected override bool fPlaceComplete(object[] param)
        {
            RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
            ModuleName sourcemodule;
            if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
            int Sourceslotindex;
            if (!int.TryParse(CurrentParamter[2].ToString(), out Sourceslotindex)) return false;
            BladeTarget = ModuleName.System;
            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;
            MoveInfo = new RobotMoveInfo()
            {
                ArmTarget = arm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = (arm == RobotArmEnum.Lower ? RobotArm.ArmA.ToString() : RobotArm.ArmB.ToString()) + "." + ModuleName.System,
            };
            if (arm == RobotArmEnum.Lower)
            {
                WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                UpdateThicknessType(RobotModuleName, 0, sourcemodule, Sourceslotindex);
            }
            if (arm == RobotArmEnum.Upper)
            {      
                WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);
                UpdateThicknessType(RobotModuleName, 1, sourcemodule, Sourceslotindex);
            }
            if (arm == RobotArmEnum.Both)
            {
                
                WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                UpdateThicknessType(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex + 1);
                UpdateThicknessType(RobotModuleName, 1, sourcemodule, Sourceslotindex+1);

            }


            return true;
        }

        protected override bool fStartPickWafer(object[] param)
        {
            try
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                if (ModuleHelper.IsLoadPort(module))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
                    if (lp != null)
                        lp.NoteTransferStart();
                }
                BladeTarget = module;
                Blade1Target = module;
                Blade2Target = module;
                MoveInfo = new RobotMoveInfo()
                {
                    ArmTarget = arm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = (arm == RobotArmEnum.Lower ? RobotArm.ArmA.ToString() : RobotArm.ArmB.ToString())+ "." + module,
                };
                int slot = (int)param[2] + 1;

                string TrsSt = GetStationsName(module).ToString();
                if (string.IsNullOrEmpty(TrsSt))
                {
                    EV.PostAlarmLog("Robot", "Invalid Parameter.");
                    return false;
                }

                int nArm = 0;
                if (arm == RobotArmEnum.Lower)
                    nArm = 2;
                if (arm == RobotArmEnum.Upper)
                    nArm = 1;
                if (arm == RobotArmEnum.Both)
                    nArm = 3;
                if (nArm == 0)
                    return false;


                string strpara = $"{nArm},{TrsSt},{slot}";

                lock (_locker)
                {
                    if (_lstHandlers.Count == 0 && !_connection.IsBusy)
                    {
                        ExecuteHandler(new RorzeRobot751MotionHandler(this, "LOAD",$"({strpara})"));
                    }
                    else
                    {
                        _lstHandlers.AddLast(new RorzeRobot751MotionHandler(this, "LOAD", $"({strpara})"));
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        protected override bool fPickComplete(object[] param)
        {
            RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
            ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());
            ModuleName sourcemodule;
            if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
            int SourceslotIndex;
            if (!int.TryParse(CurrentParamter[2].ToString(), out SourceslotIndex)) return false;
            BladeTarget = ModuleName.System;
            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;
            MoveInfo = new RobotMoveInfo()
            {
                ArmTarget = arm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget =(arm == RobotArmEnum.Lower ? RobotArm.ArmA.ToString() : RobotArm.ArmB.ToString()) + "." + ModuleName.System,
            };
            if (arm == RobotArmEnum.Lower)
            {                
                WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                UpdateThicknessType(sourcemodule, SourceslotIndex, RobotModuleName, 0);
            }
            if (arm == RobotArmEnum.Upper)
            {
                WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                UpdateThicknessType(sourcemodule, SourceslotIndex, RobotModuleName, 1);

            }
            if (arm == RobotArmEnum.Both)
            {
                
                WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                UpdateThicknessType(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex + 1, RobotModuleName, 1);
                UpdateThicknessType(sourcemodule, SourceslotIndex+1, RobotModuleName, 1);

            }
            return true;
        }


        protected override bool fResetToReady(object[] param)
        {
            if (_doRobotHold != null)
                _doRobotHold.SetTrigger(true, out _);
            return true;
        }

        protected override bool fReset(object[] param)
        {
            IsBusy = true;
            if (!_connection.IsConnected)
            {
                _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
                _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
                _connection = new RorzeRobot751Connection(this, _address);
                _connection.EnableLog(_enableLog);
                _connection.Connect();
            }
            lock (_locker)
            {
                if (_doRobotHold != null)
                    _doRobotHold.SetTrigger(true, out _);
                _lstHandlers.Clear();
                _connection.ForceClear();
                _lstHandlers.AddLast(new RorzeRobot751SetHandler(this, "RSTA","(1)"));
                _lstHandlers.AddLast(new RorzeRobot751SetHandler(this, "EVNT","(0,1)"));
                _lstHandlers.AddLast(new RorzeRobot751ReadHandler(this, "STAT"));
            }
            return true;
        }

       
        protected override bool fMonitorReset(object[] param)
        {
            if (_lstHandlers.Count > 0 )
                return false;
            if(IsCmdProcessing) return false;
            IsBusy = false;
            return true;
        }


        protected override bool fError(object[] param)
        {
            return true;
        }

        protected override bool fStartExtendForPick(object[] param)
        {
            return false;
        }

        protected override bool fStartExtendForPlace(object[] param)
        {
            return false;
        }

        protected override bool fStartRetractFromPick(object[] param)
        {
            return false;
        }

        protected override bool fStartRetractFromPlace(object[] param)
        {
            return false;
        }
       
        public override RobotArmWaferStateEnum GetWaferState(RobotArmEnum arm)
        {
            if (arm == RobotArmEnum.Lower)
            {
                if (_diRobotBlade1WaferOn != null)
                {
                    if (_diRobotBlade1WaferOn.Value) return RobotArmWaferStateEnum.Absent;
                    else return RobotArmWaferStateEnum.Present;
                }

                return IsWaferPresenceOnBlade1 ? RobotArmWaferStateEnum.Present : RobotArmWaferStateEnum.Absent;
            }
            if (arm == RobotArmEnum.Upper)
            {
                if (_diRobotBlade2WaferOn != null)
                {
                    if (_diRobotBlade2WaferOn.Value) return RobotArmWaferStateEnum.Absent;
                    else return RobotArmWaferStateEnum.Present;
                }
                return IsWaferPresenceOnBlade2 ? RobotArmWaferStateEnum.Present : RobotArmWaferStateEnum.Absent;
            }
            if (arm == RobotArmEnum.Both)
            {
                if (_diRobotBlade1WaferOn != null && _diRobotBlade2WaferOn != null)
                {
                    if (_diRobotBlade2WaferOn.Value && _diRobotBlade1WaferOn.Value)
                        return RobotArmWaferStateEnum.Absent;
                    else if (!_diRobotBlade2WaferOn.Value && !_diRobotBlade1WaferOn.Value)
                        return RobotArmWaferStateEnum.Present;
                    else return RobotArmWaferStateEnum.Unknown;
                }

                if (IsWaferPresenceOnBlade1 && IsWaferPresenceOnBlade2)
                {
                    return RobotArmWaferStateEnum.Present;
                }
                if ((!IsWaferPresenceOnBlade1) && !IsWaferPresenceOnBlade2)
                {
                    return RobotArmWaferStateEnum.Absent;
                }

            }
            return RobotArmWaferStateEnum.Unknown;
        }

        public void NoteError(string errortext)
        {
            OnError(errortext);
        }
        

        public override bool OnActionDone(object[] param)
        {
            //BladeTarget = ModuleName.System;
            //Blade1Target = ModuleName.System;
            //Blade2Target = ModuleName.System;
            IsBusy = false;
            ModuleName sourcemodule;
            if (CurrentParamter != null && CurrentParamter.Length > 2 && Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule))
            {
                if (ModuleHelper.IsLoadPort(sourcemodule))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(sourcemodule.ToString());
                    if (lp != null)
                        lp.NoteTransferStop();
                }
            }
            if (_lstHandlers.Count == 0)
            {
                IsBusy = false;
                return base.OnActionDone(param);
            }

            return true;
        }

        public override void Terminate()
        {
            _thread.Stop();
            if (!SC.ContainsItem($"{_scRoot}.{Name}.CloseConnectionOnShutDown") || SC.GetValue<bool>($"{_scRoot}.{Name}.CloseConnectionOnShutDown"))
            {
                LOG.Write("Close connection for" + RobotModuleName.ToString());
                _connection.Disconnect();
            }
            base.Terminate();
        }

    }

    public enum RROpModeEnum
    {
        Initializing,
        Remote,
        Maitanance,
        Recovery,
    }

    public enum RROpStatusEnum
    {
        Stop,
        Moving,
        TempStop,
    }


   
}
