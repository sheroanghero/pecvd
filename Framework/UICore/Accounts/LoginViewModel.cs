using System;
using Aitex.Core.Account;
using Aitex.Core.Util;

namespace MECF.Framework.UI.Core.Accounts
{
    public class LoginViewModel  
    {
        private string _userName = "";
        private string _password = "";

        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                _userName = value;
                //ClearError();
                //InvokePropertyChanged("UserName");
            }
        }
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
                //ClearError();
                //InvokePropertyChanged("Password");
            }
        }

        public Account AccountInfo
        {
            get;
            set;
        }

        public String SessionId
        {
            get;
            set;
        }

        public string Role
        {
            get;
            set;
        }

        public void SetLoginResult(LoginResult result)
        {
            AccountClient.Instance.CurrentUser = result.AccountInfo;
            AccountInfo = result.AccountInfo;
            SessionId = result.SessionId;
            Role = AccountInfo.Role;
        }

        public LoginViewModel()
        {



            UserName = string.Empty;
            Password = string.Empty;

            AccountInfo = new Account();

            //m_password1Property = new PassableProperty<string>(
            //       () => Password,
            //       (val) => { Password = val; }
            //   );
        }

        //private string m_error = "";
        //public string Error
        //{
        //    get
        //    {
        //        return m_error;
        //    }
        //    set
        //    {
        //        m_error = value;
        //        InvokePropertyChanged("Error");
        //    }
        //}

        //private readonly PassableProperty<string> m_password1Property;
        //public PassableProperty<string> Password1Property
        //{
        //    get { return m_password1Property; }
        //}

        //private void ClearError()
        //{
        //    Error = string.Empty;
        //}

    }
}
