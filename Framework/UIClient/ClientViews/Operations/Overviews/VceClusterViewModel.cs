using Caliburn.Micro;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.Common.RecipeCenter;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;
using MECF.Framework.UI.Client.CenterViews.Operations.WaferAssociation;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aitex.Core.Util;
using MECF.Framework.Common.CommonData;
using MECF.Framework.UI.Client.ClientControls.RobotControls;

namespace MECF.Framework.UI.Client.ClientViews.Operations.Overviews
{
    public class VceClusterViewModel : UiViewModelBase
    {
        public WaferInfo PM1WaferData { get; set; }
        public WaferInfo PM2WaferData { get; set; }
        public WaferInfo PM3WaferData { get; set; }
        public WaferInfo PM4WaferData { get; set; }
        public WaferInfo PM5WaferData { get; set; }

        public WaferInfo AlignerAWaferData { get; set; }
        public WaferInfo AlignerBWaferData { get; set; }

        public WaferInfo TMRobotLowWaferData { get; set; }
        public WaferInfo TMRobotUpWaferData { get; set; }

        [Subscription("VCEA.IsFoupOn")]
        public bool IsVCEAFoupOn { get; set; }

        [Subscription("VCEA.CarrierId")]
        public string LP1CarrierId { get; set; }

        [Subscription("VCEA.LocalJobName")]
        public string LP1JobName { get; set; }

        [Subscription("VCEA.LocalJobStatus")]
        public string LP1JobStatus { get; set; }


        [Subscription("VCEB.IsFoupOn")]
        public bool IsVCEBFoupOn { get; set; }

        [Subscription("VCEB.CarrierId")]
        public string LP2CarrierId { get; set; }

        [Subscription("VCEB.LocalJobName")]
        public string LP2JobName { get; set; }

        [Subscription("VCEB.LocalJobStatus")]
        public string LP2JobStatus { get; set; }

        [Subscription("TMRobot.RobotMoveInfo")]
        public RobotMoveInfo TMRobotMoveInfo { get; set; }

        public VTMRobotPosition StationPosition { get; set; }

        public ModuleInfo VceAModuleInfo { get; set; }
        public ModuleInfo VceBModuleInfo { get; set; }

        public ModuleInfo AlignerAModuleInfo { get; set; }
        public ModuleInfo AlignerBModuleInfo { get; set; }

        public ModuleInfo TMRobotModuleInfo { get; set; }

        public ModuleInfo PM1ModuleInfo { get; set; }
        public ModuleInfo PM2ModuleInfo { get; set; }
        public ModuleInfo PM3ModuleInfo { get; set; }
        public ModuleInfo PM4ModuleInfo { get; set; }
        public ModuleInfo PM5ModuleInfo { get; set; }

        private WaferAssociationInfo _VCEAWaferAssociation;
        public WaferAssociationInfo VCEAWaferAssociation
        {
            get { return _VCEAWaferAssociation; }
            set { _VCEAWaferAssociation = value; }
        }

        private WaferAssociationInfo _VCEBWaferAssociation;
        public WaferAssociationInfo VCEBWaferAssociation
        {
            get { return _VCEBWaferAssociation; }
            set { _VCEBWaferAssociation = value; }
        }

