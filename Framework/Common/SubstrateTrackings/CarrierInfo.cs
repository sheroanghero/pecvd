using System;
using System.Runtime.Serialization;
using Aitex.Core.Common;
using Aitex.Core.Util;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.SubstrateTrackings
{
    public enum ProcessJobStateEnum
    {
        pjCREATED = -1,
        pjQUEUED = 0,
        pjSETTING_UP = 1,
        pjWAITING_FOR_START = 2,
        pjPROCESSING = 3,
        pjPROCESS_COMPLETED = 4,
        pjRESERVED5 = 5,
        pjPAUSING = 6,
        pjPAUSED = 7,
        pjSTOPPING = 8,
        pjABORTING = 9,
        pjSTOPPED = 10,
        pjABORTED = 11,
        pjPROCESSJOB_COMPLETED = 12,
    }

    public enum CarrierStatus
    {
        Empty = 0,
        Normal = 1,
    }

    [Serializable]
    [DataContract]
    public class CarrierInfo:NotifiableItem
    {
        public bool IsEmpty
        {
            get { return Status == CarrierStatus.Empty; }
        }

        private CarrierStatus status;
        [DataMember]
        public CarrierStatus Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
                InvokePropertyChanged(nameof(Status));
            }
        }

        [DataMember]
        public Guid InnerId { get; set; }

        [DataMember]
        public string Name { get; set; }

 
        public object ProcessJob;

        [DataMember]
        public string CarrierId { get; set; }

        [DataMember]
        public string Rfid { get; set; }

        [DataMember]
        public string LotId { get; set; }

        [DataMember]
        public string ProductCategory { get; set; }

        [DataMember]
        public WaferInfo[] Wafers { get; set; }

        [DataMember]
        public SerializableDictionary<string, bool> ProcessStatus { get; set; }

        [DataMember]
        public SerializableDictionary<string, string> Attributes { get; set; }

        [DataMember]
        public bool IsStart { get; set; }

        [DataMember]
        public DateTime LoadTime { get; set; }

        [DataMember]
        public string JobSelectedRecipeName { get; set; }

        [DataMember]
        public int Priority { get; set; }

        [DataMember]
        public ModuleName InternalModuleName { get; set; }   //For Internal carrier only

        [DataMember]
        public WaferSize CarrierWaferSize { get; set; }

        [DataMember]
        public bool IsProcessCompleted { get; set; }

        [DataMember]
        public bool IsVertical { get; set; }

        [DataMember]
        public bool HasWaferIn { get; set; }

        [DataMember]
        public int NextSequenceStep { get; set; }

        public object Job { get; set; }
 
        public T GetProcessJob<T>()
        {
            return (T)ProcessJob;
        }

        public bool IsProcessed(string module)
        {
            return ProcessStatus.ContainsKey(module) && ProcessStatus[module];
        }
 
        public void Clear()
        {
            this.Status = CarrierStatus.Empty;
            this.InnerId = Guid.Empty;
            this.ProcessJob = null;
            this.Name = "";
            this.IsStart = false;
            this.CarrierId = "";
            this.Rfid = "";
            this.JobSelectedRecipeName = "";
            this.LotId = "";
            this.ProductCategory = "";
            this.ProcessStatus = new SerializableDictionary<string, bool>();
            this.Attributes = new SerializableDictionary<string, string>();
            this.Priority = 0;
            this.HasWaferIn = false;
            this.IsProcessCompleted = false;
            this.IsVertical = false;
            this.NextSequenceStep = -1;
            this.Job = null;
        }


        public CarrierInfo(int capacity)
        {
            Wafers = new WaferInfo[capacity];
            InnerId = Guid.Empty;
            Status = CarrierStatus.Empty;
            ProcessStatus = new SerializableDictionary<string, bool>();
            Attributes = new SerializableDictionary<string, string>();
        }

        public void CopyInfo(CarrierInfo source)
        {
            this.CarrierId = source.CarrierId;
            this.InnerId = source.InnerId;
            this.Attributes = source.Attributes;
            this.CarrierId = source.CarrierId;
            this.Rfid = source.Rfid;
            this.IsStart = source.IsStart;
            this.JobSelectedRecipeName = source.JobSelectedRecipeName;
            this.LoadTime = source.LoadTime;
            this.LotId = source.LotId;
            this.Name = source.Name;
            this.ProcessStatus = source.ProcessStatus;
            this.Status = source.status;
            this.ProductCategory = source.ProductCategory;
            this.Wafers = source.Wafers;
            this.ProcessJob = source.ProcessJob;
            this.Priority = source.Priority;
            this.CarrierWaferSize = source.CarrierWaferSize;
            this.InternalModuleName = source.InternalModuleName;
            this.HasWaferIn = source.HasWaferIn;
            this.IsProcessCompleted = source.IsProcessCompleted;
            this.IsVertical = source.IsVertical;
            this.NextSequenceStep = source.NextSequenceStep;
            this.Job = source.Job;
        }

    }
}