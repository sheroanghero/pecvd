using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Event;

namespace Aitex.Core.RT.Device
{
    [Serializable]
    public class BaseDevice : IAlarmHandler
    {
        public string UniqueName { get; set; }
        public string Module { get; set; }
        public string Name { get; set; }
        public string Display { get; set; }
        public string DeviceID { get; set; }
 
        public string ScBasePath { get; set; }
        public string IoBasePath { get; set; }
 
        public event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;
 
        private Dictionary<string, AlarmEventItem> AlarmList { get; set; }

        public bool HasAlarm
        {
            get
            {
                return AlarmList!=null && (AlarmList.Values.FirstOrDefault(x => !x.IsAcknowledged && x.Level==EventLevel.Alarm)!=null);
            }
        }

        public BaseDevice()
        {
            ScBasePath = "System";
            IoBasePath = "System";
            AlarmList = new Dictionary<string, AlarmEventItem>();
        }

        public BaseDevice(string module, string name, string display, string id) : this()
        {
            display = string.IsNullOrEmpty(display) ? name : display;
            id = string.IsNullOrEmpty(id) ? name : id;

            Module = module;
            Name = name;
            Display = display;
            DeviceID = id;
            UniqueName = $"{module}.{name}";
        }

        public void AlarmStateChanged(AlarmEventItem args)
        {
            if (OnDeviceAlarmStateChanged != null)
            {
                OnDeviceAlarmStateChanged($"{UniqueName}", args);
            }
        }

        protected AlarmEventItem SubscribeAlarm(string name, string description, Func<bool> resetChecker, EventLevel level=EventLevel.Alarm)
        {
            AlarmEventItem item = new AlarmEventItem(Module, name, description, resetChecker, this);
            item.Level = level;

            AlarmList[name] = item;

            EV.Subscribe(item);

            return item;
        }

        protected void ResetAlarm()
        {
            foreach (var alarmEventItem in AlarmList)
            {
                alarmEventItem.Value.Reset();
            }
        }

        public DOAccessor ParseDoNode(string name, XmlElement node, string ioModule = "")
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return IO.DO[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : $"{ioModule}.{node.GetAttribute(name).Trim()}"];

            return null;
        }

        public DIAccessor ParseDiNode(string name, XmlElement node, string ioModule = "")
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return IO.DI[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : $"{ioModule}.{node.GetAttribute(name).Trim()}"];
            return null;
        }

        public AOAccessor ParseAoNode(string name, XmlElement node, string ioModule = "")
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return IO.AO[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : $"{ioModule}.{node.GetAttribute(name).Trim()}"];
            return null;
        }

        public AIAccessor ParseAiNode(string name, XmlElement node, string ioModule = "")
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return IO.AI[string.IsNullOrEmpty(ioModule) ? node.GetAttribute(name).Trim() : $"{ioModule}.{node.GetAttribute(name).Trim()}"];
            return null;
        }

        public SCConfigItem ParseScNode(string name, XmlElement node, string ioModule = "", string defaultScPath = "")
        {
            SCConfigItem result = null;

            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                result = SC.GetConfigItem(node.GetAttribute(name));

            if (result == null && !string.IsNullOrEmpty(defaultScPath) && SC.ContainsItem(defaultScPath))
                result = SC.GetConfigItem(defaultScPath);

            return result;
        }

        public static T ParseDeviceNode<T>(string name, XmlElement node) where T : class, IDevice
        {
            if (!string.IsNullOrEmpty(node.GetAttribute(name).Trim()))
                return DEVICE.GetDevice<T>(node.GetAttribute(name));
            LOG.Write(string.Format("{0}，未定义{1}", node.InnerXml, name));
            return null;
        }

        public static T ParseDeviceNode<T>(string module, string name, XmlElement node) where T : class, IDevice
        {
            string device_id = node.GetAttribute(name);
            if (!string.IsNullOrEmpty(device_id) && !string.IsNullOrEmpty(device_id.Trim()))
            {
                return DEVICE.GetDevice<T>($"{module}.{device_id}");
            }
            LOG.Write(string.Format("{0}，未定义{1}", node.InnerXml, name));
            return null;
        }
 
    }
}
