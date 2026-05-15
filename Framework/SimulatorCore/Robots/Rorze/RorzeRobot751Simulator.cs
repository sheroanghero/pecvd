using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MECF.Framework.Common.Communications.Tcp.Socket.Server.APM.EventArgs;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Robots
{
    public class RorzeRobot751Simulator : RorzeRobot751ControllerSimulator
    {
        public RorzeRobot751Simulator()
            : base(10110)
        {
 
        }

        private string _msgHeader = "TRB1";
        public override bool ProcessReceivedData(TcpClientDataReceivedEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Data, e.DataOffset, e.DataLength);
            AddMessageIn(message);
            _msgHeader = message.Substring(1, 4);
            string command = message.Substring(6, 4);

            switch(command)
            {
                case "MODE":
                case "RSTA":
                case "EVNT":
                case "SSPD":
                    //Thread.Sleep(3000);
                    SendAckMsg(command);
                    break;

                case "STAT":
                    //Thread.Sleep(10000);
                    SendEvent("STAT", "11000/0000");
                    Thread.Sleep(2000);
                    SendAckMsg(command, "11000/0000");
                    break;
                case "EXST":
                    SendAckMsg(command, "0");
                    break;

                case "ORGN":
                    SendAckMsg(command, "11000/0000");
                    SendEvent("STAT", "10110/0000");
                    SendEvent("GPIO", "00000020102C03E0/0000000FDF080141");
                    Thread.Sleep(2000);
                    SendEvent("STAT", "11000/0000");
                    break;
                case "HOME":
                case "EXTD":
                case "LOAD":
                case "UNLD":
                case "TRNS":
                case "EXCH":
                case "CLMP":
                case "UCLM":
                case "WMAP":
                case "UTRN":
                case "MGET":
                case "MPUT":
                case "MGT1":
                case "MGT2":
                case "MPT1":
                    SendAckMsg(command, "11000/0000");
                    SendEvent("STAT", "11110/0000");
                    Thread.Sleep(2000);
                    SendEvent("STAT", "11000/0000");
                    break;
                case "INIT":
                    SendAckMsg(command, "");
                    SendEvent("STAT", "01110/0000");
                    Thread.Sleep(2000);
                    SendEvent("STAT", "11000/0000");
                    break;
            }

            




            return true;
        }

        private void SendAckMsg(string command,string data="")
        {
            string msg = $"a{_msgHeader}.{command}:{data}\r";
            OnWriteMessage(msg);
        }

        private void SendEvent(string eventName,string data)
        {
            string msg = $"e{_msgHeader}.{eventName}:{data}\r";
            OnWriteMessage(msg);
        }


        //$,<UNo>(,<SeqNo>),INIT,<ErrClr>,<SrvOn>,<Home>(,<Sum>)<CR>
        //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,INIT,<ErrClr>,<SrvOn>,<Axis>(,<Sum>)<CR>
        //!,<UNo>(,<SeqNo>),<Sts>,<Errcd>,INIT,<ExeTime>,<PosData1>…,<PosDataN>(,<Sum>)<CR>
        public override  void HandleINIT(string msg)
        {
            string[] arrayMsg = msg.Split(',');
            string unit = arrayMsg[1];
            string seq = arrayMsg[2];
            string cmd = arrayMsg[3];
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");
            string errorClr = arrayMsg[4];
            string srvOn = arrayMsg[5];
            string axis = arrayMsg[6];


            string feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},{7},", unit, seq, sts, Ackcd, cmd, errorClr, srvOn, axis);
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", arrayMsg[0], feedback, sum);

            OnWriteMessage(result);


            Thread.Sleep(3000);

            Random rd = new Random();
            int executeTime = rd.Next(10000, 999999);
            string exeTime = executeTime.ToString("D6");
            string posData = GetPosData();
            string error = 0.ToString("X04");

            feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},", unit, seq, sts, error, cmd, exeTime, posData);
            sum = CheckSum(feedback);
            result = String.Format("{0}{1}{2}", "!", feedback, sum);

            OnWriteMessage(result);
        }

        //$,<UNo>(,<SeqNo>),CCLR,<CMode>(,<Sum>)<CR>
        //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,CCLR,<CMode>(,<Sum>)<CR>
        //!,<UNo>(,<SeqNo>),<Sts>,<Errcd>,CCLR,<ExeTime>,<PosData1>…,<PosDataN>(,<Sum>)<CR>
        public override  void HandleCCLR(string msg)
        {
            string[] arrayMsg = msg.Split(',');
            string unit = arrayMsg[1];
            string seq = arrayMsg[2];
            string cmd = arrayMsg[3];
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");
            string cmode = arrayMsg[4];

            string feedback = string.Format(",{0},{1},{2},{3},{4},{5},", unit, seq, sts, Ackcd, cmd, cmode);
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", arrayMsg[0], feedback, sum);

            OnWriteMessage(result);


            Thread.Sleep(3000);

            int executeTime = _rd.Next(10000, 999999);
            string exeTime = executeTime.ToString("D6");
            string posData = GetPosData();
            string error = 0.ToString("X04");

            feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},", unit, seq, sts, error, cmd, exeTime, posData);
            sum = CheckSum(feedback);
            result = String.Format("{0}{1}{2}", "!", feedback, sum);

            OnWriteMessage(result);

        }



        //$,<UNo>(,<SeqNo>),RSTS(,<Sum>)<CR>
        //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,RSTS,<Errcd>,<Status>(,<Sum>)<CR>
        public override void HandleRSTS(string msg)
        {
            string[] arrayMsg = msg.Split(',');
            string unit = arrayMsg[1];
            string seq = arrayMsg[2];
            string cmd = arrayMsg[3];
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");
            string error = 0.ToString("X04");
            int status1 = Convert.ToInt32("0011", 2);
            string status = string.Format("{0}000", status1.ToString("X1"));

            string feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},", unit, seq, sts, Ackcd, cmd, error, status);
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", arrayMsg[0], feedback, sum);

            OnWriteMessage(result);

        }


        //$,<UNo>(,<SeqNo>),MALN,<Mode>,<Angle>(,<Sum>)<CR>
        //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,MALN,<Mode>,<Angle>(,<Sum>)<CR>
        //!,<UNo>(,<SeqNo>),<Sts>,<Errcd>,MALN,<ExeTime>,<PosData1>…,<PosDataN>,<Value1>…,<Value10>(,<Sum>)<CR>
        public override void HandleMALN(string msg)
        {
            string[] arrayMsg = msg.Split(',');
            string unit = arrayMsg[1];
            string seq = arrayMsg[2];
            string cmd = arrayMsg[3];
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");
            string mode = arrayMsg[4];
            string angle = arrayMsg[5];

            string feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},", unit, seq, sts, Ackcd, cmd, mode, angle);
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", arrayMsg[0], feedback, sum);

            OnWriteMessage(result);


            Thread.Sleep(3000);

            string error = 0.ToString("X04");
            int executeTime = _rd.Next(10000, 999999);
            string exeTime = executeTime.ToString("D6");
            string posData = GetPosData();
            string valueData = GetValueData();

            feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},{7},", unit, seq, sts, error, cmd, exeTime, posData, valueData);
            sum = CheckSum(feedback);
            result = String.Format("{0}{1}{2}", "!", feedback, sum);

            OnWriteMessage(result);

        }

        //$,<UNo>(,<SeqNo>),MTRS,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt>(,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
        //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,MTRS,<Mtn>,<TrsSt>,<Slot>,<Posture>,<Hand>,<TrsPnt>(,<OfstX>,<OfstY>,<OfstZ>)(,<Angle>)(,<Sum>)<CR>
        //!,<UNo>(,<SeqNo>),<Sts>,<Errcd>,MTRS,<ExeTime>,<PosData1>…,<PosDataN>(,<Sum>)<CR>
        public override void HandleMTRS(string msg)
        {
            string[] arrayMsg = msg.Split(',');
            string unit = arrayMsg[1];
            string seq = arrayMsg[2];
            string cmd = arrayMsg[3];
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");
            string leftAll = "";
            for (int i = 4; i < arrayMsg.Length - 1; i++)
                leftAll += arrayMsg[i] + ",";

            string feedback = string.Format(",{0},{1},{2},{3},{4},{5}", unit, seq, sts, Ackcd, cmd, leftAll);
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", arrayMsg[0], feedback, sum);

            OnWriteMessage(result);


            Thread.Sleep(3000);

            string error = 0.ToString("X04");
            int executeTime = _rd.Next(10000, 999999);
            string exeTime = executeTime.ToString("D6");
            string posData = GetPosData();

            feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},", unit, seq, sts, error, cmd, exeTime, posData);
            sum = CheckSum(feedback);
            result = String.Format("{0}{1}{2}", "!", feedback, sum);

            OnWriteMessage(result);
            OnWriteMessage(result);

        }

        //$,<UNo>(,<SeqNo>),RMAP,<TrsSt>,<Slot>(,<Sum>)<CR>
        //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,RMAP,<TrsSt>,<Slot>,01:<Result1>…,N:<ResultN>(,<Sum>)<CR>

        public override void HandleRMAP(string msg)
        {
            Random rd = new Random();
            string[] arrayMsg = msg.Split(',');
            string unit = arrayMsg[1];
            string seq = arrayMsg[2];
            string cmd = arrayMsg[3];
            string trs = arrayMsg[4];
            string slot = arrayMsg[5];
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");
            string error = 0.ToString("X04");

            //bool random = false;
            bool full = trs == "C01" || trs == "C02";

            //string mapData = GetMapData(random, full);

            bool pre = trs == "C01" || trs == "C02";
            bool mid = trs == "C03" || trs == "C04";
            bool post = trs == "C05" || trs == "C06";

            string mapData = full? GetMapData(true, true, true) : GetMapData(pre, mid, post);

            if (trs == "C01")
                mapData = GetMapData(1, 2);
            if (trs == "C02")
                mapData = GetMapData(4, 5);
            if (trs == "C07")
                mapData = GetMapData(4, 5);
            if (trs == "C08")
                mapData = GetMapData(4, 5);
            if (trs == "C06")
                mapData = GetMapData(4, 5);

            string feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},{7},", unit, seq, sts, Ackcd, cmd, trs, slot, mapData);
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", arrayMsg[0], feedback, sum);

            OnWriteMessage(result);
        }

        //$,<UNo>(,<SeqNo>),MMAP,<TrsSt>,<Slot>(,<Safe>)(,<Sum>)<CR>
        //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,MMAP,<TrsSt>,<Slot>(,<Safe>)(,<Sum>)<CR>
        //!,<UNo>(,<SeqNo>),<Sts>,<Errcd>,MMAP,<ExeTime>,<PosData1>,…<PosDataN> ,01:<Result1>…,N:<ResultN>(,<Sum>)<CR>
        public override void HandleMMAP(string msg)
        {
            string[] arrayMsg = msg.Split(',');
            string unit = arrayMsg[1];
            string seq = arrayMsg[2];
            string cmd = arrayMsg[3];
            string trs = arrayMsg[4];
            string slot = arrayMsg[5];
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");
            string leftAll = "";
            for (int i = 4; i < arrayMsg.Length - 1; i++)
                leftAll += arrayMsg[i] + ",";

            string feedback = string.Format(",{0},{1},{2},{3},{4},{5}", unit, seq, sts, Ackcd, cmd, leftAll);
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", arrayMsg[0], feedback, sum);

            OnWriteMessage(result);


            Thread.Sleep(2000);

            string error = 0.ToString("X04");
            int executeTime = _rd.Next(10000, 999999);
            string exeTime = executeTime.ToString("D6");
            string posData = GetPosData();
            //bool random = false;
            bool full = trs == "C01" || trs == "C02";

            bool pre = trs == "C03" || trs == "C04" || trs == "C05" || trs == "C06";
            bool mid = trs == "C03" || trs == "C04"  ;
            bool post = trs == "C05" || trs == "C06";

            string mapData = full ? GetMapData(true, true, true) : GetMapData(pre, mid, post);

            feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},{7},", unit, seq, sts, error, cmd, exeTime, posData, mapData);
            sum = CheckSum(feedback);
            result = String.Format("{0}{1}{2}", "!", feedback, sum);

            OnWriteMessage(result);
        }

        public override void HandleRMPD(string msg)
        {
            Random rd = new Random();
            string[] arrayMsg = msg.Split(',');
            string unit = arrayMsg[1];
            string seq = arrayMsg[2];
            string cmd = arrayMsg[3];
            string trs = arrayMsg[4];
            //string slot = arrayMsg[5];
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");
            string error = 0.ToString("X04");

            //bool random = false;
            bool full = trs == "C01" || trs == "C02";

            //string mapData = GetMapData(random, full);

            bool pre = trs == "C01" || trs == "C02";
            bool mid = trs == "C03" || trs == "C04";
            bool post = trs == "C05" || trs == "C06";

            string mapData = full ? GetMapData(true, true, true) : GetMapData(pre, mid, post);

            if (trs == "C01")
                mapData = GetMapThickData(0, 1);
            if (trs == "C02")
                mapData = GetMapThickData(1, 5);
            mapData = GetMapThickData(1, 5);

            string feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},", unit, seq, sts, Ackcd, cmd, trs, mapData);
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", arrayMsg[0], feedback, sum);

            OnWriteMessage(result);
        }
        public override void HandleCSOL(string msg)
        {
            Response("CSOL", msg);

            Thread.Sleep(3000);

            string[] arrayMsg = msg.Split(',');
            string unit = arrayMsg[1];
            string seq = arrayMsg[2];
            string sts = 0.ToString("X02");
            string error = 0.ToString("X04");
            string type = arrayMsg[3];
            int executeTime = _rd.Next(10000, 999999);
            string exeTime = executeTime.ToString("D6");
            string posData = GetPosData();
            int status1 = Convert.ToInt32("0011", 2);
            string status = string.Format("{0}000", status1.ToString("X1"));
            string feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},", unit, seq, sts, error, type, exeTime, posData);
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", "!", feedback, sum);

            OnWriteMessage(result);
        }

        public override void HandleMPNT(string msg)
        {
            Response("MPNT", msg);

            Thread.Sleep(3000);

            string[] arrayMsg = msg.Split(',');
            string unit = arrayMsg[1];
            string seq = arrayMsg[2];
            string sts = 0.ToString("X02");
            string error = 0.ToString("X04");
            string type = arrayMsg[3];
            int executeTime = _rd.Next(10000, 999999);
            string exeTime = executeTime.ToString("D6");
            string posData = GetPosData();
            int status1 = Convert.ToInt32("0011", 2);
            string status = string.Format("{0}000", status1.ToString("X1"));
            string feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},", unit, seq, sts, error, type, exeTime, posData);
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", "!", feedback, sum);

            OnWriteMessage(result);
        }
    }
}