using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MECF.Framework.Common.OperationCenter;

namespace Aitex.Core.RT.OperationCenter
{
    public class OP
    {
        public static ICommonOperation InnerOperationManager { set; private get; }
        public static IOperation OperationManager { set; private get; }

        public static void Subscribe<T>(T instance, string keyPrefix = null) where T : class
        {
            if (InnerOperationManager != null)
                InnerOperationManager.Subscribe<T>(instance, keyPrefix);
        }

        public static void Subscribe(string key, Func<string, object[], bool> op)
        {
            if (InnerOperationManager != null)
                InnerOperationManager.Subscribe(key, op);
        }

        public static void Subscribe(string key, OperationFunction op)
        {
            if (OperationManager != null)
                OperationManager.Subscribe(key, op);
        }
        public static void DoOperation(string operationName, params object[] args)
        {
            if (InnerOperationManager != null)
                InnerOperationManager.DoOperation(operationName, args);
        }

        public static void DoOperation(string operationName, out string reason, int time, params object[] args)
        {
            reason = string.Empty;
            if (OperationManager != null)
                OperationManager.DoOperation(operationName, out reason, time, args);
        }

        public static bool ContainsOperation(string operation)
        {
            if (InnerOperationManager != null)
                return InnerOperationManager.ContainsOperation(operation);
            return true;
        }

        public static bool CanDoOperation(string operation, out string reason, params object[] args)
        {
            if (InnerOperationManager != null)
                return InnerOperationManager.CanDoOperation(operation, out reason, args);

            reason = string.Empty;
            return true; 
        }
        public static bool AddCheck(string operation, IInterlockChecker checker)
        {
            if (InnerOperationManager != null)
                return InnerOperationManager.AddCheck(operation, checker);
 
            return true;
        }
 

    }
}
