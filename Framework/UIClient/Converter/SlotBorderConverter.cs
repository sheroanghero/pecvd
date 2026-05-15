using OpenSEMI.Ctrlib.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OpenSEMI.Ctrlib.Converter
{
    internal class SlotBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is SlotBorderStatus)
            {
                //deal with the priority
                SlotBorderStatus status = (SlotBorderStatus)value;

                if (status.HasFlag(SlotBorderStatus.MouseOver))
                    return SlotBorderStatus.MouseOver;
                else if (status.HasFlag(SlotBorderStatus.TransferSource))
                    return SlotBorderStatus.TransferSource;
                else if (status.HasFlag(SlotBorderStatus.TransferTarget))
                    return SlotBorderStatus.TransferTarget;
                else if (status.HasFlag(SlotBorderStatus.Selected))
                    return SlotBorderStatus.Selected;
                else
                    return SlotBorderStatus.None;
            }
            return SlotBorderStatus.None;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
