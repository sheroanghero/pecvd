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
    public class YaskawaAligner:AlignerBaseDevice,IConnection
    {
        public YaskawaAligner(string module,string name,string scRoot,IoSensor[] dis,IoTrigger[] dos, int alignerType = 0) :base(module,name)
        {
            isSimulatorMode = SC.ContainsItem("System.IsSimulatorMode") ? SC.GetValue<bool>("System.IsSimulatorMode") : false;
            _scRoot = scRoot;
            _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            UnitNumber = SC.GetValue<int>($"{_scRoot}.{Name}.UnitNumber");
            IsEnableCheckSum = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableCheckSum");
            IsEnableSeqNo = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableSeqNo");
            AlignerType = alignerType;   //1=Vacuum 0=Mechnical
            _connection = new YaskawaAlignerConnection(this,_address);
            _connection.EnableLog(_enableLog);
            SeqnoGenerator = new YaskawaTokenGenerator($"{_scRoot}.{Name}.CommunicationToken");

            if(dis!= null && dis.Length >=1)
                _diPreAlignerWaferOn = dis[0];
            if (dis != null && dis.Length >= 2)
                _diPreAlignerReady = dis[1];
            if (dis != null && dis.Length >= 3)
            {
                _diPreAlignerError = dis[2];
                if(_diPreAlignerError !=null)
                    _diPreAlignerError.OnSignalChanged += _diPreAlignerError_OnSignalChanged;
            }
            if (dis != null && dis.Length >= 4)
            {
                _diTPinUse = dis[3];
                if(_diTPinUse !=null)
                    _diTPinUse.OnSignalChanged += _diTPinUse_OnSignalChanged;
            }
            if (dos != null && dos.Length >= 1)
            {
                _doPreAlignerHold = dos[0];
                if(_doPreAlignerHold!=null)
                    _doPreAlignerHold.SetTrigger(true, out _);
            }
            ConnectionManager.Instance.Subscribe($"{Name}", this);
            _thread = new PeriodicJob(100, OnTimer, $"{_scRoot}.{Name} MonitorHandler", true);
            ResetPropertiesAndResponses();
            RegisterSpecialData();
            RegisterAlarm();

        }

        private void RegisterAlarm()
        {
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error020", $"{Name} Aligner Occurred Error:Secondary power off.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error021", $"{Name} Aligner Occurred Error:Secondary power on.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error040", $"{Name} Aligner Occurred Error:In TEACH Mode.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error050", $"{Name} Aligner Occurred Error:Unit is in motion.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error051", $"{Name} Aligner Occurred Error:Unable to set pitch between slots.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error052", $"{Name} Aligner Occurred Error:Unable to restart motion.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error053", $"{Name} Aligner Occurred Error:Ready position moveincomplete.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error054", $"{Name} Aligner Occurred Error:Alignment Ready position move incomplete.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error055", $"{Name} Aligner Occurred Error:Improper station type.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error058", $"{Name} Aligner Occurred Error:Command not supported 1-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error059", $"{Name} Aligner Occurred Error:Invalid transfer point.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error05A", $"{Name} Aligner Occurred Error:Linear motion failed.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error05C", $"{Name} Aligner Occurred Error:Unable to reference waferalignment result.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error05D", $"{Name} Aligner Occurred Error:Unable to perform armcalibration.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error05E", $"{Name} Aligner Occurred Error:Unable to read mapping data.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error05F", $"{Name} Aligner Occurred Error:Data Upload/Download inprogress.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error061", $"{Name} Aligner Occurred Error:Unable to motion.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error064", $"{Name} Aligner Occurred Error:Lifter interference error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error070", $"{Name} Aligner Occurred Error:Bottom slot position recordincomplete.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error071", $"{Name} Aligner Occurred Error:Top slot position record incomplete.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error088", $"{Name} Aligner Occurred Error:Position generating error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error089", $"{Name} Aligner Occurred Error:Position generating error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error08A", $"{Name} Aligner Occurred Error:Position generating error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error08B", $"{Name} Aligner Occurred Error:Position generating error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error08C", $"{Name} Aligner Occurred Error:Position generating error 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error08D", $"{Name} Aligner Occurred Error:Position generating error 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error090", $"{Name} Aligner Occurred Error:Host parameter out of range.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error0A0", $"{Name} Aligner Occurred Error:Alignment motion error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error0E0", $"{Name} Aligner Occurred Error:Teach position adjustmentoffset amount limit error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error0F0", $"{Name} Aligner Occurred Error:Voltage drop warning.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*06", $"{Name} Aligner Occurred Error:Amplifier Type Mismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*07", $"{Name} Aligner Occurred Error:Encoder Type Mismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*10", $"{Name} Aligner Occurred Error:Overflow Current.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*30", $"{Name} Aligner Occurred Error:Regeneration Error Detected.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*40", $"{Name} Aligner Occurred Error:Excess Voltage (converter).", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*41", $"{Name} Aligner Occurred Error:Insufficient Voltage.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*45", $"{Name} Aligner Occurred Error:Brake circuit error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*46", $"{Name} Aligner Occurred Error:Converter ready signal error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*47", $"{Name} Aligner Occurred Error:Input power error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*48", $"{Name} Aligner Occurred Error:Converter main circuit chargeerror.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*49", $"{Name} Aligner Occurred Error:Amplifier ready signal error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*51", $"{Name} Aligner Occurred Error:Excessive Speed.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*71", $"{Name} Aligner Occurred Error:Momentary Overload (Motor).", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*72", $"{Name} Aligner Occurred Error:Continuous Overload (Motor).", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*78", $"{Name} Aligner Occurred Error:Overload (Converter).", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*7B", $"{Name} Aligner Occurred Error:Amplifier overheat.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*7C", $"{Name} Aligner Occurred Error:Continuous Overload(Amplifier).", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*7D", $"{Name} Aligner Occurred Error:Momentary Overload.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*81", $"{Name} Aligner Occurred Error:Absolute Encoder Back-upError.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*83", $"{Name} Aligner Occurred Error:Absolute Encoder Battery.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*84", $"{Name} Aligner Occurred Error:Encoder Data Error 2-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*85", $"{Name} Aligner Occurred Error:Encoder Excessive Speed.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*86", $"{Name} Aligner Occurred Error:Encoder Overheat.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*88", $"{Name} Aligner Occurred Error:Encoder error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*89", $"{Name} Aligner Occurred Error:Encoder Command failed.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*8A", $"{Name} Aligner Occurred Error:Encoder multi-turn range.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*8C", $"{Name} Aligner Occurred Error:Encoder Reset not completed.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*98", $"{Name} Aligner Occurred Error:Servo parameter error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*9A", $"{Name} Aligner Occurred Error:Feedback Over Flow.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*B4", $"{Name} Aligner Occurred Error:Servo Control Board Failure.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*BC", $"{Name} Aligner Occurred Error:Encoder error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*C1", $"{Name} Aligner Occurred Error:Motor runaway detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*C9", $"{Name} Aligner Occurred Error:Encoder Communication.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*CE", $"{Name} Aligner Occurred Error:Encoder error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*CF", $"{Name} Aligner Occurred Error:Encoder error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*D0", $"{Name} Aligner Occurred Error:Position deviation error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*D1", $"{Name} Aligner Occurred Error:Position deviation saturation.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*D2", $"{Name} Aligner Occurred Error:Motor directive position error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*D4", $"{Name} Aligner Occurred Error:Servo Tracking Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error*F1", $"{Name} Aligner Occurred Error:Phase loss.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorE*1", $"{Name} Aligner Occurred Error:Positioning Timeout.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorE*D", $"{Name} Aligner Occurred Error:Command not supported 1-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorE*E", $"{Name} Aligner Occurred Error:Communication Error(internal controller) 1-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorE*F", $"{Name} Aligner Occurred Error:Servo control board responsetimeout 1..", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error701", $"{Name} Aligner Occurred Error:ROM Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error703", $"{Name} Aligner Occurred Error:Communication Error(internal controller) 2-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error704", $"{Name} Aligner Occurred Error:Communication Error (internal controller) 2-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error705", $"{Name} Aligner Occurred Error:Communication Error(internal controller) 2-3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error706", $"{Name} Aligner Occurred Error:Servo system error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error707", $"{Name} Aligner Occurred Error:Servo system error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error709", $"{Name} Aligner Occurred Error:Current feedback error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error70A", $"{Name} Aligner Occurred Error:Power Lost.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error70B", $"{Name} Aligner Occurred Error:Rush Current PreventionRelay Abnormal.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error70C", $"{Name} Aligner Occurred Error:Converter mismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error70F", $"{Name} Aligner Occurred Error:Servo control board response timeout 2..", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error713", $"{Name} Aligner Occurred Error:DB error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error714", $"{Name} Aligner Occurred Error:Converter charge Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error715", $"{Name} Aligner Occurred Error:Servo OFF Status Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error716", $"{Name} Aligner Occurred Error:Servo ON Status Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error717", $"{Name} Aligner Occurred Error:Servo OFF Status Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error718", $"{Name} Aligner Occurred Error:Servo ON Status Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error719", $"{Name} Aligner Occurred Error:Servo On Abnormal.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error71A", $"{Name} Aligner Occurred Error:Brake circuit error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error71B", $"{Name} Aligner Occurred Error:Brake circuit error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error71C", $"{Name} Aligner Occurred Error:Power relay error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error721", $"{Name} Aligner Occurred Error:Servo parameter error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error722", $"{Name} Aligner Occurred Error:Servo parameter error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error725", $"{Name} Aligner Occurred Error:Converter Overheat.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error726", $"{Name} Aligner Occurred Error:Communication Error(internal controller) 2-4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error727", $"{Name} Aligner Occurred Error:Command not supported 1-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error728", $"{Name} Aligner Occurred Error:Communication Error(internal controller) 2-5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error729", $"{Name} Aligner Occurred Error:Servo system error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error72A", $"{Name} Aligner Occurred Error:Servo system error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error72B", $"{Name} Aligner Occurred Error:Servo parameter error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error730", $"{Name} Aligner Occurred Error:Amp module disconnected..", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error732", $"{Name} Aligner Occurred Error:Servo parameter error 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error733", $"{Name} Aligner Occurred Error:Servo parameter error 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error734", $"{Name} Aligner Occurred Error:Servo parameter error 7.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error735", $"{Name} Aligner Occurred Error:Servo parameter error 8.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error73F", $"{Name} Aligner Occurred Error:Undefined Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error740", $"{Name} Aligner Occurred Error:Encoder Status Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error741", $"{Name} Aligner Occurred Error:Servo system error 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error742", $"{Name} Aligner Occurred Error:Servo system error 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error743", $"{Name} Aligner Occurred Error:Servo system error 7.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error744", $"{Name} Aligner Occurred Error:Servo system error 8.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error745", $"{Name} Aligner Occurred Error:Servo system error 9.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error746", $"{Name} Aligner Occurred Error:Servo system error 10.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error74A", $"{Name} Aligner Occurred Error:Servo system error 11.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error74B", $"{Name} Aligner Occurred Error:Servo system error 12.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error74C", $"{Name} Aligner Occurred Error:Servo system error 13.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error74D", $"{Name} Aligner Occurred Error:Servo system error 14.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7A0", $"{Name} Aligner Occurred Error:Communication Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7A1", $"{Name} Aligner Occurred Error:Communication Error(internal controller) 3-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7A2", $"{Name} Aligner Occurred Error:Command not supported 3-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7A3", $"{Name} Aligner Occurred Error:Data buffer full.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7A4", $"{Name} Aligner Occurred Error:Command not supported 3-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7A5", $"{Name} Aligner Occurred Error:Encoder data error 3-1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7A6", $"{Name} Aligner Occurred Error:Command not supported 3-3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7AE", $"{Name} Aligner Occurred Error:Communication Error(internal controller) 1-2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7AF", $"{Name} Aligner Occurred Error:Communication Error(internal controller) 1-3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7B0", $"{Name} Aligner Occurred Error:CCD sensor abnormal 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7B4", $"{Name} Aligner Occurred Error:CCD sensor abnormal 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7B5", $"{Name} Aligner Occurred Error:CCD sensor abnormal 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7C0", $"{Name} Aligner Occurred Error:PAIF board Failure 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7C1", $"{Name} Aligner Occurred Error:PAIF board Failure 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7C2", $"{Name} Aligner Occurred Error:PAIF board Failure 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7C3", $"{Name} Aligner Occurred Error:CCD sensor abnormal 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7CF", $"{Name} Aligner Occurred Error:PAIF board disconnected.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7D0", $"{Name} Aligner Occurred Error:PAIF board Failure 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error7D1", $"{Name} Aligner Occurred Error:PAIF board Failure 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error900", $"{Name} Aligner Occurred Error:Character Interval Timeout.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error910", $"{Name} Aligner Occurred Error:Received Data ChecksumError.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error920", $"{Name} Aligner Occurred Error:Unit Number Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error930", $"{Name} Aligner Occurred Error:Undefined CommandReceived.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error940", $"{Name} Aligner Occurred Error:Message Parameter Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error950", $"{Name} Aligner Occurred Error:Receiving Time-out Error for Confirmation of Execution Completion.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error960", $"{Name} Aligner Occurred Error:Incorrect sequence number.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error961", $"{Name} Aligner Occurred Error:Duplicated message.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error970", $"{Name} Aligner Occurred Error:Delimiter error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error9A1", $"{Name} Aligner Occurred Error:Message buffer overflow.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error9C0", $"{Name} Aligner Occurred Error:LAN device setting error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error9C1", $"{Name} Aligner Occurred Error:IP address error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error9C2", $"{Name} Aligner Occurred Error:Subnet mask error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error9C3", $"{Name} Aligner Occurred Error:Default gateway error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error9D0", $"{Name} Aligner Occurred Error:Ethernet receive error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error9E0", $"{Name} Aligner Occurred Error:During operation themaintenance tool.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}Error9E1", $"{Name} Aligner Occurred Error:The data abnormal.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorA01", $"{Name} Aligner Occurred Error:Re-detection of a powerSupply voltage fall.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorA10", $"{Name} Aligner Occurred Error:External emergency stop.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorA20", $"{Name} Aligner Occurred Error:T.P emergency stop.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorA21", $"{Name} Aligner Occurred Error:Interlock board failure 0.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorA30", $"{Name} Aligner Occurred Error:Emergency stop.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorA40", $"{Name} Aligner Occurred Error:Controller Fan 1 Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorA41", $"{Name} Aligner Occurred Error:Controller Fan 2 Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorA42", $"{Name} Aligner Occurred Error:Controller Fan 3 Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorA45", $"{Name} Aligner Occurred Error:Unit fan 1 error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorA46", $"{Name} Aligner Occurred Error:Unit fan 2 error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorA4F", $"{Name} Aligner Occurred Error:Controller Battery Alarm.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAC0", $"{Name} Aligner Occurred Error:Safety fence signal detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAC9", $"{Name} Aligner Occurred Error:Protection stop signal.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAE0", $"{Name} Aligner Occurred Error:HOST Mode Switching error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAE1", $"{Name} Aligner Occurred Error:TEACH Mode Switching Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAE8", $"{Name} Aligner Occurred Error:Deadman switch error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAF0", $"{Name} Aligner Occurred Error:Interlock board failure 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAF1", $"{Name} Aligner Occurred Error:Interlock board failure 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAF2", $"{Name} Aligner Occurred Error:Interlock board failure 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAF3", $"{Name} Aligner Occurred Error:Interlock board failure 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAF4", $"{Name} Aligner Occurred Error:Interlock board failure 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAF5", $"{Name} Aligner Occurred Error:Interlock board failure 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAF6", $"{Name} Aligner Occurred Error:Interlock board failure 7.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAF8", $"{Name} Aligner Occurred Error:Input compare error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAF9", $"{Name} Aligner Occurred Error:Input compare error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAFA", $"{Name} Aligner Occurred Error:Input compare error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAFB", $"{Name} Aligner Occurred Error:Input compare error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAFC", $"{Name} Aligner Occurred Error:Input compare error 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAFD", $"{Name} Aligner Occurred Error:Input compare error 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAFE", $"{Name} Aligner Occurred Error:Input compare error 7.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorAFF", $"{Name} Aligner Occurred Error:Input compare error 8.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB10", $"{Name} Aligner Occurred Error:Axis-1 Speed Limit Detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB11", $"{Name} Aligner Occurred Error:Axis-2 Speed Limit Detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB12", $"{Name} Aligner Occurred Error:Axis-3 Speed Limit Detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB13", $"{Name} Aligner Occurred Error:Axis-4 Speed Limit Detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB14", $"{Name} Aligner Occurred Error:Axis-5 Speed Limit Detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB20", $"{Name} Aligner Occurred Error:Axis-1 Positive (+) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB21", $"{Name} Aligner Occurred Error:Axis-2 Positive (+) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB22", $"{Name} Aligner Occurred Error:Axis-3 Positive (+) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB23", $"{Name} Aligner Occurred Error:Axis-4 Positive (+) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB24", $"{Name} Aligner Occurred Error:Axis-5 Positive (+) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB28", $"{Name} Aligner Occurred Error:Axis-1 Positive (+) Direction Software-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB29", $"{Name} Aligner Occurred Error:Axis-2 Positive (+) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB2A", $"{Name} Aligner Occurred Error:Axis-3 Positive (+) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB2B", $"{Name} Aligner Occurred Error:Axis-4 Positive (+) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB2C", $"{Name} Aligner Occurred Error:Axis-5 Positive (+) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB30", $"{Name} Aligner Occurred Error:Axis-1 Negative (-) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB31", $"{Name} Aligner Occurred Error:Axis-2 Negative (-) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB32", $"{Name} Aligner Occurred Error:Axis-3 Negative (-) Direction Software-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB33", $"{Name} Aligner Occurred Error:Axis-4 Negative (-) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB34", $"{Name} Aligner Occurred Error:Axis-5 Negative (-) DirectionSoftware-limit Detection 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB38", $"{Name} Aligner Occurred Error:Axis-1 Negative (-) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB39", $"{Name} Aligner Occurred Error:Axis-2 Negative (-) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB3A", $"{Name} Aligner Occurred Error:Axis-3 Negative (-) Direction Software-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB3B", $"{Name} Aligner Occurred Error:Axis-4 Negative (-) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB3C", $"{Name} Aligner Occurred Error:Axis-5 Negative (-) DirectionSoftware-limit Detection 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB40", $"{Name} Aligner Occurred Error:Access Permission Signal 1Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB41", $"{Name} Aligner Occurred Error:Access Permission Signal 2Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB42", $"{Name} Aligner Occurred Error:Access Permission Signal 3Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB43", $"{Name} Aligner Occurred Error:Access Permission Signal 4Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB44", $"{Name} Aligner Occurred Error:Access Permission Signal 5 Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB45", $"{Name} Aligner Occurred Error:Access Permission Signal 6Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB46", $"{Name} Aligner Occurred Error:Access Permission Signal 7Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB47", $"{Name} Aligner Occurred Error:Access Permission Signal 8Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB48", $"{Name} Aligner Occurred Error:Access Permission Signal 9Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB49", $"{Name} Aligner Occurred Error:Access Permission Signal 10 Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB4A", $"{Name} Aligner Occurred Error:Access Permission Signal 11Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB4B", $"{Name} Aligner Occurred Error:Access Permission Signal 12Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB4C", $"{Name} Aligner Occurred Error:Access Permission Signal 13Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB4D", $"{Name} Aligner Occurred Error:Access Permission Signal 14Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB4E", $"{Name} Aligner Occurred Error:Access Permission Signal 15Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB4F", $"{Name} Aligner Occurred Error:Access Permission Signal 16Time-out Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB60", $"{Name} Aligner Occurred Error:Access Permission to P/A Stage Time-out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB61", $"{Name} Aligner Occurred Error:Access Permission to P/AStage Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB62", $"{Name} Aligner Occurred Error:Access Permission to P/A Stage Time-out Error 3.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB63", $"{Name} Aligner Occurred Error:Access Permission to P/A Stage Time-out Error 4.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB64", $"{Name} Aligner Occurred Error:Access Permission to P/AStage Time-out Error 5.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB65", $"{Name} Aligner Occurred Error:Access Permission to P/AStage Time-out Error 6.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB66", $"{Name} Aligner Occurred Error:Access Permission to P/AStage Time-out Error 7.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB68", $"{Name} Aligner Occurred Error:P/A motion permission timeout error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB70", $"{Name} Aligner Occurred Error:SS signal detection.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB80", $"{Name} Aligner Occurred Error:Fork 1/Pre-aligner: Wafer Presence Confirmation Time-out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB81", $"{Name} Aligner Occurred Error:Fork 1/Pre-aligner: WaferAbsence Confirmation Time- out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB82", $"{Name} Aligner Occurred Error:Fork 1/Pre-aligner: Wafer Presence Confirmation Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB83", $"{Name} Aligner Occurred Error:Fork 1/Pre-aligner: WaferAbsence Confirmation Time- out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB88", $"{Name} Aligner Occurred Error:Grip sensor Time-out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB89", $"{Name} Aligner Occurred Error:Grip sensor Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB8A", $"{Name} Aligner Occurred Error:UnGrip sensor Time-out Error1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB8B", $"{Name} Aligner Occurred Error:UnGrip sensor Time-out Error2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB8F", $"{Name} Aligner Occurred Error:Fork 1: Plunger non-operationerror.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB90", $"{Name} Aligner Occurred Error:Fork 2: Wafer Presence Confirmation Time-out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB91", $"{Name} Aligner Occurred Error:Fork 2: Wafer AbsenceConfirmation Time-out Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB92", $"{Name} Aligner Occurred Error:Fork 2: Wafer PresenceConfirmation Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB93", $"{Name} Aligner Occurred Error:Fork 2: Wafer AbsenceConfirmation Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB98", $"{Name} Aligner Occurred Error:Lifter up sensor Time-outError 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB99", $"{Name} Aligner Occurred Error:Lifter up sensor Time-out Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB9A", $"{Name} Aligner Occurred Error:Lifter down sensor Time-outError 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB9B", $"{Name} Aligner Occurred Error:Lifter down sensor Time-outError 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorB9F", $"{Name} Aligner Occurred Error:Fork 2: Plunger non-operationerror.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorBA0", $"{Name} Aligner Occurred Error:Fork 1/Pre-aligner: WaferAbsence Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorBA1", $"{Name} Aligner Occurred Error:Fork 1: Sensor StatusMismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorBA8", $"{Name} Aligner Occurred Error:Grip sensor status Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorBA9", $"{Name} Aligner Occurred Error:Grip sensor status Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorBAA", $"{Name} Aligner Occurred Error:Ungrip sensor status Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorBAB", $"{Name} Aligner Occurred Error:Ungrip sensor status Error 2.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorBAC", $"{Name} Aligner Occurred Error:Grip sensor status mismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorBAD", $"{Name} Aligner Occurred Error:Lifter/Grip sensor statusmismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorBB0", $"{Name} Aligner Occurred Error:Fork 2: Wafer Absence Error.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorBB1", $"{Name} Aligner Occurred Error:Fork 2: Sensor StatusMismatch.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", $"{Name}ErrorBB8", $"{Name} Aligner Occurred Error:Lifter up sensor status Error 1.", EventLevel.Alarm, EventType.EventUI_Notify));

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
                //if(AlignerType == 1)  // Vacuum Type
                //{
                //    IsNeedRelease = true;
                //}
                
            }
            else
            {
                //_lstHandlers.AddLast(new YaskawaAlignerMotionHandler(this, "CSOL", "1,0,0"));
                if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                {                    
                    EV.PostWarningLog($"{RobotModuleName}", $"System didn't detec wafer on aligner, but it has record.");

                }
                IsNeedRelease = false;
            }
            _lstHandlers.AddLast(new YaskawaAlignerMotionHandler(this, "CSOL", "1,0,0"));
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
                        if(_lstHandlers.Count != 0)
                        {
                            _lstHandlers.Clear();
                            OnError("CommunicationError");
                        }
                        
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
        private void ExecuteHandler(HandlerBase handler)
        {
            string commandstr = $",{UnitNumber}";
            if (IsEnableSeqNo)
            {
                CurrentSeqNo = SeqnoGenerator.create();
                commandstr += $",{CurrentSeqNo:D2}";
                SeqnoGenerator.release(CurrentSeqNo);
            }
            commandstr += $",{handler.SendText}";

            if (IsEnableCheckSum)
            {
                commandstr += ",";
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
        private YaskawaAlignerConnection _connection;
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
                _lstHandlers.AddLast(new YaskawaAlignerMotionHandler(this, "CSOL", strpara));
            }
            return true;
        }
        protected override bool fMonitorLiftup(object[] param)
        {
            IsBusy = false;
            return _lstHandlers.Count == 0 && !_connection.IsBusy;

        }
        protected override bool fStartLiftdown(object[] param)
        {
            lock (_locker)
            {
                string strpara = "2,0,0";
                _lstHandlers.AddLast(new YaskawaAlignerMotionHandler(this, "CSOL", strpara));
            }
            return true;
        }
        protected override bool fMonitorLiftdown(object[] param)
        {
            IsBusy = false;
            return _lstHandlers.Count == 0 && !_connection.IsBusy;

        }

        protected override bool fStartAlign(object[] param)
        {
            double aligneangle = (double)param[0];
            int intangle = (int)(aligneangle * 1000);
            CurrentNotch = aligneangle;
            lock (_locker)
            {
                string strpara = $"0,{intangle:D8}";
                _lstHandlers.AddLast(new YaskawaAlignerMotionHandler(this, "MALN", strpara));
                if (AlignerType == 1)
                {
                    _lstHandlers.AddLast(new YaskawaAlignerMotionHandler(this, "CSOL", "1,0,0"));
                    if (AlignerType == 1)  // Vacuum Type
                    {
                        IsNeedRelease = false;
                    }
                }
            }
            return true;
        }
        protected override bool fMonitorAligning(object[] param)
        {
            IsBusy = false;
            return _lstHandlers.Count == 0 && !_connection.IsBusy;

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
                ExecuteHandler(new YaskawaAlignerMotionHandler(this, "CSTP", "H"));
            }
            return true;
        }

        protected override bool fClear(object[] param)
        {
            lock (_locker)
            {
                ExecuteHandler(new YaskawaAlignerMotionHandler(this, "CCLR", "E"));
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
                        _lstHandlers.AddLast(new YaskawaAlignerReadHandler(this, "RSTS"));
                        _lstHandlers.AddLast(new YaskawaAlignerReadHandler(this, "RPOS", "F"));
                        _lstHandlers.AddLast(new YaskawaAlignerReadHandler(this, "RPOS", "R"));
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
            return _lstHandlers.Count == 0 && !_connection.IsBusy;

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
                            _lstHandlers.AddLast(new YaskawaAlignerSetHandler(this, "SSPD", strParameter));
                        }
                        break;
                    case "TransferSpeedLevel":    //SSLV Select the transfer speed level
                        string sslvlevel = param[1].ToString();
                        if (!"123".Contains(sslvlevel))
                        {
                            EV.PostAlarmLog(Name, $"Set {setcommand} with invalid parameter:" + sslvlevel);
                            return false;
                        }
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new YaskawaAlignerSetHandler(this, "SSLV", sslvlevel));
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
                            _lstHandlers.AddLast(new YaskawaAlignerSetHandler(this, "SPOS", strParameter));
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
                            _lstHandlers.AddLast(new YaskawaAlignerSetHandler(this, "SABS", strParameter));
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
                            _lstHandlers.AddLast(new YaskawaAlignerSetHandler(this, "SAPS", strParameter));
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
                            _lstHandlers.AddLast(new YaskawaAlignerSetHandler(this, "SPDL", strParameter));
                        }
                        break;
                    case "RegisterThePositionDataToVolatile": //SPSV
                        string spsvTrsSt = param[1].ToString();
                        string spsvPosture = param[2].ToString();
                        string spsvHand = param[3].ToString();
                        strParameter = $"{spsvTrsSt},{spsvPosture},{spsvHand}";
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new YaskawaAlignerSetHandler(this, "SPSV", strParameter));
                        }



                        break;
                    case "ReadThePostionDataFromVolatile": //SPLD
                        string spldTrsSt = param[1].ToString();
                        string spldPosture = param[2].ToString();
                        string spldHand = param[3].ToString();
                        strParameter = $"{spldTrsSt},{spldPosture},{spldHand}";
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new YaskawaAlignerSetHandler(this, "SPLD", strParameter));
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
                            _lstHandlers.AddLast(new YaskawaAlignerSetHandler(this, "SSTR", strParameter));
                        }
                        break;
                    case "ChangeParameterValue": // SPRM
                        string sprmParaType = param[1].ToString();
                        int sprmParaNO = Convert.ToInt32(param[2].ToString());
                        Int32 sprmValue = Convert.ToInt32(param[3].ToString());
                        strParameter = sprmParaType + "," + sprmParaNO.ToString("D4") + "," + sprmValue.ToString("D12");
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new YaskawaAlignerSetHandler(this, "SPRM", strParameter));
                        }
                        break;
                    case "EnableInterLock": //SMSK
                        int smskValid = Convert.ToInt16(param[1].ToString());
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new YaskawaAlignerSetHandler(this, "SPRM", smskValid.ToString("D4")));
                        }
                        break;
                    case "RegisterTheCurrentPositionAsCoordinate": //SSTD
                        string sstdAxis = param[1].ToString();
                        lock (_locker)
                        {
                            _lstHandlers.AddLast(new YaskawaAlignerSetHandler(this, "SSTD", sstdAxis));
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
                            _lstHandlers.AddLast(new YaskawaAlignerSetHandler(this, "SSTN", strParameter));
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
            return _lstHandlers.Count == 0 && !_connection.IsBusy;

        }

        protected override bool fStartUnGrip(object[] param)
        {
            lock (_locker)
            {
                string strpara = "1,0,0";
                _lstHandlers.AddLast(new YaskawaAlignerMotionHandler(this, "CSOL", strpara));
                if (AlignerType == 1)  // Vacuum Type
                {
                    IsNeedRelease = false;
                }
            }
            return true;
        }

        protected override bool fMonitorUnGrip(object[] param)
        {
            IsBusy = false;
            return _lstHandlers.Count == 0 && !_connection.IsBusy;

        }

        protected override bool fStartGrip(object[] param)
        {
            lock (_locker)
            {
                string strpara = "1,1,0";
                _lstHandlers.AddLast(new YaskawaAlignerMotionHandler(this, "CSOL", strpara));
                if (AlignerType == 1)  // Vacuum Type
                {
                    IsNeedRelease = true;
                }
            }
            return true;
        }
        protected override bool fMonitorGrip(object[] param)
        {
            IsBusy = false;
            return _lstHandlers.Count == 0 && !_connection.IsBusy;

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
            if (!_connection.IsConnected)
            {
                _connection.Connect();
            }
            return true;
        }
        protected override bool fMonitorReset(object[] param)
        {
            if (!_connection.IsConnected)
            {
                _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
                _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
                UnitNumber = SC.GetValue<int>($"{_scRoot}.{Name}.UnitNumber");
                IsEnableCheckSum = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableCheckSum");
                IsEnableSeqNo = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableSeqNo");
                _connection = new YaskawaAlignerConnection(this, _address);
                _connection.EnableLog(_enableLog);
                _connection.Connect();
            }
            return _connection.IsConnected;
        }

        public override bool IsNeedRelease
        {
            //get
            //{
            //if (AlignerType == 1)
            //    return true;
            //if (SC.ContainsItem($"{_scRoot}.{Name}.NeedReleaseBeforePick") && SC.GetValue<bool>($"{_scRoot}.{Name}.NeedReleaseBeforePick"))
            //    return true;
            get;set;
            //}
        }

        protected override bool fStartInit(object[] param)
        {
            CurrentNotch = 0;
            _connection.ForceClear();
            lock (_locker)
            {
                string strpara = "1,1,G";
                if( AlignerType==1)     //Vacuum Type
                    strpara = "1,1,N";
                _lstHandlers.AddLast(new YaskawaAlignerMotionHandler(this, "INIT", strpara));
                _lstHandlers.AddLast(new YaskawaAlignerGripAndDetectHandler(this));


            }
            return true;
        }
        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            return _lstHandlers.Count == 0 && !_connection.IsBusy;

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
                int intstatus = Convert.ToInt32("02", 16);
                IsManipulatorBatteryLow = ((intstatus & 0x10) == 0x10);
                IsCommandExecutionReady = ((intstatus & 0x20) == 0x20);
                IsServoON = ((intstatus & 0x40) == 0x40);
                IsErrorOccurred = ((intstatus & 0x80) == 0x80);
                IsControllerBatteryLow = ((intstatus & 0x1) == 0x1);
                IsWaferPresenceOnFromVacuumSensorOrGripSensor = ((intstatus & 0x2) == 0x2);
                IsWaferPresenceOnFromCCDSensor = ((intstatus & 0x4) == 0x4);
                return true;

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        public bool ParseReadData(string _command, string[] rdata)
        {
            try
            {
                if (_command == "RSTS")
                {
                    return (rdata.Length == 2 && ParseRSTSStatus(rdata));

                }
                if (_command == "RSLV")      //Read the speed level
                {
                    return (rdata.Length == 1 && ParseSpeedLevel(rdata[0]));
                }
                if (_command == "RPOS")   //Reference current postion
                {
                    return (rdata.Length > 1 && ParsePositionData(rdata));

                }
               
                if (_command == "RSTR")   //Reference station item value
                {
                    return (rdata.Length == 4 && ParseStationData(rdata));
                }
                if (_command == "RPRM")  //Reference the parameter values of the specified unit
                {
                    return (rdata.Length == 3 && ParseParameterData(rdata));
                }
                if (_command == "RMSK")  //Reference the interlock information
                {
                    return (rdata.Length == 1 && ParseInterlockInfo(rdata));

                }
                if (_command == "RVER")  //Reference the software version
                {
                    return (rdata.Length == 2 && ParseSoftwareVersion(rdata));
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
                    if(pdata.Length >=2)
                        CommandThetaPosition = Convert.ToInt32(pdata[1]) / 1000;
                    if(pdata.Length >=3)
                        CommandExtensionPosition = Convert.ToInt32(pdata[2]) / 1000;
                    if(pdata.Length >=4)
                        CommandArm1Position = Convert.ToInt32(pdata[3]) / 1000;
                    if (pdata.Length >= 5)
                        CommandArm2Position = Convert.ToInt32(pdata[4]) / 1000;
                    if (pdata.Length >= 6)
                        CommandZPosition = Convert.ToInt32(pdata[5]) / 1000;
                    return true;
                }
                if (pdata[0] == "F")
                {
                    if (pdata.Length >= 2)
                    {
                        CurrentThetaPosition = Convert.ToInt32(pdata[1]) / 1000;
                        PositionAxis1 = CurrentThetaPosition;
                    }
                    if (pdata.Length >= 3)
                    {
                        CurrentExtensionPosition = Convert.ToInt32(pdata[2]) / 1000;
                        PositionAxis2 = CurrentExtensionPosition;
                    }
                    if (pdata.Length >= 4)
                    {
                        CurrentArm1Position = Convert.ToInt32(pdata[3]) / 1000;
                        PositionAxis3 = CurrentArm1Position;
                    }
                    if (pdata.Length >= 5)
                    {
                        CurrentArm2Position = Convert.ToInt32(pdata[4]) / 1000;
                        PositionAxis4 = CurrentArm2Position;
                    }
                    if (pdata.Length >= 6)
                    {
                        CurrentZPosition = Convert.ToInt32(pdata[5]) / 1000;
                        PositionAxis5 = CurrentZPosition;
                    }
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
                IsCheckInterlockWaferPresenceAbsent = (intdata & 0x1) == 0;
                IsCheckInterlockManipulatorOperation = (intdata & 0x1000) == 0;
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
                AlignerSystemVersion = pdata[0];
                AlignerSoftwareVersion = pdata[1];
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        public bool ParseRSTSStatus(string[] status)
        {
            try
            {
                ErrorCode = status[0];
                int intstatus = Convert.ToInt32(status[1]);
                IsWaferPresenceOnFromVacuumSensorOrGripSensor = ((intstatus & 0x1) == 0x1);
                IsWaferPresenceOnFromCCDSensor = ((intstatus & 0x2) == 0x2);
                IsWaferHoldOnChuck = ((intstatus & 0x4) == 0x4);
                //IsGrippedBlade2 = ((intstatus & 0x8) == 0x8);
                IsGripSensorOnEnd = ((intstatus & 0x10) == 0x10);
                IsUngripSensorOnEnd = ((intstatus & 0x20) == 0x20);
                
                IsLifterUpperSensorOnEnd = ((intstatus & 0x100) == 0x100);
                IsLifterDownSensorOnEnd = ((intstatus & 0x200) == 0x200);
                IsLifterSolenoidOnUp = ((intstatus & 0x400) == 0x400);                
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
            //_connection.SendAck();
        }

        public override bool OnActionDone(object[] param)
        {
            //if (_lstHandlers.Count == 0)
            //{
            //    IsBusy = false;
            //    return base.OnActionDone(param);
            //}
            return true;
        }

        private  bool _isSimulatorMode
        {
            get
            {
                if(SC.ContainsItem("System.IsSimulatorMode"))
                {
                    return SC.GetValue<bool>("System.IsSimulatorMode");
                }

                return false;
            }
        }

        public override bool IsWaferPresent(int slotindex)
        {
            if (_isSimulatorMode)
                return base.IsWaferPresent(slotindex);
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
