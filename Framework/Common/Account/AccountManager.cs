using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Aitex.Common.Util;
using Aitex.Core.Util;
using Aitex.Core.RT.Log;
using Aitex.Core.Utilities;
using Aitex.Core.RT.Event;

namespace Aitex.Core.Account
{
    public sealed class AccountManager
    {
        static Dictionary<string, Tuple<Guid, DateTime, string>> _userList;  //已登录用户和客户端Guid之间的映射表
        private static string _accountPath;
        private static string _rolePath;                              //账号信息所对应的XML文件路径
        private static string _viewPath;
        private static XmlDocument _accountXml;  //"Account.xml"
        private static XmlDocument _roleXml;     //"Roles.xml"
        private static XmlDocument _viewsXml;
        const int MAX_LOGIN_USER_NUM = 16;

        public static string SerialNumber { get; private set; }
        public static string Module { get; private set; }


        /// <summary>
        /// 静态构造函数
        /// </summary>
        static AccountManager()
        {
            SerialNumber = "001";
            Module = "System";

            try
            {
                _userList = new Dictionary<string, Tuple<Guid, DateTime, string>>();
                _accountPath = Path.Combine(PathManager.GetAccountFilePath(), "Account.xml");
                _rolePath = Path.Combine(PathManager.GetAccountFilePath(), "Roles.xml");
                _viewPath = Path.Combine(PathManager.GetAccountFilePath(), "Views.xml");
                _accountXml = new XmlDocument();
                _roleXml = new XmlDocument();

                //检查Roles.xml是否存在，如果不存在则自动创建
                FileInfo roleFileInfo = new System.IO.FileInfo(_rolePath);
                if (!roleFileInfo.Directory.Exists)
                    roleFileInfo.Directory.Create();
                if (!roleFileInfo.Exists)
                {
                    _roleXml.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><Aitex><Roles></Roles></Aitex>");
                    Save(_roleXml, _rolePath);
                }
                else
                {
                    _roleXml.Load(_rolePath);
                }
                //检查Account.xml文件是否存在，如果不存在则自动创建
                FileInfo fileInfo = new System.IO.FileInfo(_accountPath);
                if (!fileInfo.Directory.Exists)
                    fileInfo.Directory.Create();
                if (!fileInfo.Exists)
                {
                    _accountXml.LoadXml("<?xml version='1.0' encoding='utf-8' ?><AccountManagement></AccountManagement>");
                    Save(_accountXml, _accountPath);
                }
                else
                {
                    _accountXml.Load(_accountPath);
                }

                //检查views.xml文件是否存在，如果不存在则自动创建
                _viewsXml = new XmlDocument();
                fileInfo = new System.IO.FileInfo(_viewPath);
                if (!fileInfo.Directory.Exists)
                    fileInfo.Directory.Create();
                if (!fileInfo.Exists)
                {
                    _viewsXml.LoadXml("<?xml version='1.0' encoding='utf-8' ?><root><Views></Views></root>");
                    Save(_viewsXml, _viewPath);
                }
                else
                {
                    _viewsXml.Load(_viewPath);
                }

                string recipePermissionFile = System.IO.Path.Combine(PathManager.GetCfgDir(), "RolePermission.xml");

                if (!File.Exists(recipePermissionFile))
                {
                    XmlDocument xmlRecipeFormat = new XmlDocument();
                    xmlRecipeFormat.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\" ?><Aitex></Aitex>");
                    xmlRecipeFormat.Save(recipePermissionFile);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

            }


        }

        /// <summary>
        /// 获取当前登录的用户列表
        /// </summary>
        /// <returns></returns>
        public static List<Account> GetLoginUserList()
        {
            List<Account> userList = new List<Account>();
            foreach (var accountId in _userList.Keys)
            {
                Account temp = GetAccountInfo(accountId).AccountInfo;
                temp.LoginIP = _userList[accountId].Item3;
                userList.Add(temp);
            }
            return userList;
        }

