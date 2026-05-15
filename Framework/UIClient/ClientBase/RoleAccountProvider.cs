using System.Collections.Generic;
using MECF.Framework.Common.Account.Extends;
using MECF.Framework.UI.Core.Accounts;
using OpenSEMI.ClientBase.ServiceProvider;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class RoleAccountProvider : IProvider
    {
        private static RoleAccountProvider _Instance = null;
        public static RoleAccountProvider Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new RoleAccountProvider();
                    _Instance.Create();
                }
                return _Instance;
            }
        }

        public void Create()
        {

        }

        public List<AccountEx> GetAccounts()
        {
            return AccountClient.Instance.Service.GetAccounts();
        }

        public bool UpdateAccount(AccountEx p_newAccount)
        {
            return AccountClient.Instance.Service.UpdateAccountEx(p_newAccount);
        }

        public bool DeleteAccount(string p_strUserID)
        {
            return AccountClient.Instance.Service.DeleteAccountEx(p_strUserID);
        }

        public List<Role> GetRoles()
        {
            return AccountClient.Instance.Service.GetRoles();
        }

        public bool UpdateRole(Role p_newRole)
        {
            return AccountClient.Instance.Service.UpdateRole(p_newRole);
        }

        public bool DeleteRole(string p_strRoleID)
        {
            return AccountClient.Instance.Service.DeleteRole(p_strRoleID);
        }
 
        public List<AppMenu> GetMenusByRole(string roleid, List<AppMenu> menulist)
        {
            return AccountClient.Instance.Service.GetMenusByRole(roleid, menulist);
        }

        public int GetMenuPermission(string roleid, string menuName)
        {
            return AccountClient.Instance.Service.GetMenuPermission(roleid, menuName);
        }
    }
}
