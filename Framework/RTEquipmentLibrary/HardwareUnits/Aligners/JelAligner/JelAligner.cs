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
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.AlignersBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.JEL;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.JelAligner
{
    public class JelAligner:AlignerBaseDevice,IConnection
    {
        /* SAL3262HV model: for 2-inch to 6-inch wafer
           SAL3362HV model: for 3-inch to 6-inch wafer
           SAL3482HV model: for 4-inch to 8-inch wafer
           SAL38C3HV model: for 8-inch to 12-inch wafer*/
        public JelAligner(string module, string name,string scRoot,IoSensor[] dis,IoTrigger[] dos,int alignerType =0,string robotModel = ""):base(module,name)
        {
            isSimulatorMode = SC.ContainsItem("System.IsSimulatorMode") ? SC.GetValue<bool>("System.IsSimulatorMode") : false;
            _robotModel = robotModel;
            _scRoot = scRoot;
            if (dis != null)
            {
                _diWaferPresent = dis[0];
                if(dis.Length >2)
                {
                    _diOcrOn300 = dis[1];
                    _diOcrOn200 = dis[2];
                }
                
            }
            if (dos != null)
            {
                _doOcrTo300 = dos[0];
                _doOcrTo200 = dos[1];
            }
            ResetPropertiesAndResponses();
            RegisterSpecialData();
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            _bodyNumber = SC.GetValue<int>($"{_scRoot}.{Name}.BodyNumber");
            PortName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");
            //TimelimitAlginerHome = SC.GetValue<int>($"{_scRoot}.{Name}.TimeLimitAlignerHome");
            //TimelimitForAlignWafer = SC.GetValue<int>($"{_scRoot}.{Name}.TimeLimitForAlignWafer");

            _connection = new JelAlignerConnection(PortName);
            _connection.EnableLog(_enableLog);
            if (_connection.Connect())
            {

                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }
            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
        }


        private IoSensor _diWaferPresent;
        private IoSensor _diOcrOn300;
        private IoSensor _diOcrOn200;

        private IoTrigger _doOcrTo300;
        private IoTrigger _doOcrTo200;

        public virtual bool OnTimer()
        {
            try
            {
                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstMoveHandler.Clear();
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
                    while (_lstMoveHandler.Count > 0 && _lstMonitorHandler.Count == 0)
                    {
                        if (!_connection.IsBusy)
                        {
                            if (YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd &&
                                XAxisStatus == JelAxisStatus.NormalEnd)
                            {
                                handler = _lstMoveHandler.First.Value;
                                _connection.Execute(handler);
                                _lstMoveHandler.RemoveFirst();
                            }
                            else
                            {
                                _connection.Execute(new JelAlignerReadHandler(this, ""));
                            }
                        }
                        else
                        {
                            _connection.MonitorTimeout();

                            _trigCommunicationError.CLK = _connection.IsCommunicationError;
                            if (_trigCommunicationError.Q)
                            {
                                _lstMoveHandler.Clear();
                                OnError($"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                            }
                        }
                        if (DateTime.Now - dtstart > TimeSpan.FromSeconds(150))
                        {
                            _lstMonitorHandler.Clear();
                            _lstMoveHandler.Clear();
                            OnError("Robot action timeout");
                        }
                    }

                    while (_lstMonitorHandler.Count > 0)
                    {
                        if (!_connection.IsBusy)
                        {
                            handler = _lstMonitorHandler.First.Value;
                            _connection.Execute(handler);
                            _lstMonitorHandler.RemoveFirst();
                        }
                        if (DateTime.Now - dtstart > TimeSpan.FromSeconds(150))
                        {
                            _lstMonitorHandler.Clear();
                            _lstMoveHandler.Clear();
                            OnError("Robot action timeout");
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

        private void ResetPropertiesAndResponses()
        {
             
        }
        private void RegisterSpecialData()
        {
            DATA.Subscribe($"{Module}.{Name}.CurrentReadRoutine", () => CurrentReadRoutine);
            DATA.Subscribe($"{Module}.{Name}.CurrentReadSpeedData", () => CurrentReadSpeedData);
            DATA.Subscribe($"{Module}.{Name}.CurrentReadAData", () => CurrentReadAData);
            DATA.Subscribe($"{Module}.{Name}.IsLeftArmPressureSensorON", () => IsLeftArmPressureSensorON);
            DATA.Subscribe($"{Module}.{Name}.IsRightArmPressureSensorON", () => IsRightArmPressureSensorON);
            DATA.Subscribe($"{Module}.{Name}.YAxisAndThetaAxisStatus", () => YAxisAndThetaAxisStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.XAxisStatus", () => XAxisStatus.ToString());
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




        }

        private int _bodyNumber;
        private string _robotModel;    //T-00902H
        public int BodyNumber { get => _bodyNumber; }
        private string _address = "";
        public string Address { get => _address; }
        private bool isSimulatorMode;
        private string _scRoot;
        private PeriodicJob _thread;
        protected JelAlignerConnection _connection;
        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigCommunicationError = new R_TRIG();
      

        private R_TRIG _trigRetryConnect = new R_TRIG();
        private R_TRIG _trigActionDone = new R_TRIG();
        private LinkedList<HandlerBase> _lstMoveHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();
        private bool _isAligned;
        private bool _isOnHomedPostion;
        public bool IsSupportAlignCompaudCommand { get; set; } = true;
        public int TimelimitAlginerHome 
        { 
            get
            {
                return SC.GetValue<int>($"{_scRoot}.{Name}.TimeLimitAlignerHome");
            }
        }

        public int TimelimitForAlignWafer 
        { 
            get
            {
                return SC.GetValue<int>($"{_scRoot}.{Name}.TimeLimitForAlignWafer");
            } 
        }
        private object _locker = new object();

        private bool _enableLog;

        public string PortName;
        public bool IsConnected { get; }

        #region Properties
        public JelAxisStatus XAxisStatus { get; set; }
        public JelAxisStatus YAxisAndThetaAxisStatus { get; set; }
        public float RightArmPostion { get; set; }
        public float ThetaAxisPostion { get; set; }
        public float LeftArmPostion { get; set; }
        public float ZAxisPostion { get; set; }
        public Int32[] AData { get; set; } = new Int32[1000];
        public JelParameterData[] RobotParameters { get; set; } = new JelParameterData[2000];
        public string ReadBankNumber { get; set; } = "";
        public int ReadCassetNumber { get; set; }
        public int ReadSlotNumber { get; set; }
        public bool IsRightArmPressureSensorON { get; set; }
        public bool IsLeftArmPressureSensorON { get; set; }
        public bool IsEchoBack { get; set; }
        public JelCommandStatus CurrentCompoundCommandStatus { get; set; }

        public string StopPostionOfCompoundCommand { get; set; }
        public string[] MainRoutines { get; set; } = new string[320];
        public string[] SubRoutines { get; set; } = new string[16];

        public string CurrentReadRoutine { get; set; }

        public int HighSpeed { get; set; }
        public int LowSpeed { get; set; }
        public int DomainOfAccDec { get; set; }
        public int AccDecSpeed { get; set; }
        public int MagnificationOfFrequency { get; set; }
        public int PulsePostionOfManualSlowDown { get; set; }
        public int CurrentReadSpeedData { get; set; }
        public Int32 CurrentReadAData { get; set; }
        public Int32 ReadPosRightArmPostion { get; set; }
        public Int32 ReadPosThetaAxisPostion { get; set; }
        public Int32 ReadPosLeftArmPostion { get; set; }
        public Int32 ReadPosZAxisPostion { get; set; }

        public float ReadParameterMin { get; set; }
        public float ReadParameterMax { get; set; }
        public float ReadParameterValue { get; set; }

        //Mapping Data
        public float MappingFirstSlotPosition { get; set; }
        public float MappingTopSlotPostion { get; set; }
        public int MappingSlotsNumber { get; set; }
        public float MappingMinDetectWidth { get; set; }
        public float MappingMaxDetectWidth { get; set; }
        public float MappingGateWidth { get; set; }
        public float MappingStopPostion { get; set; }
        public float MappingSpeed { get; set; }
        public bool IsMappingSensorON { get; set; }
        public string MappingWaferResult { get; set; } = string.Empty;
        public string MappingWidthResult { get; set; } = string.Empty;
        #endregion
        

      


        #region ParseHandler
        public void ParseData(string command, string parameter, string data)
        {
            try
            {
                string datavalue = data.Replace($"${BodyNumber}", "").Replace("\r", "");
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
                    if (datavalue == "0") 
                        CurrentCompoundCommandStatus = JelCommandStatus.InPause;
                    if (datavalue == "1") CurrentCompoundCommandStatus = JelCommandStatus.InExecution;
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
                if (command == "WHI")
                    MappingTopSlotPostion = Convert.ToSingle(datavalue);
                if (command == "WFC")
                    MappingSlotsNumber = Convert.ToInt32(datavalue);
                if (command == "WWN")
                    MappingMinDetectWidth = Convert.ToSingle(datavalue);
                if (command == "WWM")
                    MappingMaxDetectWidth = Convert.ToSingle(datavalue);
                if (command == "WWG")
                    MappingGateWidth = Convert.ToSingle(datavalue);
                if (command == "WEND")
                    MappingStopPostion = Convert.ToSingle(datavalue);
                if (command == "WSP")
                    MappingSpeed = Convert.ToSingle(datavalue);
                //if (command == "WFK")
                //{
                //    MappingWaferResult = datavalue;
                //    NotifySlotMapResult(CurrentInteractModule, datavalue.Replace(",", "").Replace("E", "?"));
                //}
                if (command == "WFW")
                    MappingWidthResult = datavalue;

                if(command == "WAS")
                {
                    switch(datavalue)
                    {
                        case "2":
                            Size = WaferSize.WS2;
                            break;
                        case "3":
                            Size = WaferSize.WS3;
                            break;
                        case "4":
                            Size = WaferSize.WS4;
                            break;
                        case "5":
                            Size = WaferSize.WS5;
                            break;
                        case "6":
                            Size = WaferSize.WS6;
                            break;
                        case "8":
                            Size = WaferSize.WS8;
                            break;
                        case "9":
                            Size = WaferSize.WS12;
                            break;
                        default:                        
                            Size = WaferSize.WS0;
                            break;
                    }                    
                }
                if(command == "WIS1")
                {
                    if (datavalue == "0")
                        IsGripperHoldWafer = false;
                    if (datavalue == "1")
                        IsGripperHoldWafer = true;
                }


            }
            catch (Exception ex)
            {
                LOG.Write($"Parse {command}.{parameter ?? parameter} data {data} error:" + ex.ToString());
                OnError($"Parse data error:{command}.{parameter ?? parameter} data {data}");
            }

        }
        private void ParseRobotStatus(string data)
        {
            if (data.Length < 2) return;
            YAxisAndThetaAxisStatus = (JelAxisStatus)Convert.ToInt16(data.ToArray()[0].ToString());
            XAxisStatus = (JelAxisStatus)Convert.ToInt16(data.ToArray()[1].ToString());
            if (YAxisAndThetaAxisStatus == JelAxisStatus.SensorError || YAxisAndThetaAxisStatus == JelAxisStatus.CommandError)
            {
                IsBusy = false;
                OnError($"YAxisAndThetaAxisStatus is {YAxisAndThetaAxisStatus}");               
            }
            if (XAxisStatus == JelAxisStatus.SensorError || XAxisStatus == JelAxisStatus.CommandError)
            {
                IsBusy = false;
                OnError($"XAxisStatus is {XAxisStatus}");                
            }


        }
        private void ParseRobotPostion(string axis, string data)
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



        public override bool IsNeedChangeWaferSize(WaferSize wz)
        {
            if(_diOcrOn200 == null || _diOcrOn200.SensorDI == null || _diOcrOn300 == null || _diOcrOn300.SensorDI == null)
            {
                return Size != wz;
            }
            if(wz == WaferSize.WS12)
            {
                if (_diOcrOn300.Value && !_diOcrOn200.Value && GetCurrentWaferSize() == wz)
                    return false;
                return true;
            }
            if (wz == WaferSize.WS8)
            {
                if (!_diOcrOn300.Value && _diOcrOn200.Value && GetCurrentWaferSize() == wz)
                    return false;
                return true;
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

        protected override bool fReset(object[] param)
        {
            if (!_connection.IsConnected)
            {
                _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
                _bodyNumber = SC.GetValue<int>($"{_scRoot}.{Name}.BodyNumber");
                PortName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");

                //TimelimitAlginerHome = SC.GetValue<int>($"{_scRoot}.{Name}.TimeLimitAlignerHome");

                _connection = new JelAlignerConnection(PortName);
                _connection.EnableLog(_enableLog);
                if (_connection.Connect())
                {

                    EV.PostInfoLog(Module, $"{Module}.{Name} connected");
                }
            }
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;
            
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            _connection.EnableLog(_enableLog);

            _trigRetryConnect.RST = true;

            _lstMoveHandler.Clear();
            _lstMonitorHandler.Clear();
            _connection.ForceClear();
            lock (_locker)
            {
                _lstMonitorHandler.AddLast(new JelAlignerRawCommandHandler(this, $"${BodyNumber}RD\r"));
                _lstMonitorHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMonitorHandler.AddLast(new JelAlignerReadHandler(this, "WIS1"));
                _lstMonitorHandler.AddLast(new JelAlignerReadHandler(this, "WAS"));
            }
            return true;
        }

        protected override bool fMonitorReset(object[] param)
        {
            _isAligned = false;
            return true;
            //if (XAxisStatus == JelAxisStatus.NormalEnd
            //    && (YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            //    && _lstMoveHandler.Count == 0
            //    && _lstMonitorHandler.Count == 0
            //    && !_connection.IsBusy)
            //{
            //    IsBusy = false;
            //    return true;
            //}
            //if (_lstMonitorHandler.Count == 0 && _lstMoveHandler.Count == 0 && !_connection.IsBusy)
            //    _connection.Execute(new JelAlignerReadHandler(this, ""));
            //return false;
        }

        protected override bool fStartInit(object[] param)
        {
            lock (_locker)
            {                
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "W0"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, "WIS1"));
                

                if (SC.ContainsItem($"{_scRoot}.{Name}.JelWaferType"))
                {
                    int wafertype = SC.GetValue<int>($"{_scRoot}.{Name}.JelWaferType");
                    _lstMoveHandler.AddLast(new JelAlignerSetHandler(this, "WAT", wafertype.ToString()));

                }                
                if(WaferManager.Instance.CheckHasWafer(RobotModuleName,0))
                {

                    string strpara;
                    switch (WaferManager.Instance.GetWaferSize(RobotModuleName, 0))
                    {
                        case WaferSize.WS2:
                        case WaferSize.WS3:
                        case WaferSize.WS4:
                        case WaferSize.WS5:
                        case WaferSize.WS6:
                        case WaferSize.WS8:
                            strpara = _currentSetWaferSize.ToString().Replace("WS", "");
                            if (_doOcrTo200 != null && _doOcrTo200.DoTrigger != null)
                                _doOcrTo200.SetTrigger(true, out _);
                            if (_doOcrTo300 != null && _doOcrTo300.DoTrigger != null)
                                _doOcrTo300.SetTrigger(false, out _);
                            break;
                        case WaferSize.WS12:
                            strpara = "9";
                            if (_doOcrTo200 != null && _doOcrTo200.DoTrigger != null)
                                _doOcrTo200.SetTrigger(false, out _);
                            if (_doOcrTo300 != null && _doOcrTo300.DoTrigger != null)
                                _doOcrTo300.SetTrigger(true, out _);
                            break;
                        default:
                            return false;
                    }
                     _lstMonitorHandler.AddLast(new JelAlignerMoveHandler(this, "WAS", strpara));                    
                }
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WMC"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, "WAS"));


            }
            _isAligned = false;
            _isOnHomedPostion = false;
            _dtActionStart = DateTime.Now;
            return true;
            

        }
        private DateTime _dtActionStart;
        protected override bool fMonitorInit(object[] param)
        {
            _isAligned = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds((double)TimelimitAlginerHome))
                OnError("Init timeout");
            if(_lstMoveHandler.Count == 0 
                && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
                && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            {
                    IsBusy = false;
                if(IsWaferPresent(0) && WaferManager.Instance.CheckNoWafer(RobotModuleName,0))
                {
                    WaferManager.Instance.CreateWafer(RobotModuleName, 0,
                         WaferStatus.Normal, GetCurrentWaferSize());
                }
                if (!IsWaferPresent(0) && WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                {
                    WaferManager.Instance.DeleteWafer(RobotModuleName, 0);
                }



                return true;                               
            }
            if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelAlignerReadHandler(this, ""));
            return base.fMonitorInit(param);
        }

        protected override bool fStartHome(object[] param)
        {
            lock (_locker)
            {
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "W0"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, "WIS1"));

                if (SC.ContainsItem($"{_scRoot}.{Name}.JelWaferType"))
                {
                    int wafertype = SC.GetValue<int>($"{_scRoot}.{Name}.JelWaferType");
                    _lstMoveHandler.AddLast(new JelAlignerSetHandler(this, "WAT", wafertype.ToString()));

                }
                if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                {

                    string strpara;
                    switch (WaferManager.Instance.GetWaferSize(RobotModuleName, 0))
                    {
                        case WaferSize.WS2:
                        case WaferSize.WS3:
                        case WaferSize.WS4:
                        case WaferSize.WS5:
                        case WaferSize.WS6:
                        case WaferSize.WS8:
                            strpara = _currentSetWaferSize.ToString().Replace("WS", "");
                            if (_doOcrTo200 != null && _doOcrTo200.DoTrigger != null)
                                _doOcrTo200.SetTrigger(true, out _);
                            if (_doOcrTo300 != null && _doOcrTo300.DoTrigger != null)
                                _doOcrTo300.SetTrigger(false, out _);
                            break;
                        case WaferSize.WS12:
                            strpara = "9";
                            if (_doOcrTo200 != null && _doOcrTo200.DoTrigger != null)
                                _doOcrTo200.SetTrigger(false, out _);
                            if (_doOcrTo300 != null && _doOcrTo300.DoTrigger != null)
                                _doOcrTo300.SetTrigger(true, out _);
                            break;
                        default:
                            return false;
                    }
                    _lstMonitorHandler.AddLast(new JelAlignerMoveHandler(this, "WAS", strpara));
                }
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WMC"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, "WAS"));

            }

             _isAligned = false;
            
            _dtActionStart = DateTime.Now;
            return true;
        }

        protected override bool fMonitorHome(object[] param)
        {
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds((double)TimelimitAlginerHome))
                OnError("Home timeout");
            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
                && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            {
                IsBusy = false;
                _isOnHomedPostion = true;
                return true;
            }
            if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelAlignerReadHandler(this, ""));            
            return base.fMonitorHome(param);
        }


        public override bool IsReady()
        {
           
            return AlignerState == AlignerStateEnum.Idle && !IsBusy;
        }

        public override bool IsWaferPresent(int slotindex)
        {
            if (_diWaferPresent != null)
                return _diWaferPresent.Value;
            return WaferManager.Instance.CheckHasWafer(RobotModuleName, slotindex);
        }
        public override bool IsNeedPrepareBeforePlaceWafer()
        {
            return !_isOnHomedPostion;
        }
        protected override bool fStartLiftup(object[] param)   //Ungrip
        {
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {

                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WU"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
            }
            return true;
        }

        protected override bool fMonitorLiftup(object[] param)
        {
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitForAlignWafer))
                OnError("Alignment timeout");

            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
                && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            {
                IsBusy = false;
                _isAligned = false;
                _isOnHomedPostion = false;
                return true;
            }
            if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelAlignerReadHandler(this, ""));    

            return base.fMonitorLiftup(param);
        }

        protected override bool fStartLiftdown(object[] param)
        {
            lock (_locker)
            {
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WD"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
            }
            return true;
        }
        protected override bool fMonitorLiftdown(object[] param)
        {
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitForAlignWafer))
                OnError("Alignment timeout");

            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
                && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            {
                IsBusy = false;
                _isAligned = true;
                _isOnHomedPostion = false;
                return true;
            }
            if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelAlignerReadHandler(this, ""));
            return base.fMonitorLiftdown(param);
        }

        protected override bool fStartAlign(object[] param)
        {
            WaferSize wz = WaferManager.Instance.GetWaferSize(RobotModuleName, 0);
            if(wz != GetCurrentWaferSize())
            {
                EV.PostAlarmLog("System", "Wafer size is not match, can't do alignment");
                return false;
            }


            double aligneangle = (double)param[0];

            while(aligneangle<0 || aligneangle>360)
            {
                if (aligneangle < 0)
                    aligneangle += 360;
                if (aligneangle > 360)
                    aligneangle -= 360;
            }
            //int speed = SC.GetValue<int>($"{_scRoot}.{Name}.AlignSpeed");

            int intangle = (int)(aligneangle * 20000/360);
            CurrentNotch = aligneangle;
            lock (_locker)
            {
                _lstMoveHandler.AddLast(new JelAlignerRawCommandHandler(this, $"${BodyNumber}RD\r"));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WUC"));   //Close grip
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                //_lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WA"));   //Move down
                //_lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));                
                _lstMoveHandler.AddLast(new JelAlignerSetHandler(this, "WOP",intangle.ToString()));
                _lstMoveHandler.AddLast(new JelAlignerRawCommandHandler(this, $"${BodyNumber}RD\r"));
                if (_isAligned && IsSupportAlignCompaudCommand)
                {
                    _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "G15"));    //Align to angle compaund command
                    
                }
                else
                    _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WA"));    //Align
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WDF"));     //Ungrip
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, "WIS1"));
            }
            _dtActionStart = DateTime.Now;
            return true;
        }     
        protected override bool fMonitorAligning(object[] param)
        {
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitForAlignWafer))
                OnError("Alignment timeout");          
            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
                && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            {
                IsBusy = false;
                _isAligned = true;
                _isOnHomedPostion = false;
                return true;
            }
            if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelAlignerReadHandler(this, ""));
            return base.fMonitorAligning(param);
        }

        protected override bool fStop(object[] param)
        {
            return true;
        }

        protected override bool FsmAbort(object[] param)
        {
            return true;
        }
        protected override bool fClear(object[] param)
        {
            return true;
        }
        protected override bool fStartReadData(object[] param)
        {
            return true;
        }
        protected override bool fStartSetParameters(object[] param)
        {
            _dtActionStart = DateTime.Now;
            _currentSetParameter = param[0].ToString();
            switch(_currentSetParameter)
            {
                case "WaferSize":
                    if((_doOcrTo200 != null||_doOcrTo300!=null)&& GetWaferState() != RobotArmWaferStateEnum.Absent)
                    {
                        EV.PostAlarmLog("System","Can't set wafersize when wafer is not absent");
                        return false;
                    }
                    _currentSetWaferSize = (WaferSize)param[1];

                    if(WaferManager.Instance.CheckHasWafer(RobotModuleName,0))
                    {
                        WaferManager.Instance.UpdateWaferSize(RobotModuleName, 0, _currentSetWaferSize);
                    }
                    string strpara;
                    switch(_currentSetWaferSize)
                    {
                        case WaferSize.WS2:
                        case WaferSize.WS3:
                        case WaferSize.WS4:
                        case WaferSize.WS5:
                        case WaferSize.WS6:
                        case WaferSize.WS8:
                            strpara = _currentSetWaferSize.ToString().Replace("WS", "");
                            if (_doOcrTo200 != null && _doOcrTo200.DoTrigger!=null)
                                _doOcrTo200.SetTrigger(true, out _);
                            if (_doOcrTo300 != null && _doOcrTo300.DoTrigger != null)
                                _doOcrTo300.SetTrigger(false, out _);
                            break;
                        case WaferSize.WS12:
                            strpara = "9";
                            if (_doOcrTo200 != null && _doOcrTo200.DoTrigger != null)
                                _doOcrTo200.SetTrigger(false, out _);
                            if (_doOcrTo300 != null && _doOcrTo300.DoTrigger != null)
                                _doOcrTo300.SetTrigger(true, out _);
                            break;
                        default:
                            return false;
                    }
                    lock (_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelAlignerMoveHandler(this, "WAS", strpara));
                        _lstMonitorHandler.AddLast(new JelAlignerReadHandler(this, "WAS"));
                        _lstMonitorHandler.AddLast(new JelAlignerMoveHandler(this, "WMC"));
                    }
                    break;
            }
            return true;
        }
        private string _currentSetParameter;
        private WaferSize _currentSetWaferSize = WaferSize.WS0;
        protected override bool fMonitorSetParamter(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitForAlignWafer))
                OnError("Set parameter timeout");
            switch(_currentSetParameter)
            {
                case "WaferSize":
                    if(_lstMonitorHandler.Count == 0 && _lstMoveHandler.Count == 0 && !_connection.IsBusy)
                    {
                        if (_currentSetWaferSize != Size)
                        {
                            OnError($"Fail to set wafer size,set:{_currentSetWaferSize},return:{Size}");
                        }
                        if (_currentSetWaferSize == WaferSize.WS12)
                        {
                            if (_diOcrOn200 != null && _diOcrOn200.SensorDI !=null && _diOcrOn200.Value)
                                return false;
                            if (_diOcrOn300 != null && _diOcrOn300.SensorDI != null && !_diOcrOn300.Value)
                                return false;
                            return true;
                        }
                        if (_currentSetWaferSize == WaferSize.WS8)
                        {
                            if (_diOcrOn200 != null && _diOcrOn200.SensorDI != null && !_diOcrOn200.Value)
                                return false;
                            if (_diOcrOn300 != null && _diOcrOn300.SensorDI != null && _diOcrOn300.Value)
                                return false;
                            return true;
                        }
                        return true;
                    }
                    break;
            }
            return base.fMonitorSetParamter(param);
        }
        protected override bool fStartUnGrip(object[] param)
        {
            lock (_locker)
            {
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WDF"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
            }
            _diStartUngrip = DateTime.Now;
            return true;
        }
        private DateTime _diStartUngrip;
        protected override bool fMonitorUnGrip(object[] param)
        {
            if (DateTime.Now - _diStartUngrip > TimeSpan.FromSeconds(10))
                OnError("UnGrip timeout");
            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
                && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            {
                IsBusy = false;
                return true;
            }
            if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelAlignerReadHandler(this, ""));            
            return base.fMonitorUnGrip(param);
        }       
        protected override bool fStartGrip(object[] param)
        {
            lock (_locker)
            {
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WUC"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
            }
            _diStartUngrip = DateTime.Now;
            return true;
        }
        protected override bool fMonitorGrip(object[] param)
        {
            if (DateTime.Now - _diStartUngrip > TimeSpan.FromSeconds(10))
                OnError("Grip timeout");
            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy )
            {
                if (XAxisStatus == JelAxisStatus.NormalEnd && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
                {
                    IsBusy = false;
                    return true;
                }
                else
                {
                    _connection.Execute(new JelAlignerReadHandler(this, ""));
                }
            }
           
                
            return base.fMonitorGrip(param);
        }
        protected override bool fResetToReady(object[] param)
        {
            return true;
        }

        protected override bool fError(object[] param)
        {
            return true;
        }

        public override void OnError(string errortext)
        {
            _isAligned = false;
            _isOnHomedPostion = false;
            _lstMonitorHandler.Clear();
            _lstMoveHandler.Clear();
            base.OnError(errortext);
        }

        public override bool IsNeedRelease
        {
            get
            {
                return IsGripperHoldWafer;
            }
        }

        protected override bool fStartPrepareAccept(object[] param)
        {
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {
                _lstMonitorHandler.AddLast(new JelAlignerRawCommandHandler(this, $"${BodyNumber}RD\r"));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WMC"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
            }
            return true;
        }


        protected override bool fMonitorPrepareAccept(object[] param)
        {
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds((double)TimelimitAlginerHome))
                OnError("Home timeout");
            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
                && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            {
                IsBusy = false;
                _isAligned = false;
                _isOnHomedPostion = true;
                return true;
            }
            if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelAlignerReadHandler(this, ""));

            return base.fMonitorPrepareAccept(param);
        }




    }
}
