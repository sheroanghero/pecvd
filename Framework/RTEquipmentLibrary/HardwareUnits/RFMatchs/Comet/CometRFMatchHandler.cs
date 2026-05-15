using Aitex.Core.RT.Log;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.Comet
{

    public enum RFMatchResponseResult
    {
        InvalidResonse,
        TriggerAck,
        TriggerNAK,
        CheckInProcess,
        CheckCompleted,
        NotTrigger,
    }
    public abstract class CometRFMatchHandler : HandlerBase
    {
        public CometRFMatch Device { get; }

        public string _command;
        protected string _parameter;
        protected string _completeEvent;

        protected CometRFMatchHandler(CometRFMatch device, string command, string completeEvent, string parameter)
            : base(BuildMessage(ToByteArray(command), ToByteArray(parameter)))
        {
            Device = device;
            _command = command;
            _completeEvent = completeEvent;
            _parameter = parameter;
            _address = 0x21;
            Name = command;
        }

        protected CometRFMatchHandler(CometRFMatch device, string command)
            : base(BuildMessage(ToByteArray(command)))
        {
            Device = device;
            _command = command;
            _address = 0x21;
            Name = command;
        }


        protected CometRFMatchHandler(CometRFMatch device, string command, ushort parameter)
            : base(BuildMessage(ToByteArray(command), UshortToByteArray(parameter)))
        {
            Device = device;
            _command = command;
            _parameter = parameter.ToString();
            _address = 0x21;
            Name = command;
        }

        protected CometRFMatchHandler(CometRFMatch device, string command, string parameter)
            : base(BuildMessage(ToByteArray(command), ToByteArray(parameter)))
        {
            Device = device;
            _command = command;
            _parameter = parameter;
            _address = 0x21;
            Name = command;
        }

        protected CometRFMatchHandler(CometRFMatch device, string command, byte[] parameter)
        : base(BuildMessage(ToByteArray(command), parameter))
        {
            Device = device;
            _command = command;
            //_parameter = parameter;
            Name = command;
        }

        private static byte _start = 0xAA;
        private static byte _address = 0x21;

        protected static byte[] BuildMessage(byte[] commandArray, byte[] argumentArray = null)
        {
            List<byte> buffer = new List<byte>();

            buffer.Add(_start);
            buffer.Add(_address);

            if (commandArray != null && commandArray.Length > 0)
            {
                buffer.AddRange(commandArray);
            }
            if (argumentArray != null && argumentArray.Length > 0)
            {
                buffer.AddRange(argumentArray.Reverse());// Little Endian format
            }
            byte checkSum = 0;
            for (int i = 0; i < buffer.Count; i++)
            {
                checkSum += buffer[i];
            }
            buffer.Add(checkSum);

            return buffer.ToArray();
        }

        bool _startWaitCheckResonse = false;
        protected RFMatchResponseResult HandleTriggerMessage(MessageBase msg)
        {
            CometRFMatchMessage response = msg as CometRFMatchMessage;

            string triggerOK = "50";
            string triggerNG = "93";
            string[] checkResponseArray = new string[] { "53", "52", "51" };

            string reponseData = string.Join(",", response.Data.Select(bt => bt.ToString("X2")).ToArray());

            if (!_startWaitCheckResonse)
            {
                if (reponseData.IndexOf(triggerOK) >= 0)
                {
                    _startWaitCheckResonse = true;
                    return RFMatchResponseResult.TriggerAck;
                }
                else if (reponseData.IndexOf(triggerNG) >= 0)
                {
                    Device.NoteNAK();
                    return RFMatchResponseResult.TriggerNAK;
                }
                else
                {
                    Device.NoteError($"Invalid response, no expected command feedback: {reponseData}");
                    msg.IsFormatError = true;
                    return RFMatchResponseResult.InvalidResonse;
                }
            }
            else
            {
                var subRes = reponseData.Substring(0, checkResponseArray[0].Length);
                if (checkResponseArray.Contains(subRes))
                {
                    if (subRes == checkResponseArray[2])
                    {
                        return RFMatchResponseResult.CheckCompleted;
                    }
                    else if (subRes == checkResponseArray[1])
                    {
                        return RFMatchResponseResult.CheckInProcess;
                    }
                    else
                    {
                        return RFMatchResponseResult.NotTrigger;
                    }
                }
                else
                {
                    Device.NoteError($"Invalid response, no expected command feedback: {reponseData}");
                    msg.IsFormatError = true;
                    return RFMatchResponseResult.InvalidResonse;
                }
            }
        }

        protected bool HandleSetMessage(MessageBase msg)
        {
            CometRFMatchMessage response = msg as CometRFMatchMessage;

            string setOK = _command;
            string setNG = "94";
            string[] checkResponseArray = new string[] { "53", "52", "51" };

            string reponseData = string.Join(",", response.Data.Select(bt => bt.ToString("X2")).ToArray());
            if (reponseData.IndexOf(setOK) >= 0)
            {
                return true;
            }
            else if (reponseData.IndexOf(setNG) >= 0)
            {
                Device.NoteNAK();
                return false;
            }
            else
            {
                Device.NoteError($"Invalid response, no expected command feedback: {reponseData}");
                msg.IsFormatError = true;
                return false;
            }
        }

        protected bool HandleGetMessage(MessageBase msg)
        {
            CometRFMatchMessage response = msg as CometRFMatchMessage;
            if (!response.IsComplete)
                return false;

            string setOK = _completeEvent;
            if (response.Data == null || response.Data.Length <= 2)
            {
                return false;
            }
            byte checkSum_calc = 0;
            for (int ii = 0; ii < response.RawMessage.Length - 1; ii++)
            {
                checkSum_calc += response.RawMessage[ii];
            }
            byte checkSum = response.RawMessage[response.RawMessage.Length - 1];
            if (checkSum_calc != checkSum)
            {
                LOG.Warning($"{Device.Address} Invalid check sum"); //??? more detail information
                return false;
            }

            string reponseData = string.Join(",", response.Data.Take(2).Select(bt => bt.ToString("X2")).ToArray());
            if (reponseData.IndexOf(setOK) >= 0)
            {
                return true;
            }
            else
            {
                Device.NoteError($"Invalid response, no expected command feedback: {reponseData}");
                msg.IsFormatError = true;
                return false;
            }
        }

        protected static byte[] ToByteArray(string parameter)
        {
            if (parameter == null)
                return new byte[] { };

            return parameter.Split(',').Select(para => Convert.ToByte(para, 16)).ToArray();
        }
        protected static byte[] IntToByteArray(int num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            return bytes;
        }
        protected static byte[] UshortToByteArray(ushort num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            return bytes;
        }
        protected int ByteArrayToInt(byte[] bytes)
        {
            if (bytes.Length == 4)
            {
                int temp = BitConverter.ToInt32(bytes, 0);
                return temp;
            }
            return 0;
        }
        protected short ByteArrayToShort(byte[] bytes)
        {
            if (bytes.Length == 2)
            {
                short temp = BitConverter.ToInt16(bytes, 0);
                return temp;
            }
            return 0;
        }

        protected ushort ByteArrayToUshort(byte[] bytes)
        {
            if (bytes.Length == 2)
            {
                ushort temp = BitConverter.ToUInt16(bytes, 0);
                return temp;
            }
            return 0;
        }
    }

    public class CommetRFMatchRawCommandHandler : CometRFMatchHandler
    {
        public CommetRFMatchRawCommandHandler(CometRFMatch device, string command, string completeEvent, string parameter)
            : base(device, command, completeEvent, parameter)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            CometRFMatchMessage response = msg as CometRFMatchMessage;
            ResponseMessage = msg;

            string reponseData = string.Join(",", response.Data.Select(bt => bt.ToString("X2")).ToArray());
            if (_completeEvent.Contains("|"))
            {
                var eventArray = _completeEvent.Split('|');
                var subRes = reponseData.Substring(0, eventArray[0].Length);
                if (!eventArray.Contains(subRes))
                {
                    Device.NoteError($"Invalid response, no expected command feedback: {reponseData}");
                    msg.IsFormatError = true;

                    transactionComplete = false;
                    return false;
                }
            }
            else if (reponseData.IndexOf(_completeEvent) == -1)
            {
                Device.NoteError($"Invalid response, no expected command feedback: {reponseData}");
                msg.IsFormatError = true;

                transactionComplete = false;
                return false;
            }


            if (response.IsAck)
            {
                SetState(EnumHandlerState.Acked);
            }
            if (this.IsAcked && response.IsComplete)
            {
                SetState(EnumHandlerState.Completed);
                transactionComplete = true;
                Device.NoteRawCommandInfo(_command, string.Join(",", response.RawMessage.Select(bt => bt.ToString("X2")).ToArray()));
                return true;
            }

            transactionComplete = false;
            return false;
        }
    }

    public class CommetRFMatchTriggerFullRefRunHandler : CometRFMatchHandler
    {
        public CommetRFMatchTriggerFullRefRunHandler(CometRFMatch device)
            : base(device, "10,00")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var resonseResult = HandleTriggerMessage(msg);
            if (resonseResult == RFMatchResponseResult.TriggerAck || resonseResult == RFMatchResponseResult.TriggerNAK)
            {
                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }

    public class CommetRFMatchCheckFullRefRunHandler : CometRFMatchHandler
    {
        public CommetRFMatchCheckFullRefRunHandler(CometRFMatch device)
            : base(device, "10,01")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var resonseResult = HandleTriggerMessage(msg);
            if (resonseResult == RFMatchResponseResult.NotTrigger || resonseResult == RFMatchResponseResult.CheckInProcess)
            {
                handled = true;
                return true;
            }
            else if (resonseResult == RFMatchResponseResult.CheckCompleted)
            {
                Device.NoteFullRefRunCompleted();
                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }

    public class CommetRFMatchTriggerGotoCapHandler : CometRFMatchHandler
    {
        private bool IsLastSetCap = false;
        public CommetRFMatchTriggerGotoCapHandler(CometRFMatch device, float cap, bool isLastSetCap)
            : base(device, "20,00", (ushort)(cap * 10))
        {
            IsLastSetCap = isLastSetCap;
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var resonseResult = HandleTriggerMessage(msg);

            if (resonseResult == RFMatchResponseResult.TriggerAck || resonseResult == RFMatchResponseResult.TriggerNAK)
            {
                handled = true;
                return true;
            }
            handled = false;
            (Device as CometRFMatch).SetLastSetCap();
            return false;
        }
    }

    public class CommetRFMatchCheckGotoCapCompleteHandler : CometRFMatchHandler
    {
        public CommetRFMatchCheckGotoCapCompleteHandler(CometRFMatch device)
            : base(device, "20,01")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var resonseResult = HandleTriggerMessage(msg);
            if (resonseResult == RFMatchResponseResult.NotTrigger || resonseResult == RFMatchResponseResult.CheckInProcess)
            {
                handled = true;
                return true;
            }
            else if (resonseResult == RFMatchResponseResult.CheckCompleted)
            {
                if (Device.IsLastSetCap) Device.NoteGotoCapCompleted();
                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }


    public class CommetRFMatchSetAccelerationSpeedIndexHandler : CometRFMatchHandler
    {
        public CommetRFMatchSetAccelerationSpeedIndexHandler(CometRFMatch device, string parameter)
            : base(device, "43,00", parameter)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleSetMessage(msg))
            {
                Device.NoteAccelerationSpeedIndex(_parameter);
            }
            handled = true;
            return true;
        }
    }

    public class CommetRFMatchGetActualCapHandler : CometRFMatchHandler
    {
        public CommetRFMatchGetActualCapHandler(CometRFMatch device)
            : base(device, "40,01", "41,01", null)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleGetMessage(msg))
            {
                var result = msg as CometRFMatchMessage;
                var capArr = result.Data.Skip(2).Take(2).ToList();
                capArr.Reverse();//// Little Endian format
                Device.NoteActualCap(ByteArrayToUshort(capArr.ToArray()) / 10);

                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }

    public class CommetRFMatchGetMinimumCapHandler : CometRFMatchHandler
    {
        public CommetRFMatchGetMinimumCapHandler(CometRFMatch device)
            : base(device, "40,10", "41,10", null)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleGetMessage(msg))
            {
                var result = msg as CometRFMatchMessage;
                //Device.NoteActualCap(ByteArrayToUshort(result.Data.Skip(2).Take(2).ToArray()));

                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }

    public class CommetRFMatchGetMaximumCapHandler : CometRFMatchHandler
    {
        public CommetRFMatchGetMaximumCapHandler(CometRFMatch device)
            : base(device, "40,11", "41,11", null)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleGetMessage(msg))
            {
                var result = msg as CometRFMatchMessage;
                //Device.NoteActualCap(ByteArrayToUshort(result.Data.Skip(2).Take(2).ToArray()));

                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }

    public class CommetRFMatchGetStatusHandler : CometRFMatchHandler
    {
        public CommetRFMatchGetStatusHandler(CometRFMatch device)
            : base(device, "40,22", "41,22", null)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleGetMessage(msg))
            {
                var result = msg as CometRFMatchMessage;
                var status = result.Data.Skip(2).Take(2).ToArray()[0];
                if (status != 0x00)//ox00 no error
                {
                    var reason = string.Empty;
                    foreach (var key in Device.ErrorCodeReference.Keys)
                    {
                        if ((key & status) > 0)
                            reason += Device.ErrorCodeReference[key] + ",";
                    }
                    Device.NoteError(reason);
                }

                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }
}
