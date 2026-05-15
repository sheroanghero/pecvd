using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using System.Collections.ObjectModel;
using RecipeEditorLib.DGExtension.CustomColumn;


namespace RecipeEditorLib.RecipeModel.Params
{
    public class PositionParam : Param
    {
        private string _Value = string.Empty;
        public string Value
        {
            get { return this._Value; }
            set
            {
                this._Value = value;
                this.OptionChanged(value);
                this.NotifyOfPropertyChange("Value");
            }
        }
        public ObservableCollection<ComboxColumn.Option> Options { get; set; }

        private void OptionChanged(string value)
        {
            IEnumerable<ComboxColumn.Option> opts = Options.Where(op => op.ControlName == value);

            if (opts.Count() > 0)
            {
                string[] relatedparams = opts.ToList()[0].RelatedParameters.Split(',');
                this.Parent.ToList().ForEach(param =>
                {
                    if (relatedparams.Contains(param.Name) || param.Name == "Position" || param.Name == "StepNo" || param.Name == "Module")
                        param.Visible = Visibility.Visible;
                    else
                        param.Visible = Visibility.Hidden;
                });
            }
            else
            {
                this.Parent.ToList().ForEach(param =>
                {
                    if (param.Name == "Position" || param.Name == "StepNo" || param.Name == "Module")
                        param.Visible = Visibility.Visible;
                    else
                        param.Visible = Visibility.Hidden;
                });
            }
        }
    }
}
