using Aitex.Common.Util;
using Aitex.Core.Common;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Communications;
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
    public class Ioss110WidReader : OCRReaderBaseDevice, IConnection
    {
        public Ioss110WidReader(string module, string name, string scRoot) : base(module, name)
        {
            _scRoot = scRoot;

            CheckToPostMessage((int)OCRReaderMsg.StartInit, null);

        }
        private string libVers = "";
        private string parVers = "";
        private void InitIoss()
        {
            JobFileList = new List<string>();
            lib = new Wid110LibV7();

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

            libVers = lib.FGetVersion();
            parVers = lib.FGetVersionParam();

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

        private Wid110LibV7 lib;



        private string _scRoot;

        public string Address { get; private set; }
        private bool _enableLog;

        public bool IsConnected { get; set; }

        protected override bool fStartSavePicture(object[] param)
        {
            return true;
        }


        protected override bool fStartReset(object[] param)
        {
            Address = SC.GetStringValue($"{_scRoot}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            if (!isValidIP(Address))
            {
                EV.PostAlarmLog("System", $"Invalid IP address for WIDReader:{Name}");
                return false;
            }
            if (IsConnected)
                lib.FExit();



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
            InitIoss();
            return true;
        }
        protected override bool fMonitorInit(object[] param)
        {
            return true;
        }

        protected override bool fStartSetParameter(object[] param)
        {
            return true;
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
                            if (f.Extension == ".job" || f.Extension == ".wid")
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
            return true;
        }

        protected override bool fStartReadWaferID(object[] param)
        {
            string job = param[0].ToString();
            int lasermarkindex = (int)param[1];
            IsReadLaserMark1 = lasermarkindex == 0;
            string path = SC.GetStringValue($"{ _scRoot}.{ Name}.DirectoryForJob");
            if (!lib.FLoadRecipes(path, job))
            {
                string le = lib.FGetErrorDescription(lib.FGetLastError());
                OnError("Load job failed:" + le);
                return false;
            }

            if (!lib.FProcessRead())
            {
                string le = lib.FGetErrorDescription(lib.FGetLastError());
                OnError("Read laser mark failed:" + le);
                return false;
            }
            return true;
            //return lib.FGetWaferId();
        }
        protected override bool fMonitorReading(object[] param)
        {



            CurrentLaserMark = lib.FGetWaferId().Replace("READ:", "").Trim();
            if (CurrentLaserMark.Length > KeepLaserMarkCharatersNumber)
            {
                CurrentLaserMark = CurrentLaserMark.Substring(0, KeepLaserMarkCharatersNumber);
            }
            if (IsReadLaserMark1)
            {
                LaserMark1 = CurrentLaserMark;
                LaserMark1Score = lib.FGetCodeQualityOCR().ToString();
                LaserMark1ReadTime = lib.FGetCodeTime().ToString();
                LaserMark1ReadResult = $"ID:{LaserMark1},Score:{LaserMark1Score},Read time:{LaserMark1ReadTime}";
            }
            else
            {
                LaserMark2 = CurrentLaserMark;
                LaserMark2 = CurrentLaserMark;
                LaserMark2Score = lib.FGetCodeQualityOCR().ToString();
                LaserMark2ReadTime = lib.FGetCodeTime().ToString();
                LaserMark2ReadResult = $"ID:{LaserMark2},Score:{LaserMark2Score},Read time:{LaserMark2ReadTime}";
            }

            GetProcessImage();

            IsBusy = false;
            return true;
        }

        private void GetProcessImage(int bestOrAll = 0)
        {
            if (!lib.FIsInitialized())
            {
                string errMsg;

                int errorNo = lib.getErrno();
                errMsg = lib.FGetErrorDescription(errorNo);
                OnError("ERROR: " + errMsg);

                return;
            }
            //try
            //{
            //    if (File.Exists(CurrentImageFileName))
            //        File.Delete(CurrentImageFileName);
            //}

            //catch (Exception ex) { LOG.Write(ex); }
            
            if (!lib.FProcessGetImage(CurrentImageFileName, bestOrAll))
            {
                if (lib.getErrno() == Wid110LibConst.ecNoMoreImg)
                {
                    EV.PostInfoLog("WIDReader", "1 image retrieved after last process trigger.");
                    return;
                }
                int errorNo = lib.getErrno();
                string errMsg = lib.FGetErrorDescription(errorNo);
                OnError("ERROR: " + errMsg);
                    
            }
        }




        public override string[] GetJobFileList()
        {
            return JobFileList.ToArray();
        }

        public bool Connect()
        {
            return lib.FInit(Address);
        }

        public bool Disconnect()
        {
            return lib.FExit();
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
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }
    }
}
