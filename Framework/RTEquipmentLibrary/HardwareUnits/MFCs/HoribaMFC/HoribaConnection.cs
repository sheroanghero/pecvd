using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Equipment;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.MFCs.HoribaMFC
{
    public class HoribaMessage : BinaryMessage
    {
        public string DeviceAddress { get; set; }
        public string Action { get; set; }
        public string Parameter { get; set; }
        public int DataLength { get; set; }
        public string ErrorText { get; set; }
        public byte[] Datas { get; set; }
    }

    public class HoribaConnection: Singleton<HoribaConnection>
    {
        public HoribaConnectionBase Horiba { get; private set; }
        public HoribaConnection()
        {
            Horiba = new HoribaConnectionBase();
        }
    }

    public class HoribaConnectionBase : SerialPortConnectionBase
    {
        private const int STX = 0x02;
        private const int ETX = 0x03;
        private const int NAK = 0x15;
        private List<byte> _lstCacheBuffer = new List<byte>();
        private PeriodicJob _thread;

        private bool _activeMonitorStatus;

        private R_TRIG _trigWarningMessage = new R_TRIG();
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private object _locker = new object();

        private bool _enableLog = true;
        private bool _isFirstTime = true;
        private int _reconnectCount = 0;

        private DeviceTimer _QueryScaleTimer = new DeviceTimer();
        private readonly int _QueryScaleInterval = 3000;

        private List<MfcBase> MFCList = new List<MfcBase>();

        public HoribaConnectionBase():base(SC.GetStringValue($"{ModuleName.PM}.MFCSerialPortName"), 38400, 7, Parity.Odd, StopBits.One, "\r", false)
        {
            _activeMonitorStatus = true;
            Module = ModuleName.PM.ToString();
            Name = "HoribaConnection";

            _enableLog = SC.GetValue<bool>($"{Module}.MFCEnableLogMessage");
            EnableLog(_enableLog);

            if (Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            Thread.Sleep(2000);
            if (SC.ContainsItem("System.IsSimulatorMode") && SC.GetValue<bool>("System.IsSimulatorMode"))
                _thread = new PeriodicJob(200, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            else
                _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
        }

        public HoribaConnectionBase(string module) : base(SC.GetStringValue($"{module}.MFCSerialPortName"), 38400, 7, Parity.Odd, StopBits.One, "\r", false)
        {
            _activeMonitorStatus = true;
            Module = module;
            Name = "HoribaConnection";

            _enableLog = SC.GetValue<bool>($"{Module}.MFCEnableLogMessage");
            EnableLog(_enableLog);

            if (Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            Thread.Sleep(2000);
            _QueryScaleTimer.Start(_QueryScaleInterval);
            if (SC.ContainsItem("System.IsSimulatorMode") && SC.GetValue<bool>("System.IsSimulatorMode"))
                _thread = new PeriodicJob(200, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            else
                _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
        }

        public string Module { get; set; }
        public string Name { get; set; }

        private bool OnTimer()
        {
            try
            {
                MonitorTimeout();
                if (!IsConnected || IsCommunicationError)
                {
                    _trigRetryConnect.CLK = !IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        EV.PostAlarmLog(Module, $"Disconnect with {Module}.{Name} {SC.GetStringValue($"{ModuleName.PM}.MFCSerialPortName")}");

                        _reconnectCount = 0;
                    }

                    if (_reconnectCount <= SC.GetValue<int>($"{Module}.MFCReconnectCount"))
                    {
                        //SetPortAddress(SC.GetStringValue($"{Module}.MFCSerialPortName"));                       
                        Connect();
                        Thread.Sleep(2000);
                        if (IsConnected)
                        {
                            Reset();
                            EV.PostInfoLog(Module, $"Reconnect with {Module}.{Name} {SC.GetStringValue($"{ModuleName.PM}.MFCSerialPortName")} successed");
                        }
                        else
                        {
                            EV.PostWarningLog(Module, $"Can not connect with {Module}.{Name} {SC.GetStringValue($"{ModuleName.PM}.MFCSerialPortName")}, retry {_reconnectCount}");
                            Thread.Sleep(1000);
                        }
                    }

                    lock (_locker)
                    {
                        _lstHandler.Clear();
                        _reconnectCount++;
                    }

                    return true;
                }

                HandlerBase handler = null;
                if (!IsBusy)
                {
                    lock (_locker)
                    {
                        if (_lstHandler.Count == 0 && _activeMonitorStatus)
                        {
                            foreach(var mfc in MFCList)
                            {
                                _lstHandler.AddLast(new HoribaMFCQueryFlow(mfc, mfc.DeviceID));
                                if(_QueryScaleTimer.IsTimeout())
                                    _lstHandler.AddLast(new HoribaMFCQueryScale(mfc, mfc.DeviceID));
                                if (_isFirstTime)
                                {
                                    _lstHandler.AddLast(new HoribaMFCQueryScale(mfc, mfc.DeviceID));
                                    _lstHandler.AddLast(new HoribaMFCSetDigitalMode(mfc, mfc.DeviceID));
                                }
                            }

                            if (_QueryScaleTimer.IsTimeout())
                                _QueryScaleTimer.Start(_QueryScaleInterval);
                            _isFirstTime = false;
                        }

                        if (_lstHandler.Count > 0)
                        {
                            handler = _lstHandler.First.Value;

                            _lstHandler.RemoveFirst();
                        }
                    }

                    if (handler != null)
                    {
                        Execute(handler);
                    }
                }

                EnableLog(_enableLog);

                _trigCommunicationError.CLK = IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} communication error");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        public void AddHandler(HandlerBase handler)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(handler);
            }
        }

        public void AddMFC(MfcBase mfc)
        {
            MFCList.Add(mfc);
    }

        public void Reset()
        {
            _trigWarningMessage.RST = true;

            SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{Module}.MFCEnableLogMessage");

            _trigRetryConnect.RST = true;

            _reconnectCount = 0;
        }

        public override bool SendMessage(byte[] message)
        {
            _lstCacheBuffer.Clear();
            return base.SendMessage(message);
        }

        public void Terminate()
        {
        }

        public void SetError(byte[] content)
        {
            string reason = System.Text.Encoding.ASCII.GetString(content);
            _trigWarningMessage.CLK = true;
            if (_trigWarningMessage.Q)
            {
                EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");

            }
        }

        protected override MessageBase ParseResponse(byte[] rawMessage)
        {
            _lstCacheBuffer.AddRange(rawMessage);
            byte[] temps = _lstCacheBuffer.ToArray();

            HoribaMessage msg = new HoribaMessage();
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            msg.RawMessage = _lstCacheBuffer.ToArray();
            if (_lstCacheBuffer.Count < 4 || _lstCacheBuffer[_lstCacheBuffer.Count - 2] != ETX)
                return msg;

            if (temps.Length > 0 && (int)temps[0] == NAK)
            {
                LOG.Error($"NAK, " + temps);
                msg.IsNak = true;
                return msg;
            }

            if (temps.Length < 4)
            {
                LOG.Error($"text length check failed");
                msg.IsFormatError = true;
                return msg;
            }

            if ((int)temps[0] != STX)
            {
                LOG.Error($"text check STX failed");
                msg.IsFormatError = true;
                return msg;
            }

            int etxIndex = 0;
            foreach (var item in temps)
            {
                if ((int)item == ETX)
                    break;

                etxIndex++;
            }

            if(etxIndex >= temps.Length)
            {
                LOG.Error($"text check ETX failed");
                msg.IsFormatError = true;
                return msg;
            }

            msg.DataLength = etxIndex - 1;
            msg.Datas = new byte[etxIndex - 1];

            ///STX msg ETX BCC
            Array.Copy(temps, 1, msg.Datas, 0, etxIndex - 1);          

            var bcc = (int)temps[etxIndex + 1];

            int checkBCC = 0;
            foreach (var item in msg.Datas)
            {
                checkBCC += (int)item;
            }
            checkBCC += ETX;

            if (checkBCC % 128 != bcc)
            {
                LOG.Error($"check BCC failed");
                msg.IsFormatError = true;
                return msg;
            }

            msg.IsResponse = true;
            msg.IsAck = true;
            msg.IsComplete = true;

            return msg;
        }

    }
}
