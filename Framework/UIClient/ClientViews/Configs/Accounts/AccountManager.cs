using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MECF.Framework.Common.Account.Extends;
using MECF.Framework.UI.Client.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Configs.Accounts
{
    public class AccountManager
    {
        private static int s_AccountNum = 0;

        #region Property define
        private ObservableCollection<AccountItem> m_AccountContainer = new ObservableCollection<AccountItem>();
        public ObservableCollection<AccountItem> AccountContainer
        {
            get { return m_AccountContainer; }
        }

        /// <summary>
        /// Store all the roles
        /// </summary>
        private List<Common.Account.Extends.Role> m_RoleList;
        public List<Common.Account.Extends.Role> RoleList
        {
            get { return m_RoleList; }
        }

        #endregion


        #region Functions define

        /// <summary>
        /// Singleton implement
        /// </summary>
        private AccountManager()
        {
            m_RoleList = new List<Common.Account.Extends.Role>();
        }

        private static AccountManager m_Instance = null;
        public static AccountManager Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new AccountManager();
                return m_Instance;
            }
        }

        /// <summary>
        /// (1)Initialize information about role
        /// (2)This method must be the first called
        /// </summary>
        /// <returns>false indicates error</returns>
        /// 
        public bool Initialize()
        {
            this.m_AccountContainer.Clear();
            this.m_RoleList.Clear();

            this.m_RoleList = RoleAccountProvider.Instance.GetRoles();

            List<AccountEx> Accounts = RoleAccountProvider.Instance.GetAccounts();
            if (Accounts == null)
            {
                return false;
            }

            foreach (AccountEx Acc in Accounts)
            {
                AccountItem account = new AccountItem(Acc);
                account.InitRoleList(m_RoleList);
                m_AccountContainer.Add(account);
            }

            return true;
        }

        /// <summary>
        /// Get all the available Accounts
        /// </summary>
        /// <returns></returns>
        public List<AccountItem> GetAllAccounts()
        {
            return m_AccountContainer.ToList();
        }

        /// <summary>
        /// Generate Account ID
        /// </summary>
        /// <returns></returns>
        public string GenerateAccountID()
        {
            AccountItem Acc = GetAccountByID(s_AccountNum.ToString());
            while (Acc != null)
            {
                s_AccountNum++;
                Acc = GetAccountByID(s_AccountNum.ToString());
            }

            return s_AccountNum.ToString();
        }

        /// <summary>
        /// Add Account
        /// </summary>
        /// <param name="r">Account object</param>
        public bool AddAccount(AccountItem Acc)
        {
            AccountItem ExistAcc = GetAccountByName(Acc.AccountName);
            if (ExistAcc != null)
            {
                //ClientApp.Instance.Log.Info("Name of account to add exists,in CAccountManager");
                return false;
            }
            ExistAcc = GetAccountByID(Acc.AccountID);
            if (ExistAcc != null)
            {
                //ClientApp.Instance.Log.Info("ID of account to add exists,in CAccountManager");
                return false;
            }
            m_AccountContainer.Add(Acc);

            return true;
        }

        /// <summary>
        /// Get Account object by name
        /// </summary>
        /// <param name="name">Account name </param>
        /// <returns>null indicates error</returns>
        public AccountItem GetAccountByName(string name)
        {
            return m_AccountContainer.FirstOrDefault(t => t.AccountName == name);
        }

        /// <summary>
        /// Get Account object by ID
        /// </summary>
        /// <param name="name">Account ID </param>
        /// <returns>null indicates error</returns>
        public AccountItem GetAccountByID(string strID)
        {
            return m_AccountContainer.FirstOrDefault(t => t.AccountID == strID);

        }

        /// <summary>
        /// Create a default Account
        /// </summary>
        /// <returns></returns>
        public AccountItem CreateAccount()
        {
            AccountItem Acc = new AccountItem(GenerateAccountID());

            Acc.DisplayAccountName = Acc.AccountName = string.Empty;

            Acc.InitRoleList(RoleList);

            return Acc;
        }

        /// <summary>
        /// Create a copy Account
        /// </summary>
        /// <returns></returns>
        public AccountItem CreateAccount(AccountItem account)
        {
            AccountItem newAccount = new AccountItem(GenerateAccountID())
            {
                AccountName = account.AccountName,
                FirstName = account.FirstName,
                LastName = account.LastName,
                Email = account.Email,
                Description = account.Description,
                Password = account.Password,
                NewPassword = account.NewPassword,
                ConfirmPassword = account.ConfirmPassword,
                DisplayAccountName = account.AccountName,
                DisplayFirstName = account.FirstName,
                DisplayLastName = account.LastName,
                DisplayEmail = account.Email,
                DisplayDescription = account.Description
            };

            foreach (RoleStatusItem item in account.RoleColleciton)
            {
                newAccount.RoleColleciton.Add(item.Clone());
            }

            return newAccount;
        }

        /// <summary>
        /// Clone a Account
        /// </summary>
        /// <returns></returns>
        public AccountItem CloneAccount(AccountItem account)
        {
            AccountItem newAccount = new AccountItem(account.AccountID)
            {
                AccountName = account.AccountName,
                FirstName = account.FirstName,
                LastName = account.LastName,
                Email = account.Email,
                Description = account.Description,
                Password = account.Password,
                NewPassword = account.NewPassword,
                ConfirmPassword = account.ConfirmPassword,
                DisplayAccountName = account.AccountName,
                DisplayFirstName = account.FirstName,
                DisplayLastName = account.LastName,
                DisplayEmail = account.Email,
                DisplayDescription = account.Description
            };

            foreach (RoleStatusItem item in account.RoleColleciton)
            {
                newAccount.RoleColleciton.Add(item.Clone());
            }

            return newAccount;
        }


        /// <summary>
        /// Clone a Account by ID
        /// </summary>
        /// <param name="strAccountID"></param>
        /// <returns></returns>
        public AccountItem CloneAccount(string strAccountID)
        {
            AccountItem orignalAccount = GetAccountByID(strAccountID);
            if (null == orignalAccount)
                return null;

            AccountItem newAccount = new AccountItem(strAccountID)
            {
                AccountName = orignalAccount.AccountName,
                FirstName = orignalAccount.FirstName,
                LastName = orignalAccount.LastName,
                Email = orignalAccount.Email,
                Description = orignalAccount.Description,
                Password = orignalAccount.Password,
                NewPassword = orignalAccount.NewPassword,
                ConfirmPassword = orignalAccount.ConfirmPassword,
                DisplayAccountName = orignalAccount.AccountName,
                DisplayFirstName = orignalAccount.FirstName,
                DisplayLastName = orignalAccount.LastName,
                DisplayEmail = orignalAccount.Email,
                DisplayDescription = orignalAccount.Description
            };

            foreach (RoleStatusItem RoleItem in orignalAccount.RoleColleciton)
            {
                newAccount.RoleColleciton.Add(RoleItem.Clone());
            }

            return newAccount;
        }

        /// <summary>
        /// Check if account data avilable
        /// </summary>
        /// <param name="Acc"></param>
        public bool CheckAvilable(AccountItem account)
        {
            if (account == null)
                return false;
            //same name
            foreach (AccountItem Acc in m_AccountContainer)
            {
                if (account.AccountName == Acc.AccountName && account.AccountID != Acc.AccountID)
                {
                    //ClientApp.Instance.Log.Info("Check account avilable fail because the name exists,in CAccountManager");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Save Account
        /// </summary>
        /// <param name="Acc"></param>
        public bool SaveAccount(AccountItem Acc)
        {
            if (!CheckAvilable(Acc))
                return false;

            List<string> RoleList = new List<string>();
            foreach (RoleStatusItem RoleItem in Acc.RoleColleciton)
            {
                if (RoleItem.RoleStatus)
                {
                    RoleList.Add(RoleItem.RoleID);
                }
            }
            string strPassword = string.Empty;
            if (!Acc.TryUpdatePassword())
            {
                //ClientApp.Instance.Log.Info("New password not match,in CAccountManager");
                strPassword = Acc.Password;
            }
            else
            {
                strPassword = Acc.NewPassword;
            }

            AccountEx newAccount = new AccountEx(
                Acc.AccountID,
                Acc.AccountName,
                string.IsNullOrWhiteSpace(strPassword) ? string.Empty : strPassword,
                string.IsNullOrWhiteSpace(Acc.FirstName) ? string.Empty : Acc.FirstName,
                string.IsNullOrWhiteSpace(Acc.LastName) ? string.Empty : Acc.LastName,
                string.IsNullOrWhiteSpace(Acc.Email) ? string.Empty : Acc.Email,
                RoleList,
                string.IsNullOrWhiteSpace(Acc.Description) ? string.Empty : Acc.Description);

            if (RoleAccountProvider.Instance.UpdateAccount(newAccount))
            {
                Acc.UpdatePassword();
                AccountItem orignalAccount = GetAccountByID(Acc.AccountID);
                if (null == orignalAccount)
                {
                    AccountItem NewAccount = CloneAccount(Acc);
                    AddAccount(NewAccount);
                    return true;
                }

                orignalAccount.AccountName = Acc.AccountName;
                orignalAccount.FirstName = Acc.FirstName;
                orignalAccount.LastName = Acc.LastName;
                orignalAccount.Email = Acc.Email;
                orignalAccount.Description = Acc.Description;
                orignalAccount.Password = Acc.Password;
                orignalAccount.NewPassword = Acc.NewPassword;
                orignalAccount.ConfirmPassword = Acc.ConfirmPassword;

                orignalAccount.RoleColleciton.Clear();
                foreach (RoleStatusItem RoleItem in Acc.RoleColleciton)
                {
                    orignalAccount.RoleColleciton.Add(RoleItem.Clone());
                }
            }
            else
            {
                //ClientApp.Instance.Log.Info( "UpdateAccount method failed,in CAccountManager");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Delete a Account by ID
        /// </summary>
        /// <param name="strAccountName"></param>
        public bool DeleteAccount(string strAccountID)
        {
            AccountItem Acc = GetAccountByID(strAccountID);
            if (Acc != null)
            {

                if (RoleAccountProvider.Instance.DeleteAccount(strAccountID))
                {
                    m_AccountContainer.Remove(Acc);
                    return true;
                }
                else
                {
                    //ClientApp.Instance.Log.Info("DeleteAccount method failed,in CAccountManager");
                }
            }
            else
            {
                //ClientApp.Instance.Log.Info("Can not find the account to delete,in CAccountManager");
            }
            return false;
        }
        #endregion

    }
}
