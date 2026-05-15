using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts
{
    public class OpenStageLoadPort : LoadPort
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
        public override bool ReadRfId(out string reason)
        {
            OP.DoOperation("ReadCarrierId", Name);
            reason = "";
            return true;

        }

        public OpenStageLoadPort(string module, string name, DIAccessor diPresent, DIAccessor diNoWaferProtrude) : base(
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

            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;
            Initalized = true;
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
                    SetPresent(true);
                    SetPlaced(true);
                    
                }

                //remove foup
                if (_trigPresentAbsent.T)
                {
                    SetPresent(false);
                    SetPlaced(false);
                    
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

        public override bool IsEnableMapWafer()
        {
            return _diPresent.Value && !_diWaferProtrude.Value;
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

    }

    public class OpenStageLoadPortCRRC : LoadPort
    {
        public override FoupDoorState DoorState
        {
            get
            {                
                return FoupDoorState.Open;
            }
        }
        
        private DIAccessor _diPresent;
        private DIAccessor _diWaferProtrude;

        public DIAccessor DIinfoPadA;
        public DIAccessor DIinfoPadB;
        public DIAccessor DIinfoPadC;
        public DIAccessor DIinfoPadD;

        private DOAccessor _doCst1;
        private DOAccessor _doCst2;
        private DOAccessor _doCst3;
        private DOAccessor _doCst4;
        private DOAccessor _doPlacement;
        public DOAccessor _doAlarm;

        private int InfoPadIndex;    
        private RD_TRIG _trigPresentAbsent = new RD_TRIG();
        private RD_TRIG _trigPresentAbsentDely = new RD_TRIG();

        private R_TRIG _trigWaferProtrude = new R_TRIG();

        DeviceTimer _deviceTimer = new DeviceTimer();
        private int _queryPeriod = 0;    //ms
        //private bool _isStillThere = false;

        private bool _isVirtualMode = false;

        public override LoadportCassetteState CassetteState
        {         


            get
            {
                if (_diPresent == null) return LoadportCassetteState.Absent;
                if (_diPresent.Value)
                {
                    return LoadportCassetteState.Absent;
                }
                return LoadportCassetteState.Normal;
                //if (_diWaferProtrude.Value)
                //{
                //    return LoadportCassetteState.Normal;
                //}

                //return LoadportCassetteState.Unknown;
            }
        }

        public void SetPrensentAbsentDelay(int ms)
        {
            _queryPeriod = ms;
        }

        public OpenStageLoadPortCRRC(string module, string name, DIAccessor diPresent, DIAccessor diNoWaferProtrude,DIAccessor[] inforpad, DOAccessor[] indicators ) : base(
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

            DIinfoPadA = inforpad[0];
            DIinfoPadB = inforpad[1];
            DIinfoPadC = inforpad[2];
            DIinfoPadD = inforpad[3];

            _doCst1 = indicators[0];
            _doCst2 = indicators[1];
            _doCst3 = indicators[2];
            _doCst4 = indicators[3];
            _doPlacement = indicators[4];
            _doAlarm = indicators[5];

            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;

            Initalized = true;
            IsBusy = false;
            Error = false;

            InfoPadIndex = SC.ContainsItem($"CarrierInfo.{Name}CarrierIndex") ? SC.GetValue<int>($"CarrierInfo.{name}CarrierIndex") : -1;
            InfoPadCarrierType = SC.ContainsItem($"CarrierInfo.CarrierName{InfoPadIndex}") ?
                        SC.GetStringValue($"CarrierInfo.CarrierName{InfoPadIndex}") : "Unknown";
            EV.Subscribe(new EventItem("Event", $"{name}ProtrudeError", $"{Name} trigger protrude error", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.EventUI_Notify));

        }


        public OpenStageLoadPortCRRC(string module, string name) : base(module, name)
        {
            _isVirtualMode = true;


            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;

            Initalized = true;
            IsBusy = false;
            Error = false;
            EV.Subscribe(new EventItem("Event", $"{name}ProtrudeError", $"{Name} trigger protrude error", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.EventUI_Notify));


        }
        private int _lampflashCount = 0;

        public override void Monitor()
        {
            if (_isVirtualMode) return;
            int infoindex = ((DIinfoPadA == null || !DIinfoPadA.Value) ? 0 : 8) + ((DIinfoPadB == null || !DIinfoPadB.Value) ? 0 : 4) +
                ((DIinfoPadC == null || !DIinfoPadC.Value) ? 0 : 2) + ((DIinfoPadD == null || !DIinfoPadD.Value) ? 0 : 1);

            IsInfoPadAOn = DIinfoPadA == null ? false : DIinfoPadA.Value;
            IsInfoPadBOn = DIinfoPadB == null ? false : DIinfoPadB.Value;
            IsInfoPadCOn = DIinfoPadC == null ? false : DIinfoPadC.Value;
            IsInfoPadDOn = DIinfoPadD == null ? false : DIinfoPadD.Value;

            if (infoindex != InfoPadIndex && InfoPadIndex !=-1)
            {
                SC.SetItemValue($"CarrierInfo.{Name}CarrierIndex", infoindex);
                InfoPadIndex = infoindex;
                InfoPadCarrierType = SC.ContainsItem($"CarrierInfo.CarrierName{infoindex}") ?
                    SC.GetStringValue($"CarrierInfo.CarrierName{infoindex}") : "Unknown";
            }
            IsWaferProtrude = _diWaferProtrude != null ? !_diWaferProtrude.Value : false;
            if (!IsWaferProtrude) TrigWaferProtrude.CLK = false;
            //if (IsMonitorProtrude) TrigWaferProtrude.CLK = IsWaferProtrude;
            if (TrigWaferProtrude.Q)
            {
                EV.Notify($"{Name}ProtrudeError");
                TrigWaferProtrude.CLK = true;
            }

            if (IsPlacement && !IsComplete)
            {
                _doPlacement.Value = true;
            }
            if(IsPlacement && IsComplete && _lampflashCount++>5)
            {
                _lampflashCount = 0;
                _doPlacement.Value = !_doPlacement.Value;
            }
            if(!IsPlacement)
            {
                //MapError = false;
                ReadCarrierIDError = false;
                _doPlacement.Value = false;
            }

            switch (infoindex)
            {
                case 1:
                    _doCst1.Value = false;
                    _doCst2.Value = false;
                    _doCst3.Value = false;
                    _doCst4.Value = true;
                    //_doPlacement.Value = true;
                    break;

                case 0:
                    _doCst1.Value = true;
                    _doCst2.Value = false;
                    _doCst3.Value = false;
                    _doCst4.Value = false;
                    //_doPlacement.Value = true;
                    break;
                case 2:
                    _doCst1.Value = false;
                    _doCst2.Value = true;
                    _doCst3.Value = false;
                    _doCst4.Value = false;
                    //_doPlacement.Value = true;
                    break;
                case 9:
                    _doCst1.Value = false;
                    _doCst2.Value = false;
                    _doCst3.Value = true;
                    _doCst4.Value = false;
                    //_doPlacement.Value = true;
                    break;
                case 15:
                    _doCst1.Value = false;
                    _doCst2.Value = false;
                    _doCst3.Value = false;
                    _doCst4.Value = false;
                    //_doPlacement.Value = false;
                    break;
                case 14:
                    _doCst1.Value = false;
                    _doCst2.Value = false;
                    _doCst3.Value = false;
                    _doCst4.Value = false;
                    //_doPlacement.Value = false;
                    break;
                default:
                    _doCst1.Value = false;
                    _doCst2.Value = false;
                    _doCst3.Value = false;
                    _doCst4.Value = false;
                    //_doPlacement.Value = true;
                    break;
            }
            if(MapError || ReadCarrierIDError|| (!_diWaferProtrude.Value && !IsMapped))
            {
                _doAlarm.Value = true;
            }
            else 
                _doAlarm.Value = false;

            



            base.Monitor();

            //place foup
            //place foup
            //_trigPresentAbsent.CLK = !_diPresent.Value;

            monitorSignalDelay(_diPresent.Value, ref _trigPresentAbsent, ref _presentmonitorCount, 30);

            if (_trigPresentAbsent.R)
            {
                SetPresent(true);
                SetPlaced(true);
                _trigPresentAbsent.CLK = true;


            }

            //remove foup
            if (_trigPresentAbsent.T)
            {
                SetPresent(false);
                SetPlaced(false);
                _trigPresentAbsent.CLK = false;

            }




        }

        private int _presentmonitorCount;

        private void monitorSignalDelay(bool inputvalue, ref RD_TRIG trigger, ref int count, int trigcount)
        {
            if (inputvalue)
            {
                trigger.CLK = false;
                count = 0;
            }
            else if (count > trigcount)
            {
                trigger.CLK = true;
                count = 0;
            }
            else count++;

        }


        public override void Reset()
        {
            base.Reset();

            _trigWaferProtrude.RST = true;
        }

        public override bool IsEnableMapWafer()
        {
            return !_diPresent.Value && _diWaferProtrude.Value;
        }

        public override bool IsEnableTransferWafer()
        {
            return IsEnableTransferWafer(out _);
        }

        public override bool IsEnableTransferWafer(out string reason)
        {
            if (_diPresent.Value)
            {
                reason = "no FOUP placed";
                return false;
            }

            if (!_diWaferProtrude.Value)
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
        public override bool ReadRfId(out string reason)
        {
            reason = "";

            OP.DoOperation("ReadCarrierId", new object[] { Name });
            return true;
        }

        public override bool FALoad(out string reason)
        {
            reason = "";
            if(!IsEnableMapWafer())
            {
                reason = "Not ready to map wafer";
                return false;
            }
            OP.DoOperation("MapWafer", new object[] { Name });
            return true;
        }

    }

    public class OpenStageLoadPortCRRC02 : LoadPort
    {
        public override FoupDoorState DoorState
        {
            get
            {
                return FoupDoorState.Open;
            }
        }

        private DIAccessor _diPresent;
        private DIAccessor _diWaferProtrude;

        public DIAccessor DIinfoPadA;
        public DIAccessor DIinfoPadB;
        public DIAccessor DIinfoPadC;
        public DIAccessor DIinfoPadD;

        private DOAccessor _doCst1;
        private DOAccessor _doCst2;
        private DOAccessor _doCst3;
        private DOAccessor _doCst4;
        private DOAccessor _doPlacement;
        public DOAccessor _doAlarm;

        private int InfoPadIndex;
        private RD_TRIG _trigPresentAbsent = new RD_TRIG();
        private RD_TRIG _trigPresentAbsentDely = new RD_TRIG();

        private R_TRIG _trigWaferProtrude = new R_TRIG();

        DeviceTimer _deviceTimer = new DeviceTimer();
        private int _queryPeriod = 0;    //ms
        //private bool _isStillThere = false;

        private bool _isVirtualMode = false;

        public override LoadportCassetteState CassetteState
        {


            get
            {
                if (_diPresent == null) return LoadportCassetteState.Absent;
                if (_diPresent.Value)
                {
                    return LoadportCassetteState.Absent;
                }
                return LoadportCassetteState.Normal;
                //if (_diWaferProtrude.Value)
                //{
                //    return LoadportCassetteState.Normal;
                //}

                //return LoadportCassetteState.Unknown;
            }
        }

        public void SetPrensentAbsentDelay(int ms)
        {
            _queryPeriod = ms;
        }

        public OpenStageLoadPortCRRC02(string module, string name, DIAccessor diPresent, DIAccessor diNoWaferProtrude, DIAccessor[] inforpad, DOAccessor[] indicators) : base(
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

            DIinfoPadA = inforpad[0];
            DIinfoPadB = inforpad[1];
            DIinfoPadC = inforpad[2];
            DIinfoPadD = inforpad[3];

            _doCst1 = indicators[0];
            _doCst2 = indicators[1];
            _doCst3 = indicators[2];
            _doCst4 = indicators[3];
            _doPlacement = indicators[4];
            _doAlarm = indicators[5];

            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;

            Initalized = true;
            IsBusy = false;
            Error = false;

            InfoPadIndex = SC.ContainsItem($"CarrierInfo.{Name}CarrierIndex") ? SC.GetValue<int>($"CarrierInfo.{name}CarrierIndex") : -1;
            InfoPadCarrierType = SC.ContainsItem($"CarrierInfo.CarrierName{InfoPadIndex}") ?
                        SC.GetStringValue($"CarrierInfo.CarrierName{InfoPadIndex}") : "Unknown";
        }


        public OpenStageLoadPortCRRC02(string module, string name) : base(module, name)
        {
            _isVirtualMode = true;


            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;

            Initalized = true;
            IsBusy = false;
            Error = false;


        }
        private int _lampflashCount = 0;

        public override void Monitor()
        {
            if (_isVirtualMode) return;
            int infoindex = ((DIinfoPadA == null || !DIinfoPadA.Value) ? 0 : 8) + ((DIinfoPadB == null || !DIinfoPadB.Value) ? 0 : 4) +
                ((DIinfoPadC == null || !DIinfoPadC.Value) ? 0 : 2) + ((DIinfoPadD == null || !DIinfoPadD.Value) ? 0 : 1);

            IsInfoPadAOn = DIinfoPadA == null ? false : DIinfoPadA.Value;
            IsInfoPadBOn = DIinfoPadB == null ? false : DIinfoPadB.Value;
            IsInfoPadCOn = DIinfoPadC == null ? false : DIinfoPadC.Value;
            IsInfoPadDOn = DIinfoPadD == null ? false : DIinfoPadD.Value;


            if (infoindex != InfoPadIndex && InfoPadIndex != -1)
            {
                SC.SetItemValue($"CarrierInfo.{Name}CarrierIndex", infoindex);
                InfoPadIndex = infoindex;
                InfoPadCarrierType = SC.ContainsItem($"CarrierInfo.CarrierName{infoindex}") ?
                    SC.GetStringValue($"CarrierInfo.CarrierName{infoindex}") : "Unknown";
            }
            
            if (IsPlacement && !IsComplete)
            {
                _doPlacement.Value = true;
            }
            if (IsPlacement && IsComplete && _lampflashCount++ > 5)
            {
                _lampflashCount = 0;
                _doPlacement.Value = !_doPlacement.Value;
            }
            if (!IsPlacement)
            {
                //MapError = false;
                ReadCarrierIDError = false;
                _doPlacement.Value = false;
            }

            switch (infoindex)
            {
                case 1:
                    _doCst1.Value = false;
                    _doCst2.Value = false;
                    _doCst3.Value = false;
                    _doCst4.Value = false;
                    //_doPlacement.Value = true;
                    break;

                case 0:
                    _doCst1.Value = true;
                    _doCst2.Value = false;
                    _doCst3.Value = false;
                    _doCst4.Value = false;
                    //_doPlacement.Value = true;
                    break;
                case 2:
                    _doCst1.Value = false;
                    _doCst2.Value = true;
                    _doCst3.Value = false;
                    _doCst4.Value = false;
                    //_doPlacement.Value = true;
                    break;
                case 8:
                    _doCst1.Value = false;
                    _doCst2.Value = false;
                    _doCst3.Value = true;
                    _doCst4.Value = false;
                    //_doPlacement.Value = true;
                    break;
                case 15:
                    _doCst1.Value = false;
                    _doCst2.Value = false;
                    _doCst3.Value = false;
                    _doCst4.Value = false;
                    //_doPlacement.Value = false;
                    break;
                case 14:
                    _doCst1.Value = false;
                    _doCst2.Value = false;
                    _doCst3.Value = false;
                    _doCst4.Value = false;
                    //_doPlacement.Value = false;
                    break;
                default:
                    _doCst1.Value = false;
                    _doCst2.Value = false;
                    _doCst3.Value = false;
                    _doCst4.Value = false;
                    //_doPlacement.Value = true;
                    break;
            }
            if (MapError || ReadCarrierIDError || (!_diWaferProtrude.Value && !IsMapped))
            {
                _doAlarm.Value = true;
            }
            else
                _doAlarm.Value = false;

            IsWaferProtrude = _diWaferProtrude != null ? !_diWaferProtrude.Value : false;

            base.Monitor();

            //place foup
            //_trigPresentAbsent.CLK = !_diPresent.Value;

            monitorSignalDelay(_diPresent.Value, ref _trigPresentAbsent, ref _presentmonitorCount, 30);

            if (_trigPresentAbsent.R)
            {
                SetPresent(true);
                SetPlaced(true);
                DockState = FoupDockState.Docked;
                _trigPresentAbsent.CLK = true;


            }

            //remove foup
            if (_trigPresentAbsent.T)
            {
                SetPresent(false);
                SetPlaced(false);
                _trigPresentAbsent.CLK = false;

            }

          


        }

        private int _presentmonitorCount;

        private void monitorSignalDelay(bool inputvalue, ref RD_TRIG trigger, ref int count, int trigcount)
        {
            if (inputvalue)
            {
                trigger.CLK = false;
                count = 0;
            }
            else if (count > trigcount)
            {
                trigger.CLK = true;
                count = 0;
            }
            else count++;
            
        }
        public override bool FALoad(out string reason)
        {
            reason = "";
            if (!IsEnableMapWafer())
            {
                reason = "Not ready to map wafer";
                return false;
            }
            OP.DoOperation("MapWafer", new object[] { Name });
            return true;
        }

        public override void Reset()
        {
            base.Reset();

            _trigWaferProtrude.RST = true;
        }

        public override bool IsEnableMapWafer()
        {
            return !_diPresent.Value && _diWaferProtrude.Value;
        }

        public override bool IsEnableTransferWafer()
        {
            return IsEnableTransferWafer(out _);
        }

        public override bool IsEnableTransferWafer(out string reason)
        {
            if (_diPresent.Value)
            {
                reason = "no FOUP placed";
                return false;
            }

            if (!_diWaferProtrude.Value)
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
        public override bool Load(out string reason)
        {
            reason = "";
            OP.DoOperation("MapWafer", new object[] { Name });
            return true;
        }

        public override bool Unload(out string reason)
        {
            reason = "";

            _isMapped = false;

            OnUnloaded();

            return true;
        }
        public override bool ReadRfId(out string reason)
        {
            reason = "";

            OP.DoOperation("ReadCarrierId", new object[] { Name});
            return true;
        }

    }

    



}
