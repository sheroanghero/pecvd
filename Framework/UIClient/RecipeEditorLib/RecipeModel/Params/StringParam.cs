using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeEditorLib.RecipeModel.Params
{
    public class StringParam : Param
    {
        private string _value;
        public string Value
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
