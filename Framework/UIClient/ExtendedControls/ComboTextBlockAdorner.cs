using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Input;

namespace OpenSEMI.Ctrlib.Controls
{
    public class ComboTextBlockAdorner: Adorner
    {
        public ComboBoxExt TextBox
        {
            get { return this._comboBox; }
        }

        private readonly VisualCollection _collection;
        private readonly ComboBoxExt _comboBox;
        private readonly ComboTextBlock _textBlock;

        private TextBox _textboxInEditableComboBox;

        public ComboTextBlockAdorner(ComboTextBlock adornedElement)
            : base(adornedElement)
        {
            _collection = new VisualCollection(this);

            _comboBox = new ComboBoxExt();
            _comboBox.DisplayMemberPath = "DisplayName";
            _comboBox.IsEditable = adornedElement.IsEditable;


            _textBlock = adornedElement;


            Binding binding = new Binding("ItemsSource") { Source = adornedElement };
            binding.Mode = BindingMode.TwoWay;
            _comboBox.SetBinding(ComboBoxExt.ItemsSourceProperty, binding);

            binding = new Binding("SelectedItem") { Source = adornedElement };
            binding.Mode = BindingMode.TwoWay;
            _comboBox.SetBinding(ComboBoxExt.SelectedItemProperty, binding);

            binding = new Binding("Text") { Source = adornedElement };
            binding.Mode = BindingMode.TwoWay;
            _comboBox.SetBinding(ComboBoxExt.TextProperty, binding);
            _comboBox.Text = _textBlock.Text;
 
            binding = new Binding("TextSaved") { Source = adornedElement };
            binding.Mode = BindingMode.TwoWay;
            _comboBox.SetBinding(ComboBoxExt.ComboBoxSavedProperty, binding);
            //_textBox.KeyUp += _textBox_KeyUp;

            _comboBox.Loaded += _comboBox_Loaded;
            _comboBox.Unloaded += _comboBox_Unloaded;

            _collection.Add(_comboBox);
        }

        private void _comboBox_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_comboBox.IsEditable)
            {
                _textboxInEditableComboBox = (_comboBox.Template.FindName("PART_EditableTextBox", _comboBox) as TextBox);
                if (_textboxInEditableComboBox != null)
                {
                    _textboxInEditableComboBox.LostFocus -= _textBlock.TextBoxLostFocus;
                }
            }
            else
            {
                //_comboBox.SelectionChanged -= _textBlock.TextBoxLostFocus;

                _comboBox.DropDownClosed -= _textBlock.DropDownClosedLostFocus;
            }

        }


        private void _comboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (_comboBox.IsEditable)
            {
                _textboxInEditableComboBox = (_comboBox.Template.FindName("PART_EditableTextBox", _comboBox) as TextBox);
                if (_textboxInEditableComboBox != null)
                {
                    _textboxInEditableComboBox.LostFocus += _textBlock.TextBoxLostFocus;

                    _textboxInEditableComboBox.Focus();
                }

                
            }
            else
            {
                //_comboBox.SelectionChanged += _textBlock.TextBoxLostFocus;

                _comboBox.IsDropDownOpen = true;

                _comboBox.DropDownClosed += _textBlock.DropDownClosedLostFocus;
            }


        }
 

        public event RoutedEventHandler TextBoxLostFocus
        {
            add
            {
                
                if (_textboxInEditableComboBox != null)
                    _textboxInEditableComboBox.LostFocus += value;
            }
            remove
            {
                //var textBox = (_comboBox.Template.FindName("PART_EditableTextBox", _comboBox) as TextBox);
                if (_textboxInEditableComboBox != null)
                    _textboxInEditableComboBox.LostFocus -= value;
            }
        }
        //public event KeyEventHandler TextBoxKeyUp
        //{
        //    add
        //    {
        //        _textBox.KeyUp += value;
        //    }
        //    remove
        //    {
        //        _textBox.KeyUp -= value;
        //    }
        //}


        protected override Visual GetVisualChild(int index)
        {
            return _collection[index];
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return _collection.Count;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _comboBox.Arrange(new Rect(0, 0, _textBlock.ActualWidth, _textBlock.ActualHeight));
            //if (_textbox == null)
           //     _comboBox.Focus();
            //_comboBox.ScrollToEnd();
            return finalSize;
        }

        private void _textBox_KeyUp(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Enter)
            //{
            //    _textBox.Text = _textBox.Text.Replace("\r\n", string.Empty);
            //    BindingExpression expression = _textBox.GetBindingExpression(TextBoxEx.TextProperty);
            //    if (null != expression)
            //        expression.UpdateSource();

            //    expression = _textBox.GetBindingExpression(TextBoxEx.TextSavedProperty);
            //    if (null != expression)
            //        expression.UpdateSource();
            //}
        }

    }
}
