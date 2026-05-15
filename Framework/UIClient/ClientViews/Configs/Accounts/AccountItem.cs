using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using MECF.Framework.Common.Account.Extends;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Configs.Accounts
{
    public class AccountItem : ValidatorBase, INotifyPropertyChanged
    {
        /// <summary>
        /// Account Constructor
        /// </summary>
        public AccountItem(string strID)
        {
            m_Account = new AccountEx(strID, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, new List<string>(), string.Empty);
        }

        /// <summary>
        /// Account Constructor
        /// </summary>
        public AccountItem(AccountEx Acc)
        {
            m_Account = Acc;
        }

        private AccountEx m_Account;
        public AccountEx Account
        {
            get { return m_Account; }
            set { m_Account = value; }
        }

        /// <summary>
        /// Account ID
        /// </summary>
        public string AccountID
        {
            get { return m_Account.UserID; }
            set { m_Account.UserID = value; }
        }
        /// <summary>
        /// Account name
        /// </summary>
        public string AccountName
        {
            get { return Account.LoginName; }
            set
            {
                Account.LoginName = value;
            }
        }

        /// <summary>
        /// Account Password
        /// </summary>
        public string Password
        {
            get { return Account.Password; }
            set
            {
                Account.Password = value;
            }
        }

        /// <summary>
        /// Account New Password
        /// </summary>
        private string m_strNewPassword;
        public string NewPassword
        {
            get { return m_strNewPassword; }
            set
            {
                if (value != m_strNewPassword)
                {
                    m_strNewPassword = value;
                }
            }
        }

        /// <summary>
        /// Account Confirm New Password
        /// </summary>
        private string m_strConfirmPassword;
        public string ConfirmPassword
        {
            get { return m_strConfirmPassword; }
            set
            {
                if (value != m_strConfirmPassword)
                {
                    m_strConfirmPassword = value;
                }
            }
        }

        /// <summary>
        /// Account first name
        /// </summary>
        public string FirstName
        {
            get { return Account.FirstName; }
            set
            {
                Account.FirstName = value;
            }
        }

        /// <summary>
        /// Account last name
        /// </summary>
        public string LastName
        {
            get { return Account.LastName; }
            set
            {
                Account.LastName = value;
            }
        }

        /// <summary>
        /// Email address
        /// </summary>
        public string Email
        {
            get { return Account.Email; }
            set
            {
                Account.Email = value;
            }
        }

        public string Description
        {
            get { return Account.Description; }
            set
            {
                Account.Description = value;
            }
        }

        private string _DisplayAccountName;

        //[Required(ErrorMessage = "AccountName Required")]
        public string DisplayAccountName
        {
            get
            {
                return _DisplayAccountName;
            }
            set
            {
                _DisplayAccountName = value;
                this.OnPropertyChanged("DisplayAccountName");
            }

        }

        //[Required(ErrorMessage = "FirstName Required")]
        private string _DisplayFirstName = string.Empty;
        public string DisplayFirstName
        {
            get { return _DisplayFirstName; }
            set
            {
                _DisplayFirstName = value;
            }
        }

        //[Required(ErrorMessage = "LastName Required")]
        private string _DisplayLastName = string.Empty;
        public string DisplayLastName
        {
            get { return _DisplayLastName; }
            set
            {
                _DisplayLastName = value;
            }
        }

        private string _DisplayEmail = string.Empty;

        //[Required(ErrorMessage = "Email Required")]
        //[RegularExpression(@"^(\w)+(\.\w+)*@(\w)+((\.\w+)+)$", ErrorMessage = "Email Format Error")]
        public string DisplayEmail
        {
            get { return _DisplayEmail; }
            set
            {
                _DisplayEmail = value;
            }
        }

        private string _DisplayDescription = string.Empty;
        public string DisplayDescription
        {
            get { return _DisplayDescription; }
            set
            {
                _DisplayDescription = value;
            }
        }

        private bool _IsSelected = false;
        public bool IsSelected
        {
            get
            {
                return _IsSelected;
            }
            set
            {
                _IsSelected = value;
                this.OnPropertyChanged("IsSelected");
            }
        }

        private bool _AccountTextSaved = true;
        public bool AccountTextSaved
        {
            get { return _AccountTextSaved; }
            set
            {
                if (_AccountTextSaved != value)
                {
                    _AccountTextSaved = value;
                    this.OnPropertyChanged("AccountTextSaved");
                }
            }
        }

        private bool _FirstNameTextSaved = true;
        public bool FirstNameTextSaved
        {
            get { return _FirstNameTextSaved; }
            set
            {
                if (_FirstNameTextSaved != value)
                {
                    _FirstNameTextSaved = value;
                    this.OnPropertyChanged("FirstNameTextSaved");
                }
            }
        }

        private bool _LastNameTextSaved = true;
        public bool LastNameTextSaved
        {
            get { return _LastNameTextSaved; }
            set
            {
                if (_LastNameTextSaved != value)
                {
                    _LastNameTextSaved = value;
                    this.OnPropertyChanged("LastNameTextSaved");
                }
            }
        }

        private bool _EmailTextSaved = true;
        public bool EmailTextSaved
        {
            get { return _EmailTextSaved; }
            set
            {
                if (_EmailTextSaved != value)
                {
                    _EmailTextSaved = value;
                    this.OnPropertyChanged("EmailTextSaved");
                }
            }
        }

        private bool _DescriptionTextSaved = true;
        public bool DescriptionTextSaved
        {
            get { return _DescriptionTextSaved; }
            set
            {
                if (_DescriptionTextSaved != value)
                {
                    _DescriptionTextSaved = value;
                    this.OnPropertyChanged("DescriptionTextSaved");
                }
            }
        }

        private bool _IsEnableChangeAccountName = true;
        public bool IsEnableChangeAccountName
        {
            get
            {
                return _IsEnableChangeAccountName;
            }
            set
            {
                if (_IsEnableChangeAccountName != value)
                {
                    _IsEnableChangeAccountName = value;
                    this.OnPropertyChanged("IsEnableChangeAccountName");
                }
            }
        }

        /// <summary>
        /// Store all roles
        /// </summary>
        private ObservableCollection<RoleStatusItem> m_RoleColleciton = new ObservableCollection<RoleStatusItem>();
        public ObservableCollection<RoleStatusItem> RoleColleciton
        {
            get { return m_RoleColleciton; }
        }

        /// <summary>
        /// Update password
        /// </summary>
        public bool UpdatePassword()
        {
            if (NewPassword == null || NewPassword == string.Empty
                    || ConfirmPassword == null || ConfirmPassword == string.Empty
                    || NewPassword != ConfirmPassword)
            {
                return false;
            }

            Password = NewPassword;
            NewPassword = null;
            ConfirmPassword = null;
            return true;
        }

        /// <summary>
        /// Try update password
        /// </summary>
        public bool TryUpdatePassword()
        {
            if (NewPassword == null || NewPassword == string.Empty
                    || ConfirmPassword == null || ConfirmPassword == string.Empty
                    || NewPassword != ConfirmPassword)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Init role list
        /// </summary>
        /// <param name="listRole"></param>
        public void InitRoleList(List<Role> listRole)
        {
            m_RoleColleciton.Clear();
            foreach (Role role in listRole)
            {
                RoleStatusItem RoleStatus = new RoleStatusItem()
                {
                    RoleID = role.RoleID,
                    RoleName = role.RoleName,
                    RoleStatus = this.Account.RoleIDs.Contains(role.RoleID) ? true : false
                };
                m_RoleColleciton.Add(RoleStatus);
            }
        }

        public bool IsAccountChanged()
        {
            if (this.DisplayAccountName != Account.LoginName)
                return true;

            if (this.DisplayFirstName != Account.FirstName)
                return true;

            if (this.DisplayLastName != Account.LastName)
                return true;

            if (this.DisplayEmail != Account.Email)
                return true;

            if (this.DisplayDescription != Account.Description)
                return true;

            foreach (RoleStatusItem item in m_RoleColleciton)
            {
                if (item.DisplayRoleStatus != item.RoleStatus)
                    return true;
            }
          
            return false;
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
