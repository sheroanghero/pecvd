using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DataGridTransform
{
    public class DataGridDynamicColumns : ObservableCollection<DataGridDynamicColumn>
    {
        #region Constructor and Property

        public DataGridExtern DataGridParent
        {
            get;
            set;
        }

        #endregion

        #region Override Functions

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (DataGridParent != null)
                DataGridParent.OnDynamicColumnItemChanged(e);

            base.OnCollectionChanged(e);
        }

        #endregion

        #region Define Virtual Functions

        public virtual DataGridDynamicColumns Clone()
        {
            DataGridDynamicColumns columns = new DataGridDynamicColumns();

            foreach ( DataGridDynamicColumn column in this )
            {
                DataGridDynamicColumn newColumn = column.Clone();
                //newColumn.Header = newColumn;
                //newColumn.HeaderTemplate = column.HeaderTemplate;
                //newColumn.CellTemplate = column.CellTemplate;

                columns.Add(newColumn);
            }

            return columns;
        }

        #endregion
    }
}
