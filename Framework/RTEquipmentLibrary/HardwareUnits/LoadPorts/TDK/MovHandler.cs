using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK
{
    public class MovHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }

        public string _cmd = string.Empty;
        TDKLoadPort _device = null;
        public MovHandler()
        {
            background = true;
        }
        public string package(params object[] args)
        {
            MovType type = (MovType)args[0];
            _cmd = type.ToString();

            return string.Format("MOV:{0}", _cmd);
        }
        public string retry()
        {
            return string.Format("RMV:{0}", _cmd);
        }


        public bool unpackage(string type, string[] cmds)
        {
            if (!type.Equals("INF"))
                return false;

            _device = DEVICE.GetDevice<TDKLoadPort>(deviceID);
            if (_cmd == MovType.CULOD.ToString() || _cmd == MovType.CULDK.ToString())
                _device.OnUnloaded(); 
            if (_cmd == MovType.CLDDK.ToString() || _cmd == MovType.CLDMP.ToString() || _cmd == MovType.CLOAD.ToString())
                _device.OnLoaded(); 
       
                
             return true;
        }

        public bool canhandle(string id)
        {
            return id.Equals(_cmd);
        }
    }



    public class HomeHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }
        TDKLoadPort _device = null;
        private string _cmd = string.Empty;
        public HomeHandler()
        {
            background = true;

        }
        public string package(params object[] args)
        {
            _cmd = MovType.ORGSH.ToString();
            return string.Format("MOV:{0}", _cmd);
        }
        public string retry()
        {
            return string.Format("RMV:{0}", _cmd);
        }


        public bool unpackage(string type, string[] cmds)
        {
            if (!type.Equals("INF"))
                return false;

            _device = DEVICE.GetDevice<TDKLoadPort>(deviceID);
            _device.Initalized = true;

            _device.OnHomed();
            
            return true;
        }

        public bool canhandle(string id)
        {
            return id.Equals(_cmd);
        }
    }
        
    public class ForceHomeHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }
        TDKLoadPort _device = null;
        private string _cmd = string.Empty;
        public ForceHomeHandler()
        {
            background = true;

        }
        public string package(params object[] args)
        {
            _cmd = MovType.ABORG.ToString();
            return string.Format("MOV:{0}", _cmd);
        }
        public string retry()
        {
            return string.Format("RMV:{0}", _cmd);
        }


        public bool unpackage(string type, string[] cmds)
        {
            if (!type.Equals("INF"))
                return false;

            _device = DEVICE.GetDevice<TDKLoadPort>(deviceID);
            _device.Initalized = true;

            _device.OnHomed();
            
            return true;
        }

        public bool canhandle(string id)
        {
            return id.Equals(_cmd);
        }
    }

    /// <summary>
    /// 单步移动到Dock位置动作 指令为YDOOR
    /// </summary>
    public class DockPosHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }
        TDKLoadPort _device = null;
        private string _cmd = string.Empty;
        public DockPosHandler()
        {
            background = true;

        }
        public string package(params object[] args)
        {
            _cmd = MovType.YDOOR.ToString();
            return string.Format("MOV:{0}", _cmd);
        }
        public string retry()
        {
            return string.Format("RMV:{0}", _cmd);
        }


        public bool unpackage(string type, string[] cmds)
        {
            if (!type.Equals("INF"))
                return false;
            
            _device = DEVICE.GetDevice<TDKLoadPort>(deviceID);
            _device.ClampState = FoupClampState.Close;
            return true;
        }

        public bool canhandle(string id)
        {
            return id.Equals(_cmd);
        }
    }
    
    /// <summary>
    /// 单步移动到Un Dock位置动作 指令为YWAIT
    /// </summary>
    public class UnDockPosHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }
        TDKLoadPort _device = null;
        private string _cmd = string.Empty;
        public UnDockPosHandler()
        {
            background = true;

        }
        public string package(params object[] args)
        {
            _cmd = MovType.YWAIT.ToString();
            return string.Format("MOV:{0}", _cmd);
        }
        public string retry()
        {
            return string.Format("RMV:{0}", _cmd);
        }


        public bool unpackage(string type, string[] cmds)
        {
            if (!type.Equals("INF"))
                return false;
            
            _device = DEVICE.GetDevice<TDKLoadPort>(deviceID);
            _device.DockPOS = TDKY_AxisPos.Undock;
            
            return true;
        }

        public bool canhandle(string id)
        {
            return id.Equals(_cmd);
        }
    }
    
    /// <summary>
    /// 单步开门动作 指令为DORBK
    /// </summary>
    public class DoorOpenHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }
        //TDKLoadPort _device = null;
        private string _cmd = string.Empty;
        public DoorOpenHandler()
        {
            background = true;

        }
        public string package(params object[] args)
        {
            _cmd = MovType.DORBK.ToString();
            return string.Format("MOV:{0}", _cmd);
        }
        public string retry()
        {
            return string.Format("RMV:{0}", _cmd);
        }


        public bool unpackage(string type, string[] cmds)
        {
            if (!type.Equals("INF"))
                return false;
            
            return true;
        }

        public bool canhandle(string id)
        {
            return id.Equals(_cmd);
        }
    }
    
    /// <summary>
    /// 单步关门动作 指令为DORFW
    /// </summary>
    public class DoorCloseHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }
        //TDKLoadPort _device = null;
        private string _cmd = string.Empty;
        public DoorCloseHandler()
        {
            background = true;

        }
        public string package(params object[] args)
        {
            _cmd = MovType.DORFW.ToString();
            return string.Format("MOV:{0}", _cmd);
        }
        public string retry()
        {
            return string.Format("RMV:{0}", _cmd);
        }


        public bool unpackage(string type, string[] cmds)
        {
            if (!type.Equals("INF"))
                return false;
            
            return true;
        }

        public bool canhandle(string id)
        {
            return id.Equals(_cmd);
        }
    }
    
    /// <summary>
    /// 单步门下降动作 指令为ZDRDW
    /// </summary>
    public class DoorDownHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }
        TDKLoadPort _device = null;
        private string _cmd = string.Empty;
        public DoorDownHandler()
        {
            background = true;

        }
        public string package(params object[] args)
        {
            _cmd = MovType.ZDRDW.ToString();
            return string.Format("MOV:{0}", _cmd);
        }
        public string retry()
        {
            return string.Format("RMV:{0}", _cmd);
        }


        public bool unpackage(string type, string[] cmds)
        {
            if (!type.Equals("INF"))
                return false;

            _device = DEVICE.GetDevice<TDKLoadPort>(deviceID);
            _device.DoorState = FoupDoorState.Open;
            
            return true;
        }

        public bool canhandle(string id)
        {
            return id.Equals(_cmd);
        }
    }
    
    /// <summary>
    /// 单步门上升动作 指令为ZDRUP
    /// </summary>
    public class DoorUpHandler : IMsg
    {
        public bool background { get; private set; }
        public string deviceID { private get; set; }
        TDKLoadPort _device = null;
        private string _cmd = string.Empty;
        public DoorUpHandler()
        {
            background = true;

        }
        public string package(params object[] args)
        {
            _cmd = MovType.ZDRUP.ToString();
            return string.Format("MOV:{0}", _cmd);
        }
        public string retry()
        {
            return string.Format("RMV:{0}", _cmd);
        }


        public bool unpackage(string type, string[] cmds)
        {
            if (!type.Equals("INF"))
                return false;

            _device = DEVICE.GetDevice<TDKLoadPort>(deviceID);
            _device.DoorState = FoupDoorState.Close;
            
            return true;
        }

        public bool canhandle(string id)
        {
            return id.Equals(_cmd);
        }
    }
}
