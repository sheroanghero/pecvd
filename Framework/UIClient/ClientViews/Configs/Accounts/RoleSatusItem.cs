namespace MECF.Framework.UI.Client.CenterViews.Configs.Accounts
{
    public class RoleStatusItem
    {
        public string RoleID { get; set; }
        /// <summary>
        /// role name
        /// </summary>
        private string m_strRoleName;
        public string RoleName
        {
            get { return m_strRoleName; }
            set { m_strRoleName = value; }
        }

        /// <summary>
        /// role Status
        /// </summary>
        private bool m_bRoleStatus;
        public bool RoleStatus
        {
            get { return m_bRoleStatus; }
            set { m_bRoleStatus = value; }
        }

        public bool DisplayRoleStatus { get; set; }

        public RoleStatusItem Clone()
        {
            RoleStatusItem entity = new RoleStatusItem();
            entity.RoleID = this.RoleID;
            entity.RoleName = this.RoleName;
            entity.RoleStatus = this.RoleStatus;
            entity.DisplayRoleStatus = this.RoleStatus;
            return entity;
        }
    }
}
