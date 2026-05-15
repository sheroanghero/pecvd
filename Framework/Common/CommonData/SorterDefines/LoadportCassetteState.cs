using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Sorter.Common
{
    public enum LoadportCassetteState
    {
        None,   //Load sensor/Position sensor: All OFF 
        Normal, //Load sensor/Position sensor: All ON 
        Absent, //Load sensor: ON, Position sensor: 1 or 2: ON 
        Unknown,//Load sensor: ON, Position sensor: All OFF  
        //or Load sensor: OFF, Except Position sensor: All OFF 
    }
}
