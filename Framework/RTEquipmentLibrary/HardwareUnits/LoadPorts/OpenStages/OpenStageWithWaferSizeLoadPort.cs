using System;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts
{
    public class OpenStageWithWaferSizeLoadPort : LoadPort
    {
        public override bool IsWaferProtrude
        {
            get { return !_sensorWaferNotProtrude.Value; }
            
        }

        public WaferSize WaferSize
        {
            get { return _sizeDetector.Value; }
        }

        private IoWaferSizeDetector _sizeDetector;
        private IoSensor _sensorWaferNotProtrude;

        private RD_TRIG _trigPresentAbsent = new RD_TRIG();
        private RD_TRIG _trigPresentAbsentDely = new RD_TRIG();
        
        private F_TRIG _trigWaferProtrude = new F_TRIG();
        
        DeviceTimer _deviceTimer = new DeviceTimer();
        private int _queryPeriod = 0;    //ms
        private bool _isStillThere = false;

        public override bool IsBusy { get; set; }

        public override LoadportCassetteState CassetteState
        {
            get
            {
                if (_sizeDetector.NotPresent3 && _sizeDetector.NotPresent4 && _sizeDetector.NotPresent6)
                {
                    return LoadportCassetteState.Absent;
                }

                if (_sensorWaferNotProtrude.Value)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="module"></param>
        /// <param name="name"></param>
        /// <param name="sensorPresent"></param>
        /// <param name="sensorWaferNotProtrude"></param>
        public OpenStageWithWaferSizeLoadPort(string module, string name, IoWaferSizeDetector sensorPresent, IoSensor sensorWaferNotProtrude) : base(
            module, name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");

            _sizeDetector = sensorPresent ?? throw new ArgumentException("prensent sensor can not be null", "sensorPresent");
            _sensorWaferNotProtrude = sensorWaferNotProtrude ?? throw new ArgumentException("protrude sensor cannot be null", "sensorWaferNotProtrude");

            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStageWithWaferSize;
            Initalized = true;
        }

        public override bool Initialize()
        {
            DATA.Subscribe(Name, "IsWaferNotProtrude", () => _sensorWaferNotProtrude.Value);

            DATA.Subscribe(Name, "IsNotPresent3", () => _sizeDetector.NotPresent3);
            DATA.Subscribe(Name, "IsNotPresent4", () => _sizeDetector.NotPresent4);
            DATA.Subscribe(Name, "IsNotPresent6", () => _sizeDetector.NotPresent6);

            //DATA.Subscribe(Name, "CassetteState", () => CassetteState.ToString());
            DATA.Subscribe(Name, "WaferSize", () => _sizeDetector.Value.ToString());

            if (SC.ContainsItem("LoadPort.TimeDelayNotifyCassettePresent"))
            {
                _queryPeriod = SC.GetValue<int>("LoadPort.TimeDelayNotifyCassettePresent");
            }

            return base.Initialize();
        }

        public override  void Monitor()
        {
            base.Monitor();

            //place foup
            _trigPresentAbsent.CLK = _sizeDetector.HasCassette;
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

                if (_sizeDetector.HasCassette && _deviceTimer.IsTimeout() && !_isStillThere)
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
                
                if (!_sizeDetector.HasCassette && _deviceTimer.IsTimeout() && _isStillThere)
                {
                    SetPlaced(false);
                    SetPresent(false);
                    _isStillThere = false;
                    _deviceTimer.Stop();
                }

            }

        }


        public override void Reset()
        {
            base.Reset();

            _trigWaferProtrude.RST = true;
        }

        public override bool IsEnableMapWafer()
        {
            return _sizeDetector.HasCassette && _sensorWaferNotProtrude.Value;
        }

        public override bool IsEnableTransferWafer()
        {
            return IsEnableTransferWafer(out _);
        }

        public override  bool IsEnableTransferWafer(out string reason)
        {
            if (!_sizeDetector.HasCassette)
            {
                reason = "no FOUP placed";
                return false;
            }

            if (_sensorWaferNotProtrude.Value == false)
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
 
    }
}
