using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.Backend
{
    interface IIOView
    {
        void EnableTimer(bool enable);
        void Close();
    }
}
