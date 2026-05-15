using System.Runtime.InteropServices;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;

namespace Caliburn.Micro {
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Linq;
    using System.Windows.Navigation;
    using Caliburn.Micro.Core;
    using System.Windows.Media;

    /// <summary>
    /// A service that manages windows.
    /// </summary>
    public interface IWindowManager {
        /// <summary>
        /// Shows a modal dialog for the specified model.
        /// </summary>
        /// <param name="rootModel">The root model.</param>
        /// <param name="context">The context.</param>
        /// <param name="settings">The optional dialog settings.</param>
        /// <returns>The dialog result.</returns>
        bool? ShowDialog(object rootModel, object context = null, IDictionary<string, object> settings = null);

        /// <summary>
        /// Shows a non-modal window for the specified model.
        /// </summary>
        /// <param name="rootModel">The root model.</param>
        /// <param name="context">The context.</param>
        /// <param name="settings">The optional window settings.</param>
        void ShowWindow(object rootModel, object context = null, IDictionary<string, object> settings = null);

        /// <summary>
        /// Shows a popup at the current mouse position.
        /// </summary>
        /// <param name="rootModel">The root model.</param>
        /// <param name="context">The view context.</param>
        /// <param name="settings">The optional popup settings.</param>
        void ShowPopup(object rootModel, object context = null, IDictionary<string, object> settings = null);
    }

    /// <summary>
    /// A service that manages windows.
    /// </summary>
    public class WindowManager : IWindowManager {
        /// <summary>
        /// Shows a modal dialog for the specified model.
        /// </summary>
        /// <param name="rootModel">The root model.</param>
        /// <param name="context">The context.</param>
        /// <param name="settings">The dialog popup settings.</param>
        /// <returns>The dialog result.</returns>
        public virtual bool? ShowDialog(object rootModel,  object context = null, IDictionary<string, object> settings = null){
            Window window = CreateWindow(rootModel, true, context, settings);
            window.ShowInTaskbar = false;
            window.ResizeMode = ResizeMode.NoResize;
            window.WindowStyle = WindowStyle.SingleBorderWindow;
            return window.ShowDialog();
        }

        public virtual bool? ShowDialog(object rootModel, Point position, object context = null, IDictionary<string, object> settings = null)
        {
            Window window = CreateWindow(rootModel, true, context, settings);
            window.ShowInTaskbar = false;
            window.ResizeMode = ResizeMode.NoResize;
            window.WindowStyle = WindowStyle.SingleBorderWindow;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Owner = Application.Current.MainWindow;
            window.Left = position.X;
            window.Top = position.Y;

            return window.ShowDialog();
        }

        public virtual bool? ShowDialog(object rootModel, Window owner, object context = null, IDictionary<string, object> settings = null)
        {
            Window window = CreateWindow(rootModel, true, context, settings);
            window.ShowInTaskbar = false;
            window.ResizeMode = ResizeMode.NoResize;
            window.WindowStyle = WindowStyle.SingleBorderWindow;
            window.Owner = owner;

            return window.ShowDialog();
        }

        public virtual bool? ShowDialog(object rootModel, WindowState pWindowState , object context = null, IDictionary<string, object> settings = null)
        {
            Window window = CreateWindow(rootModel, true, context, settings);
            window.ShowInTaskbar = false;
            window.ResizeMode = ResizeMode.CanResize;
            window.WindowStyle = WindowStyle.SingleBorderWindow;
            window.WindowState = pWindowState;
            return window.ShowDialog();
        }

        public virtual bool? ShowDialogWithNoWindow(object rootModel, object context = null, IDictionary<string, object> settings = null)
        {
            Window window = CreateWindow(rootModel, true, context, settings);
            window.ShowInTaskbar = false;
            window.ResizeMode = ResizeMode.NoResize;
            window.WindowStyle = WindowStyle.None;
            return window.ShowDialog();
        }

