using System;
using System.ServiceModel;
using Aitex.Core.RT.Device;
using MECF.Framework.Common.Event;

namespace MECF.Framework.Common.PLC
{
    [ServiceContract]
    [ServiceKnownType(typeof(float[]))]
    [ServiceKnownType(typeof(bool[]))]
    [ServiceKnownType(typeof(int[]))]
    [ServiceKnownType(typeof(byte[]))]
    [ServiceKnownType(typeof(double[]))]
    public interface IAdsPlc : IDevice
    {
        AlarmEventItem AlarmConnectFailed { get; set; }
        AlarmEventItem AlarmCommunicationError { get; set; }

        event Action OnConnected;
        event Action OnDisconnected;

        [OperationContract]
        bool CheckIsConnected();

        [OperationContract]
        bool Read(string variable, out object data, string type, int length, out string reason);

        [OperationContract]
        bool WriteArrayElement(string variable, int index, object value, out string reason);
    }
}
