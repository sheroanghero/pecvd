using System;
using System.Threading;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Robots
{

    public class YaskawaNX100ControllerSimulator : SocketDeviceSimulator
    {
        protected Random _rd = new Random();
        public bool Failed { get; set; }
        public string ResultValue { get; set; }

        public YaskawaNX100ControllerSimulator(int port)
            :base(port, 4, "\r", ' ')
        {
            //Action Command
            AddCommandHandler("INIT", HandleINIT);
            AddCommandHandler("MHOM", HandleMHOM);

            AddCommandHandler("MTRG", HandleMTRG);
            AddCommandHandler("MTRE", HandleMTRE);
            AddCommandHandler("MTGO", HandleMTGO);
            AddCommandHandler("MTPO", HandleMTPO);

            AddCommandHandler("MTRP", HandleMTRP);

            AddCommandHandler("MMAP", HandleMMAP);


            AddCommandHandler("MALN", HandleMALN);

            AddCommandHandler("MPNT", HandleMPNT);
            AddCommandHandler("MTPT", HandleMTPT);

            AddCommandHandler("MTRS", HandleMTRS);
            AddCommandHandler("MEXG", HandleMEXG);
            /*
            AddCommandHandler("MREL", HandleMREL);
            AddCommandHandler("MALN", HandleMALN);
            AddCommandHandler("MACA", HandleMACA);
            AddCommandHandler("MCTR", HandleMCTR);
            AddCommandHandler("MTCH", HandleMTCH);
            AddCommandHandler("MABS", HandleMABS);
            
 
            AddCommandHandler("MMCA", HandleMMCA);
            */
            //Control Command
            AddCommandHandler("CCLR", HandleCCLR);
            AddCommandHandler("CSOL", HandleCSOL);
            AddCommandHandler("CHLT", HandleCHLT);
            AddCommandHandler("CLFT", HandleCLFT);

            //AddCommandHandler("CSTP", HandleCSTP);
            AddCommandHandler("CRSM", HandleCRSM);
            //AddCommandHandler("CSRV", HandleCSRV);


            //Setting Command
            AddCommandHandler("SSPD", HandleSSPD);
            AddCommandHandler("SSLV", HandleSSLV);
            AddCommandHandler("SPRM", HandleSPRM);
            AddCommandHandler("SMSK", HandleSMSK);

            //Reference Command

            AddCommandHandler("RMAP", HandleRMAP);
            AddCommandHandler("RSTS", HandleRSTS);
            /*
            AddCommandHandler("RSPD", HandleRSPD);
            AddCommandHandler("RSLV", HandleRSLV);
            AddCommandHandler("RPRM", HandleRPRM);
 
            AddCommandHandler("RERR", HandleRERR);
            AddCommandHandler("RMSK", HandleRMSK);
            AddCommandHandler("RVER", HandleRVER);
            AddCommandHandler("RALN", HandleRALN);
            AddCommandHandler("RACA", HandleRACA);
            AddCommandHandler("RCCD", HandleRCCD);
            AddCommandHandler("RLOG", HandleRLOG);
            AddCommandHandler("RMAP", HandleRMAP);
            */
            //ACK
            AddCommandHandler("ACKN", HandleACKN);

            AddCommandHandler("Unknown", HandleUnknown);
        }


        public virtual void HandleMTRG(string obj)
        {
            Response("MTRG", obj);

            Thread.Sleep(2000);
            EndOfExecution(obj);
        }
        public virtual void HandleMTRE(string obj)
        {
            Response("MTRE", obj);

            Thread.Sleep(1000);
            EndOfExecution(obj);
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

            Thread.Sleep(2000);
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
            Response("MTRS", obj);
            EndOfExecution(obj);
        }

        public virtual void HandleMPNT(string obj)
        {
            Response("MPNT", obj);

            Thread.Sleep(1000);

            EndOfExecution(obj);
        }

        public virtual void HandleMTPT(string obj)
        {
            Response("MTPT", obj);

            Thread.Sleep(1000);

            EndOfExecution(obj);
        }
        public virtual void HandleMEXG(string obj)
        {
            Response("MEXG", obj);

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
            string unit = msg.Substring(1, 1);
            string seq = msg.Substring(2, 2); ;
            string cmd = msg.Substring(4, 4); ;
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");

            string error = 0.ToString("X04");
            int status1 = Convert.ToInt32("0011", 2);
            string status = string.Format("{0}000", status1.ToString("X1"));

            string feedback = string.Format(",{0},{1},{2},{3},{4},{5}", unit, seq, sts, Ackcd, error, status).Replace(",","");
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", "$", feedback, sum);

            OnWriteMessage(result);
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
        public  virtual void HandleCCLR(string msg)
        {
            string unit = msg.Substring(1, 1);
            string seq = msg.Substring(2, 2); ;
            string cmd = msg.Substring(4, 4); ;
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");

            string feedback1 = string.Format(",{0},{1},{2},{3}", unit, seq, sts, Ackcd).Replace(",", "");
            string feedback2 = string.Format(",{0},{1},{2},{3},{4},", unit, seq, sts, Ackcd, cmd).Replace(",", "");


            string sum = CheckSum(feedback1);
            string result = String.Format("{0}{1}{2}", "$", feedback1, sum);
            

            OnWriteMessage(result);

            Thread.Sleep(3000);

            sum = CheckSum(feedback2);
            result = String.Format("{0}{1}{2}", "!", feedback2, sum);
            OnWriteMessage(result);

            //int executeTime = _rd.Next(10000, 999999);
            //string exeTime = executeTime.ToString("D6");
            //string posData = GetPosData();
            //string error = 0.ToString("X04");

            //feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},", unit, seq, sts, error, cmd, exeTime, posData).Replace(",", "");
            //sum = CheckSum(feedback);
            //result = String.Format("{0}{1}{2}", "!", feedback, sum);

            //OnWriteMessage(result);
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


        //$,<UNo>(,<SeqNo>),MALN,<Mode>,<Angle>(,<Sum>)<CR>
        //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,MALN,<Mode>,<Angle>(,<Sum>)<CR>
        //! <UNo> (<SeqNo>) <StsN> <Errcd> MALN<Sum> <CR>
        //!,<UNo>(,<SeqNo>),<Sts>,<Errcd>,MALN,<ExeTime>,<PosData1>…,<PosDataN>,<Value1>…,<Value10>(,<Sum>)<CR>
        public virtual void HandleMALN(string msg)
        {
            string unit = msg.Substring(1, 1);
            string seq = msg.Substring(2, 2); ;
            string cmd = msg.Substring(4, 4); ;
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");

            string feedback = string.Format(",{0},{1},{2},{3},{4},", unit, seq, sts, Ackcd, cmd).Replace(",", "");
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", "$", feedback, sum);

            OnWriteMessage(result);

            Thread.Sleep(3000);

            string error = 0.ToString("X04");
            int executeTime = _rd.Next(10000, 999999);
            string exeTime = executeTime.ToString("D6");
            string posData = GetPosData();
            string valueData = GetValueData();

            feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},{7},", unit, seq, sts, error, cmd, exeTime, posData, valueData).Replace(",", "");
            sum = CheckSum(feedback);
            result = String.Format("{0}{1}{2}", "!", feedback, sum);

            OnWriteMessage(result);
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
            string unit = msg.Substring(1, 1);
            string seq = msg.Substring(2, 2); ;
            string cmd = msg.Substring(4, 4); ;
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");

            //$ <UNo> (<SeqNo>) <StsN> <Ackcd> <Sum> <CR>
            string feedback = string.Format(",{0},{1},{2},{3}", unit, seq, sts, Ackcd).Replace(",", "");
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

        public virtual void HandleMHOM(string msg)
        {

            string unit = msg.Substring(1,1);
            string seq = msg.Substring(2, 2); ;
            string cmd = msg.Substring(4, 4); ;
            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");


            //$ <UNo> (<SeqNo>) <StsN> <Ackcd> <Sum> <CR>
            string feedback = string.Format(",{0},{1},{2},{3},", unit, seq, sts, Ackcd).Replace(",", "");
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
        public void Response(string cmd, string msg,string data="")
        {
            string unit = msg.Substring(1, 1);
            string seq = msg.Substring(2, 2); ;

            string sts = 0.ToString("X02");
            string Ackcd = 0.ToString("X04");
            int status1 = Convert.ToInt32("0011", 2);
            string status = string.Format("{0}000", status1.ToString("X1"));

            //$ <UNo> (<SeqNo>) <StsN> <Ackcd> <Sum> <CR>
            string feedback = string.Format(",{0},{1},{2},{3},{4}", unit, seq, sts, Ackcd,data).Replace(",", "");
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
            string Ackcd = Failed ? "33D4" :  0.ToString("X04");

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
            string result="";
            for (int i = 0; i < 25; i++)
            {
                if (random)
                {
                    result += string.Format("{0:00}:{1}", i + 1, _rd.Next(100) > 50 ? "OK" : "--");
                }else if (full)
                {
                    result += string.Format("{0:00}:{1}", i + 1,  "OK"  );
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
        public string GetMapData(int startslot, int endslot)
        {
            string result = "";
            for (int i = 0; i < 25; i++)
            {
                if (i >= startslot && i<=endslot)
                {
                    result += string.Format("{0:00}:{1}", i + 1,  "OK" );
                }
                
                else
                {
                    result += string.Format("{0:00}:{1}", i + 1,  "--");
                }

                if (i < 24)
                {
                    result += ",";
                }
            }

            return result;
        }
    }
}

