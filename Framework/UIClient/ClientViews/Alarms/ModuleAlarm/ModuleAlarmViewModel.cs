using System;
using Aitex.Core.RT.Event;
using Aitex.Core.UI.MVVM;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Event;
using MECF.Framework.UI.Client.ClientBase;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using MECF.Framework.Common.OperationCenter;

namespace MECF.Framework.UI.Client.CenterViews.Alarms.ModuleAlarm
{
    public class ModuleAlarmViewModel : UiViewModelBase
    {
        public class UIModuleItem : NotifiableItem
        {
            public string ModuleName { get; set; }
            public bool IsSelected { get; set; }
        }

        public class UIAlarmItem : NotifiableItem
        {
            public string OccuringTime { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }

            public int EventId { get; set; }
            public string EventEnum { get; set; }
            public string Explaination { get; set; }
            public string Solution { get; set; }
            public string Source { get; set; }

            public bool IsEqualTo(UIAlarmItem item)
            {
                return item.OccuringTime == OccuringTime &&
                       item.EventEnum == EventEnum &&
                       item.Description == Description &&
                       item.Source == Source &&
                       item.Type == Type;
            }
        }


        public ObservableCollection<UIAlarmItem> FilteredAlarms { get; set; }

        public ObservableCollection<UIModuleItem> AllModules { get; set; }
        public ObservableCollection<UIModuleItem> SelectedModules { get; set; }

        public ICommand SelectionChanged { get; set; }
        public ICommand SelectAllChanged { get; set; }

        public bool IsAllSelected { get; set; }

        public ModuleAlarmViewModel()
        {
            Subscribe("System.ActiveAlarm");

            SelectionChanged = new DelegateCommand<object>(DoSelectionChanged);
            SelectAllChanged = new DelegateCommand<object>(DoSelectAllChanged);

            AllModules = new ObservableCollection<UIModuleItem>();

            FilteredAlarms = new ObservableCollection<UIAlarmItem>();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            var data = QueryDataClient.Instance.Service.GetData("System.Modules");
            if (data != null)
            {
                foreach (var module in (List<string>)data)
                {
                    AllModules.Add(new UIModuleItem()
                    {
                        IsSelected = true,
                        ModuleName = module,
                    });
                }
            }
            InvokePropertyChanged(nameof(AllModules));

            IsAllSelected = true;

            SelectedModules = new ObservableCollection<UIModuleItem>(AllModules);
        }

        private void DoSelectAllChanged(object obj)
        {
            if (IsAllSelected)
            {
                var modules = new ObservableCollection<UIModuleItem>(AllModules);
                foreach (var uiModuleItem in modules)
                {
                    uiModuleItem.IsSelected = true;
                }

                SelectedModules = modules;
                NotifyOfPropertyChange(nameof(SelectedModules));
            }
            else
            {
                SelectedModules.Clear();
            }
        }

        private void DoSelectionChanged(object obj)
        {
            bool allSelected = true;
            foreach (var uiModuleItem in AllModules)
            {
                if (!uiModuleItem.IsSelected && uiModuleItem.ModuleName != "All")
                {
                    allSelected = false;
                    break;
                }
            }

            IsAllSelected = allSelected;
            NotifyOfPropertyChange(nameof(IsAllSelected));
        }

        protected override void OnActivate()
        {
            base.OnActivate();

        }

        public void ResetAlarm(UIAlarmItem item)
        {
            InvokeClient.Instance.Service.DoOperation("System.ResetAlarm", item.Source, item.EventEnum);
        }

        protected override void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {
            if (data.ContainsKey("System.ActiveAlarm"))
                UpdateAlarmEvent((List<EventItem>)data["System.ActiveAlarm"]);
        }

        public void UpdateAlarmEvent(List<EventItem> evItems)
        {
            List<UIAlarmItem> removeList = new List<UIAlarmItem>();
            foreach (var filteredItem in FilteredAlarms)
            {
                if (SelectedModules.FirstOrDefault(x => x.ModuleName == filteredItem.Source) == null && !IsAllSelected)
                {
                    removeList.Add(filteredItem);
                    continue;
                }

                if (!evItems.Exists(x => x.Source == filteredItem.Source && x.EventEnum == filteredItem.EventEnum))
                {
                    removeList.Add(filteredItem);
                    continue;
                }

                if (evItems.Exists(x => x.Source == filteredItem.Source && x.EventEnum == filteredItem.EventEnum && x.Description == filteredItem.Description))
                {
                    removeList.Add(filteredItem);
                    continue;
                }
            }

            foreach (var uiAlarmItem in removeList)
            {
                FilteredAlarms.Remove(uiAlarmItem);
            }

            foreach (var item in evItems)
            {
                var newItem = new UIAlarmItem()
                {
                    Type = item.Level == EventLevel.Alarm ? "Alarm" : (item.Level == EventLevel.Information ? "Info" : "Warning"),
                    OccuringTime = item.OccuringTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    Description = item.Description,
                    EventEnum = item.EventEnum,
                    EventId = item.Id,
                    Explaination = item.Explaination,
                    Solution = item.Solution,
                    Source = item.Source,
                };

                if (FilteredAlarms.FirstOrDefault(x => x.IsEqualTo(newItem)) == null &&
                    (SelectedModules.FirstOrDefault(x => x.ModuleName == newItem.Source) != null || IsAllSelected))
                {
                    FilteredAlarms.Add(newItem);
                }
            }

        }
    }
}