using Aitex.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Simulator.Core.Driver
{
    public class DeviceSimulator
    {
        public event Action<string> MessageIn;
        public event Action<string> MessageOut;
        public event Action<string> ErrorOccur;

        public virtual bool IsEnabled
        {
            get { return false; }
        }

        public virtual bool IsConnected
        {
            get { return false; }
        }

        public bool BinaryDataMode { get; set; } = true;

        protected SortedList<string, Action<string>> commandList = new SortedList<string, Action<string>>();

        protected string _lineDelimiter;
        protected char _msgDelimiter;
        protected int _commandIndex;

        protected int _cmdMaxLength = 4;

        private FixSizeQueue<string> _lstAsciiMsgs = new FixSizeQueue<string>(1000);
        private FixSizeQueue<byte[]> _lstBinMsgs = new FixSizeQueue<byte[]>(1000);
        private PeriodicJob _thread;

        public DeviceSimulator(int commandIndex, string lindDelimiter, char msgDelimiter, int cmdMaxLength=4)
        {
             _lineDelimiter = lindDelimiter;
            _msgDelimiter = msgDelimiter;
            _commandIndex = commandIndex;
            _cmdMaxLength = cmdMaxLength;
 
            _thread = new PeriodicJob(100, MonitorMessages, "Monitor Messages Thread", true);

        }

        private bool MonitorMessages()
        {
            try
            {
                string asciimsg;
                while (_lstAsciiMsgs.TryDequeue(out asciimsg))
                {
                    ProcessUnsplitMessage(asciimsg);
                }
                byte[] binmsg;
                while (_lstBinMsgs.TryDequeue(out binmsg))
                {
                    ProcessUnsplitMessage(binmsg);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex);
            }

            return true;


        }

        protected virtual void AddCommandHandler(string command, Action<string> handler)
        {
            commandList.Add(command, handler);
        }
 
        protected virtual void ProcessUnsplitMessage(string message)
        {

        }

        protected virtual void ProcessUnsplitMessage(byte[] message)
        {

        }
        protected virtual void OnErrorMessage(string message)
        {
            if (ErrorOccur != null)
            {
                ErrorOccur(message);
            }
        }

        protected void AddMessageIn(string message)
        {
            if (MessageIn != null)
            {
                MessageIn(message);
            }
        }

        protected  void OnReadMessage(byte[] message)
        {
            if (MessageIn != null)
            {
                if (BinaryDataMode)
                {
                    MessageIn(string.Join(" ", Array.ConvertAll(message, x=>x.ToString("X2"))));
                }
                //MessageIn(Encoding.ASCII.GetString(message)); 
            }
            // liuyao : 这里好像有点什么问题，如果发byte会不行，我也忘记了
            //OnReadMessage(Encoding.ASCII.GetString(message));

            _lstBinMsgs.Enqueue(message);

            //ProcessUnsplitMessage(message);
        }

        protected void OnReadMessage(string message)
        {
            if (MessageIn != null)
            {
                MessageIn(message);
            }

            if (_commandIndex < 0)
            {
                _lstAsciiMsgs.Enqueue(message);
                //ProcessUnsplitMessage(message);
                return;
            }

            if (_msgDelimiter.Equals(' '))
            {
                //string cmd = message.Contains(_msgDelimiter)? message.Substring(_commandIndex, _cmdMaxLength):message;
                string cmd = message.Substring(_commandIndex, _cmdMaxLength);   
                if (cmd.Contains(_msgDelimiter))
                        cmd = cmd.Substring(0, cmd.IndexOf(_msgDelimiter));
                

                if (!commandList.ContainsKey(cmd))
                {
                    CommandNotRecognized(message);
                }
                else
                {
                    //Log.WriteIfEnabled( LogCategory.Debug, source, DeviceId + ":ProcessMessages: '" + msg.Message );
                    var handler = commandList[cmd];
                    if (handler == null)
                    {
                        //Log.WriteIfEnabled( LogCategory.Error, source, DeviceId + ":ProcessMessages: CANNOT FIND MESSAGE HANDLER '" + msg.Message );
                    }
                    else
                    {
                        if (!message.Contains("stat_pdo") && !message.Contains("statfx"))
                        {
                            //Log.WriteIfEnabled(LogCategory.Debug, source, "Received command " + message);
                        }

                        handler(message);
                    }
                }
            }
            else
            {
                string[] msgComponents = message.Split(_msgDelimiter);
                int index = msgComponents[0] == "$$$" ? 1 : _commandIndex;

                if (msgComponents.Length <= index)
                {
                    CommandNotRecognized(message);
                    return;
                }

                // find the message handler in the dictionary
                string cmd = msgComponents[index];

                if (!commandList.ContainsKey(cmd))
                {
                    CommandNotRecognized(message);
                }
                else
                {
                    //Log.WriteIfEnabled( LogCategory.Debug, source, DeviceId + ":ProcessMessages: '" + msg.Message );
                    var handler = commandList[cmd];
                    if (handler == null)
                    {
                        //Log.WriteIfEnabled( LogCategory.Error, source, DeviceId + ":ProcessMessages: CANNOT FIND MESSAGE HANDLER '" + msg.Message );
                    }
                    else
                    {
                        if (!message.Contains("stat_pdo") && !message.Contains("statfx"))
                        {
                            //Log.WriteIfEnabled(LogCategory.Debug, source, "Received command " + message);
                        }

                        handler(message);
                    }
                }
            }
        }

        protected virtual void CommandNotRecognized(string msg)
        {
            if (commandList.ContainsKey("Unknown") && commandList["Unknown"] != null)
            {

                commandList["Unknown"](msg);
            }
            else
            {
                ProcessWriteMessage("_ERR Unrecognized command");
            }
        }

        protected void OnWriteMessage(string msg)
        {
            if (MessageOut != null)
                MessageOut(msg);

            ProcessWriteMessage(msg);
        }

        protected void OnWriteMessage(byte[] msg)
        {
            if (MessageOut != null)
                MessageOut(string.Join(" ", Array.ConvertAll(msg, x => x.ToString("X2"))));

            ProcessWriteMessage(msg);
        }

        protected virtual void ProcessWriteMessage(string msg)
        {

        }

        protected virtual void ProcessWriteMessage(byte[] msg)
        {

        }

    }
}
