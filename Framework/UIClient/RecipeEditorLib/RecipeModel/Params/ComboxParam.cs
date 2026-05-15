using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RecipeEditorLib.DGExtension.CustomColumn;
namespace RecipeEditorLib.RecipeModel.Params
{
    public class ComboxParam : Param
    {
        public ObservableCollection<ComboxColumn.Option> Options { get; set; }

        private string _Value = string.Empty;
        public string Value
        {
            get { return this._Value; }
            set
            {
                this._Value = value;
                if (this.Feedback != null)
                    this.Feedback(this);
                this.NotifyOfPropertyChange("Value");
            }
        }
        
        private bool _isEditable;
        public bool IsEditable
        {
            get
            {
                return _isEditable;
            }
            set
            {
                _isEditable = value;
                this.NotifyOfPropertyChange("IsEditable");
            }
        }

        public string LoopBackground
        {
            get
            {
                return  "Transparent";
            }

        }


        public bool IsLoopItem
        {
            get { return false; }
        }
    }
}
