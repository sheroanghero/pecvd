using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aitex.Core.RT.Log;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.LoadPorts
{
    //public enum StateIndex : int
    //{
    //    EquipmentStatus,
    //    Mode,
    //    InitialPosition,
    //    OperationStatus,
    //    ErrorCodeUpper,

    //    ErrorCodeLower,
    //    Present,
    //    ClampClosed,
    //    LatchClosed,
    //    VacuumOn,

    //    DoorClosed,
    //    WaferProtrusion,

    //}

    public class TazmoAlignerSimulator : SerialPortDeviceSimulator
    {
        public string SlotMap
        {
            get { return string.Join("", _slotMap); }
        }
        //public event Action<string> WriteDeviceEvent;

        private string[] _slotMap = new string[25];
        private string[] _state = new string[20];
        private string[] _led = new string[13];

        public string InforPadState { get; set; } = "0";

        //private bool _isPlaced;
        //private bool _isPresent;

        //private int _moveTime = 5000; //

        public TazmoAlignerSimulator(string portName)
            : base(portName, -1, "", ' ')
        {



            for (int i = 0; i < _slotMap.Length; i++)
                _slotMap[i] = "0";
            for (int i = 0; i < _state.Length; i++)
                _state[i] = "0";
            for (int i = 0; i < _led.Length; i++)
                _led[i] = "0";


            //00110 01?10 10001 01100

            //A000A 41010 10001 01000
            _state[0] = "0"; //Equipment status 0 = Normal A = Recoverable error E = Fatal error
            _state[1] = "0"; //Mode 0 = Online                      1 = Teaching
            _state[2] = "0"; //Initial position 0 = Unexecuted       1 = Executed
            _state[3] = "0"; //Operation status 0 = Stopped           1 = Operating
            _state[4] = "0"; //Error code Error code (upper)

            _state[5] = "0"; // Error code Error code (lower)
            _state[6] = "0"; //Cassette presence 0 = None            1 = Normal position        2 = Error load
            _state[7] = "0"; //FOUP clamp status 0 = Open            1 = Close                  ? = Not defined
            _state[8] = "0"; //Latch key status 0 = Open             1 = Close                 ? = Not defined
            _state[9] = "0"; //Vacuum 0 = OFF 1 = ON

            _state[10] = "1"; //Door position 0 = Open position      1 = Close position         ? = Not defined
            _state[11] = "0"; //Wafer protrusion sensor 0 = Blocked. 1 = Unblocked.
            _state[12] = "0";
            _state[13] = "0";
            _state[14] = "0";

            _state[15] = "0";
            _state[16] = "0";
            _state[17] = "0";
            _state[18] = "0";
            _state[19] = InforPadState;

            //A000A 41010 10001 01000
            _led[0] = "0"; //LOAD
            _led[1] = "0"; //UNLOAD
            _led[2] = "0"; //OP.ACCESS
            _led[3] = "0"; //PRESENCE
            _led[4] = "0"; //PLACEMENT

            _led[5] = "0"; // ALARM
            _led[6] = "0"; //STATUS1
            _led[7] = "0"; //STATUS2
            _led[8] = "0"; // 
            _led[9] = "0"; // 

            _led[10] = "0"; // 
            _led[11] = "0"; // 
            _led[12] = "0";
        }

        //public void ChangeSlotMap(SlotMapChangedEventArgs obj)
        //{
        //    string slotMap = obj.SlotMap.Replace("'", "");
        //    for (int i = 0; i < slotMap.Length; i++)
        //        _slotMap[i] = slotMap.Substring(i, 1);
        //}

        protected override void ProcessUnsplitMessage(string message)
        {
            string cmd = message.Replace("s00", "").Replace("\r", "").Replace("\u0006","");

            //message = message.Replace("s00", "").Replace(";", "");
            //string[] msg = message.Split(':');

            //if (msg.Length < 2)
            //    return;


            //string type = msg[0];
            //string cmd = msg[1];

            





            if (cmd.Contains("ALG"))
            {
                _liftUP = false;
                //_waferON = true;
                stsfeedback = "102";
                ReceiveTwinCommand(cmd, 4);
                return;
            }

            if (cmd.Contains("PRR"))
            {
                _liftUP = true;
                ReceiveTwinCommand(cmd, 4);
                return;
            }
            if (cmd.Contains("VVN"))
            {
                ReceiveTwinCommand(cmd, 2);
                return;
            }

            if (cmd.Contains("WCH"))
            {
                

                OnWriteMessage("WCH,0\r");

                return;
            }
            if (cmd.Contains("VCH"))
            {
                OnWriteMessage("VCH,0\r");
                return;
            }


            if (cmd.Contains("VVF"))
            {
                stsfeedback = "102";
                ReceiveTwinCommand(cmd, 2);
                return;
            }
            //if (cmd.Contains("VVF")) ReceiveTwinCommand(cmd, 2);
            if (cmd.Contains("NYG"))
            {
                stsfeedback = "100";
                ReceiveTwinCommand(cmd, 2);
                return;
            }
            if (cmd.Contains("PRP"))
            {
                _liftUP = true;
                ReceiveTwinCommand(cmd, 2);
                return;
            }
            if (cmd.Contains("STS"))
            {
                ReceiveQueryCommand(cmd);
                return;
            }
            if (cmd.Contains("STU"))
            {
                ReceiveQueryCommand(cmd);
                return;
            }
            if (cmd.Contains("DWL"))
            {
                ReceiveSignleCommand(cmd);
                return;
            }


            switch (cmd)
            {
                case "RST":
                    ReceiveTwinCommand(cmd,3);
                    break;
                case "ERF":
                    ReceiveSignleCommand(cmd);
                    break;
                case "HOM":
                    //_waferON = false;
                    _liftUP = false;
                    ReceiveTwinCommand(cmd, 5);
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
            string C41Feedback = ASCIIEncoding.Default.GetString(new byte[] { 0x50, 0x43, 0x74, 0x6F, 0x50, 0x41, 0x3A, 0x43, 0x34, 0x31, 0x0D, 0x0A, 0x31, 0x0D, 0x0A, 0x4D, 0x34, 0x31, 0x0A });
            if (message == "C01") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M00" });
            if (message == "C02") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M01" });
            if (message == "C03") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M03" });
            if (message == "C04") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M05" });
            if (message == "C05") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M07" });
            if (message == "C06") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M09" });
            if (message == "C07") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M09" });
            if (message == "C08") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M10" });
            if (message == "C09") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M10" });
            if (message == "C10") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M11" });
            if (message == "C11") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M12" });
            if (message == "C12") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M12" });
            if (message == "C13") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M13" });
            if (message == "C14") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M13" });
            if (message == "C15") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M11" });
            if (message == "C16") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M31" });
            if (message == "C17") SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M32" });
            if (message == "C40") SendHonghuPAResponse(new string[] { "M40" });
            if (message == "C41") SendHonghuPAResponse(new string[] { "M41" });

            if (message.Contains("A")) SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M14" });
            if (message.Contains("B")) SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M15" });
            if (message.Contains("T")) SendHonghuPAResponse(new string[] { $"PCtoPA:{message}", "M16" });

        }

        private void ReceiveSignleCommand(string cmd)
        { 
            SendAck(cmd);
        }
        private string stsfeedback = "100";

        private void ReceiveQueryCommand(string cmd)
        {
            string data = string.Empty;
            if (cmd == "STS")
                data = cmd + "," + stsfeedback + "\r";
            if(cmd == "STU")
                data = cmd + ",00" +(_liftUP? "0":"1") +"1001011"  + "\r";
            OnWriteMessage(data);
        }

        //private bool _waferON = false;

        private bool _liftUP;


        private void ReceiveTwinCommand(string cmd,int delaytime)
        {
            SendAck(cmd);

            Thread.Sleep(1000* delaytime);

            SendResponse(cmd);
        }

        
        private void SendResponse(string cmd)
        {
            string data = cmd + "\r";

            OnWriteMessage(data);

          
        }


        private void ReceiveMovCommand(string cmd)
        {
            bool needINF = true;
            switch (cmd)
            {
                case "ORGSH": //back to initial
                    _state[(int)StateIndex.ClampClosed] = "0";
                    _state[(int)StateIndex.DoorClosed] = "1";
                    
                    break;
                case "ABORG": //Force back to initial
                    _state[(int)StateIndex.ClampClosed] = "0";
                    _state[(int)StateIndex.DoorClosed] = "1";
                    
                    break;
                case "PODCL": //FOUP clamp: Close
                    _state[(int)StateIndex.ClampClosed] = "1";
                    //needINF = false;
                    break;
                case "PODOP": //FOUP clamp: open
                    _state[(int)StateIndex.ClampClosed] = "0";
                    break;
                case "CLDDK": //FOUP dock:  
                    _state[(int)StateIndex.ClampClosed] = "1";
                    break;
                case "CUDCL": //FOUP undock
                    _state[(int)StateIndex.ClampClosed] = "0";
                    break;
                case "CULFC": //FOUP undock
                    _state[(int)StateIndex.DoorClosed] = "1";
                    break;
                case "CULDK": //Door Close
                    _state[(int)StateIndex.DoorClosed] = "1";
                    break;
                case "CLMPO": //Door open
                    _state[(int)StateIndex.DoorClosed] = "0";
                    break;
                case "CLDMP": //Maps and loads the FOUP.
                    _state[(int)StateIndex.DoorClosed] = "0";
                    _state[(int)StateIndex.ClampClosed] = "1";
                    break;
                case "CLOAD": //loads the FOUP.
                    _state[(int)StateIndex.DoorClosed] = "0";
                    _state[(int)StateIndex.ClampClosed] = "1";
                    break;
                case "CULOD": //Unloads the FOUP (at the ejection position).
                    _state[(int)StateIndex.ClampClosed] = "0";
                    _state[(int)StateIndex.DoorClosed] = "1";
                    break;
                case "YDOOR": //move to dock pos in fosb mode
                    break;
                case "YWAIT": //move to undock pos in fosb mode
                    break;
                case "DORBK": //move to door open pos in fosb mode
                    _state[(int) StateIndex.DoorClosed] = "0";
                    break;
                case "DORFW": //move to door close pos in fosb mode
                    _state[(int) StateIndex.DoorClosed] = "1";
                    break;
                case "ZDRDW": //move to door down pos in fosb mode
                    break;
                case "ZDRUP": //move to door up pos in fosb mode
                    break;
                case "STOP_":
                    break;

            }

            SendAck(cmd);

            Thread.Sleep(2000);
            if(needINF) SendInf(cmd);
        }

        private void ReceiveSetCommand(string cmd)
        {
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
            _state[19] = InforPadState;
            switch (cmdGet)
            {
                case "STATE":
                    FeedbackGetStatus(cmdGet);
                    break;

                case "VERSN":
                    FeedbackGetVersion();
                    break;

                case "LEDST":
                    FeedbackGetIndicator(cmdGet);
                    break;

                case "MAPDT":
                    FeedbackGetWaferMapDescendingOrder(cmdGet);
                    break;

                case "MAPRD":
                    FeedbackGetWaferMapAscendingOrder(cmdGet);
                    break;

                case "WFCNT":
                    FeedbackGetWaferCount();
                    break;
                
                case "FSBxx":
                    FeedbackGetFOSBMode(cmdGet);
                    break;
            }
        }

        private void FeedbackGetStatus(string cmd)
        {
            string message = string.Format("s00ACK:{0}/{1};\r", cmd, string.Join("", _state));
            OnWriteMessage(message);

        }

        private void FeedbackGetVersion()
        {

        }

        private void FeedbackGetIndicator(string cmd)
        {
            string message = string.Format("s00ACK:{0}/{1};\r", cmd, string.Join("", _led));
            OnWriteMessage(message);

        }

        //25 - 1
        private void FeedbackGetWaferMapDescendingOrder(string cmd)
        {
            var sm = _slotMap.Reverse();
            string message = string.Format("s00ACK:{0}/{1};\r", cmd, string.Join("", sm));
            OnWriteMessage(message);
        }

        //1-25
        private void FeedbackGetWaferMapAscendingOrder(string cmd)
        {
            string message = string.Format("s00ACK:{0}/{1};\r", cmd, string.Join("", _slotMap));
            OnWriteMessage(message);
        }

        private void FeedbackGetWaferCount()
        {

        }

        private void FeedbackGetFOSBMode(string cmd)
        {
            string message = string.Format("s00ACK:{0}/{1};\r", cmd, string.Join("", _state));
            OnWriteMessage(message);
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

            string message = string.Format("{0};\r", cmd);
            OnWriteMessage(message);

        }

        private void SendAck(string cmd)
        {
            string ack = Encoding.Default.GetString(new byte[] { 0x6});

            OnWriteMessage(ack);

        }
        private void SendHonghuPAResponse(string[] cmds)
        {
            foreach (string cmd in cmds)
            {

                Thread.Sleep(200);
                OnWriteMessage(cmd + "\n");
            }
        }

        public void PlaceCarrier()
        {
            //SimManager.Instance.PlaceFoup(_loadPortNumber);

            //_isPlaced = _isPresent = true;

            _state[6] = "1";

            //_led[(int) Indicator.PLACEMENT] = _isPlaced ? "1" : "0";
            //_led[(int) Indicator.PRESENCE] = _isPresent ? "1" : "0";

            string msg = "s00INF:PODON;\r";
            OnWriteMessage(msg);
        }

        public void RemoveCarrier()
        {
            //SimManager.Instance.RemoveFoup(_loadPortNumber);

            //_isPlaced = _isPresent = false;
            _state[6] = "0";

            //_led[(int) Indicator.PLACEMENT] = "0";
            //_led[(int) Indicator.PRESENCE] = "0";

            string msg = "s00INF:PODOF;\r";
            OnWriteMessage(msg);
        }

        public void ClearWafer()
        {
            for (int i = 0; i < _slotMap.Length; i++)
            {
                _slotMap[i] = "0";
            }
        }

        public void SetAllWafer()
        {
            for (int i = 0; i < _slotMap.Length; i++)
            {
                _slotMap[i] = "1";
            }
        }


        public void SetUpWafer()
        {
            for (int i = 0; i < _slotMap.Length; i++)
            {
                _slotMap[i] =  i>15 ? "1" : "0";
            }
        }


        public void SetLowWafer()
        {
            for (int i = 0; i < _slotMap.Length; i++)
            {
                _slotMap[i] = i <10 ? "1" : "0";
            }
        }

        public void RandomWafer()
        {
            Random _rd = new Random();
            for (int i = 0; i < _slotMap.Length; i++)
            {
                _slotMap[i] = _rd.Next(0,10) < 6 ? "0" : "1";
            }
        }

    }

}
