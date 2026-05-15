using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.SubstrateTrackings
{
    public enum TransferStateEnum
    {
 
        AtSource,
 
        AtDestination,
 
        Enroute,
 
        Lost
    }

    public enum ProcessStateEnum
    {
 
        Unprocessed,
 
        InProcess,
 
        Processed,
 
        Skipped,
 
        Aborted,
 
        Stopped,
 
        Rejected
    }
}
