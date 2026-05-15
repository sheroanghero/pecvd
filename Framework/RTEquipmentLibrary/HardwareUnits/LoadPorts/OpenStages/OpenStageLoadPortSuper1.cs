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
    public class OpenStageLoadPortSuper1 : LoadPort
    {
        public override bool IsWaferProtrude
        {
            get
            {
                return _diWaferProtrude.Value;
            }

        }
        private IoSensor _diPresent3;
        private IoSensor _diPresent4;
        private IoSensor _diPresent6;
        private IoSensor _diWaferProtrude;

       

        

        //private int _alarmCount = 0;
        //private int _presentCount = 0;
        //private int _BusyingCount = 0;
        //private int _CompleteCount = 0;

        
        

        private RD_TRIG _trigPresentAbsent = new RD_TRIG();

        private R_TRIG _trigWaferProtrude = new R_TRIG();


        public override LoadportCassetteState CassetteState
        {
            get
            {
                return _trigPresentAbsent.M ? LoadportCassetteState.Normal : LoadportCassetteState.Absent;
            }
        }

        public OpenStageLoadPortSuper1(string module, string name, IoSensor[] diPresents, IoTrigger[] doIndicators) : base(
            module, name)
        {
            if (diPresents[0] == null)
                throw new ArgumentException("DI present cannot be null", "diPresent");
            if (diPresents[1] == null)
                throw new ArgumentException("DI present cannot be null", "diPresent");
            if (diPresents[2] == null)
                throw new ArgumentException("DI present cannot be null", "diPresent");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");

            _diPresent3 = diPresents[0];
            _diPresent4 = diPresents[1];
            _diPresent6 = diPresents[2];
            _diWaferProtrude = diPresents[3];
            

            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;
            SetPlaced(false);
            SetPresent(false);
            Initalized = true;
            //InfoPadCarrierIndex = SC.GetValue<int>($"CarrierInfo.{Name}CarrierIndex");
        }

        public override bool Initialize()
        {
            DATA.Subscribe($"{Name}.WaferSize", () => GetCurrentWaferSize().ToString());
            DATA.Subscribe($"{Name}.WaferSizeInfo", () => $"Index:{InfoPadCarrierIndex}\r\n" + 
            $"WaferSize:{GetCurrentWaferSize()}");
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
            if (InfoPadCarrierIndex == 1) return WaferSize.WS3;
            if (InfoPadCarrierIndex == 2) return WaferSize.WS4;
            if (InfoPadCarrierIndex == 4) return WaferSize.WS6;

            return WaferSize.WS0;
        }

        public override void Monitor()
        {
            try
            {
                base.Monitor();                

                bool present3 = _diPresent3.Value && (!_diPresent4.Value) && (!_diPresent6.Value);
                bool present4 = (!_diPresent3.Value) && (_diPresent4.Value) && (!_diPresent6.Value);
                bool present6 = (!_diPresent3.Value) && (!_diPresent4.Value) && (_diPresent6.Value);

                _trigPresentAbsent.CLK = present3|| present4 || present6;
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
                }
                int infopadindex = (_diPresent3.Value ? 1 : 0) + (_diPresent4.Value ? 2 : 0) + (_diPresent6.Value ? 4 : 0);
                if (InfoPadCarrierIndex != infopadindex)
                {
                    InfoPadCarrierIndex = infopadindex;
                    SC.SetItemValue($"CarrierInfo.{Name}CarrierIndex", infopadindex);                    
                }
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
            bool present3 = _diPresent3.Value && (!_diPresent4.Value) && (!_diPresent6.Value);
            bool present4 = (!_diPresent3.Value) && (_diPresent4.Value) && (!_diPresent6.Value);
            bool present6 = (!_diPresent3.Value) && (!_diPresent4.Value) && (_diPresent6.Value);

            return (!_diWaferProtrude.Value) && (present4 ||present3||present6);
        }

        public override bool IsEnableTransferWafer()
        {
            return IsEnableTransferWafer(out _);
        }

        public override bool IsEnableTransferWafer(out string reason)
        {
            bool present3 = !_diPresent3.Value && (!_diPresent4.Value) && (!_diPresent6.Value);
            bool present4 = (!_diPresent3.Value) && (_diPresent4.Value) && (!_diPresent6.Value);
            bool present6 = (_diPresent3.Value) && (!_diPresent4.Value) && (_diPresent6.Value);

            if ((!present4) && (!present6) &&(!present3))
            {
                reason = "no cassette placed";
                return false;
            }


            if (_diWaferProtrude.Value)
            {
                reason = "wafer protrude";
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
            return true;
        }


    }
}
