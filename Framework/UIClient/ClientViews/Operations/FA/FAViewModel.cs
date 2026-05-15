using Aitex.Core.UI.MVVM;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.OperationCenter;
using OpenSEMI.ClientBase;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Aitex.Core.RT.Log;
using MECF.Framework.UI.Client.ClientBase;
using MessageBox = Xceed.Wpf.Toolkit.MessageBox;
using PageSCValue = MECF.Framework.UI.Core.View.Common.PageSCValue;

namespace MECF.Framework.UI.Client.CenterViews.Operations.FA
{

    public enum FACommunicationState
    {
        Disabled,
        Enabled,
        EnabledNotCommunicating,
        EnabledCommunicating,
        WaitCRA,
        WaitDelay,
        WaitCRFromHost,
    }

    public enum FAControlState
    {
        Unknown,
        EquipmentOffline,
        AttemptOnline,
        HostOffline,
        OnlineLocal,
        OnlineRemote,
    }

    public enum FAControlSubState
    {
        Local,
        Remote,
    }

    public enum FASpoolingState
    {
        Active = 1,

        Inactive
    }

    public class FACommandName
    {
        public const string FAEnable = "FAEnable";
        public const string FADisable = "FADisable";

        public const string FAOnline = "FAOnline";
        public const string FAOffline = "FAOffline";

        public const string FALocal = "FALocal";
        public const string FARemote = "FARemote";

        public const string FAEnableSpooling = "FAEnableSpooling";
        public const string FADisableSpooling = "FADisableSpooling";

        public const string FASendTerminalMessage = "FASendTerminalMessage";

    }
    public class FAViewModel : UiViewModelBase
    {
        public bool IsPermission { get => this.Permission == 3; }

        private class DataName
        {
            public const string CommunicationStatus = "CommunicationStatus";
            public const string ControlStatus = "ControlStatus";
            public const string ControlSubStatus = "ControlSubStatus";

            public const string SpoolingState = "SpoolingState";
            public const string SpoolingActual = "SpoolingActual";
            public const string SpoolingTotal = "SpoolingTotal";
            public const string SpoolingFullTime = "SpoolingFullTime";
            public const string SpoolingStartTime = "SpoolingStartTime";
            public const string IsSpoolingEnable = "IsSpoolingEnable";

            //EFEM


        }

        public FAViewModel()
        {
            this.DisplayName = "FA";

            ConfigFeedback = new PageSCFA();
            ConfigSetPoint = new PageSCFA();
            SetConfigCommand = new DelegateCommand<object>(SetConfig);
            SetConfig2Command = new DelegateCommand<object>(SetConfig2);
            InitializeCommand = new DelegateCommand<object>(InitializeFa);
            InvokeCommand = new DelegateCommand<object>(Invoke);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _isSubscriptionAttribute = attribute => attribute is SubscriptionAttribute;
            _hasSubscriptionAttribute = mi => mi.GetCustomAttributes(false).Any(_isSubscriptionAttribute);

            Parallel.ForEach(this.GetType().GetProperties().Where(_hasSubscriptionAttribute),
                property =>
                {
                    SubscriptionAttribute subscription = property.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
                    string key = subscription.ModuleKey;
                    if (!_subscribedKeys.Contains(key))
                        _subscribedKeys.Add(key);
                });

            _timer = new PeriodicJob(1000, this.OnTimer, "UIUpdaterThread - " + GetType().Name, true);

            ConfigSetPoint.Update(QueryDataClient.Instance.Service.PollConfig(ConfigSetPoint.GetKeys()));
        }

        #region subscrib
        [Subscription(DataName.IsSpoolingEnable, ModuleNameString.System)]
        public bool IsSpoolingEnable
        {
            get;
            set;
        }

        [Subscription("System.SpoolingState")]
        public string SpoolingState
        {
            get;
            set;
        }
        [Subscription(DataName.SpoolingActual, ModuleNameString.System)]
        public string SpoolingActual
        {
            get;
            set;
        }
        [Subscription(DataName.SpoolingTotal, ModuleNameString.System)]
        public string SpoolingTotal
        {
            get;
            set;
        }
        [Subscription(DataName.SpoolingFullTime, ModuleNameString.System)]
        public string SpoolingFullTime
        {
            get;
            set;
        }
        [Subscription(DataName.SpoolingStartTime, ModuleNameString.System)]
        public string SpoolingStartTime
        {
            get;
            set;
        }

