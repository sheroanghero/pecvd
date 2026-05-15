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

    public class XRFPMSimulator : SocketDeviceSimulator
    {
        public string SlotMap
        {
            get { return string.Join("", _slotMap); }
        }
        //public event Action<string> WriteDeviceEvent;

        private string[] _slotMap = new string[25];
        private string[] _state = new string[20];
        private string[] _led = new string[13];

        private char linedemiliter = Encoding.ASCII.GetChars(new byte[] { 0x4 })[0];

        public string InforPadState { get; set; } = "0";

        //private bool _isPlaced;
        //private bool _isPresent;

        //private int _moveTime = 5000; //

        public XRFPMSimulator(int port)
            : base(port, -1, Encoding.ASCII.GetString(new byte[] {0x10,0x4 }), ' ')
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
        //private bool _isWaferON;
        //private string state = "Init";

        //int retrytime = 1;
        protected override void ProcessUnsplitMessage(byte[] msg)
        {
            //byte[] msg = Encoding.ASCII.GetBytes(message);
            int msgLength = msg.Length;
            byte functionID = msg[8];

            List<byte> reply = msg.Take(4).ToList();
            //reply.Add(functionID);
            List<byte> dataArea = new List<byte> { functionID };

            byte[] state = new byte[] { 0x8, 0x8 };

            byte[] crc;

            switch(functionID)
            {
                case 32:   //Get Status                    
                    dataArea.AddRange(new byte[] { 0x8, 0x18 });               
                    break;
                case 36:    //Get Current analysis file   
                    dataArea.AddRange(Encoding.ASCII.GetBytes("Recipe1"));
                    break;
                case 37:  //Get/Set XYZ                    
                    byte[] xPos = BitConverter.GetBytes(Xpos);
                    byte[] yPos = BitConverter.GetBytes(Ypos);
                    byte[] zPos = BitConverter.GetBytes(Zpos);

                    if (msg.Length >=25)
                    {
                        Xpos = BitConverter.ToSingle(msg.Skip(9).Take(4).ToArray(),0);
                        Ypos = BitConverter.ToSingle(msg.Skip(13).Take(4).ToArray(), 0);
                        Zpos = BitConverter.ToSingle(msg.Skip(17).Take(4).ToArray(), 0);
                        xPos = BitConverter.GetBytes(Xpos);
                        yPos = BitConverter.GetBytes(Ypos);
                        zPos = BitConverter.GetBytes(Zpos);
                    }

                    dataArea.AddRange(xPos);
                    dataArea.AddRange(yPos);
                    dataArea.AddRange(zPos);
                    break;
                case 49:
                  
                    dataArea.Add(1);
                    break;
                case 50:
                    dataArea.Add(1);
                    break;
                case 64:
                    


                    dataArea.AddRange(BitConverter.GetBytes(1));
                    crc = ToCRC16_CCITT_FALSE(dataArea.ToArray());
                    reply.AddRange(dataArea);
                    if (functionID == 50 && msg[9] == 1)
                    {
                        reply.AddRange(Encoding.ASCII.GetBytes("Result is OK"));
                    }
                    reply.AddRange(new byte[] { 0x10, 0x3 });


                    reply.AddRange(crc.Reverse());
                    //reply.AddRange(new byte[] { 0x10});


                    SendMessage(reply.ToArray());
                    

                    Thread.Sleep(3000);



                    reply = msg.Take(4).ToList();
                    //reply.Add(functionID);
                    dataArea = new List<byte> { 0x20 };

                    dataArea.AddRange(new byte[] { 0x01, 0x12 });
                    crc = ToCRC16_CCITT_FALSE(dataArea.ToArray());
                    reply.AddRange(dataArea);
                    if (functionID == 50 && msg[9] == 1)
                    {
                        reply.AddRange(Encoding.ASCII.GetBytes("Result is OK"));
                    }
                    reply.AddRange(new byte[] { 0x10, 0x3 });


                    reply.AddRange(crc.Reverse());
                    //reply.AddRange(new byte[] { 0x10});


                    SendMessage(reply.ToArray());

                    Thread.Sleep(2000);

                    reply = msg.Take(4).ToList();
                    //reply.Add(functionID);
                    dataArea = new List<byte> { 0x20 };

                    dataArea.AddRange(new byte[] { 0x08, 0x12 });
                    crc = ToCRC16_CCITT_FALSE(dataArea.ToArray());
                    reply.AddRange(dataArea);
                    if (functionID == 50 && msg[9] == 1)
                    {
                        reply.AddRange(Encoding.ASCII.GetBytes("Result is OK"));
                    }
                    reply.AddRange(new byte[] { 0x10, 0x3 });
                    reply.AddRange(crc.Reverse());
                    //reply.AddRange(new byte[] { 0x10});
                    SendMessage(reply.ToArray());
                    return;                   
                case 72:
                    //if (retrytime++ % 3 != 0)
                    //{
                    //    return;
                    //}
                    dataArea.AddRange(Encoding.UTF8.GetBytes("|Result:11.11:22.22:33.33|layer 1 OK|lay 2 NG|"));
                    break;





            }

            crc = ToCRC16_CCITT_FALSE(dataArea.ToArray());
            reply.AddRange(dataArea);
            if (functionID == 50 && msg[9] == 1)
            {
               reply.AddRange(Encoding.ASCII.GetBytes("Result is OK"));
            }
            reply.AddRange(new byte[] { 0x10, 0x3 });
            

            reply.AddRange(crc.Reverse());
            //reply.AddRange(new byte[] { 0x10});

            
            SendMessage(reply.ToArray());



            //message = message.Replace("s00", "").Replace(";", "");


        }

        public float Xpos, Ypos, Zpos;

        public static byte[] CRC16(byte[] data)
        {
            int len = data.Length;
            if (len > 0)
            {
                ushort crc = 0xFFFF;

                for (int i = 0; i < len; i++)
                {
                    crc = (ushort)(crc ^ (data[i]));
                    for (int j = 0; j < 8; j++)
                    {
                        crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
                    }
                }
                byte hi = (byte)((crc & 0xFF00) >> 8);  //高位置
                byte lo = (byte)(crc & 0x00FF);         //低位置

                return new byte[] { hi, lo };
            }
            return new byte[] { 0, 0 };
        }


        private byte[] ToCRC16_CCITT_FALSE(byte[] buffer)  //C# crc-16/CCITT-FALSE,带判断校验的
        {
            byte[] ResCRC16 = new byte[2];
            
            ushort crc = 0xFFFF;
            int size = buffer.Length;  //计算待计算的数据长度
            int i = 0;

            if (size > 0)
            {
                while (size-- > 0)
                {
                    crc = (ushort)((crc >> 8) | (crc << 8));
                    crc ^= buffer[i++];
                    crc ^= (ushort)(((byte)crc) >> 4);
                    crc ^= (ushort)(crc << 12);
                    crc ^= (ushort)((crc & 0xff) << 5);
                }
            }
            //判断输入的ResCRC16与计算出来的是否一致
            //if (ResCRC16[0] == (byte)((crc >> 8) & 0xff) && ResCRC16[1] == (byte)(crc & 0xff))
            //{
            //    status = true;
            //}
            ResCRC16[1] = (byte)(crc & 0xff);
            ResCRC16[0] = (byte)((crc >> 8) & 0xff);
            return ResCRC16;
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
                case "CUDNC":
                    _state[(int)StateIndex.ClampClosed] = "0";
                    _state[(int)StateIndex.VacuumOn] = "0";
                    _state[(int)StateIndex.Y_AxisPos] = "0";
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
                    _state[(int)StateIndex.Y_AxisPos] = "1";
                    _state[(int)StateIndex.ClampClosed] = "1";
                    break;
                case "CLDOP": //FOUP dock:  
                    _state[(int)StateIndex.Z_AxisPos] = "1";
                    _state[(int)StateIndex.DoorClosed] = "0";
                    break;
                case "CUDCL": //FOUP undock
                    _state[(int)StateIndex.ClampClosed] = "0";
                    break;
                case "CULFC": //FOUP undock
                    _state[(int)StateIndex.DoorClosed] = "1";
                    break;
                case "CULDK": //Door Close
                    _state[(int)StateIndex.DoorClosed] = "1";
                    _state[(int)StateIndex.Z_AxisPos] = "0";
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
                    _state[0] = "0";
                    _state[1] = "0";
                    _state[2] = "1";
                    _state[3] = "0";
                    _state[4] = "0";
                    _state[5] = "0";
                    _state[6] = "1";
                    _state[7] = "1";
                    _state[8] = "0";
                    _state[9] = "1";
                    _state[10] = "0";
                    _state[11] = "1";
                    _state[12] = "1";
                    _state[13] = "1";
                    _state[14] = "1";
                    _state[15] = "1";
                    _state[16] = "1";
                    _state[17] = "1";
                    _state[18] = "0";
                    _state[19] = "B";



                    //_state[(int)StateIndex.Z_AxisPos] = "1";
                    break;
                case "ZDRUP": //move to door up pos in fosb mode
                    _state[(int)StateIndex.Z_AxisPos] = "0";
                    break;
                case "STOP_":
                    break;

            }

            SendAck(cmd);

            Thread.Sleep(2000);
            if(needINF)
            {
                //if (cmd == "CLDMP")
                //{
                //    SendABS(cmd);
                //    return;
                //}
                SendInf(cmd);


            }
                
        }



        private void SendMessage(byte[] cmd)
        {
            Thread.Sleep(1000);


            
            OnWriteMessage(cmd);

        }

        private void ReceiveSetCommand(string cmd)
        {

            if (cmd.Contains("FSB"))
            {
                SendAck(cmd); return;
            }
            if(cmd.Contains("LOF"))
                SendAckAndInf(cmd);
            else
            {
                SendAck(cmd);
                SendInf(cmd);
            }
            //SendInf(cmd);
        }
        private void SendAckAndInf(string cmd)
        {
            string ack = string.Format("s00ACK:{0};\rs00INF:{0};\r", cmd);

            OnWriteMessage(ack);

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

            string message = string.Format("s00INF:{0};\r", cmd);
            OnWriteMessage(message);

        }

        private void SendAck(string cmd)
        {
            string ack = string.Format("s00ACK:{0};\r", cmd);

            OnWriteMessage(ack);

        }

        public void PlaceCarrier()
        {
            //SimManager.Instance.PlaceFoup(_loadPortNumber);

            //_isPlaced = _isPresent = true;

            _state[6] = "1";
            //_state[10] = "0";

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
                var rtn= _rd.Next(0, 10) % 4;
                switch (rtn)
                {
                    case 0:
                        _slotMap[i] = "0";
                        break;
                    case 1:
                        _slotMap[i] = "1";
                        break;
                    case 2:
                        _slotMap[i] = "2";
                        break;
                    case 3:
                        _slotMap[i] = "W";
                        break;
                    default:
                        break;
                }
            }
        }

    }

}
