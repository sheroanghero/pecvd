using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Aitex.Core.Util;
using MECF.Framework.Common.Account.Extends;

namespace Aitex.Core.Account
{
    [ServiceContract]
    public interface IAccountService
    {
        [OperationContract]
        LoginResult Login(string accountId, string password);

        [OperationContract]
        void Logout(string accountId);

        [OperationContract]
        CreateAccountResult CreateAccount(Account newAccount);

        [OperationContract]
        DeleteAccountResult DeleteAccount(string accountId);

        [OperationContract]
        GetAccountInfoResult GetAccountInfo(string accountId);

        [OperationContract]
        void RegisterViews(List<string> views);

        [OperationContract]
        UpdateAccountResult UpdateAccount(Account account);

        [OperationContract]
        GetAccountListResult GetAccountList();

        [OperationContract]
        ChangePwdResult ChangePassword(string accountId, string newPassword);

        [OperationContract]
        List<Account> GetLoginUsers();

        [OperationContract]
        void KickUserOut(string accountId, string reason);

        [OperationContract]
        SerializableDictionary<string, SerializableDictionary<string, ViewPermission>> GetAllRolesPermission();

        [OperationContract]
        bool SaveAllRolesPermission(Dictionary<string, Dictionary<string, ViewPermission>> data);

        [OperationContract]
        SerializableDictionary<string, string> GetAllViewList();

        [OperationContract]
        IEnumerable<string> GetAllRoles();

        [OperationContract]
        void CheckAlive(string accountId);

        [OperationContract]
        string GetProcessViewPermission();

        [OperationContract]
        bool SaveProcessViewPermission(string viewXML);


        //---------------------------------------------------------------
        //extended account management

        [OperationContract]
        List<Role> GetAllRoleList();

        [OperationContract]
        List<AccountEx> GetAllAccountExList();

        /// <summary>
        /// Return all roles except super role       
        /// </summary>
        [OperationContract] 
        List<Role> GetRoles();

        /// <summary>
        /// Return all accounts except super account       
        /// </summary>    
        [OperationContract]
        List<AccountEx> GetAccounts();

        [OperationContract]
        bool UpdateRole(Role role);

        [OperationContract]
        bool DeleteRole(string roleId);

        [OperationContract]
        List<AppMenu> GetMenusByRole(string roleId, List<AppMenu> lstMenu);

        [OperationContract]
        int GetMenuPermission(string roleId, string menuName);
        
        [OperationContract]
        bool UpdateAccountEx(AccountEx account);

        [OperationContract]
        bool DeleteAccountEx(string accountId);

        [OperationContract]
        LoginResult LoginEx(string accountId, string password, string role);

        [OperationContract]
        void LogoutEx(string accountId, string loginId );
    }
}
