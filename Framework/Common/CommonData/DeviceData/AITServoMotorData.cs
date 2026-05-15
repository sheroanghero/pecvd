using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;
using MECF.Framework.Common.CommonData;

namespace Aitex.Core.Common.DeviceData
{
    [DataContract]
    public enum ServoState
    {
         [EnumMember]
        Unknown,

         [EnumMember]
        NotInitial,

         [EnumMember]
        Idle,

         [EnumMember]
        Moving,

         [EnumMember]
        Error,
    }
    [DataContract]
    [Serializable]
    public class AITServoMotorData : NotifiableItem, IDeviceData
    {
        [DataMember]
        public string Module { get; set; }

        [DataMember]
        public string DeviceName { get; set; }

        /// <summary>
        /// 显示在界面上的名称
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// IO 表中定义的物理编号，物理追溯使用 比如: V122
        /// </summary>
        [DataMember]
        public string DeviceSchematicId { get; set; }

        [DataMember]
        public bool IsServoOn { get; set; }
        [DataMember]
        public bool IsStopped { get; set; }
        [DataMember]
        public bool IsError { get; set; }
        [DataMember]
        public bool IsRunning { get; set; }
        [DataMember]
        public bool IsEmo { get; set; }
        [DataMember]
        public bool IsAuto { get; set; }
        [DataMember]
        public bool IsManual { get; set; }
        [DataMember]
        public bool IsServoNormal { get; set; }
        [DataMember]
        public bool IsServoNoWarning { get; set; }
        [DataMember]
        public bool IsServoNoAlarm { get; set; }
        [DataMember]
        public bool IsNop { get; set; }
        [DataMember]
        public bool IsPositionComplete { get; set; }
        [DataMember]
        public bool IsNLimit { get; set; }
        [DataMember]
        public bool IsPLimit { get; set; }

        #region Boffotto
        [DataMember]
        public bool DiPosFeedBack1 { get; set; }
        [DataMember]
        public bool DiPosFeedBack2 { get; set; }
        [DataMember]
        public bool DiPosFeedBack3 { get; set; }
        [DataMember]
        public bool DiReady { get; set; }
        [DataMember]
        public bool DiOnTarget { get; set; }
        [DataMember]
        public bool DiOnError { get; set; }
        [DataMember]
        public bool DiOnLeftLimit { get; set; }
        [DataMember]
        public bool DiOnRightLimit { get; set; }
        [DataMember]
        public bool DiOnHomeSensor { get; set; }
        [DataMember]
        public bool DoStart { get; set; }
        [DataMember]
        public bool DoPos1 { get; set; }
        [DataMember]
        public bool DoPos2 { get; set; }
        [DataMember]
        public bool DoPos3 { get; set; }
        [DataMember]
        public bool DoHomeOn { get; set; }
        [DataMember]
        public bool DoFreeOn { get; set; }
        [DataMember]
        public bool DoStop { get; set; }
        [DataMember]
        public bool DoReset { get; set; }
        [DataMember]
        public bool DoJogFwd { get; set; }
        [DataMember]
        public bool DoJogRev { get; set; }

        


        #endregion
        [DataMember]
        public int ErrorCode { get; set; }

        [DataMember]
        public float CurrentPosition { get; set; }

        [DataMember]
        public float CurrentSpeed { get; set; }

        [DataMember]
        public string CurrentStatus { get; set; }

        [DataMember]
        public float JogSpeedSetPoint { get; set; }
        [DataMember]
        public float AutoSpeedSetPoint { get; set; }
        [DataMember]
        public float AccSpeedSetPoint { get; set; }

        [DataMember]
        public ServoState State { get; set; }


        public AITServoMotorData()
        {
            DisplayName = "Undefined Servo Motor";
        }

        public void Update(IDeviceData data)
        {

        }
    }

    public class AITServoMotorOperation
    {
        public const string Home = "Home";
        public const string SetServoOn = "SetServoOn";
        public const string SetServoOff = "SetServoOff";

        public const string MoveTo = "MoveTo";
        public const string MoveBy = "MoveBy";

        public const string Reset = "Reset";
        public const string Stop = "Stop";
    }

    public class AITServoMotorProperty
    {
        public const string IsServoOn = "IsServoOn";

        public const string IsStopped = "IsStopped";
        public const string IsError = "IsError";

        public const string CurrentPosition = "CurrentPosition";

        public const string CurrentStatus = "CurrentStatus";

        public const string IsWalkingShaftStart = "IsWalkingShaftStart";

        public const string IsWalkingShaftIn1 = "IsWalkingShaftIn1";
        public const string IsWalkingShaftIn2 = "IsWalkingShaftIn2";

        public const string IsWalkingShaftIn3 = "IsWalkingShaftIn3";

        public const string IsWalkingShaftHome = "IsWalkingShaftHome";

        public const string IsWalkingShaftFree = "IsWalkingShaftFree";

        public const string IsWalkingShafStop = "IsWalkingShafStop";
        public const string IsWalkingShaftReset = "IsWalkingShaftReset";

        public const string IsWalkingShaftJogFwd = "IsWalkingShaftJogFwd";

        public const string IsWalkingShaftJogRev = "IsWalkingShaftJogRev";

    }
}
