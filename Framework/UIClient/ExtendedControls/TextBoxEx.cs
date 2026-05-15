using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace OpenSEMI.Ctrlib.Controls
{
    public enum EditBoxMode
    {
        Default,
        SignInteger,
        UnSignInteger,
        Decimal,
        UnSignDecimal,
        Email,
        Time,
        FileName
    }

    public class TextBoxEx : TextBox
    {
        public event KeyEventHandler TextBoxExEnterKeyDown;

        #region define fields & properties
        private string m_strOldValue = string.Empty;

        public EditBoxMode EditBoxMode
        {
            get { return (EditBoxMode)GetValue(EditBoxModeProperty); }
            set { SetValue(EditBoxModeProperty, value); }
        }
        public static readonly DependencyProperty EditBoxModeProperty = DependencyProperty.Register("EditBoxMode", typeof(EditBoxMode), typeof(TextBoxEx), new UIPropertyMetadata(EditBoxMode.Default));

        #region NormalColor (DependencyProperty)
        public static readonly DependencyProperty NormalColorProperty = DependencyProperty.Register("NormalColor", typeof(Brush), typeof(TextBoxEx));
        public Brush NormalColor
        {
            get { return (Brush)GetValue(NormalColorProperty); }
            set { SetValue(NormalColorProperty, value); }
        }
        #endregion

        #region ChangedColor (DependencyProperty)
        public static readonly DependencyProperty ChangedColorProperty = DependencyProperty.Register("ChangedColor", typeof(Brush), typeof(TextBoxEx));
        public Brush ChangedColor
        {
            get { return (Brush)GetValue(ChangedColorProperty); }
            set { SetValue(ChangedColorProperty, value); }
        }
        #endregion

        #region WarningColor (DependencyProperty)
        public static readonly DependencyProperty WarningColorProperty = DependencyProperty.Register("WarningColor", typeof(Brush), typeof(TextBoxEx));
        public Brush WarningColor
        {
            get { return (Brush)GetValue(WarningColorProperty); }
            set { SetValue(WarningColorProperty, value); }
        }
        #endregion

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
        private bool m_AllowBackgroundChange = true;
        public bool AllowBackgroundChange
        {
            get { return m_AllowBackgroundChange; }
            set { m_AllowBackgroundChange = value; }
        }

        private bool m_AllowIsOutOfRangeSaveChange = true;
        public bool AllowIsOutOfRangeSaveChange
        {
            get { return m_AllowIsOutOfRangeSaveChange; }
            set { m_AllowIsOutOfRangeSaveChange = value; }
        }

        private int m_KeepDecimals = -1;

        public int KeepDecimals
        {
            get { return m_KeepDecimals; }
            set { m_KeepDecimals = value; }
        }


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            //this.TextSaved = true;
        }
        #region MaxValue (DependencyProperty)
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(double), typeof(TextBoxEx), new FrameworkPropertyMetadata(double.NaN) { BindsTwoWayByDefault=true});
        public double MaxValue
        {
            get { return (double)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        #endregion

        #region MinValue (DependencyProperty)
        public double MinValue
        {
            get { return (double)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register("MinValue", typeof(double), typeof(TextBoxEx), new UIPropertyMetadata(double.NaN));
        #endregion

        #region Accuracy (DependencyProperty)
        public Int32 Accuracy
        {
            get { return (Int32)GetValue(AccuracyProperty); }
            set { SetValue(AccuracyProperty, value); }
        }
        public static readonly DependencyProperty AccuracyProperty = DependencyProperty.Register("Accuracy", typeof(Int32), typeof(TextBoxEx), new UIPropertyMetadata(4));
        #endregion

        #region TextSaved (DependencyProperty)
        public bool TextSaved
        {
            get { return (bool)GetValue(TextSavedProperty); }
            set { SetValue(TextSavedProperty, value); }
        }
        public static readonly DependencyProperty TextSavedProperty =
            DependencyProperty.Register("TextSaved", typeof(bool), typeof(TextBoxEx),
                new UIPropertyMetadata(true, new PropertyChangedCallback(TextSavedChangedCallBack)));
        #endregion

        #region IsScrollToEnd (DependencyProperty)
        public Boolean IsScrollToEnd
        {
            get { return (Boolean)GetValue(ScrollToEndProperty); }
            set { SetValue(ScrollToEndProperty, value); }
        }
        public static readonly DependencyProperty ScrollToEndProperty = DependencyProperty.Register("IsScrollToEnd", typeof(Boolean), typeof(TextBoxEx), new UIPropertyMetadata(false));
        #endregion

        public Boolean IsTextboxFocused
        {
            get { return (Boolean)GetValue(IsTextboxFocusedProperty); }
            set { SetValue(IsTextboxFocusedProperty, value); }
        }
        public static readonly DependencyProperty IsTextboxFocusedProperty = DependencyProperty.Register("IsTextboxFocused", typeof(Boolean), typeof(TextBoxEx), new UIPropertyMetadata(false));
        #endregion

        static TextBoxEx()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextBoxEx), new FrameworkPropertyMetadata(typeof(TextBoxEx)));
        }


        #region Input Control
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            switch (this.EditBoxMode)
            {
                case EditBoxMode.SignInteger:
                    AllowInteger(e);
                    break;
                case EditBoxMode.UnSignInteger:
                    AllowUnsignInteger(e);
                    break;
                case EditBoxMode.Decimal:
                    AllowDecimal(e);
                    break;
                case EditBoxMode.UnSignDecimal:
                    AllowUnSignDecimal(e);
                    break;
                case EditBoxMode.Time:
                    AllowTime(e);
                    break;
                case EditBoxMode.FileName:
                    AllowFileName(e);
                    break;
            }
        }

        private void AllowInteger(KeyEventArgs e)
        {
            bool isControl = ((Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers != ModifierKeys.Shift)
                || e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Insert
                || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up
                || e.Key == Key.Tab
                || e.Key == Key.PageDown || e.Key == Key.PageUp
                || e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Escape
                || e.Key == Key.Home || e.Key == Key.End);

            //if (this.Text.IndexOfAny(new char[] { '-' }, 0) > -1 && this.CaretIndex == 0 && !isControl) //Disable input before minus 
            //{
            //    e.Handled = true;
            //    return;
            //}

            bool isNumPadNumeric = (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
            bool isNumeric = (e.Key >= Key.D0 && e.Key <= Key.D9);

            if ((isNumeric || isNumPadNumeric) && Keyboard.Modifiers != ModifierKeys.None)
            {
                e.Handled = true;
                return;
            }

            //Minus
            if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    isControl = false;
                }
                else
                {

                    if (this.Text.IndexOfAny(new char[] { '-' }, 0) == -1 && this.CaretIndex == 0)
                    {
                        isControl = true;
                    }
                    else
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }

            e.Handled = !isControl && !isNumeric && !isNumPadNumeric;
        }

        private void AllowUnsignInteger(KeyEventArgs e)
        {
            bool isNumPadNumeric = (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
            bool isNumeric = (e.Key >= Key.D0 && e.Key <= Key.D9);

            if ((isNumeric || isNumPadNumeric) && Keyboard.Modifiers != ModifierKeys.None)
            {
                e.Handled = true;
                return;
            }

            bool isControl = ((Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers != ModifierKeys.Shift)
                || e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Insert
                || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up
                || e.Key == Key.Tab
                || e.Key == Key.PageDown || e.Key == Key.PageUp
                || e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Escape
                || e.Key == Key.Home || e.Key == Key.End);

            e.Handled = !isControl && !isNumeric && !isNumPadNumeric;
        }

        private void AllowDecimal(KeyEventArgs e)
        {
            bool isControl = ((Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers != ModifierKeys.Shift)
                || e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Insert
                || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up
                || e.Key == Key.Tab
                || e.Key == Key.PageDown || e.Key == Key.PageUp
                || e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Escape
                || e.Key == Key.Home || e.Key == Key.End);

            //if (this.Text.IndexOfAny(new char[] { '-' }, 0) > -1 && this.CaretIndex == 0 && !isControl) //Disable input before minus 
            //{
            //    e.Handled = true;
            //    return;
            //}

            bool isNumPadNumeric = (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
            bool isNumeric = (e.Key >= Key.D0 && e.Key <= Key.D9);

            if ((isNumeric || isNumPadNumeric) && Keyboard.Modifiers != ModifierKeys.None)
            {
                e.Handled = true;
                return;
            }

            //Minus
            if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    isControl = false;
                }
                else
                {

                    if (this.Text.IndexOfAny(new char[] { '-' }, 0) == -1 && this.CaretIndex == 0)
                    {
                        isControl = true;
                    }
                    else
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }

            //Decimal point
            if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    isControl = false;
                }
                else
                {
                    if (this.Text.IndexOfAny(new char[] { '.' }, 0) == -1 && this.CaretIndex > 0
                        && this.CaretIndex + this.Accuracy >= this.Text.Length)       //Accuracy
                    {
                        isControl = true;
                    }
                    else
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }

            //Accuracy            
            int pointIndex = this.Text.IndexOf('.');
            if (pointIndex > -1 && this.CaretIndex > pointIndex)
            {
                if (this.Text.Length - pointIndex > this.Accuracy)
                {
                    isNumeric = isNumPadNumeric = false;
                }
            }

            e.Handled = !isControl && !isNumeric && !isNumPadNumeric;
        }

        private void AllowUnSignDecimal(KeyEventArgs e)
        {
            bool isNumPadNumeric = (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
            bool isNumeric = (e.Key >= Key.D0 && e.Key <= Key.D9);

            if ((isNumeric || isNumPadNumeric) && Keyboard.Modifiers != ModifierKeys.None)
            {
                e.Handled = true;
                return;
            }

            bool isControl = ((Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers != ModifierKeys.Shift)
                || e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Insert
                || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up
                || e.Key == Key.Tab
                || e.Key == Key.PageDown || e.Key == Key.PageUp
                || e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Escape
                || e.Key == Key.Home || e.Key == Key.End);

            //Decimal point
            if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    isControl = false;
                }
                else
                {
                    if (this.Text.IndexOfAny(new char[] { '.' }, 0) == -1 && this.CaretIndex > 0
                        && this.CaretIndex + this.Accuracy >= this.Text.Length)       //Accuracy
                    {
                        isControl = true;
                    }
                    else
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }

            //Accuracy            
            int pointIndex = this.Text.IndexOf('.');
            if (pointIndex > -1 && this.CaretIndex > pointIndex)
            {
                if (this.Text.Length - pointIndex > this.Accuracy)
                {
                    isNumeric = isNumPadNumeric = false;
                }
            }
            e.Handled = !isControl && !isNumeric && !isNumPadNumeric;
        }

        private void AllowTime(KeyEventArgs e)
        {
            AllowDecimal(e);

            if (e.Key == Key.Oem1 && Keyboard.Modifiers == ModifierKeys.Shift)
                e.Handled = false;

            if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
                e.Handled = true;
        }

        private void AllowFileName(KeyEventArgs e)
        {
            if (this.Text.Length >= 128)
            {
                e.Handled = true;
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (e.Key == Key.OemPeriod ||   //>
                    e.Key == Key.OemComma ||    //<
                    e.Key == Key.D8 ||          //*
                    e.Key == Key.Oem1 ||        //:
                    e.Key == Key.Oem7           //"                    
                    )
                {
                    e.Handled = true;
                    return;
                }
            }

            if (e.Key == Key.Oem2 ||    //? /
                e.Key == Key.Oem5)      //\ |
            {
                e.Handled = true;
                return;
            }

        }
        #endregion

        #region Range Control
        public bool IsOutOfRange()
        {
            if (!double.IsNaN(this.MinValue) && !double.IsNaN(this.MaxValue))
            {
                double value;
                bool m_flag = double.TryParse(this.Text, out value);

                if (value > this.MaxValue)
                    return true;
                else if (value < this.MinValue)
                    return true;
                else
                    return false;
            }
            return false;
        }


        public bool IsOutOfRange(double value)
        {
            if (!double.IsNaN(this.MinValue) && !double.IsNaN(this.MaxValue))
            {
                if (value > this.MaxValue)
                    return true;
                else if (value < this.MinValue)
                    return true;
                else
                    return false;
            }
            return false;
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            this.IsTextboxFocused = false;
            if (this.Text.Length == 0)
            {
                if (!AllowEmpty)
                    this.Text = m_strOldValue;
                return;
            }
            else
            {
                if (EditBoxMode == EditBoxMode.UnSignDecimal || EditBoxMode == EditBoxMode.Decimal || EditBoxMode == EditBoxMode.SignInteger || EditBoxMode == EditBoxMode.UnSignInteger)
                {
                    if (IsOutOfRange())
                    {
                        this.Background = Brushes.Red;
                        this.ToolTip = this.MinValue + "-" + this.MaxValue;
                    }
                    else
                    {
                        SetBGColor(this);
                    }
                }
            }
        }
        #endregion

        #region Expose an event to support save function by ENTER key
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                this.SelectAll();
            }
            KeyEventHandler handler = this.TextBoxExEnterKeyDown;
            if (handler != null && e.Key == Key.Enter)
            {
                if (IsOutOfRange())
                {
                    this.Background = Brushes.Red; //this.WarningColor
                    //this.ToolTip = this.MinValue + "-" + this.MaxValue;
                    this.ToolTip = this.Text;
                }
                else
                    handler(this, e);
            }
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                if (IsOutOfRange())
                {
                    this.Background = Brushes.Red;
                    //this.ToolTip = this.MinValue + "-" + this.MaxValue;
                    this.ToolTip = this.Text;
                }
                else
                {
                    SetBGColor(this);
                }
            }
        }
        #endregion

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            if (IsInitialized)
            {
                //text changed because of binding changed
                if (m_strOldValue != this.Text && this.IsFocused && AllowBackgroundChange)
                {
                    this.TextSaved = false;
                }
                else
                {
                    this.m_strOldValue = this.Text;
                    this.TextSaved = true;
                }
                double.TryParse(this.Text, out double controlValue);
                if (IsOutOfRange(controlValue))
                {
                    this.Background = Brushes.Red; //this.WarningColor
                    //this.ToolTip = this.MinValue + "-" + this.MaxValue;
                    this.ToolTip = this.Text;
                }
                else
                {
                    SetBGColor(this);
                    //this.ToolTip = this.MinValue + "-" + this.MaxValue;
                    this.ToolTip = this.Text;
                }
               

                if (EditBoxMode == EditBoxMode.FileName)
                {
                    Regex regex = new Regex("\\*|\\\\|\\/|\\?|\"|:|\\<|\\>|\\|");  //*:"<>?/\|
                    this.Text = regex.Replace(this.Text, String.Empty);
                }
            }

            if (IsScrollToEnd)
            {
                this.ScrollToEnd();
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.ClickCount == 2)
            {
                this.SelectAll();
            }
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            this.IsTextboxFocused = true;
        }

        private static void TextSavedChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            TextBoxEx m_txt = d as TextBoxEx;
            if (m_txt != null)
            {
                if (m_txt.IsOutOfRange())
                {
                    m_txt.Background = Brushes.Red;
                    //m_txt.ToolTip = m_txt.MinValue + "-" + m_txt.MaxValue;
                    m_txt.ToolTip = m_txt.Text;
                }
                else
                {
                    SetBGColor(m_txt);
                }
            }
        }

        /// <summary>
        /// Set the Background of textbox according to the TextSaved property
        /// </summary>
        private static void SetBGColor(TextBoxEx tb)
        {
            if (tb.TextSaved)
            {
                tb.m_strOldValue = tb.Text;
                if (tb.NormalColor != null)
                    tb.Background = tb.NormalColor;
            }
            else
            {
                if (tb.ChangedColor != null)
                    tb.Background = tb.ChangedColor;
            }
        }

        public void SaveText()
        {
            this.TextSaved = true;
        }

    }
}
