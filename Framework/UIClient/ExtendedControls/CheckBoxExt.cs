using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OpenSEMI.Ctrlib.Controls
{
    public class CheckBoxExt:CheckBox
    {
        public bool CheckBoxSaved
        {
            get { return (bool)GetValue(CheckBoxSavedProperty); }
            set { SetValue(CheckBoxSavedProperty, value); }
        }
        public static readonly DependencyProperty CheckBoxSavedProperty =
            DependencyProperty.Register("CheckBoxSaved", typeof(bool), typeof(CheckBoxExt),
                new UIPropertyMetadata(true));

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            this.CheckBoxSaved = false;
        }
        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            this.CheckBoxSaved = false;
        }

    }
}
