using Aitex.Core.RT.Log;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using MECF.Framework.Common.DataCenter;
using OpenSEMI.Ctrlib.Controls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class ModuleUiViewModelBase : UiViewModelBase
    {
        public string SystemName { get; set; }


        public override void SubscribeKeys()
        {
            SubscribeKeys(this, SystemName);
        }


        public override void UpdateSubscribe(Dictionary<string, object> data, object target, string module = null)
        {
            Parallel.ForEach(target.GetType().GetProperties().Where(_hasSubscriptionAttribute),
                property =>
                {
                    PropertyInfo pi = (PropertyInfo)property;
                    SubscriptionAttribute subscription = property.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
                    string key = module == null ? (SystemName==null?subscription.ModuleKey.ToString():$"{SystemName}.{subscription.ModuleKey}"): string.Format("{0}.{1}", module, subscription.ModuleKey);

                    if (_subscribedKeys.Contains(key) && data.ContainsKey(key))
                    {
                        try
                        {
                            var convertedValue = Convert.ChangeType(data[key], pi.PropertyType);
                            var originValue = Convert.ChangeType(pi.GetValue(target, null), pi.PropertyType);
                            if (originValue != convertedValue)
                            {
                                pi.SetValue(target, convertedValue, null);
                            }
                        }
                        catch (Exception ex)
                        {
                            LOG.Error("由RT返回的数据更新失败" + key, ex);

                        }

                    }
                });

            Parallel.ForEach(target.GetType().GetFields().Where(_hasSubscriptionAttribute),
                property =>
                {
                    FieldInfo pi = (FieldInfo)property;
                    SubscriptionAttribute subscription = property.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
                    string key = module == null ? $"{SystemName}.{subscription.ModuleKey}" : string.Format("{0}.{1}", module, subscription.ModuleKey);

                    if (_subscribedKeys.Contains(key) && data.ContainsKey(key))
                    {
                        try
                        {
                            var convertedValue = Convert.ChangeType(data[key], pi.FieldType);
                            pi.SetValue(target, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            LOG.Error("由RT返回的数据更新失败" + key, ex);

                        }

                    }
                });
        }


        protected override void InvokePropertyChanged()
        {
            PropertyInfo[] ps = this.GetType().GetProperties();
            foreach (PropertyInfo p in ps)
            {
                if (!p.GetCustomAttributes(false).Any(attribute => attribute is IgnorePropertyChangeAttribute))
                    InvokePropertyChanged(p.Name);

                if (p.PropertyType == typeof(ICommand))
                {
                    if (p.GetValue(this, null) is IDelegateCommand cmd)
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

            //Parallel.ForEach(this.GetType().GetProperties(), property => InvokePropertyChanged(property.Name));
        }
    }
    public class UiViewModelBase : BaseModel
    {
        PeriodicJob _timer;
        protected ConcurrentBag<string> _subscribedKeys = new ConcurrentBag<string>();

        protected Func<object, bool> _isSubscriptionAttribute;
        protected Func<MemberInfo, bool> _hasSubscriptionAttribute;

        public ICommand DeviceOperationCommand { get; private set; }

        /// <summary>
        /// 是否在退出页面后，仍然在后台更新数据，默认不更新
        /// </summary>
        public bool ActiveUpdateData { get; set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            DeviceOperationCommand = new DelegateCommand<object>(DeviceOperation);

            SubscribeKeys();

        }


        void DeviceOperation(object param)
        {
            //InvokeClient.Instance.Service.DoOperation(OperationName.DeviceOperation.ToString(), (object[])param);
        }


        public string GetUnitStatusBackground(string status)
        {
            return ModuleStatusBackground.GetStatusBackground(status);
        }

        public string GetUnitOnlineColor(bool isOnline)
        {
            return isOnline ? "LimeGreen" : "Black";
        }
        /// <summary>
        /// support wafer transfer for slot
        /// </summary>
        public virtual void OnWaferTransfer(DragDropEventArgs args)
        {
            try
            {
                WaferMoveManager.Instance.TransferWafer(args.TranferFrom, args.TranferTo);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        /// <summary>
        /// support context menu
        /// </summary>
        public void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
                return;

            if (sender is Slot slot)
            {
                if (slot.ViewType == "Top" &&  slot.ModuleID == "LLB")
                {
                    return;
                }
                if (slot.ViewType== "Top" &&slot.ModuleID=="LLA")
                {
                    return;
                }
                ContextMenu cm = ContextMenuManager.Instance.GetSlotMenus(slot);
                if (cm != null)
                {
                    ((FrameworkElement)e.Source).ContextMenu = cm;
                }

                return;
            }

            if (sender is CarrierContentControl carrier)
            {
                ContextMenu cm = ContextMenuManager.Instance.GetCarrierMenus(carrier);
                if (cm != null)
                {
                    ((FrameworkElement)e.Source).ContextMenu = cm;
                }

                return;
            }
        }

        public UiViewModelBase()
        {
            _timer = new PeriodicJob(1000, this.OnTimer, "UIUpdaterThread - " + GetType().Name);

            _isSubscriptionAttribute = attribute => attribute is SubscriptionAttribute;
            _hasSubscriptionAttribute = mi => mi.GetCustomAttributes(false).Any(_isSubscriptionAttribute);
        }


        //
        protected virtual bool OnTimer()
        {
            try
            {
                Poll();
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }

            return true;
        }


        public virtual void EnableTimer(bool enable)
        {
            if (enable) _timer.Start();
            else _timer.Pause();
        }


        protected virtual void Poll()
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

                InvokeBeforeUpdateProperty(result);

                UpdateValue(result);

                Application.Current?.Dispatcher.Invoke(new Action(() =>
                {
                    InvokePropertyChanged();

                    InvokeAfterUpdateProperty(result);
                }));


            }
        }

        public void InvokePropertyChanged(string propertyName)
        {
            NotifyOfPropertyChange(propertyName);
        }

        public void InvokeAllPropertyChanged()
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
        }

        protected virtual void InvokePropertyChanged()
        {
            Refresh();
        }

        protected virtual void InvokeBeforeUpdateProperty(Dictionary<string, object> data)
        {

        }

        protected virtual void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {

        }

        void UpdateValue(Dictionary<string, object> data)
        {
            if (data == null)
                return;

            UpdateSubscribe(data, this);

            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetCustomAttribute<SubscriptionModuleAttribute>() != null);
            foreach (var property in properties)
            {
                var moduleAttr = property.GetCustomAttribute<SubscriptionModuleAttribute>();
                UpdateSubscribe(data, property.GetValue(this), moduleAttr.Module);
            }
        }

        public virtual void SubscribeKeys()
        {
            SubscribeKeys(this);
        }

        protected void Subscribe(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _subscribedKeys.Add(key);
            }
        }

        public void SubscribeKeys(UiViewModelBase target)
        {
            SubscribeKeys(target, "");
        }

        public void SubscribeKeys(UiViewModelBase target, string module)
        {
            Parallel.ForEach(target.GetType().GetProperties().Where(_hasSubscriptionAttribute),
                property =>
                {
                    SubscriptionAttribute subscription = property.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
                    string key = subscription.ModuleKey;
                    if (!string.IsNullOrEmpty(module))
                    {
                        key = $"{module}.{key}";
                        subscription.SetModule(module);
                    }
                    if (!_subscribedKeys.Contains(key))
                        _subscribedKeys.Add(key);
                });

            Parallel.ForEach(target.GetType().GetFields().Where(_hasSubscriptionAttribute),
                method =>
                {
                    SubscriptionAttribute subscription = method.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
                    string key = subscription.ModuleKey;
                    if (!string.IsNullOrEmpty(module))
                    {
                        key = $"{module}.{key}";
                        subscription.SetModule(module);
                    }
                    if (!_subscribedKeys.Contains(key))
                        _subscribedKeys.Add(key);
                });
        }

        public virtual void UpdateSubscribe(Dictionary<string, object> data, object target, string module = null)
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
                            }
                        }
                        catch (Exception ex)
                        {
                            LOG.Error("由RT返回的数据更新失败" + key, ex);
                        }
                    }
                });

            Parallel.ForEach(target.GetType().GetFields().Where(_hasSubscriptionAttribute),
                property =>
                {
                    FieldInfo pi = (FieldInfo)property;
                    SubscriptionAttribute subscription = property.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
                    string key = subscription.ModuleKey;

                    if (_subscribedKeys.Contains(key) && data.ContainsKey(key))
                    {
                        try
                        {
                            var convertedValue = Convert.ChangeType(data[key], pi.FieldType);
                            pi.SetValue(target, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            LOG.Error("由RT返回的数据更新失败" + key, ex);

                        }

                    }
                });
        }


        protected override void OnActivate()
        {
            base.OnActivate();

            EnableTimer(true);
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);

            if (!ActiveUpdateData)
            {
                EnableTimer(false);
            }
        }
    }
}
