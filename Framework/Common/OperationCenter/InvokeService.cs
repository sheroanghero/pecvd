using System;
using System.ServiceModel;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;

namespace MECF.Framework.Common.OperationCenter
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class InvokeService : IInvokeService
    {
        public void DoOperation(string operationName, params object[] args)
        {
            try
            {
                //if (KeyManager.Instance.IsExpired)
                //{
                //    EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                //    return;
                //}

                OP.DoOperation(operationName, args);
            }
            catch (Exception ex)
            {
                LOG.Error(string.Format("调用{0}，碰到未处理的WCF操作异常", operationName), ex);
            }
        }

    }
}