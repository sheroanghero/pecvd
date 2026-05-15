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

    public class AlarmParameterOperation : PropertyChangedBase
    {
        public AlarmParameterOperation()
        { }

        private string _alarmName;
        public string AlarmName
        {
            get => _alarmName;
            set
            {
                _alarmName = value;
                NotifyOfPropertyChange("AlarmName");
            }
        }
        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                NotifyOfPropertyChange("DisplayName");
            }
        }
        private string _source;
        public string Source
        {
            get => _source;
            set
            {
                _source = value;
                NotifyOfPropertyChange("Source");
            }
        }

        private int _id;
        public int ID
        {
            get => _id;
            set
            {
                _id = value;
                NotifyOfPropertyChange("ID");
            }
        }

        private string _description;
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                NotifyOfPropertyChange("Description");
            }
        }

        private string _solution;
        public string Solution
        {
            get => _solution;
            set
            {
                _solution = value;
                NotifyOfPropertyChange("Solution");
            }
        }

        private string _explaination;
        public string Explaination
        {
            get => _explaination;
            set
            {
                _explaination = value;
                NotifyOfPropertyChange("Explaination");
            }
        }


        private bool _bypass;
        public bool Bypass
        {
            get => _bypass;
            set
            {
                _bypass = value;
                NotifyOfPropertyChange("Bypass");
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

        private bool _isEdit;
        public bool IsEdit
        {
            get => _isEdit;
            set
            {
                _isEdit = value;
                NotifyOfPropertyChange("IsEdit");
            }
        }

        private int _selectedIndex;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;
                NotifyOfPropertyChange("SelectedIndex");
            }
        }
        private EventLevel _level;
        public EventLevel Level
        {
            get => _level;
            set
            {
                _level = value;
                NotifyOfPropertyChange("Level");
            }
        }
        private bool _autoRecovery;
        public bool AutoRecovery
        {
            get => _autoRecovery;
            set
            {
                _autoRecovery = value;
                NotifyOfPropertyChange("AutoRecovery");
            }
        }

        public AlarmParameterOperation(string key, EventItem eventItem)
        {
            AlarmName = key;
            if (eventItem != null)
            {
                ID = eventItem.Id;
                Source = eventItem.Source;
                Description = eventItem.Description;
                Solution = eventItem.Solution;
                Explaination = eventItem.Explaination;
                Bypass = eventItem.Bypass;
                Group = eventItem.Group.ToString();
                IsEdit = eventItem.Editable;
                Level = eventItem.Level;
                DisplayName = eventItem.DisplayName;
                AutoRecovery = eventItem.AutoRecovery;
            }
        }

        public AlarmParameterOperation(string alarmParameterStr)
        {
            if (alarmParameterStr != null)
            {
                var alarmPar = alarmParameterStr.Split(',');
                if (alarmPar != null && alarmPar.Length > 0)
                {
                    _id = int.Parse(alarmPar[0]);
                    //_isSave = bool.Parse(alarmPar[1]);
                    _group = alarmPar[2];
                    _isEdit = bool.Parse(alarmPar[3]);
                }
            }
        }

        public override string ToString()
        {
            string rtnString = $"{_id},{_group},{_isEdit}";
            return rtnString;
        }

    }


    public class ShowAlarmGroup : PropertyChangedBase
    {
        private string _name;
        public string Name
        {
            get => _name; set
            {
                _name = value;
                NotifyOfPropertyChange("Name");
            }
        }

        private string _display;
        public string Display
        {
            get => _display; set
            {
                _display = value;
                NotifyOfPropertyChange("Display");
            }
        }

        private bool _bvalue = false;
        public bool AlarmBoolValue
        {
            get { return _bvalue; }
            set { _bvalue = value; NotifyOfPropertyChange("AlarmBoolValue"); }
        }


    }
}