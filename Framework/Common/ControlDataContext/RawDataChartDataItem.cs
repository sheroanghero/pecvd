using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using SciChart.Charting.Visuals.Axes;
using SciChart.Data.Model;
using SciChart.Charting.Model.DataSeries;

namespace Aitex.Core.UI.ControlDataContext
{
    public class RawDataChartDataItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private AutoRange _AutoRange;
        public AutoRange AutoRange
        {
            get { return _AutoRange; }
            set
            {
                _AutoRange = value;
                InvokePropertyChanged("AutoRange");
            }
        }

        private DoubleRange _visibleRange;
        public DoubleRange VisibleRange
        {
            get
            {
                return _visibleRange;
            }
            set
            {
                _visibleRange = value;
                InvokePropertyChanged("VisibleRange");
            }
        }

        private bool _temperatureVisible = true;
        public bool TemperatureVisible
        {
            get { return _temperatureVisible; }
            set
            {
                _temperatureVisible = value;
                InvokePropertyChanged("TemperatureVisible");
            }
        }

        private bool _reflectVisible = true;
        public bool ReflectVisible
        {
            get { return _reflectVisible; }
            set
            {
                _reflectVisible = value;
                InvokePropertyChanged("ReflectVisible");
            }
        }

        private bool _curvatureVisible = true;
        public bool CurvatureVisible
        {
            get { return _curvatureVisible; }
            set
            {
                _curvatureVisible = value;
                InvokePropertyChanged("CurvatureVisible");
            }
        }

        private IDataSeries _reflectData;
        public IDataSeries  ReflectData
        {
            get { return _reflectData; }
            set
            {
                _reflectData = value;
                InvokePropertyChanged("ReflectData");
            }
        }

        private IDataSeries _temperatureData;
        public IDataSeries  TemperatureData
        {
            get { return _temperatureData; }
            set
            {
                _temperatureData = value;
                InvokePropertyChanged("TemperatureData");
            }
        }

        private IDataSeries _curvatureData;
        public IDataSeries  CurvatureData
        {
            get { return _curvatureData; }
            set
            {
                _curvatureData = value;
                InvokePropertyChanged("CurvatureData");
            }
        }

        public RawDataChartDataItem()
        {
            AutoRange = SciChart.Charting.Visuals.Axes.AutoRange.Always;
            VisibleRange = new DoubleRange(0, 10);

            _temperatureData = GetSeries();
            _reflectData = GetSeries();
            _curvatureData = GetSeries();
        }

        XyDataSeries<double, double> GetSeries()
        {
            Random rd = new Random();
            XyDataSeries<double, double> series = new XyDataSeries<double, double>();
            for (int j = 0; j < 1000; j++)
            {
                series.Append(j + 1, j+rd.Next(0,100));
            }
            return series;
        }
    }
}
