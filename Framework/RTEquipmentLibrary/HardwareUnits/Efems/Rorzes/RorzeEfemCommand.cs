using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems.Rorzes
{
    public enum RorzeEfemBasicMessage
    {
        READY,      //Ready , ACK, INF
        INIT,       //EFEM initialization MOV, ACK, INF
        ORGSH,      //Origin search    

        LOCK,       //Pod lock MOV, ACK, INF
        UNLOCK,     //Pod unlock
        DOCK,       //Moving to pod dock position MOV, ACK, INF
        UNDOCK,     //Moving to pod undock position MOV, ACK, INF
        OPEN,       //Pod open MOV, ACK, INF
        CLOSE,      //Pod close MOV, ACK, INF

        WAFSH,      //Wafer mapping MOV, ACK, INF
        MAPDT,      //Wafer map information GET, ACK, INF, EVT

        LOAD,       //Wafer carry-out MOV, ACK, INF
        UNLOAD,     //Wafer carry-in MOV, ACK, INF

        GOTO,       //Move to specified object MOV, ACK, INF
        TRANS,      //Wafer transfer MOV, ACK, INF
        CHANGE,     //Wafer exchange MOV, ACK, INF

        ALIGN,      //Alignment MOV, SET, ACK, INF

        HOME,       //Home MOV, ACK, INF
        HOLD,       //Hold MOV, ACK, INF
        RESTR,      //Restart MOV, ACK
        ABORT,      //Abort termination MOV, ACK, INF
        EMS,        //Emergency stop MOV, ACK, INF
        ERROR,      //Error GET, SET, ACK, INF

        CLAMP,      //Clamp output, get state GET, SET, ACK, INF
        STATE,      //Get status GET, ACK, INF

        MODE,       //E84 mode setting GET, SET, ACK, INF
        TRANSREQ,   //E84 automatic transfer request   MOV, GET, ACK, INF, EVT

        SIGOUT,     //Signal output SET, ACK
        SIGSTAT,    //Signal input/output information GET, ACK, INF, EVT


        EVENT,      //Event setting GET, SET, ACK, INF
        CSTID,      //Carrier ID GET, ACK, INF

        USDEFINE,   //User Define 

        FFU,        //Ffu  SET,ACK,INF
        WTYPE,      //Wtype Set and acquire wafer type  SET,ACK,GET,ACK,INF
        PURGE,      //Purge SET,ACK,GET,ACK,INF
        ADPLOCK,    //Adplock MOV,ACK,INF
        ADPUNLOCK,  //Adpunlock SET,ACK,INF
        LED,        //Led SET,ACK,INF
        WORKCHK,    //Workchk MOV,ACK,INF

    }
 
}
