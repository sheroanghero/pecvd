using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Aitex.Core.RT.Log;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.Command;

namespace MECF.Framework.UI.Client.CenterViews.Configs.Roles
{
    public class RoleViewModel : BaseModel
    {
        public bool IsPermission { get => this.Permission == 3; }

        private bool _IsEnabledRoleName = false;
        public bool IsEnabledRoleName
        {
            get { return _IsEnabledRoleName; }
            set { _IsEnabledRoleName = value; NotifyOfPropertyChange("IsEnabledRoleName"); }
        }

        public RoleViewModel()
        {
            this.DisplayName = "Role";
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
        }
        protected override void OnActivate()
        {
            _RolesList.Clear();
            _TreeSelectedRole = null;
            RoleManager.Initialize();
            LoadRoleList();

            base.OnActivate();
        }

        protected override void OnDeactivate(bool close)
        {
            if (ControlMode == CtrlMode.EDIT && IsPermission)
            {
                if (DialogBox.Confirm("The data has been modified. Do you want to save the change(s)?"))
                {
                    if (SaveChanged())
                    {
                        ControlMode = CtrlMode.VIEW;
                        DialogBox.ShowInfo("Operated successfully.");
                    }
                }
            }
            base.OnDeactivate(close);
        }

        public void OnRoleChanged()
        {
            string[] RoleNameList = { "Manager", "Engineer", "Technician", "Operator" };
            string _RoleName = RoleNameList.Contains(TreeSelectedRole.DisplayRoleName) ? TreeSelectedRole.DisplayRoleName : "";
            IsEnabledRoleName = true;
            if (!string.IsNullOrEmpty(_RoleName))
                IsEnabledRoleName = false;

            if (ControlMode == CtrlMode.EDIT)
            {
                if (!string.IsNullOrEmpty(TreeSelectedRole.DisplayRoleName) && TreeSelectedRole.DisplayRoleName.Length > 18)
                    TreeSelectedRole.DisplayRoleName = TreeSelectedRole.DisplayRoleName.Substring(0, 18);

                return;
            }

            //check role to set the mode from view to edit
            if (_TreeSelectedRole != null && _TreeSelectedRole.IsRoleChanged())
                ControlMode = CtrlMode.EDIT;
        }

        public bool OnAutoLogoutTimeChecked(object sender)
        {
            ControlMode = CtrlMode.EDIT;
            return ((CheckBox)(sender)).IsChecked.Value;
        }

