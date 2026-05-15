using System;
using System.Text.RegularExpressions;
using Aitex.Core.RT.Log;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders
{
    public interface IReaderMsg
    {
        string package(params object[] args);
        /// </summary>
        /// return value, completed
        /// <param name="type"></param>
        /// <param name="cmd"></param>
        /// <returns></returns>
        bool unpackage(string type, string[] items);
        bool background { get; }

        void SetExeError();
    }

    public class handler : IHandler  
    {
        public int ID { get; set; }
        public int Unit { get; set; }

        public bool IsBackground { get { return _imp.background; } }
        
        private static int retry_time = 1;
        private int retry_count = retry_time;

        private IReaderMsg _imp  ;
                
        private object[] _objs = null;
        public handler(IReaderMsg imp, params object[] objs)
        {
            _imp = imp; 
            this._objs = objs;
        }

        public bool Execute<TPort>(ref TPort port) where TPort : ICommunication
        {
            retry_count = retry_time;
            //return port.Write(string.Format("{0}\r", _imp.package(this._objs)));
            return port.Write(string.Format("{0}\r\n", _imp.package(this._objs)));
        }

        /// <summary>
        /// return value: bhandle
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
                string msg = message.Trim();
                //if (msg.IndexOf("WELCOME") >= 0)
                //{
                //    completed = true;
                //    return true;
                //}
                //msg = msg.Replace("USER:","");
                //if (string.IsNullOrWhiteSpace(msg))
                //{
                //    completed = true;
                //    return true;
                //}
                msg = message.TrimStart('[');
                msg = msg.TrimEnd(']');
                string[] data = Regex.Split(msg, ",");
                completed = _imp.unpackage("", data);



                //if (msg.Length == 1)
                //{
                //    if (!msg.Equals("1"))  //0: command failed
                //    {
                //        _imp.SetExeError();
                //        completed = true;
                //    }

                //    if (!IsBackground)
                //    {
                //        _imp.unpackage("", null);
                //        completed = true;
                //    }
       
                //    return true;
                //}

                //if (IsBackground)
                //{
                //    msg = message.TrimStart('[');
                //    msg = msg.TrimEnd(']');
                //    string[] data = Regex.Split(msg, ",");

                //    completed = _imp.unpackage("", data);
                
                //}
                return true;

            }
            catch (ExcuteFailedException e)
            {
                throw (e);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                throw (new InvalidPackageException(message));
            }
        }
    }


    public class ReadHandler : IReaderMsg   //common move
    {
        public bool background { get; private set; }
        private OcrReader _device ;

        public ReadHandler(OcrReader device)
        {
            _device = device;
            background = false;
        }
        public string package(params object[] args)
        {
            return string.Format("SM\"READ\"0 ");
        }
        public void SetExeError()
        {
            _device.ExeError = true;
        }

        public bool unpackage(string type, string[] items)
        {
            if(items.Length == 1)
            {
                if (items[0] != "1" && !items[0].Contains("WELCOME"))
                {
                    SetExeError();
                    return true;
                }
                else return false;
            }
            if (_device.ReadLaserMaker)
            {
                _device.LaserMaker = items[0];
                _device.LaserMark1 = items[0];
                if (items.Length > 1) _device.LaserMark1Score = items[1];
                if (items.Length > 2) _device.LaserMark1ReadTime = items[2];

                LOG.Write($"{_device.Name} laser marker updated to {_device.LaserMaker}");
            }
            else
            {
                _device.T7Code = items[0];
                _device.LaserMark2 = items[0];
                if (items.Length > 1) _device.LaserMark2Score = items[1];
                if (items.Length > 2) _device.LaserMark2ReadTime = items[2];
                LOG.Write($"{_device.Name} T7 code updated to {_device.T7Code}");
            }
            _device.ReadOK = double.Parse(items[1]) > 0;

            return true;
        }
    }


    public class OnlineHandler : IReaderMsg   //common move
    {
        public bool background { get; private set; }

        private OcrReader _device ;

        private bool _online = false;
        public OnlineHandler(OcrReader device)
        {
            _device = device;
            background = false;
        }
        public string package(params object[] args)
        {
            _online = (bool)args[0];
            if(_online)
                return string.Format("SO1");

            return string.Format("SO0");
        }
        public void SetExeError()
        {
            _device.ExeError = true;
        }

        public bool unpackage(string type, string[] items)
        {
            if (items[0] != "1" && !items[0].Contains("WELCOME")) 
                    SetExeError();
            return true;
        }

    }

    public class GetJobHandler : IReaderMsg   
    {
        public bool background { get; private set; }

        private OcrReader _device ;


        public GetJobHandler(OcrReader device)
        {
            _device = device;
            background = false;
        }
        public string package(params object[] args)
        {
            return string.Format("GF");
        }
        public void SetExeError()
        {
            _device.ExeError = true;
        }

        public bool unpackage(string type, string[] items)
        {
            //if (items[0] != "1")
            //{
            //    SetExeError();
            //    return true;
            //}
            //else return false;

            _device.JobName = (string)items[0];
            return true;
        }

    }

    public class LoadJobHandler : IReaderMsg   //common move
    {
        public bool background { get; private set; }
        private OcrReader _device ;

        private string _job;
        public LoadJobHandler(OcrReader device)
        {
            _device = device;
            background = false;
        }
        public string package(params object[] args)
        {
            _job = (string)args[0];    //full path
      //      _job = _job.Substring(_job.LastIndexOf("\\") + 1);  //remove dir
      //      _job = _job.Substring(0, _job.LastIndexOf("."));  //remove expand
            return string.Format("LF{0}.job",_job);
        }
        public void SetExeError()
        {
            _device.ExeError = true;
        }

        public bool unpackage(string type, string[] items)
        {
            if (items[0] != "1" && !items[0].Contains("WELCOME")) 
                SetExeError();
            else _device.JobName = _job;
            return true;
        }

    }
}
