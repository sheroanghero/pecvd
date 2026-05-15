using System;
using SciChart.Charting.Visuals.Axes.LabelProviders;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.WaferHistory
{
    public class YAxisLabelProvider : LabelProviderBase
    {
        //public string[] LabelList { get; set; }

        private readonly string[] LabelList = { "Unload", "HotN2", "QDR2", "AP7990-2", "QDR1", "AP7990-1", "C/C", "Load" };

        public override string FormatLabel(IComparable dataValue)
        {
            var i = Convert.ToInt32(dataValue);

            int len = LabelList.Length; 

            string result = "";
            if (i >= 0 && i < len)
            {
                result = LabelList[i];
            }
            return result;
        }

        public override string FormatCursorLabel(IComparable dataValue)
        {
            var i = Convert.ToInt32(dataValue);
            string result = "";
            if (i >= 0 && i < LabelList.Length)
            {
                result = LabelList[i];
            }
            else
            {
                result = string.Empty;
            }
            return result;
        }
    }
}
