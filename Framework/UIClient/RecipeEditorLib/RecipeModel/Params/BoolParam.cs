using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeEditorLib.RecipeModel.Params
{
    public class BoolParam : Param
    {
        private bool _value;
        public bool Value
        {
            get { return this._value; }
            set
            {
                this._value = value;
                if (this.Feedback != null)
                    this.Feedback(this);
                this.NotifyOfPropertyChange("Value");
            }
        }
        
    }
}
