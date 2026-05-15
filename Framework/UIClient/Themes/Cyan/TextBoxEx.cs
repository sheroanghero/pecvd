using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.ClientViews.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MECF.Framework.UI.Client.Themes.Cyan
{
    public partial class TextBoxEx : ResourceDictionary
    {
        public bool EnablePopupKeyboard { get; set; }
        public NumberKeyboard numberKeyboard;
        public FullKeyboard fullKeyboard;
        public string textValue { get; set; }
        public string textType { get; set; }
        public bool IsOpen { get; set; }

        public bool IsMouseLeave { get; set; } = false;
        public bool IsNumberOpen { get; set; }

        public Point MousePosition { get; set; }

        /// 引用user32.dll动态链接库（windows api），
        /// 使用库中定义 API：SetCursorPos 
        /// </summary>
        [DllImport("user32.dll")]
        private static extern int SetCursorPos(int x, int y);

        public void OnClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox)
            {
                TextBox textBox = sender as TextBox;
                textType = "TextBox";
                string strRet = Show(sender, textBox.Text, textBox);
                if (string.IsNullOrEmpty(strRet)) return;
                textBox.Text = strRet;
            }
            else if (sender is PasswordBox)
            {
                PasswordBox passwordBox = sender as PasswordBox;
                textType = "PasswordBox";
                string strRet = Show(sender, passwordBox.Password, passwordBox);
                if (string.IsNullOrEmpty(strRet)) return;
                passwordBox.Password = strRet;
            }

        }

        private void FullKeyboard_TouchLeave(object sender, TouchEventArgs e)
        {
            fullKeyboard.Close();
        }

        private void FullKeyboard_MouseLeave(object sender, MouseEventArgs e)
        {
            if (IsOpen)
            {
                fullKeyboard.CaptureMouse();
            }
        }



        private void NumberKeyboard_MouseLeave(object sender, MouseEventArgs e)
        {
            if (IsNumberOpen)
            {
                numberKeyboard.CaptureMouse();
            }
        }

        private void NumberKeyboard_TouchLeave(object sender, TouchEventArgs e)
        {
            numberKeyboard.Close();
        }

        /// <summary>
        /// 移动鼠标到指定的坐标点
        /// </summary>
        public void MoveMouseToPoint(Point p)
        {
            SetCursorPos((int)p.X, (int)p.Y);
        }

        private string Show(object sender, string strDefaultValue, object tbText = null)
        {
            Control control = sender as Control;
            string strRet = string.Empty;
            EnablePopupKeyboard = (bool)QueryDataClient.Instance.Service.GetConfig($"System.EnablePopupKeyboard");
            if (!EnablePopupKeyboard) { return string.Empty; }
            if (control.Tag != null && control.Tag.ToString().Equals("Number"))
            {
                var point = control.PointFromScreen(new Point(0, 0));
                double x = SystemParameters.WorkArea.Width;
                double y = SystemParameters.WorkArea.Height;
                numberKeyboard = new NumberKeyboard(textValue, strDefaultValue, tbText as TextBox);

                //numberKeyboard.TouchLeave += NumberKeyboard_TouchLeave;
                //numberKeyboard.MouseLeave += NumberKeyboard_MouseLeave;
                if (-point.Y + control.ActualHeight + 5 + numberKeyboard.Height < y)
                {
                    numberKeyboard.Top = -point.Y + control.ActualHeight + 5;
                }
                else
                {
                    numberKeyboard.Top = -point.Y - numberKeyboard.Height - 5;
                }
                if (-point.X + numberKeyboard.Width < x)
                {
                    numberKeyboard.Left = -point.X;
                }
                else
                {
                    numberKeyboard.Left = -point.X - (numberKeyboard.Width - control.ActualWidth);
                }
                Point centerP = new Point(numberKeyboard.Left + 100, numberKeyboard.Top + 100);
                MoveMouseToPoint(centerP);
                //numberKeyboard.Focusable = true;

                numberKeyboard.Show();
                IsNumberOpen = true;
                numberKeyboard.PreviewMouseDown += NumberKeyboard_PreviewMouseDown; ;
                numberKeyboard.Closing += NumberKeyboard_Closed;
                numberKeyboard.MouseLeave += NumberKeyboard_MouseLeave;
                numberKeyboard.CaptureMouse();
                //control.Focusable = true;
                //if ((bool)numberKeyboard.ShowDialog()) strRet = numberKeyboard.ValueString;
            }
            else if (control.Tag != null && control.Tag.ToString().Contains("None"))
            {
                return string.Empty;
            }
            else
            {
                //fullKeyboard = new FullKeyboard(textValue, strDefaultValue, tbText as TextBox);
                if (textType == "TextBox")
                    fullKeyboard = new FullKeyboard(textValue, strDefaultValue, tbText as TextBox);
                else
                    fullKeyboard = new FullKeyboard(textValue, strDefaultValue, tbText as PasswordBox);
                //fullKeyboard.MouseLeave += FullKeyboard_MouseLeave;
                //fullKeyboard.TouchLeave += FullKeyboard_TouchLeave;
                var point = control.PointFromScreen(new Point(0, 0));
                double x = SystemParameters.WorkArea.Width;
                double y = SystemParameters.WorkArea.Height;
                if (-point.Y + control.ActualHeight + 5 + fullKeyboard.Height < y)
                {
                    fullKeyboard.Top = -point.Y + control.ActualHeight + 5;
                }
                else
                {
                    fullKeyboard.Top = -point.Y - fullKeyboard.Height - 5;
                }
                if (-point.X + fullKeyboard.Width < x)
                {
                    fullKeyboard.Left = -point.X;
                }
                else
                {
                    fullKeyboard.Left = -point.X - (fullKeyboard.Width - control.ActualWidth);
                }
                Point centerP = new Point(fullKeyboard.Left + 100, fullKeyboard.Top + 100);
                MoveMouseToPoint(centerP);
                fullKeyboard.Show();

                IsOpen = true;
                fullKeyboard.PreviewMouseDown += FullKeyboard_PreviewMouseDown;
                fullKeyboard.Closed += FullKeyboard_Closed;
                fullKeyboard.MouseLeave += FullKeyboard_MouseLeave;
                fullKeyboard.CaptureMouse();
                //control.Focusable = true;
                //fullKeyboard.PreviewLostKeyboardFocus += FullKeyboard_PreviewLostKeyboardFocus;
                //if ((bool)fullKeyboard.ShowDialog()) strRet = fullKeyboard.ValueString;
            }
            return strRet;
        }


        private void FullKeyboard_Closed(object sender, EventArgs e)
        {
            if (IsOpen)
            {
                IsOpen = false;
                fullKeyboard.ReleaseMouseCapture();
            }
        }

        private void FullKeyboard_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pp = Mouse.GetPosition(e.Source as FrameworkElement);
            if (pp.X != 0 && pp.Y != 0)
            {
                Point ppp = (e.Source as FrameworkElement).PointToScreen(pp);

                if (ppp.X < fullKeyboard.Left || ppp.X > fullKeyboard.Left + fullKeyboard.Width)
                {
                    fullKeyboard.Close();
                }
                else if (ppp.Y < fullKeyboard.Top || ppp.Y > fullKeyboard.Top + fullKeyboard.Height)
                {
                    fullKeyboard.Close();
                }
                else
                {
                    fullKeyboard.ReleaseMouseCapture();
                }

            }
        }

        private void NumberKeyboard_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pp = Mouse.GetPosition(e.Source as FrameworkElement);
            if (pp.X != 0 && pp.Y != 0)
            {
                Point ppp = (e.Source as FrameworkElement).PointToScreen(pp);

                if (ppp.X < numberKeyboard.Left || ppp.X > numberKeyboard.Left + numberKeyboard.Width)
                {
                    numberKeyboard.Close();
                }
                else if (ppp.Y < numberKeyboard.Top || ppp.Y > numberKeyboard.Top + numberKeyboard.Height)
                {
                    numberKeyboard.Close();
                }
                else
                {
                    numberKeyboard.ReleaseMouseCapture();
                }

            }
        }

        private void NumberKeyboard_Closed(object sender, EventArgs e)
        {
            if (IsNumberOpen)
            {
                IsNumberOpen = false;
                numberKeyboard.ReleaseMouseCapture();
            }
        }


        // private void NumberKeyboard_MouseLeave(object sender, MouseEventArgs e)
        // {
        // numberKeyboard.Close();
        //}

        //private void NumberKeyboard_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        //{
        //    Point pp = Mouse.GetPosition(e.Source as FrameworkElement);
        //    if (pp.X != 0 && pp.Y != 0)
        //    {
        //        Point ppp = (e.Source as FrameworkElement).PointToScreen(pp);
        //        if (IsNumberOpen == false)
        //        {
        //            if (ppp.X < numberKeyboard.Left || ppp.X > numberKeyboard.Left + numberKeyboard.Width)
        //                numberKeyboard.Close();
        //            if (ppp.Y < numberKeyboard.Top || ppp.Y > numberKeyboard.Top + numberKeyboard.Height)
        //                numberKeyboard.Close();
        //        }
        //        IsNumberOpen = false;
        //    }
        //}

        //private void FullKeyboard_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        //{
        //    Point pp = Mouse.GetPosition(e.Source as FrameworkElement);
        //    if (pp.X != 0 && pp.Y != 0)
        //    {
        //        Point ppp = (e.Source as FrameworkElement).PointToScreen(pp);
        //        if (IsOpen == false)
        //        {
        //            if (ppp.X < fullKeyboard.Left || ppp.X > fullKeyboard.Left + fullKeyboard.Width)
        //                fullKeyboard.Close();
        //            if (ppp.Y < fullKeyboard.Top || ppp.Y > fullKeyboard.Top + fullKeyboard.Height)
        //                fullKeyboard.Close();
        //        }
        //        IsOpen = false;
        //    }
        //}
    }
}

