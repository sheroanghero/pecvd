using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RemoteControl
{
    public abstract class RemoteControlHandler : HandlerBase
    {
        public enum FuncName
        {
            GETSENSORSTATUS = 0,
            STARTMEASUREMENT = 1,
            GETEPDRECIPELIST = 2,
            REQUESTRUNACTION = 3,
        }
        public RemoteControl Device { get; }

        public string _commandType;
        public string _command;
        public int _commandID;
        public string _parameter;

        public const char ascii_nul = '\0';
        public const char ascii_cr = '\r';
        public const char ascii_lf = '\n';

        // locals
        /// <summary>
        /// counter auto incremented at each sent message
        /// </summary>
        private static UInt32 tx_message_id_auto_inc = 0;

        // persistent fields values
        // header sender_id = name of simulator H (as host) , when sending message
        public static string _senderID = "SPV";

        // header receiver_id = name of Sigma_P instrument to receive message
        public static string _receiverID = "Ch1";

        public static uint _message_id = 0;
        public static uint _command_id = 0;

        public RemoteControlHandler(RemoteControl device, FuncName command, string senderID = null, string receiverID = null, List<string> paramList = null)
                : base(BuildMessage(command, paramList))
        {
            Device = device;
            _command = command.ToString();
            _commandID = (int)command;
            Name = command.ToString();
            if (receiverID != null)
                _receiverID = receiverID;
            if (senderID != null)
                _senderID = senderID;
        }

        protected static string BuildMessage(FuncName command,List<string> paramList)
        {
            string sendStr = "";
            switch (command)
            {
                case FuncName.GETSENSORSTATUS:
                    _message_id = auto_inc_message_id();
                    _command_id = 1;
                    sendStr = $"{_message_id}{ascii_nul}{_command_id}{ascii_nul}{_senderID}{ascii_nul}{_receiverID}{ascii_nul}0{ascii_nul}\r\n";
                    
                    break;
                case FuncName.STARTMEASUREMENT:
                    _message_id = auto_inc_message_id();
                    _command_id = 2;
                    sendStr = $"{_message_id}{ascii_nul}{_command_id}{ascii_nul}{_senderID}{ascii_nul}{_receiverID}";
                    sendStr += $"{ascii_nul}{paramList.Count}";
                    foreach (string param in paramList)
                    {
                        sendStr += $"{ascii_nul}{param}";
                    }
                    sendStr+= $"{ ascii_nul}\r\n";
                    break;
                case FuncName.GETEPDRECIPELIST:
                    _message_id = auto_inc_message_id();
                    _command_id = 3;
                    sendStr = $"{_message_id}{ascii_nul}{_command_id}{ascii_nul}{_senderID}{ascii_nul}{_receiverID}{ascii_nul}0{ascii_nul}\r\n";

                    break;
                case FuncName.REQUESTRUNACTION:
                    _message_id = auto_inc_message_id();
                    _command_id = 4;
                    sendStr = $"{_message_id}{ascii_nul}{_command_id}{ascii_nul}{_senderID}{ascii_nul}{_receiverID}{ascii_nul}1{ascii_nul}STOP{ascii_nul}\r\n";

                    break;
            }

            return sendStr;
        }

        #region demo code
        public static UInt32 auto_inc_message_id()
        {
            tx_message_id_auto_inc++;
            if (tx_message_id_auto_inc > 1_000_000)
            {   // auto wrap
                tx_message_id_auto_inc = 1;
            }

            return tx_message_id_auto_inc;
        }
        #endregion

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            RemoteControlMessage response = msg as RemoteControlMessage;
            ResponseMessage = msg;
            if (msg.IsError)
            {
                Device.OnError(response.RawMessage);
                transactionComplete = true;
                return true;
            }
            if (msg.IsAck)
            {
                SetState(EnumHandlerState.Acked);
            }

            if (!(response.message_id == _message_id || (response.command_id-100) == _commandID))
            {
                SetState(EnumHandlerState.Completed);                
                //Device.OnActionDone(null);
                transactionComplete = false;
                return true;
            }

            if (_commandID == 0)
            {
                if (response.MessagePart.Length < 7)
                {
                    response.IsError = true;
                    transactionComplete = true;
                    return false;
                }
                if (Device.ParseGetSensorStatus(Convert.ToInt32(response.MessagePart[5]), response.MessagePart[6]))
                {
                    transactionComplete = true;
                    return true;
                }
            }
            else if (_commandID == 1)
            {
                if (response.MessagePart.Length < 8)
                {
                    response.IsError = true;
                    transactionComplete = true;
                    return false;
                }
                if (Device.ParseStartMeasurement(Convert.ToInt32(response.MessagePart[5]), response.MessagePart[6], response.MessagePart[7]))
                {
                    transactionComplete = true;
                    return true;
                }
            }
            else if (_commandID == 2)
            {
                if (response.MessagePart.Length < 8)
                {
                    response.IsError = true;
                    transactionComplete = true;
                    return false;
                }
                List<string> recipes = new List<string>();
                for (int i = 0; i < Convert.ToInt32(response.MessagePart[7]); i++)
                {
                    recipes.Add( response.MessagePart[8 + i]);
                }
                if (Device.ParseGetEPDRecipeList(Convert.ToInt32(response.MessagePart[5]), response.MessagePart[6], Convert.ToInt32(response.MessagePart[7]), recipes))
                {
                    transactionComplete = true;
                    return true;
                }
            }
            else if (_commandID == 3)
            {
                if (response.MessagePart.Length < 7)
                {
                    response.IsError = true;
                    transactionComplete = true;
                    return false;
                }
                if (Device.ParseRunAction(Convert.ToInt32(response.MessagePart[5]), response.MessagePart[6]))
                {
                    transactionComplete = true;
                    return true;
                }
            }
            response.IsError = true;
            transactionComplete = false;
            return false;
        }
    }

    public class RemoteControlGetSensorStatusHandler : RemoteControlHandler
    {
        public RemoteControlGetSensorStatusHandler(RemoteControl remoteControl, string senderID = null, string receiverID = null)
            : base(remoteControl, FuncName.GETSENSORSTATUS, senderID, receiverID)
        {
            LOG.Write($"{remoteControl.Name} execute GetStatus {FuncName.GETSENSORSTATUS} in ASCII.");
        }
    }
    public class RemoteControlStartMeasurementHandler : RemoteControlHandler
    {
        public RemoteControlStartMeasurementHandler(RemoteControl remoteControl, List<string> paramList, string senderID = null, string receiverID = null)
            : base(remoteControl, FuncName.STARTMEASUREMENT, senderID, receiverID, paramList)
        {
            LOG.Write($"{remoteControl.Name} execute GetStatus {FuncName.STARTMEASUREMENT} in ASCII.");
        }
    }
    public class RemoteControlGetEPDRecipeListHandler : RemoteControlHandler
    {
        public RemoteControlGetEPDRecipeListHandler(RemoteControl remoteControl, string senderID = null, string receiverID = null)
            : base(remoteControl, FuncName.GETEPDRECIPELIST, senderID, receiverID)
        {
            LOG.Write($"{remoteControl.Name} execute GetStatus {FuncName.GETEPDRECIPELIST} in ASCII.");
        }
    }
    /// <summary>
    /// Stop
    /// </summary>
    public class RemoteControlRequestRunActionHandler : RemoteControlHandler
    {
        public RemoteControlRequestRunActionHandler(RemoteControl remoteControl, string senderID = null, string receiverID = null)
            : base(remoteControl, FuncName.REQUESTRUNACTION, senderID, receiverID)
        {
            LOG.Write($"{remoteControl.Name} execute GetStatus {FuncName.REQUESTRUNACTION} in ASCII.");
        }
    }
}
