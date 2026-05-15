using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;
using System.Reflection;
using System.Windows;
using Aitex.Core.Account;
using MECF.Framework.Common.Properties;
using MECF.Framework.UI.Core.Properties;
using Autofac;
using Aitex.Core.UI.View.Common;
using MECF.Framework.UI.Core.Applications;

namespace Aitex.Core.UI.View.Frame
{
	public class ViewManager
	{
		public const string Culture_CN = "zh-CN";
		public const string Culture_EN = "en-US";

		public List<string> GetAllViewList
		{
			get { return _views == null ? null : _views.ViewIdList; }
		}
		public string SystemName { get; set; }
		public string UILayoutFile { get; set; }
		public string ViewAssembly { get; set; }
		public ImageSource SystemLogo { get; set; }
		public Window MainWindow { get { return _mainWindow; } }
		public event Action OnMainWindowLoaded;
		public static Account.Account LoginAccount;
		public bool MaxSizeShow { get; set; }

		UILayoutParser _views;
		ITopView _topView;
		BottomView _bottomView = new BottomView();
		CenterView _centerView = new CenterView();
		StandardFrameWindow _mainWindow;

		private bool _isLogoff;

		private string _culture = "en-US";

		public int PreferWidth { get; set; }
		public int PreferHeight { get; set; }
		public int PreferHeightTopPanel { get; set; }
		public int PreferHeightCenterPanel { get; set; }
		public int PreferHeightBottomPanel { get; set; }

		public ViewManager()
		{
			PreferWidth = 1920;
			PreferHeight = 1020;
			PreferHeightTopPanel = 130;
			PreferHeightCenterPanel = 800;
			PreferHeightBottomPanel = 90;
			MaxSizeShow = true;
		}

		public void InitWindow()
		{
			_views = new UILayoutParser(UILayoutFile);

			if (_views.PreferWidth > 0)
				PreferWidth = _views.PreferWidth;

			if (_views.PreferTopPanelHeight > 0)
				PreferHeightTopPanel = _views.PreferTopPanelHeight;
			if (_views.PreferCenterPanelHeight > 0)
				PreferHeightCenterPanel = _views.PreferCenterPanelHeight;
			if (_views.PreferBottomPanelHeight > 0)
				PreferHeightBottomPanel = _views.PreferBottomPanelHeight;
			PreferHeight = PreferHeightTopPanel + PreferHeightCenterPanel + PreferHeightBottomPanel;

			try
			{
				using (var scope = UiApplication.Instance.Container.BeginLifetimeScope())
				{
					var type = Assembly.Load(_views.TitleView.AssemblyName).GetType(_views.TitleView.ViewClass);
					_topView = scope.Resolve(type) as ITopView;
				}
			}
			catch (Exception )
			{
				throw new ApplicationException(string.Format("在程序集{0}中，没有找到{1}，请检查UILayout配置文件中的设置", ViewAssembly, _views.TitleView.ViewClass));
			}
			UserControl uc = (UserControl)_topView;

			_bottomView.CreateMenu(_views.NavigationView);
			_bottomView.ButtonClicked += new Action<string>(_bottomView_ButtonClicked);

			_centerView.CreateView(_views.NavigationView);
		}

		void _bottomView_ButtonClicked(string obj)
		{
			UpdateSelection(obj, "");
		}

