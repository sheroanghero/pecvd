using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts
{
    /// <summary>
    /// 定制OpenStage： 4,6寸兼容
    /// 4寸左右两个位置传感器
    /// 6寸左右两个位置传感器
    /// 4寸突出Sensor
    /// 6寸突出Sensor
    ///
    /// 4个小灯。Present/Alarm/Busy/Complete
    /// </summary>
    public class OpenStageLoadPort3 : LoadPort
    {
        public override bool IsWaferProtrude
        {
            get
            {
                bool present4 = _diPresentLeft4.Value && _diPresentRight4.Value && !_diPresentLeft6.Value && !_diPresentRight6.Value;
                bool present6 = _diPresentLeft6.Value && _diPresentRight6.Value && !_diPresentLeft4.Value && !_diPresentRight4.Value;
                if (present4 && _diWaferProtrude4Inch.Value)
                    return true;
                if (present6 && _diWaferProtrude6Inch.Value)
                    return true;
                return false;
            }

        }
        private IoSensor _diPresentLeft4;
        private IoSensor _diPresentRight4;
        private IoSensor _diPresentLeft6;
        private IoSensor _diPresentRight6;

        private IoSensor _diWaferProtrude4Inch;
        private IoSensor _diWaferProtrude6Inch;


        private IoTrigger _doPresent;
        private IoTrigger _doAlarm;
        private IoTrigger _doBusying;
        private IoTrigger _doComplete;

 

        private int _alarmCount = 0;
        //private int _presentCount = 0;
        private int _BusyingCount = 0;
        private int _CompleteCount = 0;


        public int CarrierIndex { get; private set; }

        private RD_TRIG _trigPresentAbsent = new RD_TRIG();
       
        private R_TRIG _trigWaferProtrude = new R_TRIG();


        public override LoadportCassetteState CassetteState
        {
            get
            {
                return _trigPresentAbsent.M ? LoadportCassetteState.Normal : LoadportCassetteState.Absent;
            }
        }

        public OpenStageLoadPort3(string module, string name, IoSensor[] diPresents,IoTrigger[] doIndicators) : base(
            module, name)
        {
            if (diPresents[0] == null)
                throw new ArgumentException("DI present cannot be null", "diPresent");

            if (diPresents[1] == null)
                throw new ArgumentException("DI present cannot be null", "diPresent");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");

            _diPresentLeft4 = diPresents[0];
            _diPresentRight4 = diPresents[1];
            _diPresentLeft6 = diPresents[2];
            _diPresentRight6 = diPresents[3];
            _diWaferProtrude4Inch = diPresents[4];
            _diWaferProtrude6Inch = diPresents[5];

            _doPresent = doIndicators[0];
            _doAlarm = doIndicators[1];
            _doBusying = doIndicators[2];
            _doComplete = doIndicators[3];

            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;
            SetPlaced(false);
            SetPresent(false);
            Initalized = true;

            CarrierIndex = SC.GetValue<int>($"CarrierInfo.{Name}CarrierIndex");
        }

        public override bool Initialize()
        {
            DATA.Subscribe($"{Name}.WaferSize", ()=>GetCurrentWaferSize().ToString());


            LoadPortIndicatorLightMap =
                new Dictionary<IndicatorType, Indicator>()
                {
                    {IndicatorType.Presence, Indicator.PRESENCE},
                    {IndicatorType.Alarm, Indicator.ALARM},
                    {IndicatorType.Busy, Indicator.RESERVE1 },
                    {IndicatorType.Complete, Indicator.RESERVE2},
                };

            return base.Initialize();
        }

        public override WaferSize GetCurrentWaferSize()
        {
            if (CarrierIndex == 2) return WaferSize.WS6;
            if (CarrierIndex == 3) return WaferSize.WS4;

            return WaferSize.WS0;
        }

        public override void Monitor()
        {
            try
            {
                base.Monitor();

                if (GetIndicator(IndicatorType.Alarm) == IndicatorState.OFF)
                    _doAlarm.SetTrigger(false, out _);
                if (GetIndicator(IndicatorType.Alarm) == IndicatorState.ON)
                    _doAlarm.SetTrigger(true, out _);
                if (GetIndicator(IndicatorType.Alarm) == IndicatorState.BLINK && ++_alarmCount > 20)
                {
                    _alarmCount = 0;
                    _doAlarm.SetTrigger(!_doAlarm.Value, out _);
                }
 
                if (GetIndicator(IndicatorType.Busy) == IndicatorState.OFF)
                    _doBusying.SetTrigger(false, out _);
                if (GetIndicator(IndicatorType.Busy) == IndicatorState.ON)
                    _doBusying.SetTrigger(true, out _);
                if (GetIndicator(IndicatorType.Busy) == IndicatorState.BLINK && ++_BusyingCount > 20)
                {
                    _BusyingCount = 0;
                    _doBusying.SetTrigger(!_doBusying.Value, out _);
                }
                if (GetIndicator(IndicatorType.Complete) == IndicatorState.OFF)
                    _doComplete.SetTrigger(false, out _);
                if (GetIndicator(IndicatorType.Complete) == IndicatorState.ON)
                    _doComplete.SetTrigger(true, out _);
                if (GetIndicator(IndicatorType.Complete) == IndicatorState.BLINK && ++_CompleteCount > 20)
                {
                    _CompleteCount = 0;
                    _doComplete.SetTrigger(!_doComplete.Value, out _);
                }

                bool present4 = _diPresentLeft4.Value && _diPresentRight4.Value && !_diPresentLeft6.Value && !_diPresentRight6.Value;
                bool present6 = _diPresentLeft6.Value && _diPresentRight6.Value && !_diPresentLeft4.Value && !_diPresentRight4.Value;

                _trigPresentAbsent.CLK = present4 ^ present6;
                if (_trigPresentAbsent.R)
                {
                    SetPresent(true);
                    SetPlaced(true);
                }
                if (_trigPresentAbsent.T)
                {
                    SetPresent(false);
                    SetPlaced(false);
                    _isMapped = false;
                    MapError = false;
                }

                if (present6)
                {
                    if (CarrierIndex != 2)
                    {
                        SC.SetItemValue($"CarrierInfo.{Name}CarrierIndex", 2);
                        CarrierIndex = 2;
                    }
                }

                if (present4)
                {
                    if (CarrierIndex != 3)
                    {
                        SC.SetItemValue($"CarrierInfo.{Name}CarrierIndex", 3);
                        CarrierIndex = 3;
                    }
                }

                _doPresent.SetTrigger(_trigPresentAbsent.M, out _);

                SetIndicator(IndicatorType.Presence, _trigPresentAbsent.M ? IndicatorState.ON : IndicatorState.OFF);
 
                SetIndicator(IndicatorType.Alarm, _trigPresentAbsent.M && MapError ? IndicatorState.BLINK : IndicatorState.OFF );
            }
            catch (Exception ex)
            { LOG.Write(ex); }


        }


        public override void Reset()
        {
            base.Reset();
 
            _trigWaferProtrude.RST = true;
        }

        public override bool IsEnableMapWafer()
        {
            bool present4 = _diPresentLeft4.Value && _diPresentRight4.Value && !_diPresentLeft6.Value && !_diPresentRight6.Value;
            bool present6 = _diPresentLeft6.Value && _diPresentRight6.Value && !_diPresentLeft4.Value && !_diPresentRight4.Value;

            return (present6 && !_diWaferProtrude6Inch.Value) || (present4 && !_diWaferProtrude4Inch.Value);
        }

        public override bool IsEnableTransferWafer()
        {
            return IsEnableTransferWafer(out _);
        }

        public override bool IsEnableTransferWafer(out string reason)
        {
            bool present4 = _diPresentLeft4.Value && _diPresentRight4.Value && !_diPresentLeft6.Value && !_diPresentRight6.Value;
            bool present6 = _diPresentLeft6.Value && _diPresentRight6.Value && !_diPresentLeft4.Value && !_diPresentRight4.Value;

            if (!present4 && !present6)
            {
                reason = "no cassette placed";
                return false;
            }

            if (present6 && _diWaferProtrude6Inch.Value)
            {
                reason = "6 Inch wafer protrude";
                return false;
            }

            if (present4 && _diWaferProtrude4Inch.Value)
            {
                reason = "4 Inch wafer protrude";
                return false;
            }
            if (!_isMapped)
            {
                reason = "Not Mapped";
                return false;
            }
            if (MapError)
            {
                reason = "Mapping error";
                return false;
            }

            reason = "";
            return true;
        }

        public override bool Unload(out string reason)
        {
            reason = "";

            _isMapped = false;

            OnUnloaded();

            return true;
        }

        public override bool QueryState(out string reason)
        {
            reason = string.Empty;
            

            return true;
        }

        public override FoupDoorState DoorState
        {
            get
            {
                return FoupDoorState.Open;
            }
        }

        public override bool SetIndicator(Indicator light, IndicatorState state, out string reason)
        {
            reason = "";

            IndicatorStateFeedback[(int) light] = state;

            return true;
        }
    }
}