        [Subscription(DataName.ControlStatus, ModuleNameString.System)]
        public string HostControlStatus
        {
            get;
            set;
        }

        [Subscription(DataName.CommunicationStatus, ModuleNameString.System)]
        public string HostCommunicationStatus
        {
            get;
            set;
        }

        #endregion

        #region  logic

        public FACommunicationState FACommunicationState
        {
            get
            {
                return string.IsNullOrEmpty(HostCommunicationStatus) ? FACommunicationState.Disabled
              : (FACommunicationState)Enum.Parse(typeof(FACommunicationState), HostCommunicationStatus);
            }
        }

        public FAControlState FAControlState
        {
            get
            {
                return string.IsNullOrEmpty(HostControlStatus) ? FAControlState.Unknown
              : (FAControlState)Enum.Parse(typeof(FAControlState), HostControlStatus);
            }
        }

        //Disabled,
        //Enabled,
        //EnabledNotCommunicating,
        //EnabledCommunicating,
        //WaitCRA,
        //WaitDelay,
        //WaitCRFromHost,

        public bool IsEnableEnableButton
        {
            get { return FACommunicationState == FACommunicationState.Disabled; }
        }

        public bool IsEnableDisableButton
        {
            get { return FACommunicationState != FACommunicationState.Disabled; }
        }

        //Unknown,
        //EquipmentOffline,
        //AttemptOnline,
        //HostOffline,
        //OnlineLocal,
        //OnlineRemote,
        public bool IsEnableOnlineButton
        {
            get { return FACommunicationState == FACommunicationState.EnabledCommunicating && (FAControlState == FAControlState.Unknown || FAControlState == FAControlState.EquipmentOffline); }
        }

        public bool IsEnableOfflineButton
        {
            get { return FACommunicationState == FACommunicationState.EnabledCommunicating && (FAControlState == FAControlState.Unknown || FAControlState != FAControlState.EquipmentOffline); }
        }

        public bool IsEnableLocalButton
        {
            get { return FACommunicationState == FACommunicationState.EnabledCommunicating && (FAControlState == FAControlState.OnlineRemote); }
        }

        public bool IsEnableRemoteButton
        {
            get { return FACommunicationState == FACommunicationState.EnabledCommunicating && (FAControlState == FAControlState.OnlineLocal); }
        }

        public bool IsEnableSpoolingEnableButton
        {
            get
            {
                return !IsSpoolingEnable;//SpoolingState == FASpoolingState.Inactive.ToString();
            }
        }

        public bool IsEnableSpoolingDisableButton
        {
            get
            {
                return IsSpoolingEnable;// SpoolingState == FASpoolingState.Active.ToString();
            }
        }



        #endregion
        [IgnorePropertyChange]
        public PageSCFA ConfigFeedback
        {
            get;
            set;
        }

        [IgnorePropertyChange]
        public PageSCFA ConfigSetPoint
        {
            get;
            set;
        }

        [IgnorePropertyChange]
        public ICommand SetConfigCommand
        {
            get;
            private set;
        }

        [IgnorePropertyChange]
        public ICommand SetConfig2Command
        {
            get;
            private set;
        }

        [IgnorePropertyChange]
        public ICommand InitializeCommand
        {
            get;
            private set;
        }

        [IgnorePropertyChange]
        public ICommand InvokeCommand
        {
            get;
            private set;
        }

        PeriodicJob _timer;
        //protected ConcurrentBag<string> _subscribedKeys = new ConcurrentBag<string>();

        //protected Func<object, bool> _isSubscriptionAttribute;
        //protected Func<MemberInfo, bool> _hasSubscriptionAttribute;

        private void Invoke(object param)
        {
            InvokeClient.Instance.Service.DoOperation("FACommand", ((string)param).Split(',')[1]);
        }