		public void ShowMainWindow(bool visible)
		{
			InitWindow();

			_mainWindow = new StandardFrameWindow()
			{
				TopView = _topView as UserControl,
				BottomView = _bottomView,
				CenterView = _centerView,
				Icon = SystemLogo,
				Title = SystemName,
				WindowState = MaxSizeShow ? WindowState.Maximized : WindowState.Normal,
			};

			_mainWindow.CenterGrid.Width = PreferWidth;
			_mainWindow.CenterGrid.Height = PreferHeight;
			_mainWindow.TopRow.Height = new GridLength(PreferHeightTopPanel);
			_mainWindow.CenterRow.Height = new GridLength(PreferHeightCenterPanel);
			_mainWindow.BottomRow.Height = new GridLength(PreferHeightBottomPanel);

			_centerView.Height = PreferHeightCenterPanel;

			_bottomView.Height = PreferHeightBottomPanel;

			_mainWindow.UpdateLayout();

			UpdateSelection(_views.NavigationView[0].Id, "");
			_mainWindow.Closing += new System.ComponentModel.CancelEventHandler(_mainWindow_Closing);
			_mainWindow.Loaded += new RoutedEventHandler(_mainWindow_Loaded);

			if (visible)
				_mainWindow.Show();
        }

		public void Logoff()
		{
			_isLogoff = true;

			_mainWindow.Close();
		}

		void _mainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (OnMainWindowLoaded != null)
				OnMainWindowLoaded();
		}

		void _mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (_isLogoff)
			{
				e.Cancel = false;
				System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

				Application.Current.Shutdown();

				return;
			}

			e.Cancel = !Exit();
		}

		bool Exit()
		{
			return MessageBox.Show("Are you sure you want to exit system?", SystemName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
		}

		public void UpdateSelection(string navigationId, string subviewId)
		{
			if (navigationId == "exit")
			{
				_mainWindow.Close();
				return;
			}
			_bottomView.SetSelection(navigationId);

			_centerView.SetSelection(navigationId);

			_topView.SetTitle(_centerView.GetCurrentViewName(_culture));
		}

		public UserControl FindView(string id)
		{
			return _centerView.GetView(id);
		}

		public TabItem FindTab(string id)
		{
			return _centerView.GetTab(id);
		}

		public void SetViewPermission(Account.Account account)
		{
			UserControl userControl;
			TabItem tabItem;

			LoginAccount = account;

			foreach (var dic in account.Permission)
			{
				userControl = FindView(dic.Key);
				tabItem = FindTab(dic.Key);

				if (userControl == null)
					continue;

				switch (dic.Value)
				{
					case ViewPermission.FullyControl:
					case ViewPermission.ProcessOPControl:
						userControl.Visibility = Visibility.Visible;
						userControl.IsEnabled = true;
						break;
					case ViewPermission.PartlyControl:
						userControl.Visibility = Visibility.Visible;
						userControl.IsEnabled = true;
						break;
					case ViewPermission.Readonly:
						userControl.Visibility = Visibility.Visible;
						userControl.IsEnabled = dic.Key == "recipe" ? true : false;
						break;
					case ViewPermission.Invisiable:
						userControl.Visibility = Visibility.Hidden;
						tabItem.Visibility = Visibility.Hidden;
						tabItem.Width = 0;
						break;
				}
			}

			foreach (ViewItem item in _views.NavigationView)
			{
				bool enable = false;
				foreach (ViewItem sub in item.SubView)
				{
					foreach (var dic in account.Permission)
					{
						if (dic.Key == sub.Id && dic.Value != ViewPermission.Invisiable)
						{
							enable = true;
							break;
						}
					}
				}
				_bottomView.Enable(item.Id, enable);
			}

		}

		public void SetCulture(string culture)
		{
			_culture = culture;

			if (_topView != null)
				_topView.SetTitle(_centerView.GetCurrentViewName(_culture));

			_centerView.SetCulture(culture);

			_bottomView.SetCulture(culture);

			UpdateCultureResource(culture);
		}

		private void UpdateCultureResource(string culture)
		{
			//string culture = language == 2 ? "zh-CN" : "en-US";

			//Copy all MergedDictionarys into a auxiliar list.
			var dictionaryList = Application.Current.Resources.MergedDictionaries.ToList();

			//Search for the specified culture.     
			string requestedCulture = string.Format(@"/MECF.Framework.Common;component/Resources/Language/StringResources.{0}.xaml", culture);
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
				Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
				Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
			}

			//Inform the threads of the new culture.     
			Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
			Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);

		}
	}
}
