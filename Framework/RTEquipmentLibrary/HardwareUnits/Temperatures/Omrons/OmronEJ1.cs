using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temperatures.Omrons
{
    public class AdressInfo
    {
        public string Name { get; set; }

        public byte DeviceAddress { get; set; }
        public int UnitAddress { get; set; }
        public int Length { get; set; }

        public int Data;
        public string Remark { get; set; }//H3-N2-H4-N5-H6(H3代表连续3个地址有数据，N2代表H3与H4中间有两个地址为预留)

    }
    public class OmronEJ1 : BaseDevice, IConnection, IDevice
    {
        public event Action<int, int, short[]> OnVariableReadout; //slave, start address, data

        public class AddressMapItem
        {
            public string Name { get; set; }
            public int SlaveAddress { get; set; }
            public int AddressFeedback { get; set; }
            public int AddressSetPoint { get; set; }
            public int AddressAlarm { get; set; }
            public int AddressStatus { get; set; }
            public int Channel { get; set; }

            public AddressMapItem(string name, int slaveAddress, int addressFeedback, int addressSetpoint, int addressAlarm, int addressStatus, int channel)
            {
                Name = name;
                SlaveAddress = slaveAddress;
                AddressFeedback = addressFeedback;
                AddressSetPoint = addressSetpoint;
                AddressAlarm = addressAlarm;
                AddressStatus = addressStatus;
                Channel = channel;
            }
        }

        public Dictionary<int, string> AbnormalCode = new Dictionary<int, string>()
        {
            { 1,"ncorrect function code, this machine does not correspond to the function code." },
            { 2,"Incorrect data address" },
            { 3,"Incorrect data." },
            { 4,"operation error" },
        };
        public Dictionary<string, AdressInfo> _dicAdressInfo = new Dictionary<string, AdressInfo>()//配置最多32个数据
        {
            {"Channel1Setting1", new AdressInfo() { DeviceAddress=0x01, UnitAddress = 0x0240,Length = 1 } },//手册 11-3
            {"Channel1Setting2", new AdressInfo() { DeviceAddress=0x01, UnitAddress = 0x0440,Length = 1 } },
            {"Channel1Setting3", new AdressInfo() { DeviceAddress=0x01, UnitAddress = 0x0640,Length = 1 } },
            {"Channel1Setting4", new AdressInfo() { DeviceAddress=0x01, UnitAddress = 0x0840,Length = 1 } },

            {"Channel2Setting1", new AdressInfo() { DeviceAddress=0x02, UnitAddress = 0x0240,Length = 1 } },//手册 11-3
            {"Channel2Setting2", new AdressInfo() { DeviceAddress=0x02, UnitAddress = 0x0440,Length = 1 } },
            {"Channel2Setting3", new AdressInfo() { DeviceAddress=0x02, UnitAddress = 0x0640,Length = 1 } },
            {"Channel2Setting4", new AdressInfo() { DeviceAddress=0x02, UnitAddress = 0x0840,Length = 1 } },

            {"Channel3Setting1", new AdressInfo() { DeviceAddress=0x03, UnitAddress = 0x0240,Length = 1 } },//手册 11-3
            {"Channel3Setting2", new AdressInfo() { DeviceAddress=0x03, UnitAddress = 0x0440,Length = 1 } },
            {"Channel3Setting3", new AdressInfo() { DeviceAddress=0x03, UnitAddress = 0x0640,Length = 1 } },
            {"Channel3Setting4", new AdressInfo() { DeviceAddress=0x03, UnitAddress = 0x0840,Length = 1 } },

            {"Channel1RunALL", new AdressInfo()  { DeviceAddress=0x01, UnitAddress = 0xFFFF,Length = 1,Data=0x0AFF} },
            {"Channel1Run1", new AdressInfo() { DeviceAddress=0x01, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0A00} },
            {"Channel1Run2", new AdressInfo() { DeviceAddress=0x01, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0A01} },
            {"Channel1Run3", new AdressInfo() { DeviceAddress=0x01, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0A02} },
            {"Channel1Run4", new AdressInfo() { DeviceAddress=0x01, UnitAddress = 0xFFFF,Length = 1,Data=0x0A03} },

            {"Channel2RunALL", new AdressInfo()  { DeviceAddress=0x02, UnitAddress = 0xFFFF,Length = 1,Data=0x0AFF } },
            {"Channel2Run1", new AdressInfo() { DeviceAddress=0x02, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0A00} },
            {"Channel2Run2", new AdressInfo() { DeviceAddress=0x02, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0A01} },
            {"Channel2Run3", new AdressInfo() { DeviceAddress=0x02, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0A02} },
            {"Channel2Run4", new AdressInfo() { DeviceAddress=0x02, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0A03} },

            {"Channel3RunALL", new AdressInfo()  { DeviceAddress=0x03, UnitAddress = 0xFFFF,Length = 1,Data=0x0AFF } },
            {"Channel3Run1", new AdressInfo() { DeviceAddress=0x03, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0A00} },
            {"Channel3Run2", new AdressInfo() { DeviceAddress=0x03, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0A01 } },
            {"Channel3Run3", new AdressInfo() { DeviceAddress=0x03, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0A02} },
            {"Channel3Run4", new AdressInfo() { DeviceAddress=0x03, UnitAddress = 0xFFFF,Length = 1,Data=0x0A03} },

            {"Channel1StopALL", new AdressInfo()  { DeviceAddress=0x01, UnitAddress = 0xFFFF,Length = 1,Data=0x0BFF } },
            {"Channel1Stop1", new AdressInfo() { DeviceAddress=0x01, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0B00} },
            {"Channel1Stop2", new AdressInfo() { DeviceAddress=0x01, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0B01} },
            {"Channel1Stop3", new AdressInfo() { DeviceAddress=0x01, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0B02} },
            {"Channel1Stop4", new AdressInfo() { DeviceAddress=0x01, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0B03} },

            {"Channel2StopALL", new AdressInfo()  { DeviceAddress=0x02, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0BFF} },
            {"Channel2Stop1", new AdressInfo() { DeviceAddress=0x02, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0B00} },
            {"Channel2Stop2", new AdressInfo() { DeviceAddress=0x02, UnitAddress = 0xFFFF,Length = 1,Data=0x0B01} },
            {"Channel2Stop3", new AdressInfo() { DeviceAddress=0x02, UnitAddress = 0xFFFF,Length = 1,Data=0x0B02 } },
            {"Channel2Stop4", new AdressInfo() { DeviceAddress=0x02, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0B03} },

            {"Channel3StopALL", new AdressInfo()  { DeviceAddress=0x03, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0BFF} },
            {"Channel3Stop1", new AdressInfo() { DeviceAddress=0x03, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0B00} },
            {"Channel3Stop2", new AdressInfo() { DeviceAddress=0x03, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0B01} },
            {"Channel3Stop3", new AdressInfo() { DeviceAddress=0x03, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0B02} },
            {"Channel3Stop4", new AdressInfo() { DeviceAddress=0x03, UnitAddress = 0xFFFF,Length = 1 ,Data=0x0B03} },


        };
        private OmronEJ1Connection _connection;
        private bool _activeMonitorStatus;
        private int _errorCode;
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private PeriodicJob _thread;
        private int tempCount = 1;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private object _locker = new object();

        private bool _enableLog = true;

        private string _scRoot;

        private int _tempWarningMaxDiff = 1;        //报警最大温度（实际温度与设定温度之间的差值）
        private R_TRIG[] TempWarningTrig = new R_TRIG[12];   //温度报警
        string[] WarningInfo = new string[12] 
            { "HeatTMAPanel", "HeatTCSPanel", "OuterPanel", 
                "MidPanel", "InnerPanel", "HeatShowerOuter", "HeatShowerInner", 
                "HeatShowerMid", "HeatDptPanel", "HeatLeakSource", "HeatLeakBypass", "HeatDptGate" };

        public float[] ActualTemp = new float[12];

        public float[] SettingTemp = new float[12];

        public string Address
        {
            get; set;
        }

        public bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsConnected;
            }
        }

        public int _connecteTimes { get; set; }

        public bool EnableContinuousQuery { get; set; }

        public bool Connect()
        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }

        public OmronEJ1(string module, string name, string scRoot) : base(module, name, name, name)
        {
            _scRoot = scRoot;
            _activeMonitorStatus = true;
            EnableContinuousQuery = true;
        }

        ~OmronEJ1()
        {
            _connection.Disconnect();

        }

        public void QueryTemp()
        {
            lock (_locker)
            {
                _lstHandler.Clear();

                _lstHandler.AddLast(new OmronEJ1ReadVariableHandler(this, 1, 0x0080, 16));
                _lstHandler.AddLast(new OmronEJ1ReadVariableHandler(this, 2, 0x0080, 16));
                _lstHandler.AddLast(new OmronEJ1ReadVariableHandler(this, 3, 0x0080, 16));

            }

            //_lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel1 Actual1 Temp", 0x01, 0x02, 0x00, 0x00, 0x01));
            //_lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel1 Setting1 Temp", 0x01, 0x02, 0x40, 0x00, 0x01));
            //_lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel1 Actual2 Temp", 0x01, 0x04, 0x00, 0x00, 0x01));
            //_lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel1 Setting2 Temp", 0x01, 0x04, 0x40, 0x00, 0x01));
            //_lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel1 Actual3 Temp", 0x01, 0x06, 0x00, 0x00, 0x01));
            //_lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel1 Setting3 Temp", 0x01, 0x06, 0x40, 0x00, 0x01));
            //_lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel1 Actual4 Temp", 0x01, 0x08, 0x00, 0x00, 0x01));
            //_lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel1 Setting4 Temp", 0x01, 0x08, 0x40, 0x00, 0x01));

            //if (moduleCount >= 2)
            //{
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel2 Actual1 Temp", 0x02, 0x02, 0x00, 0x00, 0x01));
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel2 Setting1 Temp", 0x02, 0x02, 0x40, 0x00, 0x01));
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel2 Actual2 Temp", 0x02, 0x04, 0x00, 0x00, 0x01));
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel2 Setting2 Temp", 0x02, 0x04, 0x40, 0x00, 0x01));
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel2 Actual3 Temp", 0x02, 0x06, 0x00, 0x00, 0x01));
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel2 Setting3 Temp", 0x02, 0x06, 0x40, 0x00, 0x01));
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel2 Actual4 Temp", 0x02, 0x08, 0x00, 0x00, 0x01));
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel2 Setting4 Temp", 0x02, 0x08, 0x40, 0x00, 0x01));
            //}
            //if (moduleCount >= 3)
            //{
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel3 Actual1 Temp", 0x03, 0x02, 0x00, 0x00, 0x01));
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel3 Setting1 Temp", 0x03, 0x02, 0x40, 0x00, 0x01));
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel3 Actual2 Temp", 0x03, 0x04, 0x0, 0x00, 0x01));
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel3 Setting2 Temp", 0x03, 0x04, 0x40, 0x00, 0x01));
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel3 Actual3 Temp", 0x03, 0x06, 0x0, 0x00, 0x01));
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel3 Setting3 Temp", 0x03, 0x06, 0x40, 0x00, 0x01));
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel3 Actual4 Temp", 0x03, 0x08, 0x00, 0x00, 0x01));
            //    _lstHandler.AddLast(new OmronEJ1QueryHandler(this, "Query Channel3 Setting4 Temp", 0x03, 0x08, 0x40, 0x00, 0x01));
            //}
        }


        public void ResetDevice()
        {

        }

        public void QueryError()
        {
            EV.PostInfoLog(Module, "Query error");
        }


        public bool Initialize(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits)
        {
            _enableLog = true;
            _connection = new OmronEJ1Connection(portName, baudRate, dataBits, parity, stopBits, "", false);
            _connection.EnableLog(_enableLog);

            EnableContinuousQuery = false;

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(200, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            return true;
        }

        public bool Initialize()
        {
            string portName = SC.GetStringValue($"{_scRoot}.Address");
            int baudRate = SC.GetValue<int>($"{_scRoot}.BaudRate");
            int dataBits = SC.GetValue<int>($"{_scRoot}.DataBits");
            Parity parity = (Parity)Enum.Parse(typeof(Parity), SC.GetStringValue($"{_scRoot}.Parity"));
            StopBits stopBits = (StopBits)Enum.Parse(typeof(StopBits), SC.GetStringValue($"{_scRoot}.StopBits"));

            Address = portName;
 
            _enableLog = SC.GetValue<bool>($"{_scRoot}.EnableLogMessage");

            _connection = new OmronEJ1Connection(portName, baudRate, dataBits, parity, stopBits);
            _connection.EnableLog(_enableLog);
 
            int count = SC.ContainsItem("System.ComPortRetryCount") ? SC.GetValue<int>("System.ComPortRetryCount") : 5;
            int sleep = SC.ContainsItem("System.ComPortRetryDelayTime") ? SC.GetValue<int>("System.ComPortRetryDelayTime") : 2;
            if (sleep <= 0 || sleep > 10)
                sleep = 2;
            int retry = 0;
            do
            {
                _connection.Disconnect();
                Thread.Sleep(sleep * 1000);
                if (_connection.Connect())
                {
                    EV.PostInfoLog(Module, $"{Name} connected");
                    break;
                }

                if (count > 0 && retry++ > count)
                {
                    LOG.Write($"Retry connect {Module}.{Name} stop retry.");
                    EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
                    break;
                }

                Thread.Sleep(sleep * 1000);
                LOG.Write($"Retry connect {Module}.{Name} for the {retry + 1} time.");

            } while (true);

            _thread = new PeriodicJob(200, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            ConnectionManager.Instance.Subscribe($"{Name}", this);

            return true;
        }

        private bool OnTimer()
        {
            try
            {
                _connection.MonitorTimeout();

                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {

                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        _connection.SetPortAddress(SC.GetStringValue($"{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                    }

                    if (_connecteTimes++ < 3)
                        _connection.ForceClear();
                    else _connecteTimes = 4;
                    return true;
                }

                HandlerBase handler = null;
                lock (_locker)
                {
                    if (_lstHandler.Count == 0  && EnableContinuousQuery)
                        QueryTemp();
                    if (_lstHandler.Count > 0 && !_connection.IsBusy)
                    {
                        handler = _lstHandler.First.Value;
                        _lstHandler.RemoveFirst();
                        if (handler != null)
                        {
                            _connection.Execute(handler);
                        }
                    }
                }

                Thread.Sleep(30);


            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        internal void NoteError()
        {

        }

        public void Monitor()
        {
            try
            {
                _connection.EnableLog(_enableLog);

                if (_connecteTimes < 4) return;
                _trigCommunicationError.CLK = _connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        ActualTemp[i] = 0;
                        SettingTemp[i] = 0;
                    }
                    EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                }

                MonitorWarning();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }


        private void MonitorWarning()
        {
            for (int i = 0; i < 12; i++)
            {
                if (TempWarningTrig[i] == null)
                    continue;
                TempWarningTrig[i].CLK = SettingTemp[i] > 0 && _tempWarningMaxDiff > 0 && Math.Abs(ActualTemp[i] - SettingTemp[i]) > _tempWarningMaxDiff;
                if (TempWarningTrig[i].Q)
                {
                    EV.PostWarningLog(Module, $"{WarningInfo[i]},the difference between the actual value and the set value is too large");
                }
            }
        }
 
        public void Reset()
        {
            _connecteTimes = 0;
            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{_scRoot}.EnableLogMessage");

            _trigRetryConnect.RST = true;

            for (int i = 0; i < 12; i++)
            {
                TempWarningTrig[i].RST = true;
            }
        }
        public void RespondAbnormal(int funcode, int abnormalcode)
        {
            if (funcode == 0x83)
            {
                foreach (var code in AbnormalCode)
                {
                    if (code.Key == abnormalcode)
                        EV.PostWarningLog(Name, $"Query Abnormal:{code.Value}");
                }
            }
            if (funcode == 0x90 || funcode == 0x86)
            {
                foreach (var code in AbnormalCode)
                {
                    if (code.Key == abnormalcode)
                        EV.PostWarningLog(Name, $"Write Abnormal:{code.Value}");
                }
            }
        }

        public void SetActiveMonitor(bool active)
        {
            _activeMonitorStatus = active;
        }

        public void SetActiveMonitorHandler(bool active)
        {
            lock (_locker)
            {
                _lstHandler.Clear();

                int[] channelOn = new int[] { 0x0A00, 0x0A01, 0x0A02, 0x0A03 };
                int[] channelOff = new int[] { 0x0B00, 0x0B01, 0x0B02, 0x0B03 };

                //_lstHandler.AddLast(new OmronEJ1ReadVariableHandler(this, slaveAddress, 0xFFFF, isOn ? channelOn[channel] : channelOff[channel]));

            }
            //return true;
        }

        public void SetErrorCode(int errorCode)
        {
            _errorCode = errorCode;
        }

        public void Terminate()
        {
            _connection.Disconnect();
        }


        public void NoteVariableReadout(int slaveAddress, int startAddress, short[] value)
        {
            if (OnVariableReadout != null)
                OnVariableReadout(slaveAddress, startAddress, value);
        }

        public bool SetChannelRunStop(int slaveAddress, int channel, bool isOn)
        {
            lock (_locker)
            {
                _lstHandler.Clear();

                int[] channelOn = new int[] { 0x0A00, 0x0A01, 0x0A02, 0x0A03 };
                int[] channelOff = new int[] { 0x0B00, 0x0B01, 0x0B02, 0x0B03 };

                _lstHandler.AddLast(new OmronEJ1WriteVariableHandler(this, slaveAddress, 0xFFFF, isOn ? channelOn[channel] : channelOff[channel]));

            }
            return true;
        }

        public bool SetTemperature(int slaveAddress, int addressSetPoint, int temperature)
        {
            lock (_locker)
            {
                _lstHandler.Clear();
 
                _lstHandler.AddLast(new OmronEJ1WriteVariableHandler(this, slaveAddress, addressSetPoint, temperature));

            }
            return true;
        }
    }
}
