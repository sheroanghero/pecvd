using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DocumentFormat.OpenXml.Drawing;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.Jobs;

namespace Aitex.Core.Common
{
    public enum WaferSize
    {
        WS0,    //undefine, initialize status
        WS3,
        WS4,
        WS6,
		WS8,
		WS12,
		WS2,
		WS5,
		WS7,
	}

    public enum WaferStatus
	{
		Empty = 0,
		Normal = 1,
		Crossed = 2,
		Double = 3,
		Unknown = 4,
		Dummy = 5
	}

	public enum EnumE90Status
	{
		None = -1,
		NeedProcessing = 0,
		InProcess,
		Processed,
		Aborted,
		Stopped,
		Rejected,
		Lost,
		Skipped,
	}
	//public enum EnumWaferProcessStatus
	//{

	//	NeedProcessing=0,
	//	Processed,
	//	Aborted,
	//	Stopped,
	//	Rejected,
	//	Lost,
	//	Skipped,



	//}
	public enum SubstrateTransportStatus
    {
		AtSource =0,
		AtWork,
		AtDestination,
		None,
    }

    public enum EnumWaferProcessStatus
    {
        Idle = 0,
        InProcess,
        Completed,
        Failed,
        Wait,
		Abort,
    }
    public enum ProcessStatus
    {
        Idle = 0,
        Wait = 1,
        Busy = 2,
        Completed = 3,
        Failed = 4,
		Abort = 5,

	}

	public enum SubstAccessType
    {
		Create=0,
		UpdateWaferID,
		Arrive,
		Left,
		UpdateLM1,
		UpdateLM2,
		Delete,
		Alignment,
	}
	public enum WaferType
	{
		None,
		P,//Production wafer  Processed so that a thin film forms on the surface, and becomes product.
		M,//Monitor wafer  Used for film thickness and particle inspection
		SD,//Side dummy wafer  Used to rectify process gases for more uniform wafer processing production.
		ED,//Extra dummy wafer  Used as a supplement when there is a shortage of production wafers.
		XD,//Expert dummy wafer  Dummy wafer used in expert mode to fill empty slots during test wafer processing.
		T,//Test wafer  Used in expert mode for process verification, performance verification, and pilot processes
	}

	[Serializable]
	[DataContract]
	public class SubstHistory
	{
		[DataMember]
		public string locationID { get; set; }
		[DataMember]
		public int SlotNO { get; set; }

		[DataMember]
		public DateTime AccessTimeIn { get; set; }

		[DataMember]
		public DateTime AccessTimeOut { get; set; }
		[DataMember]
		public SubstAccessType AccessType { get; set; }

		public SubstHistory(string loc,int slot,DateTime dt, SubstAccessType type)
        {
			locationID = loc;
			SlotNO = slot;
			AccessTimeIn = dt;
			AccessType = type;
		}
	}

	[Serializable]
    [DataContract]
	public class WaferInfo : NotifiableItem
	{		
		private string procesjobid;
		[DataMember]
		public string ProcessJobID
		{
			get
			{
				return procesjobid;
			}
			set
			{
				procesjobid = value;
				InvokePropertyChanged("ProcessJobID");
			}
		}
		private string controljobid;
		[DataMember]
		public string ControlJobID
		{
			get
			{
				return controljobid;
			}
			set
			{
				controljobid = value;
				InvokePropertyChanged("ControlJobID");
			}
		}




		private SubstrateTransportStatus substtransstatus;
		[DataMember]
		public SubstrateTransportStatus SubstTransStatus
		{
			get
			{
				return substtransstatus;
			}
			set
			{
				substtransstatus = value;
				InvokePropertyChanged("SubstTransStatus");
			}
		}

		private EnumE90Status Subste90status;
		[DataMember]
		public EnumE90Status SubstE90Status
		{
			get
			{
				return Subste90status;
			}
			set
			{
				Subste90status = value;
				InvokePropertyChanged("SubstE90Status");
			}
		}

		private SubstHistory[] substhists;
		[DataMember]
		public SubstHistory[] SubstHists
		{
			get
			{
				return substhists;
			}
			set
			{
				substhists = value;
				InvokePropertyChanged("SubstHists");
			}
		}


		public bool IsEmpty
		{
			get { return Status == WaferStatus.Empty; }
		}

		private string waferID;
		[DataMember]
		public string WaferID
		{
			get
			{
				return waferID;
			}
			set
			{
				waferID = value;
			    InvokePropertyChanged("WaferID");
			}
		}

		private string _waferOrigin;
		[DataMember]
		public string WaferOrigin
		{
			get
			{
				return _waferOrigin;
			}
			set
			{
				_waferOrigin = value;
			    InvokePropertyChanged("WaferOrigin");
			}
		}

