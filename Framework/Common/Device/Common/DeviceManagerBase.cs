using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;

namespace Aitex.Core.RT.Device
{
    public class DeviceManagerBase : IDeviceManager
    {
        private Dictionary<string, IDevice> _nameDevice = new Dictionary<string, IDevice>();
        private Dictionary<Type, List<IDevice>> _typeDevice = new Dictionary<Type, List<IDevice>>();

        protected XmlElement DeviceModelNodes { get; private set; }

        DeviceTimer _peformanceTimer = new DeviceTimer();
        R_TRIG _trigExceed500 = new R_TRIG();

        public bool DisableAsyncInitialize { get; set; }


        private List<IDevice> _optionDevice = new List<IDevice>();

        private object _lockerDevice = new object();

        //如果为true，key=module.name，否则默认key=name
        private bool _isModularDevice;

        public DeviceManagerBase()
        {
            DEVICE.Managers.Add(this);
        }

        public DeviceManagerBase(bool modularDevice)
        {
            _isModularDevice = modularDevice;
            DEVICE.Managers.Add(this);
        }

        public virtual void Terminate()
        {
            lock (_lockerDevice)
            {
                foreach (var device in _nameDevice.Values)
                {
                    device.Terminate();
                }
            }

        }

        public void Monitor()
        {
            lock (_lockerDevice)
            {
                foreach (var device in _nameDevice.Values)
                {
                    try
                    {
                        _peformanceTimer.Start(0);

                        device.Monitor();

                        _trigExceed500.CLK = _peformanceTimer.GetElapseTime() > 500;
                        if (_trigExceed500.Q)
                        {
                            LOG.Warning($"{device.Module}.{device.Name} monitor time {_peformanceTimer.GetElapseTime()} ms");
                        }
                    }
                    catch (Exception ex)
                    {
                        LOG.Write(ex, string.Format("Monitor {0} Exception", device.Name));
                    }
                }
            }

        }

        public void Reset()
        {
            lock (_lockerDevice)
            {
                foreach (var device in _nameDevice.Values)
                {
                    device.Reset();
                }

                _trigExceed500.RST = true;
            }

        }

