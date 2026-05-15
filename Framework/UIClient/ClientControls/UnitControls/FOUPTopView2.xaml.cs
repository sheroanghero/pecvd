using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.UI.Client.ClientControls.UnitControls
{
    /// <summary>
    /// FOUPTopView.xaml 的交互逻辑
    /// </summary>
    public partial class FOUPTopView2 : UserControl
    {
        public FOUPTopView2()
        {
            InitializeComponent();
            root.DataContext = this;
        }

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Label.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(FOUPTopView2), new PropertyMetadata(null));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(FOUPTopView2), new PropertyMetadata(null));

        public LoadportCassetteState CassetteState
        {
            get { return (LoadportCassetteState)GetValue(CassetteStateProperty); }
            set { SetValue(CassetteStateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Present.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CassetteStateProperty =
            DependencyProperty.Register("CassetteState", typeof(LoadportCassetteState), typeof(FOUPTopView2), new FrameworkPropertyMetadata(LoadportCassetteState.Unknown));

        public FoupDoorState DoorState
        {
            get { return (FoupDoorState)GetValue(DoorStateProperty); }
            set { SetValue(DoorStateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Open.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DoorStateProperty =
            DependencyProperty.Register("DoorState", typeof(FoupDoorState), typeof(FOUPTopView2), new FrameworkPropertyMetadata(FoupDoorState.Unknown));  //FrameworkPropertyMetadataOptions.AffectsRender

        public ModuleName Station
        {
            get { return (ModuleName)GetValue(StationProperty); }
            set { SetValue(StationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Station.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StationProperty =
            DependencyProperty.Register("Station", typeof(ModuleName), typeof(FOUPTopView2), new PropertyMetadata(ModuleName.System));     

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var cmd = ((MenuItem)sender).Tag;

            Command.Execute(new[] { Station.ToString(), cmd });
        }

        //protected override void OnRender(DrawingContext drawingContext)
        //{
        //    base.OnRender(drawingContext);
        //    if (DoorState == FoupDoorState.Open)
        //    {
        //        // cassette.VerticalAlignment = VerticalAlignment.Top;
        //    }
        //    else if (DoorState == FoupDoorState.Close)
        //    {
        //        //cassette.VerticalAlignment = VerticalAlignment.Bottom;
        //    }
        //}
    }
}
