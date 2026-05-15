using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Interop;

namespace OpenSEMI.Ctrlib.Window
{
    public class CustomWnd : System.Windows.Window
    {
        private HwndSource curHwndSource = null;
        private HwndSourceHook curHwndSourceHook = null;

        static CustomWnd()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomWnd), new FrameworkPropertyMetadata(typeof(CustomWnd)));
        }

        public CustomWnd()
        {
            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, CloseWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, MaximizeWindow, CanResizeWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, MinimizeWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, RestoreWindow, CanResizeWindow));
        }

        #region Window event
        private void CanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResizeMode == ResizeMode.CanResize || ResizeMode == ResizeMode.CanResizeWithGrip;
        }

        private void RestoreWindow(object sender, ExecutedRoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Normal;
        }

        private void MinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void MaximizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Maximized;
        }

        private void CloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }
        #endregion

        #region hook
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 获取窗体句柄
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

            curHwndSource = HwndSource.FromHwnd(hwnd);
            if (curHwndSource == null)
            {
                return;
            }

            curHwndSourceHook = new HwndSourceHook(this.WndProc);
            curHwndSource.AddHook(curHwndSourceHook);
        }

        /// <summary>
        /// 系统消息处理
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr result = IntPtr.Zero;
            int message = (int)msg;
            switch (message)
            {
                case 0x001A://0x001A,WM_SETTINGCHANGE


                    break;
                default:
                    handled = false;
                    break;
            }

            return result;
        }
        #endregion
    }
}
