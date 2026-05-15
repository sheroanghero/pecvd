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
    /// Interaction logic for VacuumPump.xaml
    /// </summary>
    public partial class VacuumPump : UserControl
    {
        public VacuumPump()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsOpenMenuProperty = DependencyProperty.Register(
           "IsOpenMenu", typeof(bool), typeof(VacuumPump), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PumpDataProperty = DependencyProperty.Register(
            "PumpData", typeof(PumpDataItem), typeof(VacuumPump), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        "Command", typeof(ICommand), typeof(VacuumPump),
         new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public PumpDataItem PumpData
        {
            get
            {
                return (PumpDataItem)GetValue(PumpDataProperty);
            }
            set
            {
                SetValue(PumpDataProperty, value);
            }
        }

        public ICommand Command
        {
            get
            {
                return (ICommand)GetValue(CommandProperty);
            }
            set
            {
                SetValue(CommandProperty, value);
            }
        }

        public bool IsOpenMenu
        {
            get
            {
                return (bool)GetValue(IsOpenMenuProperty);
            }
            set
            {
                SetValue(IsOpenMenuProperty, value);
            }
        }

        /// <summary>
        /// render override
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (PumpData != null)
            {
                if (PumpData.MainPumpEnable)
                {
                    EllipseInCircle.Fill = PumpData.IsWarning ? Brushes.Yellow : Brushes.Green;
                }
                else if (!PumpData.MainPumpEnable)
                    EllipseInCircle.Fill = Brushes.Red;

                else
                    EllipseInCircle.Fill = Brushes.Gray;

                this.ToolTip = PumpData.DisplayName;
            }
            else
                EllipseInCircle.Fill = Brushes.Gray;

            
           
        }

        private void canvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(!IsOpenMenu)
            { return; }

            ContextMenu mouseClickMenu = new ContextMenu();
            MenuItem item = new MenuItem();
            item.Header = "_" + (PumpData!=null ? PumpData.DisplayName : "未知主泵");
            item.Background = Brushes.Gray;
            item.Foreground = Brushes.White;
            item.IsEnabled = false;
            mouseClickMenu.Items.Add(item);

            if (PumpData!=null)
            {
                if (!PumpData.MainPumpEnable)
                { addOpenMenu(mouseClickMenu, item); }
                else
                { addCloseMenu(mouseClickMenu, item); }
            }

            mouseClickMenu.IsOpen = true;
        }

        void addOpenMenu(ContextMenu mouseClickMenu, MenuItem item)
        {
            item = new MenuItem();
            item.Header = "打开 (_O)";
            item.Click += TurnOnValve;
            item.Tag = this.Tag;
            mouseClickMenu.Items.Add(item);
        }
        void addCloseMenu(ContextMenu mouseClickMenu, MenuItem item)
        {
            item = new MenuItem();
            item.Header = "关闭 (_C)";
            item.Tag = this.Tag;
            item.Click += TurnOffValve;
            mouseClickMenu.Items.Add(item);
        }

        private void TurnOnValve(object sender, RoutedEventArgs e)
        {
            if (Command != null)
            {
                Command.Execute(new object[] { PumpData.DeviceName, PumpOperation.PumpON });
            }
        }

        private void TurnOffValve(object sender, RoutedEventArgs e)
        {
            if (Command != null)
            {
                Command.Execute(new object[] { PumpData.DeviceName, PumpOperation.PumpOff });
            }
        }
    }
}
