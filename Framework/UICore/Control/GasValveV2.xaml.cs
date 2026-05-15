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
using Aitex.Core.Util;
using Aitex.Core.RT.Log;
using Aitex.Core.UI.ControlDataContext;

namespace Aitex.Core.UI.Control
{
        /// <summary>
    /// Interaction logic for CoupleValve_V2.xaml
    /// 双向阀,一开一关，默认开者状态为主，如果说阀关闭则指的是主阀关闭，阀开指的是主阀打开
    /// 
    /// </summary>
    public partial class GasValveV2: UserControl
    {
        public GasValveV2()
        {
            InitializeComponent();

            Loaded+=new RoutedEventHandler(GasValveV2_Loaded);
        }

        void  GasValveV2_Loaded(object sender, RoutedEventArgs e)
        {
            switch (GasValveV2Direction)
            {
                case ValveDirection.ToLeft:
                    rotateTransform.Angle = -270;
                    break;
                case ValveDirection.ToRight:
                    rotateTransform.Angle = -90;
                    break;
                case ValveDirection.ToBottom:
                    break;
                case ValveDirection.ToTop:
                    rotateTransform.Angle = 180;
                    break;
                default:
                    break;
            }
        }

        //public event Action<string, string> ClickCoupleAct;

        public static readonly DependencyProperty GasValveV2DataProperty = DependencyProperty.Register(
       "GasValveV2Data", typeof(GasValveDataItem), typeof(GasValveV2),
       new FrameworkPropertyMetadata(default(GasValveDataItem), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GasValveV2DirectionProperty = DependencyProperty.Register(
          "GasValveV2Direction", typeof(ValveDirection), typeof(GasValveV2),
           new FrameworkPropertyMetadata(ValveDirection.ToBottom, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(ICommand), typeof(GasValveV2),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));


        public ValveDirection GasValveV2Direction
        {
            get
            {
                return (ValveDirection)GetValue(GasValveV2DirectionProperty);
            }
            set
            {
                SetValue(GasValveV2DirectionProperty, value);
            }
        }

        public GasValveDataItem GasValveV2Data
        {
            get
            {
                return (GasValveDataItem)GetValue(GasValveV2DataProperty);
            }
            set
            {
                SetValue(GasValveV2DataProperty, value);
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

        public string LeftValveTag { get; set; }
        public string RightValveTag { get; set; }
        public Thickness nothick = new Thickness(0);

        bool _needBlinding = false;
        bool _blindingTag = false;
        TimeSpan tsBlindTime = new TimeSpan(0, 0, 3);

        /// <summary>
        /// 容许设定值和真实值不同的时间段 ，在这段时间内不一致可以接受，超出时间就显示警告色
        /// </summary>
        TimeSpan tsTolerateTime = new TimeSpan(0, 0, 0, 500);
        DateTime _startBindingTime = new DateTime();
        bool _vCurrentOnOff = false;
        bool _vPreviousOnOff = false;
        DeviceTimer timer = new DeviceTimer();

        /// <summary>
        /// 设置警告时显示
        /// </summary>
        /// <param name="tooltipmessage"></param>
        void SetWarning(string tooltipmessage)
        {
            nopass.Fill = Brushes.Red;
            passtrigle.Fill = Brushes.Red;
            nopass2.Fill = Brushes.Red;
            passtrigle2.Fill = Brushes.Red;

            this.ToolTip = tooltipmessage;
        }
        bool IsWarningSet()
        {
            return nopass.Fill == Brushes.Red;
        }

        /// <summary>
        /// over rendering behavior
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            try
            {
                base.OnRender(drawingContext);
                //两阀有关联

                if (GasValveV2Data != null)
                {
                    this.ToolTip = string.Format("阀门名称：{0} \r\n设备编号：{1}\r\n默认设定：{2}\r\n当前设定：{3} \r\n实际状态：{4}",
                                                                    GasValveV2Data.DisplayName,
                                                                    GasValveV2Data.DeviceId,
                                                                    GasValveV2Data.DefaultValue,
                                                                    GasValveV2Data.SetValue,
                                                                    GasValveV2Data.Feedback);

                    if (GasValveV2Data.Feedback != GasValveV2Data.SetValue)
                    {
                        if (timer.IsIdle())
                            timer.Start(500);
                        else
                        {
                            if (timer.IsTimeout())
                            {
                                //if (!IsWarningSet())
                                SetWarning(string.Format("设定值和实际值不一致!设定:{0},实际:{1}\r\n", GasValveV2Data.SetValue ? "打开" : "关闭", GasValveV2Data.Feedback ? "打开" : "关闭") + this.ToolTip);
                            }
                        }
                    }
                    else
                    {
                        if (!timer.IsIdle())
                            timer.Stop();

                        if (nopass.Fill == Brushes.Red)
                        {
                            nopass.Fill = Brushes.Black;
                            passtrigle.Fill = Brushes.Green;
                            nopass2.Fill = Brushes.Black;
                            passtrigle2.Fill = Brushes.Green;
                        }
                    }

                }


                if (GasValveV2Data == null)
                {
                    _vCurrentOnOff = false;
                }
                else
                {
                    _vCurrentOnOff = GasValveV2Data.Feedback;
                }

                if (_vCurrentOnOff)
                {
                    passtrigle.Visibility = Visibility.Visible;
                    nopass.Visibility = Visibility.Hidden;
                    passtrigle2.Visibility = Visibility.Hidden;
                    nopass2.Visibility = Visibility.Visible;
                }
                else
                {
                    passtrigle.Visibility = Visibility.Hidden;
                    nopass.Visibility = Visibility.Visible;
                    passtrigle2.Visibility = Visibility.Visible;
                    nopass2.Visibility = Visibility.Hidden;
                }


                if (_needBlinding)
                {
                    if (_blindingTag)
                    {
                        nopass.Fill = Brushes.LightCoral;
                        nopass2.Fill = Brushes.LightCoral;
                        passtrigle.Fill = Brushes.Gold;
                        passtrigle2.Fill = Brushes.Gold;
                    }
                    else
                    {
                        nopass.Fill = Brushes.Black;
                        passtrigle.Fill = Brushes.Green;
                        nopass2.Fill = Brushes.Black;
                        passtrigle2.Fill = Brushes.Green;
                    }
                    _blindingTag = !_blindingTag;
                }

                if (_vPreviousOnOff != _vCurrentOnOff)
                {
                    _needBlinding = true;
                    _startBindingTime = DateTime.Now;
                    _vPreviousOnOff = _vCurrentOnOff;
                }
                else
                {
                    if (_startBindingTime < DateTime.Now.Subtract(tsBlindTime))
                    {
                        //停止闪烁
                        if (_needBlinding)
                        {
                            _needBlinding = false;
                            nopass.Fill = Brushes.Black;
                            passtrigle.Fill = Brushes.Green;
                            nopass2.Fill = Brushes.Black;
                            passtrigle2.Fill = Brushes.Green;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        void SetRectVisible()
        {

        }

        void SetValveDirection(ValveDirection Valve_Direction, RotateTransform rotateTransform)
        {
            switch (Valve_Direction)
            {
                case ValveDirection.ToBottom:
                    rotateTransform.Angle = 90;
                    break;
                case ValveDirection.ToTop:
                    rotateTransform.Angle = -90;
                    break;
                case ValveDirection.ToLeft:
                    rotateTransform.Angle = 180;
                    break;
                case ValveDirection.ToRight:
                    break;

                default:
                    break;
            }
        }

        private void SwitchValve(object sender, RoutedEventArgs e)
        {
            if ((Command != null) && (GasValveV2Data != null))
            {
                 Command.Execute(new object[] { GasValveV2Data.DeviceName, GasValveOperation.GVTurnValve, !GasValveV2Data.Feedback });
            }
        }

        private void canvasmain_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (GasValveV2Data == null || string.IsNullOrEmpty(GasValveV2Data.DeviceName))
                return;

            ContextMenu mouseClickMenu = new ContextMenu();
            MenuItem item = new MenuItem();
            item.Header = "_" + GasValveV2Data.DisplayName;
            item.Background = Brushes.Gray;
            item.Foreground = Brushes.White;
            item.IsEnabled = false;
            mouseClickMenu.Items.Add(item);

            item = new MenuItem();
            item.Header = "切换 (_C)";
            item.Tag = this.Tag;
            item.Click += SwitchValve;
            mouseClickMenu.Items.Add(item);
            mouseClickMenu.IsOpen = true;
        }
    }
}
