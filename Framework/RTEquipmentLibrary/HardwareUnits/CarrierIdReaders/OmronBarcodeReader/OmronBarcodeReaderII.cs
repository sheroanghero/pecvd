using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.CarrierIDReaderBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.OmronV640
{
    public class OmronBarcodeReaderII : CIDReaderBaseDevice, IConnection
    {
        private string _scRoot;
        private string _portName;
        private bool _enableLog;
        private int _baudRate;
        private int _dataBits;
        private Parity _parity;
        private int _stopBits;
        private AsyncSerialPort _port;
        private object _locker = new object();
        DateTime _scanStartTimer;
        public OmronBarcodeReaderII(string module, string name, string scRoot, LoadPortBaseDevice lp = null) : base(module, name, lp)
        {
            _scRoot = scRoot;
            _portName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            _baudRate = SC.GetValue<int>($"{_scRoot}.{Name}.BaudRate");
            _dataBits = SC.GetValue<int>($"{_scRoot}.{Name}.DataBits");
            _parity = (Parity)Enum.Parse(typeof(Parity), SC.GetStringValue($"{_scRoot}.{Name}.Parity"));
            _stopBits = SC.GetValue<int>($"{_scRoot}.{Name}.StopBits");
            _port = new AsyncSerialPort(_portName, _baudRate, 8);//, Parity.Even, StopBits.One, "\r", true);
            _port.OnDataChanged += _port_OnDataChanged;
            _port.OnErrorHappened += _port_OnErrorHappened;
            int count = SC.ContainsItem("System.ComPortRetryCount") ? SC.GetValue<int>("System.ComPortRetryCount") : 5;
            int sleep = SC.ContainsItem("System.ComPortRetryDelayTime") ? SC.GetValue<int>("System.ComPortRetryDelayTime") : 2;
            if (sleep <= 0 || sleep > 10)
                sleep = 2;

            int retry = 0;
            do
            {
                if (_port.Open())
                {
                    EV.PostInfoLog(Module, $"Connected with {Module}.{Name} .");
                    break;
                }
                else
                {
                    EV.PostInfoLog(Module, $"Can't connected with {Module}.{Name},retry time {retry}.");
                    _port.Close();
                    Thread.Sleep(sleep * 1000);
                }
                if (count > 0 && retry++ > count)
                {
                    EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
                    break;
                }
            } while (true);
        }

        private void _port_OnErrorHappened(string obj)
        {
            OnError();
        }

        private void _port_OnDataChanged(string package)
        {
            try
            {
                lock (_locker)
                {
                    if (DeviceState == CIDReaderStateEnum.ReadCarrierID)
                    {
                        CarrierIDBeRead = package.Replace("\r", "");
                        OnCarrierIDRead(CarrierIDBeRead);
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

            }
        }

        public string Address { get; set; }

        public bool IsConnected => _port.IsOpen();

        public bool Connect()
        {
            return _port.Open();
        }


        public bool Disconnect()
        {
            return _port.Close();
        }

        protected override bool fStartWriteCarrierID(object[] param)
        {
            return false;
        }
        protected override bool fStartReadCarrierID(object[] param)
        {
            try
            {
                lock (_locker)
                {
                    if (!_port.IsOpen())
                    {
                        EV.PostWarningLog(Module, _portName + " not open, can not scan");
                        return false;
                    }
                    _port.Write(new byte[] { 0x1B, 0x5A, 0x0D });
                    _scanStartTimer = DateTime.Now;
                    return true;
                }
            }

            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        protected override bool fMonitorReadCarrierID(object[] param)
        {
            if(DateTime.Now - _scanStartTimer > TimeSpan.FromSeconds(60))
            {
                EV.PostWarningLog(Module, _portName + " error, timeout");
                OnCarrierIDReadFailed("Timeout");
                return true;
            }
            return false;
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
        private string ASCII2HEX(string src, int length)
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
    }
}
