using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.SCCore;
using Aitex.Sorter.Common;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders
{
    public class OcrReader : BaseDevice, IDevice
    {
        public DeviceState State
        {
            get
            {
                if (!Initalized)
                {
                    return DeviceState.Unknown;
                }
                if (Error)
                {
                    return DeviceState.Error;
                }

                if (Busy)
                    return DeviceState.Busy;

                return DeviceState.Idle;
            }
        }

        public bool Initalized { get; set; }
        public Guid OCRGuid { get; set; }
        public bool Busy
        {
            get { return _handlers.Count > 0 || _foregroundHandler != null; }
        }

        public bool Error
        {
            get
            {
                return _commErr || ExeError || !_socket.IsConnected;
            }
        }

        public bool SocketIsConnected { set; get; }
        
        public bool ExeError { get; set; } = false;
        public bool ReadLaserMaker { get; set; }

        public string JobName { get; set; }
        public string LaserMaker { get; set; }

        public string LaserMark1 { get; set; }
        public string LaserMark1Score { get; set; }

        public string LaserMark1ReadTime { get; set; }
        public string T7Code { get; set; }
        public string LaserMark2 { get; set; }
        public string LaserMark2Score { get; set; }
        public string LaserMark2ReadTime { get; set; }
        public bool ReadOK { get; set; }

        public string ImageFileName { get; set; }
        public string ImageStorePath { get; set; }

        private static Object _locker = new Object();

        private AsyncSocket _socket;

        private IHandler _foregroundHandler = null;  //current handler

        private Queue<IHandler> _handlers = new Queue<IHandler>();

        public const string delimiter = "\r\n";

        private bool _commErr = false;
        private string _addr;

        //private bool _onlineInited;

        public OcrReader(string module, string name, string display, string deviceId, string address)
            : base(module, name, display, deviceId)
        {
            _addr = address;
            _socket = new AsyncSocket(address, delimiter);
            _socket.OnDataChanged += new AsyncSocket.MessageHandler(OnDataChanged);
            _socket.OnErrorHappened += new AsyncSocket.ErrorHandler(OnErrorHandler);

            Initalized = false;
        }

        public bool Initialize()
        {
            _socket.Connect(this._addr);

            DEVICE.Register(String.Format("{0}.{1}", Name,  "ReadWaferID"), (out string reason, int time, object[] param) =>
            {
                bool bLaser = bool.Parse((string)param[0]);
                string jobName = (string)param[1];
                bool ret = Read(bLaser, jobName, out reason);
                if (ret)
                {
                    reason = string.Format("{0}", Name, "读WaferID");
                    return true;
                }

                return false;
            });

            DATA.Subscribe(Name, "WRIDReaderState", () => State);
            DATA.Subscribe(Name, "WRIDReaderBusy", () => Busy);
            DATA.Subscribe(Name, "LaserMaker1", () => LaserMaker);
            DATA.Subscribe(Name, "LaserMaker2", () => T7Code);

            _handlers.Clear();
            Online(false, out _);
            Initalized = true;

            return true;
        }

        public void Terminate()
        {
        }
        //private int timerCount = 0;

        public void Monitor()
        {
            //if(timerCount ++>50)
            //{
            //    timerCount = 0;
            //    lock (_locker)
            //    {
            //        if (!Busy)
            //        {
            //            if (!_socket.IsHstConnected) _commErr = true;
            //        }
            //    }

            //}
            //if (!Busy)
            //{

            //    lock (_locker)
            //    {
            //        SocketIsConnected = _socket.IsConnected;
            //    }
            //}

        }

        public void Reset()
        {
            lock (_locker)
            {
                _foregroundHandler = null;
                _handlers.Clear();
            }
            if (_commErr || !_socket.IsConnected)
            {
                _commErr = false;               
                _socket.Connect(this._addr);
            }
            ExeError = false;
            Initalized = true;
        }


        #region Command
        public bool Read(bool bLaserMaker, string jobName, out string reason)
        {
            _handlers.Clear();
            if (string.IsNullOrEmpty(jobName))
            {
                reason = "Job is undefine.";
                return false;
            }

            
                _handlers.Enqueue(new handler(new OnlineHandler(this), false));   //offline
                _handlers.Enqueue(new handler(new LoadJobHandler(this), ConvetJobName(jobName))); //LoadJob
                _handlers.Enqueue(new handler(new OnlineHandler(this), true));   //online
            
            ReadOK = false;
            ReadLaserMaker = bLaserMaker;
            _handlers.Enqueue(new handler(new ReadHandler(this)));         //Read
            return execute(out reason);
        }

        public bool Read(bool bLaserMaker, out string reason)
        {
            _handlers.Clear();

            string[] jobs = GetJobName(bLaserMaker);


            if (jobs.Length == 0 || string.IsNullOrEmpty(jobs[0]))
            {
                reason = "Job is undefine";
                return false;
            }


            string jobName = ConvetJobName(jobs[0]);
            if (JobName != jobName)
            {
                _handlers.Enqueue(new handler(new OnlineHandler(this), false));   //offline
                _handlers.Enqueue(new handler(new LoadJobHandler(this), jobName)); //LoadJob
                _handlers.Enqueue(new handler(new OnlineHandler(this), true));   //online               
            }
            _handlers.Enqueue(new handler(new ReadHandler(this)));         //Read

            
            ReadOK = false;
            ReadLaserMaker = bLaserMaker;
            return execute(out reason);
        }
        public bool ReadOnHostMode(bool bLaserMaker, string[] jobnames, out string reason)
        {
            _handlers.Clear();
            ReadOK = false;
            string[] jobs = jobnames;

            if (jobs.Length == 0 || string.IsNullOrEmpty(jobs[0]))
            {
                reason = "Job is undefine";
                return false;
            }
                _handlers.Enqueue(new handler(new OnlineHandler(this), false));   //offline
                _handlers.Enqueue(new handler(new LoadJobHandler(this), ConvetJobName(jobnames[0]))); //LoadJob
                _handlers.Enqueue(new handler(new OnlineHandler(this), true));   //online               
          
            _handlers.Enqueue(new handler(new ReadHandler(this)));         //Read


            
            ReadLaserMaker = bLaserMaker;
            return execute(out reason);
        }

        public bool Read(out string reason)
        {
            reason = string.Empty;
            return execute(new handler(new ReadHandler(this)), out reason);
        }


        public bool ReadJobName(out string reason)
        {
            reason = string.Empty;
            return execute(new handler(new GetJobHandler(this)), out reason);
        }


        public bool Online(bool online, out string reason)
        {
            reason = string.Empty;
            return execute(new handler(new OnlineHandler(this), online), out reason);
        }

        public bool LoadJob(string jobName, out string reason)
        {
            reason = string.Empty;
            return execute(new handler(new LoadJobHandler(this), jobName), out reason);
        }

        #endregion
        private string[] GetJobName(bool bLaserMark1)
        {
            string jobs = string.Empty;

            if (bLaserMark1)
            {
                jobs = SC.GetStringValue("OcrReader.ReaderJob1Main");
            }
            else
            {
                jobs = SC.GetStringValue("OcrReader.ReaderJob2Main");
            }

            return jobs.Split(';');
        }

        private bool execute(IHandler handler, out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _handlers.Enqueue(handler);
                if (_foregroundHandler == null) execute(out _);                
            }
            return true;
        }

        private bool execute(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                if (_handlers.Count > 0)
                {
                    ExeError = false;
                    _foregroundHandler = _handlers.Dequeue();

                    if (!_foregroundHandler.Execute(ref _socket))
                    {
                        reason = " communication failed, please recovery it.";
                        //LOG.Error(reason);
                        EV.PostMessage(Name, EventEnum.DefaultWarning, "【Reader】" + reason);
                        _handlers.Clear();
                        _foregroundHandler = null;
                        return false;
                    }

                    
                }
            }
            return true;
        }

        private void OnDataChanged(string package)
        {
            try
            {
                package = package.ToUpper();
                string[] msgs = Regex.Split(package, delimiter);

                foreach (string msg in msgs)
                {
                    if (msg.Length > 0)
                    {
                        bool completed = false;
                        string resp = msg;

                        lock (_locker)
                        {
                            if (_foregroundHandler != null && _foregroundHandler.OnMessage(ref _socket, resp, out completed))
                            {
                                if (completed && !Error)
                                {
                                    _foregroundHandler = null;
                                    string reason = string.Empty;
                                    execute(out reason);  
                                }
                                else if(Error)
                                {
                                    _handlers.Clear();
                                    _foregroundHandler = null;
                                    

                                }
                            }
                        }
                    }
                }
            }
            catch (ExcuteFailedException ex)
            {
                EV.PostMessage(DeviceID, EventEnum.DefaultWarning, ex.Message);
            }
            catch (InvalidPackageException ex)
            {
                EV.PostMessage(DeviceID, EventEnum.DefaultWarning, ex.Message);
            }
            catch (System.Exception ex)
            {
                EV.PostMessage(Name, EventEnum.DefaultWarning, "【Reader】has exception：" + ex.ToString());
            }
        }

        public void OnLaskerMarkerRead()
        {

        }

        private void OnErrorHandler(ErrorEventArgs args)
        {
            _commErr = true;
            Initalized = false;
            EV.PostMessage(Module, EventEnum.CommunicationError, Display, args.Reason);

            //EV.PostMessage(Name, EventEnum.DefaultWarning, string.Format("{0} Communication failed，please check it.{1}", Name, args.Reason));
        }

        private string ConvetJobName(string name)
        {
            name = name.Substring(name.LastIndexOf("\\") + 1);  //remove dir
            name = name.Substring(0, name.LastIndexOf("."));  //remove expand
            return name;
        }
    }
}
