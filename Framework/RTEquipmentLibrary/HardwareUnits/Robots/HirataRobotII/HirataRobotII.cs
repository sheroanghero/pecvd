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
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.WalkingAixs;
using Aitex.Core.RT.Routine;
using MECF.Framework.Common.CommonData;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HirataRobotII
{
    public class HirataRobotII : RobotBaseDevice, IConnection
    {
        public bool isSimulatorMode
        {
            get
            {
                return SC.ContainsItem("System.IsSimulatorMode") ? SC.GetValue<bool>("System.IsSimulatorMode") : false;
            }
        }
        public string Address { get { return _address; } }

        public string PortName;
        public bool IsConnected { get; }
        public bool Connect()

        {
            return _connection.Connect();
        }

        public bool Disconnect()
        {
            return _connection.Disconnect();
        }


        private HirataRobotIIConnection _connection;
        public HirataRobotIIConnection Connection
        {
            get { return _connection; }
        }

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private R_TRIG _trigActionDone = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        public List<IOResponse> IOResponseList { get; set; } = new List<IOResponse>();


        private object _locker = new object();

        private bool _enableLog;

        public int Axis { get; private set; }

        private float _armPitch { get; set; }

        private string _scRoot;
        public bool DIReadValue { get; set; }

        private bool[,] di_Values { get; set; } = new bool[8,8];
        
        protected WalkingAxisBaseDevice m_WalkingAxis { get; set; }

        public HirataRobotII(string module, string name, string scRoot, WalkingAxisBaseDevice walkaxis=null) : base(module, name)
        {

            
            _scRoot = scRoot;

            m_WalkingAxis = walkaxis;

            //base.Initialize();
            ResetPropertiesAndResponses();
            RegisterSpecialData();

            _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            Axis = SC.GetValue<int>($"{_scRoot}.{Name}.RobotAxis");
            _armPitch = SC.GetValue<int>($"{_scRoot}.{Name}.ArmPitch") / 100;
            PortName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");
            _connection = new HirataRobotIIConnection(PortName,this);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {                
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(50, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            //_address = SC.GetStringValue($"{_scRoot}.{Name}.DeviceAddress");




        }

        private void RegisterSpecialData()
        {
            DATA.Subscribe($"{Module}.{Name}.CurrentExtParaNO", () => CurrentReadExtParaNO);
            DATA.Subscribe($"{Module}.{Name}.CurrentExtParaValue", () => CurrentReadExtParaValue);
            DATA.Subscribe($"{Module}.{Name}.RobotIsOnline", () => IsOnLine);
            DATA.Subscribe($"{Module}.{Name}.RobotIsManual", () => IsManual);
            DATA.Subscribe($"{Module}.{Name}.RobotIsAuto", () => IsAuto);

            DATA.Subscribe($"{Module}.{Name}.RobotStopSignal", () => IsStop);
            DATA.Subscribe($"{Module}.{Name}.RobotEMSignal", () => IsES);

            DATA.Subscribe($"{Module}.{Name}.RobotZaxisSaftyZone", () => IsZaxisSafeZone);
            DATA.Subscribe($"{Module}.{Name}.RobotPositioningCompleted", () => MovingCompleted);
            DATA.Subscribe($"{Module}.{Name}.RobotACalCompleted", () => ACalCompleted);
            DATA.Subscribe($"{Module}.{Name}.RobotExecutionCompleted", () => ExecutionComplete);
            
            DATA.Subscribe($"{Module}.{Name}.RobotReadXPosition", () => ReadXPosition);
            DATA.Subscribe($"{Module}.{Name}.RobotReadYPosition", () => ReadYPosition);
            DATA.Subscribe($"{Module}.{Name}.RobotReadZPosition", () => ReadZPosition);
            DATA.Subscribe($"{Module}.{Name}.RobotReadWPosition", () => ReadWPosition);
            DATA.Subscribe($"{Module}.{Name}.RobotMdata", () => MData);
            DATA.Subscribe($"{Module}.{Name}.RobotFCode", () => FCode);
            DATA.Subscribe($"{Module}.{Name}.RobotSCode", () => SCode);

            DATA.Subscribe($"{Module}.{Name}.ExtPara_MapWaferCount", () => ExtPara_MapWaferCount);
            DATA.Subscribe($"{Module}.{Name}.ExtPara_WaferThickness", () => ExtPara_WaferThickness);
            DATA.Subscribe($"{Module}.{Name}.ExtPara_PositionRange", () => ExtPara_PositionRange);
            DATA.Subscribe($"{Module}.{Name}.ExtPara_Pitch", () => ExtPara_Pitch);
            DATA.Subscribe($"{Module}.{Name}.ExtPara_WaferMinThickness", () => ExtPara_WaferMinThickness);
            DATA.Subscribe($"{Module}.{Name}.ExtPara_Filter", () => ExtPara_Filter);


            DATA.Subscribe($"{Name}.CurrentExtParaNO", () => CurrentReadExtParaNO);
            DATA.Subscribe($"{Name}.CurrentExtParaValue", () => CurrentReadExtParaValue);
            DATA.Subscribe($"{Name}.RobotIsOnline", () => IsOnLine);
            DATA.Subscribe($"{Name}.RobotIsManual", () => IsManual);
            DATA.Subscribe($"{Name}.RobotIsAuto", () => IsAuto);

            DATA.Subscribe($"{Name}.RobotStopSignal", () => IsStop);
            DATA.Subscribe($"{Name}.RobotEMSignal", () => IsES);

            DATA.Subscribe($"{Name}.RobotZaxisSaftyZone", () => IsZaxisSafeZone);
            DATA.Subscribe($"{Name}.RobotPositioningCompleted", () => MovingCompleted);
            DATA.Subscribe($"{Name}.RobotACalCompleted", () => ACalCompleted);
            DATA.Subscribe($"{Name}.RobotExecutionCompleted", () => ExecutionComplete);

            DATA.Subscribe($"{Name}.RobotReadXPosition", () => ReadXPosition);
            DATA.Subscribe($"{Name}.RobotReadYPosition", () => ReadYPosition);
            DATA.Subscribe($"{Name}.RobotReadZPosition", () => ReadZPosition);
            DATA.Subscribe($"{Name}.RobotReadWPosition", () => ReadWPosition);
            DATA.Subscribe($"{Name}.RobotMdata", () => MData);
            DATA.Subscribe($"{Name}.RobotFCode", () => FCode);
            DATA.Subscribe($"{Name}.RobotSCode", () => SCode);

            DATA.Subscribe($"{Name}.ExtPara_MapWaferCount", () => ExtPara_MapWaferCount);
            DATA.Subscribe($"{Name}.ExtPara_WaferThickness", () => ExtPara_WaferThickness);
            DATA.Subscribe($"{Name}.ExtPara_PositionRange", () => ExtPara_PositionRange);
            DATA.Subscribe($"{Name}.ExtPara_Pitch", () => ExtPara_Pitch);
            DATA.Subscribe($"{Name}.ExtPara_WaferMinThickness", () => ExtPara_WaferMinThickness);
            DATA.Subscribe($"{Name}.ExtPara_Filter", () => ExtPara_Filter);

        }

        protected override bool Init()
        {
            return true;
        }
        private void ResetPropertiesAndResponses()
        {
            Connected = true;

            Error = null;
            RobotStatus = null;
            IsOnLine = false;
            IsManual = false;
            IsES = false;
            MovingCompleted = false;
            ACalCompleted = false;
            AddressOutSide = null;
            PositionOutSide = null;
            EmergencyStop = null;
            XLowerSide = null;
            XUpperSide = null;
            YLowerSide = null;
            YUpperSide = null;
            ZLowerSide = null;
            ZUpperSide = null;

            foreach (var ioResponse in IOResponseList)
            {
                ioResponse.ResonseContent = null;
                ioResponse.ResonseRecievedTime = DateTime.Now;
            }
            
        }

        public override bool IsReady()
        {
            return (!_connection.IsBusy && _lstHandler.Count == 0 && IsOnLine && ACalCompleted &&
                MovingCompleted&& !_connection.IsCommunicationError && !IsBusy && RobotState == RobotStateEnum.Idle);
        }

        private string _address;
        





        private bool OnTimer()
        {
            try
            {
                //return true;               

                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstHandler.Clear();
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
                            //_lstHandler.AddLast(new RobotHirataR4QueryPinHandler(this, _deviceAddress));
                            //_lstHandler.AddLast(new RobotHirataR4SetCommModeHandler(this, _deviceAddress, EnumRfPowerCommunicationMode.Host));
                        }
                    }
                    return true;
                }
               
                HandlerBase handler = null;
                
                lock (_locker)
                {
                    if (_lstHandler.Count > 0)
                    {
                        if (!_connection.IsBusy)
                        {
                            if (IsOnLine && ACalCompleted && MovingCompleted)
                            {
                                handler = _lstHandler.First.Value;
                                _lstHandler.RemoveFirst();
                            }
                            else
                            {
                                Thread.Sleep(100);
                                handler = new HirataRobotIIMonitorRobotStatusHandler(this);
                            }
                            _connection.Execute(handler);
                        }
                        
                    }
                    if(_connection.IsBusy)                   
                    {
                        
                            _connection.MonitorTimeout();

                            _trigCommunicationError.CLK = _connection.IsCommunicationError;
                            if (_trigCommunicationError.Q)
                            {
                                _lstHandler.Clear();
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



        public override void Monitor()
        {
            try
            {
               
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }



        public override void Reset()
        {
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            _connection.EnableLog(_enableLog);

            _trigRetryConnect.RST = true;

            //base.Reset();
        }



        #region Command Functions
        public void PerformRawCommand(string command, string comandArgument)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HirataRobotIIRawCommandHandler(this, command, comandArgument));
            }
        }

        public void PerformRawCommand(string command)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HirataRobotIIRawCommandHandler(this, command));
            }
        }



        public bool WritePosition(int address, float x, float y, float z, float w,
            string Mdata, string Fcode, string Scode)
        {
            int intFcode,intScode;
            string strMdata;
            if (!int.TryParse(Fcode, out intFcode)) return false;
            if (!int.TryParse(Scode, out intScode)) return false;

            if (intFcode < 0 || intFcode > 99 || intScode < 0 || intScode > 99)
            {
                EV.PostAlarmLog("Robot", "Robot postion data format is not correct");
                return false;                
            }
            if (!int.TryParse(Mdata, out _)) strMdata = "??";

            else strMdata = (Convert.ToInt32(Mdata) < 0 || Convert.ToInt32(Mdata) > 99) ? 
                    "??" : Mdata.ToString().PadLeft(2, '0');          

            lock (_locker)
            {
                _lstHandler.AddLast(new HirataRobotIIWritePositionHandler(this, address, x, y, z, w, 0, 0, 
                    "R", strMdata, Fcode.ToString().PadLeft(2, '0'), 
                    Scode.ToString().PadLeft(2, '0')));
            }
            return true;
        }


        protected override bool fStop(object[] param)
        {
            MoveStop();
            return true;
        }

        public void MoveStop()
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                _connection.ForceClear();
                _connection.Execute(new HirataRobotIISimpleActionHandler(this, "GD"));
                _connection.ForceClear();
            }
        }
        public void MoveReset()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HirataRobotIISimpleActionHandler(this, "GE"));
                _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
            }
        }
        public void ErrorClear()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HirataRobotIISimpleActionHandler(this, "CL"));
                _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
            }
        }      
       
        public void ReadPosition(int address)
        {
           
            _lstHandler.AddLast(new HirataRobotIIReadRobotPositionHandler(this, address));
               
        }
        
        #endregion

        #region Properties
        public bool Connected { get; private set; }
        public string Error { get; private set; }
        public string RobotStatus { get; private set; }
        public bool IsOnLine { get; private set; }
        public bool IsManual { get; private set; }
        public bool IsAuto { get; private set; }

        public bool IsStop { get; private set; }
        public bool IsES { get; private set; }

        public bool IsZaxisSafeZone { get; private set; }
        public bool MovingCompleted { get; private set; }
        public bool ACalCompleted { get; private set; }

        public bool ExecutionComplete { get; private set; }
        public string AddressOutSide { get; private set; }
        public string PositionOutSide { get; private set; }
        public string EmergencyStop { get; private set; }
        public string XLowerSide { get; private set; }
        public string XUpperSide { get; private set; }
        public string YLowerSide { get; private set; }
        public string YUpperSide { get; private set; }
        public string ZLowerSide { get; private set; }
        public string ZUpperSide { get; private set; }
        public string ReadRobotPositonData { get; private set; }
        public float ReadXPosition { get; private set; }
        public float ReadYPosition { get; private set; }
        public float ReadZPosition { get; private set; }
        public float ReadWPosition { get; private set; }
        public float ReadRPosition { get; private set; }
        public float ReadCPosition { get; private set; }
        public string RobotPosture { get; private set; }
        public string MData { get; private set; }
        public string FCode { get; private set; }
        public string SCode { get; private set; }

        public string ExtPara_MapWaferCount { get; private set; }
        public string ExtPara_WaferThickness { get; private set; }
        public string ExtPara_PositionRange { get; private set; }
        public string ExtPara_Pitch { get; private set; }
        public string ExtPara_WaferMinThickness { get; private set; }
        public string ExtPara_Filter { get; private set; }
        #endregion
        public string MapExistInfor { get; private set; }
        public string MapNGInfor { get; private set; }

        public string CurrentReadExtParaNO { get; private set; }
        public string CurrentReadExtParaValue { get; private set; }

        private ModuleName _currentMotionStation;
        private int _currentMotionSlot;
        private RobotArmEnum _currentMotionArm;
        #region Note Functions
        private R_TRIG _trigWarningMessage = new R_TRIG();

        public void NoteRobotDIByte(string data, int offset)
        {
            int tempvalue;
            if (!int.TryParse(data.Replace(Encoding.ASCII.GetString(new byte[] { 0x3}),""), out tempvalue)) return;
            di_Values[offset, 0] = ((tempvalue & 0x1) == 0x1);
            di_Values[offset, 1] = ((tempvalue & 0x2) == 0x2);
            di_Values[offset, 2] = ((tempvalue & 0x4) == 0x4);
            di_Values[offset, 3] = ((tempvalue & 0x8) == 0x8);
            di_Values[offset, 4] = ((tempvalue & 0x10) == 0x10);
            di_Values[offset, 5] = ((tempvalue & 0x10) == 0x20);
            di_Values[offset, 6] = ((tempvalue & 0x10) == 0x40);
            di_Values[offset, 7] = ((tempvalue & 0x10) == 0x80);
        }
        public void NoteError(string errorData)
        {
            if (errorData != null)
            {
                EV.PostAlarmLog("Robot", $"Robot occurred error:{errorData}");
                Error = errorData;
                ParseErrorData(errorData);
                
            }
            else
            {
                Error = null;
            }
        }
        private void ParseErrorData(string errorData)
        {
            errorData = errorData.Remove(0, 1);
            ParseStatusData(errorData);
            ErrorCode = errorData.Remove(0, 4);

            var statusArray = errorData.ToArray();
            char E1 = statusArray[5];
            char E2 = statusArray[4];
            char XErr = statusArray[6];
            char YErr = statusArray[7];
            char ZErr = statusArray[8];
            char WErr = statusArray[9];
            //char RErr = statusArray[10];
            //char CErr = statusArray[11];

            if (E2 == '0' && E1 == '9')
            {
                if (XErr == '1')
                {
                    XLowerSide = "true";
                }
                else if (XErr == '2')
                {
                    XUpperSide = "true";
                }
                if (YErr == '1')
                {
                    YLowerSide = "true";
                }
                else if (YErr == '2')
                {
                    YUpperSide = "true";
                }
                if (ZErr == '1')
                {
                    ZLowerSide = "true";
                }
                else if (ZErr == '2')
                {
                    ZUpperSide = "true";
                }
            }
            else if (E2 == '1' && E1 == '0')
            {
                EmergencyStop = "true";
            }
            else if (E2 == '2' && E1 == '0')
            {

            }
            else if (E2 == '3' && E1 == '0')
            {
                AddressOutSide = "true";
            }
            else if (E2 == '3' && E1 == '1')
            {

            }
            else if (E2 == '4' && E1 == '0')
            {
                PositionOutSide = "true";
            }
            OnError($"ErrorCode:{ErrorCode}");


        }
        public override void OnError(string errortext)
        {
            if (RobotState != RobotStateEnum.Error)
            {
                _lstHandler.Clear();
                _connection.ForceClear();
                base.OnError(errortext);
            }
        }
        public void NoteRobotStatus(string statusData)
        {
            if (statusData != null)
            {
                RobotStatus = statusData;
                ParseStatusData(statusData);
            }
            else
            {
                Error = null;
            }
        }
        private void ParseStatusData(string statusData)
        {
            var statusArray = statusData.ToArray();
            int S1 = statusArray[3] - '0';
            int S2 = statusArray[2] - '0';
            int S3 = statusArray[1] - '0';
            int S4 = statusArray[0] - '0';

            IsOnLine = ((S1 & 0x1)==0x1);
            IsManual = ((S1 & 0x2) ==0x2);
            IsAuto = ((S1 & 0x4) == 0x4);
            IsStop = ((S2 & 1)==1);
            IsES = (S2 & 2)==2;
            IsZaxisSafeZone = (S3 & 1) == 1;
            MovingCompleted = (S3 & 2)==2;
            ACalCompleted = (S3 & 4)==4;

            ExecutionComplete = (S4 & 4) != 4;
        }
        internal void NoteRobotPositon(string positionData)
        {
            ReadRobotPositonData = positionData;
            ParseRobotPositon(positionData);
        }

        //"123.45-123.45 123.45 123.45-123.45 123.45 R 0 1 90 0" "-123.45-123.45 123.45 123.45-123.45 123.45 R 0 1 90 0"
        private void ParseRobotPositon(string statusData)
        {
            string tempStr = statusData.Replace("-", " -").Replace("R", " R").Replace("L", " L");
            string[] strList = tempStr.Split(' ');
            try
            {
                LOG.Write($"Start to parse {Axis} axis robot position data:{statusData}.");
                
                
                //var statusArray = statusData.ToArray();
                //int segmentStartIndex = 0;
                //for (int index = 0; index < statusArray.Length; index++)
                //{
                //    if ((statusArray[index] == '-' || statusArray[index] == ' ') && index > 0)
                //    {
                //        strList.Add(statusData.Substring(segmentStartIndex, index - segmentStartIndex).Trim());
                //        segmentStartIndex = index;
                //    }
                //}
                //strList.Add(statusData.Substring(segmentStartIndex, statusArray.Length - segmentStartIndex).Trim());

                if (Axis == 6)
                {
                    float tempvalue;
                    if (float.TryParse(strList[0], out tempvalue))
                        ReadXPosition = tempvalue;
                    if (float.TryParse(strList[1], out tempvalue))
                        ReadYPosition = tempvalue;
                    if (float.TryParse(strList[2], out tempvalue))
                        ReadZPosition = tempvalue;
                    if (float.TryParse(strList[3], out tempvalue))
                        ReadWPosition = tempvalue;
                    if (float.TryParse(strList[4], out tempvalue))
                        ReadRPosition = tempvalue;
                    if (float.TryParse(strList[5], out tempvalue))
                        ReadCPosition = tempvalue;

                    RobotPosture = strList[6];
                    MData = strList[8];
                    FCode = strList[9];
                    SCode = strList[10];
                }
                else
                {
                    float tempvalue;
                    if (float.TryParse(strList[0], out tempvalue))
                        ReadXPosition = tempvalue;
                    if (float.TryParse(strList[1], out tempvalue))
                        ReadYPosition = tempvalue;
                    if (float.TryParse(strList[2], out tempvalue))
                        ReadZPosition = tempvalue;
                    if (float.TryParse(strList[3], out tempvalue))
                        ReadWPosition = tempvalue;

                    RobotPosture = strList[4];
                    MData = strList[6];
                    FCode = strList[7];
                    SCode = strList[8];
                }
            }
            catch(Exception ex)
            {
                LOG.Write(ex);
                LOG.Write($"Failed to parse {Axis} axis robot position data:{tempStr} with length:{strList.Length}.");
            }


        }
        public void ParseReadData(string cmd,string paraname,string data)
        {
            switch(cmd)
            {
                case "LE":
                    CurrentReadExtParaNO = paraname;
                    CurrentReadExtParaValue = data;
                    if (paraname == "100") MapExistInfor = ParSlotMapData(data);
                    if (paraname == "101") MapNGInfor = ParSlotMapData(data);
                    int intParaNO;
                    if (!int.TryParse(paraname, out intParaNO)) break;

                    if(intParaNO >= 112 && intParaNO <=154)
                    {
                        if ((intParaNO - 112) % 5 == 0) ExtPara_MapWaferCount = data;
                    }
                    if (intParaNO >= 610 && intParaNO <= 654)
                    {
                        if ((intParaNO - 610) % 5 == 0) ExtPara_WaferThickness = data;
                        if ((intParaNO - 611) % 5 == 0) ExtPara_PositionRange = data;
                        if ((intParaNO - 612) % 5 == 0) ExtPara_Pitch = data;
                        if ((intParaNO - 613) % 5 == 0) ExtPara_WaferMinThickness = data;
                        if ((intParaNO - 614) % 5 == 0) ExtPara_Filter = data;
                    }


                    break;
                case "LR":
                    if (paraname == null) ParseCurrentPostion(data);
                    break;

                default:
                    break;

            }
        }
        private void ParseCurrentPostion(string data)
        {
            List<string> strList = new List<string>();
            var statusArray = data.ToArray();
            int segmentStartIndex = 0;
            for (int index = 0; index < statusArray.Length; index++)
            {
                if ((statusArray[index] == '-' || statusArray[index] == ' ') && index > 0)
                {
                    strList.Add(data.Substring(segmentStartIndex, index - segmentStartIndex).Trim());
                    segmentStartIndex = index;
                }
            }
            strList.Add(data.Substring(segmentStartIndex, statusArray.Length - segmentStartIndex).Trim());

            if (Axis == 6)
            {
                if(strList.Count <6)
                {
                    EV.PostAlarmLog("Robot", $"Received wrong position data:{data}");
                    return;
                }
                float tempvalue;
                if (float.TryParse(strList[0], out tempvalue))
                    PositionAxis1 = tempvalue;
                if (float.TryParse(strList[1], out tempvalue))
                    PositionAxis2 = tempvalue;
                if (float.TryParse(strList[2], out tempvalue))
                    PositionAxis3 = tempvalue;
                if (float.TryParse(strList[3], out tempvalue))
                    PositionAxis4 = tempvalue;
                if (float.TryParse(strList[4], out tempvalue))
                    PositionAxis5 = tempvalue;
                if (float.TryParse(strList[5], out tempvalue))
                    PositionAxis6 = tempvalue;
            }
            else
            {
                if (strList.Count < 4)
                {
                    EV.PostAlarmLog("Robot", $"Received wrong position data:{data}");
                    return;
                }
                float tempvalue;
                if (float.TryParse(strList[0], out tempvalue))
                    PositionAxis1 = tempvalue;
                if (float.TryParse(strList[1], out tempvalue))
                    PositionAxis2 = tempvalue;
                if (float.TryParse(strList[2], out tempvalue))
                    PositionAxis3 = tempvalue;
                if (float.TryParse(strList[3], out tempvalue))
                    PositionAxis4 = tempvalue;           
            }
        }
        private string ParSlotMapData(string data)
        {
            string ret = "";
            int ivalue;
            string svalut = data.Replace(Encoding.ASCII.GetString(new byte[] { 0x3}), "");
            if (!int.TryParse(svalut, out ivalue)) return "0000000000000000000000000";
            for(int i=0;i<25;i++)
            {
                if ((ivalue & (int)Math.Pow(2, i)) == (int)Math.Pow(2, i))
                    ret = ret + "1";
                else
                    ret = ret + "0";
            }
            return ret;
        }
        
      
        internal void NoteRobotConnected(bool connected)
        {
            Connected = connected;
        }
        internal void NoteRawCommandInfo(string command, string data)
        {
            var curIOResponse = IOResponseList.Find(res => res.SourceCommandName == command);
            if (curIOResponse != null)
            {
                IOResponseList.Remove(curIOResponse);
            }
            IOResponseList.Add(new IOResponse() { SourceCommandName = command, ResonseContent = data, ResonseRecievedTime = DateTime.Now });
        }
        #endregion


        public override bool SetWaferSize(RobotArmEnum arm, WaferSize size)
        {
            Size = size;
            if(arm == RobotArmEnum.Both)
            {
                if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                    WaferManager.Instance.UpdateWaferSize(RobotModuleName, 0, size);
                if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 1))
                    WaferManager.Instance.UpdateWaferSize(RobotModuleName, 1, size);
                return true;

            }
            if(arm == RobotArmEnum.Lower || arm== RobotArmEnum.Blade1)
            {
                if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                    WaferManager.Instance.UpdateWaferSize(RobotModuleName, 0, size);
                return true;
            }
            if(arm == RobotArmEnum.Upper || arm == RobotArmEnum.Blade2)
            {
                if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 1))
                    WaferManager.Instance.UpdateWaferSize(RobotModuleName, 1, size);
                return true;
            }
            return true;

            
        }
        protected override bool fStartTransferWafer(object[] param)
        {
            return true;
        }

        protected override bool fStartUnGrip(object[] param)
        {

            _dtActionStart = DateTime.Now;
            RobotArmEnum arm = (RobotArmEnum)param[0];
            if (arm == RobotArmEnum.Lower)
            {
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, true));
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, false));
            }
            if(arm == RobotArmEnum.Upper)
            {
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, true));
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, false));
            }
            if(arm == RobotArmEnum.Both)
            {
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, true));
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, false));
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, true));
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, false));

            }
            _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
            return true;
        }

        protected override bool fMonitorUnGrip(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("UnGripTimeout.");
                return true;
            }
            if (_lstHandler.Count == 0 && (!_connection.IsBusy) && IsOnLine && ACalCompleted && MovingCompleted)
            {
                EV.PostInfoLog("Robot", $"{RobotModuleName} ungrip wafer successfully.");
                return true;
            }
            return false;
        }

        protected override bool fMonitorGrip(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("GripTimeout.");
                return true;
            }
            if (_lstHandler.Count == 0 && (!_connection.IsBusy) && IsOnLine && ACalCompleted && MovingCompleted)
            {
                EV.PostInfoLog("Robot", $"{RobotModuleName} grip wafer successfully.");
                return true;
            }
            return false;
        }

        protected override bool fStartGrip(object[] param)
        {

            _dtActionStart = DateTime.Now;
            RobotArmEnum arm = (RobotArmEnum)param[0];
            if (arm == RobotArmEnum.Lower)
            {
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, true));
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, false));
            }
            if (arm == RobotArmEnum.Upper)
            {
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, true));
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, false));
            }
            if (arm == RobotArmEnum.Both)
            {
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, true));
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, false));
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, true));
                _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, false));

            }
            _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
            return true;
        }

        protected override bool fStartGoTo(object[] param)
        {

            _dtActionStart = DateTime.Now;
            try
            {
                if (param.Length == 1)
                {
                    int address = Convert.ToInt16(param[0]);
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HirataRobotIIMoveToPositionHandler(this, address));
                    }
                }
                if (param.Length >= 8)
                {
                    RobotArmEnum arm = (RobotArmEnum)param[0];
                    string station = param[1].ToString();
                    int slotindex = (int)param[2];
                    RobotPostionEnum pos = (RobotPostionEnum)param[3];
                    float xoffset = (float)param[4];
                    float yoffset = (float)param[5];
                    float zoffset = (float)param[6];
                    float woffset = (float)param[7];

                    ModuleName stationname = (ModuleName)Enum.Parse(typeof(ModuleName), station);
                    WaferSize wz = WaferManager.Instance.GetWaferSize(stationname, slotindex);

                    int address = GetBaseAddress(stationname, wz);

                    float waferpitch = GetWaferPitch(stationname, wz);
                    float insertdistance = GetInsertDistance(stationname, wz);
                    float slowupdistance = GetSlowUpDistance(stationname, wz);

                    zoffset += waferpitch * slotindex;
                    if (arm == RobotArmEnum.Lower || arm == RobotArmEnum.Both)
                        zoffset += _armPitch;

                    if (pos == RobotPostionEnum.PickReady)
                    {
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataRobotIIMoveToPositionWithDeviationHandler(this, "A",
                            address, xoffset, yoffset, zoffset, woffset));
                        }
                    }
                    else if (pos == RobotPostionEnum.PlaceReady)
                    {
                        zoffset += slowupdistance;
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataRobotIIMoveToPositionWithDeviationHandler(this, "A",
                            address, xoffset, yoffset, zoffset, woffset));
                        }
                    }
                    else
                    {
                        EV.PostAlarmLog("Robot", $"Robot doesn't support goto {pos} now.");
                        return false;
                    }
                }

                if (param.Length == 6|| param.Length == 7)
                {
                    string motion = param[0].ToString();
                    string address = param[1].ToString();

                    float x, y, z, w;
                    if (!float.TryParse(param[2].ToString(), out x)) return false;
                    if (!float.TryParse(param[3].ToString(), out y)) return false;
                    if (!float.TryParse(param[4].ToString(), out z)) return false;
                    if (!float.TryParse(param[5].ToString(), out w)) return false;

                    if (param.Length >= 7 && param[6].ToString() == "-")
                    {
                        x = x * -1;
                        y = y * -1;
                        z = z * -1;
                        w = w * -1;
                    }
                    lock (_locker)
                    {
                        if (address == "*")

                            _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, motion, x, y, z, w));

                        else
                        {
                            int intaddress;
                            if (!int.TryParse(address, out intaddress)) return false;
                            _lstHandler.AddLast(new HirataRobotIIMoveToPositionWithDeviationHandler(this, motion, intaddress, x, y, z, w));
                        }
                        ReadCurrentPostion();

                    }
                }

            }
            catch(Exception ex)
            {
                EV.PostAlarmLog("Robot","Robot Goto execution failed.");
                LOG.Write(ex);
                return false;
            }
             
            return true;
        }

        protected override bool fMonitorGoTo(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("GripTimeout.");
                return true;
            }
            if (_lstHandler.Count == 0 && (!_connection.IsBusy) && IsOnLine && ACalCompleted && MovingCompleted)
            {
                EV.PostInfoLog("Robot", $"{RobotModuleName} grip wafer successfully.");
                return true;
            }
            return false;
        }





        private int _currentMapThicknessIndex;
        protected override bool fStartMapWafer(object[] param)
        {
            ResetRoutine();
            _dtActionStart = DateTime.Now;
            if (param.Length > 1)
                _currentMapThicknessIndex = Convert.ToInt32(param[1]);
            ModuleName module = (ModuleName)param[0];

            int[] mappingaddress = GetMappingAddress(module);
            if (mappingaddress == null || mappingaddress.Length != 6)
            {
                EV.PostAlarmLog("Robot", $"{RobotModuleName} map error: Invalid mapping setting.");
                return false;
            }

            Blade1Target = module;
            Blade2Target = module;



            CmdTarget = module;
            MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = BuildBladeTarget(),
            };

            if (ModuleHelper.IsLoadPort(CmdTarget))
            {
                var lp = DEVICE.GetDevice<LoadPortBaseDevice>(CmdTarget.ToString());
                if (lp != null)
                    lp.NoteTransferStart();
            }

            return true;


        }


        protected override bool fMonitorMap(object[] param)
        {
            IsBusy = false;
            int[] mappingaddress = GetMappingAddress(CmdTarget);
            try
            {
                MoveWalkingAxis((int)RobotStepEnum.Step1,m_WalkingAxis.TimeLimitMove, CmdTarget, Notify, Stop);
                ReadRobotPosition((int)RobotStepEnum.Step2, m_WalkingAxis.TimeLimitMove, mappingaddress[1], Notify, Stop);
                MapWaferAndWaitComplete((int)RobotStepEnum.Step3, RobotCommandTimeout,CmdTarget, Notify, Stop);
            }
            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {                
                OnError("Mapping failed.");
                return true;
            }

            

            char[] exists = MapExistInfor.ToArray();
            char[] ngs = MapNGInfor.ToArray();

            for (int i = 0; i < 25; i++)
            {
                if (ngs[i] != '0') exists[i] = '2';
            }
            NotifySlotMapResult(CmdTarget, new string(exists));

            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;

            if (ModuleHelper.IsLoadPort(CmdTarget))
            {
                var lp = DEVICE.GetDevice<LoadPortBaseDevice>(CmdTarget.ToString());
                if (lp != null)
                    lp.NoteTransferStop();
            }
            CmdTarget = ModuleName.System;
            MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = BuildBladeTarget(),
            };

            

            return true;
        }
       
        protected override bool fStartSwapWafer(object[] param)
        {

            _dtActionStart = DateTime.Now;
            if (param.Length < 3) return false;
            RobotArmEnum arm = (RobotArmEnum)param[0];
            ModuleName station = (ModuleName)Enum.Parse(typeof(ModuleName), (string)param[1]);
            int slot = (int)param[2];
            _currentMotionStation = station;
            _currentMotionSlot = slot;
            _currentMotionArm = arm;

            float xoffset = 0;
            float yoffset = 0;
            float zoffset = 0;
            float woffset = 0;
            if (param.Length >= 7)
            {
                xoffset = (float)param[3];
                yoffset = (float)param[4];
                zoffset = (float)param[5];
                woffset = (float)param[6];
            }
            WaferSize wafersize = WaferManager.Instance.GetWaferSize(station,slot);
            int address = GetBaseAddress(station, wafersize);
            zoffset = zoffset + (slot) * GetWaferPitch(station, wafersize);
            float insertdistance = GetInsertDistance(station, wafersize);
            float slowupdistance = GetSlowUpDistance(station, wafersize);
            lock (_locker)
            {
                if (arm == RobotArmEnum.Lower)
                {
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset + _armPitch, woffset));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", insertdistance, 0, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "U", 0, 0, slowupdistance, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, false));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", (-1) * insertdistance, insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, false));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "U", 0, 0, -(slowupdistance+ _armPitch), 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", 0, -insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIReadDIByteHandler(this, 0));

                    //WaferManager.Instance.WaferMoved( station, slot, RobotModuleName, 0);
                    //WaferManager.Instance.WaferMoved(RobotModuleName, 1,station, slot);

                }
                if (arm == RobotArmEnum.Upper)
                {
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset, woffset));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", 0, insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "U", 0, 0, slowupdistance+_armPitch, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, false));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", insertdistance, -insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, false));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "U", 0, 0, -slowupdistance, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I",  -insertdistance,0, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIReadDIByteHandler(this, 0));

                    //WaferManager.Instance.WaferMoved(station, slot, RobotModuleName, 1);
                    //WaferManager.Instance.WaferMoved(RobotModuleName, 0, station, slot);

                }
                ReadCurrentPostion();

                Blade1Target = station;
                Blade2Target = station;



                CmdTarget = station;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };
                return true;
            }
        }

        protected override bool fMonitorSwap(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("SwapTimeout.");
                return true;
            }
            if (_lstHandler.Count == 0 && (!_connection.IsBusy) && IsOnLine && ACalCompleted && MovingCompleted)
            {
                OnSwapComplete();
                EV.PostInfoLog("Robot", $"{RobotModuleName} swap wafer successfully.");


                Blade1Target = ModuleName.System;
                Blade2Target = ModuleName.System;
                CmdTarget = ModuleName.System;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };


                return true;
            }
            return false;
        }
        private void OnSwapComplete()
        {
            RobotArmEnum otherarm = GetAnotherArm(_currentMotionArm);


            if ((GetWaferState(_currentMotionArm) == RobotArmWaferStateEnum.Present && GetWaferState(otherarm) == RobotArmWaferStateEnum.Absent) 
                || isSimulatorMode)
            {
                WaferManager.Instance.WaferMoved(_currentMotionStation, _currentMotionSlot,
                        RobotModuleName, (int)_currentMotionArm);
                WaferManager.Instance.WaferMoved(RobotModuleName, (int)otherarm, _currentMotionStation, _currentMotionSlot);
                
            }
            else
            {
                _lstHandler.Clear();
                OnError($"Detect wafer absent on {_currentMotionArm}");
            }

            

            //if (GetWaferState(otherarm) == RobotArmWaferStateEnum.Present)
            //{
            //    if (otherarm == RobotArmEnum.Lower || otherarm == RobotArmEnum.Upper)
            //        WaferManager.Instance.WaferMoved(RobotModuleName, (int)otherarm,
            //            _currentMotionStation, _currentMotionSlot);
            //    else
            //    {
            //        WaferManager.Instance.WaferMoved(RobotModuleName, 0,
            //            _currentMotionStation, _currentMotionSlot);
            //        WaferManager.Instance.WaferMoved(RobotModuleName, 1,
            //            _currentMotionStation, _currentMotionSlot + 1);
            //    }
            //}
            //else
            //{
            //    _lstHandler.Clear();
            //    OnError($"Detect wafer present on {_currentMotionArm}");
            //}
        }

        protected override bool fStartPlaceWafer(object[] param)
        {
            _dtActionStart = DateTime.Now;
            if (param.Length < 3) return false;
            RobotArmEnum arm = (RobotArmEnum)param[0];
            ModuleName station = (ModuleName)Enum.Parse(typeof(ModuleName), (string)param[1]); ;
            int slot = (int)param[2];
            _currentMotionStation = station;
            _currentMotionSlot = slot;
            _currentMotionArm = arm;

            float xoffset = 0;
            float yoffset = 0;
            float zoffset = 0;
            float woffset = 0;
            if (param.Length >= 7)
            {
                xoffset = (float)param[3];
                yoffset = (float)param[4];
                zoffset = (float)param[5];
                woffset = (float)param[6];
            }
            WaferSize wafersize = WaferManager.Instance.GetWaferSize(RobotModuleName, 
                (int)(arm == RobotArmEnum.Both ? RobotArmEnum.Lower : arm));
            float insertdistance = GetInsertDistance(station, wafersize);
            float slowupdistance = GetSlowUpDistance(station, wafersize);
            int address = GetBaseAddress(station, wafersize);
            zoffset = zoffset + (slot) * GetWaferPitch(station, wafersize) + slowupdistance;
            
            lock (_locker)
            {
                if (arm == RobotArmEnum.Lower)
                {
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset+ _armPitch, woffset));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", insertdistance, 0, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, false));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "U", 0, 0, (-1)*slowupdistance, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));

                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, false));

                    _lstHandler.AddLast(new HirataRobotIIWaferInforMoveHandler(this, 0, false));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, false));


                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", (-1) * insertdistance, 0, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIReadDIByteHandler(this, 0));
                    //WaferManager.Instance.WaferMoved(RobotModuleName, 0, station, slot);


                }
                if (arm == RobotArmEnum.Upper)
                {
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset, woffset));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", 0, insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, false));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "U", 0, 0, (-1)*slowupdistance, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));


                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, false));

                    _lstHandler.AddLast(new HirataRobotIIWaferInforMoveHandler(this, 0, false));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, false));


                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", 0, (-1) * insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIReadDIByteHandler(this, 0));

                    //WaferManager.Instance.WaferMoved(RobotModuleName, 1, station, slot);

                }
                if (arm == RobotArmEnum.Both)
                {
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset + _armPitch, woffset));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", insertdistance, insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, false));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, false));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "U", 0, 0, (-1)*slowupdistance, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));

                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, false));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, false));

                    _lstHandler.AddLast(new HirataRobotIIWaferInforMoveHandler(this, 0, false));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, false));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, false));

                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", (-1) * insertdistance, (-1) * insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIReadDIByteHandler(this, 0));

                    //WaferManager.Instance.WaferMoved(RobotModuleName, 0, station, slot);
                    //WaferManager.Instance.WaferMoved(RobotModuleName, 1, station, slot+1);

                }
                ReadCurrentPostion();

                Blade1Target = station;
                Blade2Target = station;



                CmdTarget = station;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };

                if (ModuleHelper.IsLoadPort(CmdTarget))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(CmdTarget.ToString());
                    if (lp != null)
                        lp.NoteTransferStart();
                }
                return true;
            }
        }

        protected override bool fMonitorPlace(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("PlaceTimeout.");
                return true;
            }
            if (_lstHandler.Count == 0 && (!_connection.IsBusy) && IsOnLine && ACalCompleted && MovingCompleted)
            {
                OnPlaceComplete();
                EV.PostInfoLog("Robot", $"{RobotModuleName} place wafer to {CmdTarget} successfully.");


                Blade1Target = ModuleName.System;
                Blade2Target = ModuleName.System;

                if (ModuleHelper.IsLoadPort(CmdTarget))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(CmdTarget.ToString());
                    if (lp != null)
                        lp.NoteTransferStop();
                }

                CmdTarget = ModuleName.System;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };




                return true;
            }
            return false;
        }
        public void MoveWaferInforToStation()
        {
            if (GetWaferState(_currentMotionArm) == RobotArmWaferStateEnum.Absent || isSimulatorMode)
            {
                if (_currentMotionArm == RobotArmEnum.Lower || _currentMotionArm == RobotArmEnum.Upper)
                    WaferManager.Instance.WaferMoved(RobotModuleName, (int)_currentMotionArm,
                        _currentMotionStation, _currentMotionSlot);
                else
                {
                    WaferManager.Instance.WaferMoved(RobotModuleName, 0,
                        _currentMotionStation, _currentMotionSlot);
                    WaferManager.Instance.WaferMoved(RobotModuleName, 1,
                        _currentMotionStation, _currentMotionSlot + 1);
                }
            }
            else
            {
                _lstHandler.Clear();
                OnError($"Detect wafer present on {_currentMotionArm}");
            }
        }

        public void MoveWaferInforFromStation()
        {
            if (GetWaferState(_currentMotionArm) == RobotArmWaferStateEnum.Present || isSimulatorMode)
            {
                if (_currentMotionArm == RobotArmEnum.Lower || _currentMotionArm == RobotArmEnum.Upper)
                    WaferManager.Instance.WaferMoved(_currentMotionStation, _currentMotionSlot,
                        RobotModuleName, (int)_currentMotionArm);
                else
                {
                    WaferManager.Instance.WaferMoved(_currentMotionStation, _currentMotionSlot,
                        RobotModuleName, 0);
                    WaferManager.Instance.WaferMoved(_currentMotionStation, _currentMotionSlot + 1,
                        RobotModuleName, 1);
                }
            }
            else
            {
                _lstHandler.Clear();
                OnError($"Detect wafer absent on {_currentMotionArm}");
            }
        }

        private void OnPlaceComplete()
        {

            if (GetWaferState(_currentMotionArm) == RobotArmWaferStateEnum.Absent || isSimulatorMode)
            {
                
            }
            else
            {
                _lstHandler.Clear();
                OnError($"Detect wafer present on {_currentMotionArm}");
            }
        }

        protected override bool fStartPickWafer(object[] param)
        {
            _dtActionStart = DateTime.Now;
            if (param.Length < 3) return false;
            RobotArmEnum arm = (RobotArmEnum)param[0];
            ModuleName station = (ModuleName)Enum.Parse(typeof(ModuleName), (string)param[1]); ;
            int slot = (int)param[2];
            float xoffset = 0;
            float yoffset = 0;
            float zoffset = 0;
            float woffset = 0;
            if(param.Length >=7)
            {
                xoffset = (float)param[3];
                yoffset = (float)param[4];
                zoffset = (float)param[5];
                woffset = (float)param[6];
            }
            WaferSize wafersize = WaferManager.Instance.GetWaferSize(station, slot);

            int address = GetBaseAddress(station, wafersize);
            zoffset = zoffset + slot * GetWaferPitch(station, wafersize);
            float insertdistance = GetInsertDistance(station, wafersize);
            float slowupdistance = GetSlowUpDistance(station, wafersize);

            _currentMotionArm = arm;
            _currentMotionStation = station;
            _currentMotionSlot = slot;

            lock (_locker)
            {
                if(arm == RobotArmEnum.Lower)
                {
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset + _armPitch, woffset));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", insertdistance, 0, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                   
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, true));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, false));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "U", 0, 0, slowupdistance, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));

                    _lstHandler.AddLast(new HirataRobotIIWaferInforMoveHandler(this, 0, true));




                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", (-1)*insertdistance, 0, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIReadDIByteHandler(this, 0));
                    //WaferManager.Instance.WaferMoved(station, slot, RobotModuleName, 0);
                }
                if(arm == RobotArmEnum.Upper)
                {
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset, woffset));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", 0, insertdistance,  0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                     _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, true));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, false));

                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "U", 0, 0, slowupdistance, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));


                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));

                    _lstHandler.AddLast(new HirataRobotIIWaferInforMoveHandler(this, 0, true));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", 0, (-1) * insertdistance,  0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIReadDIByteHandler(this, 0));

                    //WaferManager.Instance.WaferMoved(station, slot, RobotModuleName, 1);

                }
                if (arm == RobotArmEnum.Both)
                {
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset + _armPitch, woffset));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", insertdistance, insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                      _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, false));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, false));

                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "U", 0, 0, slowupdistance, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));

                    _lstHandler.AddLast(new HirataRobotIIWaferInforMoveHandler(this, 0, true));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveDeviationHandler(this, "I", (-1) * insertdistance, (-1) * insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIReadDIByteHandler(this, 0));                    
                }
                ReadCurrentPostion();


                Blade1Target = station;
                Blade2Target = station;



                CmdTarget = station;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };

                if (ModuleHelper.IsLoadPort(station))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(station.ToString());
                    if (lp != null)
                        lp.NoteTransferStart();
                }

                return true;
            }
        }
        protected override bool fMonitorPick(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("PickTimeout.");
                return true;
            }
            if (_lstHandler.Count == 0 && (!_connection.IsBusy) && IsOnLine && ACalCompleted && MovingCompleted)
            {
                OnPickComplete();
                EV.PostInfoLog("Robot", $"{RobotModuleName} pick wafer from {CmdTarget} successfully.");

                Blade1Target = ModuleName.System;
                Blade2Target = ModuleName.System;

                if (ModuleHelper.IsLoadPort(CmdTarget))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(CmdTarget.ToString());
                    if (lp != null)
                        lp.NoteTransferStop();
                }

                CmdTarget = ModuleName.System;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };

                
                return true;
            }
            return false;
        }


        private void OnPickComplete()
        {
            if (GetWaferState(_currentMotionArm) == RobotArmWaferStateEnum.Present || isSimulatorMode)
            {

            }
            else
            {
                _lstHandler.Clear();
                OnError($"Detect wafer absent on {_currentMotionArm}");
            }
        }
        protected override bool fResetToReady(object[] param)
        {
            return true;
        }

        protected override bool fReset(object[] param)
        {
            _dtActionStart = DateTime.Now;
            _connection.SetCommunicationError(false, "");

            if (!_connection.IsConnected)
            {
                _connection.Disconnect();
                _connection.Connect();
            }

            lock (_locker)
            {
                _lstHandler.AddLast(new HirataRobotIISimpleActionHandler(this, "GE"));
                _lstHandler.AddLast(new HirataRobotIIRawCommandHandler(this, "CL"));
            }
            
             return true;// throw new NotImplementedException();
        }
        private DateTime _dtActionStart;

        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            if(DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("ResetTimeout.");
                return true;
            }
            return _lstHandler.Count==0 && !_connection.IsBusy;
        }

        protected override bool fStartInit(object[] param)
        {
            ResetRoutine();

            _dtActionStart = DateTime.Now;
            return true;
        }
        protected override bool fMonitorInit(object[] param)
        {
          
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("InitTimeout.");
                return true;
            }


            try
            {
                OpenVacuum((int)RobotStepEnum.Step1, RobotCommandTimeout, Notify, Stop);

                TimeDelay((int)RobotStepEnum.Step2, 0.5);                

                IdentifyWaferStatus((int)RobotStepEnum.Step3, RobotCommandTimeout, Notify, Stop);

                HomeRobotArms((int)RobotStepEnum.Step4, RobotCommandTimeout, Notify, Stop);
                


            }
            catch (RoutineBreakException)
            {
                return false;
            }
            catch (RoutineFaildException)
            {
                OnError("Mapping failed.");
                return true;
            }
            Blade1Target =  ModuleName.System;
            Blade2Target = ModuleName.System;



            CmdTarget =  ModuleName.System;
            MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = BuildBladeTarget(),
            };


            EV.PostInfoLog("Robot", $"{RobotModuleName} homing completed successfully.");
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

        
        protected override bool fClear(object[] param)
        {
            return true; ;
        }
        protected override bool fError(object[] param)
        {
            return true;
        }
        private void ReadCurrentPostion()
        {
            _lstHandler.AddLast(new HirataRobotIIReadHandler(this, "LR", null));
        }
        protected virtual float GetWaferPitch(ModuleName module, WaferSize wz)
        {
            if (ModuleHelper.IsLoadPort(module))
            {
                var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
                int carrierindex = lp.InfoPadCarrierIndex;

                return SC.ContainsItem($"CarrierInfo.CarrierWaferPitch{carrierindex}") ?
                        (float)SC.GetValue<int>($"CarrierInfo.CarrierWaferPitch{carrierindex}") / 100 : 20;

            }
            if (wz == WaferSize.WS12)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}CarrierWaferPitch12") / 100;
            if (wz == WaferSize.WS8)
                return (float)SC.GetValue<int>($"CarrierInfo.{module.ToString()}CarrierWaferPitch8") / 100;
            if (wz == WaferSize.WS6)
                return (float)SC.GetValue<int>($"CarrierInfo.{module.ToString()}CarrierWaferPitch6") / 100;
            if (wz == WaferSize.WS4)
                return (float)SC.GetValue<int>($"CarrierInfo.{module.ToString()}CarrierWaferPitch4") / 100;
            if (wz == WaferSize.WS3)
                return (float)SC.GetValue<int>($"CarrierInfo.{module.ToString()}CarrierWaferPitch3") / 100;
            return 20;
        }
        protected virtual float GetInsertDistance(ModuleName module, WaferSize wz)
        {
            if (ModuleHelper.IsLoadPort(module))
            {
                var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
                int carrierindex = lp.InfoPadCarrierIndex;
                return SC.ContainsItem($"CarrierInfo.InsertDistance{carrierindex}") ?
                    (float)SC.GetValue<int>($"CarrierInfo.InsertDistance{carrierindex}") / 100 : 300;
            }
            if (wz == WaferSize.WS12)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}InsertDistance12") / 100;
            if (wz == WaferSize.WS8)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}InsertDistance8") / 100;
            if (wz == WaferSize.WS6)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}InsertDistance6") / 100;
            if (wz == WaferSize.WS4)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}InsertDistance4") / 100;
            if (wz == WaferSize.WS3)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}InsertDistance3") / 100;
            return (float)SC.GetValue<int>($"CarrierInfo.{module}InsertDistance0") / 100;
        }

        protected virtual float GetSlowUpDistance(ModuleName module, WaferSize wz)
        {
            if (ModuleHelper.IsLoadPort(module))
            {
                var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
                int carrierindex = lp.InfoPadCarrierIndex;

                return SC.ContainsItem($"CarrierInfo.SlowUpDistance{carrierindex}") ?
                    (float)SC.GetValue<int>($"CarrierInfo.SlowUpDistance{carrierindex}") / 100 : 10;
            }
            if (wz == WaferSize.WS12)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}SlowUpDistance12") / 100;
            if (wz == WaferSize.WS8)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}SlowUpDistance8") / 100;

            if (wz == WaferSize.WS6)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}SlowUpDistance6") / 100;

            if (wz == WaferSize.WS4)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}SlowUpDistance4") / 100;
            if (wz == WaferSize.WS3)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}SlowUpDistance3") / 100;
            return (float)SC.GetValue<int>($"CarrierInfo.{module}SlowUpDistance0") / 100;

        }
        protected virtual int GetBaseAddress(ModuleName module, WaferSize wz)
        {
            if (ModuleHelper.IsLoadPort(module))
            {
                var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
                int carrierindex = lp.InfoPadCarrierIndex;

                return SC.ContainsItem($"CarrierInfo.{module}Station{carrierindex}") ?
                    Convert.ToInt16(SC.GetStringValue($"CarrierInfo.{module}Station{carrierindex}")) : 0;
            }
            if (wz == WaferSize.WS12)
                return Convert.ToInt16(SC.GetStringValue($"CarrierInfo.{module}Station12"));
            if (wz == WaferSize.WS8)
                return Convert.ToInt16(SC.GetStringValue($"CarrierInfo.{module}Station8"));
            if (wz == WaferSize.WS6)
                return Convert.ToInt16(SC.GetStringValue($"CarrierInfo.{module}Station6"));
            if (wz == WaferSize.WS4)
                return Convert.ToInt16(SC.GetStringValue($"CarrierInfo.{module}Station4"));
            if (wz == WaferSize.WS3)
                return Convert.ToInt16(SC.GetStringValue($"CarrierInfo.{module}Station3"));

            return Convert.ToInt16(SC.GetStringValue($"CarrierInfo.{module}Station0"));
        }
        protected virtual int[] GetMappingAddress(ModuleName module)
        {
            var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
            int carrierindex = lp.InfoPadCarrierIndex;

            string addresses = SC.ContainsItem($"CarrierInfo.{module}MappingStation{carrierindex}") ?
                SC.GetStringValue($"CarrierInfo.{module}MappingStation{carrierindex}") : "";
            List<int> ret = new List<int>();

            if (addresses.Split(',').Length != 6)
            {
                EV.PostAlarmLog("Robot", "Mapping address setting error!");
                return null;
            }
            foreach (var add in addresses.Split(','))
            {
                int nValue;
                if (!int.TryParse(add, out nValue))
                {
                    EV.PostAlarmLog("Robot", "Mapping address setting error!");
                    return null;
                }
                ret.Add(nValue);
            }
            return ret.ToArray();
        }


        public override RobotArmWaferStateEnum GetWaferState(RobotArmEnum arm)
        {
            switch(arm)
            {
                case RobotArmEnum.Lower:
                    if (di_Values[0, 0]) return RobotArmWaferStateEnum.Present;
                    else return RobotArmWaferStateEnum.Absent;
                case RobotArmEnum.Upper:
                    if (di_Values[0, 2]) return RobotArmWaferStateEnum.Present;
                    else return RobotArmWaferStateEnum.Absent;
                case RobotArmEnum.Both:
                    if(di_Values[0, 0] == di_Values[0, 2])
                    {
                        if (di_Values[0, 0]) return RobotArmWaferStateEnum.Present;
                        else return RobotArmWaferStateEnum.Absent;
                    }
                    break;

            }
            return RobotArmWaferStateEnum.Unknown;
        }

        private RobotArmEnum GetAnotherArm(RobotArmEnum arm)
        {
            if (arm == RobotArmEnum.Lower) return RobotArmEnum.Upper;
            if (arm == RobotArmEnum.Upper) return RobotArmEnum.Lower;
            return RobotArmEnum.Both;
        }

        protected override bool fStartReadData(object[] param)
        {
            _dtActionStart = DateTime.Now;
            string datatype = param[0].ToString();
            lock (_locker)
            {
                switch (datatype)
                {
                    case "CurrentPositionData":
                        ReadCurrentPostion();
                        break;
                    case "ExtParameter":
                        if (param.Length < 2) return false;
                        _lstHandler.AddLast(new HirataRobotIIReadHandler(this, "LE", param[1].ToString()));
                        break;
                    case "CurrentStatus":
                        _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this)); 
                        break;
                    case "PositionData":
                        if (param.Length < 2) return false;
                        if (!uint.TryParse(param[1].ToString(), out _)) return false;
                        ReadPosition(int.Parse(param[1].ToString()));
                        break;
                    case "MappingData":
                        if (param.Length < 2) return false;
                        if (!param[1].ToString().Contains("M0")) return false;
                        int paraindex;
                        if (!int.TryParse(param[1].ToString().Replace("M", ""), out paraindex)) return false;
                        if (paraindex > 9 || paraindex < 0) return false;
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataRobotIIReadHandler(this, "LE", (112 + (5 * (paraindex - 1))).ToString()));
                            _lstHandler.AddLast(new HirataRobotIIReadHandler(this, "LE", (610 + (5 * (paraindex - 1))).ToString()));
                            _lstHandler.AddLast(new HirataRobotIIReadHandler(this, "LE", (611 + (5 * (paraindex - 1))).ToString()));
                            _lstHandler.AddLast(new HirataRobotIIReadHandler(this, "LE", (612 + (5 * (paraindex - 1))).ToString()));
                            _lstHandler.AddLast(new HirataRobotIIReadHandler(this, "LE", (613 + (5 * (paraindex - 1))).ToString()));
                            _lstHandler.AddLast(new HirataRobotIIReadHandler(this, "LE", (614 + (5 * (paraindex - 1))).ToString()));
                            _lstHandler.AddLast(new HirataRobotIIReadHandler(this, "LE", (615 + (5 * (paraindex - 1))).ToString()));
                        }


                        break;
                    

                }
            }

            return true;
        }

        protected override bool fMonitorReadData(object[] param)
        {
          
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("ReadDataTimeout.");
                return true;
            }
            if (_lstHandler.Count == 0 && (!_connection.IsBusy) && IsOnLine && ACalCompleted && MovingCompleted)
            {
                EV.PostInfoLog("Robot", $"{RobotModuleName} ReadData successfully.");
                return true;
            }
            return false;
        }
    

        protected override bool fStartSetParameters(object[] param)
        {
            _dtActionStart = DateTime.Now;
            string datatype = param[0].ToString();
            if (datatype == "TransferSpeedLevel")
            {
                if (param.Length == 2)
                {
                    int speed = 10;
                    if (param[1].ToString() == "3") speed = 10;
                    if (param[1].ToString() == "2") speed = 50;
                    if (param[1].ToString() == "1") speed = 100;
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HirataRobotIIRawCommandHandler(this, "SP", $"{speed} {speed}"));
                    }
                }
                if (param.Length == 4)
                {
                    int ABWSpeed;
                    int ZSpeed;
                    if (!int.TryParse(param[2].ToString(), out ABWSpeed)) return false;
                    if (!int.TryParse(param[3].ToString(), out ZSpeed)) return false;

                    if (ABWSpeed > 100) ABWSpeed = 100;
                    if (ABWSpeed < 0) ABWSpeed = 0;
                    if (ZSpeed > 100) ZSpeed = 100;
                    if (ZSpeed < 0) ZSpeed = 0;

                    if (param[1].ToString() == "PTPSpeed")
                    {
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataRobotIIRawCommandHandler(this, "SP", $"{ABWSpeed} {ZSpeed}"));
                        }
                    }
                    if (param[1].ToString() == "ACC/DEC")
                    {
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataRobotIIRawCommandHandler(this, "SX", $"{ABWSpeed} {ZSpeed}"));
                        }
                    }
                }
            }

            if (datatype == "ExtParameter")
            {


                if (param.Length < 3) return false;
                if (!int.TryParse(param[1].ToString(), out _)) return false;
                if (string.IsNullOrEmpty(param[2].ToString())) return false;

                lock (_locker)
                {
                    _lstHandler.AddLast(new HirataRobotIIWriteHandler(this, "SE", $"{param[1]} {param[2]}"));
                }

            }

            if (datatype == "PositionData")
            {
                if (param.Length < 9)
                {
                    EV.PostAlarmLog("Robot", "Robot postion data is not correct");
                    return false;
                }
                if (!int.TryParse(param[1].ToString(), out _))
                {
                    EV.PostAlarmLog("Robot", "Robot postion data is not correct");
                    return false;
                }
                if (!float.TryParse(param[2].ToString(), out _))
                {
                    EV.PostAlarmLog("Robot", "Robot postion data is not correct");
                    return false;
                }
                if (!float.TryParse(param[3].ToString(), out _))
                {
                    EV.PostAlarmLog("Robot", "Robot postion data is not correct");
                    return false;
                }
                if (!float.TryParse(param[4].ToString(), out _))
                {
                    EV.PostAlarmLog("Robot", "Robot postion data is not correct");
                    return false;
                }
                if (!float.TryParse(param[5].ToString(), out _))
                {
                    EV.PostAlarmLog("Robot", "Robot postion data is not correct");
                    return false;
                }
                if (!int.TryParse(param[6].ToString(), out _))
                {
                    EV.PostAlarmLog("Robot", "Robot postion data is not correct");
                    return false;
                }
                if (!int.TryParse(param[7].ToString(), out _))
                {
                    EV.PostAlarmLog("Robot", "Robot postion data is not correct");
                    return false;
                }
                if (!int.TryParse(param[8].ToString(), out _))
                {
                    EV.PostAlarmLog("Robot", "Robot postion data is not correct");
                    return false;
                }



                return WritePosition(int.Parse(param[1].ToString()), float.Parse(param[2].ToString()), float.Parse(param[3].ToString()),
                    float.Parse(param[4].ToString()), float.Parse(param[5].ToString()), param[6].ToString(), param[7].ToString(),
                    param[8].ToString());


            }

            if (datatype == "MappingData")
            {
                int paraindex;
                if (!int.TryParse(param[1].ToString().Replace("M", ""), out paraindex)) return false;
                if (paraindex > 9 || paraindex < 0) return false;
                if (param.Length < 8) return false;

                if(uint.TryParse(param[2].ToString(),out _))
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HirataRobotIIWriteHandler(this, "SE",
                            $"{(112 + (5 * (paraindex - 1)))} {param[2]}"));
                    }
                
                if(float.TryParse(param[3].ToString(),out _))
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HirataRobotIIWriteHandler(this, "SE",
                            $"{(610 + (5 * (paraindex - 1)))} {param[3]}"));
                    }
                if (float.TryParse(param[4].ToString(), out _))
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HirataRobotIIWriteHandler(this, "SE",
                            $"{(611 + (5 * (paraindex - 1)))} {param[4]}"));
                    }
                if (float.TryParse(param[5].ToString(), out _))
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HirataRobotIIWriteHandler(this, "SE",
                            $"{(612 + (5 * (paraindex - 1)))} {param[5]}"));
                    }
                if (float.TryParse(param[6].ToString(), out _))
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HirataRobotIIWriteHandler(this, "SE",
                            $"{(613 + (5 * (paraindex - 1)))} {param[6]}"));
                    }
                if (!string.IsNullOrEmpty(param[7].ToString()))
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HirataRobotIIWriteHandler(this, "SE",
                            $"{(614 + (5 * (paraindex - 1)))} {param[7]}"));
                    }
               


            }

            if(datatype == "InsertMotionParameter")
            {
                if (param.Length < 3) return false;
                float insertdistance;
                int Decratio;
                if (!float.TryParse(param[1].ToString(), out insertdistance)) return false;
                if (!int.TryParse(param[2].ToString(), out Decratio)) return false;
                if (Decratio > 99) Decratio = 99;
                if (Decratio < 0) Decratio = 0;
                _lstHandler.AddLast(new HirataRobotIIWriteHandler(this, "SI", $" {insertdistance} {Decratio}"));
            }

            if (datatype == "SlowUpMotionParameter")
            {
                if (param.Length < 3) return false;
                float insertdistance;
                int Decratio;
                if (!float.TryParse(param[1].ToString(), out insertdistance)) return false;
                if (!int.TryParse(param[2].ToString(), out Decratio)) return false;
                if (Decratio > 99) Decratio = 99;
                if (Decratio < 0) Decratio = 0;
                _lstHandler.AddLast(new HirataRobotIIWriteHandler(this, "SJ", $" {insertdistance} {Decratio}"));
            }

            if(datatype == "MappingDataForLP")
            {
                if (param.Length < 4) return false;
                string[] mappingstations;
                if (param[3].ToString() == "WS4")
                {
                    if (!SC.ContainsItem($"CarrierInfo.{param[1]}MappingStation3")) return false;
                    mappingstations = SC.GetStringValue($"CarrierInfo.{param[1]}MappingStation3").Split(',');
                }
                else if(param[3].ToString() == "WS6")
                {
                    if (!SC.ContainsItem($"CarrierInfo.{param[1]}MappingStation2")) return false;
                    mappingstations = SC.GetStringValue($"CarrierInfo.{param[1]}MappingStation2").Split(',');

                }
                else
                {
                    mappingstations = null;
                    return false;
                }



                if (mappingstations.Length < 6) return false;

                    for(int i =1;i<5;i++)
                    {
                        if (!uint.TryParse(mappingstations[i], out _)) return false;

                        lock(_locker)
                        {
                            _lstHandler.AddLast(new HirataRobotIIReadRobotPositionHandler(this, Convert.ToInt32(mappingstations[i])));
                        }
                        int waittime = 0;
                        while(_lstHandler.Count !=0 || _connection.IsBusy ||_connection.IsCommunicationError)
                        {
                            Thread.Sleep(200);
                            waittime++;
                        if (waittime > 10)
                        {
                            EV.PostAlarmLog("Robot", "Set LP mapping data failed.");
                            return false;
                        }
                        }
                        WritePosition(Convert.ToInt32(mappingstations[i]), ReadXPosition, ReadYPosition, ReadZPosition, ReadWPosition,
                            param[2].ToString().Replace("M", ""), FCode, SCode);
                    }

                }

            

            return true;
        }

        protected override bool fMonitorSetParamter(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("SetParametersTimeout.");
                return true;
            }
            if (_lstHandler.Count == 0 && (!_connection.IsBusy) && IsOnLine && ACalCompleted && MovingCompleted)
            {
                EV.PostInfoLog("Robot", $"{RobotModuleName} set parameters successfully.");
                return true;
            }
            return false;
        }
        private string BuildBladeTarget()
        {
            return (CmdRobotArm == RobotArmEnum.Upper ? "ArmB" : "ArmA") + "." + CmdTarget;
        }
        private void ReadRobotPosition(int id, int time, int address, Action<string> notify, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                notify($"Start read address data on {address}.");               
                lock (_locker)
                {
                    _lstHandler.AddLast(new HirataRobotIIReadRobotPositionHandler(this, address));
                }
                return true;
            }, () =>
            {
                return (_lstHandler.Count == 0 && !_connection.IsBusy);
                    
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {

                    error($"Wait to read position data on {address} timeout after {time} seconds");
                    throw new RoutineFaildException();
                }
                else
                {
                    throw new RoutineBreakException();
                }
            }
        }


        private void MapWaferAndWaitComplete(int id, int time, ModuleName module, Action<string> notify, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {              
                notify($"Start map wafer on {module}.");

                int[] mappingaddress = GetMappingAddress(module);
                if (mappingaddress == null || mappingaddress.Length != 6) return false;
                int thicknessIndex = _currentMapThicknessIndex;
                EV.PostInfoLog(Module, $"Start mapping, {module} thickness type {thicknessIndex} ");                
                lock (_locker)
                {
                    if (thicknessIndex > 0 && thicknessIndex < 10)
                    {
                        _lstHandler.AddLast(new HirataRobotIIWriteMdataOnCurrenPositionHandler(this, mappingaddress[1], $"0{thicknessIndex}"));
                       
                    }
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionHandler(this, mappingaddress[0], null));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionHandler(this, mappingaddress[1], null));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIRawCommandHandler(this, "SM", "1"));
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionHandler(this, mappingaddress[2], null));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionHandler(this, mappingaddress[3], null));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionHandler(this, mappingaddress[4], null));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIReadHandler(this, "LE", "100"));
                    _lstHandler.AddLast(new HirataRobotIIReadHandler(this, "LE", "101"));
                    _lstHandler.AddLast(new HirataRobotIIRawCommandHandler(this, "SM", "0"));  //Close laser
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionHandler(this, mappingaddress[5], null));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    ReadCurrentPostion();
                }
                return true;
            }, () =>
            {
                if (_lstHandler.Count == 0 && (!_connection.IsBusy) && IsOnLine && ACalCompleted  &&MovingCompleted)
                    return true;

                return false;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    error($"Wait wafer map on {module} timeout after {time} seconds");
                    throw new RoutineFaildException();
                }
                else
                {
                    throw new RoutineBreakException();
                }
            }
        }
        public void TimeDelay(int id, double time)
        {
            Tuple<bool, Result> ret = Delay(id, () =>
            {
                Notify($"Delay {time} seconds");
                return true;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.RUN)
                {
                    throw (new RoutineBreakException());
                }
            }
        }
        private void OpenVacuum(int id, int time, Action<string> notify, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                notify($"Start open vacuum valve.");               
                lock (_locker)
                {
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, false));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, true));
                    _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, false));
                }
                return true;
            }, () =>
            {
                if (_lstHandler.Count == 0 && (!_connection.IsBusy) && IsOnLine && ACalCompleted && MovingCompleted)
                    return true;

                return false;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    error($"Open blade vacuum timeout after {time} seconds");
                    throw new RoutineFaildException();
                }
                else
                {
                    throw new RoutineBreakException();
                }
            }
        }

        private void IdentifyWaferStatus(int id, int time, Action<string> notify, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                notify($"Start check wafer status.");
                lock (_locker)
                {
                    ReadCurrentPostion();
                    _lstHandler.AddLast(new HirataRobotIIReadDIByteForInitHandler(this, 0));
                }
                return true;
            }, () =>
            {
                if (_lstHandler.Count == 0 && (!_connection.IsBusy) && IsOnLine && ACalCompleted && MovingCompleted)
                {
                    if (GetWaferState(RobotArmEnum.Lower) == RobotArmWaferStateEnum.Present)
                    {
                        if (WaferManager.Instance.CheckNoWafer(RobotModuleName, 0))
                            WaferManager.Instance.CreateWafer(RobotModuleName, 0, WaferStatus.Normal);
                    }
                    if (GetWaferState(RobotArmEnum.Lower) == RobotArmWaferStateEnum.Absent)
                    {
                        if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                            WaferManager.Instance.DeleteWafer(RobotModuleName, 0);
                    }
                    if (GetWaferState(RobotArmEnum.Upper) == RobotArmWaferStateEnum.Present)
                    {
                        if (WaferManager.Instance.CheckNoWafer(RobotModuleName, 1))
                            WaferManager.Instance.CreateWafer(RobotModuleName, 1, WaferStatus.Normal);
                    }
                    if (GetWaferState(RobotArmEnum.Upper) == RobotArmWaferStateEnum.Absent)
                    {
                        if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 1))
                            WaferManager.Instance.DeleteWafer(RobotModuleName, 1);
                    }
                    return true;
                }
                return false;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    error($"Open blade vacuum timeout after {time} seconds");
                    throw new RoutineFaildException();
                }
                else
                {
                    throw new RoutineBreakException();
                }
            }
        }      

        private void HomeRobotArms(int id, int time, Action<string> notify, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                notify($"Start home robot arms.");
                lock (_locker)
                {
                    if (WaferManager.Instance.CheckNoWafer(RobotModuleName, 0))
                    {
                        _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 1, true));
                        _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 0, false));
                    }
                    if (WaferManager.Instance.CheckNoWafer(RobotModuleName, 1))
                    {
                        _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 3, true));
                        _lstHandler.AddLast(new HirataRobotIIWriteDOHandler(this, 2, false));
                    }
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionHandler(this, 0));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataRobotIIMoveToPositionHandler(this, 1, "A"));
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                }
                return true;
            }, () =>
            {
                if (_lstHandler.Count == 0 && (!_connection.IsBusy) && IsOnLine && ACalCompleted && MovingCompleted)
                {
                    return true;
                }
                return false;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    error($"Home robot timeout after {time} seconds");
                    throw new RoutineFaildException();
                }
                else
                {
                    throw new RoutineBreakException();
                }
            }
        }
        private void MoveWalkingAxis(int id, int time, ModuleName module, Action<string> notify, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                if(m_WalkingAxis == null)
                {
                    error("No valid walking axis.");
                    return false;
                }
                if(!m_WalkingAxis.IsReady())
                {
                    error("Walking axis is not ready.");
                    return false;
                }
                notify($"Start move walking axis to {module}.");

                return m_WalkingAxis.MoveTo(module);
            }, () =>
            {
                if (m_WalkingAxis.IsReady() && m_WalkingAxis.IsArrivedTarget)
                    return true;

                return false;
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {
                    
                    error($"Wait walking axis move to {module} timeout after {time} seconds");
                    throw new RoutineFaildException();
                }
                else
                {
                    throw new RoutineBreakException();
                }
            }
        }


        private void WaitRobotToReady(int id, int time, Action<string> notify, Action<string> error)
        {
            var ret = ExecuteAndWait(id, () =>
            {
                lock(_locker)
                {
                    _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                }

                return true;
            }, () =>
            {
                if(_lstHandler.Count == 0 && !_connection.IsBusy)                        
                {
                    if (IsOnLine && ACalCompleted && MovingCompleted)
                        return true;
                    else
                    {
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataRobotIIMonitorRobotStatusHandler(this));
                        }
                    }
                }
                
                return false;
                                
            }, time * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.FAIL)
                {
                    throw new RoutineFaildException();
                }
                else if (ret.Item2 == Result.TIMEOUT) //timeout
                {

                    error($"Wait robot to be ready timeout after {time} seconds");
                    throw new RoutineFaildException();
                }
                else
                {
                    throw new RoutineBreakException();
                }
            }
        }
        #region Sequence Routine




        protected void Notify(string message)
        {
            EV.PostMessage(Name, EventEnum.GeneralInfo, string.Format("{0}:{1}", Name, message));
        }
        protected void Stop(string failReason)
        {
            OnError(string.Format("Failed {0}, {1} ", Name, failReason));
        }





        private enum RobotStepEnum
        {
           

            Step1,
            Step2,
            Step3,
            Step4,
            Step5,
            Step6,
            Step7,
            Step8,

            Step9,
            Step10,
            Step11,
            Step12,

            Step13,
            Step14,
            Step15,
            Step16,
        }

        //timer, 计算routine时间
        protected DeviceTimer counter = new DeviceTimer();
        protected DeviceTimer delayTimer = new DeviceTimer();

        private enum STATE
        {
            IDLE,
            WAIT,
        }

        public int TokenId
        {
            get { return _id; }
        }
        private int _id;         //step index

        /// <summary>
        /// already done steps
        /// </summary>
        private Stack<int> _steps = new Stack<int>();

        private STATE state;    //step state //idel,wait,

        //loop control
        private int loop = 0;
        private int loopCount = 0;
        private int loopID = 0;

        private DeviceTimer timer = new DeviceTimer();

        public int LoopCounter { get { return loop; } }
        public int LoopTotalTime { get { return loopCount; } }

        // public int Timeout { get { return (int)(timer.GetTotalTime() / 1000); } }

        //状态持续时间，单位为秒
        public int Elapsed { get { return (int)(timer.GetElapseTime() / 1000); } }

        protected RoutineResult RoutineToken = new RoutineResult() { Result = RoutineState.Running };

        public void ResetRoutine()
        {
            _id = 0;
            _steps.Clear();

            loop = 0;
            loopCount = 0;

            state = STATE.IDLE;
            counter.Start(60 * 60 * 100);   //默认1小时

            RoutineToken.Result = RoutineState.Running;
        }
        protected void PerformRoutineStep(int id, Func<RoutineState> execution, RoutineResult result)
        {
            if (!Acitve(id))
                return;

            result.Result = execution();
        }



        #region interface

        public void StopLoop()
        {
            loop = loopCount;
        }

        public Tuple<bool, Result> Loop<T>(T id, Func<bool> func, int count)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (!func())
                {
                    return Tuple.Create(bActive, Result.FAIL);   //执行错误
                }

                loopID = idx;
                loopCount = count;

                next();
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> EndLoop<T>(T id, Func<bool> func)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (++loop >= loopCount)   //Loop 结束
                {
                    if (!func())
                    {
                        return Tuple.Create(bActive, Result.FAIL);   //执行错误
                    }

                    loop = 0;
                    loopCount = 0;  // Loop 结束时，当前loop和loop总数都清零

                    next();
                    return Tuple.Create(true, Result.RUN);
                }

                //继续下一LOOP

                next(loopID);

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, IRoutine routine)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    Result startRet = routine.Start();
                    if (startRet == Result.FAIL)
                    {
                        return Tuple.Create(true, Result.FAIL);   //执行错误
                    }
                    else if (startRet == Result.DONE)
                    {
                        next();
                        return Tuple.Create(true, Result.DONE);
                    }
                    state = STATE.WAIT;
                }

                Result ret = routine.Monitor();

                if (ret == Result.DONE)
                {
                    next();
                    return Tuple.Create(true, Result.DONE);
                }
                else if (ret == Result.FAIL || ret == Result.TIMEOUT)
                {
                    return Tuple.Create(true, Result.FAIL);
                }
                else
                {
                    return Tuple.Create(true, Result.RUN);
                }

            }

            return Tuple.Create(false, Result.RUN);
        }


        public Tuple<bool, Result> ExecuteAndWait<T>(T id, List<IRoutine> routines)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    foreach (var item in routines)
                    {
                        if (item.Start() == Result.FAIL)
                            return Tuple.Create(true, Result.FAIL);
                    }

                    state = STATE.WAIT;
                }
                //wait all sub failed or completedboo

                bool bFail = false;
                bool bDone = true;

                foreach (var item in routines)
                {
                    Result ret = item.Monitor();

                    bDone &= (ret == Result.FAIL || ret == Result.DONE);
                    bFail |= ret == Result.FAIL;
                }

                if (bDone)
                {
                    next();

                    if (bFail)
                        return Tuple.Create(true, Result.FAIL);

                    return Tuple.Create(true, Result.DONE);
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }



        public Tuple<bool, Result> Check<T>(T id, Func<bool> func)   //顺序执行
        {
            return Check(Check(Convert.ToInt32(id), func));
        }

        public Tuple<bool, Result> Execute<T>(T id, Func<bool> func)   //顺序执行
        {
            return Check(execute(Convert.ToInt32(id), func));
        }

        public Tuple<bool, Result> Wait<T>(T id, Func<bool> func, double timeout = int.MaxValue)  //Wait condition
        {
            return Check(wait(Convert.ToInt32(id), func, timeout));
        }

        public Tuple<bool, Result> Wait<T>(T id, Func<bool?> func, double timeout = int.MaxValue)  //Wait condition
        {
            return Check(wait(Convert.ToInt32(id), func, timeout));
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, Func<bool> execute, Func<bool?> check, double timeout = int.MaxValue)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            bool? bExecute = false;

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    if (!execute())
                    {
                        return Tuple.Create(bActive, Result.FAIL);   //执行错误
                    }
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = check();

                if (bExecute == null)
                {
                    return Tuple.Create(bActive, Result.FAIL);    //Termianate
                }
                else
                {
                    if (bExecute.Value)       //检查Success, next
                    {
                        next();
                        return Tuple.Create(true, Result.RUN);
                    }
                }

                if (timer.IsTimeout())
                    return Tuple.Create(true, Result.TIMEOUT);

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> ExecuteAndWait<T>(T id, Func<bool> execute, Func<bool?> check, Func<double> time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            bool? bExecute = false;
            double timeout = 0;
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timeout = time();
                    if (!execute())
                    {
                        return Tuple.Create(true, Result.FAIL);   //执行错误
                    }
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = check();

                if (bExecute == null)
                {
                    return Tuple.Create(true, Result.FAIL);    //Termianate
                }
                if (bExecute.Value)       //检查Success, next
                {
                    next();
                    return Tuple.Create(true, Result.RUN);
                }

                if (timer.IsTimeout())
                    return Tuple.Create(true, Result.TIMEOUT);

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        public Tuple<bool, Result> Wait<T>(T id, IRoutine rt)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    rt.Start();
                    state = STATE.WAIT;
                }

                Result ret = rt.Monitor();

                return Tuple.Create(true, ret);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //Monitor
        public Tuple<bool, Result> Monitor<T>(T id, Func<bool> func, Func<bool> check, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            bool bCheck = false;
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    if ((func != null) && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }

                    timer.Start(time);
                    state = STATE.WAIT;
                }

                bCheck = check();

                if (!bCheck)
                {
                    return Tuple.Create(true, Result.FAIL);    //Termianate
                }

                if (timer.IsTimeout())
                {
                    next();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //Delay
        public Tuple<bool, Result> Delay<T>(T id, Func<bool> func, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    if ((func != null) && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }

                    timer.Start(time);
                    state = STATE.WAIT;
                }

                if (timer.IsTimeout())
                {
                    next();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        //先delay 再运行
        public Tuple<bool, Result> DelayCheck<T>(T id, Func<bool> func, double time)
        {
            int idx = Convert.ToInt32(id);
            bool bActive = Acitve(idx);
            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timer.Start(time);
                    state = STATE.WAIT;
                }

                if (timer.IsTimeout())
                {
                    if (func != null && !func())
                    {
                        return Tuple.Create(true, Result.FAIL);
                    }
                    next();
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }
        #endregion


        private Tuple<bool, bool> execute(int id, Func<bool> func)   //顺序执行
        {
            bool bActive = Acitve(id);
            bool bExecute = false;
            if (bActive)
            {
                bExecute = func();
                if (bExecute)
                {
                    next();
                }
            }

            return Tuple.Create(bActive, bExecute);
        }


        private Tuple<bool, bool> Check(int id, Func<bool> func)   //check
        {
            bool bActive = Acitve(id);
            bool bExecute = false;
            if (bActive)
            {
                bExecute = func();
                next();
            }

            return Tuple.Create(bActive, bExecute);
        }


        /// <summary>

        /// </summary>
        /// <param name="id"></param>
        /// <param name="func"></param>
        /// <param name="timeout"></param>
        /// <returns>
        ///  item1 Active
        ///  item2 execute
        ///  item3 Timeout
        ///</returns>

        private Tuple<bool, bool, bool> wait(int id, Func<bool> func, double timeout = int.MaxValue)  //Wait condition
        {
            bool bActive = Acitve(id);
            bool bExecute = false;
            bool bTimeout = false;

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = func();
                if (bExecute)
                {
                    next();
                }

                bTimeout = timer.IsTimeout();
            }

            return Tuple.Create(bActive, bExecute, bTimeout);
        }

        private Tuple<bool, bool?, bool> wait(int id, Func<bool?> func, double timeout = int.MaxValue)  //Wait condition && Check error
        {
            bool bActive = Acitve(id);
            bool? bExecute = false;
            bool bTimeout = false;

            if (bActive)
            {
                if (state == STATE.IDLE)
                {
                    timer.Start(timeout);
                    state = STATE.WAIT;
                }

                bExecute = func();
                if (bExecute.HasValue && bExecute.Value)
                {
                    next();
                }

                bTimeout = timer.IsTimeout();
            }

            return Tuple.Create(bActive, bExecute, bTimeout);
        }

        /// <summary>      
        /// </summary>
        /// <param name="value"></param>
        /// <returns>
        /// item1 true, return item2
        /// </returns>
        private Tuple<bool, Result> Check(Tuple<bool, bool> value)
        {
            if (value.Item1)
            {
                if (!value.Item2)
                {
                    return Tuple.Create(true, Result.FAIL);
                }

                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        private Tuple<bool, Result> Check(Tuple<bool, bool, bool> value)
        {
            if (value.Item1)   // 当前执行
            {
                if (CheckTimeout(value))  //timeout
                {
                    return Tuple.Create(true, Result.TIMEOUT);
                }
                return Tuple.Create(true, Result.RUN);
            }

            return Tuple.Create(false, Result.RUN);
        }

        private Tuple<bool, Result> Check(Tuple<bool, bool?, bool> value)
        {
            if (value.Item1)   // 当前执行
            {
                if (value.Item2 == null)
                {
                    return Tuple.Create(true, Result.FAIL);
                }
                else
                {
                    if (value.Item2 == false && value.Item3 == true)  //timeout
                    {
                        return Tuple.Create(true, Result.TIMEOUT);
                    }
                    return Tuple.Create(true, Result.RUN);
                }
            }

            return Tuple.Create(false, Result.RUN);
        }

        private bool CheckTimeout(Tuple<bool, bool, bool> value)
        {
            return value.Item1 == true && value.Item2 == false && value.Item3 == true;
        }

        private bool Acitve(int id) //
        {
            if (_steps.Contains(id))
                return false;

            this._id = id;
            return true;
        }

        private void next()
        {
            _steps.Push(this._id);
            state = STATE.IDLE;
        }

        private void next(int step)   //loop
        {
            while (_steps.Pop() != step) ;

            state = STATE.IDLE;
        }


        public void Delay(int id, double delaySeconds)
        {
            Tuple<bool, Result> ret = Delay(id, () =>
            {
                return true;
            }, delaySeconds * 1000);

            if (ret.Item1)
            {
                if (ret.Item2 == Result.RUN)
                {
                    throw (new RoutineBreakException());
                }
            }
        }



        public bool IsActived(int id)
        {
            return _steps.Contains(id);
        }




        #endregion






    }

}
