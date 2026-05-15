using Aitex.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.CommonData.SorterDefines
{
    [Serializable]
    [DataContract]
    public class CarrierConfigurationItem
    {
        [DataMember]
        public int Index { get; set; }
        [DataMember]
        public string CarrierName { get; set; }
        [DataMember]
        public int CarrierWaferSize { get; set; }
        [DataMember]
        public int CarrierSlotsNumber { get; set; }

        [DataMember]
        public bool IsInfoPadAOn { get; set; }
        [DataMember]
        public bool IsInfoPadBOn { get; set; }
        [DataMember]
        public bool IsInfoPadCOn { get; set; }
        [DataMember]
        public bool IsInfoPadDOn { get; set; }
        [DataMember]
        public string LP1StationName { get; set; }
        [DataMember]
        public string LP2StationName { get; set; }
        [DataMember]
        public string LP3StationName { get; set; }
        [DataMember]
        public string LP4StationName { get; set; }
        [DataMember]
        public string LP5StationName { get; set; }
        [DataMember]
        public string LP6StationName { get; set; }
        [DataMember]
        public string LP7StationName { get; set; }
        [DataMember]
        public string LP8StationName { get; set; }

        [DataMember]
        public string GetOffset { get; set; }
        [DataMember]
        public string PutOffset { get; set; }

        [DataMember]
        public int CIDReaderIndex { get; set; }
        [DataMember]
        public bool CarrierFosbMode { get; set; }
        
        [DataMember]
        public bool NeedCheckIronDoorCarrier { get; set; }
        [DataMember]
        public bool KeepClampedAfterUnloadCarrier { get; set; }
        [DataMember]
        public bool DisableEvenSlot { get; set; }
        [DataMember]
        public bool DisableOddSlot { get; set; }
        

        [DataMember]
        public int ThicknessLowLimit { get; set; }
        [DataMember]
        public int ThicknessHighLimit { get; set; }

        [DataMember]
        public int SlotPositionBaseLine { get; set; }

        [DataMember]
        public int SlotPitch { get; set; }

        [DataMember]
        public int WaferCenterDeviationLimit { get; set; }


        [DataMember]
        public bool EnableDualTransfer { get; set; }
        [DataMember]
        public bool ForbidAccessAboveWafer { get; set; }
        [DataMember]
        public bool MappedByRobot { get; set; }
        [DataMember]
        public bool EnableCarrier { get; set; }

        [DataMember]
        public int LPRecipeNumber { get; set; }

        [DataMember]
        public string AccessPermitToCarrierIndex { get; set; }

        [DataMember]
        public int[] Int_AccessPermitToCarrierIndex { get; set; }

    }
}
