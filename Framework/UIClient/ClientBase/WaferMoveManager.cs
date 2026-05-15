using System;
using System.Collections.Generic;
using System.Windows;
using Aitex.Core.RT.Log;
using Caliburn.Micro;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.OperationCenter;
using OpenSEMI.Ctrlib.Controls;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class WaferMoveManager
    {
        #region single Instance

        public bool ShowAligner { get; set; }
        public bool ShowCooling { get; set; }
        public bool ShowBlade { get; set; }

        private WaferMoveManager()
        {
            ShowAligner = true;
            ShowCooling = true;
            ShowBlade = false;
        }

        private static WaferMoveManager m_Instance = null;
        public static WaferMoveManager Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new WaferMoveManager();
                }
                return m_Instance;
            }
        }
        #endregion

        public void TransferWafer(Slot p_from, Slot p_to)
        {
            try
            {
                if (p_from == null || p_to == null || !p_from.IsValidSlot() || !p_to.IsValidSlot())
                    return;

                string fromInfo = string.Empty;
                string toInfo = string.Empty;

                if (ModuleHelper.IsTMRobot(ModuleHelper.Converter(p_from.ModuleID)) || ModuleHelper.IsLoadLock(ModuleHelper.Converter(p_from.ModuleID)))
                    fromInfo = " from " + p_from.ModuleID + " slot " + (p_from.SlotID + 1).ToString();
                else
                    fromInfo = " from " + p_from.ModuleID;

                if (ModuleHelper.IsTMRobot(ModuleHelper.Converter(p_to.ModuleID)) || ModuleHelper.IsLoadLock(ModuleHelper.Converter(p_to.ModuleID)))
                    toInfo = " to " + p_to.ModuleID + " slot " + (p_to.SlotID + 1).ToString();
                else
                    toInfo = " to " + p_to.ModuleID;

                string info = fromInfo + toInfo;
                string message = "Are you sure to transfer the wafer: \n" + info;
   
                bool displayAlignerCondition = (ModuleHelper.IsEfemRobot(ModuleHelper.Converter(p_from.ModuleID)) || ModuleHelper.IsLoadPort(ModuleHelper.Converter(p_from.ModuleID)))
                    && (ModuleHelper.IsLoadLock(ModuleHelper.Converter(p_to.ModuleID)) || ModuleHelper.IsTMRobot(ModuleHelper.Converter(p_to.ModuleID)) 
                    || ModuleHelper.IsPm(ModuleHelper.Converter(p_to.ModuleID)) || ModuleHelper.IsAligner(ModuleHelper.Converter(p_to.ModuleID)));

                bool displayPassCoolingCondition = (ModuleHelper.IsPm(ModuleHelper.Converter(p_from.ModuleID)) || ModuleHelper.IsTMRobot(ModuleHelper.Converter(p_from.ModuleID))) 
                    && (ModuleHelper.IsEfemRobot(ModuleHelper.Converter(p_to.ModuleID)) || ModuleHelper.IsAligner(ModuleHelper.Converter(p_to.ModuleID)) || ModuleHelper.IsLoadPort(ModuleHelper.Converter(p_to.ModuleID))
                    || (ModuleHelper.IsLoadLock(ModuleHelper.Converter(p_to.ModuleID)) && p_to.SlotID == 0));

                bool displayBladeCondition = ShowBlade;
                WindowManager wm = new WindowManager();
                WaferTransferDialogViewModel _transferVM = new WaferTransferDialogViewModel(message, displayAlignerCondition, displayPassCoolingCondition, displayBladeCondition);
                _transferVM.AlignerVisibility = ShowAligner ? Visibility.Visible : Visibility.Hidden;
                _transferVM.CoolingVisibility = ShowCooling ? Visibility.Visible : Visibility.Hidden;
                _transferVM.BladeVisibility = ShowBlade ? Visibility.Visible : Visibility.Hidden;
                bool? bret = wm.ShowDialogWithNoStyle(_transferVM);
                if ((bool)bret)
                {
                    //get and use transfer conditions
                    WaferTransferCondition conditions = _transferVM.DialogResult;

                    InvokeClient.Instance.Service.DoOperation("System.MoveWafer",
                        p_from.ModuleID, p_from.SlotID, p_to.ModuleID, p_to.SlotID,
                        conditions.IsPassAligner, conditions.AlignerAngle, conditions.IsPassCooling, conditions.CoolingTime, (int)conditions.Blade);
                }

                p_from.ClearDragDropStatus();
                p_to.ClearDragDropStatus();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public void TransferWaferUI(string Invoke, string moduleFrom, int slotFrom)
        {
            string info = " from " + moduleFrom + " slot " + slotFrom + 1;

            string message = "Are you sure to move the wafer: \n" + info;

            WindowManager wm = new WindowManager();
            Dictionary<string, int> keyValues = new Dictionary<string, int>();
            foreach (string aa in ModuleManager.ModuleInfos.Keys)
            {
                keyValues.Add(aa, ModuleManager.ModuleInfos[aa].WaferManager.Wafers.Count);
            }
            WaferUIMoveDialogViewModel _transferVM = new WaferUIMoveDialogViewModel(keyValues,message);
            bool? bret = wm.ShowDialogWithNoStyle(_transferVM);
            if ((bool)bret)
            {

                WaferUIMoveCondition conditions = _transferVM.DialogResult;

                InvokeClient.Instance.Service.DoOperation(Invoke, moduleFrom, slotFrom, conditions.MoveToModule, conditions.MoveToSlot);


            }
        }
    }
}
