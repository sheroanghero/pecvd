using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;

namespace DataGridTransform
{
    public class DataGridDynamicColumn : EGC.DataGridTemplateColumn
    {
        #region Constructor and Property

        public DataGridExtern parent { get; set; }
        protected DataGridDynamicColumns m_ChildItems = null;
        public DataGridDynamicColumns ChildItems
        {
            get { return m_ChildItems; }
        }

        public DataGridExpanderProperty m_expander_property = null;
        public DataGridExpanderProperty expander_property
        {
            get { return m_expander_property; }
            set
            {
                m_expander_property = value;
            }
        }

        public String StringHeaderTemplate = null;
        public String StringCellTemplate = null;

        public DataGridDynamicColumn()
        {
            isHeader = false;
        }

        public bool isHeader {get;set;}

        #endregion

        #region Define Virtual Functions

        public virtual DataGridDynamicColumn Clone()
        {
            return new DataGridDynamicColumn();
        }

        public virtual void CopyValueFrom(DataGridDynamicColumn right)
        {

        }

        public virtual void CopyAllFrom(DataGridDynamicColumn right)
        {

        }

        public virtual void CopyTemplate(DataGridDynamicColumn right)
        {
            if (right == null)
                return;

            this.StringCellTemplate = right.StringCellTemplate;
            this.StringHeaderTemplate = right.StringHeaderTemplate;
            this.CellTemplate = right.CellTemplate;
            this.HeaderTemplate = right.HeaderTemplate;
        }

        public virtual bool IsDataEqule(DataGridDynamicColumn right)
        {
            return true;
        }
        #endregion
    }
}
