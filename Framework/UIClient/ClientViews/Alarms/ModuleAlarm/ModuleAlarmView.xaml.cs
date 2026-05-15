using System;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Aitex.Core.UI.View.Common;

namespace MECF.Framework.UI.Client.CenterViews.Alarms.ModuleAlarm
{
    /// <summary>
    /// ModuleAlarmView.xaml 的交互逻辑
    /// </summary>
    public partial class ModuleAlarmView : UserControl
    {
        public ModuleAlarmView()
        {
            InitializeComponent();
        }

        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (e.AddedItems.Count == 1)
            //{
            //    var item = e.AddedItems[0] as AlarmItem;
            //    AnalysisText.Text = string.Format("Event Type：{0}\r\n\r\nEvent Name：{1}\r\n\r\nTime：{2}\r\n\r\nDescription：{3}",
            //        item.Type,
            //        item.EventEnum,
            //        //item.EventId, Event Number：{ 2}\r\n\r\n
            //        item.OccuringTime,
            //        item.Description
            //    //item.Solution \r\n\r\nSolution：{ 5}
            //    );
            //}
            //else
            //{
            //    AnalysisText.Text = string.Empty;
            //}
        }
    }

    public class TypeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
 
            switch ((string)value)
            {
                case "Alarm": return Brushes.Red;
                case "Warning": return Brushes.Yellow;
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;

        }
    }
}
