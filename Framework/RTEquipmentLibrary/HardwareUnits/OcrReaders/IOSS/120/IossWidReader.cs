using Aitex.Common.Util;
using Aitex.Core.Common;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders.IOSS
{
    public class IossWidReader: OCRReaderBaseDevice, IConnection
    {
        public IossWidReader(string module,string name,string scRoot):base(module,name)
        {
            _scRoot = scRoot;
            InitIoss();

            CheckToPostMessage((int)OCRReaderMsg.StartInit, null);

        }
        private void InitIoss()
        {
            JobFileList = new List<string>();
            lib = new Wid110Lib();

            Address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            if (!isValidIP(Address))
            {
                EV.PostAlarmLog("System", $"Invalid IP address for WIDReader:{Name}");
                return;
            }
            if (!lib.FInit(Address))
            {
                EV.PostAlarmLog("System", $"WIDReader:{Name} initialize failed");
                int eno = Wid110LibConst.ecNotInit;

                eno = lib.FGetLastError();

                if (lib.isException())
                    LOG.Write("getLastError(): EXCEPTION\r\n" + lib.getLastExcp());

                LOG.Write("getLastError(): return " + eno);
                return;
            }
            var fileInfo = new FileInfo(PathManager.GetDirectory($"Logs\\{Name}"));

            if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
                fileInfo.Directory.Create();

            if (fileInfo.Directory != null)
                _imageStorePath = fileInfo.FullName;


            EV.PostInfoLog("System", $"WIDReader:{Name} initialize successful");

            IsConnected = true;
        }

        private string _imageStorePath;
        private bool isValidIP(string ip)
        {
            char[] sep = { '.' };
            string[] adr = ip.Split(sep);

            try
            {
                return (isValidIPPart(Convert.ToInt32(adr[0]))
                         && isValidIPPart(Convert.ToInt32(adr[1]))
                         && isValidIPPart(Convert.ToInt32(adr[2]))
                         && isValidIPPart(Convert.ToInt32(adr[3])));
            }


            catch (Exception e)
            {
                LOG.Write("isValidIP( " + ip + " )\r\n"
                      + "ERROR: Invalid IP Address\r\n"
                      + e.ToString());

                return false;
            }
        }

       

        private bool isValidIPPart(int adr)
        {
            return (adr >= 0 && adr <= 255);
        }

        private Wid110Lib lib;



        private string _scRoot;
        
        public string Address { get; private set; }
        private bool _enableLog;

        public bool IsConnected { get; set; }

        private bool _isNeedSavePicture
        {
            get
            {
                if (SC.ContainsItem($"{_scRoot}.{Name}.SavePicture"))
                    return SC.GetValue<bool>($"{_scRoot}.{Name}.SavePicture");
                return false;
            }
        }

        protected override bool fStartSavePicture(object[] param)
        {
            return true;
        }

        protected override bool fMonitorSavingPicture(object[] param)
        {
            IsBusy = false;
            GetProcessImage();
            return true;
        }


        protected override bool fStartReset(object[] param)
        {
            IsBusy = false;
            Address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            if (!isValidIP(Address))
            {
                EV.PostAlarmLog("System", $"Invalid IP address for WIDReader:{Name}");
                return false;
            }           
            if (!lib.FInit(Address))
            {
                EV.PostAlarmLog("System", $"WIDReader:{Name} initialize address:{Address} failed");
                int eno = Wid110LibConst.ecNotInit;

                eno = lib.FGetLastError();

                if (lib.isException())
                    LOG.Write("getLastError(): EXCEPTION\r\n" + lib.getLastExcp());

                LOG.Write("getLastError(): return " + eno);
                return false;
            }
            EV.PostInfoLog("System", $"WIDReader:{Name} initialize successful");

            IsConnected = true;


            return true; ;
        }

        protected override bool fStartInit(object[] param)
        {
            IsBusy = false;
            return true;
        }
        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            return true;
        }

        protected override bool fStartSetParameter(object[] param)
        {
            return true ;
        }
        private string m_readparameter;
        protected override bool fStartReadParameter(object[] param)
        {
            m_readparameter = param[0].ToString();
            switch (m_readparameter)
            {
                case "JobList":
                    if (SC.ContainsItem($"{ _scRoot}.{ Name}.DirectoryForJob") && !string.IsNullOrEmpty(SC.GetStringValue($"{ _scRoot}.{ Name}.DirectoryForJob")))
                    {
                        string jobpath = SC.GetStringValue($"{ _scRoot}.{ Name}.DirectoryForJob");
                        string[] files = Directory.GetFiles(jobpath);
                        JobFileList = new List<string>();
                        JobFileList.Clear();
                        foreach (string strfile in files)
                        {
                            FileInfo f = new FileInfo(strfile);
                            if (f.Extension == ".job")
                            {
                                JobFileList.Add(f.Name);
                            }
                        }
                    }                   
                    break;
                default:
                    break;
            }
            return true; ;
        }
        protected override bool fMonitorReadParameter(object[] param)
        {
            IsBusy = false;
            return true;
        }

        protected override bool fStartReadWaferID(object[] param)
        {
            //string job = param[0].ToString();
            //int lasermarkindex = (int)param[1];
            //IsReadLaserMark1 = lasermarkindex == 0;
            //string path = SC.GetStringValue($"{ _scRoot}.{ Name}.DirectoryForJob");        
            //if(!lib.FLoadRecipes(path + "\\" + job))
            //{
            //    string le = lib.FGetErrorDescription(lib.FGetLastError());
            //    OnError("Load job failed:"+le);                
            //    return false;
            //}

            //if (!lib.FProcessRead())
            //{
            //    string le = lib.FGetErrorDescription(lib.FGetLastError());
            //    OnError("Read laser mark failed:" +le);
            //    return false;
            //}
            return true;
            //return lib.FGetWaferId();
        }
        protected override bool fMonitorReading(object[] param)
        {
            IsBusy = false;

            string job = CurrentParamter[0].ToString();
            int lasermarkindex = (int)CurrentParamter[1];
            IsReadLaserMark1 = lasermarkindex == 0;
            string path = SC.GetStringValue($"{ _scRoot}.{ Name}.DirectoryForJob");
            if (!lib.FLoadRecipes(path + "\\" + job))
            {
                string le = lib.FGetErrorDescription(lib.FGetLastError());
                OnError("Load job failed:" + le);
                ReadOK = false;
                return false;
            }

            if (!lib.FProcessRead())
            {
                string le = lib.FGetErrorDescription(lib.FGetLastError());
                OnError("Read laser mark failed:" + le);
                ReadOK = false;
                return false;
            }

            ReadOK = true;

            CurrentLaserMark = lib.FGetWaferId().Replace("READ:","").Trim();

            if(CurrentLaserMark.Length > KeepLaserMarkCharatersNumber)
            {
                CurrentLaserMark = CurrentLaserMark.Substring(0, KeepLaserMarkCharatersNumber);
            }

            CurrentLaserMarkScore = Convert.ToSingle(lib.FGetCodeQualityOCR());

            if (IsReadLaserMark1)
            {
                LaserMark1 = CurrentLaserMark;
                LaserMark1Score = CurrentLaserMarkScore.ToString();
                LaserMark1ReadTime = lib.FGetCodeTime().ToString();
                LaserMark1ReadResult = $"ID:{LaserMark1},Score:{LaserMark1Score},Read time:{LaserMark1ReadTime}";
            }
            else
            {
                LaserMark2 = CurrentLaserMark;
                LaserMark2Score = CurrentLaserMarkScore.ToString();
                LaserMark2ReadTime = lib.FGetCodeTime().ToString();
                LaserMark2ReadResult = $"ID:{LaserMark2},Score:{LaserMark2Score},Read time:{LaserMark2ReadTime}";
            }


            if(_isNeedSavePicture)
            {
                GetProcessImage();
            }
            

            IsBusy = false;
            return true;
        }

        private void GetProcessImage(int bestOrAll=0)
        {
            try
            {
                if (!lib.FIsInitialized())
                {
                    string errMsg;
                    if (lib.CheckError(out errMsg))
                    {
                        OnError("ERROR: " + errMsg);
                    }
                    return;
                }

                if (!lib.FProcessGetImage(CurrentImageFileName, bestOrAll))
                {
                    if (lib.getErrno() == Wid110LibConst.ecNoMoreImg)
                    {
                        EV.PostInfoLog("WIDReader", "No images retrieved after last process trigger.");
                    }
                    string errMsg;
                    if (lib.CheckError(out errMsg))
                    {
                        EV.PostWarningLog(Name, "ERROR: " + errMsg);

                    }

                }
                else
                {
                    if (CurrentLaserMarkScore < ScoreThresholdToSaveImage)
                    {
                        if(SavePictureOption == PictureOptionEnum.All || SavePictureOption == PictureOptionEnum.Failed)
                            SaveCurrentImageToFailedImage();
                    }
                    else
                    {
                        if (SavePictureOption == PictureOptionEnum.All || SavePictureOption == PictureOptionEnum.Success)
                            SaveCurrentImage();
                    }
                    
                }

                DeleteEarlyImageFile(PicturesQuanlityLimit);
            }
            catch(Exception ex)
            {
                LOG.Write(ex);
            }

        }


        private string _currentWaferID => WaferManager.Instance.GetWafer(InstalledModule, 0).WaferID ?? "";
       

      
        public override string[] GetJobFileList()
        {
            return JobFileList.ToArray();
        }

        public bool Connect()
        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }
        public override void Terminate()
        {
            try
            {
                if (!lib.FExit())
                {
                    LOG.Write(lib.FGetErrorDescription(lib.FGetLastError()));
                }
            }
            catch(Exception ex)
            {
                LOG.Write(ex);
            }
            
        }

        private bool SaveCurrentImageToFailedImage(string wid = "")
        {
            try
            {
                string waferID = WaferManager.Instance.GetWaferID(ModuleName.Aligner, 0) ?? "";
                if (wid != "") waferID = wid;
                if (File.Exists(CurrentImageFileName))
                {
                    string[] temps = CurrentImageFileName.Split('\\');
                    string pathname = CurrentImageFileName.Replace(temps.LastOrDefault(), "") + "FailedPictures\\"
                        + DateTime.Now.ToString("yyyy-MM-dd");
                    if (!Directory.Exists(pathname))
                    {
                        Directory.CreateDirectory(pathname);
                    }

                    string failedImage = pathname + "\\" + "Failed_" + waferID + "_" + string.Format("{0:yyyyMMddHHmmss}", DateTime.Now) + ".bmp";
                    File.Copy(CurrentImageFileName, failedImage, true);
                    LOG.Write($"Read success image file for {waferID} was save as {failedImage}.");
                }
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        private bool SaveCurrentImage(string wid = "")
        {
            try
            {
                string waferID = WaferManager.Instance.GetWaferID(ModuleName.Aligner, 0) ?? "";
                if (wid != "") waferID = wid;
                if (File.Exists(CurrentImageFileName))
                {
                    string[] temps = CurrentImageFileName.Split('\\');
                    string pathname = CurrentImageFileName.Replace(temps.LastOrDefault(), "") + "SuccessPictures\\"
                         + DateTime.Now.ToString("yyyy-MM-dd");
                    if (!Directory.Exists(pathname))
                    {
                        Directory.CreateDirectory(pathname);
                    }

                    string successimage = pathname + "\\" + "Success_" +
                        waferID + "_" + string.Format("{0:yyyyMMddHHmmss}", DateTime.Now) + ".bmp";

                    File.Copy(CurrentImageFileName, successimage, true);
                    LOG.Write($"Read success image file for {waferID} was save as {successimage}.");
                    return true;

                }


                return true;

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }


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
            catch (Exception ex)
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

    }
}
