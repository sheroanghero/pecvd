using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using Aitex.Core.Common;
using Aitex.Core.Common.DeviceData;
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
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.TazmoRobot
{
    public enum TazmoRobotState
    {
        Standby = 0,
        TPInUse = 1,
        InOperation = 2,
        Running,
        Pause = 7,
        Initializing = 9,
        StandbyDueToUnInit = 0X10,
        RecoverabelErrorWithoutInit = 0X20,
        ErrorRequiredInit = 0xA0,
        ErrorRequiredPowerCycle = 0xC0,

    }
    public enum TazmoRobotPositionEnum
    {
        UnloadLoad,
        Mapping,
    }
    public enum TazmoRobotArmExtensionEnum
    {
        PositionToPull,
        ArmExtension,
    }
    public enum TazmoRobotWorkPresenceInfoEnum
    {
        NoWork,
        WorkExists,
        
    }
    public enum TazmoRobotGripPostionInfoEnum
    {
        Close,
        Oepn,
        WorkIsGripped,
        PostionUnknow = 0xF,
    }
    public enum TazmoRobotZPositionInfo
    {
        Down = 1,
        MidDown,
        Mid,
        MidUp,
        Up,
        Unknow = 0xF,
    }




    public class TazmoRobot : RobotBaseDevice, IConnection
    {
        private bool isSimulatorMode
        {
            get
            {
                 return SC.ContainsItem("System.IsSimulatorMode") ? SC.GetValue<bool>("System.IsSimulatorMode") : false;
            }
        }
        private string _scRoot;

        public TazmoRobotState DeviceState { get; private set; }

        public bool TaExecuteSuccss { get; set; }
        private TazmoRobotConnection _connection;
        public TazmoRobotConnection Connection => _connection;
        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigWarningMessage = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private PeriodicJob _thread;
        private static Object _locker = new Object();

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private IoSensor _diRobotReady = null;  //Normal ON
        private IoSensor _diRobotBlade1WaferOn = null;   //Off when wafer present
        private IoSensor _diRobotBlade2WaferOn = null;
        private IoSensor _diRobotError = null; //Normal ON
        private IoSensor _diTPinUse = null;

        private IoSensor _diRobotArmPos = null;
        private IoSensor _diRobotMove = null;
        private IoSensor _diRobotHome = null;

        private IoTrigger _doRobotHold = null; // Normal ON
        private IoTrigger _doRobotIn1 = null; // Normal ON
        private bool _enableLog = true;

        private bool _commErr = false;
        private string _address;

        
        public int CurrentStopPositionPoint { get; private set; }
        public int CurrentSlotNumber { get; private set; }

        public TazmoRobotPositionEnum CurrentPositionCate { get; private set; }
        public TazmoRobotArmExtensionEnum CurrentArmExtensionPos { get; private set; }
        //public TazmoRobotWorkPresenceInfoEnum CurrentWorkPresnece { get; private set; }

        public TazmoRobotWorkPresenceInfoEnum CurrentArm1WorkPresnece { get; private set; }
        public TazmoRobotWorkPresenceInfoEnum CurrentArm2WorkPresnece { get; private set; }
        public TazmoRobotGripPostionInfoEnum CurrentArm1GripperPosition { get; private set; }
        public TazmoRobotGripPostionInfoEnum CurrentArm2GripperPosition { get; private set; }
        public TazmoRobotZPositionInfo CurrentZpositionCate { get; private set; }

        public int R1AxisPosition { get; private set; }
        public int R2AxisPosition { get; private set; }
        public int ZAxisPosition { get; private set; }
        public int SAxisPosition { get; private set; }

        public string RobotSystemVersion { get; private set; }
        public string RobotSoftwareVersion { get; private set; }

        private string commasymbol = ",";
        private DateTime _dtActionStart;
        public string PortName { get; private set; }
        public TazmoRobot(string module, string name, string scRoot, IoSensor[] dis, IoTrigger[] dos) : base(module, name)
        {            
            _scRoot = scRoot;
            //base.Initialize();
            ResetPropertiesAndResponses();
            RegisterSpecialData();
            RegisterAlarm();
            if (dis != null && dis.Length >= 8)
            {
                _diRobotReady = dis[0];
                _diRobotBlade1WaferOn = dis[1];
                _diRobotBlade2WaferOn = dis[2];
                _diRobotError = dis[3];
                _diTPinUse = dis[4];
                _diRobotArmPos = dis[5];
                _diRobotMove = dis[6];
                _diRobotHome = dis[7];
                _diRobotError.OnSignalChanged += _diRobotError_OnSignalChanged;
                _diRobotMove.OnSignalChanged += _diRobotMove_OnSignalChanged;
                //_diTPinUse.OnSignalChanged += _diTPinUse_OnSignalChanged;
            }
            if (dos != null && dos.Length >= 1)
            {
                _doRobotIn1 = dos[0];
                if (_doRobotIn1 != null)
                    _doRobotIn1.SetTrigger(true, out _);
            }

            int bautRate = SC.GetValue<int>($"{scRoot}.{Name}.BaudRate");
            int dataBits = SC.GetValue<int>($"{scRoot}.{Name}.DataBits");
            Enum.TryParse(SC.GetStringValue($"{scRoot}.{Name}.Parity"), out Parity parity);
            Enum.TryParse(SC.GetStringValue($"{scRoot}.{Name}.StopBits"), out StopBits stopBits);
            _enableLog = SC.GetValue<bool>($"{scRoot}.{Name}.EnableLogMessage");
            PortName = SC.GetStringValue($"{scRoot}.{Name}.PortName");
            Address = PortName;
            _connection = new TazmoRobotConnection(this, PortName, bautRate, dataBits, parity, stopBits);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
                IsConnected = true;

            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            //_address = SC.GetStringValue($"{_scRoot}.{Name}.DeviceAddress");

            ConnectionManager.Instance.Subscribe($"{Name}", this);

      
        }

        public void ParseAxisPosition(string data)
        {
            try
            { 
                string[] datas = data.Replace("\r", "").Split(',');
                switch (datas[0])
                {
                    case "001":
                        R1AxisPosition = Convert.ToInt32(datas[2]);
                        break;
                    case "002":
                        R2AxisPosition = Convert.ToInt32(datas[2]);
                        break;
                    case "040":
                        ZAxisPosition = Convert.ToInt32(datas[2]);
                        break;
                    case "080":
                        SAxisPosition = Convert.ToInt32(datas[2]);
                        break;
                }
             }
            catch(Exception ex)
            {
                LOG.Write(ex);
            }







        }

        private void _diRobotMove_OnSignalChanged(IoSensor arg1, bool arg2)
        {
           
        }

        private void _diRobotError_OnSignalChanged(IoSensor arg1, bool arg2)
        {
            if (arg2)
            {
                lock (_locker)
                {
                    _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "STA", ""));
                }
                OnError("RobotError");
            }
        }
        public void ParseStatus(string state)
        {
            int statecode = Convert.ToInt32(state, 16);

            if (statecode <= 0x10) DeviceState = (TazmoRobotState)statecode;
            else
            {
                ErrorCode = statecode.ToString("X3");
                EV.Notify($"{Name}Error{ErrorCode}");
                OnError("RobotError");
            }

            if (statecode >= 0x20 && statecode <= 0x99) DeviceState = TazmoRobotState.RecoverabelErrorWithoutInit;
            if (statecode >= 0xA0 && statecode <= 0xBF) DeviceState = TazmoRobotState.ErrorRequiredInit;
            if (statecode > 0xC0 && statecode <= 0xFF) DeviceState = TazmoRobotState.ErrorRequiredPowerCycle;
        }

        public void ParseStatusAndPostion(string cmd,string statedata)
        {
            string[] data = statedata.Split(',');

            switch(cmd)
            {
                case "WCH":
                case "VCH":
                    if(data[0] == "1") CurrentArm1WorkPresnece = (TazmoRobotWorkPresenceInfoEnum)Convert.ToInt32(data[1]);
                    if(data[0] == "2") CurrentArm2WorkPresnece = (TazmoRobotWorkPresenceInfoEnum)Convert.ToInt32(data[1]);
                    break;
                case "STA":
                    if (data.Length != 10)
                    {
                        EV.PostAlarmLog("System", "Received invalid status and postion data:" + statedata);
                        return;
                    }
                    ParseStatus(data[0]);
                    CurrentStopPositionPoint = Convert.ToInt32(data[1]);

                    CurrentSlotNumber = Convert.ToInt32(data[2]);
                    CurrentPositionCate = (TazmoRobotPositionEnum)Convert.ToInt32(data[3]);
                    CurrentArmExtensionPos = (TazmoRobotArmExtensionEnum)Convert.ToInt32(data[4]);
                    
                    CurrentArm1WorkPresnece = (TazmoRobotWorkPresenceInfoEnum)Convert.ToInt32(data[5]);
                    CurrentArm2WorkPresnece = (TazmoRobotWorkPresenceInfoEnum)Convert.ToInt32(data[6]);
                    CurrentArm1GripperPosition = (TazmoRobotGripPostionInfoEnum)Convert.ToInt32(data[7]);
                    CurrentArm2GripperPosition = (TazmoRobotGripPostionInfoEnum)Convert.ToInt32(data[8]);
                    CurrentZpositionCate = (TazmoRobotZPositionInfo)Convert.ToInt32(data[9],16);
                    break;
            }



            
        }
        public void ParseAllStageSpeed(string statedata)
        {
            string[] data = statedata.Split(',');
            if (data.Length != 10)
            {
                // EV.PostAlarmLog("System", "Received Query Speed data:" + statedata);
                return;
            }
            ParseStatus(data[0]);
            Speed = Convert.ToInt32(data[3]);
        }
        private void RegisterAlarm()
        {
            EV.Subscribe(new EventItem(60011, "Alarm", $"{Name}Error", $"{Name} Occurred Error", EventLevel.Alarm, EventType.EventUI_Notify));
            for(int i = 0x20;i<=0x99; i++)
            {
                string errorcode = i.ToString("X3");
                EV.Subscribe(new EventItem(64000 +i,  "Alarm", $"{Name}Error{errorcode}", $"{Name} Occurred recoverable error without initialization,code:{errorcode}.", EventLevel.Alarm, EventType.EventUI_Notify));
            }
            for (int i = 0xA0; i <= 0xB0; i++)
            {
                EV.Subscribe(new EventItem(64000 + i, "Alarm", $"{Name}Error{i.ToString("X3")}", $"{Name} Occurred error require initialization,code:{i.ToString("X3")}.", EventLevel.Alarm, EventType.EventUI_Notify));
            }
            for (int i = 0xC0; i <= 0xFF; i++)
            {
                EV.Subscribe(new EventItem(64000 + i, "Alarm", $"{Name}Error{i.ToString("X3")}", $"{Name} Occurred error require power again,code:{i.ToString("X3")}.", EventLevel.Alarm, EventType.EventUI_Notify));
            }

        }
        private void RegisterSpecialData()
        {
            DATA.Subscribe($"{Name}.TazmoStatus", () => DeviceState.ToString());
            DATA.Subscribe($"{Name}.CurrentStopPositionPoint", () => CurrentStopPositionPoint.ToString());
            DATA.Subscribe($"{Name}.CurrentSlotNumber", () => CurrentSlotNumber.ToString());
            DATA.Subscribe($"{Name}.CurrentPositionCate", () => CurrentPositionCate.ToString());
            DATA.Subscribe($"{Name}.CurrentArmExtensionPos", () => CurrentArmExtensionPos.ToString());
            DATA.Subscribe($"{Name}.CurrentArm1WorkPresnece", () => CurrentArm1WorkPresnece.ToString());
            DATA.Subscribe($"{Name}.CurrentArm2WorkPresnece", () => CurrentArm2WorkPresnece.ToString());
            DATA.Subscribe($"{Name}.CurrentArm1GripperPosition", () => CurrentArm1GripperPosition.ToString());
            DATA.Subscribe($"{Name}.CurrentArm2GripperPosition", () => CurrentArm2GripperPosition.ToString());
            DATA.Subscribe($"{Name}.CurrentZpositionCate", () => CurrentZpositionCate.ToString());

            DATA.Subscribe($"{Name}.R1AxisPosition", () => R1AxisPosition.ToString());
            DATA.Subscribe($"{Name}.R2AxisPosition", () => R2AxisPosition.ToString());
            DATA.Subscribe($"{Name}.ZAxisPosition", () => ZAxisPosition.ToString());
            DATA.Subscribe($"{Name}.SAxisPosition", () => SAxisPosition.ToString());




            OP.Subscribe(String.Format("{0}.{1}", Name, "StartJog"), (out string reason, int time, object[] param) =>
            {
                string axis = param[0].ToString();
                bool direction = (bool)param[1];
                

                bool ret = ExecuteRobotJog(axis,direction);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "start jog succesfully");
                    return true;
                }
                reason = $"{Name} start jog failed";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "StopJog"), (out string reason, int time, object[] param) =>
            {
               

                bool ret = ExecuteRobotStopJog();
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Stop jog succesfully");
                    return true;
                }
                reason = $"{Name} Stop jog failed";
                return false;
            });


        }

        private bool ExecuteRobotJog(string axis, bool direction)
        {
            if (RobotState != RobotStateEnum.Idle) return false;
            lock (_locker)
            {
                string parameter = $"{axis},{(direction ? "0" : "1")},0";
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "JOG", parameter));
            }
            return true;
        }
        private bool ExecuteRobotStopJog()
        {
            if (RobotState != RobotStateEnum.Idle) return false;
            lock (_locker)
            {
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "JSP",null));
            }
            return true;




        }

        private void ResetPropertiesAndResponses()
        {

        }

        private bool OnTimer()
        {
            try
            {
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
                        _connection.SetPortAddress(SC.GetStringValue($"{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
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
                        }

                        if (_lstHandler.Count > 0)
                        {
                            handler = _lstHandler.First.Value;
                            if (handler != null) _connection.Execute(handler);
                            _lstHandler.RemoveFirst();
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

        public string Address { get; private set; }

        public bool IsConnected { get; private set; }


        public bool Connect()
        {
            return _connection.Connect();
        }

        public bool Disconnect()
        {
            return _connection.Disconnect();
        }
        public bool ParseReadData(string _command, string[] rdata)
        {
            try
            {
                if (_command.Equals("RMP"))
                {
                    ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), rdata[0].ToString());

                    CurrentSlotMap = rdata[2].ToString();
                    
                }
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return true;
            }
        }
        public string CurrentSlotMap { get; private set; }
        public override bool IsReady()
        {
            if (_diRobotError != null && _diRobotError.Value)
                return false;
            //if (_diTPinUse != null && !_diTPinUse.Value)
            //    return false;
            return RobotState == RobotStateEnum.Idle && !IsBusy;
        }

        public virtual bool ResetCPU(out string reason)
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "CPI", ""));
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "STA", ""));

            }
            reason = "";
            return true;
        }

        protected override bool fStop(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "PAU", null));
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this,"STA", null));

            }
            return true;
        }

      
        protected override bool fError(object[] param)
        {
            return true;
        }
        public override bool SetSpeed(object[] param)
        {
            if (!IsReady()) return false;
            Int32 speedvalue;
            if (!Int32.TryParse(param[0].ToString(), out speedvalue)) return false;

            if (SC.ContainsItem($"Robot.{RobotModuleName}.RobotSpeed"))
            {
                SC.SetItemValue($"Robot.{RobotModuleName}.RobotSpeed", speedvalue);
            }
            var p1 = speedvalue;
            var p2 = speedvalue;
            var p3 = speedvalue;
            var p4 = speedvalue;
            var p5 = speedvalue;
            var p6 = speedvalue;
            var p7 = speedvalue;
            var p8 = speedvalue;
            var p9 = speedvalue;
            var p10 = speedvalue;
            var p11 = speedvalue;
            var p12 = speedvalue;
            var p13 = speedvalue;
            string para1 = $"0,2,{p1},{p2},{p3},{p4},{p5},{p6},{p7},{p8},{p9},{p10},{p11},{p12},{p13}";
            lock (_locker)
            {
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "DWL", para1));
            }
            return true;

        }



        protected override bool fStartSetParameters(object[] param)
        {
            _dtActionStart = DateTime.Now;
            try
            {
                string strParameter;
                string setcommand = param[0].ToString();
                switch (setcommand)
                {
                    case "RobotSpeed":   // SSPD Set the motion speed
                        Int32 speedvalue;
                        if (!Int32.TryParse(param[1].ToString(), out speedvalue)) return false;

                        if (SC.ContainsItem($"Robot.{RobotModuleName}.RobotSpeed"))
                        {
                            SC.SetItemValue($"Robot.{RobotModuleName}.RobotSpeed", speedvalue);
                        }
                        var p1 = speedvalue;
                        var p2 = speedvalue;
                        var p3 = speedvalue;
                        var p4 = speedvalue;
                        var p5 = speedvalue;
                        var p6 = speedvalue;
                        var p7 = speedvalue;
                        var p8 = speedvalue;
                        var p9 = speedvalue;
                        var p10 = speedvalue;
                        var p11 = speedvalue;
                        var p12 = speedvalue;
                        var p13 = speedvalue;
                        string para1 = $"0,2,{p1},{p2},{p3},{p4},{p5},{p6},{p7},{p8},{p9},{p10},{p11},{p12},{p13}";
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "DWL", para1));
                        }

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

        protected override bool fMonitorSetParamter(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("SetParameterTimeout");
                return true;
            }
            return _lstHandler.Count == 0 && !_connection.IsBusy;

        }

        public bool IsPause;
        protected override bool fReset(object[] param)
        {
            _lstHandler.Clear();
            _connection.ForceClear();
            
            lock (_locker)
            {
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "CER", ""));
                if (IsPause)
                    _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "CNT", ""));
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "RED", "001,2"));
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "RED", "002,2"));
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "RED", "040,2"));
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "RED", "080,2"));
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "STA", ""));

            }
            return true;
        }

        private bool _isNeedWaferConfirm   ////0=auto detect,1=down move after release
        {
            get
            {
                if (SC.ContainsItem($"{_scRoot}.{RobotModuleName}.NeedWaferConfirm"))
                    return SC.GetValue<bool>($"{_scRoot}.{RobotModuleName}.NeedWaferConfirm");
                return true;
            }
        }

        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            if( _lstHandler.Count == 0 && !_connection.IsBusy)
            {
                BladeTarget = ModuleName.System;
                Blade1Target = ModuleName.System;
                Blade2Target = ModuleName.System;
                return true;
            }
            return false;




        }

        protected override bool fStartHome(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "HOM",""));

            }

            _dtActionStart = DateTime.Now;
            return true;
        }
        protected override bool fMonitorHome(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Init timeout");
                return true;
            }
            if (_lstHandler.Count == 0 && !_connection.IsBusy)
            {
                EV.PostInfoLog(RobotModuleName.ToString(), "Home complete.");

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
                return true;
            }
            return false;
        }


        protected override bool fStartInit(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "RST", _isNeedWaferConfirm ? "0" : "1"));                
                _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "WCH", "1"));
                _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "WCH", "2"));
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "STA", ""));
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "RED", "001,2"));
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "RED", "002,2"));
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "RED", "040,2"));
                _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "RED", "080,2"));
            }

            _dtActionStart = DateTime.Now;
            return true;
        }

        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Init timeout");
                return true;
            }
            if (_lstHandler.Count == 0 && !_connection.IsBusy)
            {
                EV.PostInfoLog(RobotModuleName.ToString(), "Init complete.");
                

                var waferinfoOnArm1 = WaferManager.Instance.GetWafer(RobotModuleName, 0);
                var waferinfoOnArm2 = WaferManager.Instance.GetWafer(RobotModuleName, 1);
                if (CurrentArm1WorkPresnece == TazmoRobotWorkPresenceInfoEnum.NoWork && !waferinfoOnArm1.IsEmpty)
                {
                    EV.PostAlarmLog(RobotModuleName.ToString(), "Robot didn't detect wafer on blade1,but it has record.");
                }
                if (CurrentArm1WorkPresnece == TazmoRobotWorkPresenceInfoEnum.WorkExists && waferinfoOnArm1.IsEmpty)
                {
                    EV.PostAlarmLog(RobotModuleName.ToString(), "Robot detect wafer on blade1, will create wafer.");
                    WaferManager.Instance.CreateWafer(RobotModuleName, 0, WaferStatus.Normal, Size);
                }
                if (CurrentArm2WorkPresnece == TazmoRobotWorkPresenceInfoEnum.NoWork && !waferinfoOnArm2.IsEmpty)
                {
                    EV.PostAlarmLog(RobotModuleName.ToString(), "Robot didn't detect wafer on blade2,but it has record.");

                }
                if (CurrentArm2WorkPresnece == TazmoRobotWorkPresenceInfoEnum.WorkExists && waferinfoOnArm2.IsEmpty)
                {
                    EV.PostAlarmLog(RobotModuleName.ToString(), "Robot detect wafer on blade2, will create wafer.");
                    WaferManager.Instance.CreateWafer(RobotModuleName, 1, WaferStatus.Normal, Size);
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
                return true;
            }
            return base.fMonitorInit(param);
        }
    

        protected override bool fResetToReady(object[] param)
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
        protected override bool fStartGoTo(object[] param)
        {
            try
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                var moduleIndex = Chamber2staion(module);
                string para1 = moduleIndex;
                int slot = (int)param[2] + 1;
                para1 += commasymbol + slot.ToString();
                para1 += arm == RobotArmEnum.Lower ? commasymbol + "1" : commasymbol + "2";

                lock (_locker)
                {
                    if (arm == RobotArmEnum.Lower)
                    {
                        _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "MTP", para1));
                    }
                    if (arm == RobotArmEnum.Upper)
                    {
                        _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "MTP", para1));
                    }
                }
                _dtActionStart = DateTime.Now;
                EV.PostInfoLog("Robot", $"{RobotModuleName} start to goto {module}.");
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        protected override bool fMonitorGoTo(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Goto Postion timeout");
                return true;
            }
            if(_lstHandler.Count == 0 && !_connection.IsBusy)
            {
                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
                ModuleName sourcemodule = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[1].ToString());
                int SourceslotIndex = (int)CurrentParamter[2];

                BladeTarget = sourcemodule;
                Blade1Target = sourcemodule;
                Blade2Target = sourcemodule;
                EV.PostInfoLog("Robot", $"{RobotModuleName} goto {sourcemodule} completed.");
                return true;
            }
            return base.fMonitorGoTo(param);
        }
        
        protected override bool fStartGrip(object[] param)
        {
            _dtActionStart = DateTime.Now;
            RobotArmEnum arm = (RobotArmEnum)param[0];
            lock (_locker)
            {
                if (arm == RobotArmEnum.Both)
                {
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVN", "1"));
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVN", "2"));
                }
                if (arm == RobotArmEnum.Lower)
                {
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVN", "1"));
                }
                if (arm == RobotArmEnum.Upper)
                {
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVN", "2"));
                }
            }
            EV.PostInfoLog("Robot", $"{RobotModuleName} start to grip arm {arm}.");
            return true;
        }

        protected override bool fMonitorGrip(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Grip timeout");
                return true;
            }

            if (_lstHandler.Count == 0 && !_connection.IsBusy)
            {
                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
               

                EV.PostInfoLog("Robot",$"{RobotModuleName} {arm} grip wafer completed.");
                return true;
            }


            return base.fMonitorGrip(param);
        }

        protected override bool fStartUnGrip(object[] param)
        {
            _dtActionStart = DateTime.Now;
            RobotArmEnum arm = (RobotArmEnum)param[0];
            lock (_locker)
            {
                if (arm == RobotArmEnum.Both)
                {
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVF", "1"));
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVF", "2"));
                }
                if (arm == RobotArmEnum.Lower)
                {
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVF", "1"));
                }
                if (arm == RobotArmEnum.Upper)
                {
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "VVF", "2"));
                }
            }
            EV.PostInfoLog("Robot", $"{RobotModuleName} start to ungrip arm {arm}.");
            return true;
        }
        protected override bool fMonitorUnGrip(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Ungrip timeout");
                return true;
            }

            if (_lstHandler.Count == 0 && !_connection.IsBusy)
            {
                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];


                EV.PostInfoLog("Robot", $"{RobotModuleName} {arm} ungrip wafer completed.");
                return true;
            }


            return base.fMonitorGrip(param);
        }

        protected override bool fStartMapWafer(object[] param)
        {
            
            _dtActionStart = DateTime.Now;
            ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[0].ToString());
            string moduleIndex = Chamber2staion(module);
            string para1 = moduleIndex + commasymbol + "0";
            _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "MAP", para1));
            _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "RMP", moduleIndex));

            
            if (ModuleHelper.IsLoadPort(module))
            {
                var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
                if (lp != null)
                    lp.NoteTransferStart();
            }


            BladeTarget = module;
            Blade1Target = module;
            Blade2Target = module;

            CmdTarget = module;
            MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = BuildBladeTarget(),
            };

            EV.PostInfoLog("Robot", $"{RobotModuleName} start to map station {module}.");
            return true;
        }

        protected override bool fMonitorMap(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Map timeout");
                return true;
            }

            if (_lstHandler.Count == 0 && !_connection.IsBusy)
            {
                ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), CurrentParamter[0].ToString());
                if (ModuleHelper.IsLoadPort(module))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
                    if (lp != null)
                        lp.NoteTransferStop();
                }

                EV.PostInfoLog("Robot",$"{RobotModuleName} map {module} completed.");
                BladeTarget =  ModuleName.System;
                Blade1Target = ModuleName.System; 
                Blade2Target = ModuleName.System;

                CmdTarget = ModuleName.System;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };
                NotifySlotMapResult(module, CurrentSlotMap);

                return true;
            }
            return base.fMonitorMap(param);
        }

        protected override bool fStartPickWafer(object[] param)
        {
            _dtActionStart = DateTime.Now;
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
                int slot = (int)param[2] + 1;

                
                string moduleIndex = Chamber2staion(module);
                if(param.Length >=4 && (bool)param[3] == true)
                {
                    moduleIndex = Chamber2staion(module,true);
                }               
                
                lock (_locker)
                {

                    if (arm == RobotArmEnum.Lower)
                    {
                        ////para1 += ",1";
                        _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "GET", $"{moduleIndex},{slot},1"));
                        //_lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "WCH", "1"));

                    }
                    if (arm == RobotArmEnum.Upper)
                    {
                        _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "GET", $"{moduleIndex},{slot},2"));
                        //_lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "WCH", "2"));
                    }
                    if(arm == RobotArmEnum.Both)
                    {
                        _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "GET", $"{moduleIndex},{slot},1"));
                        _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "GET", $"{moduleIndex},{slot+1},2"));
                        //_lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "WCH", "1"));
                        //_lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "WCH", "2"));

                    }

                    
                    //_lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "STA", ""));
                    
                }
                
                
                BladeTarget = module;
                Blade1Target = module;
                Blade2Target = module;

                CmdTarget = module;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };
                EV.PostInfoLog("Robot", $"{RobotModuleName} start to pick wafer from {module} slot{slot} with arm:{arm}.");
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

        }


        protected override bool fMonitorPick(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Pick timeout");
                return true;
            }

            if (_lstHandler.Count == 0 && !_connection.IsBusy)
            {
                RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];                
                ModuleName sourcemodule;
                if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
                if (ModuleHelper.IsLoadPort(sourcemodule))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(sourcemodule.ToString());
                    if (lp != null)
                        lp.NoteTransferStop();
                }



                int SourceslotIndex;
                if (!int.TryParse(CurrentParamter[2].ToString(), out SourceslotIndex)) return false;
                
                if (arm == RobotArmEnum.Lower)
                {
                    //if(GetWaferState(arm) != RobotArmWaferStateEnum.Present)
                    //{
                    //    OnError("Wafer detect error");
                        
                    //}
                    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);

                }
                if (arm == RobotArmEnum.Upper)
                {
                    //if(GetWaferState(arm) != RobotArmWaferStateEnum.Present)
                    //{
                    //    OnError("Wafer detect error");
                        
                    //}
                    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);

                }
                if (arm == RobotArmEnum.Both)
                {
                    //if(GetWaferState(arm) != RobotArmWaferStateEnum.Present)
                    //{
                    //   OnError("Wafer detect error");
                        
                    //}
                    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                    WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex + 1, RobotModuleName, 1);

                }


                EV.PostInfoLog("Robot", $"{RobotModuleName} pick wafer from {sourcemodule},slot:{SourceslotIndex+1} completed.");

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

                return true;
            }

            return base.fMonitorPick(param);
        }

        protected override bool fStartPlaceWafer(object[] param)
        {
            _dtActionStart = DateTime.Now;
            RobotArmEnum arm = (RobotArmEnum)param[0];
            ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
            int slot = (int)param[2] + 1;
            if (ModuleHelper.IsLoadPort(module))
            {
                var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
                if (lp != null)
                    lp.NoteTransferStart();
            }


            string moduleIndex = Chamber2staion(module);
            if (param.Length == 4 && (bool)param[3])
                moduleIndex = Chamber2staion(module,true);
            
           
            
            lock (_locker)
            {
                if (arm == RobotArmEnum.Both)
                {
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "PUT", $"{moduleIndex},{slot},1"));
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "PUT", $"{moduleIndex},{slot+1},2"));
                    //_lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "WCH", "1"));
                    //_lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "WCH", "2"));


                }
                if (arm == RobotArmEnum.Lower)
                {
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "PUT", $"{moduleIndex},{slot},1"));
                    //_lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "WCH", "1"));


                }
                if (arm == RobotArmEnum.Upper)
                {
                    // para1 += ",2";
                    _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "PUT", $"{moduleIndex},{slot},2"));
                    //_lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "WCH", "2"));
                }
                //_lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "STA", ""));
            }


            BladeTarget = module;
            Blade1Target = module;
            Blade2Target = module;

            CmdTarget = module;
            MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                BladeTarget = BuildBladeTarget(),
            };

            EV.PostInfoLog("Robot", $"{RobotModuleName} start to place wafer to {module} slot{slot} with arm:{arm}.");

            return true;
        }

        protected override bool fMonitorPlace(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Place timeout");
                return true;
            }
            if (_lstHandler.Count != 0 || _connection.IsBusy) return false;

            RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
            ModuleName sourcemodule;
            if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;

            if(ModuleHelper.IsLoadPort(sourcemodule))
            {
                var lp = DEVICE.GetDevice<LoadPortBaseDevice>(sourcemodule.ToString());
                lp.NoteTransferStop();
            }



            int Sourceslotindex;
            if (!int.TryParse(CurrentParamter[2].ToString(), out Sourceslotindex)) return false;
          
            if (arm == RobotArmEnum.Lower)
            {
                
                //if(GetWaferState(arm) != RobotArmWaferStateEnum.Absent)
                //{
                //    OnError("Wafer detect error");
                //}
                WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);

            }
            if (arm == RobotArmEnum.Upper)
            {
                
                //if (GetWaferState(arm) != RobotArmWaferStateEnum.Absent)
                //{
                //    OnError("Wafer detect error");
                       
                //}
                WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);

            }
            if (arm == RobotArmEnum.Both)
            {
                //if (GetWaferState(arm) != RobotArmWaferStateEnum.Absent)
                //{
                  
                //    OnError("Wafer detect error");
                      
                //}
                WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex + 1);

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


            EV.PostInfoLog("System", $"{RobotModuleName} place wafer to {sourcemodule} slot {Sourceslotindex+1} with arm {arm}" +
                $" successfully.");
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
                        //_lstHandlers.AddLast(new SR100RobotReadHandler(this, "RSTS"));
                        //_lstHandlers.AddLast(new SR100RobotReadHandler(this, "RPOS", "F"));
                        //_lstHandlers.AddLast(new SR100RobotReadHandler(this, "RPOS", "R"));
                    }
                    break;
                case "MappingData":
                    {
                        ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());

                        string para1 = Chamber2staion(module);
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "MAP", para1));
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
            IsBusy = false;
            return _lstHandler.Count == 0 && !_connection.IsBusy;
        }

        protected override bool fStartRetractFromPick(object[] param)
        {
            throw new NotImplementedException();
        }

        protected override bool fStartRetractFromPlace(object[] param)
        {
            throw new NotImplementedException();
        }

        

        protected override bool fStartSwapWafer(object[] param)
        {
            _dtActionStart = DateTime.Now;
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
                int slot = (int)param[2] + 1;


                string moduleIndex = Chamber2staion(module);
                if (param.Length >= 4 && (bool)param[3] == true)
                {
                    moduleIndex = Chamber2staion(module, true);
                }

                lock (_locker)
                {

                    if (arm == RobotArmEnum.Lower)
                    {
                        ////para1 += ",1";
                        _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "EXG", $"{moduleIndex},{slot},1"));
                    
                    }
                    if (arm == RobotArmEnum.Upper)
                    {
                        _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "EXG", $"{moduleIndex},{slot},2"));
                    }
                    if (arm == RobotArmEnum.Both)
                    {
                       

                    }
                    //_lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "WCH", "1"));
                    //_lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "WCH", "2"));

                    //_lstHandler.AddLast(new TazmoRobotSingleTransactionHandler(this, "STA", ""));
                }


                BladeTarget = module;
                Blade1Target = module;
                Blade2Target = module;
                CmdTarget = module;
                MoveInfo = new RobotMoveInfo()
                {
                    Action = RobotAction.Moving,
                    ArmTarget = CmdRobotArm == RobotArmEnum.Lower ? RobotArm.ArmA : RobotArm.ArmB,
                    BladeTarget = BuildBladeTarget(),
                };
                EV.PostInfoLog("Robot", $"{RobotModuleName} start to swap wafer from {module} slot{slot} with arm:{arm}.");
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        protected override bool fMonitorSwap(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(RobotCommandTimeout))
            {
                OnError("Swap timeout");
                return true;
            }
            if (_lstHandler.Count != 0 || _connection.IsBusy) return false;

            RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
            ModuleName sourcemodule;
            if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
            int Sourceslotindex;
            if (!int.TryParse(CurrentParamter[2].ToString(), out Sourceslotindex)) return false;
            if (ModuleHelper.IsLoadPort(sourcemodule))
            {
                var lp = DEVICE.GetDevice<LoadPortBaseDevice>(sourcemodule.ToString());
                lp.NoteTransferStop();
            }
            if (arm == RobotArmEnum.Lower)
            {
                

                //if ( !(GetWaferState(RobotArmEnum.Lower) == RobotArmWaferStateEnum.Present
                //        && GetWaferState(RobotArmEnum.Upper) == RobotArmWaferStateEnum.Absent))
                //{
                //    OnError("Wafer detect error");                     
                //}
                WaferManager.Instance.WaferMoved(sourcemodule, Sourceslotindex, RobotModuleName, 0);
                WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);

            }
            if (arm == RobotArmEnum.Upper)
            {
                //if ( !(GetWaferState(RobotArmEnum.Upper) == RobotArmWaferStateEnum.Present &&
                //    GetWaferState(RobotArmEnum.Lower) == RobotArmWaferStateEnum.Absent))
                //{
                //   OnError("Wafer detect error");
                    
                //}
                WaferManager.Instance.WaferMoved(sourcemodule, Sourceslotindex, RobotModuleName, 1);
                WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);

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
            EV.PostInfoLog("Robot", $"{RobotModuleName} swap wafer from {sourcemodule} slot{Sourceslotindex+1} with arm:{arm} completed.");
            return true;
        }

        public override void OnError(string errortext)
        {
            _lstHandler.Clear();
            base.OnError(errortext);
        }

        protected override bool fStartTransferWafer(object[] param)
        {
            return true;
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
        public void NoteError(string errortext)
        {
            OnError(errortext);
        }
        public void CheckWaferPresentAndGrip()
        {
            if (GetWaferState(RobotArmEnum.Lower) == RobotArmWaferStateEnum.Present)
            {
                if (WaferManager.Instance.CheckNoWafer(ModuleName.Robot, 0))
                {
                    EV.PostWarningLog($"{RobotModuleName}", $"System detec wafer on lower arm, will create wafer automatically.");
                    WaferManager.Instance.CreateWafer(ModuleName.Robot, 0, WaferStatus.Normal);
                }
            }
            if (GetWaferState(RobotArmEnum.Lower) == RobotArmWaferStateEnum.Absent)
            {
                _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "WCH", "1"));
                if (WaferManager.Instance.CheckHasWafer(ModuleName.Robot, 0))
                {
                    EV.PostWarningLog($"{ModuleName.Robot}", $"System didn't detect wafer on lower arm, but it has record.");
                }
            }
            if (GetWaferState(RobotArmEnum.Upper) == RobotArmWaferStateEnum.Present)
            {
                if (WaferManager.Instance.CheckNoWafer(ModuleName.Robot, 1))
                {
                    EV.PostWarningLog($"{ModuleName.Robot}", $"System detect wafer on upper arm, will create wafer automatically.");
                    WaferManager.Instance.CreateWafer(ModuleName.Robot, 1, WaferStatus.Normal);
                }
            }
            if (GetWaferState(RobotArmEnum.Upper) == RobotArmWaferStateEnum.Absent)
            {
                _lstHandler.AddLast(new TazmoRobotTwinTransactionHandler(this, "WCH", "2"));
                if (WaferManager.Instance.CheckHasWafer(ModuleName.Robot, 1))
                {
                    EV.PostWarningLog($"{ModuleName.Robot}", $"System didn't detect wafer on upper arm, but it has record.");
                }
            }
        }
        public override RobotArmWaferStateEnum GetWaferState(RobotArmEnum arm)
        {
            if (arm == RobotArmEnum.Lower)
            {
                if (_diRobotBlade1WaferOn != null)
                {
                    if (_diRobotBlade1WaferOn.Value) return RobotArmWaferStateEnum.Present;
                    else return RobotArmWaferStateEnum.Absent;
                }

                return CurrentArm1WorkPresnece== TazmoRobotWorkPresenceInfoEnum.WorkExists  ? RobotArmWaferStateEnum.Present : RobotArmWaferStateEnum.Absent;
            }
            if (arm == RobotArmEnum.Upper)
            {
                if (_diRobotBlade2WaferOn != null)
                {
                    if (_diRobotBlade2WaferOn.Value) return RobotArmWaferStateEnum.Present;
                    else return RobotArmWaferStateEnum.Absent;
                }
                return CurrentArm2WorkPresnece == TazmoRobotWorkPresenceInfoEnum.WorkExists ? RobotArmWaferStateEnum.Present : RobotArmWaferStateEnum.Absent;
            }
            if (arm == RobotArmEnum.Both)
            {
                if (_diRobotBlade1WaferOn != null && _diRobotBlade2WaferOn != null)
                {
                    if (_diRobotBlade2WaferOn.Value && _diRobotBlade1WaferOn.Value)
                        return RobotArmWaferStateEnum.Present;
                    else if (!_diRobotBlade2WaferOn.Value && !_diRobotBlade1WaferOn.Value)
                        return RobotArmWaferStateEnum.Absent;
                    else return RobotArmWaferStateEnum.Unknown;
                }

                if (CurrentArm1WorkPresnece == TazmoRobotWorkPresenceInfoEnum.WorkExists
                    && CurrentArm2WorkPresnece == TazmoRobotWorkPresenceInfoEnum.WorkExists)
                {
                    return RobotArmWaferStateEnum.Present;
                }
                if (CurrentArm1WorkPresnece == TazmoRobotWorkPresenceInfoEnum.NoWork 
                    && CurrentArm1WorkPresnece == TazmoRobotWorkPresenceInfoEnum.NoWork)
                {
                    return RobotArmWaferStateEnum.Absent;
                }

            }
            return RobotArmWaferStateEnum.Unknown;
        }
        public override bool OnActionDone(object[] param)
        {
            EV.PostInfoLog("Robot", $"OnActionDone");
            IsBusy = false;
          
            if (_lstHandler.Count == 0)
            {
                IsBusy = false;
                return base.OnActionDone(param);
            }

            return true;
        }

        protected override bool fClear(object[] param)
        {
            return true;
        }

        private string Chamber2staion(ModuleName chamber, bool flip = false,WaferSize size = WaferSize.WS12)
        {
            if(ModuleHelper.IsLoadPort(chamber))
            {
                var lp = DEVICE.GetDevice<LoadPortBaseDevice>(chamber.ToString());
                int carrierIndex = lp.InfoPadCarrierIndex;
                if (flip)
                    return SC.GetStringValue($"CarrierInfo.Carrier{carrierIndex}.{chamber}FlipStation");
                else
                    return SC.GetStringValue($"CarrierInfo.Carrier{carrierIndex}.{chamber}FrontStation");

            }

            if (flip)
                return SC.GetStringValue($"CarrierInfo.Size{size.ToString().Replace("WS","")}.{chamber}FlipStation");
            else
                return SC.GetStringValue($"CarrierInfo.Size{size.ToString().Replace("WS", "")}.{chamber}FrontStation");
        }

        private string BuildBladeTarget()
        {
            return (CmdRobotArm == RobotArmEnum.Upper ? "ArmB" : "ArmA") + "." + CmdTarget;
        }

    }

}
