using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.Serialization;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.RenderableSeries;
using Aitex.Core.UI.MVVM;
using Aitex.Core.UI.View.Smart;
using SciChart.Data.Model;

namespace Aitex.Core.UI.ControlDataContext
{

    [DataContract]
    [Serializable]
    public class HistoryDataItem
    {
        [DataMember]
        public DateTime dateTime
        {
            get;
            set;
        }

        [DataMember]
        public string dbName
        {
            get;
            set;
        }

        [DataMember]
        public double value
        {
            get;
            set;
        }
    }

    public class ProcessDataChartDataItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

		public DelegateCommand<object> SeriesSelectAllCommand
        {
            get;
            private set;
        }
        public DelegateCommand<object> SeriesSelectNoneCommand
        {
            get;
            private set;
        }
        public DelegateCommand<object> SeriesSelectDefaultCommand
        {
            get;
            private set;
        }

        public ObservableCollection<IRenderableSeries> RenderableSeries
        {
            get;
            set;
        }

        public string ProcessInfo
        {
            get;
            set;
        }

        public int Count
        {
            get
            {
                return RenderableSeries.Count;
            }
        }

        private int _capacity = 10000;

        private Queue<Color> colorQueue = new Queue<Color>(new Color[]{Color.Aqua,Color.Aquamarine,Color.Bisque,Color.Blue,Color.Brown,Color.BurlyWood,Color.CadetBlue,
	        Color.CornflowerBlue,Color.DarkBlue,Color.DarkCyan,Color.DarkGray,Color.DarkGreen,Color.DarkKhaki,Color.DarkMagenta,Color.DarkOliveGreen, Color.DarkOrange,
	        Color.DarkSeaGreen,Color.DarkSlateBlue,Color.DarkSlateGray,Color.DarkViolet,Color.DeepPink,Color.DeepSkyBlue,Color.DimGray, Color.DodgerBlue,Color.ForestGreen, Color.Gold,
	        Color.Gray,Color.Green,Color.GreenYellow,Color.HotPink,Color.Indigo,Color.Khaki,Color.LightBlue,Color.LightCoral,Color.LightGreen, Color.LightPink,Color.LightSalmon,Color.LightSkyBlue,
	        Color.LightSlateGray,Color.LightSteelBlue,Color.LimeGreen,Color.MediumOrchid,Color.MediumPurple,Color.MediumSeaGreen,Color.MediumSlateBlue,Color.MediumSpringGreen,
	        Color.MediumTurquoise,Color.Moccasin,Color.NavajoWhite,Color.Olive,Color.OliveDrab,Color.Orange,Color.OrangeRed,Color.Orchid,Color.PaleGoldenrod,Color.PaleGreen,
	        Color.PeachPuff,Color.Peru,Color.Pink,Color.Plum,Color.PowderBlue,Color.Purple,Color.Red,Color.RosyBrown,Color.RoyalBlue,Color.SaddleBrown,Color.Salmon,Color.SeaGreen, Color.Sienna,
	        Color.SkyBlue,Color.SlateBlue,Color.SlateGray,Color.SpringGreen,Color.Teal,Color.Tomato,Color.Turquoise,Color.Violet,Color.Wheat, Color.Yellow,Color.YellowGreen});

		public ProcessDataChartDataItem(int capacity = 60*5)
        {
            SeriesSelectAllCommand = new DelegateCommand<object>(new Action<object>(OnSeriesSelectAll), null);
            SeriesSelectNoneCommand = new DelegateCommand<object>(new Action<object>(OnSeriesSelectNone), null);
            SeriesSelectDefaultCommand = new DelegateCommand<object>(new Action<object>(OnSeriesSelectDefault), null);

            RenderableSeries = new ObservableCollection<IRenderableSeries>();
            
			_capacity = capacity;
        }

        public void SetInfo(string info)
        {
            ProcessInfo = info;
            InvokePropertyChanged("ProcessInfo");
        }

        public void AppendData(Dictionary<string, object> data)
        {
            if (data == null)
                return;

            foreach (var item in RenderableSeries)
            {
                var series = item as SmartDataLine;
                if (series == null)
                    continue;

                if (!data.ContainsKey(series.DbDataName))
                    continue;

                var dataSeries = series.DataSeries as XyDataSeries<DateTime, float>;

                while (dataSeries.Count > _capacity)
                    dataSeries.RemoveAt(0);

                dataSeries.Append(DateTime.Now, (float)(Convert.ToDouble(data[series.DbDataName])));
            }
        }

        public void UpdateCultureString(Dictionary<string, string> data)
        {
            if (data == null)
                return;

            foreach (var item in RenderableSeries)
            {
                var series = item as SmartDataLine;
                if (series == null)
                    continue;

                if (!data.ContainsKey(series.DbDataName))
                    continue;

                series.DisplayName = data[series.DbDataName];
            }
        }

        public void UpdateData(List<HistoryDataItem> data)
        {
            ClearData();

            if (data == null || data.Count == 0)
                return;

            foreach (HistoryDataItem dataItem in data)
            {
                foreach (var item in RenderableSeries)
                {
                    var series = item as SmartDataLine;
                    if (series == null)
                        continue;
                    var dataSeries = series.DataSeries as XyDataSeries<DateTime, float>;

                    while (dataSeries.Count > _capacity)
                        dataSeries.RemoveAt(0);

                    if (series.DbDataName != dataItem.dbName)
                        continue;

                    dataSeries.Append(dataItem.dateTime, (float)(dataItem.value));

                }

            }
        }

