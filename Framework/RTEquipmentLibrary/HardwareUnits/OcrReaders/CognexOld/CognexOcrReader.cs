using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Aitex.Common.Util;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using ErrorEventArgs = MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.ErrorEventArgs;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders
{
    public class CognexOcrReader : BaseDevice, IDevice, IConnection
    {
        public const string delimiter = "\r\n";
        
        private static readonly object _locker = new object();
        private readonly string _addr;

        private IHandler _foregroundHandler; //current handler

        private readonly Queue<IHandler> _handlers = new Queue<IHandler>();

        public bool IsLogined { get; set; } = false;
        public bool IsOnline { get; set; } = false; 
        
        public string ImageFileName => _imageFileName;
        public string ImageStorePath => _imageStorePath;

        private AsyncSocket _socket;
        private string _imageStorePath;
       private string _imageFileName;
        private string _imageString;    //Read image in BMP format
        //private int _imageLength;    //Read image in BMP format
        //private StringBuilder _stringBuilder;
        //private bool _readStart;

        public CongnexHandlerType CurrentHandlerType { get; set; } = CongnexHandlerType.None;
        public CongnexHandlerState CurrentHandlerState { get; set; } = CongnexHandlerState.None;

        public Guid OCRGuid { get; set; }
        public bool CurrentHandlerExcuteResult { get; set; } = true;
        public bool CurrentHandlerExcuteComplete { get; set; } = true;
        public string CurrentWaferID { get; set; }
        private const string EventCMDFail = "CongnexCommandFailed";
        private DeviceTimer _timer = new DeviceTimer();
        private int _ts;
        
        public bool NeedSocketLog
        {
            get;set;
        }
        public string Address => _addr;
        
        public bool IsConnected => _socket.IsConnected;


        public CognexOcrReader(string module, string name, string display, string deviceId, string address)
            : base(module, name, display, deviceId)
        {
            _addr = address;
            _socket = new AsyncSocket(address,524288, "");
            _socket.OnDataChanged += OnDataChanged;
            _socket.OnErrorHappened += OnErrorHandler;
            //IsReadImage = false;
            Initalized = false;

            _imageString = "";
        }

        public DeviceState State
        {
            get
            {
                if (!Initalized) return DeviceState.Unknown;
                if (Error) return DeviceState.Error;

                if (Busy)
                    return DeviceState.Busy;

                return DeviceState.Idle;
            }
        }

        public bool Initalized { get; set; }

        public bool Busy => _handlers.Count > 0 || _foregroundHandler != null;

        public bool Error { get; set; }

        public bool ReadLaserMaker { get; set; }

        public string CurrentJobName { get; set; }
        public string LaserMark1 { get; set; }
        public double LaserMark1Score { get; set; }
        public string LaserMark1ReadTime { get; set; } = string.Empty;
        public string LaserMark2 { get; set; }
        public double LaserMark2Score { get; set; }
        public string LaserMark2ReadTime { get; set; } = string.Empty;
        public string CurrentLaserMark { get;set; }

        public List<string> JobFileList { get; set; }
        
        public bool ReadOK { get; set; }

        public string LaserMark1ReadResult { get; set; }
        public string LaserMark2ReadResult { get; set; }

        public string ImageString => _imageString;
        
        public bool Connect()
        {
            _socket.Connect(_addr);
            return true;
        }

        public bool Disconnect()
        {
            _socket.Dispose();
            return true;
        }

        public bool Initialize()
        {
            ConnectionManager.Instance.Subscribe(Name, this);
            JobFileList = new List<string>();
            int retry = 0;
            while (!_socket.IsConnected && retry<5)
            {                
                _socket.Connect(_addr);
                Thread.Sleep(1000);
                retry++;
            }

            //DEVICE.Register(string.Format("{0}.{1}", Name, "ReadWaferID"),//DeviceOperationName.ReadWaferID),
            //    (out string reason, int time, object[] param) =>
            //    {
            //        var bLaser = bool.Parse((string) param[0]);
            //        var jobName = (string) param[1];
            //        var ret = Read(bLaser, jobName, out reason);
            //        if (ret)
            //        {
            //            reason = string.Format("{0}", Name, "Read Laser Mark.");
            //            return true;
            //        }

            //        return false;
            //    });

            //DEVICE.Register(string.Format("{0}.{1}", Name, "ReadLM1"),//DeviceOperationName.ReadWaferID),
            //    (out string reason, int time, object[] param) =>
            //    {
            //        var bLaser =true;
            //        var jobName = (string)param[0];
            //        var ret = Read(bLaser, jobName, out reason);
            //        if (ret)
            //        {
            //            reason = string.Format("{0}", Name, "Read Laser Mark.");
            //            return true;
            //        }

            //        return false;
            //    });

            //DEVICE.Register(string.Format("{0}.{1}", Name, "ReadLM2"),//DeviceOperationName.ReadWaferID),
            //    (out string reason, int time, object[] param) =>
            //    {
            //        var bLaser = false;
            //        var jobName = (string)param[0];
            //        var ret = Read(bLaser, jobName, out reason);
            //        if (ret)
            //        {
            //            reason = string.Format("{0}", Name, "Read Laser Mark.");
            //            return true;
            //        }

            //        return false;
            //    });
            //DEVICE.Register($"{Name}.RefreshJobList", (out string reason, int time, object[] param) =>
            //{
            //    GetJobList(out reason);
            //    return true;
            //});

            DATA.Subscribe(Name, "WRIDReaderState", () => State);
            DATA.Subscribe(Name, "WRIDReaderBusy", () => Busy);
            DATA.Subscribe(Name, "LaserMaker1", () => LaserMark1);
            DATA.Subscribe(Name, "LaserMaker2", () => LaserMark2);
            DATA.Subscribe(Name, "LaserMark1Result", () => LaserMark1ReadResult);
            DATA.Subscribe(Name, "LaserMark2Result", () => LaserMark2ReadResult);
            DATA.Subscribe(Name, "JobFileList", () => JobFileList);
            DATA.Subscribe(Name, "CurrentLaserMark", () => CurrentLaserMark);

            EV.Subscribe(new EventItem("Event", EventCMDFail, "Congnex command execution failed.", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));
            

            _ts = SC.ContainsItem("OcrReader.TimeLimitForWID") ? SC.GetValue<int>("OcrReader.TimeLimitForWID") : 5;            
            
            var fileInfo = new FileInfo(PathManager.GetDirectory($"Logs\\{DeviceID}"));
            
            if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
                fileInfo.Directory.Create();
            
            if (fileInfo.Directory != null) 
                _imageStorePath = fileInfo.FullName;

            _handlers.Clear();
            
            Initalized = true;
            if(_socket.IsConnected)
                Login(out _);
            return true;
        }

        public void Terminate()
        {
            _socket.Dispose();
        }

        public void Monitor()
        {
            var wafer = WaferManager.Instance.GetWafers(ModuleName.Aligner)[0];
            if(wafer.IsEmpty) CurrentLaserMark = string.Empty;
            //if (IO.DI["DI_PreAlignerWaferOn"].Value) CurrentLaserMark = string.Empty;
        }

        public void Reset()
        {
            lock (_locker)
            {
                _foregroundHandler = null;
                _handlers.Clear();
            }

            if (Error)
            {
                Error = false;
                _socket.Connect(_addr);
            }
        }

        private string[] GetJobName(bool bLaserMark1)
        {
            var jobs = string.Empty;

            if (bLaserMark1)
                jobs = SC.GetStringValue("OcrReader.ReaderJob1Main");
            else
                jobs = SC.GetStringValue("OcrReader.ReaderJob2Main");

            return jobs.Split(';');
        }

        private bool execute(IHandler handler, out string reason)
        {
            reason = string.Empty;
            NeedSocketLog = true;
            lock (_locker)
            {
                CurrentHandlerExcuteComplete = false;
                CurrentHandlerExcuteResult = false;
                CurrentHandlerState = CongnexHandlerState.CMDSend;
                _foregroundHandler = handler;

                if (!handler.Execute(ref _socket))
                {
                    EV.Notify(EventCMDFail);
                    reason = "Communication failed, please recovery it.";
                    CurrentHandlerState = CongnexHandlerState.CMDFail;
                    return false;                    
                }
                
            }

            return true;
        }

        private bool execute(out string reason)
        {
            reason = string.Empty;
            NeedSocketLog = true;
            lock (_locker)
            {
                if (_handlers.Count > 0)
                {
                    _foregroundHandler = _handlers.Dequeue();
                    CurrentHandlerExcuteComplete = false;
                    CurrentHandlerExcuteResult = false;
                    CurrentHandlerState = CongnexHandlerState.CMDSend;
                   

                    if (!_foregroundHandler.Execute(ref _socket))
                    {
                        EV.Notify(EventCMDFail);
                        reason = " communication failed, please recovery it.";
                        //LOG.Error(reason);
                        EV.PostMessage(Name, EventEnum.DefaultWarning, "【Reader】" + reason);
                        CurrentHandlerState = CongnexHandlerState.CMDFail;
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
                if(package.Contains("User:"))
                {
                    _foregroundHandler = null;
                    _handlers.Clear();
                    Login(out _);
                    return;
                }
                if (_foregroundHandler == null) return;                
               
                if (package.Trim() == "") return;

                bool completed = false; 
                
                lock (_locker)
                {
                    if (_foregroundHandler != null && _foregroundHandler.OnMessage(ref _socket, package.Trim(), out completed))
                    {
                        if (CurrentHandlerState == CongnexHandlerState.ExecuteComplete)
                        {
                            _foregroundHandler = null;
                            var reason = string.Empty;
                            execute(out reason);
                        }
                        if (CurrentHandlerState == CongnexHandlerState.CMDFail)
                        {
                            EV.Notify(EventCMDFail);
                            _foregroundHandler = null;
                            _handlers.Clear();
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
            catch (Exception ex)
            {
                EV.PostMessage(Name, EventEnum.DefaultWarning, "【Reader】has exception：" + ex);
            }
        }
        
        private void OnErrorHandler(ErrorEventArgs args)
        {
            Error = true;
            Initalized = false;
            EV.PostMessage(Module, EventEnum.CommunicationError, Display, args.Reason);

            //EV.PostMessage(Name, EventEnum.DefaultWarning, string.Format("{0} Communication failed，please check it.{1}", Name, args.Reason));
        }

        private string ConvertJobName(string name)
        {
            name = name.Substring(name.LastIndexOf("\\") + 1); //remove dir
            name = name.Substring(0, name.LastIndexOf(".")); //remove expand
            return name;
        }

        public void SaveImage(string imgString)
        {
            DeleteEarlyImageFile();
            var byBitmap = HexStringToBytes(imgString);
            var bmp = new Bitmap(new MemoryStream(byBitmap));
            _imageFileName = CurrentWaferID + DateTime.Now.ToString("G").Replace('/','_').Replace(':','_');
            
            bmp.Save($"{_imageStorePath}{_imageFileName}.bmp",ImageFormat.Bmp);
            EV.PostInfoLog("WIDReader",$"Picture saved, file name is {_imageFileName}.bmp");
            bmp.Dispose();
        }

        private void DeleteEarlyImageFile()
        {
            string[] fpath = Directory.GetFiles(_imageStorePath, "*.bmp", SearchOption.TopDirectoryOnly);
            Dictionary<string, DateTime> fCreateDate = new Dictionary<string, DateTime>();
            for (int i = 0; i < fpath.Length; i++)
            {
                FileInfo fi = new FileInfo(fpath[i]);
                fCreateDate[fpath[i]] = fi.CreationTime;
            }
            fCreateDate = fCreateDate.OrderBy(f => f.Value).ToDictionary(f => f.Key, f => f.Value);
            int fCount = fCreateDate.Count;
            while (fCount > 99)
            {
                string strFile = fCreateDate.First().Key;
                File.Delete(strFile);
                fCreateDate.Remove(strFile);
                fCount = fCreateDate.Count;
            }


        }

        private static byte[] HexStringToBytes(string hex)
        {
            byte[] data = new byte[hex.Length /2];
            int j = 0;
            for (int i = 0; i < hex.Length; i+=2)
            {
                data[ j ] = Convert.ToByte(hex.Substring(i, 2), 16);
                ++j;
            }
            return data;
        }
        
        private string ConvetJobName(string name)
        {
            name = name.Substring(name.LastIndexOf("\\") + 1);  //remove dir
            name = name.Substring(0, name.LastIndexOf("."));  //remove expand
            return name;
        }


        #region Command

        public bool Login(out string reason)
        {
            _handlers.Clear();
            _handlers.Enqueue(new CongexHandler(new CognexUserNameHandler(this))); //UserName
            _handlers.Enqueue(new CongexHandler(new CognexPasswordHandler(this))); //Password
            return execute(out reason);
        }

        public bool ReadOnHostMode(bool bLaserMaker, string[] jobnames, out string reason)
        {
            _handlers.Clear();

            string[] jobs = jobnames;

            if (jobs.Length == 0 || string.IsNullOrEmpty(jobs[0]))
            {
                reason = "Job is undefine";
                return false;
            }

            string jobName = ConvetJobName(jobs[0]).Replace(".job","");

            if (CurrentJobName != jobName)
            {
                _handlers.Enqueue(new CongexHandler(new CognexOnlineHandler(this), false)); //offline
                _handlers.Enqueue(new CongexHandler(new CognexLoadJobHandler(this), jobName)); //LoadJob
                           
            }
            _handlers.Enqueue(new CongexHandler(new CognexOnlineHandler(this), true)); //online    
            _handlers.Enqueue(new CongexHandler(new CognexReadHandler(this))); //Read

            //if (jobs.Length > 1)
            //{
            //    for (int i = 1; i < jobs.Length; i++)
            //    {
            //        jobName = ConvetJobName(jobs[i]);
            //        _handlers.Enqueue(new handler(new CognexOnlineHandler(this), false)); //offline
            //        _handlers.Enqueue(new handler(new CognexLoadJobHandler(this), jobName)); //LoadJob
            //        _handlers.Enqueue(new handler(new CognexOnlineHandler(this), true)); //online               

            //        _handlers.Enqueue(new handler(new CognexReadHandler(this))); //Read
            //    }
            //}

            ReadOK = false;
            ReadLaserMaker = bLaserMaker;
            return execute(out reason);
        }

        public bool Read(out string reason)
        {
            _handlers.Clear();

            string[] jobs = GetJobName(true);

            if (jobs.Length == 0 || string.IsNullOrEmpty(jobs[0]))
            {
                reason = "Job is undefine";
                return false;
            }
            
            string jobName = ConvetJobName(jobs[0]);  
            if (CurrentJobName != jobName)
            {
                _handlers.Enqueue(new CongexHandler(new CognexOnlineHandler(this), false));   //offline
                _handlers.Enqueue(new CongexHandler(new CognexLoadJobHandler(this), jobName)); //LoadJob
                            
            }
            _handlers.Enqueue(new CongexHandler(new CognexOnlineHandler(this), true));   //online   
            _handlers.Enqueue(new CongexHandler(new CognexReadHandler(this)));         //Read

            //if (jobs.Length > 1)
            //{
            //    for (int i = 1; i < jobs.Length; i++)
            //    {
            //        jobName = ConvetJobName(jobs[i]);
            //        _handlers.Enqueue(new handler(new CognexOnlineHandler(this), false));   //offline
            //        _handlers.Enqueue(new handler(new CognexLoadJobHandler(this), jobName)); //LoadJob
            //        _handlers.Enqueue(new handler(new CognexOnlineHandler(this), true));   //online               

            //        _handlers.Enqueue(new handler(new CognexReadHandler(this)));         //Read
            //    }
            //}
            ReadOK = false;
            ReadLaserMaker = true;
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
            if (CurrentJobName != jobName)
            {
                _handlers.Enqueue(new CongexHandler(new CognexOnlineHandler(this), false));   //offline
                _handlers.Enqueue(new CongexHandler(new CognexLoadJobHandler(this), jobName)); //LoadJob
                         
            }
            _handlers.Enqueue(new CongexHandler(new CognexOnlineHandler(this), true));   //online      
            _handlers.Enqueue(new CongexHandler(new CognexReadHandler(this)));         //Read

            //if (jobs.Length > 1)
            //{
            //    for (int i = 1; i < jobs.Length; i++)
            //    {
            //        jobName = ConvetJobName(jobs[i]);
            //        _handlers.Enqueue(new handler(new CognexOnlineHandler(this), false));   //offline
            //        _handlers.Enqueue(new handler(new CognexLoadJobHandler(this), jobName)); //LoadJob
            //        _handlers.Enqueue(new handler(new CognexOnlineHandler(this), true));   //online               

            //        _handlers.Enqueue(new handler(new CognexReadHandler(this)));         //Read
            //    }
            //}
            ReadOK = false;
            ReadLaserMaker = bLaserMaker;
            return execute(out reason);
        }
        
        public bool Read(bool bLaserMaker, string jobName, out string reason)
        {
            _handlers.Clear();
            if (string.IsNullOrEmpty(jobName))
            {
                reason = "Job is undefine.";
                return false;
            }

            jobName = ConvertJobName(jobName);
            if (CurrentJobName != jobName)
            {
                _handlers.Enqueue(new CongexHandler(new CognexOnlineHandler(this), false)); //offline
                _handlers.Enqueue(new CongexHandler(new CognexLoadJobHandler(this), jobName)); //LoadJob
                
            }
            

            ReadOK = false;
            ReadLaserMaker = bLaserMaker;
            _handlers.Enqueue(new CongexHandler(new CognexOnlineHandler(this), true)); //online
            _handlers.Enqueue(new CongexHandler(new CognexReadHandler(this))); //Read
            return execute(out reason);
        }
        
        public bool ReadImage(string waferid,out string reason)
        {
            _handlers.Clear();
            this.CurrentWaferID = waferid;
            reason = string.Empty;
            _handlers.Enqueue(new CongexHandler(new CognexReadImageHandler(this))); //Read
            return execute(out reason);
        }

        public bool ReadJobName(out string reason)
        {
            reason = string.Empty;
            return execute(new CongexHandler(new CognexGetJobHandler(this)), out reason);
        }

        public bool GetJobList(out string reason)
        {
            reason = string.Empty;
            return execute(new CongexHandler(new CognexGetJobListHandler(this)), out reason);
        }

        public bool Online(bool online, out string reason)
        {
            reason = string.Empty;
            return execute(new CongexHandler(new CognexOnlineHandler(this), online), out reason);
        }

        public bool LoadJob(string jobName, out string reason)
        {
            reason = string.Empty;
            return execute(new CongexHandler(new CognexLoadJobHandler(this), jobName), out reason);
        }

        #endregion
    }
}