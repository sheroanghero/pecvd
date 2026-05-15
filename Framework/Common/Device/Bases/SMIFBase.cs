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
    public abstract class SMIFBase : BaseDevice, IDevice
    {
        public virtual bool IsIdle { get; set; }
        public virtual bool IsHomed { get; set; }
        public virtual bool IsPodPresent { get; set; }
        public virtual bool IsArmRetract { get; set; }
        public virtual bool IsAlarm { get; set; }
        public virtual bool IsConnected { get; set; }

        protected SMIFBase() : base()
        {

        }
        protected SMIFBase(string module, string name) : base(module, name, name, name)
        {

        }

        public virtual bool Initialize()
        {
            OP.Subscribe($"{Module}.{Name}.Home", (string cmd, object[] args) =>
            {
                HomeSmif(out _);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.Abort", (string cmd, object[] args) =>
            {
                Stop();
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.Reset", (string cmd, object[] args) =>
            {
                Reset();
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.Unload", (string cmd, object[] args) =>
            {
                UnloadCassette(out _);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.Load", (string cmd, object[] args) =>
            {
                LoadCassette(out _);
                return true;
            });
            return true;
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

        public virtual bool HomeSmif(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool LoadCassette(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool UnloadCassette(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual void Stop()
        {
            return;
        }

    }
}