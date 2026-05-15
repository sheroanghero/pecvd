using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace RecipeEditorLib.DGExtension.CustomColumn
{
    public class PositionColumn : EditorDataGridTemplateColumnBase
    {
        public PositionColumn() : base()
        {
            this.Options = new ObservableCollection<ComboxColumn.Option>();
        }
        public ObservableCollection<ComboxColumn.Option> Options { get; set; }
    }
}
