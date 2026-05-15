using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Aitex.Core.Account;

namespace MECF.Framework.UI.Core.Accounts
{
    /// <summary>
    /// Interaction logic for RoleEditView.xaml
    /// </summary>
    public partial class RoleEditView : Window
    {

        public RoleEditView()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(RoleEditView_Loaded);
            dataGrid1.CanUserAddRows = false;
            dataGrid1.RowHeight = 25;
        }


        public class PermissionClass
        {
            public static Dictionary<Permission_CN, string> ViewPermissionCheck = new Dictionary<Permission_CN, string>();
            static IEnumerable<Permission_CN> permissions = Enum.GetValues(typeof(Permission_CN)).Cast<Permission_CN>();
            static PermissionClass()
            {
            
               foreach (Permission_CN p in permissions)
               {
                   if (p == Permission_CN.ProcessHidden)
                   {
                       //只针对ReactorProcess相关界面
                       ViewPermissionCheck.Add(p, "RecipeMonitor"); //"ProcessView"
                   }
                   else
                   {
                       ViewPermissionCheck.Add(p, "");
                   }
               }
            }

            public static List<Permission_CN> GetPermissions(string viewName)
            {
                List<Permission_CN> permissionInfos = new List<Permission_CN>();
                foreach (Permission_CN p in ViewPermissionCheck.Keys)
                {
                    if (ViewPermissionCheck[p] == "")
                        permissionInfos.Add(p);
                    else if(viewName.IndexOf(ViewPermissionCheck[p])>=0)
                        permissionInfos.Add(p);
                }
                return permissionInfos;
            }

        }


        public enum Permission_CN
        {
            Hidden,
            ReadOnly,
            Partial,
            Full,
            ProcessHidden
        }

        public List<ExpandoObject> DataSource = new List<ExpandoObject>();

        void RoleEditView_Loaded(object sender, RoutedEventArgs e)
        {
            var viewList = AccountClient.Instance.Service.GetAllViewList();           
            var viewPermission = AccountClient.Instance.Service.GetAllRolesPermission();

            //Dynamically do data binding function here
            foreach (var role in viewPermission.Keys)
            {
                dynamic singleViewPermission = new ExpandoObject();

                ((IDictionary<String, object>)singleViewPermission).Add(new KeyValuePair<string, object>("RoleName", role));
                foreach (var view in viewPermission[role].Keys)
                {
                    switch (viewPermission[role][view])
                    {
                        case ViewPermission.Invisiable:
                            ((IDictionary<String, object>)singleViewPermission).Add(new KeyValuePair<string, object>(view, Permission_CN.Hidden));
                            break;
                        case ViewPermission.Readonly:
                            ((IDictionary<String, object>)singleViewPermission).Add(new KeyValuePair<string, object>(view, Permission_CN.ReadOnly));
                            break;
                        case ViewPermission.PartlyControl:
                            ((IDictionary<String, object>)singleViewPermission).Add(new KeyValuePair<string, object>(view, Permission_CN.Partial));
                            break;
                        case ViewPermission.FullyControl:
                            ((IDictionary<String, object>)singleViewPermission).Add(new KeyValuePair<string, object>(view, Permission_CN.Full));
                            break;
                        case ViewPermission.ProcessOPControl:
                            ((IDictionary<String, object>)singleViewPermission).Add(new KeyValuePair<string, object>(view, Permission_CN.ProcessHidden));
                            break;
                    }
                }

                DataSource.Add(singleViewPermission);
            }
            dataGrid1.DataContext = DataSource;


            //dynamically creating data grid column
            foreach (var view in viewList)
            {
                var datagGridColumn = new DataGridComboBoxColumn() { Header = view.Value, Width = 100 };
                dataGrid1.Columns.Add(datagGridColumn);
                datagGridColumn.SelectedItemBinding = new Binding() { Path = new PropertyPath(view.Key) };

                datagGridColumn.ItemsSource = PermissionClass.GetPermissions(view.Key);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnAddNew_Click(object sender, RoutedEventArgs e)
        {
            dynamic singleViewPermission = new ExpandoObject();

            ((IDictionary<String, object>)singleViewPermission).Add(new KeyValuePair<string, object>("RoleName", "New Role"));

            var viewList = AccountClient.Instance.Service.GetAllViewList();

            foreach (var view in viewList.Keys)
            {
                ((IDictionary<String, object>)singleViewPermission).Add(new KeyValuePair<string, object>(view, Permission_CN.Hidden));
            }

            DataSource.Add(singleViewPermission);

            dataGrid1.DataContext = null;
            dataGrid1.DataContext = DataSource;
        }

        /// <summary>
        /// Save role to xml file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < DataSource.Count - 1; i++)
            {
                for (int j = i + 1; j < DataSource.Count; j++)
                {
                    dynamic obj1 = DataSource[i];
                    dynamic obj2 = DataSource[j];
                    if (String.Compare(obj1.RoleName, obj2.RoleName) == 0)
                    {
                        MessageBox.Show(string.Format("\"{0}\" Existed，modify to save！", obj1.RoleName), "Role Editor Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
            }

            var permissionDic = new Dictionary<string, Dictionary<string,ViewPermission>>();
            foreach (dynamic role in DataSource)
            {
                permissionDic.Add(role.RoleName, new Dictionary<string, ViewPermission>());
                foreach (var item in ((IDictionary<String, object>)role).Keys)
                {
                    if (item == "RoleName") continue;
                    var perm = (Permission_CN)((IDictionary<String, object>)role)[item];
                    switch (perm)
                    {
                        case Permission_CN.Full:
                            permissionDic[role.RoleName].Add(item, ViewPermission.FullyControl);
                            break;
                        case Permission_CN.Partial:
                            permissionDic[role.RoleName].Add(item, ViewPermission.PartlyControl);
                            break;
                        case Permission_CN.ReadOnly:
                            permissionDic[role.RoleName].Add(item, ViewPermission.Readonly);
                            break;
                        case Permission_CN.Hidden:
                            permissionDic[role.RoleName].Add(item, ViewPermission.Invisiable);
                            break;
                        case Permission_CN.ProcessHidden:
                            permissionDic[role.RoleName].Add(item, ViewPermission.ProcessOPControl);
                            break;
                        
                    }
                }
            }

            if (AccountClient.Instance.Service.SaveAllRolesPermission(permissionDic))
            {
                MessageBox.Show(Application.Current.Resources["GlobalLableAccountViewSaveRoleOk"].ToString());
            }
            else
            {
                MessageBox.Show(Application.Current.Resources["GlobalLableAccountViewSaveRoleFailed"].ToString());
            }
        }

        /// <summary>
        /// Delete current selecte role
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRoleDelete_Click(object sender, RoutedEventArgs e)
        {
            dynamic obj = dataGrid1.SelectedItem;
            if (obj != null)
            {
                if (MessageBox.Show(string.Format(Application.Current.Resources["GlobalLableAccountViewDeleteRoleInfo"].ToString(), obj.RoleName), Application.Current.Resources["GlobalLableAccountViewMsgTitle"].ToString(), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    foreach (dynamic role in DataSource)
                    {
                        if (role.RoleName == obj.RoleName)
                        {
                            DataSource.Remove(role);
                            break;
                        }
                    }
                    dataGrid1.DataContext = null;
                    dataGrid1.DataContext = DataSource;
                }
            }
        }
    }
}
