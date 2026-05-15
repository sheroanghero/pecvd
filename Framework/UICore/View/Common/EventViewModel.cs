using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.UI.MVVM;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Windows;
using Aitex.Core.RT.Log;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Aitex.Core.RT.Event;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Utilities;

namespace Aitex.Core.UI.View.Common
{
    /// <summary>
    /// 操作记录查询结果结构定义
    /// </summary>
    public class SystemLogItem
    {
        /// <summary>
        /// 时间
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// ICON 
        /// </summary>
        public object Icon { get; set; }

        /// <summary>
        /// 类型：操作日志|事件|其他
        /// </summary>
        public string LogType { get; set; }

        /// <summary>
        /// 针对腔体
        /// </summary>
        public string TargetChamber { get; set; }

        /// <summary>
        /// 发起方
        /// </summary>
        public string Initiator { get; set; }

        /// <summary>
        /// 详情
        /// </summary>
        public string Detail { get; set; }
    }

    

    public class EventViewModel : ViewModelBase
    {
        public bool SearchAlarmEvent { get; set; }
        public bool SearchWarningEvent { get; set; }
        public bool SearchInfoEvent { get; set; }
        public bool SearchOpeLog { get; set; }

        public bool SearchPMA { get; set; }
        public bool SearchPMB { get; set; }
        public bool SearchPMC { get; set; }
        public bool SearchPMD { get; set; }
        //public bool SearchCoolDown { get; set; }
        public bool SearchTM { get; set; }
        public bool SearchLL { get; set; }
        //public bool SearchBuf1 { get; set; }
        public bool SearchSystem { get; set; }

        public string SearchKeyWords { get; set; }

        public ICommand SearchCommand { get; set; }
        public ICommand ExportCommand { get; set; }

        public DateTime SearchBeginTime { get; set; }
        public DateTime SearchEndTime { get; set; }

        public string Keywords { get; set; }

        public ObservableCollection<string> EventList { get; set; }
        public string SelectedEvent { get; set; }

        public ObservableCollection<string> UserList { get; set; }
        public string SelectedUser { get; set; }

        public ObservableCollection<SystemLogItem> SearchedResult { get; set; }

        public Func<string, List<EventItem>> QueryDBEventFunc { get; set; }
        public Func<List<string>> QueryEventList { get; set; }

        public EventViewModel()
        {


            var now = DateTime.Today;
            SearchBeginTime = now;// -new TimeSpan(1, 0, 0, 0);
            SearchEndTime = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, 999);

            SelectedUser = "All";
            SearchKeyWords = string.Empty;


            SearchAlarmEvent = true;
            SearchWarningEvent = true;
            SearchInfoEvent = true;
            SearchOpeLog = false;

            SearchPMA = false;
            SearchPMB = false;
            SearchPMC = false;
            SearchPMD = false;
            //SearchCoolDown = false;
            SearchTM = false;
            SearchLL = false;
            //SearchBuf1 = false;
            SearchSystem = false;
        }

        /// <summary>
        /// 预先载入数据
        /// </summary>
        public void Preload()
        {
            SearchCommand = new DelegateCommand<object>((o) => Search(), (o) => true);
            ExportCommand = new DelegateCommand<object>((o) => Export(), (o) => true);

            //初始化所有的枚举事件类型
            EventList = new ObservableCollection<string>();
            EventList.Add("All");
            if (QueryEventList != null)
            {
                List<string> evList = QueryEventList();
                foreach (string ev in evList)
                    EventList.Add(ev);
            }
            SelectedEvent = "All";

            //初始化所有的用户
            //PreloadUsers();

            //所有属性更新
            InvokePropertyChanged();

            //第一次默认查询
            if (SearchedResult == null)
                Search();
        }


