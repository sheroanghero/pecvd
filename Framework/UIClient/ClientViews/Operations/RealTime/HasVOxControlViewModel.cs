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
    public class HasVOxControlViewModel : ModuleUiViewModelBase, ISupportMultipleSystem
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

        public List<ObservableCollection<IRenderableSeries>> SelectedDataList = new List<ObservableCollection<IRenderableSeries>> { };
        public ObservableCollection<IRenderableSeries> SelectedData1 { get; set; }
        public ObservableCollection<IRenderableSeries> SelectedData2 { get; set; }
        public ObservableCollection<IRenderableSeries> SelectedData3 { get; set; }
        public ObservableCollection<IRenderableSeries> SelectedData4 { get; set; }
        public ObservableCollection<IRenderableSeries> SelectedData5 { get; set; }
        public ObservableCollection<IRenderableSeries> SelectedData6 { get; set; }
        public ObservableCollection<IRenderableSeries> SelectedData7 { get; set; }
        public ObservableCollection<IRenderableSeries> SelectedData8 { get; set; }
        public ObservableCollection<IRenderableSeries> SelectedData9 { get; set; }

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

        public string Module => SystemName;

        [Subscription("VOX.ShieidingTemperature")]
        public float ShieidingTemperature { get; set; }

        [Subscription("VOX.O2Flow")]
        public float O2Flow { get; set; }

        [Subscription("VOX.O2Pressure")]
        public float O2Pressure { get; set; }

        [Subscription("VOX.AiSetVoltage")]
        public float AiSetVoltage { get; set; }
        [Subscription("VOX.CurrentSpeed")]
        public float CurrentSpeed { get; set; }
        [Subscription("VOX.CurrentPosition")]
        public float CurrentPosition { get; set; }

        [Subscription("DCPower.Voltage")]
        public float DCPowerVoltage { get; set; }
        [Subscription("DCPower.Current")]
        public float DCPowerCurrent { get; set; }

        [Subscription("WaterFlow.WaterFlow")]
        public float WaterFlow { get; set; }

        [Subscription("ChamberPressure")]
        public float ChamberPressure { get; set; }

        [Subscription("ChamberHeater.FeedBack")]
        public float HeaterFeedBack { get; set; }


        [Subscription("Mfc1.Feedback")]
        public float Gas1MFCFeedBack { get; set; }

        public string SelectedData1Display => "ChamberPressure : " + ChamberPressure.ToString("0.0E0");
        public string SelectedData2Display => "HeaterTemperature : " + HeaterFeedBack;
        public string SelectedData3Display => "ShieidingTemperature : " + ShieidingTemperature;
        public string SelectedData4Display => "DCPower.Voltage : " + DCPowerVoltage;
        public string SelectedData5Display => "DCPower.Current : " + DCPowerCurrent;
        public string SelectedData6Display => "O2Flow : " + O2Flow.ToString("0.00");
        public string SelectedData7Display => "O2Pressure : " + O2Pressure;
        public string SelectedData8Display => "Ar Flowrate : " + Gas1MFCFeedBack;
        public string SelectedData9Display => "WaterFlow : " +WaterFlow;

        #region Function
        public HasVOxControlViewModel()
        {
            DisplayName = "HasVOxControl";

            SelectedData = new ObservableCollection<IRenderableSeries>();

            SelectedData1 = new ObservableCollection<IRenderableSeries>();
            SelectedData2 = new ObservableCollection<IRenderableSeries>();
            SelectedData3 = new ObservableCollection<IRenderableSeries>();
            SelectedData4 = new ObservableCollection<IRenderableSeries>();
            SelectedData5 = new ObservableCollection<IRenderableSeries>();
            SelectedData6 = new ObservableCollection<IRenderableSeries>();
            SelectedData7 = new ObservableCollection<IRenderableSeries>();
            SelectedData8 = new ObservableCollection<IRenderableSeries>();
            SelectedData9 = new ObservableCollection<IRenderableSeries>();

            SelectedDataList.Add(SelectedData1);
            SelectedDataList.Add(SelectedData2);
            SelectedDataList.Add(SelectedData3);
            SelectedDataList.Add(SelectedData4);
            SelectedDataList.Add(SelectedData5);
            SelectedDataList.Add(SelectedData6);
            SelectedDataList.Add(SelectedData7);
            SelectedDataList.Add(SelectedData8);
            SelectedDataList.Add(SelectedData9);

            //var line = new ChartDataLine($"{SystemName}.ChamberPressure");
            //line.Tag = $"{SystemName}.ChamberPressure";

            //SelectedData.Add(line);

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

                Application.Current?.Dispatcher.Invoke(new Action(() =>
                {


                    if (!String.IsNullOrEmpty(Module))
                    {
                        if (SelectedData1.Count == 0)
                        {
                            var line = new ChartDataLine($"{Module}.ChamberPressure");
                            line.Tag = $"ChamberPressure";

                            SelectedData1.Add(line);

                            foreach (var item in SelectedData1)
                            {
                                if (item.Stroke.Equals(System.Windows.Media.Color.FromArgb(255, 0, 0, 255)))
                                {
                                    Color drawingColor = colorQueue.Peek();
                                    item.Stroke = System.Windows.Media.Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
                                    colorQueue.Enqueue(colorQueue.Dequeue());
                                }
                            }
                        }
                        else if (SelectedData2.Count == 0)
                        {
                            var line = new ChartDataLine($"{Module}.ChamberHeater.FeedBack");
                            line.Tag = $"ChamberHeater.FeedBack";

                            SelectedData2.Add(line);

                            foreach (var item in SelectedData2)
                            {
                                if (item.Stroke.Equals(System.Windows.Media.Color.FromArgb(255, 0, 0, 255)))
                                {
                                    Color drawingColor = colorQueue.Peek();
                                    item.Stroke = System.Windows.Media.Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
                                    colorQueue.Enqueue(colorQueue.Dequeue());
                                }
                            }
                        }
                        else if (SelectedData3.Count == 0)
                        {
                            var line = new ChartDataLine($"{Module}.VOX.ShieidingTemperature");
                            line.Tag = $"VOX.ShieidingTemperature";

                            SelectedData3.Add(line);

                            foreach (var item in SelectedData3)
                            {
                                if (item.Stroke.Equals(System.Windows.Media.Color.FromArgb(255, 0, 0, 255)))
                                {
                                    Color drawingColor = colorQueue.Peek();
                                    item.Stroke = System.Windows.Media.Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
                                    colorQueue.Enqueue(colorQueue.Dequeue());
                                }
                            }
                        }
                        else if (SelectedData4.Count == 0)
                        {
                            var line = new ChartDataLine($"{Module}.DCPower.Voltage");
                            line.Tag = $"DCPower.Voltage";

                            SelectedData4.Add(line);

                            foreach (var item in SelectedData4)
                            {
                                if (item.Stroke.Equals(System.Windows.Media.Color.FromArgb(255, 0, 0, 255)))
                                {
                                    Color drawingColor = colorQueue.Peek();
                                    item.Stroke = System.Windows.Media.Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
                                    colorQueue.Enqueue(colorQueue.Dequeue());
                                }
                            }
                        }
                        else if (SelectedData5.Count == 0)
                        {
                            var line = new ChartDataLine($"{Module}.DCPower.Current");
                            line.Tag = $"DCPower.Current";

                            SelectedData5.Add(line);

                            foreach (var item in SelectedData5)
                            {
                                if (item.Stroke.Equals(System.Windows.Media.Color.FromArgb(255, 0, 0, 255)))
                                {
                                    Color drawingColor = colorQueue.Peek();
                                    item.Stroke = System.Windows.Media.Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
                                    colorQueue.Enqueue(colorQueue.Dequeue());
                                }
                            }
                        }
                        else if (SelectedData6.Count == 0)
                        {
                            var line = new ChartDataLine($"{Module}.VOX.O2Flow");
                            line.Tag = $"VOX.O2Flow";

                            SelectedData6.Add(line);

                            foreach (var item in SelectedData6)
                            {
                                if (item.Stroke.Equals(System.Windows.Media.Color.FromArgb(255, 0, 0, 255)))
                                {
                                    Color drawingColor = colorQueue.Peek();
                                    item.Stroke = System.Windows.Media.Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
                                    colorQueue.Enqueue(colorQueue.Dequeue());
                                }
                            }
                        }
                        else if (SelectedData7.Count == 0)
                        {
                            var line = new ChartDataLine($"{Module}.VOX.O2Pressure");
                            line.Tag = $"VOX.O2Pressure";

                            SelectedData7.Add(line);

                            foreach (var item in SelectedData7)
                            {
                                if (item.Stroke.Equals(System.Windows.Media.Color.FromArgb(255, 0, 0, 255)))
                                {
                                    Color drawingColor = colorQueue.Peek();
                                    item.Stroke = System.Windows.Media.Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
                                    colorQueue.Enqueue(colorQueue.Dequeue());
                                }
                            }
                        }
                        else if (SelectedData8.Count == 0)
                        {
                            var line = new ChartDataLine($"{Module}.Mfc1.Feedback");
                            line.Tag = $"Mfc1.Feedback";

                            SelectedData8.Add(line);

                            foreach (var item in SelectedData8)
                            {
                                if (item.Stroke.Equals(System.Windows.Media.Color.FromArgb(255, 0, 0, 255)))
                                {
                                    Color drawingColor = colorQueue.Peek();
                                    item.Stroke = System.Windows.Media.Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
                                    colorQueue.Enqueue(colorQueue.Dequeue());
                                }
                            }
                        }
                        else if (SelectedData9.Count == 0)
                        {
                            var line = new ChartDataLine($"{Module}.WaterFlow.WaterFlow");
                            line.Tag = $"{Module}.WaterFlow";

                            SelectedData9.Add(line);

                            foreach (var item in SelectedData9)
                            {
                                if (item.Stroke.Equals(System.Windows.Media.Color.FromArgb(255, 0, 0, 255)))
                                {
                                    Color drawingColor = colorQueue.Peek();
                                    item.Stroke = System.Windows.Media.Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
                                    colorQueue.Enqueue(colorQueue.Dequeue());
                                }
                            }
                        }


                    }
                }));
               



                foreach (var item in SelectedDataList)
                {
                    Dictionary<string, object> data = null;
                    if (item.Count > 0)
                    {
                        data = QueryDataClient.Instance.Service.PollData(Array.ConvertAll(item.ToArray(), x => (x as ChartDataLine).DataName));
                    }

                    Application.Current?.Dispatcher.Invoke(new Action(() =>
                    {


                        ParameterNodes = _provider.GetParameters();

                        AppendData(data, item);
                    }));
                }

                


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


        public void AppendData(Dictionary<string, object> data, ObservableCollection<IRenderableSeries> selectedData)
        {
            if (data == null)
                return;

            DateTime dt = DateTime.Now;

            foreach (var item in selectedData)
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

        

        public void Preset()
        {

        }

        public void Clear()
        {

        }

        public void Apply()
        {

        }

        

  
        
        /// <summary>
        /// Refresh tree node status from current to parent
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
       

        #region Parameter Grid Control
        

        private void SetParameterNode(ObservableCollection<ParameterNode> nodes, bool isChecked)
        {
            foreach (ParameterNode n in nodes)
            {
                n.Selected = isChecked;
                SetParameterNode(n.ChildNodes, isChecked);
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
