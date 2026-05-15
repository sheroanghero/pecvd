using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Fsm;
using Aitex.Core.Util;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Device;

namespace Aitex.Core.RT.Device
{
    public abstract class DeviceEntityT<T> : Entity, IEntity where T :class, IDeviceManager, new()
    {
        public enum STATE
        {
            INIT,
            RUNNING,   
            WAIT_RESET,
            ERROR, 
               
        };

        public enum MSG
        {
            INIT,
            ERROR,   
            WAIT_RESET,
            RESET,
        };

        private T mgr = null;


        public bool IsWaitReset
        {
            get { return fsm.State == (int)STATE.WAIT_RESET; }
        }

        public DeviceEntityT()
        {
            Running = false;
            fsm = new StateMachine<DeviceEntityT<T>>("DeviceLayer", (int)STATE.INIT, 50);

            EnterExitTransition<STATE, MSG>(STATE.INIT, null, MSG.INIT, null);

            Transition(STATE.INIT, MSG.INIT, fInit, STATE.RUNNING);

            Transition(STATE.RUNNING, FSM_MSG.TIMER, fRun, STATE.RUNNING);

            Transition(STATE.RUNNING, MSG.RESET, fReset, STATE.RUNNING);

            
            mgr = Singleton<T>.Instance;
        }

        public bool Check(int msg, out string reason, params object[] args)
        {
            reason = "";
            return true;
        }
        private bool fInit(object[] objs)
        {          
            return true;
        }
        private bool fRun(object[] objs)
        {
            try
            {             
                mgr.Monitor();
            }
            catch (Exception ex)
            {
                Running = false;
                LOG.Error("Device Run exception", ex);
                //throw(ex);
            }

            Running = true;
            return true;
        }

        private bool fReset(object[] objs)
        {
            mgr.Reset();
            return true;
        }
    }




    public class DeviceController : Entity 
    {
        public enum STATE
        {
            INIT,
            RUNNING,
            ERROR,

        };

        public enum MSG
        {
            INIT,
            ERROR,
            RESET,
        };

        private IDeviceManager _deviceManager = null;
 
        public DeviceController()
        {
            fsm = new StateMachine("DeviceController", (int)STATE.INIT, 50);

            EnterExitTransition<STATE, MSG>(STATE.INIT, null, MSG.INIT, null);

            Transition(STATE.INIT, MSG.INIT, fInit, STATE.RUNNING);

            Transition(STATE.RUNNING, FSM_MSG.TIMER, fRun, STATE.RUNNING);

            Transition(STATE.RUNNING, MSG.RESET, fReset, STATE.RUNNING);
        }

        public void Start(IDeviceManager manager)
        {
            _deviceManager = manager;

            Initialize();
        }

        public void Reset()
        {
            PostMsg(MSG.RESET);
        }
 
        public bool Check(int msg, out string reason, params object[] args)
        {
            reason = "";
            return true;
        }

        private bool fInit(object[] objs)
        {
            return true;
        }

        private bool fRun(object[] objs)
        {
            try
            {
                _deviceManager.Monitor();
            }
            catch (Exception ex)
            {
                LOG.Error("Device Run exception", ex);
            }

            
            return true;
        }

        private bool fReset(object[] objs)
        {
            _deviceManager.Reset();
            return true;
        }
    }

}
