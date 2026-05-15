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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotHirataR4
{
    public class HirataR4Robot : RobotBaseDevice, IConnection
    {
        private bool isSimulatorMode;
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


        private HirataR4RobotConnection _connection;
        public HirataR4RobotConnection Connection
        {
            get { return _connection; }
        }

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private R_TRIG _trigActionDone = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

        public List<IOResponse> IOResponseList { get; set; } = new List<IOResponse>();


        private object _locker = new object();

        private bool _enableLog;

        public int Axis { get; private set; }

        private float _armPitch { get; set; }

        private string _scRoot;
        public bool DIReadValue { get; set; }

        private bool[,] di_Values { get; set; } = new bool[8,8];
        


        public HirataR4Robot(string module, string name, string scRoot, string portName) : base(module, name)
        {

            isSimulatorMode = SC.ContainsItem("System.IsSimulatorMode") ? SC.GetValue<bool>("System.IsSimulatorMode"):false;
            _scRoot = scRoot;
            PortName = portName;


            //base.Initialize();
            ResetPropertiesAndResponses();
            RegisterSpecialData();

            _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            Axis = SC.GetValue<int>($"{_scRoot}.{Name}.RobotAxis");
            _armPitch = SC.GetValue<int>($"{_scRoot}.{Name}.ArmPitch") / 100;
            PortName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");
            _connection = new HirataR4RobotConnection(PortName);
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
            return ((!_connection.IsBusy) && (_lstHandler.Count == 0) && (IsOnLine) && (ACalCompleted) &&
                (MovingCompleted)&&(!_connection.IsCommunicationError) && (!IsBusy) && (RobotState == RobotStateEnum.Idle));
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
                    if (_lstHandler.Count == 0 && (!_connection.IsBusy) && 
                        ((!IsOnLine) || (!ACalCompleted) || (!MovingCompleted)))
                    {
                        //foreach (var monitorHandler in _lstMonitorHandler)
                        //{
                        //    _lstHandler.AddLast(monitorHandler);
                        //}
                        Thread.Sleep(500);
                        handler = new HirataR4RobotMonitorRobotStatusHandler(this);
                        _connection.Execute(handler);
                        return true;
                    }

                    _trigActionDone.CLK = (_lstHandler.Count == 0 && (!_connection.IsBusy)&&
                        (IsOnLine) && (ACalCompleted) && (MovingCompleted));

                    if (_trigActionDone.Q)
                        OnActionDone(null);


                    if (_lstHandler.Count > 0)
                    {
                        if (!_connection.IsBusy)
                        {
                            if ((IsOnLine) && (ACalCompleted) && (MovingCompleted))
                            {
                                handler = _lstHandler.First.Value;
                                _lstHandler.RemoveFirst();
                            }
                            else
                            {
                                Thread.Sleep(100);
                                handler = new HirataR4RobotMonitorRobotStatusHandler(this);
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
                _lstHandler.AddLast(new HirataR4RobotRawCommandHandler(this, command, comandArgument));
            }
        }

        public void PerformRawCommand(string command)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HirataR4RobotRawCommandHandler(this, command));
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
                _lstHandler.AddLast(new HirataR4RobotWritePositionHandler(this, address, x, y, z, w, 0, 0, 
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
                _lstMonitorHandler.Clear();
                _connection.ForceClear();
                _connection.Execute(new HirataR4RobotSimpleActionHandler(this, "GD"));
                _connection.ForceClear();
            }
        }
        public void MoveReset()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HirataR4RobotSimpleActionHandler(this, "GE"));
                _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
            }
        }
        public void ErrorClear()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new HirataR4RobotSimpleActionHandler(this, "CL"));
                _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
            }
        }

        public void MonitorRawCommand(bool isSelected, string command, string comandArgument)
        {
            lock (_locker)
            {
                string msg = comandArgument == null ? $"{command}\r" : $"{command} {comandArgument}\r";

                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(HirataR4RobotRawCommandHandler) && ((HirataR4RobotRawCommandHandler)handler)._parameter == comandArgument);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new HirataR4RobotRawCommandHandler(this, command, comandArgument));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }
        public void MonitorRawCommand(bool isSelected, string command)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(HirataR4RobotRawCommandHandler) && ((HirataR4RobotRawCommandHandler)handler)._parameter == null);
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new HirataR4RobotRawCommandHandler(this, command));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
        }
       
        public void ReadPosition(int address)
        {
           
            _lstHandler.AddLast(new HirataR4RobotReadRobotPositionHandler(this, address));
               
        }
        public void RobotConnected(bool isSelected)
        {
            lock (_locker)
            {
                var existHandlers = _lstMonitorHandler.Where(handler => handler.GetType() == typeof(HirataR4RobotMonitorRobotConnectedHandler));
                if (isSelected)
                {
                    if (!existHandlers.Any())
                        _lstMonitorHandler.AddFirst(new HirataR4RobotMonitorRobotConnectedHandler(this));
                }
                else
                {
                    if (existHandlers.Any())
                    {
                        _lstMonitorHandler.Remove(existHandlers.First());
                    }
                }
            }
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
                _lstMonitorHandler.Clear();
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
            List<string> strList = new List<string>();
            var statusArray = statusData.ToArray();
            int segmentStartIndex = 0;
            for (int index = 0; index < statusArray.Length; index++)
            {
                if ((statusArray[index] == '-' || statusArray[index] == ' ') && index > 0)
                {
                    strList.Add(statusData.Substring(segmentStartIndex, index - segmentStartIndex).Trim());
                    segmentStartIndex = index;
                }
            }
            strList.Add(statusData.Substring(segmentStartIndex, statusArray.Length - segmentStartIndex).Trim());

            if (Axis == 6)
            {
                float tempvalue;
                if (float.TryParse(strList[0], out tempvalue))
                    ReadXPosition = tempvalue;
                if(float.TryParse(strList[1], out tempvalue))
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
        protected override bool fStartTransferWafer(object[] param)
        {
            return true;
        }

        protected override bool fStartUnGrip(object[] param)
        {
            RobotArmEnum arm = (RobotArmEnum)param[0];
            if (arm == RobotArmEnum.Lower)
            {
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 1, true));
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 0, false));
            }
            if(arm == RobotArmEnum.Upper)
            {
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 3, true));
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 2, false));
            }
            if(arm == RobotArmEnum.Both)
            {
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 1, true));
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 0, false));
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 3, true));
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 2, false));

            }
            _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
            return true;
        }

        protected override bool fStartGrip(object[] param)
        {
            RobotArmEnum arm = (RobotArmEnum)param[0];
            if (arm == RobotArmEnum.Lower)
            {
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 0, true));
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 1, false));
            }
            if (arm == RobotArmEnum.Upper)
            {
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 2, true));
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 3, false));
            }
            if (arm == RobotArmEnum.Both)
            {
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 0, true));
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 1, false));
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 2, true));
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 3, false));

            }
            _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
            return true;
        }

        protected override bool fStartGoTo(object[] param)
        {
            try
            {
                if (param.Length == 1)
                {
                    int address = Convert.ToInt16(param[0]);
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HirataR4RobotMoveToPositionHandler(this, address));
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
                            _lstHandler.AddLast(new HirataR4RobotMoveToPositionWithDeviationHandler(this, "A",
                            address, xoffset, yoffset, zoffset, woffset));
                        }
                    }
                    else if (pos == RobotPostionEnum.PlaceReady)
                    {
                        zoffset += slowupdistance;
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataR4RobotMoveToPositionWithDeviationHandler(this, "A",
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

                            _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, motion, x, y, z, w));

                        else
                        {
                            int intaddress;
                            if (!int.TryParse(address, out intaddress)) return false;
                            _lstHandler.AddLast(new HirataR4RobotMoveToPositionWithDeviationHandler(this, motion, intaddress, x, y, z, w));
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

        protected override bool fStartMapWafer(object[] param)
        {
            ModuleName module = (ModuleName)param[0];
            int[] mappingaddress = GetMappingAddress(module);
            if (mappingaddress == null || mappingaddress.Length !=6) return false;
            _currentMotionStation = module;

            int thicknessIndex = 0;
            if (param.Length > 1)
                thicknessIndex = Convert.ToInt32(param[1]);

            EV.PostInfoLog(Module, $"Start mapping, {module} thickness type {thicknessIndex} ");

            if (thicknessIndex > 0 && thicknessIndex < 10)
            {
                lock (_locker)
                {
                    _lstHandler.AddLast(new HirataR4RobotReadRobotPositionHandler(this, mappingaddress[1]));
                }
                int waitCount = 0;
                while(_lstHandler.Count !=0 ||_connection.IsBusy)
                {
                    waitCount++;
                    Thread.Sleep(10);
                    if(waitCount >100)
                    {
                        OnError("MappingTimeout");
                        IsBusy = false;
                        return false;
                    }
                }
                lock (_locker)
                {
                    _lstHandler.AddLast(new HirataR4RobotWriteMdataOnCurrenPositionHandler(this, mappingaddress[1], $"0{thicknessIndex}"));
                }
            }
            lock (_locker)
            {
                _lstHandler.AddLast(new HirataR4RobotMoveToPositionHandler(this, mappingaddress[0], null));
                _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                _lstHandler.AddLast(new HirataR4RobotMoveToPositionHandler(this, mappingaddress[1], null));
                _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                _lstHandler.AddLast(new HirataR4RobotRawCommandHandler(this, "SM", "1"));
                _lstHandler.AddLast(new HirataR4RobotMoveToPositionHandler(this, mappingaddress[2], null));
                _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                _lstHandler.AddLast(new HirataR4RobotMoveToPositionHandler(this, mappingaddress[3], null));
                _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                _lstHandler.AddLast(new HirataR4RobotMoveToPositionHandler(this, mappingaddress[4], null));
                _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                _lstHandler.AddLast(new HirataR4RobotReadHandler(this, "LE", "100"));
                _lstHandler.AddLast(new HirataR4RobotReadHandler(this, "LE", "101"));
                _lstHandler.AddLast(new HirataR4RobotRawCommandHandler(this, "SM", "0"));  //Close laser
                _lstHandler.AddLast(new HirataR4RobotMoveToPositionHandler(this, mappingaddress[5], null));
                _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                ReadCurrentPostion();

            }
            return true;
        }

        protected override bool fMapComplete(object[] param)
        {
            
            char[] exists = MapExistInfor.ToArray();
            char[] ngs = MapNGInfor.ToArray();

            for (int i = 0; i < 25; i++)
            {
                if (ngs[i] != '0') exists[i] = '2';
            }
            NotifySlotMapResult(_currentMotionStation, new string(exists));
            
            return base.fMapComplete(param);
        }

        protected override bool fStartSwapWafer(object[] param)
        {
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
            zoffset = zoffset + (slot) * GetWaferPitch(station, wafersize) + _armPitch;
            float insertdistance = GetInsertDistance(station, wafersize);
            float slowupdistance = GetSlowUpDistance(station, wafersize);
            lock (_locker)
            {
                if (arm == RobotArmEnum.Lower)
                {
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset + _armPitch, woffset));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", insertdistance, 0, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "U", 0, 0, slowupdistance, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 0, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 1, false));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", (-1) * insertdistance, insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 3, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 2, false));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "U", 0, 0, -(slowupdistance+ _armPitch), 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", 0, -insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotReadDIByteHandler(this, 0));

                    //WaferManager.Instance.WaferMoved( station, slot, ModuleName.Robot, 0);
                    //WaferManager.Instance.WaferMoved(ModuleName.Robot, 1,station, slot);

                }
                if (arm == RobotArmEnum.Upper)
                {
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset, woffset));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", 0, insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "U", 0, 0, slowupdistance+_armPitch, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 2, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 3, false));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", insertdistance, -insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 1, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 0, false));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "U", 0, 0, -slowupdistance, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I",  -insertdistance,0, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotReadDIByteHandler(this, 0));

                    //WaferManager.Instance.WaferMoved(station, slot, ModuleName.Robot, 1);
                    //WaferManager.Instance.WaferMoved(ModuleName.Robot, 0, station, slot);

                }
                ReadCurrentPostion();
                return true;
            }
        }

        protected override bool fSwapComplete(object[] param)
        {
            if (GetWaferState(_currentMotionArm) == RobotArmWaferStateEnum.Present || isSimulatorMode)
            {
                if (_currentMotionArm == RobotArmEnum.Lower || _currentMotionArm == RobotArmEnum.Upper)
                    WaferManager.Instance.WaferMoved(_currentMotionStation, _currentMotionSlot,
                        ModuleName.Robot, (int)_currentMotionArm);
                else
                {
                    WaferManager.Instance.WaferMoved(_currentMotionStation, _currentMotionSlot,
                        ModuleName.Robot, 0);
                    WaferManager.Instance.WaferMoved(_currentMotionStation, _currentMotionSlot + 1,
                        ModuleName.Robot, 1);
                }
            }
            else
            {
                _lstHandler.Clear();
                OnError($"Detect wafer absent on {_currentMotionArm}");
            }

            RobotArmEnum otherarm = GetAnotherArm(_currentMotionArm);

            if (GetWaferState(otherarm) == RobotArmWaferStateEnum.Present)
            {
                if (otherarm == RobotArmEnum.Lower || otherarm == RobotArmEnum.Upper)
                    WaferManager.Instance.WaferMoved(ModuleName.Robot, (int)otherarm,
                        _currentMotionStation, _currentMotionSlot);
                else
                {
                    WaferManager.Instance.WaferMoved(ModuleName.Robot, 0,
                        _currentMotionStation, _currentMotionSlot);
                    WaferManager.Instance.WaferMoved(ModuleName.Robot, 1,
                        _currentMotionStation, _currentMotionSlot + 1);
                }
            }
            else
            {
                _lstHandler.Clear();
                OnError($"Detect wafer present on {_currentMotionArm}");
            }         



            return base.fSwapComplete(param);
        }

        protected override bool fStartPlaceWafer(object[] param)
        {
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
            WaferSize wafersize = WaferManager.Instance.GetWaferSize(ModuleName.Robot, 
                (int)(arm == RobotArmEnum.Both ? RobotArmEnum.Lower : arm));
            float insertdistance = GetInsertDistance(station, wafersize);
            float slowupdistance = GetSlowUpDistance(station, wafersize);
            int address = GetBaseAddress(station, wafersize);
            zoffset = zoffset + (slot) * GetWaferPitch(station, wafersize) + slowupdistance;
            
            lock (_locker)
            {
                if (arm == RobotArmEnum.Lower)
                {
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset+ _armPitch, woffset));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", insertdistance, 0, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 1, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 0, false));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "U", 0, 0, (-1)*slowupdistance, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));

                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 0, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 1, false));

                    _lstHandler.AddLast(new HirataR4RobotWaferInforMoveHandler(this, 0, false));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 1, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 0, false));


                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", (-1) * insertdistance, 0, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotReadDIByteHandler(this, 0));
                    //WaferManager.Instance.WaferMoved(ModuleName.Robot, 0, station, slot);


                }
                if (arm == RobotArmEnum.Upper)
                {
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset, woffset));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", 0, insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 3, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 2, false));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "U", 0, 0, (-1)*slowupdistance, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));


                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 2, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 3, false));

                    _lstHandler.AddLast(new HirataR4RobotWaferInforMoveHandler(this, 0, false));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 3, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 2, false));


                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", 0, (-1) * insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotReadDIByteHandler(this, 0));

                    //WaferManager.Instance.WaferMoved(ModuleName.Robot, 1, station, slot);

                }
                if (arm == RobotArmEnum.Both)
                {
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset + _armPitch, woffset));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", insertdistance, insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 1, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 0, false));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 3, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 2, false));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "U", 0, 0, (-1)*slowupdistance, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));

                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 0, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 1, false));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 2, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 3, false));

                    _lstHandler.AddLast(new HirataR4RobotWaferInforMoveHandler(this, 0, false));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 1, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 0, false));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 3, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 2, false));

                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", (-1) * insertdistance, (-1) * insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotReadDIByteHandler(this, 0));

                    //WaferManager.Instance.WaferMoved(ModuleName.Robot, 0, station, slot);
                    //WaferManager.Instance.WaferMoved(ModuleName.Robot, 1, station, slot+1);

                }
                ReadCurrentPostion();
                return true;
            }
        }
        public void MoveWaferInforToStation()
        {
            if (GetWaferState(_currentMotionArm) == RobotArmWaferStateEnum.Absent || isSimulatorMode)
            {
                if (_currentMotionArm == RobotArmEnum.Lower || _currentMotionArm == RobotArmEnum.Upper)
                    WaferManager.Instance.WaferMoved(ModuleName.Robot, (int)_currentMotionArm,
                        _currentMotionStation, _currentMotionSlot);
                else
                {
                    WaferManager.Instance.WaferMoved(ModuleName.Robot, 0,
                        _currentMotionStation, _currentMotionSlot);
                    WaferManager.Instance.WaferMoved(ModuleName.Robot, 1,
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
                        ModuleName.Robot, (int)_currentMotionArm);
                else
                {
                    WaferManager.Instance.WaferMoved(_currentMotionStation, _currentMotionSlot,
                        ModuleName.Robot, 0);
                    WaferManager.Instance.WaferMoved(_currentMotionStation, _currentMotionSlot + 1,
                        ModuleName.Robot, 1);
                }
            }
            else
            {
                _lstHandler.Clear();
                OnError($"Detect wafer absent on {_currentMotionArm}");
            }
        }

        protected override bool fPlaceComplete(object[] param)
        {

            if (GetWaferState(_currentMotionArm) == RobotArmWaferStateEnum.Absent || isSimulatorMode)
            {
                
            }
            else
            {
                _lstHandler.Clear();
                OnError($"Detect wafer present on {_currentMotionArm}");
            }
            return base.fPlaceComplete(param);
        }

        protected override bool fStartPickWafer(object[] param)
        {
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
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset + _armPitch, woffset));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", insertdistance, 0, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                   
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 0, true));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 1, false));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "U", 0, 0, slowupdistance, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));

                    _lstHandler.AddLast(new HirataR4RobotWaferInforMoveHandler(this, 0, true));




                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", (-1)*insertdistance, 0, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotReadDIByteHandler(this, 0));
                    //WaferManager.Instance.WaferMoved(station, slot, ModuleName.Robot, 0);
                }
                if(arm == RobotArmEnum.Upper)
                {
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset, woffset));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", 0, insertdistance,  0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                     _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 2, true));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 3, false));

                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "U", 0, 0, slowupdistance, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));


                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));

                    _lstHandler.AddLast(new HirataR4RobotWaferInforMoveHandler(this, 0, true));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", 0, (-1) * insertdistance,  0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotReadDIByteHandler(this, 0));

                    //WaferManager.Instance.WaferMoved(station, slot, ModuleName.Robot, 1);

                }
                if (arm == RobotArmEnum.Both)
                {
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveToPositionWithDeviationHandler(this, "A", address, xoffset, yoffset, zoffset + _armPitch, woffset));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", insertdistance, insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                      _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 0, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 1, false));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 2, true));
                    _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 3, false));

                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "U", 0, 0, slowupdistance, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));

                    _lstHandler.AddLast(new HirataR4RobotWaferInforMoveHandler(this, 0, true));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotMoveDeviationHandler(this, "I", (-1) * insertdistance, (-1) * insertdistance, 0, 0));
                    _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                    _lstHandler.AddLast(new HirataR4RobotReadDIByteHandler(this, 0));                    
                }
                ReadCurrentPostion();
                return true;
            }
        }

        protected override bool fPickComplete(object[] param)
        {
            if (GetWaferState(_currentMotionArm) == RobotArmWaferStateEnum.Present || isSimulatorMode)
            {

            }
            else
            {
                _lstHandler.Clear();
                OnError($"Detect wafer absent on {_currentMotionArm}");
            }

            return base.fPickComplete(param);
        }
        protected override bool fResetToReady(object[] param)
        {
            return true;
        }

        protected override bool fReset(object[] param)
        {
            _connection.SetCommunicationError(false, "");

            if (!_connection.IsConnected)
            {
                _connection.Disconnect();
                _connection.Connect();
            }

            lock (_locker)
            {
                _lstHandler.AddLast(new HirataR4RobotSimpleActionHandler(this, "GE"));
                _lstHandler.AddLast(new HirataR4RobotRawCommandHandler(this, "CL"));
            }
                return true;// throw new NotImplementedException();
        }

        protected override bool fStartInit(object[] param)
        {
            lock (_locker)
            {

                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 0, true));
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 1, false));
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 2, true));
                _lstHandler.AddLast(new HirataR4RobotWriteDOHandler(this, 3, false));
                ReadCurrentPostion();
                _lstHandler.AddLast(new HirataR4RobotReadDIByteForInitHandler(this, 0));
            }
            int waitCount = 0;
            while(_lstHandler.Count !=0 || _connection.IsBusy)
            {
                Thread.Sleep(10);
                waitCount++;
                if(waitCount >500)
                {
                    OnError("HomeTimeOut");
                    IsBusy = false;
                    return false;
                }
            }
            lock (_locker)
            { 
                _lstHandler.AddLast(new HirataR4RobotOperateVacDependsOnWaferHandler(this, 0));
                _lstHandler.AddLast(new HirataR4RobotOperateVacDependsOnWaferHandler(this, 1));
                _lstHandler.AddLast(new HirataR4RobotOperateVacDependsOnWaferHandler(this, 2));
                _lstHandler.AddLast(new HirataR4RobotOperateVacDependsOnWaferHandler(this, 3));
                _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                _lstHandler.AddLast(new HirataR4RobotMoveToPositionHandler(this, 0, "A"));
                _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
                _lstHandler.AddLast(new HirataR4RobotMoveToPositionHandler(this, 1, "A"));
                _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this));
            }
            return true;
        }
        //protected override bool fMonitorInit(object[] param)
        //{
        //    if (_lstHandler.Count == 0 && !_connection.IsBusy && (IsOnLine) && (ACalCompleted) && (MovingCompleted))
        //        return true;
        //    return base.fMonitorInit(param);
        //}
        protected override bool fInitComplete(object[] param)
        {
            if(GetWaferState(RobotArmEnum.Lower) == RobotArmWaferStateEnum.Present)
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





            return base.fInitComplete(param);
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
            _lstHandler.AddLast(new HirataR4RobotReadHandler(this, "LR", null));
        }
        private float GetWaferPitch(ModuleName module,WaferSize wz)
        {
            if (ModuleHelper.IsLoadPort(module))
            {
                int carrierindex = SC.GetValue<int>($"CarrierInfo.{module.ToString()}CarrierIndex");

                    return SC.ContainsItem($"CarrierInfo.CarrierWaferPitch{carrierindex}") ?
                        (float)SC.GetValue<int>($"CarrierInfo.CarrierWaferPitch{carrierindex}") / 100 : 20;              

            }
            if (wz == WaferSize.WS12)
                return (float)SC.GetValue<int>($"CarrierInfo.{module.ToString()}CarrierWaferPitch0")/100;
            if (wz == WaferSize.WS8)
                return (float)SC.GetValue<int>($"CarrierInfo.{module.ToString()}CarrierWaferPitch1") / 100;
            if (wz == WaferSize.WS6)
                return (float)SC.GetValue<int>($"CarrierInfo.{module.ToString()}CarrierWaferPitch2") / 100;
            if (wz == WaferSize.WS4)
                return (float)SC.GetValue<int>($"CarrierInfo.{module.ToString()}CarrierWaferPitch3") / 100;
            if (wz == WaferSize.WS3)
                return (float)SC.GetValue<int>($"CarrierInfo.{module.ToString()}CarrierWaferPitch4") / 100;
            return 20;
        }
        private float GetInsertDistance(ModuleName module, WaferSize wz)
        {
            if (ModuleHelper.IsLoadPort(module))
            {
                int carrierindex = SC.GetValue<int>($"CarrierInfo.{module.ToString()}CarrierIndex");
                return SC.ContainsItem($"CarrierInfo.InsertDistance{carrierindex}") ?
                    (float)SC.GetValue<int>($"CarrierInfo.InsertDistance{carrierindex}") / 100 : 300;
            }
            if(wz == WaferSize.WS12)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}InsertDistance0")/100;
            if (wz == WaferSize.WS8)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}InsertDistance1") / 100;
            if (wz == WaferSize.WS6)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}InsertDistance2") / 100;
            if (wz == WaferSize.WS4)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}InsertDistance3") / 100;
            if (wz == WaferSize.WS3)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}InsertDistance4") / 100;
            return (float)SC.GetValue<int>($"CarrierInfo.{module}InsertDistance0") / 100;
        }

        private float GetSlowUpDistance(ModuleName module,WaferSize wz)
        {
            if (ModuleHelper.IsLoadPort(module))
            {
                int carrierindex = SC.GetValue<int>($"CarrierInfo.{module.ToString()}CarrierIndex");
                return SC.ContainsItem($"CarrierInfo.SlowUpDistance{carrierindex}") ?
                    (float)SC.GetValue<int>($"CarrierInfo.SlowUpDistance{carrierindex}") / 100 : 10;
            }
            if (wz == WaferSize.WS12)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}SlowUpDistance0")/100;
            if (wz == WaferSize.WS8)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}SlowUpDistance1") / 100;

            if (wz == WaferSize.WS6)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}SlowUpDistance2") / 100;

            if (wz == WaferSize.WS4)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}SlowUpDistance3") / 100;
            if (wz == WaferSize.WS3)
                return (float)SC.GetValue<int>($"CarrierInfo.{module}SlowUpDistance4") / 100;
            return (float)SC.GetValue<int>($"CarrierInfo.{module}SlowUpDistance0") / 100;

        }
        private int GetBaseAddress(ModuleName module,WaferSize wz)
        {
            if (ModuleHelper.IsLoadPort(module))
            {
                int carrierindex = SC.GetValue<int>($"CarrierInfo.{module.ToString()}CarrierIndex");

                return SC.ContainsItem($"CarrierInfo.{module}Station{carrierindex}") ?
                    Convert.ToInt16(SC.GetStringValue($"CarrierInfo.{module}Station{carrierindex}")) : 0;
            }
            if (wz == WaferSize.WS12)
                return Convert.ToInt16(SC.GetStringValue($"CarrierInfo.{module}Station0"));
            if (wz == WaferSize.WS8)
                return Convert.ToInt16(SC.GetStringValue($"CarrierInfo.{module}Station1"));
            if (wz == WaferSize.WS6)
                return Convert.ToInt16(SC.GetStringValue($"CarrierInfo.{module}Station2"));
            if (wz == WaferSize.WS4)
                return Convert.ToInt16(SC.GetStringValue($"CarrierInfo.{module}Station3"));
            if (wz == WaferSize.WS3)
                return Convert.ToInt16(SC.GetStringValue($"CarrierInfo.{module}Station4"));

            return Convert.ToInt16(SC.GetStringValue($"CarrierInfo.{module}Station0"));
        }
        private int[] GetMappingAddress(ModuleName module)
        {
            int carrierindex = SC.GetValue<int>($"CarrierInfo.{module.ToString()}CarrierIndex");
            string addresses = SC.ContainsItem($"CarrierInfo.{module}MappingStation{carrierindex}") ?
                SC.GetStringValue($"CarrierInfo.{module}MappingStation{carrierindex}") : "";
            List<int> ret = new List<int>();
            foreach (var add in addresses.Split(','))
            {
                ret.Add(Convert.ToInt16(add));
            }
            if (ret.Count != 6)
            {
                EV.PostAlarmLog("Robot","Mapping address setting error!");
                return null;
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
                        _lstHandler.AddLast(new HirataR4RobotReadHandler(this, "LE", param[1].ToString()));
                        break;
                    case "CurrentStatus":
                        _lstHandler.AddLast(new HirataR4RobotMonitorRobotStatusHandler(this)); 
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
                            _lstHandler.AddLast(new HirataR4RobotReadHandler(this, "LE", (112 + (5 * (paraindex - 1))).ToString()));
                            _lstHandler.AddLast(new HirataR4RobotReadHandler(this, "LE", (610 + (5 * (paraindex - 1))).ToString()));
                            _lstHandler.AddLast(new HirataR4RobotReadHandler(this, "LE", (611 + (5 * (paraindex - 1))).ToString()));
                            _lstHandler.AddLast(new HirataR4RobotReadHandler(this, "LE", (612 + (5 * (paraindex - 1))).ToString()));
                            _lstHandler.AddLast(new HirataR4RobotReadHandler(this, "LE", (613 + (5 * (paraindex - 1))).ToString()));
                            _lstHandler.AddLast(new HirataR4RobotReadHandler(this, "LE", (614 + (5 * (paraindex - 1))).ToString()));
                            _lstHandler.AddLast(new HirataR4RobotReadHandler(this, "LE", (615 + (5 * (paraindex - 1))).ToString()));
                        }


                        break;
                    

                }
            }

            return true;
        }

        protected override bool fStartSetParameters(object[] param)
        {
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
                        _lstHandler.AddLast(new HirataR4RobotRawCommandHandler(this, "SP", $"{speed} {speed}"));
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
                            _lstHandler.AddLast(new HirataR4RobotRawCommandHandler(this, "SP", $"{ABWSpeed} {ZSpeed}"));
                        }
                    }
                    if (param[1].ToString() == "ACC/DEC")
                    {
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new HirataR4RobotRawCommandHandler(this, "SX", $"{ABWSpeed} {ZSpeed}"));
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
                    _lstHandler.AddLast(new HirataR4RobotWriteHandler(this, "SE", $"{param[1]} {param[2]}"));
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
                        _lstHandler.AddLast(new HirataR4RobotWriteHandler(this, "SE",
                            $"{(112 + (5 * (paraindex - 1)))} {param[2]}"));
                    }
                
                if(float.TryParse(param[3].ToString(),out _))
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HirataR4RobotWriteHandler(this, "SE",
                            $"{(610 + (5 * (paraindex - 1)))} {param[3]}"));
                    }
                if (float.TryParse(param[4].ToString(), out _))
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HirataR4RobotWriteHandler(this, "SE",
                            $"{(611 + (5 * (paraindex - 1)))} {param[4]}"));
                    }
                if (float.TryParse(param[5].ToString(), out _))
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HirataR4RobotWriteHandler(this, "SE",
                            $"{(612 + (5 * (paraindex - 1)))} {param[5]}"));
                    }
                if (float.TryParse(param[6].ToString(), out _))
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HirataR4RobotWriteHandler(this, "SE",
                            $"{(613 + (5 * (paraindex - 1)))} {param[6]}"));
                    }
                if (!string.IsNullOrEmpty(param[7].ToString()))
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new HirataR4RobotWriteHandler(this, "SE",
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
                _lstHandler.AddLast(new HirataR4RobotWriteHandler(this, "SI", $" {insertdistance} {Decratio}"));
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
                _lstHandler.AddLast(new HirataR4RobotWriteHandler(this, "SJ", $" {insertdistance} {Decratio}"));
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
                            _lstHandler.AddLast(new HirataR4RobotReadRobotPositionHandler(this, Convert.ToInt32(mappingstations[i])));
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
    }

}
