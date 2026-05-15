using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Utilities;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.ProcessHistory
{
    public class SelectDataViewModel : UiViewModelBase
    {
        #region 
        private const int MAX_PARAMETERS = 20;
        public DateTime BeginDate { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }

        public ObservableCollection<RecipeItem> Recipes { get; set; }
        public ObservableCollection<RecipeItem> SelectedRecipes { get; set; }

        private ObservableCollection<ParameterNode> _ParameterNodes;
        public ObservableCollection<ParameterNode> ParameterNodes
        {
            get { return _ParameterNodes; }
            set { _ParameterNodes = value; NotifyOfPropertyChange("ParameterNodes"); }
        }
        public ObservableCollection<string> SelectedParameters { get; set; }

        object _lockSelection = new object();

        public ObservableCollection<string> SourcePM { get; set; }
        public string SelectedValuePM { get; set; }
        public ObservableCollection<string> SourceSLot { get; set; }
        public string SelectedValueSLot { get; set; }
        public string RecipeName { get; set; }

        public string LotID { get; set; }

        public bool CheckAllRecipeFlag = false;

        public ObservableCollection<ParameterNode> ParameterNodes1 = new ObservableCollection<ParameterNode>();

        #endregion

        #region Popup window
        public SelectDataViewModel()
        {
            var now = DateTime.Now;
            this.StartDateTime = now;
            this.BeginDate = now;
            this.StartDateTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, 0);
            this.EndDateTime = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, 999);

            Recipes = new ObservableCollection<RecipeItem>();
            SelectedRecipes = new ObservableCollection<RecipeItem>();

            SelectedParameters = new ObservableCollection<string>();
            ParameterNodes = ProcessHistoryProvider.Instance.GetParameters();

            SourcePM = new ObservableCollection<string>(new[] { "PM1", "PM2", "PM3", "PM4", "PM5", "PM6", "PM7" });

            SourceSLot = new ObservableCollection<string>();
            for (int i = 0; i < 25; i++)
            {
                SourceSLot.Add((i + 1).ToString());
            }

        }

        protected override void OnViewLoaded(object _view)
        {
            base.OnViewLoaded(_view);
            this.view = (SelectDataView)_view;
            this.view.wfTimeFrom.Value = this.StartDateTime;
            this.view.wfTimeTo.Value = this.EndDateTime;
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            ParameterNodes = ProcessHistoryProvider.Instance.GetParameters();
            foreach (var recipeItem in Recipes)
            {
                recipeItem.Selected = false;
            }
        }

        private SelectDataView view;

        public void SearchRecipe()
        {
            Recipes.Clear();

            //ObservableCollection<RecipeItem> items = ProcessHistoryProvider.Instance.SearchRecipe();
            //foreach (RecipeItem item in items)
            //    Recipes.Add(item);
            this.StartDateTime = this.view.wfTimeFrom.Value;
            this.EndDateTime = this.view.wfTimeTo.Value;

            if (StartDateTime > EndDateTime)
            {
                MessageBox.Show("Time range invalid, start time should be early than end time");
                return;
            }

            try
            {
                //string sql = $"SELECT * FROM \"process_data\",\"cj_data\",\"pj_data\",\"wafer_data\" where  \"process_data\".\"wafer_data_guid\"=\"wafer_data\".\"guid\"" +
                //$" and \"wafer_data\".\"pj_data_guid\" =\"pj_data\".\"guid\" and \"cj_data\".\"guid\"=\"pj_data\".\"cj_data_guid\" and \"process_data\".\"process_begin_time\" >='{StartDateTime:yyyyMMdd HHmmss}' and  \"process_data\".\"process_end_time\" <='{EndDateTime:yyyyMMdd HHmmss}'";
                string sql = $"SELECT * FROM \"process_data\",\"wafer_data\"" +
                   $"left join \"pj_data\" on \"wafer_data\".\"pj_data_guid\" =\"pj_data\".\"guid\"" +
                   $"left join \"cj_data\" on \"cj_data\".\"guid\"=\"pj_data\".\"cj_data_guid\"" +
                   $"left join \"carrier_data\" on \"wafer_data\".\"carrier_data_guid\"=\"carrier_data\".\"guid\"" +
                   $"where  \"process_data\".\"wafer_data_guid\"=\"wafer_data\".\"guid\" and \"process_data\".\"process_begin_time\" >='{StartDateTime:yyyyMMdd HHmmss}' and  \"process_data\".\"process_end_time\" <='{EndDateTime:yyyyMMdd HHmmss}'";


                if (!string.IsNullOrEmpty(SelectedValuePM))
                {
                    string[] pms = SelectedValuePM.Split(',');
                    if (pms.Length > 0)
                    {
                        sql += " and (FALSE ";
                        foreach (var pm in pms)
                        {
                            sql += $" OR  \"process_data\".\"process_in\"='{pm}' ";
                        }
                        sql += " ) ";
                    }

                    ParameterNodes = ProcessHistoryProvider.Instance.GetParameters();
                    bool pm1 = ((System.Collections.IList)pms).Contains("PMA") || ((System.Collections.IList)pms).Contains("PM1");//True
                    bool pm2 = ((System.Collections.IList)pms).Contains("PMB") || ((System.Collections.IList)pms).Contains("PM2");//True
                    bool pm3 = ((System.Collections.IList)pms).Contains("PMC") || ((System.Collections.IList)pms).Contains("PM3");//True
                    bool pm4 = ((System.Collections.IList)pms).Contains("PMD") || ((System.Collections.IList)pms).Contains("PM4");//True
                    bool pm5 = ((System.Collections.IList)pms).Contains("PME") || ((System.Collections.IList)pms).Contains("PM5");//True
                    bool pm6 = ((System.Collections.IList)pms).Contains("PMF") || ((System.Collections.IList)pms).Contains("PM6");//True
                    bool pm7 = ((System.Collections.IList)pms).Contains("PMG") || ((System.Collections.IList)pms).Contains("PM7");//True

                    ParameterNodes1.Clear();
                    if (pm1)
                        ParameterNodes1.Add(ParameterNodes[0]);
                    if (pm2)
                        ParameterNodes1.Add(ParameterNodes[1]);
                    if (pm3)
                        ParameterNodes1.Add(ParameterNodes[2]);
                    if (pm4)
                        ParameterNodes1.Add(ParameterNodes[3]);
                    if (pm5)
                        ParameterNodes1.Add(ParameterNodes[4]);
                    if (pm6)
                        ParameterNodes1.Add(ParameterNodes[5]);
                    if (pm7)
                        ParameterNodes1.Add(ParameterNodes[6]);
                    ParameterNodes.Clear();
                    ParameterNodes = ParameterNodes1;
                }
                else
                {
                    ParameterNodes = ProcessHistoryProvider.Instance.GetParameters();
                }

                if (!string.IsNullOrEmpty(RecipeName))
                {
                    sql += string.Format(" and lower( \"process_data\".\"recipe_name\") like '%{0}%'", RecipeName.ToLower());
                }

                if (!string.IsNullOrEmpty(LotID))
                {
                    sql += string.Format(" and lower(\"wafer_data\".\"lot_id\") like '%{0}%'", LotID.ToLower());
                }
                if (!string.IsNullOrEmpty(SelectedValueSLot))
                {
                    string[] pms = SelectedValueSLot.Split(',');
                    if (pms.Length > 0)
                    {
                        sql += " and (FALSE ";
                        foreach (var pm in pms)
                        {
                            sql += $" OR \"wafer_data\".\"create_slot\"='{pm}' ";
                        }
                        sql += " ) ";
                    }
                }
                sql += " order by \"process_data\".\"process_begin_time\" ASC;";

                DataTable dbData = QueryDataClient.Instance.Service.QueryData(sql);

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (dbData == null || dbData.Rows.Count == 0)
                        return;

                    for (int i = 0; i < dbData.Rows.Count; i++)
                    {
                        RecipeItem item = new RecipeItem();

                        item.Recipe = dbData.Rows[i]["recipe_name"].ToString();
                        item.LotID = dbData.Rows[i]["lot_id"].ToString();
                        item.SlotID = dbData.Rows[i]["create_slot"].ToString();
                        item.CjName = dbData.Rows[i]["name"].ToString();
                        item.PjName = dbData.Rows[i]["name1"].ToString();
                        item.Chamber = dbData.Rows[i]["process_in"].ToString();
                        item.Status = dbData.Rows[i]["process_status"].ToString();

                        if (!dbData.Rows[i]["process_begin_time"].Equals(DBNull.Value))
                            item.StartTime = ((DateTime)dbData.Rows[i]["process_begin_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
                        if (!dbData.Rows[i]["process_end_time"].Equals(DBNull.Value))
                            item.EndTime = ((DateTime)dbData.Rows[i]["process_end_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff");
                        Recipes.Add(item);

                    }


                }));
            }
            catch (Exception e)
            {
                LOG.Write(e);
            }
        }
        public void CheckAllRecipe()
        {
            SelectedRecipes.Clear();
            CheckAllRecipeFlag = !CheckAllRecipeFlag;
            if (CheckAllRecipeFlag)
            {
                foreach (var recipe in Recipes)
                {
                    recipe.Selected = true;
                    SelectedRecipes.Add(recipe);
                }
            }
            else
            {
                foreach (var recipe in Recipes)
                {
                    recipe.Selected = false;
                }
            }
        }

        public void CheckRecipe(RecipeItem recipe)
        {
            if (recipe.Selected)
            {
                SelectedRecipes.Add(recipe);
            }
            else
            {
                RecipeItem item = SelectedRecipes.FirstOrDefault(t => t.Recipe == recipe.Recipe);
                if (item != null)
                    SelectedRecipes.Remove(item);
            }
        }

        public void Preset()
        {

        }

        public void UnSelect()
        {

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
                        node.Selected = !node.Selected;
                        return false;
                    }
                }
            }
            else //leaf node
            {
                lock (_lockSelection)
                {
                    bool flag = SelectedParameters.Contains(node.Name);
                    if (node.Selected && !flag)
                    {
                        if (SelectedParameters.Count < MAX_PARAMETERS)
                        {
                            SelectedParameters.Add(node.Name);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (!node.Selected && flag)
                    {
                        SelectedParameters.Remove(node.Name);
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

        public void OnTreeSelectedChanged(object obj)
        {

        }

        public void OK()
        {

            this.TryClose(true);

        }

        public void Cancel()
        {
            this.TryClose(false);
        }

        //public void SelectColor(ChartParameter cp)
        //{
        //    if (cp == null)
        //        return;

        //    var dlg = new System.Windows.Forms.ColorDialog();
        //    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        //    {
        //        var newColor = new System.Windows.Media.Color() { A = dlg.Color.A, B = dlg.Color.B, G = dlg.Color.G, R = dlg.Color.R };
        //        cp.Color = new SolidColorBrush(newColor);
        //    }
        //}


        public bool GetDbData(string chamber, DateTime from, DateTime to, IEnumerable<string> dataIdList,
        out Dictionary<string, List<Tuple<DateTime, double>>> returnDatas, ref double min, ref double max)
        {
            returnDatas = new Dictionary<string, List<Tuple<DateTime, double>>>();
            for (DateTime dfrom = new DateTime(from.Year, from.Month, from.Day); dfrom < to; dfrom += new TimeSpan(1, 0, 0, 0))
            {
                DateTime begin = (dfrom.Year == from.Year && dfrom.Month == from.Month && dfrom.Day == from.Day) ? from : new DateTime(dfrom.Year, dfrom.Month, dfrom.Day, 0, 0, 0, 0);
                DateTime end = (dfrom.Date == to.Date) ? to : new DateTime(dfrom.Year, dfrom.Month, dfrom.Day, 23, 59, 59, 999);
                //if (begin.Date > DateTime.Today)
                //    continue;
                try
                {
                    string sql = "select time AS InternalTimeStamp";
                    foreach (var dataId in dataIdList)
                    {
                        if (!returnDatas.Keys.Contains(dataId) && dataId.StartsWith($"{chamber}."))
                        {
                            returnDatas[dataId] = new List<Tuple<DateTime, double>>();
                            sql += "," + string.Format("\"{0}\"", dataId);
                        }
                    }

                    sql += string.Format(" from \"{0}\" where time > {1} and time <= {2} order by time asc;",
                        begin.ToString("yyyyMMdd") + "." + chamber, begin.Ticks, end.Ticks);

                    using (var table = QueryDataClient.Instance.Service.QueryData(sql))
                    {
                        if (table == null || table.Rows.Count == 0)
                            continue;
                        DateTime dt = new DateTime();
                        Dictionary<int, string> colName = new Dictionary<int, string>();
                        for (int colNo = 0; colNo < table.Columns.Count; colNo++)
                            colName.Add(colNo, table.Columns[colNo].ColumnName);
                        for (int rowNo = 0; rowNo < table.Rows.Count; rowNo++)
                        {
                            var row = table.Rows[rowNo];

                            for (int i = 0; i < table.Columns.Count; i++)
                            {
                                if (i == 0)
                                {
                                    long ticks = (long)row[i];
                                    dt = new DateTime(ticks);
                                }
                                else
                                {
                                    string dataId = colName[i];
                                    double value = 0.0;
                                    if (row[i] is DBNull || row[i] == null)
                                    {
                                        value = 0.0;
                                    }
                                    else if (row[i] is bool)
                                    {
                                        value = (bool)row[i] ? 1.0 : 0;
                                    }
                                    else
                                    {
                                        value = (double)float.Parse(row[i].ToString());
                                    }

                                    returnDatas[dataId].Add(Tuple.Create(dt, value));
                                    if (value < min)
                                        min = value;
                                    if (value > max)
                                        max = value;
                                }
                            }
                        }
                        table.Clear();
                    }
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);

                    return false;
                }
            }
            return true;
        }

        public string SelectDataExport(string path)
        {
            try
            {
                double valueMin = double.MaxValue;
                double valueMax = double.MinValue;
                DateTime dtMin = DateTime.MaxValue;
                DateTime dtMax = DateTime.MinValue;
                foreach (var recipe in SelectedRecipes)
                {
                    if (Convert.ToDateTime(recipe.StartTime) < dtMin)
                        dtMin = Convert.ToDateTime(recipe.StartTime);

                    if (!string.IsNullOrEmpty(recipe.EndTime))
                    {
                        if (Convert.ToDateTime(recipe.EndTime) > dtMax)
                            dtMax = Convert.ToDateTime(recipe.EndTime);
                    }
                    DateTime dtFrom = Convert.ToDateTime(recipe.StartTime);
                    DateTime dtTo = dtFrom.AddMinutes(10);
                    if (!string.IsNullOrEmpty(recipe.EndTime))
                    {
                        dtTo = Convert.ToDateTime(recipe.EndTime);
                    }
                    Dictionary<string, List<Tuple<DateTime, double>>> result;
                    Dictionary<DateTime, double[]> timeValue = new Dictionary<DateTime, double[]>();
                    System.Data.DataSet ds = new System.Data.DataSet();
                    var array = recipe.Recipe.Split(new char[] { '\\' });
                    string fileName = $"{array[array.Length - 1]}_ST{ConvertFileName(recipe.StartTime)}_ED{ConvertFileName(recipe.EndTime)}";
                    //ds.Tables.Add(new System.Data.DataTable(fileName));
                    ds.Tables.Add(new System.Data.DataTable($"{array[array.Length - 1]}_Export_{DateTime.Now:yyyyMMdd_HHmmss}"));
                    ds.Tables[0].Columns.Add("Time");
                    ds.Tables[0].Columns[0].DataType = typeof(DateTime);
                    if (GetDbData(recipe.Chamber, dtFrom, dtTo, SelectedParameters, out result, ref valueMin, ref valueMax))
                    {
                        int i = 0;
                        foreach (var data in result)
                        {
                            List<Tuple<DateTime, double>> x = result[data.Key];
                            foreach (var tuple in x)
                            {
                                if (!timeValue.ContainsKey(tuple.Item1))
                                    timeValue[tuple.Item1] = new double[result.Values.Count];

                                timeValue[tuple.Item1][i] = tuple.Item2;
                            }

                            ds.Tables[0].Columns.Add(data.Key);
                            ds.Tables[0].Columns[i + 1].DataType = typeof(double);
                            i++;
                        }
                    }

                    Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                    dlg.DefaultExt = ".xlsx"; // Default file extension 
                    dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
                    //dlg.FileName = $"{path}\\{recipe.Chamber}_{fileName}.xlsx";  //导出的名称
                    dlg.FileName = $"{path}\\{recipe.Chamber}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
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

                    if (!ExcelHelper.ExportToExcel(dlg.FileName, ds, out string reason))
                    {
                        return $"{dlg.FileName} export failed, {reason}";
                    }
                    System.Threading.Thread.Sleep(10);
                }
                return null;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return $"Write failed,{ex.Message}";
            }
        }

        private string ConvertFileName(string str)
        {
            string ret = null;
            string specialkey = "[`~!@#$^&*()=|{}':;',\\[\\].<>/?~!@#?……&*()——|{}??‘;:”“'?,?? -]";
            ret = System.Text.RegularExpressions.Regex.Replace(str, specialkey, "");
            return ret;
        }

        public async void Export()
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            string ret = await Task.Run(() => SelectDataExport(dialog.SelectedPath));
            if (ret != null)
            {
                MessageBox.Show(ret, "Export", MessageBoxButton.OK,
                      MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Export succeed", "Export", MessageBoxButton.OK,
                      MessageBoxImage.Information);
            }
        }

        #endregion
    }
}
