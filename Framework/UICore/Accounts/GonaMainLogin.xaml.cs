using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Aitex.Core.Account;
using MECF.Framework.UI.Core.Applications;

namespace MECF.Framework.UI.Core.Accounts
{
    /// <summary>
    /// Interaction logic for MainLogin.xaml
    /// </summary>
    public partial class GonaMainLogin : Window
    {
        LoginViewModel viewModel;
 
        public GonaMainLogin()
        {
            InitializeComponent();
            viewModel = new LoginViewModel();
            DataContext = viewModel;

            Loaded += OnLoginViewLoaded;
        }

        private void OnLoginViewLoaded(object sender, RoutedEventArgs e)
        {
            FocusManager.SetFocusedElement(this, textBoxUserName);
        }

        private void OnLoginClicked(object sender, RoutedEventArgs e)
        {
        
            string userName = textBoxUserName.Text;
            IntPtr p = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(passwordBox.SecurePassword);
            string passWord = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(p);
            viewModel.UserName = userName;
            viewModel.Password = passWord;
            LoginResult loginResult = AccountClient.Instance.Service.Login(userName, passWord);

            LabelResult.Content = loginResult.Description;

            if (loginResult.ActSucc)
            {
                viewModel.SetLoginResult(loginResult);
                 this.DialogResult = true;
            }
            else
            {
                if (loginResult.Description.Contains("already login") && System.Windows.Forms.MessageBox.Show
                    (string.Format(loginResult.Description + "\r\n force logoff?"), UiApplication.Instance.Current.SystemName,
                    System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    AccountClient.Instance.Service.KickUserOut(userName, string.Format("account {0} login from other place", userName));
                }
                else
                {
                    string message = loginResult.Description;
                    MessageBox.Show(message, UiApplication.Instance.Current.SystemName, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

 
    }
}
