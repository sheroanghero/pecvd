using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.Simulator.Core.Robots
{
    class TazmoRobotSerialSimulator : TazmoRobotSerialControllerSimulator
    {

        public TazmoRobotSerialSimulator()
            : base("COM20")
        {

        }

      

        //$ <UNo> (<SeqNo>) MMAP<TrsSt> <SlotNo> <Sum> <CR>  //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,MTRS,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt>(,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
        //$ <UNo> (<SeqNo>) <StsN> <Ackcd> 01 <Result> 02 <Result> … XX <Result> <Sum> <CR>

        
    }
}
