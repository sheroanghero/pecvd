using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Sorter.Common
{
    public enum DeviceState
    {
        Unknown,
        Busy,
        Idle,
        Error,
    }

    public enum TDKSystemStatus
    {
        Normal = 0x30,
        RecoverableError = 0x41,
        UnrecoverableError = 0x45,
    }
    public enum TDKMode
    {
        Online = 0x30,
        Teaching = 0x31,
        Maintenance = 0x32,
    }
    public enum TDKInitPosMovement
    {
        OperationStatus = 0x30,
        HomePosStatus = 0x31,
        LoadStatus = 0x32,
    }
    public enum TDKOperationStatus
    {
        DuringStop = 0x30,
        DuringOperation = 0x31,
    }
    public enum TDKContainerStatus
    {
        Absence = 0x30,
        NormalMount = 0x31,
        MountError = 0x32,
    }
    public enum TDKPosition
    {
        Open = 0x30,
        Close = 0x31,
        TBD = 0x3F
    }
    public enum TDKVacummStatus
    {
        OFF = 0x30,
        ON = 0x31,
    }
    public enum TDKWaferProtrusion
    {
        ShadingStatus = 0x30,
        LightIncidentStatus = 0x31,
    }
    public enum TDKElevatorAxisPosition
    {
        UP = 0x30,
        Down = 0x31,
        MappingStartPos = 0x32,
        MappingEndPos = 0x33,
        TBD = 0x3F,
    }
    public enum TDKDockPosition
    {
        Undock = 0x30,
        Dock = 0x31,
        TBD = 0x3F,
    }
    public enum TDKMapPosition
    {
        MeasurementPos = 0x30,
        WaitingPost = 0x31,
        TBD = 0x3F,
    }
    public enum TDKMappingStatus
    {
        NotPerformed = 0x30,
        NormalEnd = 0x31,
        ErrorStop = 0x32,
    }
    public enum TDKModel
    {
        Type1 = 0x30,
        Type2 = 0x31,
        Type3 = 0x32,
        Type4 = 0x33,
        Type5 = 0x34,
    }





    public enum TDKEquipmentState
    {
        Normal =0,
        Recoverable_error,
        Fatal_error,

    }
   
    public enum TDKInitialPosition
    {
        Unexecuted=0,
        Executed,
    }
   
    public enum TDKLatchKepStatus
    {
        Open=0,
        Close,
        Unknown,
    }
    public enum TDKVacuum
    {
        OFF=0,
        ON,
    }
   
    public enum TDKZ_AxisPos
    {
        Up=0,
        Down,
        Start,
        End,
        Unknown,
    }
    public enum TDKY_AxisPos
    {
        Undock=0,
        Dock,
        Unknown,
    }
    public enum TDKMapper_ArmPos
    {
        Open=0,
        Close,
        Unknown,
    }
    public enum TDKMapper_Z_AxisPos
    {
        Retract=0,
        Mapping,
        Unknown,
    }
    public enum TDKMapper_StopperStatus
    {
        ON=0,
        OFF,
        Unknown,
    }
  
    public enum TDKInterlockKey
    {
        Enable=0,
        Disable,
    }
    public enum TDKInfoPad
    {
        NO_Input=0,
        A_PIN_ON,
        B_PIN_ON,
        AB_PIN_ON,

    }

}
