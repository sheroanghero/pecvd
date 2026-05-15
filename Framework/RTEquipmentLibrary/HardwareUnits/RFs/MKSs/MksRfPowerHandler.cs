using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.Communications;
using System.Collections.Generic;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.Mkss
{
    public abstract class MksRfPowerHandler : HandlerBase
    {
        public MksRfPower Device { get; }

        private string _command;

        protected MksRfPowerHandler(MksRfPower device, string command)
            : base($"{command}\r")
        {
            Device = device;
            _command = command;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            MksRfPowerMessage response = msg as MksRfPowerMessage;
            ResponseMessage = msg;

            if (response.RawMessage.Length >= 1)
            {
                ParseData(response);
            }

            SetState(EnumHandlerState.Acked);
            SetState(EnumHandlerState.Completed);

            transactionComplete = true;
            return true;
        }

        protected virtual void ParseData(MksRfPowerMessage msg)
        {

        }
    }

    public class MksRfPowerSwitchOnOffHandler : MksRfPowerHandler
    {
        public MksRfPowerSwitchOnOffHandler(MksRfPower device, bool isOn)
            : base(device, isOn ? "TRG" : "OFF")
        {
            Name = "Switch " + (isOn ? "On" : "Off");
        }
    }


    //3 regulation mode
    public class MksRfPowerSetRegulationModeHandler : MksRfPowerHandler
    {
        public MksRfPowerSetRegulationModeHandler(MksRfPower device, byte address, EnumRfPowerRegulationMode mode)
            : base(device, "")
        {
            Name = "set regulation mode";
        }

        private static byte GetMode(EnumRfPowerRegulationMode mode)
        {
            switch (mode)
            {
                case EnumRfPowerRegulationMode.DcBias:
                    return 8;
                case EnumRfPowerRegulationMode.Forward:
                    return 6;
                case EnumRfPowerRegulationMode.Load:
                    return 7;
                case EnumRfPowerRegulationMode.VALimit:
                    return 9;
            }

            return 0;
        }
    }

    //8 set power
    public class MksRfPowerSetPowerHandler : MksRfPowerHandler
    {
        public MksRfPowerSetPowerHandler(MksRfPower device, int power)
            : base(device, $"OEM{power}")
        {
            Name = "set power";
        }
    }
 
    //  query status 
    public class MksRfPowerQueryStatusHandler : MksRfPowerHandler
    {
        public MksRfPowerQueryStatusHandler(MksRfPower device)
            : base(device, "ACT")
        {
            Name = "query status";
        }

        //Response: Displays desired power setpoint, forward power, reverse power, PA voltage, and RF frequency.
        protected override void ParseData(MksRfPowerMessage response)
        {
            string[] splitData = response.RawMessage.TrimEnd('\r').Split(' ');
            List<string> dataLst = new List<string>();
            for(int i = 0; i< splitData.Length;i++)
            {
                if (!string.IsNullOrEmpty(splitData[i]))
                    dataLst.Add(splitData[i]);
            }
            if (dataLst.Count != 5)
            {
                Device.NoteError($"{Name}, return data {response.RawMessage} is not valid");
            }
            else
            {
                Device.NotePowerSetPoint(int.Parse(dataLst[0]));
                Device.NoteForwardPower(int.Parse(dataLst[1]));
                Device.NoteReflectPower(int.Parse(dataLst[2]));
                Device.NoteVoltage(int.Parse(dataLst[3]));
                Device.NoteFrequency(int.Parse(dataLst[4]));
            }
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            MksRfPowerMessage response = msg as MksRfPowerMessage;
            ResponseMessage = msg;
            if(!string.IsNullOrEmpty(response.RawMessage))
            {
                var temp = response.RawMessage.Replace("\r", "").Replace("\n", "").Replace("*", "");
                if(temp.Length >= 1)
                {
                    if (response.RawMessage.Length >= 1)
                    {
                        ParseData(response);
                    }

                    SetState(EnumHandlerState.Acked);
                    SetState(EnumHandlerState.Completed);

                    transactionComplete = true;
                    return true;
                }
            }

            transactionComplete = false;
            return true;
        }
    }
    public class MksRfPowerSetEchoOffHandler : MksRfPowerHandler
    {
        public MksRfPowerSetEchoOffHandler(MksRfPower device, bool isOn = true)
            : base(device, $"EKO{(isOn ? 0 : 1)}")
        {
            Name = "set power";
        }
    }

}
