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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Temperatures.Eurotherms
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
    public class EurothermP116 : BaseDevice, IConnection,IDevice
    {
        private EurothermP116Connection _connection;
        private bool _activeMonitorStatus;
        private int _errorCode;
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private PeriodicJob _thread;
        //private int tempCount = 1;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private object _locker = new object();

        private bool _enableLog = true;

        private string _scRoot;

        //private int _actualTempValue = 999;
        //private int _settingTempValue = 888;

        public int moduleCount { get; set; }

 
        private int _tempWarningMaxDiff = 1;        //报警最大温度（实际温度与设定温度之间的差值）
        //private R_TRIG[] TempWarningTrig = new R_TRIG[12];   //温度报警
        //string[] WarningInfo = new string[12] { "HeatTMAPanel","HeatTCSPanel","OuterPanel","MidPanel","InnerPanel","HeatShowerOuter", "HeatShowerInner", "HeatShowerMid", "HeatDptPanel","HeatLeakSource","HeatLeakBypass","HeatDptGate"};

        //public float[] ActualTemp = new float[12];

        //public float[] SettingTemp = new float[12];

        public float fPVInValue, fTargetSP;
        public int iAM = -1;

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

        public EurothermP116(string module, string name, string scRoot) : base(module, name, name, name)
        {
            _scRoot = scRoot;
            _activeMonitorStatus = true;
        }

        ~EurothermP116()
        {
            _connection.Disconnect();

        }

        //public P116PIDTC(string module, string name, string scRoot) : base(module, name, name, name)
        //{
        //    //tempCount = count;
        //    _scRoot = scRoot;
        //    _activeMonitorStatus = true;
        //}

        public void QueryTemp()
        {          
            _lstHandler.AddLast(new P116PIDTCQueryHandler(this, "QueryPVInValue", 0x01, 0x00, 0x01, 0x00, 0x01));
            _lstHandler.AddLast(new P116PIDTCQueryHandler(this, "QueryTargetSP", 0x01, 0x00, 0x02, 0x00, 0x01));
            _lstHandler.AddLast(new P116PIDTCQueryHandler(this, "QueryAM", 0x01, 0x01, 0x11, 0x00, 0x01));
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
            string portName =SC.GetStringValue($"{Name}.Address");
            Address = portName;
            //int address = SC.GetValue<int>($"{_scRoot}.{Name}.DeviceAddress");
            moduleCount= SC.GetValue<int>($"{Name}.Modules");
            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");

            _connection = new EurothermP116Connection(portName);
            _connection.EnableLog(_enableLog);

            _tempWarningMaxDiff = SC.GetValue<int>($"{Name}.TempWarnMaxDiff");

            //
            fPVInValue = fTargetSP = 0.0f;
            iAM = -1;


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

            DATA.Subscribe($"{Module}.{Name}.PVInValue", () => fPVInValue);
            DATA.Subscribe($"{Module}.{Name}.TargetSP", () => fTargetSP) ;
            DATA.Subscribe($"{Module}.{Name}.AM", () => iAM);

            OP.Subscribe($"{Module}.{Name}.WritePVInValue", SetPVInValue);
            OP.Subscribe($"{Module}.{Name}.WriteTargetSP", SetTargetSP);

            OP.Subscribe($"{Module}.{Name}.WriteAM", SetAM); //0-Auto,1-Manual,2-Off            

            ConnectionManager.Instance.Subscribe($"{Name}", this);

            return true;
        }
        public int _connecteTimes { get; set; }
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
                        //_connection.SetPortAddress(SC.GetStringValue($"{_scRoot}.{Name}.Address"));
                        _connection.SetPortAddress(SC.GetStringValue($"{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                    }

                    //if (_trigRetryConnect.CLK)
                    //{
                    //    if (!_connection.Connect())
                    //    {
                    //        if (_trigRetryConnect.Q)
                    //        {

                    //            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                    //        }
                    //    }
                    //    else
                    //    {
                    //        EV.PostAlarmLog(Module, $"Retry connect succee with {_connection.Address}, {Module}.{Name}");
                    //        _trigRetryConnect.CLK = false;
                    //    }
                    //    Thread.Sleep(3000);
                    //}
                    if (_connecteTimes++ < 3)
                        _connection.ForceClear();
                    else _connecteTimes= 4;
                    return true;
                }

                HandlerBase handler = null;
                //if (!_connection.IsBusy)
                //{
                lock (_locker)
                {
                    if (_lstHandler.Count == 0 && !IsConfig)
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

        public void Monitor()
        {
            try
            {
                _connection.EnableLog(_enableLog);

                if (_connecteTimes < 4) return;
                _trigCommunicationError.CLK = _connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    //for (int i = 0; i < 12; i++)
                    //{
                    //    ActualTemp[i] = 0;
                    //    SettingTemp[i] = 0;
                    //}
                    fPVInValue = 0f;
                    fTargetSP = 0f;
                    iAM = -1;
                    EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                }

                //MonitorWarning();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }


        //private void MonitorWarning()
        //{
        //    for (int i = 0; i < 12; i++)
        //    {
        //        TempWarningTrig[i].CLK = SettingTemp[i] > 0 && _tempWarningMaxDiff > 0 && Math.Abs(ActualTemp[i] - SettingTemp[i]) > _tempWarningMaxDiff;
        //        if (TempWarningTrig[i].Q)
        //        {
        //            EV.PostWarningLog(Module, $"{WarningInfo[i]},the difference between the actual value and the set value is too large");
        //        }
        //    }
        //}

        public bool IsConfig { get; set; }

        public bool SetPVInValue(string cmd, object [] obj)//设置单个地址 0x0001
        {
            IsConfig = true;
            _lstHandler.Clear();
            lock (_locker)
            {

                if (obj.Length > 0)
                {
                    byte DeviceAddress = 0x01;
                    int adress = 0x0001;
                    byte addressH = Convert.ToByte((adress >> 8) & 0xff); ;
                    byte addressL = Convert.ToByte(adress & 0xff);

                    //var floatdata = Convert.ToSingle(objs[1]) * 10;
                    //var data = Convert.ToInt16(floatdata);
                    var data = Convert.ToInt16(obj[0]);
                    byte dataH = Convert.ToByte((data >> 8) & 0xff);
                    byte dataL = Convert.ToByte(data & 0xff);
                    _lstHandler.AddLast(new P116PIDTCWriteSingleHandler(this, "WritePVInValue", DeviceAddress, addressH, addressL, dataH, dataL));
                }
            }
            return true;
        }
        public bool SetTargetSP(string cmd, object[] obj)//设置单个地址 0x0002
        {
            IsConfig = true;
            _lstHandler.Clear();
            lock (_locker)
            {
                if (obj.Length > 0)
                {
                    byte DeviceAddress = 0x01;
                    int adress = 0x0002;
                    byte addressH = Convert.ToByte((adress >> 8) & 0xff); ;
                    byte addressL = Convert.ToByte(adress & 0xff);

                    var data = Convert.ToInt16(obj[0]) * 10;//10倍值
                    byte dataH = Convert.ToByte((data >> 8) & 0xff);
                    byte dataL = Convert.ToByte(data & 0xff);
                    _lstHandler.AddLast(new P116PIDTCWriteSingleHandler(this, "WriteTargetSP", DeviceAddress, addressH, addressL, dataH, dataL));
                }
            }
            return true;
        }
        public bool SetAM(string cmd, object[] obj)//设置单个地址 0x0111 = 273
        {
            IsConfig = true;
            _lstHandler.Clear();
            lock (_locker)
            {
                if (obj.Length > 0)
                {
                    byte DeviceAddress = 0x01;
                    int adress = 0x0111;
                    byte addressH = Convert.ToByte((adress >> 8) & 0xff); ;
                    byte addressL = Convert.ToByte(adress & 0xff);

                    var data = Convert.ToInt16(obj[0]);
                    if (data == 0)
                    {
                        //开启自动
                        byte dataH = Convert.ToByte((data >> 8) & 0xff);
                        byte dataL = Convert.ToByte(data & 0xff);

                        _lstHandler.AddLast(new P116PIDTCWriteSingleHandler(this, "WriteAM", DeviceAddress, addressH, addressL, dataH, dataL));
                    }
                    else if(data == 2)
                    {
                        //关闭输出

                        //A-M:address 273
                        data = 0x1;
                        byte dataH = Convert.ToByte((data >> 8) & 0xff);
                        byte dataL = Convert.ToByte(data & 0xff);

                        _lstHandler.AddLast(new P116PIDTCWriteSingleHandler(this, "WriteAM", DeviceAddress, addressH, addressL, dataH, dataL));

                        //OP: address 3
                        adress = 0x0003;
                        addressH = Convert.ToByte((adress >> 8) & 0xff); ;
                        addressL = Convert.ToByte(adress & 0xff);
                        data = 0x0;
                        dataH = Convert.ToByte((data >> 8) & 0xff);
                        dataL = Convert.ToByte(data & 0xff);
                        _lstHandler.AddLast(new P116PIDTCWriteSingleHandler(this, "WriteAM", DeviceAddress, addressH, addressL, dataH, dataL));
                    }
                    else
                    {
                        EV.PostWarningLog(Module, "Find wrong parameter when writing AM.");
                    }
                }
            }
            return true;
        }
        

        public void Reset()
        {
            _connecteTimes = 0;
            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;

            //for (int i = 0; i < 12; i++)
            //{
            //    TempWarningTrig[i].RST = true;
            //}
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
        public Dictionary<int, string> AbnormalCode = new Dictionary<int, string>()
        {
            { 1,"ncorrect function code, this machine does not correspond to the function code." },
            { 2,"Incorrect data address" },
            { 3,"Incorrect data." },
            { 4,"operation error" },
        };
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
        //public Dictionary<string, AdressInfo> _dicAdressInfo = new Dictionary<string, AdressInfo>()//配置最多32个数据
        //{
        //    {"WritePVInValue", new AdressInfo() { DeviceAddress=0x01, UnitAddress = 0x0001,Length = 1 } },//手册 8-7
        //    {"WriteTargetSP", new AdressInfo() { DeviceAddress=0x01, UnitAddress = 0x0002,Length = 1 } },
            

        //};
    }
}
