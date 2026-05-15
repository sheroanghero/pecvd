using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aitex.Core.RT.Log;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.LoadPorts.Hirata
{
    public class PrsEfemSimulator : SocketDeviceSimulator
    {
        public string LP1SlotMap
        {
            get { return string.Join("", _lp1slotMap); }
        }
        public string LP2SlotMap
        {
            get { return string.Join("", _lp2slotMap); }
        }
        //public event Action<string> WriteDeviceEvent;

        private bool[] _lp1SigStateData1 = new bool[32];
        private bool[] _lp1SigStateData2 = new bool[32];

        private bool[] _lp2SigStateData1 = new bool[32];
        private bool[] _lp2SigStateData2 = new bool[32];


        private string[] _lp1slotMap = new string[25];
        private string[] _lp2slotMap = new string[25];

        private string[] _stationslotMap = new string[25];


        public string FoupID { get; set; }
        public string LP1InforPadState { get; set; } = "0";
        public string LP2InforPadState { get; set; } = "0";
        //private bool _isPlaced;
        //private bool _isPresent;

        //private int _moveTime = 5000; //

        public PrsEfemSimulator(int tcpPort)
            : base(tcpPort, -1, "\r", ' ')
        {

            for (int i = 0; i < _lp1slotMap.Length; i++)
                _lp1slotMap[i] = "0";
            for (int i = 0; i < _lp2slotMap.Length; i++)
                _lp2slotMap[i] = "0";
            for (int i = 0; i < _stationslotMap.Length; i++)
                _stationslotMap[i] = "0";


            for (int i = 0; i < _lp1SigStateData1.Length; i++)
                _lp1SigStateData1[i] = false;
            for (int i = 0; i < _lp1SigStateData2.Length; i++)
                _lp1SigStateData2[i] = false;



            for (int i = 0; i < _lp2SigStateData1.Length; i++)
                _lp2SigStateData1[i] = false;
            for (int i = 0; i < _lp2SigStateData2.Length; i++)
                _lp2SigStateData2[i] = false;

            _lp1SigStateData1[0] = false; //LP Placement
            _lp1SigStateData1[1] = false; //Pod Present
            _lp1SigStateData1[2] = false; //Access SW
            _lp1SigStateData1[3] = false; //Pod lock
            _lp1SigStateData1[4] = false; //Cover close

            _lp1SigStateData1[5] = false; // Cover lock
            _lp1SigStateData1[6] = false;
            _lp1SigStateData1[7] = false;
            _lp1SigStateData1[8] = false; //Info pad A
            _lp1SigStateData1[9] = false; //Info pad B

            _lp1SigStateData1[10] = false; //Info pad C
            _lp1SigStateData1[11] = false; //Info pad D

            _lp1SigStateData1[24] = false; //VALID (E84)
            _lp1SigStateData1[25] = false; //CS_0 (E84)
            _lp1SigStateData1[26] = false; //CS_1 (E84)
            _lp1SigStateData1[27] = false; //Valid
            _lp1SigStateData1[28] = false; //TR_REQ (E84)
            _lp1SigStateData1[29] = false; //BUSY (E84)
            _lp1SigStateData1[30] = false; //COMPT (E84)
            _lp1SigStateData1[31] = false; //CONT (E84)



            _lp1SigStateData2[0] = false; //PRESENCE
            _lp1SigStateData2[1] = false; //PLACEMENT
            _lp1SigStateData2[2] = false; //LOAD
            _lp1SigStateData2[3] = false; //UNLOAD
            _lp1SigStateData2[4] = false; //MANUAL MODE

            _lp1SigStateData2[5] = false; //ERROR
            _lp1SigStateData2[6] = false; //CLAMP / UNCLAMP
            _lp1SigStateData2[7] = false; //DOCK/UNDOCK
            _lp1SigStateData2[8] = false; //ACCESS SW


            //A000A 41010 10001 01000
            _lp2SigStateData1[0] = false; //LP Placement
            _lp2SigStateData1[1] = false; //Pod Present
            _lp2SigStateData1[2] = false; //Access SW
            _lp2SigStateData1[3] = false; //Pod lock
            _lp2SigStateData1[4] = false; //Cover close

            _lp2SigStateData1[5] = false; // Cover lock
            _lp2SigStateData1[6] = false;
            _lp2SigStateData1[7] = false;
            _lp2SigStateData1[8] = false; //Info pad A
            _lp2SigStateData1[9] = false; //Info pad B

            _lp2SigStateData1[10] = false; //Info pad C
            _lp2SigStateData1[11] = false; //Info pad D

            _lp2SigStateData1[24] = false; //VALID (E84)
            _lp2SigStateData1[25] = false; //CS_0 (E84)
            _lp2SigStateData1[26] = false; //CS_1 (E84)
            _lp2SigStateData1[27] = false; //Valid
            _lp2SigStateData1[28] = false; //TR_REQ (E84)
            _lp2SigStateData1[29] = false; //BUSY (E84)
            _lp2SigStateData1[30] = false; //COMPT (E84)
            _lp2SigStateData1[31] = false; //CONT (E84)


            _lp2SigStateData2[0] = false; //PRESENCE
            _lp2SigStateData2[1] = false; //PLACEMENT
            _lp2SigStateData2[2] = false; //LOAD
            _lp2SigStateData2[3] = false; //UNLOAD
            _lp2SigStateData2[4] = false; //MANUAL MODE

            _lp2SigStateData2[5] = false; //ERROR
            _lp2SigStateData2[6] = false; //CLAMP / UNCLAMP
            _lp2SigStateData2[7] = false; //DOCK/UNDOCK
            _lp2SigStateData2[8] = false; //ACCESS SW

        }

        //public void ChangeSlotMap(SlotMapChangedEventArgs obj)
        //{
        //    string slotMap = obj.SlotMap.Replace("'", "");
        //    for (int i = 0; i < slotMap.Length; i++)
        //        _slotMap[i] = slotMap.Substring(i, 1);
        //}

        protected override void ProcessUnsplitMessage(string message)
        {
            string[] msg = message.Split(':');
            if (msg.Length < 2)
                return;
            string type = msg[0];
            string cmd = msg[1];
            switch (type)
            {
                case "SET":
                    ReceiveSetCommand(cmd);
                    break;
                case "MOD":
                    ReceiveModCommand(cmd);
                    break;
                case "GET":
                    ReceiveGetCommand(cmd);
                    break;
                case "FIN":
                    ReceiveFinCommand(cmd);
                    break;
                case "MOV":
                    ReceiveMovCommand(cmd);
                    break;
                case "EVT":
                    ReceiveEvtCommand(cmd);
                    break;
                case "TCH":
                    ReceiveTchCommand(cmd);
                    break;
                default:
                    ReceiveUnknownCommand(message);
                    break;
            }
        }

        private void ReceiveMovCommand(string cmd)
        {
            bool needINF = true;
            string[] data = cmd.Split('/');
            switch (data[0])
            {
                case "INIT":

                    break;
                case "ORGSH": //back to initial

                    if (data[1] == "ALL")
                    {

                    }
                    if (data[1] == "P1")
                    {

                    }
                    if (data[1] == "P2")
                    {

                    }


                    break;
                case "ABORG": //Force back to initial


                    break;
                case "UNLOCK": //FOUP clamp: Close
                    //needINF = false;
                    break;
                case "LOCK": //FOUP clamp: open

                    break;
                case "DOCK": //FOUP dock:  

                    break;
                case "UNDOCK": //FOUP undock

                    break;
                case "OPEN": //FOUP undock

                    break;
                case "CLOSE": //Door Close

                    break;
                case "WAFSH": //Door open

                    break;
                case "GOTO": //Maps and loads the FOUP.

                    break;
                default:
                    break;

            }

            SendAck(cmd);

            Thread.Sleep(2000);
            if (needINF) SendInf(cmd);
        }

        private void ReceiveSetCommand(string cmd)
        {
            string[] data = cmd.Split('/');
            switch (data[0])
            {
                case "LED":
                    if (data[1] == "P1")
                    {
                        if (data[2] == "LOAD")
                        {
                            if (data[3] == "ON")
                            {
                                _lp1SigStateData2[2] = true;
                            }
                            else _lp1SigStateData2[2] = false;

                        }
                        if (data[2] == "UNLOAD")
                        {
                            if (data[3] == "ON")
                            {
                                _lp1SigStateData2[3] = true;
                            }
                            else _lp1SigStateData2[3] = false;

                        }
                        if (data[2] == "MANUAL MODE")
                        {
                            if (data[3] == "ON")
                            {
                                _lp1SigStateData2[4] = true;
                            }
                            else _lp1SigStateData2[4] = false;

                        }
                    }
                    if (data[1] == "P2")
                    {
                        if (data[2] == "LOAD")
                        {
                            if (data[3] == "ON")
                            {
                                _lp2SigStateData2[2] = true;
                            }
                            else _lp2SigStateData2[2] = false;

                        }
                        if (data[2] == "UNLOAD")
                        {
                            if (data[3] == "ON")
                            {
                                _lp2SigStateData2[3] = true;
                            }
                            else _lp2SigStateData2[3] = false;

                        }
                        if (data[2] == "MANUAL MODE")
                        {
                            if (data[3] == "ON")
                            {
                                _lp2SigStateData2[4] = true;
                            }
                            else _lp2SigStateData2[4] = false;

                        }
                    }

                    break;

                case "SIZE":
                    if (data[1] == "ARM1")
                        robotArm1Wafersize = data[2];
                    if (data[1] == "ARM2")
                        robotArm2Wafersize = data[2];
                    if (data[1] == "ALIGN1")
                        alignerwafersize = data[2];
                    break;
            }
            SendAck(cmd);
            if (cmd.Contains("FSB")) return;
            SendInf(cmd);
        }

        private void ReceiveModCommand(string cmd)
        {
            SendAck(cmd);
        }

        #region GET Command

        private void ReceiveGetCommand(string cmdGet)
        {
            string message = $"ACK:{cmdGet}";
            OnWriteMessage(message);
            string[] data = cmdGet.Split('/');

            switch (data[0])
            {
                case "MAPDT":
                    FeedbackGetWaferMapDescendingOrder(cmdGet);
                    break;
                case "ERROR":
                    break;
                case "CLAMP":
                    break;
                case "STATE":
                    FeedbackGetStatus(cmdGet);
                    break;
                case "MODE":
                    break;
                case "TRANSREQ":
                    break;
                case "SIGSTAT":
                    FeedbackGetSigStatus(cmdGet);
                    break;
                case "EVENT":
                    break;
                case "CSTID":
                    FeedbackCarrierID(cmdGet);
                    break;
                case "SIZE":
                    FeedbackWaferSize(cmdGet);
                    break;
                case "ADPLOCK":
                    break;
                case "ADPUNLOCK":
                    break;
            }
        }

        private void FeedbackWaferSize(string cmdGet)
        {
            //string message = string.Format("s00ACK:{0}/{1};\r", cmd, string.Join("", _state));
            string[] data = cmdGet.Replace(";", "").Split('/');

            string message = $"INF:{cmdGet.Replace(";", "")}/{robotArm1Wafersize};";

            if (message.Contains("BF5") || message.Contains("BF6"))
                message = message.Replace("300", "NONE");

            if (message.Contains("STATION"))
                message = message.Replace("300", "NONE");

            OnWriteMessage(message);
        }
        private string robotArm1Wafersize = "300";
        private string robotArm2Wafersize = "300";
        private string alignerwafersize = "300";

        private int _cid = 0;

        private void FeedbackCarrierID(string cmd)
        {
            //string message = string.Format("s00ACK:{0}/{1};\r", cmd, string.Join("", _state));
            string[] data = cmd.Replace(";", "").Split('/');

            _cid++;
            if (_cid > 1)
                _cid = 1;

            string fup = string.IsNullOrEmpty(FoupID) ? $"EF{ _cid}" : FoupID;

            string message = $"INF:{cmd.Replace(";", "")}/{fup};";

            OnWriteMessage(message);

        }

        private void FeedbackGetStatus(string cmd)
        {
            //string message = string.Format("s00ACK:{0}/{1};\r", cmd, string.Join("", _state));
            string[] data = cmd.Split('/');
            string reply = "";
            if (data[1] == "TRACK;")
            {
                reply = "00100";
            }
            if (data[1] == "VER;")
            {
                reply = "01.000(2021-03-11)";
            }
            if (data[1] == "INF1;")
            {
                reply = "0.06";
            }
            string message = $"INF:{cmd.Replace(";", "")}/{reply};";
            OnWriteMessage(message);

        }
        private void FeedbackGetSigStatus(string cmd)
        {
            string message = "";
            string[] data = cmd.Split('/');
            if (data[1] == "SYSTEM;")
            {
                message = "FFFF/FFFF";
            }
            if (data[1] == "P1;")
            {
                message = $"{ConvertBinToHexString(_lp1SigStateData1)}/{ConvertBinToHexString(_lp1SigStateData2)}";

            }
            if (data[1] == "P2;")
            {
                message = $"{ConvertBinToHexString(_lp2SigStateData1)}/{ConvertBinToHexString(_lp2SigStateData2)}";

            }
            string datamsg = $"INF:{cmd.Replace(";", "")}/{message};";
            OnWriteMessage(datamsg);

        }

        private void FeedbackGetVersion()
        {

        }


        //25 - 1
        private void FeedbackGetWaferMapDescendingOrder(string cmd)
        {
            var sm = _lp1slotMap.Reverse();

            if (cmd.Contains("P2"))
                sm = _lp2slotMap.Reverse();

            if (cmd.Contains("STATION"))
                sm = _stationslotMap.Reverse();
            //sm =Enumerable.Repeat("3",25); 

            string message = $"INF:{cmd.Replace(";", "")}/{string.Join("", sm)};";
            OnWriteMessage(message);
        }

        //1-25


        private void FeedbackGetWaferCount()
        {

        }


        #endregion

        private void ReceiveFinCommand(string cmd)
        {
            SendAck(cmd);
        }



        private void ReceiveEvtCommand(string cmd)
        {
            SendAck(cmd);
        }

        private void ReceiveTchCommand(string cmd)
        {
            SendAck(cmd);
        }

        private void ReceiveUnknownCommand(string message)
        {
            //LOG.Write("LoadPort" + _loadPortNumber + " Receive Unknown message," + message);
        }


        private void SendInf(string cmd)
        {
            Thread.Sleep(1000);

            //string msg = _isPlaced ? "s00INF:PODON;\r" : "s00INF:PODOF;\r";

            string message = Encoding.ASCII.GetString(BuildMesage($"INF:{cmd}"));
            OnWriteMessage("INF:" + cmd);

        }

        private void SendAck(string cmd)
        {


            string ack = Encoding.ASCII.GetString(BuildMesage($"ACK:{cmd}"));

            OnWriteMessage("ACK:" + cmd);

        }
        public static byte[] BuildMesage(string data)
        {
            List<byte> ret = new List<byte>();

            ret.Add(0x1);

            List<byte> cmd = new List<byte>();
            foreach (char c in data)
            {
                cmd.Add((byte)c);
            }
            //cmd.Add((byte)(':'));    //3A

            cmd.Add((byte)(';'));  //3B

            int length = cmd.Count + 4;

            int checksum = length;
            foreach (byte bvalue in cmd)
            {
                checksum += bvalue;
            }

            byte[] byteschecksum = Encoding.ASCII.GetBytes(Convert.ToString((int)((byte)(checksum & 0xFF)), 16));
            byte[] blength = BitConverter.GetBytes((short)length);
            ret.Add(blength[1]);
            ret.Add(blength[0]);
            ret.AddRange(new byte[] { 0, 0 });
            ret.AddRange(cmd);
            ret.AddRange(byteschecksum);
            ret.Add(0xD);
            return ret.ToArray();

        }

        public void PlaceCarrier(string lp)
        {

            string message = "";
            if (lp == "P1")
            {
                _lp1SigStateData1[0] = true; //LP Placement
                _lp1SigStateData1[1] = false; //Pod Present.

                message = $"EVT:SIGSTAT/{lp}/{ConvertBinToHexString(_lp1SigStateData1)}" +
                    $"/{ConvertBinToHexString(_lp1SigStateData2)}";

            }
            if (lp == "P2")
            {
                _lp2SigStateData1[0] = true; //LP Placement
                _lp2SigStateData1[1] = false; //Pod Present
                message = $"EVT:SIGSTAT/{lp}/{ConvertBinToHexString(_lp2SigStateData1)}" +
                    $"/{ConvertBinToHexString(_lp2SigStateData2)}";

            }
            OnWriteMessage(message);
        }
        private string ConvertBinToHexString(bool[] data)
        {
            int intdata = 0;

            for (int i = 0; i < data.Length; i++)
            {
                intdata += data[i] ? (int)Math.Pow(2, i) : 0;
            }
            return intdata.ToString("X4");
        }

        public void RemoveCarrier(string lp)
        {

            string message = "";
            if (lp == "P1")
            {
                _lp1SigStateData1[0] = false; //LP Placement
                _lp1SigStateData1[1] = false; //Pod Present.

                message = $"EVT:SIGSTAT/{lp}/{ConvertBinToHexString(_lp1SigStateData1)}" +
                    $"/{ConvertBinToHexString(_lp1SigStateData2)}";

            }
            if (lp == "P2")
            {
                _lp2SigStateData1[0] = false; //LP Placement
                _lp2SigStateData1[1] = false; //Pod Present
                message = $"EVT:SIGSTAT/{lp}/{ConvertBinToHexString(_lp2SigStateData1)}" +
                    $"/{ConvertBinToHexString(_lp2SigStateData2)}";

            }
            OnWriteMessage(message);
        }

        public void ClearWafer(string lp)
        {
            if (lp == "P1")
            {
                for (int i = 0; i < _lp1slotMap.Length; i++)
                {
                    _lp1slotMap[i] = "0";
                }
            }
            if (lp == "P2")
            {
                for (int i = 0; i < _lp2slotMap.Length; i++)
                {
                    _lp2slotMap[i] = "0";
                }

            }
        }

        public void SetAllWafer(string lp)
        {
            if (lp == "P1")
            {
                for (int i = 0; i < _lp1slotMap.Length; i++)
                {
                    _lp1slotMap[i] = "1";
                }
            }
            if (lp == "P2")
            {
                for (int i = 0; i < _lp2slotMap.Length; i++)
                {
                    _lp2slotMap[i] = "1";
                }

            }
        }


        public void SetUpWafer(string lp)
        {
            if (lp == "P1")
            {
                for (int i = 0; i < _lp1slotMap.Length; i++)
                {
                    _lp1slotMap[i] = i > 15 ? "1" : "0";
                }
            }
            if (lp == "P2")
            {
                for (int i = 0; i < _lp2slotMap.Length; i++)
                {
                    _lp2slotMap[i] = i > 15 ? "1" : "0";
                }

            }



        }


        public void SetLowWafer(string lp)
        {
            if (lp == "P1")
            {
                for (int i = 0; i < _lp1slotMap.Length; i++)
                {
                    _lp1slotMap[i] = i < 10 ? "1" : "0";
                }
            }
            if (lp == "P2")
            {
                for (int i = 0; i < _lp2slotMap.Length; i++)
                {
                    _lp2slotMap[i] = i < 10 ? "1" : "0";
                }

            }
        }

        public void RandomWafer(string lp)
        {
            Random _rd = new Random();
            if (lp == "P1")
            {
                for (int i = 0; i < _lp1slotMap.Length; i++)
                {
                    _lp1slotMap[i] = _rd.Next(0, 10) < 6 ? "0" : "1";
                }
            }
            if (lp == "P2")
            {
                for (int i = 0; i < _lp2slotMap.Length; i++)
                {
                    _lp2slotMap[i] = _rd.Next(0, 10) < 6 ? "0" : "1";
                }
            }
        }

    }

}
