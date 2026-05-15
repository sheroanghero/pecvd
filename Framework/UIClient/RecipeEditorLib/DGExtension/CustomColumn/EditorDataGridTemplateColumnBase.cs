using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using RecipeEditorLib.RecipeModel.Params;

namespace RecipeEditorLib.DGExtension.CustomColumn
{
    public class EditorDataGridTemplateColumnBase: ExtendedGrid.Microsoft.Windows.Controls.DataGridTemplateColumn
    {
        public EditorDataGridTemplateColumnBase()
        {
            this.IsReadOnly = false;
            this.IsEnable = true;
        }
        public string ModuleName { get; set; }
        public string DisplayName { get; set; }
        public string ControlName { get; set; }
        public string Description { get; set; }
        public string StringCellTemplate { get; set; }
        public string StringHeaderTemplate { get; set; }
        public bool  IsEnable { get; set; }
        public string Default { get; set; }
        public bool EnableConfig { get; set; }
        public bool EnableTolerance { get; set; }
        public bool IsColumnSelected
        {
            get { return (bool)GetValue(IsColumnSelectedProperty); }
            set { SetValue(IsColumnSelectedProperty, value); }
        }
        /// <summary>
        ///     The DependencyProperty that represents the IsReadOnly property.
        /// </summary>
        public static readonly DependencyProperty IsColumnSelectedProperty =
            DependencyProperty.Register("IsColumnSelected", typeof(bool), typeof(EditorDataGridTemplateColumnBase), new FrameworkPropertyMetadata(false, OnNotifyCellPropertyChanged));

        public Action<Param> Feedback { get; set; }
        public Func<Param,bool> Check { get; set; }
    }
}
