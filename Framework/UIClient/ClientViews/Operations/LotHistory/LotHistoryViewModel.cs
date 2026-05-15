using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Aitex.Core.RT.Log;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Util;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.DataLogs.WaferHistory;
using MECF.Framework.UI.Client.ClientBase;
using SciChart.Core.Extensions;

namespace MECF.Framework.UI.Client.CenterViews.Operations.LotHistory
{
    //public class WaferHistoryDetail
    //{
    //    public string Guid { get; set; }
    //    public string CreateTime { get; set; }
    //    public string DeleteTime { get; set; }
    //    public string LoadPort { get; set; }
    //    public string Slot { get; set; }
    //    public string CarrierID { get; set; }
    //    public string LotID { get; set; }
    //    public string WaferID { get; set; }
    //    public string Status { get; set; }
    //}

    //public class WaferMovement
    //{
    //    public string Time { get; set; }
    //    public string Station { get; set; }
    //    public string Slot { get; set; }
    //    public string Status { get; set; }
    //}

    /*
     *
     *   start_time timestamp without time zone,
  end_time timestamp without time zone,
  carrier_data_guid text,
  name text,
  input_port text,
  output_port text,
  total_wafer_count integer,
  abort_wafer_count integer,
  unprocessed_wafer_count integer,
  CONSTRAINT pj_data_pkey PRIMARY KEY (guid)
     */
    public class CJItem : NotifiableItem
    {
        public string Guid { get; set; }
        public string Name { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string CarrierId { get; set; }
        public string TotalWafer { get; set; }
        public string PortIn { get; set; }
        public string PortOut { get; set; }
        public string AbortedWafer { get; set; }
        public string UnprocessedWafer { get; set; }


    }

    public class LotHistoryViewModel : UiViewModelBase
    {
        public bool IsPermission { get => this.Permission == 3; }

        private DateTime _historyDate;

        public DateTime HistoryJobDate
        {
            get { return _historyDate; }
            set
            {
                _historyDate = value;
                UpdateHistoryList(value);
            }
        }

        private CJItem _selectedHistoryLot;

        public CJItem SelectedHistoryLot
        {
            get { return _selectedHistoryLot; }
            set
            {
                if (_selectedHistoryLot != value)
                {
                    _selectedHistoryLot = value;
                    HistoryLotChanged(value);
                }

            }
        }


        public CJItem Current { get; set; }

        private WaferHistoryDetail _currentWafer;
        public WaferHistoryDetail CurrentWafer
        {
            get { return _currentWafer; }
            set
            {
                if (_currentWafer != value)
                {
                    _currentWafer = value;
                    //WaferHistoryChanged(value);
                }

            }
        }
        private CJItem _selectedRunningLot;

        public CJItem SelectedRunLot
        {
            get { return _selectedRunningLot; }
            set
            {
                if (_selectedRunningLot != value)
                {
                    _selectedRunningLot = value;
                    RunLotChanged(value);
                }

            }
        }
        public CJItem RunningJob { get; set; }


        public ObservableCollection<WaferHistoryDetail> Wafers { get; set; }
        public ObservableCollection<WaferMovement> Movements { get; set; }

        public ObservableCollection<CJItem> RunningJobs { get; set; }

        public ObservableCollection<CJItem> HistoryJobs { get; set; }

        public ICommand FilterHistoryCommand { get; set; }

        [Subscription("Scheduler.CjIdList")]
        public List<string> RunningCjIdList { get; set; }

        public LotHistoryViewModel()
        {
            this.DisplayName = "Lot History";
            var now = DateTime.Now;

            this.HistoryJobDate = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, 0);

            this.Wafers = new ObservableCollection<WaferHistoryDetail>();
            this.Movements = new ObservableCollection<WaferMovement>();

            RunningJobs = new ObservableCollection<CJItem>();
            HistoryJobs = new ObservableCollection<CJItem>();

            FilterHistoryCommand = new DelegateCommand<object>(PerformFilter);
        }

        private void PerformFilter(object obj)
        {
            UpdateHistoryList(HistoryJobDate);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

        }

        protected override void InvokeBeforeUpdateProperty(Dictionary<string, object> data)
        {
            base.InvokeBeforeUpdateProperty(data);

            //foreach (var pj in RunningPjIdList)
            {

            }
        }

