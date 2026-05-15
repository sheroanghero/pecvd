using Aitex.Core.RT.Device;
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MECF.Framework.Simulator.Core.Commons
{
    /// <summary>
    /// SiasunPhoenixBSimulatorView.xaml 的交互逻辑
    /// </summary>
    public partial class CommonSerialPortDeviceSimulatorView : UserControl
    {

        CommonSerialPortSimulatorViewModel _viewModel;

        public CommonSerialPortDeviceSimulatorView()
        {
            InitializeComponent();
        }

        public void Initialize(string deviceGroup, string deviceName, string defaultPort, bool isNewVersion = false)
        {
            _viewModel = new CommonSerialPortSimulatorViewModel(deviceGroup, deviceName, defaultPort, isNewVersion);
            _viewModel.SerialIOSimulator.SimulatorItemActived += SerialIOReader_SimulatorItemActived;
            this.DataContext = _viewModel;
            this.Loaded += CommonSerialPortDeviceSimulatorView_Loaded;
        }

        private void SerialIOReader_SimulatorItemActived(IOSimulatorItemViewModel obj)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                _simaulatorItemGrid.SelectedItem = obj;
            }));

        }

        private void CommonSerialPortDeviceSimulatorView_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.EnableTimer(true);
        }

        private void BtnReply_Click(object sender, RoutedEventArgs e)
        {
            IOSimulatorItemViewModel commandViewModel = (IOSimulatorItemViewModel)_simaulatorItemGrid.SelectedItem;
            if (commandViewModel != null)
            {
                _viewModel.SerialIOSimulator.ManualWriteMessage(commandViewModel);
            }
        }
    }

    class CommonSerialPortSimulatorViewModel : SerialPortDeviceViewModel
    {
        public CommonSerialPortSimulatorViewModel(string deviceGroup, string deviceName, string port, bool isNewVersion) : base("CommonSerialPortSimulatorViewModel")
        {
            SerialIOSimulator = SerialPortDeviceSimulatorFactory.GetCommonSerialPortDeviceSimulator(port, deviceName);
            Init(SerialIOSimulator,true);

            _deviceName = deviceName;

            if (isNewVersion)
                IOSimulatorItemViewModelConfig = IOSimulatorItemReader.GetCommandModelConfigV2(deviceName);
            else
                IOSimulatorItemViewModelConfig = IOSimulatorItemReader.GetCommandModelConfig(deviceGroup, deviceName);

            IOSimulatorItemViewModelConfig.IOSimulatorItemList.ForEach(sItem =>
            {
                sItem.IsManualReplyEnable = !AutoReply;
                sItem.Response = IsFailed ? sItem.FailedResponseStr : sItem.SuccessResponseStr;
            });

            SerialIOSimulator.IOSimulatorItemList = IOSimulatorItemViewModelConfig.IOSimulatorItemList;
        }
        private string _deviceName;
        public IOSimulatorItemViewModelConfig IOSimulatorItemViewModelConfig { get; set; }

        public string Title
        {
            get { return _deviceName + " Simulator"; }
        }

        public CommonSerialPortDeviceSimulator SerialIOSimulator;

        public bool IsFailed
        {
            get
            {
                return SerialIOSimulator.Failed;
            }
            set
            {
                SerialIOSimulator.Failed = value;
                IOSimulatorItemViewModelConfig.IOSimulatorItemList.ForEach(sItem =>
                {
                    sItem.Response = value ? sItem.FailedResponseStr : sItem.SuccessResponseStr;
                });
            }
        }

        public bool AutoReply
        {
            get
            {
                return SerialIOSimulator.AutoReply;
            }
            set
            {
                SerialIOSimulator.AutoReply = value;

                IOSimulatorItemViewModelConfig.IOSimulatorItemList.ForEach(sItem =>
                {
                    sItem.IsManualReplyEnable = !value;
                });
            }
        }


        //private string _value;

        [IgnorePropertyChange]
        public string ResultValue
        {
            get
            {
                return SerialIOSimulator.ResultValue;
            }
            set
            {
                SerialIOSimulator.ResultValue = value;
            }
        }


    }




}
