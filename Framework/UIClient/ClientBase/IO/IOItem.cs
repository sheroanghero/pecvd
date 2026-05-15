using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OpenSEMI.ClientBase.IO
{
    public class IOItem : DependencyObject
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Key { get; set; }
        public string DisplayName { get; set; }
    }

    public class IOItem<T> : IOItem
    {
        public static DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(T), typeof(IOItem<T>));

        public T Value
        {
            get
            {
                return (T)GetValue(ValueProperty);
            }
            set
            {
                SetValue(ValueProperty, value);
            }
        }
        public static DependencyProperty IsMatchProperty = DependencyProperty.Register("IsMatch", typeof(bool), typeof(IOItem<T>), new PropertyMetadata(true));
        public bool IsMatch
        {
            get
            {
                return (bool)GetValue(IsMatchProperty);
            }
            set
            {
                SetValue(IsMatchProperty, value);
            }
        }
    }

    public class AOItem : IOItem<short>
    {
        public static readonly DependencyProperty NewValueProperty = DependencyProperty.Register("NewValue", typeof(short), typeof(AOItem), new PropertyMetadata((short)0));

        public short NewValue
        {
            get { return (short)GetValue(NewValueProperty); }
            set { SetValue(NewValueProperty, value); }
        }

        public static readonly DependencyProperty TextSavedProperty = DependencyProperty.Register("TextSaved", typeof(bool), typeof(AOItem), new PropertyMetadata(true));

        public bool TextSaved
        {
            get { return (bool)GetValue(TextSavedProperty); }
            set { SetValue(TextSavedProperty, value); }
        }
    }

    public class AOItemFloat : IOItem<float>
    {
        public static readonly DependencyProperty NewValueProperty = DependencyProperty.Register("NewValue", typeof(float), typeof(AOItem), new PropertyMetadata((float)0));

        public float NewValue
        {
            get { return (float)GetValue(NewValueProperty); }
            set { SetValue(NewValueProperty, value); }
        }

        public static readonly DependencyProperty TextSavedProperty = DependencyProperty.Register("TextSaved", typeof(bool), typeof(AOItem), new PropertyMetadata(true));

        public bool TextSaved
        {
            get { return (bool)GetValue(TextSavedProperty); }
            set { SetValue(TextSavedProperty, value); }
        }
    }
}
