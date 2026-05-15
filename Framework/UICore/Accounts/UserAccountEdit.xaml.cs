using System.Windows;
using Aitex.Core.Account;

namespace MECF.Framework.UI.Core.Accounts
{
    /// <summary>
    /// Interaction logic for UserAccountEdit.xaml
    /// </summary>
    public partial class UserAccountEdit : Window
    {

        public UserAccountEdit(Account account)
        {
            InitializeComponent();

            _account = account;

            DataContext = account;

            var roles = AccountClient.Instance.Service.GetAllRoles();
            foreach (var item in roles)
            {
                comboBoxGroup.Items.Add(item);
            }
            comboBoxGroup.SelectedItem = account.Role;
        }

        Account _account;

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
                MessageBox.Show(Application.Current.Resources["GlobalLableAccountViewRoleBeSelected"].ToString(), Application.Current.Resources["GlobalLableAccountViewMsgTitle"].ToString(), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            account.Role = (string)comboBoxGroup.SelectedValue;

            var ret = AccountClient.Instance.Service.UpdateAccount(account);
            if (ret.ActSucc)
            {
                DialogResult = true;
                Close();
                MessageBox.Show(string.Format(Application.Current.Resources["GlobalLableAccountViewMsgUpdateOk"].ToString(), _account.AccountId), Application.Current.Resources["GlobalLableAccountViewMsgTitle"].ToString(), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(string.Format(Application.Current.Resources["GlobalLableAccountViewMsgUpdateFailed"].ToString(), _account.AccountId, ret.Description), Application.Current.Resources["GlobalLableAccountViewMsgTitle"].ToString(), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
