using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Aitex.Core.UI.MVVM;
using MECF.Framework.Simulator.Core.Commons;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Aligners
{
    /// <summary>
    /// HstOcrReaderView.xaml 的交互逻辑
    /// </summary>
    public partial class HstOcrReaderView : UserControl
    {
        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
            "Port", typeof(int), typeof(HstOcrReaderView),
            new FrameworkPropertyMetadata(23, FrameworkPropertyMetadataOptions.AffectsRender));

        public int Port
        {
            get
            {
                return (int)this.GetValue(PortProperty);
            }
            set
            {
                this.SetValue(PortProperty, value);
            }
        }

        public HstOcrReaderView()
        {
            InitializeComponent();


            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {

                this.DataContext = new HstOcrReaderViewModel(Port);
            }
            (DataContext as TimerViewModelBase).Start();
        }
    }

    class HstOcrReaderViewModel : SocketDeviceViewModel
    {
        public string Title
        {
            get { return "Hst Ocr Reader Simulator"; }
        }

        public HstOcrReaderViewModel(int port) : base("HstOcrReaderViewModel")
        {
            Init(new HstOcrReaderSimulator(port));

        }
    }
 
}

