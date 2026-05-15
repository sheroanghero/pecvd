using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.RT.Fsm
{
    public class DemoEntity : Entity
    {
        public enum STATE
        {
            IDLE,
            RUN,
            ERROR,
        }

        public enum MSG
        {
            INIT,
            RUN,
            STOP,
            ERROR,
        }

        public static void Testcase()
        {
            DemoEntity demo = new DemoEntity();
            demo.Initialize();

            Console.WriteLine("DemoEntity Unit Test.");
            Console.WriteLine("Input \"exit\" for terminate.");

            string cmd;
            while ((cmd = Console.ReadLine()) != "exit")
            {
                switch (cmd)
                {
                    case "run":
                        demo.PostMsg<DemoEntity.MSG>(DemoEntity.MSG.RUN, null);
                        break;
                    case "init":
                        demo.PostMsg<DemoEntity.MSG>(DemoEntity.MSG.INIT, null);
                        break;
                    case "stop":
                        demo.PostMsg<DemoEntity.MSG>(DemoEntity.MSG.STOP, null);
                        break;
                    case "error":
                        demo.PostMsg<DemoEntity.MSG>(DemoEntity.MSG.ERROR, null);
                        break;
                }
            }

            demo.Terminate();

            Console.WriteLine("Input any key for quite.");
            Console.Read();
            Console.WriteLine("DemoEntity Unit Test end");
        }


        public DemoEntity()
        {
            fsm = new StateMachine<DemoEntity>("Demo", (int)STATE.IDLE, 3000);

            EnterExitTransition<STATE, MSG>(STATE.IDLE, EnterIdel, null, null);

            AnyStateTransition(MSG.ERROR, Error, STATE.ERROR);

            Transition(STATE.IDLE, MSG.RUN, Run, STATE.RUN);
            Transition(STATE.ERROR, MSG.INIT, Init, STATE.IDLE);
            Transition(STATE.RUN, MSG.STOP, null, STATE.IDLE);

            Transition(STATE.RUN, FSM_MSG.TIMER, OnTimeout, STATE.RUN);
        }


        private bool Run(object[] objs)
        {
            Console.WriteLine("Demo Run");
            return true;
        }

        private bool Init(object[] objs)
        {
            Console.WriteLine("Demo Init");
            return true;
        }

        private bool Error(object[] objs)
        {
            Console.WriteLine("Demo Error");
            return true;
        }

        private bool EnterIdel(object[] objs)
        {
            Console.WriteLine("Enter idle state");
            return true;
        }


        private bool OnTimeout(object[] objs)
        {
            if (fsm.ElapsedTime > 10000)
            {
                Console.WriteLine("Demo run is timeout");
                PostMsg<MSG>(MSG.ERROR);
            }
            else
            {
                Console.WriteLine("Demo run recive timer msg");
            }
            return true;
        }
    }
}
