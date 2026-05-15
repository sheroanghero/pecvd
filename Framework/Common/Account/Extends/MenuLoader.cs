using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MECF.Framework.Common.Account.Extends
{
    public class MenuLoader : XmlLoader
    {
        public List<AppMenu> MenuList
        {
            get { return _Menulist; }
            set { _Menulist = value; }
        }

        public MenuLoader(string p_strPath)
            : base(p_strPath)
        {
        }

        protected override void AnalyzeXml()
        {
            if (this.m_xdoc != null)
            {
                List<AppMenu> menulist = TranslateMenus(this.m_xdoc.Root);
                this._Menulist = menulist;
            }
        }

        private List<AppMenu> TranslateMenus(XElement pElement)
        {
            List<AppMenu> Menulist = new List<AppMenu>();

            var results = from r in pElement.Elements("menuItem") select r;
            foreach (var result in results)
            {
                string strMenuID = string.Empty;
                string strMenuViewMode = string.Empty;
                string strMenuResKey = string.Empty;
                if (!result.HasElements)
                {
                    strMenuID = result.Attribute("id").Value;
                    strMenuViewMode = result.Attribute("viewmodel").Value;
                    strMenuResKey = result.Attribute("resKey").Value;
                    AppMenu item = new AppMenu(strMenuID, strMenuViewMode, strMenuResKey, null);
                    if (result.Attribute("System") != null)
                        item.System = result.Attribute("System").Value;
                    if (result.Attribute("AlarmModule") != null)
                        item.AlarmModule = result.Attribute("AlarmModule").Value;
                    if (result.Attribute("type") != null)
                        item.Type = result.Attribute("type").Value;
                    Menulist.Add(item);
                }
                else
                {
                    strMenuID = result.Attribute("id").Value;
                    strMenuResKey = result.Attribute("resKey").Value;
                    List<AppMenu> subMenuList = new List<AppMenu>();
                    subMenuList = TranslateMenus(result);
                    AppMenu item = new AppMenu(strMenuID, strMenuViewMode, strMenuResKey, subMenuList);

                    if (result.Attribute("System") != null)
                        item.System = result.Attribute("System").Value;
                    if (result.Attribute("AlarmModule") != null)
                        item.AlarmModule = result.Attribute("AlarmModule").Value;
                    if (result.Attribute("type") != null)
                        item.Type = result.Attribute("type").Value;
                    Menulist.Add(item);
                }
            }
            return Menulist;
        }

        private List<AppMenu> _Menulist = new List<AppMenu>();
    }
}