		private string hostlaserMark1;
		[DataMember]
		public string HostLaserMark1
		{
			get
			{
				return hostlaserMark1;
			}
			set
			{
				hostlaserMark1 = value;
				InvokePropertyChanged("HostLaserMark1");
			}
		}

		private string hostlaserMark2;
		[DataMember]
		public string HostLaserMark2
		{
			get
			{
				return hostlaserMark2;
			}
			set
			{
				hostlaserMark2 = value;
				InvokePropertyChanged("HostLaserMark2");
			}
		}



		private string laserMarker;
		[DataMember]
		public string LaserMarker
		{
			get
			{
				return laserMarker;
			}
			set
			{
				laserMarker = value;
			    InvokePropertyChanged("LaserMarker");
			}
		}

		private string t7Code;
		[DataMember]
		public string T7Code
		{
			get
			{
				return t7Code;
			}
			set
			{
				t7Code = value;
			    InvokePropertyChanged("T7Code");
			}
		}

        private string _laserMarkerScore;
        [DataMember]
        public string LaserMarkerScore
        {
            get
            {
                return _laserMarkerScore;
            }
            set
            {
                _laserMarkerScore = value;
                InvokePropertyChanged("LaserMarker1Score");
            }
        }


        private string _t7CodeScore;
        [DataMember]
        public string T7CodeScore
        {
            get
            {
                return _t7CodeScore;
            }
            set
            {
                _t7CodeScore = value;
                InvokePropertyChanged("T7CodeScore");
            }
        }
        
        private string _imageFileName;
        [DataMember]
        public string ImageFileName
        {
	        get
	        {
		        return _imageFileName;
	        }
	        set
	        {
		        _imageFileName = value;
		        InvokePropertyChanged("ImageFileName");
	        }
        }
        
        private string _imageFilePath;
        [DataMember]
        public string ImageFilePath
        {
	        get
	        {
		        return _imageFilePath;
	        }
	        set
	        {
		        _imageFilePath = value;
		        InvokePropertyChanged("ImageFilePath");
	        }
        }


		[DataMember]
	    public string LotId { get; set; }


	    [DataMember]
	    public string TransFlag { get; set; }

        private WaferStatus status;
		[DataMember]
		public WaferStatus Status
		{
			get
			{
				return status;
			}
			set
			{
				status = value;
			    InvokePropertyChanged("Status");
			}
		}
		[DataMember]
		public string CurrentCarrierID
		{
			get;
			set;
		}

		[DataMember]
		public int Station
		{
			get;
			set;
		}

		[DataMember]
		public int Slot
		{
			get;
			set;
		}

		[DataMember]
		public int OriginStation
		{
			get;
			set;
		}

		[DataMember]
		public int OriginSlot
		{
			get;
			set;
		}
        [DataMember]
        public string OriginCarrierID
        {
            get;
            set;
        }

		[DataMember]
		public int DestinationStation
		{
			get;
			set;
		}
		[DataMember]
		public string DestinationCarrierID
		{
			get;
			set;
		}

		[DataMember]
		public int DestinationSlot
		{
			get;
			set;
		}

		[DataMember]
		public int NextStation
		{
			get;
			set;
		}
	

		[DataMember]
		public int NextStationSlot
		{
			get;
			set;
		}

		[DataMember]
		public int Notch
		{
			get;
			set;
		}

        [DataMember]
        public WaferSize Size
        {
            get;
            set;
        }
		[DataMember]
		public float UseCount
		{
			get;
			set;
		}
		[DataMember]
		public float UseTime
		{
			get;
			set;
		}
		[DataMember]
		public float UseThick
		{
			get;
			set;
		}

		private bool isChecked;
		public bool IsChecked
		{
			get
			{ return isChecked; }
			set
			{
				isChecked = value;
				InvokePropertyChanged("IsChecked");
			}
		}


		[DataMember]
		public string PPID     //Recipe or Sequence
		{
			get;
			set;
		}


		private EnumWaferProcessStatus processState;
		[DataMember]
		public EnumWaferProcessStatus ProcessState
		{
			get
			{
				return processState;
			}
			set
			{
				processState = value;
			    InvokePropertyChanged("ProcessStatus");
			}
		}

		private bool isSource;
	    [DataMember]
        public bool IsSource
		{
			get
			{
				return isSource;
			}
			set
			{
				isSource = value;
			    InvokePropertyChanged("IsSource");
			}
		}

		private bool isDestination;
	    [DataMember]
        public bool IsDestination
		{
			get
			{
				return isDestination;
			}
			set
			{
				isDestination = value;
				InvokePropertyChanged("IsDestination");
			}
		}

	    [DataMember]
        public Guid InnerId { get; set; }

	    [DataMember]
        public ProcessJobInfo ProcessJob { get; set; }

