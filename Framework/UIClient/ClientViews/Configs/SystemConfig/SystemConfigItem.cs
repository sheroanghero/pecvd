using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro.Core;
using SciChart.Core.Extensions;

namespace MECF.Framework.UI.Client.CenterViews.Configs.SystemConfig
{
    public enum DataType
    {
        Unknown,
        Int,
        Double,
        Bool,
        String
    };

    public class ConfigNode : PropertyChangedBase
    {
        private string name = string.Empty;
        public string Name
        {
            get { return name; }
            set { name = value; NotifyOfPropertyChange("Name"); }
        }

        private string path = string.Empty;
        public string Path
        {
            get { return path; }
            set { path = value; NotifyOfPropertyChange("Path"); }
        }

        private string _display = string.Empty;
        public string Display
        {
            get { return _display; }
            set { _display = value; NotifyOfPropertyChange("Display"); }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; NotifyOfPropertyChange("IsExpanded"); }
        }

        private bool _isMatch;
        public bool IsMatch
        {
            get { return _isMatch; }
            set { _isMatch = value; NotifyOfPropertyChange("IsMatch"); }
        }

        private List<ConfigNode> _subNodes = null;
        public List<ConfigNode> SubNodes
        {
            get { return _subNodes; }
            set { _subNodes = value; NotifyOfPropertyChange("SubNodes"); }
        }

        private List<ConfigItem> _items = null;
        public List<ConfigItem> Items
        {
            get { return _items; }
            set { _items = value; NotifyOfPropertyChange("Items"); }
        }



        private bool IsCriteriaMatched(string criteria)
        {
            bool matched =  string.IsNullOrEmpty(criteria) || name.Contains(criteria) || name.ToLower().Contains(criteria.ToLower());

            if (matched)
                return true;

            foreach (var configItem in Items)
            {
                if (configItem.Name.Contains(criteria))
                    return true;

                if (configItem.Name.ToLower().Contains(criteria.ToLower()))
                    return true;
            }

            return false;
        }

        private void CheckChildren(string criteria, ConfigNode parent)
        {
            foreach (var child in parent.SubNodes)
            {
                if ( !child.IsCriteriaMatched(criteria))
                {
                    child.IsMatch = false;
                }
                CheckChildren(criteria, child);
            }
        }

        public void ApplyCriteria(string criteria, Stack<ConfigNode> ancestors)
        {
            if (IsCriteriaMatched(criteria))
            {
                IsMatch = true;
                foreach (var ancestor in ancestors)
                {
                    ancestor.IsMatch = true;
                    ancestor.IsExpanded = !string.IsNullOrEmpty(criteria);
                    //CheckChildren(criteria, ancestor);
                }
                IsExpanded = true;

            }
            else
                IsMatch = false;

            ancestors.Push(this);
            foreach (var child in SubNodes)
                child.ApplyCriteria(criteria, ancestors);

            ancestors.Pop();
        }
    }

    public class ConfigItem : PropertyChangedBase
    {
        private string name = string.Empty;
        public string Name
        {
            get { return name; }
            set { name = value; NotifyOfPropertyChange("Name"); }
        }

        private string description = string.Empty;
        public string Description
        {
            get { return description; }
            set { description = value; NotifyOfPropertyChange("Description"); }
        }
        private bool enable = true;
        public bool Enable
        {
            get { return enable; }
            set { enable = value; NotifyOfPropertyChange("Enable"); }
        }
        private string _display = string.Empty;
        public string Display
        {
            get { return _display; }
            set { _display = value; NotifyOfPropertyChange("Display"); }
        }

        private DataType type = DataType.Unknown;
        public DataType Type
        {
            get { return type; }
            set { type = value; NotifyOfPropertyChange("Type"); }
        }

        private string defaultValue = string.Empty;
        public string DefaultValue
        {
            get { return defaultValue; }
            set { defaultValue = value; NotifyOfPropertyChange("DefaultValue"); }
        }

        private double max = double.NaN;
        public double Max
        {
            get { return max; }
            set { max = value; NotifyOfPropertyChange("Max"); }
        }

        private double min = double.NaN;
        public double Min
        {
            get { return min; }
            set { min = value; NotifyOfPropertyChange("Min"); }
        }

        private string parameter = string.Empty;
        public string Parameter
        {
            get { return parameter; }
            set { parameter = value; NotifyOfPropertyChange("Parameter"); }
        }

        private string tag = string.Empty;
        public string Tag
        {
            get { return tag; }
            set { tag = value; NotifyOfPropertyChange("Tag"); }
        }

        private string unit = string.Empty;
        public string Unit
        {
            get { return unit; }
            set { unit = value; NotifyOfPropertyChange("Unit"); }
        }

        private bool visible = true;
        public bool Visible
        {
            get { return visible; }
            set { visible = value; NotifyOfPropertyChange("Visible"); }
        }

        private bool _isExpanded ;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; NotifyOfPropertyChange("IsExpanded"); }
        }

 
        private string cvalue = string.Empty;
        public string CurrentValue
        {
            get { return cvalue; }
            set { cvalue = value; NotifyOfPropertyChange("CurrentValue"); }
        }
 
        private bool _bvalue = false;
        public bool BoolValue
        {
            get { return _bvalue; }
            set { _bvalue = value; NotifyOfPropertyChange("BoolValue"); }
        }

        private string _sValue = string.Empty;
        public string StringValue
        {
            get { return _sValue; }
            set { _sValue = value; NotifyOfPropertyChange("StringValue"); }
        }

        private bool _textSaved = true;
        public bool TextSaved
        {
            get { return _textSaved; }
            set { _textSaved = value; NotifyOfPropertyChange("TextSaved"); }
        }

        //public List<string> Options
        //{
        //    get
        //    {
        //        return Parameter.IsNullOrEmpty() ? null : Parameter.Split(';').ToList();
        //    }
        //}

        public List<Option> Options
        {
            get
            {
                List<Option> options = new List<Option>();
                foreach (var item in Parameter.Split(';').ToList())
                {
                    Option o = new Option();
                    o.Name = item;
                    o.IsChecked = CurrentValue == item;
                    options.Add(o);
                }
                return options;
            }
        }

    }

    public class Option : PropertyChangedBase
    {
        public string Name { get; set; }

        private bool _IsChecked;
        public bool IsChecked
        {
            get { return _IsChecked; }
            set { _IsChecked = value; this.NotifyOfPropertyChange(nameof(IsChecked)); }
        }

        private bool _IsEnable = true;
        public bool IsEnable
        {
            get { return _IsEnable; }
            set { _IsEnable = value; this.NotifyOfPropertyChange(nameof(IsEnable)); }
        }
        private int _Index;
        public int Index
        {
            get { return _Index; }
            set { _Index = value; this.NotifyOfPropertyChange(nameof(Index)); }
        }
    }
}
