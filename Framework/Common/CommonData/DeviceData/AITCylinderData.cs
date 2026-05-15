using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
    public enum CylinderState
    {
        Close = 0,
        Open = 1,
        Unknown = 2,
        Error = 3,
    }

    [DataContract]
    [Serializable]
    public class AITCylinderData : NotifiableItem, IDeviceData
    {
 
        [DataMember]
        public string Module { get; set; }
 
        [DataMember]
        public string DeviceName { get; set; }
 
        [DataMember]
        public string DisplayName { get; set; }

 
        [DataMember]
        public string DeviceSchematicId { get; set; }

        [DataMember]
        public  bool OpenFeedback { get; set; }

        [DataMember]
        public bool CloseFeedback { get; set; }

        [DataMember]
        public bool OpenSetPoint { get; set; }

        [DataMember]
        public bool CloseSetPoint { get; set; }


        [DataMember]
        public bool IsLoop { get; set; }

        public string StringStatus
        {
            get
            {
                if (OpenFeedback && !CloseFeedback) return CylinderState.Open.ToString();
                if (!OpenFeedback && CloseFeedback) return CylinderState.Close.ToString();
                if (OpenFeedback && CloseFeedback) return CylinderState.Error.ToString();
                if (!OpenFeedback && !CloseFeedback) return CylinderState.Unknown.ToString();
                return "Unknown";
            }
        }

        public string StringSetPoint
        {
            get
            {
                if (OpenSetPoint && !CloseSetPoint) return CylinderState.Open.ToString();
                if (!OpenSetPoint && CloseSetPoint) return CylinderState.Close.ToString();
                if (OpenSetPoint && CloseSetPoint) return CylinderState.Error.ToString();
                if (!OpenSetPoint && !CloseSetPoint) return CylinderState.Unknown.ToString();
                return "Unknown";
            }
        }


        public AITCylinderData()
        {
            DisplayName = "Undefined Cylinder";
        }

        public void Update(IDeviceData data)
        {

        }
    }

    public class AITCylinderOperation
    {
        public const string Open = "Open";
        public const string Close = "Close";
 

    }

    public class AITCylinderProperty
    {
        public const string OpenFeedback = "OpenFeedback";
        public const string CloseFeedback = "CloseFeedback";
 
        public const string OpenSetPoint = "OpenSetPoint";
        public const string CloseSetPoint = "CloseSetPoint";
 

 

    }
}