        public void ClearData()
        {
            foreach (var item in RenderableSeries)
            {
                var series = item as SmartDataLine;
                if (series == null)
                    continue;
                var dataSeries = series.DataSeries as XyDataSeries<DateTime, float>;

                dataSeries.Clear();
            }
        }
 

        /// <summary>
        /// 如果已经有了key，更新名字和做标注
        /// 如果不存在，创建新的
        /// </summary>
        /// <param name="items"></param>
        public void UpdateColumns(Dictionary<string/*key value=database column name*/, Tuple<string, string, bool>> dicItems)
        {
            List<IRenderableSeries> lstRemovedSeries = new List<IRenderableSeries>();
            foreach (var seriesItem in RenderableSeries)
            {
                var series = seriesItem as SmartDataLine;
                if (series == null)
                    continue;

                if (!dicItems.ContainsKey(series.DbDataName))
                {
                    lstRemovedSeries.Add(seriesItem);
                    continue;
                }

                UpdateColumn(dicItems[series.DbDataName]);

                dicItems.Remove(series.DbDataName);
            }

            foreach (var newItem in dicItems)
            {
                RenderableSeries.Add(new SmartDataLine(newItem.Value.Item2, newItem.Value.Item3? System.Windows.Media.Colors.Blue : System.Windows.Media.Colors.Red,
                newItem.Value.Item1, true)
                {
                    YAxisId = newItem.Value.Item3 ? "PressureYAxisId" : "GeneralYAxisId"
                });            
            }

            foreach (var removeItem in lstRemovedSeries)
            {
                RenderableSeries.Remove(removeItem);
            }
        }

        public void UpdateColumn(Tuple<string, string, bool> columnInfo)
        {
            foreach (var item in RenderableSeries)
            {
                var series = item as SmartDataLine;
                if (series != null && series.DbDataName == columnInfo.Item1)
                {
                    series.DisplayName = columnInfo.Item2;
                    if (series.Stroke.Equals(System.Windows.Media.Color.FromArgb(255, 0, 0, 255)))
                    {
	                    Color drawingColor = colorQueue.Peek();
	                    series.Stroke = System.Windows.Media.Color.FromRgb(drawingColor.R, drawingColor.G, drawingColor.B);
	                    colorQueue.Enqueue(colorQueue.Dequeue());
                    }
					series.YAxisId = columnInfo.Item3 ? "PressureYAxisId" : "GeneralYAxisId";
                    break;
                }
            }
        }

        public void UpdateColumn(string dbName, string displayName, bool isPressure)
        {
            foreach (var item in RenderableSeries)
            {
                var series = item as SmartDataLine;
                if (series != null && series.DbDataName == dbName)
                {
                    series.DisplayName = displayName;
                    break;
                }
            }
        }

        public void UpdateColumnDisplayName(string dbName, string displayName)
        {
            foreach (var item in RenderableSeries)
            {
                var series = item as SmartDataLine;
                if (series != null && series.DbDataName == dbName)
                {
                    series.DisplayName = displayName;
                    break;
                }
            }     
        }

        public void RemoveColumn(string dbName)
        {
            foreach (var item in RenderableSeries)
            {
                var series = item as SmartDataLine;
                if (series != null && series.DbDataName == dbName)
                {
                    RenderableSeries.Remove(series);
                    break;
                }
            }
        }
        public void RemoveAllColumn()
        {
            ClearData();
            RenderableSeries.Clear();
        }
        public void InitColumns(Dictionary<string, string> items)
        {
            ClearData();
            RenderableSeries.Clear();

            foreach (KeyValuePair<string, string> item in items)
            {
                RenderableSeries.Add(new SmartDataLine(item.Key, System.Windows.Media.Colors.Blue,
                    item.Value, true)
                    {
                        YAxisId = item.Key.Contains("Pressure") ? "PressureYAxisId" : "GeneralYAxisId"
                    });
            }
        }

        public void SetColor(string key, System.Windows.Media.Color color)
        {
            foreach (var item in RenderableSeries)
            {
                var series = item as SmartDataLine;
                if (series == null)
                    continue;

                if (series.DisplayName == key)
                    series.Stroke = color;
            }
        }

        public void OnSeriesSelectAll(object param)
        {
            foreach (var item in RenderableSeries)
            {
                var series = item as SmartDataLine;
                if (series == null)
                    continue;
                series.IsVisible = true;
            }
        }

        public void OnSeriesSelectNone(object param)
        {
            foreach (var item in RenderableSeries)
            {
                var series = item as SmartDataLine;
                if (series == null)
                    continue;
                series.IsVisible = false;
            }
        }

        public void OnSeriesSelectDefault(object param)
        {
            foreach (var item in RenderableSeries)
            {
                var series = item as SmartDataLine;
                if (series == null)
                    continue;
                series.IsVisible = series.IsDefaultVisable;
                series.Stroke = series.DefaultSeriesColor;
                series.LineThickness = 1;
            }
        }
    }
}
