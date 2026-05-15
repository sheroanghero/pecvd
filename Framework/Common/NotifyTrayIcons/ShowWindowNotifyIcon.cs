using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hardcodet.Wpf.TaskbarNotification;

namespace MECF.Framework.Common.NotifyTrayIcons
{
    public class ShowWindowNotifyIcon: TaskbarIcon
    {
        public event Action<bool> ShowMainWindow;
        public event Action ExitWindow;

        public ShowWindowNotifyIcon(string windowName, ImageSource icon)
        {
             
            ContextMenu menu = new ContextMenu();

            MenuItem item = new MenuItem();
            item.Header = "Show " + windowName;
            item.Click += ClickShow;
            menu.Items.Add(item);

            item = new MenuItem();
            item.Header = "Hide " + windowName;
            item.Click += ClickHide;
            menu.Items.Add(item);

            item = new MenuItem();
            item.Header = "-";
            menu.Items.Add(item);


            item = new MenuItem();
            item.Header = "Exit ";
            item.Click += ClickExit;
            menu.Items.Add(item);

            this.ContextMenu = menu;

            this.TrayMouseDoubleClick += ShowWindowNotifyIcon_TrayMouseDoubleClick; 

            ToolTipText = "Double-click for window, right-click for menu";
            IconSource = icon;
        }

        private void ShowWindowNotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (ShowMainWindow != null)
                ShowMainWindow(true);
        }

        public void ShowBallon(string title, string text)
        {
            ShowBalloonTip(title, text, BalloonIcon.Info);
        }
        private void ClickHide(object sender, RoutedEventArgs e)
        {
            if (ShowMainWindow != null)
                ShowMainWindow(false);
        }

        private void ClickShow(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ShowMainWindow != null)
            {
                ShowMainWindow(true);
            }
        }

        private void ClickExit(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ExitWindow != null)
            {
                Application.Current.Dispatcher.Invoke(() => { ExitWindow(); });

            }
        }

    }
}
