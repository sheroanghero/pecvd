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

namespace Aitex.Core.UI.View.Common
{
    /// <summary>
    /// Interaction logic for MonitorView.xaml
    /// </summary>
    public partial class MonitorView : UserControl
    {
        public MonitorView()
        {
            InitializeComponent();

            IsVisibleChanged += new DependencyPropertyChangedEventHandler(MainWindow_IsVisibleChanged);
        }

        void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }

        /// <summary>
        /// Alarm, Warning selected item changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                var item = e.AddedItems[0] as AlarmItem;
                AnalysisText.Text = string.Format("Event Type：{0}\r\n\r\nEvent Name：{1}\r\n\r\nTime：{2}\r\n\r\nDescription：{3}",
                    item.Type,
                    item.EventEnum, 
                    //item.EventId, Event Number：{ 2}\r\n\r\n
                    item.OccuringTime,
                    item.Description);
                    //item.Solution);\r\n\r\nSolution：{ 5}
            }
            else
            {
                AnalysisText.Text = string.Empty;
            }
        }
    }
}
