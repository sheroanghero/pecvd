using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Aitex.Core.RT.Event;
using Aitex.Core.UI.View.Common;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using Caliburn.Micro;
using Caliburn.Micro.Core;
using MECF.Framework.Common.Alarms;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Alarms.Alarm
{
 
    public class AlarmtableParameterViewModel : UiViewModelBase 
    {
        public ObservableCollection<AlarmParameterOperation> AlarmParameterOperations { get; set; } = new ObservableCollection<AlarmParameterOperation>();

        public ObservableCollection<string> AlarmGroups { get; set; } = new ObservableCollection<string>();


        public ObservableCollection<AlarmTabItem> TableList { get; set; } = new ObservableCollection<AlarmTabItem>();
        private Dictionary<string, ObservableCollection<AlarmParameterOperation>> _tempAlarmParameterOperationDic = new Dictionary<string, ObservableCollection<AlarmParameterOperation>>();

        private int _alarmGroupsWidth = 150;

        public string SelectedAlarmGroup { get; set; }

        public int AlarmGroupsWidth
        {
            get => _alarmGroupsWidth;
            set
            {
                _alarmGroupsWidth = value;
                NotifyOfPropertyChange("AlarmGroupsWidth");
            }
        }

        private int _alarmGridSelectIndex = -1;
        public int AlarmGridSelectIndex
        {
            get => _alarmGridSelectIndex;
            set
            {
                _alarmGridSelectIndex = value;
                NotifyOfPropertyChange(nameof(AlarmGridSelectIndex));
            }
        }

        private List<string> _groupNames = new List<string>
           (new string[] {
            "None Specified",
            "ALG01",
            "ALG02",
            "ALG03",
            "ALG04",
            "ALG05",
            "ALG06",
            "ALG07",
            "ALG08",
            "ALG09",
            "ALG10"
           });

        private Dictionary<string, Dictionary<string, EventItem>> _allGroupAlarmDic;

        public List<string> GroupNames
        {
            get => _groupNames;
            set

            {
                _groupNames = value;
                NotifyOfPropertyChange("GroupNames");
            }
        }

        public int SelectedAlarmTableIndex = 0;

        public string EntryValue;

        public Visibility _alarmTableVisibility = Visibility.Collapsed;

        public Visibility AlarmTableVisibility
        {
            get => _alarmTableVisibility;
            set
            {
                _alarmTableVisibility = value;
                NotifyOfPropertyChange("AlarmTableVisibility");
            }
        }

        public Visibility _alarmGroupsVisibility = Visibility.Collapsed;

        public Visibility AlarmGroupsVisibility
        {
            get => _alarmGroupsVisibility;
            set
            {
                _alarmGroupsVisibility = value;
                if (_alarmGroupsVisibility == Visibility.Visible)
                { AlarmGroupsWidth = 150; }
                else
                {
                    AlarmGroupsWidth = 0;
                }
                NotifyOfPropertyChange("AlarmGroupsVisibility");
            }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
        }
        protected override void OnActivate()
        {
            base.OnActivate();
            _allGroupAlarmDic = AlarmClient.Instance.Service.GetAlarmDefineTemplate();

            SetVisibility();
        }

        private void SetVisibility()
        {
            if (_allGroupAlarmDic != null)
            {
                if (_allGroupAlarmDic.Count < 2)
                {
                    AlarmTableVisibility = Visibility.Collapsed;
                    if (_allGroupAlarmDic.Count != 0)
                    {
                        AlarmParameterOperations.Clear();

                        foreach (var item in _allGroupAlarmDic.Keys)
                        {
                            TableList.Add(new AlarmTabItem() { TabName = item, IsCheck = true });
                        }
                        foreach (var subitem in _allGroupAlarmDic[_allGroupAlarmDic.Keys.FirstOrDefault()].Keys)
                        {
                            AlarmParameterOperations.Add(new AlarmParameterOperation(subitem, _allGroupAlarmDic[_allGroupAlarmDic.Keys.FirstOrDefault()][subitem]));
                        }
                    }
                }
                else
                {
                    AlarmTableVisibility = Visibility.Visible;
                    int icount = 0;
                    TableList.Clear();
                    foreach (var item in _allGroupAlarmDic.Keys)
                    {
                        if (icount == 0)
                        {
                            TableList.Add(new AlarmTabItem() { TabName = item, IsCheck = true });
                        }
                        else
                        {
                            TableList.Add(new AlarmTabItem() { TabName = item, IsCheck = false });
                        }
                        icount++;
                    }
                    AlarmParameterOperations.Clear();
                    foreach (var subitem in _allGroupAlarmDic[_allGroupAlarmDic.Keys.FirstOrDefault()].Keys)
                    {
                        AlarmParameterOperations.Add(new AlarmParameterOperation(subitem, _allGroupAlarmDic[_allGroupAlarmDic.Keys.FirstOrDefault()][subitem]));
                    }
                }

                SetAlarmGroup(_allGroupAlarmDic.Keys.FirstOrDefault());
            }
            else
            {
                AlarmTableVisibility = Visibility.Hidden;
            }

        }
        public void AlarmTableSelected(string index)
        {
            int tempValue = int.Parse(index);
            SelectedAlarmTableIndex = tempValue;
            AlarmParameterOperations.Clear();
            SetAlarmGroup(index);
            //TableAlarmParameterOperations[tempValue].ToList().ForEach(p => AlarmParameterOperations.Add(p));
        }

        private void SetAlarmGroup(string index)
        {
            AlarmGroups.Clear();
            var categoryList = _allGroupAlarmDic[index].Values.Select(x => x.Category);
            foreach (var item in categoryList)
            {
                if (!AlarmGroups.Contains(item))
                {
                    AlarmGroups.Add(item);
                }
            }
            if (AlarmGroups.Count < 2)
            {
                AlarmGroupsVisibility = Visibility.Collapsed;
                AlarmParameterOperations.Clear();
                foreach (var subitem in _allGroupAlarmDic[_allGroupAlarmDic.Keys.FirstOrDefault()].Keys)
                {
                    AlarmParameterOperations.Add(new AlarmParameterOperation(subitem, _allGroupAlarmDic[_allGroupAlarmDic.Keys.FirstOrDefault()][subitem]));
                }
            }
            else
            {
                AlarmGroupsVisibility = Visibility.Visible;
                AlarmParameterOperations.Clear();
                foreach (var subitem in _allGroupAlarmDic[index].Keys)
                {
                    AlarmParameterOperations.Add(new AlarmParameterOperation(subitem, _allGroupAlarmDic[index][subitem]));
                }
            }

            if (SelectedAlarmTableIndex < TableList.Count)
            {
                var tableName = TableList[SelectedAlarmTableIndex].TabName;
                _tempAlarmParameterOperationDic[tableName] = new ObservableCollection<AlarmParameterOperation>(AlarmParameterOperations);
            }
        }

        public void AlarmTableSave()
        {
            if (_tempAlarmParameterOperationDic.Count > 0)
            {
                foreach (var table in _tempAlarmParameterOperationDic.Keys)
                {
                    foreach (var item in _tempAlarmParameterOperationDic[table])
                    {
                        if (_allGroupAlarmDic.ContainsKey(table) && _allGroupAlarmDic[table].ContainsKey(item.AlarmName))
                        {
                            _allGroupAlarmDic[table][item.AlarmName].Description = item.Description;
                            _allGroupAlarmDic[table][item.AlarmName].Solution = item.Solution;
                            _allGroupAlarmDic[table][item.AlarmName].Explaination = item.Explaination;
                            _allGroupAlarmDic[table][item.AlarmName].Bypass = item.Bypass;
                            _allGroupAlarmDic[table][item.AlarmName].Group = item.Group.Contains("None Specified") ? EventGroup.Group0 : (EventGroup)Enum.ToObject(typeof(EventGroup), item.SelectedIndex);
                            _allGroupAlarmDic[table][item.AlarmName].Editable = item.IsEdit;
                            _allGroupAlarmDic[table][item.AlarmName].Level = item.Level;
                            _allGroupAlarmDic[table][item.AlarmName].DisplayName = item.DisplayName;
                            _allGroupAlarmDic[table][item.AlarmName].AutoRecovery = item.AutoRecovery;
                        }
                    }
                }
            }
            InvokeClient.Instance.Service.DoOperation($"System.UpdateAlarmTable", _allGroupAlarmDic);
        }


        public void AlarmTableEdit()
        {
            WindowManager wm = IoC.Get<IWindowManager>() as WindowManager;
            if (AlarmGridSelectIndex != -1)
            {
                if (AlarmParameterOperations[AlarmGridSelectIndex].IsEdit)
                {
                    AlarmParameterEditViewModel itemsSelectDialogView = new AlarmParameterEditViewModel();
                    itemsSelectDialogView.AlarmParameterOperationItem = AlarmParameterOperations[AlarmGridSelectIndex];
                    itemsSelectDialogView.AllGroupAlarmDic = _allGroupAlarmDic;
                    itemsSelectDialogView.AlrmTabName = TableList[SelectedAlarmTableIndex].TabName;
                    if ((bool)wm.ShowDialogWithNoWindow(itemsSelectDialogView))
                    {
                        _allGroupAlarmDic = itemsSelectDialogView.AllGroupAlarmDic;
                    }
                }
            }
        }

        /// <summary>
        /// 按照Id号或者Alarm文本查找
        /// </summary>
        /// <param name="alarmId"></param>
        /// <param name="alarmText"></param>
        public void AlarmValueFind(string alarmId, string alarmText)
        {
            if (!string.IsNullOrEmpty(alarmId.Trim()) || !string.IsNullOrEmpty(alarmText.Trim()))
            {
                bool bHaveValue = false;
                AlarmParameterOperations.Clear();
                foreach (string subitem in _allGroupAlarmDic[SelectedAlarmTableIndex.ToString()].Keys)
                {
                    var alarmOperation = _allGroupAlarmDic[SelectedAlarmTableIndex.ToString()][subitem];
                    if (alarmOperation.Id.ToString() == alarmId || (alarmText != "" && alarmOperation.Description.ToLower().Contains(alarmText.ToLower())))
                    {
                        //if (!bHaveValue)
                        //{
                        //    bHaveValue = true;
                        //    AlarmParameterOperations.Clear();
                        //    AlarmParameterOperations.Add(new AlarmParameterOperation(subitem, _allGroupAlarmDic[SelectedAlarmTableIndex.ToString()][subitem]));
                        //}
                        //else
                        //{
                        //    AlarmParameterOperations.Add(new AlarmParameterOperation(subitem, _allGroupAlarmDic[SelectedAlarmTableIndex.ToString()][subitem]));
                        //}
                        AlarmParameterOperations.Add(new AlarmParameterOperation(subitem, _allGroupAlarmDic[SelectedAlarmTableIndex.ToString()][subitem]));
                    }
                }
            }
            else
            {
                AlarmParameterOperations.Clear();
                foreach (string subitem in _allGroupAlarmDic[SelectedAlarmTableIndex.ToString()].Keys)
                {
                    AlarmParameterOperations.Add(new AlarmParameterOperation(subitem, _allGroupAlarmDic[SelectedAlarmTableIndex.ToString()][subitem]));
                }
                //MessageBox.Show("Please input condition! ");
            }
        }
        private Dictionary<int, ObservableCollection<AlarmParameterOperation>> StrParameterToAlarmOperation(string parameterStr)
        {
            Dictionary<int, ObservableCollection<AlarmParameterOperation>> tempAlarmOperation = new Dictionary<int, ObservableCollection<AlarmParameterOperation>>();
            string[] tableParameters = parameterStr.Split('|');
            if (tableParameters.Length > 0)
            {
                foreach (var item in tableParameters)
                {
                    ObservableCollection<AlarmParameterOperation> alarmParameterOperations = new ObservableCollection<AlarmParameterOperation>();
                    var tabelPar = item.Split(':');
                    var alarmstrs = tabelPar[1].Split(';');
                    if (alarmstrs != null && alarmstrs.Length > 0)
                    {
                        foreach (var alarmitem in alarmstrs)
                        {
                            alarmParameterOperations.Add(new AlarmParameterOperation(alarmitem));
                        }
                    }
                    if (alarmParameterOperations.Count > 0)
                    {
                        tempAlarmOperation.Add(int.Parse(tabelPar[0]), alarmParameterOperations);
                    }
                }
            }
            return tempAlarmOperation;
        }

        public void AlarmTableAllOn()
        {
            EntryValue = "AllOn";
            GetAlarmParameterOperations();
        }

        public void AlarmTableAllOff()
        {
            EntryValue = "AllOff";
            GetAlarmParameterOperations();
        }

        public void AlarmTableCancel()
        {
            _tempAlarmParameterOperationDic.Clear();

            _allGroupAlarmDic = AlarmClient.Instance.Service.GetAlarmDefineTemplate();
            SetVisibility();
        }


        public void GetAlarmParameterOperations()
        {
            if (_allGroupAlarmDic == null || _allGroupAlarmDic.Count == 0) return;
            foreach (var item in _allGroupAlarmDic[SelectedAlarmTableIndex.ToString()])
            {
                if (AlarmGroups.Count < 2)
                {
                    if (EntryValue == "AllOn")
                        item.Value.Bypass = true;
                    else if (EntryValue == "AllOff")
                        item.Value.Bypass = false;
                }
                else
                {
                    if (SelectedAlarmGroup != null)
                    {
                        if (item.Value.Category == SelectedAlarmGroup)
                        {
                            if (EntryValue == "AllOn")
                                item.Value.Bypass = true;
                            else if (EntryValue == "AllOff")
                                item.Value.Bypass = false;
                        }
                    }
                }
            }
            AlarmParameterOperations.Clear();
            IEnumerable<KeyValuePair<string, EventItem>> findGroupAlarm;
            if (SelectedAlarmGroup != null)
            {
                findGroupAlarm = _allGroupAlarmDic[SelectedAlarmTableIndex.ToString()].Where(x => x.Value.Category == SelectedAlarmGroup);
            }
            else
            {
                findGroupAlarm = _allGroupAlarmDic[SelectedAlarmTableIndex.ToString()];
            }
            foreach (var item in findGroupAlarm)
            {
                AlarmParameterOperations.Add(new AlarmParameterOperation(item.Key, item.Value));
            }
            //  GetParameterStr();
        }

        public void AlarmTypeSelected(object obj)
        {
            var tempobj = obj;
            var selectedAlarmGroup = (string)tempobj;
            SelectedAlarmGroup = selectedAlarmGroup;
            AlarmParameterOperations.Clear();
            var findGroupAlarm = _allGroupAlarmDic[SelectedAlarmTableIndex.ToString()].Where(x => x.Value.Category == selectedAlarmGroup);
            foreach (var item in findGroupAlarm)
            {
                AlarmParameterOperations.Add(new AlarmParameterOperation(item.Key, item.Value));
            }
        }

        public class AlarmTabItem : PropertyChangedBase
        {
            private string _tabName;

            public string TabName
            {
                get { return _tabName; }
                set
                {
                    _tabName = value;
                    NotifyOfPropertyChange("TabName");
                }
            }

            private bool _isCheck;

            public bool IsCheck
            {
                get { return _isCheck; }
                set
                {
                    _isCheck = value;
                    NotifyOfPropertyChange("IsCheck");
                }
            }
        }
    }
}