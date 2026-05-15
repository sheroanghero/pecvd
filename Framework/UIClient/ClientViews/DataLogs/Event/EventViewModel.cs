using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Utilities;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.Event
{
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

    public class PerPageItem
    {
        public string Value { get; set; }
    }

    public class EventViewModel : BaseModel
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

        public string SearchDSKeyWords { get; set; }

        public DateTime SearchBeginTime { get; set; }
        public DateTime SearchEndTime { get; set; }

        public string Keywords { get; set; }

        public ObservableCollection<string> EventList { get; set; }
        public string SelectedEvent { get; set; }

        public ObservableCollection<string> UserList { get; set; }
        public string SelectedUser { get; set; }

        public ObservableCollection<SystemLogItem> SearchedResult { get; set; }
        public ObservableCollection<SystemLogItem> searchedResult { get; set; }

        public Func<string, List<EventItem>> QueryDBEventFunc { get; set; }
        public Func<List<string>> QueryEventList { get; set; }

        public bool IsPermission { get => this.Permission == 3; }

        private EventView view;

        public ObservableCollection<string> SourceLP { get; set; }
        public ObservableCollection<string> sourcelp { get; set; }
        //public ObservableCollection<string> sourceLP { get; set; }
        public string SelectedValueLP { get; set; }
        public ObservableCollection<string> ToolTipValueLP { get; set; }

        public ObservableCollection<string> SourceDS { get; set; }
        public ObservableCollection<string> sourceDS { get; set; }
        public string SelectedValueDS { get; set; }

        public ICommand tbLoadPort1SelectionChangedCommand { get; set; }
        //public ICommand tbLoadPort2SelectionChangedCommand { get; set; }

        public ICommand NavigateCommand { get; set; }



        public ObservableCollection<PerPageItem> PerPageItems { get; set; }

        public bool IsLoading { get; set; }

        private int _currentPage;
        private int _countPerPage;
        private int _totalPage;

        private PerPageItem _selectedPerPage;

        public PerPageItem SelectedPerPage
        {
            get
            {
                return _selectedPerPage;
            }
            set
            {
                _selectedPerPage = value;

                Task.Run(() => DisplayResult(false));
            }
        }

        public string PageInfo { get; set; }

        private List<EventItem> _resultEvent;


        public bool EnableFirst { get; set; }
        public bool EnablePrevious { get; set; }
        public bool EnableNext { get; set; }
        public bool EnableLast { get; set; }


        public EventViewModel()
        {
            this.DisplayName = "Event";

            this.QueryDBEventFunc = (sql) => QueryDataClient.Instance.Service.QueryDBEvent(sql);

            this.QueryEventList = () =>
            {
                List<string> result = new List<string>();

                foreach (var eventName in Enum.GetNames(typeof(EventEnum)))
                    result.Add(eventName);

                return result;
            };

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
            SourceLP = new ObservableCollection<string>();
            //sourceLP = new ObservableCollection<string>();
            //SourceLP = new ObservableCollection<string>(new[] { "LP1", "LP2", "LP3","lp4" });
            //SourceDS = new ObservableCollection<string>();
            //sourceDS = new ObservableCollection<string>();
            //ToolTipValueLP = new ObservableCollection<string>();
            tbLoadPort1SelectionChangedCommand = new BaseCommand<object>(tbLoadPort1Selection);
            searchedResult = new ObservableCollection<SystemLogItem>();
            sourcelp = new ObservableCollection<string>();
            //tbLoadPort2SelectionChangedCommand = new BaseCommand<object>(tbLoadPort2Selection);




            PerPageItems = new ObservableCollection<PerPageItem>();
            PerPageItems.Add(new PerPageItem() { Value = "500" });
            PerPageItems.Add(new PerPageItem() { Value = "1000" });
            PerPageItems.Add(new PerPageItem() { Value = "3000" });
            PerPageItems.Add(new PerPageItem() { Value = "10000" });
            PerPageItems.Add(new PerPageItem() { Value = "30000" });


            _selectedPerPage = PerPageItems[2];

            PageInfo = "0/0";

            NavigateCommand = new BaseCommand<string>((o) => Navigate(o), (o) => true);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
        }

        public void Navigate(string args)
        {
            if (args == "first")
                _currentPage = 1;
            if (args == "previous")
                _currentPage--;
            if (args == "next")
                _currentPage++;
            if (args == "last")
                _currentPage = _totalPage;

            DisplayResult(false);
        }


        protected override void OnViewLoaded(object _view)
        {
            base.OnViewLoaded(_view);
            this.view = (EventView)_view;
            this.view.wfTimeFrom.Value = this.SearchBeginTime;
            this.view.wfTimeTo.Value = this.SearchEndTime;
            this.Preload();
        }


        public void Preload()
        {

            EventList = new ObservableCollection<string>();
            EventList.Add("All");
            if (QueryEventList != null)
            {
                List<string> evList = QueryEventList();
                foreach (string ev in evList)
                    EventList.Add(ev);
            }
            SelectedEvent = "All";

            if (SearchedResult == null)
                Search();
        }

        public void GetFormSearchedResult()
        {
            try
            {
                if (SearchedResult != null)
                {
                    SourceLP.Clear();
                    searchedResult.Clear();
                    SearchedResult.ToList().ForEach(i => searchedResult.Add(i));
                    SourceLP.Add("ALL");
                    sourcelp.Add("ALL");
                    foreach (var result in SearchedResult)
                    {
                        if (!SourceLP.Contains(result.TargetChamber))
                        {
                            SourceLP.Add(result.TargetChamber);
                            sourcelp.Add(result.TargetChamber);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        /// <summary>
        /// 筛选信息
        /// </summary>
        public void Filter()
        {
            try
            {
                if (SelectedValueLP != "")
                {
                    SearchedResult.Clear();
                    searchedResult.ToList().ForEach(i => SearchedResult.Add(i));
                    string[] lsvp = SelectedValueLP.Split(',');
                    SourceLP.ToList().ForEach(i => { if (!lsvp.Contains(i) && i != "ALL") SearchedResult.ToList().ForEach(m => { if (m.TargetChamber == i) SearchedResult.Remove(m); }); });// sourceLP.Add(i); });
                }
                else SearchedResult.Clear();
                if (SearchedResult != null && !string.IsNullOrWhiteSpace(SearchDSKeyWords))
                {
                    SearchedResult.ToList().ForEach(m => { if (!m.Detail.Contains(SearchDSKeyWords)) SearchedResult.Remove(m); });
                }
                NotifyOfPropertyChange("SearchedResult");
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                MessageBox.Show("筛选信息失败", "筛选失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        /// <summary>
        /// 命令
        /// </summary>
        /// <param name="o"></param>
        public void tbLoadPort1Selection(object o)
        {
            if (o != null)
            {
                var Item = o as ItemSelectionData;
                if (Item.SelectItem == "ALL")
                {
                    if (Item.IsSelect)
                    {
                        SourceLP.ToList().ForEach(sp => { if (!sourcelp.Contains(sp)) sourcelp.Add(sp); });
                    }
                    else
                    {
                        sourcelp.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// 导出
        /// </summary>
        public void Export()
        {
            try
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.DefaultExt = ".xlsx"; // Default file extension 
                dlg.FileName = $"Operation Log_{DateTime.Now:yyyyMMdd_HHmmss}";
                dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
                Nullable<bool> result = dlg.ShowDialog();// Show open file dialog box
                if (result == true) // Process open file dialog box results
                {
                    System.Data.DataSet ds = new System.Data.DataSet();
                    ds.Tables.Add(new System.Data.DataTable("系统运行日志"));
                    ds.Tables[0].Columns.Add("Type");
                    ds.Tables[0].Columns.Add("Time");
                    ds.Tables[0].Columns.Add("System");
                    ds.Tables[0].Columns.Add("Content");
                    foreach (var item in SearchedResult)
                    {
                        var row = ds.Tables[0].NewRow();
                        row[0] = item.LogType;
                        row[1] = item.Time;
                        row[2] = item.TargetChamber;
                        row[3] = item.Detail;
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
            return "";
        }


        public void Search()
        {
            if (IsLoading)
                return;

            IsLoading = true;
            NotifyOfPropertyChange("IsLoading");

            Task.Factory.StartNew(() =>
            {
                try
                {
                    this.SearchBeginTime = this.view.wfTimeFrom.Value;
                    this.SearchEndTime = this.view.wfTimeTo.Value;

                    if (SearchBeginTime > SearchEndTime)
                    {
                        IsLoading = false;
                        NotifyOfPropertyChange("IsLoading");
                        MessageBox.Show("Time range invalid, start time should be early than end time");
                        return;
                    }
                    if (Math.Round((SearchEndTime - SearchBeginTime).TotalDays, 5) > 7)
                    {
                        IsLoading = false;
                        NotifyOfPropertyChange("IsLoading");
                        MessageBox.Show("The query interval cannot exceed seven days");
                        return;
                    }
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
                        sql += " order by \"occur_time\" asc ;";

                        _resultEvent = QueryDBEventFunc(sql);

                        IsLoading = false;


                        DisplayResult(true);

                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            NotifyOfPropertyChange("IsLoading");
                            GetFormSearchedResult();
                        }));
                    }
                    else
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            SearchedResult = new ObservableCollection<SystemLogItem>();
                            NotifyOfPropertyChange("SearchedResult");
                        }));
                    }
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }
            });
        }


        private void DisplayResult(bool firstTimeDisplay)
        {
            if (IsLoading)
                return;

            SearchedResult = new ObservableCollection<SystemLogItem>();

            if (_resultEvent == null || _resultEvent.Count == 0)
            {
                PageInfo = "0/0";
                EnableFirst = EnableLast = EnableNext = EnablePrevious = false;

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    NotifyOfPropertyChange(nameof(SearchedResult));
                    NotifyOfPropertyChange(nameof(PageInfo));
                    NotifyOfPropertyChange(nameof(EnableFirst));
                    NotifyOfPropertyChange(nameof(EnableLast));
                    NotifyOfPropertyChange(nameof(EnableNext));
                    NotifyOfPropertyChange(nameof(EnablePrevious));
                }));

                return;
            }

            if (SelectedPerPage==null || SelectedPerPage.Value == "All" || string.IsNullOrEmpty(SelectedPerPage.Value))
            {
                _countPerPage = _resultEvent.Count;
            }
            else
            {
                _countPerPage = int.Parse(SelectedPerPage.Value);
            }

            if (firstTimeDisplay)
            {
                _currentPage = 1;
            }

            _totalPage = _resultEvent.Count / _countPerPage + (_resultEvent.Count % _countPerPage > 0 ? 1 : 0);

            if (_currentPage < 1)
                _currentPage = 1;

            if (_currentPage > _totalPage)
                _currentPage = _totalPage;

            EnableFirst = _currentPage > 1;
            EnablePrevious = _currentPage > 1;
            EnableNext = _currentPage < _totalPage;
            EnableLast = _currentPage < _totalPage;

            PageInfo = $"{_currentPage}/{_totalPage}";

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                NotifyOfPropertyChange(nameof(SearchedResult));
                NotifyOfPropertyChange(nameof(PageInfo));
                NotifyOfPropertyChange(nameof(EnableFirst));
                NotifyOfPropertyChange(nameof(EnableLast));
                NotifyOfPropertyChange(nameof(EnableNext));
                NotifyOfPropertyChange(nameof(EnablePrevious));
            }));

            //2000一次，显示全部页面

            int from = (_currentPage - 1) * _countPerPage;

            for (int i = 0; i < _countPerPage; i = i + 2000)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action<int>((index) =>
                {
                    string logTypeStr;

                    for (int j = from+index; j - index-from < Math.Min(2000,_countPerPage)  && j < _resultEvent.Count; j++)
                    {
                        EventItem ev = _resultEvent[j];
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
                            Icon = new BitmapImage(new Uri(string.Format("pack://application:,,,/MECF.Framework.UI.Core;component/Resources/SystemLog/{0}.png", ev.Level.ToString()), UriKind.Absolute))
                        });
                    }
                    NotifyOfPropertyChange("SearchedResult");
                }), i);

                Thread.Sleep(300);
            }


        }
 
    }
}

