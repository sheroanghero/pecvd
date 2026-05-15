using System.Collections.Generic;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK
{
    public enum CommandType
    {
        SET,        // Initialization setting command
        MOD,        // Operation mode setting command
        GET,        // Status acquisition command
        FIN,        // Normal reception command
        MOV,        // Operation command
        EVT,        // Start/Stop event report command
        RST,        // Resend initialization setting command comman
        RFN,        // Resend normal reception command
        RMV,        // Resend operation command
    }

    public enum RespType
    {
        NAK,        // Initialization setting command
        ACK,        // Operation mode setting command
        INF,        // Status acquisition command
        ABS,        // Normal reception command
    }

    //顺序不能变！！！
    public enum Indicator
    {
        LOAD = 0x01,
        UNLOAD = 0x02,
        ACCESSMANUL = 0x03,
        PRESENCE = 0x04,
        PLACEMENT = 0x05,

        ACCESSAUTO = 0x06,
        RESERVE1 = 0x07,
        ALARM = 0x08,
        RESERVE2 = 0x9,

        CLAMP,
        DOCK,
        ACCESS_SW,
    }

    //public enum IndicatorState
    //{
    //    ON,
    //    BLINK,
    //    OFF,
    //}

    public enum Mode
    {
        ONMGV,   //Changes to the online mode
        MENTE,
    }

    public enum QueryType
    {
        STATE,      //Checks the status.
        VERSN,      //Checks the version.
        LEDST,      //Reports the indicator status.
        MAPDT,      //Wafer search data (descending order)
        MAPRD,      //Wafer search data (ascending order)
        WFCNT,      //Wafer quantity check    
        FSBxx       //Checks whether the FOSB Mode is ON/OFF
    }

    public enum EvtType
    {
        EVTON,      //Starts reporting all events.(FOUP events is excluded)
        EVTOF,      //Stops reporting all events. (FOUP events is excluded)
        FPEON,      //Starts reporting FOUP events.
        FPEOF,      //Stops reporting FOUP events.
    }

    public enum MovType
    {
        ORGSH,		//Moves the FOUP to the initial position.
        ABORG,		//Aborts the operation and moves the FOUP to the initial position.
        CLOAD,		//Loads the FOUP (transfers the FOUP to the process unit).
        CLDDK,		//Loads the FOUP (same as CLOAD) to the point where the system is ready to open the door.
        CLDYD, 		//Clamps the FOUP and moves the FOUP to the Y-axis docking position.
        CLDOP, 	    //Continues loading the FOUP after CLDDK.
        CLDMP,		//Maps and loads the FOUP.
        CLMPO,		//Continues mapping and loading the FOUP after CLDDK.
        CULOD, 	    //Unloads the FOUP (at the ejection position).
        CULDK, 		//Closes the door (same as CULOD).
        CUDCL,		//Undocks the FOUP (while being clamped) after CULDK.
        CUDNC, 	    //Unloads the FOUP after CULDK.
        CULYD, 		//Unloads the FOUP to the docking status.
        CULFC, 		//Unloads the FOUP to the point where the system can release (unclamp) the FOUP.
        CUDMP,	    //Maps and unloads the FOUP from the loaded status.
        CUMDK, 	    //Maps the FOUP and closes the door from the loaded status.
        CUMFC, 	    //Maps the FOUP to the before-unclamp status from the loaded status.
        MAPDO, 	    //Maps the FOUP while being loaded.
        REMAP,		//Resumes the interrupted mapping.

        PODOP,      //FOUP clamp: Open
        PODCL,      //FOUP clamp: Close
        VACON,      //Vacuum on
        VACOF,      //Vacuum off
        DOROP,      //Latch key: Open (Unlatches the FOUP door.)
        DORCL,      //Latch key: Close (Latches the FOUP door.)
        MAPOP,      //Mapper arm: Open
        MAPCL,      //Mapper arm: Close
        ZDRUP,      //Move to Z-axis up position (door open position)
        ZDRDW,      //Move to Z-axis down position (transport unit handover        possible position)
        ZDRMP,      //Lower to Z-axis mapping end position and conduct mapping
        ZMPST,      //Move the mapper to the start position
        YWAIT,      //Move to Y-axis undock position
        YDOOR,      //Move to Y-axis dock position
        DORBK,      //Move to door open position
        DORFW,      //Move to door close position 


        RETRY,      //Retry during recoverable error
        STOP_,      //Immediate stop and command abort
        PAUSE,      //Immediate stop
        ABORT,      //Command abort
        RESUM,      //Resume operation
    }

    public class ErrorCode
    {

        private  Dictionary<string, string> dict = new Dictionary<string, string>
        {
            {"02","Z-axis position: NG (Down)"},
            {"42","Z-axis position: NG (Up)"},
            {"04","Y-axis position: NG (Dock)"},
            {"44","Y-axis position: NG (Undock)"},
            {"07","Wafer protrusion"},
            {"47","Grass wafer protrusion (Option)"},
            {"08","Door forward/backward position: NG(Open)"},
            {"48","Door forward/backward position: NG(Close)"},
            {"F2","FOUP door close error."},
            {"09","Mapper arm position: NG (Open)"},
            {"49","Mapper arm position: NG (Close)"},
            
            {"11","Mapper stopper position: NG (On)"},
            {"51","Mapper stopper position: NG (Off)"},
            {"12","Mapping end position: NG"},
            
            {"61","FOUP clamp open error (Up)"},
            {"21","FOUP clamp open error (Back)"},
            {"62","FOUP clamp open error (Down)"},
            {"22","FOUP clamp close error (Front)"},
            {"63","FOUP clamp close error (Middle)"},
            {"23","Latch key open error"},
            {"24","Latch key close error"},           
           
            {"25","Vacuum on error"},
            {"26","Vacuum off error"},
            {"27","Main air error "},
            {"A1","Normal position error at FOUP open"},
            {"A2","Normal position error at FOUP close"},
            {"A3","Mapper storage error when Z-axis lowered"},            
            {"A4","Parallel signal error from upper machine"},

            {"FD","Interlock relay failure"},
            {"FE","Communication failure"},

            {"FF","Obstacle detection sensor failure"},
            {"FC","Fan operation error"},

            {"EE","Mapping mechanical(Adjustment) error"},
            {"EF","Mapping mechanical(Sensor) error"},

            {"F1","Door detection error during dock"},
            {"31","Door detection error except dock"},
        };
        public ErrorCode()
        {
        }

        public string Code2Msg(string code)
        {
            if (dict.ContainsKey(code))
            {
                return string.Format("Code:{0},Message:{1}", code, dict[code]);
            }
            return code;
        }

    }

    /*
        public enum LoadportCassetteState
        {
            None,   //Load sensor/Position sensor: All OFF 
            Normal, //Load sensor/Position sensor: All ON 
            Absent, //Load sensor: ON, Position sensor: 1 or 2: ON 
            Unknown,//Load sensor: ON, Position sensor: All OFF  
                    //or Load sensor: OFF, Except Position sensor: All OFF 
        }

        public enum FoupDoorState
        { 
            Open,
            Close,
            Unknown,
        }
        public enum FoupClampState
        { 
            Open,
            Close,
            Unknown,
        }
    */


}
