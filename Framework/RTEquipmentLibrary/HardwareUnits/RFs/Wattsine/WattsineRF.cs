using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.Wattsine
{
    public class WattsineRF : RfPowerBase, IConnection
    {
        public string Address { get; }
        public override bool IsConnected { get; }
        public bool Connect()
        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }

        public string PortStatus { get; set; } = "Closed";

        private WattsineRFConnection _connection;
        public WattsineRFConnection Connection
        {
            get { return _connection; }
        }

        public string PortName { get; set; }

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

        public List<IOResponse> IOResponseList { get; set; } = new List<IOResponse>();


        private object _locker = new object();

        //private bool _enableLog;
        private string _scRoot;

        public WattsineRF(string module, string name, string scRoot, string portName) : base(module, name)
        {
            _scRoot = scRoot;
            PortName = portName;


        }

        public override bool Initialize()
        {
            base.Initialize();

            //ResetPropertiesAndResponses();

            if (_connection != null && _connection.IsConnected  )
                return true;

            if (_connection != null && _connection.IsConnected)
                _connection.Disconnect();

            PortName = PortName;

            //_enableLog = SC.GetValue<bool>($"{_scRoot}.{Module}.{Name}.EnableLogMessage");
            _connection = new WattsineRFConnection(PortName);
            //_connection.EnableLog(_enableLog);

            //SetLocalAdress(1);
            _lstMonitorHandler.AddLast(new WattsineRFWriteRegisterHandler(this, "00,11", $"00,01"));


            if (_connection.Connect())
            {
                PortStatus = "Open";
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(6000, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            return true;
        }

        private bool OnTimer()
        {
            try
            {
                //_connection.MonitorTimeout();

                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        _connection.SetPortAddress(SC.GetStringValue($"{ScBasePath}.{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {

                        }
                    }
                    return true;
                }

                HandlerBase handler = null;
                if (!_connection.IsBusy)
                {
                    lock (_locker)
                    {
                        if (_lstHandler.Count == 0)
                        {
                            foreach (var monitorHandler in _lstMonitorHandler)
                            {
                                _lstHandler.AddLast(monitorHandler);
                            }

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
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        public override void Monitor()
        {
            try
            {
                //_connection.EnableLog(_enableLog);


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

        public override void Reset()
        {
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            //_enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;

            base.Reset();
        }


        #region Command Functions

        /// <summary>
        /// 系统复位
        /// </summary>
        internal void SystemReset()
        {
            _lstHandler.AddLast(new WattsineRFWriteRegisterHandler(this, "00,10", "00,01"));
        }

        internal void SetLocalAdress(int adress)
        {
            string addres16 = string.Format("{0:X1}", adress);
            _lstHandler.AddLast(new WattsineRFWriteRegisterHandler(this, "00,11", $"00,{addres16}"));
        }

        internal void SetPowerONOFF(bool isOn)
        {
            _lstHandler.AddLast(new WattsineRFWriteRegisterHandler(this, "00,80", $"00,{(isOn ? "01" : "00")}"));
        }

        internal void SetRfONOFF(bool isOn)
        {
            _lstHandler.AddLast(new WattsineRFWriteRegisterHandler(this, "00,81", $"00,{(isOn ? "01" : "00")}"));
        }

        internal void ReadAlarm()
        {
            _lstHandler.AddLast(new WattsineRFReadRegisterHandler(this, "00,A0", $"00,00"));
        }

        /// <summary>
        /// 读取设备
        /// 整机输出功率
        /// 整机反射功率
        /// 整机驻波比值
        /// </summary>
        internal void ReadDeviceInfo()
        {
            _lstHandler.AddLast(new WattsineRFReadRegisterHandler(this, "00,A1", $"00,03"));
        }

        #endregion

        #region Properties
        public string Error { get; private set; }

        public int Alarm { get; private set; }

        public int OutputPower { get; set; }

        public int ReflectedPower { get; set; }

        public int IsolatorPower { get; set; }


        #endregion


    }
}
