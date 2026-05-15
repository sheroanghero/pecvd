using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Account.Extends;
using MECF.Framework.UI.Client.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Configs.Roles
{
    public class RoleManager
    {
        private RoleManager()
        {
        }

        public bool Initialize()
        {
            _RoleContainer.Clear();
            List<Common.Account.Extends.Role> roles = RoleAccountProvider.Instance.GetRoles();
            if (roles == null)
            {
                LOG.Error("GetRoles method failed,in RoleManager");
                return false;
            }

            List<AppMenu> menus = BaseApp.Instance.MenuLoader.MenuList;
            this._listMenuItems = menus;
            if (menus == null)
            {
                LOG.Error("GetMenus method failed,in RoleManager");
                return false;
            }

            foreach (Common.Account.Extends.Role r in roles)
            {
                RoleItem role = new RoleItem(r);
                role.DisplayRoleName = role.RoleName;
                role.DisplayAutoLogoutTime = role.AutoLogoutTime;
                role.DisplayIsAutoLogout = role.IsAutoLogout;
                role.DisplayDescription = role.Description;

                //Parse menu permission
                MenuPermission menuPermissionParse = new MenuPermission();
                menuPermissionParse.ParsePermission(r.MenuPermission);

                MenuInfo mp = new MenuInfo() { ID = "Header" };
                mp.Name = "Header";
                mp.EnumPermission = menuPermissionParse.GetPermissionByPageID("Header");
                role.AddMenuInfo(mp);

                //Get Menu information
                foreach (AppMenu topMenuItem in menus)
                {
                    foreach (AppMenu subMenuItem in topMenuItem.MenuItems)
                    {
                        mp = new MenuInfo() { ID = subMenuItem.MenuID };
                        mp.Name = topMenuItem.ResKey + "." + subMenuItem.ResKey; //Application.Current.FindResource(subMenuItem.ResKey) as string;
                        mp.EnumPermission = menuPermissionParse.GetPermissionByPageID(subMenuItem.MenuID);
                        role.AddMenuInfo(mp);
                    }
                }
                _RoleContainer.Add(role);
            }
            return true;
        }

        public List<RoleItem> GetAllRoles()
        {
            return _RoleContainer.ToList();
        }

        public bool AddRole(RoleItem r)
        {
            RoleItem existRole = GetRoleByName(r.RoleName);
            if (existRole != null)
            {
                LOG.Info("the role already exists, in RoleManager");
                return false;
            }
            existRole = GetRoleByID(r.RoleID);
            if (existRole != null)
            {
                LOG.Info("the role already exists, in RoleManager");
                return false;
            }
            _RoleContainer.Add(r);

            return true;
        }

        public string GenerateRoleID()
        {
            RoleItem r = GetRoleByID(_RoleNum.ToString());
            while (r != null)
            {
                _RoleNum++;
                r = GetRoleByID(_RoleNum.ToString());
            }

            return _RoleNum.ToString();
        }

        public RoleItem GetRoleByID(string ID)
        {
            return _RoleContainer.FirstOrDefault(t => t.RoleID == ID);
        }

        public RoleItem GetRoleByName(string name)
        {
            return _RoleContainer.FirstOrDefault(t => t.RoleName == name);
        }

        public RoleItem CreateRole()
        {
            RoleItem r = new RoleItem(GenerateRoleID());

            r.DisplayRoleName = r.RoleName = string.Empty;
            r.DisplayAutoLogoutTime = r.AutoLogoutTime = 10;
            r.DisplayIsAutoLogout = r.IsAutoLogout = false;
            r.DisplayDescription = r.Description = string.Empty;

            r.MenuCollection.Add(new MenuInfo() { ID = "Header", Name = "Header", EnumPermission = MenuPermissionEnum.MP_NONE });

            foreach (AppMenu topMenuItem in _listMenuItems)
            {
                foreach (AppMenu subMenuItem in topMenuItem.MenuItems)
                {
                    MenuInfo mp = new MenuInfo()
                    {
                        ID = subMenuItem.MenuID,
                        Name = topMenuItem.ResKey + "." + subMenuItem.ResKey, //Application.Current.FindResource(subMenuItem.ResKey) as string,
                        EnumPermission = MenuPermissionEnum.MP_NONE
                    };

                    r.MenuCollection.Add(mp);
                }
            }

            return r;
        }

        public RoleItem CreateRole(RoleItem role)
        {
            RoleItem newRole = new RoleItem(GenerateRoleID())
            {
                RoleName = role.RoleName,
                AutoLogoutTime = role.AutoLogoutTime,
                IsAutoLogout = role.IsAutoLogout,
                Description = role.Description,
                DisplayRoleName = role.DisplayRoleName,
                DisplayAutoLogoutTime = role.DisplayAutoLogoutTime,
                DisplayIsAutoLogout = role.DisplayIsAutoLogout,
                DisplayDescription = role.DisplayDescription
            };

            foreach (MenuInfo mInfo in role.MenuCollection)
            {
                newRole.AddMenuInfo(mInfo.Clone());
            }

            return newRole;
        }

        public RoleItem CloneRole(RoleItem role)
        {
            RoleItem newRole = new RoleItem(role.RoleID)
            {
                RoleName = role.RoleName,
                AutoLogoutTime = role.AutoLogoutTime,
                IsAutoLogout = role.IsAutoLogout,
                Description = role.Description,
                DisplayRoleName = role.DisplayRoleName,
                DisplayAutoLogoutTime = role.DisplayAutoLogoutTime,
                DisplayIsAutoLogout = role.DisplayIsAutoLogout,
                DisplayDescription = role.DisplayDescription
            };

            foreach (MenuInfo mInfo in role.MenuCollection)
            {
                newRole.AddMenuInfo(mInfo.Clone());
            }

            return newRole;
        }

        public RoleItem CloneRole(string strRoleID)
        {
            RoleItem orignalRole = GetRoleByID(strRoleID);
            if (null == orignalRole)
                return null;

            RoleItem newRole = new RoleItem(strRoleID)
            {
                AutoLogoutTime = orignalRole.AutoLogoutTime,
                IsAutoLogout = orignalRole.IsAutoLogout,
                RoleName = orignalRole.RoleName,
                Description = orignalRole.Description,
                DisplayRoleName = orignalRole.DisplayRoleName,
                DisplayAutoLogoutTime = orignalRole.DisplayAutoLogoutTime,
                DisplayIsAutoLogout = orignalRole.DisplayIsAutoLogout,
                DisplayDescription = orignalRole.DisplayDescription
            };

            foreach (MenuInfo mInfo in orignalRole.MenuCollection)
            {
                newRole.AddMenuInfo(mInfo.Clone());
            }

            return newRole;
        }

        public bool CheckAvailable(RoleItem role)
        {
            if (role == null)
                return false;

            foreach (RoleItem r in _RoleContainer)
            {
                if (role.RoleName == r.RoleName && role.RoleID != r.RoleID)
                {
                    LOG.Info("Check account available fail because the name already exists, in RoleManager");
                    return false;
                }
            }

            return true;
        }

        public bool SaveRole(RoleItem pRoletoSave)
        {
            if (!CheckAvailable(pRoletoSave))
                return false;


            Common.Account.Extends.Role r = new Common.Account.Extends.Role(
                pRoletoSave.RoleID,
                pRoletoSave.RoleName,
                pRoletoSave.IsAutoLogout,
                pRoletoSave.AutoLogoutTime,
                MenuPermission.PermissionToString(pRoletoSave.MenuCollection.ToList()),
                pRoletoSave.Description
                );

            if (RoleAccountProvider.Instance.UpdateRole(r))
            {
                RoleItem orignalRole = GetRoleByID(pRoletoSave.RoleID);
                if (null == orignalRole)
                {
                    RoleItem NewRole = CloneRole(pRoletoSave);
                    AddRole(NewRole);
                    return true;
                }

                orignalRole.RoleName = pRoletoSave.RoleName;
                orignalRole.IsAutoLogout = pRoletoSave.IsAutoLogout;
                orignalRole.AutoLogoutTime = pRoletoSave.AutoLogoutTime;
                orignalRole.Description = pRoletoSave.Description;
                orignalRole.MenuCollection.Clear();
                foreach (MenuInfo mInfo in pRoletoSave.MenuCollection)
                {
                    orignalRole.AddMenuInfo(mInfo.Clone());
                    mInfo.ComboBoxSaved = true;
                }
            }
            else
            {
                LOG.Error("UpdateRoles method failed, in RoleManager");
                return false;
            }

            return true;
        }

        public bool DeleteRole(string strRoleID)
        {
            RoleItem r = GetRoleByID(strRoleID);
            if (r != null)
            {
                if (RoleAccountProvider.Instance.DeleteRole(strRoleID))
                {
                    _RoleContainer.Remove(r);
                    return true;
                }
                else
                {
                    LOG.Error("DeleteRole method failed, in RoleManager");
                }
            }
            else
            {
                LOG.Warning("Can not find the role to delete, in RoleManager");
            }

            return false;
        }


        private static RoleManager _Instance = null;
        public static RoleManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new RoleManager();
                }

                return _Instance;
            }
        }

        private ObservableCollection<RoleItem> _RoleContainer = new ObservableCollection<RoleItem>();
        public ObservableCollection<RoleItem> RoleContainer
        {
            get { return _RoleContainer; }
        }

        private static int _RoleNum = 0;
        private List<AppMenu> _listMenuItems = null;
    }

    public class MenuPermission
    {
        public void ParsePermission(string strPermission)
        {
            try
            {
                strPermission = strPermission.Replace("\n", "").Replace(" ", "");
                int groupIndex = strPermission.IndexOf(';');
                if (groupIndex == -1)
                {
                    AddToDictionary(strPermission);
                    return;
                }

                string strGroup = strPermission.Substring(0, groupIndex);
                AddToDictionary(strGroup);

                ParsePermission(strPermission.Substring(groupIndex + 1));

            }
            catch (System.Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public MenuPermissionEnum GetPermissionByPageID(string strPageID)
        {
            if (MenuPermissionDictionary.ContainsKey(strPageID))
            {
                return MenuPermissionDictionary[strPageID];
            }

            return MenuPermissionEnum.MP_READ_WRITE;
        }

        public static string PermissionToString(List<MenuInfo> listPermission)
        {
            string strResult = "";

            if (listPermission == null)
                return strResult;

            foreach (MenuInfo info in listPermission)
            {
                strResult += info.ID;
                strResult += ",";
                strResult += info.IndexPermission;
                strResult += ";";
            }

            return strResult;
        }

        private void AddToDictionary(string strGroupName)
        {
            int Index = strGroupName.IndexOf(',');
            if (Index == -1)
                return;

            string strMenuID = strGroupName.Substring(0, Index);
            string strMenuPermision = strGroupName.Substring(Index + 1);

            MenuPermissionDictionary.Add(strMenuID, (MenuPermissionEnum)Convert.ToInt32(strMenuPermision));
        }

        public MenuPermissionEnum this[string strPageID]
        {
            get
            {
                return GetPermissionByPageID(strPageID);
            }

        }

        private Dictionary<string, MenuPermissionEnum> _dicPermission = new Dictionary<string, MenuPermissionEnum>();
        public Dictionary<string, MenuPermissionEnum> MenuPermissionDictionary
        {
            get { return _dicPermission; }
        }
    }
}
