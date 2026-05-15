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
using Aitex.Core.Common.DeviceData;
using Aitex.Core.UI.Control;
using MECF.Framework.Common.OperationCenter;

namespace Aitex.Core.UI.DeviceControl
{
    /// <summary>
    /// TexGasValve.xaml 的交互逻辑
    /// </summary>
    public partial class AITIsoGasValve : UserControl
    {
        // define dependency properties
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                        "Command", typeof(ICommand), typeof(AITIsoGasValve),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public ICommand Command
        {
            get
            {
                return (ICommand)this.GetValue(CommandProperty);
            }
            set
            {
                this.SetValue(CommandProperty, value);
            }
        }



        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                        "DeviceData", typeof(AITCylinderData), typeof(AITIsoGasValve),
                        new FrameworkPropertyMetadata(new AITCylinderData(), FrameworkPropertyMetadataOptions.AffectsRender));
        public AITCylinderData DeviceData
        {
            get
            {
                return (AITCylinderData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }

        public static readonly DependencyProperty EnableServiceControlProperty = DependencyProperty.Register(
            "EnableServiceControl", typeof(bool), typeof(AITIsoGasValve),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
        public bool EnableServiceControl
        {
            get
            {
                return (bool)this.GetValue(EnableServiceControlProperty);
            }
            set
            {
                this.SetValue(EnableServiceControlProperty, value);
            }
        }

        public static readonly DependencyProperty DeviceData2Property = DependencyProperty.Register(
                "DeviceData2", typeof(AITCylinderData), typeof(AITIsoGasValve),
                new FrameworkPropertyMetadata(new AITCylinderData(), FrameworkPropertyMetadataOptions.AffectsRender));
        public AITCylinderData DeviceData2
        {
            get
            {
                return (AITCylinderData)this.GetValue(DeviceData2Property);
            }
            set
            {
                this.SetValue(DeviceData2Property, value);
            }
        }


        public static readonly DependencyProperty ValveOpenOrientationProperty = DependencyProperty.Register(
    "ValveOpenOrientation", typeof(LineOrientation), typeof(AITIsoGasValve),
    new FrameworkPropertyMetadata(LineOrientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsRender));
        public LineOrientation ValveOpenOrientation
        {
            get { return (LineOrientation)GetValue(ValveOpenOrientationProperty); }
            set { SetValue(ValveOpenOrientationProperty, value); }
        }

        public AITIsoGasValve()
        {
            InitializeComponent();
        }

        private BitmapSource GetImage(string name)
        {
            return
                new BitmapImage(new Uri(string.Format("pack://application:,,,/MECF.Framework.Common;component/Resources/Valve/{0}", name)));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            rotateTransform.Angle = ValveOpenOrientation == LineOrientation.Horizontal ? 0 : 90;

            bool IsOpen = DeviceData == null || (DeviceData.OpenSetPoint && !DeviceData.CloseSetPoint);

 
            imagePicture.Source = GetImage(IsOpen ? "ValveOpenHorizontal.png" : "ValveCloseHorizontal.png");

            if (DeviceData != null)
            {
                this.ToolTip =
                    $"Valve Name：{DeviceData.DisplayName} \r\n Device ID：{DeviceData.DeviceSchematicId} \r\n SetPoint：{DeviceData.StringStatus}  \r\n Status：{DeviceData.StringStatus}";
            }

            base.OnRender(drawingContext);
        }

        private void OpenValve(object sender, RoutedEventArgs e)
        {
            if (EnableServiceControl)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITCylinderOperation.Open}" );
                return;
            }

            if (Command == null)
                return;

            Command.Execute(new object[] { DeviceData.DeviceName, AITCylinderOperation.Open });


            if (DeviceData2 != null && !string.IsNullOrEmpty(DeviceData2.DeviceName))
            {
                Command.Execute(new object[] { DeviceData2.DeviceName, AITCylinderOperation.Open });
            }

        }

        private void CloseValve(object sender, RoutedEventArgs e)
        {
            if (EnableServiceControl)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData.Module}.{DeviceData.DeviceName}.{AITCylinderOperation.Close}" );
                return;
            }

            if (Command == null)
                return;

            Command.Execute(new object[] { DeviceData.DeviceName, AITCylinderOperation.Close });

            if (DeviceData2 != null && !string.IsNullOrEmpty(DeviceData2.DeviceName))
            {
                Command.Execute(new object[] { DeviceData2.DeviceName, AITCylinderOperation.Close });
            }
        }
    }
}
