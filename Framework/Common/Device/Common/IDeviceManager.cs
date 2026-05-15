using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Device;

namespace Aitex.Core.RT.Device
{
    public interface IDeviceManager
    {
        bool Initialize();
        void Terminate();
        void Monitor();
        void Reset();  
        
        T GetDevice<T>(string name) where T : class,IDevice;
        List<T> GetDevice<T>() where T : class,IDevice;
        List<IDevice> GetAllDevice();
        object GetDevice(string name);
        object GetOptionDevice(string name, Type type);
        bool ContainsDevice(string name);
    }
}
