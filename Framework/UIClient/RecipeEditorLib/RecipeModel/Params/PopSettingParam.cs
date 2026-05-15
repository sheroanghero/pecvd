using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Caliburn.Micro;
using Caliburn.Micro.Core;

namespace RecipeEditorLib.RecipeModel.Params
{
    public class PopSettingParam : Param
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