        private bool SaveChanged()
        {
            if (String.IsNullOrWhiteSpace(TreeSelectedRole.DisplayRoleName))
            {
                DialogBox.ShowWarning("{0} cannot be empty.", "Role name");
                //TreeSelectedRole.DisplayRoleName = "No Name";
                return false;
            }

            if (IsRoleExists(TreeSelectedRole))
            {
                DialogBox.ShowWarning("{0} already exists.", "Role");
                return false;
            }

            foreach (MenuInfo menu in TreeSelectedRole.MenuCollection)
            {
                menu.IndexPermission = menu.DisplayIndexPermission + 1;
            }
            TreeSelectedRole.RoleName = TreeSelectedRole.DisplayRoleName;
            TreeSelectedRole.IsAutoLogout = TreeSelectedRole.DisplayIsAutoLogout;
            TreeSelectedRole.AutoLogoutTime = TreeSelectedRole.DisplayAutoLogoutTime;
            TreeSelectedRole.RoleNameTextSaved = TreeSelectedRole.TimeTextSaved = true;
            TreeSelectedRole.Description = TreeSelectedRole.DisplayDescription;

            try
            {
                RoleManager.SaveRole(TreeSelectedRole);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
            return true;
        }

        private Boolean IsRoleExists(RoleItem role)
        {
            if (RoleList == null || RoleList.Count == 0)
                return false;

            var sameNameList = RoleList.Where(t => t.DisplayRoleName == role.DisplayRoleName);
            if (sameNameList == null || sameNameList.Count() <= 1)
                return false;

            return true;
        }

        private void LoadRoleList()
        {
            _RolesList.Clear();
            List<RoleItem> roles = RoleManager.GetAllRoles();
            if (roles == null || roles.Count == 0)
                return;

            foreach (RoleItem r in roles)
            {
                RoleItem treeRole = RoleManager.CloneRole(r);
                if (treeRole != null)
                {
                    _RolesList.Add(treeRole);
                }
            }
            TreeSelectedRole = _RolesList.FirstOrDefault();
            TreeSelectedRole.IsSelected = true;
            ControlMode = CtrlMode.VIEW;
        }

        private void OnRoleTreeSelectedChanged(EventCommandParameter<object, RoutedEventArgs> arg)
        {
            RoleItem roleItem = arg.CustomParameter as RoleItem;
            if (roleItem == null)
                return;
            TreeSelectedRole = roleItem;
        }
        private void OnBtnAddRoleCommand(Object arg)
        {
            RoleItem newRole = RoleManager.CreateRole();
            if (newRole != null)
            {
                _RolesList.Add(newRole);
                TreeSelectedRole = newRole;
                TreeSelectedRole.IsSelected = true;
            }
            ControlMode = CtrlMode.EDIT;
        }
        private void OnBtnDeleteRoleCommand(Object arg)
        {
            if (TreeSelectedRole == null) return;

            if (!DialogBox.Confirm("Are you sure that you want to delete this role?"))
            {
                return;
            }

            if (BaseApp.Instance.UserContext.RoleID == TreeSelectedRole.RoleID)
            {
                DialogBox.ShowWarning("The action cannot be completed because {0} is currently in use.", "the role");
                return;
            }

            try
            {
                int index = _RolesList.IndexOf(TreeSelectedRole);
                _RolesList.Remove(TreeSelectedRole);
                RoleManager.DeleteRole(TreeSelectedRole.RoleID);
                index = index > 1 ? index - 1 : 0;
                TreeSelectedRole = _RolesList == null ? null : _RolesList[index];
                TreeSelectedRole.IsSelected = true;
                DialogBox.ShowInfo("Operated successfully.");
            }
            catch (Exception ex)
            {
                LOG.Error(ex.StackTrace);
                DialogBox.ShowInfo("Operation failed.");
            }
        }
        private void OnBtnCloneRoleCommand(Object arg)
        {
            if (_TreeSelectedRole != null)
            {
                RoleItem newRole = RoleManager.CreateRole(_TreeSelectedRole);
                if (newRole != null)
                {
                    newRole.DisplayRoleName = newRole.RoleName = "Copy of " + newRole.DisplayRoleName;
                    _RolesList.Add(newRole);
                    TreeSelectedRole = newRole;
                    TreeSelectedRole.IsSelected = true;
                    ControlMode = CtrlMode.EDIT;
                }
            }
        }
        private void OnBtnSaveCommand(Object arg)
        {
            try
            {
                if (SaveChanged())
                {
                    ControlMode = CtrlMode.VIEW;
                    DialogBox.ShowInfo("Operated successfully.");
                }
                else
                    DialogBox.ShowInfo("Operation failed.");
            }
            catch (Exception ex)
            {
                LOG.Error(ex.StackTrace);
                DialogBox.ShowInfo("Operation failed.");
            }
        }

        private void OnBtnCancelRoleCommand(Object arg)
        {
            LoadRoleList();
            ControlMode = CtrlMode.VIEW;
        }

        #region commands
        private ICommand _RoleTreeSelectChangedCmd;
        public ICommand RoleTreeSelectChangedCommand
        {
            get
            {
                if (this._RoleTreeSelectChangedCmd == null)
                    this._RoleTreeSelectChangedCmd = new BaseCommand<EventCommandParameter<object, RoutedEventArgs>>((EventCommandParameter<object, RoutedEventArgs> arg) => this.OnRoleTreeSelectedChanged(arg));
                return this._RoleTreeSelectChangedCmd;
            }
        }

        private ICommand _BtnSaveCommand;
        public ICommand btnSaveCommand
        {
            get
            {
                if (this._BtnSaveCommand == null)
                    this._BtnSaveCommand = new BaseCommand<Object>((Object arg) => this.OnBtnSaveCommand(arg));
                return this._BtnSaveCommand;
            }
        }

        private ICommand _BtnAddRoleCommand;
        public ICommand btnAddRoleCommand
        {
            get
            {
                if (this._BtnAddRoleCommand == null)
                    this._BtnAddRoleCommand = new BaseCommand<Object>((Object arg) => this.OnBtnAddRoleCommand(arg));
                return this._BtnAddRoleCommand;
            }
        }

        private ICommand _BtnDeleteRoleCommand;
        public ICommand btnDeleteRoleCommand
        {
            get
            {
                if (this._BtnDeleteRoleCommand == null)
                    this._BtnDeleteRoleCommand = new BaseCommand<Object>((Object arg) => this.OnBtnDeleteRoleCommand(arg));
                return this._BtnDeleteRoleCommand;
            }
        }

        private ICommand _BtnCloneRoleCommand;
        public ICommand btnCloneRoleCommand
        {
            get
            {
                if (this._BtnCloneRoleCommand == null)
                    this._BtnCloneRoleCommand = new BaseCommand<Object>((Object arg) => this.OnBtnCloneRoleCommand(arg));
                return this._BtnCloneRoleCommand;
            }
        }
        private ICommand _BtnCancelRoleCommand;
        public ICommand BtnCancelRoleCommand
        {
            get
            {
                if (this._BtnCancelRoleCommand == null)
                    this._BtnCancelRoleCommand = new BaseCommand<Object>((Object arg) => this.OnBtnCancelRoleCommand(arg));
                return this._BtnCancelRoleCommand;
            }
        }
        #endregion

        public RoleManager RoleManager
        {
            get { return RoleManager.Instance; }
        }

        public ObservableCollection<PermissionType> PermissionDictionary
        {
            get { return RolePermissionMapper.Instance.PermissionDictionary; }
        }

        private ObservableCollection<RoleItem> _RolesList = new ObservableCollection<RoleItem>();
        public ObservableCollection<RoleItem> RoleList
        {
            get { return _RolesList; }
        }

        private RoleItem _TreeSelectedRole = null;
        public RoleItem TreeSelectedRole
        {
            get { return _TreeSelectedRole; }
            set
            {
                _TreeSelectedRole = value;
                this.NotifyOfPropertyChange("TreeSelectedRole");
            }
        }

        private CtrlMode _ControlMode = CtrlMode.VIEW;
        public CtrlMode ControlMode
        {
            get { return _ControlMode; }
            set { _ControlMode = value; NotifyOfPropertyChange("ControlMode"); }
        }
    }
}
