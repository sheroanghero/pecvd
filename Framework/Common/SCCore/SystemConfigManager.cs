using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Aitex.Common.Util;
using Aitex.Core.RT.ConfigCenter;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.DBCore;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.FAServices;

namespace MECF.Framework.Common.SCCore
{
	public class SystemConfigManager : Singleton<SystemConfigManager>, ISCManager
	{
		private Dictionary<string, SCConfigItem> _items = new Dictionary<string, SCConfigItem>(StringComparer.OrdinalIgnoreCase);
		private object _itemLocker = new object();

		private Dictionary<string, string> _scConfigFile = new Dictionary<string, string>();
        XmlDocument _xmlConfigContent;
		private string _scDataFile = PathManager.GetCfgDir() + "_sc.data";
		private string _scDataBackupFile = PathManager.GetCfgDir() + "_sc.data.bak";
		private string _scDataErrorFile = PathManager.GetCfgDir() + "_sc.data.err.";

        public List<VIDItem> VidConfigList
        {
            get
            {
                List<VIDItem> result = new List<VIDItem>();
                foreach (var dataItem in _items)
                {
                    result.Add(new VIDItem()
                    {
                        DataType = "",
                        Description = dataItem.Value.Description,
                        Index = 0,
                        Name = dataItem.Key,
                        Unit = "",
                    });
                }

                return result;
            }
        }

		public void Initialize(string scConfigPathName)
        {
            Initialize(scConfigPathName, "System", null);
        }

		/// <summary>
		/// 模块化配置
		/// </summary>
		/// <param name="scConfigPathName"></param>
		/// <param name="module"></param>
		/// <param name="moduleNamePlaceHolder"></param>
		public void Initialize(string scConfigPathName, string module, string moduleNamePlaceHolder)
        {
            if (_xmlConfigContent == null)
            {
                _xmlConfigContent = new XmlDocument();
                _xmlConfigContent.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><root></root>");

				BackupAndRecoverDataFile();

                OP.Subscribe("System.SetConfig", InvokeSetConfig);

                SC.Manager = this;
			}

            if (string.IsNullOrEmpty(module))
                module = "System";

            _scConfigFile[module] = scConfigPathName;

            Dictionary<string, SCConfigItem> newItem = BuildItems(scConfigPathName, module, moduleNamePlaceHolder);

            CustomData(newItem);

            GenerateDataFile();

            foreach (var item in _items)
            {
                if (newItem.ContainsKey(item.Key))
                {
                    CONFIG.Subscribe("", item.Key, () => item.Value.Value);
                }
            }
        }

		private bool InvokeSetConfig(string cmd, object[] parameters)
		{
			string key = (string)parameters[0];

			if (!ContainsItem(key))
			{
				EV.PostWarningLog("System", string.Format("Not find SC with name {0}", key));
				return false;
			}

			object old = GetConfigValueAsObject(key);

			if (InitializeItemValue(_items[(string)parameters[0]], parameters[1].ToString()))
			{
				GenerateDataFile();
				EV.PostInfoLog("System", string.Format("SC {0} value changed from {1} to {2}", key, old, parameters[1]));
			}

			return true;
		}

		public void Terminate()
		{

		}

		public string GetFileContent()
        {
            if (_xmlConfigContent == null)
                return "";

            return _xmlConfigContent.InnerXml;// GetFileContent("System");
        }

        public string GetFileContent(string module)
        {
            if (string.IsNullOrEmpty(module))
                module = "System";

			if (!File.Exists(_scConfigFile[module]))
                return "";

            StringBuilder s = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(_scConfigFile[module]))
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


		private Dictionary<string, SCConfigItem> BuildItems(string xmlFile, string module, string moduleNamePlaceHolder)
		{
			XmlDocument xml = new XmlDocument();
            Dictionary<string, SCConfigItem> newItem = new Dictionary<string, SCConfigItem>();
			try
			{
				xml.Load(xmlFile);

				XmlNodeList nodeConfigs = xml.SelectNodes("root/configs");

                XmlNode content = _xmlConfigContent.SelectSingleNode("root");

				foreach (XmlElement nodeConfig in nodeConfigs)
                {
                    string nodeName = nodeConfig.GetAttribute("name");
                    if (!string.IsNullOrEmpty(moduleNamePlaceHolder) && nodeName == moduleNamePlaceHolder)
                    {
						string display = nodeConfig.GetAttribute("display");
                        display = $"{module}-{display}";
                        nodeConfig.SetAttribute("display", display);
                        nodeName = module;
                    }

                    nodeConfig.SetAttribute("name", nodeName);
 
                    content.AppendChild(_xmlConfigContent.ImportNode(nodeConfig, true));

					BuildPathConfigs(nodeName, nodeConfig as XmlElement, ref newItem);
                }
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}

            return newItem;
        }

