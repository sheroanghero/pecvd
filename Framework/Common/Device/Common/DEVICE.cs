using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.Util;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Device;


namespace Aitex.Core.RT.Device
{
    /// <summary>
    /// DEVICE不容易满足模块化的需求，基本不再使用
    /// </summary>
    //[Obsolete("DEVICE不再使用")]
    public class DEVICE
    {
        //public static IDeviceManager Manager { set; private get; }
 
        public delegate bool? DeviceFunc(out string reason, int time, params object[] param);
	
        private static object _locker = new object();
        private static Dictionary<string, DeviceFunc> deviceFuncs = new Dictionary<string, DeviceFunc>();

        public static string Module = "Device";

        public static List<IDeviceManager> Managers { set; get; } = new List<IDeviceManager>();
 
        //[Obsolete("DEVICE不再使用")]
        public static void Register(string name, DeviceFunc func)
        {
            lock (_locker)
            {
                if(!deviceFuncs.ContainsKey(name))                
                    deviceFuncs.Add(name, func);
            }
        }
 
        public static void UnRegister(string name)
        {
            lock (_locker)
            {
                deviceFuncs.Remove(name);
            }
        }
 
        public static bool CanDo(string name)
        {
            return deviceFuncs.ContainsKey(name);
        }

 
        public static bool? Do(string name, int time, bool isClientCmd, params object[] param)
        {
            bool? ret = false;
            string reason;

            string cmd = name;

            lock (_locker)
            {
                try
                {
 
                    ret = deviceFuncs[name].Invoke(out reason, time, param);

                    string result = string.Format("Execute：{0}，{1}", name, reason);
 
                    if (ret.HasValue && !ret.Value)
                    {
                        string msg = String.Format("Failed to do {0}, {1}", name, reason);
                        EV.PostMessage(Module, EventEnum.GuiCmdExecFailed, Module, msg);
                    }
                    else
                    {
                        EV.PostMessage(Module, EventEnum.GuiCmdExecSucc, Module, result);
                    }
                }
                catch (Exception ex)
                {
                    LOG.Write(ex, string.Format("Device:执行{0}命令发生异常",name));
                    ret = false;
                }
            }
            return ret;
        }

        public static List<IDevice> GetAllDevice( )
        {
            List < IDevice > result = new List<IDevice>();
            foreach (var deviceManager in Managers)
            {
                result.AddRange( deviceManager.GetAllDevice());
            }

            return result;
        }

        public static T GetDevice<T>(string name) where T : class, IDevice
        {
            foreach (var deviceManager in Managers)
            {
                if (deviceManager.ContainsDevice(name))
                    return deviceManager.GetDevice<T>(name);
            }

            return null;
        }

        public static object GetDevice(string name)
        {
            foreach (var deviceManager in Managers)
            {
                if (deviceManager.ContainsDevice(name))
                    return deviceManager.GetDevice(name);
            }

            return null;
        }

        public static List<T> GetDevice<T>() where T : class, IDevice
        {
            List<T> result = new List<T>();
            foreach (var deviceManager in Managers)
            {
                result.AddRange(deviceManager.GetDevice<T>());
            }

            return result;
        }

        public static object GetOptionDevice(string name, Type type)
        {
            foreach (var deviceManager in Managers)
            {
                if (deviceManager.GetOptionDevice(name, type) != null)
                    return deviceManager.GetOptionDevice(name, type);
            }

            return null;
        }
    }
}
