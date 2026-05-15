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

namespace Aitex.Core.UI.Dialog
{
    /// <summary>
    /// Interaction logic for NotificationDialog.xaml
    /// </summary>
    public partial class NotificationDialog : Window
    {
        public NotificationDialog()
        {
            InitializeComponent();
            Closing += new System.ComponentModel.CancelEventHandler(NotificationDialog_Closing);
            _tm.Elapsed += new System.Timers.ElapsedEventHandler(_tm_Elapsed);
            _tm.Start();

            BannerColor = Brushes.DodgerBlue;
        }

        private TimeSpan _autoCloseTime = new TimeSpan(0, 1, 0);

        void NotificationDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _tm.Stop();
        }

        DateTime _startTime = DateTime.Now;
        System.Timers.Timer _tm = new System.Timers.Timer(500);

        void _tm_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (DateTime.Now - _startTime > _autoCloseTime)
            {
                //延迟1min后，自动关闭提示对话框
                Dispatcher.Invoke(new Action(() =>
                {
                    Close();
                }));
            }
            else
            {
                //显示对话框弹出显示至今的时间
                labelElapsedTime.Dispatcher.BeginInvoke(new Action(() =>
                {
                    labelElapsedTime.Content = (_startTime + new TimeSpan(0, 1, 0) - DateTime.Now).ToString(@"hh\:mm\:ss");
                }));
            }
        }

        /// <summary>
        /// close current window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            _tm.Stop();
            Close();
        }

        /// <summary>
        /// Message title
        /// </summary>
        public string MessageTitle
        {
            get
            {
                return _msgTitle.Text;
            }
            set
            {
                _msgTitle.Text = value;
            }
        }

        /// <summary>
        /// Message content
        /// </summary>
        public string MessageContent
        {
            get
            {
                return _msgContent.Text;
            }
            set
            {
                _msgContent.Text = value;
            }
        }

        /// <summary>
        /// Banner color
        /// </summary>
        public Brush BannerColor
        {
            get
            {
                return topGrid.Background;
            }
            set
            {
                topGrid.Background = value;
            }
        }
    }
}
