using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using RecipeEditorLib.DGExtension.CustomColumn;

namespace RecipeEditorLib.RecipeModel.Params
{
    public class MultipleSelectParam : Param
    {
        public MultipleSelectParam(MultipleSelectColumn col) : base()
        {
            this.Options = new ObservableCollection<MultipleSelectColumn.Option>();
            col.CloneOptions(this).ForEach(opt => this.Options.Add(opt));
            this.IsSaved = true;
        }

        public ObservableCollection<MultipleSelectColumn.Option> Options { get; set; }
    }
}
