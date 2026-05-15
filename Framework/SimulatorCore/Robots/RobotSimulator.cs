using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Robots
{
    public enum RobotStateEnum
    {
        Idle,
        Homing,
        Picking,
        Placing,
        Exchanging,
        Approaching,
        Extending,
        Retracting,
        Errored,
        SVON,
        SVOFF
    }

    public class RobotStateEventArgs : EventArgs
    {
        public RobotStateEnum State { get; set; }

        public DateTime TimeStamp { get; set; }

        public RobotStateEventArgs(RobotStateEnum newState, DateTime time)
        {
            State = newState;
            TimeStamp = time;
        }
    }

    public abstract class RobotSimulator : SocketDeviceSimulator 
    {
        public string ErrorMessage
        {
            get { return errorMessage; }
            set { errorMessage = value; }
        }
        public string RobotType
        {
            get { return robotType; }
            set { robotType = value; }
        }

        public virtual Dictionary<string, double> MoveTimes
        {
            get { return moveTimes; }
            set { moveTimes = value; }
        }

        public virtual ReadOnlyCollection<string> Arms
        {
            get { return arms; }
        }
        private string robotType;
        private event EventHandler<RobotStateEventArgs> RobotStateChange;
        protected RobotStateEventArgs robotStateArgs;
         private string errorMessage;
        private Dictionary<string, double> moveTimes;
        private ReadOnlyCollection<string> arms;
        protected Dictionary<string, string> errorLookup;
        protected int lastError;

        protected RobotSimulator(int port, int commandIndex, string lineDelimiter, char msgDelimiter, int cmdMaxLength = 4) 
            : base(port,commandIndex,lineDelimiter,msgDelimiter,cmdMaxLength)
        {
            //this.robotType = parms["RobotType"].Value;

            robotStateArgs = new RobotStateEventArgs(RobotStateEnum.Idle, DateTime.Now);
            SetRobotState(RobotStateEnum.Idle);

            moveTimes = new Dictionary<string, double>();
            moveTimes["Realistic Delay"] = 1.0;

            arms = new ReadOnlyCollection<string>(new List<string>());
            errorLookup = new Dictionary<string, string>();
            lastError = 0;
        }

 

        protected string LookupError(string errorCode)
        {
            if (errorLookup.ContainsKey(errorCode) == false)
                return "Error not found";

            string errorMsg = errorLookup[errorCode];
            errorLookup.Remove(errorCode);
            return errorMsg;
        }

 
        public void AttachToRobotState(EventHandler<RobotStateEventArgs> target)
        {
            target(this, robotStateArgs);
            RobotStateChange += target;
        }

 
        protected void SetRobotState(RobotStateEnum newState)
        {
            robotStateArgs.State = newState;
            robotStateArgs.TimeStamp = DateTime.Now;
            if (RobotStateChange != null)
                RobotStateChange(this, robotStateArgs);
        }

 
    }


}