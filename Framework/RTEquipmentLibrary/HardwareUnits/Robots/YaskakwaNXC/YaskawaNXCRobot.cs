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
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using EventType = Aitex.Core.RT.Event.EventType;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.YaskawaRobots
{
    public class YaskawaNXC100Robot : RobotBaseDevice, IConnection
    {        
        public int UnitNumber
        {
            get;private set;
        }
        private bool isSimulatorMode;
        private string _scRoot;
        //private string _ipaddress;
        public YaskawaTokenGenerator SeqnoGenerator { get; private set; }
        public bool IsEnableSeqNo { get; private set; }
        public bool IsEnableCheckSum { get; private set; }
        public int CurrentSeqNo { get; set; }

        public string PortName;
        private string _address;
        private bool _enableLog;
        private YaskawaNXC100Connection _connection;
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
        public YaskawaPositonEnum ReadPositionType { get; private set; }
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

        public override bool IsEnableMapWafer
        {
            get
            {
                if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 1))
                    return false;
                return true;
            }
        }
        public Dictionary<string, float> ReadMappingCalibrationResult { get; private set; }

        

        public YaskawaNXC100Robot(string module, string name, string scRoot,IoSensor[] dis,IoTrigger[] dos) : base(module, name)
        {
            Module = module;
            Name = name;

            isSimulatorMode = SC.ContainsItem("System.IsSimulatorMode") ? SC.GetValue<bool>("System.IsSimulatorMode") : false;
            _scRoot = scRoot;
            _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            UnitNumber = SC.GetValue<int>($"{_scRoot}.{Name}.UnitNumber");

            IsEnableCheckSum = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableCheckSum");
            IsEnableSeqNo = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableSeqNo");
            SpeedLevelSetting = SC.GetValue<int>($"{_scRoot}.{Name}.SpeedLevel");
            _connection = new YaskawaNXC100Connection(this,_address);
            _connection.EnableLog(_enableLog);
            SeqnoGenerator = new YaskawaTokenGenerator($"{_scRoot}.{Name}.CommunicationToken");


            if(dis !=null && dis.Length >=5)
            {
                _diRobotReady = dis[0];
                _diRobotBlade1WaferOn = dis[1];
                _diRobotBlade2WaferOn = dis[2];
                _diRobotError = dis[3];
                _diTPinUse = dis[4];
                _diRobotError.OnSignalChanged += _diRobotError_OnSignalChanged;
                _diTPinUse.OnSignalChanged += _diTPinUse_OnSignalChanged;
            }
            if(dos!=null && dos.Length >=1)
            {
                _doRobotHold = dos[0];
            }


            ConnectionManager.Instance.Subscribe($"{Name}", _connection);
            _thread = new PeriodicJob(10, OnTimer, $"{_scRoot}.{Name} MonitorHandler", true);
            ReadStationItemContents.Add("00", "Upward offset");
            ReadStationItemContents.Add("01", "Downword offset");
            ReadStationItemContents.Add("02", "Grip position offset");
            ReadStationItemContents.Add("06", "G2/P3 offset in the extending direction");
            ReadStationItemContents.Add("08", "Put downward offset");
            ReadStationItemContents.Add("70", "Get operation Movet_grip function yes/no");
            ReadStationItemContents.Add("71", "Get operation rsing pattern");
            ReadStationItemContents.Add("80", "Put operation Move_grip function yes/no");
            ReadStationItemContents.Add("81", "Put operation dropping pattern");
            ReadStationItemContents.Add("50", "Slot Numbers");
            ReadStationItemContents.Add("30", "Slot pitch(Left elbow,Blade1)");
            ReadStationItemContents.Add("31", "Slot pitch(Left elbow,Blade2)");
            ReadStationItemContents.Add("32", "Slot pitch(Rigth elbow,Blade1)");
            ReadStationItemContents.Add("33", "Slot pitch(Right elbow,Blade2)");



            ResetPropertiesAndResponses();
            RegisterSpecialData();
            //RegisterAlarm();
          
        }

        

        private void RegisterAlarm()
        {
            EV.Subscribe(new EventItem("Alarm", "RobotError020", $"{Name} Error:Robot Occurred Error:Secondary power off.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError021", $"{Name} Error:Robot Occurred Error:Secondary power on.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError040", $"{Name} Error:Robot Occurred Error:In TEACH Mode.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError050", $"{Name} Error:Robot Occurred Error:Unit is in motion.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError051", $"{Name} Error:Robot Occurred Error:Unable to set pitch between slots.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError052", $"{Name} Error:Robot Occurred Error:Unable to restart motion.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError053", $"{Name} Error:Robot Occurred Error:Ready position move incomplete.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError054", $"{Name} Error:Robot Occurred Error:Alignment Ready position move incomplete.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError055", $"{Name} Error:Robot Occurred Error:Improper station type.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError058", $"{Name} Error:Robot Occurred Error:Command not supported 1-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError059", $"{Name} Error:Robot Occurred Error:Invalid transfer point.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError05A", $"{Name} Error:Robot Occurred Error:Linear motion failed.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError05C", $"{Name} Error:Robot Occurred Error:Unable to reference wafer alignment result.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError05d", $"{Name} Error:Robot Occurred Error:Unable to perform arm calibration.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError05E", $"{Name} Error:Robot Occurred Error:Unable to read mapping data.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError05F", $"{Name} Error:Robot Occurred Error:Data Upload/Download in progress.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError061", $"{Name} Error:Robot Occurred Error:Unable to motion.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError064", $"{Name} Error:Robot Occurred Error:Lifter interference error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError070", $"{Name} Error:Robot Occurred Error:Bottom slot position record incomplete.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError071", $"{Name} Error:Robot Occurred Error:Top slot position record incomplete.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError088", $"{Name} Error:Robot Occurred Error:Position generating error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError089", $"{Name} Error:Robot Occurred Error:Position generating error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError08A", $"{Name} Error:Robot Occurred Error:Position generating error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError08b", $"{Name} Error:Robot Occurred Error:Position generating error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError08C", $"{Name} Error:Robot Occurred Error:Position generating error 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError08d", $"{Name} Error:Robot Occurred Error:Position generating error 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError090", $"{Name} Error:Robot Occurred Error:Host parameter out of range.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError0A0", $"{Name} Error:Robot Occurred Error:Alignment motion error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError0E0", $"{Name} Error:Robot Occurred Error:Teach position adjustmentoffset amount limit error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError0F0", $"{Name} Error:Robot Occurred Error:Voltage drop warning.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*06", $"{Name} Error:Robot Occurred Error:Amplifier Type Mismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*07", $"{Name} Error:Robot Occurred Error:Encoder Type Mismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*10", $"{Name} Error:Robot Occurred Error:Overflow Current.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*30", $"{Name} Error:Robot Occurred Error:Regeneration Error Detected.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*40", $"{Name} Error:Robot Occurred Error:Excess Voltage (converter).", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*41", $"{Name} Error:Robot Occurred Error:Insufficient Voltage.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*45", $"{Name} Error:Robot Occurred Error:Brake circuit error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*46", $"{Name} Error:Robot Occurred Error:Converter ready signal error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*47", $"{Name} Error:Robot Occurred Error:Input power error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*48", $"{Name} Error:Robot Occurred Error:Converter main circuit chargeerror.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*49", $"{Name} Error:Robot Occurred Error:Amplifier ready signal error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*51", $"{Name} Error:Robot Occurred Error:Excessive Speed.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*71", $"{Name} Error:Robot Occurred Error:Momentary Overload (Motor).", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*72", $"{Name} Error:Robot Occurred Error:Continuous Overload (Motor).", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*78", $"{Name} Error:Robot Occurred Error:Overload (Converter).", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*7b", $"{Name} Error:Robot Occurred Error:Amplifier overheat.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*7C", $"{Name} Error:Robot Occurred Error:Continuous Overload(Amplifier).", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*7d", $"{Name} Error:Robot Occurred Error:Momentary Overload.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*81", $"{Name} Error:Robot Occurred Error:Absolute Encoder Back-upError.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*83", $"{Name} Error:Robot Occurred Error:Absolute Encoder Battery.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*84", $"{Name} Error:Robot Occurred Error:Encoder Data Error 2-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*85", $"{Name} Error:Robot Occurred Error:Encoder Excessive Speed.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*86", $"{Name} Error:Robot Occurred Error:Encoder Overheat.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*88", $"{Name} Error:Robot Occurred Error:Encoder error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*89", $"{Name} Error:Robot Occurred Error:Encoder Command failed.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*8A", $"{Name} Error:Robot Occurred Error:Encoder multi-turn range.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*8C", $"{Name} Error:Robot Occurred Error:Encoder Reset not completed.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*98", $"{Name} Error:Robot Occurred Error:Servo parameter error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*9A", $"{Name} Error:Robot Occurred Error:Feedback Over Flow.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*b4", $"{Name} Error:Robot Occurred Error:Servo Control Board Failure.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*bC", $"{Name} Error:Robot Occurred Error:Encoder error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*C1", $"{Name} Error:Robot Occurred Error:Motor runaway detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*C9", $"{Name} Error:Robot Occurred Error:Encoder Communication.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*CE", $"{Name} Error:Robot Occurred Error:Encoder error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*CF", $"{Name} Error:Robot Occurred Error:Encoder error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*d0", $"{Name} Error:Robot Occurred Error:Position deviation error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*d1", $"{Name} Error:Robot Occurred Error:Position deviation saturation.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*d2", $"{Name} Error:Robot Occurred Error:Motor directive position error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*d4", $"{Name} Error:Robot Occurred Error:Servo Tracking Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError*F1", $"{Name} Error:Robot Occurred Error:Phase loss.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorE*1", $"{Name} Error:Robot Occurred Error:Positioning Timeout.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorE*d", $"{Name} Error:Robot Occurred Error:Command not supported 1-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorE*E", $"{Name} Error:Robot Occurred Error:Communication Error(internal controller) 1-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorE*F", $"{Name} Error:Robot Occurred Error:Servo control board responsetimeout 1..", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError701", $"{Name} Error:Robot Occurred Error:ROM Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError703", $"{Name} Error:Robot Occurred Error:Communication Error(internal controller) 2-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError704", $"{Name} Error:Robot Occurred Error:Communication Error (internal controller) 2-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError705", $"{Name} Error:Robot Occurred Error:Communication Error(internal controller) 2-3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError706", $"{Name} Error:Robot Occurred Error:Servo system error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError707", $"{Name} Error:Robot Occurred Error:Servo system error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError709", $"{Name} Error:Robot Occurred Error:Current feedback error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError70A", $"{Name} Error:Robot Occurred Error:Power Lost.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError70b", $"{Name} Error:Robot Occurred Error:Rush Current PreventionRelay Abnormal.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError70C", $"{Name} Error:Robot Occurred Error:Converter mismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError70F", $"{Name} Error:Robot Occurred Error:Servo control board response timeout 2..", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError713", $"{Name} Error:Robot Occurred Error:DB error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError714", $"{Name} Error:Robot Occurred Error:Converter charge Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError715", $"{Name} Error:Robot Occurred Error:Servo OFF Status Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError716", $"{Name} Error:Robot Occurred Error:Servo ON Status Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError717", $"{Name} Error:Robot Occurred Error:Servo OFF Status Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError718", $"{Name} Error:Robot Occurred Error:Servo ON Status Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError719", $"{Name} Error:Robot Occurred Error:Servo On Abnormal.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError71A", $"{Name} Error:Robot Occurred Error:Brake circuit error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError71b", $"{Name} Error:Robot Occurred Error:Brake circuit error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError71C", $"{Name} Error:Robot Occurred Error:Power relay error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError721", $"{Name} Error:Robot Occurred Error:Servo parameter error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError722", $"{Name} Error:Robot Occurred Error:Servo parameter error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError725", $"{Name} Error:Robot Occurred Error:Converter Overheat.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError726", $"{Name} Error:Robot Occurred Error:Communication Error(internal controller) 2-4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError727", $"{Name} Error:Robot Occurred Error:Command not supported 1-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError728", $"{Name} Error:Robot Occurred Error:Communication Error(internal controller) 2-5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError729", $"{Name} Error:Robot Occurred Error:Servo system error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError72A", $"{Name} Error:Robot Occurred Error:Servo system error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError72b", $"{Name} Error:Robot Occurred Error:Servo parameter error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError730", $"{Name} Error:Robot Occurred Error:Amp module disconnected..", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError732", $"{Name} Error:Robot Occurred Error:Servo parameter error 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError733", $"{Name} Error:Robot Occurred Error:Servo parameter error 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError734", $"{Name} Error:Robot Occurred Error:Servo parameter error 7.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError735", $"{Name} Error:Robot Occurred Error:Servo parameter error 8.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError73F", $"{Name} Error:Robot Occurred Error:Undefined Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError740", $"{Name} Error:Robot Occurred Error:Encoder Status Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError741", $"{Name} Error:Robot Occurred Error:Servo system error 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError742", $"{Name} Error:Robot Occurred Error:Servo system error 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError743", $"{Name} Error:Robot Occurred Error:Servo system error 7.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError744", $"{Name} Error:Robot Occurred Error:Servo system error 8.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError745", $"{Name} Error:Robot Occurred Error:Servo system error 9.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError746", $"{Name} Error:Robot Occurred Error:Servo system error 10.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError74A", $"{Name} Error:Robot Occurred Error:Servo system error 11.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError74b", $"{Name} Error:Robot Occurred Error:Servo system error 12.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError74C", $"{Name} Error:Robot Occurred Error:Servo system error 13.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError74d", $"{Name} Error:Robot Occurred Error:Servo system error 14.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7A0", $"{Name} Error:Robot Occurred Error:Communication Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7A1", $"{Name} Error:Robot Occurred Error:Communication Error(internal controller) 3-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7A2", $"{Name} Error:Robot Occurred Error:Command not supported 3-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7A3", $"{Name} Error:Robot Occurred Error:Data buffer full.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7A4", $"{Name} Error:Robot Occurred Error:Command not supported 3-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7A5", $"{Name} Error:Robot Occurred Error:Encoder data error 3-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7A6", $"{Name} Error:Robot Occurred Error:Command not supported 3-3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7AE", $"{Name} Error:Robot Occurred Error:Communication Error(internal controller) 1-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7AF", $"{Name} Error:Robot Occurred Error:Communication Error(internal controller) 1-3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7b0", $"{Name} Error:Robot Occurred Error:CCD sensor abnormal 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7b4", $"{Name} Error:Robot Occurred Error:CCD sensor abnormal 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7b5", $"{Name} Error:Robot Occurred Error:CCD sensor abnormal 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7C0", $"{Name} Error:Robot Occurred Error:PAIF board Failure 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7C1", $"{Name} Error:Robot Occurred Error:PAIF board Failure 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7C2", $"{Name} Error:Robot Occurred Error:PAIF board Failure 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7C3", $"{Name} Error:Robot Occurred Error:CCD sensor abnormal 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7CF", $"{Name} Error:Robot Occurred Error:PAIF board disconnected.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7d0", $"{Name} Error:Robot Occurred Error:PAIF board Failure 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError7d1", $"{Name} Error:Robot Occurred Error:PAIF board Failure 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError", $"{Name} Error:Robot Occurred Error:.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError900", $"{Name} Error:Robot Occurred Error:Character Interval Timeout.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError910", $"{Name} Error:Robot Occurred Error:Received Data ChecksumError.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError920", $"{Name} Error:Robot Occurred Error:Unit Number Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError930", $"{Name} Error:Robot Occurred Error:Undefined CommandReceived.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError940", $"{Name} Error:Robot Occurred Error:Message Parameter Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError950", $"{Name} Error:Robot Occurred Error:Receiving Time-out Error for Confirmation of Execution Completion.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError960", $"{Name} Error:Robot Occurred Error:Incorrect sequence number.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError961", $"{Name} Error:Robot Occurred Error:Duplicated message.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError970", $"{Name} Error:Robot Occurred Error:Delimiter error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError9A1", $"{Name} Error:Robot Occurred Error:Message buffer overflow.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError9C0", $"{Name} Error:Robot Occurred Error:LAN device setting error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError9C1", $"{Name} Error:Robot Occurred Error:IP address error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError9C2", $"{Name} Error:Robot Occurred Error:Subnet mask error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError9C3", $"{Name} Error:Robot Occurred Error:Default gateway error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError9d0", $"{Name} Error:Robot Occurred Error:Ethernet receive error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError9E0", $"{Name} Error:Robot Occurred Error:During operation themaintenance tool.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotError9E1", $"{Name} Error:Robot Occurred Error:The data abnormal.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorA01", $"{Name} Error:Robot Occurred Error:Re-detection of a powerSupply voltage fall.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorA10", $"{Name} Error:Robot Occurred Error:External emergency stop.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorA20", $"{Name} Error:Robot Occurred Error:T.P emergency stop.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorA21", $"{Name} Error:Robot Occurred Error:Interlock board failure 0.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorA30", $"{Name} Error:Robot Occurred Error:Emergency stop.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorA40", $"{Name} Error:Robot Occurred Error:Controller Fan 1 Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorA41", $"{Name} Error:Robot Occurred Error:Controller Fan 2 Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorA42", $"{Name} Error:Robot Occurred Error:Controller Fan 3 Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorA45", $"{Name} Error:Robot Occurred Error:Unit fan 1 error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorA46", $"{Name} Error:Robot Occurred Error:Unit fan 2 error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorA4F", $"{Name} Error:Robot Occurred Error:Controller Battery Alarm.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAC0", $"{Name} Error:Robot Occurred Error:Safety fence signal detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAC9", $"{Name} Error:Robot Occurred Error:Protection stop signal.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAE0", $"{Name} Error:Robot Occurred Error:HOST Mode Switching error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAE1", $"{Name} Error:Robot Occurred Error:TEACH Mode Switching Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAE8", $"{Name} Error:Robot Occurred Error:Deadman switch error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAF0", $"{Name} Error:Robot Occurred Error:Interlock board failure 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAF1", $"{Name} Error:Robot Occurred Error:Interlock board failure 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAF2", $"{Name} Error:Robot Occurred Error:Interlock board failure 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAF3", $"{Name} Error:Robot Occurred Error:Interlock board failure 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAF4", $"{Name} Error:Robot Occurred Error:Interlock board failure 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAF5", $"{Name} Error:Robot Occurred Error:Interlock board failure 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAF6", $"{Name} Error:Robot Occurred Error:Interlock board failure 7.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAF8", $"{Name} Error:Robot Occurred Error:Input compare error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAF9", $"{Name} Error:Robot Occurred Error:Input compare error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAFA", $"{Name} Error:Robot Occurred Error:Input compare error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAFb", $"{Name} Error:Robot Occurred Error:Input compare error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAFC", $"{Name} Error:Robot Occurred Error:Input compare error 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAFd", $"{Name} Error:Robot Occurred Error:Input compare error 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAFE", $"{Name} Error:Robot Occurred Error:Input compare error 7.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorAFF", $"{Name} Error:Robot Occurred Error:Input compare error 8.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb10", $"{Name} Error:Robot Occurred Error:Axis-1 Speed Limit Detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb11", $"{Name} Error:Robot Occurred Error:Axis-2 Speed Limit Detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb12", $"{Name} Error:Robot Occurred Error:Axis-3 Speed Limit Detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb13", $"{Name} Error:Robot Occurred Error:Axis-4 Speed Limit Detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb14", $"{Name} Error:Robot Occurred Error:Axis-5 Speed Limit Detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb20", $"{Name} Error:Robot Occurred Error:Axis-1 Positive (+) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb21", $"{Name} Error:Robot Occurred Error:Axis-2 Positive (+) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb22", $"{Name} Error:Robot Occurred Error:Axis-3 Positive (+) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb23", $"{Name} Error:Robot Occurred Error:Axis-4 Positive (+) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb24", $"{Name} Error:Robot Occurred Error:Axis-5 Positive (+) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb28", $"{Name} Error:Robot Occurred Error:Axis-1 Positive (+) Direction Software-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb29", $"{Name} Error:Robot Occurred Error:Axis-2 Positive (+) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb2A", $"{Name} Error:Robot Occurred Error:Axis-3 Positive (+) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb2b", $"{Name} Error:Robot Occurred Error:Axis-4 Positive (+) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb2C", $"{Name} Error:Robot Occurred Error:Axis-5 Positive (+) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb30", $"{Name} Error:Robot Occurred Error:Axis-1 Negative (-) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb31", $"{Name} Error:Robot Occurred Error:Axis-2 Negative (-) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb32", $"{Name} Error:Robot Occurred Error:Axis-3 Negative (-) Direction Software-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb33", $"{Name} Error:Robot Occurred Error:Axis-4 Negative (-) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb34", $"{Name} Error:Robot Occurred Error:Axis-5 Negative (-) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb38", $"{Name} Error:Robot Occurred Error:Axis-1 Negative (-) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb39", $"{Name} Error:Robot Occurred Error:Axis-2 Negative (-) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb3A", $"{Name} Error:Robot Occurred Error:Axis-3 Negative (-) Direction Software-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb3b", $"{Name} Error:Robot Occurred Error:Axis-4 Negative (-) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb3C", $"{Name} Error:Robot Occurred Error:Axis-5 Negative (-) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb40", $"{Name} Error:Robot Occurred Error:Access Permission Signal 1Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb41", $"{Name} Error:Robot Occurred Error:Access Permission Signal 2Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb42", $"{Name} Error:Robot Occurred Error:Access Permission Signal 3Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb43", $"{Name} Error:Robot Occurred Error:Access Permission Signal 4Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb44", $"{Name} Error:Robot Occurred Error:Access Permission Signal 5 Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb45", $"{Name} Error:Robot Occurred Error:Access Permission Signal 6Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb46", $"{Name} Error:Robot Occurred Error:Access Permission Signal 7Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb47", $"{Name} Error:Robot Occurred Error:Access Permission Signal 8Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb48", $"{Name} Error:Robot Occurred Error:Access Permission Signal 9Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb49", $"{Name} Error:Robot Occurred Error:Access Permission Signal 10 Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb4A", $"{Name} Error:Robot Occurred Error:Access Permission Signal 11Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb4b", $"{Name} Error:Robot Occurred Error:Access Permission Signal 12Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb4C", $"{Name} Error:Robot Occurred Error:Access Permission Signal 13Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb4d", $"{Name} Error:Robot Occurred Error:Access Permission Signal 14Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb4E", $"{Name} Error:Robot Occurred Error:Access Permission Signal 15Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb4F", $"{Name} Error:Robot Occurred Error:Access Permission Signal 16Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb60", $"{Name} Error:Robot Occurred Error:Access Permission to P/A Stage Time-out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb61", $"{Name} Error:Robot Occurred Error:Access Permission to P/AStage Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb62", $"{Name} Error:Robot Occurred Error:Access Permission to P/A Stage Time-out Error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb63", $"{Name} Error:Robot Occurred Error:Access Permission to P/A Stage Time-out Error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb64", $"{Name} Error:Robot Occurred Error:Access Permission to P/AStage Time-out Error 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb65", $"{Name} Error:Robot Occurred Error:Access Permission to P/AStage Time-out Error 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb66", $"{Name} Error:Robot Occurred Error:Access Permission to P/AStage Time-out Error 7.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb68", $"{Name} Error:Robot Occurred Error:P/A motion permission timeout error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb70", $"{Name} Error:Robot Occurred Error:SS signal detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb80", $"{Name} Error:Robot Occurred Error:Fork 1/Pre-aligner: Wafer Presence Confirmation Time-out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb81", $"{Name} Error:Robot Occurred Error:Fork 1/Pre-aligner: WaferAbsence Confirmation Time- out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb82", $"{Name} Error:Robot Occurred Error:Fork 1/Pre-aligner: Wafer Presence Confirmation Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb83", $"{Name} Error:Robot Occurred Error:Fork 1/Pre-aligner: WaferAbsence Confirmation Time- out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb88", $"{Name} Error:Robot Occurred Error:Grip sensor Time-out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb89", $"{Name} Error:Robot Occurred Error:Grip sensor Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb8A", $"{Name} Error:Robot Occurred Error:UnGrip sensor Time-out Error1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb8b", $"{Name} Error:Robot Occurred Error:UnGrip sensor Time-out Error2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb8F", $"{Name} Error:Robot Occurred Error:Fork 1: Plunger non-operationerror.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb90", $"{Name} Error:Robot Occurred Error:Fork 2: Wafer Presence Confirmation Time-out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb91", $"{Name} Error:Robot Occurred Error:Fork 2: Wafer AbsenceConfirmation Time-out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb92", $"{Name} Error:Robot Occurred Error:Fork 2: Wafer PresenceConfirmation Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb93", $"{Name} Error:Robot Occurred Error:Fork 2: Wafer AbsenceConfirmation Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb98", $"{Name} Error:Robot Occurred Error:Lifter up sensor Time-outError 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb99", $"{Name} Error:Robot Occurred Error:Lifter up sensor Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb9A", $"{Name} Error:Robot Occurred Error:Lifter down sensor Time-outError 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb9b", $"{Name} Error:Robot Occurred Error:Lifter down sensor Time-outError 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorb9F", $"{Name} Error:Robot Occurred Error:Fork 2: Plunger non-operationerror.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorbA0", $"{Name} Error:Robot Occurred Error:Fork 1/Pre-aligner: WaferAbsence Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorbA1", $"{Name} Error:Robot Occurred Error:Fork 1: Sensor StatusMismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorbA8", $"{Name} Error:Robot Occurred Error:Grip sensor status Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorbA9", $"{Name} Error:Robot Occurred Error:Grip sensor status Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorbAA", $"{Name} Error:Robot Occurred Error:Ungrip sensor status Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorbAb", $"{Name} Error:Robot Occurred Error:Ungrip sensor status Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorbAC", $"{Name} Error:Robot Occurred Error:Grip sensor status mismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorbAd", $"{Name} Error:Robot Occurred Error:Lifter/Grip sensor statusmismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorbb0", $"{Name} Error:Robot Occurred Error:Fork 2: Wafer Absence Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorbb1", $"{Name} Error:Robot Occurred Error:Fork 2: Sensor StatusMismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "RobotErrorbb8", $"{Name} Error:Robot Occurred Error:Lifter up sensor status Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
           
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
                    _lstHandlers.AddLast(new NXC100RobotReadHandler(this, "RSTS"));
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
                if (SpeedLevelSetting == 1) return "Fast";
                if (SpeedLevelSetting == 2) return "Medium";
                if (SpeedLevelSetting == 3) return "Slow";
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
                    while (_lstHandlers.Count > 0 || _connection.IsBusy)
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
            if (_diRobotError!=null && !_diRobotError.Value) 
                return false;
            if (_diTPinUse != null && !_diTPinUse.Value) 
                return false;
            return RobotState == RobotStateEnum.Idle && !IsBusy;
        }

        public bool ParseReadData(string _command,string rdata)
        {
            try
            {
                if (_command == "RSTS")
                {
                    return (ParseRSTSStatus(rdata));
                    
                }
                if (_command == "RSLV")      //Read the speed level
                {
                    return (ParseSpeedLevel(rdata));                   
                }
                if (_command == "RPOS")   //Reference current postion
                {
                    return (ParsePositionData(rdata));
                    
                }
                if (_command == "RSTP")   //Reference registered position, read the save postion for station
                {
                    return (ParseRegisteredPositionData(rdata));
                   
                }

                if (_command == "RPRM")  //Reference the parameter values of the specified unit
                {
                    return (ParseParameterData(rdata));
                }
                if (_command == "RMSK")  //Reference the interlock information
                {
                    return (ParseInterlockInfo(rdata));

                }
                if (_command == "RVER")  //Reference the software version
                {
                    return (ParseSoftwareVersion(rdata));
                }
                if (_command == "RMAP")  //Reference the slot map
                {
                    return (ParseSlotMap(rdata));
                }              
                if (_command == "RMPD")  //reference the mapping data
                {
                    return (ParseMappingData(rdata));
                }
                if (_command == "RMCA")   // Reference the mapping calibration result
                {
                    return true;
                }

                if (_command == "RALN") // Reference the alignment result
                {
                    return true;
                }
                if (_command == "RACA") // Reference calibration result for alignment
                {
                    return true;
                }
                return true;
            }
            catch(Exception ex)
            {
                LOG.Write(ex);
                return true;
            }
        }
        public bool ParseStatus(string status)
        {
            try
            {
                int intstatus = Convert.ToInt32(status,16);
                IsManipulatorBatteryLow = ((intstatus & 0x10) == 0x10);
                IsCommandExecutionReady = ((intstatus & 0x20) == 0x20);
                IsServoON = ((intstatus & 0x40) == 0x40);
                IsErrorOccurred = ((intstatus & 0x80) == 0x80);
                IsControllerBatteryLow = ((intstatus & 0x1) == 0x1);
                //IsWaferPresenceOnBlade1 = ((intstatus & 0x2) == 0x2);
                //IsWaferPresenceOnBlade2 = ((intstatus & 0x4) == 0x4);
                return true;

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        public bool ParseRSTSStatus(string status)
        {
            try
            {
                string StsN = status.Substring(0, 2);
                string Ackcd = status.Substring(2, 4);

                ParseStatus(StsN);
                if (status.Length <= 6) return true;

                ErrorCode = status.Substring(6,4);
                int intstatus = Convert.ToInt32(status.Substring(10,4), 16);
                IsWaferPresenceOnBlade1 = ((intstatus & 0x1000) != 0x1000);
                IsWaferPresenceOnBlade2 = ((intstatus & 0x2000) != 0x2000);
                IsGrippedBlade1 = ((intstatus & 0x4000) != 0x4000);
                IsGrippedBlade2 = ((intstatus & 0x8000) != 0x8000);
                IsPermittedInterlock1 = ((intstatus & 0x100) == 0x100);
                IsPermittedInterlock2 = ((intstatus & 0x200) == 0x200);
                IsPermittedInterlock3 = ((intstatus & 0x400) == 0x400);
                IsPermittedInterlock4 = ((intstatus & 0x800) == 0x800);
                IsPermittedInterlock5 = ((intstatus & 0x10) == 0x10);
                IsPermittedInterlock6 = ((intstatus & 0x20) == 0x20);
                IsPermittedInterlock7 = ((intstatus & 0x40) == 0x40);
                IsPermittedInterlock8 = ((intstatus & 0x80) == 0x80);
                return true;

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return true;
            }
        }

        public bool ParseSpeedLevel(string data)
        {
            try
            {
                string speedtag = data.Substring(6, 1);

                switch(speedtag)
                {
                    case "H":
                        SpeedLevel = 1;
                        break;
                    case "M":
                        SpeedLevel = 2;
                        break;
                    case "L":
                        SpeedLevel = 3;
                        break;
                }

                return true;

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        public bool ParsePositionData(string pdata)
        {
            try
            {
                string StsN = pdata.Substring(0, 2);
                string Ackcd = pdata.Substring(2, 4);
                ParseStatus(StsN);

                CurrentThetaPosition = Convert.ToInt32(pdata.Substring(6, 8)) / 100;
                PositionAxis1 = CurrentThetaPosition;               
                CurrentArm1Position = Convert.ToInt32(pdata.Substring(14, 8)) / 100;
                PositionAxis2 = CurrentArm1Position;
                CurrentArm2Position = Convert.ToInt32(pdata.Substring(22, 8)) / 100;
                PositionAxis3 = CurrentArm2Position;
                CurrentZPosition = Convert.ToInt32(pdata.Substring(30, 8)) / 100;
                PositionAxis4 = CurrentZPosition;

                //if (pdata[0] == "R")
                //{
                //    CommandThetaPosition = Convert.ToSingle(pdata[1]) / 1000;
                //    CommandExtensionPosition = Convert.ToSingle(pdata[2]) / 1000;
                //    CommandArm1Position = Convert.ToSingle(pdata[3]) / 1000;
                //    CommandArm2Position = Convert.ToSingle(pdata[4]) / 1000;
                //    CommandZPosition = Convert.ToSingle(pdata[5]) / 1000;
                //    return true;
                //}
                //if (pdata[0] == "F")
                //{
                //    CurrentThetaPosition = Convert.ToSingle(pdata[1]) / 1000;
                //    PositionAxis1 = CurrentThetaPosition;
                //    CurrentExtensionPosition = Convert.ToSingle(pdata[2]) / 1000;
                //    PositionAxis2 = CurrentExtensionPosition;
                //    CurrentArm1Position = Convert.ToSingle(pdata[3]) / 1000;
                //    PositionAxis3 = CurrentArm1Position;
                //    CurrentArm2Position = Convert.ToSingle(pdata[4]) / 1000;
                //    PositionAxis4 = CurrentArm2Position;
                //    CurrentZPosition = Convert.ToSingle(pdata[5]) / 1000;
                //    PositionAxis5 = CurrentZPosition;
                //    return true;
                //}
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        public bool ParseRegisteredPositionData(string pdata)
        {
            try
            {
                string StsN = pdata.Substring(0, 2);
                string Ackcd = pdata.Substring(2, 4);
                ParseStatus(StsN);

                CommandThetaPosition = Convert.ToInt32(pdata.Substring(6, 8)) / 100;
                
                CommandArm1Position = Convert.ToInt32(pdata.Substring(14, 8)) / 100;
              
                CommandArm2Position = Convert.ToInt32(pdata.Substring(22, 8)) / 100;
              
                CommandZPosition = Convert.ToInt32(pdata.Substring(30, 8)) / 100;
           

                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
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
        public bool ParseParameterData(string pdata)
        {
            try
            {
                ReadParameterType = pdata.Substring(6,3);
                ReadParameterNo = pdata.Substring(9, 4);
                ReadParameterValue = pdata.Substring(13, 12);
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        public bool ParseInterlockInfo(string pdata)
        {
            try
            {
                int intdata = Convert.ToInt16(pdata.Substring(6,4));
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
        public bool ParseSoftwareVersion(string pdata)
        {
            try
            {
                RobotSystemVersion = pdata.Substring(6,4);
                RobotSoftwareVersion = pdata.Substring(6, 4);
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        public bool ParseSlotMap(string pdata)
        {
            //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,RMAP,<TrsSt>,<Slot>,
            //01:<Result1>…,N:<ResultN>(,<Sum>)<CR>
            //• UNo : Unit number (1 byte)
            //• SeqNo : Sequence number (None / 2 bytes)
            //• Sts : Status (2 bytes)
            //• Ackcd : Response code (4 bytes)
            //• TrsSt : Transfer station (3 bytes)
            //• Slot : Slot number (2 bytes)
            //• Result* : Mapping result (2 bytes each)
            //• “--” : No wafer detected.
            //• “OK” : Wafer inserted correctly.
            //• “CW” : Wafer inserted incorrectly (inclined).
            //• “DW” : Wafer inserted incorrectly (duplicated).
            //Note) Responds with the number of slots of the specified transfer station.
            //$,1,00,0000,RMAP,C02,00,
            //01:OK,02:DW,03:OK,04:CW,05:CW,06:OK,07:OK,08:--,09:OK,10:OK    
            // No wafer: "0", Wafer: "1", Crossed:"2", Undefined: "?", Overlapping wafers: "W" 
            try
            {
                string slotmapdata = pdata.Substring(6, pdata.Length - 6);

                int wafercount = slotmapdata.Length / 4;

                StringBuilder sb = new StringBuilder();
                for (int i= 0;i< wafercount; i++)
                {
                    string value = slotmapdata.Substring(4*i+2,2);
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
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        public bool ParseMappingData(string pdata)
        {
            try
            {
                string mdata = pdata.Substring(6, pdata.Length - 6);
                int wafercount = mdata.Length / 14;
                List<string> lstupdata = new List<string>();
                List<string> lstdowndata = new List<string>();
                for(int i=0;i< wafercount; i++)
                {
                    lstupdata.Add(mdata.Substring(14*i,7));
                    lstdowndata.Add(mdata.Substring(14 * i+7, 7));
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
            lock(_locker)
            {
                _lstHandlers.Clear();
                _connection.ForceClear();
                _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "CCLR", "E"));
                _lstHandlers.AddLast(new NXC100RobotReadHandler(this, "RSTS"));
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
                        _lstHandlers.AddLast(new NXC100RobotReadHandler(this, "RSTS"));
                        _lstHandlers.AddLast(new NXC100RobotReadHandler(this, "RPOS", "FFAA"));
                    }
                    break;
                default:
                    break;
            }
            return true;
        }
        private void ExecuteHandler(HandlerBase handler)
        {
            string commandstr = $"{UnitNumber}";
            if (IsEnableSeqNo)
            {
                CurrentSeqNo = SeqnoGenerator.create();
                commandstr += $"{CurrentSeqNo:D2}";
                SeqnoGenerator.release(CurrentSeqNo);
            }
            commandstr += $"{handler.SendText}";

            if(IsEnableCheckSum)
            {
                commandstr += "";
                commandstr += Checksum(Encoding.ASCII.GetBytes(commandstr));
            }
            handler.SendText = $"${commandstr}\r";
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
                switch (setcommand)
                {
                    case "MotionSpeed":   // SSPD Set the motion speed
                        string strlevel = param[1].ToString();
                        string strspeedtype = param[2].ToString();
                        string strAxis = param[3].ToString();
                        uint speeddata = Convert.ToUInt32(param[4]);
                        if (!"0123".Contains(strlevel))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + strlevel);
                            return false;
                        }
                        if(!"HMLOB".Contains(strspeedtype))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + strspeedtype);
                            return false;
                        }
                        if (!"SAHIZRG".Contains(strAxis))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + strAxis);
                            return false;
                        }
                        strParameter = $"{strlevel},{strspeedtype},{strAxis}," + speeddata.ToString("D8");
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new NXC100RobotSetHandler(this, "SSPD", strParameter));
                        }
                        break;
                    case "TransferSpeedLevel":    //SSLV Select the transfer speed level
                        string sslvlevel = param[1].ToString();
                        if (!"123".Contains(sslvlevel))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + sslvlevel);
                            return false;
                        }
                        string strSpeedLevel = "M";
                        switch (SpeedLevelSetting)
                        {
                            case 1:
                                strSpeedLevel = "H";
                                break;
                            case 2:
                                strSpeedLevel = "M";
                                break;
                            case 3:
                                strSpeedLevel = "L";
                                break;
                            default:
                                strSpeedLevel = "H";
                                break;
                        }
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new NXC100RobotSetHandler(this, "SSLV", strSpeedLevel));
                        }
                        if (SC.ContainsItem($"{_scRoot}.{Name}.SpeedLevel"))
                        {
                            SC.SetItemValue($"{_scRoot}.{Name}.SpeedLevel", Convert.ToInt32(sslvlevel));
                            SpeedLevelSetting = SC.GetValue<int>($"{_scRoot}.{Name}.SpeedLevel");
                        }
                        break;
                    case "RegisterTheCurrentPositionAsTransferStation":   // SPOS: Register the current position as the specified transfer station
                        string sposMem = param[1].ToString();
                        string sposRmode = param[2].ToString();
                        string sposTrsSt = param[3].ToString();
                        uint sposSlot = Convert.ToUInt16(param[4]);
                        string sposPosture = param[5].ToString();
                        string sposHand= param[6].ToString();
                        if(!"VN".Contains(sposMem))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + sposMem);
                            return false;
                        }
                        if (!"AN".Contains(sposRmode))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + sposRmode);
                            return false;
                        }
                        if (sposSlot <1 || sposSlot>30)
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + sposSlot.ToString());
                            return false;
                        }
                        if (!"LR".Contains(sposPosture))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + sposPosture);
                            return false;
                        }
                        if (!"12".Contains(sposHand))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + sposHand);
                            return false;
                        }
                        strParameter = $"{sposMem},{sposRmode},{sposTrsSt},{sposSlot},{sposPosture},{sposHand}";

                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new NXC100RobotSetHandler(this, "SPOS", strParameter));
                        }
                        break;
                    case "RegisterTheSpePostionAsTransferStation":   //SABS
                        string sabsMem = param[1].ToString();
                        string sabsRmode = param[2].ToString();
                        string sabsTrsSt = param[3].ToString();
                        string sabsPosture = param[4].ToString();
                        string sabsHand = param[5].ToString();
                        Int32 sabsValue1 = Convert.ToInt32(param[6]);
                        Int32 sabsValue2 = Convert.ToInt32(param[7]);
                        Int32 sabsValue3 = Convert.ToInt32(param[8]);
                        Int32 sabsValue4 = Convert.ToInt32(param[9]);
                        Int32 sabsValue5 = Convert.ToInt32(param[10]);
                        if (!"VN".Contains(sabsMem))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + sabsMem);
                            return false;
                        }
                        if (!"AN".Contains(sabsRmode))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + sabsRmode);
                            return false;
                        }                      
                        if (!"LR".Contains(sabsPosture))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + sabsPosture);
                            return false;
                        }
                        if (!"12".Contains(sabsHand))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + sabsHand);
                            return false;
                        }
                        strParameter = $"{sabsMem},{sabsRmode},{sabsTrsSt},{sabsPosture},{sabsHand},"
                            + sabsValue1.ToString("D8") + "," + sabsValue2.ToString("D8") + "," + sabsValue3.ToString("D8") +
                            "," + sabsValue4.ToString("D8") + "," + sabsValue5.ToString("D8");

                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new NXC100RobotSetHandler(this, "SABS", strParameter));
                        }
                        break;
                    case "ModifyTheSpecStationPostionByOffset": //SAPS
                        string sapsMem = param[1].ToString();
                        string sapsRmode = param[2].ToString();
                        string sapsTrsSt = param[3].ToString();                      
                        string sapsPosture = param[4].ToString();
                        string sapsHand = param[5].ToString();
                        Int32 sapsOffsetX = Convert.ToInt32(param[6]);
                        Int32 sapsOffsetY = Convert.ToInt32(param[7]);
                        Int32 sapsOffsetZ = Convert.ToInt32(param[8]);
                        strParameter = $"{sapsMem},{sapsRmode},{sapsTrsSt},{sapsPosture},{sapsHand},"
                            + sapsOffsetX.ToString("D8") + "," + sapsOffsetY.ToString("D8") + "," + sapsOffsetZ.ToString("D8");
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new NXC100RobotSetHandler(this, "SAPS", strParameter));
                        }
                        break;
                    case "DeleteTheSpecStation": //SPDL
                        string spdlMem = param[1].ToString();
                        string spdlTrsSt = param[2].ToString();
                        string spdlPosture = param[3].ToString();
                        string spdlHand = param[4].ToString();

                        strParameter = $"{spdlMem},{spdlTrsSt},{spdlPosture},{spdlHand}";        
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new NXC100RobotSetHandler(this, "SPDL", strParameter));
                        }
                        break;
                    case "RegisterThePositionDataToVolatile": //SPSV
                        string spsvTrsSt = param[1].ToString();
                        string spsvPosture = param[2].ToString();
                        string spsvHand = param[3].ToString();
                        strParameter = $"{spsvTrsSt},{spsvPosture},{spsvHand}";
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new NXC100RobotSetHandler(this, "SPSV", strParameter));
                        }



                        break;
                    case "ReadThePostionDataFromVolatile": //SPLD
                        string spldTrsSt = param[1].ToString();
                        string spldPosture = param[2].ToString();
                        string spldHand = param[3].ToString();
                        strParameter = $"{spldTrsSt},{spldPosture},{spldHand}";
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new NXC100RobotSetHandler(this, "SPLD", strParameter));
                        }

                        break;
                    case "SetTheStationParameters": //SSTR
                        string sstrMem = param[1].ToString();
                        string sstrTrsSt = param[2].ToString();
                        string sstrItem = param[3].ToString();
                        Int32 sstrValue = Convert.ToInt32(param[4].ToString());
                        strParameter = $"{sstrMem},{sstrTrsSt},{sstrItem}," + sstrValue.ToString("D8");
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new NXC100RobotSetHandler(this, "SSTR", strParameter));
                        }
                        break;
                    case "ChangeParameterValue": // SPRM
                        string sprmParaType = param[1].ToString();
                        int sprmParaNO = Convert.ToInt32(param[2].ToString());
                        Int32 sprmValue = Convert.ToInt32(param[3].ToString());
                        strParameter = sprmParaType + "," +sprmParaNO.ToString("D4") + "," + sprmValue.ToString("D12");
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new NXC100RobotSetHandler(this, "SPRM", strParameter));
                        }
                        break;
                    case "EnableInterLock": //SMSK
                        int smskValid = Convert.ToInt16(param[1].ToString());
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new NXC100RobotSetHandler(this, "SPRM", smskValid.ToString("D4")));
                        }
                        break;
                    case "RegisterTheCurrentPositionAsCoordinate": //SSTD
                        string sstdAxis = param[1].ToString();
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new NXC100RobotSetHandler(this, "SSTD", sstdAxis));
                        }
                        break;
                    case "ResigterTheSpecNumberAsReferencePostion":   //SSTN
                        Int32 sstnValue1 = Convert.ToInt32(param[1]);
                        Int32 sstnValue2 = Convert.ToInt32(param[2]);
                        Int32 sstnValue3 = Convert.ToInt32(param[3]);
                        Int32 sstnValue4 = Convert.ToInt32(param[4]);
                        Int32 sstnValue5 = Convert.ToInt32(param[5]);

                        strParameter = sstnValue1.ToString("D12") + "," + sstnValue2.ToString("D12") + ","
                            + sstnValue3.ToString("D12") + "," + sstnValue4.ToString("D12") + ","
                            + sstnValue5.ToString("D12");
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new NXC100RobotSetHandler(this, "SSTN", strParameter));
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
            return true;
        }
        protected override bool fStartTransferWafer(object[] param)
        {
            return false; 
        }

        protected override bool fStartUnGrip(object[] param)
        {
            lock(_locker)
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                switch (arm)
                {
                    case RobotArmEnum.Lower:
                        _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "CSOL", "B0"));
                        break;
                    case RobotArmEnum.Upper:
                        _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "CSOL", "A0"));
                        break;
                    case RobotArmEnum.Both:
                        _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "CSOL", "A0"));
                        _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "CSOL", "B0"));
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
                switch(arm)
                {
                    case RobotArmEnum.Lower:                        
                        _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "CSOL", "B1"));
                        break;
                    case RobotArmEnum.Upper:
                        _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "CSOL", "A1"));
                        break;
                    case RobotArmEnum.Both:
                        _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "CSOL", "A1"));
                        _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "CSOL", "B1"));
                        break;
                }
                
            }
            return true;
        }
        protected override bool fStartInit(object[] param)
        {
            SpeedLevelSetting = SC.GetValue<int>($"{_scRoot}.{Name}.SpeedLevel");
            string strSpeedLevel = "H";
            switch(SpeedLevelSetting)
            {
                case 1:
                    strSpeedLevel = "H";
                    break;
                case 2:
                    strSpeedLevel = "M";
                    break;
                case 3:
                    strSpeedLevel = "L";
                    break;
                default:
                    strSpeedLevel = "H";
                    break;
            }
            lock (_locker)
            {
                if (_doRobotHold != null)
                {
                    _doRobotHold.SetTrigger(true, out _);
                    Thread.Sleep(100);
                }                
                //_lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "CCLR", "E"));
                //_lstHandlers.AddLast(new NXC100RobotGripAndCheckWaferMotionHandler(this));
                _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "INIT", "00"));
                _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "MHOM", "F"));
                if (SpeedLevelSetting >=1 && SpeedLevelSetting<=3)
                    _lstHandlers.AddLast(new NXC100RobotSetHandler(this, "SSLV", strSpeedLevel));
                _lstHandlers.AddLast(new NXC100RobotReadHandler(this, "RSTS"));

            }
            return true;
        }
        protected override bool fStartHome(object[] param)
        {
            SpeedLevelSetting = SC.GetValue<int>($"{_scRoot}.{Name}.SpeedLevel");
            string strSpeedLevel = "H";
            switch (SpeedLevelSetting)
            {
                case 1:
                    strSpeedLevel = "H";
                    break;
                case 2:
                    strSpeedLevel = "M";
                    break;
                case 3:
                    strSpeedLevel = "L";
                    break;
                default:
                    strSpeedLevel = "H";
                    break;
            }
            lock (_locker)
            {
                if (_doRobotHold != null)
                {
                    _doRobotHold.SetTrigger(true, out _);
                    Thread.Sleep(100);
                }
                //_lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "CCLR", "E"));
                //_lstHandlers.AddLast(new NXC100RobotGripAndCheckWaferMotionHandler(this));
                _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "INIT", "00"));
                _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "MHOM", "F"));
                if (SpeedLevelSetting >= 1 && SpeedLevelSetting <= 3)
                    _lstHandlers.AddLast(new NXC100RobotSetHandler(this, "SSLV", strSpeedLevel));
                _lstHandlers.AddLast(new NXC100RobotReadHandler(this, "RSTS"));

            }
            return true;
        }

        protected override bool fStartGoTo(object[] param)
        {
            try
            {
                RobotArmEnum arm = (RobotArmEnum)param[0];
                ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[1].ToString());
                Blade1Target = module;
                Blade2Target = module;
                int slot = (int)param[2]+1;
                RobotPostionEnum postype = (RobotPostionEnum)param[3];
                float x = (float)param[4];
                float y = (float)param[5];
                float z = (float)param[6];
                float w = (float)param[7];
                int intXvalue = (int)(x * 1000);
                int intYvalue = (int)(y * 1000);
                int intZvalue = (int)(z * 1000);
                int intWvalue = (int)(w * 1000);

                bool isFromOriginal = (bool)param[8];
                bool isJumpToNextMotion = (bool)param[9];
                string strpara =string.Empty;
                if((int)postype >=0 && (int)postype<10)
                {
                    strpara = "G";
                }
                if((int)postype >= 10 && (int)postype < 20)
                {
                    strpara = "P";
                }
                if ((int)postype >= 20 && (int)postype < 30)
                {
                    strpara = "E";
                }
                
                string trsSt = GetStationsName(module);
                
                string TrsPnt = string.Empty;
                if(postype == RobotPostionEnum.PickReady)
                {
                    TrsPnt = "G1";
                }
                if(postype == RobotPostionEnum.PickExtendLow)
                {
                    TrsPnt = "G2";
                }
                if (postype == RobotPostionEnum.PickAtWafer)
                {
                    TrsPnt = "Gb";
                }
                if (postype == RobotPostionEnum.PickExtendUp)
                {
                    TrsPnt = "G3";
                }
                if (postype == RobotPostionEnum.PickRetracted)
                {
                    TrsPnt = "G4";
                }

                if (postype == RobotPostionEnum.PlaceReady)
                {
                    TrsPnt = "P1";
                }
                if (postype == RobotPostionEnum.PlaceExtendUp)
                {
                    TrsPnt = "P2";
                }
                if (postype == RobotPostionEnum.PlaceExtendAtWafer)
                {
                    TrsPnt = "Pb";
                }
                if (postype == RobotPostionEnum.PlaceExtendDown)
                {
                    TrsPnt = "P3";
                }
                if (postype == RobotPostionEnum.PlaceRetract)
                {
                    TrsPnt = "P4";
                }
                string strCmd = string.Empty;
                if(isFromOriginal)
                {
                    strCmd = "MTRS";
                }
                else if(isJumpToNextMotion)
                {
                    strCmd = "MCTR";
                }
                else
                {
                    strCmd = "MPNT";
                }
                if (string.IsNullOrEmpty(trsSt) || string.IsNullOrEmpty(strpara)||string.IsNullOrEmpty(TrsPnt))
                {
                    EV.PostAlarmLog("Robot", "invalid transfer paramter");
                    return false;
                }
                strpara += $",{trsSt},{slot:D2},A,{(arm == RobotArmEnum.Both ? "F" : ((int)arm+1).ToString())}," +
                    $"{TrsPnt}";
                if (x != 0 || y != 0 || z != 0 || w != 0)
                {
                    string strxoffset = intXvalue >= 0 ? $"{intXvalue:D8}" : $"{intXvalue:D7}";
                    string stryoffset = intYvalue >= 0 ? $"{intYvalue:D8}" : $"{intYvalue:D7}";
                    string strzoffset = intZvalue >= 0 ? $"{intZvalue:D8}" : $"{intZvalue:D7}";
                    string strwoffset = intWvalue >= 0 ? $"{intWvalue:D8}" : $"{intWvalue:D7}";
                    strpara += $",{strxoffset},{stryoffset},{strzoffset},{strwoffset}";
                }
                if (strCmd == "MPNT")
                    strpara = TrsPnt;
                lock (_locker)
                {
                    if(_lstHandlers.Count == 0 && !_connection.IsBusy)
                    {
                        ExecuteHandler(new NXC100RobotMotionHandler(this, strCmd, strpara));
                    }
                    else
                    {
                        _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, strCmd, strpara));
                    }
                }
                return true;
            }
            catch(Exception ex)
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
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return base.fGoToComplete(param);
        }
        protected override bool fStop(object[] param)
        {
            
                if(_doRobotHold !=null)
                    _doRobotHold.SetTrigger(false,out _);
                _lstHandlers.Clear();
                _connection.ForceClear();
                //ExecuteHandler(new SR100RobotMotionHandler(this, "CSTP", "E"));
            
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
                    if(i==1)
                        strpara += param[i].ToString();
                    else
                        strpara += param[i].ToString();
                }
                lock (_locker)
                {
                    _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, strCmd, strpara));
                    _lstHandlers.AddLast(new NXC100RobotReadHandler(this, "RPOS", "FFAA"));
                }
                return true;
            }
            catch(Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        private string GetStationsName(ModuleName module)
        {
            try
            {
                if (ModuleHelper.IsLoadPort(module))
                {
                    int infopadindex = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString()).InfoPadCarrierIndex;
                    return SC.GetStringValue($"CarrierInfo.{module}Station{infopadindex}");
                }
                return SC.GetStringValue($"CarrierInfo.{module}Station");
            }
            catch(Exception ex)
            {
                LOG.Write(ex);
                return null;
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
        protected override bool fStartMapWafer(object[] param)
        {
            try
            {
                //RobotArmEnum pickarm = (RobotArmEnum)param[0];
                ModuleName module = (ModuleName)Enum.Parse(typeof(ModuleName), param[0].ToString());
                Blade1Target = module;
                Blade2Target = module;
                int slotsNumber = GetSlotsNumber(module);
                if (slotsNumber == -1)
                {
                    EV.PostAlarmLog("Robot", "Invalid mapping paramter slots number");
                    return false;
                }
                //int slot = 25;// (int)param[2];
                string strpara = $"{GetStationsName(module)}FF";
                if(ModuleHelper.IsLoadPort(module))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
                    if (lp != null) lp.NoteTransferStart();
                }

                lock (_locker)
                {
                    CurrentInteractiveModule = module;
                    _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "MMAP", strpara));
                    //_lstHandlers.AddLast(new NXC100RobotReadHandler(this, "RMPD", $"{GetStationsName(module)}"));
                    _lstHandlers.AddLast(new NXC100RobotReadHandler(this, "RMAP", strpara));
                    


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
                Blade1Target = module;
                Blade2Target = module;
                if (ModuleHelper.IsLoadPort(module))
                {
                    var lp = DEVICE.GetDevice<LoadPortBaseDevice>(module.ToString());
                    if (lp != null)
                        lp.NoteTransferStart();
                }

                int slot = (int)param[2]+1;
                float x = 0, y = 0, z = 0, w = 0;
                if (param.Length > 3)
                {
                    x = (float)param[3];
                    y = (float)param[4];
                    z = (float)param[5];
                    w = (float)param[6];
                }
                int intXvalue = (int)(x * 1000);
                int intYvalue = (int)(y * 1000);
                int intZvalue = (int)(z * 1000);
                int intWvalue = (int)(w * 1000);
                string TrsSt = GetStationsName(module);
                if (string.IsNullOrEmpty(TrsSt))
                {
                    EV.PostAlarmLog("Robot", "Invalid Paramter.");
                    return false;
                }
                string strpara = $"{TrsSt}{slot:D2}E{hand2string(arm)}A";
               
                lock (_locker)
                {
                    if (_lstHandlers.Count == 0 && !_connection.IsBusy)
                    {

                        ExecuteHandler(new NXC100RobotMotionHandler(this, "MTRE", strpara));
                    }
                    else
                    {
                        _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "MTRE", strpara));
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
            int delayCount = 0;
            if (arm == RobotArmEnum.Lower)
            {
                
                while (!isSimulatorMode && !(GetWaferState(RobotArmEnum.Lower) == RobotArmWaferStateEnum.Present
                        && GetWaferState(RobotArmEnum.Upper) == RobotArmWaferStateEnum.Absent)) 
                {
                    delayCount++;
                    Thread.Sleep(50);
                    LOG.Write($"{RobotModuleName} delay {delayCount} time to detect wafer");
                    if (delayCount >100)
                    {
                        OnError("Wafer detect error");
                        return true;
                    }
                }
                WaferManager.Instance.WaferMoved(sourcemodule, Sourceslotindex, RobotModuleName, 0);
                WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);
               
            }
            if (arm == RobotArmEnum.Upper)
            {
                //WaferManager.Instance.WaferMoved(sourcemodule, Sourceslotindex, RobotModuleName, 1);
                //WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                delayCount = 0;
                while (!isSimulatorMode && !(GetWaferState(RobotArmEnum.Upper) == RobotArmWaferStateEnum.Present && 
                    GetWaferState(RobotArmEnum.Lower) == RobotArmWaferStateEnum.Absent))
                {
                    delayCount++;
                    Thread.Sleep(50);
                    LOG.Write($"{RobotModuleName} delay {delayCount} time to detect wafer");
                    if (delayCount > 100)
                    {
                        OnError("Wafer detect error");
                        return true;
                    }
                }
                WaferManager.Instance.WaferMoved(sourcemodule, Sourceslotindex, RobotModuleName, 1);
                WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                
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
                Blade1Target = module;
                Blade2Target = module;
                int slot = (int)param[2]+1;
                float x=0, y=0, z=0, w=0;
                if(param.Length >3)
                {
                    x = (float)param[3];
                    y= (float)param[4];
                    z= (float)param[5];
                    w = (float)param[6];
                }
                int intXvalue = Convert.ToInt32(x * 100);
                int intYvalue = Convert.ToInt32(y * 100);
                int intZvalue = Convert.ToInt32(z * 100);
                //int intWvalue = Convert.ToInt32(w * 100);
                string TrsSt = GetStationsName(module);
                if(string.IsNullOrEmpty(TrsSt))
                {
                    EV.PostAlarmLog("Robot", "Invalid Parameter.");
                    return false;
                }
                string strpara = $"{TrsSt}{slot:D2}P{hand2string(arm)}A";
                if (x!=0 ||y!=0||z!=0)
                {
                    string strxoffset = intXvalue >= 0 ? $"{intXvalue:D5}" : $"{intXvalue:D4}";
                    string stryoffset = intYvalue >= 0 ? $"{intYvalue:D5}" : $"{intYvalue:D4}";
                    string strzoffset = intZvalue >= 0 ? $"{intZvalue:D5}" : $"{intZvalue:D4}";
                    //string strwoffset = intWvalue >= 0 ? $"{intWvalue:D8}" : $"{intWvalue:D7}";
                    strpara += $"{strxoffset}{stryoffset}{strzoffset}";
                }
               
                lock(_locker)
                {
                    if(_lstHandlers.Count ==0 && !_connection.IsBusy)
                    {
                        if (x != 0 || y != 0 || z != 0)
                        {
                            ExecuteHandler(new NXC100RobotMotionHandler(this, "MTPO", strpara));
                        }
                        else
                            ExecuteHandler(new NXC100RobotMotionHandler(this, "MTRP", strpara));

                    }
                    else
                    {
                        if (x != 0 || y != 0 || z != 0)
                        {
                            _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "MTPO", strpara));
                        }
                        else
                            _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "MTRP", strpara));
                    }
                }
                return true;
            }
            catch(Exception ex)
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
            int delayCount = 0;
            if (arm == RobotArmEnum.Lower)
            {
                //WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                while (!isSimulatorMode && GetWaferState(arm) != RobotArmWaferStateEnum.Absent)
                {
                    delayCount++;
                    Thread.Sleep(50);
                    LOG.Write($"{RobotModuleName} delay {delayCount} time to detect wafer");
                    if (delayCount > 100)
                    {
                        OnError("Wafer detect error");
                        return true;
                    }
                }
                WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
              
            }
            if (arm == RobotArmEnum.Upper)
            {
                //WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);
                delayCount = 0;
                while (!isSimulatorMode && GetWaferState(arm) != RobotArmWaferStateEnum.Absent)
                {
                    delayCount++;
                    Thread.Sleep(50);
                    LOG.Write($"{RobotModuleName} delay {delayCount} time to detect wafer");
                    if (delayCount > 100)
                    {
                        OnError("Wafer detect error");
                        return true;
                    }
                }
                WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);
                
            }
            if (arm == RobotArmEnum.Both)
            {
                //WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                //WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex);
                while (!isSimulatorMode && GetWaferState(arm) != RobotArmWaferStateEnum.Absent)
                {
                    delayCount++;
                    Thread.Sleep(50);
                    LOG.Write($"{RobotModuleName} delay {delayCount} time to detect wafer");
                    if (delayCount > 100)
                    {
                        OnError("Wafer detect error");
                        return true;
                    }
                }
                WaferManager.Instance.WaferMoved(RobotModuleName, 0, sourcemodule, Sourceslotindex);
                WaferManager.Instance.WaferMoved(RobotModuleName, 1, sourcemodule, Sourceslotindex+1);
                
            }
            return base.fPlaceComplete(param);
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
                Blade1Target = module;
                Blade2Target = module;
                int slot = (int)param[2]+1;
                float x = 0, y = 0, z = 0, w = 0;
                if (param.Length > 3)
                {
                    x = (float)param[3];
                    y = (float)param[4];
                    z = (float)param[5];
                    w = (float)param[6];
                }
                int intXvalue = Convert.ToInt32(x * 100);
                int intYvalue = Convert.ToInt32(y * 100);
                int intZvalue = Convert.ToInt32(z * 100);
                string TrsSt = GetStationsName(module);
                if (string.IsNullOrEmpty(TrsSt))
                {
                    EV.PostAlarmLog("Robot", "Invalid Paramter.");
                    return false;
                }
                string strpara = $"{TrsSt}{slot:D2}G{hand2string(arm)}A";
                if (x != 0 || y != 0 || z != 0)
                {
                    string strxoffset = intXvalue >= 0 ? $"{intXvalue:D5}" : $"{intXvalue:D4}";
                    string stryoffset = intYvalue >= 0 ? $"{intYvalue:D5}" : $"{intYvalue:D4}";
                    string strzoffset = intZvalue >= 0 ? $"{intZvalue:D5}" : $"{intZvalue:D4}";
                    //string strwoffset = intWvalue >= 0 ? $"{intWvalue:D5}" : $"{intWvalue:D4}";
                    strpara += $"{strxoffset}{stryoffset}{strzoffset}";                    
                }
                
                lock (_locker)
                {
                    if (_lstHandlers.Count == 0 && !_connection.IsBusy)
                    {
                        if (x != 0 || y != 0 || z != 0)
                        {
                            ExecuteHandler(new NXC100RobotMotionHandler(this, "MTGO", strpara));
                        }
                        else
                        {
                            ExecuteHandler(new NXC100RobotMotionHandler(this, "MTRG", strpara));
                        }
                    }
                    else
                    {
                        if (x != 0 || y != 0 || z != 0)
                        {
                            _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "MTGO", strpara));
                        }
                        else
                        {
                            _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "MTRG", strpara));
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
        protected override bool fPickComplete(object[] param)
        {
            RobotArmEnum arm = (RobotArmEnum)CurrentParamter[0];
            ModuleName sourcemodule;
            if (!Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule)) return false;
            int SourceslotIndex;
            if (!int.TryParse(CurrentParamter[2].ToString(), out SourceslotIndex)) return false;
            int delayCount = 0;
            if (arm == RobotArmEnum.Lower)
            {
                //WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                while (!isSimulatorMode && GetWaferState(arm) != RobotArmWaferStateEnum.Present)
                {
                    delayCount++;
                    LOG.Write($"{RobotModuleName} delay {delayCount} time to detect wafer");
                    Thread.Sleep(50);
                    if (delayCount > 100)
                    {
                        OnError("Wafer detect error");
                        return true;
                    }
                }
                WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
               
            }
            if (arm == RobotArmEnum.Upper)
            {
                //WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                while (!isSimulatorMode && GetWaferState(arm) != RobotArmWaferStateEnum.Present)
                {
                    delayCount++;
                    LOG.Write($"{RobotModuleName} delay {delayCount} time to detect wafer");
                    Thread.Sleep(50);
                    if (delayCount > 100)
                    {
                        OnError("Wafer detect error");
                        return true;
                    }
                }
                WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                
            }
            if (arm == RobotArmEnum.Both)
            {
                //WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                //WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 1);
                while (!isSimulatorMode && GetWaferState(arm) != RobotArmWaferStateEnum.Present)
                {
                    delayCount++;
                    LOG.Write($"{RobotModuleName} delay {delayCount} time to detect wafer");
                    Thread.Sleep(50);
                    if (delayCount > 100)
                    {
                        OnError("Wafer detect error");
                        return true;
                    }
                }
                WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex, RobotModuleName, 0);
                WaferManager.Instance.WaferMoved(sourcemodule, SourceslotIndex+1, RobotModuleName, 1);
                
            }
            return base.fPickComplete(param);
        }

       
        protected override bool fResetToReady(object[] param)
        {
            if (_doRobotHold != null)
                _doRobotHold.SetTrigger(true, out _);
            return true ;
        }

        protected override bool fReset(object[] param)
        {
            if(!_connection.IsConnected || _address != SC.GetStringValue($"{_scRoot}.{Name}.Address"))
            {
                _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
                _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");                
                _connection = new YaskawaNXC100Connection(this, _address);
                _connection.EnableLog(_enableLog);
                _connection.Connect();
            }
            lock (_locker)
            {
                if (_doRobotHold != null)
                    _doRobotHold.SetTrigger(true, out _);
                _lstHandlers.Clear();
                _connection.ForceClear();
                _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "CCLR", "E"));
                _lstHandlers.AddLast(new NXC100RobotReadHandler(this, "RSTS"));
            }
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
        public void CheckWaferPresentAndGrip()
        {
            if(GetWaferState( RobotArmEnum.Lower) == RobotArmWaferStateEnum.Present)
            {
                if (WaferManager.Instance.CheckNoWafer(RobotModuleName, 0))
                {
                    EV.PostWarningLog($"{RobotModuleName}", $"System detec wafer on lower arm, will create wafer automatically.");
                    WaferManager.Instance.CreateWafer(RobotModuleName, 0, WaferStatus.Normal);
                }
            }
            if (GetWaferState(RobotArmEnum.Lower) == RobotArmWaferStateEnum.Absent)
            {
                _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "CSOL","1,0,0"));
                if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                {
                    EV.PostWarningLog($"{RobotModuleName}", $"System didn't detec wafer on lower arm, but it has record.");
                }
            }
            if (GetWaferState(RobotArmEnum.Upper) == RobotArmWaferStateEnum.Present)
            {
                if (WaferManager.Instance.CheckNoWafer(RobotModuleName, 1))
                {
                    EV.PostWarningLog($"{RobotModuleName}", $"System detec wafer on upper arm, will create wafer automatically.");
                    WaferManager.Instance.CreateWafer(RobotModuleName, 1, WaferStatus.Normal);
                }
            }
            if (GetWaferState(RobotArmEnum.Upper) == RobotArmWaferStateEnum.Absent)
            {
                _lstHandlers.AddLast(new NXC100RobotMotionHandler(this, "CSOL", "2,0,0"));
                if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 1))
                {
                    EV.PostWarningLog($"{RobotModuleName}", $"System didn't detec wafer on upper arm, but it has record.");
                }
            }
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
            if(arm == RobotArmEnum.Upper)
            {
                if (_diRobotBlade2WaferOn != null)
                {
                    if (_diRobotBlade2WaferOn.Value) return RobotArmWaferStateEnum.Absent;
                    else return RobotArmWaferStateEnum.Present;
                }
                return IsWaferPresenceOnBlade2 ? RobotArmWaferStateEnum.Present : RobotArmWaferStateEnum.Absent;
            }
            if(arm == RobotArmEnum.Both)
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
                if((!IsWaferPresenceOnBlade1)&&!IsWaferPresenceOnBlade2)
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
        public void SenACK()
        {
            _connection.SendAck();
        }

        public override bool OnActionDone(object[] param)
        {
            Blade1Target = ModuleName.System;
            Blade2Target = ModuleName.System;
            IsBusy = false;
            ModuleName sourcemodule;
            if (CurrentParamter!=null && CurrentParamter.Length >2 && Enum.TryParse(CurrentParamter[1].ToString(), out sourcemodule))
            {
                if(ModuleHelper.IsLoadPort(sourcemodule))
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



        public static string hand2string(RobotArmEnum hand)
        {
            string st = "";
            
            switch (hand)
            {
                case  RobotArmEnum.Upper:
                    st = "A";      //单手臂 下
                    break;
                case RobotArmEnum.Lower:
                    st = "B";     //4手臂  上
                    break;
                case RobotArmEnum.Both:
                    st = "W";
                    break;
            }
          
            
            return st;
        }

    }

    //public class YaskawaTokenGenerator
    //{
    //    private int _last = 0;
    //    List<int> _pool = new List<int>();

    //    SCConfigItem scToken = null;
    //    public int CurrentToken => _last;
        
    //    public YaskawaTokenGenerator(string scName)
    //    {
    //        scToken = SC.GetConfigItem(scName);
    //        if (scToken == null)

    //            _last = scToken.IntValue;

    //        Random r = new Random();
    //        _last = r.Next() % 20;
    //    }


    //    public int create()
    //    {
    //        int first = _last;
    //        int token = first;

    //        do
    //        {
    //            token = (token + 1) % 100;

    //            if (_pool.Contains(token))
    //                continue;

    //            _pool.Add(token);
    //            _last = token;

    //            scToken.IntValue = _last;
    //            return _last;
    //        } while (token != first);

    //        throw (new ExcuteFailedException("Get token failed,pool is full"));
    //    }      
    //    public void release(int token)
    //    {
    //        _pool.Remove(token);
    //    }


    //    public void release()
    //    {
    //        _last = 0;
    //        _pool.Clear();
    //    }
    //}
    //public enum YaskawaPositonEnum
    //{
    //    RegisteredPosition,
    //    ReadyPosition,
    //    IntermediatePosition,
    //    MappingStartPosition,
    //    MappingFinishPosition,
    //}
}
