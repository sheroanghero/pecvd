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
    /// GasValveV3.xaml 的交互逻辑
    /// </summary>
    public partial class GasValveV3 : UserControl
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                "Command", typeof(ICommand), typeof(GasValveV3),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ValveOpenOrientationProperty = DependencyProperty.Register(
            "ValveOpenOrientation", typeof (LineOrientation), typeof (GasValveV3),
            new FrameworkPropertyMetadata(LineOrientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ValveNameProperty = DependencyProperty.Register(
            "ValveName", typeof (string), typeof (GasValveV3),
            new FrameworkPropertyMetadata("Undefined", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsValveOpenProperty = DependencyProperty.Register(
            "IsValveOpen", typeof (bool), typeof (GasValveV3),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

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

        public bool IsValveOpen
        {
            get { return (bool) GetValue(IsValveOpenProperty); }
            set { SetValue(IsValveOpenProperty, value); }
        }


        public LineOrientation ValveOpenOrientation
        {
            get { return (LineOrientation) GetValue(ValveOpenOrientationProperty); }
            set { SetValue(ValveOpenOrientationProperty, value); }
        }

        public string ValveName
        {
            get { return (string) GetValue(ValveNameProperty); }
            set { SetValue(ValveNameProperty, value); }
        }

        public GasValveV3()
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
            imagePicture.Source = GetImage(IsValveOpen ? "ValveOpenHorizontal.png" : "ValveCloseHorizontal.png");


            base.OnRender(drawingContext);
        }

        private void OpenValve(object sender, RoutedEventArgs e)
        {
            if (Command == null)
                return;

             Command.Execute(new object[] { ValveName, GasValveOperation.GVTurnValve, true });
        }

        private void CloseValve(object sender, RoutedEventArgs e)
        {
            if (Command == null)
                return;

            Command.Execute(new object[] { ValveName, GasValveOperation.GVTurnValve, false });

        }
    }
}
