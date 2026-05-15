using Aitex.Core.Util;
using Aitex.Core.WCF;

namespace MECF.Framework.Common.OperationCenter
{
    public class InvokeClient : Singleton<InvokeClient>
    {
        public bool InProcess { get; set; }

        private IInvokeService _service;
        public IInvokeService Service
        {
            get
            {
                if (_service == null)
                {
                    if (InProcess)
                    {
                        _service = new InvokeService();
                    }
                    else
                    {
                        _service = new InvokeServiceClient();
                    }
                }

                return _service;
            }
        }
    }
    public class InvokeServiceClient : ServiceClientWrapper<IInvokeService>, IInvokeService
    {

        public InvokeServiceClient()
            : base("Client_IInvokeService", "InvokeService")
        {
        }
 
        public void DoOperation(string operationName, params object[] args)
        {
            Invoke(svc => { svc.DoOperation(operationName, args); });
        }


    }
}
