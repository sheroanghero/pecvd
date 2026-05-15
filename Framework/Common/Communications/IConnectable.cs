using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.Communications
{
    public interface IConnectable
    {
        //连接上之后的通讯错误
        event Action<string> OnCommunicationError;

        event Action<string> OnAsciiDataReceived;

        event Action<byte[]> OnBinaryDataReceived;

        bool Connect(out string reason);

        bool Disconnect(out string reason);

        bool CheckIsConnected();

        bool SendBinaryData(byte[] data);

        bool SendAsciiData(string data);
    }
 
}
