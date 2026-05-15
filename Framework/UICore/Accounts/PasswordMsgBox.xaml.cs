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
    public partial class PasswordMsgBox : Window
    {
        PasswordMsgBoxModel viewModel;
 
        public PasswordMsgBox()
        {
            InitializeComponent();
            viewModel = new PasswordMsgBoxModel();
            DataContext = viewModel;
        }

        private string userName = "admin";

        private void OnLoginClicked(object sender, RoutedEventArgs e)
        {
        
            IntPtr p = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(passwordBox.SecurePassword);
            string passWord = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(p);
            viewModel.UserName = userName;
            viewModel.Password = passWord;
            LoginResult loginResult = AccountClient.Instance.Service.Login(userName, passWord);

            LabelResult.Content = loginResult.Description;

            if (loginResult.ActSucc)
            {
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                this.passwordBox.Focus();
                this.LabelResult.Content = "error in your password. Please re-enter";
            }
        }

        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

 
    }
}
