using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Aitex.Core.RT.Log;
using System.IO;
using Aitex.Common.Util;
using System.Collections.Concurrent;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;

namespace Aitex.Core.RT.ConfigCenter
{
    public class ConfigManager : Singleton<ConfigManager>, ICommonConfig
    {
        Dictionary<string, object> _dic = new Dictionary<string,object>();

        ConcurrentDictionary<string, DataItem<object>> _keyValueMap = new ConcurrentDictionary<string, DataItem<object>>(StringComparer.OrdinalIgnoreCase);

        object _locker = new object();

        public ConfigManager()
        {
           
        }

        public void Initialize()
        {
            CONFIG.InnerConfigManager = this;

            OP.Subscribe("SetConfig", InvokeSetConfig);
        }

        private bool InvokeSetConfig(string arg1, object[] arg2)
        {
            //SC.SetItemValue((string)args[0], args[1]);

            //if ((string)args[0] == SCName.System_Language)
            //{
            //    //RtApplication.UpdateCultureResource(((int)SC.GetItemValue(SCName.System_Language)) == 2 ? "zh-CN" : "en-US");
            //}

            return true;
        }

        public void Terminate()
        {
            
        }

        /// <summary>
        /// 如果文件不存在，返回NULL.如果读取错误，返回""
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetFileContent(string fileName)
        {
            if (!Path.IsPathRooted(fileName))
                fileName = PathManager.GetCfgDir() + "\\" + fileName;

            if (!File.Exists(fileName))
                return null;

            StringBuilder s = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(fileName))
                {
                    while (!sr.EndOfStream)
                    {
                        s.Append(sr.ReadLine());
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return "";
            }
            return s.ToString();
        }

        public object GetConfig(string config)
        {
            return _dic.ContainsKey(config) ? _dic[config] : null;
        }

        public void SetConfig(string config, object value)
        {
            _dic[config] = value;
        }

        public void Subscribe(string module, string key, Func<object> getter)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException("key");

            if (!string.IsNullOrEmpty(module))
                key = module + "." + key;

            if (_keyValueMap.ContainsKey(key))
                throw new Exception(string.Format("Duplicated Key:{0}", key));
            if (getter == null)
                throw new ArgumentNullException("getter");

            _keyValueMap.TryAdd(key, new DataItem<object>(getter));
        }

        public object Poll(string key)
        {
            return _keyValueMap.ContainsKey(key) ? _keyValueMap[key].Value : null;
        }

        public Dictionary<string, object> PollConfig(IEnumerable<string> keys)
        {
            Dictionary<string, object> result = new Dictionary<string,object>();
            foreach (string key in keys)
            {
                if (_keyValueMap.ContainsKey(key))
                    result[key] = _keyValueMap[key].Value;
                else
                {
                    LOG.Error("undefined config：" + key);
                }
            }

            return result;
        }
    }
}
