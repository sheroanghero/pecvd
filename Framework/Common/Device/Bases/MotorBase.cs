using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.OperationCenter;

namespace MECF.Framework.Common.Device.Bases
{
    public abstract class MotorBase : BaseDevice, IDevice
    {
        public virtual bool IsServoOn { get; set; }
        public virtual bool IsError { get; set; }
        public virtual bool IsHomed { get; set; }
        public virtual bool IsMoving { get; set; }
        public virtual bool IsInTargetPosition { get; set; }

        public virtual float CurrentPosition { get; set; }
        public virtual float Target { get; set; }
 
        public virtual AITServoMotorData DeviceData { get; set; }

        protected MotorBase() : base()
        {

        }
        protected MotorBase(string module, string name) : base(module, name, name, name)
        {

        }

        public virtual bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            DATA.Subscribe($"{Module}.{Name}.IsServoOn", () => IsServoOn);
            DATA.Subscribe($"{Module}.{Name}.IsError", () => IsError);
            DATA.Subscribe($"{Module}.{Name}.IsHomed", () => IsHomed);
            DATA.Subscribe($"{Module}.{Name}.IsMoving", () => IsMoving);
            DATA.Subscribe($"{Module}.{Name}.IsInTargetPosition", () => IsInTargetPosition);
            DATA.Subscribe($"{Module}.{Name}.CurrentPosition", () => CurrentPosition);

            OP.Subscribe($"{Module}.{Name}.Home", (function, args) =>
            {
                Home();
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.ServoOn", (function, args) =>
            {
                ServoOn();
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetSpeed", (function, args) =>
            {
                SetSpeed((float)args[0]);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.SetPosition", (function, args) =>
            {
                SetPosition((float)args[0]);
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.StopMotion", (function, args) =>
            {
                StopMotion( );
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.ResetError", (function, args) =>
            {
                ResetError( );
                return true;
            });

            return true;
        }

        public virtual void Home()
        {
             
        }

        public virtual void ServoOn()
        {
             
        }

        public virtual void SetSpeed(float speed)
        {
             
        }

        public virtual void SetPosition(float position)
        {
             
        }

        public virtual void StopMotion()
        {
             
        }

        public virtual void ResetError()
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