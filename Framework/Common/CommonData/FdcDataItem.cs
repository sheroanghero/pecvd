using System;
using System.Runtime.Serialization;

namespace MECF.Framework.Common.CommonData
{
    [DataContract]
    [Serializable]
    public class FdcDataItem
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public float MaxValue { get; set; }

        [DataMember]
        public float MinValue { get; set; }

        [DataMember]
        public float MeanValue
        {
            get
            {
                if (SampleCount == 0)
                    return 0f;
                return Total / SampleCount;
            }
        }

        [DataMember]
        public float StdValue
        {
            get
            {
                if (SampleCount <= 1)
                    return 0f;
                return (float)Math.Sqrt(Math.Abs((SqrtTotal - 2 * (Total / SampleCount) * Total + SampleCount * (Total / SampleCount) * (Total / SampleCount)) / (SampleCount - 1)));
            }
        }

        [DataMember]
        public float SetPoint { get; set; }

        [DataMember]
        public float Total { get; set; }

        [DataMember]
        public float SqrtTotal { get; set; }

        [DataMember]
        public int SampleCount { get; set; }

        public void Clear()
        {
            MaxValue = float.MinValue;
            MinValue = float.MaxValue;
            SetPoint = 0f;
            Total = 0f;
            SampleCount = 0;
            SqrtTotal = 0f;

        }

        public void Update(float value)
        {
            if (value < MinValue)
                MinValue = value;

            if (value > MaxValue)
                MaxValue = value;

            Total += value;

            SqrtTotal += value * value;

            SampleCount++;
        }
    }
}
