using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using Aitex.Core.Account;
using Aitex.Core.RT.Log;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Util;

namespace MECF.Framework.UI.Core.Accounts
{
    public class AccountViewModel
    {

        public class AccountInfo
        {
            public Account Account { get; set; }
            public int No { get; set; }
            public string AccountId { get { return Account.AccountId; } }
            public bool IsEnabled { get { return Account.AccountStatus; } }
            public string RealName { get { return Account.RealName; } }
            public string Role { get { return Account.Role; } }
            public string Department { get { return Account.Department; } }
            public string LastLoginTime { get { return Account.LastLoginTime; } }
            public string Description { get { return Account.Description; } }
            public string Email { get { return Account.Email; } }
            public string Telephone { get { return Account.Telephone; } }
        }

        public List<AccountInfo> AccountList { get; private set; }

        /// <summary>
        /// 总共创建的账号数目
        /// </summary>
        public int TotalAccountNum
        {
            get
            {
                return AccountList.Count;
            }
        }

        /// <summary>
        /// 账号有效的用户数
        /// </summary>
        public int EnabledAccountNum
        {
            get
            {
                int num = 0;
                foreach (var item in AccountList)
                {
                    if (item.IsEnabled) num++;
                }
                return num;
            }
        }

        /// <summary>
        /// Construction 
        /// </summary>
        /// <param name="hideDisabledAccounts"></param>
        public AccountViewModel(bool hideDisabledAccounts)
        {
            AccountList = new List<AccountInfo>();
            var accounts = AccountClient.Instance.Service.GetAccountList();
            if (accounts == null) return;

            int num = 1;
            foreach (var account in accounts.AccountList)
            {
                if (!account.AccountStatus && hideDisabledAccounts) continue;
                AccountList.Add(new AccountInfo() { Account = account, No = num++ });
            }
        }
    }

    public class RolePermissionViewModel : INotifyPropertyChanged
    {
        #region Command
        public DelegateCommand<object> SavePermissionCommand { get; set; }

        #endregion

        
        public SerializableDictionary<string, SerializableDictionary<string, ViewPermission>> Roles
        {
            get;
            set;
        }
        ObservableCollection<string> roleNames = new ObservableCollection<string>();
        public ObservableCollection<string> RoleNames
        {
            get
            {
                return roleNames;
            }
            set
            {
                roleNames = value;
            }
        }

        public RolePermissionViewModel()
        {
            //cs 设置
            loadPermissionFile();
            Roles = AccountClient.Instance.Service.GetAllRolesPermission();
            RoleNames.Clear();
            foreach (string rolename in Roles.Keys)
            {
                RoleNames.Add(rolename);
            }
        }
        public void SelectRoleChanged(string role)
        {
            CurrentRoleName = role;
            BindAll();
        }


        public void InitialRolePermissionVM()
        {

            SavePermissionCommand = new DelegateCommand<object>(
              param =>
              {
                  try
                  {
                      //_xmlRecipeFormat.Save(recipePermissionFile);
                      bool suc = AccountClient.Instance.Service.SaveProcessViewPermission(_xmlRecipeFormat.InnerXml);
                      if (suc)
                          MessageBox.Show("保存菜单查看权限成功");
                      //Publisher.Notify(Subject.SendSaveSuccessMessage, "保存菜单查看权限成功");
                  }
                  catch (Exception ex)
                  {
                      LOG.Write(ex);
                      MessageBox.Show("保存菜单查看权限失败!");
                      //Publisher.Notify(Subject.SendWarningMessage, "保存菜单查看权限失败!");

                  }
              },
              param => { if (string.IsNullOrEmpty(CurrentRoleName))return false; return true; });


        }

        public CheckTreeViewModel ChamberAViewModel { get; set; }
        //public CheckTreeViewModel ChamberBViewModel { get; set; }
        //public CheckTreeViewModel ChamberCViewModel { get; set; }
        //public CheckTreeViewModel ChamberDViewModel { get; set; }

