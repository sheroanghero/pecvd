using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.RecipeCenter
{
    public class RecipeTemplateColumnBase
    {
        public RecipeTemplateColumnBase()
        {
            IsReadOnly = false;
            IsEnable = true;
        }
        public string ValueType { get; set; }
        public bool IsReadOnly { get; set; }
        public string ModuleName { get; set; }
        public string DisplayName { get; set; }
        public string ControlName { get; set; }
        public string Description { get; set; }
        public bool IsEnable { get; set; }
        public string Default { get; set; }
        public int SelectedValueIndex { get; set; }
        public double Value { get; set; }
        public bool EnableConfig { get; set; }
        public bool EnableTolerance { get; set; }
        public string InputMode { get; set; }
        public double Minimun { get; set; }
        public double Maximun { get; set; }

        public ObservableCollection<Option> Options { get; set; } = new ObservableCollection<Option>();

    }

    public class Option
    {
        public string ControlName { get; set; }
        public string DisplayName { get; set; }
        public string RelatedParameters { get; set; }

    }
}
