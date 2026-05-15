
using System;
using System.Collections.Generic;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;

namespace ExtendedGrid.Microsoft.Windows.Controls
{
    /// <summary>
    /// AutomationPeer for DataGridRowsPresenter
    /// </summary>
    public sealed class DataGridRowsPresenterAutomationPeer : FrameworkElementAutomationPeer
    {
        #region Constructors

        /// <summary>
        /// AutomationPeer for DataGridColumnHeadersPresenter
        /// </summary>
        /// <param name="owner">DataGridColumnHeadersPresenter</param>
        public DataGridRowsPresenterAutomationPeer(EGC.DataGridRowsPresenter owner)
            : base(owner)
        {
        }

        #endregion

        #region Properties

        private EGC.DataGridAutomationPeer DataGridPeer
        {
            get
            {
                if (this.OwningRowsPresenter.Owner != null)
                {
                    return CreatePeerForElement(this.OwningRowsPresenter.Owner) as EGC.DataGridAutomationPeer;
                }

                return null;
            }
        }

        private EGC.DataGridRowsPresenter OwningRowsPresenter
        {
            get
            {
                return (EGC.DataGridRowsPresenter)Owner;
            }
        }

        #endregion

        #region AutomationPeer Overrides

        ///
        protected override List<AutomationPeer> GetChildrenCore()
        {
            if (DataGridPeer != null)
            {
                return DataGridPeer.GetItemPeers();
            }

            return base.GetChildrenCore();
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

        /// <summary>
        /// Gets a value that specifies whether the element is a content element.
        /// </summary>
        /// <returns>true if the element is a content element; otherwise false</returns>
        protected override bool IsContentElementCore()
        {
            return false;
        }

        #endregion
    }
}
