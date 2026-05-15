using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Aitex.Core.Account;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace MECF.Framework.UI.Core.Accounts
{
    /// <summary>
    /// Interaction logic for CurrentLogInUsers.xaml
    /// </summary>
    public partial class CurrentLogInUsers : Window, INotifyPropertyChanged
    {

        public CurrentLogInUsers()
        {
            InitializeComponent();

            this.DataContext = this;

            Loaded += new RoutedEventHandler(CurrentLogInUsers_Loaded);
        }

        void CurrentLogInUsers_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshLoginUser();
            //throw new NotImplementedException();
        }

        

        List<Account> _loginUser;
        public List<Account> LoginUser 
        {
            get { return _loginUser; }
            set { _loginUser = value; }
        }

        public void RefreshLoginUser()
        {
            LoginUser = AccountClient.Instance.Service.GetLoginUsers();
            //labelAuthorizedUser.Content = string.Format("授权用户：{0}", MainWindow.AuthorizedUser);
            Notify("LoginUser");
        }

        private void Notify(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                Account kickUser = ((sender as Button).DataContext as Account);
                if (MessageBox.Show(string.Format("确认踢出用户:{0}?", kickUser.AccountId), "警告", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    AccountClient.Instance.Service.KickUserOut(kickUser.AccountId, string.Format("Admin将账号{0}强制注销", kickUser.AccountId));
                    RefreshLoginUser();
 
                    if (AccountClient.Instance.CurrentUser.AccountId.Equals(kickUser.AccountId))
                        this.Close();
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                throw ex;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
