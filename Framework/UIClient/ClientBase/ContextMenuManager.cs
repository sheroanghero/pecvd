using Aitex.Core.RT.Log;
using Caliburn.Micro;
using MECF.Framework.Common.OperationCenter;
using OpenSEMI.Ctrlib.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class MenuElement
    {
        public string Name { get; set; }
        public string Invoke { get; set; }
        public bool IsSeparator { get; set; }
    }

    public class ContextMenuManager
    {
        public bool ShowAligner { get; set; }
        public bool ShowCooling { get; set; }

        public ContextMenuManager()
        {
            _slotMenus = InitMenus(_SlotMenuElements);
            _carrierMenus = InitMenus(_carrierMenuElements);

            ShowAligner = true;
            ShowCooling = true;
        }

        #region single Instance
        private static ContextMenuManager m_Instance = null;
        public static ContextMenuManager Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new ContextMenuManager();
                }
                return m_Instance;
            }
        }
        #endregion

        #region Slot Menus

        private ContextMenu _slotMenus;
        public ContextMenu SlotMenus
        {
            get { return _slotMenus; }
        }

        private readonly List<MenuElement> _SlotMenuElements = new List<MenuElement>() {
            new MenuElement(){ Name="Create Wafer", Invoke="CreateWafer"},
            new MenuElement(){ Name="Delete Wafer", Invoke="DeleteWafer"},
            new MenuElement(){ Name="-", IsSeparator = true},
            new MenuElement(){ Name="Return Wafer", Invoke="ReturnWafer"},
             new MenuElement(){ Name="-", IsSeparator = true},
             new MenuElement(){ Name="Move Wafer", Invoke="MoveWafer"},
        };

        public void OnContextMenuClick(object sender, RoutedEventArgs args)
        {
            try
            {
                MenuItem m_menu = sender as MenuItem;
                Slot CurrentSlot = ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent(m_menu)) as Slot;
                if (CurrentSlot == null)
                    return;

                MenuElement ele = m_menu.Tag as MenuElement;

                //if (ele.Invoke == "ReturnWafer")
                //{
                //    ReturnWafer(ele.Invoke, CurrentSlot);
                //}
                //else
                //{
                //    InvokeClient.Instance.Service.DoOperation(ele.Invoke, CurrentSlot.ModuleID, CurrentSlot.SlotID);
                //}

                //var param = new object[] { ModuleName.LP1, 5, WaferStatus.Normal };

                if (ele.Invoke == "MoveWafer")
                {
                    WaferMoveManager.Instance.TransferWaferUI(ele.Invoke, CurrentSlot.ModuleID, CurrentSlot.SlotID);
                }
                else if (ele.Invoke == "ReturnWafer")
                {
                    ReturnWafer(CurrentSlot.ModuleID, CurrentSlot);
                }
                else
                {
                    InvokeClient.Instance.Service.DoOperation(ele.Invoke, CurrentSlot.ModuleID, CurrentSlot.SlotID);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        private ContextMenu InitMenus(List<MenuElement> menus)
        {
            ContextMenu cm = new ContextMenu();

            foreach (MenuElement element in menus)
            {
                if (element.IsSeparator)
                {
                    cm.Items.Add(new Separator());
                }
                else
                {
                    MenuItem m_item = new MenuItem();
                    m_item.Header = element.Name;
                    m_item.Tag = element;
                    m_item.Click += new RoutedEventHandler(OnContextMenuClick);
                    m_item.IsEnabled = true;
                    cm.Items.Add(m_item);
                }

            }
            return cm;
        }

        public ContextMenu GetSlotMenus(Slot slot)
        {
            if (slot != null)
            {
                MenuItem createWafer = _slotMenus.Items.GetItemAt(0) as MenuItem;
                MenuItem deleteWafer = _slotMenus.Items.GetItemAt(1) as MenuItem;
                MenuItem returnWafer = _slotMenus.Items.GetItemAt(3) as MenuItem;
                MenuItem moveWafer = _slotMenus.Items.GetItemAt(5) as MenuItem;

                createWafer.IsEnabled = (WaferStatus)slot.WaferStatus == WaferStatus.Empty ? true : false;
                deleteWafer.IsEnabled = (WaferStatus)slot.WaferStatus == WaferStatus.Empty ? false : true;
                returnWafer.IsEnabled = (WaferStatus)slot.WaferStatus != WaferStatus.Empty && !slot.ModuleID.StartsWith("LP");
                moveWafer.IsEnabled = (WaferStatus)slot.WaferStatus == WaferStatus.Empty ? false : true;
            }
            return _slotMenus;
        }

        public void ReturnWafer(string menu, Slot p_from)
        {
            try
            {
                if (p_from == null || !p_from.IsValidSlot())
                    return;
                
                //DialogButton btns = DialogButton.Transfer | DialogButton.Cancel;       
                string info = " from " + p_from.ModuleID + " slot " + (p_from.SlotID + 1).ToString();
                string message = "Are you sure to return the wafer: \n" + info;
                //DialogButton m_dResult = DialogBox.ShowDialog(btns, DialogType.CONFIRM, message);

                bool displayAlignerCondition = p_from.ModuleID == "LP1" || p_from.ModuleID == "LP2" || p_from.ModuleID == "LP3" || p_from.ModuleID == "EfemRobot"
                    || p_from.ModuleID == "LLA" || p_from.ModuleID == "LLB" || p_from.ModuleID == "LLC" || p_from.ModuleID == "LLD" || p_from.ModuleID == "Buffer";

                displayAlignerCondition = displayAlignerCondition && (p_from.ModuleID != "Aligner");

                bool displayPassCoolingCondition = (p_from.ModuleID.Contains("PM") || p_from.ModuleID == "TMRobot");

                WindowManager wm = new WindowManager();
                WaferTransferDialogViewModel _transferVM = new WaferTransferDialogViewModel(message, displayAlignerCondition, displayPassCoolingCondition);
                _transferVM.AlignerVisibility = ShowAligner ? Visibility.Visible : Visibility.Hidden;
                _transferVM.CoolingVisibility = ShowCooling ? Visibility.Visible : Visibility.Hidden;
                bool? bret = wm.ShowDialogWithNoStyle(_transferVM);
                if ((bool)bret)
                {
                    //get and use transfer conditions
                    WaferTransferCondition conditions = _transferVM.DialogResult;
                    //InvokeClient.Instance.Service.DoOperation("System.MoveWafer",
                    //    p_from.ModuleID, p_from.SlotID, p_to.ModuleID, p_to.SlotID,
                    //    conditions.IsPassAligner, conditions.AlignerAngle, conditions.IsPassCooling, conditions.CoolingTime, (int)conditions.Blade);
                    InvokeClient.Instance.Service.DoOperation("ReturnWafer", p_from.ModuleID, p_from.SlotID,
                        conditions.IsPassCooling, conditions.CoolingTime);
                }

                p_from.ClearDragDropStatus();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        #endregion



        #region Carrier menus

        private ContextMenu _carrierMenus;
        public ContextMenu CarrierMenus
        {
            get { return _carrierMenus; }
        }

        private readonly List<MenuElement> _carrierMenuElements = new List<MenuElement>() {
            new MenuElement(){ Name="Create Carrier", Invoke="System.CreateCarrier"},
            new MenuElement(){ Name="Delete Carrier", Invoke="System.DeleteCarrier"},

        };

        public void OnCarrierContextMenuClick(object sender, RoutedEventArgs args)
        {
            try
            {
                MenuItem menuItem = sender as MenuItem;
                CarrierContentControl locateCarrier = ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent(menuItem)) as CarrierContentControl;
                if (locateCarrier == null)
                    return;

                MenuElement ele = menuItem.Tag as MenuElement;
 
                InvokeClient.Instance.Service.DoOperation(ele.Invoke, locateCarrier.ModuleID, locateCarrier.CarrierID);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }


        public ContextMenu GetCarrierMenus(CarrierContentControl carrier)
        {
            if (carrier != null)
            {
                MenuItem createCarrier = _carrierMenus.Items.GetItemAt(0) as MenuItem;
                MenuItem deleteWafer = _carrierMenus.Items.GetItemAt(1) as MenuItem;

                createCarrier.IsEnabled = (WaferStatus)carrier.WaferStatus == WaferStatus.Empty ? true : false;
                deleteWafer.IsEnabled = (WaferStatus)carrier.WaferStatus == WaferStatus.Empty ? false : true;
            }
            return _carrierMenus;
        }

        #endregion

    }
}
