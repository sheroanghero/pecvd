using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RecipeEditorLib.DGExtension.CustomColumn
{
    public class ExpanderColumn : EditorDataGridTemplateColumnBase
    {
        public static DependencyProperty IsExpanderProperty = DependencyProperty.Register("IsExpanded", typeof(bool), typeof(ExpanderColumn), new PropertyMetadata(true));
        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpanderProperty); }
            set
            {
                SetValue(IsExpanderProperty, value);
            }
        }
    }
}
