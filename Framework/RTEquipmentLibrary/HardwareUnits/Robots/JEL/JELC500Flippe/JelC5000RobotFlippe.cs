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
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.Common.CommonData;
using Aitex.Core.RT.IOCore;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.JEL
{
    public class JelC5000RobotFlippe : RobotBaseDevice, IConnection
    {
        private int _bodyNumber;
        private string _robotModel;    //T-00902H
        public int BodyNumber { get => _bodyNumber; }
        private string _address = "";
        public string Address { get => _address; }
        private bool isSimulatorMode;
        private string _scRoot;       
        private PeriodicJob _thread;
        private JelC5000RobotFlippeConnection _connection;
        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private R_TRIG _trigActionDone = new R_TRIG();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

        private AOAccessor _aoBladeRegulator { get; set; }

        private object _locker = new object();

        private bool _enableLog;

        public string PortName;
        public bool IsConnected { get; }
        public bool Connect()
        {
            return true;
        }

        private int _lowerArmWaferSensorIndex
        {
            get
            {
                if (SC.ContainsItem($"Robot.{RobotModuleName}.LowerArmWaferSensorIndex"))
                    return SC.GetValue<int>($"Robot.{RobotModuleName}.LowerArmWaferSensorIndex");
                return 1;
            }
        }


        public bool Disconnect()
        {
            return true;
        }
        public JelC5000RobotFlippe(string module, string name, string scRoot, AOAccessor aoBladeRegulator, string robotModel = "") : base(module, name)
        {
            _robotModel = robotModel;
            isSimulatorMode = SC.ContainsItem("System.IsSimulatorMode") ? SC.GetValue<bool>("System.IsSimulatorMode") : false;
            _scRoot = scRoot;

            //base.Initialize();
            ResetPropertiesAndResponses();
            RegisterSpecialData();
           
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            _bodyNumber = SC.GetValue<int>($"{_scRoot}.{Name}.BodyNumber");
            PortName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");

            _aoBladeRegulator = aoBladeRegulator;

            _connection = new JelC5000RobotFlippeConnection(this,PortName);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
               
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }
            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);   
        }

        #region Properties
        public JelAxisStatus JelC5000RobotStatus { get; private set; }
        public JelAxisStatus AxisStatus { get; private set; }
        public float XAxisPostion { get; private set; }
        public float ThetaAxisPostion { get; private set; }
        public float YAxisPostion { get; private set; }
        public float ZAxisPostion { get; private set; }
        public Int32[] AData { get; private set; } = new Int32[1000];
        public JelParameterData[] RobotParameters { get; private set; } = new JelParameterData[2000];
        public string ReadBankNumber { get; private set; } = "";
        public int ReadCassetNumber { get; private set; }
        public int ReadSlotNumber { get; private set; }
        public bool IsVacuumSensorON { get; private set; } = true;
       
        public bool IsEchoBack { get; private set; }
        public JelCommandStatus CurrentCompoundCommandStatus { get; set; }   

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
        public int CurrentReadSpeedData { get; private set; }
        public Int32 CurrentReadAData { get; private set; }
        public Int32 ReadPosXAxisPostion { get; private set; }
        public Int32 ReadPosYAxisPostion { get; private set; }
        public Int32 ReadPosThetaAxisPostion { get; private set; }
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

        public override bool BladeAEnable => true;
        public override bool BladeBEnable => false;

        private ModuleName LastTargeModule { get; set; }

        #endregion

        #region ParseHandler
        public void ParseData(string command,string parameter,string data)
        {
            try
            {
                string datavalue = data.Replace($"${BodyNumber}", "").Replace("\r", "");
                if(command == "SPA")
                {
                    CurrentReadSpeedData = Convert.ToInt32(datavalue);
                }

                if (command == "") ParseRobotStatus(datavalue);     // Read Robot Status
                if (command == "6" || command == "6M") ParseRobotPostion(parameter, datavalue); //Read Postion Data
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
                if (command == $"CS{_lowerArmWaferSensorIndex}")
                {
                    IsVacuumSensorON = datavalue == "1";
                   
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
                    CurrentReadSpeedData = Convert.ToInt32(datavalue);
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
                    ReadPosXAxisPostion = Convert.ToInt32(stradata[0].ToString());
                    ReadPosYAxisPostion = Convert.ToInt32(stradata[1].ToString());
                    ReadPosThetaAxisPostion = Convert.ToInt32(stradata[2].ToString());
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
        private void ParseRobotStatus(string data)
        {
            if (data.Length < 2) return;
            int intRobostatus = Convert.ToInt16(data.ToArray()[0].ToString());
            if (intRobostatus == 0)
                JelC5000RobotStatus = (JelAxisStatus)Convert.ToInt16(data.ToArray()[0].ToString());
            if((intRobostatus & 0x1) == 0x1)
            {
                JelC5000RobotStatus = JelAxisStatus.InOperation;
            }
            if ((intRobostatus & 0x2) == 0x2)
            {
                JelC5000RobotStatus = JelAxisStatus.SensorError;
                OnError(JelC5000RobotStatus.ToString());
            }
            if ((intRobostatus & 0x4) == 0x4)
            {
                JelC5000RobotStatus = JelAxisStatus.SensorErrorOrStoppedByAlarm;
                OnError(JelC5000RobotStatus.ToString());
            }
            if ((intRobostatus & 0x8) == 0x8)
            {
                JelC5000RobotStatus = JelAxisStatus.CommandError;
                OnError(JelC5000RobotStatus.ToString());
            }

            if (data.ToArray()[1] == '0')
                AxisStatus = JelAxisStatus.NormalEnd;
            else
                AxisStatus = JelAxisStatus.InOperation;
        }
        private void ParseRobotPostion(string axis,string data)
        {
            float _floatvalue;
            if (!float.TryParse(data, out _floatvalue)) return;
            if (axis == "1")
            {
                PositionAxis1 = _floatvalue;
            }
            if (axis == "2")
            {                
                PositionAxis2 = _floatvalue;
            }
            if (axis == "3")
            {               
                PositionAxis3 = _floatvalue;
            }
            if (axis == "4")
            {                
                PositionAxis4 = _floatvalue;
            }

            if (axis == "X")
            {
                XAxisPostion = _floatvalue;
            }
            if (axis == "Y")
            {
                YAxisPostion = _floatvalue;
            }
            if (axis == "U")
            {
                ThetaAxisPostion = _floatvalue;
            }
            if (axis == "Z")
            {
                ZAxisPostion = _floatvalue;
            }
        }

        #endregion

        private bool OnTimer()
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

                lock (_locker)
                {
                    if(_lstMonitorHandler.Count > 0)
                    {
                        if (!_connection.IsBusy)
                        {                           
                                handler = _lstMonitorHandler.First.Value;
                                _connection.Execute(handler);
                            _lstMonitorHandler.RemoveFirst();                           
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
            DATA.Subscribe($"{Module}.{Name}.IsVacuumSensorON", () => IsVacuumSensorON);
            DATA.Subscribe($"{Module}.{Name}.JelC5000RobotStatus", () => JelC5000RobotStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.AxisStatus", () => AxisStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.CurrentCompoundCommandStatus", () => CurrentCompoundCommandStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadCassetNumber", () => ReadCassetNumber.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadSlotNumber", () => ReadSlotNumber.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadBankNumber", () => ReadBankNumber.ToString());

            DATA.Subscribe($"{Module}.{Name}.ReadPosXAxisPostion", () => ReadPosXAxisPostion);
            DATA.Subscribe($"{Module}.{Name}.ReadPosYAxisPostion", () => ReadPosYAxisPostion);
            DATA.Subscribe($"{Module}.{Name}.ReadPosZAxisPostion", () => ReadPosZAxisPostion);
            DATA.Subscribe($"{Module}.{Name}.ReadPosThetaAxisPostion", () => ReadPosThetaAxisPostion);

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
            DATA.Subscribe($"{Name}.CurrentReadRoutine", () => CurrentReadRoutine);
            DATA.Subscribe($"{Name}.CurrentReadSpeedData", () => CurrentReadSpeedData);


            DATA.Subscribe($"{Name}.CurrentReadAData", () => CurrentReadAData);
            DATA.Subscribe($"{Name}.IsVacuumSensorON", () => IsVacuumSensorON);
            DATA.Subscribe($"{Name}.JelC5000RobotStatus", () => JelC5000RobotStatus.ToString());
            DATA.Subscribe($"{Name}.AxisStatus", () => AxisStatus.ToString());
            DATA.Subscribe($"{Name}.CurrentCompoundCommandStatus", () => CurrentCompoundCommandStatus.ToString());
            DATA.Subscribe($"{Name}.ReadCassetNumber", () => ReadCassetNumber.ToString());
            DATA.Subscribe($"{Name}.ReadSlotNumber", () => ReadSlotNumber.ToString());
            DATA.Subscribe($"{Name}.ReadBankNumber", () => ReadBankNumber.ToString());

            DATA.Subscribe($"{Name}.ReadPosXAxisPostion", () => ReadPosXAxisPostion.ToString());
            DATA.Subscribe($"{Name}.ReadPosYAxisPostion", () => ReadPosYAxisPostion.ToString());
            DATA.Subscribe($"{Name}.ReadPosZAxisPostion", () => ReadPosZAxisPostion.ToString());
            DATA.Subscribe($"{Name}.ReadPosThetaAxisPostion", () => ReadPosThetaAxisPostion.ToString());

            DATA.Subscribe($"{Name}.XAxisPostion", () => XAxisPostion.ToString());
            DATA.Subscribe($"{Name}.YAxisPostion", () => YAxisPostion.ToString());
            DATA.Subscribe($"{Name}.ZAxisPostion", () => ZAxisPostion.ToString());
            DATA.Subscribe($"{Name}.ThetaAxisPostion", () => ThetaAxisPostion.ToString());

            DATA.Subscribe($"{Name}.ReadParameterMax", () => ReadParameterMax.ToString());
            DATA.Subscribe($"{Name}.ReadParameterMin", () => ReadParameterMin.ToString());
            DATA.Subscribe($"{Name}.ReadParameterValue", () => ReadParameterValue.ToString());

            DATA.Subscribe($"{Name}.MappingFirstSlotPosition", () => MappingFirstSlotPosition.ToString());
            DATA.Subscribe($"{Name}.MappingGateWidth", () => MappingGateWidth.ToString());
            DATA.Subscribe($"{Name}.MappingMaxDetectWidth", () => MappingMaxDetectWidth.ToString());
            DATA.Subscribe($"{Name}.MappingMinDetectWidth", () => MappingMinDetectWidth.ToString());
            DATA.Subscribe($"{Name}.MappingSlotsNumber", () => MappingSlotsNumber.ToString());
            DATA.Subscribe($"{Name}.MappingSpeed", () => MappingSpeed.ToString());
            DATA.Subscribe($"{Name}.MappingStopPostion", () => MappingStopPostion.ToString());
            DATA.Subscribe($"{Name}.MappingTopSlotPostion", () => MappingTopSlotPostion.ToString());
            DATA.Subscribe($"{Name}.IsMappingSensorON", () => IsMappingSensorON.ToString());
            DATA.Subscribe($"{Name}.MappingWaferResult", () => MappingWaferResult.ToString());
            DATA.Subscribe($"{Name}.MappingWidthResult", () => MappingWidthResult.ToString());
            DATA.Subscribe($"{Name}.BladeRegulatorRatio", () => BladeRegulatorRatio);

            OP.Subscribe(String.Format("{0}.{1}", Name, "FlippeTo0Degree"), (out string reason, int time, object[] param) =>
            {
                bool ret = ExecuteCommand(param);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Execute command succesfully");
                    return true;
                }
                reason = $"{Name} execute failed";
                return false;
            });
        }

        private int BladeRegulatorRatio
        {
            get
            {
                if (_aoBladeRegulator == null)
                    return 0;
                return (int)(_aoBladeRegulator.Value / 100);
            }
        }

        private void ResetPropertiesAndResponses()
        {
            
        }
        protected override bool fReset(object[] param)
        {
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            _connection.EnableLog(_enableLog);

            _trigRetryConnect.RST = true;

            _lstMonitorHandler.Clear();
            //_lstMonitorHandler.Clear();
            _connection.ForceClear();
            lock (_locker)
            {
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeRawCommandHandler(this, $"RD"));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, ""));
            }
            return true;
        }



        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            if (JelC5000RobotStatus == JelAxisStatus.NormalEnd
                && (AxisStatus == JelAxisStatus.NormalEnd)
                && _lstMonitorHandler.Count == 0 
                && !_connection.IsBusy)
            {
                IsBusy = false;
                return true;
            }
            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelC5000RobotFlippeReadHandler(this, ""));
            return false;
        }
        protected override bool fStartMove(object[] param)
        {
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
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "2", instance.ToString()));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeMoveHandler(this, commandstr + "M", axis.ToString() + speedtype));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, ""));
            }
            return true;
        }
        protected override bool fMonitorMoving(object[] param)
        {
            if (JelC5000RobotStatus == JelAxisStatus.NormalEnd
                && (AxisStatus == JelAxisStatus.NormalEnd)                
                && _lstMonitorHandler.Count == 0
                && !_connection.IsBusy)
            {
                IsBusy = false;
                return true;
            }
            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelC5000RobotFlippeReadHandler(this, ""));
            return false;
        }

        protected override bool fMonitorGoTo(object[] param)
        {
            if (JelC5000RobotStatus == JelAxisStatus.NormalEnd
                && (AxisStatus == JelAxisStatus.NormalEnd)                
                && _lstMonitorHandler.Count == 0
                && !_connection.IsBusy)
            {
                IsBusy = false;
                return true;
            }
            if (_lstMonitorHandler.Count == 0  && !_connection.IsBusy)
                _connection.Execute(new JelC5000RobotFlippeReadHandler(this, ""));
            return false;
        }
        protected override bool fStartInit(object[] param)
        {
            //string speedtype = string.Empty;
            //if (param != null && param.Length >0)
            //    speedtype = Regex.Replace(param[0].ToString().Replace("（", "(").Replace("）", ")"), @"\([^\(]*\)", "");

            //if (speedtype != "" && speedtype != "M" && speedtype != "L") return false;
            _dtActionStart = DateTime.Now;
            int compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.InitCmdNO");
            int Robotspeed = SC.GetValue<int>($"Robot.{Name}.RobotSpeed");
            if (Robotspeed < 0)
                Robotspeed = 0;
            if (Robotspeed > 99)
                Robotspeed = 99;
            lock (_locker)
            {
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeCompaundCommandHandler(this, compaundcmdNO.ToString()));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, ""));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "G"));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, $"CS{_lowerArmWaferSensorIndex}"));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "BC"));     // Read bank number
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WCP"));    // Read Cassette Number
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "1"));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "2"));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "3"));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "4"));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "X"));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "Y"));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "U"));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "Z"));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "SPA", Robotspeed.ToString()));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "SPA"));
            }
            return true;
        }
        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                HandlerError("InitTimeout");
                return true;
            }

           
            if (_lstMonitorHandler.Count ==0 && !_connection.IsBusy)
            {
                if (CurrentCompoundCommandStatus == JelCommandStatus.NormalEnd && 
                    JelC5000RobotStatus == JelAxisStatus.NormalEnd
                && AxisStatus == JelAxisStatus.NormalEnd)               
                {
                        Blade1Target = ModuleName.System;
                        Blade2Target = ModuleName.System;



                    CmdTarget = ModuleName.System;
                    LastTargeModule = ModuleName.System;
                    MoveInfo = new RobotMoveInfo()
                    {
                        Action = RobotAction.Picking,
                        ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                        BladeTarget = BuildBladeTarget(),
                    };

                    if(IsVacuumSensorON && WaferManager.Instance.CheckNoWafer(RobotModuleName,0))
                    {
                        EV.PostAlarmLog("Robot", $"There's unknown wafer on {RobotModuleName}.");
                        WaferManager.Instance.CreateWafer(RobotModuleName, 0, WaferStatus.Normal);      
                    }

                    if (!IsVacuumSensorON && WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                    {
                        EV.PostAlarmLog("Robot", $"There's a wafer on {RobotModuleName} but robot didn't detect.");                       
                    }


                    return true;  
                }
                if (CurrentCompoundCommandStatus != JelCommandStatus.NormalEnd)
                {
                    if (CurrentCompoundCommandStatus == JelCommandStatus.InError)
                    {
                        OnError("Init Compaund command execution error");
                        return false;
                    }                  
                }
            }
            return false;            
        }
        protected override bool fStartReadData(object[] param)
        {
            if(param.Length <1) return false;
            string readcommand = param[0].ToString();
            switch(readcommand)
            {
                case "CurrentStatus":
                    lock (_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, ""));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, $"CS{_lowerArmWaferSensorIndex}"));

                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "G"));      // Read out the compaund command status 
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "BC"));     // Read bank number
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WCP"));    // Read Cassette Number
                    }

                    break;
                case "CurrentPositionData":
                    lock(_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "1"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "2"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "3"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "4"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "X"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "Y"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "U"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "6", "Z"));
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
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "IR",routineno.ToString().PadLeft(3,'0')));
                    }
                    if (routineselction == "SubRoutine")
                    {
                        char routineno;
                        if (!char.TryParse(param[2].ToString(), out routineno)) return false;
                        if (routineno > 'F' || routineno < 1) return false;
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "IRS", routineno.ToString()));
                    }
                    break;
                case "RobotSpeed":                    
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "SPA"));
                    break;
                case "AData":
                    if (param.Length < 2) return false;
                    int datano;
                    if (!int.TryParse(param[1].ToString(), out datano)) return false;
                    if (datano < 0 || datano > 999) return false;
                    lock(_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "A", datano.ToString("D3")+"D"));
                    }
                    break;
                case "EPData":
                    if (param.Length < 2) return false;
                    if (param[1].ToString() == "AData")
                    {
                        lock (_locker)
                        {
                            _lstMonitorHandler.AddLast(new JelC5000RobotFlippeRawCommandHandler (this, $"AL"));
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
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "PSD", posNO.ToString("D3")));
                    }
                    break;
                case "RobotParameter":
                    if (param.Length < 2) return false;
                    uint parano;
                    if(!uint.TryParse(param[1].ToString(), out parano)) return false;
                    lock(_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "DTD", parano.ToString()));
                    }
                    break;
                case "MappingData":
                    lock(_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WLO"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WHI"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WFC"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WWN"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WWM"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WWG"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WEND"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WSP"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WFK"));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WFW"));
                    }
                    break;

            }
            return true;


        }
        protected override bool fMonitorReadData(object[] param)
        {
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
                            _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "I", routineno.ToString().PadLeft(3, '0')));
                            _lstMonitorHandler.AddLast(new JelC5000RobotFlippeRawCommandHandler(this, param[3].ToString()));

                        }
                        if (routineselction == "SubRoutine")
                        {
                            char routineno;
                            if (!char.TryParse(param[2].ToString(), out routineno)) return false;
                            if (routineno > 'F' || routineno < 1) return false;
                            _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "IS", routineno.ToString()));
                            _lstMonitorHandler.AddLast(new JelC5000RobotFlippeRawCommandHandler(this, param[3].ToString()));

                        }
                        break;
                    case "RobotSpeed":
                        //if (param.Length < 3) return false;
                        //string speedtype = param[1].ToString().Split('(')[0];
                        //Int32 speedvalue;
                        //if (!Int32.TryParse(param[2].ToString(), out speedvalue)) return false;
                        //_lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "O" + speedtype, speedvalue.ToString().PadLeft(5, '0')));
                        Int32 speedvalue;
                        if (!Int32.TryParse(param[2].ToString(), out speedvalue)) return false;

                        if(SC.ContainsItem($"Robot.{RobotModuleName}.RobotSpeed"))
                        {
                            SC.SetItemValue($"Robot.{RobotModuleName}.RobotSpeed", speedvalue);
                        }


                        if (speedvalue < 0)
                            speedvalue = 0;
                        if (speedvalue > 99)
                            speedvalue = 99;
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "SPA" , speedvalue.ToString()));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "SPA"));

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
                            _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "A", datano.ToString("D3") + datavalue.ToString()));
                        }
                        break;
                    case "EPData":
                        if (param.Length < 2) return false;
                        if (param[1].ToString() == "AData")
                        {
                            lock (_locker)
                            {
                                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "AW"));
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
                            _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "PS",
                                $"{posno.ToString("D3")}{rightarmpos},{thetapos},{leftarmpos},{zpos}"));
                        }
                        return true;
                    case "CurrentPositionData":
                        Int32 currentpos = Int32.Parse(param[1].ToString());
                        if (currentpos > 999 || currentpos < 0) return false;
                        lock (_locker)
                        {
                            _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "PS",
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
                            _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "DTSVAL", $"{parano},{paravalue}"));
                            _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "DW"));
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
                                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WLO",strtempvalue));
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
                                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WHI",strtempvalue));
                            }

                            if (!string.IsNullOrEmpty(param[3].ToString()))
                                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WFC",
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
                                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WWN", strtempvalue));
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
                                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WWM", strtempvalue));
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
                                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WWG", strtempvalue));
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
                                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WEND", strtempvalue));
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
                                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WSP", strtempvalue));
                            }

                        }
                        break;
                    case "MappingCommand":
                        lock(_locker)
                        {
                            //_lstMonitorHandler.AddLast(new JelC5000RobotMoveHandler(this, "WFS"));
                            //_lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WFK"));
                            //_lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WFW"));
                        }
                        break;
                    case "BankNumber":
                        lock (_locker)
                        {
                            if (param.Length > 1 && !string.IsNullOrEmpty(param[1].ToString()))
                            {
                                int bankno = Convert.ToInt16(param[1].ToString());
                                if (bankno <= 15 && bankno >= 0)
                                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "BC", bankno.ToString("X")));
                            }
                            if (param.Length > 2 && !string.IsNullOrEmpty(param[2].ToString()))
                            {
                                int cassetteno = Convert.ToInt16(param[2].ToString());
                                if (cassetteno <=5 && cassetteno <= 1) 
                                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WCP", cassetteno.ToString()));
                            }
                            if (param.Length > 3 && !string.IsNullOrEmpty(param[3].ToString()))
                            {
                                int slotno = Convert.ToInt16(param[3].ToString());
                                if (slotno <= 25 && slotno >= 1)
                                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WCD", slotno.ToString()));
                            }
                            _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "BC"));
                            _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WCP"));
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
            return false;
        }

        protected override bool fStartUnGrip(object[] param)
        {
            if (param == null || param.Length < 1) return false;
            RobotArmEnum arm = (RobotArmEnum)((int)param[0]);
            lock (_locker)
            {               
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "DS", "10"));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, ""));
            }
            return true;

        }
        protected override bool fMonitorUnGrip(object[] param)
        {
            IsBusy = false;

            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
            {
                if (JelC5000RobotStatus != JelAxisStatus.NormalEnd
                || AxisStatus != JelAxisStatus.NormalEnd)
                {
                    lock (_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, ""));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, $"CS{_lowerArmWaferSensorIndex}"));
                    }
                    return false;
                }
                RobotArmEnum arm = (RobotArmEnum)((int)CurrentParamter[0]);
                //if(IsVacuumSensorON)
                //    HandlerError("UnGrip wafer failed!");  
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
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "DS", "11"));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, ""));

            }
            return true;
        }
        protected override bool fMonitorGrip(object[] param)
        {
            IsBusy = false;
            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
            {
                if (JelC5000RobotStatus != JelAxisStatus.NormalEnd
                || AxisStatus != JelAxisStatus.NormalEnd)
                {
                    lock (_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, ""));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, $"CS{_lowerArmWaferSensorIndex}"));
                    }
                    return false;
                }
                //if (!IsVacuumSensorON)
                //    HandlerError("UnGrip wafer failed!");

                IsBusy = false;
                return true;           
            }
                
            return false;
        }
        protected override bool fStop(object[] param)
        {
            IsBusy = false;
            _lstMonitorHandler.Clear();            
            _connection.ForceClear();
            _connection.Execute(new JelC5000RobotFlippeSetHandler(this, "S"));
            _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "GE"));
            return ReadStatus();
            
        }

        protected override bool fStartGoTo(object[] param)
        {
            return false;
        }

        protected override bool fStartMapWafer(object[] param)
        {
            IsBusy = false;
            try
            {
                _dtActionStart = DateTime.Now;
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), param[0].ToString());
                int carrierindex = 0;

                if (ModuleHelper.IsLoadPort(tempmodule))
                {
                    LoadPortBaseDevice lp = DEVICE.GetDevice<LoadPortBaseDevice>(tempmodule.ToString());
                    carrierindex = lp.InfoPadCarrierIndex;
                }
                else
                {
                    WaferSize wz = WaferManager.Instance.GetWaferSize(tempmodule, 0);
                    if (wz == WaferSize.WS12) carrierindex = 12;
                    if (wz == WaferSize.WS8) carrierindex = 8;
                    if (wz == WaferSize.WS6) carrierindex = 6;
                    if (wz == WaferSize.WS4) carrierindex = 4;
                    if (wz == WaferSize.WS3) carrierindex = 3;
                }
                int bankno = SC.GetValue<int>($"CarrierInfo.{tempmodule}BankNumber{carrierindex}");
                int cassetteNO = SC.GetValue<int>($"CarrierInfo.{tempmodule}CassetteNumber{carrierindex}");

                int compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.MapCmdNO");

                lock (_locker)
                {
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "BC", bankno.ToString("X")));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WCP", cassetteNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WCD", "1"));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "BC"));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WCP"));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeRawCommandHandler(this, $"RD"));


                }
                DateTime readstarttime = DateTime.Now;
                while (_lstMonitorHandler.Count != 0 || _connection.IsBusy)
                {
                    if (DateTime.Now - readstarttime > TimeSpan.FromSeconds(20))
                    {
                        OnError("Set Timeout.");
                        return false;
                    }
                }
                if (ReadBankNumber != bankno.ToString("X") || cassetteNO != ReadCassetNumber)
                {
                    OnError("Set data failed.");
                    return false;
                }
                lock (_locker)
                {
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeCompaundCommandHandler(this, compaundcmdNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "G"));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WFK"));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WFW"));
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
            return true;
            //if(_lstMonitorHandler.Count == 0 && _lstMoveHandler.Count == 0 && !_connection.IsBusy)
            //{
            //    IsBusy = false;
            //    return true;
            //}
            //return false;
        }

        protected override bool fStartSwapWafer(object[] param)
        {
            return false;
           
        }
        private DateTime _dtActionStart;
        protected override bool fMonitorSwap(object[] param)
        {
            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
            {
                if (CurrentCompoundCommandStatus == JelCommandStatus.NormalEnd)
                {
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
                    
                        IsBusy = false;
                        return true;
                    
                }
                if (CurrentCompoundCommandStatus == JelCommandStatus.InExecution ||
                    CurrentCompoundCommandStatus == JelCommandStatus.None)
                {
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "G"));
                }
                if (CurrentCompoundCommandStatus == JelCommandStatus.InError)
                {
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "GER"));
                    OnError("Compaund Comand execution failed");
                }
            }

            return false;
        }

        protected override bool fStartPlaceWafer(object[] param)
        {
            _dtActionStart = DateTime.Now;
            try
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());

                int slotindex = int.Parse(param[2].ToString());
                //JelC5000RobotArm jelarm = (JelC5000RobotArm)(int)arm;

                var wz = WaferManager.Instance.GetWaferSize(RobotModuleName, arm == RobotArmEnum.Both ? 0 : (int)arm);
                int wzindex = 0;
                if (wz == WaferSize.WS12) wzindex = 12;
                if (wz == WaferSize.WS8) wzindex = 8;
                if (wz == WaferSize.WS6) wzindex = 6;
                if (wz == WaferSize.WS5) wzindex = 5;
                if (wz == WaferSize.WS4) wzindex = 4;
                if (wz == WaferSize.WS3) wzindex = 3;
                if (wz == WaferSize.WS2) wzindex = 2;
                if (ModuleHelper.IsLoadPort(tempmodule))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(tempmodule.ToString());
                    if (lp != null)
                        lp.NoteTransferStart();
                    wzindex = DEVICE.GetDevice<LoadPortBaseDevice>(tempmodule.ToString()).InfoPadCarrierIndex;

                }
                int bankno = SC.GetValue<int>($"CarrierInfo.{tempmodule}BankNumber{wzindex}");
                int cassetteNO = SC.GetValue<int>($"CarrierInfo.{tempmodule}CassetteNumber{wzindex}");
                int compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.PlaceCmdNO");
                int compaundcmdNOforSafe = SC.GetValue<int>($"Robot.{Name}.SafeCmdNO");

                if (!ModuleHelper.IsLoadPort(tempmodule) && Blade1ActionPosture == BladePostureEnum.Degree180)
                {
                    bankno = SC.GetValue<int>($"CarrierInfo.Over{tempmodule}BankNumber{wzindex}");
                    cassetteNO = SC.GetValue<int>($"CarrierInfo.Over{tempmodule}CassetteNumber{wzindex}");
                    compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.OverPlaceCmdNO");
                    compaundcmdNOforSafe = SC.GetValue<int>($"Robot.{Name}.SafeCmdNO");
                }


                lock (_locker)
                {
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "BC"));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WCP"));
                }
                DateTime readstarttime = DateTime.Now;
                while (_lstMonitorHandler.Count != 0 || _connection.IsBusy)
                {
                    if (DateTime.Now - readstarttime > TimeSpan.FromSeconds(10))
                    {
                        OnError("Read bankno and cassette no for place Timeout.");
                        return false;
                    }
                }

                lock (_locker)
                {
                    if(LastTargeModule != tempmodule) //(ReadBankNumber != bankno.ToString("X") || cassetteNO != ReadCassetNumber)
                    {
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeCompaundCommandHandler(this, compaundcmdNOforSafe.ToString()));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "G"));
                    }               
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "BC", bankno.ToString("X")));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WCP", cassetteNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WCD", (slotindex+1).ToString()));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "BC"));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WCP"));               
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeCompaundCommandHandler(this, compaundcmdNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "G"));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, $"CS{_lowerArmWaferSensorIndex}"));
                }
                Blade1Target = tempmodule;
                Blade2Target = tempmodule;
                CmdTarget = tempmodule;
                LastTargeModule = tempmodule;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Placing,
                    ArmTarget = RobotArm.Both,  // CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
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
            _dtActionStart = DateTime.Now;
            try
            {
                //RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName tempmodule = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                
                int slotindex = int.Parse(param[2].ToString());                
                //JelC5000RobotArm jelarm = (JelC5000RobotArm)(int)arm;

                var wz = WaferManager.Instance.GetWaferSize(tempmodule, slotindex);
                int wzindex = 0;
                if (wz == WaferSize.WS12) wzindex = 12;
                if (wz == WaferSize.WS8) wzindex = 8;
                if (wz == WaferSize.WS6) wzindex = 6;
                if (wz == WaferSize.WS5) wzindex = 5;
                if (wz == WaferSize.WS4) wzindex = 4;
                if (wz == WaferSize.WS3) wzindex = 3;
                if (wz == WaferSize.WS2) wzindex = 2;
                
                if (ModuleHelper.IsLoadPort(tempmodule))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(tempmodule.ToString());
                    if (lp != null)
                        lp.NoteTransferStart();
                    wzindex = lp.InfoPadCarrierIndex;
                }

                int bankno = SC.GetValue<int>($"CarrierInfo.{tempmodule}BankNumber{wzindex}");
                int cassetteNO = SC.GetValue<int>($"CarrierInfo.{tempmodule}CassetteNumber{wzindex}");
                int compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.PickCmdNO");
                int compaundcmdNOforSafe = SC.GetValue<int>($"Robot.{Name}.SafeCmdNO");

                if(!ModuleHelper.IsLoadPort(tempmodule) && Blade1ActionPosture == BladePostureEnum.Degree180)
                {
                    bankno = SC.GetValue<int>($"CarrierInfo.Over{tempmodule}BankNumber{wzindex}");
                    cassetteNO = SC.GetValue<int>($"CarrierInfo.Over{tempmodule}CassetteNumber{wzindex}");
                    compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.OverPickCmdNO");
                    compaundcmdNOforSafe = SC.GetValue<int>($"Robot.{Name}.SafeCmdNO");
                }


                lock (_locker)
                {
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "BC"));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WCP"));
                }
                DateTime readstarttime = DateTime.Now;
                while (_lstMonitorHandler.Count != 0 || _connection.IsBusy)
                {
                    if (DateTime.Now - readstarttime > TimeSpan.FromSeconds(20))
                    {
                        OnError("Read bankno and cassette no for pick Timeout.");
                        return false;
                    }
                }
                
               lock (_locker)
               {
                    if(LastTargeModule != tempmodule)//(ReadBankNumber != bankno.ToString("X") || cassetteNO != ReadCassetNumber)
                    {
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeCompaundCommandHandler(this, compaundcmdNOforSafe.ToString()));
                        _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "G"));
                    }
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "BC", bankno.ToString("X")));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WCP", cassetteNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeSetHandler(this, "WCD", (slotindex+1).ToString()));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "BC"));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "WCP"));              
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeCompaundCommandHandler(this,compaundcmdNO.ToString()));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "G"));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, $"CS{_lowerArmWaferSensorIndex}"));
                    
                }
                Blade1Target = tempmodule;
                Blade2Target = tempmodule;


                CmdTarget = tempmodule;
                LastTargeModule = tempmodule;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Picking,
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
            if(DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Place wafer timeout");
                return true;
            }
           
            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
            {
                if (CurrentCompoundCommandStatus == JelCommandStatus.NormalEnd)
                {
                    RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                    ModuleName sourcemodule;
                    if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
                    int Sourceslotindex;
                    if (!int.TryParse(CurrentParamter[2].ToString(), out Sourceslotindex)) return false;

                    if (IsVacuumSensorON)
                    {
                        OnError("Vacuum sensor on blade is ON");
                        return false;
                    }
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

                    if (ModuleHelper.IsLoadPort(sourcemodule))
                    {
                        var lp = DEVICE.GetDevice<LoadPortBaseDevice>(sourcemodule.ToString());
                        if (lp != null)
                            lp.NoteTransferStop();
                    }

                    IsBusy = false;

                    CmdTarget = ModuleName.System;
                    MoveInfo = new RobotMoveInfo()
                    {
                        Action = RobotAction.Picking,
                        ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                        BladeTarget = BuildBladeTarget(),
                    };

                    return true;
                    
                }   
                
                if (CurrentCompoundCommandStatus == JelCommandStatus.InError)
                {
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "GER"));
                    HandlerError("PlaceFailed");
                }
             
            }
            return false;
        }
        protected override bool fMonitorPick(object[] param)
        {
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Pick wafer timeout");
                return true;
            }
            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
            {
                if (CurrentCompoundCommandStatus == JelCommandStatus.NormalEnd)
                {
                    RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                    ModuleName sourcemodule;
                    if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
                    int SourceslotIndex;
                    if (!int.TryParse(CurrentParamter[2].ToString(), out SourceslotIndex)) return false;

                    if(!IsVacuumSensorON)
                    {
                        OnError("Vacuum sensor is not ON");
                        return false;
                    }

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

                    if (ModuleHelper.IsLoadPort(sourcemodule))
                    {
                        var lp = DEVICE.GetDevice<LoadPortBaseDevice>(sourcemodule.ToString());
                        if (lp != null)
                            lp.NoteTransferStop();
                    }
                    IsBusy = false;


                    CmdTarget = ModuleName.System;
                    MoveInfo = new RobotMoveInfo()
                    {
                        Action = RobotAction.Picking,
                        ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                        BladeTarget = BuildBladeTarget(),
                    };
                    return true;                    
                }        
                if(CurrentCompoundCommandStatus == JelCommandStatus.InError)
                {
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "GER"));
                    OnError("Compaund Comand execution failed");
                }
                else
                {
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "G"));
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, $"CS{_lowerArmWaferSensorIndex}"));
                }
                
            }
            return false;
        }

        protected override bool fStartExecuteCommand(object[] param)
        {
            string command = param[0].ToString();
            if (string.IsNullOrEmpty(command))
                return false;
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {
                if(command == "RobotFlippe")
                {
                    int compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.FlippeTo0CmdNO");
                    if(!Convert.ToBoolean(param[1]))
                        compaundcmdNO = SC.GetValue<int>($"Robot.{Name}.FlippeTo180CmdNO");
                    _lstMonitorHandler.AddLast(new JelC5000RobotFlippeCompaundCommandHandler(this, compaundcmdNO.ToString()));
                    return true;

                }
                if(command == "SetRegulator")
                {
                    int ratio = Convert.ToInt32(param[1]);
                    if(_aoBladeRegulator != null)
                        _aoBladeRegulator.Value = (short)(100 * ratio);
                    _dtActionStart = DateTime.Now;
                    return true;
                }
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeRawCommandHandler(this, command));
            }
            _dtActionStart = DateTime.Now;
            return true;
        }

        protected override bool fMonitorExecuting(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Command execution timeout");
                return true;
            }
            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
                return true;

            return false;
        }

        public override bool RobotFlippe(bool to0degree)
        {
            return CheckToPostMessage(RobotMsg.ExecuteCommand, new object[] { "RobotFlippe", to0degree });
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
            bool ispresent = WaferManager.Instance.CheckHasWafer(RobotModuleName, 0);
            if (IsVacuumSensorON == ispresent)
                return ispresent ? RobotArmWaferStateEnum.Present : RobotArmWaferStateEnum.Absent;
            return RobotArmWaferStateEnum.Unknown;
        }

        public bool ReadStatus()
        {
            lock(_locker)
            {
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, ""));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, $"CS{_lowerArmWaferSensorIndex}"));
                _lstMonitorHandler.AddLast(new JelC5000RobotFlippeReadHandler(this, "G"));

            }
            return true;
        }

        public override bool SetWaferSize(RobotArmEnum arm, WaferSize size)
        {
            if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                WaferManager.Instance.UpdateWaferSize(RobotModuleName, 0, size);
            return true;
        }

        public override WaferSize GetCurrentWaferSize()
        {
            if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                return WaferManager.Instance.GetWaferSize(RobotModuleName, 0);
           return WaferSize.WS0;
        }


        public void HandlerError(string errorMsg)
        {
            _lstMonitorHandler.Clear();            
            OnError(errorMsg);
        }

        private string BuildBladeTarget()
        {
            return (CmdRobotArm == RobotArmEnum.Upper ? "ArmB" : "ArmA") + "." + CmdTarget;
        }
    }   

}
