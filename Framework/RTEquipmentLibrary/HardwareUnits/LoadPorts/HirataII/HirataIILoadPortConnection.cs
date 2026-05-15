using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.HirataII
{
    public class HirataIILoadPortMessage : AsciiMessage
    {
        public string CommandType { get; set; }
        public string Command { get; set; }
        public string Data { get; set; }
    }
    public class HirataIILoadPortConnection : SerialPortConnectionBase
    {
        
        private HirataIILoadPort _device;
        public HirataIILoadPortConnection(HirataIILoadPort device, string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\r", true)
        {
            _device = device;
        }
        
        protected override MessageBase ParseResponse(string rawMsg)
        {
            HirataIILoadPortMessage msg = new HirataIILoadPortMessage();
            msg.RawMessage = rawMsg;
            msg.IsAck = false;
            msg.IsResponse = false;
            msg.IsComplete = false;
            msg.IsResponse = false;
            msg.IsNak = false;
            msg.IsError = false;
            msg.CommandType = rawMsg.Substring(5, 3);
            msg.Command = Regex.Match(rawMsg, "(?<=:).*?(?=;)").Value;

            if (msg.CommandType.Contains("ACK")) msg.IsAck = true;
            if (msg.CommandType.Contains("NAK"))
            {
                msg.IsNak = true;
                _device.OnError($"Received NAK:{rawMsg}");
            }
            if (msg.CommandType.Contains("MOV")) msg.IsAck = true;
            if (msg.CommandType.Contains("GET")) msg.IsAck = true;
            if (msg.CommandType.Contains("INF"))
            {
                msg.IsEvent = true;
            }
            if (msg.CommandType.Contains("ABS"))
            {
                _device.OnError($"Received ABS:{rawMsg}");
                msg.IsError = true;
            }
            LOG.Write($"{Address} received message:{rawMsg}");
            return msg;
        }

        //protected override MessageBase ParseResponse(byte[] byteMsg)
        //{
        //    byte[] temp = new byte[] { };
        //    foreach (byte bmsg in byteMsg)
        //    {
        //        _lstCacheBuffer.Add(bmsg);
        //        if (bmsg == 0xD)
        //        {
        //            temp = _lstCacheBuffer.ToArray();
        //            _lstCacheBuffer.Clear();
        //        }
        //    }

        //    HirataLoadPortMessage msg = new HirataLoadPortMessage();
        //    msg.RawMessage = Encoding.ASCII.GetString(temp);
        //    msg.IsAck = false;
        //    msg.IsResponse = false;
        //    msg.IsComplete = false;
        //    msg.IsResponse = false;
        //    msg.IsNak = false;
        //    msg.IsError = false;
        //    if (temp.LastOrDefault() != 0xD)
        //    {
        //        return msg;
        //    }
        //    msg.CommandType = msg.RawMessage.Substring(5, 3);
        //    msg.Command = Regex.Match(msg.RawMessage, "(?<=:).*?(?=;)").Value;

        //    if (msg.CommandType.Contains("ACK")) msg.IsAck = true;
        //    if (msg.CommandType.Contains("NAK")) msg.IsNak = true;
        //    if (msg.CommandType.Contains("MOV")) msg.IsAck = true;
        //    if (msg.CommandType.Contains("GET")) msg.IsAck = true;
        //    if (msg.CommandType.Contains("INF"))
        //    {
        //        msg.IsEvent = true;
        //    }
        //    if (msg.CommandType.Contains("ABS"))
        //    {
        //        msg.IsError = true;
        //    }
        //    LOG.Write($"{Address} received message:{msg.RawMessage}");
        //    return msg;
        //}
        protected override void OnEventArrived(MessageBase msg)
        {
            HirataIILoadPortMessage message = msg as HirataIILoadPortMessage;
            string evtcontent = message.RawMessage;
            if (evtcontent.Contains("PODON"))
            {
                _device.OnCarrierPresent();
                _device.OnCarrierPlaced();
            }
            if (evtcontent.Contains("PODOF"))
            {
                _device.OnCarrierNotPlaced();
                _device.OnCarrierNotPresent();
            }
            if (evtcontent.Contains("ABNST"))
            {
                _device.OnCarrierNotPlaced();
                _device.OnCarrierPresent();
            }
            if (evtcontent.Contains("SMTON"))
            {
                _device.OnCarrierNotPlaced();
                _device.OnCarrierPresent();
            }
            if (evtcontent.Contains("MANSW"))
            {
                _device.OnSwitchKey1();
            }
            _device.OnEvent(out _);

        }

    }

    public class HirataIILoadPortTCPConnection :TCPPortConnectionBase
    {
        private HirataIILoadPort _device;
        public HirataIILoadPortTCPConnection(HirataIILoadPort device, string ipaddress)
            : base(ipaddress)
        {
            _device = device;
            

        }
        protected override MessageBase ParseResponse(string rawMsg)
        {
            HirataIILoadPortMessage msg = new HirataIILoadPortMessage();
            msg.RawMessage = rawMsg;
            msg.IsAck = false;
            msg.IsResponse = false;
            msg.IsComplete = false;
            msg.IsResponse = false;
            msg.IsNak = false;
            msg.IsError = false;
            msg.CommandType = rawMsg.Substring(5, 3);
            msg.Command = Regex.Match(rawMsg, "(?<=:).*?(?=;)").Value;

            if (msg.CommandType.Contains("ACK")) msg.IsAck = true;
            if (msg.CommandType.Contains("NAK"))
            {
                msg.IsNak = true;
                _device.OnError($"Received NAK:{rawMsg}");
            }
            if (msg.CommandType.Contains("MOV")) msg.IsAck = true;
            if (msg.CommandType.Contains("GET")) msg.IsAck = true;
            if (msg.CommandType.Contains("INF"))
            {
                msg.IsEvent = true;
            }
            if (msg.CommandType.Contains("ABS"))
            {
                _device.OnError($"Received ABS:{rawMsg}");
                msg.IsError = true;
            }
            LOG.Write($"{Address} received message:{rawMsg}");
            return msg;
        }

        protected override void OnEventArrived(MessageBase msg)
        {
            HirataIILoadPortMessage message = msg as HirataIILoadPortMessage;
            string evtcontent = message.RawMessage;
            if (evtcontent.Contains("PODON"))
            {
                _device.OnCarrierPresent();
                _device.OnCarrierPlaced();
            }
            if (evtcontent.Contains("PODOF"))
            {
                _device.OnCarrierNotPlaced();
                _device.OnCarrierNotPresent();
            }
            if (evtcontent.Contains("ABNST"))
            {
                _device.OnCarrierNotPlaced();
                _device.OnCarrierPresent();
            }
            if (evtcontent.Contains("SMTON"))
            {
                _device.OnCarrierNotPlaced();
                _device.OnCarrierPresent();
            }
            if (evtcontent.Contains("MANSW"))
            {
                _device.OnSwitchKey1();
            }
            _device.OnEvent(out _);

        }
    }
}
