using Aitex.Core.Common;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.OpenStages
{
    public class OpenStageGcl200 : LoadPortBaseDevice
    {
        public override bool IsWaferProtrude
        {
            get { return _diWaferProtrude != null && _diWaferProtrude.Value; }

        }
        private DIAccessor _diPresent;
        private DIAccessor _diWaferProtrude;

        private RD_TRIG _trigPresentAbsent = new RD_TRIG();
        private RD_TRIG _trigPresentAbsentDely = new RD_TRIG();

        private R_TRIG _trigWaferProtrude = new R_TRIG();

        DeviceTimer _deviceTimer = new DeviceTimer();
        private int _queryPeriod = 0;    //ms
        private bool _isStillThere = false;
        public override LoadportCassetteState CassetteState
        {
            get
            {
                if (!_diPresent.Value)
                {
                    return LoadportCassetteState.Absent;
                }

                if (!_diWaferProtrude.Value)
                {
                    return LoadportCassetteState.Normal;
                }

                return LoadportCassetteState.Unknown;
            }
        }

        public void SetPrensentAbsentDelay(int ms)
        {
            _queryPeriod = ms;
        }

        public OpenStageGcl200(string module, string name, DIAccessor diPresent, DIAccessor diNoWaferProtrude,RobotBaseDevice robot) : base(
            module, name,robot)
        {
            if (diPresent == null)
                throw new ArgumentException("DI present cannot be null", "diPresent");

            if (diNoWaferProtrude == null)
                throw new ArgumentException("DI NoWaferProtrude cannot be null", "diNoWaferProtrude");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");

            _diPresent = diPresent;
            _diWaferProtrude = diNoWaferProtrude;

            IsMapWaferByLoadPort = false;            
            
        }

        public override string SpecCarrierType
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.CarrierName{InfoPadCarrierIndex}"))
                    return SC.GetStringValue($"CarrierInfo.CarrierName{InfoPadCarrierIndex}");
                return "";
            }            
        }

        public override WaferSize GetCurrentWaferSize()
        {
            int intwz = SC.GetValue<int>($"CarrierInfo.CarrierWaferSize{InfoPadCarrierIndex}");
            switch (intwz)
            {
                case 0:
                    return WaferSize.WS0;
                case 1:
                    return WaferSize.WS0;
                case 2:
                    return WaferSize.WS2;
                case 3:
                    return WaferSize.WS3;
                case 4:
                    return WaferSize.WS4;
                case 5:
                    return WaferSize.WS5;
                case 6:
                    return WaferSize.WS6;
                case 7:
                case 8:
                    return WaferSize.WS8;
                case 12:
                    return WaferSize.WS12;
                default:
                    return WaferSize.WS0;
            }
        }
        public override bool IsLoaded => _isLoaded;
        private bool _isLoaded;
        public override void Monitor()
        {
            base.Monitor();

            //place foup
            _trigPresentAbsent.CLK = _diPresent.Value;
            if (_queryPeriod == 0)
            {
                if (_trigPresentAbsent.R)
                {
                    SetPresent(true);
                    SetPlaced(true);

                }

                //remove foup
                if (_trigPresentAbsent.T)
                {
                    SetPresent(false);
                    SetPlaced(false);
                    _isLoaded = false;

                }
            }
            else
            {
                if (_trigPresentAbsent.R)
                {
                    _deviceTimer.Start(_queryPeriod);
                }

                if (_diPresent.Value && _deviceTimer.IsTimeout() && !_isStillThere)
                {
                    SetPresent(true);
                    SetPlaced(true);

                    _isStillThere = true;
                    _deviceTimer.Stop();
                }

                //remove foup
                if (_trigPresentAbsent.T)
                {
                    _deviceTimer.Start(_queryPeriod);
                }

                if (!_diPresent.Value && _deviceTimer.IsTimeout() && _isStillThere)
                {
                    SetPlaced(false);
                    SetPresent(false);
                    _isStillThere = false;
                    _deviceTimer.Stop();
                }

            }
            //            _trigWaferProtrude.CLK = _diPresent.Value && _diWaferProtrude.Value;
            //            if (_trigWaferProtrude.Q)
            //            {
            //                EV.PostAlarmLog(Module, $"{Module}.{Name} Found wafer protrude");
            //            }

        }
        

        public override void Reset()
        {
            base.Reset();

            _trigWaferProtrude.RST = true;
        }

        public override bool IsEnableMapWafer(out string reason)
        {
            reason = "";
            if(!_diPresent.Value)
            {
                reason = "NoCarrierPlaced";
                return false;
             }
            if(_diWaferProtrude.Value)
            {
                reason = "WaferProtrude";
                return false;
            }
            if(!IsLoaded)
            {
                reason = "NotLoaded";
                return false;
            }
            return true;
        }

        public override bool MapWafer(out string reason)
        {
            _isLoaded = true;
            return base.MapWafer(out reason);
        }



        public override bool IsEnableTransferWafer(out string reason)
        {
            if (!_diPresent.Value)
            {
                reason = "no FOUP placed";
                return false;
            }

            if (_diWaferProtrude.Value)
            {
                reason = "Found wafer protrude";
                return false;
            }

            if (!_isMapped)
            {
                reason = "FOUP not mapped";
                return false;
            }

            reason = "";
            return true;
        }
        protected override bool fMonitorTransferBlock(object[] param)
        {
            if(!_diPresent.Value)
            {
                EV.PostAlarmLog(LPModuleName.ToString(), "Carrier was removed unexpected.");
                OnError("CarrierRemoveUnexpected");
            }
            return true;
        }

        public override bool Unload(out string reason)
        {
            reason = "";

            _isMapped = false;

            OnUnloaded();

            return true;
        }

        protected override bool fStartWrite(object[] param)
        {
            return true;
        }

        protected override bool fStartRead(object[] param)
        {
            return true;
        }

        protected override bool fStartExecute(object[] param)
        {
            return true;
        }
        protected override bool fMonitorExecuting(object[] param)
        {
        
            IsBusy = false;
            return true;
        }

        protected override bool fStartUnload(object[] param)
        {
            return true;
        }
        protected override bool fMonitorUnload(object[] param)
        {
            IsBusy = false;
            return true;
        }

        protected override bool fStartLoad(object[] param)
        {
            _isLoaded = true;
            return true;
        }

        protected override bool fMonitorLoad(object[] param)
        {
            IsBusy = false;
            return true;
        }

        protected override bool fStartInit(object[] param)
        {
            IsBusy = false;
            return true;
        }
        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            return true;
        }

        protected override bool fStartReset(object[] param)
        {
            IsBusy = false;
            return true;
        }

        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            return true;
        }
    }
}
