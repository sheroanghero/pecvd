using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Caliburn.Micro;
using MECF.Framework.Common.RecipeCenter;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Operations.WaferAssociation
{
    public class WaferAssociationViewModel : BaseModel
    {
        private WaferAssociationInfo _LP1;
        public WaferAssociationInfo LP1
        {
            get { return _LP1; }
            set { _LP1 = value; }
        }

        private WaferAssociationInfo _LP2;
        public WaferAssociationInfo LP2
        {
            get { return _LP2; }
            set { _LP2 = value; }
        }

        private WaferAssociationInfo _LP3;
        public WaferAssociationInfo LP3
        {
            get { return _LP3; }
            set { _LP3 = value; }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            #region test data
            LP1 = new WaferAssociationInfo();
            LP1.ModuleData = ModuleManager.ModuleInfos["LP1"];         

            LP2 = new WaferAssociationInfo();
            LP2.ModuleData = ModuleManager.ModuleInfos["LP2"];

            LP3 = new WaferAssociationInfo();
            LP3.ModuleData = ModuleManager.ModuleInfos["LP3"];
            #endregion
        }

        #region functions
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
            List<object> param = new List<object>();
            param.Add(info.ModuleData.ModuleID);
            foreach (var wafer in info.ModuleData.WaferManager.Wafers)
            {
                param.Add(wafer.SequenceName);
            }
 
            param.Add(false);    //auto start

            //WaferAssociationProvider.Instance.CreateJob(param.ToArray());
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
