using System.Collections.ObjectModel;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Configs.Roles
{
    public class RolePermissionMapper
    {
        private RolePermissionMapper()
        {
            _DicPermission.Add(new PermissionType() 
            {   EnumPermission = MenuPermissionEnum.MP_NONE, 
                StringPermission = "NONE"
            });

            _DicPermission.Add(new PermissionType()
            {
                EnumPermission = MenuPermissionEnum.MP_READ,
                StringPermission = "Read" 
            });

            _DicPermission.Add(new PermissionType()
            {
                EnumPermission = MenuPermissionEnum.MP_READ_WRITE,
                StringPermission = "Read & Write"
            });
        }

        private static RolePermissionMapper _Instance = null;
        public static RolePermissionMapper Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new RolePermissionMapper();
                }

                return _Instance;
            }
        }

        private ObservableCollection<PermissionType> _DicPermission = new ObservableCollection<PermissionType>();
        public ObservableCollection<PermissionType> PermissionDictionary
        {
            get { return _DicPermission; }
        }

        public int ToInt(MenuPermissionEnum enumPermistion)
        {
            return (int)enumPermistion;
        }

        public string ToString(MenuPermissionEnum enumPermistion)
        {
            foreach (PermissionType pd in _DicPermission)
            {
                if (pd.EnumPermission == enumPermistion)
                {
                    return pd.StringPermission;
                }
            }

            return "";
        }
    }

    public class PermissionType
    {
        public MenuPermissionEnum EnumPermission{get;set;}
        public string StringPermission { get; set; }  
    }
}
