using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using Aitex.Core.RT.Log;
using Aitex.Core.UI.ControlDataContext;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.ControlDataContext;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Utilities;
using MECF.Framework.UI.Client.CenterViews.DataLogs.ProcessHistory;
using MECF.Framework.UI.Client.CenterViews.Operations.RealTime;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Data.Model;
using Cali = Caliburn.Micro;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.DataHistory
{
    public class TimeChartDataLine : ChartDataLine
    {
        public string Module { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime TokenTime { get; set; }

        private string[] _dbModules = { "PM1", "PM2", "PM3", "PM4","PM5", "PM6", "PM7" };

        public TimeChartDataLine(string dataName, DateTime startTime, DateTime endTime) : base(dataName)
        {
            StartTime = startTime;
            EndTime = endTime;
            TokenTime = startTime;
            Module = "System";
            foreach (var dbModule in _dbModules)
            {
                if (dataName.StartsWith(dbModule))
                {
                    Module = dbModule;
                    break;
                }
            }
        }
    }

    public class DataViewModel : UiViewModelBase
    {
        public bool IsPermission { get => this.Permission == 3; }

        private class QueryIndexer
        {
            public DateTime TimeToken { get; set; }
            public List<string> DataList { get; set; }
            public string Module { get; set; }
        }

        #region Property
        private const int MAX_PARAMETERS = 20;

        private Queue<Color> colorQueue = new Queue<Color>(new Color[]{Color.Red,Color.Orange,Color.Yellow,Color.Green,Color.Blue,Color.Pink,Color.Purple,Color.Aqua,Color.Bisque,Color.Brown,Color.BurlyWood,Color.CadetBlue,
            Color.CornflowerBlue,Color.DarkBlue,Color.DarkCyan,Color.DarkGray,Color.DarkGreen,Color.DarkKhaki,Color.DarkMagenta,Color.DarkOliveGreen, Color.DarkOrange,
            Color.DarkSeaGreen,Color.DarkSlateBlue,Color.DarkSlateGray,Color.DarkViolet,Color.DeepPink,Color.DeepSkyBlue,Color.DimGray, Color.DodgerBlue,Color.ForestGreen, Color.Gold,
            Color.Gray,Color.GreenYellow,Color.HotPink,Color.Indigo,Color.Khaki,Color.LightBlue,Color.LightCoral,Color.LightGreen, Color.LightPink,Color.LightSalmon,Color.LightSkyBlue,
            Color.LightSlateGray,Color.LightSteelBlue,Color.LimeGreen,Color.MediumOrchid,Color.MediumPurple,Color.MediumSeaGreen,Color.MediumSlateBlue,Color.MediumSpringGreen,
            Color.MediumTurquoise,Color.Moccasin,Color.NavajoWhite,Color.Olive,Color.OliveDrab,Color.OrangeRed,Color.Orchid,Color.PaleGoldenrod,Color.PaleGreen,
            Color.PeachPuff,Color.Peru,Color.Plum,Color.PowderBlue,Color.RosyBrown,Color.RoyalBlue,Color.SaddleBrown,Color.Salmon,Color.SeaGreen, Color.Sienna,
            Color.SkyBlue,Color.SlateBlue,Color.SlateGray,Color.SpringGreen,Color.Teal,Color.Aquamarine,Color.Tomato,Color.Turquoise,Color.Violet,Color.Wheat, Color.YellowGreen});

        private ObservableCollection<ParameterNode> _ParameterNodes;
        public ObservableCollection<ParameterNode> ParameterNodes
        {
            get { return _ParameterNodes; }
            set { _ParameterNodes = value; NotifyOfPropertyChange("ParameterNodes"); }
        }

        public ObservableCollection<IRenderableSeries> SelectedData { get; set; }
        Cali.WindowManager wm = new Cali.WindowManager();
        SelectUserDefineViewModel selectDataDlg = new SelectUserDefineViewModel();

        private object _lockSelection = new object();
        private object _lockQueryCondition = new object();

        private AutoRange _autoRange;
        public AutoRange ChartAutoRange
        {
            get { return _autoRange; }
            set
            {
                _autoRange = value;
                NotifyOfPropertyChange(nameof(ChartAutoRange));
            }
        }


        private IRange _timeRange;
        public IRange VisibleRangeTime
        {
            get { return _timeRange; }
            set
            {
                _timeRange = value;
                NotifyOfPropertyChange(nameof(VisibleRangeTime));
            }
        }


        private IRange _VisibleRangeValue;
        public IRange VisibleRangeValue
        {
            get { return _VisibleRangeValue; }
            set { _VisibleRangeValue = value; NotifyOfPropertyChange(nameof(VisibleRangeValue)); }
        }

        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }

        public DateTime _queryDateTimeToken;
        //private bool _restart;

        DeviceTimer dt = new DeviceTimer();
        int statisticsCount = 0;

        private PeriodicJob _thread;

        ConcurrentBag<QueryIndexer> _lstTokenTimeData = new ConcurrentBag<QueryIndexer>();

        RealtimeProvider _provider = new RealtimeProvider();
        //private int _selectedDataCount;
        #endregion

        #region Function
        public DataViewModel()
        {
            DisplayName = "Data History";

            SelectedData = new ObservableCollection<IRenderableSeries>();

            ParameterNodes = _provider.GetParameters();

            var now = DateTime.Now;
            StartDateTime = now.AddHours(-2);
            EndDateTime = now;
            _queryDateTimeToken = StartDateTime;

            VisibleRangeTime = new DateRange(DateTime.Now.AddMinutes(60), DateTime.Now.AddMinutes(-60));
            VisibleRangeValue = new DoubleRange(0, 10);

            _thread = new PeriodicJob(200, MonitorData, "RealTime", true);
        }

        protected override void OnViewLoaded(object _view)
        {
            base.OnViewLoaded(_view);
            this.view = (DataView)_view;
            this.view.wfTimeFrom.Value = this.StartDateTime;
            this.view.wfTimeTo.Value = this.EndDateTime;
        }

        protected override void OnActivate()
        {
            base.OnActivate();

        }

        protected bool MonitorData()
        {
            try
            {
                bool allUpdated = true;
                lock (_lockSelection)
                {
                    foreach (var item in _lstTokenTimeData)
                    {
                        DateTime timeFrom = item.TimeToken;
                        if (timeFrom >= EndDateTime)
                            continue;

                        allUpdated = false;
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => { ChartAutoRange = AutoRange.Always; }));
                        DateTime timeTo = timeFrom.AddMinutes(60);
                        if (timeTo.DayOfYear > timeFrom.DayOfYear)
                            timeTo = new DateTime(timeFrom.Year, timeFrom.Month, timeFrom.Day).AddDays(1);
                        if (timeTo > EndDateTime)
                            timeTo = EndDateTime;

                        item.TimeToken = timeTo;

                        GetData(item.DataList, timeFrom, timeTo, item.Module);

                        #region Update   VisualMin、VisualMax、DataStatisticsInfo

                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            double min = ((DoubleRange)VisibleRangeValue).Min;
                            double max = ((DoubleRange)VisibleRangeValue).Max;
                            int PointCount = 0;
                            foreach (var selectedData in SelectedData)
                            {
                                var seriesItem = selectedData as ChartDataLine;
                                if (seriesItem == null)
                                    continue;

                                double sumValue = 0;
                                double minValue = 0;
                                double maxValue = 0;

                                double.TryParse((seriesItem.Tag as ParameterNode).AverageValue, out double perAverageVlaue);
                                double perSumValue = perAverageVlaue * statisticsCount;
                                double.TryParse((seriesItem.Tag as ParameterNode).MinValue, out double perMinValue);
                                double.TryParse((seriesItem.Tag as ParameterNode).MaxValue, out double perMaxValue);

                                var pointValueList = seriesItem.Points.Select(x => x.Item2).ToList();
                                for (int i = statisticsCount; i < pointValueList.Count; i++)
                                {
                                    sumValue += pointValueList[i];
                                    if (pointValueList[i] < minValue)
                                        minValue = pointValueList[i];
                                    if (pointValueList[i] > maxValue)
                                        maxValue = pointValueList[i];
                                }

                                if (pointValueList.Count > statisticsCount)
                                {
                                    (seriesItem.Tag as ParameterNode).AverageValue = ((perSumValue + sumValue) / pointValueList.Count).ToString("F2");
                                    (seriesItem.Tag as ParameterNode).MinValue = ((perMinValue > minValue) ? minValue : perMinValue).ToString("F2");
                                    (seriesItem.Tag as ParameterNode).MaxValue = ((perMaxValue > maxValue) ? perMaxValue : maxValue).ToString("F2");
                                }

                                min = (perMinValue > minValue) ? minValue : perMaxValue;
                                max = (perMaxValue > maxValue) ? perMaxValue : maxValue;
                                PointCount = pointValueList.Count;
                            }

                            statisticsCount = PointCount;
                            VisibleRangeValue = new DoubleRange(min, max);
                        }));

                        #endregion

                        #region Debug

                        Trace.WriteLine(this.GetType() + "  SpendTime  :  " + dt.GetElapseTime());
                        LOG.Write(this.GetType() + "  SpendTime  :  " + dt.GetElapseTime());

                        #endregion
                    }
                }

                if (allUpdated)
                {
                    lock (_lockSelection)
                    {
                        while (_lstTokenTimeData.Count > 0)
                        {
                            _lstTokenTimeData.TryTake(out _);
                        }
                    }

                    #region Debug
                    if (!dt.IsIdle())
                    {
                        Trace.WriteLine(this.GetType() + "  " + StartDateTime.ToString("yyyy/MM/dd HH:mm:ss") + "----" + EndDateTime.ToString("MM/dd/yyyy HH:mm:ss") + "  AllSpendTime  :  " + dt.GetElapseTime());
                        LOG.Write(this.GetType() + "  " + StartDateTime.ToString("yyyy/MM/dd HH:mm:ss") + "----" + EndDateTime.ToString("MM/dd/yyyy HH:mm:ss") + "  AllSpendTime  :  " + dt.GetElapseTime());
                        dt.Stop();

                    }
                    #endregion

                    Application.Current.Dispatcher.BeginInvoke(new Action(() => { ChartAutoRange = AutoRange.Never; }));

                }

            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }

            return true;
        }

        public void Preset()
        {

        }

        public void Clear()
        {

        }

        private void GetData(List<string> keys, DateTime from, DateTime to, string module)
        {
            string sql = "select time AS InternalTimeStamp";
            foreach (var dataId in keys)
            {
                sql += "," + string.Format("\"{0}\"", dataId);
            }
            sql += string.Format(" from \"{0}\" where time > {1} and time <= {2} order by time asc",
                from.ToString("yyyyMMdd") + "." + module, from.Ticks, to.Ticks);

            DataTable dataTable = QueryDataClient.Instance.Service.QueryData(sql);

            Dictionary<string, List<HistoryDataItem>> historyData = new Dictionary<string, List<HistoryDataItem>>();
            if (dataTable == null || dataTable.Rows.Count == 0)
                return;

            DateTime dt = new DateTime();
            Dictionary<int, string> colName = new Dictionary<int, string>();
            for (int colNo = 0; colNo < dataTable.Columns.Count; colNo++)
            {
                colName.Add(colNo, dataTable.Columns[colNo].ColumnName);
                historyData[dataTable.Columns[colNo].ColumnName] = new List<HistoryDataItem>();
            }
            for (int rowNo = 0; rowNo < dataTable.Rows.Count; rowNo++)
            {
                var row = dataTable.Rows[rowNo];

                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    HistoryDataItem data = new HistoryDataItem();
                    if (i == 0)
                    {
                        long ticks = (long)row[i];
                        dt = new DateTime(ticks);
                        continue;
                    }
                    else
                    {
                        string dataId = colName[i];
                        if (row[i] is DBNull || row[i] == null)
                        {
                            data.dateTime = dt;
                            data.dbName = colName[i];
                            data.value = 0;
                        }
                        else if (row[i] is bool)
                        {
                            data.dateTime = dt;
                            data.dbName = colName[i];
                            data.value = (bool)row[i] ? 1 : 0;
                        }
                        else
                        {
                            data.dateTime = dt;
                            data.dbName = colName[i];
                            data.value = float.Parse(row[i].ToString());
                        }
                    }
                    historyData[data.dbName].Add(data);
                }
            }

            foreach (var item in historyData)
            {
                item.Value.Sort((x, y) => DateTime.Compare(x.dateTime, y.dateTime));
            }

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    //double min = ((DoubleRange)VisibleRangeValue).Min;
                    //double max = ((DoubleRange)VisibleRangeValue).Max;

                    foreach (var item in SelectedData)
                    {
                        var seriesItem = item as ChartDataLine;

                        if (seriesItem == null)
                            continue;
                        //if (historyData.ContainsKey(seriesItem.DisplayName))
                        //{
                        //    (seriesItem.Tag as ParameterNode).AverageValue = historyData[seriesItem.DisplayName].Average(it => it.value).ToString("F2");
                        //    (seriesItem.Tag as ParameterNode).MinValue = historyData[seriesItem.DisplayName].Min(it => it.value).ToString("F2");
                        //    (seriesItem.Tag as ParameterNode).MaxValue = historyData[seriesItem.DisplayName].Max(it => it.value).ToString("F2");
                        //}

                        foreach (var data in historyData)
                        {
                            if (data.Key != seriesItem.DataName)
                                continue;

                            seriesItem.Capacity += data.Value.Count;

                            foreach (var historyDataItem in data.Value)
                            {
                                //if (historyDataItem.value < min)
                                //    min = historyDataItem.value;
                                //if (historyDataItem.value > max)
                                //    max = historyDataItem.value;

                                seriesItem.Append(historyDataItem.dateTime, historyDataItem.value);
                            }
                        }
                    }


                    //VisibleRangeValue = new DoubleRange(min, max);


                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }

            }));
        }

        private void SelectedDataChanged()
        {
            foreach (var item in SelectedData)
            {
                if (item.Stroke.Equals(System.Windows.Media.Color.FromArgb(255, 0, 0, 255)))
                {
                    Color drawingColor = colorQueue.Peek();
                    item.Stroke = System.Windows.Media.Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
                    colorQueue.Enqueue(colorQueue.Dequeue());
                }
            }
        }

        private DataView view;



        public void Query(object parameter)
        {
            WaferHistoryRecipe waferHistoryRecipe = parameter as WaferHistoryRecipe;
            if (waferHistoryRecipe != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (waferHistoryRecipe.StartTime != DateTime.MinValue)
                        this.view.wfTimeFrom.Value = waferHistoryRecipe.StartTime;
                    if (waferHistoryRecipe.EndTime != DateTime.MinValue)
                        this.view.wfTimeTo.Value = waferHistoryRecipe.EndTime;
                    else
                        this.view.wfTimeTo.Value = waferHistoryRecipe.StartTime.AddHours(1);
                    //ParameterNodes.ForEachDo((x) =>
                    //{
                    //    x.Selected = true;
                    //    ParameterCheck(x);
                    //});
                    Query();
                }));
            }
        }

        public void Query()
        {
            this.StartDateTime = this.view.wfTimeFrom.Value;
            this.EndDateTime = this.view.wfTimeTo.Value;

            if (StartDateTime > EndDateTime)
            {
                MessageBox.Show("Time range invalid, start time should be early than end time");
                return;
            }

            ParameterNodes = _provider.GetParameters();

            VisibleRangeTime = new DateRange(StartDateTime.AddMinutes(-5), EndDateTime.AddMinutes(5));

            ChartAutoRange = AutoRange.Always;

            lock (_lockQueryCondition)
            {
                _queryDateTimeToken = StartDateTime;
                //_restart = true;

                while (!_lstTokenTimeData.IsEmpty)
                {
                    _lstTokenTimeData.TryTake(out _);
                }

                foreach (var dataLine in SelectedData)
                {
                    TimeChartDataLine line = dataLine as TimeChartDataLine;
                    line.ClearData();


                    QueryIndexer indexer = _lstTokenTimeData.FirstOrDefault(x => x.Module == line.Module);

                    if (indexer == null)
                    {
                        indexer = new QueryIndexer()
                        {
                            DataList = new List<string>(),
                            Module = line.Module,
                            TimeToken = StartDateTime,
                        };
                        _lstTokenTimeData.Add(indexer);
                    }

                    indexer.DataList.Add(line.DataName);
                }

                #region Debug
                dt.Start(0);
                #endregion
            }
        }


        public void ParameterCheck(ParameterNode node)
        {
            bool result = RefreshTreeStatusToChild(node);
            if (!result)
            {
                node.Selected = !node.Selected;
                DialogBox.ShowWarning($"The max number of parameters is {MAX_PARAMETERS}.");
            }
            else
            {
                RefreshTreeStatusToParent(node);
            }
        }

        /// <summary>
        /// Refresh tree node status from current to children, and add data to SelectedData
        /// </summary>
        private bool RefreshTreeStatusToChild(ParameterNode node)
        {
            if (node.ChildNodes.Count > 0)
            {
                for (int i = 0; i < node.ChildNodes.Count; i++)
                {
                    ParameterNode n = node.ChildNodes[i];
                    n.Selected = node.Selected;

                    if (!RefreshTreeStatusToChild(n))
                    {
                        //uncheck left node
                        for (int j = i; j < node.ChildNodes.Count; j++)
                        {
                            node.ChildNodes[j].Selected = !node.Selected;
                        }
                        //node.Selected = !node.Selected;
                        return false;
                    }
                }
            }
            else //leaf node
            {
                lock (_lockSelection)
                {
                    bool isExist = SelectedData.FirstOrDefault(x => (x as ChartDataLine).DataName == node.Name) != null;
                    if (node.Selected && !isExist)
                    {
                        if (SelectedData.Count < MAX_PARAMETERS)
                        {
                            var line = new TimeChartDataLine(node.Name, StartDateTime, EndDateTime);
                            line.Tag = node;
                            SelectedData.Add(line);
                            SelectedDataChanged();
                            QueryIndexer indexer = _lstTokenTimeData.FirstOrDefault(x => x.Module == line.Module);

                            if (indexer == null)
                            {
                                indexer = new QueryIndexer()
                                {
                                    DataList = new List<string>(),
                                    Module = line.Module,
                                    TimeToken = StartDateTime,
                                };
                                _lstTokenTimeData.Add(indexer);
                            }

                            indexer.DataList.Add(line.DataName);


                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (!node.Selected && isExist)
                    {
                        //SelectedParameters.Remove(node.Name);
                        var data = SelectedData.FirstOrDefault(d => (d as ChartDataLine).DataName == node.Name);
                        SelectedData.Remove(data);
                    }
                }

            }
            return true;
        }

        /// <summary>
        /// Refresh tree node status from current to parent
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private void RefreshTreeStatusToParent(ParameterNode node)
        {
            if (node.ParentNode != null)
            {
                if (node.Selected)
                {
                    bool flag = true;
                    for (int i = 0; i < node.ParentNode.ChildNodes.Count; i++)
                    {
                        if (!node.ParentNode.ChildNodes[i].Selected)
                        {
                            flag = false;  //as least one child is unselected
                            break;
                        }
                    }
                    if (flag)
                        node.ParentNode.Selected = true;
                }
                else
                {
                    node.ParentNode.Selected = false;
                }
                RefreshTreeStatusToParent(node.ParentNode);
            }
        }

        public void SelectData()
        {
            selectDataDlg.SelectedParameters.Clear();
            selectDataDlg.SelectedParameters = _provider.GetUserDefineParameters();

            var settings = new Dictionary<string, object> { { "Title", "Select User Define" } };
            bool? ret = wm.ShowDialog(selectDataDlg, null, settings);
            if (ret == null || !ret.Value)
                return;

            QueryDataClient.Instance.Service.SetTypedConfigContent("UserDefine", "", string.Join(",", selectDataDlg.SelectedParameters));

            _provider.Clear();
            ParameterNodes = _provider.GetParameters();
        }

        #region Parameter Grid Control
        public void DeleteAll()
        {
            //uncheck all tree nodes
            foreach (ChartDataLine cp in SelectedData)
            {
                (cp.Tag as ParameterNode).Selected = false;
                RefreshTreeStatusToParent(cp.Tag as ParameterNode);
            }

            SelectedData.Clear();
        }

        private void SetParameterNode(ObservableCollection<ParameterNode> nodes, bool isChecked)
        {
            foreach (ParameterNode n in nodes)
            {
                n.Selected = isChecked;
                SetParameterNode(n.ChildNodes, isChecked);
            }
        }

        public void Delete(ChartDataLine cp)
        {
            if (cp != null && SelectedData.Contains(cp))
            {
                //uncheck tree node
                (cp.Tag as ParameterNode).Selected = false;
                RefreshTreeStatusToParent(cp.Tag as ParameterNode);

                SelectedData.Remove(cp);
            }

        }

        public void ExportAll()
        {
            try
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.DefaultExt = ".xlsx"; // Default file extension 
                dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
                dlg.FileName = $"{DisplayName}_{DateTime.Now:yyyyMMdd_HHmmss}";
                if (SelectedData.Count == 0)
                {
                    MessageBox.Show($"Please select the data you want to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Nullable<bool> result = dlg.ShowDialog();// Show open file dialog box
                if (result == true) // Process open file dialog box results
                {
                    System.Data.DataSet ds = new System.Data.DataSet();
                    ds.Tables.Add(new System.Data.DataTable($"{DisplayName}_{DateTime.Now:yyyyMMdd_HHmmss}"));
                    ds.Tables[0].Columns.Add("Time");
                    ds.Tables[0].Columns[0].DataType = typeof(DateTime);

                    lock (_lockSelection)
                    {
                        Dictionary<DateTime, double[]> timeValue = new Dictionary<DateTime, double[]>();

                        for (int i = 0; i < SelectedData.Count; i++)
                        {
                            List<Tuple<DateTime, double>> data = (SelectedData[i] as ChartDataLine).Points;
                            foreach (var tuple in data)
                            {
                                if (!timeValue.ContainsKey(tuple.Item1))
                                    timeValue[tuple.Item1] = new double[SelectedData.Count];

                                timeValue[tuple.Item1][i] = tuple.Item2;
                            }

                            ds.Tables[0].Columns.Add((SelectedData[i] as ChartDataLine).DataName);
                            ds.Tables[0].Columns[i + 1].DataType = typeof(double);
                        }

                        foreach (var item in timeValue)
                        {
                            var row = ds.Tables[0].NewRow();
                            row[0] = item.Key;
                            for (int j = 0; j < item.Value.Length; j++)
                            {
                                row[j + 1] = item.Value[j];
                            }
                            ds.Tables[0].Rows.Add(row);
                        }
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
                MessageBox.Show("Write failed," + ex.Message, "export failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void Export(ChartDataLine cp)
        {
            try
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.DefaultExt = ".xlsx"; // Default file extension 
                dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
                dlg.FileName = $"{cp.DataName}_{DateTime.Now:yyyyMMdd_HHmmss}";
                Nullable<bool> result = dlg.ShowDialog();// Show open file dialog box
                if (result == true) // Process open file dialog box results
                {
                    System.Data.DataSet ds = new System.Data.DataSet();
                    ds.Tables.Add(new System.Data.DataTable(cp.DataName));
                    ds.Tables[0].Columns.Add("Time");
                    ds.Tables[0].Columns[0].DataType = typeof(DateTime);
                    ds.Tables[0].Columns.Add(cp.DataName);
                    ds.Tables[0].Columns[1].DataType = typeof(double);

                    foreach (var item in cp.Points)
                    {
                        var row = ds.Tables[0].NewRow();
                        row[0] = item.Item1;
                        row[1] = item.Item2;
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
                MessageBox.Show("Write failed," + ex.Message, "export failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void SelectColor(ChartDataLine cp)
        {
            if (cp == null)
                return;

            var dlg = new System.Windows.Forms.ColorDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                cp.Stroke = new System.Windows.Media.Color() { A = dlg.Color.A, B = dlg.Color.B, G = dlg.Color.G, R = dlg.Color.R };

            }
        }

        #endregion

        #endregion
    }
}
