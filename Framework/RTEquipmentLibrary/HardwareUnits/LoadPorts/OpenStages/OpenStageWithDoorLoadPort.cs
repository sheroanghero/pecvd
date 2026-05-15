using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
 
namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts
{
    public class OpenStageWithDoorLoadPort : LoadPort
    {
        public override bool IsWaferProtrude
        {
            get { return _sensorWaferProtrude.Value; }            
        }
        private IoSensor _sensorPresent;
        private IoSensor _sensorWaferProtrude;
        private IoDoor _door;

        private RD_TRIG _trigPresentAbsent = new RD_TRIG();
        private RD_TRIG _trigPresentAbsentDely = new RD_TRIG();
        
        private R_TRIG _trigWaferProtrude = new R_TRIG();
        
        DeviceTimer _deviceTimer = new DeviceTimer();
        private int _queryPeriod = 0;    //ms
        private bool _isStillThere = false;

        public override bool IsBusy { get; set; }

        public override LoadportCassetteState CassetteState
        {
            get
            {
                if (!_sensorPresent.Value)
                {
                    return LoadportCassetteState.Absent;
                }

                if (!_sensorWaferProtrude.Value)
                {
                    return LoadportCassetteState.Normal;
                } 

                return LoadportCassetteState.Unknown;
            }
        }

        public override FoupDoorState DoorState
        {
            get
            {
                if (_door.IsOpen)
                    return FoupDoorState.Open;
                if (_door.IsClose)
                    return FoupDoorState.Close;
                return FoupDoorState.Unknown;
            }
        }


        public void SetPrensentAbsentDelay(int ms)
        {
            _queryPeriod = ms;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="module"></param>
        /// <param name="name"></param>
        /// <param name="sensorPresent"></param>
        /// <param name="sensorWaferProtrude"></param>
        /// <param name="door"></param>
        public OpenStageWithDoorLoadPort(string module, string name, IoSensor sensorPresent, IoSensor sensorWaferProtrude, IoDoor door) : base(
            module, name)
        {
            if (sensorPresent == null)
                throw new ArgumentException("prensent sensor can not be null", "sensorPresent");

            if (sensorWaferProtrude == null)
                throw new ArgumentException("protrude sensor cannot be null", "sensorWaferProtrude");

            if (door == null)
                throw new ArgumentException("door cannot be null", "door");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");

            _sensorPresent = sensorPresent;
            _sensorWaferProtrude = sensorWaferProtrude;
            _door = door;

            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStageWithDoor;
        }

        public override bool Initialize()
        {
            DATA.Subscribe(Name, "IsWaferProtrude", () => IsWaferProtrude);

            return base.Initialize();
        }

        public override  void Monitor()
        {
            base.Monitor();

            //place foup
            _trigPresentAbsent.CLK = _sensorPresent.Value;
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

                if (_sensorPresent.Value && _deviceTimer.IsTimeout() && !_isStillThere)
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
                
                if (!_sensorPresent.Value && _deviceTimer.IsTimeout() && _isStillThere)
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
            return _sensorPresent.Value && !_sensorWaferProtrude.Value && _door.IsOpen;
        }

        public override bool IsEnableTransferWafer()
        {
            return IsEnableTransferWafer(out _);
        }

        public override  bool IsEnableTransferWafer(out string reason)
        {
            if (!_sensorPresent.Value)
            {
                reason = "no FOUP placed";
                return false;
            }

            if (_sensorWaferProtrude.Value)
            {
                reason = "Found wafer protrude";
                return false;
            }

            if (!_door.IsOpen)
            {
                reason = "door not open";
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

            _door.Close(out reason);

            OnUnloaded();

            return true;
        }

        public override bool Load(out string reason)
        {
            reason = "";

            _isMapped = false;

            OnUnloaded();

            return true;
        }


        public override bool LoadWithoutMap(out string reason)
        {
            reason = "";

            _isMapped = false;

            _door.Open(out reason);

            return true;
        }

        public override bool OpenDoor(out string reason)
        {
            return _door.Open(out reason);
        }

        public override bool CloseDoor(out string reason)
        {
            return _door.Close(out reason);
        }
    }
}
