using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeEditorLib.DGExtension.CustomColumn
{
    public class LoopComboxColumn : EditorDataGridTemplateColumnBase
    {
        public LoopComboxColumn() : base()
        {
            this.Options = new ObservableCollection<Option>();
        }
        public class Option
        {
            public string ControlName { get; set; }
            public string DisplayName { get; set; }
            public string RelatedParameters { get; set; }
        }
        public ObservableCollection<Option> Options { get; set; }

    }
}
