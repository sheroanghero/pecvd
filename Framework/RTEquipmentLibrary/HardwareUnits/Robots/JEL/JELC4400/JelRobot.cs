using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using Newtonsoft.Json;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using System.Threading;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using System.Text.RegularExpressions;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using MECF.Framework.Common.CommonData;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Flipper.FlipperBase;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.JEL
{
    public class JelRobot : RobotBaseDevice, IConnection
    {
        private int _bodyNumber;
        private string _robotModel;    //T-00902H
        public int BodyNumber { get => _bodyNumber; }
        private string _address = "";
        public string Address { get => _address; }
        private bool isSimulatorMode;
        private string _scRoot;       
        private PeriodicJob _thread;
        protected JelRobotConnection _connection;
        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private R_TRIG _trigActionDone = new R_TRIG();
        
        protected LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();
        protected DateTime _dtActionStart;
        protected object _locker = new object();

        private bool _enableLog;

        public string PortName;
        public bool IsConnected { get; }
        public bool Connect()

        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }
        public JelRobot(string module, string name, string scRoot, string robotModel = "", int armCount = 2) : base(module, name, "Robot", armCount)
        {
            _robotModel = robotModel;
            isSimulatorMode = SC.ContainsItem("System.IsSimulatorMode") ? SC.GetValue<bool>("System.IsSimulatorMode") : false;
            _scRoot = scRoot;

            ResetPropertiesAndResponses();
            RegisterSpecialData();

            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            _bodyNumber = SC.GetValue<int>($"{_scRoot}.{Name}.BodyNumber");
            PortName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");

            _connection = new JelRobotConnection(PortName);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {

                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }
            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
        }


        public JelRobot(string module, string name, string scRoot, string portName,string robotModel = "",int armCount=2) : base(module, name,"Robot",armCount)
        {
            _robotModel = robotModel;
            isSimulatorMode = SC.ContainsItem("System.IsSimulatorMode") ? SC.GetValue<bool>("System.IsSimulatorMode") : false;
            _scRoot = scRoot;

            ResetPropertiesAndResponses();
            RegisterSpecialData();
           
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            _bodyNumber = SC.GetValue<int>($"{_scRoot}.{Name}.BodyNumber");
            PortName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");
           
            _connection = new JelRobotConnection(PortName);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
               
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }
            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);   
        }

        #region Properties
        public JelAxisStatus RightarmAndThetaarmStatus { get; private set; }
        public JelAxisStatus LeftarmAndZaxisStatus { get; private set; }
        public float RightArmPostion { get; private set; }
        public float ThetaAxisPostion { get; private set; }
        public float LeftArmPostion { get; private set; }
        public float ZAxisPostion { get; private set; }
        public Int32[] AData { get; private set; } = new Int32[1000];
        public JelParameterData[] RobotParameters { get; private set; } = new JelParameterData[2000];
        public string ReadBankNumber { get; private set; } = "";
        public int ReadCassetNumber { get; private set; }
        public int ReadSlotNumber { get; private set; }
        public bool IsRightArmPressureSensorON { get; private set; }
        public bool IsLeftArmPressureSensorON { get; private set; }
        public bool IsEchoBack { get; private set; }
        public JelCommandStatus CurrentCompoundCommandStatus { get; private set; }   

        public string StopPostionOfCompoundCommand { get; private set; }
        public string[] MainRoutines { get; private set; } = new string[320];
        public string[] SubRoutines { get; private set; } = new string[16];

        public string CurrentReadRoutine { get; private set; }

        public int HighSpeed { get; private set; }
        public int LowSpeed { get; private set; }
        public int DomainOfAccDec { get; private set; }
        public int AccDecSpeed { get; private set; }
        public int MagnificationOfFrequency { get; private set; }
        public int PulsePostionOfManualSlowDown { get; private set; }
        public int CurrentReadSpeedData
        {
            get;set;
        }
        public Int32 CurrentReadAData { get; private set; }
        public Int32 ReadPosRightArmPostion { get; private set; }
        public Int32 ReadPosThetaAxisPostion { get; private set; }
        public Int32 ReadPosLeftArmPostion { get; private set; }
        public Int32 ReadPosZAxisPostion { get; private set; }

        public float ReadParameterMin { get; private set; }
        public float ReadParameterMax { get; private set; }
        public float ReadParameterValue { get; private set; }      

        //Mapping Data
        public float MappingFirstSlotPosition { get; private set; }
        public float MappingTopSlotPostion { get; private set; }
        public int MappingSlotsNumber { get; private set; }
        public float MappingMinDetectWidth { get; private set; }
        public float MappingMaxDetectWidth { get; private set; }
        public float MappingGateWidth { get; private set; }
        public float MappingStopPostion { get; private set; }
        public float MappingSpeed { get; private set; }
        public bool IsMappingSensorON { get; private set; }
        public string MappingWaferResult { get; private set; } = string.Empty;
        public string MappingWidthResult { get; private set; } = string.Empty;
        protected bool ResetBeforeCmd
        {
            get
            {
                if (SC.ContainsItem($"Robot.{RobotModuleName}.ResetBeforeCmd") && SC.GetValue<bool>($"Robot.{RobotModuleName}.ResetBeforeCmd"))
                    return true;
                return false;
            }
        }

        protected bool GotoSafePositionBeforeCmd
        {
            get
            {
                if (SC.ContainsItem($"Robot.{RobotModuleName}.GotoSafePositionBeforeCmd"))
                    return SC.GetValue<bool>($"Robot.{RobotModuleName}.GotoSafePositionBeforeCmd");
                   
                return true;
            }
        }
        #endregion

        #region ParseHandler
        public void ParseData(string command,string parameter,string data)
        {
            try
            {
                string datavalue = data.Replace($"${BodyNumber}", "").Replace("\r", "");
                if (command == "SPA")
                {
                    CurrentReadSpeedData = Convert.ToInt32(datavalue);
                }


                if (command == "") ParseRobotStatus(datavalue);     // Read Robot Status
                if (command == "6" || command == "6M") 
                    ParseRobotPostion(parameter, datavalue); //Read Postion Data
                if (command == "A")     // Read A data
                {
                    int _index;
                    if (!int.TryParse(parameter.Replace("D", ""), out _index)) return;
                    AData[_index] = Convert.ToInt32(datavalue);
                    CurrentReadAData = Convert.ToInt32(datavalue);
                }
                if (command == "BC")
                {
                    ReadBankNumber = datavalue;
                }
                if (command == "WCP")
                {
                    string[] strvalues = datavalue.Split(',');                    
                    ReadCassetNumber = Convert.ToInt32(strvalues[0]);
                    ReadSlotNumber = Convert.ToInt32(strvalues[1]);
                }
                if (command == "CS")
                {
                    if (parameter == "1")
                        IsRightArmPressureSensorON = datavalue == "1";
                    if (parameter == "2")
                        IsLeftArmPressureSensorON = datavalue == "1";
                }
                if (command == "DTD")
                {
                    int _index;
                    if (!int.TryParse(parameter, out _index)) return;
                    string[] paradata = datavalue.Split(',');
                    //if (paradata.Length < 3) return;
                    RobotParameters[_index].Value = paradata[0];
                    RobotParameters[_index].MinValue = paradata[1];
                    RobotParameters[_index].MaxValue = paradata[2];
                }
                if (command == "EE")
                {
                    IsEchoBack = datavalue == "E";
                }
                if (command == "G")
                {
                    if (datavalue == "0") CurrentCompoundCommandStatus = JelCommandStatus.NormalEnd;
                    if (datavalue == "1") CurrentCompoundCommandStatus = JelCommandStatus.InExecution;
                    if (datavalue == "P") CurrentCompoundCommandStatus = JelCommandStatus.InPause;
                    if (datavalue == "E") CurrentCompoundCommandStatus = JelCommandStatus.InError;
                }
                if (command == "GER")
                {
                    LOG.Write("Compaund command stop postion:" + datavalue);
                }
                if (command == "IR")
                {
                    int _index;
                    if (!int.TryParse(parameter, out _index)) return;
                    if (_index < 0 || _index > 319) return;
                    MainRoutines[_index] = datavalue;
                    CurrentReadRoutine = datavalue;
                }
                if (command == "IRS")
                {
                    int _index = Convert.ToInt32(parameter, 16);
                    if (_index < 0 || _index > 15) return;
                    SubRoutines[_index] = datavalue;
                    CurrentReadRoutine = datavalue;
                }
                if (command == "O")
                {
                    //CurrentReadSpeedData = Convert.ToInt32(datavalue);
                    if (parameter == "H")
                        HighSpeed = Convert.ToInt32(datavalue);
                    if (parameter == "L")
                        LowSpeed = Convert.ToInt32(datavalue);
                    if (parameter == "S")
                        DomainOfAccDec = Convert.ToInt32(datavalue);
                    if (parameter == "G")
                        AccDecSpeed = Convert.ToInt32(datavalue);
                    if (parameter == "X")
                        MagnificationOfFrequency = Convert.ToInt32(datavalue);
                    if (parameter == "D")
                        PulsePostionOfManualSlowDown = Convert.ToInt32(datavalue);
                }
                if (command == "PSD")
                {
                    string[] stradata = datavalue.Split(',');    
                    ReadPosRightArmPostion = Convert.ToInt32(stradata[0].ToString());
                    ReadPosThetaAxisPostion = Convert.ToInt32(stradata[1].ToString());
                    ReadPosLeftArmPostion = Convert.ToInt32(stradata[2].ToString());
                    ReadPosZAxisPostion = Convert.ToInt32(stradata[3].ToString()); 
                }
                if (command == "DTD")
                {
                    string[] stradata = datavalue.Split(',');                   
                    ReadParameterValue = Convert.ToSingle(stradata[0].ToString());
                    ReadParameterMin = Convert.ToSingle(stradata[1].ToString());
                    ReadParameterMax = Convert.ToSingle(stradata[2].ToString());
                }
                if (command == "WLO") // Obtaining the postion data of mapping 1st slot detected
                   MappingFirstSlotPosition = Convert.ToSingle(datavalue);
                if(command == "WHI")
                    MappingTopSlotPostion = Convert.ToSingle(datavalue);
                if(command == "WFC")
                    MappingSlotsNumber = Convert.ToInt32(datavalue);
                if(command == "WWN")
                    MappingMinDetectWidth = Convert.ToSingle(datavalue);
                if(command == "WWM")
                    MappingMaxDetectWidth = Convert.ToSingle(datavalue);
                if(command == "WWG")
                    MappingGateWidth = Convert.ToSingle(datavalue);
                if(command == "WEND")
                    MappingStopPostion = Convert.ToSingle(datavalue);
                if(command == "WSP")
                    MappingSpeed = Convert.ToSingle(datavalue);
                if(command == "WFK")
                {
                    MappingWaferResult = datavalue;
                    NotifySlotMapResult(CurrentInteractModule, datavalue.Replace(",", "").Replace("E", "?"));
                }
                if (command == "WFW")
                    MappingWidthResult = datavalue;


            }
            catch(Exception ex)
            {
                LOG.Write($"Parse {command}.{parameter??parameter} data {data} error:" + ex.ToString());
                OnError($"Parse data error:{command}.{parameter ?? parameter} data {data}");
            }
            
        }

        public void HandlerWaferCheckResult(bool checkError, RobotArmEnum arm, bool iswaferOn)
        {
            if(checkError)
            {
                OnError("CheckWaferError");
                return;
            }

            if (iswaferOn && WaferManager.Instance.CheckNoWafer(RobotModuleName, (int)arm))
            {
                EV.PostAlarmLog("Robot", $"There's unknown wafer on {RobotModuleName} {arm}");
                WaferManager.Instance.CreateWafer(RobotModuleName, (int)arm, WaferStatus.Normal);
            }

            if (!iswaferOn && WaferManager.Instance.CheckHasWafer(RobotModuleName, (int)arm))
            {
                EV.PostAlarmLog("Robot", $"There's a wafer on {RobotModuleName} {arm} but robot didn't detect");

            }
        }

        private void ParseRobotStatus(string data)
        {
            if (data.Length < 2) return;
            RightarmAndThetaarmStatus = (JelAxisStatus)Convert.ToInt16(data.ToArray()[0].ToString());
            LeftarmAndZaxisStatus = (JelAxisStatus)Convert.ToInt16(data.ToArray()[1].ToString());
        }
        private void ParseRobotPostion(string axis,string data)
        {
            float _floatvalue;
            if (!float.TryParse(data, out _floatvalue)) return;
            if (axis == "1")
            {
                RightArmPostion = _floatvalue;
                PositionAxis1 = _floatvalue;
            }
            if (axis == "2")
            {
                ThetaAxisPostion = _floatvalue;
                PositionAxis2 = _floatvalue;
            }
            if (axis == "3")
            {
                LeftArmPostion = _floatvalue;
                PositionAxis3 = _floatvalue;
            }
            if (axis == "4")
            {
                ZAxisPostion = _floatvalue;
                PositionAxis4 = _floatvalue;
            }
        }

        #endregion

        public virtual bool OnTimer()
        {
            try
            {
                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstMonitorHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        _connection.SetPortAddress(SC.GetStringValue($"{_scRoot}.{Name}.PortName"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                         }
                    }
                    return true;
                }

                HandlerBase handler = null;
                DateTime dtstart = DateTime.Now;

                if (_lstMonitorHandler.Count > 0)
                {
                    if (!_connection.IsBusy)
                    {
                        lock (_locker)
                        {
                            handler = _lstMonitorHandler.First.Value;
                            _connection.Execute(handler);
                            _lstMonitorHandler.RemoveFirst();
                        }
                    }
                    else
                    {
                        _connection.MonitorTimeout();
                        _trigCommunicationError.CLK = _connection.IsCommunicationError;
                        if (_trigCommunicationError.Q)
                        {
                            _lstMonitorHandler.Clear();
                            OnError($"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
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

        private void RegisterSpecialData()
        {
            DATA.Subscribe($"{Module}.{Name}.CurrentReadRoutine", () => CurrentReadRoutine); 
            DATA.Subscribe($"{Module}.{Name}.CurrentReadSpeedData", () => CurrentReadSpeedData);
            DATA.Subscribe($"{Module}.{Name}.CurrentReadAData", () => CurrentReadAData);
            DATA.Subscribe($"{Module}.{Name}.IsLeftArmPressureSensorON", () => IsLeftArmPressureSensorON);
            DATA.Subscribe($"{Module}.{Name}.IsRightArmPressureSensorON", () => IsRightArmPressureSensorON);
            DATA.Subscribe($"{Module}.{Name}.LeftarmAndZaxisStatus", () => LeftarmAndZaxisStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.RightarmAndThetaarmStatus", () => RightarmAndThetaarmStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.CurrentCompoundCommandStatus", () => CurrentCompoundCommandStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadCassetNumber", () => ReadCassetNumber.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadSlotNumber", () => ReadSlotNumber.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadBankNumber", () => ReadBankNumber.ToString());



            DATA.Subscribe($"{Module}.{Name}.ReadPosLeftArmPostion", () => ReadPosLeftArmPostion);
            DATA.Subscribe($"{Module}.{Name}.ReadPosRightArmPostion", () => ReadPosRightArmPostion.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadPosZAxisPostion", () => ReadPosZAxisPostion.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadPosThetaAxisPostion", () => ReadPosThetaAxisPostion.ToString());

            DATA.Subscribe($"{Module}.{Name}.ReadParameterMax", () => ReadParameterMax.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadParameterMin", () => ReadParameterMin.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadParameterValue", () => ReadParameterValue.ToString());

            DATA.Subscribe($"{Module}.{Name}.MappingFirstSlotPosition", () => MappingFirstSlotPosition.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingGateWidth", () => MappingGateWidth.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingMaxDetectWidth", () => MappingMaxDetectWidth.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingMinDetectWidth", () => MappingMinDetectWidth.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingSlotsNumber", () => MappingSlotsNumber.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingSpeed", () => MappingSpeed.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingStopPostion", () => MappingStopPostion.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingTopSlotPostion", () => MappingTopSlotPostion.ToString());
            DATA.Subscribe($"{Module}.{Name}.IsMappingSensorON", () => IsMappingSensorON.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingWaferResult", () => MappingWaferResult.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingWidthResult", () => MappingWidthResult.ToString());

            DATA.Subscribe($"{Name}.RobotSpeed", () => CurrentReadSpeedData);




        }

        private void ResetPropertiesAndResponses()
        {
            
        }
        protected override bool fReset(object[] param)
        {
            _dtActionStart = DateTime.Now;
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            _connection.EnableLog(_enableLog);

            _trigRetryConnect.RST = true;
            
            _lstMonitorHandler.Clear();
            _connection.ForceClear();

            lock (_locker)
            {
                _lstMonitorHandler.AddLast(new JelRobotRawCommandHandler(this, $"${BodyNumber}RD\r"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, ""));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "1"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "2"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "3"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "4"));
            }
            return true;
        }



        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("ResetTimeout");
                return true;
            }
            if (RightarmAndThetaarmStatus == JelAxisStatus.NormalEnd
                && LeftarmAndZaxisStatus == JelAxisStatus.NormalEnd
                && _lstMonitorHandler.Count == 0
                && !_connection.IsBusy)
            {
                IsBusy = false;
                return true;
            }
            if (_lstMonitorHandler.Count == 0 &&  !_connection.IsBusy)
                _connection.Execute(new JelRobotReadHandler(this, ""));
            return false;
        }
        protected override bool fStartMove(object[] param)
        {
            _dtActionStart = DateTime.Now;
            if (param.Length < 4) return false;
            int axis,intdirect;
            if (!int.TryParse(param[0].ToString(), out axis)) return false;
            if (axis > 4 || axis < 0) return false;
            if (!int.TryParse(param[1].ToString(), out intdirect)) return false;
            if (intdirect > 1 || intdirect < 0) return false;
            RobotMoveDirectionEnum direction = (RobotMoveDirectionEnum)Convert.ToInt16(param[1].ToString());
            uint instance;
            if (!uint.TryParse(param[2].ToString(), out instance)) return false;
            if (instance > 8388607) instance = 8388607;
            string movebasetype = Regex.Replace(param[3].ToString().Replace("（", "(").Replace("）", ")"), @"\([^\(]*\)", "");
            if (movebasetype != "0" && movebasetype != "1") return false;
            string commandstr = "";
            if (movebasetype == "0") commandstr = "3";
            if(movebasetype == "1")
            {
                if (direction == RobotMoveDirectionEnum.Fwd) commandstr = "4";
                else commandstr = "5";
            }
            if (string.IsNullOrEmpty(commandstr)) return false;
            string speedtype = string.Empty;
            if (param.Length > 4) 
                speedtype = Regex.Replace(param[4].ToString().Replace("（", "(").Replace("）", ")"), @"\([^\(]*\)", "");
            
            if (speedtype != "" && speedtype != "M" && speedtype != "L") return false;

            lock (_locker)
            {
                _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "2", instance.ToString()));
                _lstMonitorHandler.AddLast(new JelRobotMoveHandler(this, commandstr + "M", axis.ToString() + speedtype));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, ""));
            }
            return true;
        }
        protected override bool fMonitorMoving(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("MoveTimeout");
                return true;
            }
            if (RightarmAndThetaarmStatus == JelAxisStatus.NormalEnd
                && (LeftarmAndZaxisStatus == JelAxisStatus.NormalEnd)
                && _lstMonitorHandler.Count == 0
                && !_connection.IsBusy)
            {
                IsBusy = false;
                return true;
            }
            if (_lstMonitorHandler.Count == 0 &&!_connection.IsBusy)
                _connection.Execute(new JelRobotReadHandler(this, ""));
            return false;
        }

        protected override bool fStartInit(object[] param)
        {
            _dtActionStart = DateTime.Now;
            int compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.InitCmdNO");
            int Robotspeed = SC.GetValue<int>($"Robot.{Name}.RobotSpeed");
            if (Robotspeed < 0)
                Robotspeed = 0;
            if (Robotspeed > 9)
                Robotspeed = 9;

            if(GetForbidHomeArea() != null && GetForbidHomeArea().Count >1)
            {
                lock (_locker)
                {
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "1"));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "2"));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "3"));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "4"));
                }
                int waitCount = 0;

                while(true)
                {
                    waitCount++;
                    if(waitCount >100)
                    {
                        OnError("InitErrorWhenGetPostionData");
                        return false;
                    }
                    if(_lstMonitorHandler.Count ==0 && !_connection.IsBusy)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }

                foreach(var forbidData in GetForbidHomeArea())
                {
                    if(RightArmPostion >= forbidData[0].Item1 && RightArmPostion<= forbidData[0].Item2
                        && ThetaAxisPostion >= forbidData[1].Item1 && ThetaAxisPostion <= forbidData[1].Item2
                        && ZAxisPostion >= forbidData[2].Item1 && ZAxisPostion <= forbidData[2].Item2)
                    {
                        OnError("RobotIsInForbidHomeArea");
                        return false;
                    }

                    if (ReadPosLeftArmPostion >= forbidData[0].Item1 && ReadPosLeftArmPostion <= forbidData[0].Item2
                        && ThetaAxisPostion >= forbidData[1].Item1 && ThetaAxisPostion <= forbidData[1].Item2
                        && ZAxisPostion >= forbidData[2].Item1 && ZAxisPostion <= forbidData[2].Item2)
                    {
                        OnError("RobotIsInForbidHomeArea");
                        return false;
                    }
                }
            }


            


            lock (_locker)
            {
                _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, compaundcmdNO.ToString()));
                _lstMonitorHandler.AddLast(new JelRobotMoveHandler(this, ""));
                _lstMonitorHandler.AddLast(new JelRobotMoveHandler(this, "G"));
                _lstMonitorHandler.AddLast(new JelRobotMoveHandler(this, "CS1"));
                _lstMonitorHandler.AddLast(new JelRobotMoveHandler(this, "BC"));     // Read bank number
                _lstMonitorHandler.AddLast(new JelRobotMoveHandler(this, "WCP"));    // Read Cassette Number
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "1"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "2"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "3"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "4"));
                _lstMonitorHandler.AddLast(new JelRobotMoveHandler(this, "SP", Robotspeed.ToString()));


                if(SC.ContainsItem($"Robot.{Name}.UpperCheckWaferCmdNO"))
                {
                    compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.UpperCheckWaferCmdNO");
                    _lstMonitorHandler.AddLast(new JelRobotCheckWaferCommandHandler(this, compaundcmdNO.ToString(), RobotArmEnum.Upper));
                }

                if (SC.ContainsItem($"Robot.{Name}.LowerCheckWaferCmdNO"))
                {
                    compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.LowerCheckWaferCmdNO");
                    _lstMonitorHandler.AddLast(new JelRobotCheckWaferCommandHandler(this, compaundcmdNO.ToString(), RobotArmEnum.Lower));
                }


                //_lstMonitorHandler.AddLast(new JelRobotMoveHandler(this, "SPA"));


            }
            return true;
        }

        private List<Tuple<float,float>[]> GetForbidHomeArea()
        {
            List<Tuple<float, float>[]> retValue = new List<Tuple<float, float>[]>();

            int i = 0;
            while(SC.ContainsItem($"Robot.{RobotModuleName}.ForbidHomeArea{++i}"))
            {                
                string[] strData = SC.GetStringValue($"Robot.{RobotModuleName}.ForbidHomeArea{i}").Split('|');
                if (strData.Length < 3) continue;

                List<Tuple<float, float>> sValue = new List<Tuple<float, float>>();
                bool datavalid = true;
                foreach(var sdata in strData)
                {
                    string[] strDatas1 = sdata.Split(',');
                    if (strDatas1.Length < 2)
                    {
                        datavalid = false;
                        break;
                    }

                    if(!float.TryParse(strDatas1[0],out float lowlimit))
                    {
                        datavalid = false;
                        break;
                    }
                    if (!float.TryParse(strDatas1[1], out float highlimit))
                    {
                        datavalid = false;
                        break;
                    }
                    sValue.Add(new Tuple<float, float>(lowlimit, highlimit));
                }
                if (!datavalid) continue;
                retValue.Add(sValue.ToArray());
            }

            return retValue;
        }
        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("InitTimeout");
                return true;
            }


            if (!_connection.IsBusy && _lstMonitorHandler.Count ==0)
            {
                if (CurrentCompoundCommandStatus == JelCommandStatus.InError)
                {
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "GER"));
                    OnError("Init Compaund Comand execution failed");
                }

                Blade1Target = ModuleName.System;
                Blade2Target = ModuleName.System;

                CmdTarget = ModuleName.System;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };
                IsBusy = false;
                EV.PostInfoLog("Robot", $"{RobotModuleName} init complete");
                return true;
                
            }
            return false;            
        }
        protected override bool fStartReadData(object[] param)
        {
            _dtActionStart = DateTime.Now;
            if(param.Length <1) return false;
            string readcommand = param[0].ToString();
            switch(readcommand)
            {
                case "CurrentStatus":
                    lock (_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, ""));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "CS", "1"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "CS", "2"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "G"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "BC"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WCP"));
                    }

                    break;
                case "CurrentPositionData":
                    lock(_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "1"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "2"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "3"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "6M", "4"));
                    }
                    break;
                case "CompaundCommand":
                    if (param.Length < 3) return false;
                    string routineselction = param[1].ToString();                    
                    if(routineselction == "MainRoutine")
                    {
                        int routineno;
                        if (!int.TryParse(param[2].ToString(), out routineno)) return false;
                        if (routineno > 319 || routineno < 1) return false;
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "IR",routineno.ToString().PadLeft(3,'0')));
                    }
                    if (routineselction == "SubRoutine")
                    {
                        char routineno;
                        if (!char.TryParse(param[2].ToString(), out routineno)) return false;
                        if (routineno > 'F' || routineno < 1) return false;
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "IRS", routineno.ToString()));
                    }
                    break;
                case "RobotSpeed":
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "SPA"));
                    break;
                case "AData":
                    if (param.Length < 2) return false;
                    int datano;
                    if (!int.TryParse(param[1].ToString(), out datano)) return false;
                    if (datano < 0 || datano > 999) return false;
                    lock(_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "A", datano.ToString("D3")+"D"));
                    }
                    break;
                case "EPData":
                    if (param.Length < 2) return false;
                    if (param[1].ToString() == "AData")
                    {
                        lock (_locker)
                        {
                            _lstMonitorHandler.AddLast(new JelRobotRawCommandHandler (this, $"${BodyNumber}AL"));
                        }
                    }
                    break;
                case "PositionData":
                    if (param.Length < 2) return false;
                    int posNO;
                    if (!int.TryParse(param[1].ToString(), out posNO)) return false;
                    if (posNO > 999 || posNO < 0) return false;
                    lock (_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "PSD", posNO.ToString("D3")));
                    }
                    break;
                case "RobotParameter":
                    if (param.Length < 2) return false;
                    uint parano;
                    if(!uint.TryParse(param[1].ToString(), out parano)) return false;
                    lock(_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "DTD", parano.ToString()));
                    }
                    break;
                case "MappingData":
                    lock(_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WLO"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WHI"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WFC"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WWN"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WWM"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WWG"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WEND"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WSP"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WFK"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WFW"));
                    }
                    break;

            }
            return true;


        }
        protected override bool fMonitorReadData(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("ReadDataTimeout");
                return true;
            }
            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
            {
                IsBusy = false;
                return true;
            }
            return false;
        }
        protected override bool fStartSetParameters(object[] param)
        {
            try 
            { 
            
            string setcommand = param[0].ToString();
                switch (setcommand)
                {
                    case "CompaundCommand":
                        if (param.Length < 4) return false;
                        string routineselction = param[1].ToString();
                        if (routineselction == "MainRoutine")
                        {
                            int routineno;
                            if (!int.TryParse(param[2].ToString(), out routineno)) return false;
                            if (routineno > 319 || routineno < 1) return false;
                            _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "I", routineno.ToString().PadLeft(3, '0')));
                            _lstMonitorHandler.AddLast(new JelRobotRawCommandHandler(this, param[3].ToString()));

                        }
                        if (routineselction == "SubRoutine")
                        {
                            char routineno;
                            if (!char.TryParse(param[2].ToString(), out routineno)) return false;
                            if (routineno > 'F' || routineno < 1) return false;
                            _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "IS", routineno.ToString()));
                            _lstMonitorHandler.AddLast(new JelRobotRawCommandHandler(this, param[3].ToString()));

                        }
                        break;
                    case "RobotSpeed":
                        Int32 speedvalue;
                        if (!Int32.TryParse(param[2].ToString(), out speedvalue)) return false;

                        if (SC.ContainsItem($"Robot.{RobotModuleName}.RobotSpeed"))
                        {
                            SC.SetItemValue($"Robot.{RobotModuleName}.RobotSpeed", speedvalue);
                        }


                        if (speedvalue < 0)
                            speedvalue = 0;
                        if (speedvalue > 9)
                            speedvalue = 9;
                        _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "SPA", speedvalue.ToString()));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "SPA"));

                        break;


                    case "AData":
                        if (param.Length < 3) return false;
                        Int32 datano, datavalue;
                        if (!Int32.TryParse(param[1].ToString(), out datano)) return false;
                        if (!Int32.TryParse(param[2].ToString(), out datavalue)) return false;
                        if (datano > 999 || datano < 0) return false;
                        if (datavalue > 8388607 || datavalue < -8388608) return false;
                        lock (_locker)
                        {
                            _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "A", datano.ToString("D3") + datavalue.ToString()));
                        }
                        break;
                    case "EPData":
                        if (param.Length < 2) return false;
                        if (param[1].ToString() == "AData")
                        {
                            lock (_locker)
                            {
                                _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "AW"));
                            }
                        }
                        break;
                    case "PositionData":
                        if (param.Length < 6) return false;
                        Int32 posno, rightarmpos, thetapos, leftarmpos, zpos;
                        if (!Int32.TryParse(param[1].ToString(), out posno)) return false;
                        if (!Int32.TryParse(param[2].ToString(), out rightarmpos)) return false;
                        if (!Int32.TryParse(param[3].ToString(), out thetapos)) return false;
                        if (!Int32.TryParse(param[4].ToString(), out leftarmpos)) return false;
                        if (!Int32.TryParse(param[5].ToString(), out zpos)) return false;
                        if (posno > 999 || posno < 0) return false;
                        lock (_locker)
                        {
                            _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "PS",
                                $"{posno.ToString("D3")}{rightarmpos},{thetapos},{leftarmpos},{zpos}"));
                        }
                        return true;
                    case "CurrentPositionData":
                        Int32 currentpos = Int32.Parse(param[1].ToString());
                        if (currentpos > 999 || currentpos < 0) return false;
                        lock (_locker)
                        {
                            _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "PS",
                                $"{currentpos.ToString("D3")}"));
                        }
                        break;

                    case "RobotParameter":
                        if (param.Length < 3) return false;
                        Int32 parano, paravalue;
                        if (!Int32.TryParse(param[1].ToString(), out parano)) return false;
                        if (!Int32.TryParse(param[2].ToString(), out paravalue)) return false;
                        lock (_locker)
                        {
                            _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "DTSVAL", $"{parano},{paravalue}"));
                            _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "DW"));
                        }
                        break;

                    case "MappingData":
                        lock(_locker)
                        {
                            
                            if (!string.IsNullOrEmpty(param[1].ToString()))
                            {
                                float tempvalue = float.Parse(param[1].ToString());
                                string strtempvalue;
                                if (tempvalue <0)
                                {
                                    strtempvalue = "-" +(-tempvalue).ToString().PadLeft(7, '0');
                                }
                                else
                                    strtempvalue = tempvalue.ToString().PadLeft(8, '0');
                                _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WLO",strtempvalue));
                            }

                            if (!string.IsNullOrEmpty(param[2].ToString()))
                            {
                                float tempvalue = float.Parse(param[2].ToString());
                                string strtempvalue;
                                if (tempvalue < 0)
                                {
                                    strtempvalue = "-" + (-tempvalue).ToString().PadLeft(7, '0');
                                }
                                else
                                    strtempvalue = tempvalue.ToString().PadLeft(8, '0');
                                _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WHI",strtempvalue));
                            }

                            if (!string.IsNullOrEmpty(param[3].ToString()))
                                _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WFC",
                                    (Convert.ToInt16(param[3].ToString()).ToString("D2"))));

                            if (!string.IsNullOrEmpty(param[4].ToString()))
                            {
                                float tempvalue = float.Parse(param[4].ToString());
                                string strtempvalue;
                                if (tempvalue < 0)
                                {
                                    strtempvalue = "-" + (-tempvalue).ToString().PadLeft(7, '0');
                                }
                                else
                                    strtempvalue = tempvalue.ToString().PadLeft(8, '0');
                                _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WWN", strtempvalue));
                            }

                            if (!string.IsNullOrEmpty(param[5].ToString()))
                            {
                                float tempvalue = float.Parse(param[5].ToString());
                                string strtempvalue;
                                if (tempvalue < 0)
                                {
                                    strtempvalue = "-" + (-tempvalue).ToString().PadLeft(7, '0');
                                }
                                else
                                    strtempvalue = tempvalue.ToString().PadLeft(8, '0');
                                _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WWM", strtempvalue));
                            }

                            if (!string.IsNullOrEmpty(param[6].ToString()))
                            {
                                float tempvalue = float.Parse(param[6].ToString());
                                string strtempvalue;
                                if (tempvalue < 0)
                                {
                                    strtempvalue = "-" + (-tempvalue).ToString().PadLeft(7, '0');
                                }
                                else
                                    strtempvalue = tempvalue.ToString().PadLeft(8, '0');
                                _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WWG", strtempvalue));
                            }

                            if (!string.IsNullOrEmpty(param[7].ToString()))
                            {
                                float tempvalue = float.Parse(param[7].ToString());
                                string strtempvalue;
                                if (tempvalue < 0)
                                {
                                    strtempvalue = "-" + (-tempvalue).ToString().PadLeft(7, '0');
                                }
                                else
                                    strtempvalue = tempvalue.ToString().PadLeft(8, '0');
                                _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WEND", strtempvalue));
                            }

                            if (!string.IsNullOrEmpty(param[8].ToString()))
                            {
                                float tempvalue = float.Parse(param[8].ToString());
                                string strtempvalue;
                                if (tempvalue < 0)
                                {
                                    strtempvalue = "-" + (-tempvalue).ToString().PadLeft(7, '0');
                                }
                                else
                                    strtempvalue = tempvalue.ToString().PadLeft(8, '0');
                                _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WSP", strtempvalue));
                            }

                        }
                        break;
                    case "MappingCommand":
                        lock(_locker)
                        {
                            _lstMonitorHandler.AddLast(new JelRobotMoveHandler(this, "WFS"));
                            _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WFK"));
                            _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WFW"));
                        }
                        break;
                    case "BankNumber":
                        lock (_locker)
                        {
                            if (param.Length > 1 && !string.IsNullOrEmpty(param[1].ToString()))
                            {
                                int bankno = Convert.ToInt16(param[1].ToString());
                                if (bankno <= 15 && bankno >= 0)
                                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "BC", bankno.ToString("X")));
                            }
                            if (param.Length > 2 && !string.IsNullOrEmpty(param[2].ToString()))
                            {
                                int cassetteno = Convert.ToInt16(param[2].ToString());
                                if (cassetteno <=5 && cassetteno <= 1) 
                                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCP", cassetteno.ToString()));
                            }
                            if (param.Length > 3 && !string.IsNullOrEmpty(param[3].ToString()))
                            {
                                int slotno = Convert.ToInt16(param[3].ToString());
                                if (slotno <= 25 && slotno >= 1)
                                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCD", slotno.ToString()));
                            }
                            _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "BC"));
                            _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WCP"));
                        }
                        break;
                }
            }
            catch (Exception )
            {
                string reason = "";
                if(param!=null)
                {
                    foreach(var para in param)
                    {
                        reason += para.ToString() + ",";
                    }
                }
                EV.PostAlarmLog(Name, "Set command parameter valid:" + reason);
                return false;
            }
            return true;



        }
        protected override bool fMonitorSetParamter(object[] param)
        {
            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
            {
                IsBusy = false;
                return true;
            }

            return base.fMonitorSetParamter(param);
        }


        public override bool IsReady()
        {
            return RobotState == RobotStateEnum.Idle && !IsBusy;
        }

        protected override bool fClear(object[] param)
        {
            return true;
        }

        

        

        protected override bool fStartTransferWafer(object[] param)
        {
            return true;
        }

        protected override bool fStartUnGrip(object[] param)
        {
            if (param == null || param.Length < 1) return false;
            RobotArmEnum arm = (RobotArmEnum)((int)param[0]);
            lock (_locker)
            {
                if (arm == RobotArmEnum.Lower)
                {
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "DS", "20"));
                }
                if (arm == RobotArmEnum.Upper)
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "DS", "10"));
                if (arm == RobotArmEnum.Both)
                {
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "DS", "20"));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "DS", "10"));
                }
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "CS", "1"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "CS", "2"));
            }
            return true;

        }
        protected override bool fMonitorUnGrip(object[] param)
        {
            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
            {
                RobotArmEnum arm = (RobotArmEnum)((int)CurrentParamter[0]);
                if (arm == RobotArmEnum.Lower && IsLeftArmPressureSensorON)
                {
                    OnError("Grip wafer failed!");
                }
                if (arm == RobotArmEnum.Upper && IsRightArmPressureSensorON)
                {

                    OnError("Grip wafer failed!");
                }
                if (arm == RobotArmEnum.Both && ((IsRightArmPressureSensorON) || IsLeftArmPressureSensorON))
                    OnError("Grip wafer failed!");
                
                    IsBusy = false;
                    return true;
                
            }

            return false;
        }

        protected override bool fStartGrip(object[] param)
        {            
            if(param == null || param.Length <1) return false;

            RobotArmEnum arm = (RobotArmEnum)((int)param[0]);
            lock (_locker)
            {
                if (arm == RobotArmEnum.Lower)
                {
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "DS", "21"));
                }
                if(arm == RobotArmEnum.Upper)
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "DS", "11"));
                if(arm ==  RobotArmEnum.Both)
                {
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "DS", "21"));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "DS", "11"));
                }
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "CS", "1"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "CS", "2"));
            }
            _dtActionStart = DateTime.Now;

            return true;
        }
        protected override bool fMonitorGrip(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("GripTimeout");
                return true;
            }
            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
            {
                RobotArmEnum arm = (RobotArmEnum)((int)CurrentParamter[0]);
                if (arm == RobotArmEnum.Lower && !IsLeftArmPressureSensorON)
                {
                    OnError("Grip wafer failed!");
                }
                if (arm == RobotArmEnum.Upper && !IsRightArmPressureSensorON)
                {

                    OnError("Grip wafer failed!");
                }
                if(arm == RobotArmEnum.Both &&((!IsRightArmPressureSensorON) || !IsLeftArmPressureSensorON))
                    OnError("Grip wafer failed!");
                
                IsBusy = false;
                
                return true;
            }
                
            return false;
        }
        protected override bool fStop(object[] param)
        {
            _lstMonitorHandler.Clear();            
            _connection.ForceClear();
            _connection.Execute(new JelRobotSetHandler(this, "S"));
            return ReadStatus();
            
        }

      

        protected override bool fStartGoTo(object[] param)
        {
            try
            {
                _dtActionStart = DateTime.Now;
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                BladeTarget = tempmodule;
                Blade1Target = tempmodule;
                Blade2Target = tempmodule;
                int slot = (int)param[2] + 1;
                RobotPostionEnum postype = (RobotPostionEnum)param[3];

                var wz = WaferManager.Instance.GetWaferSize(RobotModuleName, (int)arm);               

                if (ModuleHelper.IsLoadPort(tempmodule))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(tempmodule.ToString());
                    if (lp != null) lp.NoteTransferStart();
                    
                }
                int bankno;
                int cassetteNO;
                if(!GetBankAndCassetteNumber(tempmodule,wz,out bankno,out cassetteNO))
                {
                    EV.PostAlarmLog("Robot", $"{RobotModuleName} can't find the bankno or cassette no for {tempmodule}");
                    return false;
                }
                    

                int compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.{arm}Goto{postype}CmdNO");
                int compaundcmdNOforSafe = SC.GetValue<int>($"Robot.{Name}.SafeCmdNO");


                lock (_locker)
                {
                    if (ResetBeforeCmd)
                        _lstMonitorHandler.AddLast(new JelRobotRawCommandHandler(this, $"RD"));
                    if (GotoSafePositionBeforeCmd && (ReadBankNumber != bankno.ToString("X") || cassetteNO != ReadCassetNumber))
                    {
                        _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, compaundcmdNOforSafe.ToString()));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "G"));
                    }
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "BC", bankno.ToString("X")));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCP", cassetteNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCD", slot.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "BC"));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WCP"));
                    _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, compaundcmdNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "G"));
                }

                Blade1Target = tempmodule;
                Blade2Target = tempmodule;


                CmdTarget = tempmodule;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Picking,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };





                return true;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                LOG.Write(ex);
                return false;
            }
        }

        protected override bool fMonitorGoTo(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Robot Goto timeout");
                return true;
            }
            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
            {
                if (CurrentCompoundCommandStatus == JelCommandStatus.NormalEnd)
                {
                    RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                    ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());
                                        
                    ModuleName sourcemodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());
                    int SourceslotIndex = (int)CurrentParamter[2];
                    RobotPostionEnum postype = (RobotPostionEnum)CurrentParamter[3];
                    bool isFromOriginal = (bool)CurrentParamter[8];
                    bool isJumpToNextMotion = (bool)CurrentParamter[9];

                    switch (postype)
                    {
                        case RobotPostionEnum.PickExtendUp:
                        case RobotPostionEnum.PickRetracted:
                            if (arm == RobotArmEnum.Lower)
                            {
                                WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                                //if (isSimulatorMode || GetWaferState(arm) == RobotArmWaferStateEnum.Present)
                                //    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                                //else
                                //    OnError("Wafer detect error");
                            }
                            if (arm == RobotArmEnum.Upper)
                            {
                                WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                                //if (isSimulatorMode || GetWaferState(arm) == RobotArmWaferStateEnum.Present)
                                //    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                                //else
                                //    OnError("Wafer detect error");
                            }
                            if (arm == RobotArmEnum.Both)
                            {
                                WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                                WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                                //if (isSimulatorMode || GetWaferState(arm) == RobotArmWaferStateEnum.Present)
                                //{
                                //    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                                //    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                                //}
                                //else
                                //    OnError("Wafer detect error");
                            }
                            break;
                        case RobotPostionEnum.PlaceExtendDown:
                        case RobotPostionEnum.PlaceRetract:
                            if (arm == RobotArmEnum.Lower)
                            {
                                WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, SourceslotIndex);
                                //if (isSimulatorMode || GetWaferState(arm) == RobotArmWaferStateEnum.Absent)
                                //    WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, SourceslotIndex);
                                //else
                                //    OnError("Wafer detect error");
                            }
                            if (arm == RobotArmEnum.Upper)
                            {
                                WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, SourceslotIndex);
                                //if (isSimulatorMode || GetWaferState(arm) == RobotArmWaferStateEnum.Absent)
                                //    WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, SourceslotIndex);
                                //else
                                //    OnError("Wafer detect error");
                            }
                            if (arm == RobotArmEnum.Both)
                            {
                                WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, SourceslotIndex);
                                WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, SourceslotIndex);
                                //if (isSimulatorMode || GetWaferState(arm) == RobotArmWaferStateEnum.Absent)
                                //{
                                //    WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, SourceslotIndex);
                                //    WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, SourceslotIndex);
                                //}
                                //else
                                //    OnError("Wafer detect error");
                            }
                            break;
                    }




















                    BladeTarget = ModuleName.System;
                    Blade1Target = ModuleName.System;
                    Blade2Target = ModuleName.System;

                    CmdTarget = ModuleName.System;
                    MoveInfo = new RobotMoveInfo()
                    {
                        Action = RobotAction.Moving,
                        ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                        BladeTarget = BuildBladeTarget(),
                    };
                   



                    if (ModuleHelper.IsLoadPort(tempmodule))
                    {
                        var lp = DEVICE.GetDevice<LoadPortBaseDevice>(tempmodule.ToString());
                        if (lp != null) lp.NoteTransferStop();

                    }



                    return true;

                }

                if (CurrentCompoundCommandStatus == JelCommandStatus.InError)
                {
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "GER"));
                    HandlerError("GoToFailed");
                }

            }
            return false;
        }

        protected override bool fStartMapWafer(object[] param)
        {
            try
            {
                _dtActionStart = DateTime.Now;
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), param[0].ToString());               

                if(ModuleHelper.IsLoadPort(tempmodule))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(tempmodule.ToString());
                    if (lp != null) lp.NoteTransferStart();                  
                }
           
                WaferSize wz = WaferManager.Instance.GetWaferSize(tempmodule, 0);
                int bankno;
                int cassetteNO;
                if (!GetBankAndCassetteNumber(tempmodule, wz, out bankno, out cassetteNO))
                {
                    EV.PostAlarmLog("Robot", $"{RobotModuleName} can't find the bankno or cassette no for {tempmodule}");
                    return false;
                }

                int compaundcmdNO = -1;
                if(SC.ContainsItem($"Robot.{Name}.MapCmdNO"))
                {
                    compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.MapCmdNO");
                }
                else if(SC.ContainsItem($"Robot.{Name}.{tempmodule}MapCmdNO"))
                {
                    compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.{tempmodule}MapCmdNO");
                }
                else
                {
                    EV.PostAlarmLog("Robot", $"{RobotModuleName} can't find the map command for {tempmodule}");
                    return false;
                }
                





                lock (_locker)
                {
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "BC", bankno.ToString("X")));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCP", cassetteNO.ToString()));                    
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "BC"));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WCP"));
               
                    _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, compaundcmdNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, ""));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WFK"));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WFW"));
                }


            }
            catch (Exception ex)
            {
                string reason = "";
                if (param != null)
                    foreach (var strpara in param)
                        reason += strpara.ToString();
                OnError($"{Name} Map command valid:" + reason);
                LOG.Write(ex);
                return false;
            }
            return true;
        }

        protected override bool fMonitorMap(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("MapTimeout");
                return true;
            }

            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
            {
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[0].ToString());

                if (ModuleHelper.IsLoadPort(tempmodule))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(tempmodule.ToString());
                    if (lp != null) lp.NoteTransferStop();
                }

                IsBusy = false;
                return true;
            }
            return false;
        }

        private bool IsSupportSwapCmd
        {
            get
            {
                if (SC.ContainsItem($"Robot.{Name}.IsSupportSwapCmd"))
                    return SC.GetValue<bool>($"Robot.{Name}.IsSupportSwapCmd");
                return true;
            }
        }
        protected override bool fStartSwapWafer(object[] param)
        {
            try
            {
                _dtActionStart = DateTime.Now;
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());

                int slotindex = int.Parse(param[2].ToString());
                JelRobotArm jelarm = (JelRobotArm)(int)arm;

                var wz = WaferManager.Instance.GetWaferSize(RobotModuleName, arm == RobotArmEnum.Both ? 0 : (int)arm);
                
                if (ModuleHelper.IsLoadPort(tempmodule))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(tempmodule.ToString());

                    if (lp != null)
                        lp.NoteTransferStart();
                       
                }
                int bankno;
                int cassetteNO;
                if (!GetBankAndCassetteNumber(tempmodule, wz, out bankno, out cassetteNO))
                {
                    EV.PostAlarmLog("Robot", $"{RobotModuleName} can't find the bankno or cassette no for {tempmodule}");
                    return false;
                }
                


                if(IsSupportSwapCmd)
                {
                    int compaundcmdNO = -1;
                    if (SC.ContainsItem($"Robot.{Name}.{jelarm}PickSwapCmdNO"))
                    {
                        compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.{jelarm}PickSwapCmdNO");
                    }
                    else if (SC.ContainsItem($"Robot.{Name}.{arm}PickSwapCmdNO"))
                    {
                        compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.{arm}PickSwapCmdNO");
                    }
                    else
                    {
                        EV.PostAlarmLog("Robot", $"Can't find the compaund command for swap");
                        OnError("CompaundCommandError");
                        return false;
                    }

                    lock (_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "BC", bankno.ToString("X")));
                        _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCP", cassetteNO.ToString()));
                        _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCD", (slotindex + 1).ToString()));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "BC"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WCP"));
                        _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, compaundcmdNO.ToString()));
                    }
                }               

                else
                {
                    int pickCmdNO = -1;
                    if (SC.ContainsItem($"Robot.{Name}.{arm}PickCmdNO"))
                    {
                        pickCmdNO = SC.GetValue<int>($"Robot.{Name}.{arm}PickCmdNO");
                    }
                    else
                    {
                        EV.PostAlarmLog("Robot", $"Can't find the compaund command for pick");
                        OnError("CompaundCommandError");
                        return false;
                    }

                    int placeCmdNO =-1;
                    RobotArmEnum placeArm = RobotArmEnum.Lower;
                    if (arm == RobotArmEnum.Lower)
                        placeArm = RobotArmEnum.Upper;
                    if (SC.ContainsItem($"Robot.{Name}.{placeArm}PlaceCmdNO"))
                    {
                        placeCmdNO = SC.GetValue<int>($"Robot.{Name}.{placeArm}PlaceCmdNO");
                    }
                    else
                    {
                        EV.PostAlarmLog("Robot", $"Can't find the compaund command for pick");
                        OnError("CompaundCommandError");
                        return false;
                    }
                    lock (_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "BC", bankno.ToString("X")));
                        _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCP", cassetteNO.ToString()));
                        _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCD", (slotindex + 1).ToString()));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "BC"));
                        _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WCP"));
                        _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, pickCmdNO.ToString()));
                        _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, placeCmdNO.ToString()));
                    }


                }

                Blade1Target = tempmodule;
                Blade2Target = tempmodule;



                CmdTarget = tempmodule;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };
                
                
            }
            catch (Exception ex)
            {
                string reason = "";
                if (param != null)
                    foreach (var strpara in param)
                        reason += strpara.ToString();
                OnError($"{Name} Swap command valid:" + reason);
                LOG.Write(ex);
                return false;
            }


            return true;
        }
        protected override bool fMonitorSwap(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("SwapTimeout");
                return true;
            }
            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
            {
                if (CurrentCompoundCommandStatus == JelCommandStatus.InError)
                {
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "GER"));
                    OnError("Compaund Comand execution failed");
                }
                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                    ModuleName sourcemodule;
                    if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
                    int Sourceslotindex;
                    if (!int.TryParse(CurrentParamter[2].ToString(), out Sourceslotindex)) return false;
                    if (arm == RobotArmEnum.Lower)
                    {
                        WaferManager.Instance.WaferMoved(sourcemodule, Sourceslotindex, RobotModuleName, 0);
                        WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);
                    }
                    if (arm == RobotArmEnum.Upper)
                    {
                        WaferManager.Instance.WaferMoved(sourcemodule, Sourceslotindex, RobotModuleName, 1);
                        WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                    }
                    if (arm == RobotArmEnum.Both)
                    {
                        
                    }
                Blade1Target =  ModuleName.System;
                Blade2Target =  ModuleName.System;



                CmdTarget =  ModuleName.System;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };


                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());

                if (ModuleHelper.IsLoadPort(tempmodule))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(tempmodule.ToString());
                    if (lp != null) lp.NoteTransferStop();
                }

                IsBusy = false;
                return true;
                    
                
                
            }

            return false;
        }

        protected override bool fStartPlaceWafer(object[] param)
        {
            try
            {
                _dtActionStart = DateTime.Now;
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());

                int slotindex = int.Parse(param[2].ToString());
                JelRobotArm jelarm = (JelRobotArm)(int)arm;

                var wz = WaferManager.Instance.GetWaferSize(RobotModuleName, arm == RobotArmEnum.Both ? 0 : (int)arm);
             

                if (ModuleHelper.IsLoadPort(tempmodule))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(tempmodule.ToString());
                    if (lp != null)
                        lp.NoteTransferStart();                    
                }
                int bankno;
                int cassetteNO;
                if (!GetBankAndCassetteNumber(tempmodule, wz, out bankno, out cassetteNO))
                {
                    EV.PostAlarmLog("Robot", $"{RobotModuleName} can't find the bankno or cassette no for {tempmodule}");
                    return false;
                }
                int compaundcmdNO = -1;

                if (SC.ContainsItem($"Robot.{Name}.{jelarm}PlaceCmdNO"))
                {
                    compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.{jelarm}PlaceCmdNO");
                }
                else if (SC.ContainsItem($"Robot.{Name}.{arm}PlaceCmdNO"))
                {
                    compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.{arm}PlaceCmdNO");
                }
                else
                {
                    EV.PostAlarmLog("Robot", $"Can't find the compaund command for place");
                    OnError("CompaundCommandError");
                    return false;
                }



                lock (_locker)
                {
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "BC", bankno.ToString("X")));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCP", cassetteNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCD", (slotindex+1).ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "BC"));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WCP"));
                    _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this, compaundcmdNO.ToString()));
                }

                Blade1Target = tempmodule;
                Blade2Target = tempmodule;



                CmdTarget = tempmodule;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };


            }
            catch (Exception ex)
            {
                string reason = "";
                if (param != null)
                    foreach (var strpara in param)
                        reason += strpara.ToString();
                OnError($"{Name} Pick command valid:" + reason);
                LOG.Write(ex);
                return false;
            }
            return true;
        }

        protected override bool fStartPickWafer(object[] param)
        {
            try
            {
                _dtActionStart = DateTime.Now;
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                
                int slotindex = int.Parse(param[2].ToString());                
                JelRobotArm jelarm = (JelRobotArm)(int)arm;

                var wz = WaferManager.Instance.GetWaferSize(tempmodule, slotindex);               

                if (ModuleHelper.IsLoadPort(tempmodule))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(tempmodule.ToString());

                    if (lp != null)
                        lp.NoteTransferStart();
                }

                int bankno;
                int cassetteNO;
                if (!GetBankAndCassetteNumber(tempmodule, wz, out bankno, out cassetteNO))
                {
                    EV.PostAlarmLog("Robot", $"{RobotModuleName} can't find the bankno or cassette no for {tempmodule}");
                    return false;
                }





                int compaundcmdNO = -1;
                
                if(SC.ContainsItem($"Robot.{Name}.{jelarm}PickCmdNO"))
                {
                    compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.{jelarm}PickCmdNO");
                }
                else if(SC.ContainsItem($"Robot.{Name}.{arm}PickCmdNO"))
                {
                    compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.{arm}PickCmdNO");
                }
                else
                {
                    EV.PostAlarmLog("Robot", $"Can't find the compaund command for pick");
                    OnError("CompaundCommandError");
                    return false;
                }



                lock (_locker)
                {
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "BC", bankno.ToString("X")));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCP", cassetteNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotSetHandler(this, "WCD", (slotindex+1).ToString()));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "BC"));
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "WCP"));
                    _lstMonitorHandler.AddLast(new JelRobotCompaundCommandHandler(this,compaundcmdNO.ToString()));
                }
                Blade1Target = tempmodule;
                Blade2Target = tempmodule;



                CmdTarget = tempmodule;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };

            }
            catch (Exception ex)
            {
                string reason =  "";
                if (param != null)
                {
                    foreach (var strpara in param)
                        reason += strpara.ToString();
                }
                LOG.Write(ex);
                OnError($"{Name} Pick command valid:" + reason);
                
                return false;
            }
            return true;
        }
        protected override bool fMonitorPlace(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("PlaceTimeout");
                return true;
            }
            if ( _lstMonitorHandler.Count == 0 && !_connection.IsBusy)
            {
                if (CurrentCompoundCommandStatus == JelCommandStatus.InError)
                {
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "GER"));
                    OnError("Compaund Comand execution failed");
                }

                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                    ModuleName sourcemodule;
                    if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
                    int Sourceslotindex;
                    if (!int.TryParse(CurrentParamter[2].ToString(), out Sourceslotindex)) return false;
                    if (arm == RobotArmEnum.Lower)
                        WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                    if (arm == RobotArmEnum.Upper)
                        WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);
                    if (arm == RobotArmEnum.Both)
                    {
                        WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                        WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);
                    }

                Blade1Target = ModuleName.System;
                Blade2Target = ModuleName.System;



                CmdTarget = ModuleName.System;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };

                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());

                if (ModuleHelper.IsLoadPort(tempmodule))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(tempmodule.ToString());
                    if (lp != null) lp.NoteTransferStop();
                }

                IsBusy = false;
                return true;
                    
                
                
               
            }
            return false;
        }
        protected override bool fMonitorPick(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("PickTimeout");
                return true;
            }
            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
            {
                if (CurrentCompoundCommandStatus == JelCommandStatus.InError)
                {
                    _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "GER"));
                    OnError("Compaund Comand execution failed");
                }

                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                    ModuleName sourcemodule;
                    if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
                    int SourceslotIndex;
                    if (!int.TryParse(CurrentParamter[2].ToString(), out SourceslotIndex)) return false;
                    if (arm == RobotArmEnum.Lower)
                         WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                    if (arm == RobotArmEnum.Upper)
                        WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                    if(arm == RobotArmEnum.Both)
                    {
                        WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                        WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                    }

                Blade1Target = ModuleName.System;
                Blade2Target = ModuleName.System;



                CmdTarget = ModuleName.System;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };

                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());

                if (ModuleHelper.IsLoadPort(tempmodule))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(tempmodule.ToString());
                    if (lp != null) lp.NoteTransferStop();
                }

                IsBusy = false;
                return true;
                    
                                
                
            }
            return false;
        }

        protected override bool fResetToReady(object[] param)
        {
            return true;
        }

        

        protected override bool fError(object[] param)
        {
            return true;
        }

        protected override bool fStartExtendForPick(object[] param)
        {
            return true;
        }

        protected override bool fStartExtendForPlace(object[] param)
        {
            return true;
        }

        protected override bool fStartRetractFromPick(object[] param)
        {
            return true;
        }

        protected override bool fStartRetractFromPlace(object[] param)
        {
            return true;
        }

        public override RobotArmWaferStateEnum GetWaferState(RobotArmEnum arm)
        {
            if (arm == RobotArmEnum.Lower)
                return IsLeftArmPressureSensorON? RobotArmWaferStateEnum.Present: RobotArmWaferStateEnum.Absent;
            if (arm == RobotArmEnum.Upper)
                return IsRightArmPressureSensorON ? RobotArmWaferStateEnum.Present : RobotArmWaferStateEnum.Absent; ;
            return RobotArmWaferStateEnum.ArmInvalid;
        }

        public bool ReadStatus()
        {
            lock(_locker)
            {
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, ""));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "CS", "1"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "CS", "2"));
                _lstMonitorHandler.AddLast(new JelRobotReadHandler(this, "G"));

            }
            return true;
        }

        public void HandlerError(string errorMsg)
        {
            _lstMonitorHandler.Clear();
            OnError(errorMsg);
        }
        protected string BuildBladeTarget()
        {
            return (CmdRobotArm == RobotArmEnum.Upper ? "ArmB" : "ArmA") + "." + CmdTarget;
        }


        public bool GetBankAndCassetteNumber(ModuleName module, WaferSize wz, out int BankNum,out int CassetteNum)
        {
            BankNum = 0;
            CassetteNum = 0;
            int wzindex = 0;
            if (ModuleHelper.IsLoadPort(module))
            {
                
                var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
                if (lp != null) 
                {
                    wzindex = lp.InfoPadCarrierIndex;
                }
                else
                {
                    var lp1 = DEVICE.GetDevice<LoadPort>(module.ToString());
                    wzindex = lp.InfoPadCarrierIndex;
                }


                if (SC.ContainsItem($"CarrierInfo.{module}BankNumber{wzindex}"))
                    BankNum = SC.GetValue<int>($"CarrierInfo.{module}BankNumber{wzindex}");

                else if (SC.ContainsItem($"CarrierInfo.Carrier{wzindex}.{module}BankNumber"))
                    BankNum = SC.GetValue<int>($"CarrierInfo.Carrier{wzindex}.{module}BankNumber");
                else
                {
                    if(SC.ContainsItem($"CarrierInfo.Carrier{wzindex}.{module}Station"))
                    {
                        string[] stationInfo = SC.GetStringValue($"CarrierInfo.Carrier{wzindex}.{module}Station").Split('.');

                        if (stationInfo.Length < 2) return false;

                        if (!int.TryParse(stationInfo[0], out BankNum))
                        {
                            EV.PostAlarmLog("Robot", $"{RobotModuleName} parse bank number failed");
                            return false;
                        }
                            
                        if (!int.TryParse(stationInfo[1], out CassetteNum))
                        {
                            EV.PostAlarmLog("Robot", $"{RobotModuleName} parse cassette number failed");
                            return false;
                        }
                        return true;
                    }
                }

                if (SC.ContainsItem($"CarrierInfo.{module}CassetteNumber{wzindex}"))
                    CassetteNum = SC.GetValue<int>($"CarrierInfo.{module}CassetteNumber{wzindex}");

                else if (SC.ContainsItem($"CarrierInfo.Carrier{wzindex}.{module}CassetteNumber"))
                    CassetteNum = SC.GetValue<int>($"CarrierInfo.Carrier{wzindex}.{module}CassetteNumber");

                else
                {
                    EV.PostAlarmLog("Robot", $"{RobotModuleName} parse cassette number failed");
                    return false;
                }
                return true;
            }

            if(ModuleHelper.IsTurnOverStation(module))
            {
                var flipper = DEVICE.GetDevice<FlipperBaseDevice>(module.ToString());

                if (SC.ContainsItem($"CarrierInfo.{module}{flipper.CurrentFlipperPosition}Station"))
                {
                    string[] stationInfo = SC.GetStringValue($"CarrierInfo.{module}{flipper.CurrentFlipperPosition}Station").Split('.');

                    if (stationInfo.Length < 2) return false;

                    if (!int.TryParse(stationInfo[0], out BankNum))
                    {
                        EV.PostAlarmLog("Robot", $"{RobotModuleName} parse bank number failed");
                        return false;
                    }

                    if (!int.TryParse(stationInfo[1], out CassetteNum))
                    {
                        EV.PostAlarmLog("Robot", $"{RobotModuleName} parse cassette number failed");
                        return false;
                    }
                    return true;
                }




            }





            if (wz == WaferSize.WS12) wzindex = 12;
            if (wz == WaferSize.WS8) wzindex = 8;
            if (wz == WaferSize.WS6) wzindex = 6;
            if (wz == WaferSize.WS4) wzindex = 4;
            if (wz == WaferSize.WS3) wzindex = 3;






            if (SC.ContainsItem($"CarrierInfo.{module}Station"))
            {
                string[] stationInfo = SC.GetStringValue($"CarrierInfo.{module}Station").Split('.');

                if (stationInfo.Length < 2) return false;

                if (!int.TryParse(stationInfo[0], out BankNum))
                {
                    EV.PostAlarmLog("Robot", $"{RobotModuleName} parse bank number failed");
                    return false;
                }

                if (!int.TryParse(stationInfo[1], out CassetteNum))
                {
                    EV.PostAlarmLog("Robot", $"{RobotModuleName} parse cassette number failed");
                    return false;
                }
                return true;
            }


            if (SC.ContainsItem($"CarrierInfo.{module}Station{wzindex}"))
            {
                string[] stationInfo = SC.GetStringValue($"CarrierInfo.{module}Station{wzindex}").Split('.');

                if (stationInfo.Length < 2) return false;

                if (!int.TryParse(stationInfo[0], out BankNum))
                {
                    EV.PostAlarmLog("Robot", $"{RobotModuleName} parse bank number failed");
                    return false;
                }

                if (!int.TryParse(stationInfo[1], out CassetteNum))
                {
                    EV.PostAlarmLog("Robot", $"{RobotModuleName} parse cassette number failed");
                    return false;
                }
                return true;
            }





            if (SC.ContainsItem($"CarrierInfo.{module}BankNumber{wzindex}"))
                BankNum = SC.GetValue<int>($"CarrierInfo.{module}BankNumber{wzindex}");
            else
                return false;

            if (SC.ContainsItem($"CarrierInfo.{module}CassetteNumber{wzindex}"))
                CassetteNum = SC.GetValue<int>($"CarrierInfo.{module}CassetteNumber{wzindex}");
            else
                return false;

            return true;
        }


    }
    public enum JelAxisStatus
    {
        NormalEnd,
        InOperation,
        SensorError,
        SensorErrorOrStoppedByAlarm = 4,
        CommandError = 8,
    }
    public enum JelCommandStatus
    {
        None,
        NormalEnd,
        InExecution,
        InPause,
        InError,
    }

    public struct JelParameterData
    {
        public string Value;
        public string MinValue;
        public string MaxValue;
    }
    public enum JelRobotArm
    {
        Left =0,
        Right,
        Dual,
    }

}
