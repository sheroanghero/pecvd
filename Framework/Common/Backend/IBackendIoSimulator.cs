using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.Backend
{
    public interface IBackendIoSimulator
    {
        void SetDi(string group, string name, byte value);
    }
}
