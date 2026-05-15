using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MECF.Framework.Common.CommonData;
using MECF.Framework.UI.Client.ClientControls.Common;

namespace MECF.Framework.UI.Client.ClientControls.RobotControls.SingleArmRobot1
{
    /// <summary>
    /// SingleArmRobot.xaml 的交互逻辑
    /// </summary>
    public partial class SingleArmRobot : UserControl, INotifyPropertyChanged
    {
        protected int MoveTime = 1000;
        private const int AnimationTimeout = 3000; // seconds

        public int RotateAngle
        {
            get { return (int)GetValue(RotateAngleProperty); }
            set { SetValue(RotateAngleProperty, value); }
        }

        public ICommand CreateDeleteWaferCommand
        {
            get { return (ICommand)GetValue(CreateDeleteWaferCommandProperty); }
            set { SetValue(CreateDeleteWaferCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CreateDeleteWaferCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CreateDeleteWaferCommandProperty =
            DependencyProperty.Register("CreateDeleteWaferCommand", typeof(ICommand), typeof(SingleArmRobot), new PropertyMetadata(null));

        public ICommand MoveWaferCommand
        {
            get { return (ICommand)GetValue(MoveWaferCommandProperty); }
            set { SetValue(MoveWaferCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MoveWaferCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MoveWaferCommandProperty =
            DependencyProperty.Register("MoveWaferCommand", typeof(ICommand), typeof(SingleArmRobot), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for RotateAngel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RotateAngleProperty =
            DependencyProperty.Register("RotateAngel", typeof(int), typeof(SingleArmRobot), new PropertyMetadata(0));

        public string Station
        {
            get { return (string)GetValue(StationProperty); }
            set { SetValue(StationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Station.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StationProperty =
            DependencyProperty.Register("Station", typeof(string), typeof(SingleArmRobot), new PropertyMetadata("TMRobot"));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(SingleArmRobot), new PropertyMetadata(null));

        public RobotMoveInfo RobotMoveInfo
        {
            get { return (RobotMoveInfo)GetValue(RobotMoveInfoProperty); }
            set { SetValue(RobotMoveInfoProperty, value); }
        }

        public static readonly DependencyProperty RobotMoveInfoProperty =
            DependencyProperty.Register("RobotMoveInfo", typeof(RobotMoveInfo), typeof(SingleArmRobot), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public VTMRobotPosition StationPosition
        {
            get { return (VTMRobotPosition)GetValue(StationPositionProperty); }
            set { SetValue(StationPositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StationPosition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StationPositionProperty =
            DependencyProperty.Register("StationPosition", typeof(VTMRobotPosition), typeof(SingleArmRobot), new PropertyMetadata(null, PropertyChangedCallback));


        public MECF.Framework.UI.Client.ClientBase.WaferInfo Wafer1
        {
            get { return (MECF.Framework.UI.Client.ClientBase.WaferInfo)GetValue(Wafer1Property); }
            set { SetValue(Wafer1Property, value); }
        }

        // Using a DependencyProperty as the backing store for Wafer1.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Wafer1Property =
            DependencyProperty.Register("Wafer1", typeof(MECF.Framework.UI.Client.ClientBase.WaferInfo), typeof(SingleArmRobot), new PropertyMetadata(null));

        public MECF.Framework.UI.Client.ClientBase.WaferInfo Wafer2
        {
            get { return (MECF.Framework.UI.Client.ClientBase.WaferInfo)GetValue(Wafer2Property); }
            set { SetValue(Wafer2Property, value); }
        }

        // Using a DependencyProperty as the backing store for Wafer2.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Wafer2Property =
            DependencyProperty.Register("Wafer2", typeof(MECF.Framework.UI.Client.ClientBase.WaferInfo), typeof(SingleArmRobot), new PropertyMetadata(null));

        public bool WaferPresentA
        {
            get { return (bool)GetValue(WaferPresentAProperty); }
            set { SetValue(WaferPresentAProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WaferPresent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WaferPresentAProperty =
            DependencyProperty.Register("WaferPresentA", typeof(bool), typeof(SingleArmRobot), new PropertyMetadata(false));

        public bool WaferPresentB
        {
            get { return (bool)GetValue(WaferPresentBProperty); }
            set { SetValue(WaferPresentBProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WaferPresent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WaferPresentBProperty =
            DependencyProperty.Register("WaferPresentB", typeof(bool), typeof(SingleArmRobot), new PropertyMetadata(false));


        private string CurrentPosition;

        private RobotAction CurrentAction
        {
            get; set;
        }

        private List<MenuItem> menu;
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

        public ICommand MoveCommand
        {
            get
            {
                return new RelayCommand(MoveTo);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (SingleArmRobot)d;
            switch (e.Property.Name)
            {
                case "StationPosition":
                    if (e.NewValue != null)
                    {
                        var positions = ((VTMRobotPosition)e.NewValue).Rotations;
                        var menus = new List<MenuItem>();
                        foreach (var position in positions)
                        {
                            var m = new MenuItem() { Header = position.Key };
                            Enum.TryParse(position.Key.Split('.')[0], out RobotArm arm);
                            m.Items.Add(new MenuItem() { Header = "Pick", Command = self.MoveCommand, CommandParameter = new RobotMoveInfo() { BladeTarget = position.Key, Action = RobotAction.Picking, ArmTarget = arm } });
                            m.Items.Add(new MenuItem() { Header = "Place", Command = self.MoveCommand, CommandParameter = new RobotMoveInfo() { BladeTarget = position.Key, Action = RobotAction.Placing, ArmTarget = arm } });
                            m.Items.Add(new MenuItem() { Header = "Move", Command = self.MoveCommand, CommandParameter = new RobotMoveInfo() { BladeTarget = position.Key, Action = RobotAction.Moving, ArmTarget = arm } });
                            menus.Add(m);
                        }
                        //self.Menu = menus;
                        //self.MoveTo(new RobotMoveInfo() { BladeTarget = positions.First().Key, Action = RobotAction.None });
                    }
                    break;
                default:
                    break;
            }
        }

        public SingleArmRobot()
        {
#if DEBUG
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
#endif
            InitializeComponent();
            root.DataContext = this;

            StationPosition = new VTMRobotPosition()
            {
                Rotations = new Dictionary<string, int>()
                {
                    { "ArmA.System", 0},
                    { "ArmA.PM1", 117},
                    { "ArmA.PM2", 180},
                    { "ArmA.PM3", 238},
                    { "ArmA.PA1", 50},
                    { "ArmA.PA2", 305},
                    { "ArmA.VCE1", 25},
                    { "ArmA.VCE2", 333},
                    { "ArmB.PM1", -90},
                    { "ArmB.PM2", -45},
                    { "ArmB.PM3", 0},
                    { "ArmB.PM4", 45},
                    { "ArmB.PM5", 90},
                    { "ArmB.PA1", -153},
                    { "ArmB.PA2", 148},
                    { "ArmB.VCE1",  -158},
                    { "ArmB.VCE2", 153},
                },
                Home = new int[8] { 180, 165, -77, 0, -165, 194, 77, 165 },
                Arm1Extend = new int[8] { 100, 180, -12, 80, -180, 340, 12, 20 },
                Arm2Extend = new int[8] { 255, 26, -8, -76, -30, 180, 20, 180 },
                Arm3Extend = new int[8] { 140, 180, -47, 40, -180, 265, 47, 95 },
                Arm4Extend = new int[8] { 255, 26, -10, -76, -30, 180, 20, 180 },
            };

            RotateCanvas(StationPosition.Home);

            Loaded += (s, e) =>
            {
                if (StationPosition != null)
                {
                    RotateCanvas(StationPosition.Home);
                }
            };

            CurrentPosition = "ArmA.System";
        }

        private void RotateCanvas(int[] angles)
        {
            //canvas1.Rotate(angles[0]);
            //canvas112.Rotate(angles[1]);
            //canvas113.Rotate(angles[2]);
            //canvas2.Rotate(angles[3]);
            //canvas21.Rotate(angles[4]);
            //canvas122.Rotate(angles[5]);
            //canvas123.Rotate(angles[6]);
            //canvas22.Rotate(angles[7]);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            if (RobotMoveInfo != null)
            {


                var needMove = CurrentPosition != RobotMoveInfo.BladeTarget || CurrentAction != RobotMoveInfo.Action;

                if (needMove)
                {
                    LogMsg($" RobotMoveInfo, action:{RobotMoveInfo.Action}  armTarget:{RobotMoveInfo.ArmTarget} bladeTarget:{RobotMoveInfo.BladeTarget}");

                    Invoke(() => MoveRobot(RobotMoveInfo));

                    CurrentAction = RobotMoveInfo.Action;
                    CurrentPosition = RobotMoveInfo.BladeTarget;
                }
            }
        }


        private void MoveRobot(RobotMoveInfo moveInfo)
        {
            var updateWafer = new Action(() => UpdateWafer(moveInfo));

            canvas1.Stop();
            //canvas112.Stop();
            //canvas113.Stop();
            //canvas2.Stop();
            //canvas21.Stop();
            //canvas122.Stop();
            //canvas123.Stop();
            //canvas22.Stop();

            var target = moveInfo.BladeTarget;
            var arm = moveInfo.ArmTarget;

            Actoin(StationPosition.Home, () => Rotate(moveInfo.BladeTarget, () =>
            {
                var moved = false;
                if (moveInfo.ArmTarget == RobotArm.ArmA)
                {
                    switch (moveInfo.Action)
                    {
                        case RobotAction.None:
                            break;
                        case RobotAction.Picking:
                        case RobotAction.Placing:
                            if (target.Split('.').Count() > 1)
                            {
                                var pos = target.Split('.')[1];
                                if (pos == "PA1" || pos == "PA2" || pos.Contains("Aligner"))
                                { Actoin(StationPosition.Arm3Extend, updateWafer); }
                                else
                                {
                                    Actoin(StationPosition.Arm1Extend, updateWafer);
                                }
                            }
                            moved = true;
                            break;
                        case RobotAction.Moving:
                            break;
                        default:
                            break;
                    }
                }
                else if (moveInfo.ArmTarget == RobotArm.ArmB)
                {
                    switch (moveInfo.Action)
                    {
                        case RobotAction.None:
                            break;
                        case RobotAction.Picking:
                        case RobotAction.Placing:
                            if (target.Split('.').Count() > 1)
                            {
                                var pos = target.Split('.')[1];
                                if (pos == "PA1" || pos == "PA2" || pos.Contains("Aligner"))
                                {
                                    Actoin(StationPosition.Arm4Extend, updateWafer);
                                }
                                else
                                {
                                    Actoin(StationPosition.Arm2Extend, updateWafer);
                                }
                            }
                            moved = true;
                            break;
                        case RobotAction.Moving:
                            break;
                        default:
                            break;
                    }
                }

                if (!moved && updateWafer != null)
                {
                    updateWafer();
                }
            }));
        }

        private void Actoin(int[] angles, Action onComplete = null)
        {
            //var storyboard = new Storyboard();
            //storyboard.Completed += (s, e) => onComplete?.Invoke();
            //var needRotate = new List<bool>
            //{
            //    canvas1.Rotate(storyboard, angles[0], true, MoveTime),
            //    canvas112.Rotate(storyboard,angles[1], true, MoveTime),
            //    canvas113.Rotate(storyboard, angles[2], true, MoveTime),
            //    canvas2.Rotate(storyboard, angles[3], true, MoveTime),
            //    canvas21.Rotate(storyboard, angles[4], true, MoveTime),
            //    canvas122.Rotate(storyboard, angles[5], true, MoveTime),
            //    canvas123.Rotate(storyboard, angles[6], true, MoveTime),
            //    canvas22.Rotate(storyboard, angles[7], true, MoveTime)
            //};

            //if (needRotate.Any(x => x))
            //{
            //    storyboard.Begin();
            //}
            //else
            //{
            //    onComplete?.Invoke();
            //}
        }

        private void Rotate(string station, Action onComplete = null)
        {
            var angle = StationPosition.Rotations[station];
            RotateAngle = angle;
            RotateTo(onComplete);
        }

        private void MoveTo(object obj)
        {
            MoveRobot((RobotMoveInfo)obj);
        }

        private void RotateTo(Action onComplete)
        {
            //if (rotate.Angle != RotateAngle)
            //{
            //    //if (Math.Abs(RotateAngle) > Math.Abs(RotateAngle - 360))
            //    //    RotateAngle = 360 - RotateAngle;
            //    if (Math.Abs(rotate.Angle - RotateAngle) > 180)
            //    {
            //        if (rotate.Angle > RotateAngle)
            //        {
            //            RotateAngle = RotateAngle + 360;
            //        }
            //        else
            //        {
            //            RotateAngle = RotateAngle - 360;

            //        }
            //    }
            //    var animation = new DoubleAnimation(rotate.Angle, RotateAngle, new Duration(TimeSpan.FromMilliseconds(MoveTime)));
            //    animation.Completed += (s, e) => onComplete?.Invoke();
            //    rotate.BeginAnimation(RotateTransform.AngleProperty, animation);
            //}
            //else
            //{
            //    onComplete?.Invoke();
            //}
        }

        private void UpdateWafer(RobotMoveInfo moveInfo)
        {
            var waferPresent = false;
            switch (moveInfo.Action)
            {
                case RobotAction.None:
                case RobotAction.Moving:
                    return;
                case RobotAction.Picking:
                    waferPresent = true;
                    break;
                case RobotAction.Placing:
                    waferPresent = false;
                    break;
                default:
                    break;
            }

            switch (moveInfo.ArmTarget)
            {
                case RobotArm.ArmA:
                    WaferPresentA = waferPresent;
                    break;
                case RobotArm.ArmB:
                    WaferPresentB = waferPresent;
                    break;
                case RobotArm.Both:
                    WaferPresentA = waferPresent;
                    WaferPresentB = waferPresent;
                    break;
                default:
                    break;
            }
        }

        private void Invoke(Action action)
        {
            Dispatcher.Invoke(action);
        }

        private void LogMsg(string msg)
        {
            var source = "VTMRobot";
            Console.WriteLine("{0} {1}", source, msg);
        }
    }

    public class VTMRobotPosition
    {
        public Dictionary<string, int> Rotations;
        public int[] Arm1Extend;
        public int[] Arm2Extend;
        public int[] Arm3Extend;
        public int[] Arm4Extend;
        public int[] Home;
    }
}
