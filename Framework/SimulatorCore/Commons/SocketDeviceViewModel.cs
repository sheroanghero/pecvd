using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Commons
{
    public class SocketDeviceViewModel:TimerViewModelBase
    {
        public ICommand ClearLogCommand { get; set; }
        public ICommand EnableCommand { get; set; }
        public ICommand DisableCommand { get; set; }

        public bool IsEnableEnable
        {
            get { return !_simulator.IsEnabled; }
        }
        public bool IsEnableDisable
        {
            get { return _simulator.IsEnabled; }
        }

        public bool IsConnected
        {
            get { return _simulator.IsConnected; }
        }
 
        public int LocalPort
        {
            get { return _simulator.LocalPort; }
        }
        public string RemoteConnection
        {
            get { return _simulator.RemoteConnection; }
        }

        public string ConnectionStatus
        {
            get
            {
                if (_simulator.IsConnected)
                    return "Connected";

                if (_simulator.IsEnabled)
                    return "Listening";

                return "Disable";
            }
        }

        public string LocalPortSetPoint { get; set; }

        public ObservableCollection<TransactionLogItem> TransactionLogItems { get; set; }

        protected SocketDeviceSimulator _simulator;

        public SocketDeviceViewModel(string name) : base(name)
        {
            ClearLogCommand = new DelegateCommand<string>(ClearLog);
            EnableCommand = new DelegateCommand<string>(Enable);
            DisableCommand = new DelegateCommand<string>(Disable);

            TransactionLogItems = new ObservableCollection<TransactionLogItem>();

            LocalPortSetPoint = "";
        }

        protected void Init(SocketDeviceSimulator sim, bool enable=true)
        {
            LocalPortSetPoint = sim.LocalPort.ToString();

            _simulator = sim;
            _simulator.MessageOut += _simulator_MessageOut;
            _simulator.MessageIn += _simulator_MessageIn;

            if (enable)
                _simulator.Enable();
        }

        private void Disable(string obj)
        {
            _simulator.Disable();
        }

        private void Enable(string obj)
        {
            int port;
            if (int.TryParse(LocalPortSetPoint, out port))
                _simulator.LocalPort = port;

            _simulator.Enable();
        }

        private void ClearLog(string obj)
        {
            TransactionLogItems.Clear();
        }

        private void _simulator_MessageIn(string obj)
        {
            if(!string.IsNullOrEmpty(obj))
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    TransactionLogItems.Add(new TransactionLogItem() { Incoming = obj, OccurTime = DateTime.Now.ToString("HH:mm:ss.fff") });

                }));
            }
        }

        private void _simulator_MessageOut(string obj)
        {
            if (!string.IsNullOrEmpty(obj))
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    TransactionLogItems.Add(new TransactionLogItem() { Outgoing = obj, OccurTime = DateTime.Now.ToString("HH:mm:ss.fff") });

                }));
            }
            
        }

        protected override void Poll()
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {


                    InvokeAllPropertyChanged();

                }));
            }
        }
    }

    public class SimpleSocketDeviceViewModel : TimerViewModelBase
    {
        public ICommand ClearLogCommand { get; set; }
        public ICommand EnableCommand { get; set; }
        public ICommand DisableCommand { get; set; }

        public bool IsEnableEnable
        {
            get { return !_simulator.IsEnabled; }
        }
        public bool IsEnableDisable
        {
            get { return _simulator.IsEnabled; }
        }

        public bool IsConnected
        {
            get { return _simulator.IsConnected; }
        }

        public int LocalPort
        {
            get { return _simulator.LocalPort; }
        }
        //public string RemoteConnection
        //{
        //    get { return _simulator.RemoteConnection; }
        //}

        public string ConnectionStatus
        {
            get
            {
                if (_simulator.IsConnected)
                    return "Connected";

                if (_simulator.IsEnabled)
                    return "Listening";

                return "Disable";
            }
        }

        public string LocalPortSetPoint { get; set; }

        public ObservableCollection<TransactionLogItem> TransactionLogItems { get; set; }

        protected SimpleSocketDeviceSimulator _simulator;

        public SimpleSocketDeviceViewModel(string name) : base(name)
        {
            ClearLogCommand = new DelegateCommand<string>(ClearLog);
            EnableCommand = new DelegateCommand<string>(Enable);
            DisableCommand = new DelegateCommand<string>(Disable);

            TransactionLogItems = new ObservableCollection<TransactionLogItem>();

            LocalPortSetPoint = "";
        }

        protected void Init(SimpleSocketDeviceSimulator sim, bool enable = true)
        {
            LocalPortSetPoint = sim.LocalPort.ToString();

            _simulator = sim;
            _simulator.MessageOut += _simulator_MessageOut;
            _simulator.MessageIn += _simulator_MessageIn;

            if (enable)
                _simulator.Enable();
        }

        private void Disable(string obj)
        {
            _simulator.Disable();
        }

        private void Enable(string obj)
        {
            int port;
            if (int.TryParse(LocalPortSetPoint, out port))
                _simulator.LocalPort = port;

            _simulator.Enable();
        }

        private void ClearLog(string obj)
        {
            TransactionLogItems.Clear();
        }

        private void _simulator_MessageIn(string obj)
        {
            if(!string.IsNullOrEmpty(obj))
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    TransactionLogItems.Add(new TransactionLogItem() { Incoming = obj, OccurTime = DateTime.Now.ToString("HH:mm:ss.fff") });

                }));
            }
        }

        private void _simulator_MessageOut(string obj)
        {
            if (!string.IsNullOrEmpty(obj))
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    TransactionLogItems.Add(new TransactionLogItem() { Outgoing = obj, OccurTime = DateTime.Now.ToString("HH:mm:ss.fff") });

                }));
            }
        }

        protected override void Poll()
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {


                    InvokeAllPropertyChanged();

                }));
            }
        }
    }
}
