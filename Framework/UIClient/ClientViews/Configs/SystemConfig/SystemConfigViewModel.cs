using System;
using System.Collections.Generic;
using Aitex.Core.Util;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.Command;

namespace MECF.Framework.UI.Client.CenterViews.Configs.SystemConfig
{
    public class SystemConfigViewModel : ModuleUiViewModelBase
    {
        #region Properties
        [Subscription("System.IsOnline")]
        public bool IsOnlineSystem { get; set; }

        [Subscription("TM.IsOnline")]
        public bool IsOnlineTM { get; set; }
        [Subscription("Aligner.IsOnline")]
        public bool IsAlignerOnline { get; set; }

        [Subscription("Cooler.IsOnline")]
        public bool IsCoolerOnline { get; set; }

        [Subscription("VCEA.IsOnline")]
        public bool IsOnlineVCEA { get; set; }
        [Subscription("VCEB.IsOnline")]
        public bool IsOnlineVCEB { get; set; }
        [Subscription("PM1.IsOnline")]
        public bool IsOnlinePM1 { get; set; }
        [Subscription("PM2.IsOnline")]
        public bool IsOnlinePM2 { get; set; }
        [Subscription("PM3.IsOnline")]
        public bool IsOnlinePM3 { get; set; }
        [Subscription("PM4.IsOnline")]
        public bool IsOnlinePM4 { get; set; }
        [Subscription("PM5.IsOnline")]
        public bool IsOnlinePM5 { get; set; }
        [Subscription("PM6.IsOnline")]
        public bool IsOnlinePM6 { get; set; }
        [Subscription("PM7.IsOnline")]
        public bool IsOnlinePM7 { get; set; }
        public bool IsPermission { get => this.Permission == 3; }

        private List<ConfigNode> _ConfigNodes = new List<ConfigNode>();
        public List<ConfigNode> ConfigNodes
        {
            get { return _ConfigNodes; }
            set { _ConfigNodes = value; NotifyOfPropertyChange("ConfigNodes"); }
        }

        private List<ConfigItem> _configItems = null;
        public List<ConfigItem> ConfigItems
        {
            get { return _configItems; }
            set { _configItems = value; NotifyOfPropertyChange("ConfigItems"); }
        }

        string _CurrentNodeName = string.Empty;

        public BaseCommand<ConfigNode> TreeViewSelectedItemChangedCmd { private set; get; }


