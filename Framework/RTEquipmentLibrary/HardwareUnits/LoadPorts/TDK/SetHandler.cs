using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK
{
    public class ResetHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }

        public ResetHandler()
        {
            background = true;
        }
        public string package(params object[] args)
        {
            return "SET:RESET";
        }

        public string retry()
        {
            return "RST:RESET";
        }

        public bool unpackage(string type, string[] cmds)
        {
            if (type.Equals("INF"))              
                return true;

            return false;
        }

        public bool canhandle(string id)
        {
            return id.Equals("RESET");
        }
    }

    /// <summary>
    /// 将LoadPort切换为FOSB模式
    /// </summary>
    public class FOSBModeHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }

        public FOSBModeHandler()
        {
            background = true;
        }
        public string package(params object[] args)
        {
            return "SET:FSBON";
        }

        public string retry()
        {
            return "RST:FSBON";
        }

        public bool unpackage(string type, string[] cmds)
        {
            if (type.Equals("ACK"))              
                return true;

            return false;
        }

        public bool canhandle(string id)
        {
            return id.Equals("FSBON");
        }
    }
    
    /// <summary>
    /// 将LoadPort切换为FOUP模式
    /// </summary>
    public class FOUPModeHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }

        public FOUPModeHandler()
        {
            background = true;
        }
        public string package(params object[] args)
        {
            return "SET:FSBOF";
        }

        public string retry()
        {
            return "RST:FSBOF";
        }

        public bool unpackage(string type, string[] cmds)
        {
            if (type.Equals("ACK"))              
                return true;

            return false;
        }

        public bool canhandle(string id)
        {
            return id.Equals("FSBOF");
        }
    }
    
    public class IndicatorHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }

        private string[] _opstring = { "LON", "LBL", "LOF" };
        private string _cmd =" ";
        Indicator light;
        IndicatorState func;

        public IndicatorHandler()
        {
            background = true;
        }
        public string package(params object[] args)
        {
            light = (Indicator)args[0];
            func = (IndicatorState)args[1];
            _cmd = indicator(light,func);
            return string.Format("SET:{0}", _cmd);
        }
        public string retry()
        {
            return string.Format("RST:{0}", _cmd);
        }

        public bool unpackage(string type, string[] cmds)
        {
            if (type.Equals("INF"))
            {
                SetLight(light,func);

                TDKLoadPort device = DEVICE.GetDevice<TDKLoadPort>(deviceID);
                string reason = string.Empty;
       //         device.QueryIndicator(out reason);
                return true;
            }
            return false;
        }

        public bool canhandle(string id)
        {
            return id.Equals(_cmd);
        }

        private string indicator(Indicator light, IndicatorState op)
        {
            return string.Format("{0}{1:D2}", _opstring[(int)op-1], (int)light);
        }

        private void SetLight(Indicator light, IndicatorState op)
        {
            TDKLoadPort device = DEVICE.GetDevice<TDKLoadPort>(deviceID);
            switch (light)
            { 
                case Indicator.LOAD:
                    device.IndicatiorLoad = op;
                    break;
                case Indicator.UNLOAD:
                    device.IndicatiorUnload = op;
                    break;
                case Indicator.ACCESSAUTO:
                    device.IndicatiorAccessAuto = op;
                    break;
                case Indicator.ACCESSMANUL:
                    device.IndicatiorAccessManual = op;
                    break;
                case Indicator.PLACEMENT:
                    device.IndicatiorPlacement = op;
                    break;
                case Indicator.PRESENCE:
                    device.IndicatiorPresence = op;
                    break;
            }
        }
    }
}
