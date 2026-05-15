using System;
using System.Collections.Generic;

using Aitex.Core.RT.Log;
using System.Collections;

using Aitex.Core.RT.Device;

namespace Aitex.Core.RT.Device
{
    public class DeviceFactory<T> : IEnumerable
                                    where T:class, IDevice
    {
        private object locker = new object();
        protected  Dictionary<string, T> devices = null;

        public string Type { get; protected set; }
        public T this[string name]
        {
            get
            { 
                T value = null;
                lock (locker)
                {
                    if (devices.ContainsKey(name))
                        value = devices[name];
                }

                return value;
            }

            set
            {
                devices[name] = value;
            }
        }


        public IEnumerator GetEnumerator()
        {
            return devices.Values.GetEnumerator();
        }

        public List<T> GetAll()
        {
            return new List<T>(devices.Values);
        }

        /// <summary>
        /// in future, all device define by xml
        /// </summary>
        public DeviceFactory()
        {
            devices = new Dictionary<string, T>();
        }

        public bool Initialize()
        {
            lock (locker)
            {
                Type = DeviceType.UnknownType;

                Product();

                foreach (var device in devices)
                {
                    if ((device.Value != null) && !device.Value.Initialize())
                    {
                        LOG.Warning("Device {0} initialize false", 2, device.Key);
                    }
                }
            }
            return true;
        }

        public void Terminate()
        {
            lock (locker)
            {
                foreach (T device in devices.Values)
                {
                    if (device != null)
                    {
                        device.Terminate();
                    }
                }
            }
        }


        protected virtual void Product()
        {
            
        }



    }
}
