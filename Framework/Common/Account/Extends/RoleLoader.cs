using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace MECF.Framework.Common.Account.Extends
{

    public class RoleLoader : XmlLoader
    {
        private List<Role> m_rolelist;
        public List<Role> RoleList
        {
            get { return m_rolelist; }
            set { m_rolelist = value; }
        }

        private List<AccountEx> m_accountlist;
        public List<AccountEx> AccountList
        {
            get { return m_accountlist; }
            set { m_accountlist = value; }
        }


        public RoleLoader(string p_strPath)
            : base(p_strPath)
        {
        }

        /// <summary>
        /// Return all roles except super role       
        /// </summary>      
        public List<Role> GetRoles()
        {
            return this.m_rolelist.Where(e => { return !e.IsSuper; }).ToList();
        }

        /// <summary>
        /// Return all accounts except super account       
        /// </summary>    
        public List<AccountEx> GetAccounts()
        {
            return this.m_accountlist.Where(e => { return !e.IsSuper; }).ToList();
        }

        protected override void AnalyzeXml()
        {
            if (this.m_xdoc != null)
            {
                //load roles
                var results = from r in this.m_xdoc.Descendants("roleItem") select r;
                List<Role> rolelist = new List<Role>();
                bool IsAutoLogout = false;
                int nLogoutTime;
                foreach (var result in results)
                {
                    string RoleID = result.Attribute("id").Value;
                    string RoleName = result.Attribute("name").Value;
                    string AutoLogout = result.Attribute("autologout").Value;
                    string LogoutTime = result.Attribute("logouttime").Value;
                    int.TryParse(LogoutTime, out nLogoutTime);
                    IsAutoLogout = AutoLogout == "1" ? true : false;
                    string Description = "";

                    if (result.Attribute("description") != null)
                        Description = result.Attribute("description").Value;

                    string Permissions = result.Value;
                    Role roleObject = new Role(RoleID, RoleName, IsAutoLogout, nLogoutTime, Permissions, Description);
                    rolelist.Add(roleObject);
                }
                //Create an super role
                Role superRole = new Role("-1", "Administrators", true, 20, null, null) { IsSuper = true };
                rolelist.Add(superRole);
                this.m_rolelist = rolelist;

                //load users
                results = from r in this.m_xdoc.Descendants("userItem") select r;
                List<AccountEx> accountlist = new List<AccountEx>();
                foreach (var result in results)
                {
                    List<string> roleIds = new List<string>();
                    string UserID = result.Attribute("id").Value;
                    string LoginName = result.Attribute("loginname").Value;
                    string Password = Decrypt(result.Attribute("password").Value);
                    string FirstName = result.Attribute("firstname").Value;
                    string LastName = result.Attribute("lastname").Value;
                    string Email = result.Attribute("email").Value;
                    string Description = "";
                    if (result.Attribute("description") != null)
                        Description = result.Attribute("description").Value;

                    var roles = from ro in result.Descendants("role") select ro;
                    foreach (var role in roles)
                    {
                        string strID = role.Attribute("id").Value;
                        roleIds.Add(strID);
                    }
                    AccountEx accountObject = new AccountEx(UserID, LoginName, Password, FirstName, LastName, Email, roleIds, Description);
                    accountlist.Add(accountObject);
                }

                AccountEx superAccount = new AccountEx("-1", "admin", "admin", "", "", "", new List<string>() { "-1" }, "") { IsSuper = true };
                accountlist.Add(superAccount);
                this.m_accountlist = accountlist;
            }
        }

        public bool UpdateRole(Role p_newRole)
        {
            Role m_role = m_rolelist.Find(item => item.RoleID == p_newRole.RoleID);
            if (m_role == null)
                m_rolelist.Add(p_newRole);
            else
                m_rolelist[m_rolelist.IndexOf(m_role)] = p_newRole;

            //save the roles to file
            XDocument xdoc = this.m_xdoc;
            var results = (from m_xRole in xdoc.Descendants("roleItem")
                           where m_xRole.Attribute("id").Value == p_newRole.RoleID
                           select m_xRole).ToList();
            if (results.Count > 0)
            {
                results[0].Attribute("name").Value = p_newRole.RoleName;
                results[0].Attribute("autologout").Value = p_newRole.IsAutoLogout ? "1" : "0";
                results[0].Attribute("logouttime").Value = p_newRole.LogoutTime.ToString();
                if (results[0].Attribute("description") != null)
                    results[0].Attribute("description").Value = p_newRole.Description.ToString();

                results[0].Value = p_newRole.MenuPermission;
            }
            else
            {
                XElement m_new =
                           new XElement("roleItem",
                               new XAttribute("id", p_newRole.RoleID),
                               new XAttribute("name", p_newRole.RoleName),
                               new XAttribute("autologout", p_newRole.IsAutoLogout ? "1" : "0"),
                               new XAttribute("logouttime", p_newRole.LogoutTime),
                               new XAttribute("description", p_newRole.Description)
                               )
                           { Value = p_newRole.MenuPermission };
                xdoc.Root.Element("roles").Add(m_new);
            }

            xdoc.Save(this.m_strPath);
            return true;
        }

        public bool DeleteRole(string p_strRoleID)
        {
            this.Load();

            Role m_role = m_rolelist.Find(item => item.RoleID == p_strRoleID);
            if (m_role != null)
            {
                m_rolelist.Remove(m_role);

                //save the roles to file
                XDocument xdoc = this.m_xdoc;
                var results = (from m_xRole in xdoc.Descendants("roleItem")
                               where m_xRole.Attribute("id").Value == p_strRoleID
                               select m_xRole).ToList();
                if (results.Count > 0)
                {
                    results[0].Remove();

                    //remove role from account
                    foreach (var account in this.m_accountlist)
                    {
                        if (account.RoleIDs.Contains(m_role.RoleID))
                            account.RoleIDs.Remove(m_role.RoleID);
                    }

                    results = (from m_xRole in xdoc.Descendants("role")
                               where m_xRole.Attribute("id").Value == m_role.RoleID
                               select m_xRole).ToList();
                    if (results.Count > 0)
                        results.Remove();

                    xdoc.Save(this.m_strPath);
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        private List<string> GetRolePermission(string roleid)
        {
            List<string> rolePermissions = new List<string>();
            foreach (Role role in this.m_rolelist)
            {
                if (role.RoleID == roleid)
                {
                    rolePermissions = role.MenuPermission.Split(';').ToList();
                    break;
                }
            }
            return rolePermissions;
        }

        private int GetMenuPermission(List<string> rolePermissions, string menuid)
        {
            foreach (string menuPermission in rolePermissions)
            {
                if (menuPermission.IndexOf(menuid) >= 0)
                {
                    string[] pair = menuPermission.Split(',');
                    if (pair.Length > 1 && pair[0].Trim() == menuid)  //need check the whole menuid
                        return int.Parse(pair[1].Trim());
                }
            }
            return 3;
        }

        public List<AppMenu> GetMenusByRole(string roleid, List<AppMenu> menulist)
        {
            List<AppMenu> menus = new List<AppMenu>();
            List<string> rolePermissions = GetRolePermission(roleid);

            foreach (AppMenu menuItem in menulist)
            {
                List<AppMenu> subMenus = new List<AppMenu>();

                foreach (AppMenu subMenu in menuItem.MenuItems)
                {
                    AppMenu RetSubMenu = new AppMenu(subMenu.MenuID, subMenu.ViewModel, subMenu.ResKey, null);
                    RetSubMenu.System = subMenu.System;
                    RetSubMenu.Type = subMenu.Type;
                    RetSubMenu.AlarmModule = subMenu.AlarmModule;
                    RetSubMenu.Permission = this.GetMenuPermission(rolePermissions, subMenu.MenuID);
                    if (RetSubMenu.Permission > 1)
                        subMenus.Add(RetSubMenu);
                }

                if (subMenus.Count > 0)
                {
                    var menu = new AppMenu(menuItem.MenuID, menuItem.ViewModel, menuItem.ResKey, subMenus);
                    menu.System = menuItem.System;
                    menu.Type = menuItem.Type;
                    menu.AlarmModule = menuItem.AlarmModule;
                    menus.Add(menu);
                }
            }
            return menus;
        }

        public int GetMenuPermission(string roleid, string menuName)
        {
            List<string> rolePermissions = GetRolePermission(roleid);

            int menuPermission = this.GetMenuPermission(rolePermissions, menuName);
            return menuPermission;
        }

        public bool UpdateAccount(AccountEx p_newAccount)
        {
            AccountEx Acc = m_accountlist.Find(item => item.UserID == p_newAccount.UserID);
            if (Acc == null)
                m_accountlist.Add(p_newAccount);
            else
                m_accountlist[m_accountlist.IndexOf(Acc)] = p_newAccount;

            //save the roles to file
            XDocument xdoc = this.m_xdoc;
            var results = (from xAccount in xdoc.Descendants("userItem")
                           where xAccount.Attribute("id").Value == p_newAccount.UserID
                           select xAccount).ToList();
            if (results.Count > 0)
            {
                results[0].Attribute("loginname").Value = p_newAccount.LoginName;
                results[0].Attribute("password").Value = Encrypt(p_newAccount.Password);
                results[0].Attribute("firstname").Value = p_newAccount.FirstName;
                results[0].Attribute("lastname").Value = p_newAccount.LastName;
                results[0].Attribute("email").Value = p_newAccount.Email;
                results[0].Attribute("description").Value = p_newAccount.Description;
                results[0].Element("rolegroup").RemoveAll();

                foreach (string strRole in p_newAccount.RoleIDs)
                {
                    results[0].Element("rolegroup").Add(new XElement("role", new XAttribute("id", strRole)));
                }
            }
            else
            {
                XElement m_new =
                           new XElement("userItem",
                               new XAttribute("id", p_newAccount.UserID),
                               new XAttribute("loginname", p_newAccount.LoginName),
                               new XAttribute("password", Encrypt(p_newAccount.Password)),
                               new XAttribute("firstname", p_newAccount.FirstName),
                               new XAttribute("lastname", p_newAccount.LastName),
                               new XAttribute("email", p_newAccount.Email),
                               new XAttribute("description", p_newAccount.Description),
                               new XElement("rolegroup"));
                foreach (string strRole in p_newAccount.RoleIDs)
                {
                    m_new.Element("rolegroup").Add(new XElement("role", new XAttribute("id", strRole)));
                }
                xdoc.Root.Element("users").Add(m_new);
            }
            xdoc.Save(this.m_strPath);
            return true;
        }

        public bool DeleteAccount(string p_strUserID)
        {
            AccountEx Acc = m_accountlist.Find(item => item.UserID == p_strUserID);
            if (Acc != null)
            {
                m_accountlist.Remove(Acc);

                XDocument xdoc = this.m_xdoc;
                var results = (from xAccount in xdoc.Descendants("userItem")
                               where xAccount.Attribute("id").Value == p_strUserID
                               select xAccount).ToList();
                if (results.Count > 0)
                {
                    results[0].Remove();

                    xdoc.Save(this.m_strPath);
                    return true;

                }
                else
                    return false;
            }
            else
                return false;
        }

        public String Encrypt(String encrytStr)
        {
            if (String.IsNullOrWhiteSpace(encrytStr)) return String.Empty;

            try
            {
                Byte[] bytes = Encoding.UTF8.GetBytes(encrytStr);

                return Convert.ToBase64String(bytes);
            }
            catch
            {
                return encrytStr;
            }
        }

        public String Decrypt(String decryptStr)
        {
            if (String.IsNullOrWhiteSpace(decryptStr)) return String.Empty;

            try
            {
                Byte[] bytes = Convert.FromBase64String(decryptStr);

                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return decryptStr;
            }
        }
    }
}

