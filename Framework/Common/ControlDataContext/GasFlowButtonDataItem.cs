using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Aitex.Core.UI.ControlDataContext
{
    public class GasFlowButtonDataItem : INotifyPropertyChanged
    {
        Dictionary<string, string> _colorDic = new Dictionary<string, string>()
        {
            {"N2", "SkyBlue"},
            {"H2", "Pink"},
            {"N2|H2", "Red"},
            {"NH3", "Cyan"},
            {"MO", "Gold"},
            {"Default", "Gainsboro"},
        };
        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string DeviceName { get; set; }

        string _displayName = "";
        public string DisplayName
        {
            get
            {
                return _displayName;
            }
            set
            {
                _displayName = value;
                InvokePropertyChanged("DisplayName");
            }
        }


        string _gasType = "Default";
        public string GasType
        {
            get
            {
                return _gasType;
            }
            set
            {
                _gasType = value;
                GasBackgroundColor = MapColor(value);

                InvokePropertyChanged("GasType");
                InvokePropertyChanged("GasBackgroundColor");
            }
        }
        public string GasBackgroundColor{get;    set;    }

        string _carrierGasName = "";
        public string CarrierGasName
        {
            get
            {
                return _carrierGasName;
            }
            set
            {
                _carrierGasName = value;
                CarrierGasBackgroundColor = MapColor(value);

                InvokePropertyChanged("CarrierGasName");
                InvokePropertyChanged("CarrierGasBackgroundColor");
            }
        }
        public string CarrierGasBackgroundColor{get;    set;    }


        string _pipeGasName = "Default";
        public string PipeGasName
        {
            get
            {
                return _pipeGasName;
            }
            set
            {
                _pipeGasName = value;
                PipeGasColor = MapColor(value);

                InvokePropertyChanged("PipeGasName");
                InvokePropertyChanged("PipeGasColor");
            }
        }
        public string PipeGasColor { get; set; }

        bool _isFlow2NH3 = false;
        public bool IsFlow2NH3
        {
            get
            {
                return _isFlow2NH3;
            }
            set
            {
                _isFlow2NH3 = value;
                InvokePropertyChanged("IsFlow2NH3");
            }
        }

        bool _isFlow2NH3E = false;
        public bool IsFlow2NH3E
        {
            get
            {
                return _isFlow2NH3E;
            }
            set
            {
                _isFlow2NH3E = value;
                InvokePropertyChanged("IsFlow2NH3E");
            }
        }

        bool _isFlow2NH3C = false;
        public bool IsFlow2NH3C
        {
            get
            {
                return _isFlow2NH3C;
            }
            set
            {
                _isFlow2NH3C = value;
                InvokePropertyChanged("IsFlow2NH3C");
            }
        }

        bool _isFlow2Run = false;
        public bool IsFlow2Run
        {
            get
            {
                return _isFlow2Run;
            }
            set
            {
                _isFlow2Run = value;
                InvokePropertyChanged("IsFlow2Run");
            }
        }

        bool _isFlow2Vent = false;
        public bool IsFlow2Vent
        {
            get
            {
                return _isFlow2Vent;
            }
            set
            {
                _isFlow2Vent = value;
                InvokePropertyChanged("IsFlow2Vent");
            }
        }

        string _totalFlow="-";
        public string TotalFlow
        {
            get
            {
                return _totalFlow;
            }
            set
            {
                _totalFlow = value;
                InvokePropertyChanged("TotalFlow");
            }
        }



        public GasFlowButtonDataItem()
        {
            GasType = "default";
            CarrierGasName = "";
            PipeGasName = "default";
        }

        string MapColor(string gasName)
        {
            return _colorDic.ContainsKey(gasName) ? _colorDic[gasName] : _colorDic["Default"];
        }
    }
}
