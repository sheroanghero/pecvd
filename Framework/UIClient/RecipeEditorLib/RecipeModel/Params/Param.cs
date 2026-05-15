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
    public abstract class Param : PropertyChangedBase
    {
        public Action<Param> Feedback { get; set; }
        public Func<Param, bool> Check { get; set; }

        public ObservableCollection<Param> Parent { get; set; }


        private string name;
        public string Name
        {
            get { return this.name; }
            set
            {
                this.name = value;
                this.NotifyOfPropertyChange("Name");
            }
        }

        private string _displayName;
        public string DisplayName
        {
            get { return this._displayName; }
            set
            {
                this._displayName = value;
                this.NotifyOfPropertyChange("DisplayName");
            }
        }

        private bool isEnabled;
        public bool IsEnabled
        {
            get
            {
                return this.isEnabled;
            }
            set
            {
                this.isEnabled = value;
                this.NotifyOfPropertyChange("IsEnabled");
            }
        }

        private bool isSaved;
        public bool IsSaved
        {
            get { return this.isSaved; }
            set
            {
                this.isSaved = value;
                this.NotifyOfPropertyChange("IsSaved");
            }
        }
        private bool _IsColumnSelected;

        public bool IsColumnSelected
        {
            get { return this._IsColumnSelected; }
            set
            {
                this._IsColumnSelected = value;
                this.NotifyOfPropertyChange("IsColumnSelected");
            }
        }
        private Visibility visibility;
        public Visibility Visible
        {
            get { return this.visibility; }
            set
            {
                this.visibility = value;
                this.NotifyOfPropertyChange("Visible");
            }
        }


        public bool EnableConfig { get; set; }
        public bool EnableTolerance { get; set; }

        public Visibility StepCheckVisibility { get; set; }

        public Param()
        {
            this.IsSaved = true;
            isEnabled = true;
        }

    }
}
