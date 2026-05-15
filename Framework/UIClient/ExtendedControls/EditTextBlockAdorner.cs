using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Input;

namespace OpenSEMI.Ctrlib.Controls
{
    public class EditTextBlockAdorner: Adorner
    {

        public TextBoxEx TextBox
        {
            get { return this._textBox; }
        }

        private readonly VisualCollection _collection;
        private readonly TextBoxEx _textBox;
        private readonly TextBlock _textBlock;

        public EditTextBlockAdorner(EditTextBlock adornedElement)
            : base(adornedElement)
        {
            _collection = new VisualCollection(this);
            _textBox = new TextBoxEx();
            _textBlock = adornedElement;
            Binding binding = new Binding("Text") { Source = adornedElement };
            binding.Mode = BindingMode.TwoWay;
            _textBox.SetBinding(TextBoxEx.TextProperty, binding);

            _textBox.EditBoxMode = adornedElement.EditBoxMode;
            _textBox.AllowEmpty = adornedElement.AllowEmpty;
            _textBox.AllowBackgroundChange = adornedElement.AllowBackgroundChange;
            _textBox.MaxValue = adornedElement.MaxValue;
            _textBox.MinValue = adornedElement.MinValue;
            _textBox.Accuracy = adornedElement.Accuracy;
            _textBox.IsScrollToEnd = adornedElement.IsScrollToEnd;

            binding = new Binding("TextSaved") { Source = adornedElement };
            binding.Mode = BindingMode.TwoWay;
            _textBox.SetBinding(TextBoxEx.TextSavedProperty, binding);


            _textBox.KeyUp += _textBox_KeyUp;
            _collection.Add(_textBox);

            if (!adornedElement.IsEnabled)
                _textBlock.Background = Brushes.Gray;
        }

        public event RoutedEventHandler TextBoxLostFocus
        {
            add
            {
                _textBox.LostFocus += value;
            }
            remove
            {
                _textBox.LostFocus -= value;
            }
        }
        public event KeyEventHandler TextBoxKeyUp
        {
            add
            {
                _textBox.KeyUp += value;
            }
            remove
            {
                _textBox.KeyUp -= value;
            }
        }


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
            _textBox.Arrange(new Rect(0, 0, _textBlock.ActualWidth, _textBlock.ActualHeight));
            _textBox.Focus();
            _textBox.ScrollToEnd();
 
            return finalSize;
        }

        private void _textBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _textBox.Text = _textBox.Text.Replace("\r\n", string.Empty);
                BindingExpression expression = _textBox.GetBindingExpression(TextBoxEx.TextProperty);
                if (null != expression)
                    expression.UpdateSource();

                expression = _textBox.GetBindingExpression(TextBoxEx.TextSavedProperty);
                if (null != expression)
                    expression.UpdateSource();
            }
        }

    }
}
