using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeEditorLib.RecipeModel.Params
{
    public class StepParam : Param
    {
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
        private bool _checked = false;
        public bool Checked
        {
            get { return this._checked; }
            set
            {
                this._checked = value;
                this.NotifyOfPropertyChange("Checked");
            }
        }
    }
}