        public string CurrentRoleName { get; set; }

        #region UI Logical

        public

        void loadPermissionFile()
        {
            ChamberAViewModel = new CheckTreeViewModel();
 
        }

 
        public static XmlDocument _xmlRecipeFormat = new XmlDocument();

        public void BindAll()
        {
 
        }
 
        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string proName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(proName));
        }

        #endregion
    }

    public class CheckTreeViewModel : INotifyPropertyChanged
    {
        public NodeViewModel Nodes { get; private set; }
        public NodeInfo NodeInfo { get; private set; }


        public CheckTreeViewModel()
        {
            NodeInfo = new NodeInfo();
            NodeInfo.SelectedNodeChanged += (s, e) => RefreshCommands();
            Nodes = new NodeViewModel(NodeInfo);
        }



        NodeViewModel GetOperationNode()
        {
            if (NodeInfo.SelectedNode == null)
                return Nodes;
            return NodeInfo.SelectedNode;
        }

        bool CheckSelection(object obj)
        {
            return NodeInfo.SelectedNode != null;
        }

        void RefreshCommands()
        {
 
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string proName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(proName));
        }

        #endregion

    }

    public class NodeInfo : INotifyPropertyChanged
    {
        NodeViewModel selectedNode;
        int count;

        public NodeViewModel SelectedNode
        {
            get { return selectedNode; }
            set
            {
                if (selectedNode != value)
                {
                    selectedNode = value;
                    OnSelectedNodeChanged();
                    OnPropertyChanged("SelectedNode");
                }
            }
        }

        public int Count
        {
            get { return count; }
            private set
            {
                if (count != value)
                {
                    count = value;
                    OnPropertyChanged("Count");
                }
            }
        }

        internal void SetCount(int newcount)
        {
            Count = newcount;
        }

        public event EventHandler SelectedNodeChanged;

        protected virtual void OnSelectedNodeChanged()
        {
            if (SelectedNodeChanged != null)
                SelectedNodeChanged(this, EventArgs.Empty);
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string proName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(proName));
        }

        #endregion
    }

    public class NodeViewModel : INotifyPropertyChanged
    {
        /*
         * 省去构造函数，公共方法和一些静态成员
         * */

        const string DataName = "数据 ";

        public XmlNode NodeXmlData { get; set; }
        public string CurrentRoleName { get; set; }
        static int DataCounter = 1;
        public static string GetNextDataName()
        {
            return String.Concat(DataName, DataCounter++);
        }

        public NodeViewModel(NodeInfo info)
        {
            children = new ObservableCollection<NodeViewModel>();
            NodeInfo = info;
            CheckBoxCommand = new DelegateCommand<object>((p) => checkBoxSelect(p), param => { return true; });
        }
        private NodeViewModel(NodeViewModel parent, string name, string role)
            : this(parent, name, false, null, role)
        {
        }
        private NodeViewModel(NodeViewModel parent, string name, bool isSelected, XmlNode node, string role)
        {
            Parent = parent;
            Name = name;
            NodeInfo = parent.NodeInfo;
            IsSelected = isSelected;
            children = new ObservableCollection<NodeViewModel>();
            SelectedNode = node;
            CheckBoxCommand = new DelegateCommand<object>((p) => checkBoxSelect(p), param =>
            {
                return true;
            });
            CurrentRoleName = role;
        }

        public XmlNode SelectedNode = null;
        void checkBoxSelect(object p)
        {
            if (SelectedNode != null)
            {
                CheckBox cb = p as CheckBox;

                if (null != SelectedNode.Attributes["DenyRole"])
                {
                    string denyroles = "," + SelectedNode.Attributes["DenyRole"].Value + ",";
                    string tagrole = "," + CurrentRoleName + ",";
                    if (cb.IsChecked.Value)
                    {
                        denyroles = denyroles.Replace(tagrole, ",");
                        SelectedNode.Attributes["DenyRole"].Value = denyroles.Trim(","[0]);
                    }
                    else
                    {
                        //没权限
                        if (denyroles.IndexOf(tagrole) < 0)
                        {
                            //本来有权限，现在要没权限
                            SelectedNode.Attributes["DenyRole"].Value = (denyroles + CurrentRoleName).Trim(","[0]);
                        }
                    }
                }
                else
                {
                    if (cb.IsChecked.Value)
                    {

                    }
                    else
                    {
                        XmlAttribute xmlAttribute = RolePermissionViewModel._xmlRecipeFormat.CreateAttribute("DenyRole");
                        xmlAttribute.Value = CurrentRoleName;
                        SelectedNode.Attributes.Append(xmlAttribute);
                    }
                }

                //SelectedNode.Attributes["DenyRole"].Value = cb.IsChecked + "";
            }
        }
        #region 字段

        string name;
        bool isExpanded;
        bool isSelected;
        ObservableCollection<NodeViewModel> children;

        #endregion


        #region 属性
        public DelegateCommand<object> CheckBoxCommand { get; private set; }

        public NodeInfo NodeInfo { get; private set; }
        public NodeViewModel Parent { get; private set; }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public bool IsExpanded
        {
            get
            {
                return isExpanded;
            }
            set
            {
                if (isExpanded != value)
                {
                    isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                    OnIsExpandedChanged();
                }
            }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged("IsSelected");
                    OnIsSelectedChanged();
                }
            }
        }

        public ReadOnlyObservableCollection<NodeViewModel> Children
        {
            get
            {
                return new ReadOnlyObservableCollection<NodeViewModel>(children);
            }
        }


        #endregion


        #region 方法

        public void Remove()
        {
            //if (Parent != null)
            //{
            //    Parent.children.Remove(this);
            //    RefreshInfoCount(-1);
            //}
            children.Clear();
            RefreshInfoCount(-1);
        }
        public NodeViewModel Add(string name, XmlNode node, string role)
        {
            return Add(name, false, node, role);
        }
        public NodeViewModel Add(string name, bool ischecked, XmlNode node, string role)
        {
            //SelectedNode = node;
            IsExpanded = true;
            NodeViewModel nodeModel = new NodeViewModel(this, name, ischecked, node, role);
            children.Insert(0, nodeModel);
            RefreshInfoCount(1);
            return nodeModel;
        }
        public void Append(string name, string role)
        {
            IsExpanded = true;
            children.Add(new NodeViewModel(this, name, role));
            RefreshInfoCount(1);
        }
        public void Rename()
        {
            Name = GetNextDataName();
        }

        public void MoveUp()
        {
            if (Parent != null)
            {
                var idx = Parent.children.IndexOf(this);
                if (idx > 0)
                {
                    Parent.children.RemoveAt(idx);
                    Parent.children.Insert(--idx, this);
                }
                IsSelected = true;
            }
        }
        public void MoveDown()
        {
            if (Parent != null)
            {
                var idx = Parent.children.IndexOf(this);
                if (idx < Parent.children.Count - 1)
                {
                    Parent.children.RemoveAt(idx);
                    Parent.children.Insert(++idx, this);
                }
                IsSelected = true;
            }
        }

        #endregion

        #region 私有方法

        void RefreshInfoCount(int addition)
        {
            if (NodeInfo != null)
                NodeInfo.SetCount(NodeInfo.Count + addition);
        }

        #endregion

        #region 事件

        public event EventHandler IsExpandedChanged;
        public event EventHandler IsSelectedChanged;

        protected virtual void OnIsExpandedChanged()
        {
            if (IsExpandedChanged != null)
                IsExpandedChanged(this, EventArgs.Empty);
        }

        protected virtual void OnIsSelectedChanged()
        {
            if (IsSelectedChanged != null)
                IsSelectedChanged(this, EventArgs.Empty);
            if (IsSelected)
                NodeInfo.SelectedNode = this;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string proName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(proName));
        }

        #endregion
    }
}
