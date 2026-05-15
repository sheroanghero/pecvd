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
    /// Interaction logic for PressureTransducer.xaml
    /// tag为指定的命令
    /// </summary>
    public partial class PressureTransducer : UserControl
    {
        public PressureTransducer()
        {
            InitializeComponent();
        }

        //public static readonly DependencyProperty PtNameProperty = DependencyProperty.Register(
        //    "PtName", typeof(object), typeof(PressureTransducer),
        //    new FrameworkPropertyMetadata("PT", FrameworkPropertyMetadataOptions.AffectsRender));

        //public static readonly DependencyProperty CheckOkProperty = DependencyProperty.Register(
        //    "CheckOk", typeof(object), typeof(PressureTransducer),
        //    new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        //public static readonly DependencyProperty DPDataProperty = DependencyProperty.Register(
        //    "DPData", typeof(object), typeof(PressureTransducer),
        //    new FrameworkPropertyMetadata("DPT", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(ICommand), typeof(PressureTransducer),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public ICommand Command
        {
            get
            {
                return GetValue(CommandProperty) as ICommand;
            }
            set
            {
                SetValue(CommandProperty, value);
            }
        }

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
                "DeviceData", typeof(PressureTransducerDataItem), typeof(PressureTransducer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        //            SetBinding(PocketRawDataChart.PocketInfoProperty, new Binding("PocketInfo") { });
        //    SetBinding(PocketRawDataChart.SetDummyCommandProperty, new Binding("SetDummyCommand") { });

        public PressureTransducerDataItem DeviceData
        {
            get
            {
                return (PressureTransducerDataItem)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }



        //bool innerEnableClick = true;
        //public bool EnableClick
        //{
        //    get { return innerEnableClick; }
        //    set { innerEnableClick = value; }
        //}

        /// <summary>
        /// render override
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DeviceData != null)
            {
                this.ToolTip = "当前状态:" + (DeviceData.IsEnable ? "打开" : "关闭");
                this.label1.Content = DeviceData.Value.ToString("f1");
                this.EllipsePT.Fill = DeviceData.IsEnable ? Brushes.Green : Brushes.MediumSlateBlue;
            }

        }

        private void TurnOnValve(object sender, RoutedEventArgs e)
        {
            Execute(true);
        }

        private void TurnOffValve(object sender, RoutedEventArgs e)
        {
                Execute(false);
        }

        private void Execute(bool value)
        {
            if (Command != null)
                Command.Execute(new object[] { DeviceData.DeviceName, PressureTransducerOperation.DPEnable, value });
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DeviceData == null)
                return;

            //if (!EnableClick) return;
            //bool tempcheck = false;
            //bool.TryParse(CheckOk + "", out tempcheck);
            ContextMenu mouseClickMenu = new ContextMenu();
            MenuItem item = new MenuItem();
            item.Header = DeviceData.DisplayName;
            item.Background = Brushes.Gray;
            item.Foreground = Brushes.White;
            item.IsEnabled = false;
            mouseClickMenu.Items.Add(item);
            if (DeviceData.IsEnable)
                CreateMenuItem("关闭", TurnOffValve, mouseClickMenu);
            else
                CreateMenuItem("打开", TurnOnValve, mouseClickMenu);
        }

        void CreateMenuItem(string headerName, RoutedEventHandler clickfunc, ContextMenu mouseClickMenu)
        {
            MenuItem item = new MenuItem();
            item.Header = headerName;
            item.Click += clickfunc;
            item.Tag = this.Tag;
            mouseClickMenu.Items.Add(item);
            mouseClickMenu.IsOpen = true;
        }
    }
}
