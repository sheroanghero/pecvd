using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Aitex.Core.Account;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.UI.MVVM;
using Aitex.Core.UI.View.Common;
using Aitex.Core.UI.View.Frame;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using Aitex.Core.WCF;
using Autofac;
using MECF.Framework.UI.Core.Accounts;

namespace MECF.Framework.UI.Core.Applications
{
    public class UiApplication : Singleton<UiApplication>
    {
        public Action<string> CultureChanged;

        public string Culture
        {
            get; private set;
        }
        public IUiInstance Current
        {
            get { return _instance; }
        }

        public event Action OnWindowsLoaded;

        private IUiInstance _instance;
        private ViewManager _views;
        static ThreadExceptionEventHandler ThreadHandler = new ThreadExceptionEventHandler(Application_ThreadException);
        //private Guid _clientId = new Guid();

        private IContainer container;
        private ContainerBuilder containerBuilder;

        public ContainerBuilder ContainerBuilder
        {
            get => containerBuilder;
        }

        public IContainer Container
        {
            get => container;
        }

        protected System.Windows.Application application { get; set; }

        public UiApplication()
        {
            containerBuilder = new ContainerBuilder();
        }

        public void Initialize(IUiInstance instance)
        {
            application = System.Windows.Application.Current;
            application.DispatcherUnhandledException += OnUnhandledException;

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

            //Because this is a static event, you must detach your event handlers when your application is disposed, or memory leaks will result.
            Application.ThreadException += ThreadHandler;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            _instance = instance;

            var asms = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.GlobalAssemblyCache).ToArray();
            containerBuilder.RegisterAssemblyTypes(asms).Where(x =>
            {
                return typeof(IBaseView).IsAssignableFrom(x);
            })
            .PublicOnly()
            .AsSelf();

            containerBuilder.RegisterAssemblyTypes(asms).Where(x =>
            {
                return typeof(IBaseModel).IsAssignableFrom(x);
            })
            .PublicOnly()
            .As(t => t.GetInterfaces().First(x => x != typeof(IBaseModel) && typeof(IBaseModel).IsAssignableFrom(x)));

            Init();
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

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Application.ThreadException -= ThreadHandler;
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
            try
            {
                MessageBox.Show(string.Format("{0} UI Inner Exception, {1}", UiApplication.Instance.Current.SystemName, ex.Message));
            }
            finally
            {
                //Environment.Exit(0);
            }
        }

        public void Terminate()
        {
            EventClient.Instance.Stop();

            Singleton<LogManager>.Instance.Terminate();
        }

