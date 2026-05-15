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
    public abstract class VCEBase : BaseDevice, IDevice
    {
        public virtual bool IsIdle { get; set; }
        public virtual bool IsHomed { get; set; }
        public virtual bool IsDoorOpened { get; set; }
        public virtual bool IsDoorClosed { get; set; }
        public virtual bool IsPlatformIn { get; set; }
        public virtual bool IsPlatformOut { get; set; }
        public virtual bool IsAlarm { get; set; }
        public virtual bool IsConnected { get; set; }
        public virtual bool IsMapped { get; set; }
        public virtual bool IsZAxisAtLoadPostion { get; set; }
        public virtual bool IsZAxisAtHomePostion { get; set; }
        public virtual int ZAxisAtSlot { get; set; }
        public virtual bool IsWaferSlideOut { get; set; }
        public virtual bool IsCassettePresent { get; set; }
        public virtual string SlotMap { get; set; }//from 25->1

        protected VCEBase() : base()
        {

        }
        protected VCEBase(string module, string name) : base(module, name, name, name)
        {

        }

        public virtual bool Initialize()
        {
            OP.Subscribe($"{Module}.{Name}.Home", (string cmd, object[] args) =>
            {
                Home(out _);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.Abort", (string cmd, object[] args) =>
            {
                Abort();
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.Reset", (string cmd, object[] args) =>
            {
                Reset();
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.Unload", (string cmd, object[] args) =>
            {
                Unload(out _);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.Load", (string cmd, object[] args) =>
            {
                Load(out _);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.MoveToSlot", (string cmd, object[] args) =>
            {
                MoveToSlot(Convert.ToInt32(args[0]), out _);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.MoveToLoadPositon", (string cmd, object[] args) =>
            {
                MoveToLoadPositon(out _);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.PlatformIn", (string cmd, object[] args) =>
            {
                PlatformIn(out _);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.PlatformOut", (string cmd, object[] args) =>
            {
                PlatformOut(out _);
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

        public virtual bool Home(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool MoveToSlot(int slot, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool MoveToLoadPositon(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool PlatformIn(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool PlatformOut(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool Load(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool Unload(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool OpenDoor(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool CloseDoor(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool MapCassette(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool QuerySlotMap(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool CheckCassettePresent(bool isCheckPresent, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool CheckOutsideDoorStatus() { throw new NotImplementedException(); }

        public virtual bool CheckWaferSlideOutStatus(out string reason) { throw new NotImplementedException(); }




        public virtual void Abort()
        {
            return;
        }

        public virtual bool SetSpeed(out string reason)
        {
            reason = string.Empty;
            return true;
        }
    }
}