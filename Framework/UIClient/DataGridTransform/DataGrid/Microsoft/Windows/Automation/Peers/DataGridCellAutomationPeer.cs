using System;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;

namespace ExtendedGrid.Microsoft.Windows.Controls
{
    /// <summary>
    /// AutomationPeer for DataGridCell
    /// </summary>
    public sealed class DataGridCellAutomationPeer : FrameworkElementAutomationPeer
    {
        #region Constructors

        /// <summary>
        /// AutomationPeer for DataGridCell.
        /// This automation peer should not be part of the automation tree.
        /// It should act as a wrapper peer for DataGridCellItemAutomationPeer
        /// </summary>
        /// <param name="owner">DataGridCell</param>
        public DataGridCellAutomationPeer(EGC.DataGridCell owner)
            : base(owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            UpdateEventSource();
        }

        #endregion
        
        #region AutomationPeer Overrides

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <returns>The control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }

        /// <summary>
        /// Called by GetClassName that gets a human readable name that, in addition to AutomationControlType, 
        /// differentiates the control represented by this AutomationPeer.
        /// </summary>
        /// <returns>The string that contains the name.</returns>
        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

        #endregion

        #region Private Helpers
        private void UpdateEventSource()
        {
            EGC.DataGridCell cell = (EGC.DataGridCell)Owner;
            EGC.DataGrid dataGrid = cell.DataGridOwner;
            if (dataGrid != null)
            {
                EGC.DataGridAutomationPeer dataGridAutomationPeer = CreatePeerForElement(dataGrid) as EGC.DataGridAutomationPeer;
                if (dataGridAutomationPeer != null)
                {
                    EGC.DataGridItemAutomationPeer itemAutomationPeer = dataGridAutomationPeer.GetOrCreateItemPeer(cell.DataContext);
                    if (itemAutomationPeer != null)
                    {
                        EGC.DataGridCellItemAutomationPeer cellItemAutomationPeer = itemAutomationPeer.GetOrCreateCellItemPeer(cell.Column);
                        this.EventsSource = cellItemAutomationPeer;
                    }
                }
            }
        }
        #endregion
    }
}