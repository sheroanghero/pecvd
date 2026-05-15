using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using Aitex.Core.Util;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.RenderableSeries;

namespace MECF.Framework.Common.ControlDataContext
{
    public class ChartDataLine : FastLineRenderableSeries, INotifyPropertyChanged
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void InvokePropertyChanged(string propertyName)
        {
            PropertyChangedEventArgs eventArgs = new PropertyChangedEventArgs(propertyName);
            PropertyChangedEventHandler changed = PropertyChanged;
            if (changed != null)
            {
                changed(this, eventArgs);
            }
        }
        public void InvokePropertyChanged()
        {
            Type t = this.GetType();
            var ps = t.GetProperties();
            foreach (var p in ps)
            {
                InvokePropertyChanged(p.Name);
            }
        }
        #endregion

        public string UniqueId { get; set; }

        public List<Tuple<DateTime, double>> Points
        {
            get
            {
                lock (_lockData)
                {
                    return _queueRawData.ToList();
                }
            }
        }

        public string DataSource { get; set; }
        public string DataName { get; set; }

        public string DisplayName
        {
            get
            {
                return DataSeries.SeriesName;
            }
            set
            {
                DataSeries.SeriesName = value;
            }
        }
        public double LineThickness
        {
            get
            {
                return StrokeThickness;
            }
            set
            {
                var i = Convert.ToInt32(value);
                if (i < 1) i = 1;
                if (i > 100) i = 100;
                StrokeThickness = i;
                InvokePropertyChanged("LineThickness");
            }
        }

        private double _dataFactor = 1.0;
        public double DataFactor
        {
            get => _dataFactor;
            set
            {
                if (Math.Abs(_dataFactor - value) > 0.001)
                {
                    _dataFactor = value;
                    InvokePropertyChanged(nameof(DataFactor));
                    UpdateChartSeriesValue();
                }
            }
        }

        private double _dataOffset = 0;
        public double DataOffset
        {
            get => _dataOffset;
            set
            {
                if (Math.Abs(_dataOffset - value) > 0.001)
                {
                    _dataOffset = value;
                    InvokePropertyChanged(nameof(DataFactor));
                    UpdateChartSeriesValue();
                }
            }
        }

        private int _capacity = 10000;

        public int Capacity
        {
            get { return _capacity; }
            set
            {
                _capacity = value;
                //DataSeries.FifoCapacity = value;
                if (_queueRawData != null)
                {
                    _queueRawData.FixedSize = value;
                }

            }
        }

        private object _lockData = new object();

        private FixSizeQueue<Tuple<DateTime, double>> _queueRawData;

        public ChartDataLine(string dataName)
        : this(dataName, dataName, Colors.Blue)
        {
        }

        public ChartDataLine(string dataName, string displayName, Color seriesColor)
        {
            UniqueId = Guid.NewGuid().ToString();

            _queueRawData = new FixSizeQueue<Tuple<DateTime, double>>(_capacity);

            XAxisId = "DefaultAxisId";
            YAxisId = "DefaultAxisId";
            DataSeries = new XyDataSeries<DateTime, double>(0);

            DisplayName = displayName;

            DataName = dataName;

            Stroke = seriesColor;

            IsVisible = true;
            DataOffset = 0;
            LineThickness = 1;
            DataFactor = 1;
        }

        public void Append(DateTime dt, double value)
        {
            lock (_lockData)
            {
                _queueRawData.Enqueue(Tuple.Create(dt, value));
                var series = DataSeries as XyDataSeries<DateTime, double>;
                if (series != null)
                {
                    series.Append(dt, value * _dataFactor + _dataOffset);
                    while (series.Count > _capacity)
                    {
                        series.RemoveRange(0, series.Count - _capacity);
                    }

                }
                //(DataSeries as XyDataSeries<DateTime, double>)?.Append(dt, value*_dataFactor + _dataOffset);
            }

        }

        public void UpdateChartSeriesValue()
        {
            lock (_lockData)
            {
                using (DataSeries.SuspendUpdates())
                {
                    XyDataSeries<DateTime, double> s = DataSeries as XyDataSeries<DateTime, double>;
                    for (int i = 0; i < s.Count; i++)
                    {
                        s.Update(i, _queueRawData.ElementAt(i).Item2 * _dataFactor + _dataOffset);
                    }
                }
            }
        }

        public void ClearData()
        {
            lock (_lockData)
            {
                _queueRawData.Clear();
                (DataSeries as XyDataSeries<DateTime, double>)?.Clear();
            }
        }

    }
}
