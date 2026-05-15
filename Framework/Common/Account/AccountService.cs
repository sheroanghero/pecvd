using System;
using System.Collections.Generic;
using System.Xml;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Key;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Common.Util;
using MECF.Framework.Common.Account;
using MECF.Framework.Common.Account.Extends;


namespace Aitex.Core.Account
{
    public sealed class AccountService : IAccountService
    {
        public string Module = "System";

        public LoginResult Login(string accountId, string accountPwd)
        {
                if (KeyManager.Instance.IsExpired)
                {
                    EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not login");
                    return new LoginResult(){ActSucc = false,Description = "Software is expired"};
                }

            EV.PostInfoLog(Module, string.Format("User {0} try to login System.", accountId));
            return AccountManager.Login(accountId, accountPwd);
        }

        public void Logout(string accountId)
        {
            EV.PostInfoLog(Module, string.Format("User {0} logout sytem.", accountId));
            try
            {
    //            Authorization.Exit(accountId);
            }
            catch { }

            AccountManager.Logout(accountId);            
        }

        public GetAccountInfoResult GetAccountInfo(string accountId)
        {
            return AccountManager.GetAccountInfo(accountId);
        }

        public void RegisterViews(List<string> views)
        {
            AccountManager.RegisterViews(views);
        }

        public ChangePwdResult ChangePassword(string accountId, string newPassword)
        {
                if (KeyManager.Instance.IsExpired)
                {
                    EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                    return new ChangePwdResult();
                }

            EV.PostInfoLog(Module, string.Format("Change user {0} password.", accountId));
            LOG.Write(string.Format("Account operation, change user {0} password.", accountId));
            return AccountManager.ChangePassword(accountId, newPassword);
        }

        public CreateAccountResult CreateAccount(Account newAccount)
        {
                if (KeyManager.Instance.IsExpired)
                {
                    EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                    return new CreateAccountResult();
                }

            EV.PostInfoLog(Module, string.Format("Create account{0}.", newAccount));
            LOG.Write(string.Format("Account operation, Create user {0}.", newAccount.AccountId));
            return AccountManager.CreateAccount(newAccount);
        }

        public DeleteAccountResult DeleteAccount(string accountId)
        {
                if (KeyManager.Instance.IsExpired)
                {
                    EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                    return new DeleteAccountResult();
                }
            EV.PostInfoLog(Module, string.Format("Delete account {0}.", accountId));
            return AccountManager.DeleteAccount(accountId);
        }

        public UpdateAccountResult UpdateAccount(Account account)
        {
                if (KeyManager.Instance.IsExpired)
                {
                    EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                    return new UpdateAccountResult();
                }
            EV.PostInfoLog(Module, string.Format("Update {0} account information.", account));
            return AccountManager.UpdateAccount(account);
        }

        public GetAccountListResult GetAccountList()
        {
            return AccountManager.GetAccountList();
        }

        public List<Account> GetLoginUsers()
        {
            return AccountManager.GetLoginUserList();
        }


        public void KickUserOut(string accountId, string kickoutReason)
        {
            EV.PostInfoLog(Module, string.Format("Force user {0} logout system，reason：{1}.", accountId, kickoutReason));
            try
            {
           //     Authorization.Exit(accountId);
            }
            catch { }
            AccountManager.Kickout(accountId, kickoutReason);
        }

        public SerializableDictionary<string, SerializableDictionary<string, ViewPermission>> GetAllRolesPermission()
        {
            return AccountManager.GetAllRolesPermission();
        }

        public bool SaveAllRolesPermission(Dictionary<string, Dictionary<string, ViewPermission>> data)
        {
                if (KeyManager.Instance.IsExpired)
                {
                    EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                    return false;
                }
            return AccountManager.SaveAllRolesPermission(data);
        }

        public SerializableDictionary<string, string> GetAllViewList()
        {
            return AccountManager.GetAllViewList();
        }

        public IEnumerable<string> GetAllRoles()
        {
            return AccountManager.GetAllRoles();
        }

        public void CheckAlive(string accountId)
        {
            AccountManager.CheckAlive(accountId);
        }

        
    
        public string GetProcessViewPermission()
        {
            string recipePermissionFile = System.IO.Path.Combine(PathManager.GetCfgDir(), "RolePermission.xml");
            XmlDocument _xmlRecipeFormat = new XmlDocument();
            try
            {
                _xmlRecipeFormat.Load(recipePermissionFile);
                return _xmlRecipeFormat.InnerXml;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return "<Aitex></Aitex>";
            }
            finally
            { 
                
            }
        }

        public bool SaveProcessViewPermission(string viewXML)
        {
            try
            {
                string recipePermissionFile = System.IO.Path.Combine(PathManager.GetCfgDir(), "RolePermission.xml");

                XmlDocument _xmlRecipeFormat = new XmlDocument(); 
                _xmlRecipeFormat.LoadXml(viewXML);
                 XmlTextWriter writer = new XmlTextWriter(recipePermissionFile, null);
                writer.Formatting = Formatting.Indented;
                _xmlRecipeFormat.Save(writer);
                writer.Close(); 
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        public List<Role> GetAllRoleList()
        {
            return AccountExManager.Instance.RoleLoader.RoleList;
        }

        public List<AccountEx> GetAllAccountExList()
        {
            return AccountExManager.Instance.RoleLoader.AccountList;
        }

        public List<Role> GetRoles()
        {
            return AccountExManager.Instance.RoleLoader.GetRoles();
        }

        public List<AccountEx> GetAccounts()
        {
            return AccountExManager.Instance.RoleLoader.GetAccounts();
        }

        public bool UpdateRole(Role role)
        {
            return AccountExManager.Instance.RoleLoader.UpdateRole(role);
        }

        public bool DeleteRole(string roleId)
        {
            return AccountExManager.Instance.RoleLoader.DeleteRole(roleId);
        }

        public List<AppMenu> GetMenusByRole(string roleId, List<AppMenu> lstMenu)
        {
            return AccountExManager.Instance.RoleLoader.GetMenusByRole(roleId, lstMenu) ;
        }

        public int GetMenuPermission(string roleId, string menuName)
        {
            return AccountExManager.Instance.RoleLoader.GetMenuPermission(roleId, menuName);
        }

        public bool UpdateAccountEx(AccountEx account)
        {
            return AccountExManager.Instance.RoleLoader.UpdateAccount(account);
        }

        public bool DeleteAccountEx(string accountId)
        {
            return AccountExManager.Instance.RoleLoader.DeleteAccount(accountId);
        }

        public LoginResult LoginEx(string accountId, string password, string role)
        {
            return AccountExManager.Instance.AuthLogin(accountId, password, role);
        }

        public void LogoutEx(string accountId, string loginId)
        {
            AccountExManager.Instance.Logout(accountId, loginId );
        }
    }
}
