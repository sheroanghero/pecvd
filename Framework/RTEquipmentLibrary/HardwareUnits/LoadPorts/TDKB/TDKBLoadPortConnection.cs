using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDKB
{
    public class TDKBLoadPortMessage : AsciiMessage
    {
        public string CommandType { get; set; }
        public string Command { get; set; }
        public string Data { get; set; }
    }
    public class TDKBLoadPortConnection : SerialPortConnectionBase
    {

        private TDKBLoadPort _device;
        public TDKBLoadPortConnection(TDKBLoadPort device, string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\r", true)
        {
            _device = device;
        }

        protected override MessageBase ParseResponse(string rawMsg)
        {
            TDKBLoadPortMessage msg = new TDKBLoadPortMessage();
            msg.RawMessage = rawMsg;
            msg.IsAck = false;
            msg.IsResponse = false;
            msg.IsComplete = false;
            msg.IsResponse = false;
            msg.IsNak = false;
            msg.IsError = false;
            msg.CommandType = rawMsg.Split(':')[0].Replace("s00", "");
            msg.Command = Regex.Match(rawMsg, "(?<=:).*?(?=;)").Value;

            if (msg.CommandType.Contains("ACK")) msg.IsAck = true;
            if (msg.CommandType.Contains("NAK"))
            {
                msg.IsNak = true;
                _device.OnNak(rawMsg);
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
                _device.OnAbs(rawMsg);
                msg.IsError = true;
            }
            LOG.Write($"{Address} received message:{rawMsg}");
            return msg;
        }

        protected override void OnEventArrived(MessageBase msg)
        {
            TDKBLoadPortMessage message = msg as TDKBLoadPortMessage;
            string evtcontent = message.RawMessage;
            
               

            if (evtcontent.Contains("PODON"))
            {
                //_device.OnCarrierPresent();
                //_device.OnCarrierPlaced();
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
            if (evtcontent.Contains("PRSIN"))
            {
                _device.IsWaferProtrude = true;
            }
            if (evtcontent.Contains("PRSCL"))
            {
                _device.IsWaferProtrude = false;
            }

            if(evtcontent.Contains("E84IO"))
            {
                _device.OnE84IOEvent(evtcontent);
            }

            if(evtcontent.Contains("E84ER"))
            {
                _device.OnE84Error(evtcontent);
            }
            if (evtcontent.Contains("E84EV"))
            {
                _device.OnE84Load(evtcontent);
            }
            if (evtcontent.Contains("E84ev"))
            {
                _device.OnE84Unload(evtcontent);
            }
            _device.OnEvent(out _);
        }

    }

    public class TDKBLoadPortTCPConnection : TCPPortConnectionBase
    {
        private TDKBLoadPort _device;
        public TDKBLoadPortTCPConnection(TDKBLoadPort device, string ipaddress)
            : base(ipaddress)
        {
            _device = device;
        }
        protected override MessageBase ParseResponse(string rawMsg)
        {
            TDKBLoadPortMessage msg = new TDKBLoadPortMessage();
            msg.RawMessage = rawMsg;
            msg.IsAck = false;
            msg.IsResponse = false;
            msg.IsComplete = false;
            msg.IsResponse = false;
            msg.IsNak = false;
            msg.IsError = false;
            msg.CommandType = rawMsg.Split(':')[0].Replace("s00","");
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
                _device.OnAbs(rawMsg);
                msg.IsError = true;
            }
            LOG.Write($"{Address} received message:{rawMsg}");
            return msg;
        }

        protected override void OnEventArrived(MessageBase msg)
        {
            TDKBLoadPortMessage message = msg as TDKBLoadPortMessage;
            string evtcontent = message.RawMessage;
            if (evtcontent.Contains("PODON"))
            {
                //_device.OnCarrierPresent();
                //_device.OnCarrierPlaced();
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
            if (evtcontent.Contains("PRSIN"))
            {
                _device.IsWaferProtrude = true;
            }
            if (evtcontent.Contains("PRSCL"))
            {
                _device.IsWaferProtrude = false;
            }

            if (evtcontent.Contains("E84IO"))
            {
                _device.OnE84IOEvent(evtcontent);
            }

            if (evtcontent.Contains("E84ER"))
            {
                _device.OnE84Error(evtcontent);
            }
            if (evtcontent.Contains("E84EV"))
            {
                _device.OnE84Load(evtcontent);
            }
            if (evtcontent.Contains("E84ev"))
            {
                _device.OnE84Unload(evtcontent);
            }

        }
    }
}
