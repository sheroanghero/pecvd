using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders.HST
{
    public class HstOcrReader : BaseDevice, IDevice
    {
        #region properties
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
            get { return _lstHandler.Count > 0 || _connection.IsBusy; }
        }

        public bool Error
        {
            get
            {
                return _commErr || ExeError|| _connection.IsCommunicationError||!_connection.IsConnected;
            }
        }

        public bool ExeError { get; set; } = false;
        public bool ReadLaserMaker { get; set; }

        public string JobName { get; set; }
        public string LaserMaker { get; set; }
        public string T7Code { get; set; }

        public string LaserMark1 { get; set; }
        public string LaserMark1Score { get; set; }

        public string LaserMark1ReadTime { get; set; }
        
        public string LaserMark2 { get; set; }
        public string LaserMark2Score { get; set; }
        public string LaserMark2ReadTime { get; set; }
        public bool ReadOK { get; set; }

        public string ImageFileName { get; set; }
        public string ImageStorePath { get; set; }

        #endregion

        #region fields

        private HstConnection _connection;
        private string _address;
        private PeriodicJob _thread;
        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private object _lockerHandlerList = new object();

        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigWarningMessage = new R_TRIG();
        private bool _enableLog;

        #endregion







        public const string delimiter = "\r\n";

        private bool _commErr = false;   

        public HstOcrReader(string module, string name, string display, string deviceId, string address)
            : base(module, name, display, deviceId)
        {
            _address = address;
           

            Initalized = false;
        }
        

        public bool Initialize()
        {
            _address = SC.GetStringValue($"{Module}.{Name}.Address");            
            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");
            _lstHandler.Clear();

            _connection = new HstConnection(_address);
            if (CheckAddressPort(_address))
            {
                if (_connection.Connect())
                {
                    EV.PostInfoLog(Module, $"{Module}.{Name} connected");
                }
            }
            else EV.PostWarningLog(Module, $"{Module}.{Name} Adress is illegal");

            _thread = new PeriodicJob(50, OnTimer, $"{Module} MonitorHandler", true);


            DEVICE.Register(String.Format("{0}.{1}", Name, "ReadWaferID"), (out string reason, int time, object[] param) =>
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
            DATA.Subscribe(Name, "LaserMaker1", () => LaserMark1);
            DATA.Subscribe(Name, "LaserMaker2", () => LaserMark2);

            

            Initalized = true;
            _lstHandler.AddLast(new OnlineHandler(this, true));
            return true;
        }
        //int queryintervalcount = 0;
        private bool OnTimer()
        {
            try
            {
                _connection.MonitorTimeout();                

                //if (!_connection.IsConnected || _connection.IsCommunicationError)
                if (_connection.IsCommunicationError)
                {
                    _lstHandler.Clear();
                    return true;
                }
                if (_connection.IsBusy)
                    return true;

                HandlerBase handler = null;

                lock (_lockerHandlerList)
                {
                    if (_lstHandler.Count > 0)
                    {
                        handler = _lstHandler.First.Value;                        
                        if (handler != null) _connection.Execute(handler);
                        _lstHandler.RemoveFirst();
                    }
                    //else
                    //{
                    //    if (queryintervalcount > 100)
                    //    {
                    //        _lstHandler.AddLast(new OnlineHandler(this, true));
                    //        _lstHandler.AddLast(new OnlineHandler(this, false));
                    //        queryintervalcount = 0;
                    //    }
                    //    else 
                    //        queryintervalcount++;



                    //}
                }
            }

            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
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
        public void Terminate()
        {
        }

        public void Monitor()
        {
            //if (!_onlineInited && _socket.IsConnected)
            //{
            //    _onlineInited = true;

            //    string error = string.Empty;
            //    Online(true, out error);
            //}
        }

        public void Reset()
        {
            _trigError.RST = true;
            _trigWarningMessage.RST = true;
            _lstHandler.Clear();
            if(_connection.IsCommunicationError ||!_connection.IsConnected)
            {
                _connection.Connect();
            }
            _connection.SetCommunicationError(false, "");
            

            //_trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");

            //_trigRetryConnect.RST = true;
            ExeError = false;
        }


        #region Command
        public bool Read(bool bLaserMaker, string jobName, out string reason)
        {
            
            lock(_lockerHandlerList)
            {
                _lstHandler.AddLast(new OnlineHandler(this, false));
                _lstHandler.AddLast(new LoadJobHandler(this, ConvetJobName(jobName)));
                _lstHandler.AddLast(new OnlineHandler(this, true));
                _lstHandler.AddLast(new ReadLMHandler(this));
            }
            ReadOK = false;
            ReadLaserMaker = bLaserMaker;
            reason = "";
            return true;        
           
        }
  
        public bool ReadOnHostMode(bool bLaserMaker, string[] jobnames, out string reason)
        {
            lock (_lockerHandlerList)
            {
                _lstHandler.AddLast(new OnlineHandler(this, false));
                _lstHandler.AddLast(new LoadJobHandler(this, ConvetJobName(jobnames[0])));
                _lstHandler.AddLast(new OnlineHandler(this, true));
                _lstHandler.AddLast(new ReadLMHandler(this));
            }
            ReadOK = false;
            ReadLaserMaker = bLaserMaker;
            reason = "";
            return true;
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
        
        
        public void OnLaskerMarkerRead()
        {

        }

        

        private string ConvetJobName(string name)
        {
            name = name.Substring(name.LastIndexOf("\\") + 1);  //remove dir
            name = name.Substring(0, name.LastIndexOf("."));  //remove expand
            return name;
        }
    }
}
