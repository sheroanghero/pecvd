using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using HslCommunication;
using HslCommunication.ModBus;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.CarrierIDReaderBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.MyModbus
{
    public class MyRfidReaderModbusTcp : CIDReaderBaseDevice, IConnection
    {
        public MyRfidReaderModbusTcp(string module, string name, string scRoot, LoadPortBaseDevice lp = null, int offset = 0, int length = 16, int readerIndex = 1)
            : base(module, name, lp, readerIndex)
        {
            _scRoot = scRoot;
            _address = SC.GetStringValue($"{_scRoot}.{Name}.Address").Split(':').FirstOrDefault();

            Offset = offset;
            Length = length;
            _readerClient = new ModbusTcpNet(_address);
            _isConnected = false;
            Reset();

            _thread = new PeriodicJob(5000, OnTimerMonitor, $"{_scRoot}.{Name} MonitorHandler", true);
        }

        private bool OnTimerMonitor()
        {
            if (!IsConnected) return true;

            lock (_locker)
            {
                try
                {
                    Testlink();
                }
                catch (Exception ex)
                {

                }
                return true;
            }


        }
        private object _locker = new object();
        private PeriodicJob _thread;
        private ModbusTcpNet _readerClient;

        private string _scRoot;
        private string _address;
        private int Offset;
        private int Length;

        private DateTime _startTime;
        private int _retryTime;

        private bool _isConnected;

        public string Address => _address;

        public bool IsConnected => _isConnected;

        public bool Connect()
        {
            lock (_locker)
            {
                _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
                _address = _address.Split(':').FirstOrDefault();
                _readerClient.IpAddress = _address;
                _isConnected = _readerClient.ConnectServer().IsSuccess;
                return true;
            }
        }

        public bool Disconnect()
        {
            lock (_locker)
            {
                _readerClient.ConnectClose();
                _isConnected = false;
                return true;
            }
        }
        private int _linktestFailTime = 0;
        private bool Testlink()
        {
            var result = _readerClient?.Read("2", 1);
            if (!result.IsSuccess)
            {
                _linktestFailTime++;
                if(_linktestFailTime >10)
                {
                    LOG.Write($"{Name} link test continue failed times reach 10,content:{result.Content},message:{result.Message}.");
                    _linktestFailTime = 0;
                }

            }
            else
                _linktestFailTime = 0;
            return result.IsSuccess;
        }




        protected override bool fStartReset(object[] param)
        {
            lock (_locker)
            {
                _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");

                if (!_isConnected)
                {
                    _isConnected = _readerClient.ConnectServer().IsSuccess;
                    if(_isConnected)
                        EV.PostInfoLog("RFIDReader", $"{Name} success to connect RFID reader.");
                }

                _isConnected = Testlink();
                _retryTime = 0;
                while (!_isConnected)
                {
                    EV.PostInfoLog("RFIDReader", $"{Name} start to re-connect RFID reader.");
                    _readerClient.IpAddress = SC.GetStringValue($"{_scRoot}.{Name}.Address").Split(':').FirstOrDefault();
                    _readerClient.ConnectClose();
                    Thread.Sleep(1000);
                    _isConnected = _readerClient.ConnectServer().IsSuccess;
                    if (_isConnected)
                        EV.PostInfoLog("RFIDReader", $"{Name} success to re-connect RFID reader.");
                    _retryTime++;

                    if (_retryTime > 5)
                    {
                        EV.PostWarningLog("RFIDReader", $"{Name} failed to re-connect RFID reader.");
                        break;
                    }
                }










                if (_isConnected)
                {
                    LOG.Write($"Success to connect to {Name} on address:{_address}.");
                }
                else
                {
                    LOG.Write($"Failed to connect to {Name} on address:{_address}.");
                }
                return true;
            }
        }

        protected override bool fStartReadCarrierID(object[] param)
        {
            int offset = 0;
            int length = 16;
            if (param != null)
            {
                offset = (int)param[0];
                length = (int)param[1];
            }
            _startTime = DateTime.Now;
            _retryTime = 0;

            CarrierIDStartPage = offset + 1;
            CarrierIDPageLength = length;
            EV.PostInfoLog(Module, $"{Name} Start to read RFID from startpage:{CarrierIDStartPage}.with length:{CarrierIDPageLength}");

            return true;
        }



        protected override bool fMonitorReadCarrierID(object[] param)
        {

            if (DateTime.Now - _startTime > TimeSpan.FromSeconds(30))
            {
                OnCarrierIDReadFailed("Timeout");
                return true;
            }
            lock (_locker)
            {
                if (_retryTime < 10)
                {
                    EV.PostInfoLog(Module, $"{Name} read the {_retryTime + 1}st time RFID from startpage:{CarrierIDStartPage}.with length:{CarrierIDPageLength}");

                    ushort startaddress = (ushort)(5 + (CarrierIDStartPage - 1) * 4);
                    ushort datalength = (ushort)(CarrierIDPageLength * 4);
                    if (datalength > 64) datalength = 64;
                    OperateResult<string> ret1;
                    OperateResult<string> ret2;
                    if (datalength > 32)
                    {

                        ret1 = _readerClient.ReadString(startaddress.ToString(), 32);
                        ret2 = _readerClient.ReadString((startaddress + 32).ToString(), Convert.ToUInt16(datalength - 32));
                        if (!ret1.IsSuccess || !ret2.IsSuccess)
                        {
                            EV.PostInfoLog(Module, $"{Name} Start to retry {_retryTime + 1} RFID read from startpage:{CarrierIDStartPage}.with length:{CarrierIDPageLength}");
                            _retryTime++;
                            _startTime = DateTime.Now;
                            if (ret1.Message.Contains("连接") && ret1.Message.Contains("失败"))
                            {
                                _readerClient.IpAddress = _address;
                                _readerClient.ConnectClose();
                                Thread.Sleep(500);
                                _isConnected = _readerClient.ConnectServer().IsSuccess;

                                if (_isConnected)
                                {
                                    EV.PostInfoLog("CarrierIDReader", $"Success to connect to {Name} on address:{_address}.");
                                }
                                else
                                {
                                    EV.PostInfoLog("CarrierIDReader", $"Failed to connect to {Name} on address:{_address}.");
                                }
                            }
                            return false;
                        }
                        string rawcid = ret1.Content + ret2.Content;
                        string cid = rawcid.Trim('\0').Split('\0').FirstOrDefault().Trim();
                        OnCarrierIDRead(cid);
                        return true;
                    }
                    var ret = _readerClient.ReadString(startaddress.ToString(), datalength);
                    if (ret.IsSuccess)
                    {
                        string cid = ret.Content.Trim('\0').Split('\0').FirstOrDefault().Trim();
                        OnCarrierIDRead(cid);
                        return true;
                    }


                    EV.PostInfoLog(Module, $"{Name} Start to retry {_retryTime + 1} RFID read from startpage:{CarrierIDStartPage}.with length:{CarrierIDPageLength}");
                    _retryTime++;
                    _startTime = DateTime.Now;

                    if (ret.Message.Contains("连接") && ret.Message.Contains("失败"))
                    {
                        _readerClient.IpAddress = _address;
                        _readerClient.ConnectClose();
                        Thread.Sleep(500);
                        _isConnected = _readerClient.ConnectServer().IsSuccess;

                        if (_isConnected)
                        {
                            EV.PostInfoLog("CarrierIDReader", $"Success to connect to {Name} on address:{_address}.");
                        }
                        else
                        {
                            EV.PostInfoLog("CarrierIDReader", $"Failed to connect to {Name} on address:{_address}.");
                        }
                    }


                    return false;
                }
            }
            OnCarrierIDReadFailed("RetryTimeOut");
            return true;
        }


        protected override bool fStartWriteCarrierID(object[] param)
        {
            try
            {
                _startTime = DateTime.Now;
                _retryTime = 0;
                CarrierIDToBeWriten = param[0].ToString();
                int offset = 0;
                int length = 16;

                if (param.Length == 3)
                {
                    offset = (int)param[1];
                    length = (int)param[2];
                }
                CarrierIDStartPage = offset + 1;
                CarrierIDPageLength = length;
                EV.PostInfoLog(Module, $"{Name} start to write RFID:{CarrierIDToBeWriten} from startpage:{CarrierIDStartPage}.with length:{CarrierIDPageLength}");
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        protected override bool fMonitorWriteCarrierID(object[] param)
        {

            if (DateTime.Now - _startTime > TimeSpan.FromSeconds(30))
            {
                OnCarrierIDWriteFailed("Timeout");
                return true;
            }
            lock (_locker)
            {
                if (_retryTime < 5)
                {
                    EV.PostInfoLog(Module, $"{Name} write the {_retryTime + 1}st time RFID from startpage:{CarrierIDStartPage}.with length:{CarrierIDPageLength}");

                    ushort startaddress = (ushort)(5 + (CarrierIDStartPage - 1) * 4);

                    OperateResult ret1;
                    OperateResult ret2;
                    if (CarrierIDToBeWriten.Length >= 64)
                    {
                        string cidpart1 = CarrierIDToBeWriten.Substring(0, 64);
                        ret1 = _readerClient.Write(startaddress.ToString(), cidpart1);

                        string cidpart2 = CarrierIDToBeWriten.Length >= 128 ?
                            CarrierIDToBeWriten.Substring(64, 64) : (CarrierIDToBeWriten.Substring(64) + "\0");
                        ret2 = _readerClient.Write((startaddress + 32).ToString(), cidpart2);
                        if (ret1.IsSuccess && ret2.IsSuccess)
                        {
                            OnCarrierIDWrite();
                            return true;
                        }
                        EV.PostInfoLog(Module, $"{Name} Start to retry {_retryTime + 1} RFID write from startpage:{CarrierIDStartPage}.with length:{CarrierIDPageLength}");
                        _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
                        if (ret1.Message.Contains("连接") && ret1.Message.Contains("失败"))
                        {
                            _readerClient.IpAddress = _address;
                            _readerClient.ConnectClose();
                            Thread.Sleep(500);
                            _isConnected = _readerClient.ConnectServer().IsSuccess;

                            if (_isConnected)
                            {
                                EV.PostInfoLog("CarrierIDReader", $"Success to connect to {Name} on address:{_address}.");
                            }
                            else
                            {
                                EV.PostInfoLog("CarrierIDReader", $"Failed to connect to {Name} on address:{_address}.");
                            }
                        }

                        _retryTime++;
                        _startTime = DateTime.Now;
                        return false;
                    }


                    CarrierIDToBeWriten += "\0";

                    var ret = _readerClient.Write(startaddress.ToString(), CarrierIDToBeWriten);
                    if (ret.IsSuccess)
                    {
                        OnCarrierIDWrite();
                        return true;
                    }
                    _address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
                    if (ret.Message.Contains("连接") && ret.Message.Contains("失败"))
                    {
                        _readerClient.IpAddress = _address;
                        _readerClient.ConnectClose();
                        Thread.Sleep(500);
                        _isConnected = _readerClient.ConnectServer().IsSuccess;

                        if (_isConnected)
                        {
                            EV.PostInfoLog("CarrierIDReader", $"Success to connect to {Name} on address:{_address}.");
                        }
                        else
                        {
                            EV.PostInfoLog("CarrierIDReader", $"Failed to connect to {Name} on address:{_address}.");
                        }
                    }

                    EV.PostInfoLog(Module, $"{Name} Start to retry {_retryTime + 1} RFID write from startpage:{CarrierIDStartPage}.with length:{CarrierIDPageLength}");
                    _retryTime++;
                    _startTime = DateTime.Now;
                    return false;
                }
            }
            OnCarrierIDWriteFailed("RetryTimeOut");
            return true;

        }

        public override void Terminate()
        {
            _readerClient.ConnectClose();
            LOG.Write($"{Name} close connection succefully.");
        }



    }
}
