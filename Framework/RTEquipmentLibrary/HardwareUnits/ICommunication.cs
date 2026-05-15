using System;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts
{
    public class ErrorEventArgs : EventArgs
    {
        public readonly string Reason;
        public readonly string Code;

        public ErrorEventArgs(string reason, string code = "")
        {
            Reason = reason;
            Code = code;
        }
    }

    public class DataEventArgs : EventArgs
    {
        public readonly string Data;

        public DataEventArgs(string data)
        {
            Data = data;
        }
    }

    public interface ICommunication
    {
        bool Write(string msg);
    }


    public interface IHandler
    {
        int ID { get; set; }
        int Unit { get; set; }

        bool IsBackground { get; } 

        bool Execute<T>(ref T port) where T : ICommunication;

        /// <summary>
        /// return  value : handle
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="port"></param>
        /// <param name="msg"></param>
        /// <param name="completed"></param>
        /// <returns></returns>
        bool OnMessage<T>(ref T port, string msg, out bool completed) where T : ICommunication;
    }


    public class InvalidPackageException : ApplicationException
    { 
        public InvalidPackageException(string msg) : base(msg)
        {
        }

        public override string Message
        {
            get
            {
                return base.Message;
            }
        }

    }

    public class ExcuteFailedException : ApplicationException
    {
        public ExcuteFailedException(string msg)
            : base(msg)
        {
        }

        public override string Message
        {
            get
            {
                return base.Message;
            }
        }

    }



}
