using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Utilities;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase.Command;
using SciChart.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Cali = Caliburn.Micro;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.WaferHistory
{
    public class WaferHistoryDBViewModel : BaseModel
    {
        public WaferHistoryDBViewModel()
        {
            SelectionChangedCommand = new BaseCommand<WaferHistoryItem>(SelectionChanged);
            QueryCommand = new BaseCommand<object>(QueryLots);
            ToChartCommand = new BaseCommand<object>(GoToChart);
            StepChartCommand = new BaseCommand<object>(GoToStepChart);
            HistoryData = new ObservableCollection<LazyTreeItem<WaferHistoryItem>>();

            InitTime();
        }

        private ObservableCollection<LazyTreeItem<WaferHistoryItem>> _historyData;
        public ObservableCollection<LazyTreeItem<WaferHistoryItem>> HistoryData
        {
            get { return _historyData; }
            set
            {
                _historyData = value;
                NotifyOfPropertyChange("HistoryData");
            }
        }

        private WaferHistoryItem _selectedItem;
        public WaferHistoryItem SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                NotifyOfPropertyChange("SelectedItem");
            }
        }

        private ObservableCollection<WaferHistoryLot> _lots = new ObservableCollection<WaferHistoryLot>();
        public ObservableCollection<WaferHistoryLot> Lots
        {
            get { return _lots; }
            set
            {
                _lots = value;
                NotifyOfPropertyChange("Lots");
            }
        }


        private ObservableCollection<WaferHistoryWafer> _wafers = new ObservableCollection<WaferHistoryWafer>();
        public ObservableCollection<WaferHistoryWafer> Wafers
        {
            get { return _wafers; }
            set
            {
                _wafers = value;
                NotifyOfPropertyChange("Wafers");
            }
        }

        private ObservableCollection<WaferHistoryMovement> _movements = new ObservableCollection<WaferHistoryMovement>();
        public ObservableCollection<WaferHistoryMovement> Movements
        {
            get { return _movements; }
            set
            {
                _movements = value;
                NotifyOfPropertyChange("Movements");
            }
        }

        private WaferHistoryRecipe _recipe;
        public WaferHistoryRecipe Recipe
        {
            get { return _recipe; }
            set
            {
                _recipe = value;
                NotifyOfPropertyChange("Recipe");
            }
        }


        public ObservableCollection<WaferHistoryRecipe> _recipes = new ObservableCollection<WaferHistoryRecipe>();
        public ObservableCollection<WaferHistoryRecipe> Recipes
        {
            get { return _recipes; }
            set
            {
                _recipes = value;
                NotifyOfPropertyChange("Recipes");
            }
        }
        private DateTime _searchBeginTime;
        public DateTime SearchBeginTime
        {
            get { return _searchBeginTime; }
            set
            {
                _searchBeginTime = value;
                NotifyOfPropertyChange("SearchBeginTime");
            }
        }

        private DateTime _searchEndTime;
        public DateTime SearchEndTime
        {
            get { return _searchEndTime; }
            set
            {
                _searchEndTime = value;
                NotifyOfPropertyChange("SearchEndTime");
            }
        }

        private string keyWord;
        private WaferHistoryDBView view;

        public string KeyWord
        {
            get { return keyWord; }
            set
            {
                keyWord = value;
                NotifyOfPropertyChange("KeyWord");
            }
        }
        public ICommand SelectionChangedCommand { get; set; }

        public ICommand QueryCommand { get; set; }

        public ICommand ToChartCommand { get; set; }
        public ICommand StepChartCommand { get; set; }
        void InitTime()
        {
            SearchBeginTime = DateTime.Now.Date;
            SearchEndTime = DateTime.Now.Date.AddDays(1).Date;
        }


        void QueryLots(object e)
        {
            this.SearchBeginTime = this.view.wfTimeFrom.Value;
            this.SearchEndTime = this.view.wfTimeTo.Value;

            if (SearchBeginTime > SearchEndTime)
            {
                System.Windows.MessageBox.Show("Time range invalid, start time should be early than end time");
                return;
            }

            Lots = new ObservableCollection<WaferHistoryLot>(QueryLot(SearchBeginTime, SearchEndTime, KeyWord));//.OrderByDescending(lot => lot.StartTime).ToArray();

            HistoryData.Clear();
            var lotsItem = new WaferHistoryItem() { Name = "Lots", };
            var root = new LazyTreeItem<WaferHistoryItem>(lotsItem, x => LoadSubItem(x));
            root.SubItems = new ObservableCollection<LazyTreeItem<WaferHistoryItem>>(Lots.Select(x => new LazyTreeItem<WaferHistoryItem>(new WaferHistoryItem() { ID = x.ID, Name = x.Name, StartTime = x.StartTime, EndTime = x.EndTime, Type = WaferHistoryItemType.Lot }, LoadSubItem)));
            root.IsExpanded = true;
            HistoryData.Add(root);

            SelectedItem = lotsItem;
        }

        Cali.WindowManager wm = new Cali.WindowManager();
        ToChartHistoryViewModel toChartHistoryView;

        void GoToStepChart(object o)
        {
            WaferHistoryRecipe chartQuery = null;
            //RecipeStepFdcData chartQuery = null;
           
            if (o is RecipeStepFdcData wafer)
            {
               // string[] charmber = wafer.Name.Split('.');
                chartQuery = new WaferHistoryRecipe() { StartTime = wafer.StartTime, EndTime = wafer.EndTime,Name=wafer.Name,Recipe=wafer.recipe_name };
            }
            //导航切换到chart页面
           // BaseApp.Instance.SwitchPage("DataLog", "DataHistory", chartQuery);
            BaseApp.Instance.SwitchPage("DataLog", "WaferHistoryToChart", chartQuery);
        }

        /// <summary>
        /// Notify chart page to prepare datas
        /// </summary>
        /// <param name="o"></param>
        /// 
        void GoToChart(object o)
        {
            WaferHistoryRecipe chartQuery = null;

            if (o is string name)
            {
                var query = _recipes.FirstOrDefault(t => t.Recipe == name);
                if (query is null) return;
                chartQuery = query;
            }
            if (o is WaferHistoryLot waferLot)
            {
                DateTime start = waferLot.StartTime;
                double.TryParse(waferLot.Duration, out double duration);
                DateTime end = start.AddSeconds(duration);
                chartQuery = new WaferHistoryRecipe() { StartTime = start, EndTime = end, /*Chamber = waferLot.CarrierID */};

            }
            else if (o is WaferHistoryWafer wafer)
            {
                chartQuery = new WaferHistoryRecipe() { StartTime = wafer.StartTime, EndTime = wafer.EndTime/*,Chamber = waferLot.CarrierID*/ };
            }


            //导航切换到chart页面
            BaseApp.Instance.SwitchPage("DataLog", "WaferHistoryToChart", chartQuery);
        }

        public List<WaferHistoryLot> QueryLot(DateTime from, DateTime to, string key)
        {
            List<WaferHistoryLot> result = new List<WaferHistoryLot>();

            string sql = $"SELECT * FROM \"lot_data\" where \"start_time\" >= '{from:yyyy/MM/dd HH:mm:ss.fff}' and \"start_time\" <= '{to:yyyy/MM/dd HH:mm:ss.fff}'";

            if (!string.IsNullOrWhiteSpace(key))
                sql += $" and lower(\"name\") like '%{key.ToLower()}%'";

            sql += "order by \"start_time\" ASC;";

            //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //{
            DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count; i++)
                {
                    WaferHistoryLot item = new WaferHistoryLot();

                    string name = dbData.Rows[i]["name"].ToString();
                    string time = "";
                    item.WaferCount = (int)dbData.Rows[i]["total_wafer_count"];
                    item.ID = dbData.Rows[i]["guid"].ToString();

                    if (!dbData.Rows[i]["start_time"].Equals(DBNull.Value))
                    {
                        item.StartTime = ((DateTime)dbData.Rows[i]["start_time"]);
                        time = item.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    if (!dbData.Rows[i]["end_time"].Equals(DBNull.Value))
                    {
                        item.EndTime = ((DateTime)dbData.Rows[i]["end_time"]);
                    }

                    item.Name = $"{name} - {time}";

                    result.Add(item);
                }
            }


            //}));

            return result;
        }

        public List<WaferHistoryWafer> QueryLotWafer(string lotGuid)
        {
            List<WaferHistoryWafer> result = new List<WaferHistoryWafer>();

            string sql = $"SELECT * FROM \"wafer_data\",\"lot_wafer_data\" where \"lot_wafer_data\".\"lot_data_guid\" = '{lotGuid}' and \"lot_wafer_data\".\"wafer_data_guid\" = \"wafer_data\".\"guid\" order by \"wafer_data\".\"create_time\" ASC;;";

            Wafers.Clear();
            //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //{
            DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count; i++)
                {
                    WaferHistoryWafer item = new WaferHistoryWafer();

                    item.ID = dbData.Rows[i]["guid"].ToString();
                    var itemLoadPort = dbData.Rows[i]["create_station"].ToString();
                    var itemSlot = dbData.Rows[i]["create_slot"].ToString();

                    //item.CarrierID = dbData.Rows[i]["rfid"].ToString();

                    //item.LotID = dbData.Rows[i]["lot_id"].ToString();
                    var itemWaferID = dbData.Rows[i]["wafer_id"].ToString();

                    item.Sequence = dbData.Rows[i]["sequence_name"].ToString();

                    item.Status = dbData.Rows[i]["process_status"].ToString();

                    if (!dbData.Rows[i]["create_time"].Equals(DBNull.Value))
                    {
                        item.StartTime = ((DateTime)dbData.Rows[i]["create_time"]);
                    }

                    if (!dbData.Rows[i]["delete_time"].Equals(DBNull.Value))
                        item.EndTime = ((DateTime)dbData.Rows[i]["delete_time"]);

                    item.Name = $"{itemLoadPort} - {itemSlot} - {itemWaferID}";

                    item.Type = WaferHistoryItemType.Wafer;

                    Wafers.Add(item);

                    result.Add(item);
                }
            }


            //}));

            return result;
        }


        public List<WaferHistoryRecipe> QueryWaferRecipe(string waferGuid)
        {
            List<WaferHistoryRecipe> result = new List<WaferHistoryRecipe>();

            string sql = $"SELECT * FROM \"process_data\" where \"wafer_data_guid\" = '{waferGuid}';";

            Recipes.Clear();
            //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //{
            DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count; i++)
                {
                    WaferHistoryRecipe item = new WaferHistoryRecipe();

                    item.ID = dbData.Rows[i]["guid"].ToString();
                    var itemName = dbData.Rows[i]["recipe_name"].ToString();

                    if (!dbData.Rows[i]["process_begin_time"].Equals(DBNull.Value))
                        item.StartTime = (DateTime)dbData.Rows[i]["process_begin_time"];

                    if (dbData.Rows[i].Table.Columns.Contains("process_end_time") && !dbData.Rows[i]["process_end_time"].Equals(DBNull.Value))
                        item.EndTime = (DateTime)dbData.Rows[i]["process_end_time"];

                    if (dbData.Rows[i].Table.Columns.Contains("recipe_setting_time") && !dbData.Rows[i]["recipe_setting_time"].Equals(DBNull.Value))
                        item.SettingTime = dbData.Rows[i]["recipe_setting_time"].ToString();

                    item.ActualTime = item.Duration;

                    item.Recipe = itemName;

                    item.Chamber = dbData.Rows[i]["process_in"].ToString();

                    item.Name = itemName;

                    item.Type = WaferHistoryItemType.Recipe;

                    Recipes.Add(item);

                    result.Add(item);
                }
            }


            //}));

            return result;
        }

        public List<WaferHistoryMovement> QueryWaferMovement(string waferGuid)
        {
            List<WaferHistoryMovement> result = new List<WaferHistoryMovement>();

            string sql = $"SELECT * FROM \"wafer_move_history\" where \"wafer_data_guid\" = '{waferGuid}' order by \"arrive_time\" ASC limit 1000;";

            Movements.Clear();
            //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //{
            DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count - 1; i++)
                {
                    WaferHistoryMovement item = new WaferHistoryMovement();

                    item.Source = $"station : {dbData.Rows[i]["station"]} slot : {dbData.Rows[i]["slot"]}";
                    item.Destination = $"station : {dbData.Rows[i + 1]["station"]} slot : {dbData.Rows[i + 1]["slot"]}";
                    item.InTime = dbData.Rows[i]["arrive_time"].ToString();

                    result.Add(item);

                    Movements.Add(item);

                }
            }


            //}));

            return result;
        }

        public WaferHistoryRecipe QueryRecipe(string recipeGuid)
        {
            WaferHistoryRecipe result = new WaferHistoryRecipe();

            string sql = $"SELECT * FROM \"process_data\" where \"guid\" = '{recipeGuid}';";
            string recipe_name = "";

            //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            //{
            DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count; i++)
                {
                    WaferHistoryRecipe item = new WaferHistoryRecipe();

                    item.ID = dbData.Rows[i]["guid"].ToString();
                    var itemName = dbData.Rows[i]["recipe_name"].ToString();
                    recipe_name = itemName;
                    if (!dbData.Rows[i]["process_begin_time"].Equals(DBNull.Value))
                        item.StartTime = (DateTime)dbData.Rows[i]["process_begin_time"];

                    if (!dbData.Rows[i]["process_end_time"].Equals(DBNull.Value))
                        item.EndTime = (DateTime)dbData.Rows[i]["process_end_time"];

                    if (dbData.Rows[i].Table.Columns.Contains("recipe_setting_time") && !dbData.Rows[i]["recipe_setting_time"].Equals(DBNull.Value))
                        item.SettingTime = dbData.Rows[i]["recipe_setting_time"].ToString();

                    item.ActualTime = item.Duration;

                    item.Recipe = itemName;

                    item.Chamber = dbData.Rows[i]["process_in"].ToString();

                    item.Name = itemName;

                    item.Type = WaferHistoryItemType.Recipe;

                    result = item;


                }
            }


            sql = $"SELECT * FROM \"recipe_step_data\" where \"process_data_guid\" = '{recipeGuid}' order by step_number ASC;";
            dbData = QueryDataClient.Instance.Service.QueryData(sql);
            string ActualTime = "";
            string SettingTime = "";
            DateTime StartTime=DateTime.Now.AddDays(-1);
            DateTime EndTime = DateTime.Now;
            List<RecipeStep> steps = new List<RecipeStep>();
            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count; i++)
                {
                    RecipeStep item = new RecipeStep();

                    item.No = int.Parse(dbData.Rows[i]["step_number"].ToString());
                    item.Name = dbData.Rows[i]["step_name"].ToString();

                    if (!dbData.Rows[i]["step_begin_time"].Equals(DBNull.Value))
                        item.StartTime = (DateTime)dbData.Rows[i]["step_begin_time"];
                    StartTime = item.StartTime;
                    if (!dbData.Rows[i]["step_end_time"].Equals(DBNull.Value))
                        item.EndTime = (DateTime)dbData.Rows[i]["step_end_time"];
                    EndTime = item.EndTime;
                    item.ActualTime = item.EndTime.CompareTo(item.StartTime) <= 0 ? "" : item.EndTime.Subtract(item.StartTime).TotalSeconds.ToString();
                    ActualTime = item.ActualTime;
                    item.SettingTime = dbData.Rows[i]["step_time"].ToString();
                    SettingTime = item.SettingTime;
                    steps.Add(item);
                }
            }


            sql = $"SELECT * FROM \"step_fdc_data\" where \"process_data_guid\" = '{recipeGuid}' order by step_number ASC;";
            dbData = QueryDataClient.Instance.Service.QueryData(sql);

            List<RecipeStepFdcData> fdcs = new List<RecipeStepFdcData>();
            if (dbData != null && dbData.Rows.Count > 0)
            {
                for (int i = 0; i < dbData.Rows.Count; i++)
                {
                    RecipeStepFdcData item = new RecipeStepFdcData();

                    item.StepNumber = int.Parse(dbData.Rows[i]["step_number"].ToString());
                    item.Name = dbData.Rows[i]["parameter_name"].ToString();

                    if (!dbData.Rows[i]["sample_count"].Equals(DBNull.Value))
                        item.SampleCount = (int)dbData.Rows[i]["sample_count"];

                    if (!dbData.Rows[i]["setpoint"].Equals(DBNull.Value))
                        item.SetPoint = (float)dbData.Rows[i]["setpoint"];

                    if (!dbData.Rows[i]["min_value"].Equals(DBNull.Value))
                        item.MinValue = (float)dbData.Rows[i]["min_value"];

                    if (!dbData.Rows[i]["max_value"].Equals(DBNull.Value))
                        item.MaxValue = (float)dbData.Rows[i]["max_value"];

                    if (!dbData.Rows[i]["std_value"].Equals(DBNull.Value))
                        item.StdValue = (float)dbData.Rows[i]["std_value"];

                    if (!dbData.Rows[i]["mean_value"].Equals(DBNull.Value))
                        item.MeanValue = (float)dbData.Rows[i]["mean_value"];
                    item.ActualTime = ActualTime;
                    item.SettingTime = SettingTime;
                    item.StartTime = StartTime;
                    item.EndTime = EndTime;
                    item.recipe_name = recipe_name;
                    fdcs.Add(item);
                }
            }

            result.Steps = steps;
            result.Fdcs = fdcs;

            Recipe = result;

            return result;
        }

        private void SelectionChanged(WaferHistoryItem item)
        {
            switch (item.Type)
            {
                case WaferHistoryItemType.Lot:

                    QueryLotWafer(item.ID);

                    break;
                case WaferHistoryItemType.Wafer:
                    QueryWaferRecipe(item.ID);
                    QueryWaferMovement(item.ID);

                    break;
                case WaferHistoryItemType.Recipe:

                    QueryRecipe(item.ID);

                    break;
                default:
                    break;
            }
            SelectedItem = item;
        }

        protected List<LazyTreeItem<WaferHistoryItem>> LoadSubItem(WaferHistoryItem item)
        {
            switch (item.Type)
            {
                case WaferHistoryItemType.Lot:
                    var wafers = QueryLotWafer(item.ID);
                    return wafers.Select(x => new LazyTreeItem<WaferHistoryItem>(x, y => LoadSubItem(y))).OrderBy(s => s.Data.StartTime).ToList();
                case WaferHistoryItemType.Wafer:
                    var recipes = QueryWaferRecipe(item.ID);
                    return recipes.Select(x => new LazyTreeItem<WaferHistoryItem>(x, y => LoadSubItem(y))).OrderBy(s => s.Data.StartTime).ToList();
                default:
                    break;
            }
            return new List<LazyTreeItem<WaferHistoryItem>> { };
        }


        protected override void OnViewLoaded(object _view)
        {
            base.OnViewLoaded(_view);
            this.view = (WaferHistoryDBView)_view;
            this.view.wfTimeFrom.Value = this.SearchBeginTime;
            this.view.wfTimeTo.Value = this.SearchEndTime;

            QueryLots(new object());
            SelectionChanged(SelectedItem);
        }
        public async void Export()
        {

            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            string ret = await Task.Run(() => SelectDataExport(dialog.SelectedPath));
            if (ret != null)
            {
                System.Windows.MessageBox.Show(ret, "Export", MessageBoxButton.OK,
                      MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("Export succeed", "Export", MessageBoxButton.OK,
                      MessageBoxImage.Information);
            }
        }
        public string SelectDataExport(string path)
        {
            try
            {
                string Result = "";
                ObservableCollection<WaferHistoryRecipe> ExportWaferRecipe = new ObservableCollection<WaferHistoryRecipe>();
                ObservableCollection<WaferHistoryMovement> ExportMovement = new ObservableCollection<WaferHistoryMovement>();
                ObservableCollection<RecipeStep> ExportRecipeStep = new ObservableCollection<RecipeStep>();
                ObservableCollection<RecipeStepFdcData> ExportRecipeStepFdc = new ObservableCollection<RecipeStepFdcData>();
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.DefaultExt = ".xlsx"; // Default file extension 
                dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 

                System.Data.DataSet ds = new System.Data.DataSet();
                ds.Tables.Add(new System.Data.DataTable($"WaferLot_Export_{DateTime.Now:yyyyMMdd_HHmmss}"));
                ds.Tables[0].Columns.Add("Wafer Name");
                ds.Tables[0].Columns.Add("Arrive Time");
                ds.Tables[0].Columns.Add("Remove Time");
                ds.Tables[0].Columns.Add("Duration");
                ds.Tables[0].Columns.Add("Sequence");
                ds.Tables[0].Columns.Add("Status");
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (Wafers.Count > 0)
                    {
                        foreach (var item in Wafers)
                        {
                            var row = ds.Tables[0].NewRow();
                            row[0] = item.Name;
                            row[1] = item.StartTime == DateTime.MinValue ? "" : ((DateTime)item.StartTime).ToString("yyyy-MM-dd HH:mm:ss");
                            row[2] = item.EndTime == DateTime.MinValue ? "" : ((DateTime)item.EndTime).ToString("yyyy-MM-dd HH:mm:ss");
                            row[3] = item.Duration;
                            row[4] = item.Sequence;
                            row[5] = item.Status;
                            ds.Tables[0].Rows.Add(row);


                            List<WaferHistoryRecipe> lstRecipe = QueryWaferRecipe(item.ID);
                            if (lstRecipe.Count > 0)
                            {
                                foreach (var WaferRecipeitem in lstRecipe)
                                {
                                    ExportWaferRecipe.Add(WaferRecipeitem);
                                    List<RecipeStep> lstRecipeStep = QueryRecipe(WaferRecipeitem.ID).Steps;
                                    foreach (var Recipeitem in lstRecipeStep)
                                    {
                                        ExportRecipeStep.Add(Recipeitem);
                                    }
                                    foreach (var Recipefdcitem in QueryRecipe(WaferRecipeitem.ID).Fdcs)
                                    {
                                        ExportRecipeStepFdc.Add(Recipefdcitem);
                                    }
                                }
                            }

                            List<WaferHistoryMovement> lstMovement = QueryWaferMovement(item.ID);
                            if (lstMovement.Count > 0)
                            {
                                foreach (var MovementRecipeitem in lstMovement)
                                {
                                    ExportMovement.Add(MovementRecipeitem);
                                }
                            }

                        }
                        dlg.FileName = $"{path}\\WaferLot_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                        if (!ExcelHelper.ExportToExcel(dlg.FileName, ds, out string reason))
                        {
                            Result = $"{dlg.FileName} export failed, {reason}";
                        }
                        else
                        {
                            ds = new System.Data.DataSet();
                            ds.Tables.Add(new System.Data.DataTable($"Recipes_Export_{DateTime.Now:yyyyMMdd_HHmmss}"));
                            ds.Tables[0].Columns.Add("Name");
                            ds.Tables[0].Columns.Add("StartTime");
                            ds.Tables[0].Columns.Add("EndTime");
                            ds.Tables[0].Columns.Add("Chamber");
                            //ds.Tables[0].Columns.Add("SettingTime");
                            ds.Tables[0].Columns.Add("ActualTime");

                            if (ExportWaferRecipe.Count > 0)
                            {
                                foreach (var item in ExportWaferRecipe)
                                {
                                    var row = ds.Tables[0].NewRow();
                                    row[0] = item.Name;
                                    row[1] = item.StartTime == DateTime.MinValue ? "" : ((DateTime)item.StartTime).ToString("yyyy-MM-dd HH:mm:ss");
                                    row[2] = item.EndTime == DateTime.MinValue ? "" : ((DateTime)item.EndTime).ToString("yyyy-MM-dd HH:mm:ss");
                                    row[3] = item.Chamber;
                                    row[4] = item.ActualTime;
                                    ds.Tables[0].Rows.Add(row);
                                }

                            }
                            dlg.FileName = $"{path}\\Recipes_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                            if (!ExcelHelper.ExportToExcel(dlg.FileName, ds, out string reason1))
                            {
                                Result = $"{dlg.FileName} export failed, {reason1}";
                            }
                            else
                            {
                                ds = new System.Data.DataSet();
                                ds.Tables.Add(new System.Data.DataTable($"Movements_Export_{DateTime.Now:yyyyMMdd_HHmmss}"));
                                ds.Tables[0].Columns.Add("Source");
                                ds.Tables[0].Columns.Add("Destination");
                                ds.Tables[0].Columns.Add("InTime");
                                //ds.Tables[0].Columns.Add("Time");
                                if (ExportMovement.Count > 0)
                                {
                                    foreach (var item in ExportMovement)
                                    {
                                        var row = ds.Tables[0].NewRow();
                                        row[0] = item.Source;
                                        row[1] = item.Destination;
                                        row[2] = item.InTime;
                                        //row[3] = item.Time;
                                        ds.Tables[0].Rows.Add(row);
                                    }

                                }
                                dlg.FileName = $"{path}\\Movements_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                                if (!ExcelHelper.ExportToExcel(dlg.FileName, ds, out string reason2))
                                {
                                    Result = $"{dlg.FileName} export failed, {reason2}";
                                }
                                else
                                {
                                    ds = new System.Data.DataSet();
                                    ds.Tables.Add(new System.Data.DataTable($"Stepfdcs_Export_{DateTime.Now:yyyyMMdd_HHmmss}"));
                                    ds.Tables[0].Columns.Add("StepNumber");
                                    ds.Tables[0].Columns.Add("Name");
                                    ds.Tables[0].Columns.Add("SetPoint");
                                    ds.Tables[0].Columns.Add("SampleCount");
                                    //ds.Tables[0].Columns.Add("StartValue");
                                    //ds.Tables[0].Columns.Add("EndValue");
                                    ds.Tables[0].Columns.Add("MinValue");
                                    ds.Tables[0].Columns.Add("MaxValue");
                                    ds.Tables[0].Columns.Add("MeanValue");
                                    ds.Tables[0].Columns.Add("StdValue");
                                    if (ExportRecipeStepFdc.Count > 0)
                                    {
                                        foreach (var item in ExportRecipeStepFdc)
                                        {
                                            var row = ds.Tables[0].NewRow();
                                            row[0] = item.StepNumber;
                                            row[1] = item.Name;
                                            row[2] = item.SetPoint;
                                            row[3] = item.SampleCount;
                                            //row[4] = item.StartValue;
                                            //row[5] = item.EndValue;
                                            row[4] = item.MinValue;
                                            row[5] = item.MaxValue;
                                            row[6] = item.MeanValue;
                                            row[7] = item.StdValue;
                                            ds.Tables[0].Rows.Add(row);
                                        }

                                    }
                                    dlg.FileName = $"{path}\\Stepfdcs_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                                    if (!ExcelHelper.ExportToExcel(dlg.FileName, ds, out string reason3))
                                    {
                                        Result = $"{dlg.FileName} export failed, {reason3}";
                                    }
                                    else
                                    {
                                        ds = new System.Data.DataSet();
                                        ds.Tables.Add(new System.Data.DataTable($"Steps_Export_{DateTime.Now:yyyyMMdd_HHmmss}"));
                                        ds.Tables[0].Columns.Add("No");
                                        ds.Tables[0].Columns.Add("Name");
                                        ds.Tables[0].Columns.Add("StartTime");
                                        ds.Tables[0].Columns.Add("EndTime");
                                        ds.Tables[0].Columns.Add("ActualTime");
                                        ds.Tables[0].Columns.Add("SettingTime");
                                        //ds.Tables[0].Columns.Add("ErrorCount");
                                        if (ExportRecipeStep.Count > 0)
                                        {
                                            foreach (var item in ExportRecipeStep)
                                            {
                                                var row = ds.Tables[0].NewRow();
                                                row[0] = item.No;
                                                row[1] = item.Name;
                                                row[2] = item.StartTime == DateTime.MinValue ? "" : ((DateTime)item.StartTime).ToString("yyyy-MM-dd HH:mm:ss");
                                                row[3] = item.EndTime == DateTime.MinValue ? "" : ((DateTime)item.EndTime).ToString("yyyy-MM-dd HH:mm:ss");
                                                row[4] = item.ActualTime;
                                                row[5] = item.SettingTime;
                                                //row[6] = item.ErrorCount;
                                                ds.Tables[0].Rows.Add(row);
                                            }

                                        }
                                        //dlg.FileName = $"{path}\\{recipe.Chamber}_{fileName}.xlsx";  //导出的名称
                                        dlg.FileName = $"{path}\\Steps_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                                        if (!ExcelHelper.ExportToExcel(dlg.FileName, ds, out string reason4))
                                        {
                                            Result = $"{dlg.FileName} export failed, {reason4}";
                                        }
                                    }
                                }
                            }
                        }
                    }
                }));





                System.Threading.Thread.Sleep(10);
                if (Result != "")
                {
                    return Result;
                }
                return null;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return $"Write failed,{ex.Message}";
            }
        }
    }


    public class LazyTreeItem<T> : INotifyPropertyChanged where T : ITreeItem<T>, new()
    {
        private LazyTreeItem<T> dummyChild;
        private T data;
        private Func<T, List<LazyTreeItem<T>>> loader;


        private LazyTreeItem()
        {
            data = new T();
            data.ID = Guid.NewGuid().ToString();
        }

        public LazyTreeItem(T data, Func<T, List<LazyTreeItem<T>>> loader)
        {
            this.data = data;
            this.loader = loader;

            dummyChild = new LazyTreeItem<T>();

            SubItems = new ObservableCollection<LazyTreeItem<T>>();
            SubItems.Add(dummyChild);
        }

        public T Data
        {
            get
            {
                return data;
            }
        }



        public ObservableCollection<LazyTreeItem<T>> SubItems
        {
            get; set;
        }

        private bool isExpanded;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public bool HasDummyChild
        {
            get { return SubItems.Count == 1 && SubItems.First().data.ID == dummyChild.data.ID; }
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                if (value != isExpanded)
                {
                    isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                }

                if (HasDummyChild)
                {
                    SubItems.Remove(dummyChild);
                    var items = loader(data);
                    items.ForEachDo(x => SubItems.Add(x));
                }
            }
        }
    }


}
