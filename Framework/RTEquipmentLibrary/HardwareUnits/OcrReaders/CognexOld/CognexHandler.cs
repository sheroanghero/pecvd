using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;


namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders
{
    public enum CongnexHandlerType
    {
        None,
        LoginUsername,
        LoginPassword,
        ReadLM,
        Online,
        ReadImage,
        GetJob,
        GetJobList,
        LoadJob,
    }
    public enum CongnexHandlerState
    {
        None,
        CMDSend,
        CMDSuccess,
        CMDFail,
        ExecuteComplete,
        Error,
        Timeout,        
    }

    public class CongexHandler : IHandler
    {
        private static readonly int retry_time = 1;

        private readonly IReaderMsg _imp;

        private readonly object[] _objs;
        private int retry_count = retry_time;

        public CongexHandler(IReaderMsg imp, params object[] objs)
        {
            _imp = imp;
            _objs = objs;
        }

        public int ID { get; set; }
        public int Unit { get; set; }

        public bool IsBackground => _imp.background;

        public bool Execute<TPort>(ref TPort port) where TPort : ICommunication
        {
            retry_count = retry_time;
            //return port.Write(string.Format("{0}\r", _imp.package(this._objs)));
            return port.Write(string.Format("{0}\r\n", _imp.package(_objs)));          
            
        }

        /// <summary>
        ///     return value: bhandle
        /// </summary>
        /// <typeparam name="TPort"></typeparam>
        /// <param name="port"></param>
        /// <param name="msg"></param>
        /// <param name="completed"></param>
        /// <returns></returns>
        public bool OnMessage<TPort>(ref TPort port, string message, out bool completed) where TPort : ICommunication
        {
            completed = false;
            try
            {
                var msg = message.Trim();
                
                if (string.IsNullOrWhiteSpace(msg)) return true;

                if(msg.Equals("-1"))
                {                    
                    if (retry_count-- <= 0)
                    {
                        var warning = string.Format("retry over {0} times", retry_time);
                        LOG.Warning(warning);
                        if (!IsBackground)
                        {
                            throw new ExcuteFailedException(warning);
                        }
                        completed = true;
                        return true;
                    }
                    port.Write(string.Format("{0}\r\n", _imp.package(_objs)));
                }

                //msg = message.Trim().Trim("[]".ToCharArray());
                var data = Regex.Split(msg, "\r\n");
                completed = _imp.unpackage("", data);   
                return true;
            }
            catch (ExcuteFailedException e)
            {
                throw e;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                throw new InvalidPackageException(message);
            }
        }
    }

    public class CognexUserNameHandler : IReaderMsg //common move
    {
        private readonly CognexOcrReader _device;

        public CognexUserNameHandler(CognexOcrReader device)
        {
            _device = device;
            background = true;
            
        }
        
        public bool background { get; }

        public string package(params object[] args)
        {
            _device.CurrentHandlerType = CongnexHandlerType.LoginUsername;
            return "admin";
        }

        public void SetExeError()
        {
            _device.Error = true; ;
        }

        public bool unpackage(string type, string[] items)
        {
            if (items.Length != 1) return false;

            if (items[0].Contains("Password"))
            {
                _device.CurrentHandlerState = CongnexHandlerState.ExecuteComplete;
                return true;
            }
            else
            {
                _device.CurrentHandlerState = CongnexHandlerState.CMDFail;
                return false;
            }
        }
    }
    
    public class CognexPasswordHandler : IReaderMsg //common move
    {
        private readonly CognexOcrReader _device;

        public CognexPasswordHandler(CognexOcrReader device)
        {
            _device = device;
            background = true;
        }

        public bool background { get; }

        public string package(params object[] args)
        {
            _device.CurrentHandlerType = CongnexHandlerType.LoginPassword;
            return "";
        }
        public void SetExeError()
        {
            _device.Error = true; ;
        }

        public bool unpackage(string type, string[] items)
        {
            if (items.Length != 1) return false;
            if(items[0].Contains("User Logged In"))
            {
                _device.CurrentHandlerState = CongnexHandlerState.ExecuteComplete;
                _device.IsLogined = true;
                return true;
            }
            if(items[0].Contains("Invalid Password"))
            {
                _device.CurrentHandlerState = CongnexHandlerState.CMDFail;
                _device.IsLogined = false;
                return false;
            }
            return false;            
            
        }
    }

    public class CognexReadHandler : IReaderMsg //common move
    {
        private readonly CognexOcrReader _device;

        public CognexReadHandler(CognexOcrReader device)
        {
            _device = device;
            background = true;
            
        }
        public void SetExeError()
        {
            _device.Error = true; ;
        }
        public bool background { get; }

        public string package(params object[] args)
        {
            _device.CurrentHandlerType = CongnexHandlerType.ReadLM;
            return "SM\"READ\"0 ";
        }


        public bool unpackage(string type, string[] items)
        {
            string[] stritems;
            if(items[0] == "1" && items.Length == 1)
            {
                _device.CurrentHandlerState = CongnexHandlerState.CMDSuccess;
                return true;
            }
            if(items[0] == "1" && items.Length == 2)
            {
                stritems = items[1].Trim("[]".ToCharArray()).Split(',');

            }
            else stritems = items[0].Trim("[]".ToCharArray()).Split(',');
            if (_device.ReadLaserMaker)            {
                
                _device.LaserMark1ReadResult = "";
                foreach(string item in stritems)
                {
                    _device.LaserMark1ReadResult = _device.LaserMark1ReadResult + item + ",";
                }

                _device.LaserMark1 = stritems[0].Trim("[]".ToCharArray());
                _device.CurrentLaserMark = _device.LaserMark1;
                _device.LaserMark1Score = 0;
                if (stritems.Length > 1 && double.TryParse(stritems[1], out double score))
                {
                    _device.LaserMark1Score = score;

                }                
                else _device.LaserMark1ReadResult = _device.LaserMark1ReadResult + "Failed!";
                if (stritems.Length > 2) _device.LaserMark1ReadTime = stritems[2];

                EV.PostInfoLog("WIDReader",$"Wafer laser mark1 read : {stritems[0]}, score:{_device.LaserMark1Score},time:{_device.LaserMark1ReadTime}.");
            }
            else
            {
                _device.LaserMark2ReadResult = "";
                foreach (string item in stritems)
                {
                    _device.LaserMark2ReadResult = _device.LaserMark2ReadResult + item + ",";
                }
                _device.LaserMark2 = stritems[0].Trim("[]".ToCharArray());
                _device.CurrentLaserMark = _device.LaserMark2;
                _device.LaserMark2Score = 0;
                if (stritems.Length > 1 && double.TryParse(stritems[1], out double score))
                    _device.LaserMark2Score = score;
                else _device.LaserMark2ReadResult = _device.LaserMark2ReadResult + "Failed!";
                if (stritems.Length > 2) _device.LaserMark2ReadTime = stritems[2];
                EV.PostInfoLog("WIDReader",$"Wafer laser mark2 read : {stritems[0]}, score:{_device.LaserMark2Score},time:{_device.LaserMark2ReadTime}.");
            }
            _device.ReadOK = true;
            _device.CurrentHandlerState = CongnexHandlerState.ExecuteComplete;
            return true;
        }
    }

    public class CognexOnlineHandler : IReaderMsg //common move
    {
        private CognexOcrReader _device;

        private bool _online;

        public CognexOnlineHandler(CognexOcrReader device)
        {
            _device = device;
            background = false;
            
        }
        public void SetExeError()
        {
            _device.Error = true; ;
        }
        public bool background { get; }

        public string package(params object[] args)
        {
            _device.CurrentHandlerType = CongnexHandlerType.Online;
            _online = (bool) args[0];
            if (_online)
                return "SO1";

            return "SO0";
        }


        public bool unpackage(string type, string[] items)
        {
            if(items.Length ==1 && items[0].Trim() =="1")
            {
                _device.CurrentHandlerState = CongnexHandlerState.ExecuteComplete;
                _device.IsOnline = _online;
            }
            return true;
        }
    }
    
    public class CognexReadImageHandler : IReaderMsg
    {
        private readonly CognexOcrReader _device;
        //private bool _readStart;
        private int _imageLength;
        private StringBuilder _stringBuilder;

        public CognexReadImageHandler(CognexOcrReader device)
        {
            _device = device;
            background = true;
            //_readStart = false;
            _imageLength = 0;
            _stringBuilder = new StringBuilder();
                  
        }
        public void SetExeError()
        {
            _device.Error = true; ;
        }
        public bool background { get; }

        public string package(params object[] args)
        {
            _device.CurrentHandlerType = CongnexHandlerType.ReadImage;
            _device.NeedSocketLog = false;
            return "RI";
        }


        public bool unpackage(string type, string[] items)
        {
            if(_device.CurrentHandlerState == CongnexHandlerState.CMDSend)
            {
                if (items.Length>=2 && items[0].Trim() == "1" && int.TryParse(items[1], out _imageLength))
                {
                    _device.CurrentHandlerState = CongnexHandlerState.CMDSuccess;

                }
                else _device.CurrentHandlerState = CongnexHandlerState.CMDFail;
                return true;
            }
            if(_device.CurrentHandlerState == CongnexHandlerState.CMDSuccess)
            {
                foreach(string str in items)
                {
                    _stringBuilder?.Append(str.Trim());
                }
                if (_stringBuilder.Length >= _imageLength)
                {
                    string strimage = _stringBuilder?.ToString().Substring(0, _imageLength);
                    _device.SaveImage(strimage);
                    _device.CurrentHandlerState = CongnexHandlerState.ExecuteComplete;
                    _device.NeedSocketLog = true;
                }
            }

            return true;
        }
    }

    public class CognexGetJobHandler : IReaderMsg
    {
        private readonly CognexOcrReader _device;

        public void SetExeError()
        {
            _device.Error = true; ;
        }
       
        public CognexGetJobHandler(CognexOcrReader device)
        {
            _device = device;
            background = true;
            
        }

        public bool background { get; }

        public string package(params object[] args)
        {
            _device.CurrentHandlerType = CongnexHandlerType.GetJob;
            return "GF";
        }


        public bool unpackage(string type, string[] items)
        {
            if (items[0].Trim() == "1")
            {                
                if (items.Length > 1) _device.CurrentJobName = items[1].Trim();
                else _device.CurrentJobName = "";
                _device.CurrentHandlerState = CongnexHandlerState.ExecuteComplete;
            }
            else
            {
                _device.CurrentHandlerState = CongnexHandlerState.CMDFail;
            }
            
            return true;
        }
    }
    
    public class CognexGetJobListHandler : IReaderMsg
    {
        private readonly CognexOcrReader _device;
        public void SetExeError()
        {
            _device.Error = true; ;
        }

        public CognexGetJobListHandler(CognexOcrReader device)
        {
            _device = device;
            background = true;
            
        }

        public bool background { get; }

        public string package(params object[] args)
        {
            _device.CurrentHandlerType = CongnexHandlerType.GetJobList;
            return "Get Filelist";
        }


        public bool unpackage(string type, string[] items)
        {
            if (items[0].Trim() == "1" && items.Length >=2)
            {
                int filecount;
                int.TryParse(items[1].Trim(), out filecount);
                var job = items.Where(i => i.Contains(".job")).ToList();
                _device.JobFileList = job;
                _device.CurrentHandlerState = CongnexHandlerState.ExecuteComplete;                
            }
            else
            {
                _device.CurrentHandlerState = CongnexHandlerState.CMDFail;
            }
            return true;
        }
    }

    public class CognexLoadJobHandler : IReaderMsg //common move
    {
        private readonly CognexOcrReader _device;

        private string _job;

        public CognexLoadJobHandler(CognexOcrReader device)
        {
            _device = device;
            background = false;
            
        }
        public void SetExeError()
        {
            _device.Error = true; ;
        }
        public bool background { get; }

        public string package(params object[] args)
        {
            _device.CurrentHandlerType = CongnexHandlerType.LoadJob;
            _job = (string) args[0]; //full path
            //      _job = _job.Substring(_job.LastIndexOf("\\") + 1);  //remove dir
            //      _job = _job.Substring(0, _job.LastIndexOf("."));  //remove expand
            return $"LF{_job}.job";
        }


        public bool unpackage(string type, string[] items)
        {
            if (items[0].Trim() == "1")
            {
                _device.CurrentJobName = _job;
                _device.CurrentHandlerState = CongnexHandlerState.ExecuteComplete;
            }
            else
            {
                _device.CurrentHandlerState = CongnexHandlerState.CMDFail;
            }
            return true;
        }
    }
}