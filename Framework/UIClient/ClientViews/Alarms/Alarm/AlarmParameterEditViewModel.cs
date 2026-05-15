using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Aitex.Core.RT.Event;
using Aitex.Core.UI.View.Common;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using MECF.Framework.Common.Alarms;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Alarms.Alarm
{

    public class AlarmParameterEditViewModel : UiViewModelBase
    {

        private AlarmParameterOperation _alarmParameterOperationItem;
        public AlarmParameterOperation AlarmParameterOperationItem
        {
            get { return _alarmParameterOperationItem; }
            set
            {
                _alarmParameterOperationItem = value;
                NotifyOfPropertyChange("AlarmParameterOperationItem");
            }
        }

        private string _alrmTabName;

        public string AlrmTabName
        {
            get { return _alrmTabName; }
            set { _alrmTabName = value; }
        }

        private string _alarmParameterName;

        public string AlarmParameterName
        {
            get { return _alarmParameterName; }
            set { _alarmParameterName = value; }
        }

        private int _groupSelectedIndex = 0;
        public int GroupSelectedIndex
        {
            get { return _groupSelectedIndex; }
            set
            {
                _groupSelectedIndex = value;
                NotifyOfPropertyChange("GroupSelectedIndex");
            }
        }

        private int _levelSelectedIndex = 0;
        public int LevelSelectedIndex
        {
            get { return _levelSelectedIndex; }
            set
            {
                _levelSelectedIndex = value;
                NotifyOfPropertyChange("LevelSelectedIndex");
            }
        }

        private bool _isEdit;

        public bool IsEdit
        {
            get { return _isEdit; }
            set
            {
                _isEdit = value;
                NotifyOfPropertyChange("IsEdit");
            }
        }

        private bool _notEdit;

        public bool NotEdit
        {
            get { return _notEdit; }
            set
            {
                _notEdit = value;
                NotifyOfPropertyChange("NotEdit");
            }
        }

        private bool _isPassBy;

        public bool IsPassBy
        {
            get { return _isPassBy; }
            set
            {
                _isPassBy = value;
                NotifyOfPropertyChange("IsPassBy");
            }
        }

        private bool _notPassby;

        public bool NotPassBy
        {
            get { return _notPassby; }
            set
            {
                _notPassby = value;
                NotifyOfPropertyChange("NotPassBy");
            }
        }

        private bool _isAutoRecovery;

        public bool IsAutoRecovery
        {
            get { return _isAutoRecovery; }
            set
            {
                _isAutoRecovery = value;
                NotifyOfPropertyChange("IsAutoRecovery");
            }
        }


        private bool _notAutoRecovery;

        public bool NotAutoRecovery
        {
            get { return _notAutoRecovery; }
            set
            {
                _notAutoRecovery = value;
                NotifyOfPropertyChange("NotAutoRecovery");
            }

        }

        private string _group;

        public string Group
        {
            get => _group;
            set
            {
                _group = value;
                NotifyOfPropertyChange("Group");
            }
        }

        //private 


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

        private List<string> _levelList = new List<string>
        (
            new string[] {
            "Information",
            "Warning",
            "Alarm"
            });

        public Dictionary<string, Dictionary<string, EventItem>> AllGroupAlarmDic = new Dictionary<string, Dictionary<string, EventItem>>();

        public List<string> GroupNames
        {
            get => _groupNames;
            set

            {
                _groupNames = value;
                NotifyOfPropertyChange("GroupNames");
            }
        }

        public List<string> LevelList
        {
            get => _levelList;
            set

            {
                _levelList = value;
                NotifyOfPropertyChange("LevelList");
            }
        }


        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            IsEdit = AlarmParameterOperationItem.IsEdit;
            NotEdit = !IsEdit;
            IsPassBy = AlarmParameterOperationItem.Bypass;
            NotPassBy = !IsPassBy;
            IsAutoRecovery = AlarmParameterOperationItem.AutoRecovery;
            NotAutoRecovery = !IsAutoRecovery;

            GroupSelectedIndex = _groupNames.IndexOf(AlarmParameterOperationItem.Group);

            LevelSelectedIndex = _levelList.IndexOf(AlarmParameterOperationItem.Level.ToString());

        }

        protected override void OnActivate()
        {
            base.OnActivate();
        }

        public void IsEditChecked(string val)
        {
            if (val.Equals("REFF"))
            {
                _isEdit = true;
            }
            else
            {
                _isEdit = false;
            }
        }

        public void IsbBypassChecked(string val)
        {
            if (val.Equals("YES"))
            {
                _isPassBy = true;
            }
            else
            {
                _isPassBy = false;
            }
        }

        public void IsAutoRecoveryChecked(string val)
        {
            if (val.Equals("TURE"))
            {
                _isAutoRecovery = true;
            }
            else
            {
                _isAutoRecovery = false;
            }
        }

        public void AlarmTableItemSave()
        {
            AlarmParameterOperationItem.IsEdit = IsEdit;
            AlarmParameterOperationItem.Bypass = IsPassBy;
            AlarmParameterOperationItem.AutoRecovery = _isAutoRecovery;

            if (AllGroupAlarmDic.Count > 0)
            {
                if (AllGroupAlarmDic.ContainsKey(AlrmTabName) && AllGroupAlarmDic[AlrmTabName].ContainsKey(AlarmParameterOperationItem.AlarmName))
                {
                    AllGroupAlarmDic[AlrmTabName][AlarmParameterOperationItem.AlarmName].Id = AlarmParameterOperationItem.ID;
                    AllGroupAlarmDic[AlrmTabName][AlarmParameterOperationItem.AlarmName].Description = AlarmParameterOperationItem.Description;
                    AllGroupAlarmDic[AlrmTabName][AlarmParameterOperationItem.AlarmName].Solution = AlarmParameterOperationItem.Solution;
                    AllGroupAlarmDic[AlrmTabName][AlarmParameterOperationItem.AlarmName].Explaination = AlarmParameterOperationItem.Explaination;
                    AllGroupAlarmDic[AlrmTabName][AlarmParameterOperationItem.AlarmName].Bypass = AlarmParameterOperationItem.Bypass;
                    AllGroupAlarmDic[AlrmTabName][AlarmParameterOperationItem.AlarmName].Group = AlarmParameterOperationItem.Group.Contains("None Specified") ? EventGroup.Group0 : (EventGroup)Enum.ToObject(typeof(EventGroup), LevelSelectedIndex);
                    AllGroupAlarmDic[AlrmTabName][AlarmParameterOperationItem.AlarmName].Editable = AlarmParameterOperationItem.IsEdit;
                    AllGroupAlarmDic[AlrmTabName][AlarmParameterOperationItem.AlarmName].Level = AlarmParameterOperationItem.Level;
                    AllGroupAlarmDic[AlrmTabName][AlarmParameterOperationItem.AlarmName].DisplayName = AlarmParameterOperationItem.DisplayName;
                    AllGroupAlarmDic[AlrmTabName][AlarmParameterOperationItem.AlarmName].AutoRecovery = AlarmParameterOperationItem.AutoRecovery;
                }
            }

            InvokeClient.Instance.Service.DoOperation($"System.UpdateAlarmTable", AllGroupAlarmDic);
            ((Window)GetView()).DialogResult = false;
        }

        public void AlarmTablaItemCancel()
        {
            ((Window)GetView()).DialogResult = false;
        }
    }
}