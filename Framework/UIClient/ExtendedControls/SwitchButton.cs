using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;

namespace OpenSEMI.Ctrlib.Controls
{
    public class SwitchButton : Button
    {
        public static readonly DependencyProperty ONProperty;
        static SwitchButton()
        {
            ONProperty = DependencyProperty.Register("ON", typeof(bool), typeof(SwitchButton));
        }

        public bool ON
        {
            get { return (bool)GetValue(ONProperty); }
            set { SetValue(ONProperty, value); }
        }

    }
}
