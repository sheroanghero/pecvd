using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using MECF.Framework.Common.CommonData;
using MECF.Framework.UI.Client.ClientBase;
using MECF.Framework.UI.Client.ClientControls.Common;

namespace MECF.Framework.UI.Client.ClientControls.RobotControls.DualWaferRobot2
{
    /// <summary>
    /// DualWaferRobot.xaml 的交互逻辑
    /// </summary>
    public partial class DualWaferRobot : UserControl, INotifyPropertyChanged
    {
        protected int moveTime = 300;

        private const int AnimationTimeout = 3000;
        public const string RobotRetract = "0";
        public const string RobotExtend = "1";
        private string CurrentArmA;
        private string CurrentArmB;

        private string CurrentPosition
        {
            get; set;
        }

        public int MoveTime
        {
            get => moveTime;
            set
            {
                moveTime = value;
            }
        }

        public int RotateAngle
        {
            get { return (int)GetValue(RotateAngleProperty); }
            set { SetValue(RotateAngleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RotateAngel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RotateAngleProperty =
            DependencyProperty.Register("RotateAngel", typeof(int), typeof(DualWaferRobot), new PropertyMetadata(0));

        public Dictionary<string, StationPosition> StationPosition
        {
            get { return (Dictionary<string, StationPosition>)GetValue(StationPositionProperty); }
            set { SetValue(StationPositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StationPosition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StationPositionProperty =
            DependencyProperty.Register("StationPosition", typeof(Dictionary<string, StationPosition>), typeof(DualWaferRobot), new PropertyMetadata(null, StationPositionChangedCallback));

        public RobotMoveInfo RobotMoveInfo
        {
            get { return (RobotMoveInfo)GetValue(RobotMoveInfoProperty); }
            set { SetValue(RobotMoveInfoProperty, value); }
        }

        public static readonly DependencyProperty RobotMoveInfoProperty =
            DependencyProperty.Register("RobotMoveInfo", typeof(RobotMoveInfo), typeof(DualWaferRobot), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public WaferInfo Wafer1
        {
            get { return (WaferInfo)GetValue(Wafer1Property); }
            set { SetValue(Wafer1Property, value); }
        }

        // Using a DependencyProperty as the backing store for Wafer.  This enables animation, styling, binding, etc...
        private static readonly DependencyProperty Wafer1Property =
            DependencyProperty.Register("Wafer1", typeof(WaferInfo), typeof(DualWaferRobot), new PropertyMetadata(null));

        public WaferInfo Wafer2
        {
            get { return (WaferInfo)GetValue(Wafer2Property); }
            set { SetValue(Wafer2Property, value); }
        }

        // Using a DependencyProperty as the backing store for Wafer2.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Wafer2Property =
            DependencyProperty.Register("Wafer2", typeof(WaferInfo), typeof(DualWaferRobot), new PropertyMetadata(null));

        public WaferInfo Wafer3
        {
            get { return (WaferInfo)GetValue(Wafer3Property); }
            set { SetValue(Wafer3Property, value); }
        }

        // Using a DependencyProperty as the backing store for Wafer3.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Wafer3Property =
            DependencyProperty.Register("Wafer3", typeof(WaferInfo), typeof(DualWaferRobot), new PropertyMetadata(null));

        public WaferInfo Wafer4
        {
            get { return (WaferInfo)GetValue(Wafer4Property); }
            set { SetValue(Wafer4Property, value); }
        }

        // Using a DependencyProperty as the backing store for Wafer4.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Wafer4Property =
            DependencyProperty.Register("Wafer4", typeof(WaferInfo), typeof(DualWaferRobot), new PropertyMetadata(null));

        public string SourceName1
        {
            get { return (string)GetValue(SourceName1Property); }
            set { SetValue(SourceName1Property, value); }
        }

        // Using a DependencyProperty as the backing store for SourceName1.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceName1Property =
            DependencyProperty.Register("SourceName1", typeof(string), typeof(DualWaferRobot), new PropertyMetadata(""));


        public string SourceName2
        {
            get { return (string)GetValue(SourceName2Property); }
            set { SetValue(SourceName2Property, value); }
        }

        // Using a DependencyProperty as the backing store for SourceName2.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceName2Property =
            DependencyProperty.Register("SourceName2", typeof(string), typeof(DualWaferRobot), new PropertyMetadata(""));


        public string SourceName3
        {
            get { return (string)GetValue(SourceName3Property); }
            set { SetValue(SourceName3Property, value); }
        }

        // Using a DependencyProperty as the backing store for SourceName3.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceName3Property =
            DependencyProperty.Register("SourceName3", typeof(string), typeof(DualWaferRobot), new PropertyMetadata(""));

        public string SourceName4
        {
            get { return (string)GetValue(SourceName4Property); }
            set { SetValue(SourceName4Property, value); }
        }

        // Using a DependencyProperty as the backing store for SourceName4.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceName4Property =
            DependencyProperty.Register("SourceName4", typeof(string), typeof(DualWaferRobot), new PropertyMetadata(""));


        public string Station
        {
            get { return (string)GetValue(StationProperty); }
            set { SetValue(StationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Station.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StationProperty =
            DependencyProperty.Register("Station", typeof(string), typeof(DualWaferRobot), new FrameworkPropertyMetadata("ArmA.Robot", FrameworkPropertyMetadataOptions.AffectsRender));

        public string ArmAExtended
        {
            get { return (string)GetValue(ArmAExtendedProperty); }
            set { SetValue(ArmAExtendedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ArmAExtended.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ArmAExtendedProperty =
            DependencyProperty.Register("ArmAExtended", typeof(string), typeof(DualWaferRobot), new FrameworkPropertyMetadata(RobotRetract, FrameworkPropertyMetadataOptions.AffectsRender));

        public string ArmBExtended
        {
            get { return (string)GetValue(ArmBExtendedProperty); }
            set { SetValue(ArmBExtendedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ArmBExtended.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ArmBExtendedProperty =
            DependencyProperty.Register("ArmBExtended", typeof(string), typeof(DualWaferRobot), new FrameworkPropertyMetadata(RobotRetract, FrameworkPropertyMetadataOptions.AffectsRender));

        public ICommand CreateDeleteWaferCommand
        {
            get { return (ICommand)GetValue(CreateDeleteWaferCommandProperty); }
            set { SetValue(CreateDeleteWaferCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CreateDeleteWaferCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CreateDeleteWaferCommandProperty =
            DependencyProperty.Register("CreateDeleteWaferCommand", typeof(ICommand), typeof(DualWaferRobot), new PropertyMetadata(null));

        public ICommand MoveWaferCommand
        {
            get { return (ICommand)GetValue(MoveWaferCommandProperty); }
            set { SetValue(MoveWaferCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MoveWaferCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MoveWaferCommandProperty =
            DependencyProperty.Register("MoveWaferCommand", typeof(ICommand), typeof(DualWaferRobot), new PropertyMetadata(null));

        public bool WaferPresentA
        {
            get { return (bool)GetValue(WaferPresentAProperty); }
            set { SetValue(WaferPresentAProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WaferPresent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WaferPresentAProperty =
            DependencyProperty.Register("WaferPresentA", typeof(bool), typeof(DualWaferRobot), new PropertyMetadata(false));

        public bool WaferPresentB
        {
            get { return (bool)GetValue(WaferPresentBProperty); }
            set { SetValue(WaferPresentBProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WaferPresent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WaferPresentBProperty =
            DependencyProperty.Register("WaferPresentB", typeof(bool), typeof(DualWaferRobot), new PropertyMetadata(false));

        private List<MenuItem> menu;

        public event PropertyChangedEventHandler PropertyChanged;

        public List<MenuItem> Menu
        {
            get
            {
                return menu;
            }
            set
            {
                menu = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Menu"));
            }
        }

        private ICommand MoveCommand
        {
            get; set;
        }

        public DualWaferRobot()
        {
            InitializeComponent();
            root.DataContext = this;

            MoveCommand = new RelayCommand(MoveTo);

            canvas1.Rotate(180);
            canvas2.Rotate(-180);
            canvas3.Rotate(90);

            CurrentPosition = "ArmA.Robot";
        }

        static void StationPositionChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (DualWaferRobot)d;
            var positions = (Dictionary<string, StationPosition>)e.NewValue;
            if (self.Menu == null)
            {
                //var menus = new List<MenuItem>();
                //foreach (var position in positions)
                //{
                //    var m = new MenuItem() { Header = position.Key };
                //    Enum.TryParse<RobotArm>(position.Key.Split('.')[0], out RobotArm arm);
                //    m.Items.Add(new MenuItem() { Header = "Pick", Command = self.MoveCommand, CommandParameter = new RobotMoveInfo() { BladeTarget = position.Key, Action = RobotAction.Picking, ArmTarget = arm } });
                //    m.Items.Add(new MenuItem() { Header = "Place", Command = self.MoveCommand, CommandParameter = new RobotMoveInfo() { BladeTarget = position.Key, Action = RobotAction.Placing, ArmTarget = arm } });
                //    m.Items.Add(new MenuItem() { Header = "Move", Command = self.MoveCommand, CommandParameter = new RobotMoveInfo() { BladeTarget = position.Key, Action = RobotAction.Moving, ArmTarget = arm } });
                //    menus.Add(m);
                //}
                //self.Menu = menus;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            //if (Station != null || ArmAExtended != null || ArmBExtended != null)
            //{
            //    LogMsg(string.Format("Target: {0} - {1}  ArmA: {2} ArmB: {3}", CurrentPosition, Station, ArmAExtended, ArmBExtended));

            //    var needMove = CurrentPosition != Station || CurrentArmA != ArmAExtended || CurrentArmB != ArmBExtended;

            //    if (needMove && Station != null)
            //    {
            //        Invoke(() => MoveRobot(Station, ArmAExtended, ArmBExtended));
            //    }
            //}
        }

        private void MoveRobot(string station, string ArmAExtended, string ArmBExtended, Action onComplete = null)
        {
            canvas1.Stop();
            canvas2.Stop();
            canvas3.Stop();

            var target = station;

            MoveToStart(CurrentPosition
                , () => RotateTo(target
                , () => MoveToStart(target
                , () => MoveToEnd(target, ArmAExtended, ArmBExtended, onComplete))));
        }

        private void MoveRobot(RobotMoveInfo moveInfo, Action onComplete = null)
        {
            canvas1.Stop();
            canvas2.Stop();
            canvas3.Stop();

            var target = moveInfo.BladeTarget;

            MoveToStart(CurrentPosition
           , () => RotateTo(target
           , () => MoveToStart(target
           , () =>
           {
               if (moveInfo.Action != RobotAction.Moving) MoveToEnd(target
               , () => UpdateWafer(moveInfo, onComplete));
           }
           )));
        }

        private void MoveToEnd(string station, string ArmAExtended = "", string ArmBExtended = "", Action onComplete = null)
        {
            var position = StationPosition[station];
            var extended = false;

            if (ArmAExtended == RobotExtend || ArmBExtended == RobotExtend)
            {
                canvas1.Rotate(position.EndPosition.Root, true, MoveTime, 0, 0, onComplete);
                canvas2.Rotate(position.EndPosition.Arm, true, MoveTime);
                canvas3.Rotate(position.EndPosition.Hand, true, MoveTime);
                extended = true;
            }

            if (!extended && onComplete != null)
            {
                onComplete();
            }

            CurrentPosition = station;
            CurrentArmA = ArmAExtended;
            CurrentArmB = ArmBExtended;
        }

        private void RotateTo(string station, Action onComplete = null)
        {
            var position = StationPosition[station];
            root.Rotate(position.StartPosition.X, true, MoveTime, 0, 0, onComplete);
        }

        private void MoveToStart(string station, Action onComplete = null)
        {
            if (!StationPosition.ContainsKey(station))
                return;
            var position = StationPosition[station];
            canvas1.Rotate(position.StartPosition.Root, true, MoveTime, 0, 0, onComplete);
            canvas2.Rotate(position.StartPosition.Arm, true, MoveTime);
            canvas3.Rotate(position.StartPosition.Hand, true, MoveTime);
            CurrentPosition = station;
        }

        private void MoveToEnd(string station, Action onComplete = null)
        {
            var position = StationPosition[station];
            canvas1.Rotate(position.EndPosition.Root, true, MoveTime, 0, 0, onComplete);
            canvas2.Rotate(position.EndPosition.Arm, true, MoveTime);
            canvas3.Rotate(position.EndPosition.Hand, true, MoveTime);
            CurrentPosition = station;
        }

        private void UpdateWafer(RobotMoveInfo moveInfo, Action onComplete = null)
        {
            return;
        }

        private void Invoke(Action action)
        {
            Dispatcher.Invoke(action);
        }

        private void LogMsg(string msg)
        {
            var source = "Robot";
            Console.WriteLine("{0} {1}", source, msg);
        }

        private void MoveTo(object target)
        {
            MoveRobot((RobotMoveInfo)target);
        }
    }

    public enum RobotArm
    {
        ArmA,
        ArmB,
        Both
    }
}

