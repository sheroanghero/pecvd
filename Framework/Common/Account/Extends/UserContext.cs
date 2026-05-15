using System;
using MECF.Framework.Common.CommonData;

namespace MECF.Framework.Common.Account.Extends
{
    [Serializable]
    public class UserContext: NotifiableItem
    {       
        public string Address { get; set; }
        public string HostName { get; set; }
      
        private string _loginName;
        public string LoginName
        {
            get { return this._loginName; }
            set { this._loginName = value; this.InvokePropertyChanged("LoginName"); }
        }

        private string _rolename;
        public string RoleName
        {
            get { return this._rolename; }
            set { this._rolename = value; this.InvokePropertyChanged("RoleName"); }
        }          

        public string RoleID { get; set; }
        public string LoginId { get; set; }

        public Role Role { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime LastAccessTime { get; set; }     
        public int Token { get; set; }  
        public string Language { get; private set; }
        public bool IsLogin { get; set; }
    }
}
