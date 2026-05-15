using MECF.Framework.Common.Communications;
using System.Collections.Generic;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Pumps.OsakaPumpTGkine
{
    public abstract class OsakaPumpTGkineHandler : HandlerBase
    {
        public OsakaPumpTGkine Device { get; }

        private static byte _slaveID = 0x01;

        protected OsakaPumpTGkineHandler(OsakaPumpTGkine device, byte addressHigh, byte addressLow)
            : base(BuildMessage(new byte[] { addressHigh, addressLow }))
        {
            Device = device;
        }

        protected OsakaPumpTGkineHandler(OsakaPumpTGkine device, byte addressHigh, byte addressLow, byte valueHigh, byte valueLow)
            : base(BuildMessage(new byte[] { addressHigh, addressLow }, new byte[] { valueHigh, valueLow }))
        {
            Device = device;
        }

        private static byte[] BuildMessage(byte[] address)
        {
            List<byte> result = new List<byte>() { 0x02, _slaveID, 0x00, 0x09, 0x52, 0x00, 0x02 };
            if (address != null)
            {
                foreach (byte data in address)
                    result.Add(data);
            }
            result.AddRange(new List<byte>() { 0x00, 0x02 });
            result.Add(CalcSum(result));
            result.AddRange(new List<byte>() { 0x03, 0x03 });
            return result.ToArray();
        }

        private static byte[] BuildMessage(byte[] address, byte[] datas)
        {
            List<byte> result = new List<byte>() { 0x02, _slaveID, 0x00, 0x0A, 0x57, 0x00, 0x02 };
            if (address != null)
            {
                foreach (byte data in address)
                    result.Add(data);
            }

            result.Add(CalcSum(result));
            if (address != null)
            {
                foreach (byte data in datas)
                    result.Add(data);
            }

            result.Add(CalcSum(result));
            result.AddRange(new List<byte>() { 0x03, 0x03 });
            return result.ToArray();
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            OsakaPumpTGkineMessage response = msg as OsakaPumpTGkineMessage;
            ResponseMessage = msg;

            if (response.IsResponse)
            {
                SetState(EnumHandlerState.Acked);

                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                Device.NoteError(null);
                return true;
            }

            transactionComplete = false;
            return false;
        }

        public static byte CalcSum(List<byte> value)
        {
            byte ret = 0x00;
            for (var i = 0; i < value.Count; i++)
            {
                ret += value[i];
            }

            ret = (byte)~ret;

            ret++;

            return ret;

        }

    }

    public class OsakaPumpTGkineLowerReadHandler : OsakaPumpTGkineHandler
    {
        public byte ResultHigh { get; set; }
        public byte ResultLow { get; set; }

        public OsakaPumpTGkineLowerReadHandler(OsakaPumpTGkine device, byte addressHigh, byte addressLow)
            : base(device, addressHigh, addressLow)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as OsakaPumpTGkineMessage;

                ResultHigh = result.RawMessage[5];
                ResultLow = result.RawMessage[6];

                Device.NoteLowerReadDone();
            }
            return true;
        }
    }

    public class OsakaPumpTGkineLowerWriteHandler : OsakaPumpTGkineHandler
    {
        public OsakaPumpTGkineLowerWriteHandler(OsakaPumpTGkine device, byte addressHigh, byte addressLow, byte valueHigh, byte valueLow)
            : base(device, addressHigh, addressLow, valueHigh, valueLow)
        {
        }
        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (base.HandleMessage(msg, out handled))
            {
                var result = msg as OsakaPumpTGkineMessage;

                Device.NoteLowerWriteDone();
            }
            return true;

        }

    }

}
