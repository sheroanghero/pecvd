using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners
{
    public class AlignerBase : BaseDevice, IDevice
    {
        public virtual bool IsBusy { get; set; }

        public virtual bool IsError { get; set; }



        public AlignerBase(string module, string name, string display, string deviceId)
            : base(module, name, display, deviceId)
        {

        }

        public virtual bool Initialize()
        {
            WaferManager.Instance.SubscribeLocation(Module, 1);

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
            IsError = false;
        }


        public virtual bool Home(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool Clear(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool Grip(Hand hand, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool Release(Hand hand, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool LiftUp(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool LiftDown(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool Stop(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool Align(double angle, out string reason)
        {
            reason = string.Empty;
            return true;
        }


        public virtual bool QueryState(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual void OnError()
        {
            IsError = true;
        }

        public virtual bool QueryWaferPresence(out string reason)
        {
            reason = string.Empty;

            return true;
        }

        public virtual void OnAligned()
        {

        }

    }
}
