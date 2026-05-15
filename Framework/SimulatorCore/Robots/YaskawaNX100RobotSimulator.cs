using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Robots
{
    public class YaskawaNX100RobotSimulator : YaskawaNX100ControllerSimulator
    {

        public YaskawaNX100RobotSimulator()
            : base(10113)
        {

        }



        //$ <UNo> (<SeqNo>) MMAP<TrsSt> <SlotNo> <Sum> <CR>  //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,MTRS,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt>(,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
        //$ <UNo> (<SeqNo>) <StsN> <Ackcd> 01 <Result> 02 <Result> … XX <Result> <Sum> <CR>

        public override void HandleRMAP(string msg)
        {
            Random rd = new Random();

            string unit = msg.Substring(1, 1);
            string seq = msg.Substring(2, 2); 
            string cmd = msg.Substring(4, 4); 
            string trs = msg.Substring(8, 2); 
            string slot = msg.Substring(10, 2);
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");
            string error = 0.ToString("X04");

            //bool random = false;
            bool full = trs == "C1" || trs == "C2";

            //string mapData = GetMapData(random, full);

            bool pre = trs == "C1" || trs == "C3";
            bool mid = trs == "C2" || trs == "C4";
            bool post = trs == "C5" || trs == "C6";
            string mapData = GetMapData(pre, mid, post);

            if (trs == "C1")
                mapData = GetMapData(1,5);
            else
                mapData = GetMapData(0, 1);



            string feedback = string.Format(",{0},{1},{2},{3},{4},", unit, seq, sts, Ackcd,  mapData).Replace(",","").Replace(":", "");
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}","$", feedback, sum);

            OnWriteMessage(result);
        }
  
    }
}