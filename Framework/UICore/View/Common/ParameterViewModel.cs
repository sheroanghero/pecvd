using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.UI.MVVM;
using Aitex.Core.RT.Log;
using System.Xml;
using Aitex.Core.RT.ConfigCenter;

namespace Aitex.Core.UI.View.Common
{
    public class ParameterViewModel : ViewModelBase
    {
        public ParameterViewModel()
        {
        }

        protected virtual string GetConfigXmlContent()
        {
            return "";
        }

        public virtual bool SaveConfigSection(string groupName, List<ConfigEntry> lstEntry)
        {
            return false;
        }

        //description, navigation, sectionPath
        public List<KeyValuePair<string, string>> GetSectionList()
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            string xmlContent = GetConfigXmlContent();

            if (string.IsNullOrEmpty(xmlContent))
            {
                LOG.Error("没有读取到SC配置文件");
                return result;
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlContent);

            XmlNodeList lst = doc.SelectSingleNode("/SCData").ChildNodes;
            foreach (XmlElement node in lst)
            {
                result.Add(new KeyValuePair<string, string>(GetNodeText(node, "Description"), string.Format("/SCData/Section[@Name='{0}']", GetNodeText(node, "Name"))));
            }

            return result;
        }

        public List<ConfigEntry> GetConfigEntries(string sectionPath)
        {
            List<ConfigEntry> result = new List<ConfigEntry>();

            string xmlContent = GetConfigXmlContent();

            if (string.IsNullOrEmpty(xmlContent))
            {
                LOG.Error("没有读取到SC配置文件");
                return result;
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlContent);

            XmlNode parentNode = doc.SelectSingleNode(sectionPath);

            XmlNodeList lstNode = parentNode.ChildNodes;

            foreach (XmlNode node in lstNode)
            {
                try
                {
                    result.Add(new ConfigEntry()
                    {
                        EntryName = GetNodeText(node, "Name"),
                        SectionName = GetNodeText(node, "Name"),
                         Description = GetNodeText(node, "Description"),
                        Type = GetNodeText(node, "Type"),
                        Default = GetNodeText(node, "Default"),
                        RangeLowLimit = GetNodeText(node, "RangeLowLimit"),
                        RangeUpLimit = GetNodeText(node, "RangeUpLimit"),
                        SystemType = GetNodeText(parentNode, "Name"),
                        Unit = GetNodeText(node, "Unit"),
                        Value = ((string.IsNullOrEmpty(GetNodeText(node, "Type")) || GetNodeText(node, "Type") == "Double") ?
                                Math.Round(Convert.ToDouble(GetNodeText(node, "Value")), 3).ToString() : GetNodeText(node, "Value")),
                        XPath = string.Format("{0}/{1}", sectionPath, node.Name),
                    });
                }
                catch (Exception ex)
                {
                    LOG.Error("SC配置项错误，" + node.Name, ex);
                }
            }

            return result;
        }

        string GetNodeText(XmlNode parent, string child, string defaultValue = "")
        {
            return (parent.Attributes[child] == null) ? defaultValue : parent.Attributes[child].Value;
        }

    }
}
