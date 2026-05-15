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
using Hardcodet.Wpf.TaskbarNotification;

namespace Aitex.Core.UI.Control
{
    /// <summary>
    /// Interaction logic for MessageControl.xaml
    /// </summary>
    public partial class MessageControl : UserControl
    {
        public MessageControl()
        {
            InitializeComponent();
            this.Visibility = Visibility.Hidden;
        }
        private bool isClosing = false;

        #region BalloonText dependency property

        /// <summary>
        /// Description
        /// </summary>
        public static readonly DependencyProperty MessageToolBarText =
            DependencyProperty.Register("MessageText",
                                        typeof(string),
                                        typeof(MessageControl),
                                        new FrameworkPropertyMetadata("aaaaa"));

        /// <summary>
        /// A property wrapper for the <see cref="BalloonTextProperty"/>
        /// dependency property:<br/>
        /// Description
        /// </summary>
        public string MessageText
        {
            get { return (string)GetValue(MessageToolBarText); }
            set { SetValue(MessageToolBarText, value); }
        }

        #endregion
        /// <summary>
        /// By subscribing to the <see cref="TaskbarIcon.BalloonClosingEvent"/>
        /// and setting the "Handled" property to true, we suppress the popup
        /// from being closed in order to display the fade-out animation.
        /// </summary>
        private void OnBalloonClosing(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            isClosing = true;
        }


        /// <summary>
        /// Resolves the <see cref="TaskbarIcon"/> that displayed
        /// the balloon and requests a close action.
        /// </summary>
        private void imgClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //the tray icon assigned this attached property to simplify access
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            taskbarIcon.CloseBalloon();
        }

        /// <summary>
        /// If the users hovers over the balloon, we don't close it.
        /// </summary>
        private void grid_MouseEnter(object sender, MouseEventArgs e)
        {
            //if we're already running the fade-out animation, do not interrupt anymore
            //(makes things too complicated for the sample)
            if (isClosing) return;

            //the tray icon assigned this attached property to simplify access
            TaskbarIcon taskbarIcon = TaskbarIcon.GetParentTaskbarIcon(this);
            taskbarIcon.ResetBalloonCloseTimer();
        }


        /// <summary>
        /// Closes the popup once the fade-out animation completed.
        /// The animation was triggered in XAML through the attached
        /// BalloonClosing event.
        /// </summary>
        private void OnFadeOutCompleted(object sender, EventArgs e)
        {
            //fix bug
            //this will cause next message display fail sometimes
            //let taskbaricon class do this job

            //Popup pp = (Popup)Parent;
            //pp.IsOpen = false;
        }

        public void SetMessage(MessageType type, string content)
        {
            switch (type)
            {
                case MessageType.Info:
                    MessageLbl.Foreground = Brushes.DarkGreen;
                    break;
                case MessageType.Success:
                    MessageLbl.Foreground = Brushes.Green;
                    break;
                case MessageType.Warning:
                    MessageLbl.Foreground = Brushes.Yellow;
                    break;
                case MessageType.Erro:
                    MessageLbl.Foreground = Brushes.Red;
                    break;
                default:
                    break;
            }
            this.Visibility = Visibility.Visible;
            //MessageText = @"MessageLbl\r\nMessageLbl";//content;
            MessageLbl.Text = content;//@"MessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLblMessageLbl&#x000A;Messag&#13;eLbl";
            MessageLbl.ToolTip = content;
        }
        /// <summary>
        /// use to records all the messages
        /// </summary>
        public static List<string> messageList = new List<string>();


        public void ClearMessage()
        {
            if (!string.IsNullOrEmpty(MessageLbl.Text + ""))
            {
                messageList.Add(MessageLbl.Text.ToString());
                MessageText = "";
            }
        }
    }
}
