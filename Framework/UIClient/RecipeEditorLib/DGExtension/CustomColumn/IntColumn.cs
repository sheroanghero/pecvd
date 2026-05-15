using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeEditorLib.DGExtension.CustomColumn
{
    public class IntColumn : EditorDataGridTemplateColumnBase
    {
        public double Minimun { get; set; }
        public double Maximun { get; set; }
    }
}