	    [DataMember]
        public int NextSequenceStep { get; set; }

        [DataMember]
        public bool HasWarning { get; set; }
		[DataMember]
		public WaferType WaferType { get; set; }

		public WaferInfo()
		{
			InnerId = Guid.Empty;
		}

		public WaferInfo(string waferID, WaferStatus status = WaferStatus.Empty) : this()
		{
			this.WaferID = waferID;
			this.Status = status;
		}
 
		public void Update(WaferInfo source)
		{
		    InnerId = source.InnerId;
			
			WaferID = source.waferID;
			WaferOrigin = source.WaferOrigin;
			LaserMarker = source.LaserMarker;
			LaserMarkerScore = source.LaserMarkerScore;
			T7Code = source.T7Code;
			T7CodeScore = source.T7CodeScore;
			Status = source.Status;
			ProcessState = source.ProcessState;
			IsSource = source.IsSource;
			IsDestination = source.IsDestination;

			Station = source.Station;
			Slot = source.Slot;
			OriginStation = source.OriginStation;
			OriginSlot = source.OriginSlot;
            if(source.OriginCarrierID!=null) OriginCarrierID = source.OriginCarrierID;
			if (source.DestinationCarrierID != null) DestinationCarrierID = source.DestinationCarrierID;
			


			DestinationStation = source.DestinationStation;
			DestinationSlot = source.DestinationSlot;

			NextStation = source.NextStation;
			NextStationSlot = source.NextStationSlot;

			Notch = source.Notch;
            Size  = source.Size;

		    TransFlag = source.TransFlag;
		    LotId = source.LotId;

		    ProcessJob = source.ProcessJob;
		    NextSequenceStep = source.NextSequenceStep;
			SubstHists = source.SubstHists;

            HasWarning = source.HasWarning;
			SubstE90Status = source.SubstE90Status;

			PPID = source.PPID;

			HostLaserMark1 = source.HostLaserMark1;
			HostLaserMark2 = source.HostLaserMark2;

		}
        public WaferInfo Clone()
        {
            WaferInfo source = this;

			return new WaferInfo()
            {
                InnerId = source.InnerId,

                WaferID = source.waferID,
                WaferOrigin = source.WaferOrigin,
                LaserMarker = source.LaserMarker,
                LaserMarkerScore = source.LaserMarkerScore,
                T7Code = source.T7Code,
                T7CodeScore = source.T7CodeScore,
                Status = source.Status,
                ProcessState = source.ProcessState,
                IsSource = source.IsSource,
                IsDestination = source.IsDestination,

                Station = source.Station,
                Slot = source.Slot,
                OriginStation = source.OriginStation,
                OriginSlot = source.OriginSlot,
                OriginCarrierID = source.OriginCarrierID,
                DestinationCarrierID = source.DestinationCarrierID,

                DestinationStation = source.DestinationStation,
                DestinationSlot = source.DestinationSlot,

                NextStation = source.NextStation,
                NextStationSlot = source.NextStationSlot,

                Notch = source.Notch,
                Size = source.Size,

                TransFlag = source.TransFlag,
                LotId = source.LotId,

                ProcessJob = source.ProcessJob,
                NextSequenceStep = source.NextSequenceStep,
                SubstHists = source.SubstHists,

                HasWarning = source.HasWarning,
                SubstE90Status = source.SubstE90Status,

                PPID = source.PPID,
				UseCount = source.UseCount,
				UseTime = source.UseTime,
				UseThick = source.UseThick,
			};


        }
		public void SetEmpty()
	    {
            this.InnerId = Guid.Empty;
	        
	        this.WaferID = string.Empty;

	        this.WaferOrigin = string.Empty;

	        this.LaserMarker = string.Empty;
	        this.LaserMarkerScore = string.Empty;
	        this.T7Code = string.Empty;
	        this.T7CodeScore = string.Empty;

            this.Status = (int)WaferStatus.Empty;
 
	        this.ProcessState = EnumWaferProcessStatus.Idle;

	        this.IsSource = false;

	        this.IsDestination = false;
            
	        this.Station = 0;
	        this.Slot = 0;

	        this.OriginStation = 0;

	        this.OriginSlot = 0;

	        //this.SourceStation = 0;

	        //this.SourceSlot = 0;

	        this.DestinationStation = 0;

	        this.DestinationSlot = 0;

	        this.Notch = 0;

			//保留最后一次的Wafer Size信息，便于后续判断和优化
            //this.Size = WaferSize.WS0;

	        this.TransFlag = string.Empty;

	        this.LotId = string.Empty;

	        this.ProcessJob = null;

	        this.NextSequenceStep = 0;

            this.HasWarning = false;

			this.PPID = "";
			this.UseCount = 0;

			this.UseTime = 0;

			this.UseThick = 0;
		}
	}


}
