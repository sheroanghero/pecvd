using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Common;
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
    public class OpenStageLoadPort2 : LoadPort
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

        public OpenStageLoadPort2(string module, string name, DIAccessor diPresent, DIAccessor diNoWaferProtrude) : base(
            module, name)
        {
            if (diPresent == null)
                throw new ArgumentException("DI present cannot be null", "diPresent");

            if (diNoWaferProtrude == null)
                throw new ArgumentException("DI NoWaferProtrude cannot be null", "diNoWaferProtrude");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");

            _diPresent = diPresent;
            _diWaferProtrude = diNoWaferProtrude;
            _diPresent.Value = true;
            _diWaferProtrude.Value = false;
            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;
            SetPlaced(true);
            SetPresent(true);
        }
        
        public override  void Monitor()
        {
            base.Monitor();

            //place foup
            _trigPresentAbsent.CLK = _diPresent.Value;
            if (_queryPeriod == 0)
            {
                if (_trigPresentAbsent.R)
                {
                    SetPlaced(true);
                    SetPresent(true);
                }

                //remove foup
                if (_trigPresentAbsent.T)
                {
                    SetPlaced(false);
                    SetPresent(false);
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
                    SetPlaced(true);
                    SetPresent(true);
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

        public override bool IsEnableMapWafer()
        {
            return  _diPresent.Value && !_diWaferProtrude.Value;
        }

        public override bool IsEnableTransferWafer()
        {
            return IsEnableTransferWafer(out _);
        }

        public override  bool IsEnableTransferWafer(out string reason)
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
            Initalized =true;
          
            return true;
        }

        public override FoupDoorState DoorState
        {
            get
            {
                return FoupDoorState.Open;
            }
        }

    }
}
