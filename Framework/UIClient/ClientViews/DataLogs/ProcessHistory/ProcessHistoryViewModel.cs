using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.ControlDataContext;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Utilities;
using MECF.Framework.UI.Client.ClientBase;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Data.Model;
using Cali = Caliburn.Micro;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.ProcessHistory
{
    public class ProcessHistoryViewModel : UiViewModelBase
    {
        public bool IsPermission { get => this.Permission == 3; }

        public ObservableCollection<IRenderableSeries> SelectedData { get; set; }

        public AutoRange ChartAutoRange
        {
            get { return EnableAutoZoom ? AutoRange.Always : AutoRange.Never; }
        }

        public AutoRange AutoRangeX
        {
            get => _autoRangeX;
            set { _autoRangeX = value; NotifyOfPropertyChange(nameof(AutoRangeX)); }
        }

        public AutoRange AutoRangeY
        {
            get => _autoRangeY;
            set { _autoRangeY = value; NotifyOfPropertyChange(nameof(AutoRangeY)); }
        }

        private bool _enableAutoZoom = true;
        public bool EnableAutoZoom
        {
            get { return _enableAutoZoom; }

            set
            {
                _enableAutoZoom = value;
                NotifyOfPropertyChange(nameof(EnableAutoZoom));
                NotifyOfPropertyChange(nameof(ChartAutoRange));
            }
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

        private IRange _ValueRange;
        public IRange ValueRange
        {
            get { return _ValueRange; }
            set { _ValueRange = value; }
        }

        private Queue<Color> colorQueue = new Queue<Color>(new Color[]{Color.Red,Color.Orange,Color.Yellow,Color.Green,Color.Blue,Color.Pink,Color.Purple,Color.Aqua,Color.Bisque,Color.Brown,Color.BurlyWood,Color.CadetBlue,
            Color.CornflowerBlue,Color.DarkBlue,Color.DarkCyan,Color.DarkGray,Color.DarkGreen,Color.DarkKhaki,Color.DarkMagenta,Color.DarkOliveGreen, Color.DarkOrange,
            Color.DarkSeaGreen,Color.DarkSlateBlue,Color.DarkSlateGray,Color.DarkViolet,Color.DeepPink,Color.DeepSkyBlue,Color.DimGray, Color.DodgerBlue,Color.ForestGreen, Color.Gold,
            Color.Gray,Color.GreenYellow,Color.HotPink,Color.Indigo,Color.Khaki,Color.LightBlue,Color.LightCoral,Color.LightGreen, Color.LightPink,Color.LightSalmon,Color.LightSkyBlue,
            Color.LightSlateGray,Color.LightSteelBlue,Color.LimeGreen,Color.MediumOrchid,Color.MediumPurple,Color.MediumSeaGreen,Color.MediumSlateBlue,Color.MediumSpringGreen,
            Color.MediumTurquoise,Color.Moccasin,Color.NavajoWhite,Color.Olive,Color.OliveDrab,Color.OrangeRed,Color.Orchid,Color.PaleGoldenrod,Color.PaleGreen,
            Color.PeachPuff,Color.Peru,Color.Plum,Color.PowderBlue,Color.RosyBrown,Color.RoyalBlue,Color.SaddleBrown,Color.Salmon,Color.SeaGreen, Color.Sienna,
            Color.SkyBlue,Color.SlateBlue,Color.SlateGray,Color.SpringGreen,Color.Teal,Color.Aquamarine,Color.Tomato,Color.Turquoise,Color.Violet,Color.Wheat, Color.YellowGreen});

        Cali.WindowManager wm = new Cali.WindowManager();
        SelectDataViewModel selectDataDlg = new SelectDataViewModel();
        private AutoRange _autoRangeX;
        private AutoRange _autoRangeY;

        public ProcessHistoryViewModel()
        {
            DisplayName = "Process History";

            SelectedData = new ObservableCollection<IRenderableSeries>();

            TimeRange = new DateRange(DateTime.Now.AddMinutes(-1), DateTime.Now.AddMinutes(59));

            AutoRangeX = AutoRange.Once;
            AutoRangeY = AutoRange.Once;
        }

        public void SelectData()
        {
            selectDataDlg.CheckAllRecipeFlag = false;
            selectDataDlg.SelectedRecipes.Clear();
            selectDataDlg.SelectedParameters.Clear();

            var settings = new Dictionary<string, object> { { "Title", "Select Recipe Data" } };
            bool? ret = wm.ShowDialog(selectDataDlg, null, settings);
            if (ret == null || !ret.Value)
                return;

            SelectedData.Clear();

            double valueMin = double.MaxValue;
            double valueMax = double.MinValue;
            DateTime dtMin = DateTime.MaxValue;
            DateTime dtMax = DateTime.MinValue;

            foreach (var recipe in selectDataDlg.SelectedRecipes)
            {
                if (Convert.ToDateTime(recipe.StartTime) < dtMin)
                    dtMin = Convert.ToDateTime(recipe.StartTime);

                if (!string.IsNullOrEmpty(recipe.EndTime))
                {
                    if (Convert.ToDateTime(recipe.EndTime) > dtMax)
                        dtMax = Convert.ToDateTime(recipe.EndTime);
                }

                QueryData(recipe, selectDataDlg.SelectedParameters, ref valueMin, ref valueMax);
            }

            TimeRange = new DateRange(dtMin.AddMinutes(-1), dtMax.AddMinutes(5));
            ValueRange = new DoubleRange(valueMin - 5, valueMax + 5);
        }

        public void QueryData(RecipeItem recipe, ObservableCollection<string> keys, ref double min, ref double max)
        {
            if (string.IsNullOrEmpty(recipe.StartTime))
            {
                MessageBox.Show($"can not query recipe data, did not record {recipe.Recipe} start time");
                return;
            }

            DateTime dtFrom = Convert.ToDateTime(recipe.StartTime);
            DateTime dtTo = dtFrom.AddMinutes(10);
            if (!string.IsNullOrEmpty(recipe.EndTime))
            {
                dtTo = Convert.ToDateTime(recipe.EndTime);
            }

            Dictionary<string, List<Tuple<DateTime, double>>> result;

            if (GetDbData(recipe.Chamber, dtFrom, dtTo, keys, out result, ref min, ref max))
            {
                foreach (var data in result)
                {
                    ChartDataLine line = new ChartDataLine(data.Key);
                    line.DataSource = recipe.Recipe;

                    foreach (var tuple in data.Value)
                    {
                        line.Append(tuple.Item1, tuple.Item2);
                    }
                    SelectedData.Add(line);
                    SelectedDataChanged();
                }
            }
        }

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

        public void DeleteAll()
        {
            SelectedData.Clear();
        }


        public void Delete(ChartDataLine cp)
        {
            if (cp != null && SelectedData.Contains(cp))
            {

                SelectedData.Remove(cp);
            }

        }

        public void ExportAll()
        {
            try
            {
                if (SelectedData.Count == 0)
                {
                    MessageBox.Show($"Please select the data you want to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.DefaultExt = ".xlsx"; // Default file extension 
                dlg.Filter = "Excel数据表格文件(*.xlsx)|*.xlsx"; // Filter files by extension 
                dlg.FileName = $"{DisplayName}_{DateTime.Now:yyyyMMdd_HHmmss}";
                Nullable<bool> result = dlg.ShowDialog();// Show open file dialog box
                if (result == true) // Process open file dialog box results
                {
                    System.Data.DataSet ds = new System.Data.DataSet();
                    ds.Tables.Add(new System.Data.DataTable($"Export_{DateTime.Now:yyyyMMdd_HHmmss}"));
                    ds.Tables[0].Columns.Add("Time");
                    ds.Tables[0].Columns[0].DataType = typeof(DateTime);

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

                    if (!ExcelHelper.ExportToExcel(dlg.FileName, ds, out string reason))
                    {
                        MessageBox.Show($"Export failed, {reason}", "Export", MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    MessageBox.Show($"Export succeed, file save as {dlg.FileName}", "Export", MessageBoxButton.OK,
                        MessageBoxImage.Information);
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

    }
}