        private void InitializeFa(object obj)
        {
            if (MessageBox.Show("Are you sure you want to re-initialize FA connection?",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                InvokeClient.Instance.Service.DoOperation("Fa.Initialize");

            }
        }

        void SetConfig(object param)
        {
            object[] sc = (object[])param;

            InvokeClient.Instance.Service.DoOperation("System.SetConfig", sc[0].ToString(), sc[1].ToString());


            UpdateConfig();
        }

        void SetConfig2(object param)
        {
            string id = (string)param;
            string key = id.Replace("Fa.", "Fa_");

            PropertyInfo[] property = typeof(PageSCFA).GetProperties();

            foreach (PropertyInfo prop in property)
            {
                if (prop.Name == key)
                {

                    InvokeClient.Instance.Service.DoOperation("System.SetConfig", id, prop.GetValue(ConfigSetPoint, null).ToString());

                    break;
                }
            }


            UpdateConfig();
        }

        public void UpdateConfig()
        {
            ConfigFeedback.Update(QueryDataClient.Instance.Service.PollConfig(ConfigFeedback.GetKeys()));
            NotifyOfPropertyChange("ConfigFeedback");
        }

        new bool OnTimer()
        {
            try
            {
                Poll();
                UpdateConfig();
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }

            return true;
        }

        new void Poll()
        {
            if (_subscribedKeys.Count > 0)
            {
                Dictionary<string, object> result = QueryDataClient.Instance.Service.PollData(_subscribedKeys);

                if (result == null)
                {
                    LOG.Error("获取RT数据失败");
                    return;
                }

                if (result.Count != _subscribedKeys.Count)
                {
                    string unknowKeys = string.Empty;
                    foreach (string key in _subscribedKeys)
                    {
                        if (!result.ContainsKey(key))
                        {
                            unknowKeys += key + "\r\n";
                        }
                    }
                    //System.Diagnostics.Debug.Assert(false, unknowKeys);
                }

                UpdateValue(result);

            }
        }


        void UpdateValue(Dictionary<string, object> data)
        {
            if (data == null)
                return;

            UpdateSubscribe(data, this);
        }

        new void UpdateSubscribe(Dictionary<string, object> data, object target, string module = null)
        {
            Parallel.ForEach(target.GetType().GetProperties().Where(_hasSubscriptionAttribute),
                property =>
                {
                    PropertyInfo pi = (PropertyInfo)property;
                    SubscriptionAttribute subscription = property.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
                    string key = subscription.ModuleKey;
                    key = module == null ? key : string.Format("{0}.{1}", module, key);

                    if (_subscribedKeys.Contains(key) && data.ContainsKey(key))
                    {
                        try
                        {
                            var convertedValue = Convert.ChangeType(data[key], pi.PropertyType);
                            var originValue = Convert.ChangeType(pi.GetValue(target, null), pi.PropertyType);
                            if (originValue != convertedValue)
                            {
                                pi.SetValue(target, convertedValue, null);
                                NotifyOfPropertyChange(pi.Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            LOG.Error("由RT返回的数据更新失败" + key, ex);
                        }
                    }
                });
        }

        //protected override void InvokeBeforeUpdateProperty(Dictionary<string, object> data)
        //{

        //}
    }

    public class PageSCFA : PageSCValue
    {
        public string Fa_ConnectionMode { get; set; }

        public string Fa_LocalIpAddress { get; set; }
        public int Fa_LocalPortNumber { get; set; }
        public string Fa_RemoteIpAddress { get; set; }
        public int Fa_RemotePortNumber { get; set; }

        public int Fa_T3Timeout { get; set; }
        public int Fa_T5Timeout { get; set; }
        public int Fa_T6Timeout { get; set; }
        public int Fa_T7Timeout { get; set; }
        public int Fa_T8Timeout { get; set; }
        public bool Fa_EnableSpooling { get; set; }
        public int Fa_LinkTestInterval { get; set; }
        public string Fa_DefaultCommunicationState { get; set; }
        public string Fa_DefaultControlState { get; set; }
        public string Fa_DefaultControlSubState { get; set; }
        public string Fa_DeviceId { get; set; }

        public PageSCFA()
        {
            UpdateKeys(typeof(PageSCFA).GetProperties());
        }

    }
}
