using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;

namespace Aitex.Core.RT.ConfigCenter
{
    public class ConfigItem
    {
        public object this[string configName]
        {
            get
            {
                return CONFIG.Get(configName);
            }
            set
            {
                CONFIG.Set(configName, value);
            }
        }
    }

    public class CONFIG
    {
        public static ConfigItem Items = new ConfigItem();

        public static ICommonConfig InnerConfigManager { set; private get; }

        public static Dictionary<string, ICommonConfig> ModularManager { private set; get; } = new Dictionary<string, ICommonConfig>();

        public static object Get(string configName)
        {
            return (InnerConfigManager != null) ? InnerConfigManager.GetConfig(configName) : null;
        }

        public static void Set(string configName, object value)
        {
            if (InnerConfigManager != null)
                InnerConfigManager.SetConfig(configName, value);
        }

 
        public static void Subscribe(string module, string key, Func<object> getter)
        {
            if (InnerConfigManager != null)
                InnerConfigManager.Subscribe(module, key, getter);
        }

        public static object Poll(string paramName)
        {
            return (InnerConfigManager == null) ? null : InnerConfigManager.Poll(paramName);
        }

        public static object Poll(string module, string paramName)
        {
            if (ModularManager != null && ModularManager.ContainsKey(module) && ModularManager[module] != null)
                return ModularManager[module].Poll(paramName);

            return Poll(paramName);
        }

        public static Dictionary<string, object> PollConfig(IEnumerable<string> keys)
        {
            return (InnerConfigManager == null) ? null : InnerConfigManager.PollConfig(keys);
        }

        public static Dictionary<string, object> PollConfig(string module, IEnumerable<string> keys)
        {
            if (ModularManager != null && ModularManager.ContainsKey(module) && ModularManager[module] != null)
                return ModularManager[module].PollConfig(keys);

            return PollConfig(keys);
        }
    }
}