        public VceClusterViewModel()
        {
            DisplayName = "VceClusterViewModel";

        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            VceAModuleInfo = ModuleManager.ModuleInfos[ModuleName.VCEA.ToString()];
            VceBModuleInfo = ModuleManager.ModuleInfos[ModuleName.VCEB.ToString()];

            AlignerAModuleInfo = ModuleManager.ModuleInfos[ModuleName.AlignerA.ToString()];
            AlignerBModuleInfo = ModuleManager.ModuleInfos[ModuleName.AlignerB.ToString()];

            TMRobotModuleInfo = ModuleManager.ModuleInfos[ModuleName.TMRobot.ToString()];

            PM1ModuleInfo = ModuleManager.ModuleInfos[ModuleName.PM1.ToString()];
            PM2ModuleInfo = ModuleManager.ModuleInfos[ModuleName.PM2.ToString()];
            PM3ModuleInfo = ModuleManager.ModuleInfos[ModuleName.PM3.ToString()];
            PM4ModuleInfo = ModuleManager.ModuleInfos[ModuleName.PM4.ToString()];
            PM5ModuleInfo = ModuleManager.ModuleInfos[ModuleName.PM5.ToString()];

            StationPosition = new VTMRobotPosition()
            {
                Rotations = new Dictionary<string, int>()
                {
                    { "ArmA.System", 0},

                    { "ArmA.PM1", 90},
                    { "ArmA.PM2", 135},
                    { "ArmA.PM3", 180},
                    { "ArmA.PM4", 225},
                    { "ArmA.PM5", 270},
                    { "ArmA.PA1", 50},
                    { "ArmA.PA2", 305},
                    { "ArmA.VCE1", 128},
                    { "ArmA.VCEA", 27},
                    { "ArmA.AlignerA", 27},
                    { "ArmA.VCE2", 328},
                    { "ArmA.VCEB", 328},
                    { "ArmA.AlignerB", 328},

                    { "ArmB.PM1",-90},
                    { "ArmB.PM2",-45},
                    { "ArmB.PM3",0},
                    { "ArmB.PM4", 45},
                    { "ArmB.PM5", 90},
                    { "ArmB.PA1", -153},
                    { "ArmB.PA2", 148},
                    { "ArmB.VCE1",-150},
                    { "ArmB.VCEA", -150},
                    { "ArmB.AlignerA",-150},
                    { "ArmB.VCE2", 153},
                    { "ArmB.VCEB", 153},
                    { "ArmB.AlignerB", 153},
                },
                Home = new int[8] { 180, 165, -77, 0, -165, 194, 77, 165 },
                Arm1Extend = new int[8] { 100, 180, -12, 80, -180, 340, 12, 20 },
                Arm2Extend = new int[8] { 255, 26, -8, -76, -30, 180, 20, 180 },
                Arm3Extend = new int[8] { 140, 180, -47, 40, -180, 265, 47, 95 },
                Arm4Extend = new int[8] { 255, 26, -8, -76, -30, 180, 20, 180 },
            };

        }

        protected override void OnActivate()
        {
            base.OnActivate();

        }

        protected override void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {
            if (VCEAWaferAssociation == null)
            {
                VCEAWaferAssociation = new WaferAssociationInfo();
                VCEAWaferAssociation.ModuleData = ModuleManager.ModuleInfos["VCEA"];
            }

            if (VCEBWaferAssociation == null)
            {
                VCEBWaferAssociation = new WaferAssociationInfo();
                VCEBWaferAssociation.ModuleData = ModuleManager.ModuleInfos["VCEB"];
            }

            //VCEAWaferAssociation.JobID = LP1JobName;
            //VCEAWaferAssociation.JobStatus = LP1JobStatus;

            //VCEBWaferAssociation.JobID = LP2JobName;
            //VCEBWaferAssociation.JobStatus = LP2JobStatus;

            //PM1WaferData = PM1ModuleInfo.WaferManager.Wafers[0];
            //PM2WaferData = PM2ModuleInfo.WaferManager.Wafers[0];
            //PM3WaferData = PM3ModuleInfo.WaferManager.Wafers[0];
            //PM4WaferData = PM4ModuleInfo.WaferManager.Wafers[0];
            //PM5WaferData = PM5ModuleInfo.WaferManager.Wafers[0];
            //AlignerAWaferData = AlignerAModuleInfo.WaferManager.Wafers[0];
            //AlignerBWaferData = AlignerBModuleInfo.WaferManager.Wafers[0];
            //TMRobotLowWaferData = TMRobotModuleInfo.WaferManager.Wafers[0];
            //TMRobotUpWaferData = TMRobotModuleInfo.WaferManager.Wafers[1];

        }


        public void Preset()
        {

        }

        public void Clear()
        {

        }

        public void Apply()
        {

        }

        public void HomeAll()
        {
            InvokeClient.Instance.Service.DoOperation("System.HomeAll");
        }

        public void Abort()
        {
            InvokeClient.Instance.Service.DoOperation("System.Abort");
        }

        public void ReturnWafer()
        {
            InvokeClient.Instance.Service.DoOperation("System.ReturnAllWafer");
        }

        public void Auto()
        {
            InvokeClient.Instance.Service.DoOperation("System.SetAutoMode");
        }

        public void Manual()
        {
            InvokeClient.Instance.Service.DoOperation("System.SetManualMode");
        }

        public void Load(string loadPortName)
        {
            InvokeClient.Instance.Service.DoOperation($"{loadPortName}.Load");
        }

        public void Unload(string loadPortName)
        {
            InvokeClient.Instance.Service.DoOperation($"{loadPortName}.Unload");
        }


