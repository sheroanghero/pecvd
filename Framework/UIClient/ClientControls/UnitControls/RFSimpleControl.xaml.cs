using Aitex.Core.Common.DeviceData;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    /// <summary>
    /// RFSimpleControl.xaml 的交互逻辑
    /// </summary>
    public partial class RFSimpleControl : UserControl
    {
        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
            "DeviceData", typeof(AITRfData), typeof(RFSimpleControl),
            new FrameworkPropertyMetadata(new AITRfData(), FrameworkPropertyMetadataOptions.AffectsRender));
        public AITRfData DeviceData
        {
            get
            {
                return (AITRfData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }

        public RFSimpleControl()
        {
            InitializeComponent();
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DeviceData == null)
                return;

            if (DeviceData.IsRfOn)
                imagePicture.Source = new BitmapImage(new Uri("pack://application:,,,/MECF.Framework.UI.Client;component/Resources/Images/units/rfOn.png"));
            else
                imagePicture.Source = new BitmapImage(new Uri("pack://application:,,,/MECF.Framework.UI.Client;component/Resources/Images/units/rfOff.png"));


        }
    }
}
