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
    public partial class GasValve : UserControl
    {
        public GasValve()
        {
            InitializeComponent();
        }

        public event Action<string, string> clickAct;

        public static readonly DependencyProperty GasValveDataProperty = DependencyProperty.Register(
       "GasValveData", typeof(GasValveDataItem), typeof(GasValve),
       new FrameworkPropertyMetadata(default(GasValveDataItem), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ValveDirectionProperty = DependencyProperty.Register(
          "ValveDirection", typeof(ValveDirection), typeof(GasValve),
           new FrameworkPropertyMetadata(ValveDirection.ToBottom, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
          "Command", typeof(ICommand), typeof(GasValve),
           new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        
        public ValveDirection ValveDirection
        {
            get
            {
                return (ValveDirection)GetValue(ValveDirectionProperty);
            }
            set
            {
                SetValue(ValveDirectionProperty, value);
            }
        }

        public GasValveDataItem GasValveData
        {
            get
            {
                return (GasValveDataItem)GetValue(GasValveDataProperty);
            }
            set
            {
                SetValue(GasValveDataProperty, value);
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

        bool _needBlinding = false;
        bool _blindingTag = false;
        TimeSpan tsBlindTime = new TimeSpan(0, 0, 3);
        DateTime _startBindingTime = new DateTime();
        bool _vCurrentOnOff = false;
        bool _vPreviousOnOff = false;
        DeviceTimer _timer = new DeviceTimer();
        bool _isWarning = false;

        void SetWarning(string tooltipmessage)
        {
            nopass.Fill = Brushes.Red;
            passtrigle.Fill = Brushes.Red;
            this.ToolTip = tooltipmessage;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            try
            {
                base.OnRender(drawingContext);

                if (GasValveData != null)
                {
                    _vCurrentOnOff = GasValveData.Feedback;

                    this.ToolTip = string.Format("阀门名称：{0} \r\n设备编号：{1}\r\n默认设定：{2}\r\n当前设定：{3} \r\n实际状态：{4}", 
                                                                    GasValveData.DisplayName, 
                                                                    GasValveData.DeviceId,
                                                                    GasValveData.DefaultValue,
                                                                    GasValveData.SetValue,
                                                                    GasValveData.Feedback);
                    nopass.Fill = Brushes.Black;
                    passtrigle.Fill = Brushes.Green;
                    
                    //判断阀的设定值和实际值是否在容许时间内一致
                    if (GasValveData.Feedback != GasValveData.SetValue)
                    {
                        if (_timer.IsIdle())
                        {
                            _timer.Start(500);
                        }
                        else
                        {
                            if (_timer.IsTimeout())
                            {
                                _isWarning = true;
                                SetWarning(string.Format("设定值和实际值不一致!设定:{0},实际:{1}\r\n", GasValveData.SetValue ? "打开" : "关闭", GasValveData.Feedback ? "打开" : "关闭") + this.ToolTip);
                            }
                        }
                    }
                    else
                    {
                        if (!_timer.IsIdle())
                        {
                            _timer.Stop();
                        }
                        _isWarning = false;
                    }
                }

                if (_needBlinding)
                {
                    if (_blindingTag)
                    {
                        if (nopass.Visibility == System.Windows.Visibility.Visible) nopass.Fill = Brushes.LightCoral;
                        if (passtrigle.Visibility == System.Windows.Visibility.Visible) passtrigle.Fill = Brushes.Gold;
                    }
                    else
                    {
                        if (nopass.Visibility == System.Windows.Visibility.Visible) nopass.Fill = Brushes.Black;
                        if (passtrigle.Visibility == System.Windows.Visibility.Visible) passtrigle.Fill = Brushes.Green;
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
                    if (_startBindingTime < DateTime.Now.Subtract(tsBlindTime) && _needBlinding)
                    {
                        //停止闪烁
                        _needBlinding = false;
                        nopass.Fill = Brushes.Black;
                        passtrigle.Fill = Brushes.Green;
                    }
                }

                switch (ValveDirection)
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
                        // No rotation is needed.
                        break;

                    default:
                        break;
                }

                if (_vCurrentOnOff)
                {
                    passtrigle.Visibility = Visibility.Visible;
                    nopass.Visibility = Visibility.Hidden;
                }
                else
                {
                    passtrigle.Visibility = Visibility.Hidden;
                    nopass.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }
        /// <summary>
        /// open 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnOnValve(object sender, RoutedEventArgs e)
        {
            if (Command != null)
            {
                Command.Execute(new object[] { GasValveData.DeviceName, GasValveOperation.GVTurnValve, true});
            }
            else if (clickAct != null)
            {
                clickAct(((MenuItem)e.Source).Tag + "", "true");
            }
        }

        /// <summary>
        /// close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnOffValve(object sender, RoutedEventArgs e)
        {
            if (Command != null)
            {
                Command.Execute(new object[] { GasValveData.DeviceName, GasValveOperation.GVTurnValve, false });
            }
            else if (clickAct != null)
            {
                clickAct(((MenuItem)e.Source).Tag + "", "false");
            }
        }

        private void nopass_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (GasValveData == null || string.IsNullOrEmpty(GasValveData.DeviceName))
                return;

            ContextMenu mouseClickMenu = new ContextMenu();
            MenuItem item = new MenuItem();
            item.Header = "_" + GasValveData.DisplayName;
            item.Background = Brushes.Gray;
            item.Foreground = Brushes.White;
            item.IsEnabled = false;
            mouseClickMenu.Items.Add(item);

            addOpenMenu(mouseClickMenu, item);
            if (_isWarning)
            {
                addCloseMenu(mouseClickMenu, item);
            }

            mouseClickMenu.IsOpen = true;
        }

        private void passtrigle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (GasValveData == null || string.IsNullOrEmpty(GasValveData.DeviceName))
                return;

            ContextMenu mouseClickMenu = new ContextMenu();
            MenuItem item = new MenuItem();
            item.Header = "_" + GasValveData.DisplayName;
            item.Background = Brushes.Gray;
            item.Foreground = Brushes.White;
            item.IsEnabled = false;
            mouseClickMenu.Items.Add(item);

            addCloseMenu(mouseClickMenu, item);
            if (_isWarning)
            {
                addOpenMenu(mouseClickMenu, item);
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

        private void Canvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (passtrigle.Visibility == Visibility.Visible)
            {
                passtrigle_MouseLeftButtonDown(null, null);
            }
            else
            {
                nopass_MouseLeftButtonDown(null, null);
            }
        }

        private void Canvas_MouseEnter(object sender, MouseEventArgs e)
        {
            nopass.Fill = Brushes.Goldenrod;
            passtrigle.Fill = Brushes.Goldenrod;
        }

        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            nopass.Fill = Brushes.Black;
            passtrigle.Fill = Brushes.Green;
        }
    }
}
