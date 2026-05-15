using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Aitex.Core.RT.SCCore;
using Aitex.Core.UI.MVVM;

namespace MECF.Framework.RT.Core.Backend
{
    public class NotifiableSCConfigItem : SCConfigItem, INotifyPropertyChanged
    {
        public string SetPoint { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void InvokePropertyChanged()
        {
            PropertyInfo[] ps = this.GetType().GetProperties();
            foreach (PropertyInfo p in ps)
            {
                InvokePropertyChanged(p.Name);

                if (p.PropertyType == typeof(ICommand))
                {
                    DelegateCommand<string> cmd = p.GetValue(this, null) as DelegateCommand<string>;
                    if (cmd != null)
                        cmd.RaiseCanExecuteChanged();

                }
            }

            FieldInfo[] fi = this.GetType().GetFields();
            foreach (FieldInfo p in fi)
            {
                InvokePropertyChanged(p.Name);

                if (p.FieldType == typeof(ICommand))
                {
                    DelegateCommand<string> cmd = p.GetValue(this) as DelegateCommand<string>;
                    if (cmd != null)
                        cmd.RaiseCanExecuteChanged();

                }
            }
        }
    }

    public class BackendSCConfigViewModel : ViewModelBase
    {
        private List<SCConfigItem> _scItems;

        public ObservableCollection<NotifiableSCConfigItem> ScItemList { get; set; }

        public ICommand SetScCommand { get; set; }
        public ICommand ReloadCommand { get; set; }

        public BackendSCConfigViewModel()
        {
            Init();

            SetScCommand = new DelegateCommand<string>(SetSc);
            ReloadCommand = new DelegateCommand<string>(Reload);
        }

        private void SetSc(string obj)
        {
            NotifiableSCConfigItem item = ScItemList.First(x => x.PathName == obj);

            if (item.Type != SCConfigType.String.ToString() && string.IsNullOrEmpty(item.SetPoint))
                return;

            SC.SetItemValueFromString(obj, item.SetPoint == null ? "" : item.SetPoint);

            item.BoolValue = SC.GetConfigItem(obj).BoolValue;
            item.IntValue = SC.GetConfigItem(obj).IntValue;
            item.StringValue = SC.GetConfigItem(obj).StringValue;
            item.DoubleValue = SC.GetConfigItem(obj).DoubleValue;
            item.InvokePropertyChanged(nameof(item.Value));
        }

        public void Reload(string obj)
        {
            foreach (var item in ScItemList)
            {
                item.BoolValue = SC.GetConfigItem(item.PathName).BoolValue;
                item.IntValue = SC.GetConfigItem(item.PathName).IntValue;
                item.StringValue = SC.GetConfigItem(item.PathName).StringValue;
                item.DoubleValue = SC.GetConfigItem(item.PathName).DoubleValue;
                item.InvokePropertyChanged(nameof(item.Value));
            }
        }

        void Init()
        {
            if (_scItems != null)
                return;
            {
                _scItems = SC.GetItemList();


                ScItemList = new ObservableCollection<NotifiableSCConfigItem>();
                foreach (var scItem in _scItems)
                {
 
                        ScItemList.Add(new NotifiableSCConfigItem()
                        {
                            BoolValue = scItem.BoolValue,
                            Default = scItem.Default,
                            Description = scItem.Description,
                            DoubleValue = scItem.DoubleValue,
                            IntValue = scItem.IntValue,
                            Max = scItem.Max,
                            Min = scItem.Min,
                            Name = scItem.Name,
                            Type = scItem.Type,
                            Tag = scItem.Tag,
                            Parameter = scItem.Parameter,
                            Path = scItem.Path,
                            SetPoint = scItem.Value.ToString(),
                            StringValue = scItem.StringValue,
                             
                        });
                    
                }


            }
        }
    }

    /// <summary>
    /// SCConfigView.xaml 的交互逻辑
    /// </summary>
    public partial class BackendSCConfigView : UserControl
    {

        public BackendSCConfigView()
        {
            InitializeComponent();

            this.Loaded += SCConfigView_Loaded;

            this.IsVisibleChanged += BackendSCConfigView_IsVisibleChanged;
        }

        private void BackendSCConfigView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext == null)
            {
                DataContext = new BackendSCConfigViewModel();
            }

            (this.DataContext as BackendSCConfigViewModel).Reload(null);
        }

        private void SCConfigView_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null)
            {
                DataContext = new BackendSCConfigViewModel();
            }
 
        }
 
    }
}
