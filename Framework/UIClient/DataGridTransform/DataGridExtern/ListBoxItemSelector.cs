using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using DataGridTransform;

namespace DataGridTransform
{
    public class ListBoxItemSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            DataGridDynamicColumn convertItem = item as DataGridDynamicColumn;
            if (convertItem != null)
            {
                if (convertItem.isHeader)
                    return convertItem.HeaderTemplate;
                else
                    return convertItem.CellTemplate;
            }

            return null;
        }
    }
}
