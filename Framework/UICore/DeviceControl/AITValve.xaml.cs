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
    public partial class AITValve : UserControl
    {
        // define dependency properties
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                        "Command", typeof(ICommand), typeof(AITValve),
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

        public Visibility MenuVisibility
        {
            get { return (Visibility)this.GetValue(MenuVisibilityProperty); }
            set { this.SetValue(MenuVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MenuVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MenuVisibilityProperty =
            DependencyProperty.Register("MenuVisibility", typeof(Visibility), typeof(AITValve), 
                new FrameworkPropertyMetadata(Visibility.Visible));


        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                        "DeviceData", typeof(AITValveData), typeof(AITValve),
                        new FrameworkPropertyMetadata(new AITValveData(), FrameworkPropertyMetadataOptions.AffectsRender));
        public AITValveData DeviceData
        {
            get
            {
                return (AITValveData)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }

        public static readonly DependencyProperty EnableServiceControlProperty = DependencyProperty.Register(
            "EnableServiceControl", typeof(bool), typeof(AITValve),
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
                "DeviceData2", typeof(AITValveData), typeof(AITValve),
                new FrameworkPropertyMetadata(new AITValveData(), FrameworkPropertyMetadataOptions.AffectsRender));
        public AITValveData DeviceData2
        {
            get
            {
                return (AITValveData)this.GetValue(DeviceData2Property);
            }
            set
            {
                this.SetValue(DeviceData2Property, value);
            }
        }


        public static readonly DependencyProperty ValveOpenOrientationProperty = DependencyProperty.Register(
    "ValveOpenOrientation", typeof(LineOrientation), typeof(AITValve),
    new FrameworkPropertyMetadata(LineOrientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsRender));
        public LineOrientation ValveOpenOrientation
        {
            get { return (LineOrientation)GetValue(ValveOpenOrientationProperty); }
            set { SetValue(ValveOpenOrientationProperty, value); }
        }

        public AITValve()
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

            bool IsOpen = DeviceData == null || DeviceData.IsOpen;

            IsOpen = IsOpen && (DeviceData2 == null || !DeviceData2.IsOpen);

            imagePicture.Source = GetImage(IsOpen ? "ValveOpenHorizontal.png" : "ValveCloseHorizontal.png");
            imagePicture.ContextMenu.Visibility = MenuVisibility;

            if (DeviceData != null)
            {
                this.ToolTip =
                    $"Valve Name：{DeviceData.DisplayName} \r\n Device ID：{DeviceData.DeviceSchematicId} \r\n SetPoint：{DeviceData.SetPoint}  \r\n Status：{DeviceData.Feedback}";
            }

            base.OnRender(drawingContext);
        }

        private void OpenValve(object sender, RoutedEventArgs e)
        {
            if (EnableServiceControl)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData.UniqueName}.{AITValveOperation.GVTurnValve}", true);
                return;
            }

            if (Command == null)
                return;

            Command.Execute(new object[] { DeviceData.DeviceName, AITValveOperation.GVTurnValve, true });


            if (DeviceData2 != null && !string.IsNullOrEmpty(DeviceData2.DeviceName))
            {
                Command.Execute(new object[] { DeviceData2.DeviceName, AITValveOperation.GVTurnValve, false });
            }

        }

        private void CloseValve(object sender, RoutedEventArgs e)
        {
            if (EnableServiceControl)
            {
                InvokeClient.Instance.Service.DoOperation($"{DeviceData.UniqueName}.{AITValveOperation.GVTurnValve}", false);
                return;
            }

            if (Command == null)
                return;

            Command.Execute(new object[] { DeviceData.DeviceName, AITValveOperation.GVTurnValve, false });

            if (DeviceData2 != null && !string.IsNullOrEmpty(DeviceData2.DeviceName))
            {
                Command.Execute(new object[] { DeviceData2.DeviceName, AITValveOperation.GVTurnValve, true });
            }
        }
    }
}