        public void Initialize(string modelFile, string type, ModuleName mod = ModuleName.System, string ioModule = "", bool endCallInit = true)
        {
            if (!File.Exists(modelFile))
            {
                throw new ApplicationException(string.Format("did not find the device model file {0} ", modelFile));
            }

            XmlDocument _dom = new XmlDocument();
            try
            {
                _dom.Load(modelFile);
                XmlElement node = _dom.SelectSingleNode("DeviceModelDefine") as XmlElement;
                if (node == null)
                {
                    throw new ApplicationException(string.Format("device mode file {0} is not valid", modelFile));
                }

                string fileType = node.GetAttribute("type");
                if (fileType != type)
                {
                    throw new ApplicationException(string.Format("the type {0} in device mode file {1} is inaccordance with the system type {2}", fileType, modelFile, type));
                }
                DeviceModelNodes = node;

                foreach (XmlNode nodeModelChild in node.ChildNodes)
                {
                    if (nodeModelChild.NodeType == XmlNodeType.Comment)
                        continue;
                    XmlElement nodeDevices = nodeModelChild as XmlElement;

                    if (null == nodeDevices) continue;
                    string assName = nodeDevices.GetAttribute("assembly");
                    if (string.IsNullOrEmpty(assName))
                        assName = "MECF.Framework.RT.EquipmentLibrary";

                    string className = nodeDevices.Name.Substring(0, nodeDevices.Name.Length - 1);
                    string typeName = nodeDevices.GetAttribute("classType");
                    if (string.IsNullOrEmpty(typeName))
                        typeName = "Aitex.Core.RT.Device.Unit." + className;

                    Assembly assembly = Assembly.Load(assName);
                    Type deviceType = assembly.GetType(typeName);

                    if (deviceType == null)
                    {
                        //throw new ApplicationException(string.Format("can not find the device type {0}", "Aitex.Core.RT.Device.Unit." + typeName));
                        continue;
                    }

                    foreach (var nodeDeviceChild in nodeDevices.ChildNodes)
                    {

                        XmlElement nodeDevice = nodeDeviceChild as XmlElement;

                        if (nodeDevice == null)
                        {
                            LOG.Write("Device Model File contains non element node, " + (nodeDeviceChild as XmlNode).Value);
                            continue;
                        }

                        IDevice device = Activator.CreateInstance(deviceType, mod.ToString(), nodeDevice, ioModule) as IDevice;
                        if (device.Name == "PendulumValve")
                        {
                            if (SC.ContainsItem($"{device.Module}.PendulumValve.PendulumValveType") && SC.GetStringValue($"{device.Module}.PendulumValve.PendulumValveType") != "PLC")
                            {
                                continue;
                            }
                            
                        }
                        

                        if (nodeDevice.HasAttribute("option") && Convert.ToBoolean(nodeDevice.Attributes["option"].Value))
                        {
                            _optionDevice.Add( device);
                            continue;
                        }

                        QueueDevice(device);

                        DATA.Subscribe(device, string.Format("{0}.{1}.{2}", device.Module, className, device.Name));


                        if (!_typeDevice.ContainsKey(deviceType))
                            _typeDevice[deviceType] = new List<IDevice>();
                        _typeDevice[deviceType].Add(device);

                        if (DisableAsyncInitialize)
                        {
                            InitDevice(device);
                        }
                        else
                        {
                            Task task = Task.Run(() => InitDevice(device));
                        }

                    }
                }

                if (endCallInit)
                {
                    Initialize();
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }


        public void Initialize(string modelFile, string modelType, string moduleName, string ioPath, string scPath)
        {
            if (!File.Exists(modelFile))
            {
                throw new ApplicationException(string.Format("did not find the device model file {0} ", modelFile));
            }

            XmlDocument _dom = new XmlDocument();
            try
            {
                _dom.Load(modelFile);
                XmlElement node = _dom.SelectSingleNode("DeviceModelDefine") as XmlElement;
                if (node == null)
                {
                    throw new ApplicationException(string.Format("device mode file {0} is not valid", modelFile));
                }

                string fileType = node.GetAttribute("type");
                if (fileType != modelType)
                {
                    throw new ApplicationException(string.Format("the type {0} in device mode file {1} is different with the system type {2}", fileType, modelFile, modelType));
                }
                DeviceModelNodes = node;

                foreach (XmlNode nodeModelChild in node.ChildNodes)
                {
                    if (nodeModelChild.NodeType == XmlNodeType.Comment)
                        continue;
                    XmlElement nodeDevices = nodeModelChild as XmlElement;

                    if (null == nodeDevices) continue;
                    string assName = nodeDevices.GetAttribute("assembly");
                    if (string.IsNullOrEmpty(assName))
                        assName = "MECF.Framework.RT.EquipmentLibrary";

                    string className = nodeDevices.Name.Substring(0, nodeDevices.Name.Length - 1);
                    string typeName = nodeDevices.GetAttribute("classType");
                    if (string.IsNullOrEmpty(typeName))
                        typeName = "Aitex.Core.RT.Device.Unit." + className;

                    Assembly assembly = Assembly.Load(assName);
                    Type deviceType = assembly.GetType(typeName);

                    if (deviceType == null)
                    {
                        //throw new ApplicationException(string.Format("can not find the device type {0}", "Aitex.Core.RT.Device.Unit." + typeName));
                        continue;
                    }

                    foreach (var nodeDeviceChild in nodeDevices.ChildNodes)
                    {

                        XmlElement nodeDevice = nodeDeviceChild as XmlElement;

                        if (nodeDevice == null)
                        {
                            LOG.Write("Device Model File contains non element node, " + (nodeDeviceChild as XmlNode).Value);
                            continue;
                        }

                        nodeDevice.SetAttribute("ioPath", ioPath);
                        nodeDevice.SetAttribute("scPath", scPath);

                        IDevice device = Activator.CreateInstance(deviceType, moduleName, nodeDevice, ioPath) as IDevice;

                        if (nodeDevice.HasAttribute("option") && Convert.ToBoolean(nodeDevice.Attributes["option"]))
                        {
                            _optionDevice.Add(device);
                            continue;
                        }

                        DATA.Subscribe(device, string.Format("{0}.{1}.{2}", device.Module, className, device.Name));

                        QueueDevice(device);

                        if (!_typeDevice.ContainsKey(deviceType))
                            _typeDevice[deviceType] = new List<IDevice>();
                        _typeDevice[deviceType].Add(device);

                        if (DisableAsyncInitialize)
                        {
                            InitDevice(device);
                        }
                        else
                        {
                            Task task = Task.Run(() => InitDevice(device));
                        }
                    }
                }

                Initialize();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        private void InitDevice(IDevice device)
        {
            try
            {
                if (!device.Initialize())
                {
                    LOG.Write($"{device.Name} initialize failed.");
                }
            }
            catch (Exception e)
            {
                LOG.Write(e);
            }
        }

        protected virtual void QueueDevice(IDevice device)
        {
            if (_isModularDevice)
            {
                QueueDevice($"{device.Module}.{device.Name}", device);
            }
            else
            {
                QueueDevice(device.Name, device);
            }
            
        }

        protected void QueueDevice(string key, IDevice device)
        {
            lock (_lockerDevice)
            {
                foreach (var deviceManager in DEVICE.Managers)
                {
                    if (deviceManager.ContainsDevice(key))
                    {
                        System.Diagnostics.Debug.Assert(false, $"Failed add duplicated device, named with {key}");

                        LOG.Write($"Failed add duplicated device, name {key}");
                        return;
                    }
                }

                _nameDevice.Add(key, device);
            }
        }

        public IDevice AddCustomDevice(IDevice device, string groupType, Type deviceType)
        {
            lock (_lockerDevice)
            {
                DATA.Subscribe(device, string.Format("{0}.{1}.{2}", device.Module, groupType, device.Name));
                if (!string.IsNullOrEmpty(device.Module) && device.Module != "System")
                {
                    QueueDevice(device.Module + "." + device.Name, device);
                }
                else
                {
                    QueueDevice(device.Name, device);
                }

                if (!_typeDevice.ContainsKey(deviceType))
                    _typeDevice[deviceType] = new List<IDevice>();
                _typeDevice[deviceType].Add(device);

                device.Initialize();

                //Task task = Task.Run(()=>device.Initialize());
            }

            return device;
        }

        public IDevice AddCustomDevice(IDevice device, Type deviceType)
        {
            lock (_lockerDevice)
            {
                try
                {
                    DATA.Subscribe(device, string.Format("{0}.{1}", device.Module, device.Name));

                    QueueDevice(device.Name, device);

                    if (!_typeDevice.ContainsKey(deviceType))
                        _typeDevice[deviceType] = new List<IDevice>();
                    _typeDevice[deviceType].Add(device);

                    device.Initialize();
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }

            }
            //Task task = Task.Run(()=>device.Initialize());

            return device;
        }

        public IDevice AddCustomDevice(IDevice device)
        {
            lock (_lockerDevice)
            {
                try
                {
                    DATA.Subscribe(device, string.Format("{0}.{1}", device.Module, device.Name));

                    QueueDevice(device.Name, device);

                    if (!_typeDevice.ContainsKey(device.GetType()))
                        _typeDevice[device.GetType()] = new List<IDevice>();
                    _typeDevice[device.GetType()].Add(device);

                    device.Initialize();
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }

            }
            //Task task = Task.Run(()=>device.Initialize());

            return device;
        }

        public IDevice AddCustomModuleDevice(IDevice device)
        {
            lock (_lockerDevice)
            {

                try
                {
                    DATA.Subscribe(device, string.Format("{0}.{1}", device.Module, device.Name));


                    QueueDevice($"{device.Module}.{device.Name}", device);

                    if (!_typeDevice.ContainsKey(device.GetType()))
                        _typeDevice[device.GetType()] = new List<IDevice>();
                    _typeDevice[device.GetType()].Add(device);

                    device.Initialize();
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }


                //Task task = Task.Run(()=>device.Initialize());
            }

            return device;
        }

        /// <summary>
        /// 根据设备类名添加设备，将从RT、Framework命名空间下查找设备，设备需继承IDevice接口，并且构造函数中参数与输入参数一致
        /// </summary>
        /// <param name="type">类型名</param>
        /// <param name="module">设备module</param>
        /// <param name="name">设备Name</param>
        /// <param name="otherParam">设备构造函数中其他参数</param>
        /// <returns></returns>

        public dynamic DynamicAddCustomDevice(string type, string module, string name, params object[] otherParam)
        {
            var RTTypes = Assembly.Load(Process.GetCurrentProcess().ProcessName).GetTypes();
            var typeInRT = RTTypes.FirstOrDefault(x => x.IsClass & x.Name == type);
            if (typeInRT != null)
            {
                IDevice device;
                try
                {
                    object[] param = new object[] { module, name }.Concat(otherParam).ToArray();
                    device = Activator.CreateInstance(typeInRT, param) as IDevice;
                    if (device == null)
                    {
                        throw new InvalidCastException($"{typeInRT} does not inherit IDevice interface");
                    }
                }
                catch (Exception e)
                {
                    LOG.Write(e);
                    throw e;
                }
                return AddCustomDevice(device);
            }

            var CommonTypes = Assembly.GetExecutingAssembly().GetTypes();
            var typeInCommon = CommonTypes.FirstOrDefault(x => x.IsClass & x.Name == type);
            if (typeInCommon != null)
            {
                IDevice device;
                try
                {
                    object[] param = new object[] { module, name }.Concat(otherParam).ToArray();
                    device = Activator.CreateInstance(typeInCommon, param) as IDevice;
                    if (device == null)
                    {
                        throw new InvalidCastException($"{typeInCommon} does not inherit IDevice interface");
                    }
                }
                catch (Exception e)
                {
                    LOG.Write(e);
                    throw e;
                }
                return AddCustomDevice(device);
            }
            throw new NotSupportedException($"{type} Undefined in RT or Common Assembly");
        }

        public virtual bool Initialize()
        {

            return true;
        }

        public T GetDevice<T>(string name) where T : class, IDevice
        {
            if (!_nameDevice.ContainsKey(name))
                return null;

            return _nameDevice[name] as T;
        }

        public object GetDevice(string name)
        {
            if (!_nameDevice.ContainsKey(name))
                return null;

            return _nameDevice[name];
        }

        public bool ContainsDevice(string name)
        {
            return _nameDevice.ContainsKey(name) ;
        }

        public List<T> GetDevice<T>() where T : class, IDevice
        {
            if (!_typeDevice.ContainsKey(typeof(T)))
                return null;

            List<T> result = new List<T>();
            foreach (var d in _typeDevice[typeof(T)])
            {
                result.Add(d as T);
            }

            return result;
        }

        public List<IDevice> GetAllDevice()
        {
            return _nameDevice.Values.ToList();
        }

        public object GetOptionDevice(string name, Type type)
        {
            foreach (var device in _optionDevice)
            {
                if ($"{device.Module}.{device.Name}" == name)
                {
                    if (type == null || device.GetType() == type)
                    {
                        return device;
                    }
                }
            }

            return null;
        }

    }
}
