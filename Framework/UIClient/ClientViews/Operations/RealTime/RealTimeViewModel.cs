using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using MECF.Framework.Common.ControlDataContext;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Utilities;
using MECF.Framework.UI.Client.CenterViews.DataLogs.ProcessHistory;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.RenderableSeries;

namespace MECF.Framework.UI.Client.CenterViews.Operations.RealTime
{
    public class RealtimeViewModel : UiViewModelBase
    {
        #region Property
        public bool IsPermission { get => this.Permission == 3; }

        private const int MAX_PARAMETERS = 20;

        private Queue<Color> colorQueue = new Queue<Color>(new Color[]{Color.Aqua,Color.Aquamarine,Color.Bisque,Color.Blue,Color.Brown,Color.BurlyWood,Color.CadetBlue,
            Color.CornflowerBlue,Color.DarkBlue,Color.DarkCyan,Color.DarkGray,Color.DarkGreen,Color.DarkKhaki,Color.DarkMagenta,Color.DarkOliveGreen, Color.DarkOrange,
            Color.DarkSeaGreen,Color.DarkSlateBlue,Color.DarkSlateGray,Color.DarkViolet,Color.DeepPink,Color.DeepSkyBlue,Color.DimGray, Color.DodgerBlue,Color.ForestGreen, Color.Gold,
            Color.Gray,Color.Green,Color.GreenYellow,Color.HotPink,Color.Indigo,Color.Khaki,Color.LightBlue,Color.LightCoral,Color.LightGreen, Color.LightPink,Color.LightSalmon,Color.LightSkyBlue,
            Color.LightSlateGray,System.Drawing.Color.LightSteelBlue,Color.LimeGreen,Color.MediumOrchid,Color.MediumPurple,Color.MediumSeaGreen,Color.MediumSlateBlue,Color.MediumSpringGreen,
            Color.MediumTurquoise,Color.Moccasin,Color.NavajoWhite,Color.Olive,Color.OliveDrab,Color.Orange,Color.OrangeRed,Color.Orchid,Color.PaleGoldenrod,Color.PaleGreen,
            Color.PeachPuff,Color.Peru,Color.Pink,Color.Plum,Color.PowderBlue,Color.Purple,Color.Red,Color.RosyBrown,Color.RoyalBlue,Color.SaddleBrown,Color.Salmon,Color.SeaGreen, Color.Sienna,
            Color.SkyBlue,Color.SlateBlue,Color.SlateGray,Color.SpringGreen,Color.Teal,Color.Tomato,Color.Turquoise,Color.Violet,Color.Wheat, Color.Yellow,Color.YellowGreen});

        private ObservableCollection<ParameterNode> _ParameterNodes;
        public ObservableCollection<ParameterNode> ParameterNodes
        {
            get { return _ParameterNodes; }
            set { _ParameterNodes = value; NotifyOfPropertyChange("ParameterNodes"); }
        }

        private int _selectedDataCount;
        public ObservableCollection<IRenderableSeries> SelectedData { get; set; }

        object _lockSelection = new object();

        public AutoRange ChartAutoRange
        {
            get { return EnableAutoZoom ? AutoRange.Always : AutoRange.Never; }
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

        RealtimeProvider _provider = new RealtimeProvider();

        [IgnorePropertyChange]
        public int TrendInterval { get; set; }
        public bool IntervalSaved { get; set; }

        [IgnorePropertyChange]
        public int TrendTimeSpan { get; set; }
        public bool TimeSpanSaved { get; set; }

        private PeriodicJob _thread;

        private int _pointCount = 0;
        #endregion

        #region Function
        public RealtimeViewModel()
        {
            DisplayName = "Realtime";

            SelectedData = new ObservableCollection<IRenderableSeries>();

            ParameterNodes = _provider.GetParameters();

            IntervalSaved = true;
            TrendInterval = 500;
            TimeSpanSaved = true;
            TrendTimeSpan = 60*5;

            _thread = new PeriodicJob(TrendInterval, MonitorData, "RealTime", true);
            _pointCount = Math.Max( TrendTimeSpan*1000 / TrendInterval, 10);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            //_provider.Clear();
            //_thread = new PeriodicJob(100, MonitorData, "RealTime", true);
        }
        //protected override void OnDeactivate(bool close)
        //{
        //    base.OnDeactivate(close);
        //    _thread.Stop();
        //}
        protected bool MonitorData()
        {
            try
            {
                Dictionary<string, object> data = null;
                if (SelectedData.Count > 0)
                {
                    data = QueryDataClient.Instance.Service.PollData(Array.ConvertAll(SelectedData.ToArray(), x => (x as ChartDataLine).DataName));
                }

                ParameterNodes = _provider.GetParameters();

                Application.Current?.Dispatcher.Invoke(new Action(() =>
                {
                    if (SelectedData != null && SelectedData.Count != _selectedDataCount)
                    {
                        SelectedDataChanged();
                        _selectedDataCount = SelectedData.Count;
                    }

                    AppendData(data);
                }));
                for (int j = 0; j < ParameterNodes.Count; j++)
                {
                    ParameterNode par = ParameterNodes[j];
                    par.IsVisibilityParentNode = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }

            return true;
        }


        public void AppendData(Dictionary<string, object> data)
        {
            if (data == null)
                return;

            DateTime dt = DateTime.Now;

            foreach (var item in SelectedData)
            {
                var seriesItem = item as ChartDataLine;
                if (seriesItem.Capacity != _pointCount)
                {
                    seriesItem.Capacity = _pointCount;
                }

                if (!data.ContainsKey(seriesItem.DataName))
                    continue;

                seriesItem.Append(dt, Convert.ToDouble(data[seriesItem.DataName]));
            }
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
                    _selectedDataCount = SelectedData.Count;
                }
            }
        }

        public void Preset()
        {

        }

        public void Clear()
        {

        }

        public void Apply()
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
                            var line = new ChartDataLine(node.Name);
                            line.Tag = node;

                            SelectedData.Add(line);
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

        public void SetInterval( )
        {
            _thread.ChangeInterval(TrendInterval);
            _pointCount = Math.Max(10, TrendTimeSpan * 1000 / TrendInterval);
            IntervalSaved = true;
            NotifyOfPropertyChange(nameof(IntervalSaved));
        }

        public void SetTimeSpan()
        {
            _pointCount = Math.Max(10, TrendTimeSpan * 1000 / TrendInterval);
            TimeSpanSaved = true;
            NotifyOfPropertyChange(nameof(TimeSpanSaved));
        }

        #endregion


        #endregion
    }
}
