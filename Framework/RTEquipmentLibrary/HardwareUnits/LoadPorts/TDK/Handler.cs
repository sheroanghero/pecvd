using System;
using System.Text.RegularExpressions;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Log;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK
{
    public interface IMsg
    {
        string deviceID { set; }
        bool background { get; }
        

        string package(params object[] args);

        string retry();
        /// </summary>
        /// return value, completed
        /// <param name="type"></param>
        /// <param name="cmd"></param>
        /// <returns></returns>
        bool unpackage(string type, string[] cmd);

        bool canhandle(string id);
    }

    public class handler<T> : IHandler where T : IMsg, new()
    {
        private static int retry_time = 3;
        private int retry_count = retry_time;

        private T _imp = new T();

        public int ID { get; set; }
        public int Unit { get; set; }
        public bool IsBackground { get { return _imp.background; } }

        private object[] _objs = null;
        private string _deviceID = string.Empty;
        private LoadPort _lpdevice;
        public handler(LoadPort lp, string deviceID, params object[] objs)
        {
            _deviceID = deviceID;
            _imp.deviceID = _deviceID;
            this._objs = objs;
            _lpdevice = lp;
        }

        public bool Execute<TPort>(ref TPort port) where TPort : ICommunication
        {
            //_lpdevice.ExecuteError = false;
            retry_count = retry_time;
            return port.Write(string.Format("s00{0};\r", _imp.package(this._objs)));
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
           // message = "ACK:STATE/A000A400101000101000;";
            completed = false;
            try
            {
                string msg = message.TrimEnd(';');
                string[] words = Regex.Split(msg, ":");

                string type = words[0];
                string data = words[1];

                string[] items = Regex.Split(data, "/");

                if (!_imp.canhandle(items[0]))
                    return false;

                if (type == "ABS")
                {
                    _lpdevice.ExecuteError = true;
                    throw (new ExcuteFailedException(message));
                }
                else if (type == "NAK")    //process retry
                {
                    if (items.Length > 1)
                    {
                        string cause = items[1];
                        if (cause != "CKSUM" || cause != "CMDER")
                        {                            
                            string warning = string.Format("can't excute retry, failed cause is {0}", cause);
                            
                            LOG.Warning(warning);
                            throw (new ExcuteFailedException(warning));           
                        }

                        if (retry_count-- <= 0)
                        {
                            string warning = string.Format("retry over {0} times", retry_time);
                            LOG.Warning(warning);
                            throw (new ExcuteFailedException(warning));
                        }

                        port.Write(_imp.retry());
                        return true;
                    }
                }
                else
                {
                    //_lpdevice.ExecuteError = false;
                    completed = _imp.unpackage(type, items);
                    return true;

                }
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

            return false;
        }
    }

}