		private void BuildPathConfigs(string parentPath, XmlElement configElement, ref Dictionary<string, SCConfigItem> newItem)
		{
			XmlNodeList nodeConfigsList = configElement.SelectNodes("configs");
			foreach (XmlElement nodeConfig in nodeConfigsList)
			{
				if (string.IsNullOrEmpty(parentPath))
				{
					BuildPathConfigs(nodeConfig.GetAttribute("name"), nodeConfig as XmlElement, ref newItem);
				}
				else
				{
					BuildPathConfigs(parentPath + "." + nodeConfig.GetAttribute("name"), nodeConfig as XmlElement, ref newItem);
				}
			}

			XmlNodeList nodeConfigs = configElement.SelectNodes("config");
			foreach (XmlElement nodeConfig in nodeConfigs)
			{
				SCConfigItem item = new SCConfigItem()
				{
					Default = nodeConfig.GetAttribute("default"),

					Name = nodeConfig.GetAttribute("name"),
					Description = nodeConfig.GetAttribute("description"),
					Max = nodeConfig.GetAttribute("max"),
					Min = nodeConfig.GetAttribute("min"),
					Parameter = nodeConfig.GetAttribute("paramter"),
					Path = parentPath,
					Tag = nodeConfig.GetAttribute("tag"),
					Type = nodeConfig.GetAttribute("type"),
					Unit = nodeConfig.GetAttribute("unit"),
				};
				InitializeItemValue(item, item.Default);

				if (_items.ContainsKey(item.PathName))
				{
					LOG.Error("Duplicated SC item, " + item.PathName);
				}

				_items[item.PathName] = item;
				newItem[item.PathName] = item;
			}
		}