        private string _currentCriteria = String.Empty;
        public string CurrentCriteria
        {
            get { return _currentCriteria; }
            set
            {
                if (value == _currentCriteria)
                    return;

                _currentCriteria = value;
                NotifyOfPropertyChange("CurrentCriteria");
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            foreach (var node in ConfigNodes)
                node.ApplyCriteria(CurrentCriteria, new Stack<ConfigNode>());
        }
        #endregion

        #region Functions
        public SystemConfigViewModel()
        {
            this.DisplayName = "System Config";
            TreeViewSelectedItemChangedCmd = new BaseCommand<ConfigNode>(TreeViewSelectedItemChanged);
        }
        private string SystemName="System";
        protected override void OnInitialize()
        {
            base.OnInitialize();

            ConfigNodes = SystemConfigProvider.Instance.GetConfigTree(SystemName).SubNodes;
            
        }

        private void TreeViewSelectedItemChanged(ConfigNode node)
        {
           
            _CurrentNodeName = string.IsNullOrEmpty(node.Path) ? node.Name : $"{node.Path}.{node.Name}";
            ConfigItems = node.Items;

            GetDataOfConfigItems();
        }

        private void GetDataOfConfigItems()
        {
            if (ConfigItems == null)
                return;

            for (int i = 0; i < ConfigItems.Count; i++)
            {
                string key = String.Format("{0}{1}{2}", _CurrentNodeName, ".", ConfigItems[i].Name);
                ConfigItems[i].CurrentValue = SystemConfigProvider.Instance.GetValueByName(SystemName, key);
                if ((IsOnlineSystem&& _CurrentNodeName.Contains("System"))||(IsOnlineTM && _CurrentNodeName.Contains("TM")) || (IsAlignerOnline && _CurrentNodeName.Contains("Aligner")) || ((IsOnlineVCEA||IsOnlineVCEB) && _CurrentNodeName.Contains("VCE")) || (IsCoolerOnline && _CurrentNodeName.Contains("Cooler")) || (IsOnlinePM1 && _CurrentNodeName.Contains("PM1")) || (IsOnlinePM2 && _CurrentNodeName.Contains("PM2")) || (IsOnlinePM3 && _CurrentNodeName.Contains("PM3")) || (IsOnlinePM4 && _CurrentNodeName.Contains("PM4")) || (IsOnlinePM5 && _CurrentNodeName.Contains("PM5")) || (IsOnlinePM6 && _CurrentNodeName.Contains("PM6")) || (IsOnlinePM7 && _CurrentNodeName.Contains("PM7")))
                {
                    ConfigItems[i].Enable = false;
                }else
                {
                    ConfigItems[i].Enable = true;
                }
                if (ConfigItems[i].Type == DataType.Bool)
                {
                    bool value;
                    if (bool.TryParse(ConfigItems[i].CurrentValue, out value))
                    {
                        ConfigItems[i].BoolValue = value;

                        ConfigItems[i].CurrentValue = value ? "Yes" : "No";
                    }
                }
                else
                    ConfigItems[i].StringValue = ConfigItems[i].CurrentValue;
            }
        }

        public void SetValue(ConfigItem item)
        {
            //key ：System.IsSimulatorMode 
            //value: true or false 都是字符串

            //input check

            string Des = string.Empty;
            if (!string.IsNullOrEmpty(_CurrentNodeName))
            {
                string[] str = _CurrentNodeName.Split('.');
                Des = str[0];
            }
            if (Des == "System")
            {
                if (!DialogBox.Confirm($"Are you sure you want to modify this value?\r\n\r\n value = {(item.Type == DataType.Bool ? item.BoolValue.ToString().ToLower() : item.StringValue)}"))
                    return;
            }

            string value;
            if (item.Type == DataType.Bool)
            {
                value = item.BoolValue.ToString().ToLower();
            }
            else
            {
                if (item.TextSaved && item.Tag!="ReadOnlySelection")
                    return;

                if (item.Type == DataType.Int)
                {
                    int iValue;
                    if (int.TryParse(item.StringValue, out iValue))
                    {
                        if (!double.IsNaN(item.Max) && !double.IsNaN(item.Min))
                        {
                            if (iValue > item.Max || iValue < item.Min)
                            {
                                DialogBox.ShowWarning(string.Format("The value should be between {0}  and {1}.", ((int)item.Min).ToString(), ((int)item.Max).ToString()));
                                return;
                            }
                        }
                    }
                    else
                    {
                        DialogBox.ShowWarning("Please input valid data.");
                        return;
                    }
                    value = item.StringValue;
                }
                else if (item.Type == DataType.Double)
                {
                    double fValue;
                    if (double.TryParse(item.StringValue, out fValue))
                    {
                        if (!double.IsNaN(item.Max) && !double.IsNaN(item.Min))
                        {
                            if (fValue > item.Max || fValue < item.Min)
                            {
                                DialogBox.ShowWarning(string.Format("The value should be between {0}  and {1}.", item.Min.ToString(), item.Max.ToString()));
                                return;
                            }

                            string[] box = fValue.ToString().Split('.');
                            if (box.Length > 1 && box[1].Length > 6)
                            {
                                DialogBox.ShowWarning(string.Format("The value should be more than six decimal places"));
                                return;
                            }
                        }
                    }
                    else
                    {
                        DialogBox.ShowWarning("Please input valid data.");
                        return;
                    }
                    value = item.StringValue;
                }
                else
                    value = item.StringValue;
            }

            string key = String.Format("{0}{1}{2}", _CurrentNodeName, ".", item.Name);
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.SetConfig", key, value);

            item.TextSaved = true;

            Reload();
        }

        public void Reload()
        {
            GetDataOfConfigItems();
        }

        public void SaveAll()
        {
            if (ConfigItems == null)
                return;

            ConfigItems.ForEach(item => SetValue(item));
        }

        public void CollapseAll()
        {
             SetExpand(ConfigNodes, false) ;
        }

        public void ExpandAll()
        {
             SetExpand(ConfigNodes, true) ;
        }

        public void ClearFilter()
        {
            CurrentCriteria = "";
        }

        public void SetExpand(List<ConfigNode> configs, bool expand)
        {
            if (configs == null)
                return;

            foreach (var configNode in configs)
            {
                configNode.IsExpanded = expand;

                if (configNode.SubNodes != null)
                    SetExpand(configNode.SubNodes, expand);
            }
            
        }

        #endregion
    }
}
