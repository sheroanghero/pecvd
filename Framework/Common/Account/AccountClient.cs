using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Account;
using Aitex.Core.Util;
using Aitex.Core.WCF;
using MECF.Framework.Common.Account.Extends;
using LoginResult = Aitex.Core.Account.LoginResult;

namespace MECF.Framework.UI.Core.Accounts
{
    public class AccountClient : Singleton<AccountClient>
    {
        public bool InProcess { get; set; }
        public Account CurrentUser { get; set; }

        private IAccountService _service;
        public IAccountService Service
        {
            get
            {
                if (_service == null)
                {
                    if (InProcess)
                        _service = new AccountService();
                    else
                    {
                        _service = new AccountServiceClient();
                    }
                }

                return _service;
            }
        }
    }

    public class AccountServiceClient : ServiceClientWrapper<IAccountService>, IAccountService
    {
        public AccountServiceClient()
            : base("Client_IAccountService", "AccountService")
        {
        }

        /// <summary>
        /// user login verify
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="accountPwd"></param>
        /// <returns></returns>
        public LoginResult Login(string accountId, string accountPwd)
        {
            LoginResult result = null;
            Invoke(svc => { result = svc.Login(accountId, accountPwd); });
            return result;
        }

        LoginResult IAccountService.Login(string accountId, string password)
        {
            return Login(accountId, password);
        }

        /// <summary>
        /// user logout system
        /// </summary>
        /// <param name="accountId"></param>
        public void Logout(string accountId)
        {
            Invoke(svc => { svc.Logout(accountId); });
        }

        /// <summary>
        /// get user data by accountId
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public GetAccountInfoResult GetAccountInfo(string accountId)
        {
            GetAccountInfoResult result = null;
            Invoke(svc => { result = svc.GetAccountInfo(accountId); });
            return result;
        }

        public void RegisterViews(List<string> views)
        {

            Invoke(svc => { svc.RegisterViews(views); });

        }

        /// <summary>
        /// change account password
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="newPassword"></param>       
        public ChangePwdResult ChangePassword(string accountId, string newPassword)
        {
            ChangePwdResult result = null;
            Invoke(svc => { result = svc.ChangePassword(accountId, newPassword); });
            return result;
        }

        /// <summary>
        /// create account
        /// </summary>
        /// <param name="newAccount"></param>
        /// <returns></returns>
        public CreateAccountResult CreateAccount(Account newAccount)
        {
            CreateAccountResult result = null;
            Invoke(svc => { result = svc.CreateAccount(newAccount); });
            return result;
        }

        /// <summary>
        /// Administrator user calls this method to delete an account.
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public DeleteAccountResult DeleteAccount(string accountId)
        {
            DeleteAccountResult result = null;
            Invoke(svc => { result = svc.DeleteAccount(accountId); });
            return result;
        }

        /// <summary>
        /// Update account information
        /// </summary>
        /// <param name="accountList"></param>
        /// <returns></returns>
        public UpdateAccountResult UpdateAccount(Account account)
        {
            UpdateAccountResult result = null;
            Invoke(svc => { result = svc.UpdateAccount(account); });
            return result;
        }

        /// <summary>
        /// get account list
        /// </summary>
        /// <returns></returns>
        public GetAccountListResult GetAccountList()
        {
            GetAccountListResult result = null;
            Invoke(svc => { result = svc.GetAccountList(); });
            return result;
        }

        /// <summary>
        /// 获取当前所有已登录的用户列表
        /// </summary>
        /// <returns></returns>
        public List<Account> GetLoginUsers()
        {
            List<Account> result = null;
            Invoke(svc => { result = svc.GetLoginUsers(); });
            return result;
        }

        /// <summary>
        /// 强制注销用户登录
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="kickoutReason"></param>
        public void KickUserOut(string accountId, string kickoutReason)
        {
            Invoke(svc => { svc.KickUserOut(accountId, kickoutReason); });
        }

