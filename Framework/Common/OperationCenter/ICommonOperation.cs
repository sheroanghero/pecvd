using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MECF.Framework.Common.OperationCenter;

namespace Aitex.Core.RT.OperationCenter
{
    public delegate bool OperationFunction(out string reason, int time, params object[] param);

    public interface ICommonOperation
    {
        void Subscribe<T>(T instance, string keyPrefix = null) where T : class;
        void Subscribe(string key, Func<string, object[], bool> op);
        bool DoOperation(string operationName, params object[] args);

        bool ContainsOperation(string operation);
        bool CanDoOperation(string operation, out string reason, params object[] args);

        bool AddCheck(string operation, IInterlockChecker checker);
    }

    public interface IOperation
    {
        void Subscribe(string key, OperationFunction op);
        bool DoOperation(string operationName, out string reason, int time, params object[] args);
    }
}
