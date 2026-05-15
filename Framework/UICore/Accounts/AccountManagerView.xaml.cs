using System.Windows;
using System.Windows.Controls;
using Aitex.Core.Account;

namespace MECF.Framework.UI.Core.Accounts
{
    /// <summary>
    /// Interaction logic for AccountManagement.xaml
    /// </summary>
    public partial class AccountManagerView : UserControl
    {
 
        public AccountManagerView()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(AccountManagement_Loaded);
            dataGrid1.CanUserAddRows = false;
        }

        bool _hideDisabledAccounts = false;

        /// <summary>
        /// When loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AccountManagement_Loaded(object sender, RoutedEventArgs e)
        {
            _hideDisabledAccounts = false;
            UpdateAccountList();
            this.IsEnabled = true;
            this.Name = "account";
            switch (this.GetPermission())
            {
                case ViewPermission.FullyControl:
                    btnCreateAccount.Visibility = System.Windows.Visibility.Visible;
                    btnDeleteAccount.Visibility = System.Windows.Visibility.Visible;
                    btnUserProperty.Visibility = System.Windows.Visibility.Visible;
                    btnUserPwd.Visibility = System.Windows.Visibility.Visible;
                    //btnShowOnline.Visibility = System.Windows.Visibility.Visible;  
                    btnRoleEdit.Visibility = System.Windows.Visibility.Visible;
                    //btnPermission.Visibility = System.Windows.Visibility.Visible;
                    break;
                default:
                    btnCreateAccount.Visibility = System.Windows.Visibility.Hidden;
                    btnDeleteAccount.Visibility = System.Windows.Visibility.Hidden;
                    btnUserProperty.Visibility = System.Windows.Visibility.Hidden;
                    btnUserPwd.Visibility = System.Windows.Visibility.Hidden;
                    //btnShowOnline.Visibility = System.Windows.Visibility.Hidden;   
                    btnRoleEdit.Visibility = System.Windows.Visibility.Hidden;
                    //btnPermission.Visibility = System.Windows.Visibility.Hidden;
                    break;
            }
        }

        /// <summary>
        /// Update account list
        /// </summary>
        private void UpdateAccountList()
        {      
            DataContext = new AccountViewModel(_hideDisabledAccounts);
        }

        /// <summary>
        /// Toggle user list (enable only/show all)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxToggleAvailable_Click(object sender, RoutedEventArgs e)
        {
            _hideDisabledAccounts = !_hideDisabledAccounts;
            UpdateAccountList();
        }

        /// <summary>
        /// delete selected account
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            var item = dataGrid1.SelectedItem as  AccountViewModel.AccountInfo;
            if (item == null) return;
            if (MessageBox.Show(string.Format(Application.Current.Resources["GlobalLableAccountViewDeleteInfo"].ToString(), item.AccountId), Application.Current.Resources["GlobalLableAccountViewMsgTitle"].ToString(), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var ret = AccountClient.Instance.Service.DeleteAccount(item.AccountId);
                if (ret.ActSucc)
                {
                    MessageBox.Show(string.Format(Application.Current.Resources["GlobalLableAccountViewDeleteOk"].ToString(), item.AccountId), Application.Current.Resources["GlobalLableAccountViewMsgTitle"].ToString(), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(string.Format(Application.Current.Resources["GlobalLableAccountViewDeleteFailed"].ToString(), item.AccountId, ret.Description), Application.Current.Resources["GlobalLableAccountViewMsgTitle"].ToString(), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                UpdateAccountList();
            }
        }

        /// <summary>
        /// Change user's password
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUserPwd_Click(object sender, RoutedEventArgs e)
        {
            var item = dataGrid1.SelectedItem as  AccountViewModel.AccountInfo;
            if (item == null) return;
            UserPwdChangeView view = new UserPwdChangeView(item.AccountId) { Owner = Application.Current.MainWindow };
            view.ShowDialog();            
        }

        /// <summary>
        /// Edit current selected users
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRoleEdit_Click(object sender, RoutedEventArgs e)
        {
            RoleEditView view = new RoleEditView() { Owner = Application.Current.MainWindow }; 
            view.ShowDialog();
        }

        /// <summary>
        /// Show current online users
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnShowOnline_Click(object sender, RoutedEventArgs e)
        {
            CurrentLogInUsers view = new CurrentLogInUsers() { Owner = Application.Current.MainWindow };
            view.ShowDialog();
        }

        /// <summary>
        /// Edit user's profile
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUserProperty_Click(object sender, RoutedEventArgs e)
        {
            var item = dataGrid1.SelectedItem as  AccountViewModel.AccountInfo;
            if (item == null) return;
            var editor = new UserAccountEdit(item.Account) { Owner = Application.Current.MainWindow };
            var ret = editor.ShowDialog();
            if (ret.HasValue && ret.Value)
            {
                UpdateAccountList();
            }            
        }


        /// <summary>
        /// Create account
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            AccountCreation view = new AccountCreation() { Owner = Application.Current.MainWindow };
            var ret = view.ShowDialog();
            if (ret.HasValue && ret.Value)
                UpdateAccountList();
        }

        /// <summary>
        /// 显示个人账号信息页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_MyAccount_Click(object sender, RoutedEventArgs e)
        {
            MyAccount view = new MyAccount() { Owner = Application.Current.MainWindow };
            view.ShowDialog();
        }

        private void btnPermission_Click(object sender, RoutedEventArgs e)
        {
            RolePermissionEdit view = new RolePermissionEdit() { Owner = Application.Current.MainWindow };
            view.ShowDialog();
        }
    }
}
