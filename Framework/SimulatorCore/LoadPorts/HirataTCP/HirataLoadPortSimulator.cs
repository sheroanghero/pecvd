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
   

    public class HirataLoadPortTCPSimulator : SocketDeviceSimulator
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

        public HirataLoadPortTCPSimulator(int tcpPort)
            : base(tcpPort, -1, "\r", ' ')
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

        protected override void ProcessUnsplitMessage(string message)
        {

            //string message = Encoding.ASCII.GetString(data); 


            //message = message.Replace("s00", "").Replace(";\r", "");
            if (message.Length >= 8)
            {
                int cmdlength = message.Length - 8;
                message = message.Substring(5, cmdlength).Replace(";", "");
                //message = message.Replace("s00", "").Replace(";", "");
            }
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
                    _state[(int)StateIndex.DoorClosed] = "0";
                    break;
                case "DORFW": //move to door close pos in fosb mode
                    _state[(int)StateIndex.DoorClosed] = "1";
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

            SendAck(cmd);

            Thread.Sleep(2000);
            if (needINF) SendInf(cmd);
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
            OnWriteMessage(message);

        }

        private void SendAck(string cmd)
        {


            string ack = Encoding.ASCII.GetString(BuildMesage($"ACK:{cmd}"));

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
                _slotMap[i] = i > 15 ? "1" : "0";
            }
        }


        public void SetLowWafer()
        {
            for (int i = 0; i < _slotMap.Length; i++)
            {
                _slotMap[i] = i < 10 ? "1" : "0";
            }
        }

        public void RandomWafer()
        {
            Random _rd = new Random();
            for (int i = 0; i < _slotMap.Length; i++)
            {
                _slotMap[i] = _rd.Next(0, 10) < 6 ? "0" : "2";
            }
        }

    }

}
