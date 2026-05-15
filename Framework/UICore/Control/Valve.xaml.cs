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
using Aitex.Core.RT.Log;
using Aitex.Core.UI.ControlDataContext;
using Aitex.Core.UI.DeviceControl;
using Aitex.Core.Util;

namespace Aitex.Core.UI.Control
{
    /// <summary>
    /// Interaction logic for ValveRound.xaml
    /// </summary>
    public partial class Valve : UserControl
    {
        public Valve()
        {
            InitializeComponent();
        }

        public event Action<string, string> clickAct;

        public static readonly DependencyProperty OnOffProperty = DependencyProperty.Register(
       "OnOff", typeof(object), typeof(Valve),
       new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsManualModeProperty = DependencyProperty.Register(
          "IsManualModeValve", typeof(bool), typeof(Valve),
          new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ValveNameProperty = DependencyProperty.Register(
          "ValveName", typeof(string), typeof(Valve),
           new FrameworkPropertyMetadata("未知阀门", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ValveDirectionProperty = DependencyProperty.Register(
          "ValveDirection", typeof(ValveDirection), typeof(Valve),
           new FrameworkPropertyMetadata(ValveDirection.ToBottom, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
          "Command", typeof(ICommand), typeof(Valve),
           new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsOpenedProperty = DependencyProperty.Register(
            "IsOpened", typeof(bool), typeof(Valve));

        public static readonly DependencyProperty HideBlindingProperty = DependencyProperty.Register(
            "HideBlinding", typeof(bool), typeof(Valve));

        public bool IsOpened
        {
            get
            {
                return (bool)this.GetValue(IsOpenedProperty);
            }
            set
            {
                this.SetValue(IsOpenedProperty, value);
            }
        }

        public bool HideBlinding
        {
            get
            {
                return (bool)this.GetValue(HideBlindingProperty);
            }
            set
            {
                this.SetValue(HideBlindingProperty, value);
            }
        }

        /// <summary>
        /// direction of the valve
        /// </summary>
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

        /// <summary>
        /// valve name
        /// </summary>
        public string ValveName
        {
            get
            {
                return (string)GetValue(ValveNameProperty);
            }
            set
            {
                SetValue(ValveNameProperty, value);
            }
        }

        /// <summary>
        /// Valve_V2 on/off status
        /// </summary>
        public object OnOff
        {
            get
            {
                return GetValue(OnOffProperty);
            }
            set
            {
                SetValue(OnOffProperty, value);
            }
        }


        /// <summary>
        /// Valve_V2 on/off status
        /// </summary>
        public bool IsManualModeValve
        {
            get
            {
                return (bool)GetValue(IsManualModeProperty);
            }
            set
            {
                SetValue(IsManualModeProperty, value);
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
        bool vOnOff = false;
        bool vOnOffTemp = false;
        DeviceTimer timer = new DeviceTimer();

        /// <summary>
        /// 设置警告时显示
        /// </summary>
        /// <param name="tooltipmessage"></param>
        void SetWarning(string tooltipmessage)
        {
            nopass.Fill = Brushes.Red;
            passtrigle.Fill = Brushes.Red;
            this.ToolTip = tooltipmessage;
        }

        bool isWarning = false;
        /// <summary>
        /// over rendering behavior
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            try
            {
                base.OnRender(drawingContext);

                 AITValveData deviceData = OnOff as AITValveData;
                if (deviceData != null)
                {

                    bool.TryParse(OnOff + "", out vOnOff);
                    string tooltipmsg = string.Format("阀门:{0}\r\nID:{1}", ValveName, deviceData.DeviceSchematicId, deviceData.DefaultValue, deviceData.SetPoint, deviceData.Feedback);
                    this.ToolTip = tooltipmsg;
                    nopass.Fill = Brushes.Black;
                    passtrigle.Fill = Brushes.Green;
                    #region 判断阀的设定值和实际值是否在容许时间内一致

                    if (deviceData.Feedback != deviceData.SetPoint)
                    {
                        if (timer.IsIdle())
                        {
                            timer.Start(500);
                        }
                        else
                        {
                            if (timer.IsTimeout())
                            {
                                isWarning = true;
                                SetWarning(string.Format("设定值和实际值不一致!设定:{0},实际:{1}\r\n", deviceData.SetPoint ? "打开" : "关闭", deviceData.Feedback ? "打开" : "关闭") + tooltipmsg);
                            }
                        }
                    }
                    else
                    {
                        if (!timer.IsIdle())
                        {
                            timer.Stop();
                        }
                        isWarning = false;
                    }

                    #endregion

                }
                else if ((OnOff + "").ToString().ToLower() == "false")
                {
                    vOnOff = false;
                }
                else if ((OnOff + "").ToString().ToLower() == "true")
                {
                    vOnOff = true;
                }

                IsOpened = vOnOff;

                if (!HideBlinding)
                { 
                    #region 闪烁 
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
                    else
                    {
                        //nopass.Fill = Brushes.Black;
                        //passtrigle.Fill = Brushes.Green;
                    }
                    if (vOnOffTemp != vOnOff)
                    {
                        _needBlinding = true;
                        _startBindingTime = DateTime.Now;
                        vOnOffTemp = vOnOff;
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
                    #endregion
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

                if (vOnOff)
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
                LOG.Write(ex);
            }
        }
        /// <summary>
        /// open 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnOnValve(object sender, RoutedEventArgs e)
        {
            if (Command != null || clickAct != null)
            {
                AITValveData deviceData = OnOff as AITValveData;
                if (deviceData != null)
                {
                    deviceData.SetPoint = true;
                }
            }
            if (Command != null)
            {
                KeyValuePair<string, string> pair = new KeyValuePair<string, string>(((MenuItem)e.Source).Tag + "", "true");

                Command.Execute(pair);
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
            if (Command != null || clickAct != null)
            {
                AITValveData deviceData = OnOff as AITValveData;
                if (deviceData != null)
                {
                    deviceData.SetPoint = false;
                }
            }
            if (Command != null)
            {
                KeyValuePair<string, string> pair = new KeyValuePair<string, string>(((MenuItem)e.Source).Tag + "", "false");

                Command.Execute(pair);
            }
            else if (clickAct != null)
            {
                clickAct(((MenuItem)e.Source).Tag + "", "false");
            }
        }

        private void nopass_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsManualModeValve)
                return;

            ContextMenu mouseClickMenu = new ContextMenu();
            MenuItem item = new MenuItem();
            item.Header = "_" + ValveName;
            item.Background = Brushes.LightGray;
            item.Foreground = Brushes.White;
            item.IsEnabled = false;
            mouseClickMenu.Items.Add(item);

            addOpenMenu(mouseClickMenu, item);
            if (isWarning)
            {
                addCloseMenu(mouseClickMenu, item);
            }

            mouseClickMenu.IsOpen = true;
        }

        private void passtrigle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsManualModeValve)
                return;

            ContextMenu mouseClickMenu = new ContextMenu();
            MenuItem item = new MenuItem();
            item.Header = "_" + ValveName;
            item.Background = Brushes.LightGray;
            item.Foreground = Brushes.White;
            item.IsEnabled = false;
            mouseClickMenu.Items.Add(item);

            addCloseMenu(mouseClickMenu, item);
            if (isWarning)
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

            ToolTip = $"Name: {ValveName}\r\nStatus: {IsOpened}";
        }

        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            nopass.Fill = Brushes.Black;
            passtrigle.Fill = Brushes.Green;
        }
    }
}
