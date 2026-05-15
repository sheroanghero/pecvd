using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.VCE.BrooksVCE
{
    public class BrooksVCEMessage : AsciiMessage
    {
        public string Data { get; set; }
        public string ErrorText { get; set; }
        public bool IsRDY { get; set; }
    }

    public class BrooksVCEConnection : SerialPortConnectionBase
    {
        //private bool _isBKGPlusMode = true;//should read from config file

        private string _cachedBuffer = string.Empty;

        public BrooksVCEConnection(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\r", true)
        {

        }

        public override bool SendMessage(byte[] message)
        {
            return base.SendMessage(message);
        }


        //1) Format: id,X,category,data<LF><CR>


        //2) request response
        //00,R,ARM,R,EX
        //00,X,ARM,R,EX,0169500

        //3) action response

        //S,BUFFER,NS,2
        //_RDY
        //_BKGRDY

        //A,DO
        //_RDY
        //_BKGERR A6

        //BAD,COMMAND
        //_ERR S4
        //_RDY

        protected override MessageBase ParseResponse(string rawText)
        {
            BrooksVCEMessage msg = new BrooksVCEMessage();
            msg.RawMessage = rawText;

            if (rawText.Length <= 0)
            {
                LOG.Error($"empty response,");
                msg.IsFormatError = true;
                return msg;
            }

            rawText = rawText.Remove(rawText.Length - 1).Substring(3);
            if (rawText.Contains("_ERR"))
            {
                msg.IsError = true;
                msg.IsAck = true;

                var rewTextArray = rawText.Split(' ');
                msg.Data = rewTextArray[1];
            }
            else if (rawText.Contains("_BKGERR"))
            {
                msg.IsError = true;
                msg.IsComplete = true;

                var rewTextArray = rawText.Split(' ');
                msg.Data = rewTextArray[1];
            }

            else if (rawText == "_RDY")
            {
                msg.IsRDY = true;
            }
            else if (rawText == "_BKGRDY")
            {
                msg.Data = rawText;
                msg.IsComplete = true;
            }
            //Request response axisPosition
            //R,ARM,R,RE
            //X,ARM,R,RE,0000000
            else
            {
                msg.MessagePart = rawText.Split(',');
                if (msg.MessagePart.Length < 2)
                {
                    LOG.Error($"too short response,");
                    msg.IsFormatError = true;
                    return msg;
                }

                if (msg.MessagePart[0] != "X")
                {
                    LOG.Error($"invalid record type for response ,");
                    msg.IsFormatError = true;
                    return msg;
                }

                if (msg.MessagePart.Length > 2)
                {
                    msg.Data = string.Join(",", msg.MessagePart, 2, msg.MessagePart.Length - 2);
                }
                msg.IsComplete = true;
            }

            return msg;
        }

    }
}
