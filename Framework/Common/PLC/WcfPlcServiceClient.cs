using Aitex.Core.WCF;

namespace MECF.Framework.Common.PLC
{
    public class WcfPlcServiceClient : ServiceClientWrapper<IWcfPlcService>, IWcfPlcService
    {
        public WcfPlcServiceClient(string configName)
            : base(configName, "WcfPlcService")
        {
        }

        public WcfPlcServiceClient()
            : base("Client_IWcfPlcService", "WcfPlcService")
        {
        }

        public bool Read(string variable, out object data, string type, int length, out string reason)
        {
            bool result = false;
            object data1 = null;
            string reason1 = string.Empty;
            Invoke(svc =>
            {
                result = svc.Read(variable, out data1, type, length, out reason1);
            });
            data = data1;
            reason = reason1;
            return result;
        }

        public bool WriteArrayElement(string variable, int index, object value, out string reason)
        {
            bool result = false;
            string reason1 = string.Empty;
            Invoke(svc =>
            {
                result = svc.WriteArrayElement(variable, index, value, out reason1);
            });

            reason = reason1;
            return result;
        }

        public int Heartbeat(int counter)
        {
            int result = 0;
            Invoke(svc =>
            {
                result = svc.Heartbeat(counter);
            });

            return result;
        }

        public bool[] ReadDi(int offset, int size, out string reason)
        {
            bool[] result = null;
            string reason1 = string.Empty;
            Invoke(svc => { result = svc.ReadDi(offset, size, out reason1); });

            reason = reason1;
            return result;
        }

        public float[] ReadAiFloat(int offset, int size, out string reason)
        {
            float[] result = null;
            string reason1 = string.Empty;
            Invoke(svc => { result = svc.ReadAiFloat(offset, size, out reason1); });

            reason = reason1;
            return result;
        }

        public int[] ReadAiInt(int offset, int size, out string reason)
        {
            int[] result = null;
            string reason1 = string.Empty;
            Invoke(svc => { result = svc.ReadAiInt(offset, size, out reason1); });

            reason = reason1;
            return result;
        }

        public short[] ReadAiInt16(int offset, int size, out string reason)
        {
            short[] result = null;
            string reason1 = string.Empty;
            Invoke(svc => { result = svc.ReadAiInt16(offset, size, out reason1); });

            reason = reason1;
            return result;
        }

        public bool WriteDo(int offset, bool[] buffer, out string reason)
        {
            bool result = false;
            string reason1 = string.Empty;
            Invoke(svc => { result = svc.WriteDo(offset, buffer, out reason1); });

            reason = reason1;
            return result;
        }

        public bool WriteAoFloat(int offset, float[] buffer, out string reason)
        {
            bool result = false;
            string reason1 = string.Empty;
            Invoke(svc => { result = svc.WriteAoFloat(offset, buffer, out reason1); });

            reason = reason1;
            return result;
        }

        public bool WriteAoInt(int offset, int[] buffer, out string reason)
        {
            bool result = false;
            string reason1 = string.Empty;
            Invoke(svc => { result = svc.WriteAoInt(offset, buffer, out reason1); });

            reason = reason1;
            return result;
        }

        public bool WriteAoInt16(int offset, short[] buffer, out string reason)
        {
            bool result = false;
            string reason1 = string.Empty;
            Invoke(svc => { result = svc.WriteAoInt16(offset, buffer, out reason1); });

            reason = reason1;
            return result;
        }
    }
}
