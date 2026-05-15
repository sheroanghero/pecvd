using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.Alarms
{
    public enum AlarmAction
    {
        Clear = 1,
        Abort,
        Retry,
        Continue,
    }
}
