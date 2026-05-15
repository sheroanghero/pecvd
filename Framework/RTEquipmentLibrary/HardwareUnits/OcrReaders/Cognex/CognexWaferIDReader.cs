using Aitex.Common.Util;
using Aitex.Core.Common;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.DBCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders.Cognex
{
    public class CognexWaferIDReader:OCRReaderBaseDevice, IConnection
    {
        private CognexOCRConnection _connection;
        private static readonly object _lockerHandlerList = new object();
        private string _scRoot;
        private PeriodicJob _thread;
        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        private R_TRIG _trigWarningMessage = new R_TRIG();
        private R_TRIG _trigActionDone = new R_TRIG();
        private bool _enableLog => SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
        private string _imageStorePath;
        private DateTime _dtActionStart;
        private string _currentJob;

        private int m_TimelimitAck
        {
            get
            {
                if (SC.ContainsItem($"OCRReader.{Name}.TimeLimitACK"))
                    return SC.GetValue<int>($"OCRReader.{Name}.TimeLimitACK");
                return 10;
            }
        }

        private int m_TimelimitCOM
        {
            get
            {
                if (SC.ContainsItem($"OCRReader.{Name}.TimeLimitCOM"))
                    return SC.GetValue<int>($"OCRReader.{Name}.TimeLimitCOM");
                return 90;
            }
        }

        

        public override string CurrentImageFileName
        {
            get
            {
                if (SC.ContainsItem($"OCRReader.{Name}.TempImageFilePath"))
                {
                    return SC.GetStringValue($"OCRReader.{Name}.TempImageFilePath");
                }
                return "";
            }
        }
        public bool IsLogined { get; set; }
        public bool IsOnline { get; private set; }
        public CognexWaferIDReader(string module,string name,string scRoot):base(module,name)
        {
            _scRoot = scRoot;
            InitializeCognex();            
            CheckToPostMessage((int)OCRReaderMsg.Reset, null);
        }
        public void InitializeCognex()
        {
            Address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            _lstHandler.Clear();
            _connection = new CognexOCRConnection(Address,this);
            _connection.EnableLog(_enableLog);
            if (CheckAddressPort(Address))
            {
                if (_connection.Connect())
                {
                    EV.PostInfoLog(Module, $"{Module}.{Name} connected");                    
                }
            }
            else EV.PostWarningLog(Module, $"{Module}.{Name} Adress is illegal");
            ConnectionManager.Instance.Subscribe(Name, this);
            _thread = new PeriodicJob(50, OnTimer, $"{Module}.{Name} MonitorHandler", true);
            var fileInfo = new FileInfo(PathManager.GetDirectory($"Logs\\{Name}"));

            if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
                fileInfo.Directory.Create();

            if (fileInfo.Directory != null)
                _imageStorePath = fileInfo.FullName;
            ConnectionManager.Instance.Subscribe($"{Name}", this);
        }


        static bool CheckAddressPort(string s)
        {
            bool isLegal;
            Regex regex = new Regex(@"^((2[0-4]\d|25[0-5]|[1]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[1]?\d\d?)\:([1-9]|[1-9][0-9]|[1-9][0-9][0-9]|[1-9][0-9][0-9][0-9]|[1-6][0-5][0-5][0-3][0-5])$");//CheckAddressPort
            Match match = regex.Match(s);
            if (match.Success)
            {
                isLegal = true;
            }
            else
            {
                isLegal = false;
            }
            return isLegal;
        }
        private HandlerBase handler = null;
        private int retryTime = 0;
        private bool OnTimer()
        {
            try
            {

               
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        public void OnLaserMarkRead()
        {
            
        }

        public string Address { get; private set; }

        public bool IsConnected => _connection.IsConnected ;

        public bool Connect()
        {
            _connection.Connect();
            return true;
        }

        public bool Disconnect()
        {
            _connection.Disconnect();
            return true;
        }
        public override bool IsReady()
        {
            if (_lstHandler.Count != 0 || _connection.IsBusy)
                return false;
            return base.IsReady();
        }

        protected override bool fStartInit(object[] param)
        {
            
            return true;
        }
        private bool _isEnableSaveImageWithRead
        {
            get
            {
                if (SC.ContainsItem($"{_scRoot}.{Name}.EnableSaveImage"))
                    return SC.GetValue<bool>($"{_scRoot}.{Name}.EnableSaveImage");
                return false;
            }
        }
        protected override bool fStartReadWaferID(object[] param)
        {
            _currentJob = param[0].ToString();
            int lasermarkindex = (int)param[1];
            IsReadLaserMark1 = lasermarkindex == 0;
            lock (_lockerHandlerList)
            {
                if(CurrentJobName != _currentJob)
                {
                    _lstHandler.AddLast(new OnlineHandler(this, false, m_TimelimitAck, m_TimelimitCOM));
                    _lstHandler.AddLast(new LoadJobHandler(this, _currentJob, m_TimelimitAck, m_TimelimitCOM));
                }
                
                _lstHandler.AddLast(new OnlineHandler(this, true, m_TimelimitAck, m_TimelimitCOM));
                _lstHandler.AddLast(new ReadWaferIDHandler(this, m_TimelimitAck, m_TimelimitCOM));
                //if (_isEnableSaveImageWithRead)
                //    _lstHandler.AddLast(new SavePictureHandler(this, CurrentImageFileName, m_TimelimitAck, m_TimelimitCOM));
            }
            _dtActionStart = DateTime.Now;
            retryTime = 0;
            return true;
        }
        protected override bool fMonitorReading(object[] param)
        {
            IsBusy = false;
            ReadOK = false;
            Guid guid = Guid.NewGuid();
            var wafer = WaferManager.Instance.GetWafer(InstalledModule, 0);
            if (wafer == null) wafer = new WaferInfo();
            _lastWaferID = wafer.WaferID;

            if (!_connection.IsConnected)
            {
                Address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
                _connection = new CognexOCRConnection(Address, this);
                _connection.EnableLog(_enableLog);
                if (CheckAddressPort(Address))
                {
                    if (_connection.Connect())
                    {
                        EV.PostInfoLog(Module, $"{Module}.{Name} connected");
                    }
                    else
                    {
                        OnError($"{Name} Lost Communication");
                        return false;
                    }
                }
                else
                {
                    EV.PostWarningLog(Module, $"{Module}.{Name} Adress is illegal");
                    OnError($"{Name} Lost Communication");
                    return false;
                }
            }



            if (DateTime.Now - _dtActionStart >TimeSpan.FromSeconds(TimeLimitForRead))
            {
                EV.Notify(AlarmWIDReadTimeout);                
                CurrentLaserMark = "******";
                CurrentLaserMarkScore = 0;
                CurrentLaserMarkReadTime = (DateTime.Now - _dtActionStart).TotalSeconds.ToString();
                if (IsReadLaserMark1)
                {
                    LaserMark1 = "******";
                    LaserMark1Score = "0";
                    LaserMark1ReadTime = (DateTime.Now - _dtActionStart).TotalSeconds.ToString();
                }
                else
                {
                    LaserMark2 = "******";
                    LaserMark2Score = "0";
                    LaserMark2ReadTime = (DateTime.Now - _dtActionStart).TotalSeconds.ToString();
                }
                OCRDataRecorder.OcrReadComplete(guid.ToString(), wafer.WaferID ?? "", wafer.OriginStation.ToString() ?? "",
                               wafer.OriginCarrierID ?? "", wafer.OriginSlot.ToString() ?? "", Name, _currentJob, false, CurrentLaserMark,
                              CurrentLaserMarkScore.ToString(), CurrentLaserMarkReadTime);
                
                _lstHandler.Clear();
                _connection.ForceClear();
                ReadOK = false;
                return true;
            }

            


            _connection.MonitorTimeout();

            

            if (_connection.IsCommunicationError && _connection.HandlerInError != null &&
                    _connection.HandlerInError.IsAckTimeout && handler != null)
            {
                _connection.ForceClear();
                _lstHandler.Clear();

                retryTime++;
                EV.PostInfoLog(Module, $"{Name} start to retry read lasermark for the {retryTime} time");                    
                _lstHandler.AddLast(new OnlineHandler(this, false, m_TimelimitAck, m_TimelimitCOM));
                _lstHandler.AddLast(new LoadJobHandler(this, _currentJob, m_TimelimitAck, m_TimelimitCOM));
                _lstHandler.AddLast(new OnlineHandler(this, true, m_TimelimitAck, m_TimelimitCOM));
                _lstHandler.AddLast(new ReadWaferIDHandler(this, m_TimelimitAck, m_TimelimitCOM));                    
                //_dtActionStart = DateTime.Now;
                return false;               
            
            }
            if(_connection.CurrentActiveHandler!= null && _connection.CurrentActiveHandler.CurrentState == EnumHandlerState.Nak)
            {
                _connection.ForceClear();
                _lstHandler.Clear();

                retryTime++;
                EV.PostInfoLog(Module, $"{Name} start to retry read lasermark for the {retryTime} time");
                _lstHandler.AddLast(new OnlineHandler(this, false, m_TimelimitAck, m_TimelimitCOM));
                _lstHandler.AddLast(new LoadJobHandler(this, _currentJob, m_TimelimitAck, m_TimelimitCOM));
                _lstHandler.AddLast(new OnlineHandler(this, true, m_TimelimitAck, m_TimelimitCOM));
                _lstHandler.AddLast(new ReadWaferIDHandler(this, m_TimelimitAck, m_TimelimitCOM));
                //_dtActionStart = DateTime.Now;
                return false;
            }

            if (_connection.IsBusy)
                return false;

            if (_lstHandler.Count > 0)
            {
                handler = _lstHandler.First.Value;
                if (handler != null)
                {
                    if(handler.GetType() == typeof(SavePictureHandler))
                    {
                        _connection.EnableLog(false);
                    }
                    else
                        _connection.EnableLog(_enableLog);
                    _connection.Execute(handler);
                }
                _lstHandler.RemoveFirst();                
                return false;
            }

            if (CurrentLaserMarkScore < ScoreThresholdToSaveImage)
            {
                CurrentLaserMark = "******";
            }
            else
            {
                ReadOK = true;
            }

            if (CurrentLaserMark.Length > KeepLaserMarkCharatersNumber)
            {
                CurrentLaserMark = CurrentLaserMark.Substring(0, KeepLaserMarkCharatersNumber);
            }


            OCRDataRecorder.OcrReadComplete(guid.ToString(), wafer.WaferID ?? "", wafer.OriginStation.ToString() ?? "",
                               wafer.OriginCarrierID ?? "", wafer.OriginSlot.ToString() ?? "", Name, _currentJob, !CurrentLaserMark.Contains("*"), CurrentLaserMark,
                              CurrentLaserMarkScore.ToString(), CurrentLaserMarkReadTime);

            _connection.EnableLog(_enableLog);
            return true;
            

        }
        protected override bool fStartReadParameter(object[] param)
        {
            readparameter = param[0].ToString();
            switch (readparameter)
            {
                case "JobList":
                    if (SC.ContainsItem($"{ _scRoot}.{ Name}.DirectoryForJob") && !string.IsNullOrEmpty(SC.GetStringValue($"{ _scRoot}.{ Name}.DirectoryForJob")))
                    {
                        string jobpath = SC.GetStringValue($"{ _scRoot}.{ Name}.DirectoryForJob");
                        string[] files = Directory.GetFiles(jobpath);
                        JobFileList = new List<string>();
                        JobFileList.Clear();
                        foreach(string strfile in files)
                        {
                            FileInfo f = new FileInfo(strfile);
                            if(f.Extension == ".job")
                            {
                                JobFileList.Add(f.Name);
                            }
                        }
                    }
                    else
                    {
                        lock (_lockerHandlerList)
                        {
                            _lstHandler.AddLast(new GetJobListHandler(this, m_TimelimitAck, m_TimelimitCOM));
                        }
                    }
                    break;
                default:
                    break;
            }
            return true; ;
        }
        private string readparameter = "";

        protected override bool fMonitorReadParameter(object[] param)
        {
            IsBusy = false;
            if (readparameter == "JobList" && SC.ContainsItem($"{ _scRoot}.{ Name}.DirectoryForJob") && !string.IsNullOrEmpty(SC.GetStringValue($"{ _scRoot}.{ Name}.DirectoryForJob")))
            {
                return true;
            }


            if (_connection.IsBusy)
                return false;

            lock (_lockerHandlerList)
            {
                _connection.MonitorTimeout();
                if (_connection.IsCommunicationError && _connection.HandlerInError != null &&
                    _connection.HandlerInError.IsAckTimeout && handler != null)
                {
                    if (retryTime++ < 5)
                    {
                        _connection.ForceClear();
                        _connection.Execute(handler);
                        return false;
                    }
                    LaserMark1 = "******";
                    LaserMark1Score = "0";
                    LaserMark1ReadTime = (DateTime.Now - _dtActionStart).TotalSeconds.ToString();

                    OnError("Retry Failed.");
                }


                if (_lstHandler.Count > 0)
                {
                    handler = _lstHandler.First.Value;
                    if (handler != null) _connection.Execute(handler);
                    _lstHandler.RemoveFirst();
                    retryTime = 0;
                    return false;
                }
                
            }


            return true;


        }

        protected override bool fStartReset(object[] param)
        {
            _trigError.RST = true;
            _trigWarningMessage.RST = true;
            _lstHandler.Clear();
            _connection.ForceClear();
            if (_connection.IsCommunicationError || !_connection.IsConnected || Address != SC.GetStringValue($"{_scRoot}.{Name}.Address"))
            {
                Address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
                       
                _connection = new CognexOCRConnection(Address, this);
                _connection.Connect();
            }
            _connection.SetCommunicationError(false, "");
            //_trigCommunicationError.RST = true;
            //TimeLimitForRead = SC.GetValue<int>($"{_scRoot}.{Name}.TimeLimitForWID");
            _connection.EnableLog(_enableLog);
            IsOnline = false;
            CurrentJobName = "";

            return true;
        }

        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;

            if (!_connection.IsConnected)
                OnError($"{Name} lost communication");
            return true;


        }

        protected override bool fStartSetParameter(object[] param)
        {
            string paraname = param[0].ToString();
            switch(paraname)
            {
                case "SetOnline":
                    
                    break;
            }
            return true; ;
        }
        protected override bool fSetParameterComplete(object[] param)
        {
            return true;
        }

        public override string[] GetJobFileList()
        {
            lock(_lockerHandlerList)
            {
                _lstHandler.AddLast(new GetJobListHandler(this, m_TimelimitAck, m_TimelimitCOM));
            }
            int delaytime = 0;
            while((_lstHandler.Count !=0 || _connection.IsBusy)&& delaytime<10)
            {
                Thread.Sleep(500);
                delaytime++;
            }
            return JobFileList.ToArray();
        }
        protected override bool fStartSavePicture(object[] param)
        {
            _dtActionStart = DateTime.Now;
            lock (_lockerHandlerList)
            {
                _lstHandler.AddLast(new SavePictureHandler(this, CurrentImageFileName, m_TimelimitAck, m_TimelimitCOM));
            }
            return true;
        }

        private bool _isSaveFtpImage
        {
            get
            {
                if (SC.ContainsItem($"{ _scRoot}.{ Name}.EnableHandleFtpImage"))
                    return SC.GetValue<bool>($"{ _scRoot}.{ Name}.EnableHandleFtpImage");
                return true;
            }
        }

        private string _lastWaferID;
        protected override bool fMonitorSavingPicture(object[] param)
        {
            IsBusy = false;

            _connection.EnableLog(false);

            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitForRead))
            {
                EV.Notify(AlarmWIDSavePictureFail);     
                return true;
            }
            _connection.MonitorTimeout();
            if (_connection.IsBusy)
                return false;
            if (_lstHandler.Count > 0)
            {
                handler = _lstHandler.First.Value;
                if (handler != null) _connection.Execute(handler);
                _lstHandler.RemoveFirst();
                retryTime = 0;
                return false;
            }

            if (_connection.HandlerInError != null && _connection.HandlerInError.IsAckTimeout && handler != null)
            {
                
            }

            _connection.EnableLog(_enableLog);
            LOG.Write($"Save image for {_lastWaferID} successfully, cost {(DateTime.Now - _dtActionStart).TotalSeconds} seconds.");
            return true;
        }

       

        private void DeleteEarlyImageFile(int fileCount)
        {
            try
            {
                string[] temps = CurrentImageFileName.Split('\\');
                string imagepath = CurrentImageFileName.Replace("\\" + temps.LastOrDefault(), "");
                if (string.IsNullOrEmpty(imagepath)) return;
                string[] fpath = Directory.GetFiles(imagepath, "*.bmp", SearchOption.AllDirectories);
                Dictionary<string, DateTime> fCreateDate = new Dictionary<string, DateTime>();
                for (int i = 0; i < fpath.Length; i++)
                {
                    FileInfo fi = new FileInfo(fpath[i]);
                    fCreateDate[fpath[i]] = fi.CreationTime;
                }
                fCreateDate = fCreateDate.OrderBy(f => f.Value).ToDictionary(f => f.Key, f => f.Value);
                int fCount = fCreateDate.Count;
                while (fCount > fileCount)
                {

                    string strFile = fCreateDate.First().Key;
                    if (strFile == CurrentImageFileName)
                    {
                        fCreateDate.Remove(strFile);
                        fCount = fCreateDate.Count;
                        continue;
                    }

                    File.Delete(strFile);
                    fCreateDate.Remove(strFile);
                    fCount = fCreateDate.Count;

                    LOG.Write($"Success to delete file:{strFile}");


                }

                DeleteEmptyDirectory(imagepath);

                LOG.Write($"Complete to delete file");
            }
            catch(Exception ex)
            {

            }


        }

        public static void DeleteEmptyDirectory(String storagepath)
        {
            DirectoryInfo dir = new DirectoryInfo(storagepath);
            DirectoryInfo[] subdirs = dir.GetDirectories("*.*", SearchOption.AllDirectories);
            foreach (DirectoryInfo subdir in subdirs)
            {
                FileSystemInfo[] subFiles = subdir.GetFileSystemInfos();
                if (subFiles.Count() == 0)
                {
                    subdir.Delete();
                }
            }
        }
        public void SaveImage(string filename,string imgString)
        {
            try
            {
                //DeleteEarlyImageFile();

                string foldername = Path.GetDirectoryName(filename);

                if(!Directory.Exists(foldername))
                {
                    Directory.CreateDirectory(foldername);
                }

                var byBitmap = HexStringToBytes(imgString);
                var bmp = new Bitmap(new MemoryStream(byBitmap));
                if (File.Exists(filename)) 
                    File.Delete(filename);
                bmp.Save($"{filename}", ImageFormat.Bmp);
                EV.PostInfoLog("WIDReader", $"Picture saved, file name is {filename}.");

                if (CurrentLaserMark.Contains("*"))
                {
                    if (SavePictureOption == PictureOptionEnum.All || SavePictureOption == PictureOptionEnum.Failed)
                    {
                        string[] temps = CurrentImageFileName.Split('\\');

                        string pathname = CurrentImageFileName.Replace(temps.LastOrDefault(), "") + "FailedPictures\\"
                            + DateTime.Now.ToString("yyyy-MM-dd");
                        if (!Directory.Exists(pathname))
                        {
                            Directory.CreateDirectory(pathname);
                        }
                        string failedImage = pathname + "\\" + "Failed_" + _lastWaferID + "_" + string.Format("{0:yyyyMMddHHmmss}", DateTime.Now) + ".bmp";
                        bmp.Save(failedImage, ImageFormat.Bmp);
                        EV.PostInfoLog("WIDReader", $"Picture saved, file name is {failedImage}.");
                    }
                }
                else 
                {
                    if (SavePictureOption == PictureOptionEnum.All || SavePictureOption == PictureOptionEnum.Success)
                    {
                        string[] temps = CurrentImageFileName.Split('\\');
                        string pathname = CurrentImageFileName.Replace(temps.LastOrDefault(), "") + "SuccessPictures\\"
                            + DateTime.Now.ToString("yyyy-MM-dd");
                        if (!Directory.Exists(pathname))
                        {
                            Directory.CreateDirectory(pathname);
                        }
                        string sucessImage = pathname + "\\" + "Success_" +
                            _lastWaferID + "_" + string.Format("{0:yyyyMMddHHmmss}", DateTime.Now) + ".bmp";
                        bmp.Save(sucessImage, ImageFormat.Bmp);
                        EV.PostInfoLog("WIDReader", $"Picture saved, file name is {sucessImage}.");
                    }
                }
                DeleteEarlyImageFile(PicturesQuanlityLimit);  
                bmp.Dispose();
            }
            catch(Exception ex)
            {
                LOG.Write($"Save image for wafer:{_lastWaferID} to {filename} exception.");
                LOG.Write(ex);
            }

        }
        
        private static byte[] HexStringToBytes(string hex)
        {
            byte[] data = new byte[hex.Length / 2];
            int j = 0;
            for (int i = 0; i < hex.Length; i += 2)
            {
                data[j] = Convert.ToByte(hex.Substring(i, 2), 16);
                ++j;
            }
            return data;
        }

        protected override void OnWaferIDRead(string wid, string score, string readtime)
        {
            if(DeviceState == OCRReaderStateEnum.ReadWaferID)
                base.OnWaferIDRead(wid, score, readtime);
        }

        public void SetLogEnable(bool enable)
        {
            if(enable)
                _connection.EnableLog(_enableLog);
            else
                _connection.EnableLog(enable);
        }

    }

}
