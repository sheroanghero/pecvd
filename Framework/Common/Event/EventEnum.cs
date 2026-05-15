using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Aitex.Common.Util;

namespace Aitex.Core.RT.Event
{
    [DataContract]
    [Serializable]
    public enum EventEnum
    {
 
        [EnumMember]
        OperationAuthorization = 9,
 
        [EnumMember]
        UserLoggedOff = 10,
 
        [EnumMember]
        UserLoggedIn = 11,
 
        [EnumMember]
        AccountChanged = 12,
 
        [EnumMember]
        PasswordChanged = 13,
 
        [EnumMember]
        AccountDeleted = 14,
 
        [EnumMember]
        AccountCreated = 15,
 
        [EnumMember]
        PuttingWaferToChamberEnds = 19,
 
        [EnumMember]
        PuttingWaferToChamberBegins = 20,
 
        [EnumMember]
        PickingWaferFromChamberEnds = 21,
 
        [EnumMember]
        PickingWaferFromChamberBegins = 22,
 
        [EnumMember]
        AlignBegins = 25,
 
        [EnumMember]
        AlignEnds = 26,
 
        [EnumMember]
        AlignFailed = 27,
 
        [EnumMember]
        WaferMoved = 28,

        /// <summary>
        /// Wafer create
        /// 
        /// </summary>
        [EnumMember]
        WaferCreate = 36,

        /// <summary>
        /// 
        /// 
        /// /// </summary>
        [EnumMember]
        WaferDelete = 37,
 

        /// <summary>
        /// 
        /// LOAD FOUP start
        /// </summary>
        [EnumMember]
        LoadFOUPStart = 41,

        /// <summary>
        /// 
        /// LOAD FOUP End
        /// /// </summary>
        [EnumMember]
        LoadFOUPEnd = 42,

        /// <summary>
        /// 
        /// 
        /// </summary>
        [EnumMember]
        LoadFOUPFailed = 43,

        /// <summary>
        /// Unload FOUP Start
        /// 
        /// /// </summary>
        [EnumMember]
        UnloadFOUPStart = 44,

        /// <summary>
        /// Unload FOUP Start
        /// </summary>
        [EnumMember]
        UnloadFOUPEnd = 45,

        /// <summary>
        /// UnLoad FOUP Failed
        /// 
        /// </summary>
        [EnumMember]
        UnloadFOUPFailed = 46,
 
        [EnumMember]
        GeneralInfo = 1000,
 
        [EnumMember]
        ServiceRoutineAborted = 1023,
 
        [EnumMember]
        ServiceRoutineInfo = 1033,
  
        [EnumMember]
        GuiCmdExecSucc = 1040,
 
        [EnumMember]
        SwInterlock = 1052,
 
        [EnumMember]
        AccountWithoutAuthorization = 2052,
 
        [EnumMember]
        DefaultWarning = 2053,
 
        [EnumMember]
        GuiCmdExecFailed = 3027,
 
        [EnumMember]
        DbConnFailed = 3034,
 
        [EnumMember]
        SafePlcInterlock = 3038,
 
        [EnumMember]
        ValveOperationFail = 3039,
 
        [EnumMember]
        TransferPrepareFailed = 4050,
 
        [EnumMember]
        WaferAbsentWithoutRecord = 4051,
 
        [EnumMember]
        WaferPresentWithRecord = 4052,
 
        [EnumMember]
        PlaceWaferFailed = 4054,
 
        [EnumMember]
        PickingWaferFromChamberFails = 4055,
 
        [EnumMember]
        WaferDetectedAfterSend = 4061,
 
        [EnumMember]
        WaferNotDetectedBeforeSend = 4062,
 
        [EnumMember]
        WaferNotDetectedAfterPick = 4063,
 
        [EnumMember]
        WaferDetectedBeforePick = 4064,

        [EnumMember]
        DefaultAlarm = 4093,

        [EnumMember]
        ThrottleValveAbnormal = 5020,

        [EnumMember]
        PlcHeartBeatFail = 5027,

        [EnumMember]
        TCPConnSucess = 5120,

        [EnumMember]
        IOPointAlarm = 5123,
        [EnumMember]
        IOPointAlarmReset = 5124,

        [EnumMember]
        CommunicationError = 5125,

        [EnumMember]
        ToleranceAlarm = 5126,

        [EnumMember]
        TCBroken = 5128,

 

        [EnumMember]
        RunningModeChanged = 5130,

        [EnumMember]
        HomeBegins = 5200,

        [EnumMember]
        HomeEnds = 5201,


        [EnumMember]
        HomeFailed = 5202,

 

        [EnumMember]
        ManualOpAccess = 5205,
        

        [EnumMember]
        CarrierIdRead = 5206,

        [EnumMember]
        CarrierIdReadFailed = 5207,


        [EnumMember]
        SlotMapAvailable = 5208,


        [EnumMember]
        CarrierCreate = 5209,

        [EnumMember]
        CarrierDelete = 5210,

    }
}
