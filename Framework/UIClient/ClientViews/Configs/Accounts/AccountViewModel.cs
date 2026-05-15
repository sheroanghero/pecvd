using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using Aitex.Core.RT.Log;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.Command;

namespace MECF.Framework.UI.Client.CenterViews.Configs.Accounts
{
    public class AccountViewModel : BaseModel
    {
        #region Property
        public bool IsPermission { get => this.Permission == 3; }

        private AccountItem _treeSelectedAccount = null;
        public AccountItem TreeSelectedAccount
        {
            get { return _treeSelectedAccount; }
            set { _treeSelectedAccount = value; this.NotifyOfPropertyChange("TreeSelectedAccount"); }
        }

        private CtrlMode _ControlMode = CtrlMode.VIEW;
        public CtrlMode ControlMode
        {
            get { return _ControlMode; }
            set { _ControlMode = value; NotifyOfPropertyChange("ControlMode"); }
        }

        private ObservableCollection<AccountItem> _AccountsList = new ObservableCollection<AccountItem>();
        public ObservableCollection<AccountItem> AccountList
        {
            get { return _AccountsList; }
        }

        public AccountManager AccountManager
        {
            get { return AccountManager.Instance; }
        }

        private PasswordBox NewPasswordBox;
        private PasswordBox ConfirmPasswordBox;

        #region command define
        private ICommand _BtnSaveAccountCommand;
        public ICommand BtnSaveAccountCommand
        {
            get
            {
                if (this._BtnSaveAccountCommand == null)
                    this._BtnSaveAccountCommand = new BaseCommand<Object>((Object arg) => this.OnBtnSaveAccountCommand(arg));
                return this._BtnSaveAccountCommand;
            }
        }

        private ICommand _BtnAddAccountCommand;
        public ICommand BtnAddAccountCommand
        {
            get
            {
                if (this._BtnAddAccountCommand == null)
                    this._BtnAddAccountCommand = new BaseCommand<Object>((Object arg) => this.OnBtnAddAccountCommand(arg));
                return this._BtnAddAccountCommand;
            }
        }

        private ICommand _BtnCloneAccountCommand;
        public ICommand BtnCloneAccountCommand
        {
            get
            {
                if (this._BtnCloneAccountCommand == null)
                    this._BtnCloneAccountCommand = new BaseCommand<Object>((Object arg) => this.OnBtnCloneAccountCommand(arg));
                return this._BtnCloneAccountCommand;
            }
        }

        private ICommand _BtnDeleteAccountCommand;
        public ICommand BtnDeleteAccountCommand
        {
            get
            {
                if (this._BtnDeleteAccountCommand == null)
                    this._BtnDeleteAccountCommand = new BaseCommand<Object>((Object arg) => this.OnBtnDeleteAccountCommand(arg));
                return this._BtnDeleteAccountCommand;
            }
        }

        private ICommand _BtnCancelAccountCommand;
        public ICommand BtnCancelAccountCommand
        {
            get
            {
                if (this._BtnCancelAccountCommand == null)
                    this._BtnCancelAccountCommand = new BaseCommand<Object>((Object arg) => this.OnBtnCancelAccountCommand(arg));
                return this._BtnCancelAccountCommand;
            }
        }
        #endregion
        #endregion

        public AccountViewModel()
        {
            this.DisplayName = "Account";
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            AccountView av = view as AccountView;
            NewPasswordBox = av.pwNewPassword;
            ConfirmPasswordBox = av.pwConfirmPassword;
        }

        protected override void OnActivate()
        {
            AccountManager.Initialize();
            RefreshAccountList();
        }

        protected override void OnDeactivate(bool close)
        {
            if (ControlMode == CtrlMode.EDIT && IsPermission)
            {
                if (DialogBox.Confirm("The data has been modified. Do you want to save the change(s)?"))
                {
                    if (SaveChanged())
                    {
                        ControlMode = CtrlMode.VIEW;
                        DialogBox.ShowInfo("Operated successfully.");
                    }
                }
            }
            base.OnDeactivate(close);
        }

        #region Function
        private void RefreshAccountList()
        {
            _AccountsList.Clear();
            _treeSelectedAccount = null;

            List<AccountItem> Accounts = AccountManager.GetAllAccounts();
            if (Accounts == null || Accounts.Count == 0) return;

            foreach (AccountItem Acc in Accounts)
            {
                AccountItem treeAccount = AccountManager.CloneAccount(Acc);
                if (treeAccount != null)
                {
                    if (treeAccount.AccountName == BaseApp.Instance.UserContext.LoginName)
                    {
                        treeAccount.IsEnableChangeAccountName = false;
                    }
                    _AccountsList.Add(treeAccount);
                }
            }

            TreeSelectedAccount = _AccountsList.FirstOrDefault();
            TreeSelectedAccount.IsSelected = true;
            ControlMode = CtrlMode.VIEW;
        }

        public void OnAccountChanged()
        {
            if (ControlMode == CtrlMode.EDIT)
                return;

            //check account to set the mode from view to edit
            if (_treeSelectedAccount != null && _treeSelectedAccount.IsAccountChanged())
                ControlMode = CtrlMode.EDIT;
        }

