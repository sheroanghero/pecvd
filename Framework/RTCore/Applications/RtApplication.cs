using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Aitex.Core.Backend;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Common.NotifyTrayIcons;
using MECF.Framework.RT.Core.Backend;

namespace MECF.Framework.RT.Core.Applications
{
    public class RtApplication: Singleton<RtApplication>
    {
 
        static ThreadExceptionEventHandler ThreadHandler = new ThreadExceptionEventHandler(Application_ThreadException);

        private ShowWindowNotifyIcon _trayIcon;

        private IRtInstance _instance;

        private static bool _ignoreError;

        public static BackendMainView MainView { get; set; }

        private PassWordView _loginWindow = new PassWordView();

        private static Mutex _mutex;

        protected System.Windows.Application application { get; set; }

        public void Initialize(IRtInstance instance)
        {
            AdminRelauncher.RelaunchIfNotAdmin();

            application = System.Windows.Application.Current;
            application.DispatcherUnhandledException += OnUnhandledException;

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

            //Because this is a static event, you must detach your event handlers when your application is disposed, or memory leaks will result.
            System.Windows.Forms.Application.ThreadException += ThreadHandler;
            System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            _instance = instance;

            CheckAnotherInstanceRunning(instance.SystemName);

            MainView = new BackendMainView();
            MainView.Icon = _instance.TrayIcon;
			MainView.Title = _instance.SystemName + " Console";

            _loginWindow.Title = _instance.SystemName + " RT Backend Login";
            _loginWindow.Icon = _instance.TrayIcon;
            _ignoreError = _instance.KeepRunningAfterUnknownException;

            if (_instance.EnableNotifyIcon)
            {
                _trayIcon = new ShowWindowNotifyIcon(_instance.SystemName, _instance.TrayIcon);
				_trayIcon.ExitWindow += TrayIconExitWindow;
				_trayIcon.ShowMainWindow += TrayIconShowMainWindow;
				_trayIcon.ShowBallon(_instance.SystemName, "Start running");
            }

            InitSystem();

            if (_instance.DefaultShowBackendWindow)
            {
                MainView.Show();
            }
        }

        protected void OnUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                LOG.Write(e.Exception);
                if (e.Exception.InnerException != null)
                    LOG.Write(e.Exception.InnerException);
            }

            e.Handled = true;
        }

        void CheckAnotherInstanceRunning(string appName)
        {
            _mutex = new Mutex(true, appName, out bool createNew);

            if (!createNew)
            {
                if (!_mutex.WaitOne(1000))
                {
                    string msg = $"{appName} is already running，can not start again";

                    System.Windows.MessageBox.Show(msg, $"{appName} Launch Failed", System.Windows.MessageBoxButton.OK,  MessageBoxImage.Warning, System.Windows.MessageBoxResult.No,
                        System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                    Environment.Exit(0);
                }
            }
        }

 

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.ThreadException -= ThreadHandler;
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ShowMessageDialog(e.Exception);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowMessageDialog((Exception)e.ExceptionObject);
        }

        static void ShowMessageDialog(Exception ex)
        {
            LOG.Write(ex);
            if (!_ignoreError)
            {
                string message = string.Format(" RT unknown exception：{0},\n", DateTime.Now);
                System.Windows.MessageBox.Show(message + ex.Message, "Unexpected exception", System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Exclamation,
                    System.Windows.MessageBoxResult.No,
                    System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                //Environment.Exit(0);
            }

        }

        private void TrayIconExitWindow()
        {
            if (System.Windows.MessageBox.Show("Are you sure you want to exit system?", _instance.SystemName, System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Exclamation,
                    System.Windows.MessageBoxResult.No,
                    System.Windows.MessageBoxOptions.DefaultDesktopOnly) == System.Windows.MessageBoxResult.Yes)
            {
                if (_trayIcon != null)
                {
                    _trayIcon.ShowBallon(_instance.SystemName, "Stop running");
                }

                Terminate();
            }
        }

        private void TrayIconShowMainWindow(bool show)
        {
            if (MainView == null)
                return;

            if (show )
            {
                if (!MainView.IsVisible)
                {
                    if (!_loginWindow.IsVisible)
                    {
                        _loginWindow.Reset();

                        _loginWindow.ShowDialog();

                        if (_loginWindow.VerificationResult) 
                            MainView.Show();
                    }
                }
                else
                {
                    MainView.Show();
                }
            }
            else
            {
                MainView.Hide();
            }
        }


        private void InitSystem()
        {
            if (_instance.Loader != null)
            {
                _instance.Loader.Initialize();
            }
        }

        public void Terminate()
        {
            if (_instance.Loader != null)
            {
                _instance.Loader.Terminate();
            }
 
            if (_trayIcon != null)
            {
                _trayIcon.Dispose();
            }
 
            if (MainView != null)
            {
                MainView.Close();
            }

            System.Windows.Application.Current.Shutdown(0);

            Environment.Exit(0);
        }
    }
}
