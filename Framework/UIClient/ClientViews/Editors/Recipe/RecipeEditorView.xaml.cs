using OpenSEMI.Controls.Controls;
using RecipeEditorLib.DGExtension.CustomColumn;
using System.Windows.Controls;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    /// <summary>
    /// Interaction logic for RecipePM1View.xaml
    /// </summary>
    public partial class RecipeEditorView : UserControl
    {
        public RecipeEditorView()
        {
            InitializeComponent();
        }
        EditorDataGridTemplateColumnBase _PreColumn = null;
        private void dgCustom_CurrentCellChanged(object sender, System.EventArgs e)
        {
            var datagrid = sender as XDataGrid;
            if (datagrid == null) return;
            var column = datagrid.CurrentColumn as EditorDataGridTemplateColumnBase;
            if (column == null) return;

            if (_PreColumn == datagrid.CurrentColumn) return;

            if (_PreColumn != null)
            {
                _PreColumn.IsColumnSelected = false;
                foreach (var item in datagrid.Items)
                {
                    var list = item as System.Collections.ObjectModel.ObservableCollection<RecipeEditorLib.RecipeModel.Params.Param>;
                    if (list == null) continue;
                    foreach (var p in list)
                    {
                        if (p.Name == _PreColumn.ControlName) p.IsColumnSelected = false;
                    }
                }
            }
            column.IsColumnSelected = true;
            _PreColumn = column;
            //var jj = datagrid.Items as System.Collections.ObjectModel.ObservableCollection<RecipeEditorLib.RecipeModel.Params.Param>;
            foreach (var item in datagrid.Items)
            {
                var list = item as System.Collections.ObjectModel.ObservableCollection<RecipeEditorLib.RecipeModel.Params.Param>;
                if (list == null) continue;
                foreach (var p in list)
                {
                    if (p.Name == column.ControlName) p.IsColumnSelected = true;
                }
            }
        }

       
    }
}
