using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;

namespace DataGridTransform
{
    public class DataGridExpanderProperty : INotifyPropertyChanged
    {
        public DataGridDynamicColumn parent { get; set; }

        private bool m_IsExpanded = true;
        public bool MyIsExpanded
        {
            get { return m_IsExpanded; }
            set
            {
                if (m_IsExpanded != value)
                {
                    m_IsExpanded = value;
                    OnChanged(value);
                    NotifyPropertyChanged("MyIsExpanded");
                }
            }
        }

        public void OnChanged(bool val)
        {
            if (parent != null)
            {
                if (parent.parent != null)
                {
                    parent.parent.UpdateColumnWidth(parent, val);
                }
            }
        }
        #region Notify implement

        /// <summary>
        /// Notify implement
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(String PropertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
            }
        }


        #endregion
    }
}