        protected override void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {
            base.InvokeAfterUpdateProperty(data);

            try
            {
                List<string> alreadyExistList = Array.ConvertAll(RunningJobs.ToArray(), x => x.Guid).ToList();
                foreach (var o in RunningCjIdList)
                {
                    if (!alreadyExistList.Contains(o))
                    {
                        RunningJobs.Add(GetCJ(o));
                    }
                    else
                    {
                        alreadyExistList.Remove(o);
                    }
                }

                foreach (var id in alreadyExistList)
                {
                    RunningJobs.RemoveWhere(x => x.Guid == id);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        public void WaferRunningChanged(CJItem date)
        {
            Current = date;
            NotifyOfPropertyChange(nameof(Current));
            if (Current != null)
            {
                Current.InvokePropertyChanged();

                Query(Current.Guid);
            }
            else
            {
                Wafers.Clear();
                Movements.Clear();
            }
        }

        public void HistoryLotChanged(CJItem date)
        {
            Current = date;
            NotifyOfPropertyChange(nameof(Current));
            if (Current != null)
            {
                Current.InvokePropertyChanged();

                Query(Current.Guid);
            }
            else
            {
                Wafers.Clear();
                Movements.Clear();
            }
        }

        public void RunLotChanged(CJItem date)
        {
            HistoryLotChanged(date);

        }


        public void UpdateHistoryList(DateTime date)
        {
            try
            {
                var from = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
                var to = new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);

                string sql = string.Format(
                    "SELECT * FROM \"cj_data\" where \"start_time\" >= '{0}' and \"start_time\" <= '{1}'  order by \"start_time\" ASC;",
                    from.ToString("yyyy/MM/dd HH:mm:ss.fff"), to.ToString("yyyy/MM/dd HH:mm:ss.fff"));


                DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    HistoryJobs.Clear();

                    if (dbData == null || dbData.Rows.Count == 0)
                        return;

                    for (int i = 0; i < dbData.Rows.Count; i++)
                    {
                        var cjItem = new CJItem();

                        cjItem.Guid = dbData.Rows[i]["guid"].ToString();

                        cjItem.Name = dbData.Rows[i]["name"].ToString();
                        cjItem.PortIn = dbData.Rows[i]["input_port"].ToString();
                        cjItem.PortOut = dbData.Rows[i]["output_port"].ToString();

                        cjItem.TotalWafer = dbData.Rows[i]["total_wafer_count"].ToString();
                        cjItem.AbortedWafer = dbData.Rows[i]["abort_wafer_count"].ToString();
                        cjItem.UnprocessedWafer = dbData.Rows[i]["unprocessed_wafer_count"].ToString();

                        if (!dbData.Rows[i]["start_time"].Equals(DBNull.Value))
                            cjItem.StartTime = ((DateTime)dbData.Rows[i]["start_time"]).ToString("HH:mm:ss");

                        if (!dbData.Rows[i]["end_time"].Equals(DBNull.Value))
                            cjItem.EndTime = ((DateTime)dbData.Rows[i]["end_time"]).ToString("HH:mm:ss");

                        HistoryJobs.Add(cjItem);
                    }


                }));
            }
            catch (Exception e)
            {
                LOG.Write(e);
            }

        }

        public CJItem GetCJ(string guid)
        {
            CJItem cjItem = new CJItem();

            try
            {
                string sql = string.Format(
                    "SELECT * FROM \"cj_data\" where \"guid\" = '{0}'  order by \"start_time\" ASC;",
                    guid);

                DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (dbData == null || dbData.Rows.Count == 0)
                        return;

                    for (int i = 0; i < dbData.Rows.Count; i++)
                    {
                        //pjItem = new PJItem();
                        cjItem.Guid = dbData.Rows[i]["guid"].ToString();
                        cjItem.Name = dbData.Rows[i]["name"].ToString();
                        cjItem.PortIn = dbData.Rows[i]["input_port"].ToString();
                        cjItem.PortOut = dbData.Rows[i]["output_port"].ToString();

                        cjItem.TotalWafer = dbData.Rows[i]["total_wafer_count"].ToString();
                        cjItem.AbortedWafer = dbData.Rows[i]["abort_wafer_count"].ToString();
                        cjItem.UnprocessedWafer = dbData.Rows[i]["unprocessed_wafer_count"].ToString();


                        if (!dbData.Rows[i]["start_time"].Equals(DBNull.Value))
                            cjItem.StartTime = ((DateTime)dbData.Rows[i]["start_time"]).ToString("HH:mm:ss");

                        if (!dbData.Rows[i]["end_time"].Equals(DBNull.Value))
                            cjItem.EndTime = ((DateTime)dbData.Rows[i]["end_time"]).ToString("HH:mm:ss");

                    }


                }));
            }
            catch (Exception e)
            {
                LOG.Write(e);
            }

            return cjItem;
        }

        public void Query(string cjID)
        {
            try
            {
                string sql = string.Format(
                    "SELECT * FROM \"wafer_data\",\"carrier_data\" where \"lot_data_guid\" = '{0}' and \"wafer_data\".\"carrier_data_guid\"=\"carrier_data\".\"guid\" order by \"create_time\" ASC, \"create_station\" ASC,\"create_slot\" ASC;",
                    cjID);

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

                            item.CarrierID = dbData.Rows[i]["carrier_data_guid"].ToString();

                            item.LotID = dbData.Rows[i]["lot_id"].ToString();
                            item.WaferID = dbData.Rows[i]["wafer_id"].ToString();

                            item.Status = dbData.Rows[i]["process_status"].ToString();

                            item.NotchAngle = dbData.Rows[i]["notch_angle"].ToString();
                            item.SequenceName = dbData.Rows[i]["sequence_name"].ToString();
                            item.LotId = dbData.Rows[i]["lot_id"].ToString();

                            if (!dbData.Rows[i]["create_time"].Equals(DBNull.Value))
                                item.CreateTime = ((DateTime)dbData.Rows[i]["create_time"]).ToString("HH:mm:ss.fff");

                            if (!dbData.Rows[i]["delete_time"].Equals(DBNull.Value))
                                item.DeleteTime = ((DateTime)dbData.Rows[i]["delete_time"]).ToString("HH:mm:ss.fff");

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


                        if (!dbData.Rows[i]["arrive_time"].Equals(DBNull.Value))
                            item.Time = ((DateTime)dbData.Rows[i]["arrive_time"]).ToString("HH:mm:ss.fff");

                        Movements.Add(item);

                    }


                }));
            }
            catch (Exception e)
            {
                LOG.Write(e);
            }
        }


    }
}
