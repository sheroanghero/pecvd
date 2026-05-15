using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataGridTransform
{
    public class DataGridColumnExpander : DataGridDynamicColumn
    {
        public DataGridColumnExpander()
            : base()
        {
            m_ChildItems = new DataGridDynamicColumns();
        }

        public override DataGridDynamicColumn Clone()
        {
            DataGridColumnExpander expanderColumn = new DataGridColumnExpander();
            expanderColumn.HeaderTemplate = this.HeaderTemplate;

            foreach (DataGridDynamicColumn column in this.ChildItems)
            {
                expanderColumn.ChildItems.Add(column.Clone());
            }

            expanderColumn.expander_property = this.expander_property;

            return expanderColumn;
        }
    }
}
