using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Device.Bases
{

    public enum PumpRunStatus
    {
        Stop,
        Running
    }
    public enum ValveStatus
    {
        Closed,
        Opened
    }
    public enum PumpLLStatus
    {
        Off,
        On
    }
    public enum ControlMode
    {
        Local,
        Remote
    }
    public enum FaultResult
    {
        OK,
        Alert,
        Alarm
    }
    public abstract class PumpBase : BaseDevice, IDevice
    {
        public virtual bool IsOn { get; set; }
        public virtual bool IsError { get; set; }
        public virtual bool IsStable { get; set; }
        public virtual bool IsOverTemperature { get; set; }

        public virtual float Speed { get; set; }
        public virtual float Temperature { get; set; }
 
        public virtual AITPumpData DeviceData { get; set; }

        protected PumpBase() : base()
        {

        }
        protected PumpBase(string module, string name) : base(module, name, name, name)
        {

        }

        public virtual bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.IsOn", () => IsOn);
            DATA.Subscribe($"{Module}.{Name}.IsError", () => IsError);
            DATA.Subscribe($"{Module}.{Name}.IsOverTemperature", () => IsOverTemperature);

            DATA.Subscribe($"{Module}.{Name}.Speed", () => Speed);
            DATA.Subscribe($"{Module}.{Name}.Temperature", () => Temperature);


            OP.Subscribe($"{Module}.{Name}.SetPumpOn", (function, args) =>
            {
                SetPumpOnOff(true);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetPumpOff", (function, args) =>
            {
                SetPumpOnOff(false);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.PumpOn", (function, args) =>
            {
                SetPumpOnOff(true);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.PumpOff", (function, args) =>
            {
                SetPumpOnOff(false);
                return true;
            });
            return true;
        }
 
        public virtual void SetPumpOnOff(bool isOn)
        {
             
        }
 
 
        public virtual void Terminate()
        {
        }

        public virtual void Monitor()
        {

        }

        public virtual void Reset()
        {

        }

    }
}