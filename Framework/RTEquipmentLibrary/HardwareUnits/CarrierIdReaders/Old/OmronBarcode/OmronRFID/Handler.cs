using System;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.OmronRFID
{
    public interface IRFIDMsg
    {
        string DeviceID { get; set; }

        string package(params object[] args);
        /// </summary>
        /// return value, completed
        /// <param name="type"></param>
        /// <param name="cmd"></param>
        /// <returns></returns>
        bool unpackage(string msg);
    }

    public class handler<T> : IHandler where T : IRFIDMsg, new()
    {
        public int ID { get; set; }
        public int Unit { get; set; }

        public bool IsBackground { get; set; } 

        private static int retry_time = 3;
        private int retry_count = retry_time;

        private T _imp = new T();
                
        private object[] _objs = null;
        public handler(string deviceID, params object[] objs)
        {
            _imp.DeviceID = deviceID;
            this._objs = objs;
        }

        public bool Execute<TPort>(ref TPort port) where TPort : ICommunication
        {
            retry_count = retry_time;
            return port.Write(string.Format("{0}\r", _imp.package(this._objs)));
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

                string type = msg.Substring(0, 2);

                if (!type.Equals("00"))  //0: command failed
                {
                    /*
                    if (retry_count-- <= 0)
                    {
                        string warning = string.Format("retry over {0} times", retry_time);
                        LOG.Warning(warning);
                        throw (new ExcuteFailedException(warning));
                    }
                    */
                    string warning = string.Format("excute failed. cause is {0}.", getErrMsg(type));
                    LOG.Warning(warning);
                    throw (new ExcuteFailedException(warning));
      
                    //port.Write(string.Format("{0}\r", _imp.package(this._objs)));

                    //return true;
                }

                msg = msg.Substring(2, msg.Length - 2);
                completed = _imp.unpackage(msg);

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

        private string getErrMsg(string error)
        {
            string msg = "";
            switch (error)
            { 
                case "14":
                    msg = "Format error There is a mistake in the command format";
                    break;
                case "70":
                    msg = "Communications error Noise or another hindrance occurs during communications with an ID Tag, and communications cannot be completed normally.";
                    break;
                case "71":
                    msg = "Verification error Correct data cannot be written to an ID Tag";
                    break;
                case "72":
                    msg = "No Tag error Either there is no ID Tag in front of the CIDRW Head, or the CIDRW Head is unable to detect the ID Tag due to environmental factors";
                    break;           
                case "7B":
                    msg = "Outside write area error A write operation was not completed normally because the ID Tag was in an area in which the ID Tag could be read but not written";
                    break;
               case "7E":
                    msg = "ID system error (1) The ID Tag is in a status where it cannot execute command processing";
                    break;
               case "7F":
                    msg = "ID system error (2) An inapplicable ID Tag has been used";
                    break;
               case "9A":
                    msg = "Hardware error in CPU An error occurred when writing to EEPROM.";
                    break;
            }
            return msg;
        }
    }


    public class ReadHandler : IRFIDMsg   //common move
    {
        public string DeviceID { get; set; }
        public ReadHandler()
        {
        }

        public string package(params object[] args)
        {
            string page = (string)args[0];
            return string.Format("{0}{1}", "0100", page);
        }

        public bool unpackage(string msg)
        {
            OmronRfidReader device = DEVICE.GetDevice<OmronRfidReader>(DeviceID);

            string asciiValue = HEX2ASCII(msg.Substring(0, Math.Min(256, msg.Length))).Trim('\0');

            if (asciiValue.IndexOf('\0') != -1)
            {
                asciiValue = asciiValue.Substring(0, asciiValue.IndexOf('\0'));
            }

            if (SC.ContainsItem("LoadPort.CarrierIdNeedTrimSpace") &&
                SC.GetValue<bool>("LoadPort.CarrierIdNeedTrimSpace"))
            {
                asciiValue = asciiValue.Trim();
            }

            device.SetCarrierIdReadResult(asciiValue);

            return true;
        }

        public string HEX2ASCII(string hex)
        {
            string res = String.Empty;

            try
            {
                for (int a = 0; a < hex.Length; a = a + 2)

                {

                    string Char2Convert = hex.Substring(a, 2);

                    int n = Convert.ToInt32(Char2Convert, 16);

                    char c = (char)n;

                    res += c.ToString();

                }

            }
            catch (Exception e)
            {
                LOG.Write(e);
            }



            return res;

        }
    }


    public class WriteHandler : IRFIDMsg   //common move
    {
        public string DeviceID { get; set; }
        public string Rfid { get; set; }
        public WriteHandler()
        {
        }

        public string package(params object[] args)
        {
            string page = (string)args[0];
            Rfid = (string)args[1];
            //Rfid = ASCII2HEX((string)args[1]);
            return string.Format("{0}{1}{2}", "0200", page, ASCII2HEX((string)args[1]).ToUpper());
        }

        public string ASCII2HEX(string src)
        {
            while (src.Length < 128)
            {
                src = '\0' + src;
            }

            if (src.Length > 128)
            {
                src = src.Substring(0, 128);
                LOG.Write("RFID support max 128 characters");
            }
            string res = String.Empty;
            try
            {

                char[] charValues = src.ToCharArray();
                string hexOutput = "";
                foreach (char _eachChar in charValues)
                {
                    // Get the integral value of the character.
                    int value = Convert.ToInt32(_eachChar);
                    // Convert the decimal value to a hexadecimal value in string form.
                    hexOutput += String.Format("{0:X2}", value);
                    // to make output as your eg 
                    //  hexOutput +=" "+ String.Format("{0:X}", value);

                }

                return hexOutput;
            }
            catch (Exception e)
            {
                LOG.Write(e);
            }

            return res;

        }

        public bool unpackage(string msg)
        {
            OmronRfidReader device = DEVICE.GetDevice<OmronRfidReader>(DeviceID);

            device.SetCarrierIdReadResult(Rfid);

            return true;
        }
    }

}
