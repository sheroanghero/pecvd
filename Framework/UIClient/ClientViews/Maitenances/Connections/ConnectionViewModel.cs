using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.UI.MVVM;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Maitenances.Connections
{
    public class ConnectionViewModel : UiViewModelBase, ISupportMultipleSystem
    {

        public string SystemName { get; set; }



        protected override void OnInitialize()
        {
            base.OnInitialize();

        }
        protected override void OnActivate()
        {
            base.OnActivate();

        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
        }


        public ObservableCollection<NotifiableConnectionItem> ListConnections { get; set; }

        public ICommand ConnectCommand { get; set; }
        public ICommand DisconnectCommand { get; set; }

        public ConnectionViewModel()
        {
            ListConnections = new ObservableCollection<NotifiableConnectionItem>();

            ConnectCommand = new DelegateCommand<string>(DoConnect);
            DisconnectCommand = new DelegateCommand<string>(DoDisconnect);
        }

        private void DoConnect(string obj)
        {
            if (DialogBox.Confirm($"Are you sure you want to connect {obj}?"))
            {
                InvokeClient.Instance.Service.DoOperation($"Connection.Connect", obj);
            }
        }
        private void DoDisconnect(string obj)
        {
            if (DialogBox.Confirm($"Are you sure you want to disconnect {obj}?"))
            {
                InvokeClient.Instance.Service.DoOperation($"Connection.Disconnect", obj);
            }
        }


        protected override void Poll()
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                var connections = QueryDataClient.Instance.Service.GetData("Connection.List");
                if (connections == null)
                    return;

                List<NotifiableConnectionItem> items = connections as List<NotifiableConnectionItem>;

                foreach (var item in items)
                {

                    if (ListConnections.Count == 0 || (ListConnections.FirstOrDefault(x => x.Name == item.Name) == null))
                    {
                        ListConnections.Add(item);
                    }
                    else
                    {
                        var find = ListConnections.First(x => x.Name == item.Name);
                        find.IsConnected = find.IsConnected;
                        find.InvokePropertyChanged("IsConnected");

                    }

                }

                InvokePropertyChanged("ListConnections");
            }));


        }
    }


    public class IoButton : ToggleButton
    {
        public static readonly DependencyProperty ONProperty;
        static IoButton()
        {
            ONProperty = DependencyProperty.Register("ON", typeof(bool), typeof(IoButton));
        }

        public bool ON
        {
            get { return (bool)GetValue(ONProperty); }
            set { SetValue(ONProperty, value); }
        }

    }

    public class BoolBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool? ret = (bool?)value;
            return ret.HasValue && ret.Value ? "LightBlue" : "Transparent";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;

        }
    }
}
