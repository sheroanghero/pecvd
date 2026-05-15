using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.Core.Applications
{
    public interface IRtLoader
    {
        void Initialize();
        void Terminate();
    }
}
