using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Utilities;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.Annotations;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Data.Model;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.WaferHistory
{
     public class WaferProcess
    {
        public string ProcessTime { get; set; }
        public string Station { get; set; }
        public string DataName { get; set; }
        public string DataValue { get; set; }
    }
    public class WaferHistory2ViewModel : BaseModel
    {
        public bool IsPermission { get => this.Permission == 3; }

        public DateTime BeginDate { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public DateTime UIEndTime { get; set; }


        public ObservableCollection<WaferHistoryDetail> Wafers { get; set; }
        public ObservableCollection<WaferMovement> Movements { get; set; }

        public ObservableCollection<WaferProcess> Processes { get; set; } = new ObservableCollection<WaferProcess>();

        public ObservableCollection<string> SourceLP { get; set; }
        public string SelectedValueLP { get; set; }

        public WaferHistory2ViewModel()
        {
            this.DisplayName = "Wafer History";
            var now = DateTime.Now;
            this.StartDateTime = now;
            this.BeginDate = now;
            this.StartDateTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, 0);
            this.EndDateTime = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, 999);
            this.Wafers = new ObservableCollection<WaferHistoryDetail>();
            this.Movements = new ObservableCollection<WaferMovement>();


            SourceLP = new ObservableCollection<string>(new[] { "LP1", "LP2", "LP3" });
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            this.YLabelProvider = new YAxisLabelProvider();
            this.RenderableSeries = new ObservableCollection<IRenderableSeries>();
            this.RenderableSeries.Add(new FastLineRenderableSeries() { DataSeries = new XyDataSeries<int, int>() });
            this.Annotations = new AnnotationCollection();
        }

        private WaferHistory2View view;
        public void Query(string loadport, string waferid, string lotid, string carrierid, string sequencename, string processchamber, string processrecipe)
        {
            try
            {
                this.StartDateTime = this.view.wfTimeFrom.Value;
                this.EndDateTime = this.view.wfTimeTo.Value;

                if (StartDateTime > EndDateTime)
                {
                    MessageBox.Show("Time range invalid, start time should be early than end time");
                    return;
                }

                string sql = string.Format(
                    "SELECT * FROM \"wafer_data\",\"carrier_data\" where \"create_time\" >= '{0}' and \"create_time\" <= '{1}' and \"wafer_data\".\"carrier_data_guid\"=\"carrier_data\".\"guid\"",
                    StartDateTime.ToString("yyyy/MM/dd HH:mm:ss.fff"), EndDateTime.ToString("yyyy/MM/dd HH:mm:ss.fff"));

                if (!string.IsNullOrWhiteSpace(loadport))
                {
                    sql += " and (FALSE ";
                    var loadPorts = loadport.Split(',');
                    for (int i = 0; i < loadPorts.Length; i++)
                    {
                        sql += $" OR \"create_station\"='{loadPorts[i]}'";
                    }
                    sql += " )";
                }

                if (!string.IsNullOrWhiteSpace(waferid))
                    sql += $" and lower(\"wafer_id\") like '%{waferid.ToLower()}%'";

                if (!string.IsNullOrWhiteSpace(lotid))
                    sql += $" and lower(\"wafer_data\".\"lot_id\") like '%{lotid.ToLower()}%'";

                if (!string.IsNullOrWhiteSpace(carrierid))
                    sql += $" and lower(\"rfid\") like '%{carrierid.ToLower()}%'";

                sql += " order by \"create_time\" ASC, \"create_station\" ASC,\"create_slot\" ASC";

                DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Wafers.Clear();
                    try
                    {
                        if (dbData == null || dbData.Rows.Count == 0)
                            return;

                        for (int i = 0; i < dbData.Rows.Count; i++)
                        {
                            WaferHistoryDetail item = new WaferHistoryDetail();

                            item.Guid = dbData.Rows[i]["guid"].ToString();
                            item.LoadPort = dbData.Rows[i]["create_station"].ToString();
                            item.Slot = dbData.Rows[i]["create_slot"].ToString();

                            item.CarrierID = dbData.Rows[i]["rfid"].ToString();

                            item.LotID = dbData.Rows[i]["lot_id"].ToString();
                            item.WaferID = dbData.Rows[i]["wafer_id"].ToString();

                            item.Status = dbData.Rows[i]["process_status"].ToString();

                            if (!dbData.Rows[i]["create_time"].Equals(DBNull.Value))
                                item.CreateTime = ((DateTime)dbData.Rows[i]["create_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                            if (!dbData.Rows[i]["delete_time"].Equals(DBNull.Value))
                                item.DeleteTime = ((DateTime)dbData.Rows[i]["delete_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                            Wafers.Add(item);

                        }
                    }
                    catch (Exception ex)
                    {
                        LOG.Write(ex);
                    }

                }));
            }
            catch (Exception e)
            {
                LOG.Write(e);
            }
        }
        public void WaferHistoryChanged(WaferHistoryDetail detail)
        {
            try
            {
                if (detail == null)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Movements.Clear();
                    }));
                    return;
                }

                string sql = string.Format(
                    "SELECT * FROM \"wafer_move_history\" where \"wafer_data_guid\" = '{0}'  order by \"arrive_time\" ASC;",
                    detail.Guid);

                DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Movements.Clear();

                    if (dbData == null || dbData.Rows.Count == 0)
                        return;

                    for (int i = 0; i < dbData.Rows.Count; i++)
                    {
                        WaferMovement item = new WaferMovement();

                        item.Station = dbData.Rows[i]["station"].ToString();
                        item.Slot = dbData.Rows[i]["slot"].ToString();
                        item.Status = dbData.Rows[i]["status"].ToString();
                        item.WaferID = detail.WaferID;

                        if (!dbData.Rows[i]["arrive_time"].Equals(DBNull.Value))
                            item.Time = ((DateTime)dbData.Rows[i]["arrive_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");

                        Movements.Add(item);
                    }


                }));

                string sql2= string.Format(
                    "SELECT * FROM \"wafer_process_data\" where \"wafer_guid\" = '{0}'  order by \"process_time\" ASC;",
                    detail.Guid);

                DataTable dbData2 = QueryDataClient.Instance.Service.QueryData(sql2);

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Processes.Clear();

                    if (dbData2 == null || dbData2.Rows.Count == 0)
                        return;

                    for (int i = 0; i < dbData2.Rows.Count; i++)
                    {
                        WaferProcess item = new WaferProcess();
                        if (!dbData2.Rows[i]["process_time"].Equals(DBNull.Value))
                            item.ProcessTime = ((DateTime)dbData2.Rows[i]["process_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");


                        item.Station = dbData2.Rows[i]["station"].ToString();
                        item.DataName = dbData2.Rows[i]["data_name"].ToString();
                        item.DataValue = dbData2.Rows[i]["data_value"].ToString();

                        
                     
                        Processes.Add(item);
                    }


                }));
            }
            catch (Exception e)
            {
                LOG.Write(e);
            }
        }

        public void Export()
        {
            try
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.DefaultExt = ".xlsx"; // Default file extension 
                dlg.FileName = $"{DisplayName}_{DateTime.Now:yyyyMMdd_HHmmss}";
                dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
                Nullable<bool> result = dlg.ShowDialog();// Show open file dialog box
                if (result == true) // Process open file dialog box results
                {
                    System.Data.DataSet ds = new System.Data.DataSet();
                    ds.Tables.Add(new System.Data.DataTable("系统运行日志"));
                    ds.Tables[0].Columns.Add("CreateTime");
                    ds.Tables[0].Columns.Add("DeleteTime");
                    ds.Tables[0].Columns.Add("LoadPort");
                    ds.Tables[0].Columns.Add("Slot");
                    ds.Tables[0].Columns.Add("CarrierID");
                    ds.Tables[0].Columns.Add("LotID");
                    ds.Tables[0].Columns.Add("WaferID");
                    ds.Tables[0].Columns.Add("Status");
                    foreach (var item in Wafers)
                    {
                        var row = ds.Tables[0].NewRow();
                        row[0] = item.CreateTime;
                        row[1] = item.DeleteTime;
                        row[2] = item.LoadPort;
                        row[3] = item.Slot;
                        row[4] = item.CarrierID;
                        row[5] = item.LotID;
                        row[6] = item.WaferID;
                        row[7] = item.Status;
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

        public void ExportMove()
        {
            try
            {
                if (Movements.Count == 0)
                {
                    DialogBox.ShowDialog(DialogButton.OK, DialogType.INFO, "No wafer move history to export.");
                    return;
                }

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.DefaultExt = ".xlsx"; // Default file extension 
                dlg.FileName = $"Wafer_Move_History_{DateTime.Now:yyyyMMdd_HHmmss}";
                dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
                Nullable<bool> result = dlg.ShowDialog();// Show open file dialog box
                if (result == true) // Process open file dialog box results
                {
                    System.Data.DataSet ds = new System.Data.DataSet();
                    ds.Tables.Add(new System.Data.DataTable("系统运行日志"));
                    ds.Tables[0].Columns.Add("Time");
                    ds.Tables[0].Columns.Add("Station");
                    ds.Tables[0].Columns.Add("Slot");
                    ds.Tables[0].Columns.Add("Status");
                    ds.Tables[0].Columns.Add("WaferId");
                    foreach (var item in Movements)
                    {
                        var row = ds.Tables[0].NewRow();
                        row[0] = item.Time;
                        row[1] = item.Station;
                        row[2] = item.Slot;
                        row[3] = item.Status;
                        row[4] = item.WaferID;
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
                MessageBox.Show($"导出系统日志发生错误", "导出失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #region scichart

        public ObservableCollection<IRenderableSeries> RenderableSeries
        {
            get;
            set;
        }

        private AnnotationCollection _annotations = new AnnotationCollection();
        public AnnotationCollection Annotations
        {
            get { return _annotations; }
            set { _annotations = value; }
        }

        private IRange _TimeRange;
        public IRange TimeRange
        {
            get { return _TimeRange; }
            set
            {
                _TimeRange = value;
                NotifyOfPropertyChange("TimeRange");

            }
        }

        private IRange _YTimeRange;
        public IRange YTimeRange
        {
            get { return _YTimeRange; }
            set { _YTimeRange = value; }
        }


        public YAxisLabelProvider YLabelProvider { get; set; }
        public void AppendData()
        {
            (this.RenderableSeries[0].DataSeries as XyDataSeries<int, int>).Clear();

            for (var index = 1; index <= 8; index++)
            {
                (this.RenderableSeries[0].DataSeries as XyDataSeries<int, int>).Append(index * 5, index);

                this.Annotations.Add(new LineArrowAnnotation() { X1 = index * 5, Y1 = index, X2 = index * 5, Y2 = index + 2, });
            }

        }

        protected override void OnViewLoaded(object _view)
        {
            base.OnViewLoaded(_view);
            this.view = (WaferHistory2View)_view;
            this.view.wfTimeFrom.Value = this.StartDateTime;
            this.view.wfTimeTo.Value = this.EndDateTime;
            this.AppendData();
        }

        #endregion
    }
}
