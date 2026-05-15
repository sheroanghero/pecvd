using Aitex.Core.RT.Log;
using Caliburn.Micro;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.Editors;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using SciChart.Core.Extensions;

namespace MECF.Framework.UI.Client.CenterViews.Configs.DataConfig
{
    public class DataGroupItem : NotifiableItem
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; InvokePropertyChanged(nameof(Name)); }
        }

        private bool _isFix;
        public bool IsFix
        {
            get { return _isFix; }
            set { _isFix = value; InvokePropertyChanged(nameof(IsFix)); }
        }

        private bool _isVisible;
        public bool IsVisible
        {
            get { return _isVisible; }
            set { _isVisible = value; InvokePropertyChanged(nameof(IsVisible)); }
        }
    }

    public class DataConfigViewModel : ModuleUiViewModelBase, ISupportMultipleSystem
    {
        public class DataItem : NotifiableItem
        {
            private string _name;
            public string Name
            {
                get { return _name; }
                set { _name = value; InvokePropertyChanged(nameof(Name)); }
            }

            private bool _isSelected;
            public bool IsSelected
            {
                get { return _isSelected; }
                set { _isSelected = value; InvokePropertyChanged(nameof(IsSelected)); }
            }

            private bool _isChecked;
            public bool IsChecked
            {
                get { return _isChecked; }
                set { _isChecked = value; InvokePropertyChanged(nameof(IsChecked)); }
            }
        }

        #region Properties

        public ObservableCollection<DataGroupItem> GroupData { get; set; }

        private DataGroupItem _currentSelection;

        public DataGroupItem CurrentSelection
        {
            get { return _currentSelection; }
            set
            {
                _currentSelection = value;
                ChangeGroupSelection(_currentSelection);

                NotifyOfPropertyChange(nameof(CurrentSelection));
            }
        }

        private string _content;
        private XmlDocument _xmlContent;
 
        public ObservableCollection<DataItem> Unselections { get; set; }
        public ObservableCollection<DataItem> Selections { get; set; }

        public string NewGroupName { get; set; }

        #endregion

        #region Functions
        public DataConfigViewModel()
        {
            this.DisplayName = "Data Config";

            GroupData = new ObservableCollection<DataGroupItem>();

            Unselections = new ObservableCollection<DataItem>();
            Selections = new ObservableCollection<DataItem>();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            var lstItems = (List<string>)QueryDataClient.Instance.Service.GetConfig("System.NumericDataList");

            lstItems.Sort();

            var selection = new ObservableCollection<DataItem>();
            var unselection = new ObservableCollection<DataItem>();
            foreach (var item in lstItems)
            {
                unselection.Add(new DataItem() { Name = item, IsChecked = false, IsSelected = false });
                selection.Add(new DataItem() { Name = item, IsChecked = false, IsSelected = false });
            }

            Unselections = unselection;
            Selections = selection;

            NotifyOfPropertyChange(nameof(Unselections));
            NotifyOfPropertyChange(nameof(Selections));
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            UpdateData();
        }


        private void UpdateData()
        {
            string content = QueryDataClient.Instance.Service.GetTypedConfigContent("DataGroup", "");

            if (_content == content)
                return;
 
            GroupData.Clear();

            try
            {
                _xmlContent = new XmlDocument();
                _xmlContent.LoadXml(content);
                _content = content;

                var groups = _xmlContent.SelectNodes("DataGroupConfig/DataGroup");

                foreach (var item in groups)
                {
                    XmlElement element = item as XmlElement;

                    string strFix = element.GetAttribute("fix");
                    bool bFix = false;
                    if (!string.IsNullOrEmpty(strFix))
                    {
                        bool.TryParse(strFix, out bFix);
                    }

                    string strVisible = element.GetAttribute("visible");
                    bool bVisible = true;
                    if (!string.IsNullOrEmpty(strVisible))
                    {
                         bool.TryParse(strVisible, out bVisible)  ;
                    }

                    if (!bVisible)
                        continue;

                    string name = element.GetAttribute("name");

                    GroupData.Add(new DataGroupItem() { Name = name, IsFix = bFix});
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }



        public void NewGroup()
        {
            if (string.IsNullOrEmpty(NewGroupName))
            {
                DialogBox.ShowWarning($"Please input new data group name");
                return;
            }
            NewGroupName = NewGroupName.Trim();

            var nodeGroup = _xmlContent.SelectSingleNode($"DataGroupConfig/DataGroup[@name='{NewGroupName}']");

            if (nodeGroup != null)
            {
                DialogBox.ShowWarning($"{NewGroupName} Already exist");
                return;
            }

            var nodeRoot = _xmlContent.SelectSingleNode($"DataGroupConfig");

            var node = _xmlContent.CreateElement("DataGroup");
            node.SetAttribute("name", NewGroupName);
            nodeRoot.AppendChild(node);

            var item = new DataGroupItem()
            {
                IsFix = false,
                Name = NewGroupName
            };

            GroupData.Add(item);

            CurrentSelection = item;

            QueryDataClient.Instance.Service.SetTypedConfigContent("DataGroup", "", _xmlContent.InnerXml);
        }

        public void RenameGroup(DataGroupItem group)
        {
            InputFileNameDialogViewModel dialog = new InputFileNameDialogViewModel("Input New Config Name");
            dialog.FileName = group.Name;
            WindowManager wm = new WindowManager();
            bool? dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            string name = dialog.FileName.Trim();
            if (string.IsNullOrEmpty(name))
            {
                DialogBox.ShowWarning("Folder name should not be empty");
                return;
            }

            var nodeGroup = _xmlContent.SelectSingleNode($"DataGroupConfig/DataGroup[@name='{name}']");

            if (nodeGroup != null)
            {
                DialogBox.ShowWarning($"{name} Already exist");
                return;
            }

            nodeGroup = _xmlContent.SelectSingleNode($"DataGroupConfig/DataGroup[@name='{group.Name}']");

            (nodeGroup as XmlElement).SetAttribute("name", name);

            group.Name = name;

            QueryDataClient.Instance.Service.SetTypedConfigContent("DataGroup", "", _xmlContent.InnerXml);
        }


        public void DeleteGroup(DataGroupItem group)
        {
            if (!DialogBox.Confirm($"Are you sure you want to delete {group.Name}?"))
                return;

            var nodeGroup = _xmlContent.SelectSingleNode($"DataGroupConfig/DataGroup[@name='{group.Name}']");

            nodeGroup.ParentNode.RemoveChild(nodeGroup);

            QueryDataClient.Instance.Service.SetTypedConfigContent("DataGroup", "", _xmlContent.InnerXml);

            GroupData.RemoveWhere(x=>x.Name == group.Name);

            //CurrentSelection = null;
        }

        public void Select()
        {
            foreach (var unselection in Unselections)
            {
                if (unselection.IsChecked)
                {
                    unselection.IsSelected = false;
                    unselection.IsChecked = false;
                    foreach (var selection in Selections)
                    {
                        if (selection.Name == unselection.Name)
                        {
                            selection.IsChecked = false;
                            selection.IsSelected = true;
                            break;
                        }

                    }
                }
            }
        }

        public void Unselect()
        {
            foreach (var selection in Selections)
            {
                if (selection.IsChecked)
                {
                    selection.IsSelected = false;
                    selection.IsChecked = false;
                    foreach (var unselection in Unselections)
                    {
                        if (unselection.Name == selection.Name)
                        {
                            unselection.IsChecked = false;
                            unselection.IsSelected = true;
                            break;
                        }

                    }
                }
            }
        }

        public void SaveSelection()
        {
            var nodeGroup = _xmlContent.SelectSingleNode($"DataGroupConfig/DataGroup[@name='{CurrentSelection.Name}']");

            var nodeItem = _xmlContent.SelectNodes($"DataGroupConfig/DataGroup[@name='{CurrentSelection.Name}']/DataItem");
            foreach (var nodeGroupChildNode in nodeItem)
            {
                var node = nodeGroupChildNode as XmlElement;
                nodeGroup.RemoveChild(node);
            }

            foreach (var item in Selections)
            {
                if (item.IsSelected)
                {
                    var node = _xmlContent.CreateElement("DataItem");
                    node.SetAttribute("name", item.Name);
                    nodeGroup.AppendChild(node);
                }
            }

            QueryDataClient.Instance.Service.SetTypedConfigContent("DataGroup", "", _xmlContent.InnerXml);
        }

        public void CancelSelection()
        {
            ChangeGroupSelection(CurrentSelection);
        }

        protected void ChangeGroupSelection(DataGroupItem group)
        {
            if (group == null)
            {
                foreach (var unselection in Unselections)
                {
                    unselection.IsSelected = false;
                }

                foreach (var selection in Selections)
                {
                    selection.IsSelected = false;
                }
                return;
            }
            var items = _xmlContent.SelectNodes($"DataGroupConfig/DataGroup[@name='{group.Name}']/DataItem");

            List<string> names = new List<string>();
            foreach (var item in items)
            {
                var node = item as XmlElement;
                names.Add(node.GetAttribute("name"));
            }

            foreach (var unselection in Unselections)
            {
                unselection.IsSelected = !names.Contains(unselection.Name);
            }

            foreach (var selection in Selections)
            {
                selection.IsSelected = names.Contains(selection.Name);
            }
        }

        #endregion
    }
}
