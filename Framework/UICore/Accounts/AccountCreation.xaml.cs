using System.Windows;
using Aitex.Core.Account;

namespace MECF.Framework.UI.Core.Accounts
{
    /// <summary>
    /// Interaction logic for AccountCreation.xaml
    /// </summary>
    public partial class AccountCreation : Window
    {
        Account _account = new Account();

        public AccountCreation()
        {
            InitializeComponent();

            DataContext = _account;

            var roles = AccountClient.Instance.Service.GetAllRoles();
            foreach (var item in roles)
            {
                comboBoxGroup.Items.Add(item);
            }
            comboBoxGroup.SelectedItem = _account.Role;
        }



        /// <summary>
        /// Save current configuration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_Save_Click(object sender, RoutedEventArgs e)
        {
            Account account = DataContext as Account;
            if (string.IsNullOrEmpty((string)comboBoxGroup.SelectedValue))
            {
                MessageBox.Show("Please select role", "Account", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            account.Role = (string)comboBoxGroup.SelectedValue;
            var ret = AccountClient.Instance.Service.CreateAccount(account);
            if (ret.ActSucc)
            {
                DialogResult = true;
                Close();
                MessageBox.Show(string.Format("\" {0} \" created！", _account.AccountId), "Account", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(string.Format("\" {0} \" create failed，\r\n{1}", _account.AccountId, ret.Description), "Account", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
