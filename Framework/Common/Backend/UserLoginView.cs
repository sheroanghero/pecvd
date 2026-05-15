using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using Aitex.Core.Utilities;
using Aitex.Core.RT.Log;

namespace Aitex.Core.Backend
{
    public partial class UserLoginView : Form
    {
        private UserLoginView()
        {
            InitializeComponent();
            AcceptButton = this.buttonLogin;
            CancelButton = this.buttonCancel;

            Load += new EventHandler(UserLoginView_Load);
        }

        void UserLoginView_Load(object sender, EventArgs e)
        {
            this.Text = "Login";
        }

		MainView _mainView;

        /// <summary>
        /// create only one instance
        /// </summary>
        static UserLoginView _instance;

        /// <summary>
        /// clear passowrd input
        /// </summary>
        private void ResetInput()
        {
            this.textBoxPassword.Clear();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            buttonCancel_Click(null, null);
            e.Cancel = true;
            base.OnClosing(e);
        }

        /// <summary>
        /// display this dialog
        /// </summary>
		public static void Display(bool ignorePassword)
		{
			if (_instance == null)
				_instance = new UserLoginView();

			_instance.ResetInput();

		    if (ignorePassword)
		    {
		        if (_instance._mainView == null)
		        {
		            _instance._mainView = new MainView();
                }
		        _instance._mainView.Show();
                return;
		    }

			if (_instance._mainView != null && _instance._mainView.Visible)
			{
				_instance._mainView.Show();
			}
			else
			{
				_instance.Show();
			}
		}
 

        public static void AddCustomView(string name, UserControl uc)
        {
            if (_instance == null)
                _instance = new UserLoginView();

            if (_instance._mainView == null)
                _instance._mainView = new MainView();

                _instance._mainView.AddCustomView(name, uc);

        }

        /// <summary>
        /// when user click to login
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void buttonLogin_Click(object sender, EventArgs e)
		{
            var superPwd = System.Configuration.ConfigurationManager.AppSettings["Su"];
            var user = textBoxAccountId.Text;
            var pwd = textBoxPassword.Text;
            if (String.Compare(user, "admin", true) == 0 && Md5Helper.VerifyMd5Hash(pwd, superPwd))
            {
                LOG.Write("用户登入后台界面");

                if (_mainView == null) _mainView = new MainView();
                _mainView.Show();
                Hide();
            }
            else
            {
                LOG.Write("用户密码错误，登入后台界面失败");

                MessageBox.Show("Account name or password is error, login failed.", "Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
		}


        /// <summary>
        /// when user click to cancel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Hide();
            DialogResult = DialogResult.Cancel;
        }
    }
}
