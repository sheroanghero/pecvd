using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace Aitex.Core.UI.MVVM
{
	/*
     * 要求绑定的格式参考如下方式，命名统一为ElementData，这样才能统一由程序把需要从RT获取的数据项名称自动添加进来
            <my1:AnalogControl Command="{Binding Path=DeviceOperationCommand}" DeviceData="{Binding Path=ElementData[ReactorA.mfcNH3PushH2]  }" Width="54" />
    */
	public class SubscriptionViewModelBase : TimerViewModelBase
	{
		const string BindingPathName = "ElementData";

		protected Func<IEnumerable<string>, Dictionary<string, object>> PollDataFunction;
		protected Action<string[]> InvokeFunction;
		protected Action<string, string> DeviceFunction;
		protected Action<object[]> DeviceControlFunction;

		protected Dictionary<string, Func<string, bool>> PreCommand = new Dictionary<string, Func<string, bool>>();

		ConcurrentBag<string> _subscribedKeys = new ConcurrentBag<string>();

		Func<object, bool> _isSubscriptionAttribute;
		Func<MemberInfo, bool> _hasSubscriptionAttribute;


		public ICommand InvokeCommand { get; set; }

		List<DelegateCommand<string>> _lstICommand = new List<DelegateCommand<string>>();

		List<IViewModelControl> viewModelControls = new List<IViewModelControl>();

		/// <summary>
		/// 设备名称，设备操作
		/// </summary>
		public ICommand DeviceCommand { get; set; }
		public ICommand DeviceControlCommand { get; set; }

		public SubscriptionViewModelBase(string name)
			: base(name)
		{
			_isSubscriptionAttribute = attribute => attribute is SubscriptionAttribute;
			_hasSubscriptionAttribute = mi => mi.GetCustomAttributes(false).Any(_isSubscriptionAttribute);

			InvokeCommand = new DelegateCommand<string>(param =>
			{
				PerformInvokeFunction(param.Split(','));

			}, functionName => { return true; });

			DeviceCommand = new DelegateCommand<string>(operation =>
			{
				string[] args = operation.Split(',');
				DeviceFunction(args[0], args[1]);
			}, functionName => { return true; });

			DeviceControlCommand = new DelegateCommand<object>(args =>
			{
				DeviceControlFunction((object[])args);
			}, functionName => { return true; });

			TraverseKeys();
		}

		public virtual bool PerformInvokeFunction(string[] param)
		{

			return true;
		}

		public static string UIKey(string param1, string param2)
		{
			return param1 + "." + param2;
		}
		public static string UIKey(string param1, string param2, string param3)
		{
			return param1 + "." + param2 + "." + param3;
		}
		public static string UIKey(string param1, string param2, string param3, string param4)
		{
			return param1 + "." + param2 + "." + param3 + "." + param4;
		}

		protected override void Poll()
		{
			if (PollDataFunction != null && _subscribedKeys.Count > 0)
			{
				Dictionary<string, object> result = PollDataFunction(_subscribedKeys);

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
					//if (unknowKeys.Length > 0)
					//    LOG.Error("UI did not get data element：" + unknowKeys);
				}

				InvokeBeforeUpdateProperty(result);

				UpdateValue(result);

				Application.Current.Dispatcher.Invoke(new Action(() =>
				{
					InvokePropertyChanged();

					foreach (var viewModelControl in viewModelControls)
					{
						viewModelControl.InvokePropertyChanged();
					}

					InvokeAfterUpdateProperty(result);
				}));


			}
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

			foreach (var viewModelControl in viewModelControls)
			{
				UpdateSubscribe(data, viewModelControl);
			}
		}

		void TraverseKeys()
		{
			SubscribeKeys(this);
		}

		void Subscribe(Binding binding, string bindingPathName)
		{
			if (binding != null)
			{
				string path = binding.Path.Path;
				if (path.Contains(bindingPathName) && path.Contains('[') && path.Contains(']'))
				{
					try
					{
						Subscribe(path.Substring(path.IndexOf('[') + 1, path.IndexOf(']') - path.IndexOf('[') - 1));
					}
					catch (Exception ex)
					{
						LOG.Write(ex);
					}
				}
			}
		}

		protected void Subscribe(string key)
		{
			if (!string.IsNullOrEmpty(key))
			{

				_subscribedKeys.Add(key);
			}
		}

		protected void Subscribe(string key, object value)
		{
			if (!string.IsNullOrEmpty(key))
			{

				_subscribedKeys.Add(key);
			}
		}
	    //public void SubscribeKeys(UiViewModelBase target)
	    //{
	    //    SubscribeKeys(target, "");
	    //}

	    //public void SubscribeKeys(UiViewModelBase target, string module)
	    //{
	    //    Parallel.ForEach(target.GetType().GetProperties().Where(_hasSubscriptionAttribute),
	    //        property =>
	    //        {
	    //            SubscriptionAttribute subscription = property.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
	    //            string key = subscription.ModuleKey;
	    //            if (!string.IsNullOrEmpty(module))
	    //            {
	    //                key = $"{module}.{key}";
	    //                subscription.SetModule(module);
	    //            }
	    //            if (!_subscribedKeys.Contains(key))
	    //                _subscribedKeys.Add(key);
	    //        });

	    //    Parallel.ForEach(target.GetType().GetFields().Where(_hasSubscriptionAttribute),
	    //        method =>
	    //        {
	    //            SubscriptionAttribute subscription = method.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
	    //            string key = subscription.ModuleKey;
	    //            if (!string.IsNullOrEmpty(module))
	    //            {
	    //                key = $"{module}.{key}";
	    //                subscription.SetModule(module);
	    //            }
	    //            if (!_subscribedKeys.Contains(key))
	    //                _subscribedKeys.Add(key);
	    //        });
	    //}

        public void SubscribeKeys(IViewModelControl target)
		{
			Parallel.ForEach(target.GetType().GetProperties().Where(_hasSubscriptionAttribute),
				property =>
				{
					SubscriptionAttribute subscription = property.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
					string key = subscription.ModuleKey;
					if (!_subscribedKeys.Contains(key))
						_subscribedKeys.Add(key);
				});

			Parallel.ForEach(target.GetType().GetFields().Where(_hasSubscriptionAttribute),
				method =>
				{
					SubscriptionAttribute subscription = method.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute;
					string key = subscription.ModuleKey;
					if (!_subscribedKeys.Contains(key))
						_subscribedKeys.Add(key);
				});

			// Add sub module subscription
			var properties = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetCustomAttribute<SubscriptionModuleAttribute>() != null);
			foreach (var property in properties)
			{
				var moduleAttr = property.GetCustomAttribute<SubscriptionModuleAttribute>();
				var module = moduleAttr.Module;
				var type = property.PropertyType;
				var ps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetCustomAttribute<SubscriptionAttribute>() != null);
				foreach (var p in ps)
				{
					var a = p.GetCustomAttribute<SubscriptionAttribute>();
					var key = string.Format("{0}.{1}", module, a.ModuleKey);
					if (!_subscribedKeys.Contains(key))
						_subscribedKeys.Add(key);
				}
			}

			if (target != this)
			{
				viewModelControls.Add(target);
			}
		}

		public void UpdateSubscribe(Dictionary<string, object> data, object target, string module = null)
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
								if (pi.Name == "PumpLimitSetPoint")
									pi.SetValue(target, convertedValue, null);
								else
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
	}
}
