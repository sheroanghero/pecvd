using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SciChart.Charting.Visuals.RenderableSeries;
using System.ComponentModel;
using System.Windows.Media;

using SciChart.Charting.Model.DataSeries;
using Aitex.Core.Util;

namespace Aitex.Core.UI.View.Smart
{
    /// <summary>
    /// Customized line series type
    /// </summary>
    public class SmartDataLine : FastLineRenderableSeries, INotifyPropertyChanged
    {
        public SmartDataLine(string displayName, Color seriesColor, string dbName, bool isVisable)
        {
            UniqueId = Guid.NewGuid().ToString();

            XAxisId = "DefaultAxisId";      
            YAxisId = "DefaultAxisId";
            DataSeries = new XyDataSeries<DateTime, float>();

            DisplayName = displayName;
            
            DbDataName = dbName;

            Stroke = seriesColor;

            DefaultSeriesColor = seriesColor;

            NextQueryTime = DateTime.MinValue;

            IsVisible = isVisable;

            IsDefaultVisable = isVisable;
        }

        public DateTime NextQueryTime { get; set; }

        public bool IsDefaultVisable { get; set; }


        public Color DefaultSeriesColor { get; set; }

        public string DbDataName { get; private set; }

        public string DisplayName
        {
            get
            {
                return DataSeries.SeriesName;
            }
            set
            {
                DataSeries.SeriesName = value;
                InvokePropertyChanged("DisplayName");
            }
        }

        /// <summary>
        /// series line's thickness
        /// </summary>
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

        /// <summary>
        /// series object's unique id
        /// </summary>
        public string UniqueId { get; private set; }

        /// <summary>
        /// raw data points
        /// </summary>
        public DataItem Points { get; set; }

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
    }
}
