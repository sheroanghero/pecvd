using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders.Cognex
{
    public class CognexOCRMessage : AsciiMessage
    {
        public string Data { get; set; }

    }
    public class CognexOCRConnection : TCPPortConnectionBase
    {
        private CognexWaferIDReader _reader;
        public CognexOCRConnection(string addr,CognexWaferIDReader reader) : base(addr,"")
        {
            _reader = reader;
        }
         

        protected override MessageBase ParseResponse(string rawText)
        {
            CognexOCRMessage msg = new CognexOCRMessage();           


            if (rawText.Contains("User:"))
            {
                SendMessage("admin\r\n");
                
                msg.IsResponse = false;
                msg.IsAck = false;
                msg.IsComplete = false;
                return msg;

            }
            if (rawText.Contains("Welcome to In-Sight"))
            {
                msg.IsResponse = false;
                msg.IsAck = false;
                msg.IsComplete = false;
                return msg;
            }
            if (rawText.Contains("Password:"))
            {
                SendMessage("\r\n");
                //SendMessage("admin\r\n");

                msg.IsResponse = false;
                msg.IsAck = false;
                msg.IsComplete = false;
                return msg;
            }
            if (rawText.Contains("User Logged In"))
            {
                _reader.IsLogined = true;
                //SendMessage("admin\r\n");

                msg.IsResponse = false;
                msg.IsAck = false;
                msg.IsComplete = false;
                return msg;
            }
            if (rawText.Contains("Invalid Password"))
            {
                _reader.IsLogined = false;
                //_reader.OnError(rawText);
                SendMessage("admin\r\n");

                msg.IsResponse = false;
                msg.IsAck = false;
                msg.IsComplete = false;
                return msg;
            }

            msg.IsNak = false;
            msg.IsResponse = false;
            msg.IsAck = false;
            msg.IsComplete = false;
            string tempdata = Regex.Split(rawText, "\r\n").FirstOrDefault().Replace("\r","").Replace("\n","");
            msg.RawMessage = rawText;
            msg.Data = rawText;
            if (tempdata.Contains("Welcome"))
            {
                msg.IsAck = true;
            }
                
            if (tempdata == "1")
            {
                msg.IsAck = true;
            }
                
            else if(tempdata == "-2")
            {
                msg.IsNak = true;
            }      
            else
            {                
                msg.IsResponse = true;                
            }
            msg.Data = rawText;
            return msg;
        }
        




    }


    
}
