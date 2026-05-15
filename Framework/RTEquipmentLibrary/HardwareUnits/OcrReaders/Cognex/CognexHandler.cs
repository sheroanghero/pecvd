using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders.Cognex
{
    public abstract class CognexHandler : HandlerBase
    {
        protected ModuleName _target;
        protected int _slot;
        public CognexWaferIDReader OCRDevice { get; set; }
        public string Command;
        protected CognexHandler(CognexWaferIDReader device, string command, string para,int ackLimit=10,int comlimit =90) : base(BuildMessage(command, para))
        {
            OCRDevice = device;
            Command = command;
            AckTimeout = TimeSpan.FromSeconds(ackLimit);
            CompleteTimeout = TimeSpan.FromSeconds(comlimit);

            //_isSimulator = SC.GetValue<bool>("System.IsSimulatorMode");
        }

        public static string BuildMessage(string command, string para)
        {

            return command + (string.IsNullOrEmpty(para)?"":para ) + "\r\n";
        }

        public virtual void Update()
        {

        }

    }
    public class OnlineHandler : CognexHandler
    {
        public OnlineHandler(CognexWaferIDReader reader, bool online,int ackTimelimit=10,int comTimeLimit=90) : base(reader, "SO", online ? "1" : "0", ackTimelimit, comTimeLimit)
        {
            OCRDevice = reader;
            Command = "SO";
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            CognexOCRMessage response = msg as CognexOCRMessage;
            ResponseMessage = msg;
            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                transactionComplete = true;
                return true;
            }
            if (response.IsNak)
            {
                SetState(EnumHandlerState.Nak);
                transactionComplete = false;
                return true;
            }
            if (response.IsResponse)
            {
                //SetState(EnumHandlerState.Completed);
                transactionComplete = false;
                return true;
            }
            transactionComplete = true;
            return true;
        }
    }
    public class LoadJobHandler : CognexHandler
    {
        private string jobname;
        public LoadJobHandler(CognexWaferIDReader reader, string jobfile, int ackTimelimit = 10, int comTimeLimit = 90) : base(reader, "LF", jobfile, ackTimelimit, comTimeLimit)
        {
            OCRDevice = reader;
            jobname = jobfile;
            Command = string.Format("LF{0}.job", jobfile);
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            CognexOCRMessage response = msg as CognexOCRMessage;
            ResponseMessage = msg;
            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                OCRDevice.CurrentJobName = jobname;

                transactionComplete = true;
                return true;
            }
            if (response.IsNak)
            {
                SetState(EnumHandlerState.Nak);
                transactionComplete = false;
                return true;
            }
            if (response.IsResponse)
            {
                //SetState(EnumHandlerState.Completed);
                transactionComplete = false;
                return true;
            }

            transactionComplete = true;
            return true;
        }
    }

    public class ReadWaferIDHandler : CognexHandler
    {
        public ReadWaferIDHandler(CognexWaferIDReader reader, int ackTimelimit = 10, int comTimeLimit = 90) : base(reader, "SM\"READ\"0 ", null, ackTimelimit, comTimeLimit)
        {
            OCRDevice = reader;
            Command = "SM\"READ\"0 ";
            reader.CurrentLaserMarkScore = 400;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            CognexOCRMessage response = msg as CognexOCRMessage;
            ResponseMessage = msg;
            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
                transactionComplete = false;
                return true;
            }
            if (response.IsNak)
            {
                SetState(EnumHandlerState.Nak);
                transactionComplete = false;
                return true;
            }
            if (response.IsResponse && IsAcked)
            {
                SetState(EnumHandlerState.Completed);

                string[] items = response.Data.Replace("\r","").Replace("\n","").TrimStart('[').TrimEnd(']').Replace("[","").Replace("]","").Split(',');
                if (OCRDevice.IsReadLaserMark1)
                {
                    if(!string.IsNullOrEmpty(items[0]) && items[0].Length > OCRDevice.KeepLaserMarkCharatersNumber)
                    {
                        OCRDevice.CurrentLaserMark = items[0].Substring(0, OCRDevice.KeepLaserMarkCharatersNumber);
                    }
                    else
                    {
                        OCRDevice.CurrentLaserMark = items[0];
                    }
                    OCRDevice.LaserMark1ReadResult = response.Data;
                    OCRDevice.LaserMark1 = OCRDevice.CurrentLaserMark;
                    
                    if (items.Length > 1)
                    {
                        OCRDevice.LaserMark1Score = items[1];
                        float fvalue; 
                        if(float.TryParse(items[1],out fvalue))
                        {
                            OCRDevice.CurrentLaserMarkScore = fvalue;
                        }
                        
                    }
                    if (items.Length > 2)
                    {
                        OCRDevice.LaserMark1ReadTime = items[2];
                        OCRDevice.CurrentLaserMarkReadTime = items[2];
                    }

                    LOG.Write($"{OCRDevice.Name} laser mark1 updated to {OCRDevice.LaserMark1}");
                }
                else
                {

                    if (!string.IsNullOrEmpty(items[0]) && items[0].Length > OCRDevice.KeepLaserMarkCharatersNumber)
                    {
                        OCRDevice.CurrentLaserMark = items[0].Substring(0, OCRDevice.KeepLaserMarkCharatersNumber);
                    }
                    else
                    {
                        OCRDevice.CurrentLaserMark = items[0];
                    }
                    OCRDevice.LaserMark2ReadResult = response.Data;
                    OCRDevice.LaserMark2 = OCRDevice.CurrentLaserMark;

                    if (items.Length > 1)
                    {
                        OCRDevice.LaserMark2Score = items[1];
                        float fvalue;
                        if (float.TryParse(items[1], out fvalue))
                        {
                            OCRDevice.CurrentLaserMarkScore = fvalue;
                        }
                    }
                    if (items.Length > 2)
                    {
                        OCRDevice.LaserMark2ReadTime = items[2];
                        OCRDevice.CurrentLaserMarkReadTime = items[2];
                    }
                    LOG.Write($"{OCRDevice.Name} laser mark2 updated to {OCRDevice.LaserMark2}");
                }
                OCRDevice.OnLaserMarkRead();
                transactionComplete = true;
                return true;
            }
            transactionComplete = false;
            return true;
        }
    }

    public class GetJobListHandler: CognexHandler
    {
        public GetJobListHandler(CognexWaferIDReader reader, int ackTimelimit = 10, int comTimeLimit = 90) : base(reader, "Get Filelist", "", ackTimelimit, comTimeLimit)
        {
            OCRDevice = reader;
            Command = "Get Filelist";
            reader.JobFileList = new List<string>();
            reader.JobFileList.Clear();
            fileCount = -2;
            tempCount = 0;
        }

        private int fileCount;
        private int tempCount;
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            CognexOCRMessage response = msg as CognexOCRMessage;
            ResponseMessage = msg;
            if (response.IsAck) SetState(EnumHandlerState.Acked);
            if (response.IsNak)
            {
                SetState(EnumHandlerState.Nak);
                transactionComplete = false;
                return true;
            }
            if (response.IsResponse|| response.IsAck)
            {
                SetState(EnumHandlerState.Completed);
                var data = Regex.Split(response.Data, "\r\n");
                tempCount += data.Length;
                if (fileCount == -1)  //Second Message
                {
                    fileCount = Convert.ToInt32(data[0].Trim());
                }
                if (fileCount == -2)    // First Time get message
                {
                    if (data[0] != "1")
                    {
                        OCRDevice.OnError($"Response Error:{response.Data}");
                        transactionComplete = true;
                        return true;
                    }
                    if (data.Length ==2 )
                    {
                        fileCount = -1;
                    }
                    else
                    {
                        fileCount = Convert.ToInt32(data[1].Trim());
                    }
                }
                
                

                
                var job = data.Where(i => i.Contains(".job")).ToList();
                OCRDevice.JobFileList.AddRange(job);
                transactionComplete = false;
                if(tempCount >= fileCount+2 && fileCount>=0)
                   transactionComplete = true;
                return true;
            }
            transactionComplete = false;
            return true;

        }
    }

    public class SavePictureHandler: CognexHandler
    {
        private int _imageLength;
        private StringBuilder _stringBuilder;
        private DateTime _dtStart;
        private string _filename;
        public SavePictureHandler(CognexWaferIDReader reader,string filename, int ackTimelimit = 10, int comTimeLimit = 90) : base(reader, "RI", "", ackTimelimit, comTimeLimit)
        {
            OCRDevice = reader;
            Command = "RI";
            _imageLength = 0;
            _stringBuilder = new StringBuilder();
            _filename= filename;
            _dtStart = DateTime.Now;
            //OCRDevice.SetLogEnable(false);

        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            CognexOCRMessage response = msg as CognexOCRMessage;
            if(response.IsAck)
            {
                var data = Regex.Split(response.Data, "\r\n");
                if (data.Length >= 2 && data[0].Trim() == "1" && int.TryParse(data[1], out _imageLength))
                {
                    SetState(EnumHandlerState.Acked);
                    transactionComplete = false;
                    return false;
                }
                else
                {
                    OCRDevice.OnError("Invalid data");
                    transactionComplete = true;
                    return true;

                }
            }
            if (response.IsNak)
            {
                SetState(EnumHandlerState.Nak);
                transactionComplete = false;
                return true;
            }
            if (response.IsResponse)
            {
                foreach(string str in Regex.Split(response.Data, "\r\n"))
                    _stringBuilder?.Append(str.Trim());
                if (_stringBuilder.Length >= _imageLength)
                {
                    string strimage = _stringBuilder?.ToString().Substring(0, _imageLength);
                    OCRDevice.SaveImage(_filename,strimage);
                    EV.PostInfoLog($"{OCRDevice.Name}", $"Complete save image for {_filename},cost {(DateTime.Now - _dtStart).TotalSeconds} seconds.");
                    OCRDevice.SetLogEnable(true);
                    transactionComplete = true;
                    return true;
                }
            }
            transactionComplete = false;
            return true;



        }
    }


}
