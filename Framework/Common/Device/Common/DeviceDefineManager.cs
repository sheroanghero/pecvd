using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device
{
    /// <summary>
    ///  读取机台对应的config xml文件.
    /// </summary>
    public class DeviceDefineManager : Singleton<DeviceDefineManager>
    {
        private readonly Dictionary<string, string> _dictionary = new Dictionary<string, string>();
        private XDocument _xml;
        private string _xmlConfigPath;

        public T? GetValue<T>(string name) where T : struct 
        {
            try
            {
                if (!_dictionary.ContainsKey(name))
                    return default(T);
                if (typeof(T) == typeof(bool))
                    return (T)(object)(_dictionary[name].ToLower() == "true");
                if (typeof(T) == typeof(int))
                    return (T)(object)(int.Parse(_dictionary[name]));
                if (typeof(T) == typeof(double))
                    return (T)(object)(double.Parse(_dictionary[name]));
                //Debug.Assert(false, "unsupported type");
                
                return default(T);
            }
            catch
            {
                return null;
            }
        }

        public string GetValue(string name)
        {
            _dictionary.TryGetValue(name, out string value);
            return value;
        }

        private void Load()
        {
            foreach (var xNode in _xml.DescendantNodes())
                if (xNode is XElement element)
                {
                    if (element.Name == _xml.Root?.Name)
                        continue;
                    _dictionary.Add(element.Name.ToString(), element.Value);
                }
        }

        private void Subscribe()
        {
            foreach (var define in _dictionary)
            {
                DATA.Subscribe($"DeviceDefine.{define.Key}",()=>_dictionary[define.Key]);
            }
        }

        public void Initialize(string xmlConfigPath)
        {
            _xmlConfigPath = xmlConfigPath;
            _xml = XDocument.Load(_xmlConfigPath);
            Load();
            Subscribe();
        }
    }
}