using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using System;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK
{

    public class QueryStateHandler : IMsg
    {
        /// <summary>

        /// / (1) (2) (3) (4) (5) (6) (7) (8) (9) (10) (11) (12) (13) (14) (15) (16) (17) (18) (19) (20)
        /// (1) Equipment status 0 = Normal A = Recoverable error E = Fatal error
        ///(5)Error code Error code(upper)
        ///(6)Error code Error code(lower)
        ///(7)Cassette presence 0 = None 1 = Normal position 2 = Error load
        ///(8)FOUP clamp status 0 = Open 1 = Close? = Not defined
        ///(11)Door position 0 = Open position 1 = Close position ? = Not defined
        /// </summary>

        public bool background { get; private set; }
        public string deviceID { private get; set; }

        private string _cmd = string.Empty;
        public QueryStateHandler()
        {
            background = false;
        }

        public string package(params object[] args)
        {
            _cmd = QueryType.STATE.ToString();
            return string.Format("GET:{0}", _cmd);
        }
        public string retry()
        {
            return string.Format("RFN:{0}", _cmd);
        }

        public bool unpackage(string type, string[] items)
        {
            if (!type.Equals("ACK"))
                return false;

            string state = items[1];

            TDKLoadPort device = DEVICE.GetDevice<TDKLoadPort>(deviceID);
            device.ErrorCode = state.Substring(4, 2);

            if (state[0] != '0')
            {
                device.Error = true;

                device.ErrorCode = state.Substring(4,2);
                string error = Singleton<ErrorCode>.Instance.Code2Msg(device.ErrorCode);
                EV.PostMessage(deviceID, EventEnum.DefaultWarning, string.Format("{0} has error, error is {1}", deviceID, error));
                EV.Notify(device.AlarmLoadPortError, new SerializableDictionary<string, object> {
                {"AlarmText",string.Format(error)}
                });
            }
            else
            {
                device.Error = false; 
            }
         
            
            device.ClampState = getClampState(state[7]);
            device.DoorState = getDoorState(state[10]);
            device.DockPOS = GetDockPos(state[13]);
            device.DockState = GetFoupDockPos(state[13]);

            device.DoorPOS = GetDoorPos(state[12]);
            LOG.Write($"Get State:{state}");
            if (SC.ContainsItem("CarrierInfo.InfoPadType") && (SC.GetValue<int>("CarrierInfo.InfoPadType") == 0))    //0 is TDK sensor, 1 is for external sensors.
            {
                int infopadstatus = Convert.ToInt16($"0x{state[19]}", 16);

                if ((infopadstatus & 1) != 0) device.IsInfoPadAOn = true;
                else device.IsInfoPadAOn = false;
                if ((infopadstatus & 2) != 0) device.IsInfoPadBOn = true;
                else device.IsInfoPadBOn = false;
                if ((infopadstatus & 4) != 0) device.IsInfoPadCOn = true;
                else device.IsInfoPadCOn = false;
                if ((infopadstatus & 8) != 0) device.IsInfoPadDOn = true;
                else device.IsInfoPadDOn = false;


                int cindex = (device.IsInfoPadAOn ? 8 : 0) + (device.IsInfoPadBOn ? 4 : 0) + (device.IsInfoPadCOn ? 2 : 0) + (device.IsInfoPadDOn ? 1 : 0);
                device.InfoPadCarrierIndex = cindex;
                if (getCassetteState(state[6]) == LoadportCassetteState.Normal)
                {
                    if (SC.ContainsItem($"CarrierInfo.CarrierName{cindex}"))
                        device.InfoPadCarrierType = SC.GetStringValue($"CarrierInfo.CarrierName{cindex}");
                }
                else
                    device.InfoPadCarrierType = "";
                if (SC.ContainsItem($"CarrierInfo.{device.Name}CarrierIndex"))
                    SC.SetItemValue($"CarrierInfo.{device.Name}CarrierIndex", cindex);
                if (SC.ContainsItem($"CarrierInfo.CarrierFosbMode{cindex}"))
                {
                    int fosbmode = SC.GetValue<int>($"CarrierInfo.CarrierFosbMode{cindex}");
                    if (fosbmode >= 0 && fosbmode <= 1)
                        device.CasstleType = (CasstleType)fosbmode;
                }

                LOG.Write($"{device.Name} detect carrier Type:{device.InfoPadCarrierType}");
            }
            if (SC.ContainsItem("CarrierInfo.InfoPadType") && (SC.GetValue<int>("CarrierInfo.InfoPadType") == 2))    //0 is TDK sensor, 1 is for external sensors.
            {
                int cindex = SC.GetValue<int>($"CarrierInfo.{device.Name}CarrierIndex");
                device.InfoPadCarrierIndex = cindex;
                if (SC.ContainsItem($"CarrierInfo.CarrierName{cindex}"))
                    device.InfoPadCarrierType = SC.GetStringValue($"CarrierInfo.CarrierName{cindex}");
                
                if (SC.ContainsItem($"CarrierInfo.CarrierFosbMode{cindex}"))
                {
                    int fosbmode = SC.GetValue<int>($"CarrierInfo.CarrierFosbMode{cindex}");
                    if (fosbmode >= 0 && fosbmode <= 1)
                        device.CasstleType = (CasstleType)fosbmode;
                    if(device.CasstleType == CasstleType.FOSB)                    
                        device.IsMapWaferByLoadPort = false;
                    else
                        device.IsMapWaferByLoadPort = true;

                }

                LOG.Write($"{device.Name} detect carrier Type:{device.InfoPadCarrierType}");
            }

            device.SetCassetteState(getCassetteState(state[6]));

            string reason = string.Empty;            

            device.OnStateUpdated();

            return true;
        }

        public bool canhandle(string id)
        {
            return id.Equals(_cmd);
        }




        //(7) Cassette presence 0 = None 1 = Normal position 2 = Error load
        private LoadportCassetteState getCassetteState(char x)
        {
            if (x == '0')
                return LoadportCassetteState.Absent;
            if (x == '1')
                return LoadportCassetteState.Normal;

            return LoadportCassetteState.Absent;
        }
        //    (8) FOUP clamp status 0 = Open 1 = Close ? = Not defined
        private FoupClampState getClampState(char x)
        {
            if (x == '0')
                return FoupClampState.Open;
            if (x == '1')
                return FoupClampState.Close;

            return FoupClampState.Unknown;
        }

        private TDKY_AxisPos GetDockPos(char x)
        {
            if (x == '0')
                return TDKY_AxisPos.Undock;
            if (x == '1')
                return TDKY_AxisPos.Dock;

            return TDKY_AxisPos.Unknown;
        }
        private FoupDockState GetFoupDockPos(char x)
        {
            if (x == '0')
                return  FoupDockState.Undocked;
            if (x == '1')
                return FoupDockState.Docked;

            return FoupDockState.Unknown;
        }
        private TDKZ_AxisPos GetDoorPos(char x)
        {
            if (x == '0')
                return TDKZ_AxisPos.Up;
            if (x == '1')
                return TDKZ_AxisPos.Down;
            if (x == '2')
                return TDKZ_AxisPos.Start;
            if (x == '3')
                return TDKZ_AxisPos.End;
            return TDKZ_AxisPos.Unknown;
        }

        //(11) Door position 0 = Open position 1 = Close position ? = Not defined
        private FoupDoorState getDoorState(char x)
        {
            if (x == '0')
                return FoupDoorState.Open;
            if (x == '1')
                return FoupDoorState.Close;

            return FoupDoorState.Unknown;
        }
    }

    public class QueryIndicatorHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }

        private string _cmd = string.Empty;
        public QueryIndicatorHandler()
        {
            background = false;
        }
        public string package(params object[] args)
        {
            _cmd = QueryType.LEDST.ToString();
            return string.Format("GET:{0}", _cmd);
        }
        public string retry()
        {
            return string.Format("RFN:{0}", _cmd);
        }

        public bool unpackage(string type, string[] items)
        {
            if (!type.Equals("ACK"))
                return false;

            string state = items[1];

            TDKLoadPort device = DEVICE.GetDevice<TDKLoadPort>(deviceID);


            if (state.Length == 7)
            {
                device.IndicatiorPresence = getIndicatorState(state[0]);
                device.IndicatiorPlacement = getIndicatorState(state[1]);
                device.IndicatorAlarm = getIndicatorState(state[2]);
                device.IndicatiorLoad = getIndicatorState(state[3]);
                device.IndicatiorUnload = getIndicatorState(state[4]);
                device.IndicatiorOpAccess = getIndicatorState(state[5]);
                device.IndicatiorStatus1 = getIndicatorState(state[6]);
            }
            else
            {
                device.IndicatiorLoad = getIndicatorState(state[0]);
                device.IndicatiorUnload = getIndicatorState(state[1]);
                device.IndicatiorAccessManual = getIndicatorState(state[2]);
                device.IndicatiorPresence = getIndicatorState(state[3]);
                device.IndicatiorPlacement = getIndicatorState(state[4]);
                device.IndicatiorAccessAuto = getIndicatorState(state[5]);
                device.IndicatorAlarm = getIndicatorState(state[7]);


                device.IndicatiorStatus1 = getIndicatorState(state[6]);
                device.IndicatiorStatus2 = getIndicatorState(state[8]);
            }

            return true;
        }



        public bool canhandle(string id)
        {
            return id.Equals(_cmd);
        }


        private IndicatorState getIndicatorState(char x)
        {
            if (x == '0')
                return IndicatorState.OFF;
            if (x == '1')
                return IndicatorState.ON;

            return IndicatorState.BLINK;
        }

    }
    
    public class QueryWaferMappingHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }

        private string _cmd = string.Empty;


        public QueryWaferMappingHandler()
        {
            background = false;
        }
        public string package(params object[] args)
        {        
            _cmd = QueryType.MAPRD.ToString();

            return string.Format("GET:{0}", _cmd);
        }
        public string retry()
        {
            return string.Format("RFN:{0}", _cmd);
        }

        public bool unpackage(string type, string[] items)
        {
            if (!type.Equals("ACK"))
                return false;

            TDKLoadPort device = DEVICE.GetDevice<TDKLoadPort>(deviceID);
            device.OnSlotMapRead(items[1]);

            string reason = string.Empty;
            device.QueryState(out reason);
        
            return true;
        }

        public bool canhandle(string id)
        {
            return id.Equals(_cmd);
        }


    }

    /// <summary>
    /// 查询FOSB模式是否激活
    /// </summary>
    public class QueryFOSBModeHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }

        private string _cmd = string.Empty;
        public QueryFOSBModeHandler()
        {
            background = false;
        }
        public string package(params object[] args)
        {
            _cmd = QueryType.FSBxx.ToString();
            return string.Format("GET:{0}", _cmd);
        }
        public string retry()
        {
            return string.Format("RFN:{0}", _cmd);
        }

        public bool unpackage(string type, string[] items)
        {
            if (!type.Equals("ACK"))
                return false;

            string modeState = items[1];

            TDKLoadPort device = DEVICE.GetDevice<TDKLoadPort>(deviceID);

            switch (modeState)
            {
                case "ON":
                    device.IsFOSBMode = true;                    
                    break;
                case "OF":
                    device.IsFOSBMode = false;                    
                    break;
                default:
                    device.IsFOSBMode = false;
                    break;
            }
            return true;
        }

        
        public bool canhandle(string id)
        {
            return id.Equals(_cmd);
        }

        
    }

}
