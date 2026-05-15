using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Aitex.Core.RT.Event;
using MECF.Framework.RT.Core.IoProviders;

namespace Aitex.Core.RT.IOCore
{
    public delegate void SetValue<T>(int index, T Value);
    public delegate T GetValue<T>(int index);

    public enum IOType
    {
        DI,
        DO,
        AI,
        AO,
    }

    public class IOAccessor<T>
    {
        protected string name;
        protected string addr;

        protected int index;
        protected T[] values;

        protected SetValue<T> setter = null;
        protected GetValue<T> getter = null;

        public T Value
        {
            get
            {
                return getter(index);
            }
            set
            {
                setter(index, value);
            }
        }

        public T[] Buffer
        {
            get
            {
                return values;
            }
        }

        public string Name
        {
            get { return name; }
        }

        public int Index
        {
            get { return index; }
        }

        public int BlockOffset
        {
            get;
            set;
        }

        public string Addr
        {
            get { return addr; }
            set { addr = value; }
        }

        public string Provider
        {
            get;
            set;
        }


        public string Description
        {
            get;
            set;
        }

        public string StringIndex
        {
            get
            {
                return name.Substring(0, 2) + "-" + index.ToString();
            }
        }

        public int IoTableIndex { get; set; }

        public IOAccessor(string name, int index, T[] values)
        {
            this.name = name;
            this.index = index;
            this.values = values;

            getter = GetValue;
            setter = SetValue;
        }

        private T GetValue(int index)
        {
            Debug.Assert(values != null && index >= 0 && index < values.Length);
            return values[index];
        }


        private void SetValue(int index, T value)
        {
            Debug.Assert(values != null && index >= 0 && index < values.Length);
            values[index] = value;
        }
    }

    public class DIAccessor : IOAccessor<bool>
    {
        private IOAccessor<bool> rawAccessor;

        public bool RawData
        {
            get
            {
                return rawAccessor.Value;
            }
            set
            {
                rawAccessor.Value = value;
            }
        }

        public DIAccessor(string name, int index, bool[] values, bool[] raws)
            : base(name, index, values)
        {
            rawAccessor = new IOAccessor<bool>(name, index, raws);
        }
    }

    public class DOAccessor : IOAccessor<bool>
    {
        public DOAccessor(string name, int index, bool[] values)
        : base(name, index, values)
        {
            setter = SetValueSafe;
        }


        public bool SetValue(bool value, out string reason)
        {
            if (IO.CanSetDO(name, value, out reason))
            {
                IIoProvider provider = IoProviderManager.Instance.GetProvider(this.Provider);
                if (provider != null)
                {
                    if (!provider.SetValue(this, value))
                    {
                        reason = $"Write DO[{Name}] failed";
                        return false;
                    }
                }

                values[index] = value;
                return true;
            }
            return false;
        }

        public async Task<bool> SetPulseValue(bool value, int milliSecondsDelay, bool holdValue = false)
        {
            if (IO.CanSetDO(name, value, out string _))
            {
                IIoProvider provider = IoProviderManager.Instance.GetProvider(this.Provider);
                if (provider != null)
                {
                    if (!provider.SetValue(this, value))
                    {
                        return false;
                    }
                }

                values[index] = value;
            }
            else
            {
                return false;
            }

            await Task.Delay(milliSecondsDelay);

            if (IO.CanSetDO(name, holdValue, out string _))
            {
                IIoProvider provider = IoProviderManager.Instance.GetProvider(this.Provider);
                if (provider != null)
                {
                    if (!provider.SetValue(this, holdValue))
                    {
                        return false;
                    }
                }
                values[index] = holdValue;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Check(bool value, out string reason)
        {
            return IO.CanSetDO(name, value, out reason);
        }


        private void SetValueSafe(int index, bool value)
        {
            string reason;
            if (IO.CanSetDO(name, value, out reason))
            {
                values[index] = value;

                IIoProvider provider = IoProviderManager.Instance.GetProvider(this.Provider);
                if (provider != null)
                {
                    provider.SetValue(this, value);
                }
            }
            else
            {
                EV.PostWarningLog("System", $"Can not set DO {name} to {value}, {reason}");
            }
        }
    }

    public class AIAccessor : IOAccessor<short>
    {
        protected float[] _floatValues;

        public float FloatValue
        {
            get
            {
                return _floatValues[index];
            }
            set
            {
                _floatValues[index] = value;
            }
        }

        public AIAccessor(string name, int index, short[] values, float[] floatValues)
        : base(name, index, values)
        {
            _floatValues = floatValues;

        }
    }

    public class AOAccessor : IOAccessor<short>
    {
        protected float[] _floatValues;

        public float FloatValue
        {
            get
            {
                return _floatValues[index];
            }
            set
            {
                IIoProvider provider = IoProviderManager.Instance.GetProvider(this.Provider);
                if (provider != null)
                {
                    provider.SetValueFloat(this, value);
                }

                _floatValues[index] = value;
            }
        }

        public AOAccessor(string name, int index, short[] values, float[] floatValues)
        : base(name, index, values)
        {
            setter = SetValueSafe;
            _floatValues = floatValues;
        }


        private void SetValueSafe(int index, short value)
        {
            values[index] = value;
            IIoProvider provider = IoProviderManager.Instance.GetProvider(this.Provider);
            if (provider != null)
            {
                provider.SetValue(this, value);
            }
        }
    }




}
