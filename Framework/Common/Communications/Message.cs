using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.Communications
{
    public abstract class MessageBase
    {
        public bool IsResponse { get; set; }

        public bool IsAck { get; set; }

        public bool IsError { get; set; }

        public bool IsEvent { get; set; }

        public bool IsComplete { get; set; }

        public bool IsFormatError { get; set; }

        public bool IsBusy { get; set; }

        public bool IsNak { get; set; }

        public bool IsCommand { get; set; }

        public int MutexTag { get; set; }
    }

    public class AsciiMessage : MessageBase
    {
        public string RawMessage { get; set; }

        public string[] MessagePart { get; set; }
    }

    public class BinaryMessage : MessageBase
    {
        public byte[] RawMessage { get; set; }
    }
}
