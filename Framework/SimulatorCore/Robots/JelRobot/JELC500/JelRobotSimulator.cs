using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aitex.Core.RT.Log;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.LoadPorts.Hirata
{
   

    public class JelC5000RobotSimulator : SerialPortDeviceSimulator
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

        public JelC5000RobotSimulator(string portName)
            : base(portName, -1, "\r", ' ',false)
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
            _state[13] = "0";   //Dock Position 0 = Undock, 1 = dock;
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

        protected override void ProcessUnsplitMessage(byte[] data)
        {
            string message = Encoding.ASCII.GetString(data).Replace("$1","").Replace("\r","");
            message = message.Replace("$2", "");
            if (message == "RD")
            {
                SendAck();
                return;
            }
            if(message == "")
            {
                SendStatus(message);
                return;
            }
            if(message.Contains("IRS"))
            {
                SendSubRountine(message);
                return;
            }
            if (message.Contains("IR"))
            {
                SendRountine(message);
                return;
            }
            if (message.Contains("OH")|| message.Contains("OL")||message.Contains("OS")
                || message.Contains("OG") || message.Contains("OX") || message.Contains("OD"))
            {
                SendSpeedData(message);
                return;
            }
            if(message.Contains("6M"))
            {
                if (message == "6M1")
                {
                    OnWriteMessage($">$1-{message.Replace("6M", "")}888.888\r");
                }
                else
                    OnWriteMessage($">$1+{message.Replace("6M", "")}888.888\r");
                return;
            }
            if (message.Contains("6")&&!message.Contains("G"))
            {
                if (message == "61")
                {
                    OnWriteMessage($">$1-{message.Replace("6M", "")}888.888\r");
                }
                else
                    OnWriteMessage($">$1+{message.Replace("6M", "")}888.888\r");
                return;
            }
            if (message.Contains("PSD"))
            {
                SendPositionData(message);
                return;
            }
            if (message.Contains("A") && message.Contains("D"))
            {
                OnWriteMessage($">$1+100{message.Replace("A", "").Replace("D", "")}\r");
                return;
            }
            if (message.Contains("CS"))
            {
                string strvalue = message.Replace("CS", "");
                if(strvalue == "1")
                {
                    if (isrightarmOn) strvalue = "1";
                    else strvalue = "0";
                }
                if(strvalue == "2")
                {
                    if (isleftarmOn) strvalue = "1";
                    else strvalue = "0";
                }
                if (strvalue == "4")
                {
                    if (isrightarmOn) strvalue = "1";
                    else strvalue = "0";
                }
                OnWriteMessage($">$1{strvalue}\r");
                return;
            }
            if(message.Contains("DS10"))
            {
                isrightarmOn = false;
            }
            if (message.Contains("DS20"))
                isleftarmOn = false;
            if (message.Contains("DS11"))
                isrightarmOn = true;
            if (message.Contains("DS21"))
                isleftarmOn = true;
            if(message == "G")
            {
                SendCompaundStatus(message);
                return;
            }
            if(message.Contains("DTD"))
            {
                string parano = message.Replace("DTD", "").Replace("\r", "");
                OnWriteMessage($">$11{parano}1111,-002{parano}2222,3{parano}3333\r");
                return;
            }
            if(message == "BC")
            {
                OnWriteMessage($">$1{bankno}\r");
                return;
            }
            if(message.Contains("BC"))
            {
                bankno = message.Replace("BC", "");
            }
            if (message == "GER")
            {
                OnWriteMessage($">$1CompaundStopPostion\r");
                return;
            }
            if(message == "WCP")
            {
                OnWriteMessage($">$1{CassetNO},0");
                Thread.Sleep(50);
                OnWriteMessage($"{SlotNO}\r");
                return;
            }
            if(message.Contains("WCD"))
            {
                SlotNO = message.Replace("WCD", "");
            }
            if (message.Contains("WCP"))
            {
                CassetNO = message.Replace("WCP", "");
            }
            if(message == "WLO")
            {
                OnWriteMessage($">$1{mappingfirstslotpostion}\r");
                return;
            }
            if (message.Contains("WLO"))
            {
                mappingfirstslotpostion = message.Replace("WLO", "");
            }
            if(message == "WHI")
            {
                OnWriteMessage($">$1{mappingtopslotpostion}\r");
                return;
            }
            if (message.Contains("WHI"))
            {
                mappingtopslotpostion = message.Replace("WHI", "");
            }
            if (message == "WFC")
            {
                OnWriteMessage($">$1{mappingslotsnumber}\r");
                return;
            }
            if (message.Contains("WFC"))
            {
                mappingslotsnumber = message.Replace("WFC", "");
            }
            if (message == "WWN")
            {
                OnWriteMessage($">$1{mappingminwidth}\r");
                return;
            }
            if (message.Contains("WWN"))
            {
                mappingminwidth = message.Replace("WWN", "");
            }
            if (message == "WWM")
            {
                OnWriteMessage($">$1{mappingmaxwidth}\r");
                return;
            }
            if (message.Contains("WWM"))
            {
                mappingmaxwidth = message.Replace("WWM", "");
            }
            if (message == "WWG")
            {
                OnWriteMessage($">$1{mappinggatewidth}\r");
                return;
            }
            if (message.Contains("WWG"))
            {
                mappinggatewidth = message.Replace("WWG", "");
            }
            if (message == "WEND")
            {
                OnWriteMessage($">$1{mappingendpos}\r");
                return;
            }
            if (message.Contains("WEND"))
            {
                mappingendpos = message.Replace("WEND", "");
            }
            if (message == "WSP")
            {
                OnWriteMessage($">$1{mappingspeed}\r");
                return;
            }
            if (message.Contains("WSP"))
            {
                mappingendpos = message.Replace("WSP", "");
            }
            if (message == "WFK")
            {
                OnWriteMessage($">$10,0,1");
                OnWriteMessage($",0,1,0,1");

                OnWriteMessage($",0,1,0,0");
                OnWriteMessage($",0,0,0,0");
                OnWriteMessage($",0,0,0,0");
                OnWriteMessage($",0,0,0,0");
                OnWriteMessage($",0,0\r");


                return;
            }
            if (message == "WFW")
            {
                OnWriteMessage($">$1{mappingwidthresult()}\r");
                return;
            }

            if(message == "G1")     //LeftPick
            {
                //isleftarmOn = true;
                //isrightarmOn = true;
                SendAck();
                Thread.Sleep(2000);
                SendEnd("1");
                return;

            }
            
            if (message == "G2")
            {
                //isleftarmOn = false;
                //isrightarmOn = false;
                SendAck();
                Thread.Sleep(2000);
                SendEnd("2");
                return;
            }
            if(message == "G3")
            {
                //isleftarmOn = true;
                //isrightarmOn = true;
                SendAck();
                Thread.Sleep(2000);
                SendEnd("3");
                return;
            }
            if(message == "G4")
            {
                //isleftarmOn = false;
                SendAck();
                Thread.Sleep(2000);
                SendEnd("4");
                return;
            }
            if (message == "G5" || message == "G6" ||message == "G11")
            {
                isleftarmOn = true;
                isrightarmOn = true;
                SendAck();
                Thread.Sleep(2000);
                SendEnd("5");
                return;
            }
            //if (message == "G6")
            //{
            //    isrightarmOn = false;
            //    isleftarmOn = false;
            //    SendAck();
            //    Thread.Sleep(2000);
            //    SendEnd("6");
            //    return;
            //}
            if (message == "G7" || message == "G8" || message == "G13")
            {
                isrightarmOn = false;
                isleftarmOn = false;
                SendAck();
                Thread.Sleep(2000);
                SendEnd("7");
                return;
            }
            //if (message == "G8")
            //{
            //    //isrightarmOn = true;
            //    //isleftarmOn = false;
            //    SendAck();
            //    Thread.Sleep(2000);
            //    SendEnd("8");
            //    return;
            //}
            if (message == "G9")
            {
                //isrightarmOn = true;
                //isleftarmOn = false;
                SendAck();
                Thread.Sleep(2000);
                SendEnd("9");
                return;
            }
            if (message == "G16")
            {
                //isrightarmOn = true;
                //isleftarmOn = false;
                SendAck();
                Thread.Sleep(2000);
                SendEnd("16");
                return;
            }
            if (message.Contains("G")&&message !="G")     
            {
                //isleftarmOn = true;
                //isrightarmOn = true;
                SendAck();
                Thread.Sleep(2000);
                SendEnd(message.Replace("G",""));
                return;

            }
            if (message == "SPA")
            {
                OnWriteMessage($">$1{speed}\r");
                return;
            }
            if(message.Contains("SPA"))
            {
                speed = message.Replace("SPA", "");
            }

            SendAck();
        }
        string speed = "10";
        private string mappingwidthresult()
        {
            string ret = "";
            for (int i = 0; i < Convert.ToInt16(mappingslotsnumber); i++)
            {
                if (i == Convert.ToInt16(mappingslotsnumber) - 1)
                    ret += $"10{i}";
                else
                {
                    if (i < 10)
                        ret += $"10{i},";
                    else ret += "0,";
                }
            }
            return ret;
        }
        private string mappingwaferresult()
        {
            string ret = "";
            for(int i=0;i<Convert.ToInt16(mappingslotsnumber);i++)
            {
                if (i == Convert.ToInt16(mappingslotsnumber)-1)
                    ret += "1";
                else
                {
                    if (i < 10)
                        ret += "1,";
                    else ret += "0,";
                }
            }
            return ret;
        }
        
        private string mappingspeed = "0020";
        private string mappingendpos = "5123";
        private string mappinggatewidth = "50";
        private string mappingmaxwidth = "150";
        private string mappingminwidth = "100";
        private string mappingslotsnumber = "25";
        private string mappingfirstslotpostion = "0";
        private string mappingtopslotpostion = "100";
        private string bankno = "0";
        private string CassetNO = "1";
        private string SlotNO = "1";
        

        private void SendPositionData(string message)
        {
            string posno = message.Replace("PSD", "").Replace("\r", "");
            OnWriteMessage($">$1-1{posno}1111,2{posno}2222,3{posno}3333,-4{posno}8444\r");
            
        }

        private bool isleftarmOn, isrightarmOn;

        private void SendSpeedData(string message)
        {
            OnWriteMessage($">$1888\r");
        }

        private void SendRountine(string message)
        {
            OnWriteMessage($">$1Routine Content Of {message.Replace("IRS", "")}\r");
        }

        private void SendSubRountine(string message)
        {
            OnWriteMessage($">$1Subroutine Content Of {message.Replace("IRS","")}\r");
        }
        private void SendCompaundStatus(string message)
        {
            string status = ">";
            if (message == "G")
            {
                
                
                    status += "$10\r";
                    //statuscount = 0;
                
            }
            OnWriteMessage(status);
        }

        //private int statuscount = 0;

        private void SendStatus(string message)
        {
            string status = ">";
            if (message == "")
            {
                
                    status += "$100\r";
                    //statuscount = 0;
                               
            }
            OnWriteMessage(status);
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
                    _state[(int)StateIndex.Z_AxisPos] = "1";
                    _state[(int)StateIndex.Y_AxisPos] = "1";      //Dock
                    break;
                case "CLOAD": //loads the FOUP.
                    _state[(int)StateIndex.DoorClosed] = "0";
                    _state[(int)StateIndex.ClampClosed] = "1";
                    _state[(int)StateIndex.Z_AxisPos] = "1";
                    _state[(int)StateIndex.Y_AxisPos] = "1";
                    break;
                case "CULOD": //Unloads the FOUP (at the ejection position).
                    _state[(int)StateIndex.ClampClosed] = "0";
                    _state[(int)StateIndex.DoorClosed] = "1";
                    _state[(int)StateIndex.Z_AxisPos] = "0";
                    _state[(int)StateIndex.Y_AxisPos] = "0";      //Dock
                    break;
                case "YDOOR": //move to dock pos in fosb mode
                    _state[(int)StateIndex.Y_AxisPos] = "1";
                    break;
                case "YWAIT": //move to undock pos in fosb mode
                    _state[(int)StateIndex.Y_AxisPos] = "0";
                    break;
                case "DORBK": //move to door open pos in fosb mode
                    _state[(int) StateIndex.DoorClosed] = "0";
                    break;
                case "DORFW": //move to door close pos in fosb mode
                    _state[(int) StateIndex.DoorClosed] = "1";
                    break;
                case "ZDRDW": //move to door down pos in fosb mode
                    _state[(int)StateIndex.Z_AxisPos] = "1";
                    break;
                case "ZDRUP": //move to door up pos in fosb mode
                    _state[(int)StateIndex.Z_AxisPos] = "0";
                    break;
                case "STOP_":
                    break;

            }

            SendAck();

            Thread.Sleep(2000);
            if(needINF) SendInf(cmd);
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
            //string message = string.Format("s00ACK:{0}/{1};\r", cmd, string.Join("", _state));

            string message = Encoding.ASCII.GetString(BuildMesage($"ACK:{cmd}/{string.Join("", _state)}"));
            OnWriteMessage(message);

        }

        private void FeedbackGetVersion()
        {

        }

        private void FeedbackGetIndicator(string cmd)
        {
            //string message = string.Format("s00ACK:{0}/{1};\r", cmd, string.Join("", _led));

            string message = Encoding.ASCII.GetString(BuildMesage($"ACK:{cmd}/{string.Join("", _led)}"));
            OnWriteMessage(message);
            //OnWriteMessage(message);

        }

        //25 - 1
        private void FeedbackGetWaferMapDescendingOrder(string cmd)
        {
            var sm = _slotMap.Reverse();
            string message = Encoding.ASCII.GetString(BuildMesage($"ACK:{cmd}/{string.Join("", sm)}"));
            OnWriteMessage(message);
        }

        //1-25
        private void FeedbackGetWaferMapAscendingOrder(string cmd)
        {
            //string message = string.Format("s00ACK:{0}/{1};\r", cmd, string.Join("", _slotMap));
            string message = Encoding.ASCII.GetString(BuildMesage($"ACK:{cmd}/{string.Join("", _slotMap)}"));
            OnWriteMessage(message);
        }

        private void FeedbackGetWaferCount()
        {

        }

        private void FeedbackGetFOSBMode(string cmd)
        {
            //string message = string.Format("s00ACK:{0}/{1};\r", cmd, string.Join("", _state));
            string message = Encoding.ASCII.GetString(BuildMesage($"ACK:{cmd}/{string.Join("", _state)}"));
            OnWriteMessage(message);
        }

        #endregion

       
        private void ReceiveUnknownCommand(string message)
        {
            //LOG.Write("LoadPort" + _loadPortNumber + " Receive Unknown message," + message);
        }


        private void SendInf(string cmd)
        {
            Thread.Sleep(1000);

            //string msg = _isPlaced ? "s00INF:PODON;\r" : "s00INF:PODOF;\r";

            string message = Encoding.ASCII.GetString(BuildMesage($"INF:{cmd}"));
            OnWriteMessage(message);

        }
        private void SendEnd(string cmd)
        {
            string ack = "END-" + cmd + "\r"; ;
            OnWriteMessage(ack);

        }
        private void SendAck()
        {


            string ack = ">";

            OnWriteMessage(ack);

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

        public void PlaceCarrier()
        {
            //SimManager.Instance.PlaceFoup(_loadPortNumber);

            //_isPlaced = _isPresent = true;

            _state[6] = "1";

            //_led[(int) Indicator.PLACEMENT] = _isPlaced ? "1" : "0";
            //_led[(int) Indicator.PRESENCE] = _isPresent ? "1" : "0";

            //string msg = "s00INF:PODON;\r";
            string message = Encoding.ASCII.GetString(BuildMesage($"INF:PODON"));
            OnWriteMessage(message);
        }

        public void RemoveCarrier()
        {
            //SimManager.Instance.RemoveFoup(_loadPortNumber);

            //_isPlaced = _isPresent = false;
            _state[6] = "0";

            //_led[(int) Indicator.PLACEMENT] = "0";
            //_led[(int) Indicator.PRESENCE] = "0";

            //string msg = "s00INF:PODOF;\r";
            string message = Encoding.ASCII.GetString(BuildMesage($"INF:PODOF"));
            OnWriteMessage(message);
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
                _slotMap[i] = _rd.Next(0,10) < 6 ? "0" : "2";
            }
        }

    }

}