        /// <summary>
        /// Shows a special modal dialog for the specified model.
        /// </summary>
        /// <param name="rootModel">The root model.</param>
        /// <param name="context">The context.</param>
        /// <returns>The dialog result.</returns>
        public virtual bool? ShowDialogWithNoStyle(object rootModel, object context = null)
        {
            Window window = CreateWindow(rootModel, true, context);
            window.WindowStyle = WindowStyle.None;
            window.AllowsTransparency = true;
            window.ResizeMode = ResizeMode.NoResize;
            window.Background = new SolidColorBrush(Colors.Transparent);
            window.ShowInTaskbar = false;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Topmost = true;
            return window.ShowDialog();
        }
        /// <summary>
        /// Shows a special modal dialog for the specified model.
        /// </summary>
        /// <param name="rootModel">The root model.</param>
        /// <param name="context">The context.</param>
        /// <returns>The dialog result.</returns>
        public virtual bool? ShowDialogWithTitle(object rootModel, object context = null, string title = "")
        {
            Window window = CreateWindow(rootModel, true, context);
            window.ShowInTaskbar = false;
            window.ResizeMode = ResizeMode.NoResize;
            window.WindowStyle = WindowStyle.SingleBorderWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Title = title;
            return window.ShowDialog();
        }

        /// <summary>
        /// Creates a window.
        /// </summary>
        /// <param name="rootModel">The view model.</param>
        /// <param name="isDialog">Whethor or not the window is being shown as a dialog.</param>
        /// <param name="context">The view context.</param>
        /// <param name="createCompletedCallBack">Call back and return the new window when create window completed</param>
        /// <returns>The window.</returns>
        protected virtual Window CreateWindow(object rootModel, bool isDialog, object context, Action<Window> createCompletedCallBack = null)
        {
            var view = EnsureWindow(rootModel, ViewLocator.LocateForModel(rootModel, null, context), isDialog);

            ViewModelBinder.Bind(rootModel, view, context);

            var haveDisplayName = rootModel as IHaveDisplayName;
            if (haveDisplayName != null && !ConventionManager.HasBinding(view, Window.TitleProperty))
            {
                var binding = new Binding("DisplayName") { Mode = BindingMode.OneWay };
                view.SetBinding(Window.TitleProperty, binding);
            }

            if (createCompletedCallBack != null)
            {
                createCompletedCallBack(view);
            }

            new WindowConductor(rootModel, view);

            return view;
        }

        /// <summary>
        /// Shows a window for the specified model.
        /// </summary>
        /// <param name="rootModel">The root model.</param>
        /// <param name="context">The context.</param>
        /// <param name="settings">The optional window settings.</param>
        public virtual void ShowWindow(object rootModel, object context = null, IDictionary<string, object> settings = null){
            NavigationWindow navWindow = null;

            var application = Application.Current;
            if (application != null && application.MainWindow != null) {
                navWindow = application.MainWindow as NavigationWindow;
            }

            if(navWindow != null) {
                var window = CreatePage(rootModel, context, settings);
                navWindow.Navigate(window);
            }
            else {
                CreateWindow(rootModel, false, context, settings).Show();
            }
        }

        /// <summary>
        /// Shows a popup at the current mouse position.
        /// </summary>
        /// <param name="rootModel">The root model.</param>
        /// <param name="context">The view context.</param>
        /// <param name="settings">The optional popup settings.</param>
        public virtual void ShowPopup(object rootModel, object context = null, IDictionary<string, object> settings = null) {
            var popup = CreatePopup(rootModel, settings);
            var view = ViewLocator.LocateForModel(rootModel, popup, context);

            popup.Child = view;
            popup.SetValue(View.IsGeneratedProperty, true);

            ViewModelBinder.Bind(rootModel, popup, null);
			Action.SetTargetWithoutContext(view, rootModel);

            var activatable = rootModel as IActivate;
            if (activatable != null) {
                activatable.Activate();
            }

            var deactivator = rootModel as IDeactivate;
            if (deactivator != null) {
                popup.Closed += delegate { deactivator.Deactivate(true); };
            }

            popup.IsOpen = true;
            popup.CaptureMouse();
        }

        /// <summary>
        /// Creates a popup for hosting a popup window.
        /// </summary>
        /// <param name="rootModel">The model.</param>
        /// <param name="settings">The optional popup settings.</param>
        /// <returns>The popup.</returns>
        protected virtual Popup CreatePopup(object rootModel, IDictionary<string, object> settings) {
            var popup = new Popup();

            if (ApplySettings(popup, settings)) {
                if (!settings.ContainsKey("PlacementTarget") && !settings.ContainsKey("Placement"))
                    popup.Placement = PlacementMode.MousePoint;
                if (!settings.ContainsKey("AllowsTransparency"))
                    popup.AllowsTransparency = true;
            }else {
                popup.AllowsTransparency = true;
                popup.Placement = PlacementMode.MousePoint;
            }

            return popup;
        }

