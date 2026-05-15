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
    /// CognexOcrReaderView.xaml 的交互逻辑
    /// </summary>
    public partial class CognexOcrReaderView : UserControl
    {
        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
            "Port", typeof(int), typeof(CognexOcrReaderView),
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

        public CognexOcrReaderView()
        {
            InitializeComponent();


            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {

                this.DataContext = new CognexOcrReaderViewModel(Port);
            }
            (DataContext as TimerViewModelBase).Start();
        }
    }

    class CognexOcrReaderViewModel : SocketDeviceViewModel
    {
        public string Title
        {
            get { return "Cognex Ocr Reader Simulator"; }
        }

        public CognexOcrReaderViewModel(int port) : base("CognexOcrReaderViewModel")
        {
            Init(new CognexOcrReaderSimulator(port));

        }
    }
 
}

