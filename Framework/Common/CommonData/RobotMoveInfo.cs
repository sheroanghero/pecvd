using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.CommonData
{
    [Serializable]
    public enum RobotArm
    {
        ArmA,
        ArmB,
        Both
    }

    [Serializable]
    public enum RobotAction
    {
        None,
        Picking,
        Placing,
        Moving,

        Extending,
        Retracting,
    }

    [Serializable]
    [DataContract]
    public class RobotMoveInfo : NotifiableItem
    {
        private string bladeTarget;

        [DataMember]
        public string BladeTarget
        {
            get { return bladeTarget; }
            set
            {
                bladeTarget = value;
                InvokePropertyChanged("BladeTarget");
            }
        }

        private RobotArm armTarget;

        [DataMember]
        public RobotArm ArmTarget
        {
            get => armTarget;
            set
            {
                armTarget = value;
                InvokePropertyChanged("ArmTarget");
            }
        }

        private RobotAction action;

        [DataMember]
        public RobotAction Action
        {
            get { return action; }
            set
            {
                action = value;
                InvokePropertyChanged("Action");
            }
        }

        public override string ToString()
        {
            return $"{bladeTarget} - {action}";
        }
    }
}
