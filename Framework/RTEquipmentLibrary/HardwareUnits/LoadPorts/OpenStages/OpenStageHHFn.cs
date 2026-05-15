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
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;



namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.OpenStages
{
    public class OpenStageHHFn : LoadPortBaseDevice, IConnection
    {
        public EnumLoadPortType PortType { get; set; }
        public bool Initalized { get; set; }
        public bool Error { get; set; }
        
        public override FoupDoorState DoorState
        {
            get
            {
                return FoupDoorState.Open;
            }
        }
        private IoSensor _di6InchCst1Left;
        private IoSensor _di6InchCst1Right;
        private IoSensor _di6InchCst2Left;
        private IoSensor _di6InchCst2Right;
        private IoSensor _di6InchAdapterLeft;
        private IoSensor _di6InchAdapterRight;
        private IoSensor _di6InchCstOption;  //On 为 2
        private IoSensor _di6InchCst1Present;
        private IoSensor _di6InchCst2Present;
        private IoSensor _di6InchWaferProtrude;


        private IoSensor _di8InchCst1Left;
        private IoSensor _di8InchCst1Right;
        private IoSensor _di8InchCst2Left;
        private IoSensor _di8InchCst2Right;
        private IoSensor _di8InchCst3Left;
        private IoSensor _di8InchCst3Right;
        private IoSensor _di8InchCstPresent;
        private IoSensor _di8InchWaferProtrude;




        private IoTrigger _doIndicatorCstReady;
        private IoTrigger _doIndicatorCst6Inch;
        private IoTrigger _doIndicatorCst8Inch;
        private IoTrigger _doIndicatorCstRunning;
        private IoTrigger _doIndicatorCstComplete;
        private IoTrigger _doIndicatorCstAlarm;




        private RD_TRIG _trigPresentAbsent = new RD_TRIG();

        private RD_TRIG _trigPlacement = new RD_TRIG();

        private R_TRIG _trigWaferProtrude = new R_TRIG();
        private R_TRIG _trigCoverClosed = new R_TRIG();

        private R_TRIG _trigDoorOpen = new R_TRIG();
        private R_TRIG _trigDoorClose = new R_TRIG();

        

        
        private bool _isLoaded = false;
        private string _carrierInformation = "";



        public override LoadportCassetteState CassetteState
        {
            get
            {
                // 8"
                if(!_di6InchCst1Left.Value && !_di6InchCst1Right.Value && !_di6InchCst2Left.Value && !_di6InchCst2Right.Value &&
                    _di6InchAdapterLeft.Value && _di6InchAdapterRight.Value && !_di6InchCstOption.Value && !_di6InchCst1Present.Value &&
                    !_di6InchCst2Present.Value)
                {
                    if(!_di8InchCst1Left.Value && !_di8InchCst1Right.Value && !_di8InchCst2Left.Value && !_di8InchCst2Right.Value &&
                        _di8InchCst3Left.Value && _di8InchCst3Right.Value && _di8InchCstPresent.Value)
                    {
                        InfoPadCarrierIndex = 4;
                        _doIndicatorCst8Inch.SetTrigger(true, out _);
                        _doIndicatorCst6Inch.SetTrigger(false, out _);
                        
                        return LoadportCassetteState.Normal;
                    }

                    if (!_di8InchCst1Left.Value && !_di8InchCst1Right.Value && _di8InchCst2Left.Value && _di8InchCst2Right.Value &&
                        _di8InchCst3Left.Value && _di8InchCst3Right.Value && _di8InchCstPresent.Value)
                    {
                        InfoPadCarrierIndex = 5;
                        _doIndicatorCst8Inch.SetTrigger(true, out _);
                        _doIndicatorCst6Inch.SetTrigger(false, out _);
                        return LoadportCassetteState.Normal;
                    }

                    if (_di8InchCst1Left.Value && _di8InchCst1Right.Value && _di8InchCst2Left.Value && _di8InchCst2Right.Value &&
                        !_di8InchCst3Left.Value && !_di8InchCst3Right.Value && _di8InchCstPresent.Value)
                    {
                        InfoPadCarrierIndex = 6;
                        _doIndicatorCst8Inch.SetTrigger(true, out _);
                        _doIndicatorCst6Inch.SetTrigger(false, out _);
                        return LoadportCassetteState.Normal;
                    }
                }


                if (_di8InchCst1Left.Value && _di8InchCst1Right.Value && _di8InchCst2Left.Value && _di8InchCst2Right.Value &&
                        _di8InchCst3Left.Value && _di8InchCst3Right.Value && _di8InchCstPresent.Value)
                {
                    if (_di6InchCst1Left.Value && _di6InchCst1Right.Value && !_di6InchCst2Left.Value && !_di6InchCst2Right.Value &&
                    !_di6InchAdapterLeft.Value && !_di6InchAdapterRight.Value && !_di6InchCstOption.Value && !_di6InchCst1Present.Value &&
                    _di6InchCst2Present.Value)
                    {
                        InfoPadCarrierIndex = 1;   //黑色塑料盒子
                        _doIndicatorCst8Inch.SetTrigger(false, out _);
                        _doIndicatorCst6Inch.SetTrigger(true, out _);
                        return LoadportCassetteState.Normal;
                    }
                    if (_di6InchCst1Left.Value && _di6InchCst1Right.Value && !_di6InchCst2Left.Value && !_di6InchCst2Right.Value &&
                    !_di6InchAdapterLeft.Value && !_di6InchAdapterRight.Value && _di6InchCstOption.Value && !_di6InchCst1Present.Value &&
                    _di6InchCst2Present.Value)
                    {
                        InfoPadCarrierIndex = 2;   //白色塑料盒子
                        _doIndicatorCst8Inch.SetTrigger(false, out _);
                        _doIndicatorCst6Inch.SetTrigger(true, out _);
                        return LoadportCassetteState.Normal;
                    }

                    if (!_di6InchCst1Left.Value && !_di6InchCst1Right.Value && _di6InchCst2Left.Value && _di6InchCst2Right.Value &&
                    !_di6InchAdapterLeft.Value && !_di6InchAdapterRight.Value && !_di6InchCst1Present.Value &&
                    _di6InchCst2Present.Value)
                    {
                        InfoPadCarrierIndex = 3;  //金属盒子
                        _doIndicatorCst8Inch.SetTrigger(false, out _);
                        _doIndicatorCst6Inch.SetTrigger(true, out _);
                        return LoadportCassetteState.Normal;
                    }
                }
                _doIndicatorCst8Inch.SetTrigger(false, out _);
                _doIndicatorCst6Inch.SetTrigger(false, out _);
                InfoPadCarrierIndex = 0;
                return LoadportCassetteState.Absent;
            }
        }
        public override string SpecCarrierType
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.CarrierName{InfoPadCarrierIndex}"))
                    return SC.GetStringValue($"CarrierInfo.CarrierName{InfoPadCarrierIndex}");
                return "";
            }
            set => base.SpecCarrierType = value;
        }

        public override WaferSize GetCurrentWaferSize()
        {
            switch(InfoPadCarrierIndex)
            {
                case 1:
                case 2:
                case 3:
                    return WaferSize.WS6;
                case 4:
                case 5:
                case 6:
                    return WaferSize.WS8;
                default:
                    return WaferSize.WS0;
            }
        }
        public string Address { get => ""; }

        public bool IsConnected => true;
      

        public OpenStageHHFn(string module, string name, IoSensor[] dis, IoTrigger[] indicators, RobotBaseDevice mapRobot) : base(
            module, name, mapRobot)
        {
            if (dis == null)
                throw new ArgumentException("DI cannot be null", "diPresent");

            if (indicators == null)
                throw new ArgumentException("DO indicators cannot be null", "diNoWaferProtrude");

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name cannot be null or empty", "name");

            _di6InchCst1Left = dis[0];
            _di6InchCst1Right = dis[1];
            _di6InchCst2Left = dis[2];
            _di6InchCst2Right = dis[3];
            _di6InchAdapterLeft = dis[4];
            _di6InchAdapterRight = dis[5];
            _di6InchCstOption = dis[6];  //On 为 2
            _di6InchCst1Present = dis[7];
            _di6InchCst2Present = dis[8];
            

            _di8InchCst1Left = dis[9];
            _di8InchCst1Right=dis[10];
            _di8InchCst2Left = dis[11];
            _di8InchCst2Right = dis[12];
            _di8InchCst3Left = dis[13];
            _di8InchCst3Right = dis[14];
            _di8InchCstPresent = dis[15];
            _di6InchWaferProtrude = dis[16];
            _di8InchWaferProtrude = dis[17];


            _doIndicatorCstReady = indicators[0]; 
            _doIndicatorCst6Inch = indicators[1]; 
            _doIndicatorCst8Inch = indicators[2]; 
            _doIndicatorCstRunning = indicators[3]; 
            _doIndicatorCstComplete = indicators[4]; 
            _doIndicatorCstAlarm = indicators[5]; 

            IsMapWaferByLoadPort = false;
            PortType = EnumLoadPortType.OpenStage;
            LoadPortType = "OpenStage";
            Initalized = true;
            IsBusy = false;
            Error = false;

            
            DockState = FoupDockState.Docked;
            DoorState = FoupDoorState.Open;
            
            LoadportReset(out _);

        }

        private bool SetAndMap(object[] param, out string reason)
        {
            _isLoaded = true;
            //InfoPadCarrierIndex = Convert.ToInt16(param[0]);
            return MapWafer(out reason);
        }

        private bool SetAndLoad(object[] param, out string reason)
        {
            InfoPadCarrierIndex = Convert.ToInt16(param[0]);
            _isLoaded = true;
            reason = "";
            return true;
            //return Load(out reason);
        }

        public override bool IsLoaded => _isLoaded;



        protected override bool fStartExecute(object[] param)
        {
            try
            {
                switch (param[0].ToString())
                {
                    case "MapWafer":
                        if (!IsMapWaferByLoadPort)
                        {
                            string reason = "";
                            if (!IsEnableMapWafer(out reason))
                            {
                                EV.PostAlarmLog("LoadPort", $"{LPModuleName} is not ready to map wafer:{reason}");
                                return false;
                            }
                            if (MapRobot != null)
                                return MapRobot.WaferMapping(LPModuleName, out _);
                            return false;
                        }
                        break;
                }
                IsBusy = false;
                return false;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                EV.PostAlarmLog(Name, $"Parameter invalid");
                return false;

            }
        }
        public override void Monitor()
        {

            if (CassetteState == LoadportCassetteState.Absent)
            {
                _trigPresentAbsent.CLK = false;
                _isLoaded = false;
            }
            if (CassetteState == LoadportCassetteState.Normal)
            {
                _trigPresentAbsent.CLK = true;
                if (CurrentState == LoadPortStateEnum.Idle)
                {
                    _doIndicatorCstReady.SetTrigger(true, out _);
                    _doIndicatorCstRunning.SetTrigger(false, out _);                    
                }
                else
                {
                    _doIndicatorCstReady.SetTrigger(true, out _);
                    _doIndicatorCstRunning.SetTrigger(true, out _);

                }
            }

            if (_trigPresentAbsent.R)
            {
                SetPresent(true);
                SetPlaced(true);

            }

            if (_trigPresentAbsent.T)
            {
                SetPresent(false);
                SetPlaced(false);
            }

               




            base.Monitor();
        }

      
        
        


        public override void Reset()
        {
            base.Reset();

            //_trigWaferProtrude.RST = true;
        }
        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            return true;
        }

        public override bool IsEnableMapWafer(out string reason)
        {
            if (CurrentState == LoadPortStateEnum.Error)
            {
                reason = "In Error State";
                return false;
            }
            if (CassetteState != LoadportCassetteState.Normal)
            {
                reason = "no FOUP placed";
                return false;
            }

            switch (InfoPadCarrierIndex)
            {
                case 1:
                case 2:
                case 3:
                    if (_di6InchWaferProtrude.Value)
                    {
                        reason = "Wafer Protrude";
                        return false;
                    }
                    break;
                case 4:
                case 5:
                case 6:
                    if (_di8InchWaferProtrude.Value)
                    {
                        reason = "Wafer Protrude";
                        return false;
                    }
                    break;
                default:
                    break;
            }


            if (!_isLoaded)
            {
                reason = "Cassette is not loaded";
                return false;
            }            
            reason = "";
            return true;
        }
        public override bool MapWafer(out string reason)
        {
            _isLoaded = true;

            _trigWaferProtrude.RST = true;
            return base.MapWafer(out reason);
        }

        public override bool Load(out string reason)
        {
            _isLoaded = true;
            reason = "";
            _trigWaferProtrude.RST = true;
            return true;
        }

        public override bool IsEnableLoad(out string reason)
        {
            if (CurrentState == LoadPortStateEnum.Error)
            {
                reason = "In Error State";
                return false;
            }
            if (CassetteState != LoadportCassetteState.Normal)
            {
                reason = "no FOUP placed";
                return false;
            }         
            reason = "";
            return true;
        }
        public override bool IsEnableTransferWafer(out string reason)
        {
            if (CurrentState == LoadPortStateEnum.Error)
            {
                reason = "In Error State";
                return false;
            }
            if (CassetteState != LoadportCassetteState.Normal)
            {
                reason = "no FOUP placed";
                return false;
            }
          
            if (!_isLoaded)
            {
                reason = "Cassette is not loaded";
                return false;
            }

            if (!_isMapped)
            {
                reason = "FOUP not mapped";
                return false;
            }
            switch (InfoPadCarrierIndex)
            {
                case 1:
                case 2:
                case 3:
                    if (_di6InchWaferProtrude.Value)
                    {
                        reason = "Wafer Protrude";
                        return false;
                    }
                    break;
                case 4:
                case 5:
                case 6:
                    if (_di8InchWaferProtrude.Value)
                    {
                        reason = "Wafer Protrude";
                        return false;
                    }
                    break;
                default:
                    break;
            }

            foreach (var wafer in WaferManager.Instance.GetWafers(LPModuleName))
            {
                if (wafer.IsEmpty) continue;
                if (wafer.Status == WaferStatus.Crossed)
                {
                    reason = "Crossed wafer";
                    return false;
                }
                if (wafer.Status == WaferStatus.Double)
                {
                    reason = "Double wafer";
                    return false;
                }
            }


            reason = "";
            return true;
        }
        
        public override bool IsForbidAccessSlotAboveWafer()
        {
            return false;
        }

        protected override bool fStartUnload(object[] para)
        {
            if (!_isPlaced) return false;
            return true;
        }


        protected override bool fMonitorUnload(object[] param)
        {
            IsBusy = false;
            _isLoaded = false;
            return true;
        }

        public override bool SetIndicator(Indicator light, IndicatorState state, out string reason)
        {
            reason = "";
            switch (light)
            {
                case Indicator.LOAD:

                    break;
                case Indicator.UNLOAD:

                    break;
                case Indicator.ACCESSAUTO:

                    break;
                case Indicator.ALARM:                    
                    break;
                default:
                    reason = "Not support";
                    return false;
            }
            return true;
        }

        public bool Disconnect()
        {
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


        protected override bool fStartLoad(object[] param)
        {
            string reason;
            
            _isLoaded = true;
            IsBusy = false;
            return true;

        }

        protected override bool fMonitorLoad(object[] param)
        {
            IsBusy = false;
            return true;
        }


        protected override bool fStartInit(object[] param)
        {
            return true;
        }
        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            return true;
        }
        protected override bool fStartReset(object[] param)
        {

            return true;
        }


    }
}