        private bool SaveChanged()
        {
            if (string.IsNullOrWhiteSpace(TreeSelectedAccount.DisplayAccountName))
            {
                DialogBox.ShowWarning("{0} cannot be empty.", "Account name");
                //TreeSelectedAccount.DisplayAccountName = "NewUser";
                return false;
            }

            if (IsAccountExists(TreeSelectedAccount))
            {
                DialogBox.ShowWarning("{0} already exists.", "Account");
                return false;
            }

            TreeSelectedAccount.NewPassword = NewPasswordBox.Password;
            TreeSelectedAccount.ConfirmPassword = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(TreeSelectedAccount.NewPassword) ||
                string.IsNullOrWhiteSpace(TreeSelectedAccount.ConfirmPassword))
            {
                DialogBox.ShowWarning("{0} cannot be empty.", "The password");
                return false;
            }

            if (TreeSelectedAccount.NewPassword != TreeSelectedAccount.ConfirmPassword)
            {
                DialogBox.ShowWarning("The password does not match.");
                return false;
            }

            if (!string.IsNullOrEmpty(TreeSelectedAccount.DisplayEmail))
            {
                string reg = @"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*";
                Regex r = new Regex(reg);
                if (!r.IsMatch(TreeSelectedAccount.DisplayEmail))
                {
                    DialogBox.ShowWarning("The email is invalid.");
                    return false;
                }
            }

            TreeSelectedAccount.AccountName = TreeSelectedAccount.DisplayAccountName;
            TreeSelectedAccount.FirstName = TreeSelectedAccount.DisplayFirstName;
            TreeSelectedAccount.LastName = TreeSelectedAccount.DisplayLastName;
            TreeSelectedAccount.Email = TreeSelectedAccount.DisplayEmail;
            TreeSelectedAccount.Description = TreeSelectedAccount.DisplayDescription;
            TreeSelectedAccount.AccountTextSaved = TreeSelectedAccount.FirstNameTextSaved =
                TreeSelectedAccount.LastNameTextSaved = TreeSelectedAccount.EmailTextSaved = true;

            bool isRoleSelected = false;
            foreach (RoleStatusItem entity in TreeSelectedAccount.RoleColleciton)
            {
                if (entity.DisplayRoleStatus)
                {
                    isRoleSelected = true;
                }
                entity.RoleStatus = entity.DisplayRoleStatus;
            }

            if (!isRoleSelected)
            {
                DialogBox.ShowWarning("Please set role information for this account.");
                return false;
            }

            try
            {
                AccountManager.SaveAccount(TreeSelectedAccount);
            }
            catch (System.Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
            return true;
        }

        private bool IsAccountExists(AccountItem account)
        {
            if (AccountList == null || AccountList.Count == 0)
                return false;

            var sameNameList = AccountList.Where(t => t.DisplayAccountName == account.DisplayAccountName);
            if (sameNameList == null || sameNameList.Count() <= 1)
                return false;

            return true;
        }

        private void OnBtnAddAccountCommand(Object arg)
        {
            AccountItem newAccount = AccountManager.CreateAccount();
            if (newAccount != null)
            {
                _AccountsList.Add(newAccount);
                TreeSelectedAccount = newAccount;
                TreeSelectedAccount.IsSelected = true;
            }
            ControlMode = CtrlMode.EDIT;
        }

        private void OnBtnDeleteAccountCommand(Object arg)
        {
            if (_treeSelectedAccount == null) return;

            if (!DialogBox.Confirm("Are you sure that you want to delete this account?"))
            {
                return;
            }

            if (BaseApp.Instance.UserContext.LoginName == _treeSelectedAccount.AccountName)
            {
                DialogBox.ShowWarning("The action cannot be completed because {0} is currently in use.", "the account");
                return;
            }

            AccountManager.DeleteAccount(TreeSelectedAccount.AccountID);
            RefreshAccountList();
        }

        private void OnBtnCloneAccountCommand(Object arg)
        {
            if (_treeSelectedAccount != null)
            {
                AccountItem newAccount = AccountManager.CreateAccount(_treeSelectedAccount);
                if (newAccount != null)
                {
                    newAccount.DisplayAccountName = newAccount.AccountName = "Copy of " + newAccount.DisplayAccountName;
                    _AccountsList.Add(newAccount);
                    TreeSelectedAccount = newAccount;
                    TreeSelectedAccount.IsSelected = true;
                    ControlMode = CtrlMode.EDIT;
                }
            }
        }

        private void OnBtnSaveAccountCommand(Object arg)
        {
            if (!TreeSelectedAccount.IsValid)
            {
                DialogBox.ShowWarning("Input error.");
                return;
            }

            if (SaveChanged())
            {
                ControlMode = CtrlMode.VIEW;
                DialogBox.ShowInfo("Operated successfully.");
            }
            else
                DialogBox.ShowInfo("Operation failed.");
        }

        private void OnBtnCancelAccountCommand(Object arg)
        {
            RefreshAccountList();
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
            ControlMode = CtrlMode.VIEW;
        }

        #endregion        
    }
}