		private void BackupAndRecoverDataFile()
		{
			try
			{
				if (File.Exists(_scDataFile) && IsXmlFileLoadable(_scDataFile))
				{
					File.Copy(_scDataFile, _scDataBackupFile, true);
				}
				else if (File.Exists(_scDataBackupFile) && IsXmlFileLoadable(_scDataBackupFile))
				{
					if (File.Exists(_scDataFile))
					{
						File.Copy(_scDataFile, _scDataErrorFile + DateTime.Now.ToString("yyyyMMdd_HHmmss"), true);
					}

					File.Copy(_scDataBackupFile, _scDataFile, true);
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		private void CustomData(Dictionary<string, SCConfigItem> item)
		{
			Dictionary<string, string> values = new Dictionary<string, string>();
			try
			{
				if (File.Exists(_scDataFile))
				{
					XmlDocument xmlData = new XmlDocument();
					xmlData.Load(_scDataFile);

					XmlNodeList scdatas = xmlData.SelectNodes("root/scdata");
					foreach (XmlElement nodedata in scdatas)
					{
						string name = nodedata.GetAttribute("name");
						if (_items.ContainsKey(name) && item.ContainsKey(name))
						{
							InitializeItemValue(_items[name], nodedata.GetAttribute("value"));
                            InitializeItemValue(item[name], nodedata.GetAttribute("value"));
						}
					}
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}

		}

		private void GenerateDataFile()
		{
			try
			{
				Dictionary<string, string> oldData = new Dictionary<string, string>();
                if (File.Exists(_scDataFile))
                {
                    try
                    {
                        XmlDocument xmlOld = new XmlDocument();
                        xmlOld.Load(_scDataFile);
                        XmlElement nodeRootOld = xmlOld.SelectSingleNode("root") as XmlElement;
                        foreach (var node in nodeRootOld.ChildNodes)
                        {
                            XmlElement nodeData = node as XmlElement;
                            oldData[nodeData.GetAttribute("name")] = nodeData.GetAttribute("value");
                        }
                    }
                    catch (Exception ex)
                    {
                        LOG.Write(ex);
                    }

                    File.Copy(_scDataFile, _scDataBackupFile, true);
                }

				XmlDocument xml = new XmlDocument();
				xml.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><root></root>");
				XmlElement nodeRoot = xml.SelectSingleNode("root") as XmlElement;

				foreach (var scConfigItem in _items)
				{
					XmlElement node = xml.CreateElement("scdata");
					node.SetAttribute("name", scConfigItem.Key);
					node.SetAttribute("value", scConfigItem.Value.Value.ToString());
					nodeRoot.AppendChild(node);

                    oldData.Remove(scConfigItem.Key);
                }

                foreach (var data in oldData)
                {
					XmlElement node = xml.CreateElement("scdata");
                    node.SetAttribute("name", data.Key);
                    node.SetAttribute("value", data.Value);
                    nodeRoot.AppendChild(node);
				}

                using (FileStream fsFileStream = new FileStream(_scDataFile,
					FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 1024, FileOptions.WriteThrough))
				{
					fsFileStream.SetLength(0);

					XmlWriterSettings settings = new XmlWriterSettings();
					settings.Indent = true;
					settings.OmitXmlDeclaration = false;

					using (XmlWriter xmlWrite = XmlWriter.Create(fsFileStream, settings))
					{
						xml.Save(xmlWrite);
					}
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		private bool IsXmlFileLoadable(string file)
		{
			try
			{
				XmlDocument xml = new XmlDocument();
				xml.Load(file);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return false;
			}

			return true;
		}

		public SCConfigItem GetConfigItem(string name)
		{
			//Debug.Assert(_items.ContainsKey(name), "can not find sc name, "+name);

			if (!_items.ContainsKey(name))
			{
				return null;
			}

			return _items[name];
		}


		public bool ContainsItem(string name)
		{
			return _items.ContainsKey(name);
		}

		public object GetConfigValueAsObject(string name)
		{
			SCConfigItem item = GetConfigItem(name);
			switch (item.Type)
			{
				case "Bool": return item.BoolValue;
				case "Integer": return item.IntValue;
				case "Double": return item.DoubleValue;
				case "String": return item.StringValue;
			}

			return null;
		}

		public T GetValue<T>(string name) where T : struct
		{
			try
			{
				if (typeof(T) == typeof(bool))
					return (T)(object)_items[name].BoolValue;
				if (typeof(T) == typeof(int))
					return (T)(object)_items[name].IntValue;
				if (typeof(T) == typeof(double))
					return (T)(object)_items[name].DoubleValue;
			}
			catch (KeyNotFoundException)
			{
				EV.PostAlarmLog("System", $"Can not find system config item {name}");
				return default(T);
			}
			catch (Exception)
			{
                EV.PostAlarmLog("System", $"Can not get valid system config item value {name}");
				return default(T);
			}

			Debug.Assert(false, "unsupported type");

			return default(T);
		}

		public string GetStringValue(string name)
		{
			if (!_items.ContainsKey(name))
				return null;

			return _items[name].StringValue;
		}

		public T SafeGetValue<T>(string name, T defaultValue) where T : struct
		{
			try
			{
				if (typeof(T) == typeof(bool))
					return (T)(object)_items[name].BoolValue;
				if (typeof(T) == typeof(int))
					return (T)(object)_items[name].IntValue;
				if (typeof(T) == typeof(double))
					return (T)(object)_items[name].DoubleValue;
			}
			catch (KeyNotFoundException)
			{
				return defaultValue;
			}
			catch (Exception)
			{
				return defaultValue;
			}

			Debug.Assert(false, "unsupported type");

			return defaultValue;
		}

		public string SafeGetStringValue(string name, string defaultValue)
		{
			if (!_items.ContainsKey(name))
				return defaultValue;

			return _items[name].StringValue;
		}

		public List<SCConfigItem> GetItemList()
		{
			return _items.Values.ToList();
		}

		public void SetItemValueFromString(string name, string value)
		{
			if (InitializeItemValue(_items[name], value))
			{
				GenerateDataFile();
			}

		}

		private bool InitializeItemValue(SCConfigItem item, string value)
		{
			bool changed = false;
			switch (item.Type)
			{
				case "Bool":
					bool boolValue;
					if (bool.TryParse(value, out boolValue) && boolValue != item.BoolValue)
					{
						item.BoolValue = boolValue;
						changed = true;
					}
					break;
				case "Integer":
					int intValue;
					if (int.TryParse(value, out intValue) && intValue != item.IntValue)
					{

						int.TryParse(item.Min, out int min);
						int.TryParse(item.Max, out int max);

						if (intValue < min || intValue > max)
						{
							EV.PostWarningLog(ModuleNameString.System, $"SC {item.PathName} value  {intValue} out of setting range ({item.Min}, {item.Max})");
							break;
						}
						item.IntValue = intValue;
						changed = true;
					}
					break;
				case "Double":
					double doubleValue;
					if (double.TryParse(value, out doubleValue) && Math.Abs(doubleValue - item.DoubleValue) > 0.0001)
					{
						double.TryParse(item.Min, out double min);
						double.TryParse(item.Max, out double max);
						if (doubleValue < min || doubleValue > max)
						{
							EV.PostWarningLog(ModuleNameString.System, $"SC {item.PathName}  value  {doubleValue} out of setting range ({item.Min}, {item.Max})");
							break;
						}
						item.DoubleValue = doubleValue;
						changed = true;
					}
					break;
				case "String":
					if (value != item.StringValue)
					{
						item.StringValue = value;
						changed = true;
					}
					break;
			}

			return changed;
		}
		public void SetItemValue(string name, object value)
		{
			Debug.Assert(_items.ContainsKey(name), "can not find sc name, " + name);

			if (!_items.ContainsKey(name))
			{
				return;
			}

			bool changed = false;
			switch (_items[name].Type)
			{
				case "Bool":
					bool boolValue = (bool)value;
					if (boolValue != _items[name].BoolValue)
					{
						_items[name].BoolValue = boolValue;
						changed = true;
					}
					break;
				case "Integer":
					int intValue = (int)value;
					if (intValue != _items[name].IntValue)
					{
						_items[name].IntValue = intValue;
						changed = true;
					}
					break;
				case "Double":
					double doubleValue = (double)value;
					if (Math.Abs(doubleValue - _items[name].DoubleValue) > 0.0001)
					{
						_items[name].DoubleValue = doubleValue;
						changed = true;
					}
					break;
				case "String":
					string stringValue = (string)value;
					if (stringValue != _items[name].StringValue)
					{
						_items[name].StringValue = stringValue;
						changed = true;
					}
					break;
			}

			if (changed)
			{
				GenerateDataFile();
			}
		}

		public void SetItemValueStringFormat(string name, string value)
		{
			Debug.Assert(_items.ContainsKey(name), "can not find sc name, " + name);

			if (!_items.ContainsKey(name))
			{
				return;
			}

			bool changed = false;
			switch (_items[name].Type)
			{
				case "Bool":
					bool boolValue = Convert.ToBoolean(value);
					if (boolValue != _items[name].BoolValue)
					{
						_items[name].BoolValue = boolValue;
						changed = true;
					}
					break;
				case "Integer":
					int intValue = Convert.ToInt32(value);
					if (intValue != _items[name].IntValue)
					{
						_items[name].IntValue = intValue;
						changed = true;
					}
					break;
				case "Double":
					double doubleValue = Convert.ToDouble(value);
					if (Math.Abs(doubleValue - _items[name].DoubleValue) > 0.0001)
					{
						_items[name].DoubleValue = doubleValue;
						changed = true;
					}
					break;
				case "String":
					string stringValue = (string)value;
					if (stringValue != _items[name].StringValue)
					{
						_items[name].StringValue = stringValue;
						changed = true;
					}
					break;
			}

			if (changed)
			{
				GenerateDataFile();
			}
		}

		public void SetItemValue(string name, bool value)
		{
			Debug.Assert(_items.ContainsKey(name), "can not find sc name, " + name);
			Debug.Assert(_items[name].Type == "Bool", "sc type not bool, defined as" + _items[name].Type);

			if (value != _items[name].BoolValue)
			{
				_items[name].BoolValue = value;

				GenerateDataFile();
			}
		}

		public void SetItemValue(string name, int value)
		{
			Debug.Assert(_items.ContainsKey(name), "can not find sc name, " + name);
			Debug.Assert(_items[name].Type == "Integer", "sc type not bool, defined as" + _items[name].Type);

			if (value != _items[name].IntValue)
			{
				_items[name].IntValue = value;

				GenerateDataFile();
			}
		}

		public void SetItemValue(string name, double value)
		{
			Debug.Assert(_items.ContainsKey(name), "can not find sc name, " + name);
			Debug.Assert(_items[name].Type == "Double", "sc type not bool, defined as" + _items[name].Type);

			if (Math.Abs(value - _items[name].DoubleValue) > 0.0001)
			{
				_items[name].DoubleValue = value;

				GenerateDataFile();
			}
		}

		public void SetItemValue(string name, string value)
		{
			Debug.Assert(_items.ContainsKey(name), "can not find sc name, " + name);

			if (value != _items[name].StringValue)
			{
				_items[name].StringValue = value;

				GenerateDataFile();
			}
		}
	}
}

