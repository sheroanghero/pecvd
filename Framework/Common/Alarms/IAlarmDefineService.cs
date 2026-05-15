using Aitex.Core.RT.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.Alarms
{
    [ServiceContract]
    public interface IAlarmDefineService
    {
        [OperationContract]
        Dictionary<string, Dictionary<string, EventItem>> GetAlarmDefineTemplate();

        [OperationContract]
        string GetStringAlarmDefineTemplate();

    }
}
