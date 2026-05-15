using System.Windows;

namespace MECF.Framework.UI.Core.Accounts
{
    /// <summary>
    /// Interaction logic for UserPwdChangeView.xaml
    /// </summary>
    public partial class UserPwdChangeView : Window
    {
        /// <summary>
        /// account Id
        /// </summary>
        /// <param name="accountId"></param>
        public UserPwdChangeView(string accountId)
        {
            InitializeComponent();
            _accountId = accountId;
            groupBox1.Header = string.Format(Application.Current.Resources["GlobalLableAccountViewResetPasswordInfo"].ToString(), accountId);
            btnOK.IsEnabled = false;
        }

        private string _accountId;

        private void passwordBox1_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidatePwd();
        }

        private void passwordBox2_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidatePwd();
        }

        private void ValidatePwd()
        {
            btnOK.IsEnabled = System.Text.RegularExpressions.Regex.Match(passwordBox1.Password, "^(?=.*\\d)(?=.*[a-zA-Z]).{4,12}$").Success &&
                System.Text.RegularExpressions.Regex.Match(passwordBox2.Password, "^(?=.*\\d)(?=.*[a-zA-Z]).{4,12}$").Success &&
                passwordBox1.Password == passwordBox2.Password;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {

            var ret = AccountClient.Instance.Service.ChangePassword(_accountId, passwordBox1.Password);
            if (ret.ActSucc)
            {
                MessageBox.Show(Application.Current.Resources["GlobalLableAccountViewResetPasswordOk"].ToString(), Application.Current.Resources["GlobalLableAccountViewMsgTitle"].ToString(), MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            else
            {
                MessageBox.Show(Application.Current.Resources["GlobalLableAccountViewResetPasswordFailed"].ToString() + ret.Description, Application.Current.Resources["GlobalLableAccountViewMsgTitle"].ToString(), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
