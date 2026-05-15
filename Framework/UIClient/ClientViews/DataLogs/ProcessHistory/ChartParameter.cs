using System.Collections.ObjectModel;
using System.Windows;
using Caliburn.Micro.Core;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.ProcessHistory
{
    //public class ChartParameter : PropertyChangedBase
    //{
    //    public bool Visible { get; set; }
    //    public string DataSource { get; set; }
    //    public string DataVariable { get; set; }     
    //    public string Factor { get; set; }
    //    public string Offset { get; set; }
    //    public string LineWidth { get; set; }
  
    //    private Brush _Color;
    //    public Brush Color
    //    {
    //        get { return _Color; }
    //        set { _Color = value; NotifyOfPropertyChange("Color"); }
    //    }

    //    public ParameterNode RelatedNode { get; set; }
        
    //}

    public class RecipeItem : PropertyChangedBase
    {
        private bool _Selected = false;
        public bool Selected
        {
            get { return _Selected; }
            set { _Selected = value; NotifyOfPropertyChange("Selected"); }
        }
        public string Recipe { get; set; }
        public string LotID { get; set; }

        public string SlotID { get; set; }

        public string CjName { get; set; }

        public string PjName { get; set; }
        public string Chamber { get; set; }
        public string Status { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }

    public class ParameterNode : PropertyChangedBase
    {
        private bool _Selected = false;
        public bool Selected
        {
            get { return _Selected; }
            set
            {
                _Selected = value;
                NotifyOfPropertyChange("Selected");
            }
        }

        public string Name { get; set; }

        private string _averageValue;
        public string AverageValue
        {
            get { return _averageValue; }
            set
            {
                _averageValue = value;
                NotifyOfPropertyChange("AverageValue");
            }
        }

        private string _minValue;
        public string MinValue
        {
            get { return _minValue; }
            set
            {
                _minValue = value;
                NotifyOfPropertyChange("MinValue");
            }
        }

        private string _maxValue;
        public string MaxValue
        {
            get { return _maxValue; }
            set
            {
                _maxValue = value;
                NotifyOfPropertyChange("MaxValue");
            }
        }
        public Visibility IsVisibilityParentNode { get; set; }

        public ObservableCollection<ParameterNode> ChildNodes { get; set; }
        public ParameterNode ParentNode { get; set; }
    }
}
