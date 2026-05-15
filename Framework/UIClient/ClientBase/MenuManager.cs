using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.Account.Extends;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class MenuManager
    {
        public List<AppMenu> MenuItems { get; private set; }
 
        #region Operation

        public void LoadMenu(List<AppMenu> menuitems)
        {
            List < AppMenu > removeList = new List<AppMenu>();

            foreach (AppMenu menuitem in menuitems)
            {
                if (!string.IsNullOrEmpty(menuitem.System) && menuitem.System != "System")
                {
                    if (ModuleManager.ModuleInfos.ContainsKey(menuitem.System) &&
                        !ModuleManager.ModuleInfos[menuitem.System].IsInstalled)
                    {
                        removeList.Add(menuitem);
                    }

                }

                if (menuitem.MenuItems != null)
                {
                    for (int i = menuitem.MenuItems.Count - 1; i >= 0; i--)
                    {
                        var submenuitem = menuitem.MenuItems[i];
                        submenuitem.Parent = menuitem;
                        if (submenuitem.Permission == 1) //remove menu if permission is "None"
                            menuitem.MenuItems.Remove(submenuitem);

                        if (!string.IsNullOrEmpty(submenuitem.System) && submenuitem.System != "System")
                        {
                            if (ModuleManager.ModuleInfos.ContainsKey(submenuitem.System) &&
                                !ModuleManager.ModuleInfos[submenuitem.System].IsInstalled)
                            {
                                menuitem.MenuItems.Remove(submenuitem);
                            }

                        }
                    }
                }
            }

            foreach (var appMenu in removeList)
            {
                menuitems.Remove(appMenu);
            }

            this.MenuItems = menuitems;
        }

        public void FilterMenus(List<string> pSystems)
        {
            if (pSystems == null || this.MenuItems == null)
                return;

            foreach (AppMenu menuitem in this.MenuItems)
            {
                if (menuitem.MenuItems != null)
                {
                    for (int i = menuitem.MenuItems.Count - 1; i >= 0; i--)
                    {
                        var submenuitem = menuitem.MenuItems[i];
                       //filter
                    }
                }
            }
        }

        private bool IsExist(List<string> pSource, string pMatch)
        {
            foreach (var item in pSource)
            {
                if (pMatch.IndexOf(item) >= 0)
                    return true;
            }
            return false;
        }

        public List<AppMenu> GetAllClone()
        {
            if (this.MenuItems == null)
                return null;

            List<AppMenu> cloneList = new List<AppMenu>();

            foreach (var item in this.MenuItems)
            {
                cloneList.Add((AppMenu)item.Clone(item.Parent));
            }

            return cloneList;
        }

        #endregion
    }
}
