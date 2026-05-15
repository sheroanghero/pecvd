using System.Collections.ObjectModel;
using Caliburn.Micro.Core;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Configs.Roles
{
    public class RoleItem : PropertyChangedBase
    {
        public RoleItem(Common.Account.Extends.Role r)
        {
            this._Role = r;
        }

        public RoleItem(string roleid)
        {
            this._Role = new Common.Account.Extends.Role(roleid, string.Empty, false, 0, string.Empty, string.Empty);
        }

        private Common.Account.Extends.Role _Role;
        public Common.Account.Extends.Role Role
        {
            get { return _Role; }
            set { _Role = value; }
        }

        public string RoleID
        {
            get { return _Role.RoleID; }
            set { _Role.RoleID = value; }
        }

        public string RoleName
        {
            get { return _Role.RoleName; }
            set { _Role.RoleName = value; }
        }

        public int AutoLogoutTime
        {
            get { return _Role.LogoutTime; }
            set { _Role.LogoutTime = value; }
        }

        public string Description
        {
            get { return _Role.Description; }
            set { _Role.Description = value; }
        }
        
        public bool IsAutoLogout
        {
            get { return _Role.IsAutoLogout; }
            set { _Role.IsAutoLogout = value; }
        }

        private string _DisplayRoleName;
        public string DisplayRoleName
        {
            get { return _DisplayRoleName; }
            set { _DisplayRoleName = value; NotifyOfPropertyChange("DisplayRoleName"); }
        }

        public int DisplayAutoLogoutTime
        {
            get;
            set;
        }

        public bool DisplayIsAutoLogout
        {
            get;
            set;
        }


        public string DisplayDescription
        {
            get;
            set;
        }


        private bool _IsSelected = false;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { _IsSelected = value; NotifyOfPropertyChange("IsSelected"); }
        }

        private bool _RoleNameTextSaved = true;
        public bool RoleNameTextSaved
        {
            get { return _RoleNameTextSaved; }
            set
            {
                if (_RoleNameTextSaved != value)
                {
                    _RoleNameTextSaved = value;
                    NotifyOfPropertyChange("RoleNameTextSaved");
                }
            }
        }

        private bool _TimeTextSaved = true;
        public bool TimeTextSaved
        {
            get { return _TimeTextSaved; }
            set
            {
                if (_TimeTextSaved != value)
                {
                    _TimeTextSaved = value;
                    NotifyOfPropertyChange("TimeTextSaved");
                }
            }
        }
        private bool _DescriptionTextSaved = true;
        public bool DescriptionTextSaved
        {
            get { return _DescriptionTextSaved; }
            set
            {
                if (_DescriptionTextSaved != value)
                {
                    _DescriptionTextSaved = value;
                    NotifyOfPropertyChange("DescriptionTextSaved");
                }
            }
        }
        

        private ObservableCollection<MenuInfo> _MenuColleciton = new ObservableCollection<MenuInfo>();
        public ObservableCollection<MenuInfo> MenuCollection
        {
            get { return _MenuColleciton; }
        }

        public void AddMenuInfo(MenuInfo pInfo)
        {
            if (null == pInfo)
                return;

            MenuCollection.Add(pInfo);
        }

        public bool IsRoleChanged()
        {
            if (this.DisplayRoleName != this.Role.RoleName)
                return true;

            if (this.DisplayAutoLogoutTime != this.Role.LogoutTime)
                return true;

            if (this.DisplayIsAutoLogout != this.Role.IsAutoLogout)
                return true;

            if (this.DisplayDescription != this.Role.Description)
                return true;

            foreach (MenuInfo item in _MenuColleciton)
            {
                if (item.DisplayIndexPermission != item.IndexPermission - 1)
                    return true;
            }

            return false;
        }
    }

    public class MenuInfo : PropertyChangedBase
    {
        private string _strMenuID;
        public string ID
        {
            get { return _strMenuID; }
            set
            {
                if (value != _strMenuID)
                    _strMenuID = value;
            }
        }

        private MenuPermissionEnum _EnummenuPermission;
        public MenuPermissionEnum EnumPermission
        {
            get { return _EnummenuPermission; }
            set
            {
                if (value != _EnummenuPermission)
                {
                    _EnummenuPermission = value;
                    _IndexMenuPermission = (int)_EnummenuPermission;
                }
            }
        }

        private string _StrMenuPermission;
        public string stringPermission
        {
            get { return _StrMenuPermission; }
            set
            {
                if (value != _StrMenuPermission)
                    _StrMenuPermission = value;
            }
        }

        private int _IndexMenuPermission;
        public int IndexPermission
        {
            get { return _IndexMenuPermission; }
            set
            {
                if (value != _IndexMenuPermission)
                {
                    _IndexMenuPermission = value;
                    EnumPermission = (MenuPermissionEnum)_IndexMenuPermission;
                }
            }
        }

        private int _DisplayIndexPermission;
        public int DisplayIndexPermission
        {
            get { return _DisplayIndexPermission; }
            set
            {
                if (value != _DisplayIndexPermission)
                {
                    _DisplayIndexPermission = value;
                }
            }
        }

        private bool _ComboBoxSaved = false;
        public bool ComboBoxSaved
        {
            get { return _ComboBoxSaved; }
            set
            {
                if (_ComboBoxSaved != value)
                {
                    _ComboBoxSaved = value;
                    NotifyOfPropertyChange("ComboBoxSaved");
                }
            }
        }

        private string _strMenuName;
        public string Name
        {
            get { return _strMenuName; }
            set
            {
                if (value != _strMenuName)
                    _strMenuName = value;
            }
        }

        public MenuInfo Clone()
        {
            MenuInfo menuInfo = new MenuInfo();

            menuInfo.Name = this.Name;
            menuInfo.stringPermission = this.stringPermission;
            menuInfo.EnumPermission = this.EnumPermission;
            menuInfo.ID = this.ID;
            menuInfo.IndexPermission = this.IndexPermission;
            menuInfo.DisplayIndexPermission = this.IndexPermission - 1;

            return menuInfo;
        }
    }
}
