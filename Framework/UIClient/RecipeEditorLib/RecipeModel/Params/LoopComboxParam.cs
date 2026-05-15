using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RecipeEditorLib.DGExtension.CustomColumn;
namespace RecipeEditorLib.RecipeModel.Params
{
    public class LoopComboxParam : Param
    {
        public ObservableCollection<LoopComboxColumn.Option> Options { get; set; }

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

        private bool _isLoopStep;
        public bool IsLoopStep
        {
            get
            {
                return _isLoopStep;
            }
            set
            {
                _isLoopStep = value;
                this.NotifyOfPropertyChange("IsLoopStep");
                NotifyOfPropertyChange("LoopBackground");
            }
        }

        private bool _isValidLoop;
        public bool IsValidLoop
        {
            get
            {
                return _isValidLoop;
            }
            set
            {
                _isValidLoop = value;
                this.NotifyOfPropertyChange("IsValidLoop");
                NotifyOfPropertyChange("LoopBackground");
            }
        }

        public string LoopBackground
        {
            get
            {
                return IsLoopStep ? (IsValidLoop ? "#90EE90" : "#FFC0CB") : "Transparent";
            }
 
        }


        public bool IsLoopItem { get; set; }
    }
}
