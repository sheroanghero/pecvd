using MECF.Framework.Simulator.Core.Commons;
using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.Simulator.Core.Robots
{
    class TazmoRobotSerialControllerSimulator : SerialPortDeviceSimulator
    {
        protected Random _rd = new Random();
        public bool Failed { get; set; }
        public string ResultValue { get; set; }

        public TazmoRobotSerialControllerSimulator(string port)
          : base(port,-1, "\r",'\r')
        {
            
            //AddCommandHandler("HOM", HandleINIT);//Home
            //AddCommandHandler("CPI", HandleMHOM);//Rest CPU

            //AddCommandHandler("STS", HandleSTS);//check robot state
            //AddCommandHandler("RST", HandleRST);//Home
           
            //AddCommandHandler("PNO", HandleMTPO);//read 

            //AddCommandHandler("RMN", HandleMTRP);//read unit name

            //AddCommandHandler("RSN", HandleMMAP);//read soft name


            //AddCommandHandler("VVN", HandleMALN);

            //AddCommandHandler("VVF", HandleMPNT);
          
            //AddCommandHandler("GET", HandleMTRS);
         
            //AddCommandHandler("CER", HandleCER);
            //AddCommandHandler("PUT", HandleCCLR);
            //AddCommandHandler("RPO", HandleCSOL);
            //AddCommandHandler("WCH", HandleCHLT);
            //AddCommandHandler("VCH", HandleVCH);

            //AddCommandHandler("PAU", HandlePAU);
            //AddCommandHandler("CNT", HandleCNT);

         
            //AddCommandHandler("CSRV", HandleCSRV);


         
            //AddCommandHandler("ACH", HandleSSPD);
            //AddCommandHandler("DWL", HandleDWL);
            //AddCommandHandler("UPL", HandleUPL);
            //AddCommandHandler("MTP", HandleMPT);
            //AddCommandHandler("AEX", HandleAEX);
            //AddCommandHandler("ART", HandleART);
            //AddCommandHandler("ZUP", HandleZUP);
            //AddCommandHandler("ZDN", HandleZDN);
            //AddCommandHandler("ZSP", HandleZSP);

       
        }
        public string StsResponse { get; set; } = "000";
        protected override void ProcessUnsplitMessage(string message)
        {

            string[] msgs = message.Replace("\r", "").Split(',');
            string cmd = msgs[0];
            switch(cmd)
            {
                case "CER":
                case "CPI":
                case "CNT":
                case "PAU":
                case "CMP":
                case "SMP":
                case "TMP":
                case "SCR":
                case "SCV":
                case "SCS":
                case "SRR":
                case "SRV":
                case "SRS":
                case "DWL":
                case "JOG":
                case "JSP":


                    SendAck();
                    break;
                case "RST":
                case "HOM":
                case "VVN":
                case "VVF":
                case "HLD":
                case "REL":
                case "MTP":
                case "MAT":
                case "EXG":
                



                
                case "AEX":
                case "ART":
                case "ZUP":
                case "ZSP":
                case "ZDN":
                case "ZMV":
                case "MAP":
                case "IXR":
                case "IXZ":
                case "IXS":
                    SendAck();
                    Thread.Sleep(2000);
                    SendResponse(cmd);
                    break;
                case "GET":
                    if(msgs[3] == "1")
                    {
                        _isArm1WaferOn = true;
                    }
                    if (msgs[3] == "2")
                    {
                        _isArm2WaferOn = true;
                    }

                    SendAck();
                    Thread.Sleep(2000);
                    SendResponse(cmd);
                    break;

                case "PUT":
                    if (msgs[3] == "1")
                    {
                        _isArm1WaferOn = false;
                    }
                    if (msgs[3] == "2")
                    {
                        _isArm2WaferOn = false;
                    }

                    SendAck();
                    Thread.Sleep(2000);
                    SendResponse(cmd);
                    break;





                case "STS":
                    SendResponse(cmd, StsResponse);
                    break;
                case "RMP":
                    SendResponse(message.Replace("\r", ""), "25,1111100000000000000000000");
                    break;
                case "WTH":
                    SendResponse(message.Replace("\r",""), "0775");
                    break;
                case "UPL":
                    SendResponse(message.Replace("\r", ""), "0775");
                    break;
                case "RED":
                    SendResponse(message.Replace("\r", ""), "6666");
                    break;
                case "WCH":   //Wafer presence
                    SendResponse(message.Replace("\r", ""), "1");
                    break;

                case "VCH":   //Wafer presence
                    SendResponse(message.Replace("\r", ""), "1");
                    break; 
                case "STA":
                    SendResponse(message.Replace("\r",""), $"000,01,02,0,0,{(_isArm1WaferOn?1:0)}," +
                        $"{(_isArm2WaferOn ? 1 : 0)},0,0,5");                                                        

                    break;

            }





        }

        private bool _isArm1WaferOn;
        private bool _isArm2WaferOn;

        protected override void ProcessUnsplitMessage(byte[] message)
        {
            string msg = Encoding.ASCII.GetString(message);

            ProcessUnsplitMessage(msg);


        }



        public virtual void HandleZUP(string obj)
        {
            Thread.Sleep(1000);
            SendAck(obj);
            Thread.Sleep(1000);
            SendResponse("ZUP");
        }
        public virtual void HandleZDN(string obj)
        {
            Thread.Sleep(1000);
            SendAck(obj);
            Thread.Sleep(1000);
            SendResponse("ZDN");
        }
        public virtual void HandleZSP(string obj)
        {
            Thread.Sleep(1000);
            SendAck(obj);
            Thread.Sleep(1000);
            SendResponse("ZSP");
        }
        public virtual void HandleVCH(string obj)
        {
            //Thread.Sleep(1000);
            //SendAck(obj);
            //Thread.Sleep(1000);
            if(VF==0)
            SendResponse(obj+",1");
            if (VF == 1)
                SendResponse(obj + ",0");
        }

        public virtual void HandleMPT(string obj)
        {
            Thread.Sleep(1000);
            SendAck(obj);
            Thread.Sleep(1000);
            SendResponse("MPT");
        }

        public virtual void HandleAEX(string obj)
        {
            Thread.Sleep(1000);
            SendAck(obj);
            Thread.Sleep(1000);
            SendResponse("AEX");
        }
        public virtual void HandleART(string obj)
        {
            Thread.Sleep(1000);
            SendAck(obj);
            Thread.Sleep(1000);
            SendResponse("ART");
        }
        public virtual void HandleRST(string obj)
        {
            Thread.Sleep(1000);
            SendAck(obj);
            Thread.Sleep(1000);
            SendResponse("RST");
        }
        public virtual void HandleUPL(string obj)
        {
            Thread.Sleep(1000);

            var random = new Random();
            var speed = random.Next(1, 16);
            //SendAck(obj);
            var p1 = speed;
            var p2 = speed;
            var p3 = speed;
            var p4 = speed;
            var p5 = speed;
            var p6 = speed;
            var p7 = speed;
            var p8 = speed;
            var p9 = speed;
            var p10 = speed;
            var p11 = speed;
            var p12 = speed;
            var para1 = obj + $",{p1},{p2},{p3},{p4},{p5},{p6},{p7},{p8},{p9},{p10},{p11},{p12}";
            SendResponse(para1);
        }
        public virtual void HandleDWL(string obj)
        {
            Thread.Sleep(1000);
            SendAck(obj);

            //SendResponse(obj);
        }
        public virtual void HandleCER(string obj)
        {
            Thread.Sleep(1000);
            SendAck(obj);

            //SendResponse(obj);
        }
        public virtual void HandleMTRG(string obj)
        {
            Response("MTRG", obj);

            Thread.Sleep(1000);
            EndOfExecution(obj);
        }
        public virtual void HandlePAU(string obj)
        {
            SendAck(obj);
            Thread.Sleep(1000);
        }

        public virtual void HandleCNT(string obj)
        {
            SendAck(obj);
            Thread.Sleep(1000);
        }
        public virtual void HandleMTRE(string obj)
        {
            SendAck(obj);
            Thread.Sleep(1000);
            SendResponse(obj);
        }
        public virtual void HandleSTS(string obj)
        {
            SendAck(obj);
            Thread.Sleep(1000);
            SendResponse(obj);
        }
        public virtual void HandleMTPO(string obj)
        {
            Response("MTPO", obj);

            Thread.Sleep(1000);
            EndOfExecution(obj);
        }
        public virtual void HandleMTGO(string obj)
        {
            Response("MTGO", obj);

            Thread.Sleep(1000);
            EndOfExecution(obj);
        }
        public virtual void HandleMTRP(string obj)
        {
            Response("MTRP", obj);

            Thread.Sleep(1000);
            EndOfExecution(obj);
        }

        public virtual void HandleRMAP(string obj)
        {
            Response("RMAP", obj);
            EndOfExecution(obj);
        }

        public virtual void HandleMMCA(string obj)
        {
            Response("MMCA", obj);
            EndOfExecution(obj);
        }

        public virtual void HandleMMAP(string msg)
        {
            string unit = msg.Substring(1, 1);
            string seq = msg.Substring(2, 2); ;
            string cmd = msg.Substring(4, 4); ;
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");

            //$ <UNo> (<SeqNo>) <StsN> <Ackcd> <Sum> <CR>
            string feedback = string.Format(",{0},{1},{2},{3},{4},", unit, seq, sts, Ackcd, cmd).Replace(",", "");
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", "$", feedback, sum);

            OnWriteMessage(result);


            Thread.Sleep(3000);

            int executeTime = _rd.Next(10000, 999999);
            string exeTime = executeTime.ToString("D6");
            string posData = GetPosData();
            string error = 0.ToString("X04");

            //! <UNo> (<SeqNo>) <StsN> <Errcd> INIT <Sum> <CR>
            feedback = string.Format(",{0},{1},{2},{3},{4},", unit, seq, sts, error, cmd).Replace(",", "");
            sum = CheckSum(feedback);
            result = String.Format("{0}{1}{2}", "!", feedback, sum);

            OnWriteMessage(result);
        }

        public virtual void HandleMABS(string obj)
        {
            Response("MABS", obj);
            EndOfExecution(obj);
        }

        public virtual void HandleMTCH(string obj)
        {
            Response("MTCH", obj);
            EndOfExecution(obj);
        }

        public virtual void HandleMCTR(string obj)
        {
            Response("MCTR", obj);
            EndOfExecution(obj);
        }

        public virtual void HandleMTRS(string obj)
        {
            Thread.Sleep(2000);
            SendAck(obj);
            Thread.Sleep(2000);
            SendResponse(obj);
            //Response("MTRS", obj);
            //EndOfExecution(obj);
        }

        public virtual void HandleMPNT(string obj)
        {
            VF = 1;
            SendAck(obj);
            Thread.Sleep(1000);
            SendResponse(obj);
            //Response("MPNT", obj);
            //Thread.Sleep(1000);
            //EndOfExecution(obj);
        }

        public virtual void HandleMTPT(string obj)
        {
            Response("MTPT", obj);

            Thread.Sleep(1000);

            EndOfExecution(obj);
        }

        public virtual void HandleUnknown(string obj)
        {
            Response("", obj);
        }

        private void HandleACKN(string obj)
        {
            //DO NOTHING
        }

        private void HandleRLOG(string obj)
        {
            Response("RLOG", obj);
        }

        private void HandleRCCD(string obj)
        {
            Response("RCCD", obj);
        }

        private void HandleRACA(string obj)
        {
            Response("RACA", obj);
        }

        private void HandleRALN(string obj)
        {
            Response("RALN", obj);
        }

        private void HandleRVER(string obj)
        {
            Response("RVER", obj);
        }

        private void HandleRMSK(string obj)
        {
            Response("RMSK", obj);
        }

        private void HandleRERR(string obj)
        {
            Response("RERR", obj);
        }


        //$,<UNo>(,<SeqNo>),RSTS(,<Sum>)<CR>
        //$ <UNo> (<SeqNo>) <StsN> <Ackcd> <Sum> <CR>
        //$ <UNo> (<SeqNo>) <StsN> <Ackcd> <Errcd> <Status1> … <Status4> <Sum> <CR>
        public virtual void HandleRSTS(string msg)
        {
            //string unit = msg.Substring(1, 1);
            //string seq = msg.Substring(2, 2); ;
            //string cmd = msg.Substring(4, 4); ;
            //string sts = 0.ToString("X02");
            //string Ackcd = 0.ToString("X04");

            //string error = 0.ToString("X04");
            //int status1 = Convert.ToInt32("0011", 2);
            //string status = string.Format("{0}000", status1.ToString("X1"));

            //string feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},", unit, seq, sts, Ackcd, cmd, error, status).Replace(",", "");
            //string sum = CheckSum(feedback);
            //string result = String.Format("{0}{1}{2}", "$", feedback, sum);

            //OnWriteMessage(result);
        }

        private void HandleRPRM(string obj)
        {
            Response("RPRM", obj);
        }

        private void HandleRSLV(string obj)
        {
            Response("RSLV", obj);
        }

        private void HandleRSPD(string obj)
        {
            Response("RSPD", obj);
        }

        private void HandleSMSK(string obj)
        {
            Response("SMSK", obj);
        }

        private void HandleSPRM(string obj)
        {
            Response("SPRM", obj);
        }

        private void HandleSSLV(string obj)
        {
            Response("SSLV", obj);
        }

        private void HandleSSPD(string obj)
        {
            Response("SSPD", obj);
        }

        private void HandleCSOL(string obj)
        {
            Response("CSOL", obj);
            EndOfExecution(obj);
        }

        private void HandleCLFT(string obj)
        {
            Response("CLFT", obj);
            EndOfExecution(obj);
        }

        private void HandleCHLT(string obj)
        {
            Response("CHLT", obj);
            EndOfExecution(obj);
        }

        //$,<UNo>(,<SeqNo>),CCLR,<CMode>(,<Sum>)<CR>
        //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,CCLR,<CMode>(,<Sum>)<CR>
        //!,<UNo>(,<SeqNo>),<Sts>,<Errcd>,CCLR,<ExeTime>,<PosData1>…,<PosDataN>(,<Sum>)<CR>
        public virtual void HandleCCLR(string msg)
        {
            Thread.Sleep(3000);
            SendAck(msg);
            Thread.Sleep(3000);
            SendResponse(msg);
        }

        private void HandleCSRV(string obj)
        {
            Response("CSRV", obj);
            EndOfExecution(obj);
        }

        private void HandleCRSM(string obj)
        {
            Response("CRSM", obj);
            EndOfExecution(obj);
        }

        private void HandleCSTP(string obj)
        {
            Response("CSTP", obj);
            EndOfExecution(obj);
        }

        private void HandleMACA(string obj)
        {
            Response("MACA", obj);
            EndOfExecution(obj);
        }
        int VF = 0;
        public virtual void HandleMALN(string msg)
        {
            VF = 0;
            SendAck(msg);
            Thread.Sleep(1000);
            var par = msg.Split(',');

            SendResponse(msg);
        }

        private void HandleMREL(string obj)
        {
            Response("MREL", obj);
            EndOfExecution(obj);
        }

        //$,<UNo>(,<SeqNo>),INIT,<ErrClr>,<SrvOn>,<Home>(,<Sum>)<CR>
        //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,INIT,<ErrClr>,<SrvOn>,<Axis>(,<Sum>)<CR>
        //!,<UNo>(,<SeqNo>),<Sts>,<Errcd>,INIT,<ExeTime>,<PosData1>…,<PosDataN>(,<Sum>)<CR>
        public virtual void HandleINIT(string msg)
        {
            SendAck(msg);
            SendResponse(msg);
        }

        public virtual void HandleMHOM(string msg)
        {

            string unit = msg.Substring(1, 1);
            string seq = msg.Substring(2, 2); ;
            string cmd = msg.Substring(4, 4); ;
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");


            //$ <UNo> (<SeqNo>) <StsN> <Ackcd> <Sum> <CR>
            string feedback = string.Format(",{0},{1},{2},{3},{4},", unit, seq, sts, Ackcd, cmd).Replace(",", "");
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", "$", feedback, sum);

            OnWriteMessage(result);


            Thread.Sleep(3000);

            int executeTime = _rd.Next(10000, 999999);
            string exeTime = executeTime.ToString("D6");
            string posData = GetPosData();
            string error = 0.ToString("X04");

            //! <UNo> (<SeqNo>) <StsN> <Errcd> INIT <Sum> <CR>
            feedback = string.Format(",{0},{1},{2},{3},{4},", unit, seq, sts, error, cmd).Replace(",", "");
            sum = CheckSum(feedback);
            result = String.Format("{0}{1}{2}", "!", feedback, sum);

            OnWriteMessage(result);

        }


        //$,0     ,1       ,2    ,3      ,4        ,5          ,6        ,7     <CR>  
        //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,<Command>,<Parameter>, <Value>(,<Sum>)<CR>
        //
        public void Response(string cmd, string msg)
        {
            string unit = msg.Substring(1, 1);
            string seq = msg.Substring(2, 2); ;

            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");
            int status1 = Convert.ToInt32("0011", 2);
            string status = string.Format("{0}000", status1.ToString("X1"));

            //$ <UNo> (<SeqNo>) <StsN> <Ackcd> <Sum> <CR>
            string feedback = string.Format(",{0},{1},{2},{3},{4},", unit, seq, sts, Ackcd, cmd).Replace(",", "");
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", "$", feedback, sum);

            OnWriteMessage(result);

        }

        public string CheckSum(string feedback)
        {
            int sum = 0;
            foreach (var item in feedback)
            {
                sum += (int)item;
            }

            sum = sum % 256;
            return sum.ToString("X02");
        }

        public void EndOfExecution(string msg)
        {
            string[] arrayMsg = msg.Split(',');
            string unit = msg.Substring(1, 1);
            string seq = msg.Substring(2, 2); ;
            string cmd = msg.Substring(4, 4); ;
            string sts = 0.ToString("X02");
            string Ackcd = Failed ? "33D4" : 0.ToString("X04");

            int executeTime = _rd.Next(10000, 999999);
            string exeTime = executeTime.ToString("D6");
            string posData = 0.ToString("D8");
            int status1 = Convert.ToInt32("0011", 2);
            string status = string.Format("{0}000", status1.ToString("X1"));
            //! < UNo > (< SeqNo >) < StsN > < Errcd > MTRG<Sum> < CR >
            string feedback = string.Format(",{0},{1},{2},{3},{4},", unit, seq, sts, Ackcd, cmd).Replace(",", "");
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", "!", feedback, sum);

            OnWriteMessage(result);
        }


        public string GetPosData()
        {
            return string.Format("{0},{1},{2},{3},{4}", _rd.Next().ToString("D8"), _rd.Next().ToString("D8"), _rd.Next().ToString("D8"), _rd.Next().ToString("D8"), _rd.Next().ToString("D8"));
        }

        public string GetValueData()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                _rd.Next(0, 0).ToString("D8"),
                _rd.Next(0, 0).ToString("D8"),
                _rd.Next(0, 0).ToString("D8"),
                _rd.Next(0, 0).ToString("D8"),
                _rd.Next(0, 0).ToString("D8"),
                _rd.Next(0, 0).ToString("D8"),
                _rd.Next(0, 0).ToString("D8"),
                _rd.Next(0, 0).ToString("D8"),
                _rd.Next(0, 0).ToString("D8"),
                _rd.Next(0, 0).ToString("D8"));
        }

        public string GetMapData(bool random, bool full)
        {
            string result = "";
            for (int i = 0; i < 25; i++)
            {
                if (random)
                {
                    result += string.Format("{0:00}:{1}", i + 1, _rd.Next(100) > 50 ? "OK" : "--");
                }
                else if (full)
                {
                    result += string.Format("{0:00}:{1}", i + 1, "OK");
                }
                else
                {
                    result += string.Format("{0:00}:{1}", i + 1, "--");

                }
                if (i < 24)
                {
                    result += ",";
                }
            }

            return result;
        }

        public string GetMapData(bool pre, bool post, bool mid)
        {
            string result = "";
            for (int i = 0; i < 25; i++)
            {
                if (i < 8)
                {
                    result += string.Format("{0:00}:{1}", i + 1, pre ? "OK" : "--");
                }
                else if (i < 16)
                {
                    result += string.Format("{0:00}:{1}", i + 1, mid ? "OK" : "--");
                }
                else
                {
                    result += string.Format("{0:00}:{1}", i + 1, post ? "OK" : "--");
                }

                if (i < 24)
                {
                    result += ",";
                }
            }

            return result;
        }
        private void SendAck(string cmd="")
        {
            string ack = Encoding.Default.GetString(new byte[] { 0x6 });

            OnWriteMessage(ack);

        }
        private void ReceiveTwinCommand(string cmd, int delaytime)
        {
            SendAck(cmd);

            Thread.Sleep(1000 * delaytime);

            SendResponse(cmd);
        }


        private void SendResponse(string cmd)
        {
            string data = cmd + "\r";

            OnWriteMessage(data);


        }
        private void SendResponse(string cmd,string response)
        {
            string data = $"{cmd},{response}\r";

            OnWriteMessage(data);


        }
    }
}
