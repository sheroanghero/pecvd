using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK
{
    public class SetEventHandler : IMsg   //common move
    {
        public bool background { get; private set;}
        public string deviceID { private get;  set; }

        private string _cmd = string.Empty;
        public SetEventHandler()
        {
            background = false;
        }
        public string package(params object[] args)
        {
            _cmd = args[0].ToString();
            return string.Format("EVT:{0}", _cmd);
        }

        public string retry()
        {
            return string.Format("RFN:{0}", _cmd);
        }

        public bool unpackage(string type, string[] cmds)
        {
            if (!type.Equals("ACK"))
                return false;

            return true;
        }

        public bool canhandle(string id)
        {
            return id.Equals(_cmd);
        }
    }

    public class OnEventHandler : IMsg   //common move
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }

        private string _cmd = string.Empty;
        public OnEventHandler()
        {
            background = false;
        }
        public string package(params object[] args)
        {
            _cmd = args[0].ToString();
            return "";
        }

        public string retry()
        {
            return "";
        }

        public bool unpackage(string type, string[] items)
        {
            TDKLoadPort device = DEVICE.GetDevice<TDKLoadPort>(deviceID);

            if (type.Equals("INF") || type.Equals("RIF") )
            {
                string name = items[0];
                
                switch (name)
                {
                    case "PODON":   // PODON The FOUP is moved from no load to the normal position.                        
                    {
                        device.OnCarrierPresent();
                        device.OnCarrierPlaced();

                        string reason = string.Empty;
                        device.OnEvent(out reason);
                    }
                        break;
                    case "PODOF":   //PODOF The FOUP is moved from normal position to no load.                   
                    {
                        device.OnCarrierNotPlaced(); 
                        device.OnCarrierNotPresent();

                        string reason = string.Empty;
                        device.OnEvent(out reason);
                    }
                        break;
                    case "ABNST":   //PODOF The FOUP is moved from normal position to no load.                        
                    {
                        device.OnCarrierNotPlaced();
                        //device.OnCarrierPresent();

                        string reason = string.Empty;
                        device.OnEvent(out reason);
                    }
                        break;
                    case "SMTON":
                        {
                            device.OnCarrierNotPlaced();
                            //device.OnCarrierPresent();

                            string reason = string.Empty;
                            device.OnEvent(out reason);
                        }
                        break;
                    case "FANST":
                        device.FFUIsOK = false;
                        break;
                    case "MANSW":
                        EV.PostMessage(device.Module, EventEnum.ManualOpAccess, device.Display);

                        device.OnSwitchKey1();
                        break;
                    case "MA2SW":
                        device.OnSwitchKey2();
                        break;
                    case "MANOF":
                        device.OffSwitchKey1();
                        break;
                    case "MA2OF":
                        device.OffSwitchKey2();
                        break;
                    case "ITLOF":
                        device.UnlockKey = false;
                        break;
                    case "ITLON":
                        device.UnlockKey = true;
                        break;
                    case "IPAON":
                        device.IsInfoPadAOn = true;
                        break;
                    case "IPBON":
                        device.IsInfoPadBOn = true;
                        break;
                    case "IPCON":
                        device.IsInfoPadCOn = true;
                        break;
                    case "IPDON":
                        device.IsInfoPadDOn = true;
                        break;
                    case "IPAOF":
                        device.IsInfoPadAOn = false;
                        break;
                    case "IPBOF":
                        device.IsInfoPadBOn = false;
                        break;
                    case "IPCOF":
                        device.IsInfoPadCOn = false;
                        break;
                    case "IPDOF":
                        device.IsInfoPadDOn = false;
                        break;
                    default:
                        LOG.Write(string.Format("Not handled event {0} from LP {1}", name, device.DeviceID));
                        break;

                }

                return true;
            }
            else if (type.Equals("ABS") || type.Equals("RAS"))
            {
                LOG.Write(string.Format("Received {0} event from {1}", type, device.DeviceID));
            }

            return false;
        }

        public bool canhandle(string id)
        {
            return true;
        }
    }
}
