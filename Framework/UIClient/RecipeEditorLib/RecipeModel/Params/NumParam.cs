using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeEditorLib.RecipeModel.Params
{
    public class NumParam : Param
    {
        private int _value;
        public int Value
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

        private double _minimun;
        public double Minimun
        {
            get { return this._minimun; }
            set
            {
                this._minimun = value;
                this.NotifyOfPropertyChange("Minimun");
            }
        }

        private double _maximun;
        public double Maximun
        {
            get { return this._maximun; }
            set
            {
                this._maximun = value;
                this.NotifyOfPropertyChange("Maximun");
            }
        }
    }
}
