using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aitex.Common.Util;
using Aitex.Core.Account;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Core.WCF;
using MECF.Framework.Common.Account.Extends;
using MECF.Framework.UI.Core.Accounts;

namespace MECF.Framework.Common.Account
{
    public enum AuthorizeResult
    {
        None = -1,
        WrongPwd = 1,
        HasLogin = 2,
        NoMatchRole = 3,
        NoSession = 4,
        NoMatchUser = 5
    }

    public class AccountExManager : Singleton<AccountExManager>
    {
        public RoleLoader RoleLoader { get; private set; }

        Dictionary<string /*account id , login name*/, AccountEx> _loginUserList = new Dictionary<string, AccountEx>();
 
        private string _scAccountFile = PathManager.GetCfgDir() + "Account//Account.xml";
        private string _scAccountLocalFile = PathManager.GetCfgDir() + "Account//_Account.xml";
 
        public AccountExManager()
        {

        }

        public void Initialize(bool enableService)
        {
            if (!File.Exists(_scAccountLocalFile))
            {
                if (!File.Exists(_scAccountFile))
                {
                    throw  new ApplicationException($"Can not initialize account configuration file, {_scAccountFile}");
                }

                File.Copy(_scAccountFile, _scAccountLocalFile);
                Thread.Sleep(10);
            }

            this.RoleLoader = new RoleLoader(_scAccountLocalFile);

            this.RoleLoader.Load();

            if (enableService)
            {
                Singleton<WcfServiceManager>.Instance.Initialize(new Type[]
                {
                    typeof(AccountService)
                });
            }
        }

        public LoginResult AuthLogin(string userid, string password, string role)
        {
            LoginResult result = new LoginResult() {ActSucc = true, SessionId = Guid.NewGuid().ToString() };
 
            try
            {
                var tempAccoutList = RoleLoader.AccountList;
                var user = tempAccoutList.FirstOrDefault(f => f.LoginName == userid);
                if (user == null)
                {
                    result.ActSucc = false;
                    result.Description = AuthorizeResult.NoMatchUser.ToString();
                }
                else
                {
                    foreach (AccountEx ac in tempAccoutList)
                    {
                        if (ac.LoginName == userid && ac.Password == password)
                        {
                            foreach (string r in ac.RoleIDs)
                            {
                                if (r == role || userid == "AMTEadmin")
                                {
                                    result.ActSucc = true;
                                    result.Description = AuthorizeResult.None.ToString();

                                    foreach (var accountEx in _loginUserList)
                                    {
                                        if (SC.ContainsItem("System.AllowMultiClientLogin") 
                                            && SC.GetValue<bool>("System.AllowMultiClientLogin")
                                            && (accountEx.Value.LoginName!= userid))
                                        {
                                            continue;
                                        }

                                        EV.PostKickoutMessage(accountEx.Value.LoginId);
                                    }

                                    _loginUserList[userid] = ac;
                                    _loginUserList[userid].LoginId = result.SessionId;

                                    return result;
                                }
                                else
                                {
                                    result.ActSucc = false;
                                    result.Description = AuthorizeResult.NoMatchRole.ToString();
                                }
                            }
                            return result;
                        }
                        else
                        {
                            result.ActSucc = false;
                            result.Description = AuthorizeResult.WrongPwd.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message, ex);
            }
            return result;
        }

        internal void Logout(string accountId, string loginId)
        {
            foreach (var key in _loginUserList.Keys)
            {
                if (accountId == key)
                {
                    if (_loginUserList[key].LoginId == loginId)
                    {
                        _loginUserList.Remove(key);
                        break;
                    }
                }
            }
             
        }
    }
}
