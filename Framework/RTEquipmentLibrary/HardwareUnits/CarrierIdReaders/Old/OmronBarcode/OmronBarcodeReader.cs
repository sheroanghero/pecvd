using System;
using System.Threading;
using System.Threading.Tasks;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Utilities;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.OmronBarcode
{
    public class OmronBarcodeReader : CarrierIdReader
    {
 
        private AsyncSerialPort _port;

        private string _scannerName;

        //private int _scan;

        private int _timeout;

        private int _retryTime;

        DeviceTimer _scanTimer = new DeviceTimer();

        private bool _cmdScan;
        private bool _cmdStopScan;
        //private bool _msgError;
        private bool _msgScanned;

        public string _scanResult;

        private object _locker = new object();

        private bool _isScanning;

        public OmronBarcodeReader(string module, string scannerName, string portName, int timeout, int retryTime):base(module, scannerName)
        {
            Module = module;
            Name = scannerName;
            _scannerName = scannerName;
            _timeout = timeout*1000;
            _retryTime = retryTime;

            _port = new AsyncSerialPort(portName, 9600, 8  );
            _port.EnableLog = true;

            _port.OnDataChanged += _port_OnDataReceived;
            _port.OnErrorHappened += _port_OnErrorHappened;
        }


        public override bool Initialize()
        {
            Task.Factory.StartNew(() =>
            {
                int count = SC.ContainsItem("System.ComPortRetryCount") ?  SC.GetValue<int>("System.ComPortRetryCount") : 50;
                int sleep = SC.ContainsItem("System.ComPortRetryDelayTime") ? SC.GetValue<int>("System.ComPortRetryDelayTime") : 1;
                if (sleep <= 0 || sleep > 10)
                    sleep = 2;

                int retry = 0;
                do
                {
                    if (_port.Open())
                    {
                        //LOG.Write($"Connected with {Module}.{Name} .");
                        EV.PostInfoLog(Module, $"Connected with {Module}.{Name} .");
                        break;
                    }

                    if (count>0 &&  retry++ > count)
                    {
                        LOG.Write($"Retry connect {Module}.{Name} stop retry.");
                        EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name} .");
                        break;
                    }
                    {
                        Thread.Sleep(sleep * 1000);
                        LOG.Write($"Retry connect {Module}.{Name} for the {retry + 1} time.");
                    }
                } while (true);
 
            });
//            RetryInstance.Instance().Execute<bool>(
//                ()=> _port.Open(),
//                SC.GetValue<int>("System.ComPortRetryDelayTime"),
//                SC.GetValue<int>("System.ComPortRetryCount"),
//                true
//                );
            
            return true;
        }
        public override bool Read(out string reason)
        {
            Scan();
            reason = "";
            return true;
        }


        private bool InvokeScan(string arg1, object[] arg2)
        {
            Scan();
            return true;
        }

        private bool InvokeStopScan(string arg1, object[] arg2)
        {
            StopScan();

            return true;
        }

        public void Scan()
        {
            if (!_port.IsOpen())
            {
                EV.PostWarningLog(Module, _scannerName + " not open, can not scan");
                return;
            }

            lock (_locker)
            {
                if (!_isScanning)
                {
                    _scanTimer.Start(0);
                    _cmdScan = true;
                    _cmdStopScan = false;
                }
            }
        }

        public void StopScan()
        {
            if (!_port.IsOpen())
            {
                EV.PostWarningLog(Module,  _scannerName + " not open, can not stop scan");
                return;
            }

            lock (_locker)
            {
                if (_isScanning)
                {
                    _cmdScan = false;
                    _cmdStopScan = true;
                }
            }
        }

        private void _port_OnErrorHappened(string obj)
        {
            EV.PostWarningLog(Module, _scannerName + " error, " + obj);
 
        }

        private void _port_OnDataReceived(string obj)
        {
            lock (_locker)
            {
                if (_isScanning)
                {
                    _scanResult = obj.Replace("\r","");
                    _msgScanned = true;
                }
            }
        }

        public override void Monitor()
        {
            lock (_locker)
            {
                if (_isScanning)
                {
                    if (_cmdStopScan)
                    {
                        _cmdStopScan = false;
                        _isScanning = false;
                        PerformStopTriggerScan();
                    }
                    else
                    {
                        if (_msgScanned)
                        {
                            _msgScanned = false;
                            _isScanning = false;
                            ReadOk(_scanResult);
                        }
                        else
                        {
                            if (_scanTimer.GetElapseTime() > _timeout)
                            {
                                _isScanning = false;

                                EV.PostWarningLog(Module, _scannerName + " error, timeout");
                                ReadFailed();
                            }
                        }
                    }
                }
                else
                {
                    if (_cmdScan)
                    {
                        _cmdScan = false;
                        _isScanning = true;
                        PerformTriggerScan();
                        _scanTimer.Start(0);
                    }
                }
            }
        }

        public override void Terminate()
        {
            _port.Close();
        }

        public override void Reset()
        {
            if (!_port.IsOpen())
            {
                if (_port.Open())
                {
                    EV.PostMessage("System", EventEnum.GeneralInfo, _scannerName + " opened, ");
                }
            }
        }
 
 

        private void PerformTriggerScan()
        {
            _port.Write(new byte[] { 0x1B, 0x5A, 0x0D });
        }

        private void PerformStopTriggerScan()
        {
            //_port.Write(new byte[] { 22, 85, 13 });
        }
    }
}

