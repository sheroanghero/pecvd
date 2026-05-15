using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Device;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Event;

namespace MECF.Framework.Common.PLC
{
    [ServiceContract]
    [ServiceKnownType(typeof(float[]))]
    [ServiceKnownType(typeof(bool[]))]
    [ServiceKnownType(typeof(int[]))]
    [ServiceKnownType(typeof(byte[]))]
    [ServiceKnownType(typeof(double[]))]
    public interface IPlc : IDevice, IConnection
    {
        AlarmEventItem AlarmConnectFailed { get; set; }
        AlarmEventItem AlarmCommunicationError { get; set; }

        event Action OnConnected;
        event Action OnDisconnected;

        [OperationContract]
        bool CheckIsConnected();

        //倍福 
        [OperationContract]
        bool Read(string variable, out object data, string type, int length, out string reason);

        [OperationContract]
        bool WriteArrayElement(string variable, int index, object value, out string reason);
 

        // 三菱 
        [OperationContract]
        bool ReadBool(string address, out bool[] data, int length, out string reason);

        [OperationContract]
        bool ReadInt16(string address, out short[] data, int length, out string reason);

        [OperationContract]
        bool ReadInt32(string address, out int[] data, int length, out string reason);

        [OperationContract]
        bool ReadFloat(string address, out float[] data, int length, out string reason);

        [OperationContract]
        bool WriteBool(string address, bool[] data, out string reason);

        [OperationContract]
        bool WriteInt16(string address, short[] data, out string reason);

        [OperationContract]
        bool WriteFloat(string address, float[] data, out string reason);
    }
}
