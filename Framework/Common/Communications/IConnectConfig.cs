using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.Communications
{
    public interface IConnectionContext
    {
        bool IsEnabled { get; }

        int RetryConnectIntervalMs { get; }

        int MaxRetryConnectCount { get; }
 
        bool EnableCheckConnection { get; }

        string Address { get; }

        bool IsAscii { get; }

        string NewLine { get; }

        bool EnableLog { get; }
    }


    public class StaticConnectionContext:IConnectionContext
    {
        public bool IsEnabled { get; set; }
        public int RetryConnectIntervalMs { get; set; }
        public int MaxRetryConnectCount { get; set; }
        public bool EnableCheckConnection { get; set; }
        public string Address { get; set; }
        public bool IsAscii { get; set; }
        public string NewLine { get; set; }
        public bool EnableLog { get; set; }
    }
}
