using System.Windows.Controls;
using Aitex.Core.UI.View.Common;

namespace MECF.Framework.UI.Client.CenterViews.Alarms.Alarm
{
    /// <summary>
    /// AlarmView.xaml 的交互逻辑
    /// </summary>
    public partial class AlarmView : UserControl
    {
        public AlarmView()
        {
            InitializeComponent();
        }

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
                    item.Description
                //item.Solution \r\n\r\nSolution：{ 5}
                );
            }
            else
            {
                AnalysisText.Text = string.Empty;
            }
        }
    }
}
