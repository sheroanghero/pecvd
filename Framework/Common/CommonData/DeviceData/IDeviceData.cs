using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.Common.DeviceData
{
    public interface IDeviceData
    {
        void Update(IDeviceData data);
    }
}
