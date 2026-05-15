using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using Aitex.Core.RT.ConfigCenter;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.DataCenter;
using OpenSEMI.ClientBase.ServiceProvider;

namespace MECF.Framework.UI.Client.CenterViews.Configs.SystemConfig
{
    public class SystemConfigProvider : IProvider
    {
        private static SystemConfigProvider _Instance = null;
        public static SystemConfigProvider Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new SystemConfigProvider();

                return _Instance;
            }
        }

        public void Create()
        {

        }

        private Dictionary<string, DataType> typeDic = new Dictionary<string, DataType>() {
                { "String", DataType.String },
                { "Bool", DataType.Bool },
                { "Integer", DataType.Int },
                { "Double", DataType.Double }
        };

        public ConfigNode GetConfig()
        {
            string config = QueryDataClient.Instance.Service.GetConfigFileContent();
            if (string.IsNullOrEmpty(config))
                return null;
            ConfigNode result = new ConfigNode() { Path = string.Empty };

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(config);

                XmlElement element = xml.SelectSingleNode("root") as XmlElement;


                BuildTree(element, result);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return result;
        }
        public ConfigNode GetConfig(string module)
        {
            string config = QueryDataClient.Instance.Service.GetConfigFileContentByModule(module);
            if (string.IsNullOrEmpty(config))
                return null;
            ConfigNode result = new ConfigNode() {Path = string.Empty};

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(config);

                XmlElement element = xml.SelectSingleNode("root") as XmlElement;


                BuildTree(element, result);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return result;
        }

        public void BuildTree(XmlElement root, ConfigNode item)
        {
            item.Name = root.GetAttribute("name");
            item.SubNodes = new List<ConfigNode>();
            item.Items = new List<ConfigItem>();
            item.IsMatch = true;
            item.Display = root.GetAttribute("display");

            if (string.IsNullOrEmpty(item.Display))
                item.Display = item.Name;

            XmlNodeList group = root.SelectNodes("configs");
            XmlNodeList single = root.SelectNodes("config");

            if (group != null)
            {
                foreach (var groupItem in group)
                {
                    XmlElement element = groupItem as XmlElement;
                    string strVisible = element.GetAttribute("visible");
                    bool bVisible;
                    if (!string.IsNullOrEmpty(strVisible))
                    {
                        if (bool.TryParse(strVisible, out bVisible) && !bVisible)
                            continue;
                    }


                    ConfigNode sc = new ConfigNode() {Path = string.IsNullOrEmpty(item.Path) ? item.Name : item.Path +"."+ item.Name};

                    sc.IsExpanded = Regex.Matches(sc.Path, ".").Count < 3;

                    sc.IsMatch = true;

                    BuildTree(element, sc);

                    item.SubNodes.Add(sc);
                }
            }

            if (single != null)
            {
                foreach (var singleItem in single)
                {
                    XmlElement element = singleItem as XmlElement;

                    string strVisible = element.GetAttribute("visible");
                    bool bVisible;
                    if (!bool.TryParse(strVisible, out bVisible))
                        bVisible = true;

                    if (!bVisible)  //do not show the item if visible field is false
                        continue;

                    ConfigItem config = new ConfigItem()
                    {
                        Name = element.GetAttribute("name"),
                        Display = element.GetAttribute("display"),
                        DefaultValue = element.GetAttribute("default"),
                        Description = element.GetAttribute("description"),
                        Max = ConvertStringToFloat(element.GetAttribute("max")),
                        Min = ConvertStringToFloat(element.GetAttribute("min")),
                        Parameter = element.GetAttribute("paramter"),
                        Unit = element.GetAttribute("unit"),
                        Tag = element.GetAttribute("tag")
                    };

                    if (string.IsNullOrEmpty(config.Display))
                    {
                        config.Display = config.Description;
                        if (string.IsNullOrEmpty(config.Display))
                            config.Display = config.Name;
                    }

                    string strType = element.GetAttribute("type");
                    config.Type = typeDic.ContainsKey(strType) ? typeDic[strType] : DataType.Unknown;

                    config.Visible = bVisible;

                    item.Items.Add(config);
                }
            }
        }

        private float ConvertStringToFloat(string value)
        {
            float result;
            if (!float.TryParse(value, out result))
                result = float.NaN;
            return result;
        }

        public ConfigNode GetConfigTree()
        {
            return GetConfig();
        }

        public ConfigNode GetConfigTree(string module)
        {
            return GetConfig(module);
        }

        public List<string> GetValuesByNode(string node)
        {
            List<string> values = new List<string>();

            //interface to get values

            return values;
        }

        public string GetValueByName(string module, string name)
        {
            object result = QueryDataClient.Instance.Service.GetConfigByModule(module, name);
            if (result != null)
                return result.ToString();
            else
                return string.Empty;
        }

        public string GetValueByName(string name)
        {
            object result = QueryDataClient.Instance.Service.GetConfig(name);
            if (result != null)
                return result.ToString();
            else
                return string.Empty;
        }
    }
}
