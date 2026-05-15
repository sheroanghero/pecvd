using System;
using System.Collections.Generic;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.FFUs.MayAir;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.FFUs.Aire
{
    public class FfuAAF : BaseDevice, IConnection, IDevice
    {
        private FfuAAFConnection _connection;

        private string _deviceAddress = "001";

        public byte DeviceAddress { get; set; }


        public byte GroupAddress { get; set; }

        public byte Speedfactor { get; set; }

        public int MaxSpeed1 { get; set; }

        public int MaxSpeed2 { get; set; }

        public int MaxSpeed3 { get; set; }

        public int Status { get; set; }


        private bool _isOn;

        private bool _activeMonitorStatus;

        private int _nMaxSpeed = 0;
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

        private bool _enableLog = true;

        private string _scRoot;

        private AsyncSerial serialPort;

        private int _ffuSpeed;

        private string[] StatusStringList = new string[] { "normal", "running fault", "stop fault" };

        public int Speed
        {
            get { return _ffuSpeed; }
            set { _ffuSpeed = value; }
        }

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


        public bool Connect()
        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }

        public FfuAAF(string scRoot, string module, string name, string port) : base(module, name, name, name)
        {

            _scRoot = scRoot;
            _activeMonitorStatus = true;
            //serialPort = new AsyncSerial(port, 9600, 8);
            //serialPort.OnDataChanged += (OnDataChanged);
            //serialPort.OnErrorHappened += (OnErrorHandler);
        }

        private void OnErrorHandler(ErrorEventArgs args)
        {
        }

        private void OnDataChanged(string message)
        {
        }

        public void SetGroupAddress(byte newGroupAddress)
        {
            _lstHandler.AddLast(new FfuAAFSetGroupAddressHandler(this, DeviceAddress, GroupAddress, new byte[] { 0x00, newGroupAddress }));
        }

        public void GetGroupAddress()
        {
            _lstHandler.AddLast(new FfuAAFGetGroupAddressHandler(this, DeviceAddress, GroupAddress, new byte[] { 0x00 }));
        }

        public void SetDeviceAddress(byte newDeviceAddress)
        {
            _lstHandler.AddLast(new FfuAAFSetAddressHandler(this, DeviceAddress, GroupAddress, new byte[] { 0x01, newDeviceAddress }));
        }

        public void GetDeviceAddress()
        {
            _lstHandler.AddLast(new FfuAAFGetAddressHandler(this, DeviceAddress, GroupAddress, new byte[] { 0x01 }));
        }

        public void QueryMaxSpeed1()
        {
            _lstHandler.AddLast(new FfuAAFQueryMaxSpeed1Handler(this, DeviceAddress, GroupAddress, new byte[] { 0x08 }));
        }

        public void QueryMaxSpeed2()
        {
            _lstHandler.AddLast(new FfuAAFQueryMaxSpeed2Handler(this, DeviceAddress, GroupAddress, new byte[] { 0x09 }));
        }

        public void QueryMaxSpeed3()
        {
            _lstHandler.AddLast(new FfuAAFQueryMaxSpeed3Handler(this, DeviceAddress, GroupAddress, new byte[] { 0x0a }));
        }
        /// <summary>
        /// 查转速因子
        /// </summary>
        public void QuerySpeedFactor()
        {
            _lstHandler.AddLast(new FfuAAFQuerySpeedFactorHandler(this, DeviceAddress, GroupAddress, new byte[] { 0x11 }));
        }

        public void SetSpeedFactor(byte newSpeedFactor)
        {
            _lstHandler.AddLast(new FfuAAFSetSpeedFactorHandler(this, DeviceAddress, GroupAddress, new byte[] { 0x11, newSpeedFactor }));
        }

        public void QuerySpeed()
        {
            _lstHandler.AddLast(new FfuAAFQuerySpeedHandler(this, DeviceAddress, GroupAddress));
        }

        public void SetSpeed(int speed)
        {
            _lstHandler.AddLast(new FfuAAFSetSpeedHandler(this, DeviceAddress, GroupAddress, new byte[] { (byte)(speed * 250.0 / MaxSpeed2) }));
        }

        public void ResetDevice()
        {
            _lstHandler.AddLast(new FfuAAFResetDeviceHandler(this, DeviceAddress, GroupAddress));
        }

        public void QueryError()
        {

            _lstHandler.AddLast(new FfuAAFQueryStatusHandler(this, DeviceAddress, GroupAddress));

            EV.PostInfoLog(Module, "Query error");
        }

        private int ChangedStatus(int value)
        {
            int returnValue = 0;
            switch (value)
            {
                case 0:
                    returnValue = 0;
                    break;
                case 0x80:
                    returnValue = 1;
                    break;
                case 0x10:
                    returnValue = 2;
                    break;
                default:
                    break;
            }
            return returnValue;
        }

        public bool Initialize()
        {
            //DATA.Subscribe($"{Module}.{Name}.Power", () => _power);

            //DATA.Subscribe($"{Module}.{Name}.ErrorCode", () => _errorCode);
            //DATA.Subscribe($"{Module}.{Name}.IsAtSpeed", () => _isAtSpeed);
            //DATA.Subscribe($"{Module}.{Name}.IsAccelerate", () => _isAccelerate);

            string portName = SC.GetStringValue($"{_scRoot}.{Module}.{Name}.Address");
            Address = portName;

            int address = SC.GetValue<int>($"{_scRoot}.{Module}.{Name}.DeviceAddress");

            DeviceAddress = (byte)address;
            DATA.Subscribe($"{Name}.DeviceAddress", () => DeviceAddress);

            GroupAddress = (byte)SC.GetValue<int>($"{_scRoot}.{Module}.{Name}.GroupAddress");
            DATA.Subscribe($"{Name}.GroupAddress", () => GroupAddress);

            Status = SC.GetValue<int>($"{_scRoot}.{Module}.{Name}.Status");
            DATA.Subscribe($"{Name}.Status", () => StatusStringList[ChangedStatus(Status)]);

            _deviceAddress = address.ToString("D3");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Module}.{Name}.EnableLogMessage");

            Speedfactor = (byte)SC.GetValue<int>($"{_scRoot}.{Module}.{Name}.Speedfactor");
            DATA.Subscribe($"{Name}.Speedfactor", () => Speedfactor);

            // _nMaxSpeed = SC.GetValue<int>($"{_scRoot}.{Module}.{Name}.MaxSpeed1");
            MaxSpeed1 = SC.GetValue<int>($"{_scRoot}.{Module}.{Name}.MaxSpeed1");
            DATA.Subscribe($"{Name}.MaxSpeed1", () => MaxSpeed1);

            MaxSpeed2 = SC.GetValue<int>($"{_scRoot}.{Module}.{Name}.MaxSpeed2");
            DATA.Subscribe($"{Name}.MaxSpeed2", () => MaxSpeed2);

            MaxSpeed3 = SC.GetValue<int>($"{_scRoot}.{Module}.{Name}.MaxSpeed3");
            DATA.Subscribe($"{Name}.MaxSpeed3", () => MaxSpeed3);

            _connection = new FfuAAFConnection(portName);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(1000, OnTimer, $"{Name} MonitorHandler", true);

            _ffuSpeed = SC.GetValue<int>($"{_scRoot}.{Module}.{Name}.Speed");

            DATA.Subscribe($"{Name}.Speed", () => _ffuSpeed);


            OP.Subscribe($"{Name}SetFFUGroupAddress", (cmd, param) =>
            {
                if (int.TryParse((string)param[0], out var groupAddress))
                {
                    EV.PostWarningLog(Module, "Set FFU Group Address.");
                    return false;
                }
                int.TryParse((string)param[2], out var newgroupAddress);
                GroupAddress = (byte)newgroupAddress;
                SetGroupAddress((byte)newgroupAddress);
                EV.PostInfoLog(Module, $"{Name}Set FFU Group Address {newgroupAddress}");
                return true;
            });

            OP.Subscribe($"{Name}GetFFUGroupAddress", (cmd, param) =>
            {
                if (int.TryParse((string)param[0], out var groupAddress))
                {
                    EV.PostWarningLog(Module, "Get FFU Group Address.");
                    return false;
                }
                GetGroupAddress();
                EV.PostInfoLog(Module, $"{Name}Get FFU Group Address {groupAddress}");
                return true;
            });

            OP.Subscribe($"{Name}SetFFUDeviceAddress", (cmd, param) =>
            {
                if (int.TryParse((string)param[0], out var groupAddress))
                {
                    EV.PostWarningLog(Module, "Set FFU Device Address.");
                    return false;
                }
                int.TryParse((string)param[2], out var newAddress);
                SetDeviceAddress((byte)newAddress);
                EV.PostInfoLog(Module, $"{Name}Set FFU Device Address {newAddress}");
                return true;
            });

            OP.Subscribe($"{Name}GetFFUDeviceAddress", (cmd, param) =>
            {
                if (int.TryParse((string)param[0], out var groupAddress))
                {
                    EV.PostWarningLog(Module, "Get FFU Device Address.");
                    return false;
                }
                GetDeviceAddress();
                EV.PostInfoLog(Module, $"{Name}Get FFU Device Address {groupAddress}");
                return true;
            });

            OP.Subscribe($"{Name}SetFFUSpeedfactor", (cmd, param) =>
            {
                if (int.TryParse((string)param[0], out var groupAddress))
                {
                    EV.PostWarningLog(Module, "Set FFU speed factor.");
                    return false;
                }
                int.TryParse((string)param[2], out var newSpeedFactor);
                SetSpeedFactor((byte)newSpeedFactor);
                EV.PostInfoLog(Module, $"{Name}Set FFU speed factor {newSpeedFactor}");
                return true;
            });

            OP.Subscribe($"{Name}GetFFUSpeedfactor", (cmd, param) =>
            {
                if (int.TryParse((string)param[0], out var groupAddress))
                {
                    EV.PostWarningLog(Module, "Get FFU speed factor.");
                    return false;
                }
                QuerySpeedFactor();
                EV.PostInfoLog(Module, $"{Name}Get FFU speed factor {groupAddress}");
                return true;
            });

            OP.Subscribe($"{Name}GetFFUStatus", (cmd, param) =>
            {
                if (int.TryParse((string)param[0], out var groupAddress))
                {
                    EV.PostWarningLog(Module, "Get FFU Status.");
                    return false;
                }
                QueryError();
                EV.PostInfoLog(Module, $"{Name}Get FFU Status {groupAddress}");
                return true;
            });


            OP.Subscribe($"{Name}SetFFUSpeed", (cmd, param) =>
            {
                if (int.TryParse((string)param[0], out var groupAddress))
                {
                    EV.PostWarningLog(Module, "invalid speed.");
                    return false;
                }
                int.TryParse((string)param[2], out var speed);
                //SetSpeed((int)(speed * 250 * 1.0 / 1440));
                SetSpeed(speed);
                EV.PostInfoLog(Module, $"{Name} speed set to {speed}");
                return true;
            });

            OP.Subscribe($"{Name}GetFFUSpeed", (cmd, param) =>
            {
                if (int.TryParse((string)param[0], out var groupAddress))
                {
                    EV.PostWarningLog(Module, "FFU Query speed.");
                    return false;
                }
                QuerySpeed();
                EV.PostInfoLog(Module, $"{Name}FFU Query Speed {groupAddress}");
                return true;
            });

            OP.Subscribe($"{Name}GetFFUMaxSpeed1", (cmd, param) =>
            {
                if (int.TryParse((string)param[0], out var groupAddress))
                {
                    EV.PostWarningLog(Module, "Get FFU GetFFUMaxSpeed1.");
                    return false;
                }
                QueryMaxSpeed1();
                EV.PostInfoLog(Module, $"{Name}Get FFU GetFFUMaxSpeed1 {groupAddress}");
                return true;
            });

            OP.Subscribe($"{Name}GetFFUMaxSpeed2", (cmd, param) =>
            {
                if (int.TryParse((string)param[0], out var groupAddress))
                {
                    EV.PostWarningLog(Module, "Get FFU GetFFUMaxSpeed2.");
                    return false;
                }
                QueryMaxSpeed2();
                EV.PostInfoLog(Module, $"{Name}Get FFU GetFFUMaxSpeed2 {groupAddress}");
                return true;
            });

            OP.Subscribe($"{Name}GetFFUMaxSpeed3", (cmd, param) =>
            {
                if (int.TryParse((string)param[0], out var groupAddress))
                {
                    EV.PostWarningLog(Module, "Get FFU GetFFUMaxSpeed3.");
                    return false;
                }
                QueryMaxSpeed3();
                EV.PostInfoLog(Module, $"{Name}Get FFU GetFFUMaxSpeed3 {groupAddress}");
                return true;
            });


            OP.Subscribe($"{Name}SetFFUReset", (cmd, param) =>
            {
                if (int.TryParse((string)param[0], out var groupAddress))
                {
                    EV.PostWarningLog(Module, "Set FFU Reset.");
                    return false;
                }
                ResetDevice();
                EV.PostInfoLog(Module, $"{Name}Set FFU Reset {groupAddress}");
                return true;
            });

            OP.Subscribe($"{Name}.SetFFUSpeedBySC", (cmd, param) =>
            {
                int speed = SC.GetValue<int>("System.FFUFanSpeedSetPoint");
                SetSpeed(speed);
                EV.PostInfoLog(Module, $"Set {Name} speed");
                return true;
            });
            ConnectionManager.Instance.Subscribe($"{Name}", this);

            return true;
        }

        private bool OnTimer()
        {
            try
            {
                _connection.MonitorTimeout();

                if (!_connection.IsConnected)//  || _connection.IsCommunicationError
                {
                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
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
                HandlerBase handler = null;
                if (!_connection.IsBusy)
                {
                    lock (_locker)
                    {
                        if (_lstHandler.Count == 0 && _activeMonitorStatus)
                        {
                              QuerySpeed();
                        }

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
                return true;

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

                _trigPumpOnOff.CLK = _isOn;
                if (_trigPumpOnOff.R)
                {
                    EV.PostInfoLog(Module, $"{Module}.{Name} is on");
                }

                if (_trigPumpOnOff.T)
                {
                    EV.PostInfoLog(Module, $"{Module}.{Name} is off");
                }


                _trigCommunicationError.CLK = _connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        public void Terminate()
        {
            try
            {
                if (_connection != null)
                {
                    _connection.Disconnect();
                    _connection = null;
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            
        }

        public void Reset()
        {
            _trigError.RST = true;
            _trigOverTemp.RST = true;
            _trigWarningMessage.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"System.{Module}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;


        }

        public void SetPumpOnOff(bool isOn)
        {
            lock (_locker)
            {

            }
        }

        public void SetActiveMonitor(bool active)
        {
            _activeMonitorStatus = active;
        }


        internal void NoteOnOff(bool isOn)
        {
            _isOn = isOn;
        }


        public void SetSpeed(float speed)
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                if (_connection.IsBusy) _connection.ForceClear();

                List<byte> cmddata = new List<byte>() { 0x35, 0x40, 0x0, (byte)(speed * 250.0 / _nMaxSpeed) };
                byte checksum = ModRTU_CRC(cmddata.ToArray());
                cmddata.Add(checksum);
                _connection.SendMessage(cmddata.ToArray());
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

    }
}
