using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.Util
{
    [AttributeUsageAttribute(AttributeTargets.Property | AttributeTargets.Field)]
    public class SubscriptionAttribute : Attribute
    {
        public enum FLAG
        {
            SaveDB = 0x00,
            IgnoreSaveDB = 0x01,
        }

        public string ModuleKey { get { return string.IsNullOrEmpty(Module) ? Key : (Module + "." + Key); } }

        public readonly string Key;
        public string Module { get; internal set; }
        public readonly string Method;

        public readonly int Flag;
        
        public SubscriptionAttribute(string key, int flag = 0, string module="")
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException("key");
            Key = key;
            Flag = flag;
            Module = module;
        }

        public SubscriptionAttribute(string key, string module)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException("key");
            Key = key;
            Flag = (int)FLAG.SaveDB;
            Module = module;
        }

        public SubscriptionAttribute(object key, string module)
        {
            if (string.IsNullOrWhiteSpace(key.ToString()))
                throw new ArgumentNullException("key");
            Key = key.ToString();
            Flag = (int)FLAG.IgnoreSaveDB;
            Module = module;
        }
        public SubscriptionAttribute(object key)
        {
            if (string.IsNullOrWhiteSpace(key.ToString()))
                throw new ArgumentNullException("key");
            Key = key.ToString();
            Flag = (int)FLAG.IgnoreSaveDB;
            Module = "";
        }

        public void SetModule(string module)
        {
            Module = module;
        }
        public SubscriptionAttribute(string key, string module, string deviceName, string deviceType)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException("key");
            Key = string.Format("{0}.{1}.{2}", deviceType, deviceName, key); 
            Flag = (int)FLAG.SaveDB;
            Module = module;
        }

        public SubscriptionAttribute(string module, string method, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(module))
                throw new ArgumentNullException("module");

            Module = module;
            Method = method;
        }

    }
}
