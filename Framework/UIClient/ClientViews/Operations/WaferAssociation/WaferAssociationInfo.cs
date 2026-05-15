using Caliburn.Micro.Core;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Operations.WaferAssociation
{
    public class WaferAssociationInfo : PropertyChangedBase
    {
        private int _slotFrom = 1;
        public int SlotFrom
        {
            get { return _slotFrom; }
            set { _slotFrom = value; NotifyOfPropertyChange("SlotFrom"); }
        }

        private int _slotTo = 25;
        public int SlotTo
        {
            get { return _slotTo; }
            set { _slotTo = value; NotifyOfPropertyChange("SlotTo"); }
        }

        private string _sequenceName = string.Empty;
        public string SequenceName
        {
            get { return _sequenceName; }
            set { _sequenceName = value; NotifyOfPropertyChange("SequenceName"); }
        }

        private string _lotId = string.Empty;
        public string LotId
        {
            get { return _lotId; }
            set { _lotId = value; NotifyOfPropertyChange("LotId"); }
        }

        private bool _LotIdSaved = true;
        public bool LotIdSaved
        {
            get { return _LotIdSaved; }
            set { _LotIdSaved = value; NotifyOfPropertyChange("LotIdSaved"); }
        }

        private string _JobID = string.Empty;
        public string JobID
        {
            get { return _JobID; }
            set { _JobID = value; NotifyOfPropertyChange("JobID"); }
        }



        

        private ModuleInfo _ModuleData;
        public ModuleInfo ModuleData
        {
            get { return _ModuleData; }
            set { _ModuleData = value; NotifyOfPropertyChange("ModuleData"); }
        }

        private string _JobStatus = string.Empty;
        public string JobStatus
        {
            get { return _JobStatus; }
            set { _JobStatus = value;
                NotifyOfPropertyChange("JobStatus");
                NotifyOfPropertyChange("IsEnableSelect");
                NotifyOfPropertyChange("IsEnableCreate");
                NotifyOfPropertyChange("IsEnableAbort");
                NotifyOfPropertyChange("IsEnableStart");
                NotifyOfPropertyChange("IsEnablePause");
                NotifyOfPropertyChange("IsEnableResume");
                NotifyOfPropertyChange("IsEnableStop");
            }
        }

        public bool IsEnableSelect
        {
            get { return !IsCreated; }
        }

        public bool IsEnableCreate
        {
            get { return !IsCreated; }
        }
        public bool IsEnableAbort
        {
            get { return IsCreated; }
        }
        public bool IsEnableStart
        {
            get { return IsCreated && IsWaitingForStart; }
        }
        public bool IsEnablePause
        {
            get { return IsExecuting; }
        }
        public bool IsEnableResume
        {
            get { return IsPaused; }
        }
        public bool IsEnableStop
        {
            get { return IsExecuting || IsPaused; }
        }

        private bool IsCreated
        {
            get { return !string.IsNullOrEmpty(JobStatus); }
        }
        private bool IsExecuting
        {
            get { return JobStatus== "Executing"; }
        }
        private bool IsWaitingForStart
        {
            get { return JobStatus == "WaitingForStart"; }
        }
        private bool IsPaused
        {
            get { return JobStatus == "Paused"; }
        }
        private bool IsCompleted
        {
            get { return JobStatus == "Completed"; }
        }

        //Queued,Selected,WaitingForStart,Executing,Paused,Completed,

    }
}
