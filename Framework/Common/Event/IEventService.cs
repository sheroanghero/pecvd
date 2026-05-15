using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Aitex.Core.RT.Event;

namespace Aitex.Core.WCF.Interface
{
    [ServiceContract(CallbackContract = typeof(IEventServiceCallback))]
    public interface IEventService
    {
        event Action<EventItem> OnEvent;

        [OperationContract]
        bool Register(Guid id);

        [OperationContract(IsOneWay = true)]
        void UnRegister(Guid id);

        int Heartbeat();
    }
}
