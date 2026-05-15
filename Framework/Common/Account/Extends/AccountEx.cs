using System;
using System.Collections.Generic;

namespace MECF.Framework.Common.Account.Extends
{

    [Serializable]
    public class AccountEx
    {
        private string m_strUserID;
        private string m_strLoginName;
        private string m_strFirstName;
        private string m_strLastName;
        private string m_strEmail;
        private string m_strDescription;
        private string m_strPassword;
        private List<string> m_strRoles;

        public string UserID
        {
            get { return m_strUserID; }
            set { m_strUserID = value; }
        }

        public string LoginName
        {
            get { return m_strLoginName; }
            set { m_strLoginName = value; }
        }

        public string FirstName
        {
            get { return m_strFirstName; }
            set { m_strFirstName = value; }
        }

        public string LastName
        {
            get { return m_strLastName; }
            set { m_strLastName = value; }
        }

        public string Email
        {
            get { return m_strEmail; }
            set { m_strEmail = value; }
        }

        public string Description
        {
            get { return m_strDescription; }
            set { m_strDescription = value; }
        }

        public string Password
        {
            get { return m_strPassword; }
            set { m_strPassword = value; }
        }

        public string LoginId { get; set; }

        public bool IsSuper { get; set; }

        public List<string> RoleIDs
        {
            get { return m_strRoles; }
            set { m_strRoles = value; }
        }

        public AccountEx(string p_strUserID, string p_strLoginName, string p_strPassword, string p_strFirstName, string p_strLastName, string p_strEmail, List<string> p_roles, string p_strDescription = "")
        {
            this.m_strUserID = p_strUserID;
            this.m_strLoginName = p_strLoginName;
            this.m_strFirstName = p_strFirstName;
            this.m_strLastName = p_strLastName;
            this.m_strEmail = p_strEmail;
            this.m_strDescription = p_strDescription;
            this.m_strPassword = p_strPassword;
            this.m_strRoles = p_roles;
            this.IsSuper = false;
        }

    }
}