        #region Wafer association
        #region Sequence operation
        public void SelectSequence(WaferAssociationInfo info)
        {
            SequenceDialogViewModel dialog = new SequenceDialogViewModel();
            dialog.DisplayName = "Select Sequence";

            dialog.Files = new ObservableCollection<FileNode>(RecipeSequenceTreeBuilder.GetFiles("",
                RecipeClient.Instance.Service.GetSequenceNameList()
            ));

            WindowManager wm = new WindowManager();
            bool? bret = wm.ShowDialog(dialog);
            if ((bool)bret)
            {
                info.SequenceName = dialog.DialogResult;
            }
        }

        public void SetSlot(WaferAssociationInfo info)
        {
            if (InputSlotCheck(info.SlotFrom, info.SlotTo))
                AssociateSequence(info, true);
        }

        public void SkipSlot(WaferAssociationInfo info)
        {
            if (InputSlotCheck(info.SlotFrom, info.SlotTo))
                AssociateSequence(info, false);
        }

        public void SetAll(WaferAssociationInfo info)
        {
            info.SlotFrom = 1;
            info.SlotTo = 25;
            AssociateSequence(info, true);
        }

        public void DeselectAll(WaferAssociationInfo info)
        {
            info.SlotFrom = 1;
            info.SlotTo = 25;
            AssociateSequence(info, false);
        }

        public void SetSequence(WaferAssociationInfo info, int slotIndex, string seqName)
        {
            bool flag = string.IsNullOrEmpty(seqName);
            AssociateSequence(info, flag, slotIndex - 1);
        }

        private bool InputSlotCheck(int from, int to)
        {
            if (from > to)
            {
                DialogBox.ShowInfo("This index of from slot should be large than the index of to slot.");
                return false;
            }
            if (from < 1 || to > 25)
            {
                DialogBox.ShowInfo("This input value for from should be between 1 and 25.");
                return false;
            }
            return true;
        }

        private void AssociateSequence(WaferAssociationInfo info, bool flag, int slot = -1)
        {
            ObservableCollection<WaferInfo> wafers = info.ModuleData.WaferManager.Wafers;
            if (slot >= 0) //by wafer
            {
                int index = wafers.Count - slot - 1;
                if (index < wafers.Count)
                {
                    if (flag && HasWaferOnSlot(wafers.ToList(), index))
                        wafers[index].SequenceName = info.SequenceName;
                    else
                        wafers[index].SequenceName = string.Empty;
                }
            }
            else //by from-to
            {
                for (int i = info.SlotFrom - 1; i < info.SlotTo; i++)
                {
                    int index = wafers.Count - i - 1;
                    if (index < wafers.Count)
                    {
                        if (flag && HasWaferOnSlot(wafers.ToList(), index))
                            wafers[index].SequenceName = info.SequenceName;
                        else
                            wafers[index].SequenceName = string.Empty;
                    }
                }
            }
        }

        private bool HasWaferOnSlot(List<WaferInfo> wafers, int index)
        {
            if (wafers[index].WaferStatus == 0)
                return false;

            return true;
        }
        #endregion

        #region Job operation
        private bool JobCheck(string jobID)
        {
            if (jobID.Length == 0)
            {
                DialogBox.ShowWarning("Please create job first.");
                return false;
            }
            else
                return true;
        }

        public void CreateJob(WaferAssociationInfo info)
        {
            List<string> slotSequence = new List<string>();

            foreach (var wafer in info.ModuleData.WaferManager.Wafers)
            {
                slotSequence.Insert(0, wafer.SequenceName);
            }


            string jobId = info.LotId.Trim();
            if (string.IsNullOrEmpty(jobId))
                jobId = "CJ_Local_" + info.ModuleData.ModuleID;
            //info.LotId = "test";
            info.LotIdSaved = true;
            WaferAssociationProvider.Instance.CreateJob(jobId, info.ModuleData.ModuleID, slotSequence, true, false, 25);
        }



        public void AbortJob(string jobID)
        {
            if (JobCheck(jobID))
                WaferAssociationProvider.Instance.AbortJob(jobID);
        }
        public void Start(string jobID)
        {
            if (JobCheck(jobID))
                WaferAssociationProvider.Instance.Start(jobID);
        }
        public void Pause(string jobID)
        {
            if (JobCheck(jobID))
                WaferAssociationProvider.Instance.Pause(jobID);
        }
        public void Resume(string jobID)
        {
            if (JobCheck(jobID))
                WaferAssociationProvider.Instance.Resume(jobID);
        }
        public void Stop(string jobID)
        {
            if (JobCheck(jobID))
                WaferAssociationProvider.Instance.Stop(jobID);
        }
        #endregion
        #endregion

    }
}
