using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenSEMI.Ctrlib.Controls
{
    public class EditTextBlock : TextBlock
    {
        static EditTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EditTextBlock), new FrameworkPropertyMetadata(typeof(EditTextBlock)));

        }
        public static readonly DependencyProperty IsInEditModeProperty =
            DependencyProperty.Register("IsInEditMode", typeof(bool), typeof(EditTextBlock), new UIPropertyMetadata(false, IsInEditModeUpdate));

        public bool IsInEditMode
        {
            get
            {
                return (bool)GetValue(IsInEditModeProperty);
            }
            set
            {
                SetValue(IsInEditModeProperty, value);
            }
        }

        private static void IsInEditModeUpdate(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            EditTextBlock textBlock = obj as EditTextBlock;
            if (null != textBlock)
            {
                //Get the adorner layer of the uielement (here TextBlock)
                AdornerLayer layer = AdornerLayer.GetAdornerLayer(textBlock);
                if (layer == null)
                    return;

                //If the IsInEditMode set to true means the user has enabled the edit mode then
                //add the adorner to the adorner layer of the TextBlock.
                if (textBlock.IsInEditMode)
                {
                    if (null == textBlock._adorner)
                    {
                        textBlock._adorner = new EditTextBlockAdorner(textBlock);

                        //Events wired to exit edit mode when the user presses Enter key or leaves the control.
                        textBlock._adorner.TextBoxKeyUp += textBlock.TextBoxKeyUp;
                        textBlock._adorner.TextBoxLostFocus += textBlock.TextBoxLostFocus;
                        textBlock._adorner.TextBox.SelectAll();
                    }
                    layer.Add(textBlock._adorner);
                }
                else
                {
                    //Remove the adorner from the adorner layer.
                    Adorner[] adorners = layer.GetAdorners(textBlock);
                    if (adorners != null)
                    {
                        foreach (Adorner adorner in adorners)
                        {
                            if (adorner is EditTextBlockAdorner)
                            {
                                layer.Remove(adorner);
                            }
                        }
                    }
                }
            }
        }

        private void TextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            IsInEditMode = false;
            TextBoxEx atb = sender as TextBoxEx;
            if (atb == null)
                return;
            if (this.EditBoxMode == EditBoxMode.Decimal || this.EditBoxMode == EditBoxMode.UnSignDecimal ||
                this.EditBoxMode == EditBoxMode.SignInteger || this.EditBoxMode == EditBoxMode.UnSignInteger)
            {
                double dataValue = 0;
                if (!double.TryParse(atb.Text, out dataValue))
                    return;

                if (MinValue == MaxValue && MinValue == 0)
                    return;

                this.Background = (dataValue < MinValue || dataValue > MaxValue) ? Brushes.Red : Brushes.Transparent;
            }
        }

        /// <summary>
        /// release the edit mode when user presses enter.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
        private void TextBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                IsInEditMode = false;
                TextBoxEx atb = sender as TextBoxEx;
                if (atb == null)
                    return;
                if (this.EditBoxMode == EditBoxMode.Decimal || this.EditBoxMode == EditBoxMode.UnSignDecimal ||
                    this.EditBoxMode == EditBoxMode.SignInteger || this.EditBoxMode == EditBoxMode.UnSignInteger)
                {
                    double dataValue = 0;
                    if (!double.TryParse(atb.Text, out dataValue))
                        return;

                    if (MinValue == MaxValue && MinValue == 0)
                        return;

                    this.Background = (dataValue < MinValue || dataValue > MaxValue) ? Brushes.Red : Brushes.Transparent;
                }
            }
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseDown"/> attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs"/> that contains the event data. This event data reports details about the mouse button that was pressed and the handled state.</param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                IsInEditMode = true;
            }
            else if (e.ClickCount == 1)
            {
                IsInEditMode = true;
            }
        }

        #region EditTextBox properties

        public EditBoxMode EditBoxMode
        {
            get { return (EditBoxMode)GetValue(EditBoxModeProperty); }
            set { SetValue(EditBoxModeProperty, value); }
        }
        public static readonly DependencyProperty EditBoxModeProperty = DependencyProperty.Register("EditBoxMode", typeof(EditBoxMode), typeof(EditTextBlock), new UIPropertyMetadata(EditBoxMode.Default));


        private bool m_AllowEmpty = true;
        public bool AllowEmpty
        {
            get { return m_AllowEmpty; }
            set { m_AllowEmpty = value; }
        }

        /// <summary>
        /// This is a flag to indicate that whether to change the bg color
        /// when the text changed by backgroud not by GUI side. 
        /// This flag is true when control is bound to display text from dialog
        /// </summary>
        private bool m_AllowBackgroundChange = false;
        public bool AllowBackgroundChange
        {
            get { return m_AllowBackgroundChange; }
            set { m_AllowBackgroundChange = value; }
        }

        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(double), typeof(EditTextBlock), new UIPropertyMetadata(double.NaN));
        public double MaxValue
        {
            get { return (double)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }


        public double MinValue
        {
            get { return (double)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register("MinValue", typeof(double), typeof(EditTextBlock), new UIPropertyMetadata(double.NaN));

        public Int32 Accuracy
        {
            get { return (Int32)GetValue(AccuracyProperty); }
            set { SetValue(AccuracyProperty, value); }
        }
        public static readonly DependencyProperty AccuracyProperty = DependencyProperty.Register("Accuracy", typeof(Int32), typeof(EditTextBlock), new UIPropertyMetadata(4));


        public bool TextSaved
        {
            get { return (bool)GetValue(TextSavedProperty); }
            set { SetValue(TextSavedProperty, value); }
        }
        public static readonly DependencyProperty TextSavedProperty =
            DependencyProperty.Register("TextSaved", typeof(bool), typeof(EditTextBlock),
                new UIPropertyMetadata(true, new PropertyChangedCallback(TextSavedChangedCallBack)));


        public Boolean IsScrollToEnd
        {
            get { return (Boolean)GetValue(ScrollToEndProperty); }
            set { SetValue(ScrollToEndProperty, value); }
        }
        public static readonly DependencyProperty ScrollToEndProperty = DependencyProperty.Register("IsScrollToEnd", typeof(Boolean), typeof(EditTextBlock), new UIPropertyMetadata(false));


        private static void TextSavedChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            EditTextBlock m_txt = d as EditTextBlock;
            if (m_txt != null)
            {
                //SetBGColor(m_txt);
            }
        }

        #endregion

        private EditTextBlockAdorner _adorner;
    }
}
