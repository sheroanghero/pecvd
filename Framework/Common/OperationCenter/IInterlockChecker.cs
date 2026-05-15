using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.OperationCenter
{
    public interface IInterlockChecker
    {
        bool CanDo(out string reason, object[] args);
    }
}
