using System.Windows;
using System.Windows.Controls;

namespace MECF.Framework.UI.Core.Accounts
{
    /// <summary>
    /// Interaction logic for RolePermissionEdit.xaml
    /// </summary>
    public partial class RolePermissionEdit : Window
    {
        public RolePermissionEdit()
        {
            InitializeComponent();
            DataContext = roleViewModel;
            Loaded += new RoutedEventHandler(RolePermissionEdit_Loaded);
            roleViewModel.InitialRolePermissionVM();
        }

        void RolePermissionEdit_Loaded(object sender, RoutedEventArgs e)
        {
            //if (comboRoles.SelectedItem == null) { MessageBox.Show("请选择需要编辑的角色"); return; }

            roleViewModel.CurrentRoleName = comboRoles.SelectedValue + "";
            roleViewModel.BindAll(); 
        }
        RolePermissionViewModel roleViewModel = new RolePermissionViewModel();

        private void comboRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string newrole = comboRoles.SelectedValue+"";
            if (!string.IsNullOrEmpty(newrole))
                roleViewModel.SelectRoleChanged(newrole);
        }

    }
}
