namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK
{
    public class ModHandler : IMsg   //common move
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }

        private string _cmd = string.Empty;
        public ModHandler()
        {
            background = false;
        }
        public string package(params object[] args)
        {
            _cmd = args[0].ToString();
            return string.Format("MOD:{0}", _cmd);
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


}
