using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using Aitex.Core.Util;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.WCF;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;

namespace Aitex.Core.RT.OperationCenter
{
    public class OperationManager : ICommonOperation, IOperation
    {
        private ConcurrentDictionary<string, Func<string, object[], bool>> _operationMap = new ConcurrentDictionary<string, Func<string, object[], bool>>();

        private ConcurrentDictionary<string, OperationFunction> _timeOperationMap = new ConcurrentDictionary<string, OperationFunction>();

        private Func<object, bool> _isSubscriptionAttribute;

        private Func<MemberInfo, bool> _hasSubscriptionAttribute;

        private Dictionary<string, List<IInterlockChecker>> _operationInterlockCheck = new Dictionary<string, List<IInterlockChecker>>();

        public event Func<string, object[], bool> OnDoOperation; 

        public OperationManager()
        {

        }

        public void Initialize(bool enableService)
        {
            _isSubscriptionAttribute = attribute => attribute is SubscriptionAttribute;
            _hasSubscriptionAttribute = mi => mi.GetCustomAttributes(false).Any(_isSubscriptionAttribute);

            OP.InnerOperationManager = this;
            OP.OperationManager = this;

            if (enableService)
            {
                Singleton<WcfServiceManager>.Instance.Initialize(new Type[]
                {
                    typeof(InvokeService)
                });
            }
        }

        public void Initialize()
        {
            Initialize(true);
        }

        public void Subscribe<T>(T instance, string keyPrefix = null) where T : class
        {
            if (instance == null)
                throw new ArgumentNullException("instance");

            Traverse(instance, keyPrefix);
        }
 
        public void Subscribe(string key, OperationFunction op)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException("key");
            if (_timeOperationMap.ContainsKey(key))
                throw new Exception(string.Format("Duplicated Key:{0}", key));
            if (op == null)
                throw new ArgumentNullException("op");

            _timeOperationMap[key] = op;
        }

        public void Subscribe(string key, Func<string, object[], bool> op)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException("key");
            if (_operationMap.ContainsKey(key))
                throw new Exception(string.Format("Duplicated Key:{0}", key));
            if (op == null)
                throw new ArgumentNullException("op");

            _operationMap[key] = op;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="operation">操作名</param>
        /// <param name="time">毫秒</param>
        /// <param name="reason">失败原因</param>
        /// <param name="args">参数</param>
        /// <returns></returns>
        public bool DoOperation(string operation, out string reason, int time, params object[] args)
        {
            if (!CanDoOperation(operation, out reason, args))
            {
                EV.PostWarningLog("OP", $"Can not execute {operation}, {reason}");
                return false;
            }

            if (!_timeOperationMap[operation].Invoke(out reason, time, args))
            {
                LOG.Error($"Do operation {operation} failed, {reason}");
                return false;
            }

            return true;
        }

        public bool ContainsOperation(string operation)
        {
            return _timeOperationMap.ContainsKey(operation) || _operationMap.ContainsKey(operation);
        }

        public bool AddCheck(string operation, IInterlockChecker check)
        {
            if (!_operationInterlockCheck.ContainsKey(operation))
                _operationInterlockCheck[operation] = new List<IInterlockChecker>();

            _operationInterlockCheck[operation].Add(check);
            return true;
        }

        public bool CanDoOperation(string operation, out string reason, params object[] args)
        {
            if (!ContainsOperation(operation))
            {
                reason = $"Call unregistered function, {operation}";
                return false;
            }

            if (_operationInterlockCheck.ContainsKey(operation))
            {
                foreach (var interlockChecker in _operationInterlockCheck[operation])
                {
                    if (!interlockChecker.CanDo(out reason,  args))
                    {
                        return false;
                    }
                }
            }
 
            reason = string.Empty;
            return true;
        }

        public bool DoOperation(string operation, params object[] args)
        {
            if (OnDoOperation != null && OnDoOperation(operation, args))
                return true;

            if (!ContainsOperation(operation))
            {
                throw new ApplicationException("调用了未发布的接口" + operation);
            }

            if (!CanDoOperation(operation, out string reason, args))
            {
                EV.PostWarningLog("OP", $"Can not execute {operation}, {reason}");
                return false;
            }
 
            string parameter = "";
            if (args == null || args.Length == 0 )
                parameter = "()";
            else
            {
                parameter += "(";
                foreach (object o in args)
                {
                    if (o == null)
                        continue;

                    var param1 = "";
                    if (o.GetType() == typeof(Dictionary<string, object>))
                    {
                        param1 += "[";
                        foreach (var item in (Dictionary<string, object>)(o))
                        {
                            var value = item.Value.ToString();
                            if (value.Length > 10)
                                value = value.Substring(0, 7) + "...";
                            param1 += item.Value.ToString();
                            param1 += ", ";
                        }
                        if (param1.Length > 2)
                            param1 = param1.Remove(param1.Length - 2, 2);
                        param1 += "]";

                        parameter += param1;
                    }
                    else
                    {
                        parameter += o.ToString();
                    }
                    
                    parameter += ", ";
                }

                if (parameter.Length > 2)
                    parameter = parameter.Remove(parameter.Length - 2, 2);

                parameter += ")";
            }

            if (parameter.Length > 50)
            {
                parameter = parameter.Substring(0, 50) + "...";
                LOG.Write("long parameter: " + parameter);
            }

            EV.PostInfoLog("OP", $"Invoke operation {operation}{parameter}");

            if (_operationMap.ContainsKey(operation))
            {
                _operationMap[operation](operation, args);
            }
            else
            {
                if (!_timeOperationMap[operation].Invoke(out string reason1, 0, args))
                {
                    EV.PostWarningLog("OP", reason1);
                    return false;
                }
            }

            return true;
        }

        public void Traverse(object instance, string keyPrefix)
        {
            Parallel.ForEach(instance.GetType().GetMethods().Where<MethodInfo>(_hasSubscriptionAttribute), fi =>
            {
                string key = Parse(fi);
                key = string.IsNullOrWhiteSpace(keyPrefix) ? key : string.Format("{0}.{1}", keyPrefix, key);
                //Subscribe(key, fi.Invoke(instance, );
            });
        }

        string Parse(MethodInfo member)
        {
            return _hasSubscriptionAttribute(member) ? (member.GetCustomAttributes(false).First(_isSubscriptionAttribute) as SubscriptionAttribute).Key : null;
        }
 

    }
}
