using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Aitex.Core.UI.ControlDataContext
{
    public class GasFlowChartDataItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ObservableCollection<GasFlowButtonDataItem> MOGasItems { get; set; }

        public ObservableCollection<GasFlowButtonDataItem> NH3GasItems { get; set; }

                string _mOTotal = "";
        public string MOTotal
        {
            get
            {
                return _mOTotal;
            }
            set
            {
                _mOTotal = value;
                InvokePropertyChanged("MOTotal");
            }
        }

        string _hydrideTotal = "";
        public string HydrideTotal
        {
            get
            {
                return _hydrideTotal;
            }
            set
            {
                _hydrideTotal = value;
                InvokePropertyChanged("HydrideTotal");
            }
        }
    }
}