        public bool Init()
        {

            try
            {
                Singleton<LogManager>.Instance.Initialize();

                //EventClient.Instance.Init();

                //_event = new UiEvent();

                //SystemConfigManager.Instance.Initialize();

                //

                //SelectCulture(CultureSupported.English);

                //WcfClient.Instance.Initialize();

                //object language;
                //int i = 0;
                //do
                //{
                //    language = WCF.Query.GetConfig(SCName.System_Language);
                //    i++;
                //    if (i == 100)
                //        break;
                //    Thread.Sleep(500);
                //} while (language == null);

                container = containerBuilder.Build();

                _views = new ViewManager()
                {
                    SystemName = _instance.SystemName,
                    SystemLogo = _instance.MainIcon,
                    UILayoutFile = _instance.LayoutFile,
                    MaxSizeShow = _instance.MaxSizeShow,
                };

                //SetCulture((language != null && (int)(language) == 2) ? "zh-CN" : "en-US");


                //try
                {
                    _views.OnMainWindowLoaded += views_OnMainWindowLoaded;
                    _views.ShowMainWindow(!_instance.EnableAccountModule);



                    if (_instance.EnableAccountModule)
                    {
                        AccountClient.Instance.Service.RegisterViews(_views.GetAllViewList);
                        if (_views.SystemName == "GonaSorterUI")
                        {
                            GonaMainLogin mainLogin = new GonaMainLogin();

                            if (mainLogin.ShowDialog() == true)
                            {
                                //Account account = SorterUiSystem.Instance.WCF.Account.GetAccountInfo(mainLogin.UserName).AccountInfo;
                                //_views.SetViewPermission(account);
                                Account account = AccountClient.Instance.Service.GetAccountInfo(mainLogin.textBoxUserName.Text).AccountInfo;
                                _views.SetViewPermission(account);
                                _views.MainWindow.Show();
                            }
                        }
                        else
                        {
                            MainLogin mainLogin = new MainLogin();
                            if (Debugger.IsAttached)
                            {
                                Account account = AccountClient.Instance.Service.GetAccountInfo("admin").AccountInfo;
                                _views.SetViewPermission(account);
                                _views.MainWindow.Show();
                                return true;
                            }

                            if (mainLogin.ShowDialog() == true)
                            {
                                //Account account = SorterUiSystem.Instance.WCF.Account.GetAccountInfo(mainLogin.UserName).AccountInfo;
                                //_views.SetViewPermission(account);
                                Account account = AccountClient.Instance.Service.GetAccountInfo(mainLogin.textBoxUserName.Text).AccountInfo;
                                _views.SetViewPermission(account);
                                _views.MainWindow.Show();
                            }
                        }
                    }
                }



            }
            catch (Exception ex)
            {
                MessageBox.Show(_instance.SystemName + "Initialize failed, " + ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        void views_OnMainWindowLoaded()
        {
            if (OnWindowsLoaded != null)
                OnWindowsLoaded();

        }

        public void SetSelection(string nav, string sub)
        {
 
            _views.UpdateSelection(nav, sub);
        }

        public void Logoff()
        {
            if (Current.EnableAccountModule)
            {
                AccountClient.Instance.Service.Logout(AccountClient.Instance.CurrentUser.AccountId);
            }
            _views.Logoff();
        }
        public void SetCulture(string culture)
        {
            SelectCulture(culture);

            _views.SetCulture(culture);

            Culture = culture;

            if (CultureChanged != null)
            {
                CultureChanged(culture);
            }
        }

        void SelectCulture(string culture)
        {
            //string culture = language == 2 ? "zh-CN" : "en-US";

            //Copy all MergedDictionarys into a auxiliar list.
            var dictionaryList = System.Windows.Application.Current.Resources.MergedDictionaries.ToList();

            //Search for the specified culture.     
            string requestedCulture = string.Format(@"pack://application:,,,/Aitex.Sorter.UI;component/Resources/StringResources.{0}.xaml", culture);
            var resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == requestedCulture);

            if (resourceDictionary == null)
            {
                //If not found, select our default language.             
                requestedCulture = "StringResources.xaml";
                resourceDictionary = dictionaryList.
                    FirstOrDefault(d => d.Source.OriginalString == requestedCulture);
            }

            //If we have the requested resource, remove it from the list and place at the end.     
            //Then this language will be our string table to use.      
            if (resourceDictionary != null)
            {
               System.Windows.Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
               System.Windows.Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
            }

            //Inform the threads of the new culture.     
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);

        }

        //退出登录，UI界面仍然显示，所有页面改为只读
        public void Logout()
        {
            if (Current.EnableAccountModule)
            {
                AccountClient.Instance.Service.Logout(AccountClient.Instance.CurrentUser.AccountId);
            }

            SerializableDictionary<string, ViewPermission> PermissionDictionary = new SerializableDictionary<string, ViewPermission>();

            foreach (var pair in AccountClient.Instance.Service.GetAllViewList())
            {
                PermissionDictionary[pair.Key] = ViewPermission.Readonly;
            }

            Account account = new Account()
            {
                AccountId = "",
                Role = "",
                AccountStatus = true,
                Permission = PermissionDictionary
            };
            AccountClient.Instance.CurrentUser = account;
            _views.SetViewPermission(account);
        }

        public void Relogin()
        {
            if (_instance.EnableAccountModule)
            {
                MainLogin mainLogin = new MainLogin();
                mainLogin.IsLogOff = true;

                if (mainLogin.ShowDialog() == true)
                {

                    Account account = AccountClient.Instance.Service.GetAccountInfo(mainLogin.textBoxUserName.Text).AccountInfo;
                    _views.SetViewPermission(account);
                }
            }
        }

    }
}
