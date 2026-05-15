using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems.Rorzes
{
    public enum RorzeEfemMessageType
    {
        //Message which is issued by E/C with active
        MOV,
        GET,
        SET,

        //Event report of completion or information for messages issued by E/C
        INF,
        ABS,
        EVT,

        //Response message to the above primary message.
        ACK,
        NAK,
        CAN,

    }
 
}