        /// <summary>
        /// 导出
        /// </summary>
        void Export()
        {
            try
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.DefaultExt = ".xlsx"; // Default file extension 
                dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
                Nullable<bool> result = dlg.ShowDialog();// Show open file dialog box
                if (result == true) // Process open file dialog box results
                {
                    System.Data.DataSet ds = new System.Data.DataSet();
                    ds.Tables.Add(new System.Data.DataTable("系统运行日志"));
                    ds.Tables[0].Columns.Add("类型");
                    ds.Tables[0].Columns.Add("时间");
                    ds.Tables[0].Columns.Add("腔体");
                    ds.Tables[0].Columns.Add("发起源");
                    ds.Tables[0].Columns.Add("内容描述");
                    foreach (var item in SearchedResult)
                    {
                        var row = ds.Tables[0].NewRow();
                        row[0] = item.LogType;
                        row[1] = item.Time;
                        row[2] = item.TargetChamber;
                        row[3] = item.Initiator;
                        row[4] = item.Detail;
                        ds.Tables[0].Rows.Add(row);
                    }

                    if (!ExcelHelper.ExportToExcel(dlg.FileName, ds, out string reason))
                    {
                        MessageBox.Show($"Export failed, {reason}", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    MessageBox.Show($"Export succeed, file save as {dlg.FileName}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                MessageBox.Show("导出系统日志发生错误", "导出失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private string GetSourceWhere()
        {
            //if (!SearchPMA) sqlEvent += string.Format(" and \"Source\"<>'{0}' ", ChamberSet.ReactorA);
            //if (!SearchPMB) sqlEvent += string.Format(" and \"Source\"<>'{0}' ", ChamberSet.ReactorB);
            //if (!SearchPMC) sqlEvent += string.Format(" and \"Source\"<>'{0}' ", ChamberSet.ReactorC);
            //if (!SearchPMD) sqlEvent += string.Format(" and \"Source\"<>'{0}' ", ChamberSet.ReactorD);
            //if (!SearchSystem) sqlEvent += string.Format(" and \"Source\"<>'{0}' ", ChamberSet.System);
            //if (!SearchLL) sqlEvent += string.Format(" and \"Source\"<>'{0}' ", ChamberSet.Loadlock);
            //if (!SearchTM) sqlEvent += string.Format(" and \"Source\"<>'{0}' and \"Source\"<>'{1}' and \"Source\"<>'{2}' ",
            //    ChamberSet.Loadlock, ChamberSet.Buffer1, ChamberSet.Cooldown);

            return "";
        }


        /*
         *
         *   gid integer NOT NULL DEFAULT nextval('event_data_gid_seq'::regclass),
  event_id integer,
  event_enum text,
  type text,
  source text,
  description text,
  level text,
  occur_time timestamp without time zone,
  CONSTRAINT event_data_pkey PRIMARY KEY (gid)
         */

        /// <summary>
        /// 查询
        /// </summary>
        void Search()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    string sqlEvent = "";
                    string sqlOperationLog = "";
                    string sql = "";

                    if (SearchAlarmEvent || SearchWarningEvent || SearchInfoEvent)
                    {
                        sqlEvent = string.Format("SELECT \"event_id\", \"event_enum\", \"type\", \"occur_time\", \"level\",\"source\" , \"description\" FROM \"event_data\" where \"occur_time\" >='{0}' and \"occur_time\" <='{1}' ", SearchBeginTime.ToString("yyyyMMdd HHmmss"), SearchEndTime.ToString("yyyyMMdd HHmmss"));

                        sqlEvent += GetSourceWhere();

                        sqlEvent += " and (FALSE ";
                        if (SearchAlarmEvent) sqlEvent += " OR \"level\"='Alarm' ";
                        if (SearchWarningEvent) sqlEvent += " OR \"level\"='Warning' ";
                        if (SearchInfoEvent) sqlEvent += " OR \"level\"='Information' ";
                        sqlEvent += " ) ";

                        if (!string.IsNullOrWhiteSpace(SelectedEvent) && SelectedEvent != "All") sqlEvent += string.Format(" and lower(\"event_enum\")='{0}' ", SelectedEvent.ToLower());

                        if (!string.IsNullOrWhiteSpace(SearchKeyWords)) sqlEvent += string.Format(" and lower(\"description\") like '%{0}%' ", SearchKeyWords.ToLower());
                    }

                    if (SearchOpeLog)
                    {
                        //sqlOperationLog = string.Format(" SELECT \"UserName\" as \"Initiator\", 'UserOperation' as \"LogType\", \"Time\", \"ChamberId\" as \"TargetChamber\", \"Content\" as \"Description\" FROM \"OperationLog\" where \"Time\" >='{0}' and \"Time\" <='{1}' ", SearchBeginTime.ToString("yyyy/MM/dd HH:mm:ss"), SearchEndTime.ToString("yyyy/MM/dd HH:mm:ss"));

                        //if (!SearchPMA) sqlOperationLog += string.Format(" and \"ChamberId\"<>'{0}' ", ChamberSet.ReactorA);
                        //if (!SearchPMB) sqlOperationLog += string.Format(" and \"ChamberId\"<>'{0}' ", ChamberSet.ReactorB);
                        //if (!SearchPMC) sqlOperationLog += string.Format(" and \"ChamberId\"<>'{0}' ", ChamberSet.ReactorC);
                        //if (!SearchPMD) sqlOperationLog += string.Format(" and \"ChamberId\"<>'{0}' ", ChamberSet.ReactorD);
                        //if (!SearchSystem) sqlOperationLog += string.Format(" and \"ChamberId\"<>'{0}' ", ChamberSet.System);                      
                        //if (!SearchLL) sqlOperationLog += string.Format(" and \"ChamberId\"<>'{0}' ", ChamberSet.Loadlock);
                        //if (!SearchTM) sqlOperationLog += string.Format(" and \"ChamberId\"<>'{0}' and \"ChamberId\"<>'{1}' and \"ChamberId\"<>'{2}' ",
                        //    ChamberSet.Loadlock, ChamberSet.Buffer1, ChamberSet.Cooldown);
                        //if (!string.IsNullOrWhiteSpace(SelectedUser) && SelectedUser != "不限") sqlOperationLog += string.Format(" and lower(\"UserName\")='{0}' ", SelectedUser.ToLower());

                        //if (!string.IsNullOrWhiteSpace(SearchKeyWords)) sqlOperationLog += string.Format(" and lower(\"Content\") like '%{0}%' ", SearchKeyWords.ToLower());
                    }

                    sql = sqlEvent;

                    if (!string.IsNullOrEmpty(sqlOperationLog))
                    {
                        if (string.IsNullOrEmpty(sql))
                        {
                            sql = sqlOperationLog;
                        }
                        else
                        {
                            sql += " UNION ALL " + sqlOperationLog;
                        }
                    }


                    if (!string.IsNullOrEmpty(sql) && QueryDBEventFunc != null)
                    {
                        sql += " order by \"occur_time\" asc limit 2000;";

                        List<EventItem> lstEvent = QueryDBEventFunc(sql);

                        if (lstEvent == null)
                            return;

                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            SearchedResult = new ObservableCollection<SystemLogItem>();

                            string logTypeStr;

                            foreach (EventItem ev in lstEvent)
                            {
                                switch (ev.Level)
                                {
                                    case EventLevel.Information: logTypeStr = "Info"; break;
                                    case EventLevel.Warning: logTypeStr = "Warning"; break;
                                    case EventLevel.Alarm: logTypeStr = "Alarm"; break;
                                    default: logTypeStr = "Undefine"; break;
                                }


                                SearchedResult.Add(new SystemLogItem()
                                    {
                                        Time = ((DateTime)ev.OccuringTime).ToString("yyyy/MM/dd HH:mm:ss.fff"),
                                        LogType = logTypeStr,
                                        Detail = ev.Description,
                                        TargetChamber = ev.Source,
                                        Initiator = "",
                                        Icon = new BitmapImage(new Uri(string.Format("pack://application:,,,/MECF.Framework.Common;component/Resources/SystemLog/{0}.png", ev.Level.ToString()), UriKind.Absolute))
                                    });
                            }
                            InvokePropertyChanged("SearchedResult");

                            if (SearchedResult.Count >= 2000)
                            {
                                //MessageBox.Show("Only display max 2000 items，reset the query condition", "query too many result", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }

                        }));
                    }
                    else
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            SearchedResult = new ObservableCollection<SystemLogItem>();
                            InvokePropertyChanged("SearchedResult");
                        }));
                    }
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }
            });
        }
    }
}
