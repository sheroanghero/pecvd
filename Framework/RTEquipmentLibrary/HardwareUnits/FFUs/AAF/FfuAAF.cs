using System;
using System.Collections.Generic;
using System.Threading;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.FFUs.MayAir;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.FFUs.AAF
{
    public class FfuAAF : BaseDevice, IConnection
    {
        private FfuAAFConnection _connection;

        private string _deviceAddress = "001";

        private bool _isOn;

        private bool _activeMonitorStatus;

        private int _nMaxSpeed;
        public int NMaxSpeed { get => _nMaxSpeed; }

        private int _errorCode;

        private RD_TRIG _trigPumpOnOff = new RD_TRIG();
        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigOverTemp = new R_TRIG();

        private R_TRIG _trigWarningMessage = new R_TRIG();
        private string _lastError = string.Empty;
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private object _locker = new object();

        private bool _enableLog
        {
            get
            {
                if(SC.ContainsItem($"{_scRoot}.{Name}.EnableLogMessage"))
                    return SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
                return false;
            }
        }

        private string _scRoot;

        private int _ffu1Speed = 88;
        private int _ffu2Speed = 77;
        private int _ffu3Speed;

        private int _ffuCount;

        public int FFU1Speed
        {
            get { return _ffu1Speed; }
            set { _ffu1Speed = value; }
        }
        public int FFU2Speed
        {
            get { return _ffu2Speed; }
            set { _ffu2Speed = value; }
        }
        public int FFU3Speed
        {
            get { return _ffu3Speed; }
            set { _ffu3Speed = value; }
        }

        public string Address
        {get;set; }

        public bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsConnected;
            }
        }


        public bool Connect()
        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }

        public FfuAAF(string module, string name, string scRoot) : base(module, name, name, name)
        {
            _scRoot = scRoot;
            _activeMonitorStatus = true;
            _ffuCount = 1;
        }
        public FfuAAF(string module, string name, string scRoot, int ffucount) : base(module, name, name, name)
        {
            _scRoot = scRoot;
            _activeMonitorStatus = true;
            _ffuCount = ffucount;
        }

         ~FfuAAF()
        {
            _connection.Disconnect();

        }

        public void QuerySpeedFanaddress(int fanaddress)
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                if (_connection.IsBusy) _connection.ForceClear();

                List<byte> cmddata = new List<byte>() { 0x15, (byte)(0x20 + fanaddress), 0x1 };
                byte checksum = ModRTU_CRC(cmddata.ToArray());
                cmddata.Add(checksum);
                _connection.SendMessage(cmddata.ToArray());
            }
        }

        public void QuerySpeed()
        {
            
            _lstHandler.AddLast(new FfuAAFQuerySpeedHandler(this, 1, 1));

            if (_ffuCount == 2)
            {               
                _lstHandler.AddLast(new FfuAAFQuerySpeedHandler(this, 3, 1));
            }
            if (_ffuCount == 3)
            {                
                _lstHandler.AddLast(new FfuAAFQuerySpeedHandler(this, 5, 1));
            }
        }

        public void ResetDevice()
        {

        }

        public void QueryError()
        {

            _lstHandler.AddLast(new FfuAAFQueryStatusHandler(this, 1, 1));
            _lstHandler.AddLast(new FfuAAFQueryStatusHandler(this, 3, 1));

            EV.PostInfoLog(Module, "Query error");
        }

        public bool Initialize()
        {
            //DATA.Subscribe($"{Module}.{Name}.Power", () => _power);

            //DATA.Subscribe($"{Module}.{Name}.ErrorCode", () => _errorCode);
            //DATA.Subscribe($"{Module}.{Name}.IsAtSpeed", () => _isAtSpeed);
            //DATA.Subscribe($"{Module}.{Name}.IsAccelerate", () => _isAccelerate);

            string portName = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            Address = portName;
            int address = SC.GetValue<int>($"{_scRoot}.{Name}.DeviceAddress");

            _deviceAddress = address.ToString("D3");
            

            _nMaxSpeed = SC.GetValue<int>($"{_scRoot}.{Name}.MaxSpeed");
            _connection = new FfuAAFConnection(portName);
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






            _thread = new PeriodicJob(1000, OnTimer, $"{Name} MonitorHandler", true);
            DATA.Subscribe($"{Name}.FFU1Speed", () => _ffu1Speed);
            DATA.Subscribe($"{Name}.FFU2Speed", () => _ffu2Speed);
            DATA.Subscribe($"{Name}.FFU3Speed", () => _ffu3Speed);

            OP.Subscribe($"{Name}.SetSpeed", (cmd, param) =>
            {
                if (!int.TryParse((string)param[0], out var speed))
                {
                    EV.PostWarningLog(Module, "invalid speed.");
                    return false;
                }
                SetSpeed(speed);

                EV.PostInfoLog(Module, $"{Name} speed set to {speed}");
                return true;
            });


            OP.Subscribe($"{Name}.SetFFUSpeedBySC", (cmd, param) =>
            {
                int speed = SC.GetValue<int>("System.FFUFanSpeedSetPoint");
                SetSpeed(speed);
                EV.PostInfoLog(Module, $"Set {Name} speed");
                return true;
            });
            OP.Subscribe("FFU1.SetFFUSpeedBySC", (cmd, param) =>
            {
                int speed = SC.GetValue<int>("System.FFU1FanSpeedSetPoint");
                SetSpeed(speed,1);
                EV.PostInfoLog(Module, "Set FFU1 speed");
                return true;
            });
            OP.Subscribe("FFU2.SetFFUSpeedBySC", (cmd, param) =>
            {
                int speed = SC.GetValue<int>("System.FFU2FanSpeedSetPoint");
                SetSpeed(speed, 3);
                EV.PostInfoLog(Module, "Set FFU2 speed");
                return true;
            });
            ConnectionManager.Instance.Subscribe($"{Name}", this);

            return true;
        }
        
        private bool OnTimer()
        {
            try
            {
                _connection.EnableLog(_enableLog);
                _connection.MonitorTimeout();
                _trigRetryConnect.CLK = !_connection.IsConnected;
                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    _connection.ForceClear();
                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }
                    
                    if (_trigRetryConnect.Q)
                    {
                        _connection.SetPortAddress(SC.GetStringValue($"{_scRoot}.{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                    }
                    return true;
                }

                
                if (_lstHandler.Count == 0)
                {
                    lock(_locker)
                        QuerySpeed();
                }
                if (!_connection.IsBusy)
                {
                    HandlerBase handler = null;
                    lock (_locker)
                    {                        

                        if (_lstHandler.Count > 0)
                        {
                            handler = _lstHandler.First.Value;
                            _lstHandler.RemoveFirst();
                        }                        
                    }

                    if (handler != null)
                    {
                        _connection.Execute(handler);
                    }
                }
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

        }

        public void Reset()
        {
            _trigError.RST = true;
            _trigOverTemp.RST = true;
            _trigWarningMessage.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _connection.EnableLog(_enableLog);

            _trigRetryConnect.RST = true;


        }


     


        public void SetSpeed(float speed)
        {
            lock (_locker)
            {
                _lstHandler.AddFirst(new FfuAAFSetSpeedHandler(this, 0, 1, Convert.ToInt32(speed)));

                //_lstHandler.Clear();
                //if (_connection.IsBusy) _connection.ForceClear();

                //List<byte> cmddata = new List<byte>() { 0x35, 0x40, 0x0, (byte)(speed * 250 / _nMaxSpeed) };
                //byte checksum = ModRTU_CRC(cmddata.ToArray());
                //cmddata.Add(checksum);
                //_connection.SendMessage(cmddata.ToArray());
            }
        }

        public void SetSpeed(float speed,int ffuAddress)
        {
            lock (_locker)
            {
                _lstHandler.AddFirst(new FfuAAFSetSpeedHandler(this, ffuAddress, 1, Convert.ToInt32(speed)));



                //_lstHandler.Clear();
                //if (_connection.IsBusy) _connection.ForceClear();



                //List<byte> cmddata = new List<byte>() { 0x35, (byte)(0x40+ ffuAddress), 0x01, (byte)(speed * 250 / _nMaxSpeed) };
                //byte checksum = ModRTU_CRC(cmddata.ToArray());
                //cmddata.Add(checksum);
                //_connection.SendMessage(cmddata.ToArray());
            }
        }
        public void SetErrorCode(int errorCode)
        {
            _errorCode = errorCode;
        }


        public void SetError(string reason)
        {
            _trigWarningMessage.CLK = true;
            if (_trigWarningMessage.Q)
            {
                EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");

            }
        }
        private static byte ModRTU_CRC(byte[] buffer)
        {
            //ushort crc = 0xFFFF;
            // var buf = System.Text.Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, buffer));
            var buf = buffer;
            var len = buffer.Length;
            byte temp = buffer[0];

            for (int i = 1; i < buffer.Length; i++)
            {
                temp = (byte)(temp ^ buffer[i]);
            }
            return (byte)~temp;
        }


        public void Terminate()
        {
            _connection.Disconnect();
        }

    }
}
