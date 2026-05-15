using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Aitex.Core.UI.View.Frame
{
    public class ViewItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ViewClass { get; set; }
        public string AssemblyName { get; set; }
        public bool IsSelected { get; set; }

        public List<ViewItem> SubView { get; set; }
        public Dictionary<string, string> GlobalName
        {
            get; set; }
    }

    class UILayoutParser
    {
        public List<string> ViewIdList { get; set; }
        public ViewItem TitleView { get; private set; }
        public List<ViewItem> NavigationView { get; private set; }

        public int PreferTopPanelHeight = -1;
        public int PreferCenterPanelHeight = -1;
        public int PreferBottomPanelHeight = -1;
        public int PreferWidth = -1;


        public UILayoutParser(string xmlFile)
        {
            ViewIdList = new List<string>();
            ParseLayout(xmlFile);
        }

        void ParseLayout(string xmlFile)
        {
            if (String.IsNullOrEmpty(xmlFile))
                throw new ApplicationException("The UI layout config filename is empty,can not initialize UI");

            if (!File.Exists(xmlFile))
                throw new ApplicationException("Did not find the UI layout config file, " + xmlFile);

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(xmlFile);

                var node =   doc.SelectSingleNode("/MECFUI") as XmlNode;

                int parsed = -1;
                if (node.Attributes["TopPanelHeight"] != null && int.TryParse(node.Attributes["TopPanelHeight"].Value, out parsed))
                {
                    PreferTopPanelHeight = parsed;
                }
                if (node.Attributes["CenterPanelHeight"] != null && int.TryParse(node.Attributes["CenterPanelHeight"].Value, out parsed))
                {
                    PreferCenterPanelHeight = parsed;
                }
                if (node.Attributes["BottomPanelHeight"] != null && int.TryParse(node.Attributes["BottomPanelHeight"].Value, out parsed))
                {
                    PreferBottomPanelHeight = parsed;
                }
                if (node.Attributes["Width"] != null && int.TryParse(node.Attributes["Width"].Value, out parsed))
                {
                    PreferWidth = parsed;
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("UI layout config file " + xmlFile + " is not valid, " + ex.Message);
            }

            TitleView = ParseNode(doc.SelectSingleNode("/MECFUI/Title"));

            foreach (XmlNode nav in doc.SelectNodes("/MECFUI/Navigation"))
            {
                ViewItem menu = ParseNode(nav);
                if (menu == null)
                    continue;

                foreach (XmlNode ele in nav.SelectNodes("SubView"))
                {
                    if (menu.SubView == null)
                        menu.SubView = new List<ViewItem>();

                    ViewItem item = ParseNode(ele);
                    if (item != null)
                    {
                        menu.SubView.Add(item);
                        ViewIdList.Add(item.Id);
                    }
                }

                if (NavigationView == null)
                    NavigationView = new List<ViewItem>();
                
                NavigationView.Add(menu);
            }
        }

        ViewItem ParseNode(XmlNode node)
        {
            if (node == null)
                return null;

            ViewItem item = new ViewItem();
            item.GlobalName = new Dictionary<string, string>();

            if (node.Attributes["Id"] != null)
                item.Id = node.Attributes["Id"].Value;

            if (node.Attributes["Name"] != null)
                item.Name = node.Attributes["Name"].Value;

            if (node.Attributes["Name.en-US"] != null)
                item.GlobalName["en-US"] = node.Attributes["Name.en-US"].Value;

            if (node.Attributes["Name.zh-CN"] != null)
                item.GlobalName["zh-CN"] = node.Attributes["Name.zh-CN"].Value;

            if (node.Attributes["ViewClass"] != null)
                item.ViewClass = node.Attributes["ViewClass"].Value;

            if (node.Attributes["Assembly"] != null)
                item.AssemblyName = node.Attributes["Assembly"].Value;

            return item;
        }


    }
}
