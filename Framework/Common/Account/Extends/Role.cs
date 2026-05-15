using System;

namespace MECF.Framework.Common.Account.Extends
{
    [Serializable]
    public class Role
    {
        private string _RoleID;
        private string _RoleName;
        private bool _AutoLogout;
        private int _LogoutTime;
        private string _MenuPermission;
        private string _Description;


        public string RoleID
        {
            get { return _RoleID; }
            set { _RoleID = value; }
        }

        public string RoleName
        {
            get { return _RoleName; }
            set { _RoleName = value; }
        }

        public bool IsAutoLogout
        {
            get { return _AutoLogout; }
            set { _AutoLogout = value; }
        }

        public int LogoutTime
        {
            get { return _LogoutTime; }
            set { _LogoutTime = value; }
        }

        public string MenuPermission
        {
            get { return _MenuPermission; }
            set { _MenuPermission = value; }
        }

        public string Description
        {
            get { return _Description; }
            set { _Description = value; }
        }

        public bool IsSuper { get; set; }

        public Role(string p_strRoleID, string p_strRoleName, bool p_bAutoLogout, int p_nLogoutTime, string p_strPermission, string p_strDescription = "")
        {
            this.RoleID = p_strRoleID;
            this._RoleName = p_strRoleName;
            this._AutoLogout = p_bAutoLogout;
            this._LogoutTime = p_nLogoutTime;
            this._Description = p_strDescription;
            this._MenuPermission = p_strPermission;
            this.IsSuper = false;
        }
    }
}
