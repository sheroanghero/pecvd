using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace Aitex.Core.UI.Dialog
{
    public class MessageBoxEx
    {
        /// <summary>
        /// show information dialog
        /// </summary>
        /// <param name="infoTitle">information title string</param>
        /// <param name="infoContent">information content string</param>
        public static void ShowInfo(string infoTitle, string infoContent)
        {
            NotificationDialog dlg = new NotificationDialog() { MessageTitle = infoTitle, MessageContent = infoContent, BannerColor = Brushes.DodgerBlue };
            dlg.Show();
        }

        /// <summary>
        /// show warning dialog
        /// </summary>
        /// <param name="infoTitle">information title string</param>
        /// <param name="infoContent">information content string</param>
        public static void ShowWarning(string infoTitle, string infoContent)
        {
            NotificationDialog dlg = new NotificationDialog() { MessageTitle = infoTitle, MessageContent = infoContent, BannerColor = Brushes.Gold };
            dlg.Show();
        }

        /// <summary>
        /// show error dialog
        /// </summary>
        /// <param name="infoTitle">information title string</param>
        /// <param name="infoContent">information content string</param>
        public static void ShowError(string infoTitle, string infoContent)
        {
            NotificationDialog dlg = new NotificationDialog() { MessageTitle = infoTitle, MessageContent = infoContent, BannerColor = Brushes.Red };
            dlg.Show();
        }
    }
}