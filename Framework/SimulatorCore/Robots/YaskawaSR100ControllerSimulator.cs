using System;
using System.Threading;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.Robots
{

    public class YaskawaSR100ControllerSimulator : SocketDeviceSimulator
    {
        protected Random _rd = new Random();
 
        public YaskawaSR100ControllerSimulator(int port)
            :base(port, 3, "\r", ',')
        {
            //Action Command
            AddCommandHandler("INIT", HandleINIT);
            AddCommandHandler("MREL", HandleMREL);
            AddCommandHandler("MALN", HandleMALN);
            AddCommandHandler("MACA", HandleMACA);

            AddCommandHandler("MPNT", HandleMPNT);
            AddCommandHandler("MTRS", HandleMTRS);
            AddCommandHandler("MCTR", HandleMCTR);
            AddCommandHandler("MTCH", HandleMTCH);
            AddCommandHandler("MABS", HandleMABS);
            AddCommandHandler("MMAP", HandleMMAP);
            AddCommandHandler("RMPD", HandleRMPD);
            AddCommandHandler("MMCA", HandleMMCA);
 
            //Control Command
            AddCommandHandler("CSTP", HandleCSTP);
            AddCommandHandler("CRSM", HandleCRSM);
            AddCommandHandler("CSRV", HandleCSRV);
            AddCommandHandler("CCLR", HandleCCLR);
            AddCommandHandler("CSOL", HandleCSOL);

            //Setting Command
            AddCommandHandler("SSPD", HandleSSPD);
            AddCommandHandler("SSLV", HandleSSLV);
            AddCommandHandler("SPRM", HandleSPRM);
            AddCommandHandler("SMSK", HandleSMSK);

            //Reference Command
            AddCommandHandler("RSPD", HandleRSPD);
            AddCommandHandler("RSLV", HandleRSLV);
            AddCommandHandler("RPRM", HandleRPRM);
            AddCommandHandler("RSTS", HandleRSTS);
            AddCommandHandler("RERR", HandleRERR);
            AddCommandHandler("RMSK", HandleRMSK);
            AddCommandHandler("RVER", HandleRVER);
            AddCommandHandler("RALN", HandleRALN);
            AddCommandHandler("RACA", HandleRACA);
            AddCommandHandler("RCCD", HandleRCCD);
            AddCommandHandler("RLOG", HandleRLOG);
            AddCommandHandler("RMAP", HandleRMAP);
            AddCommandHandler("RPOS", HandleRPOS);
            //ACK
            AddCommandHandler("ACKN", HandleACKN);

            AddCommandHandler("Unknown", HandleUnknown);
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

        public virtual void HandleMMAP(string obj)
        {
            Response("MMAP", obj);
            EndOfExecution(obj);
        }

        public virtual void HandleRMPD(string obj)
        {
            Response("RMPD", obj);
            EndOfExecution(obj);
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
            EndOfExecution(obj);
        }

        public virtual void HandleUnknown(string obj)
        {
            Response("", obj);
        }

        public virtual void HandleCSOL(string obj)
        {
            Response("CSOL", obj);
            EndOfExecution(obj);
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


        public virtual void HandleRPOS(string msg)
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

        //    OnWriteMessage(result);

            Thread.Sleep(3000);

            int executeTime = _rd.Next(10000, 999999);
            string exeTime = executeTime.ToString("D6");
            string posData = GetPosData();
            string error = 0.ToString("X04");

            feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},", unit, seq, sts, Ackcd, cmd, cmode, posData);
            sum = CheckSum(feedback);
            result = String.Format("{0}{1}{2}", "!", feedback, sum);

            OnWriteMessage(result);
        }

        //$,<UNo>(,<SeqNo>),RSTS(,<Sum>)<CR>
        //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,RSTS,<Errcd>,<Status>(,<Sum>)<CR>
        public virtual void HandleRSTS(string msg)
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
            Thread.Sleep(100);
            Response("SSLV", obj);
        }

        private void HandleSSPD(string obj)
        {
            Response("SSPD", obj);
        }
        

        //$,<UNo>(,<SeqNo>),CCLR,<CMode>(,<Sum>)<CR>
        //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,CCLR,<CMode>(,<Sum>)<CR>
        //!,<UNo>(,<SeqNo>),<Sts>,<Errcd>,CCLR,<ExeTime>,<PosData1>…,<PosDataN>(,<Sum>)<CR>
        public  virtual void HandleCCLR(string msg)
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
        //!,<UNo>(,<SeqNo>),<Sts>,<Errcd>,MALN,<ExeTime>,<PosData1>…,<PosDataN>,<Value1>…,<Value10>(,<Sum>)<CR>
        public virtual void HandleMALN(string msg)
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
            //string absEvent = ">,12EVNT,100,2022 / 04 / 24 10:11:41,3AC0,980F?$,2,04,C0,0000,INIT,1,1,G,32";
            //OnWriteMessage(absEvent);



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

        //$,0     ,1       ,2    ,3      ,4        ,5          ,6        ,7     <CR>  
        //$,<UNo>(,<SeqNo>),<Sts>,<Ackcd>,<Command>,<Parameter>, <Value>(,<Sum>)<CR>
        //
        public void Response(string cmd, string msg)
        {
            string[] arrayMsg = msg.Split(',');
            string unit = arrayMsg[1];
            string seq = arrayMsg[2];
            string sts = 0.ToString("X02");
            string error = 0.ToString("X04");
            string Ackcd = 0.ToString("X04");
            int status1 = Convert.ToInt32("0011", 2);
            string status = string.Format("{0}000", status1.ToString("X1"));
            string feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},", unit, seq, sts, Ackcd, cmd, error, status);
            string sum = CheckSum(feedback);
            string result = String.Format("{0}{1}{2}", arrayMsg[0], feedback, sum);

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
            string unit = arrayMsg[1];
            string seq = arrayMsg[2];
            string sts = 0.ToString("X02");
            string error = 0.ToString("X04");
            string type = arrayMsg[3];
            int executeTime = _rd.Next(10000, 999999);
            string exeTime = executeTime.ToString("D6");
            string posData = 0.ToString("D8");
            int status1 = Convert.ToInt32("0011", 2);
            string status = string.Format("{0}000", status1.ToString("X1"));
            string feedback = string.Format(",{0},{1},{2},{3},{4},{5},{6},", unit, seq, sts, error, type, exeTime, posData);
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
                if (i >= startslot && i <= endslot)
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
            result = "01:OK,02:--,03:OK,04:--,05:OK,06:--,07:OK,08:--,09:--,10:--,11:OK,12:OK,13:OK,14:OK,15:OK,16:--,17:OK,18:--,19:--,20:--,21:--,22:--,23:--,24:--,25:--";
            return result;
        }

        public string GetMapThickData(int startslot, int endslot)
        {
            string result = "";
            for (int i = 0; i < 25; i++)
            {
                if (i >= startslot && i <= endslot)
                {
                    if(i%2==0)
                        result += string.Format("{0:00}:{1}", i + 1, "00001111,00002222");
                    else
                        result += string.Format("{0:00}:{1}", i + 1, "00001111,00002222");
                }

                else
                {
                    result += string.Format("{0:00}:{1}", i + 1, "00000000,00000000");
                }

                if (i < 24)
                {
                    result += ",";
                }
            }
            result = "00:00019221,00019728,01:00031709,00032457,02:00044349,00045076,03:00057170,00057920,04:00082661,00083387,05:00089048,00089799,06:00095386,00096138,07:00101821,00102548,08:00108206,00108931,09:00120976,00121752,10:00000000,00000000,11:00000000,00000000,12:00000000,00000000,13:00000000,00000000,14:00000000,00000000,15:00000000,00000000,16:00000000,00000000,17:00000000,00000000,18:00000000,00000000,19:00000000,00000000,20:00000000,00000000,21:00000000,00000000,22:00000000,00000000,23:00000000,00000000,24:00000000,00000000,25:00000000,00000000,26:00000000,00000000,27:00000000,00000000,28:00000000,00000000,29:00000000,00000000";
            return result;
        }
    }
}

