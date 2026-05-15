using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SciChart.Core.Extensions;

namespace MECF.Framework.UI.Client.Ctrlib.Controls
{
    public class MultipleCheckboxModel : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string Description { get; set; }

        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
    /// <summary>
    /// MultipleSelectionsCombox.xaml 的交互逻辑
    /// </summary>
    public partial class MultipleSelectionsCombox : UserControl
    {
        public MultipleSelectionsCombox()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public IEnumerable<MultipleCheckboxModel> ItemsSource
        {
            get { return (IEnumerable<MultipleCheckboxModel>)GetValue(ItemsSourceProperty); }
            set
            {
                SetValue(ItemsSourceProperty, value);
                SetText();
            }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(object), typeof(MultipleSelectionsCombox), new FrameworkPropertyMetadata(null, ItemsSourcePropertyChangedCallback));

        private static void ItemsSourcePropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var multioleCheckbox = dependencyObject as MultipleSelectionsCombox;
            if (multioleCheckbox == null) return;
            multioleCheckbox.CheckableCombo.ItemsSource = multioleCheckbox.ItemsSource;
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MultipleSelectionsCombox), new FrameworkPropertyMetadata("", TextPropertyChangedCallback));

        private static void TextPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var multioleCheckbox = dependencyObject as MultipleSelectionsCombox;
            if (multioleCheckbox == null) return;
        }

        public string DefaultText
        {
            get { return (string)GetValue(DefaultTextProperty); }
            set { SetValue(DefaultTextProperty, value); }
        }

        public static readonly DependencyProperty DefaultTextProperty =
            DependencyProperty.Register("DefaultText", typeof(string), typeof(MultipleSelectionsCombox), new UIPropertyMetadata(string.Empty));

        #endregion

        #region Event

        private void Checkbox_OnClick(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox == null) return;
            if ((string)checkbox.Content == "All")
            {
                Text = "";
                if (checkbox.IsChecked != null && checkbox.IsChecked.Value)
                {
                    ItemsSource.ForEachDo(x =>
                    {
                        x.IsSelected = true;
                        Text = "All";
                    });
                }
                else
                {
                    ItemsSource.ForEachDo(x =>
                    {
                        x.IsSelected = false;
                        Text = "None";
                    });
                }
            }
            else
            {
                SetText();
            }

        }

        #endregion


        #region Private Method

        private void SetText()
        {
            Text = "";
            var all = ItemsSource.FirstOrDefault(x => x.Description == "All");
            foreach (var item in ItemsSource)
            {

                if (item.IsSelected && item.Description != "All")
                {
                    Text += item.Description + ",";
                }
                else if (all != null)
                {
                    if (all.IsSelected)
                        all.IsSelected = false;
                }
            }

            Text = string.IsNullOrEmpty(Text) ? DefaultText : Text.TrimEnd(new[] { ',' });
        }

        #endregion
    }
}