        /// <summary>
        /// get all roles' perrmission
        /// </summary>
        /// <returns></returns>
        public SerializableDictionary<string, SerializableDictionary<string, ViewPermission>> GetAllRolesPermission()
        {
            SerializableDictionary<string, SerializableDictionary<string, ViewPermission>> result = null;
            Invoke(svc => { result = svc.GetAllRolesPermission(); });
            return result;
        }

        /// <summary>
        /// save all roles' permission
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool SaveAllRolesPermission(Dictionary<string, Dictionary<string, ViewPermission>> data)
        {
            bool result = false;
            Invoke(svc => { result = svc.SaveAllRolesPermission(data); });
            return result;
        }

        /// <summary>
        /// get all view's list
        /// </summary>
        /// <returns></returns>
        public SerializableDictionary<string, string> GetAllViewList()
        {
            SerializableDictionary<string, string> result = null;
            Invoke(svc => { result = svc.GetAllViewList(); });
            return result;
        }

        /// <summary>
        /// Get all defined roles 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetAllRoles()
        {
            IEnumerable<string> result = null;
            Invoke(svc => { result = svc.GetAllRoles(); });
            return result;
        }

        /// <summary>
        /// 检查账号是否仍旧有效
        /// </summary>
        /// <param name="accountId"></param>
        public void CheckAlive(string accountId)
        {
            Invoke(svc => { svc.CheckAlive(accountId); });
        }

        /// <summary>
        /// 获取流程视图的许可
        /// </summary>
        /// <returns></returns>
        public string GetProcessViewPermission()
        {
            string result = null;
            Invoke(svc => { result = svc.GetProcessViewPermission(); });
            return result;
        }

        /// <summary>
        /// 保存流程视图的许可
        /// </summary>
        /// <param name="viewXML"></param>
        /// <returns></returns>
        public bool SaveProcessViewPermission(string viewXML)
        {
            bool result = false;
            Invoke(svc => { result = svc.SaveProcessViewPermission(viewXML); });
            return result;
        }

        public List<Role> GetAllRoleList()
        {
            var result = new List<Role>();
            Invoke(svc => { result = svc.GetAllRoleList(); });
            return result;
        }

        public List<AccountEx> GetAllAccountExList()
        {
            var result = new List<AccountEx>();
            Invoke(svc => { result = svc.GetAllAccountExList(); });
            return result;
        }

        public List<Role> GetRoles()
        {
            var result = new List<Role>();
            Invoke(svc => { result = svc.GetRoles(); });
            return result;
        }

        public List<AccountEx> GetAccounts()
        {
            var result = new List<AccountEx>();
            Invoke(svc => { result = svc.GetAccounts(); });
            return result;
        }

        public bool UpdateRole(Role p_newRole)
        {
            bool result = false;
            Invoke(svc => { result = svc.UpdateRole(p_newRole); });
            return result;
        }

        public bool DeleteRole(string p_strRoleID)
        {
            bool result = false;
            Invoke(svc => { result = svc.DeleteRole(p_strRoleID); });
            return result;
        }

        public List<AppMenu> GetMenusByRole(string roleid, List<AppMenu> menulist)
        {
            var result = new List<AppMenu>();
            Invoke(svc => { result = svc.GetMenusByRole(roleid, menulist); });
            return result;
        }

        public int GetMenuPermission(string roleId, string menuName)
        {
            int result = 0;
            Invoke(svc => { result = svc.GetMenuPermission(roleId, menuName); });
            return result;
        }

        public bool UpdateAccountEx(AccountEx account)
        {
            bool result = false;
            Invoke(svc => { result = svc.UpdateAccountEx(account); });
            return result;
        }

        public bool DeleteAccountEx(string accountId)
        {
            bool result = false;
            Invoke(svc => { result = svc.DeleteAccountEx(accountId); });
            return result;
        }

        public LoginResult LoginEx(string accountId, string password, string role)
        {
            var result = new LoginResult();
            Invoke(svc => { result = svc.LoginEx(accountId, password, role); });
            return result;
        }

        public void LogoutEx(string accountId, string loginId)
        {

            Invoke(svc => { svc.LogoutEx(accountId, loginId); });

        }
    }
}

