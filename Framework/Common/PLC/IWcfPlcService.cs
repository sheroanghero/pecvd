using System;
using System.ServiceModel;

namespace MECF.Framework.Common.PLC
{ 
    [ServiceContract]

    [ServiceKnownType(typeof(float[]))]
    [ServiceKnownType(typeof(bool[]))]
    [ServiceKnownType(typeof(int[]))]
    [ServiceKnownType(typeof(short[]))]
    [ServiceKnownType(typeof(byte[]))]
    [ServiceKnownType(typeof(double[]))]
    public interface IWcfPlcService 
    {
        [OperationContract]
        int Heartbeat(int counter);

        [OperationContract]
        bool Read(string variable, out object data, string type, int length, out string reason);

        [OperationContract]
        bool WriteArrayElement(string variable, int index, object value, out string reason);




        [OperationContract]
        bool[] ReadDi(int offset, int size, out string reason);

        [OperationContract]
        float[] ReadAiFloat(int offset, int size, out string reason);

        [OperationContract]
        int[] ReadAiInt(int offset, int size, out string reason);

        [OperationContract]
        short[] ReadAiInt16(int offset, int size, out string reason);



        [OperationContract]
        bool WriteDo(int offset, bool[] buffer, out string reason);

        [OperationContract]
        bool WriteAoFloat(int offset, float[] buffer, out string reason);

        [OperationContract]
        bool WriteAoInt(int offset, int[] buffer, out string reason);

        [OperationContract]
        bool WriteAoInt16(int offset, short[] buffer, out string reason);
    }
}