        /// <summary>
        /// Creates a window.
        /// </summary>
        /// <param name="rootModel">The view model.</param>
        /// <param name="isDialog">Whethor or not the window is being shown as a dialog.</param>
        /// <param name="context">The view context.</param>
        /// <param name="settings">The optional popup settings.</param>
        /// <returns>The window.</returns>
        protected virtual Window CreateWindow(object rootModel, bool isDialog, object context, IDictionary<string, object> settings) {
            var view = EnsureWindow(rootModel, ViewLocator.LocateForModel(rootModel, null, context), isDialog);
            ViewModelBinder.Bind(rootModel, view, context);

            var haveDisplayName = rootModel as IHaveDisplayName;
            if (haveDisplayName != null && !ConventionManager.HasBinding(view, Window.TitleProperty)) {
                var binding = new Binding("DisplayName") { Mode = BindingMode.TwoWay };
                view.SetBinding(Window.TitleProperty, binding);
            }

            ApplySettings(view, settings);

            new WindowConductor(rootModel, view);

            return view;
        }

        /// <summary>
        /// Makes sure the view is a window is is wrapped by one.
        /// </summary>
        /// <param name="model">The view model.</param>
        /// <param name="view">The view.</param>
        /// <param name="isDialog">Whethor or not the window is being shown as a dialog.</param>
        /// <returns>The window.</returns>
        protected virtual Window EnsureWindow(object model, object view, bool isDialog) {
            var window = view as Window;

            if (window == null) {
                window = new Window {
                    Content = view,
                    SizeToContent = SizeToContent.WidthAndHeight
                };

                window.SetValue(View.IsGeneratedProperty, true);

                var owner = InferOwnerOf(window);
                if (owner != null) {
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.Owner = owner;
                }
                else {
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            }
            else {
                var owner = InferOwnerOf(window);
                if (owner != null && isDialog) {
                    window.Owner = owner;
                }
            }

            return window;
        }

        /// <summary>
        /// Infers the owner of the window.
        /// </summary>
        /// <param name="window">The window to whose owner needs to be determined.</param>
        /// <returns>The owner.</returns>
        protected virtual Window InferOwnerOf(Window window) {
            var application = Application.Current;
            if (application == null) {
                return null;
            }

            var active = application.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
            active = active ?? (PresentationSource.FromVisual(application.MainWindow) == null ? null : application.MainWindow);
            return active == window ? null : active;
        }

        /// <summary>
        /// Creates the page.
        /// </summary>
        /// <param name="rootModel">The root model.</param>
        /// <param name="context">The context.</param>
        /// <param name="settings">The optional popup settings.</param>
        /// <returns>The page.</returns>
        public virtual Page CreatePage(object rootModel, object context, IDictionary<string, object> settings) {
            var view = EnsurePage(rootModel, ViewLocator.LocateForModel(rootModel, null, context));
            ViewModelBinder.Bind(rootModel, view, context);

            var haveDisplayName = rootModel as IHaveDisplayName;
            if (haveDisplayName != null && !ConventionManager.HasBinding(view, Page.TitleProperty)) {
                var binding = new Binding("DisplayName") { Mode = BindingMode.TwoWay };
                view.SetBinding(Page.TitleProperty, binding);
            }

            ApplySettings(view, settings);

            var activatable = rootModel as IActivate;
            if (activatable != null) {
                activatable.Activate();
            }

            var deactivatable = rootModel as IDeactivate;
            if (deactivatable != null) {
                view.Unloaded += (s, e) => deactivatable.Deactivate(true);
            }

            return view;
        }

        /// <summary>
        /// Ensures the view is a page or provides one.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="view">The view.</param>
        /// <returns>The page.</returns>
        protected virtual Page EnsurePage(object model, object view) {
            var page = view as Page;

            if(page == null) {
                page = new Page { Content = view };
                page.SetValue(View.IsGeneratedProperty, true);
            }

            return page;
        }

        bool ApplySettings(object target, IEnumerable<KeyValuePair<string, object>> settings) {
            if (settings != null) {
                var type = target.GetType();

                foreach (var pair in settings) {
                    var propertyInfo = type.GetProperty(pair.Key);

                    if (propertyInfo != null) {
                        propertyInfo.SetValue(target, pair.Value, null);
                    }
                }

                return true;
            }

            return false;
        }

        class WindowConductor {
            bool deactivatingFromView;
            bool deactivateFromViewModel;
            bool actuallyClosing;
            readonly Window view;
            readonly object model;

            public WindowConductor(object model, Window view) {
                this.model = model;
                this.view = view;

                var activatable = model as IActivate;
                if (activatable != null) {
                    activatable.Activate();
                }

                var deactivatable = model as IDeactivate;
                if (deactivatable != null) {
                    view.Closed += Closed;
                    deactivatable.Deactivated += Deactivated;
                }

                var guard = model as IGuardClose;
                if (guard != null) {
                    view.Closing += Closing;
                }
            }

            void Closed(object sender, EventArgs e) {
                view.Closed -= Closed;
                view.Closing -= Closing;

                if (deactivateFromViewModel) {
                    return;
                }

                var deactivatable = (IDeactivate)model;

                deactivatingFromView = true;
                deactivatable.Deactivate(true);
                deactivatingFromView = false;
            }

            void Deactivated(object sender, DeactivationEventArgs e) {
                if (!e.WasClosed) {
                    return;
                }

                ((IDeactivate)model).Deactivated -= Deactivated;

                if (deactivatingFromView) {
                    return;
                }

                deactivateFromViewModel = true;
                actuallyClosing = true;
                view.Close();
                actuallyClosing = false;
                deactivateFromViewModel = false;
            }

            void Closing(object sender, CancelEventArgs e) {
                if (e.Cancel) {
                    return;
                }

                var guard = (IGuardClose)model;

                if (actuallyClosing) {
                    actuallyClosing = false;
                    return;
                }

                bool runningAsync = false, shouldEnd = false;

                guard.CanClose(canClose => {
                    Execute.OnUIThread(() => {
                        if(runningAsync && canClose) {
                            actuallyClosing = true;
                            view.Close();
                        }
                        else {
                            e.Cancel = !canClose;
                        }

                        shouldEnd = true;
                    });
                });

                if (shouldEnd) {
                    return;
                }

                runningAsync = e.Cancel = true;
            }
        }
    }

 
    public static class WindowExtensions
    {
        #region Public Methods

