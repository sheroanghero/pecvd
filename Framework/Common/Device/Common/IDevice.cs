using System;
using Aitex.Core.RT.Event;
using MECF.Framework.Common.Event;

namespace Aitex.Core.RT.Device
{
    public interface IDevice
    {
        event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;

        string Module { get; set; }
        string Name { get; set; }

        bool HasAlarm { get; }

        bool Initialize();
        void Monitor();
        void Terminate();

        void Reset();
    }

    public interface IModuleDevice
    {
        bool IsReady { get; }
        bool IsError { get; }
        bool IsInit { get; }

        bool Home(out string reason);
    }


}
