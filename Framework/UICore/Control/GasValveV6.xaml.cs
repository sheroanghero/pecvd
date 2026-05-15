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
using Aitex.Core.Util;
using Aitex.Core.RT.Log;

namespace Aitex.Core.UI.Control
{
    /// <summary>
    /// Interaction logic for GasValveV6.xaml
    /// </summary>
    public partial class GasValveV6 : UserControl
    {
        public GasValveV6()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty CoupleValve_V6MainProperty = DependencyProperty.Register(
          "CoupleValve_V6Main", typeof(SwithType), typeof(GasValveV6),
          new FrameworkPropertyMetadata(SwithType.Left, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GasValveV6DirectionProperty = DependencyProperty.Register(
            "GasValveV6Direction", typeof(ValveDirection), typeof(GasValveV6),
            new FrameworkPropertyMetadata(ValveDirection.ToBottom, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GasValveV6DataProperty = DependencyProperty.Register(
       "GasValveV6Data", typeof(GasValveDataItem), typeof(GasValveV6),
       new FrameworkPropertyMetadata(default(GasValveDataItem), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(ICommand), typeof(GasValveV6),
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

        public ValveDirection GasValveV6Direction
        {
            get
            {
                return (ValveDirection)GetValue(GasValveV6DirectionProperty);
            }
            set
            {
                SetValue(GasValveV6DirectionProperty, value);
            }
        }

        public GasValveDataItem GasValveV6Data
        {
            get
            {
                return (GasValveDataItem)GetValue(GasValveV6DataProperty);
            }
            set
            {
                SetValue(GasValveV6DataProperty, value);
            }
        }

        /// <summary>
        /// 指出哪个是双向阀的主阀，双向阀的开关状态即指主阀开关状态
        /// </summary>
        public SwithType CoupleValve_V6Main
        {
            get
            {
                return (SwithType)GetValue(CoupleValve_V6MainProperty);
            }
            set
            {
                SetValue(CoupleValve_V6MainProperty, value);
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
                if (GasValveV6Data != null)
                {
                    this.ToolTip = string.Format("阀门名称：{0} \r\n设备编号：{1}\r\n默认设定：{2}\r\n当前设定：{3} \r\n实际状态：{4}",
                                                                    GasValveV6Data.DisplayName,
                                                                    GasValveV6Data.DeviceId,
                                                                    GasValveV6Data.DefaultValue,
                                                                    GasValveV6Data.SetValue,
                                                                    GasValveV6Data.Feedback);

                    if (GasValveV6Data.Feedback != GasValveV6Data.SetValue)
                    {
                        if (timer.IsIdle())
                            timer.Start(500);
                        else
                        {
                            if (timer.IsTimeout())
                            {
                                //if (!IsWarningSet())
                                SetWarning(string.Format("设定值和实际值不一致!设定:{0},实际:{1}\r\n", GasValveV6Data.SetValue ? "打开" : "关闭", GasValveV6Data.Feedback ? "打开" : "关闭") + this.ToolTip);
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


                    _vCurrentOnOff = GasValveV6Data == null ? false : (CoupleValve_V6Main== SwithType.Right)? !GasValveV6Data.Feedback : GasValveV6Data.Feedback;

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
                LOG.Error(ex.Message);
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
            if ((Command != null) && (GasValveV6Data != null))
            {
                Command.Execute(new object[] { GasValveV6Data.DeviceName, GasValveOperation.GVTurnValve, !GasValveV6Data.Feedback });
            }
        }

        private void canvasmain_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (GasValveV6Data == null || string.IsNullOrEmpty(GasValveV6Data.DeviceName))
                return;

            ContextMenu mouseClickMenu = new ContextMenu();
            MenuItem item = new MenuItem();
            item.Header = "_" + GasValveV6Data.DisplayName;
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
