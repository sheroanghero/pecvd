using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.Communications
{
    public interface IConnection
    {
        string Address { get; }

        bool IsConnected { get; }

        bool Connect();

        bool Disconnect();
    }
}
