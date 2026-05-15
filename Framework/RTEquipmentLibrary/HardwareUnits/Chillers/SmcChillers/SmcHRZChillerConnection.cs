using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Device;
using MECF.Framework.Common.Utilities;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Chillers.SmcHRZChillers
{
    //Modbus RTU
    public class SmcHRZChillerConnection : SerialPortConnectionBase
    {
        public SmcHRZChillerConnection(string portName, int baudRate = 9600, int dataBits = 7, Parity parity = Parity.Even, StopBits stopBits = StopBits.One)
            : base(portName, baudRate, dataBits, parity, stopBits, "\r\n", true)
        {

        }


        protected override MessageBase ParseResponse(string rawMessage)
        {
            AsciiMessage msg = new AsciiMessage();
            if (rawMessage == null || rawMessage.Length < 5)
            {
                return null;
            }

            try
            {
                //0183017B
                string cmd = rawMessage.Split(new char[] { '\r', '\n' })[0];
                if (ModbusUtility.CalculateLrc(ModbusUtility.HexToBytes(cmd.Substring(1, cmd.Length - 3))).ToString("X2") !=
                    cmd.Substring(cmd.Length - 2))  //LRC check
                {
                    LOG.Error($"smc chiller message LRC check error");
                    return msg;
                }

                msg.IsComplete = true;
                msg.RawMessage = rawMessage;
            }
            catch (Exception ex)
            {
                LOG.Error($"smc chiller error: [{ex.Message}]");
            }

            return msg;
        }
    }
}