        public static bool ActivateCenteredToMouse(this Window window)
        {
            ComputeTopLeft(ref window);
            return window.Activate();
        }

        public static void ShowCenteredToMouse(this Window window)
        {
            // in case the default start-up location isn't set to Manual
            WindowStartupLocation oldLocation = window.WindowStartupLocation;
            // set location to manual -> window will be placed by Top and Left property
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            ComputeTopLeft(ref window);
            window.ShowDialog();
            window.WindowStartupLocation = oldLocation;
        }

        #endregion

        #region Methods

        private static void ComputeTopLeft(ref Window window)
        {
            W32Point pt = new W32Point();
            if (!GetCursorPos(ref pt))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            // 0x00000002: return nearest monitor if pt is not contained in any monitor.
            IntPtr monHandle = MonitorFromPoint(pt, 0x00000002);
            W32MonitorInfo monInfo = new W32MonitorInfo();
            monInfo.Size = Marshal.SizeOf(typeof(W32MonitorInfo));

            if (!GetMonitorInfo(monHandle, ref monInfo))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            // use WorkArea struct to include the taskbar position.
            W32Rect monitor = monInfo.WorkArea;
            double offsetX = Math.Round(window.Width / 2);
            double offsetY = Math.Round(window.Height / 2);

            double top = pt.Y - offsetY;
            double left = pt.X - offsetX;

            Rect screen = new Rect(
                new Point(monitor.Left, monitor.Top),
                new Point(monitor.Right, monitor.Bottom));
            Rect wnd = new Rect(
                new Point(left, top),
                new Point(left + window.Width, top + window.Height));

            window.Top = wnd.Top;
            window.Left = wnd.Left;

            if (!screen.Contains(wnd))
            {
                if (wnd.Top < screen.Top)
                {
                    double diff = Math.Abs(screen.Top - wnd.Top);
                    window.Top = wnd.Top + diff;
                }

                if (wnd.Bottom > screen.Bottom)
                {
                    double diff = wnd.Bottom - screen.Bottom;
                    window.Top = wnd.Top - diff;
                }

                if (wnd.Left < screen.Left)
                {
                    double diff = Math.Abs(screen.Left - wnd.Left);
                    window.Left = wnd.Left + diff;
                }

                if (wnd.Right > screen.Right)
                {
                    double diff = wnd.Right - screen.Right;
                    window.Left = wnd.Left - diff;
                }
            }
        }

        #endregion

        #region W32 API

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(ref W32Point pt);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref W32MonitorInfo lpmi);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(W32Point pt, uint dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct W32Point
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct W32MonitorInfo
        {
            public int Size;
            public W32Rect Monitor;
            public W32Rect WorkArea;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct W32Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion
    }
}