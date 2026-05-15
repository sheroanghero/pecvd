using System;
using Aitex.Core.RT.Log;
using Caliburn.Micro;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.OperationCenter;
using OpenSEMI.Ctrlib.Controls;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class WaferMoveManagerDual
    {
        #region single Instance
        private WaferMoveManagerDual()
        {
        }

        private static WaferMoveManagerDual m_Instance = null;
        public static WaferMoveManagerDual Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new WaferMoveManagerDual();
                }
                return m_Instance;
            }
        }
        #endregion

        public void TransferWafer(Slot p_from, Slot p_to, bool showAligner = true, bool showCooling = true)
        {
            try
            {
                if (p_from == null || p_to == null || !p_from.IsValidSlot() || !p_to.IsValidSlot())
                    return;

                //DialogButton btns = DialogButton.Transfer | DialogButton.Cancel;       
                string info = " from " + p_from.ModuleID + " slot " + (p_from.SlotID + 1).ToString() + " to " + p_to.ModuleID + " slot " + (p_to.SlotID + 1).ToString();
                string message = "Are you sure to transfer the wafer: \n" + info;
                //DialogButton m_dResult = DialogBox.ShowDialog(btns, DialogType.CONFIRM, message);

                bool displayAlignerCondition = false;
                if (showAligner)
                {
                    displayAlignerCondition = (p_from.ModuleID == "TMRobot" || ModuleHelper.IsVCE(ModuleHelper.Converter(p_from.ModuleID))) &&(p_to.ModuleID == "TMRobot" || ModuleHelper.IsPm(ModuleHelper.Converter(p_to.ModuleID)));
                    displayAlignerCondition = displayAlignerCondition && (!ModuleHelper.IsAligner(ModuleHelper.Converter(p_from.ModuleID)) && !ModuleHelper.IsAligner(ModuleHelper.Converter(p_to.ModuleID)));
                }
                else
                    displayAlignerCondition = false;

                bool displayPassCoolingCondition = false;
                if(showCooling)
                {
                    displayPassCoolingCondition = (p_from.ModuleID.Contains("PM") || p_from.ModuleID == "TMRobot");

                    displayPassCoolingCondition = displayPassCoolingCondition && (!p_to.ModuleID.Contains("PM")) && (p_to.ModuleID != "TMRobot")
                        && (p_to.ModuleID != "LLA") && (p_to.ModuleID != "LLB");
                }
                else
                    displayPassCoolingCondition = false;

                WindowManager wm = new WindowManager();
                WaferTransferDualDialogViewModel _transferVM = new WaferTransferDualDialogViewModel(message, displayAlignerCondition, displayPassCoolingCondition);
                bool? bret = wm.ShowDialogWithNoStyle(_transferVM);
                if ((bool)bret)
                {
                    //get and use transfer conditions
                    WaferTransferCondition conditions = _transferVM.DialogResult;                    

                    InvokeClient.Instance.Service.DoOperation("System.MoveWafer", 
                        p_from.ModuleID, p_from.SlotID, p_to.ModuleID, p_to.SlotID, _transferVM.AddMoreWafer);
                }

                p_from.ClearDragDropStatus();
                p_to.ClearDragDropStatus();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }
    }
}
