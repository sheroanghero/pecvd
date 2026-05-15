using Aitex.Core.Util;
using MECF.Framework.UI.Client.CenterViews.Controls;
using MECF.Framework.UI.Client.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Operations.E84
{
    public class E84ViewModel : UiViewModelBase
    {
        public bool IsPermission { get => this.Permission == 3; }

        public E84ViewModel()
        {
            E84InfoDataLp1 = new E84InfoData();
            E84InfoDataLp2 = new E84InfoData();
            E84InfoDataLp3 = new E84InfoData();
            E84InfoDataLp4 = new E84InfoData();
        }

        public E84InfoData E84InfoDataLp1 { get; set; }
        public E84InfoData E84InfoDataLp2 { get; set; }
        public E84InfoData E84InfoDataLp3 { get; set; }
        public E84InfoData E84InfoDataLp4 { get; set; }


        [Subscription("LP1.IsPresent")]
        public bool PodPresentLp1
        {
            get => E84InfoDataLp1.PodPresent;
            set => E84InfoDataLp1.PodPresent = value;
        }

        [Subscription("LP2.IsPresent")]
        public bool PodPresentLp2
        {
            get => E84InfoDataLp2.PodPresent;
            set => E84InfoDataLp2.PodPresent = value;
        }

        [Subscription("LP3.IsPresent")]
        public bool PodPresentLp3
        {
            get => E84InfoDataLp3.PodPresent;
            set => E84InfoDataLp3.PodPresent = value;
        }

        [Subscription("LP4.IsPresent")]
        public bool PodPresentLp4
        {
            get => E84InfoDataLp4.PodPresent;
            set => E84InfoDataLp4.PodPresent = value;
        }


        [Subscription("IsPlaced", "LP1")]
        public bool PodPlacedLp1
        {
            get => E84InfoDataLp1.PodPlaced;
            set => E84InfoDataLp1.PodPlaced = value;
        }

        [Subscription("IsPlaced", "LP2")]
        public bool PodPlacedLp2
        {
            get => E84InfoDataLp2.PodPlaced;
            set => E84InfoDataLp2.PodPlaced = value;
        }

        [Subscription("IsPlaced", "LP3")]
        public bool PodPlacedLp3
        {
            get => E84InfoDataLp3.PodPlaced;
            set => E84InfoDataLp3.PodPlaced = value;
        }

        [Subscription("IsPlaced", "LP4")]
        public bool PodPlacedLp4
        {
            get => E84InfoDataLp4.PodPlaced;
            set => E84InfoDataLp4.PodPlaced = value;
        }


        [Subscription("IsClamped", "LP1")]
        public bool PodLatchedLp1
        {
            get => E84InfoDataLp1.PodLatched;
            set => E84InfoDataLp1.PodLatched = value;
        }

        [Subscription("IsClamped", "LP2")]
        public bool PodLatchedLp2
        {
            get => E84InfoDataLp2.PodLatched;
            set => E84InfoDataLp2.PodLatched = value;
        }

        [Subscription("IsClamped", "LP3")]
        public bool PodLatchedLp3
        {
            get => E84InfoDataLp3.PodLatched;
            set => E84InfoDataLp3.PodLatched = value;
        }

        [Subscription("IsClamped", "LP4")]
        public bool PodLatchedLp4
        {
            get => E84InfoDataLp4.PodLatched;
            set => E84InfoDataLp4.PodLatched = value;
        }


        [Subscription("IsDocked", "LP1")]
        public bool PodDockedLp1
        {
            get => E84InfoDataLp1.PodDocked;
            set => E84InfoDataLp1.PodDocked = value;
        }

        [Subscription("IsDocked", "LP2")]
        public bool PodDockedLp2
        {
            get => E84InfoDataLp2.PodDocked;
            set => E84InfoDataLp2.PodDocked = value;
        }

        [Subscription("IsDocked", "LP3")]
        public bool PodDockedLp3
        {
            get => E84InfoDataLp3.PodDocked;
            set => E84InfoDataLp3.PodDocked = value;
        }

        [Subscription("IsDocked", "LP4")]
        public bool PodDockedLp4
        {
            get => E84InfoDataLp4.PodDocked;
            set => E84InfoDataLp4.PodDocked = value;
        }


        [Subscription("LoadportState", "LP1")]
        public string LoadPortStateLp1
        {
            get => E84InfoDataLp1.LoadPortState;
            set => E84InfoDataLp1.LoadPortState = value;
        }

        [Subscription("LoadportState", "LP2")]
        public string LoadPortStateLp2
        {
            get => E84InfoDataLp2.LoadPortState;
            set => E84InfoDataLp2.LoadPortState = value;
        }

        [Subscription("LoadportState", "LP3")]
        public string LoadPortStateLp3
        {
            get => E84InfoDataLp3.LoadPortState;
            set => E84InfoDataLp3.LoadPortState = value;
        }

        [Subscription("LoadportState", "LP4")]
        public string LoadPortStateLp4
        {
            get => E84InfoDataLp4.LoadPortState;
            set => E84InfoDataLp4.LoadPortState = value;
        }


        [Subscription("TransferState", "LP1")]
        public string PortStateLp1
        {
            get => E84InfoDataLp1.PortState;
            set => E84InfoDataLp1.PortState = value;
        }

        [Subscription("TransferState", "LP2")]
        public string PortStateLp2
        {
            get => E84InfoDataLp2.PortState;
            set => E84InfoDataLp2.PortState = value;
        }

        [Subscription("TransferState", "LP3")]
        public string PortStateLp3
        {
            get => E84InfoDataLp3.PortState;
            set => E84InfoDataLp3.PortState = value;
        }

        [Subscription("TransferState", "LP4")]
        public string PortStateLp4
        {
            get => E84InfoDataLp4.PortState;
            set => E84InfoDataLp4.PortState = value;
        }
        [Subscription("AccessMode", "LP1")]
        public string AccessModeLp1
        {
            get => E84InfoDataLp1.AccessMode;
            set => E84InfoDataLp1.AccessMode = value;
        }

        [Subscription("AccessMode", "LP2")]
        public string AccessModeLp2
        {
            get => E84InfoDataLp2.AccessMode;
            set => E84InfoDataLp2.AccessMode = value;
        }

        [Subscription("AccessMode", "LP3")]
        public string AccessModeLp3
        {
            get => E84InfoDataLp3.AccessMode;
            set => E84InfoDataLp3.AccessMode = value;
        }

        [Subscription("AccessMode", "LP4")]
        public string AccessModeLp4
        {
            get => E84InfoDataLp4.AccessMode;
            set => E84InfoDataLp4.AccessMode = value;
        }

        [Subscription("CarrierIDStatus", "LP1")]
        public string CarrierIDStatusLP1
        {
            get => E84InfoDataLp1.CarrierIDStatus;
            set => E84InfoDataLp1.CarrierIDStatus = value;
        }
        [Subscription("CarrierIDStatus", "LP2")]
        public string CarrierIDStatusLP2
        {
            get => E84InfoDataLp2.CarrierIDStatus;
            set => E84InfoDataLp2.CarrierIDStatus = value;
        }
        [Subscription("CarrierIDStatus", "LP3")]
        public string CarrierIDStatusLP3
        {
            get => E84InfoDataLp3.CarrierIDStatus;
            set => E84InfoDataLp3.CarrierIDStatus = value;
        }
        [Subscription("CarrierIDStatus", "LP4")]
        public string CarrierIDStatusLP4
        {
            get => E84InfoDataLp4.CarrierIDStatus;
            set => E84InfoDataLp4.CarrierIDStatus = value;
        }

        [Subscription("SlotMapStatus", "LP1")]
        public string SlotMapStatusLP1
        {
            get => E84InfoDataLp1.SlotMapStatus;
            set => E84InfoDataLp1.SlotMapStatus = value;
        }
        [Subscription("SlotMapStatus", "LP2")]
        public string SlotMapStatusLP2
        {
            get => E84InfoDataLp2.SlotMapStatus;
            set => E84InfoDataLp2.SlotMapStatus = value;
        }
        [Subscription("SlotMapStatus", "LP3")]
        public string SlotMapStatusLP3
        {
            get => E84InfoDataLp3.SlotMapStatus;
            set => E84InfoDataLp3.SlotMapStatus = value;
        }
        [Subscription("SlotMapStatus", "LP4")]
        public string SlotMapStatusLP4
        {
            get => E84InfoDataLp4.SlotMapStatus;
            set => E84InfoDataLp4.SlotMapStatus = value;
        }
        [Subscription("AccessStatus", "LP1")]
        public string AccessStatusLP1
        {
            get => E84InfoDataLp1.AccessStatus;
            set => E84InfoDataLp1.AccessStatus = value;
        }
        [Subscription("AccessStatus", "LP2")]
        public string AccessStatusLP2
        {
            get => E84InfoDataLp2.AccessStatus;
            set => E84InfoDataLp2.AccessStatus = value;
        }
        [Subscription("AccessStatus", "LP3")]
        public string AccessStatusLP3
        {
            get => E84InfoDataLp3.AccessStatus;
            set => E84InfoDataLp3.AccessStatus = value;
        }
        [Subscription("AccessStatus", "LP4")]
        public string AccessStatusLP4
        {
            get => E84InfoDataLp4.AccessStatus;
            set => E84InfoDataLp4.AccessStatus = value;
        }




        [Subscription("CarrierId", "LP1")]
        public string CarrierIdLp1
        {
            get => E84InfoDataLp1.CarrierID;
            set => E84InfoDataLp1.CarrierID = value;
        }

        [Subscription("CarrierId", "LP2")]
        public string CarrierIdLp2
        {
            get => E84InfoDataLp2.CarrierID;
            set => E84InfoDataLp2.CarrierID = value;
        }

        [Subscription("CarrierId", "LP3")]
        public string CarrierIdLp3
        {
            get => E84InfoDataLp3.CarrierID;
            set => E84InfoDataLp3.CarrierID = value;
        }

        [Subscription("CarrierId", "LP4")]
        public string CarrierIdLp4
        {
            get => E84InfoDataLp4.CarrierID;
            set => E84InfoDataLp4.CarrierID = value;
        }


        [Subscription("SlotMap", "LP1")]
        public string SlotMapLp1
        {
            get => E84InfoDataLp1.SlotMap;
            set => E84InfoDataLp1.SlotMap = value;
        }

        [Subscription("SlotMap", "LP2")]
        public string SlotMapLp2
        {
            get => E84InfoDataLp2.SlotMap;
            set => E84InfoDataLp2.SlotMap = value;
        }

        [Subscription("SlotMap", "LP3")]
        public string SlotMapLp3
        {
            get => E84InfoDataLp3.SlotMap;
            set => E84InfoDataLp3.SlotMap = value;
        }

        [Subscription("SlotMap", "LP4")]
        public string SlotMapLp4
        {
            get => E84InfoDataLp4.SlotMap;
            set => E84InfoDataLp4.SlotMap = value;
        }

        [Subscription("Valid", "LP1")]
        public bool ValidLp1
        {
            get => E84InfoDataLp1.Valid;
            set => E84InfoDataLp1.Valid = value;
        }

        [Subscription("Valid", "LP2")]
        public bool ValidLp2
        {
            get => E84InfoDataLp2.Valid;
            set => E84InfoDataLp2.Valid = value;
        }

        [Subscription("Valid", "LP3")]
        public bool ValidLp3
        {
            get => E84InfoDataLp3.Valid;
            set => E84InfoDataLp3.Valid = value;
        }

        [Subscription("Valid", "LP4")]
        public bool ValidLp4
        {
            get => E84InfoDataLp4.Valid;
            set => E84InfoDataLp4.Valid = value;
        }


        [Subscription("TransferRequest", "LP1")]
        public bool TransferRequestLp1
        {
            get => E84InfoDataLp1.TransferRequest;
            set => E84InfoDataLp1.TransferRequest = value;
        }

        [Subscription("TransferRequest", "LP2")]
        public bool TransferRequestLp2
        {
            get => E84InfoDataLp2.TransferRequest;
            set => E84InfoDataLp2.TransferRequest = value;
        }

        [Subscription("TransferRequest", "LP3")]
        public bool TransferRequestLp3
        {
            get => E84InfoDataLp3.TransferRequest;
            set => E84InfoDataLp3.TransferRequest = value;
        }

        [Subscription("TransferRequest", "LP4")]
        public bool TransferRequestLp4
        {
            get => E84InfoDataLp4.TransferRequest;
            set => E84InfoDataLp4.TransferRequest = value;
        }


        [Subscription("Busy", "LP1")]
        public bool BusyLp1
        {
            get => E84InfoDataLp1.Busy;
            set => E84InfoDataLp1.Busy = value;
        }

        [Subscription("Busy", "LP2")]
        public bool BusyLp2
        {
            get => E84InfoDataLp2.Busy;
            set => E84InfoDataLp2.Busy = value;
        }

        [Subscription("Busy", "LP3")]
        public bool BusyLp3
        {
            get => E84InfoDataLp3.Busy;
            set => E84InfoDataLp3.Busy = value;
        }

        [Subscription("Busy", "LP4")]
        public bool BusyLp4
        {
            get => E84InfoDataLp4.Busy;
            set => E84InfoDataLp4.Busy = value;
        }


        [Subscription("TransferComplete", "LP1")]
        public bool TransferCompleteLp1
        {
            get => E84InfoDataLp1.TransferComplete;
            set => E84InfoDataLp1.TransferComplete = value;
        }

        [Subscription("TransferComplete", "LP2")]
        public bool TransferCompleteLp2
        {
            get => E84InfoDataLp2.TransferComplete;
            set => E84InfoDataLp2.TransferComplete = value;
        }

        [Subscription("TransferComplete", "LP3")]
        public bool TransferCompleteLp3
        {
            get => E84InfoDataLp3.TransferComplete;
            set => E84InfoDataLp3.TransferComplete = value;
        }

        [Subscription("TransferComplete", "LP4")]
        public bool TransferCompleteLp4
        {
            get => E84InfoDataLp4.TransferComplete;
            set => E84InfoDataLp4.TransferComplete = value;
        }


        [Subscription("CS0", "LP1")]
        public bool CS0Lp1
        {
            get => E84InfoDataLp1.CS0;
            set => E84InfoDataLp1.CS0 = value;
        }

        [Subscription("CS0", "LP2")]
        public bool CS0Lp2
        {
            get => E84InfoDataLp2.CS0;
            set => E84InfoDataLp2.CS0 = value;
        }

        [Subscription("CS0", "LP3")]
        public bool CS0Lp3
        {
            get => E84InfoDataLp3.CS0;
            set => E84InfoDataLp3.CS0 = value;
        }

        [Subscription("CS0", "LP4")]
        public bool CS0Lp4
        {
            get => E84InfoDataLp4.CS0;
            set => E84InfoDataLp4.CS0 = value;
        }


        [Subscription("CS1", "LP1")]
        public bool CS1Lp1
        {
            get => E84InfoDataLp1.CS1;
            set => E84InfoDataLp1.CS1 = value;
        }

        [Subscription("CS1", "LP2")]
        public bool CS1Lp2
        {
            get => E84InfoDataLp2.CS1;
            set => E84InfoDataLp2.CS1 = value;
        }

        [Subscription("CS1", "LP3")]
        public bool CS1Lp3
        {
            get => E84InfoDataLp3.CS1;
            set => E84InfoDataLp3.CS1 = value;
        }

        [Subscription("CS1", "LP4")]
        public bool CS1Lp4
        {
            get => E84InfoDataLp4.CS1;
            set => E84InfoDataLp4.CS1 = value;
        }


        [Subscription("CONT", "LP1")]
        public bool ContinuousTransferLp1
        {
            get => E84InfoDataLp1.ContinuousTransfer;
            set => E84InfoDataLp1.ContinuousTransfer = value;
        }

        [Subscription("CONT", "LP2")]
        public bool ContinuousTransferLp2
        {
            get => E84InfoDataLp2.ContinuousTransfer;
            set => E84InfoDataLp2.ContinuousTransfer = value;
        }

        [Subscription("CONT", "LP3")]
        public bool ContinuousTransferLp3
        {
            get => E84InfoDataLp3.ContinuousTransfer;
            set => E84InfoDataLp3.ContinuousTransfer = value;
        }

        [Subscription("CONT", "LP4")]
        public bool ContinuousTransferLp4
        {
            get => E84InfoDataLp4.ContinuousTransfer;
            set => E84InfoDataLp4.ContinuousTransfer = value;
        }


        [Subscription("LoadRequest", "LP1")]
        public bool LoadRequestLp1
        {
            get => E84InfoDataLp1.LoadRequest;
            set => E84InfoDataLp1.LoadRequest = value;
        }

        [Subscription("LoadRequest", "LP2")]
        public bool LoadRequestLp2
        {
            get => E84InfoDataLp2.LoadRequest;
            set => E84InfoDataLp2.LoadRequest = value;
        }

        [Subscription("LoadRequest", "LP3")]
        public bool LoadRequestLp3
        {
            get => E84InfoDataLp3.LoadRequest;
            set => E84InfoDataLp3.LoadRequest = value;
        }

        [Subscription("LoadRequest", "LP4")]
        public bool LoadRequestLp4
        {
            get => E84InfoDataLp4.LoadRequest;
            set => E84InfoDataLp4.LoadRequest = value;
        }


        [Subscription("UnloadRequest", "LP1")]
        public bool UnloadRequestLp1
        {
            get => E84InfoDataLp1.UnloadRequest;
            set => E84InfoDataLp1.UnloadRequest = value;
        }

        [Subscription("UnloadRequest", "LP2")]
        public bool UnloadRequestLp2
        {
            get => E84InfoDataLp2.UnloadRequest;
            set => E84InfoDataLp2.UnloadRequest = value;
        }

        [Subscription("UnloadRequest", "LP3")]
        public bool UnloadRequestLp3
        {
            get => E84InfoDataLp3.UnloadRequest;
            set => E84InfoDataLp3.UnloadRequest = value;
        }

        [Subscription("UnloadRequest", "LP4")]
        public bool UnloadRequestLp4
        {
            get => E84InfoDataLp4.UnloadRequest;
            set => E84InfoDataLp4.UnloadRequest = value;
        }


        [Subscription("ReadyToTransfer", "LP1")]
        public bool ReadyToTransferLp1
        {
            get => E84InfoDataLp1.ReadyToTransfer;
            set => E84InfoDataLp1.ReadyToTransfer = value;
        }

        [Subscription("ReadyToTransfer", "LP2")]
        public bool ReadyToTransferLp2
        {
            get => E84InfoDataLp2.ReadyToTransfer;
            set => E84InfoDataLp2.ReadyToTransfer = value;
        }

        [Subscription("ReadyToTransfer", "LP3")]
        public bool ReadyToTransferLp3
        {
            get => E84InfoDataLp3.ReadyToTransfer;
            set => E84InfoDataLp3.ReadyToTransfer = value;
        }

        [Subscription("ReadyToTransfer", "LP4")]
        public bool ReadyToTransferLp4
        {
            get => E84InfoDataLp4.ReadyToTransfer;
            set => E84InfoDataLp4.ReadyToTransfer = value;
        }


        [Subscription("HandoffAvailable", "LP1")]
        public bool HandoffAvailableLp1
        {
            get => E84InfoDataLp1.HandoffAvailable;
            set => E84InfoDataLp1.HandoffAvailable = value;
        }

        [Subscription("HandoffAvailable", "LP2")]
        public bool HandoffAvailableLp2
        {
            get => E84InfoDataLp2.HandoffAvailable;
            set => E84InfoDataLp2.HandoffAvailable = value;
        }

        [Subscription("HandoffAvailable", "LP3")]
        public bool HandoffAvailableLp3
        {
            get => E84InfoDataLp3.HandoffAvailable;
            set => E84InfoDataLp3.HandoffAvailable = value;
        }

        [Subscription("HandoffAvailable", "LP4")]
        public bool HandoffAvailableLp4
        {
            get => E84InfoDataLp4.HandoffAvailable;
            set => E84InfoDataLp4.HandoffAvailable = value;
        }


        [Subscription("ES", "LP1")]
        public bool EmergencyOkLp1
        {
            get => E84InfoDataLp1.EmergencyOk;
            set => E84InfoDataLp1.EmergencyOk = value;
        }

        [Subscription("ES", "LP2")]
        public bool EmergencyOkLp2
        {
            get => E84InfoDataLp2.EmergencyOk;
            set => E84InfoDataLp2.EmergencyOk = value;
        }

        [Subscription("ES", "LP3")]
        public bool EmergencyOkLp3
        {
            get => E84InfoDataLp3.EmergencyOk;
            set => E84InfoDataLp3.EmergencyOk = value;
        }

        [Subscription("ES", "LP4")]
        public bool EmergencyOkLp4
        {
            get => E84InfoDataLp4.EmergencyOk;
            set => E84InfoDataLp4.EmergencyOk = value;
        }


        [Subscription("E84State", "LP1")]
        public string E84StateLp1
        {
            get => E84InfoDataLp1.E84State;
            set => E84InfoDataLp1.E84State = value;
        }

        [Subscription("E84State", "LP2")]
        public string E84StateLp2
        {
            get => E84InfoDataLp2.E84State;
            set => E84InfoDataLp2.E84State = value;
        }

        [Subscription("E84State", "LP3")]
        public string E84StateLp3
        {
            get => E84InfoDataLp3.E84State;
            set => E84InfoDataLp3.E84State = value;
        }

        [Subscription("E84State", "LP4")]
        public string E84StateLp4
        {
            get => E84InfoDataLp4.E84State;
            set => E84InfoDataLp4.E84State = value;
        }
    }
}