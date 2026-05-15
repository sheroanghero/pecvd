using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace OpenSEMI.Ctrlib.Controls
{
    public class ComboBoxExt : ComboBox
    {
        public bool ComboBoxSaved
        {
            get { return (bool)GetValue(ComboBoxSavedProperty); }
            set { SetValue(ComboBoxSavedProperty, value); }
        }
        public static readonly DependencyProperty ComboBoxSavedProperty =
            DependencyProperty.Register("ComboBoxSaved", typeof(bool), typeof(ComboBoxExt),
                new UIPropertyMetadata(true));

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            this.ComboBoxSaved = false;
        }
    }
}
