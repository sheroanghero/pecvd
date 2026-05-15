using Aitex.Core.UI.MVVM;
using MECF.Framework.Simulator.Core.Commons;
using MECF.Framework.Simulator.Core.Driver;
using MECF.Framework.Simulator.Core.LoadPorts;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MECF.Framework.Simulator.Core.VCE.SiasunVCE
{
    /// <summary>
    /// SimSiasunVCEView.xaml 的交互逻辑
    /// </summary>
    public partial class SimSiasunVCEView : UserControl
    {

        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
            "Port", typeof(string), typeof(SimSiasunVCEView),
            new FrameworkPropertyMetadata("COM5", FrameworkPropertyMetadataOptions.AffectsRender));

        public string Port
        {
            get
            {
                return (string)this.GetValue(PortProperty);
            }
            set
            {
                this.SetValue(PortProperty, value);
            }
        }

        public SimSiasunVCEView()
        {
            InitializeComponent();

            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            if (DataContext == null)
            {
                DataContext = new SimSiasunVCEViewModel(Port);
                (DataContext as TimerViewModelBase).Start();
            }
        }


    }

    class SimSiasunVCEViewModel : SerialPortDeviceViewModel
    {
        public string Title
        {
            get { return "Siasun VCE Simulator"; }
        }

        public string WaferMap
        {
            get { return _sim.SlotMap; }
        }

        public bool IsPresent
        {
            get
            {
                return _sim.IsPresent;
            }
            set
            {
                _sim.IsPresent = value;
            }
        }

        public bool IsProtrude
        {
            get
            {
                return _sim.IsProtrude;
            }
            set
            {
                _sim.IsProtrude = value;
            }
        }

        public bool IsCross
        {
            get
            {
                return _sim.IsCross;
            }
            set
            {
                _sim.IsCross = value;
            }
        }

        public ObservableCollection<WaferItem> WaferList { get; set; }

        public ICommand PlaceCommand { get; set; }
        public ICommand RemoveCommand { get; set; }
         public ICommand ClearCommand { get; set; }
        public ICommand SetAllCommand { get; set; }
        public ICommand RandomCommand { get; set; }
 
        private SimSiasunVCE _sim;


        public SimSiasunVCEViewModel(string port) : base("SimSiasunVCEViewModel")
        {
            PlaceCommand = new DelegateCommand<string>(Place);
            RemoveCommand = new DelegateCommand<string>(Remove);

            ClearCommand = new DelegateCommand<string>(Clear);
            SetAllCommand = new DelegateCommand<string>(SetAll);
            RandomCommand = new DelegateCommand<string>(RandomGenerateWafer);
 
            _sim = new SimSiasunVCE(port);
            Init(_sim);

            WaferList = new ObservableCollection<WaferItem>()
            {
                new WaferItem {Display = "1", Index = 2, State = 3}
            };


        }


        private void RandomGenerateWafer(string obj)
        {
            _sim.RandomWafer();
        }

        private void SetAll(string obj)
        {
            _sim.SetAllWafer();
        }

        private void Clear(string obj)
        {
            _sim.ClearWafer();
        }

        private void Remove(string obj)
        {
            _sim.RemoveCarrier();
        }

        private void Place(string obj)
        {
            _sim.PlaceCarrier();
        }
    }




    public class SimSiasunVCE : SerialPortDeviceSimulator
    {
        public string SlotMap
        {
            get { return string.Join("", _slotMap); }
        }

        private bool _isPresent;
        public bool IsPresent
        {
            get
            {
                return _isPresent;
            }
            set
            {
                _isPresent = value;
 
            }
        }

        public bool IsProtrude
        {
            get;
            set;
        }
        public bool IsCross
        {
            get;
            set;
        }
        
        private string[] _slotMap = new string[25];

        private bool _doorOpen = false;
 
        private string _zAxisPosition = "L";

 
        public SimSiasunVCE(string port) : base(port, -1, "\r", '\r', true)
        {
            IsPresent = true;

            SetAllWafer();
        }

        public void RandomWafer()
        {
            Random _rd = new Random();
            for (int i = 0; i < _slotMap.Length; i++)
            {
                var rtn = _rd.Next(0, 10) % 3;
                switch (rtn)
                {
                    case 0:
                        _slotMap[i] = "O";
                        break;
                    case 1:
                        _slotMap[i] = "S";
                        break;
                    case 2:
                        _slotMap[i] = IsCross ? "C": "S";
                        break;
                    default:
                        break;
                }
            }
        }

        internal void SetAllWafer()
        {
            for (int i = 0; i < _slotMap.Length; i++)
            {
                _slotMap[i] = "S";
            }
        }

        internal void ClearWafer()
        {
            for (int i = 0; i < _slotMap.Length; i++)
            {
                _slotMap[i] = "O";
            }
        }

        internal void RemoveCarrier()
        {
            IsPresent = false;
        }

        internal void PlaceCarrier()
        {
            IsPresent = true;
        }




        protected override void ProcessUnsplitMessage(string message)
        {
            //Command: Q<cr>
            //    Response: XXXXXXX_aaaa_bbbbb_cccc_ddddd < cr >

            string content = message.Remove(message.Length - 1);
            var contentArray = content.Split(',');
 
            if (message.StartsWith("A,LP")) // move to load position
            {
                _zAxisPosition = "L";
            }
            if (message.StartsWith("A,MP")) // MapCassette
            {
                _zAxisPosition = "M";
            }
            if (message.StartsWith("A,HM,ALL")) // HomeAll
            {
                _zAxisPosition = "H";
            }

            if (message.StartsWith("A,") || message.StartsWith("S,") || message.StartsWith("P,"))
            {
                if (message.StartsWith("A,DC"))
                {
 
                }

                if (contentArray[1] == "GO")
                {
                    _zAxisPosition = contentArray[2];
                }

                OnWriteMessage("M,OK\r");
            }
            else if (message.StartsWith("R,"))
            {
                if (message.StartsWith("R,W2"))
                {
                    if (IsPresent)
                    {
                        OnWriteMessage("X,W2Y\r");
                    }
                    else
                    {
                        OnWriteMessage("X,W2N\r");
                    }
                    return;
                }

                if (message.StartsWith("R,MI"))
                {
                    if (IsPresent)
                    {
                        var slot = string.Join("", _slotMap);

                        OnWriteMessage($"X,MI{slot}?????\r");
                    }
                    else
                    {
                        OnWriteMessage($"X,MIOOOOOOOOOOOOOOOOOOOOOOOOO?????\r");
                    }

                    return;
                }

                if (message.StartsWith("R,STAT,ARM"))
                {
   
                    OnWriteMessage($"X,STAT{_zAxisPosition}\r");
                    return;
                }

                if (message.StartsWith("R,STAT,DOOR"))
                {
                    var open = _doorOpen ? "O":"C";

                    OnWriteMessage($"X,DOOR{open}\r");
                    return;
                }

                if (message.StartsWith("R,SO"))
                {
                    var slideout = IsProtrude ? "Y" : "N";

                    OnWriteMessage($"X,SO{slideout}\r");
                    return;
                }

                OnWriteMessage("X,OK\r");
            }
            else
            {
                OnWriteMessage("M,OK\r");
            }

        }

    }


}
