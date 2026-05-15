using System;
using System.Runtime.Serialization;

namespace Aitex.Core.Common.DeviceData
{
    [DataContract]
    [Serializable]
    public class AITChillerData : AITDeviceData
    {

        [DataMember]
        public bool IsRunning { get; set; }

        [DataMember]
        public bool IsError { get; set; }


        public AITChillerData()
        {
            DisplayName = "Undefined";
        }


    }

    public enum AITChillerOperation
    {
        ChillerOn,
        ChillerOff,
    }

}
