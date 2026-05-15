using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.CarrierIDReaderBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.OmronV640
{
    public class OmronV640Tcp:CIDReaderBaseDevice,IConnection
    {
        private string _scRoot;
        private DateTime _startTime;
        private int _retryTime;
        public OmronV640Tcp(string module,string name,string scRoot,LoadPortBaseDevice lp =null,int offset=0,int length=16,int readerIndex=1,bool isUpdateLPCid = true)
            :base(module,name,lp,readerIndex, isUpdateLPCid)
        {
            _scRoot = scRoot;
            _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            Offset = offset;
            Length = length;

            _socket = new AsyncSocket(_address);
            _socket.OnDataChanged += new AsyncSocket.MessageHandler(OnDataChanged);
            _socket.OnErrorHappened += new AsyncSocket.ErrorHandler(OnErrorHandler);

            ConnectionManager.Instance.Subscribe($"{Name}", this);
            //int connecttime = 0;
            //_socket.Connect(_address);
            Reset();

        }
        
        private void OnErrorHandler(ErrorEventArgs args)
        {
            if (!_socket.IsConnected)
                return;
            EV.PostWarningLog(Module, $"{Name} occurred error:{args.Reason}.");




            //if (_socket.IsConnected)
            //    return;
            //else
            //{
            //    _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            //    _socket.Connect(_address);

            //}

            //OnError();
        }
        private bool _isEnableSendError
        {
            get
            {
                if (SC.ContainsItem($"{_scRoot}.{Name}.EnableSendError"))
                    return SC.GetValue<bool>($"{_scRoot}.{Name}.EnableSendError");
                return true;
            }
        }
        private void OnDataChanged(string package)
        {
            try
            {
                Thread.Sleep(500);
                package = package.ToUpper();
                string[] msgs = Regex.Split(package, "\r");
                foreach (string msg in msgs)
                {
                    if (msg.Length > 0)
                    {
                        string type = msg.Substring(0, 2);
                        if (type != "00")
                        {
                            EV.PostWarningLog("RFID", $"{Name} occurred error:{getErrMsg(type)}");
                            if (DeviceState == CIDReaderStateEnum.ReadCarrierID)
                            {
                                if(_retryTime < RetryTime)
                                {
                                    EV.PostInfoLog(Module, $"{Name} retry {_retryTime + 1} time to read RFID due to received error");

                                    _retryTime++;
                                    _socket.Write(_sendMsg);
                                    return;
                                }

                                if(_isEnableSendError)
                                    OnCarrierIDReadFailed(type);
                                else
                                {                                   
                                    OnActionDone();
                                }
                            }
                            if (DeviceState == CIDReaderStateEnum.WriteCarrierID)
                            {
                                if (_retryTime < RetryTime)
                                {
                                    EV.PostInfoLog(Module, $"{Name} retry {_retryTime + 1} time to write RFID due to received error");

                                    _retryTime++;
                                    _socket.Write(_sendMsg);
                                    return;
                                }
                                OnCarrierIDWriteFailed(type);
                            }
                            return;
                        }
                        if (DeviceState == CIDReaderStateEnum.ReadCarrierID)
                        {
                            string msgdata = msg.Substring(2, msg.Length - 2);
                            string tempID = HEX2ASCII(msg);
                            tempID = tempID.Trim('\0');
                            if (tempID.Contains("\0"))
                                tempID = tempID.Split('\0')[0];

                            CarrierIDBeRead = tempID;
                            
                            if (IsNeedTrimSpace)
                            {
                                CarrierIDBeRead = CarrierIDBeRead.Trim();
                            }
                            OnCarrierIDRead(CarrierIDBeRead);
                        }
                        if (DeviceState == CIDReaderStateEnum.WriteCarrierID)
                        {
                            OnCarrierIDWrite();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

            }
        }
        protected override bool fStartReset(object[] param)
        {
            _reconnectTimes = 0;
            if (_socket.IsConnected)
                return true;
            else
            {
                _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
                //_socket.Dispose();
                //_socket = new AsyncSocket(_address);
                //_socket.OnDataChanged += new AsyncSocket.MessageHandler(OnDataChanged);
                //_socket.OnErrorHappened += new AsyncSocket.ErrorHandler(OnErrorHandler);

                
                _socket.Connect(_address);
                
            }
            
            return true;
        }
        private int _reconnectTimes;
        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            if (!_socket.IsConnected)
            {
                _reconnectTimes++;
                _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
                EV.PostWarningLog(Module, $"{Name} start to re-connect to {_address} for {_reconnectTimes} time");
                _socket.Connect(_address);
                
                if (!_socket.IsConnected)                    
                {
                    EV.PostWarningLog(Module, $"{Name} failed to re-connect to {_address} on {_reconnectTimes} time");

                    if (_reconnectTimes >10)
                    {
                        return true;
                    }
                    Thread.Sleep(200);
                    return false;              
                }
                else
                {
                    EV.PostInfoLog(Module, $"{Name} success to connect to {_address}");
                    return true;
                }
                    

            }
            return true;

        }
        protected override bool fStartReadCarrierID(object[] param)
        {
            try
            {
                _retryTime = 0;
                _startTime = DateTime.Now;
                int offset = 0;
                int length = 16;
                if (param != null)
                {
                    offset = (int)param[0];
                    length = (int)param[1];
                }

                CarrierIDStartPage = offset + 1;
                CarrierIDPageLength = length;
                EV.PostInfoLog(Module, $"{Name} Start to read RFID from startpage:{CarrierIDStartPage}.with length:{CarrierIDPageLength}");

                _sendMsg = string.Format("{0}{1}\r", "0100", GetPage(offset + 1, length));
                return _socket.Write(_sendMsg);
                
            }
            catch(Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        protected override bool fStartWriteCarrierID(object[] param)
        {

            try
            {
                _startTime = DateTime.Now;
                _retryTime = 0;
                CarrierIDToBeWriten = param[0].ToString();
                int offset = 0;
                int length = 16;
                
                if (param.Length == 3)
                {
                    offset = (int)param[1];
                    length = (int)param[2];
                }
                CarrierIDStartPage = offset + 1;
                CarrierIDPageLength = length;
                EV.PostInfoLog(Module, $"{Name} start to write RFID:{CarrierIDToBeWriten} from startpage:{CarrierIDStartPage}.with length:{CarrierIDPageLength}");
                _sendMsg = string.Format("{0}{1}{2}\r", "0200", GetPage(offset+1, length), ASCII2HEX(CarrierIDToBeWriten,length));
                return _socket.Write(_sendMsg);
            }
            catch(Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        protected override bool fMonitorReadCarrierID(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _startTime > TimeSpan.FromSeconds(ActionTimeLimit))
            {
                if (_retryTime < RetryTime)
                {
                    EV.PostInfoLog(Module, $"{Name} retry {_retryTime + 1} time to read RFID due to no return message");
                    _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");                    
                    _socket.Connect(_address);
                    Thread.Sleep(500);
                    _socket.Write(_sendMsg);
                    _retryTime++;
                    _startTime = DateTime.Now;
                    return false;
                }
                OnCarrierIDReadFailed("Timeout");
                return true;
            }
            return base.fMonitorReadCarrierID(param);
        }
        protected override bool fMonitorWriteCarrierID(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _startTime > TimeSpan.FromSeconds(ActionTimeLimit))
            {                
                if(_retryTime < RetryTime)
                {
                    EV.PostInfoLog(Module, $"{Name} Retry {_retryTime+1} time to write RFID:{CarrierIDToBeWriten} due to no return message");
                    _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
                    _socket.Connect(_address);
                    Thread.Sleep(500);
                    _socket.Write(_sendMsg);
                    _retryTime++;
                    _startTime = DateTime.Now;
                    return false;
                }
                OnCarrierIDWriteFailed("Timeout");
                return true;
            }
            return base.fMonitorReadCarrierID(param);
        }
        private string _sendMsg;
        public string Address => _address;
        private string _address;
        private AsyncSocket _socket;
        public bool IsConnected =>_socket.IsConnected;

        public int Offset { get; private set; }
        public int Length { get; private set; }
        public bool Connect()
        {
            _socket.Connect(_address);
            return true;
        }

        public bool Disconnect()
        {
            _socket.Dispose();
            return true;
        }
        private string getErrMsg(string error)
        {
            string msg = "";
            switch (error)
            {
                case "14":
                    msg = "Format error There is a mistake in the command format";
                    break;
                case "70":
                    msg = "Communications error Noise or another hindrance occurs during communications with an ID Tag, and communications cannot be completed normally.";
                    break;
                case "71":
                    msg = "Verification error Correct data cannot be written to an ID Tag";
                    break;
                case "72":
                    msg = "No Tag error Either there is no ID Tag in front of the CIDRW Head, or the CIDRW Head is unable to detect the ID Tag due to environmental factors";
                    break;
                case "7B":
                    msg = "Outside write area error A write operation was not completed normally because the ID Tag was in an area in which the ID Tag could be read but not written";
                    break;
                case "7E":
                    msg = "ID system error (1) The ID Tag is in a status where it cannot execute command processing";
                    break;
                case "7F":
                    msg = "ID system error (2) An inapplicable ID Tag has been used";
                    break;
                case "9A":
                    msg = "Hardware error in CPU An error occurred when writing to EEPROM.";
                    break;
                default:
                    msg = "Unknow error.";
                    break;
            }
            return msg;
        }
        public string HEX2ASCII(string hex)
        {
            string res = String.Empty;

            try
            {
                for (int a = 0; a < hex.Length; a = a + 2)
                {
                    string Char2Convert = hex.Substring(a, 2);

                    int n = Convert.ToInt32(Char2Convert, 16);

                    char c = (char)n;

                    res += c.ToString();

                }

            }
            catch (Exception e)
            {
                LOG.Write(e);
            }
            return res;
        }
        private string GetPage(int startpage, int length)
        {

            double dpage = 0;
            for (int i = 0; i < length; i++)
            {
                dpage = dpage + Math.Pow(2, startpage + 1 + i);
            }
            string pageret = String.Format("{0:X}", Convert.ToInt32(dpage));

            for (int j = pageret.Length; j < 8; j++)
            {
                pageret = "0" + pageret;
            }
            return pageret;
        }
        private string ASCII2HEX(string src,int length)
        {
            while (src.Length < length * 8)
            {
                src = src + '\0';
            }

            if (src.Length > length * 8)
            {
                src = src.Substring(0, length * 8);
                LOG.Write($"RFID support max {(length * 8).ToString()} characters");
            }
            string res = String.Empty;
            try
            {

                char[] charValues = src.ToCharArray();
                string hexOutput = "";
                foreach (char _eachChar in charValues)
                {
                    // Get the integral value of the character.
                    int value = Convert.ToInt32(_eachChar);
                    // Convert the decimal value to a hexadecimal value in string form.
                    hexOutput += String.Format("{0:X2}", value);
                    // to make output as your eg 
                    //  hexOutput +=" "+ String.Format("{0:X}", value);

                }

                return hexOutput;
            }
            catch (Exception e)
            {
                LOG.Write(e);
            }

            return res;

        }

        public override void Terminate()
        {

            _socket.Dispose();
        }

    }
}
