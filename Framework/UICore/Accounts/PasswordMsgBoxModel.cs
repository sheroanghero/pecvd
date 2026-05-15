using Aitex.Core.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.UI.Core.Accounts
{
    public class PasswordMsgBoxModel
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

        public PasswordMsgBoxModel()
        {



            UserName = string.Empty;
            Password = string.Empty;

            AccountInfo = new Account();

        }
    }
}
