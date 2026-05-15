using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Util;

namespace MECF.Framework.Simulator.Core.SubstrateTrackings
{
    public enum WaferTrackStateEnum
    {
        Unknown,
        Unoccupied,
        Occupied,
        Blocked,
        
    }
    public class WaferTrack : Singleton<WaferTrack>
    {
        Dictionary<string, WaferTrackStateEnum> _states = new Dictionary<string, WaferTrackStateEnum>();


        public   void UpdateWaferTrackState(string location, WaferTrackStateEnum state)
        {
            
            _states[location] = state;
 
 
        }

        internal bool IsOccupied(string location)
        {
            return _states[location] == WaferTrackStateEnum.Occupied;
        }

        internal WaferTrackStateEnum GetLocationState(string location)
        {
            return _states[location];
        }

        internal void UpdateMaterialMap(string location, WaferTrackStateEnum state)
        {
            _states[location] = state;
        }
    }
}
