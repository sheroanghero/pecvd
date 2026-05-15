using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.AlignersBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.YaskawaRobots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.YaskawaAligner
{
    public class YaskawaNxcAligner:AlignerBaseDevice,IConnection
    {
        public YaskawaNxcAligner(string module,string name,string scRoot,IoSensor[] dis,IoTrigger[] dos, int alignerType = 0) :base(module,name)
        {
            isSimulatorMode = SC.ContainsItem("System.IsSimulatorMode") ? SC.GetValue<bool>("System.IsSimulatorMode") : false;
            _scRoot = scRoot;
            _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            UnitNumber = SC.GetValue<int>($"{_scRoot}.{Name}.UnitNumber");
            IsEnableCheckSum = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableCheckSum");
            IsEnableSeqNo = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableSeqNo");
            AlignerType = alignerType;
            _connection = new YaskawaNxcAlignerConnection(this,_address);
            _connection.EnableLog(_enableLog);
            SeqnoGenerator = new YaskawaTokenGenerator($"{_scRoot}.{Name}.CommunicationToken");

            if(dis!= null && dis.Length >=1)
                _diPreAlignerWaferOn = dis[0];
            if (dis != null && dis.Length >= 2)
                _diPreAlignerReady = dis[1];
            if (dis != null && dis.Length >= 3)
            {
                _diPreAlignerError = dis[2];
                _diPreAlignerError.OnSignalChanged += _diPreAlignerError_OnSignalChanged;
            }
            if (dis != null && dis.Length >= 4)
            {
                _diTPinUse = dis[3];
                _diTPinUse.OnSignalChanged += _diTPinUse_OnSignalChanged;
            }
            if (dos != null && dos.Length >= 1)
            {
                _doPreAlignerHold = dos[0];
                _doPreAlignerHold.SetTrigger(true, out _);
            }
            ConnectionManager.Instance.Subscribe($"{Name}", this);
            _thread = new PeriodicJob(100, OnTimer, $"{_scRoot}.{Name} MonitorHandler", true);
            ResetPropertiesAndResponses();
            RegisterSpecialData();
            //RegisterAlarm();

        }

        private void RegisterAlarm()
        {
            EV.Subscribe(new EventItem("Alarm", "AlignerError020", $"{Name} Error:Aligner Occurred Error:Secondary power off.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError021", $"{Name} Error:Aligner Occurred Error:Secondary power on.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError040", $"{Name} Error:Aligner Occurred Error:In TEACH Mode.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError050", $"{Name} Error:Aligner Occurred Error:Unit is in motion.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError051", $"{Name} Error:Aligner Occurred Error:Unable to set pitch between slots.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError052", $"{Name} Error:Aligner Occurred Error:Unable to restart motion.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError053", $"{Name} Error:Aligner Occurred Error:Ready position moveincomplete.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError054", $"{Name} Error:Aligner Occurred Error:Alignment Ready position move incomplete.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError055", $"{Name} Error:Aligner Occurred Error:Improper station type.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError058", $"{Name} Error:Aligner Occurred Error:Command not supported 1-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError059", $"{Name} Error:Aligner Occurred Error:Invalid transfer point.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError05A", $"{Name} Error:Aligner Occurred Error:Linear motion failed.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError05C", $"{Name} Error:Aligner Occurred Error:Unable to reference waferalignment result.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError05d", $"{Name} Error:Aligner Occurred Error:Unable to perform armcalibration.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError05E", $"{Name} Error:Aligner Occurred Error:Unable to read mapping data.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError05F", $"{Name} Error:Aligner Occurred Error:Data Upload/Download inprogress.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError061", $"{Name} Error:Aligner Occurred Error:Unable to motion.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError064", $"{Name} Error:Aligner Occurred Error:Lifter interference error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError070", $"{Name} Error:Aligner Occurred Error:Bottom slot position recordincomplete.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError071", $"{Name} Error:Aligner Occurred Error:Top slot position record incomplete.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError088", $"{Name} Error:Aligner Occurred Error:Position generating error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError089", $"{Name} Error:Aligner Occurred Error:Position generating error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError08A", $"{Name} Error:Aligner Occurred Error:Position generating error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError08b", $"{Name} Error:Aligner Occurred Error:Position generating error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError08C", $"{Name} Error:Aligner Occurred Error:Position generating error 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError08d", $"{Name} Error:Aligner Occurred Error:Position generating error 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError090", $"{Name} Error:Aligner Occurred Error:Host parameter out of range.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError0A0", $"{Name} Error:Aligner Occurred Error:Alignment motion error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError0E0", $"{Name} Error:Aligner Occurred Error:Teach position adjustmentoffset amount limit error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError0F0", $"{Name} Error:Aligner Occurred Error:Voltage drop warning.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*06", $"{Name} Error:Aligner Occurred Error:Amplifier Type Mismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*07", $"{Name} Error:Aligner Occurred Error:Encoder Type Mismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*10", $"{Name} Error:Aligner Occurred Error:Overflow Current.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*30", $"{Name} Error:Aligner Occurred Error:Regeneration Error Detected.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*40", $"{Name} Error:Aligner Occurred Error:Excess Voltage (converter).", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*41", $"{Name} Error:Aligner Occurred Error:Insufficient Voltage.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*45", $"{Name} Error:Aligner Occurred Error:Brake circuit error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*46", $"{Name} Error:Aligner Occurred Error:Converter ready signal error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*47", $"{Name} Error:Aligner Occurred Error:Input power error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*48", $"{Name} Error:Aligner Occurred Error:Converter main circuit chargeerror.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*49", $"{Name} Error:Aligner Occurred Error:Amplifier ready signal error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*51", $"{Name} Error:Aligner Occurred Error:Excessive Speed.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*71", $"{Name} Error:Aligner Occurred Error:Momentary Overload (Motor).", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*72", $"{Name} Error:Aligner Occurred Error:Continuous Overload (Motor).", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*78", $"{Name} Error:Aligner Occurred Error:Overload (Converter).", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*7b", $"{Name} Error:Aligner Occurred Error:Amplifier overheat.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*7C", $"{Name} Error:Aligner Occurred Error:Continuous Overload(Amplifier).", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*7d", $"{Name} Error:Aligner Occurred Error:Momentary Overload.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*81", $"{Name} Error:Aligner Occurred Error:Absolute Encoder Back-upError.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*83", $"{Name} Error:Aligner Occurred Error:Absolute Encoder Battery.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*84", $"{Name} Error:Aligner Occurred Error:Encoder Data Error 2-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*85", $"{Name} Error:Aligner Occurred Error:Encoder Excessive Speed.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*86", $"{Name} Error:Aligner Occurred Error:Encoder Overheat.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*88", $"{Name} Error:Aligner Occurred Error:Encoder error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*89", $"{Name} Error:Aligner Occurred Error:Encoder Command failed.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*8A", $"{Name} Error:Aligner Occurred Error:Encoder multi-turn range.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*8C", $"{Name} Error:Aligner Occurred Error:Encoder Reset not completed.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*98", $"{Name} Error:Aligner Occurred Error:Servo parameter error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*9A", $"{Name} Error:Aligner Occurred Error:Feedback Over Flow.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*b4", $"{Name} Error:Aligner Occurred Error:Servo Control Board Failure.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*bC", $"{Name} Error:Aligner Occurred Error:Encoder error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*C1", $"{Name} Error:Aligner Occurred Error:Motor runaway detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*C9", $"{Name} Error:Aligner Occurred Error:Encoder Communication.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*CE", $"{Name} Error:Aligner Occurred Error:Encoder error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*CF", $"{Name} Error:Aligner Occurred Error:Encoder error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*d0", $"{Name} Error:Aligner Occurred Error:Position deviation error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*d1", $"{Name} Error:Aligner Occurred Error:Position deviation saturation.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*d2", $"{Name} Error:Aligner Occurred Error:Motor directive position error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*d4", $"{Name} Error:Aligner Occurred Error:Servo Tracking Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError*F1", $"{Name} Error:Aligner Occurred Error:Phase loss.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorE*1", $"{Name} Error:Aligner Occurred Error:Positioning Timeout.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorE*d", $"{Name} Error:Aligner Occurred Error:Command not supported 1-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorE*E", $"{Name} Error:Aligner Occurred Error:Communication Error(internal controller) 1-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorE*F", $"{Name} Error:Aligner Occurred Error:Servo control board responsetimeout 1..", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError701", $"{Name} Error:Aligner Occurred Error:ROM Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError703", $"{Name} Error:Aligner Occurred Error:Communication Error(internal controller) 2-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError704", $"{Name} Error:Aligner Occurred Error:Communication Error (internal controller) 2-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError705", $"{Name} Error:Aligner Occurred Error:Communication Error(internal controller) 2-3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError706", $"{Name} Error:Aligner Occurred Error:Servo system error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError707", $"{Name} Error:Aligner Occurred Error:Servo system error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError709", $"{Name} Error:Aligner Occurred Error:Current feedback error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError70A", $"{Name} Error:Aligner Occurred Error:Power Lost.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError70b", $"{Name} Error:Aligner Occurred Error:Rush Current PreventionRelay Abnormal.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError70C", $"{Name} Error:Aligner Occurred Error:Converter mismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError70F", $"{Name} Error:Aligner Occurred Error:Servo control board response timeout 2..", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError713", $"{Name} Error:Aligner Occurred Error:DB error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError714", $"{Name} Error:Aligner Occurred Error:Converter charge Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError715", $"{Name} Error:Aligner Occurred Error:Servo OFF Status Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError716", $"{Name} Error:Aligner Occurred Error:Servo ON Status Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError717", $"{Name} Error:Aligner Occurred Error:Servo OFF Status Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError718", $"{Name} Error:Aligner Occurred Error:Servo ON Status Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError719", $"{Name} Error:Aligner Occurred Error:Servo On Abnormal.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError71A", $"{Name} Error:Aligner Occurred Error:Brake circuit error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError71b", $"{Name} Error:Aligner Occurred Error:Brake circuit error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError71C", $"{Name} Error:Aligner Occurred Error:Power relay error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError721", $"{Name} Error:Aligner Occurred Error:Servo parameter error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError722", $"{Name} Error:Aligner Occurred Error:Servo parameter error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError725", $"{Name} Error:Aligner Occurred Error:Converter Overheat.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError726", $"{Name} Error:Aligner Occurred Error:Communication Error(internal controller) 2-4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError727", $"{Name} Error:Aligner Occurred Error:Command not supported 1-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError728", $"{Name} Error:Aligner Occurred Error:Communication Error(internal controller) 2-5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError729", $"{Name} Error:Aligner Occurred Error:Servo system error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError72A", $"{Name} Error:Aligner Occurred Error:Servo system error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError72b", $"{Name} Error:Aligner Occurred Error:Servo parameter error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError730", $"{Name} Error:Aligner Occurred Error:Amp module disconnected..", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError732", $"{Name} Error:Aligner Occurred Error:Servo parameter error 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError733", $"{Name} Error:Aligner Occurred Error:Servo parameter error 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError734", $"{Name} Error:Aligner Occurred Error:Servo parameter error 7.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError735", $"{Name} Error:Aligner Occurred Error:Servo parameter error 8.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError73F", $"{Name} Error:Aligner Occurred Error:Undefined Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError740", $"{Name} Error:Aligner Occurred Error:Encoder Status Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError741", $"{Name} Error:Aligner Occurred Error:Servo system error 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError742", $"{Name} Error:Aligner Occurred Error:Servo system error 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError743", $"{Name} Error:Aligner Occurred Error:Servo system error 7.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError744", $"{Name} Error:Aligner Occurred Error:Servo system error 8.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError745", $"{Name} Error:Aligner Occurred Error:Servo system error 9.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError746", $"{Name} Error:Aligner Occurred Error:Servo system error 10.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError74A", $"{Name} Error:Aligner Occurred Error:Servo system error 11.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError74b", $"{Name} Error:Aligner Occurred Error:Servo system error 12.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError74C", $"{Name} Error:Aligner Occurred Error:Servo system error 13.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError74d", $"{Name} Error:Aligner Occurred Error:Servo system error 14.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7A0", $"{Name} Error:Aligner Occurred Error:Communication Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7A1", $"{Name} Error:Aligner Occurred Error:Communication Error(internal controller) 3-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7A2", $"{Name} Error:Aligner Occurred Error:Command not supported 3-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7A3", $"{Name} Error:Aligner Occurred Error:Data buffer full.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7A4", $"{Name} Error:Aligner Occurred Error:Command not supported 3-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7A5", $"{Name} Error:Aligner Occurred Error:Encoder data error 3-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7A6", $"{Name} Error:Aligner Occurred Error:Command not supported 3-3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7AE", $"{Name} Error:Aligner Occurred Error:Communication Error(internal controller) 1-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7AF", $"{Name} Error:Aligner Occurred Error:Communication Error(internal controller) 1-3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7b0", $"{Name} Error:Aligner Occurred Error:CCD sensor abnormal 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7b4", $"{Name} Error:Aligner Occurred Error:CCD sensor abnormal 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7b5", $"{Name} Error:Aligner Occurred Error:CCD sensor abnormal 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7C0", $"{Name} Error:Aligner Occurred Error:PAIF board Failure 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7C1", $"{Name} Error:Aligner Occurred Error:PAIF board Failure 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7C2", $"{Name} Error:Aligner Occurred Error:PAIF board Failure 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7C3", $"{Name} Error:Aligner Occurred Error:CCD sensor abnormal 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7CF", $"{Name} Error:Aligner Occurred Error:PAIF board disconnected.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7d0", $"{Name} Error:Aligner Occurred Error:PAIF board Failure 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError7d1", $"{Name} Error:Aligner Occurred Error:PAIF board Failure 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError", $"{Name} Error:Aligner Occurred Error:.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError900", $"{Name} Error:Aligner Occurred Error:Character Interval Timeout.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError910", $"{Name} Error:Aligner Occurred Error:Received Data ChecksumError.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError920", $"{Name} Error:Aligner Occurred Error:Unit Number Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError930", $"{Name} Error:Aligner Occurred Error:Undefined CommandReceived.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError940", $"{Name} Error:Aligner Occurred Error:Message Parameter Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError950", $"{Name} Error:Aligner Occurred Error:Receiving Time-out Error for Confirmation of Execution Completion.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError960", $"{Name} Error:Aligner Occurred Error:Incorrect sequence number.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError961", $"{Name} Error:Aligner Occurred Error:Duplicated message.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError970", $"{Name} Error:Aligner Occurred Error:Delimiter error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError9A1", $"{Name} Error:Aligner Occurred Error:Message buffer overflow.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError9C0", $"{Name} Error:Aligner Occurred Error:LAN device setting error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError9C1", $"{Name} Error:Aligner Occurred Error:IP address error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError9C2", $"{Name} Error:Aligner Occurred Error:Subnet mask error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError9C3", $"{Name} Error:Aligner Occurred Error:Default gateway error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError9d0", $"{Name} Error:Aligner Occurred Error:Ethernet receive error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError9E0", $"{Name} Error:Aligner Occurred Error:During operation themaintenance tool.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerError9E1", $"{Name} Error:Aligner Occurred Error:The data abnormal.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorA01", $"{Name} Error:Aligner Occurred Error:Re-detection of a powerSupply voltage fall.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorA10", $"{Name} Error:Aligner Occurred Error:External emergency stop.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorA20", $"{Name} Error:Aligner Occurred Error:T.P emergency stop.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorA21", $"{Name} Error:Aligner Occurred Error:Interlock board failure 0.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorA30", $"{Name} Error:Aligner Occurred Error:Emergency stop.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorA40", $"{Name} Error:Aligner Occurred Error:Controller Fan 1 Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorA41", $"{Name} Error:Aligner Occurred Error:Controller Fan 2 Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorA42", $"{Name} Error:Aligner Occurred Error:Controller Fan 3 Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorA45", $"{Name} Error:Aligner Occurred Error:Unit fan 1 error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorA46", $"{Name} Error:Aligner Occurred Error:Unit fan 2 error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorA4F", $"{Name} Error:Aligner Occurred Error:Controller Battery Alarm.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAC0", $"{Name} Error:Aligner Occurred Error:Safety fence signal detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAC9", $"{Name} Error:Aligner Occurred Error:Protection stop signal.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAE0", $"{Name} Error:Aligner Occurred Error:HOST Mode Switching error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAE1", $"{Name} Error:Aligner Occurred Error:TEACH Mode Switching Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAE8", $"{Name} Error:Aligner Occurred Error:Deadman switch error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAF0", $"{Name} Error:Aligner Occurred Error:Interlock board failure 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAF1", $"{Name} Error:Aligner Occurred Error:Interlock board failure 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAF2", $"{Name} Error:Aligner Occurred Error:Interlock board failure 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAF3", $"{Name} Error:Aligner Occurred Error:Interlock board failure 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAF4", $"{Name} Error:Aligner Occurred Error:Interlock board failure 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAF5", $"{Name} Error:Aligner Occurred Error:Interlock board failure 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAF6", $"{Name} Error:Aligner Occurred Error:Interlock board failure 7.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAF8", $"{Name} Error:Aligner Occurred Error:Input compare error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAF9", $"{Name} Error:Aligner Occurred Error:Input compare error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAFA", $"{Name} Error:Aligner Occurred Error:Input compare error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAFb", $"{Name} Error:Aligner Occurred Error:Input compare error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAFC", $"{Name} Error:Aligner Occurred Error:Input compare error 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAFd", $"{Name} Error:Aligner Occurred Error:Input compare error 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAFE", $"{Name} Error:Aligner Occurred Error:Input compare error 7.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorAFF", $"{Name} Error:Aligner Occurred Error:Input compare error 8.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb10", $"{Name} Error:Aligner Occurred Error:Axis-1 Speed Limit Detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb11", $"{Name} Error:Aligner Occurred Error:Axis-2 Speed Limit Detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb12", $"{Name} Error:Aligner Occurred Error:Axis-3 Speed Limit Detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb13", $"{Name} Error:Aligner Occurred Error:Axis-4 Speed Limit Detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb14", $"{Name} Error:Aligner Occurred Error:Axis-5 Speed Limit Detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb20", $"{Name} Error:Aligner Occurred Error:Axis-1 Positive (+) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb21", $"{Name} Error:Aligner Occurred Error:Axis-2 Positive (+) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb22", $"{Name} Error:Aligner Occurred Error:Axis-3 Positive (+) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb23", $"{Name} Error:Aligner Occurred Error:Axis-4 Positive (+) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb24", $"{Name} Error:Aligner Occurred Error:Axis-5 Positive (+) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb28", $"{Name} Error:Aligner Occurred Error:Axis-1 Positive (+) Direction Software-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb29", $"{Name} Error:Aligner Occurred Error:Axis-2 Positive (+) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb2A", $"{Name} Error:Aligner Occurred Error:Axis-3 Positive (+) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb2b", $"{Name} Error:Aligner Occurred Error:Axis-4 Positive (+) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb2C", $"{Name} Error:Aligner Occurred Error:Axis-5 Positive (+) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb30", $"{Name} Error:Aligner Occurred Error:Axis-1 Negative (-) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb31", $"{Name} Error:Aligner Occurred Error:Axis-2 Negative (-) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb32", $"{Name} Error:Aligner Occurred Error:Axis-3 Negative (-) Direction Software-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb33", $"{Name} Error:Aligner Occurred Error:Axis-4 Negative (-) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb34", $"{Name} Error:Aligner Occurred Error:Axis-5 Negative (-) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb38", $"{Name} Error:Aligner Occurred Error:Axis-1 Negative (-) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb39", $"{Name} Error:Aligner Occurred Error:Axis-2 Negative (-) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb3A", $"{Name} Error:Aligner Occurred Error:Axis-3 Negative (-) Direction Software-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb3b", $"{Name} Error:Aligner Occurred Error:Axis-4 Negative (-) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb3C", $"{Name} Error:Aligner Occurred Error:Axis-5 Negative (-) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb40", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 1Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb41", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 2Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb42", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 3Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb43", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 4Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb44", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 5 Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb45", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 6Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb46", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 7Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb47", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 8Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb48", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 9Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb49", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 10 Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb4A", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 11Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb4b", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 12Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb4C", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 13Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb4d", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 14Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb4E", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 15Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb4F", $"{Name} Error:Aligner Occurred Error:Access Permission Signal 16Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb60", $"{Name} Error:Aligner Occurred Error:Access Permission to P/A Stage Time-out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb61", $"{Name} Error:Aligner Occurred Error:Access Permission to P/AStage Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb62", $"{Name} Error:Aligner Occurred Error:Access Permission to P/A Stage Time-out Error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb63", $"{Name} Error:Aligner Occurred Error:Access Permission to P/A Stage Time-out Error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb64", $"{Name} Error:Aligner Occurred Error:Access Permission to P/AStage Time-out Error 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb65", $"{Name} Error:Aligner Occurred Error:Access Permission to P/AStage Time-out Error 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb66", $"{Name} Error:Aligner Occurred Error:Access Permission to P/AStage Time-out Error 7.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb68", $"{Name} Error:Aligner Occurred Error:P/A motion permission timeout error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb70", $"{Name} Error:Aligner Occurred Error:SS signal detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb80", $"{Name} Error:Aligner Occurred Error:Fork 1/Pre-aligner: Wafer Presence Confirmation Time-out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb81", $"{Name} Error:Aligner Occurred Error:Fork 1/Pre-aligner: WaferAbsence Confirmation Time- out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb82", $"{Name} Error:Aligner Occurred Error:Fork 1/Pre-aligner: Wafer Presence Confirmation Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb83", $"{Name} Error:Aligner Occurred Error:Fork 1/Pre-aligner: WaferAbsence Confirmation Time- out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb88", $"{Name} Error:Aligner Occurred Error:Grip sensor Time-out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb89", $"{Name} Error:Aligner Occurred Error:Grip sensor Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb8A", $"{Name} Error:Aligner Occurred Error:UnGrip sensor Time-out Error1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb8b", $"{Name} Error:Aligner Occurred Error:UnGrip sensor Time-out Error2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb8F", $"{Name} Error:Aligner Occurred Error:Fork 1: Plunger non-operationerror.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb90", $"{Name} Error:Aligner Occurred Error:Fork 2: Wafer Presence Confirmation Time-out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb91", $"{Name} Error:Aligner Occurred Error:Fork 2: Wafer AbsenceConfirmation Time-out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb92", $"{Name} Error:Aligner Occurred Error:Fork 2: Wafer PresenceConfirmation Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb93", $"{Name} Error:Aligner Occurred Error:Fork 2: Wafer AbsenceConfirmation Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb98", $"{Name} Error:Aligner Occurred Error:Lifter up sensor Time-outError 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb99", $"{Name} Error:Aligner Occurred Error:Lifter up sensor Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb9A", $"{Name} Error:Aligner Occurred Error:Lifter down sensor Time-outError 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb9b", $"{Name} Error:Aligner Occurred Error:Lifter down sensor Time-outError 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorb9F", $"{Name} Error:Aligner Occurred Error:Fork 2: Plunger non-operationerror.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorbA0", $"{Name} Error:Aligner Occurred Error:Fork 1/Pre-aligner: WaferAbsence Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorbA1", $"{Name} Error:Aligner Occurred Error:Fork 1: Sensor StatusMismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorbA8", $"{Name} Error:Aligner Occurred Error:Grip sensor status Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorbA9", $"{Name} Error:Aligner Occurred Error:Grip sensor status Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorbAA", $"{Name} Error:Aligner Occurred Error:Ungrip sensor status Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorbAb", $"{Name} Error:Aligner Occurred Error:Ungrip sensor status Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorbAC", $"{Name} Error:Aligner Occurred Error:Grip sensor status mismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorbAd", $"{Name} Error:Aligner Occurred Error:Lifter/Grip sensor statusmismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorbb0", $"{Name} Error:Aligner Occurred Error:Fork 2: Wafer Absence Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorbb1", $"{Name} Error:Aligner Occurred Error:Fork 2: Sensor StatusMismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", "AlignerErrorbb8", $"{Name} Error:Aligner Occurred Error:Lifter up sensor status Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));

        }

        public void CheckWaferPresentAndGrip()
        {
            if(IsWaferPresent(0))
            {
                if (WaferManager.Instance.CheckNoWafer(RobotModuleName, 0))
                {
                    WaferManager.Instance.CreateWafer(RobotModuleName, 0, WaferStatus.Normal);
                    EV.PostWarningLog($"{RobotModuleName}", $"System detec wafer on aligner, will create wafer automatically.");

                }
            }
            else
            {
                _lstHandlers.AddLast(new YaskawaNxcAlignerMotionHandler(this, "CSOL", "1,0,0"));
                if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                {                    
                    EV.PostWarningLog($"{RobotModuleName}", $"System didn't detec wafer on aligner, but it has record.");

                }
            }
        }

        private void _diTPinUse_OnSignalChanged(IoSensor arg1, bool arg2)
        {
            if (!arg2)
                SetMaintenanceMode(true);
            else 
                SetMaintenanceMode(false);
        }

        private void _diPreAlignerError_OnSignalChanged(IoSensor arg1, bool arg2)
        {
            if(!arg2)
                OnError("Aligner error signal");
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
            DATA.Subscribe($"{Module}.{Name}.IsWaferPresenceOnFromVacuumSensorOrGripSensor", () => IsWaferPresenceOnFromVacuumSensorOrGripSensor);
            DATA.Subscribe($"{Module}.{Name}.IsWaferPresenceOnFromCCDSensor", () => IsWaferPresenceOnFromCCDSensor);

            DATA.Subscribe($"{Module}.{Name}.ErrorCode", () => ErrorCode);
            DATA.Subscribe($"{Module}.{Name}.IsWaferHoldOnChuck", () => IsWaferHoldOnChuck);
            //DATA.Subscribe($"{Module}.{Name}.IsGrippedBlade2", () => IsGrippedBlade2);
            DATA.Subscribe($"{Module}.{Name}.IsCheckInterlockWaferPresenceAbsent", () => IsCheckInterlockWaferPresenceAbsent);
            DATA.Subscribe($"{Module}.{Name}.IsCheckInterlockManipulatorOperation", () => IsCheckInterlockManipulatorOperation);
           
        }

        private void ResetPropertiesAndResponses()
        {
            
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

            if (IsEnableCheckSum)
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
        public int UnitNumber
        {
            get; private set;
        }
        private bool isSimulatorMode;
        private string _scRoot;
        //private string _ipaddress;
        public YaskawaTokenGenerator SeqnoGenerator { get; private set; }
        public bool IsEnableSeqNo { get; private set; }
        public bool IsEnableCheckSum { get; private set; }
        public string AlignerSystemVersion { get; private set; }
        public string AlignerSoftwareVersion { get; private set; }
        public int CurrentSeqNo { get; set; }
        public int AlignerType { get; private set; }     //0= Edge grip, 1=Vacuum

        public string PortName;
        private string _address;
        private bool _enableLog;
        private YaskawaNxcAlignerConnection _connection;
        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        public string Address => _address;
        private PeriodicJob _thread;
        private object _locker = new object();
        private LinkedList<HandlerBase> _lstHandlers = new LinkedList<HandlerBase>();

        private IoSensor _diPreAlignerWaferOn = null;
        private IoSensor _diPreAlignerReady = null;
        private IoSensor _diPreAlignerError = null;
        private IoSensor _diTPinUse = null;

        private IoTrigger _doPreAlignerHold = null;
        public ModuleName CurrentInteractiveModule { get; private set; }
        public bool IsConnected => _connection.IsConnected;

        public bool IsWaferPresenceOnFromVacuumSensorOrGripSensor { get; private set; }
        public bool IsWaferPresenceOnFromCCDSensor { get; private set; }
        public bool IsWaferHoldOnChuck { get; private set; }
        //public bool IsGrippedBlade2 { get; private set; }
        public bool IsGripSensorOnEnd { get; private set; }
        public bool IsUngripSensorOnEnd { get; private set; }
        public bool IsLifterUpperSensorOnEnd { get; private set; }
        public bool IsLifterDownSensorOnEnd { get; private set; }
        public bool IsLifterSolenoidOnUp { get; private set; }
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
        public string ReadMemorySpec { get; private set; }
        public string ReadTransferStation { get; private set; }
        public int ReadSlotNumber { get; private set; }
        public string ReadArmPosture { get; private set; }
        
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

        public bool IsCheckInterlockWaferPresenceAbsent { get; private set; }
        //public bool IsCheckInterlockWaferPresenceOnBlade2 { get; private set; }

        //public bool IsCheckInterlockPAOp { get; private set; }
        //public bool IsCheckInterlockPAWaferStatus { get; private set; }
        public bool IsCheckInterlockManipulatorOperation { get; private set; }

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
            if (_diPreAlignerReady != null && !_diPreAlignerReady.Value)
                return false;
            if (_diPreAlignerError != null && !_diPreAlignerError.Value)
                return false;
            if (_diTPinUse != null && !_diTPinUse.Value)
                return false;
            return IsIdle;
        }

        protected override bool fStartLiftup(object[] param)
        {
            lock (_locker)
            {                
                string strpara =  "2,1,0";
                _lstHandlers.AddLast(new YaskawaNxcAlignerMotionHandler(this, "CSOL", strpara));
            }
            return true;
        }

        protected override bool fStartLiftdown(object[] param)
        {
            lock (_locker)
            {
                string strpara = "2,0,0";
                _lstHandlers.AddLast(new YaskawaNxcAlignerMotionHandler(this, "CSOL", strpara));
            }
            return true;
        }

        protected override bool fStartAlign(object[] param)
        {
            double aligneangle = (double)param[0];
            int intangle = (int)(aligneangle * 100);
            CurrentNotch = aligneangle;
            lock (_locker)
            {
                string strpara = $"1{intangle:D6}";
                _lstHandlers.AddLast(new YaskawaNxcAlignerMotionHandler(this, "CSOL", "A1"));
                _lstHandlers.AddLast(new YaskawaNxcAlignerMotionHandler(this, "MALN", strpara));
                _lstHandlers.AddLast(new YaskawaNxcAlignerMotionHandler(this, "CSOL", "A0"));
                
            }
            return true;
        }

        protected override bool fStop(object[] param)
        {
            lock (_locker)
            {
                _lstHandlers.Clear();
                _connection.ForceClear();
                //ExecuteHandler(new YaskawaAlignerMotionHandler(this, "CSTP", "E"));
            }
            return true;
        }

        protected override bool FsmAbort(object[] param)
        {
            lock (_locker)
            {
                _lstHandlers.Clear();
                _connection.ForceClear();
                ExecuteHandler(new YaskawaNxcAlignerMotionHandler(this, "CSTP", "H"));
            }
            return true;
        }

        protected override bool fClear(object[] param)
        {
            lock (_locker)
            {
                ExecuteHandler(new YaskawaNxcAlignerMotionHandler(this, "CCLR", "E"));
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
                        _lstHandlers.AddLast(new YaskawaNxcAlignerReadHandler(this, "RSTS"));
                    }
                    break;
                default:
                    break;
            }
            return true;
        }
        private string _currentSetCmd;

        protected override bool fStartSetParameters(object[] param)
        {
            try
            {
                string strParameter;
                string setcommand = param[0].ToString();
                _currentSetCmd = setcommand;
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
                        if (!"HMLOB".Contains(strspeedtype))
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
                            _lstHandlers.AddLast(new YaskawaNxcAlignerSetHandler(this, "SSPD", strParameter));
                        }
                        break;
                    case "TransferSpeedLevel":    //SSLV Select the transfer speed level
                        string sslvlevel = param[1].ToString();
                        if (!"123".Contains(sslvlevel))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + sslvlevel);
                            return false;
                        }
                        //string strSpeedLevel = "M";
                        //switch (sslvlevel)
                        //{
                        //    case "1":
                        //        strSpeedLevel = "H";
                        //        break;
                        //    case "2":
                        //        strSpeedLevel = "M";
                        //        break;
                        //    case "3":
                        //        strSpeedLevel = "L";
                        //        break;
                        //    default:
                        //        strSpeedLevel = "H";
                        //        break;
                        //}
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new YaskawaNxcAlignerSetHandler(this, "SSLV", sslvlevel));
                        }
                        break;
                    case "RegisterTheCurrentPositionAsTransferStation":   // SPOS: Register the current position as the specified transfer station
                        string sposMem = param[1].ToString();
                        string sposRmode = param[2].ToString();
                        string sposTrsSt = param[3].ToString();
                        uint sposSlot = Convert.ToUInt16(param[4]);
                        string sposPosture = param[5].ToString();
                        string sposHand = param[6].ToString();
                        if (!"VN".Contains(sposMem))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + sposMem);
                            return false;
                        }
                        if (!"AN".Contains(sposRmode))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + sposRmode);
                            return false;
                        }
                        if (sposSlot < 1 || sposSlot > 30)
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
                            _lstHandlers.AddLast(new YaskawaNxcAlignerSetHandler(this, "SPOS", strParameter));
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
                            _lstHandlers.AddLast(new YaskawaNxcAlignerSetHandler(this, "SABS", strParameter));
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
                            _lstHandlers.AddLast(new YaskawaNxcAlignerSetHandler(this, "SAPS", strParameter));
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
                            _lstHandlers.AddLast(new YaskawaNxcAlignerSetHandler(this, "SPDL", strParameter));
                        }
                        break;
                    case "RegisterThePositionDataToVolatile": //SPSV
                        string spsvTrsSt = param[1].ToString();
                        string spsvPosture = param[2].ToString();
                        string spsvHand = param[3].ToString();
                        strParameter = $"{spsvTrsSt},{spsvPosture},{spsvHand}";
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new YaskawaNxcAlignerSetHandler(this, "SPSV", strParameter));
                        }



                        break;
                    case "ReadThePostionDataFromVolatile": //SPLD
                        string spldTrsSt = param[1].ToString();
                        string spldPosture = param[2].ToString();
                        string spldHand = param[3].ToString();
                        strParameter = $"{spldTrsSt},{spldPosture},{spldHand}";
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new YaskawaNxcAlignerSetHandler(this, "SPLD", strParameter));
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
                            _lstHandlers.AddLast(new YaskawaNxcAlignerSetHandler(this, "SSTR", strParameter));
                        }
                        break;
                    case "ChangeParameterValue": // SPRM
                        string sprmParaType = param[1].ToString();
                        int sprmParaNO = Convert.ToInt32(param[2].ToString());
                        Int32 sprmValue = Convert.ToInt32(param[3].ToString());
                        strParameter = sprmParaType + "," + sprmParaNO.ToString("D4") + "," + sprmValue.ToString("D12");
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new YaskawaNxcAlignerSetHandler(this, "SPRM", strParameter));
                        }
                        break;
                    case "EnableInterLock": //SMSK
                        int smskValid = Convert.ToInt16(param[1].ToString());
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new YaskawaNxcAlignerSetHandler(this, "SPRM", smskValid.ToString("D4")));
                        }
                        break;
                    case "RegisterTheCurrentPositionAsCoordinate": //SSTD
                        string sstdAxis = param[1].ToString();
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new YaskawaNxcAlignerSetHandler(this, "SSTD", sstdAxis));
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
                            _lstHandlers.AddLast(new YaskawaNxcAlignerSetHandler(this, "SSTN", strParameter));
                        }
                        break;
                    case "WaferSize":
                        var wafer = WaferManager.Instance.GetWafer(RobotModuleName, 0);
                        if(!wafer.IsEmpty)
                        {
                            WaferSize size = (WaferSize)Enum.Parse(typeof(WaferSize), param[1].ToString());
                            wafer.Size = size;
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
            if (_currentSetCmd == "WaferSize") return true;
            return base.fMonitorSetParamter(param);
        }
        protected override bool fStartUnGrip(object[] param)
        {
            lock (_locker)
            {
                string strpara = "A0";
                _lstHandlers.AddLast(new YaskawaNxcAlignerMotionHandler(this, "CSOL", strpara));
            }
            return true;
        }

        protected override bool fStartGrip(object[] param)
        {
            lock (_locker)
            {
                string strpara = "A1";
                _lstHandlers.AddLast(new YaskawaNxcAlignerMotionHandler(this, "CSOL", strpara));
            }
            return true;
        }
       

        protected override bool fResetToReady(object[] param)
        {
            if(!_connection.IsConnected)
            {
                _connection.Connect();
            }
            return true;
        }

        protected override bool fReset(object[] param)
        {
            _lstHandlers.Clear();
            _connection.ForceClear();
            if (!_connection.IsConnected || _connection.Address != SC.GetStringValue($"{_scRoot}.{Name}.Address"))
            {
                _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
                _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
                UnitNumber = SC.GetValue<int>($"{_scRoot}.{Name}.UnitNumber");
                IsEnableCheckSum = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableCheckSum");
                IsEnableSeqNo = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableSeqNo");
                _connection = new YaskawaNxcAlignerConnection(this, _address);
                _connection.EnableLog(_enableLog);
                _connection.Connect();                
            }

            lock(_locker)
            {
                _lstHandlers.AddLast(new YaskawaNxcAlignerMotionHandler(this, "CCLR", "E"));
                _lstHandlers.AddLast(new YaskawaNxcAlignerReadHandler(this, "RSTS"));
            }

            return true;
        }
        protected override bool fMonitorReset(object[] param)
        {
            if (!_connection.IsBusy && _lstHandlers.Count ==0)
            {
                return true;
            }
            return false;
        }

        public override bool IsNeedRelease
        {
            //get
            //{
            //if (AlignerType == 1)
            //    return true;
            //if (SC.ContainsItem($"{_scRoot}.{Name}.NeedReleaseBeforePick") && SC.GetValue<bool>($"{_scRoot}.{Name}.NeedReleaseBeforePick"))
            //    return true;
            get { return false; }
            //}
        }

        protected override bool fStartInit(object[] param)
        {
            CurrentNotch = 0;
            lock (_locker)
            {
                string strpara = "00";               
                _lstHandlers.AddLast(new YaskawaNxcAlignerMotionHandler(this, "INIT", strpara));
                //_lstHandlers.AddLast(new YaskawaNxcAlignerGripAndDetectHandler(this));


            }
            return true;
        }

        protected override bool fError(object[] param)
        {
            return true;
        }

        public bool ParseStatus(string status)
        {
            try
            {
                //int intstatus = Convert.ToInt32(status,16);
                int intstatus = Convert.ToInt32(status, 16);
                IsManipulatorBatteryLow = ((intstatus & 0x10) == 0x10);
                IsCommandExecutionReady = ((intstatus & 0x20) == 0x20);
                IsServoON = ((intstatus & 0x40) == 0x40);
                IsErrorOccurred = ((intstatus & 0x80) == 0x80);
                IsControllerBatteryLow = ((intstatus & 0x1) == 0x1);
                //IsWaferPresenceOnFromVacuumSensorOrGripSensor = ((intstatus & 0x2) == 0x2);
                //IsWaferPresenceOnFromCCDSensor = ((intstatus & 0x4) == 0x4);
                return true;

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        public bool ParseReadData(string _command, string rdata)
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
               
                if (_command == "RSTR")   //Reference station item value
                {
                    return (ParseStationData(rdata));
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
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        public bool ParseSpeedLevel(string data)
        {
            try
            {
                string speedtag = data.Substring(6, 1);

                switch (speedtag)
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

                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }


        public bool ParseStationData(string pdata)
        {
            try
            {
                string StsN = pdata.Substring(0, 2);
                string Ackcd = pdata.Substring(2, 4);
                ParseStatus(StsN);

                //ReadMemorySpec = pdata[0];
                //ReadTransferStation = pdata[1];
                //if (ReadStationItemValues.ContainsKey(pdata[2]))
                //    ReadStationItemValues.Remove(pdata[2]);
                //ReadStationItemValues.Add(pdata[2], pdata[3]);
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
                ReadParameterType = pdata.Substring(6, 3);
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
                int intdata = Convert.ToInt16(pdata.Substring(6, 4));
                IsCheckInterlockWaferPresenceAbsent = (intdata & 0x1) == 0;
                IsCheckInterlockManipulatorOperation = (intdata & 0x1000) == 1000;
              
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
                AlignerSystemVersion = pdata.Substring(6, 4);
                AlignerSoftwareVersion = pdata.Substring(6, 4);
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

                ErrorCode = status.Substring(6, 4);
                int intstatus = Convert.ToInt32(status.Substring(10, 4), 16);
                IsWaferPresenceOnFromVacuumSensorOrGripSensor = ((intstatus & 0x1000) != 0x1000);
                IsWaferPresenceOnFromCCDSensor = ((intstatus & 0x2000) != 0x2000);
                
                IsGripSensorOnEnd = ((intstatus & 0x0100) != 0x0100);
                IsUngripSensorOnEnd = ((intstatus & 0x0200) != 0x0200);
                return true;

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        public void SenACK()
        {
            _connection.SendAck();
        }

        public override bool OnActionDone(object[] param)
        {
            if (_lstHandlers.Count == 0)
            {
                IsBusy = false;
                return base.OnActionDone(param);
            }
            return true;
        }

        public override bool IsWaferPresent(int slotindex)
        {
            if (_diPreAlignerWaferOn != null)
                return !_diPreAlignerWaferOn.Value;
            return IsWaferPresenceOnFromCCDSensor || IsWaferPresenceOnFromVacuumSensorOrGripSensor;
        }

        public override void Terminate()
        {
            _thread.Stop();
            if(!SC.ContainsItem($"{_scRoot}.{Name}.CloseConnectionOnShutDown") || SC.GetValue<bool>($"{_scRoot}.{Name}.CloseConnectionOnShutDown"))
            {
                LOG.Write($"Close connection for {RobotModuleName}");
                _connection.Disconnect();
            }
            base.Terminate();
        }
        public void NotifyAlarmByErrorCode(string errorcode)
        {
            EV.Notify($"{Name}Error{errorcode}");
        }
    }


}
