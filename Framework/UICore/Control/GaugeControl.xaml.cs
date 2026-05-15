using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Aitex.Core.UI.ControlDataContext;

namespace Aitex.Core.UI.Control
{
    /// <summary>
    /// GaugeControl.xaml 的交互逻辑
    /// </summary>
    public partial class GaugeControl : UserControl
    {

                public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                "DeviceData", typeof(GaugeItem), typeof(GaugeControl),
                new FrameworkPropertyMetadata(new GaugeItem(), FrameworkPropertyMetadataOptions.AffectsRender));

                public GaugeItem DeviceData
        {
            get
            {
                return (GaugeItem)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }


        public GaugeControl()
        {
            InitializeComponent();
        }
    }
}
