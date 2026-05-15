using System.Collections.Generic;
using System.Collections.ObjectModel;
using Caliburn.Micro.Core;
 
namespace MECF.Framework.UI.Client.ClientBase
{
    public class ModuleWaferManager : PropertyChangedBase
    {
        public string ModuleID;
        private bool hasWafer = false;
        private WaferInfo topWafer;
        private ObservableCollection<WaferInfo> wafers;

        public ModuleWaferManager(string _mod)
        {
            this.ModuleID = _mod;
            this.wafers = new ObservableCollection<WaferInfo>();
        }

        public ObservableCollection<WaferInfo> Wafers
        {
            get { return this.wafers; }
            set
            {
                this.wafers = value;
                this.RaisePropertyChangedEventImmediately("Wafers");
            }
        }

        public WaferInfo TopWafer
        {
            get { return this.topWafer; }
            set
            {
                if (this.topWafer != value)
                {
                    this.topWafer = value;
                    this.NotifyOfPropertyChange("TopWafer");
                }
            }
        }

        public bool HasWafer
        {
            get { return this.hasWafer; }
            set
            {
                if (this.hasWafer != value)
                {
                    this.hasWafer = value;
                    this.RaisePropertyChangedEventImmediately("HasWafer");
                }
            }
        }

    }
}
