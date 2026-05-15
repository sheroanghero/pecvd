using Aitex.Core.Util;
using MECF.Framework.Common.Utilities;
using MECF.Framework.Simulator.Core.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace MECF.Framework.Simulator.Core.Commons
{
    public class SerialPortDeviceSimulatorFactory
    { 
        public static CommonSerialPortDeviceSimulator GetCommonSerialPortDeviceSimulator(string port, string deviceName)
        {
            if(deviceName == "SiasunPhoenixB")
            {
                return new SiasunPhoenixBSimulator(port, deviceName);
            }
            else if (deviceName == "Siasun1500C800C")
            {
                return new Siasun1500C800CSimulator(port, deviceName);
            }
            else if (deviceName == "BrooksVCE")
            {
                return new BrooksVCESimulator(port, deviceName);
            }
            else if (deviceName == "Hanbell")
            {
                return new HanbellPumpSimulator(port, deviceName);
            }
            else if (deviceName == "HirataR4")
            {
                return new HirataR4Simulator(port, deviceName);
            }
            else if (deviceName == "BrooksSMIF")
            {
                return new BrooksSMIFSimulator(port, deviceName);
            }
            else if (deviceName == "SiasunAligner")
            {
                return new SiasunAlignerSimulator(port, deviceName);
            }
            else if (deviceName == "RisshiChiller")
            {
                return new RisshiChillerSimulator(port, deviceName);
            }
            else if (deviceName == "BaecChiller")
            {
                return new BaecChillerSimulator(port, deviceName);
            }
            else if (deviceName == "SiasunVCE")
            {
                return new SiasunVCESimulator(port, deviceName);
            }
            else if (deviceName == "TazmoRobot")
            {
                return new TazmoRobotSimulator(port, deviceName);
            }
            else if (deviceName == "VATS651")
            {
                return new VATS651Simulator(port, deviceName);
            }
            else if (deviceName == "TruPlasmaRF1001")
            {
                return new TruPlasmaRF1001Simulator(port, deviceName);
            }
            else if (deviceName == "AeRfPower")
            {
                return new SimAeRfPower(port, deviceName);
            }
            else if (deviceName == "LowPower")
            {
                return new SimLowFrequencyRF(port, deviceName);
            }
            else if (deviceName == "HighPower")
            {
                return new SimHighFrequencyRF(port, deviceName);
            }
            else if (deviceName == "BrooksAligner")
            {
                return new BrooksAlignerSimulator(port, deviceName);
            }
            else if (deviceName == "SkyPump")
            {
                return new SkyPumpSimulator(port, deviceName);
            }
            else if (deviceName == "CommetRFMatch")
            {
                return new CommetRFMatchSimulator(port, deviceName);
            }
            else if (deviceName == "FujikinMFC")
            {
                return new FujikinMFCSimulator(port, deviceName);
            }
            else if (deviceName == "PfeifferPumpA603")
            {
                return new PfeifferPumpA603Simulator(port, deviceName); 
            }
            else if (deviceName == "PfeifferPumpA100")
            {
                return new PfeifferPumpA100Simulator(port, deviceName);
            }
            else if (deviceName == "EdwardsPump") 
            {
                return new EdwardsPumpSimulator(port, deviceName);
            }
            else if (deviceName == "KaimeiRFMatch")
            {
                return new KaimeiRFMatchSimulator(port, deviceName);
            }
            else if (deviceName == "EdwardsPump2205")
            {
                return new EdwardsPump2205Simulator(port, deviceName);
            }
            else if (deviceName == "FY8300")
            {
                return new FY8300Simulator(port, deviceName);
            }
            return null;
        }
    }



    public class CommonSerialPortDeviceSimulator : SerialPortDeviceSimulator
    {
        public bool Failed { get; set; }
        public bool AutoReply { get; set; } = true;

        public bool IsAtSpeed { get; set; }

        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;

        private object _locker = new object();

        public string ResultValue { get; set; }

        public List<IOSimulatorItemViewModel> IOSimulatorItemList { get; set; }

        public event Action<IOSimulatorItemViewModel> SimulatorItemActived;

        string _deviceName;
        public CommonSerialPortDeviceSimulator(string port, string deviceName, bool isAscii = false, string newLine = "\r")
            : base(port, -1, newLine, ' ', isAscii)
        {
            _deviceName = deviceName;
            ResultValue = "";

            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();

            IsAtSpeed = true;
        }

        public CommonSerialPortDeviceSimulator(string port, string deviceName, bool isAscii, string newLine, int dataBits)
            : base(port, -1, newLine, ' ', isAscii, dataBits)
        {
            _deviceName = deviceName;
            ResultValue = "";

            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();

            IsAtSpeed = true;
        }

        private void _tick_Elapsed(object sender, ElapsedEventArgs e)
        {

            lock (_locker)
            {
                if (_timer.IsRunning && _timer.Elapsed > TimeSpan.FromSeconds(10))
                {
                    _timer.Stop();

                    IsAtSpeed = true;
                }
            }
        }

        //private string GetCharsFormFromBinaryString(String binary)
        //{
        //    return Encoding.ASCII.GetString(GetBytesFromBinaryString(binary));
        //}

        private string GetCharsFormFromBinaryString(byte[] binary)
        {
            return Encoding.ASCII.GetString(binary);
        }

        protected override void ProcessUnsplitMessage(byte[] binaryMessage)
        {
            lock (_locker)
            {
                var activeSimulatorItem = GetActiveIOSimulatorItemViewModel(binaryMessage);
                if (activeSimulatorItem == null) return;

                activeSimulatorItem.CommandContent = string.Join("," , binaryMessage.Select(bt=>bt.ToString("X2")).ToArray());
                activeSimulatorItem.CommandRecievedTime = DateTime.Now;

                if (SimulatorItemActived != null)
                    SimulatorItemActived(activeSimulatorItem);

                if (AutoReply)
                {
                    OnWriteSimulatorItem(activeSimulatorItem);
                } 
            }
        }

        protected override void ProcessUnsplitMessage(string msg)
        {
            lock (_locker)
            {
                if (msg.Contains("O"))
                    Console.WriteLine("");
                var activeSimulatorItem = GetActiveIOSimulatorItemViewModel(msg);
                if (activeSimulatorItem == null) return;

                activeSimulatorItem.CommandContent = msg;
                activeSimulatorItem.CommandRecievedTime = DateTime.Now;

                if (SimulatorItemActived != null)
                    SimulatorItemActived(activeSimulatorItem);

                if(AutoReply)
                {
                    OnWriteSimulatorItem(activeSimulatorItem);
                }
            }
        }

        protected virtual IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            return null;
        }
        protected virtual IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            return null;
        }
        protected virtual void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
        }


        public void ManualWriteMessage(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteSimulatorItem(activeSimulatorItem);
        }

    }


    public class SiasunPhoenixBSimulator : CommonSerialPortDeviceSimulator
    {
        public SiasunPhoenixBSimulator(string port, string deviceName):base(port, deviceName)
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommandName))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if(activeSimulatorItem.SourceCommandName.StartsWith("RQ"))
            {
                OnWriteMessage(activeSimulatorItem.Response + "\r");
                Thread.Sleep(1000);
                OnWriteMessage("_RDY" + "\r");
            }
            else
            {
                OnWriteMessage(activeSimulatorItem.Response + "\r");
            }
        }
    }

    public class Siasun1500C800CSimulator : CommonSerialPortDeviceSimulator
    {
        public Siasun1500C800CSimulator(string port, string deviceName) : base(port, deviceName)
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommandName))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if (activeSimulatorItem.SourceCommandName.StartsWith("RQ"))
            {
                OnWriteMessage(activeSimulatorItem.Response + "\r");
                Thread.Sleep(1000);
                OnWriteMessage("_RDY" + "\r");
            }
            else
            {
                OnWriteMessage(activeSimulatorItem.Response + "\r");
            }
        }
    }
    public class BaecChillerSimulator : CommonSerialPortDeviceSimulator
    {
        //private byte _address = 0x01;
        private string _load = "01,F4";
        private string _tune = "01,F4";
        private string _mode = "01,00";
        private string _ch1On = "00,00";
        private string _ch2On = "00,00";
        private string _ch1Temp = "00,C8";
        private string _ch2Temp = "00,C8";
        private string _ch1Flow = "00,B9";
        private string _ch2Flow = "00,B9";
        private List<byte> _msgBuffer = new List<byte>();

        public BaecChillerSimulator(string port, string deviceName) : base(port, deviceName, false, "", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            _msgBuffer.AddRange(msg);
            if (_msgBuffer.Count < 8)
            {
                return null;
            }
            if (_msgBuffer[1] == 0x03 || _msgBuffer[1] == 0x06)
            {
                if (_msgBuffer.Count >= 8)
                {
                    var msgArray = _msgBuffer.Select(bt => bt.ToString("X2")).ToArray();
                    string msgCommand = string.Join(",", msgArray, 0, 4);
                    _msgBuffer.RemoveRange(0, msgArray.Length);
                    foreach (var simulatorItem in IOSimulatorItemList)
                    {
                        if (msgCommand == simulatorItem.SourceCommand)
                        {
                            return simulatorItem;
                        }
                    }
                }
            }
            if (_msgBuffer[1] == 0x10)
            {
                var length = _msgBuffer[6];
                if (_msgBuffer.Count >= 7 + length + 2)
                {
                    var msgArray = _msgBuffer.Select(bt => bt.ToString("X2")).ToArray();
                    string msgCommand = string.Join(",", msgArray, 0, 4);
                    _msgBuffer.RemoveRange(0, msgArray.Length);
                    foreach (var simulatorItem in IOSimulatorItemList)
                    {
                        if (msgCommand == simulatorItem.SourceCommand)
                        {
                            return simulatorItem;
                        }
                    }
                }
            }

            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if (activeSimulatorItem.SourceCommandName == "SetCH1OnOff")
            {
                var contentArray = activeSimulatorItem.CommandContent.Split(',');
                if (contentArray.Length >= 8)
                {
                    var onArray = contentArray.Skip(4).Take(2).ToArray();
                    _ch1On = $"{string.Join(",", onArray)}";
                }
            }
            if (activeSimulatorItem.SourceCommandName == "SetCH2OnOff")
            {
                var contentArray = activeSimulatorItem.CommandContent.Split(',');
                if (contentArray.Length >= 8)
                {
                    var onArray = contentArray.Skip(4).Take(2).ToArray();
                    _ch2On = $"{string.Join(",", onArray)}";
                }
            }
            if (activeSimulatorItem.SourceCommandName == "SetCH1Temperature")
            {
                var contentArray = activeSimulatorItem.CommandContent.Split(',');
                if (contentArray.Length >= 8)
                {
                    var tempArray = contentArray.Skip(4).Take(2).ToArray();
                    _ch1Temp = $"{string.Join(",", tempArray)}";
                }
            }
            if (activeSimulatorItem.SourceCommandName == "SetCH2Temperature")
            {
                var contentArray = activeSimulatorItem.CommandContent.Split(',');
                if (contentArray.Length >= 8)
                {
                    var tempArray = contentArray.Skip(4).Take(2).ToArray();
                    _ch2Temp = $"{string.Join(",", tempArray)}";
                }
            }
            if (activeSimulatorItem.SourceCommandName == "SetPresetAbsoluteMode")
            {
                var contentArray = activeSimulatorItem.CommandContent.Split(',');
                if (contentArray.Length >= 15)
                {
                    var modeArray = contentArray.Skip(7).Take(2).ToArray();
                    var tuneArray = contentArray.Skip(9).Take(2).ToArray();
                    var loadArray = contentArray.Skip(11).Take(2);
                    _tune = $"0{string.Join(",", tuneArray).Substring(1)}";
                    _load = $"0{string.Join(",", loadArray).Substring(1)}";
                    _mode = string.Join(",", modeArray.Reverse());
                }
            }

            if (activeSimulatorItem.SourceCommandName == "GetStatus")
            {
                OnWriteMessage(BuildMessage($"{activeSimulatorItem.Response},{_ch1Temp},{_ch2Temp},00,00," +
                    $"{(_ch1On == "00,00" ? "00,00" : _ch1Flow)},{(_ch1On == "00,00" ? "00,00" : _ch2Flow)},00,00,00,00,00,00,00,00,00,00,00,00,00,00,{_ch1On},{_ch2On}"));
            }
            else
            {
                OnWriteMessage(BuildMessage(activeSimulatorItem.Response));
            }
        }

        private byte[] BuildMessage(string reponse)
        {
            List<byte> buffer = new List<byte>();

            buffer.AddRange(reponse.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());

            var checkSum = Crc16.CRC16_ModbusRTU(buffer.ToArray());
            var ret = BitConverter.GetBytes(checkSum);
            buffer.AddRange(ret);

            return buffer.ToArray();
        }
    }
    public class RisshiChillerSimulator : CommonSerialPortDeviceSimulator
    {
        private static char _startLine = (char)1;
        private static char _endLine = (char)3;
        public RisshiChillerSimulator(string port, string deviceName) : base(port, deviceName, true, _endLine.ToString())
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommand))
                {
                    return simulatorItem;
                }
            }
            return null;
        }


        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteMessage(BuildMessage(activeSimulatorItem.Response));
        }

        private string BuildMessage(string response)
        {
            string sLength = response.Length.ToString("D2");
            var charArray = (sLength + response).ToArray();
            int sum = charArray.Sum(chr => (int)chr);
            string sSum = sum.ToString("X");

            if (sSum.Length < 2)
                return _startLine + sLength + response + "0" + sSum + _endLine.ToString();
            else
                return _startLine + sLength + response + sSum.Substring(sSum.Length - 2) + _endLine.ToString();
        }
    }
    public class SiasunAlignerSimulator : CommonSerialPortDeviceSimulator
    {
        //private static char _startLine = (char)1;
        private static string _endline = "\r";

        public SiasunAlignerSimulator(string port, string deviceName) : base(port, deviceName, true, _endline)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommand))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if (activeSimulatorItem.SourceCommandName == "Align" || activeSimulatorItem.SourceCommandName == "Rotate")
                Thread.Sleep(3000);
            OnWriteMessage(activeSimulatorItem.Response + _endline);
        }
    }


    public class FY8300Simulator : CommonSerialPortDeviceSimulator
    {
        private static string _endline = "\n";

        Dictionary<string, float> values = new Dictionary<string, float>();
        Dictionary<string, string> cmds = new Dictionary<string, string>();
        Dictionary<string, string> outcmds = new Dictionary<string, string>();

        public FY8300Simulator(string port, string deviceName) : base(port, deviceName, true, _endline)
        {
            values.Add("CH1WAVEFORM", 0);
            values.Add("CH1FREQUENCY", 0);
            values.Add("CH1RANGE", 0);
            values.Add("CH1OUTPUT", 0);
            values.Add("CH2WAVEFORM", 0);
            values.Add("CH2FREQUENCY", 0);
            values.Add("CH2RANGE", 0);
            values.Add("CH2OUTPUT", 0);
            values.Add("CH3WAVEFORM", 0);
            values.Add("CH3FREQUENCY", 0);
            values.Add("CH3RANGE", 0);
            values.Add("CH3OUTPUT", 0);


            cmds.Add("WMW", "CH1WAVEFORM");
            cmds.Add("WMF", "CH1FREQUENCY");
            cmds.Add("WMA", "CH1RANGE");
            cmds.Add("WMN", "CH1OUTPUT");
            cmds.Add("WFW", "CH2WAVEFORM");
            cmds.Add("WFF", "CH2FREQUENCY");
            cmds.Add("WFA", "CH2RANGE");
            cmds.Add("WFN", "CH2OUTPUT");
            cmds.Add("TFW", "CH3WAVEFORM");
            cmds.Add("TFF", "CH3FREQUENCY");
            cmds.Add("TFA", "CH3RANGE");
            cmds.Add("TFN", "CH3OUTPUT");

            outcmds.Add("RMW", "CH1WAVEFORM");
            outcmds.Add("RMF", "CH1FREQUENCY");
            outcmds.Add("RMA", "CH1RANGE");
            outcmds.Add("RMN", "CH1OUTPUT");
            outcmds.Add("RFW", "CH2WAVEFORM");
            outcmds.Add("RFF", "CH2FREQUENCY");
            outcmds.Add("RFA", "CH2RANGE");
            outcmds.Add("RFN", "CH2OUTPUT");
            outcmds.Add("RTW", "CH3WAVEFORM");
            outcmds.Add("RTF", "CH3FREQUENCY");
            outcmds.Add("RTA", "CH3RANGE");
            outcmds.Add("RTN", "CH3OUTPUT");
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            string cmd = msg.Substring(0, 3);
            float value = Convert.ToSingle(cmd.Substring(3));
            if(cmds.ContainsKey(cmd))
            {
                values[cmds[cmd]] = value;
                OnWriteMessage(BuildMessage(""));
            }
            else if(outcmds.ContainsKey(cmd))
            {
                OnWriteMessage(BuildMessage(outcmds[cmd]+ values[cmd]));
            }
            return null;
        }

        private string BuildMessage(string value)
        {
            return value + "\n";
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            //OnWriteMessage(activeSimulatorItem.Response + _endline);
        }
    }


    public class TruPlasmaRFResponse
    {
        public string GS { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Data { get; set; }
    }

    public class SimAeRfPower : CommonSerialPortDeviceSimulator
    {
        public bool IsOn { get; set; }

        public bool IsHalo { get; set; }

        public bool IsContinueAck { get; set; }

        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;
        Random _rd = new Random();

        private object _locker = new object();

        public SimAeRfPower(string port, string deviceName)
            : base(port, deviceName, false, "", 8)
        {

            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();

        }

        private void _tick_Elapsed(object sender, ElapsedEventArgs e)
        {

            lock (_locker)
            {
                if (_timer.IsRunning && _timer.Elapsed > TimeSpan.FromSeconds(10))
                {
                    _timer.Stop();
                }
            }

        }

        List<byte> _cached = new List<byte>();

        private int _regulationMode;
        private int _commMode;
        private int _powerSetPoint;
        private int _pulseType;
        public int _pulseFrequency;
        public int _pulseReverseTime;

        protected override void ProcessUnsplitMessage(byte[] message1)
        {
            _cached.AddRange(message1);

            if (_cached[0] == 0x06)
                _cached.RemoveAt(0);

            if (_cached.Count < 3)
                return;

            byte[] msgIn = _cached.ToArray();

            _cached.Clear();

            List<byte> lstAck = new List<byte>();
            lstAck.Add(0x06);

            byte[] response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { 0 });

            switch (msgIn[1])
            {
                case 1:
                    IsOn = false;
                    break;
                case 2:
                    IsOn = true;
                    break;
                case 3: //regulation mode
                    _regulationMode = msgIn[2];
                    break;
                case 6: //set power
                    _powerSetPoint = msgIn[2] + (msgIn[3] << 8);
                    break;
                case 14: //communication mode
                    _commMode = msgIn[2];
                    break;
                case 65: //set pulse type
                    _pulseType = msgIn[2];
                    break;
                case 92: //set pulse frequency index
                    _pulseFrequency = msgIn[2];
                    break;
                case 93: //set pulse reverse time
                    _pulseReverseTime = msgIn[2];
                    break;
                case 146: //query pulse frequency index
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)_pulseFrequency });
                    break;
                case 147: //query pulse reverse time
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)_pulseReverseTime });
                    break;
                case 154://regulation mode 
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)_regulationMode });
                    break;
                case 155://communication mode 
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)_commMode });
                    break;
                case 162://status 
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { IsOn ? (byte)0x89 : (byte)81, 0, 0, 0 });
                    break;
                case 164: //setpoint
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)_powerSetPoint, (byte)(_powerSetPoint >> 8), (byte)_regulationMode });
                    break;
                case 165: //forward
                    int forward = _powerSetPoint;
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)forward, (byte)(forward >> 8) });
                    break;
                case 168: //forward power, voltage, current
                    int forwardPower = _powerSetPoint;
                    int voltage = (int)(_powerSetPoint * 0.4);
                    int current = (int)(_powerSetPoint * 20);
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], new byte[] { (byte)forwardPower, (byte)(forwardPower >> 8), (byte)voltage, (byte)(voltage >> 8), (byte)current, (byte)(current >> 8) });
                    break;
                case 221://pin
                    List<byte> pin = new List<byte>();
                    for (int i = 0; i < 32; i++)
                    {
                        if (i == 20 && IsHalo)
                        {
                            pin.Add(0x31);
                            continue;
                        }
                        pin.Add(0);
                    }
                    response = BuildMessage((byte)(msgIn[0] >> 3), msgIn[1], pin.ToArray());
                    break;
            }

            if (!IsContinueAck)
            {
                OnWriteMessage(lstAck.ToArray());
                OnWriteMessage(response);
            }
            else
            {
                lstAck.AddRange(response);
                OnWriteMessage(lstAck.ToArray());
            }


        }
        private static byte[] BuildMessage(byte address, byte command, byte[] data)
        {
            List<byte> buffer = new List<byte>();

            if (data.Length < 7)
            {
                buffer.Add((byte)((address << 3) + (data == null ? 0 : data.Length)));
                buffer.Add(command);
            }
            else
            {
                buffer.Add((byte)((address << 3) + 7));
                buffer.Add(command);
                buffer.Add((byte)data.Length);
            }



            if (data != null && data.Length > 0)
            {
                buffer.AddRange(data);
            }

            buffer.Add(CalcSum(buffer, buffer.Count));

            return buffer.ToArray();
        }
        private static byte CalcSum(List<byte> data, int length)
        {
            byte ret = 0x00;
            for (var i = 0; i < length; i++)
            {
                ret ^= data[i];
            }
            return ret;
        }

    }

    public class SimLowFrequencyRF : CommonSerialPortDeviceSimulator
    {
        public bool IsOn { get; set; }

        public bool IsHalo { get; set; }

        public bool IsContinueAck { get; set; }

        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;
        Random _rd = new Random();

        private object _locker = new object();

        public SimLowFrequencyRF(string port, string deviceName)
            : base(port, deviceName, false, "", 8)
        {
            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();

        }

        private void _tick_Elapsed(object sender, ElapsedEventArgs e)
        {

            lock (_locker)
            {
                if (_timer.IsRunning && _timer.Elapsed > TimeSpan.FromSeconds(10))
                {
                    _timer.Stop();
                }
            }

        }

        List<byte> _cached = new List<byte>();

        private int _mode;
        private int _powerSetPoint;

        protected override void ProcessUnsplitMessage(byte[] message1)
        {
            _cached.AddRange(message1);

            //if (_cached[0] == 0x06)
            //    _cached.RemoveAt(0);

            if (_cached.Count < 7)
                return;

            byte[] msgIn = _cached.ToArray();

            _cached.Clear();

            //List<byte> lstAck = new List<byte>();
            //lstAck.Add(0x06);

            byte[] response = new byte[15];

            if (msgIn[2] == (byte)'0' && msgIn[3] == (byte)'0')     // 00, Control Mode
            {
                if (msgIn[4] == (byte)'0' && msgIn[5] == (byte)'0') // Setting the Control Mode
                {
                    if (msgIn[6] == (byte)'0' && msgIn[7] == (byte)'1') // 01:Manual
                    {
                        _mode = 1;

                        response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], msgIn[4], msgIn[5], BuildPara('0', '1'), msgIn[msgIn.Length - 1]);
                    }
                    else if (msgIn[6] == (byte)'0' && msgIn[7] == (byte)'2') // 02:RS232C
                    {
                        _mode = 2;

                        response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], msgIn[4], msgIn[5], BuildPara('0', '2'), msgIn[msgIn.Length - 1]);
                    }
                }
                else if (msgIn[4] == (byte)'0' && msgIn[5] == (byte)'1') // Get Control Mode
                {
                    response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], msgIn[4], msgIn[5], _mode == 1 ? BuildPara('0', '1') : BuildPara('0', '2'), msgIn[msgIn.Length - 1]);
                }
            }
            else if (msgIn[2] == (byte)'0' && msgIn[3] == (byte)'2') // 02, SetPoint
            {
                if (msgIn[4] == (byte)'0' && msgIn[5] == (byte)'0') // RF output setpoint set
                {
                    _powerSetPoint = GetData(msgIn[6], msgIn[7], msgIn[8], msgIn[9]);

                    response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], msgIn[4], msgIn[5], BuildData(_powerSetPoint), msgIn[msgIn.Length - 1]);
                }
                else if (msgIn[4] == (byte)'0' && msgIn[5] == (byte)'1') // Get RF output setpoint
                {
                    response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], msgIn[4], msgIn[5], BuildData(_powerSetPoint), msgIn[msgIn.Length - 1]);
                }
            }
            else if (msgIn[2] == (byte)'0' && msgIn[3] == (byte)'3') // 03, RF ON/OFF
            {
                if (msgIn[4] == (byte)'0' && msgIn[5] == (byte)'0') // ON and OFF settings
                {
                    if (msgIn[6] == (byte)'A' && msgIn[7] == (byte)'A') // ON:AA
                    {
                        IsOn = true;

                        response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], msgIn[4], msgIn[5], BuildPara('A', 'A'), msgIn[msgIn.Length - 1]);
                    }
                    else if (msgIn[6] == (byte)'0' && msgIn[7] == (byte)'0') // OFF:OO
                    {
                        IsOn = false;

                        response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], msgIn[4], msgIn[5], BuildPara('0', '0'), msgIn[msgIn.Length - 1]);
                    }
                }
                else if (msgIn[4] == (byte)'0' && msgIn[5] == (byte)'1') // Get RF ON/OFF settings
                {
                    response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], msgIn[4], msgIn[5], IsOn ? BuildPara('A', 'A') : BuildPara('0', '0'), msgIn[msgIn.Length - 1]);
                }
            }
            else if (msgIn[2] == (byte)'8' && msgIn[3] == (byte)'0') // 90, Actual Forward and Reflected Poewr
            {
                if (msgIn[4] == (byte)'0' && msgIn[5] == (byte)'0')
                {

                }
                else if (msgIn[4] == (byte)'0' && msgIn[5] == (byte)'1')
                {
                    byte[] result = new byte[8] { (byte)'0', (byte)'0', (byte)'0', (byte)'0', (byte)'0', (byte)'0', (byte)'0', (byte)'0' };

                    response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], msgIn[4], msgIn[5], result, msgIn[msgIn.Length - 1]);
                }
            }
            else if (msgIn[2] == (byte)'9' && msgIn[3] == (byte)'0') // 90, Actual Forward and Reflected Poewr
            {
                int forward = _powerSetPoint;
                int reflect = (int)(_powerSetPoint * 0.2);

                byte[] fwdPower = BuildData(forward);
                byte[] RefPower = BuildData(reflect);
                byte[] result = new byte[8] { fwdPower[0], fwdPower[1], fwdPower[2], fwdPower[3], RefPower[0], RefPower[1], RefPower[2], RefPower[3] };

                response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], msgIn[4], msgIn[5], result, msgIn[msgIn.Length - 1]);
            }

            //if (!IsContinueAck)
            //{
            //    OnWriteMessage(lstAck.ToArray());
            //    OnWriteMessage(response);
            //}
            //else
            //{
            //    lstAck.AddRange(response);
            //    OnWriteMessage(lstAck.ToArray());
            //}

            OnWriteMessage(response);


        }

        private static int GetData(byte b1, byte b2, byte b3, byte b4)
        {
            byte[] byteData = new byte[4] { b1, b2, b3, b4 };
            string str = Encoding.ASCII.GetString(byteData);

            int value = Convert.ToInt32(str, 16);
            return value;
        }

        private static byte[] BuildData(int value)
        {
            string str = value.ToString("X4");

            return new byte[4] { (byte)str[0], (byte)str[1], (byte)str[2], (byte)str[3] };
        }

        private static byte[] BuildPara(char a, char b)
        {
            return new byte[] { (byte)a, (byte)b };
        }

        private static byte[] BuildMessage(byte header, byte command, byte functionCode1, byte functionCode2, byte executionCode1, byte executionCode2, byte[] parameter, byte stopCR)
        {
            List<byte> buffer = new List<byte>();

            buffer.Add(header);
            buffer.Add(command);
            buffer.Add(functionCode1);
            buffer.Add(functionCode2);
            buffer.Add(executionCode1);
            buffer.Add(executionCode2);

            if (parameter != null && parameter.Length > 0)
            {
                buffer.AddRange(parameter);
            }
            buffer.Add(stopCR);

            return buffer.ToArray();
        }
        private static byte CalcSum(List<byte> data, int length)
        {
            byte ret = 0x00;
            for (var i = 0; i < length; i++)
            {
                ret ^= data[i];
            }
            return ret;
        }

    }

    public class SimHighFrequencyRF : CommonSerialPortDeviceSimulator
    {
        public bool IsOn { get; set; }

        public bool IsHalo { get; set; }

        public bool IsContinueAck { get; set; }

        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;
        Random _rd = new Random();

        private object _locker = new object();

        public SimHighFrequencyRF(string port, string deviceName)
            : base(port, deviceName, false, "", 8)
        {
            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();

        }

        private void _tick_Elapsed(object sender, ElapsedEventArgs e)
        {

            lock (_locker)
            {
                if (_timer.IsRunning && _timer.Elapsed > TimeSpan.FromSeconds(10))
                {
                    _timer.Stop();
                }
            }

        }

        List<byte> _cached = new List<byte>();

        private int _mode;
        private int _powerSetPoint;

        protected override void ProcessUnsplitMessage(byte[] message1)
        {
            if (message1 == null || message1.Length == 0)
                return;

            string str = Encoding.ASCII.GetString(message1);

            List<byte> response = new List<byte>();


            if (str.EndsWith("***\r"))  // Set RS232C Mode
            {
                _mode = 2;

                response.Add(0x0D);
            }
            else if (str.EndsWith(" W\r")) // Power Setpoint 
            {
                string value = str.Substring(0, str.Length - 3);
                int.TryParse(value, out _powerSetPoint);

                response.Add(0x0D);
            }
            else if (str.EndsWith("G\r")) // RF On
            {
                IsOn = true;

                response.Add(0x0D);
            }
            else if (str.EndsWith("S\r")) // RF Off
            {
                IsOn = false;

                response.Add(0x0D);
            }
            else if (str.EndsWith("Q\r")) // Query
            {
                string isOn = IsOn ? "1" : "0";
                string setPoint = _powerSetPoint.ToString("D5");
                string forward = _powerSetPoint.ToString("D5");
                string reflect = ((int)(_powerSetPoint * 0.2)).ToString("D5");

                string result = $"{_mode}0{isOn}0000 {setPoint} {forward} {reflect} 00600\r";

                byte[] buffer = Encoding.ASCII.GetBytes(result);
                response.AddRange(buffer);
            }

            //_cached.AddRange(message1);

            //if (_cached[0] == 0x06)
            //    _cached.RemoveAt(0);

            //if (_cached.Count < 5)
            //    return;

            //byte[] msgIn = _cached.ToArray();

            //_cached.Clear();

            //List<byte> lstAck = new List<byte>();
            //lstAck.Add(0x06);

            //byte[] response = new byte[6];

            //switch (msgIn[2])
            //{
            //    case 0x02:
            //        if (msgIn[3] == 0x00)
            //        {
            //            _powerSetPoint = msgIn[4];
            //            response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], new byte[] { (byte)_powerSetPoint }, msgIn[msgIn.Length - 1]);
            //        }
            //        else if (msgIn[3] == 0x01)
            //        {
            //            response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], new byte[] { (byte)_powerSetPoint }, msgIn[msgIn.Length - 1]);
            //        }

            //        break;

            //    case 0x03:
            //        if (msgIn[3] == 0x00)
            //        {
            //            if (msgIn[4] == 0x47)
            //            {
            //                IsOn = true;
            //                response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], new byte[] { IsOn ? (byte)0x47 : (byte)0x53 }, msgIn[msgIn.Length - 1]);
            //            }
            //            else if (msgIn[4] == 0x53)
            //            {
            //                IsOn = false;
            //                response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], new byte[] { IsOn ? (byte)0x47 : (byte)0x53 }, msgIn[msgIn.Length - 1]);
            //            }
            //        }
            //        else if (msgIn[3] == 0x01)
            //        {
            //            response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], new byte[] { IsOn ? (byte)0x47 : (byte)0x53 }, msgIn[msgIn.Length - 1]);

            //        }

            //        break;

            //    case 0x90: //forward  reflect
            //        int forward = (int)(_powerSetPoint * 0.8);
            //        int reflect = (int)(_powerSetPoint * 0.2);
            //        response = BuildMessage(msgIn[0], msgIn[1], msgIn[2], msgIn[3], new byte[] { (byte)forward, (byte)reflect }, msgIn[msgIn.Length - 1]);
            //        break;


            //}

            //if (!IsContinueAck)
            //{
            //    OnWriteMessage(lstAck.ToArray());
            //    OnWriteMessage(response);
            //}
            //else
            //{
            //    lstAck.AddRange(response);
            //    OnWriteMessage(lstAck.ToArray());
            //}

            OnWriteMessage(response.ToArray());


        }
        private static byte[] BuildMessage(byte header, byte command, byte functionCode, byte executionCode, byte[] parameter, byte stopCR)
        {
            List<byte> buffer = new List<byte>();

            buffer.Add(header);
            buffer.Add(command);
            buffer.Add(functionCode);
            buffer.Add(executionCode);

            if (parameter != null && parameter.Length > 0)
            {
                buffer.AddRange(parameter);
            }
            buffer.Add(stopCR);

            return buffer.ToArray();
        }
        private static byte CalcSum(List<byte> data, int length)
        {
            byte ret = 0x00;
            for (var i = 0; i < length; i++)
            {
                ret ^= data[i];
            }
            return ret;
        }

    }

    public class BrooksAlignerSimulator : CommonSerialPortDeviceSimulator
    {
        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;

        private object _locker = new object();

        public BrooksAlignerSimulator(string port, string deviceName)
            : base(port, deviceName, false, "", 8)
        {
            ResultValue = "";

            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();

        }

        private void _tick_Elapsed(object sender, ElapsedEventArgs e)
        {

            lock (_locker)
            {
                if (_timer.IsRunning && _timer.Elapsed > TimeSpan.FromSeconds(10))
                {
                    _timer.Stop();
                }
            }

        }

        protected override void ProcessUnsplitMessage(byte[] message1)
        {
            if (message1 == null || message1.Length == 0)
                return;

            string str = Encoding.ASCII.GetString(message1);

            string response = string.Empty;

            if (str == "ALGN RSLT\r")
            {
                response = "DATA 2723 2508 01104 04412 N\r" + "_RDY\r";
            }
            else
            {
                response = "_RDY\r";
            }

            OnWriteMessage(response);
        }
    }

    public class TruPlasmaRF1001Simulator : CommonSerialPortDeviceSimulator
    {
        private static byte _start = 0xAA;
        private static byte _address = 0x01;
        private static byte _stop = 0x55;
        private static byte _ack = 0x06;
        private List<byte> _forwardPower = new List<byte>() { 0x00, 0x00, 0x00, 0x00 };
        private List<byte> _reflectedPower = new List<byte>() { 0x00, 0x00, 0x00, 0x00 };
        private List<byte> _onOff = new List<byte>() { 0x00, 0x00, 0x00, 0x00 };
        private List<byte> _pulseMode = new List<byte>() { 0x00, 0x00, 0x00, 0x00 };
        private List<byte> _clockMode = new List<byte>() { 0x00, 0x00, 0x00, 0x00 };
        private List<byte> _freq = new List<byte>() { 0x00, 0x00, 0x00, 0x00 };
        private List<byte> _processStatus = new List<byte>() { 0x00, 0x00, 0x00, 0x00 };
        private List<byte> _msgBuffer = new List<byte>();
        private object _locker = new object();
        //private int _workFrequency = 13560;//13.56MHz

        public TruPlasmaRF1001Simulator(string port, string deviceName) : base(port, deviceName, false, "", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            if (msg == null || msg.Length == 0)
                return null;
            string[] msgArray;
            lock (_locker)
            {
                _msgBuffer.AddRange(msg);
                if (msg[msg.Length - 1] != _stop)
                {
                    return null;
                }
                msgArray = _msgBuffer.Select(bt => bt.ToString("X2")).ToArray();
                _msgBuffer.Clear();
            }

            string msgCommand = string.Join(",", msgArray, 4, 4);
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msgCommand == simulatorItem.SourceCommand)
                {
                    return simulatorItem;
                }
            }

            return IOSimulatorItemList.Find(item => item.SourceCommandName == "ExecuteAnyCommand");
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteMessage(new byte[] { _ack });
            Thread.Sleep(50);
            OnWriteMessage(BuildMessage(activeSimulatorItem));
        }

        private byte[] BuildMessage(IOSimulatorItemViewModel activeSimulatorItem)
        {
            TruPlasmaRFResponse rfResponse = JsonConvert.DeserializeObject<TruPlasmaRFResponse>(activeSimulatorItem.Response);
            if (activeSimulatorItem.SourceCommandName == "SetPiValue")
            {
                _forwardPower.Clear();
                _forwardPower.AddRange(activeSimulatorItem.CommandContent.Split(',').Skip(10).Take(4).Select(s => Convert.ToByte(s, 16)).ToArray());
            }
            else if (activeSimulatorItem.SourceCommandName == "SetPowerOnOff")
            {
                _onOff.Clear();
                _onOff.AddRange(activeSimulatorItem.CommandContent.Split(',').Skip(10).Take(4).Select(s => Convert.ToByte(s, 16)).ToArray());
                if (_onOff.Count == 4)
                {
                    var isOn = BitConverter.ToInt32(_onOff.ToArray(), 0) > 0;
                    _processStatus.Clear();
                    if (isOn)
                        _processStatus.AddRange(BitConverter.GetBytes((UInt32)0x00000010));//0x00000010 = Power output on
                    else
                        _processStatus.AddRange(BitConverter.GetBytes((UInt32)0x00000000));
                }
            }
            else if (activeSimulatorItem.SourceCommandName == "SetPulseMode")
            {
                _pulseMode.Clear();
                _pulseMode.AddRange(activeSimulatorItem.CommandContent.Split(',').Skip(10).Take(4).Select(s => Convert.ToByte(s, 16)).ToArray());
            }
            else if (activeSimulatorItem.SourceCommandName == "SetFrequency")
            {
                _freq.Clear();
                var tmp = activeSimulatorItem.CommandContent.Split(',').Skip(10).Take(4).Select(s => Convert.ToByte(s, 16)).ToArray();
                //int freq = BitConverter.ToInt32(tmp, 0)/1000 + _workFrequency;
                //_freq.AddRange(BitConverter.GetBytes((UInt32)freq));
                _freq.AddRange(tmp);
            }
            else if (activeSimulatorItem.SourceCommandName == "SetClockMode")
            {
                _clockMode.Clear();
                _clockMode.AddRange(activeSimulatorItem.CommandContent.Split(',').Skip(10).Take(4).Select(s => Convert.ToByte(s, 16)).ToArray());
            }

            List<byte> buffer = new List<byte>();

            buffer.Add(_start);
            buffer.Add(_address);
            int length = 5;
            if (rfResponse.Status != null)
                length++;
            if (rfResponse.Type != null)
                length++;
            if (rfResponse.Data != null)
            {
                length += rfResponse.Data.Split(',').Length;
            }

            buffer.Add((byte)length);
            buffer.Add(Convert.ToByte(rfResponse.GS, 16));

            buffer.AddRange(activeSimulatorItem.SourceCommand.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());

            if (rfResponse.Status != null)
                buffer.Add(Convert.ToByte(rfResponse.Status, 16));
            if (rfResponse.Type != null)
                buffer.Add(Convert.ToByte(rfResponse.Type, 16));
            if (activeSimulatorItem.SourceCommandName == "ReadPiValue")
            {
                buffer.AddRange(_forwardPower.ToArray());
            }
            else if (activeSimulatorItem.SourceCommandName == "ReadPrValue")
            {
                buffer.AddRange(_reflectedPower.ToArray());
            }
            else if (activeSimulatorItem.SourceCommandName == "ReadProcessStatus")
            {
                buffer.AddRange(_processStatus.ToArray());
            }
            else if (activeSimulatorItem.SourceCommandName == "GetPowerOnOff")
            {
                buffer.AddRange(_onOff.ToArray());
            }
            else if (activeSimulatorItem.SourceCommandName == "GetPulseMode")
            {
                buffer.AddRange(_pulseMode.ToArray());
            }
            else if (activeSimulatorItem.SourceCommandName == "GetFrequency")
            {
                buffer.AddRange(_freq.ToArray());
            }
            else if (activeSimulatorItem.SourceCommandName == "GetFrequencyOffset")
            {
                buffer.AddRange(_freq.ToArray());
            }
            else if (activeSimulatorItem.SourceCommandName == "GetClockMode")
            {
                buffer.AddRange(_clockMode.ToArray());
            }
            else
            {
                if (rfResponse.Data != null)
                {
                    buffer.AddRange(rfResponse.Data.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());
                }
            }

            var contentBuffer = buffer.Take(buffer.Count).ToArray();
            buffer.AddRange(BitConverter.GetBytes(Crc16.Crc16Ccitt(contentBuffer)));
            buffer.Add(_stop);

            return buffer.ToArray();
        }
    }
    public class CommetRFMatchSimulator : CommonSerialPortDeviceSimulator
    {
        private static byte _start = 0xAA;
        private static byte _address = 0x21;
        private List<byte> _cap = new List<byte>() { 0x00, 0x00 };
        private List<byte> _msgBuffer = new List<byte>();

        public CommetRFMatchSimulator(string port, string deviceName) : base(port, deviceName, false, "", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            if (msg == null || msg.Length < 1)
            {
                return null;
            }
            _msgBuffer.AddRange(msg);
            if (_msgBuffer.Count < 4)
            {
                return null;
            }

            string[] msgArray;
            byte checkSum = 0;
            for (int i = 0; i < _msgBuffer.Count - 1; i++)
            {
                checkSum += _msgBuffer[i];
            }

            if (checkSum == _msgBuffer[_msgBuffer.Count - 1])
            {
                msgArray = _msgBuffer.Select(bt => bt.ToString("X2")).ToArray();
                _msgBuffer.Clear();
            }
            else
            {
                var index = _msgBuffer.FindIndex(4, m => m == _start);
                if (index < 0)
                {
                    return null;
                }

                msgArray = _msgBuffer.Take(index).Select(bt => bt.ToString("X2")).ToArray();
                _msgBuffer.RemoveRange(0, msgArray.Length);
            }

            string msgCommand = string.Join(",", msgArray, 2, 2);
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msgCommand == simulatorItem.SourceCommand)
                {
                    return simulatorItem;
                }
            }

            var anyCmd = IOSimulatorItemList.Find(item => item.SourceCommandName == "ExecuteAnyCommand");
            anyCmd.SourceCommand = msgCommand;
            anyCmd.Response = msgCommand;
            return anyCmd;
        }

        private static int iCount = 0;

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if (activeSimulatorItem.SourceCommandName == "TriggerGotoCap")
            {
                _cap.Clear();
                _cap.AddRange(activeSimulatorItem.CommandContent.Split(',').Skip(4).Take(2).Select(s => Convert.ToByte(s, 16)).ToArray());
            }
            if (activeSimulatorItem.Response.Contains("|"))
            {
                var resArray = activeSimulatorItem.Response.Split('|');
                int length = resArray.Length;
                OnWriteMessage(BuildMessage(resArray[iCount % length]));
                iCount++;
                if (iCount > 100)
                    iCount = 0;
            }
            else
            {
                if (activeSimulatorItem.SourceCommandName == "GetActualCap")
                {
                    OnWriteMessage(BuildMessage(activeSimulatorItem.Response, _cap.ToArray()));
                }
                else
                {
                    OnWriteMessage(BuildMessage(activeSimulatorItem.Response));
                }
            }
        }

        private static byte[] BuildMessage(string reponse, byte[] para = null)
        {
            List<byte> buffer = new List<byte>();

            buffer.Add(_start);
            buffer.Add(_address);

            buffer.AddRange(reponse.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());
            if (para != null)
                buffer.AddRange(para);

            byte checkSum = 0;
            for (int i = 0; i < buffer.Count; i++)
            {
                checkSum += buffer[i];
            }
            buffer.Add(checkSum);

            return buffer.ToArray();
        }
    }

    public class KaimeiRFMatchSimulator : CommonSerialPortDeviceSimulator
    {
        //private byte _address = 0x01;
        private string _load = "01,F4";
        private string _tune = "01,F4";
        private string _mode = "01,00";
        private List<byte> _msgBuffer = new List<byte>();
        public KaimeiRFMatchSimulator(string port, string deviceName) : base(port, deviceName, false, "", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            _msgBuffer.AddRange(msg);
            if(_msgBuffer.Count < 8)
            {
                return null;
            }
            if(_msgBuffer[1] == 0x03)
            {
                if(_msgBuffer.Count >= 8)
                {
                    var msgArray = _msgBuffer.Select(bt => bt.ToString("X2")).ToArray();
                    string msgCommand = string.Join(",", msgArray, 0, 4);
                    _msgBuffer.RemoveRange(0, msgArray.Length);
                    foreach (var simulatorItem in IOSimulatorItemList)
                    {
                        if (msgCommand == simulatorItem.SourceCommand)
                        {
                            return simulatorItem;
                        }
                    }
                }
            }
            if(_msgBuffer[1] == 0x10)
            {
                var length = _msgBuffer[6];
                if (_msgBuffer.Count >= 7 + length + 2)
                {
                    var msgArray = _msgBuffer.Select(bt => bt.ToString("X2")).ToArray();
                    string msgCommand = string.Join(",", msgArray, 0, 4);
                    _msgBuffer.RemoveRange(0, msgArray.Length);
                    foreach (var simulatorItem in IOSimulatorItemList)
                    {
                        if (msgCommand == simulatorItem.SourceCommand)
                        {
                            return simulatorItem;
                        }
                    }
                }
            }

            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if(activeSimulatorItem.SourceCommandName == "SetPresetAbsoluteMode")
            {
                var contentArray = activeSimulatorItem.CommandContent.Split(',');
                if (contentArray.Length >= 15)
                {
                    var modeArray = contentArray.Skip(7).Take(2).ToArray();
                    var tuneArray = contentArray.Skip(9).Take(2).ToArray();
                     var loadArray = contentArray.Skip(11).Take(2);
                    _tune = $"0{string.Join(",", tuneArray).Substring(1)}";
                    _load = $"0{string.Join(",", loadArray).Substring(1)}";
                    _mode = string.Join(",", modeArray.Reverse());
                }
            }

            if (activeSimulatorItem.SourceCommandName == "GetStatus")
            {
                OnWriteMessage(BuildMessage($"{activeSimulatorItem.Response},{_tune},{_load},{_mode}"));
            }
            else
            {
                OnWriteMessage(BuildMessage(activeSimulatorItem.Response));
            }
        }

        private byte[] BuildMessage(string reponse)
        {
            List<byte> buffer = new List<byte>();

            buffer.AddRange(reponse.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());

            var checkSum = Crc16.CRC16_ModbusRTU(buffer.ToArray());
            var ret = BitConverter.GetBytes(checkSum);
            buffer.AddRange(ret);

            return buffer.ToArray();
        }
    }

    public class FujikinMFCSimulator : CommonSerialPortDeviceSimulator
    {
        private static byte _start = 0x02;
        private static byte _end = 0x00;
        private static byte _address = 0x00;
        private static byte _ack = 0x06;
        private static byte _nak = 0x16;
        public FujikinMFCSimulator(string port, string deviceName) : base(port, deviceName, false, "", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            var msgArray = msg.Select(bt => bt.ToString("X2")).ToArray();
            string commandType = msgArray[2];
            string msgCommand = string.Join(",", msgArray, 4, 3);
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (commandType == simulatorItem.SourceCommandType && msgCommand == simulatorItem.SourceCommand)
                {
                    return simulatorItem;
                }
            }

            return null;
        }

        
        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if(activeSimulatorItem.Response == activeSimulatorItem.SuccessResponse.ToString())
            {
                OnWriteMessage(new byte[] {0x06});
                Thread.Sleep(1000);

                var resArray = activeSimulatorItem.Response.Split('|');
                int iIndex = new Random().Next(0, resArray.Length - 1);
                string responseData = resArray[iIndex];
                if(responseData == "06" )
                {
                    OnWriteMessage(new byte[] { _ack });
                }
                else if (responseData == "16")
                {
                    OnWriteMessage(new byte[] { _nak });
                }
                else
                {
                    OnWriteMessage(BuildMessage(activeSimulatorItem, responseData));
                }
            }
            else
            {
                OnWriteMessage(new byte[] { _nak });
            }
        }

        private static byte[] BuildMessage(IOSimulatorItemViewModel activeSimulatorItem, string responseData)
        {
            string reponse = activeSimulatorItem.Response;
            List<byte> buffer = new List<byte>();
            var responseArray = responseData.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray();
            buffer.Add(_address);
            buffer.Add(_start);
            buffer.Add(Convert.ToByte(activeSimulatorItem.SourceCommandType, 16));
            buffer.Add((byte)(3+ responseArray.Length));
            buffer.AddRange(activeSimulatorItem.SourceCommand.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());
            buffer.AddRange(responseArray);
            buffer.Add(_end);

            byte checkSum = 0;
            for (int i = 1; i < buffer.Count-1; i++)
            {
                checkSum += buffer[i];
            }
            buffer.Add(checkSum);

            return buffer.ToArray();
        }
    }


    /// <summary>
    /// Dry pump
    /// </summary>
    public class PfeifferPumpA603Simulator : CommonSerialPortDeviceSimulator
    {
        private static string _start = "#";
        private static string _address = "000";
        private static string _end = "\r";
        private bool _isOn;

        public PfeifferPumpA603Simulator(string port, string deviceName) : base(port, deviceName, true, "\r", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommand))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            _address = activeSimulatorItem.CommandContent.Substring(1, 3);
            if (activeSimulatorItem.SourceCommandName == "PumpOnOff")
                _isOn = activeSimulatorItem.CommandContent.Contains("ON");
            if (activeSimulatorItem.SourceCommandName == "ReadPumpStatus")
            {
                OnWriteMessage(_start + _address + "," + (_isOn ? "1" : "0") + activeSimulatorItem.Response + _end);
            }
            else
                OnWriteMessage(_start + _address + "," + activeSimulatorItem.Response + _end);
        }
    }

    public class PfeifferPumpA100Simulator : CommonSerialPortDeviceSimulator
    {
        private static string _start = "#";
        private static string _address = "000";
        private static string _end = "\r";
        private bool _isOn;

        public PfeifferPumpA100Simulator(string port, string deviceName) : base(port, deviceName, true, "\r", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommand))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            _address = activeSimulatorItem.CommandContent.Substring(1, 3);
            if (activeSimulatorItem.SourceCommandName == "PumpOnOff")
                _isOn = activeSimulatorItem.CommandContent.Contains("ON");
            if (activeSimulatorItem.SourceCommandName == "ReadPumpStatus")
            {
                List<byte> buffer = new List<byte>();
                buffer.Add((byte)0x23);
                buffer.Add((byte)0x30);
                buffer.Add((byte)0x30);
                buffer.Add((byte)0x30);
                buffer.Add((byte)(_isOn ? 0xC0 : 0x80));
                buffer.AddRange(activeSimulatorItem.Response.Split(',').Select(para => Convert.ToByte(para, 16)).ToArray());
                buffer.Add((byte)0x0D);
                OnWriteMessage(buffer.ToArray());
                //OnWriteMessage(_start + _address + "," + (_isOn ? "1":"0") + activeSimulatorItem.Response + _end);
            }
            else
                OnWriteMessage(_start + _address + "," + activeSimulatorItem.Response + _end);
        }
    }
    public class EdwardsPumpSimulator : CommonSerialPortDeviceSimulator
    {
        private static string _end = "\r\n";
        private bool _isPumpOn;
        public EdwardsPumpSimulator(string port, string deviceName) : base(port, deviceName, true, "\r", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommand))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if (activeSimulatorItem.SourceCommandName == "SwitchPump")
            {
                if (activeSimulatorItem.CommandContent.Contains("0"))
                    _isPumpOn = false;
                else
                    _isPumpOn = true;
            }
                
            if (activeSimulatorItem.SourceCommandName == "ReadPumpStatus")
            {
                OnWriteMessage($"{(_isPumpOn?"4":"0")},{activeSimulatorItem.Response}{_end}");
            }
            else
                OnWriteMessage(activeSimulatorItem.Response + _end);
        }
    }

    public class SkyPumpSimulator : CommonSerialPortDeviceSimulator
    {
        private static string _start = "@";
        private static string _address = "00";
        private static string _end = "\0\r\n";
        public SkyPumpSimulator(string port, string deviceName) : base(port, deviceName, true, "\r", 8)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommand))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteMessage(_start + _address + " " + activeSimulatorItem.Response + _end);
        }
    }

    public class VATS651Simulator : CommonSerialPortDeviceSimulator
    {
        private static string _endLine = "\r\n";
        private string _position = "000000";
        private string _pressure = "00000000";
        private string _pressureSet = "00000000";
        private string _remoteOperation = "1";
        private string _noWarning = "0";
        private string _controlMode = "2";
        private string _sensorDelay = "0.00";
        private string _rampTime = "0.00";
        private string _rampMode = "0";
        private string _gainFactor = "1.0";
        private string _valveSpeed = "0100";
        private bool _isLearning = false;
        private DeviceTimer _learnTimer = new DeviceTimer();
        private int _learnTime = 10000;//ms
        private string _deviceMode = "POS";

        public VATS651Simulator(string port, string deviceName) : base(port, deviceName, true, _endLine.ToString())
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.IndexOf(simulatorItem.SourceCommand) == 0)
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if (activeSimulatorItem.SourceCommandName == "Learn")
            {
                _isLearning = true;
                _learnTimer.Start(0);
            }

            if (activeSimulatorItem.SourceCommandName == "OpenValve")
            {
                _position = "001000";
                _controlMode = "4";
                _isLearning = false;
                if (!_learnTimer.IsIdle())
                    _learnTimer.Stop();
            }

            if (activeSimulatorItem.SourceCommandName == "CloseValve")
            {
                _position = "000000";
                _controlMode = "3";
                _isLearning = false;
                if (!_learnTimer.IsIdle())
                    _learnTimer.Stop();
            }

            if (activeSimulatorItem.SourceCommandName == "SetPosition")
            {
                _position = activeSimulatorItem.CommandContent.Replace(activeSimulatorItem.SourceCommand, "").Replace(_endLine, "");
                _controlMode = "2";
                _isLearning = false;
                if (!_learnTimer.IsIdle())
                    _learnTimer.Stop();
            }

            if (!_learnTimer.IsIdle() && _learnTimer.GetElapseTime() > _learnTime)
            {
                _learnTimer.Stop();
                _isLearning = false;
            }

            if (activeSimulatorItem.SourceCommandName == "SetPressure")
            {
                _pressureSet = activeSimulatorItem.CommandContent.Replace(activeSimulatorItem.SourceCommand, "").Replace(_endLine, "");
                _pressure = activeSimulatorItem.CommandContent.Replace(activeSimulatorItem.SourceCommand, "").Replace(_endLine, "");
                _controlMode = "5";
            }

            if (activeSimulatorItem.SourceCommandName == "SetSensorDelay")
            {
                _sensorDelay = activeSimulatorItem.CommandContent.Replace(activeSimulatorItem.SourceCommand, "").Replace(_endLine, "");
            }

            if (activeSimulatorItem.SourceCommandName == "SetRampTime")
            {
                _rampTime = activeSimulatorItem.CommandContent.Replace(activeSimulatorItem.SourceCommand, "").Replace(_endLine, "");
            }

            if (activeSimulatorItem.SourceCommandName == "SetRampMode")
            {
                _rampMode = activeSimulatorItem.CommandContent.Replace(activeSimulatorItem.SourceCommand, "").Replace(_endLine, "");
            }

            if (activeSimulatorItem.SourceCommandName == "SetGainFactor")
            {
                _gainFactor = activeSimulatorItem.CommandContent.Replace(activeSimulatorItem.SourceCommand, "").Replace(_endLine, "");
            }

            if (activeSimulatorItem.SourceCommandName == "SetValveSpeed")
            {
                _valveSpeed = activeSimulatorItem.CommandContent.Replace(activeSimulatorItem.SourceCommand, "").Replace(_endLine, "");
            }

            if (activeSimulatorItem.SourceCommandName == "RequestPositoin")
                OnWriteMessage($"{activeSimulatorItem.Response}{_position}{_endLine}");
            else if (activeSimulatorItem.SourceCommandName == "RequestPressure")
                OnWriteMessage($"{activeSimulatorItem.Response}{_pressure}{_endLine}");
            else if (activeSimulatorItem.SourceCommandName == "RequestOpenClose")
                OnWriteMessage($"{activeSimulatorItem.Response}V1:" + (_controlMode == "4" ? "O" : _controlMode == "3" ? "C" : "N") + $"V2:-{_endLine}");
            else if (activeSimulatorItem.SourceCommandName == "RequestMode")
            {
                _deviceMode = _controlMode == "2" ? "POS" : _controlMode == "5" ? "PRESS" : _deviceMode;
                OnWriteMessage($"{activeSimulatorItem.Response}{_deviceMode}{_endLine}");
            }
            else if (activeSimulatorItem.SourceCommandName == "RequestSetPressure")
                OnWriteMessage($"{activeSimulatorItem.Response}{_pressureSet}{_endLine}");
            else if (activeSimulatorItem.SourceCommandName == "RequestAssembly")
                OnWriteMessage($"{activeSimulatorItem.Response}{_position}{_pressure}{_remoteOperation}{_controlMode}{_noWarning}{_endLine}");
            else if (activeSimulatorItem.SourceCommandName == "GetSensorDelay")
                OnWriteMessage($"{activeSimulatorItem.Response}{_sensorDelay}{_endLine}");
            else if (activeSimulatorItem.SourceCommandName == "GetRampTime")
                OnWriteMessage($"{activeSimulatorItem.Response}{_rampTime}{_endLine}");
            else if (activeSimulatorItem.SourceCommandName == "GetRampMode")
                OnWriteMessage($"{activeSimulatorItem.Response}{_rampMode}{_endLine}");
            else if (activeSimulatorItem.SourceCommandName == "GetGainFactor")
                OnWriteMessage($"{activeSimulatorItem.Response}{_gainFactor}{_endLine}");
            else if (activeSimulatorItem.SourceCommandName == "GetValveSpeed")
                OnWriteMessage($"{activeSimulatorItem.Response}{_valveSpeed}{_endLine}");
            else if (activeSimulatorItem.SourceCommandName == "QueryLearnStatus")
                OnWriteMessage($"{activeSimulatorItem.Response}{(_isLearning ? 1 : 0)}0000000{_endLine}");
            else
                OnWriteMessage(activeSimulatorItem.Response + _endLine);
        }
    }

    public class EdwardsPump2205Simulator : CommonSerialPortDeviceSimulator
    {
        private Dictionary<int, List<byte>> _flowDic = new Dictionary<int, List<byte>>();

        Stopwatch _timer = new Stopwatch();

        private System.Timers.Timer _tick;
        Random _rd = new Random();

        private object _locker = new object();

        public string ResultValue { get; set; }

        private static string _endLine = "\r\n";

        byte[] measValues = new byte[68];
        ushort measuredSpeed = 1;

        byte[] modFonct = new byte[164];
        public EdwardsPump2205Simulator(string port, string deviceName) : base(port, deviceName, false, "\r")
        {
            ResultValue = "";

            _tick = new System.Timers.Timer();
            _tick.Interval = 200;

            _tick.Elapsed += _tick_Elapsed;
            _tick.Start();
            DoReset();
        }

        private void DoReset()
        {
            //02 30 30 31 20 5B
            //37 46 37 46 37 46 37 46 37 46
            //35 45 31 34 35 42 31 37 35 35
            //36 42 35 44 36 33 31 31 31 35
            //30 30 32 30 30 30 32 32 35 46
            //30 30 30 30 30 30 30 30 30 30
            //30 30 30 30 30 41 30 30 30 41
            //30 30 32 38 30 30 32 33 03 BF


            //02 30 30 31 20 5B
            //37 46 37 46 37 46 37 46 37 46
            //35 43 31 36 35 41 31 38 35 32
            //36 45 35 36 36 39 31 31 31 35
            //30 30 31 39 30 30 31 46 35 46
            //30 30 30 30 30 30 30 30 30 30
            //30 30 30 30 30 41 30 30 30 41
            //30 30 32 38 30 30 32 32 03 B3

            measValues[0] = 0x37; measValues[1] = 0x46; measValues[2] = 0x37; measValues[3] = 0x46; measValues[4] = 0x37;
            measValues[5] = 0x46; measValues[6] = 0x37; measValues[7] = 0x46; measValues[8] = 0x37; measValues[9] = 0x46;
            //37 46 37 46 37 46 37 46 37 46
            measValues[10] = 0x35; measValues[11] = 0x43; measValues[12] = 0x31; measValues[13] = 0x36; measValues[14] = 0x35;
            measValues[15] = 0x41; measValues[16] = 0x31; measValues[17] = 0x38; measValues[18] = 0x35; measValues[19] = 0x32;
            //35 43 31 36 35 41 31 38 35 32
            measValues[20] = 0x36; measValues[21] = 0x45; measValues[22] = 0x35; measValues[23] = 0x36; measValues[24] = 0x36;
            measValues[25] = 0x39; measValues[26] = 0x31; measValues[27] = 0x31; measValues[28] = 0x31; measValues[29] = 0x35;
            //36 45 35 36 36 39 31 31 31 35
            measValues[30] = 0x30; measValues[31] = 0x30; measValues[32] = 0x31; measValues[33] = 0x39; measValues[34] = 0x30;
            measValues[35] = 0x30; measValues[36] = 0x31; measValues[37] = 0x46; measValues[38] = 0x35; measValues[39] = 0x46;
            //30 30 31 39 30 30 31 46 35 46
            measValues[40] = 0x30; measValues[41] = 0x30; measValues[42] = 0x30; measValues[43] = 0x30; measValues[44] = 0x30;
            measValues[45] = 0x30; measValues[46] = 0x30; measValues[47] = 0x30; measValues[48] = 0x31; measValues[49] = 0x32;//48,49,50,51是速度
            //30 30 30 30 30 30 30 30 30 30
            measValues[50] = 0x33; measValues[51] = 0x34; measValues[52] = 0x30; measValues[53] = 0x30; measValues[54] = 0x30;
            measValues[55] = 0x41; measValues[56] = 0x30; measValues[57] = 0x30; measValues[58] = 0x30; measValues[59] = 0x41;
            //30 30 30 30 30 41 30 30 30 41
            measValues[60] = 0x30; measValues[61] = 0x30; measValues[62] = 0x32; measValues[63] = 0x38; measValues[64] = 0x30;
            measValues[65] = 0x30; measValues[66] = 0x32; measValues[67] = 0x32;
            //30 30 32 38 30 30 32 32 03 B3


            modFonct[0] = 0x30; modFonct[1] = 0x32; modFonct[2] = 0x30; modFonct[3] = 0x30; modFonct[4] = 0x37;
            modFonct[5] = 0x46; modFonct[6] = 0x37; modFonct[7] = 0x46; modFonct[8] = 0x37; modFonct[9] = 0x46;
            for (int i = 10; i < modFonct.Length; i++)
            {
                modFonct[i] = 0x00;
            }
        }

        private void _tick_Elapsed(object sender, ElapsedEventArgs e)
        {
 
            lock (_locker)
            {
                if (_timer.IsRunning && _timer.Elapsed > TimeSpan.FromSeconds(10))
                {
                    _timer.Stop();
                }
            }
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                var msgArray = msg.Select(bt => bt.ToString("X2")).ToArray();
                string msgCommand = string.Join(",", msgArray, 0, msgArray.Length);
                if (msgCommand == simulatorItem.SourceCommand)
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if (activeSimulatorItem.SourceCommandName == "SetStart" || activeSimulatorItem.SourceCommandName == "SetStop" || activeSimulatorItem.SourceCommandName == "SetReset")
            {
                OnWriteMessage(new byte[] { 0x06 });

                Thread.Sleep(50);
                //06 02 30 30 31 23 03 EC
                byte[] requestBytes = new byte[7];
                requestBytes[0] = 0x02;
                requestBytes[1] = 0x30;
                requestBytes[2] = 0x30;
                requestBytes[3] = 0x31;
                requestBytes[4] = 0x23;
                requestBytes[5] = 0x03;
                requestBytes[6] = 0xEC;
                OnWriteMessage(requestBytes);
            }


            if (activeSimulatorItem.SourceCommandName == "ReadMeasValue")
            {
                OnWriteMessage(new byte[] { 0x06});

                Thread.Sleep(50);

                byte[] requestBytes = new byte[measValues.Length + 8];
                requestBytes[0] = 0x02;
                requestBytes[1] = 0x30;
                requestBytes[2] = 0x30;
                requestBytes[3] = 0x31;
                requestBytes[4] = 0x20;
                requestBytes[5] = 0x5B;
                Array.Copy(measValues, 0, requestBytes, 6, measValues.Length);
                requestBytes[measValues.Length + 6] = 0x03;
                requestBytes[measValues.Length + 7] = GetLrc(requestBytes);
                OnWriteMessage(requestBytes);
            }
            else if (activeSimulatorItem.SourceCommandName == "ReadModFonct")
            {
                byte[] requestBytes = new byte[modFonct.Length + 8];
                requestBytes[0] = 0x02;
                requestBytes[1] = 0x30;
                requestBytes[2] = 0x30;
                requestBytes[3] = 0x31;
                requestBytes[4] = 0x20;
                requestBytes[5] = 0x4D;
                Array.Copy(modFonct, 0, requestBytes, 6, modFonct.Length);
                requestBytes[modFonct.Length + 6] = 0x03;
                requestBytes[modFonct.Length + 7] = GetLrc(requestBytes);
                OnWriteMessage(requestBytes);
            }
            else
            {
                byte[] requestBytes = new byte[activeSimulatorItem.Response.Split(',').Length];
                for(int i=0;i<requestBytes.Length;i++)
                {
                    requestBytes[i] = Convert.ToByte(activeSimulatorItem.Response.Split(',')[i]);
                }
                OnWriteMessage(activeSimulatorItem.Response);
            }
        }

        private static byte GetLrc(byte[] cmdBytes)
        {
            byte rt = 0;
            for (int i = 0; i < cmdBytes.Length - 1; i++)
            {
                rt ^= cmdBytes[i];
            }
            rt ^= 0xFF;
            return rt;
        }
    }

    public class TazmoRobotSimulator : CommonSerialPortDeviceSimulator
    {
        static string _enq = ((char)0x05).ToString();
        static string _ack = ((char)0x06).ToString();
        static string _nak = ((char)0x15).ToString();
        static string _busy = ((char)0x11).ToString();
        static string _cr = ((char)0x0D).ToString();
        static string _endLine = ((char)0x0A).ToString();

        public TazmoRobotSimulator(string port, string deviceName) : base(port, deviceName)
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            string content = msg.Remove(msg.Length - 1);
            var contentArray = content.Split(',');
            if (contentArray.Length > 0)
            {
                foreach (var simulatorItem in IOSimulatorItemList)
                {
                    if (simulatorItem.SourceCommand != null && contentArray[0] == simulatorItem.SourceCommand)
                    {
                        return simulatorItem;
                    }
                }
            }
            return null;
        }


        //1) action:  ack & completeEvent==command      +  send ack after complete process
        //2) query:  command,parameterlist,,,respone
        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if (activeSimulatorItem.SourceCommandName == "SendError")
            {
                OnWriteMessage("ERR," + activeSimulatorItem.Response + _endLine);
                return;
            }
            if (activeSimulatorItem.SourceCommandName.Contains("Read"))
            {
                if (activeSimulatorItem.Response != null)
                    OnWriteMessage(activeSimulatorItem.SourceCommand + "," + activeSimulatorItem.Response + _endLine);
                else
                    OnWriteMessage(activeSimulatorItem.SourceCommand + _endLine);
            }
            else
            {
                OnWriteMessage(_ack);
                if (activeSimulatorItem.Response == "CompleteEvent")
                {
                    Thread.Sleep(1000);
                    OnWriteMessage(activeSimulatorItem.SourceCommand + _endLine);
                }
            }
        }
    }

    public class SiasunVCESimulator : CommonSerialPortDeviceSimulator
    {
        public SiasunVCESimulator(string port, string deviceName) : base(port, deviceName)
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            //id,E
            //id,A,DC
            string content = msg.Remove(msg.Length - 1);
            var contentArray = content.Split(',');

            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (contentArray.Length == 2)
                {
                    if (simulatorItem.SourceCommandType != null && contentArray[1] == simulatorItem.SourceCommandType)
                    {
                        return simulatorItem;
                    }
                }
                else if (contentArray.Length > 2)
                {
                    if (simulatorItem.SourceCommandType != null && contentArray[1] == simulatorItem.SourceCommandType &&
                        simulatorItem.SourceCommand != null && contentArray[2] == simulatorItem.SourceCommand)
                    {
                        return simulatorItem;
                    }
                }
            }

            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            //if (activeSimulatorItem.Response.First() == '{')
            //{
            //    var response = activeSimulatorItem.Response.Substring(1, activeSimulatorItem.Response.Length - 2);
            //    OnWriteMessage("00," + response + "\r");
            //}
            //else
            //{
            //    var responseArray = activeSimulatorItem.Response.Split(',');
            //    foreach (var response in responseArray)
            //    {
            //        OnWriteMessage("00," + response + "\r");
            //        Thread.Sleep(1000);
            //    }
            //}

            if(activeSimulatorItem.SourceCommandType == "A" || activeSimulatorItem.SourceCommandType == "S" || activeSimulatorItem.SourceCommandType == "P")
            {
                OnWriteMessage("M," + activeSimulatorItem.Response + "\r");
            }
            else if (activeSimulatorItem.SourceCommandType == "R")
            {
                OnWriteMessage("X," + activeSimulatorItem.Response + "\r");
            }
        }
    }



    public class BrooksVCESimulator : CommonSerialPortDeviceSimulator
    {
        public BrooksVCESimulator(string port, string deviceName) : base(port, deviceName)
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            //id,E
            //id,A,DC
            string content = msg.Remove(msg.Length - 1);
            var contentArray = content.Split(',');

            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (contentArray.Length == 2)
                {
                    if (simulatorItem.SourceCommandType != null && contentArray[1] == simulatorItem.SourceCommandType)
                    {
                        return simulatorItem;
                    }
                }
                else if (contentArray.Length > 2)
                {
                    if (simulatorItem.SourceCommandType != null && contentArray[1] == simulatorItem.SourceCommandType &&
                        simulatorItem.SourceCommand != null && contentArray[2] == simulatorItem.SourceCommand)
                    {
                        return simulatorItem;
                    }
                }
            }

            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            if (activeSimulatorItem.Response.First() == '{')
            {
                var response = activeSimulatorItem.Response.Substring(1, activeSimulatorItem.Response.Length - 2);
                OnWriteMessage("00," + response + "\r");
            }
            else
            {
                var responseArray = activeSimulatorItem.Response.Split(',');
                foreach (var response in responseArray)
                {
                    OnWriteMessage("00," + response + "\r");
                    Thread.Sleep(1000);
                }
            }
        }
    }

    internal class HanbellPumpSimulator : CommonSerialPortDeviceSimulator
    {
        public HanbellPumpSimulator(string port, string deviceName) : base(port, deviceName,false)
        {
        }

        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg[1] == byte.Parse(simulatorItem.SourceCommand))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteMessage(activeSimulatorItem.Response.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());
        }
    }

    public class HirataR4Simulator : CommonSerialPortDeviceSimulator
    {
        private string _address = "002";
        protected static char _stx = '\u0002';
        protected static char _etx = '\u0003';
        protected static char _space = '\u0020';

        private int _lpIndex = 1;

        public HirataR4Simulator(string port, string deviceName) : base(port, deviceName,false,"\0")
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            IOSimulatorItemViewModel theSimulatorItem = null;
            int maxMatachLength = -1;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                int iIndex = msg.IndexOf(simulatorItem.SourceCommand);
                if(iIndex > 0)
                {
                    int matchLength = simulatorItem.SourceCommand.Length;
                    if (matchLength > maxMatachLength)
                    {
                        maxMatachLength = matchLength;
                        theSimulatorItem = simulatorItem;
                    }
                }

            }
            return theSimulatorItem;
        }


        List<byte> _cache = new List<byte>();
        protected override void ProcessUnsplitMessage(byte[] binaryMessage)
        {
            _cache.AddRange(binaryMessage);
            if (!_cache.Contains((byte) (3)))
                return;

            string message = Encoding.ASCII.GetString(_cache.ToArray());

            _cache.Clear();

            OnReadMessage(message);

            if (message.Contains("GP 1"))
                _lpIndex = 1;
            if (message.Contains("GP 2"))
                _lpIndex = 2;
            if (message.Contains("GP 3"))
                _lpIndex = 3;
            if (message.Contains("GP 4"))
                _lpIndex = 4;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteMessage(BuildMessage(activeSimulatorItem.SourceCommand, activeSimulatorItem.Response) + "\0");
        }

        private string BuildMessage(string command, string response)
        {
            if (command == "LE 100")
            {
                if (_lpIndex == 1)
                    response = Convert.ToInt32("1111111111111111111111111", 2).ToString();
                if (_lpIndex == 2)
                    response = "0";
                if (_lpIndex == 3)
                    response = Convert.ToInt32("1100000000000000000000000", 2).ToString();
                if (_lpIndex == 4)
                    response = Convert.ToInt32("0000000000000000000000011", 2).ToString();

            }
            if (command == "LE 101")
            {
                if (_lpIndex == 1)
                    response = "0";
                if (_lpIndex == 2)
                    response = "0";
                if (_lpIndex == 3)
                    response = "0";
                if (_lpIndex == 4)
                    response = "0";
            }

            string msg1 = $"{_address} {response}{_etx}";
            if(response.First() == '-')
            {
                msg1 = $"{_address}{response}{_etx}";
            }

            byte lrc = CalcLRC(Encoding.ASCII.GetBytes(msg1).ToList());
            string msg = $"{_stx}{msg1}{(char)(lrc)}";
            return msg;
        }
        private static byte CalcLRC(List<byte> data)
        {
            byte ret = 0x00;
            for (var i = 0; i < data.Count; i++)
            {
                ret ^= data[i];
            }
            return ret;
        }

    }

    public class BrooksSMIFSimulator : CommonSerialPortDeviceSimulator
    {
        private bool _isPodPresent = true;
        private DeviceTimer _timer = new DeviceTimer();
        private int _motionTime = 3000;//ms
        public BrooksSMIFSimulator(string port, string deviceName) : base(port, deviceName, true, "\r\n")
        {
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            string content = msg.Remove(msg.Length - 2);
            var contentArray = content.Split(' ');
            if (contentArray.Length < 2)
                return null;

            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (contentArray[0] == simulatorItem.SourceCommandType)
                {
                    if(simulatorItem.SourceCommand == null)
                    {
                        return simulatorItem;
                    }
                    else if(contentArray[1] == simulatorItem.SourceCommand)
                    {
                        return simulatorItem;
                    }
                }
            }

            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            string newLine = "\r\n";
            string commandType = activeSimulatorItem.SourceCommandType;
            if (activeSimulatorItem.SourceCommandName.ToUpper() == "LOAD")
                _isPodPresent = true;
            if (activeSimulatorItem.SourceCommandName.ToUpper() == "UNLOAD")
                _isPodPresent = true;

            if(activeSimulatorItem.SourceCommandName.ToUpper() == "LOAD" ||
                activeSimulatorItem.SourceCommandName.ToUpper() == "UNLOAD" ||
                activeSimulatorItem.SourceCommandName.ToUpper() == "HOME" ||
                activeSimulatorItem.SourceCommandName.ToUpper() == "RECOVERY")
            {
                if (_timer.IsIdle())
                    _timer.Stop();
                _timer.Start(0);
            }

            if (commandType == "AERS" || commandType == "ARS")
            {
                OnWriteMessage(commandType + " " + activeSimulatorItem.Response + newLine);
            }
            else if (commandType == "ECR")
            {
                OnWriteMessage("ECD " + activeSimulatorItem.Response + newLine);
            }
            else if (commandType == "FSR")
            {
                var response = activeSimulatorItem.Response;
                if (_isPodPresent)
                    response += " PIP=TRUE";
                else
                    response += " PIP=FALSE";

                if(_timer.IsIdle() || _timer.GetElapseTime() >= _motionTime)
                    response += " READY=TRUE";
                else
                    response += " READY=FALSE";
                OnWriteMessage(response + newLine);
            }
            else
            {
                if (activeSimulatorItem.Response == "OK")
                {
                    OnWriteMessage("HCA OK" + newLine);
                }
                else
                {
                    var resArray = activeSimulatorItem.Response.Split('|');
                    int iIndex = new Random().Next(0, resArray.Length - 1);
                    OnWriteMessage("HCA " + resArray[iIndex] + newLine);
                }
            }         
        }

    }

}
