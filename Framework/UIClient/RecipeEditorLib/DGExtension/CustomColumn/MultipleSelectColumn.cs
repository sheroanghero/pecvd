using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using RecipeEditorLib.RecipeModel.Params;

namespace RecipeEditorLib.DGExtension.CustomColumn
{
    public class MultipleSelectColumn : EditorDataGridTemplateColumnBase
    {
        public MultipleSelectColumn() : base()
        {
            this.Options = new ObservableCollection<Option>();
        }
        public class Option
        {
            public MultipleSelectParam parent { get; set; }
           
            public string ControlName { get; set; }
            public string DisplayName { get; set; }
            public string RelatedParameters { get; set; }
            public bool IsChecked
            {
                get { return this.isChecked; }
                set
                {
                    this.isChecked = value;
                    if (this.parent!=null)
                        this.parent.IsSaved = false;
                }
            }

            private bool isChecked;
        }
        public ObservableCollection<Option> Options { get; set; }
        public List<Option> CloneOptions(MultipleSelectParam _parent = null)
        {
            List<Option> opts = new List<Option>();
            this.Options.ToList().ForEach(opt =>
            {
                opts.Add(new Option()
                {
                    parent = _parent,
                    ControlName = opt.ControlName,
                    DisplayName = opt.DisplayName,
                    RelatedParameters = opt.RelatedParameters
                });
            });

            return opts;
        }
    }
}