        public static void RegisterViews(List<string> views)
        {
            try
            {
                var nodes = _viewsXml.SelectSingleNode("/root/Views");

                foreach (var view in views)
                {
                    if (nodes.SelectSingleNode(string.Format("View[@Name='{0}']", view)) != null)
                        continue;

                    XmlElement node = _viewsXml.CreateElement("View");
                    node.SetAttribute("Name", view);
                    node.SetAttribute("Description", view);
                    nodes.AppendChild(node);
                }
                Save(_viewsXml, _viewPath);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        /// <summary>
        /// add xml signature here, during xml save
        /// </summary>
        private static void Save(XmlDocument doc, string path)
        {
            doc.Save(path);                 //write to xml file
            FileSigner.Sign(path);          //write xml signature

            GetAccountList();
        }

        /// <summary>
        /// user login verify
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static LoginResult Login(string accountId, string accountPwd)
        {
            try
            {
                LOG.Write(string.Format("Account {0} try to login system", accountId));
                accountId = accountId.ToLower();                                            //账号大小写不敏感，先行转换为小写
                var ret = new LoginResult();
                if (accountId == "su" && accountPwd == "su")                           //判断是否为固定的秘密账号
                {
                    ret.ActSucc = true;
                    ret.AccountInfo = GetAccountInfo("admin").AccountInfo;
                    ret.SessionId = Guid.NewGuid().ToString();
                }
                else if (!FileSigner.IsValid(_accountPath))
                {
                    ret.Description = "File signer corrupt";
                    ret.ActSucc = false;
                }
                else if (_userList.ContainsKey(accountId))
                {
                    var accountA = GetAccountInfo(accountId).AccountInfo;
                    if (accountA.Md5Pwd == Md5Helper.GetMd5Hash(accountPwd))
                    {
                        ret.ActSucc = true;
                        ret.Description = string.Format("{0} login succeed", accountId);
                        ret.AccountInfo = accountA;
                        ret.SessionId = Guid.NewGuid().ToString();
                    }
                    else
                    {
                        ret.ActSucc = false;
                        ret.Description = string.Format("account {0} already login", accountId);
                    }
                }
                else if (_userList.Count >= MAX_LOGIN_USER_NUM && accountId != "admin")
                {
                    ret.ActSucc = false;
                    ret.Description = string.Format("more than {0} users login", MAX_LOGIN_USER_NUM);
                }
                else
                {
                    var account = GetAccountInfo(accountId).AccountInfo;
                    if (account == null)
                    {
                        ret.ActSucc = false;
                        ret.Description = string.Format("{0} not exist", accountId);
                    }
                    else if (account.Md5Pwd != Md5Helper.GetMd5Hash(accountPwd)
                        && (account.Role != "Admin" || accountPwd != Md5Helper.GenerateDynamicPassword(SerialNumber)))                      //检查账号密码是否正确
                    {
                        ret.ActSucc = false;
                        ret.Description = string.Format("password error");
                    }
                    else if (!account.AccountStatus)
                    {
                        ret.ActSucc = false;
                        ret.Description = string.Format("account {0} is disabled", accountId);
                    }
                    else
                    {
                        //if(accountId != "admin" && accountId != "su")
                        _userList.Add(accountId, new Tuple<Guid, DateTime, string>(NotificationService.ClientGuid, DateTime.Now, NotificationService.ClientHostName));
                        ret.ActSucc = true;
                        ret.Description = string.Format("{0} login succeed", accountId);
                        ret.AccountInfo = account;
                        ret.SessionId = Guid.NewGuid().ToString();

                        EV.PostMessage(Module, EventEnum.UserLoggedIn, accountId);

                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                string msg = string.Format("account system inner exception", accountId);
                LOG.Write(ex, msg);
                return new LoginResult() { ActSucc = false, Description = msg };
            }
        }

        /// <summary>
        /// 用户注销
        /// </summary>
        /// <param name="accountId"></param>
        public static void Logout(string accountId)
        {
            try
            {
                LOG.Write(string.Format("用户{0}注销登录", accountId));
                accountId = accountId.ToLower();
                if (_userList.ContainsKey(accountId))
                {
                    _userList.Remove(accountId);
                }

                EV.PostMessage("System", EventEnum.UserLoggedOff, accountId);
            }
            catch (Exception ex)
            {
                LOG.Write(ex, string.Format("注销用户{0}发生异常", accountId));
            }
        }

        /// <summary>
        /// 用户被强制注销的理由
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="kickOutReason"></param>
        public static void Kickout(string accountId, string kickOutReason)
        {
            try
            {
                LOG.Write(string.Format("用户{0}强制注销登录", accountId));
                accountId = accountId.ToLower();
                if (_userList.ContainsKey(accountId))
                {
                    EV.PostKickoutMessage(string.Format("用户{0}强制注销登录,{1}", accountId, kickOutReason));
                    _userList.Remove(accountId);
                }

                EV.PostMessage(Module, EventEnum.UserLoggedOff, accountId);
            }
            catch (Exception ex)
            {
                LOG.Write(ex, string.Format("强制注销用户{0}发生异常", accountId));
            }
        }

        /// <summary>
        /// 返回指定用户的账号信息
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public static GetAccountInfoResult GetAccountInfo(string accountId)
        {
            try
            {
                //LOG.Write(string.Format("获取账号信息{0}", accountId));
                accountId = accountId.ToLower();                                        //账号转小写
                GetAccountInfoResult ret = new GetAccountInfoResult();
                if (!FileSigner.IsValid(_accountPath))                                         //检查账号文件的数字签名
                {
                    ret.Description = "账号文件数字签名校验失败";
                    ret.ActSuccess = false;
                }
                else
                {
                    XmlElement userNode = GetAccountNode(accountId);
                    if (userNode == null)
                    {
                        if (accountId == "admin")                                       //如果没有admin账号，则创建默认的admin账号
                        {
                            Account adminAccount = new Account()
                            {
                                Role = "Admin",
                                Permission = GetSingleRolePermission("Admin"),
                                AccountId = "admin",
                                RealName = "admin",
                                Email = "admin@admin.com",
                                Telephone = "86-21-88886666",
                                Touxian = "Admin",
                                Company = "MY Tech",
                                Department = "IT",
                                Description = "Administrator，拥有用户权限修改、菜单修改，定序器修改等权限.",
                                AccountStatus = true,
                                Md5Pwd = Md5Helper.GetMd5Hash("admin")
                            };
                            CreateAccount(adminAccount);
                            ret.ActSuccess = true;
                            ret.AccountInfo = adminAccount;
                            ret.Description = string.Format("成功获取账号信息{0}", accountId);
                        }
                        else
                        {
                            ret.Description = string.Format("账号{0}不存在", accountId);
                            ret.ActSuccess = false;
                        }
                    }
                    else
                    {
                        ret.AccountInfo = new Account
                        {
                            Role = userNode.SelectSingleNode("Role").InnerText,
                            Permission = GetSingleRolePermission(accountId == "admin" ? "Admin" : userNode.SelectSingleNode("Role").InnerText),
                            AccountId = accountId,
                            RealName = userNode.SelectSingleNode("RealName").InnerText,
                            Email = userNode.SelectSingleNode("Email").InnerText,
                            Telephone = userNode.SelectSingleNode("Telephone").InnerText,
                            Touxian = userNode.SelectSingleNode("Touxian").InnerText,
                            Company = userNode.SelectSingleNode("Company").InnerText,
                            Department = userNode.SelectSingleNode("Department").InnerText,
                            Description = userNode.SelectSingleNode("Description").InnerText,
                            AccountStatus = (0 == String.Compare(userNode.SelectSingleNode("AccountState").InnerText, "Enable", true)),
                            AccountCreationTime = userNode.SelectSingleNode("CreationTime").InnerText,
                            LastAccountUpdateTime = userNode.SelectSingleNode("LastUpdateTime").InnerText,
                            LastLoginTime = userNode.SelectSingleNode("LastLoginTime").InnerText,
                            Md5Pwd = userNode.SelectSingleNode("Password").InnerText,
                        };

                        ret.Description = string.Format("获取账号{0}成功", accountId);
                        ret.ActSuccess = true;
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                string msg = string.Format("获取账号{0}发生异常", accountId);
                LOG.Write(ex, msg);
                return new GetAccountInfoResult() { ActSuccess = false, Description = msg };
            }
        }

        /// <summary>
        /// change account password
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="newPassword"></param>
        public static ChangePwdResult ChangePassword(string accountId, string newPassword)
        {
            try
            {
                LOG.Write(string.Format("修改账号{0}的密码", accountId));
                accountId = accountId.ToLower();
                ChangePwdResult ret = new ChangePwdResult();

                if (!FileSigner.IsValid(_accountPath))                                         //检查账号文件的数字签名
                {
                    ret.Description = "修改密码失败，账号文件数字签名损坏！";
                    ret.ActSucc = false;
                }
                else
                {
                    XmlElement userNode = GetAccountNode(accountId);
                    if (userNode == null)
                    {
                        ret.Description = string.Format("账号{0}不存在", accountId);
                        ret.ActSucc = false;
                    }
                    else
                    {
                        userNode.SelectSingleNode("Password").InnerText = Md5Helper.GetMd5Hash(newPassword);
                        Save(_accountXml, _accountPath);
                        ret.Description = "修改密码成功！";
                        ret.ActSucc = true;

                        EV.PostMessage(Module, EventEnum.PasswordChanged, accountId);
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                var msg = string.Format("修改账号{0}的密码失败", accountId);
                LOG.Write(ex, msg);
                return new ChangePwdResult() { ActSucc = false, Description = msg };
            }
        }

        /// <summary>
        /// create account
        /// </summary>
        /// <param name="newAccount"></param>
        /// <returns></returns>
        public static CreateAccountResult CreateAccount(Account newAccount)
        {
            try
            {
                LOG.Write(string.Format("创建账号{0}", newAccount.AccountId));
                CreateAccountResult ret = new CreateAccountResult();
                if (newAccount == null)
                {
                    ret.Description = "账号有误";
                    ret.ActSucc = false;
                }
                else if (!FileSigner.IsValid(_accountPath))                                             //account xml file signer verify
                {
                    ret.Description = string.Format("创建账号失败，数字签名损坏！");
                    ret.ActSucc = false;
                }
                else
                {
                    if (GetAccountNode(newAccount.AccountId) != null)                       //account has been existed
                    {
                        ret.Description = string.Format("创建账号失败，账号 {0} 已存在！", newAccount.AccountId);
                        ret.ActSucc = false;
                    }
                    else
                    {
                        XmlElement userNode = _accountXml.CreateElement("Account");
                        userNode.SetAttribute("AccountId", newAccount.AccountId.ToLower());
                        _accountXml.DocumentElement.AppendChild(userNode);

                        XmlElement subNode = _accountXml.CreateElement("RealName");
                        subNode.InnerText = newAccount.RealName;
                        userNode.AppendChild(subNode);

                        subNode = _accountXml.CreateElement("Role");
                        subNode.InnerText = newAccount.Role.ToString();
                        userNode.AppendChild(subNode);

                        subNode = _accountXml.CreateElement("Password");
                        subNode.InnerText = Md5Helper.GetMd5Hash(newAccount.AccountId);//default new create account's password same as accountId
                        userNode.AppendChild(subNode);

                        subNode = _accountXml.CreateElement("AccountState");//defualt new create account's state "Enable"
                        subNode.InnerText = newAccount.AccountStatus ? "Enable" : "Disable";
                        userNode.AppendChild(subNode);

                        subNode = _accountXml.CreateElement("Email");
                        subNode.InnerText = newAccount.Email;
                        userNode.AppendChild(subNode);

                        subNode = _accountXml.CreateElement("Telephone");
                        subNode.InnerText = newAccount.Telephone;
                        userNode.AppendChild(subNode);

                        subNode = _accountXml.CreateElement("Touxian");
                        subNode.InnerText = newAccount.Touxian;
                        userNode.AppendChild(subNode);

                        subNode = _accountXml.CreateElement("Company");
                        subNode.InnerText = newAccount.Company;
                        userNode.AppendChild(subNode);

                        subNode = _accountXml.CreateElement("Department");
                        subNode.InnerText = newAccount.Department;
                        userNode.AppendChild(subNode);

                        subNode = _accountXml.CreateElement("Description");
                        subNode.InnerText = newAccount.Description;
                        userNode.AppendChild(subNode);

                        subNode = _accountXml.CreateElement("CreationTime");
                        subNode.InnerText = DateTime.Now.ToString();
                        userNode.AppendChild(subNode);

                        subNode = _accountXml.CreateElement("LastLoginTime");
                        subNode.InnerText = string.Empty;
                        userNode.AppendChild(subNode);

                        subNode = _accountXml.CreateElement("LastUpdateTime");
                        subNode.InnerText = string.Empty;
                        userNode.AppendChild(subNode);

                        Save(_accountXml, _accountPath);//save to xml file

                        ret.Description = string.Format("创建新账号{0}成功", newAccount.AccountId);
                        ret.ActSucc = true;

                        EV.PostMessage(Module, EventEnum.AccountCreated, newAccount.AccountId);
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                var msg = string.Format("创建账号{0}失败", newAccount.AccountId);
                LOG.Write(ex, msg);
                return new CreateAccountResult() { ActSucc = false, Description = msg };
            }
        }

        /// <summary>
        /// Administrator user calls this method to delete an account.
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public static DeleteAccountResult DeleteAccount(string accountId)
        {
            try
            {
                LOG.Write(string.Format("删除账号{0}", accountId));
                accountId = accountId.ToLower();
                DeleteAccountResult ret = new DeleteAccountResult();
                if (accountId == "admin")
                {
                    ret.Description = "Admin\'admin\'账号不能删除";
                    ret.ActSucc = false;
                }
                else if (!FileSigner.IsValid(_accountPath))//account xml file signer verify
                {
                    ret.Description = "删除账号失败，账号文件数字签名损坏！";
                    ret.ActSucc = false;
                }
                else
                {
                    XmlElement accountNode = GetAccountNode(accountId);
                    if (accountNode == null)//account has been existed
                    {
                        ret.Description = string.Format("删除账号 {0} 失败，账号不存在！", accountId);
                        ret.ActSucc = false;
                    }
                    else
                    {
                        _accountXml.DocumentElement.RemoveChild(accountNode);//remove account node
                        Save(_accountXml, _accountPath);//save to xml file
                        ret.Description = string.Format("删除账号 {0} 成功！", accountId);
                        ret.ActSucc = true;


                        EV.PostMessage(Module, EventEnum.AccountDeleted, accountId);
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                var msg = string.Format("删除账号{0}发生异常", accountId);
                LOG.Write(ex, msg);
                return new DeleteAccountResult() { ActSucc = false, Description = msg };
            }
        }

        /// <summary>
        /// Update account information
        /// </summary>
        /// <param name="accountList"></param>
        /// <returns></returns>
        public static UpdateAccountResult UpdateAccount(Account account)
        {
            try
            {
                UpdateAccountResult ret = new UpdateAccountResult();
                if (account == null)
                {
                    ret.Description = "账号有误";
                    ret.ActSucc = false;
                }
                else if (!FileSigner.IsValid(_accountPath))                                    //account xml file signer verify
                {
                    ret.Description = string.Format("更新账号资料失败，账号文件数字签名损坏！");
                    ret.ActSucc = false;
                }
                else
                {
                    XmlElement userNode = GetAccountNode(account.AccountId.ToLower());
                    if (userNode == null)
                    {
                        ret.Description = string.Format("更新账号 {0} 失败，账号不存在！", account.AccountId);
                        ret.ActSucc = false;
                    }
                    else
                    {
                        userNode.SelectSingleNode("RealName").InnerText = account.RealName;
                        userNode.SelectSingleNode("Role").InnerText = account.AccountId.ToLower() == "admin" ? "Admin" : account.Role.ToString();
                        userNode.SelectSingleNode("AccountState").InnerText = account.AccountStatus ? "Enable" : "Disable";
                        userNode.SelectSingleNode("Email").InnerText = account.Email;
                        userNode.SelectSingleNode("Telephone").InnerText = account.Telephone;
                        userNode.SelectSingleNode("Touxian").InnerText = account.Touxian;
                        userNode.SelectSingleNode("Company").InnerText = account.Company;
                        userNode.SelectSingleNode("Department").InnerText = account.Department;
                        userNode.SelectSingleNode("Description").InnerText = account.Description;
                        userNode.SelectSingleNode("CreationTime").InnerText = account.AccountCreationTime;
                        userNode.SelectSingleNode("LastLoginTime").InnerText = account.LastLoginTime;
                        userNode.SelectSingleNode("LastUpdateTime").InnerText = account.LastAccountUpdateTime;
                        Save(_accountXml, _accountPath);//save to xml file
                        ret.Description = string.Format("成功更新 {0} 的账号资料！", account.AccountId);
                        ret.ActSucc = true;

                        EV.PostMessage(Module, EventEnum.AccountChanged, account.AccountId);

                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                var msg = string.Format("更新账号{0}资料发生异常", account.AccountId);
                LOG.Write(ex, msg);
                return new UpdateAccountResult() { ActSucc = false, Description = msg };
            }
        }

        public static GetAccountListResult Accounts { get; private set; }

        /// <summary>
        /// get account list
        /// </summary>
        /// <returns></returns>
        public static GetAccountListResult GetAccountList()
        {
            try
            {
                LOG.Write("获取所有的账号信息列表");
                GetAccountListResult ret = new GetAccountListResult();
                if (!FileSigner.IsValid(_accountPath))                                     //account xml file signer verify
                {
                    ret.Description = "获取账号列表失败，账号文件数字签名文件损坏！";
                    ret.ActSuccess = false;
                    ret.AccountList = null;
                }
                else
                {
                    XmlNodeList userNodeList = _accountXml.SelectNodes("AccountManagement/Account");
                    List<Account> accountList = new List<Account>();
                    foreach (XmlNode userNode in userNodeList)
                    {
                        accountList.Add(GetAccountInfo(userNode.Attributes["AccountId"].Value).AccountInfo);
                    }
                    ret.AccountList = accountList;
                    ret.Description = "成功获取账号列表！";
                    ret.ActSuccess = true;
                }

                Accounts = ret;

                return ret;
            }
            catch (Exception ex)
            {
                var msg = "获取账号列表发生异常";
                LOG.Write(ex, msg);
                return new GetAccountListResult() { AccountList = null, ActSuccess = false, Description = msg };
            }
        }
 
        public static void CheckAlive(string accountId)
        {
            try
            {
                if (_userList.ContainsKey(accountId))
                {
                    _userList[accountId] = new Tuple<Guid, DateTime, string>(_userList[accountId].Item1, DateTime.Now, _userList[accountId].Item3);
                }
                else
                {
                     EV.PostKickoutMessage(string.Format("账号{0}已在服务器上注销", accountId));

                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        /// <summary>
        /// get specified account xml node
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        private static XmlElement GetAccountNode(string accountId)
        {
            XmlNode ndl = _accountXml.SelectSingleNode(string.Format("/AccountManagement/Account[@AccountId='{0}']", accountId.ToLower()));
            return (XmlElement)ndl;
        }



        #region view permission 
        /// <summary>
        /// 获取系统注册的所有视图列表
        /// </summary>
        /// <returns></returns>
        public static SerializableDictionary<string, string> GetAllViewList()
        {
            var viewList = new SerializableDictionary<string, string>();
            try
            {
                var xml = new XmlDocument();
                var xmlPath = Path.Combine(PathManager.GetAccountFilePath(), "Views.xml");
                xml.Load(xmlPath);
                var nodes = xml.SelectNodes("/root/Views/View");
                if (nodes != null)
                {
                    foreach (XmlElement node in nodes)
                    {
                        viewList.Add(node.Attributes["Name"].Value, node.Attributes["Description"].Value);
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                viewList = new SerializableDictionary<string, string>();
            }
            return viewList;
        }

        /// <summary>
        /// Save group definition
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool SaveAllRolesPermission(Dictionary<string, Dictionary<string, ViewPermission>> data)
        {
            try
            {
                var rolesNode = _roleXml.SelectSingleNode("/Aitex/Roles") as XmlElement;
                rolesNode.RemoveAll();
                foreach (var item in data)
                {
                    if (item.Key == "Admin") continue;
                    var newRoleNode = _roleXml.CreateElement("Role");
                    newRoleNode.SetAttribute("Name", item.Key);
                    rolesNode.AppendChild(newRoleNode);
                    foreach (var view in data[item.Key].Keys)
                    {
                        var newViewNode = _roleXml.CreateElement("View");
                        newRoleNode.AppendChild(newViewNode);
                        newViewNode.SetAttribute("Name", view);
                        newViewNode.SetAttribute("Permission", data[item.Key][view].ToString());
                    }
                }
                _roleXml.Save(_rolePath);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
            return true;
        }


        /// <summary>
        /// 获取当前系统定义的分组
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetAllRoles()
        {
            List<string> roles = new List<string>();
            try
            {
                var nodes = _roleXml.SelectNodes("/Aitex/Roles/Role");
                foreach (XmlElement node in nodes)
                {
                    roles.Add(node.Attributes["Name"].Value);
                }
                //如果没有管理员组，默认添加管理员组
                if (!roles.Contains("Admin"))
                {
                    roles.Add("Admin");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                roles = new List<string>();
            }
            return roles;
        }


        /// <summary>
        /// 获取指定用户角色的权限设定
        /// </summary>
        /// <param name="roleName">指定用户的角色名</param>
        /// <returns></returns>
        public static SerializableDictionary<string, ViewPermission> GetSingleRolePermission(string roleName)
        {
            var rolePermission = new SerializableDictionary<string, ViewPermission>();
            try
            {
                var viewDic = GetAllViewList();
                if (roleName == "Admin")
                {
                    foreach (var view in viewDic)
                    {
                        rolePermission.Add(view.Key, ViewPermission.FullyControl);
                    }
                }
                else
                {
                    /* RoleName, ViewName, ViewPermission */
                    var nodes = _roleXml.SelectSingleNode(string.Format("/Aitex/Roles/Role[@Name='{0}']", roleName));
                    if (nodes != null)
                    {
                        foreach (XmlElement viewNode in nodes)
                        {
                            var viewName = viewNode.Attributes["Name"].Value;
                            var permission = viewNode.Attributes["Permission"].Value;
                            if (viewDic.ContainsKey(viewName))
                            {
                                rolePermission.Add(viewName, (ViewPermission)Enum.Parse(typeof(ViewPermission), permission, true));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                rolePermission = new SerializableDictionary<string, ViewPermission>();
            }
            return rolePermission;
        }

        /// <summary>
        /// 获取所有用户角色的权限设定
        /// </summary>
        /// <returns></returns>
        public static SerializableDictionary<string, SerializableDictionary<string, ViewPermission>> GetAllRolesPermission()
        {
            try
            {
                var rolePermission = new SerializableDictionary<string, SerializableDictionary<string, ViewPermission>>();
                foreach (var role in GetAllRoles())
                    rolePermission.Add(role, GetSingleRolePermission(role));
                return rolePermission;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return new SerializableDictionary<string, SerializableDictionary<string, ViewPermission>>();
            }
        }
        #endregion
    }
}

