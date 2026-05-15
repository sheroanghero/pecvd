using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.RT.Routine
{
    public interface IRoutine
    {
        Result Start(params object[] objs);
        Result Monitor();

        void Abort();
    }

}
