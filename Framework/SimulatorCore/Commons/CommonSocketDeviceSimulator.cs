using MECF.Framework.Simulator.Core.Commons;
using MECF.Framework.Simulator.Core.Driver;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace MECF.Framework.Simulator.Core.Commons
{
    public class SocketDeviceSimulatoFactory
    {
        public static CommonSocketDeviceSimulator GetCommonSocketDeviceSimulator(int port, string deviceName)
        {
            if (deviceName == "Hanbell")
            {
                return new HanbellPumpSocketSimulator(port, deviceName);
            }
            else if (deviceName == "SiasunPhoenixB")
            {
                return new SiasunPhoenixBSocketSimulator(port, deviceName);
            }
            else if (deviceName == "Siasun1500C800C")
            {
                return new Siasun1500C800CSocketSimulator(port, deviceName);
            }
            else if (deviceName == "TruPlasmaRF1000")
            {
                return new TruPlasmaRF1000Simulator(port, deviceName);
            }
            else if (deviceName == "RemoteControl")
            {
                return new RemoteControlSocketSimulator(port, deviceName);
            }
            return null;
        }
    }


    public class CommonSocketDeviceSimulator : SimpleSocketDeviceSimulator
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
        public CommonSocketDeviceSimulator(int port, string deviceName, bool isAscii = true, string newLine = "\r")
            : base(port, -1, newLine, ',', isAscii)
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


        protected override void ProcessUnsplitMessage(byte[] binaryMessage)
        {
            lock (_locker)
            {
                var activeSimulatorItem = GetActiveIOSimulatorItemViewModel(binaryMessage);
                if (activeSimulatorItem == null) return;

                activeSimulatorItem.CommandContent = string.Join(",", binaryMessage.Select(bt => bt.ToString("X2")).ToArray());
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
                var activeSimulatorItem = GetActiveIOSimulatorItemViewModel(msg);
                if (activeSimulatorItem == null) return;

                activeSimulatorItem.CommandContent = msg;
                activeSimulatorItem.CommandRecievedTime = DateTime.Now;

                if (SimulatorItemActived != null)
                    SimulatorItemActived(activeSimulatorItem);

                if (AutoReply)
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
    internal class TruPlasmaRF1000Simulator : CommonSocketDeviceSimulator
    {
        private List<byte> _forwardPower = new List<byte>() { 0x00, 0x00, 0x00, 0x00 };
        private List<byte> _reflectedPower = new List<byte>() { 0x00, 0x00, 0x00, 0x00 };
        private List<byte> _onOff = new List<byte>() { 0x00, 0x00, 0x00, 0x00 };
        private List<byte> _pulseMode = new List<byte>() { 0x00, 0x00, 0x00, 0x00 };
        private List<byte> _clockMode = new List<byte>() { 0x00 };
        private List<byte> _freq = new List<byte>() { 0x00, 0x00, 0x00, 0x00 };
        private List<byte> _processStatus = new List<byte>() { 0x00, 0x00, 0x00, 0x00 };
        private List<byte> _msgBuffer = new List<byte>();
        private object _locker = new object();
        private int _workFrequency = 60000;//60MHz
        public TruPlasmaRF1000Simulator(int port, string deviceName) : base(port, deviceName, false)
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
                if (_msgBuffer.Count < _msgBuffer[0] + 1)
                {
                    return null;
                }
                msgArray = _msgBuffer.Select(bt => bt.ToString("X2")).Take(_msgBuffer[0] + 1).ToArray();
                _msgBuffer.RemoveRange(0, _msgBuffer[0] + 1);
            }

            string msgCommand = string.Join(",", msgArray, 2, 4);
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
            OnWriteMessage(BuildMessage(activeSimulatorItem));
        }

        private byte[] BuildMessage(IOSimulatorItemViewModel activeSimulatorItem)
        {
            TruPlasmaRFResponse rfResponse = JsonConvert.DeserializeObject<TruPlasmaRFResponse>(activeSimulatorItem.Response);
            if (activeSimulatorItem.SourceCommandName == "SetPiValue")
            {
                _forwardPower.Clear();
                var powerArry = activeSimulatorItem.CommandContent.Split(',').Skip(8).Take(2).Select(s => Convert.ToByte(s, 16)).ToArray();
                if (powerArry.Length == 2)
                {
                    var power = BitConverter.ToUInt16(powerArry, 0);
                    _forwardPower.AddRange(BitConverter.GetBytes((UInt32)power));//set value type = UINT16, return value type = UINT32
                }
            }
            else if (activeSimulatorItem.SourceCommandName == "SetPowerOnOff")
            {
                _onOff.Clear();
                _onOff.AddRange(activeSimulatorItem.CommandContent.Split(',').Skip(8).Take(4).Select(s => Convert.ToByte(s, 16)).ToArray());
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
                _pulseMode.AddRange(activeSimulatorItem.CommandContent.Split(',').Skip(8).Take(4).Select(s => Convert.ToByte(s, 16)).ToArray());
            }
            else if (activeSimulatorItem.SourceCommandName == "SetFrequency")
            {
                _freq.Clear();
                var tmp = activeSimulatorItem.CommandContent.Split(',').Skip(8).Take(4).Select(s => Convert.ToByte(s, 16)).ToArray();
                if (tmp.Length == 4)
                {
                    int freq = BitConverter.ToInt32(tmp, 0) + _workFrequency * 1000;
                    _freq.AddRange(BitConverter.GetBytes(freq));
                }
            }
            else if (activeSimulatorItem.SourceCommandName == "SetClockMode")
            {
                _clockMode.Clear();
                _clockMode.AddRange(activeSimulatorItem.CommandContent.Split(',').Skip(8).Take(1).Select(s => Convert.ToByte(s, 16)).ToArray());
            }

            List<byte> buffer = new List<byte>();

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

            return buffer.ToArray();
        }
    }
    public class SiasunPhoenixBSocketSimulator : CommonSocketDeviceSimulator
    {
        private bool _isWaferPresent = false;
        private string _endline = "\r\n";
        private readonly Dictionary<string, int> _timeConfigs = new Dictionary<string, int>();

        public SiasunPhoenixBSocketSimulator(int port, string deviceName) : base(port, deviceName)
        {
            //try
            //{
            //    Hashtable timeSim = (Hashtable)ConfigurationManager.GetSection("VacRobotSim");

            //    _timeConfigs.Add("PICK", int.Parse(timeSim["PICK"].ToString()) * 1000);
            //    _timeConfigs.Add("PLACE", int.Parse(timeSim["PLACE"].ToString()) * 1000);
            //    _timeConfigs.Add("GOTO", int.Parse(timeSim["GOTO"].ToString()) * 1000);
            //    _timeConfigs.Add("RQLOAD", int.Parse(timeSim["RQLOAD"].ToString()) * 1000);
            //    _timeConfigs.Add("CHECKLOAD", int.Parse(timeSim["CHECKLOAD"].ToString()) * 1000);
            //    _timeConfigs.Add("HOME", int.Parse(timeSim["HOME"].ToString()) * 1000);
            //}
            //catch (ConfigurationErrorsException ex)
            //{
            //    throw ex;
            //}
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

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeItem)
        {
            string cmdName = activeItem.SourceCommandName;
            if (cmdName.StartsWith("RQ"))
            {
                if (cmdName.StartsWith("RQ LOAD"))
                {
                    //Thread.Sleep(_timeConfigs["RQLOAD"]);
                    string response = $"{activeItem.Response} {(activeItem.CommandContent.Contains(" A") ? "A" : "B")} {(_isWaferPresent ? "ON" : "OFF")}";
                    OnWriteMessage(response + _endline + "_RDY" + _endline);
                }
                else
                {
                    OnWriteMessage(activeItem.Response + _endline + "_RDY" + _endline);
                }

                //OnWriteMessage("_RDY" + _endline);
                return;
            }
            else if (cmdName.StartsWith("CHECK LOAD"))
            {
                Thread.Sleep(1000);
            }
            else if (cmdName.StartsWith("HOME"))
            {
                Thread.Sleep(2000);
            }
            else if (cmdName.StartsWith("RESET"))
            {
                return;
            }
            else if (cmdName.StartsWith("PICK"))
            {
                Thread.Sleep(2000);
            }
            else if (cmdName.StartsWith("PLACE"))
            {
                Thread.Sleep(2000);
            }
            else if (cmdName.StartsWith("GOTO"))
            {
                Thread.Sleep(1500);
            }

            OnWriteMessage(activeItem.Response + _endline);
        }
    }

    public class Siasun1500C800CSocketSimulator : CommonSocketDeviceSimulator
    {
        public Siasun1500C800CSocketSimulator(int port, string deviceName) : base(port, deviceName)
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

    internal class HanbellPumpSocketSimulator : CommonSocketDeviceSimulator
    {
        private List<byte> _msgBuffer = new List<byte>();
        private bool _isPumpOn;
        private List<byte> statusArray;
        public HanbellPumpSocketSimulator(int port, string deviceName) : base(port, deviceName, false)
        {
            statusArray = Enumerable.Repeat((byte)0x0,90).ToList();
        }
        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(byte[] msg)
        {
            if (IOSimulatorItemList == null)
                return null;

            _msgBuffer.AddRange(msg);
            if (_msgBuffer.Count < 12)
            {
                return null;
            }
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg[7] == byte.Parse(simulatorItem.SourceCommand))
                {
                    //action
                    if (msg[7] == 5)
                    {
                        simulatorItem.Response = string.Join(",", msg.Take(12).Select(bt => bt.ToString("X2")).ToArray());
                    }

                    _msgBuffer.RemoveRange(0, 12);
                    return simulatorItem;
                }
            }
            return null;
        }
        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            var responseArray = activeSimulatorItem.Response.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray();
            if(activeSimulatorItem.SourceCommandName == "OperatePump")
            {
                var response = activeSimulatorItem.Response.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray();
                _isPumpOn = response[10] == 0xFF;
                OnWriteMessage(response);
            }
            if (activeSimulatorItem.SourceCommandName == "RequestRegisters")
            {
                List<byte> buffer = new List<byte>();
                buffer.AddRange(activeSimulatorItem.Response.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray());
                statusArray[61] = (byte)(_isPumpOn ? 0xdc : 0x7f);
                buffer.AddRange(statusArray);
                OnWriteMessage(buffer.ToArray());
            }
        }
    }

    public class RemoteControlSocketSimulator : CommonSocketDeviceSimulator
    {
        uint MessageID = 0;
        uint CommandID = 0;
        string SenderID = "";
        string ReceiverID = "";

        public RemoteControlSocketSimulator(int port, string deviceName) : base(port, deviceName)
        {
        }



        protected override IOSimulatorItemViewModel GetActiveIOSimulatorItemViewModel(string msg)
        {
            List<string> partList = msg.Split('\0').ToList();
            MessageID = Convert.ToUInt32(partList[0]);
            CommandID = Convert.ToUInt32(partList[1]);
            SenderID = partList[2];
            ReceiverID = partList[3];
            if (IOSimulatorItemList == null)
                return null;
            foreach (var simulatorItem in IOSimulatorItemList)
            {
                if (msg.Contains(simulatorItem.SourceCommand.Replace('#','\0')))
                {
                    return simulatorItem;
                }
            }
            return null;
        }

        protected override void OnWriteSimulatorItem(IOSimulatorItemViewModel activeSimulatorItem)
        {
            OnWriteMessage($"{MessageID}{activeSimulatorItem.Response.Replace('#', '\0')}\r\n");
        }
    }

}
