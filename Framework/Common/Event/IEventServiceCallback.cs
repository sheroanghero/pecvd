using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.IO;
using Aitex.Core.RT.Event;

namespace Aitex.Core.WCF.Interface
{
    [ServiceContract]
    public interface IEventServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        void SendEvent(EventItem ev);
    }
}
