using System;
using System.Collections;
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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temperatures.AEs
{
    public class AEOR4000T : BaseDevice, IConnection, IDevice
    {
        private AEOR4000TConnection _connection;
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
        public double AETemp1 { get; set; }
        public double AETemp2 { get; set; }
        public double AETemp3 { get; set; }

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

        public AEOR4000T(string module, string name, string scRoot) : base(module, name, name, name)
        {
            _scRoot = scRoot;
            _activeMonitorStatus = true;
        }

        ~AEOR4000T()
        {
            _connection.Disconnect();

        }

        //public TempOmron(string module, string name, string scRoot) : base(module, name, name, name)
        //{
        //    //tempCount = count;
        //    _scRoot = scRoot;
        //    _activeMonitorStatus = true;
        //}

        public void QueryTemp()
        {
            _lstHandler.AddLast(new AETempReadCommandHandler(this, "OUT", "1"));
        }


        public void ResetDevice()
        {

        }

        public void QueryError()
        {
            EV.PostInfoLog(Module, "Query error");
        }

        public bool Initialize()
        {
            // string portName = SC.GetStringValue($"{_scRoot}.{Module}.{Name}.Address");
            string portName = SC.GetStringValue($"{Name}.Address");
            Address = portName;
            //int address = SC.GetValue<int>($"{_scRoot}.{Name}.DeviceAddress");

            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");

            _connection = new AEOR4000TConnection(portName);
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
            //调换 2 3 midle outter
            DATA.Subscribe($"{Module}.{Name}.AETemp1", () => AETemp1);
            DATA.Subscribe($"{Module}.{Name}.AETemp2", () => AETemp3);
            DATA.Subscribe($"{Module}.{Name}.AETemp3", () => AETemp2);
            //DATA.Subscribe($"{Module}.{Name}.SettingTemp", () => SettingTemp);
            //OP.Subscribe($"{Module}.{Name}.WriteConfigData", SetCofig);

            //
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

                    //
                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        //_connection.SetPortAddress(SC.GetStringValue($"{_scRoot}.{Name}.Address"));

                        if (!_connection.Connect())
                        {
                            //qbh 20220330
                            //OP.DoOperation($"{Module}.PMAETemp.SetPyroCommunicationError", true);
                            LOG.Write($"{Module}.PMAETemp.SetPyroCommunicationError");

                            //在Process模式和PreProcess模式下AE掉线直接Abort
                            string moduleStatus = DATA.Poll($"{Module}.Status").ToString();
                            if (moduleStatus == "PreProcess" || moduleStatus == "Process")
                            {
                                OP.DoOperation($"{Module}.Abort");
                                EV.PostWarningLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                            }
                            else
                            {
                                EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                            }
                        }
                        else
                        {
                            //qbh 20220330
                            //OP.DoOperation($"{Module}.PMAETemp.SetPyroCommunicationError", false);
                            LOG.Write($"{Module}.PMAETemp.SetPyroCommunicationError,reconnected.");
                        }
                    }

                    _connection.ForceClear();
                    return true;

                }

                HandlerBase handler = null;
                //if (!_connection.IsBusy)
                //{
                lock (_locker)
                {
                    if (_lstHandler.Count == 0)
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
        public void ParseCommandInfo(string command, string Message)
        {
            switch (command)
            {
                case "OUT":
                    {
                        if (Message != null)
                        {
                            if (Message.Contains(" "))
                            {
                                var strs = Message.Split(' ');
                                AETemp1 = Convert.ToDouble(strs[0]);
                                AETemp2 = Convert.ToDouble(strs[1]);
                                AETemp3 = Convert.ToDouble(strs[2]);

                                //AETemp1 = TempFilter2("AETemp1", Convert.ToDouble(strs[0]));
                                //AETemp2 = TempFilter2("AETemp2", Convert.ToDouble(strs[1]));
                                //AETemp3 = TempFilter2("AETemp3", Convert.ToDouble(strs[2]));
                            }
                        }
                    }
                    break;
            }
        }

        #region 滤波
        //qbh 20220309
        const int iQueCap = 10;
        static Queue qWafInner = new Queue(iQueCap);
        //static Queue qSusInner = new Queue(iQueCap);

        static Queue qWafMiddle = new Queue(iQueCap);
        //static Queue qSusMiddle = new Queue(iQueCap);

        static Queue qWafOuter = new Queue(iQueCap);
        //static Queue qSusOuter = new Queue(iQueCap);

        const double dbThres = 20.0;
        public static double TempFilter(string sName, double dbNewTemp)
        {
            //
            Queue qTempData = new Queue();

            switch (sName)
            {
                case "AETemp1":
                    qTempData = qWafInner;
                    break;
                case "AETemp2":
                    qTempData = qWafMiddle;
                    break;
                case "AETemp3":
                    qTempData = qWafOuter;
                    break;

                default:
                    break;
            }

            //
            qTempData.Enqueue(dbNewTemp);
            //           
            if (qTempData.Count < iQueCap)
            {
                return dbNewTemp;
            }
            else
            {
                //
                while (qTempData.Count > iQueCap)
                {
                    qTempData.Dequeue();
                }
                //
                List<double> liTemp = new List<double>();
                object[] objs = qTempData.ToArray();
                foreach (object obj in objs)
                {
                    liTemp.Add((double)obj);
                }
                liTemp.Sort();
                if (liTemp.Count > 0)
                {
                    liTemp.RemoveAt(0);
                }
                liTemp.Reverse();
                if (liTemp.Count > 0)
                {
                    liTemp.RemoveAt(0);
                }
                //
                double dbAvg = 0.0;
                foreach (double data in liTemp)
                {
                    dbAvg += data;
                }
                dbAvg /= liTemp.Count;


                //
                return dbAvg;
            }
            //            
        }

        /// <summary>
        /// 均方根值法
        /// </summary>
        /// <param name="sName"></param>
        /// <param name="dbNewTemp"></param>
        /// <returns></returns>
        public static double TempFilter2(string sName, double dbNewTemp)
        {
            //
            Queue qTempData = new Queue();

            switch (sName)
            {
                case "AETemp1":
                    qTempData = qWafInner;
                    break;
                case "AETemp2":
                    qTempData = qWafMiddle;
                    break;
                case "AETemp3":
                    qTempData = qWafOuter;
                    break;

                default:
                    break;
            }

            //
            qTempData.Enqueue(dbNewTemp);
            //           
            if (qTempData.Count < iQueCap)
            {
                return dbNewTemp;
            }
            else
            {
                //
                while (qTempData.Count > iQueCap)
                {
                    qTempData.Dequeue();
                }
                //
                List<double> liTemp = new List<double>();
                object[] objs = qTempData.ToArray();
                foreach (object obj in objs)
                {
                    liTemp.Add((double)obj);
                }
                liTemp.Sort();
                if (liTemp.Count > 0)
                {
                    liTemp.RemoveAt(0);
                }
                liTemp.Reverse();
                if (liTemp.Count > 0)
                {
                    liTemp.RemoveAt(0);
                }
                //均方根
                double dbAvg = 0.0;
                foreach (double data in liTemp)
                {
                    dbAvg += data * data;
                }
                dbAvg /= liTemp.Count;
                dbAvg = Math.Sqrt(dbAvg);

                //
                return dbAvg;
            }
            //            
        }

        #endregion

        public void Monitor()
        {
            try
            {
                _connection.EnableLog(_enableLog);

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

        public void Reset()
        {
            _connection.SetCommunicationError(false, "");
            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");
            _trigCommunicationError.RST = true;
            _trigRetryConnect.RST = true;
        }

        public void SetActiveMonitor(bool active)
        {
            _activeMonitorStatus = active;
        }

        public void SetErrorCode(int errorCode)
        {
            _errorCode = errorCode;
        }

        public void Terminate()
        {
            _connection.Disconnect();
        }


    }
}
