using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.Communications;
using System.Collections.Generic;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFs.Mkss
{
    public abstract class MksRfPlasmaGeneratorHandler : HandlerBase
    {
        public MksRfPlasmaGenerator Device { get; }

        private string _command;

        protected MksRfPlasmaGeneratorHandler(MksRfPlasmaGenerator device, string command)
            : base($"{command}")
        {
            Device = device;
            _command = command;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            MksRfPlasmaGeneratorMessage response = msg as MksRfPlasmaGeneratorMessage;
            ResponseMessage = msg;

            if (response.RawMessage.Length >= 1)
            {
                ParseData(response);
            }

            SetState(EnumHandlerState.Acked);
            transactionComplete = false;
            //if (_command != "P")
            //{
                SetState(EnumHandlerState.Completed);

                transactionComplete = true;
            //}
            return true;
        }

        protected virtual void ParseData(MksRfPlasmaGeneratorMessage msg)
        {

        }
    }

    // Enable Disable RS‐232 control
    public class MksRfPlasmaGeneratorEnableHandler : MksRfPlasmaGeneratorHandler
    {
        public MksRfPlasmaGeneratorEnableHandler(MksRfPlasmaGenerator device, bool isEnable)
            : base(device, isEnable ? "E" : "I")
        {
            Name = "Switch " + (isEnable ? "Enable" : "Disable");
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            MksRfPlasmaGeneratorMessage response = msg as MksRfPlasmaGeneratorMessage;
            ResponseMessage = msg;
            if (!string.IsNullOrEmpty(response.RawMessage))
            {
                if (response.RawMessage.EndsWith("*"))     //  E\r\n*
                {
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

    // RF on off
    public class MksRfPlasmaGeneratorSwitchOnOffHandler : MksRfPlasmaGeneratorHandler
    {
        public MksRfPlasmaGeneratorSwitchOnOffHandler(MksRfPlasmaGenerator device, bool isOn)
            : base(device, isOn ? "N" : "F")
        {
            Name = "Switch " + (isOn ? "RF On" : "Rf Off");
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            MksRfPlasmaGeneratorMessage response = msg as MksRfPlasmaGeneratorMessage;
            ResponseMessage = msg;
            if (!string.IsNullOrEmpty(response.RawMessage))
            {
                if (response.RawMessage.EndsWith("*"))     //  N\r\n*   F\r\n*
                {
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


    // Ready set power
    public class MksRfPlasmaGeneratorReadySetPowerHandler : MksRfPlasmaGeneratorHandler
    {
        public MksRfPlasmaGeneratorReadySetPowerHandler(MksRfPlasmaGenerator device)
            : base(device, "P")
        {
            Name = "Ready set power";
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            MksRfPlasmaGeneratorMessage response = msg as MksRfPlasmaGeneratorMessage;
            ResponseMessage = msg;
            if (!string.IsNullOrEmpty(response.RawMessage))
            {
                if (response.RawMessage.StartsWith("P"))     //  P\r\nEnter New Setpoint (000000 - 000750): 
                {
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

    // set power
    public class MksRfPlasmaGeneratorSetPowerHandler : MksRfPlasmaGeneratorHandler
    {
        public MksRfPlasmaGeneratorSetPowerHandler(MksRfPlasmaGenerator device, int power)
            : base(device, $"{power.ToString().PadLeft(6,'0')}")
        {
            Name = "set power";
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            MksRfPlasmaGeneratorMessage response = msg as MksRfPlasmaGeneratorMessage;
            ResponseMessage = msg;
            if (!string.IsNullOrEmpty(response.RawMessage))
            {
                if (response.RawMessage.EndsWith("*"))     //  000100\r\n*
                {
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

    //  query status 
    public class MksRfPlasmaGeneratorQueryStatusHandler : MksRfPlasmaGeneratorHandler
    {
        public MksRfPlasmaGeneratorQueryStatusHandler(MksRfPlasmaGenerator device)
            : base(device, "T")
        {
            Name = "query status";
        }

        //Response: Displays desired power setpoint, forward power, reverse power, PA voltage, and RF frequency.
        protected override void ParseData(MksRfPlasmaGeneratorMessage response)
        {
            string[] splitAllData = response.RawMessage.TrimEnd('*').Replace("\r\n","&").Split('&');
            string[] splitData = splitAllData.Length > 1 ? splitAllData[1].Split(' ') : new string[0];
            List<string> dataLst = new List<string>();
            for(int i = 0; i< splitData.Length;i++)
            {
                if (!string.IsNullOrEmpty(splitData[i]))
                    dataLst.Add(splitData[i]);
            }
            if (dataLst.Count < 6)
            {
                Device.NoteError($"{Name}, return data {response.RawMessage} is not valid");
            }
            else
            {
                Device.NotePowerOnOff(int.Parse(dataLst[0]));
                Device.NotePowerSetPoint(int.Parse(dataLst[1]));
                Device.NoteFault(dataLst[2]);
                Device.NoteStatus(dataLst[3]);
                Device.NoteForwardPower(int.Parse(dataLst[4]));
                Device.NoteReversePower(int.Parse(dataLst[5]));

                //Device.NoteDeliveredPower(int.Parse(dataLst[6]));
                //Device.NoteControllerOutput(int.Parse(dataLst[7]));
                //Device.NotInterface(int.Parse(dataLst[8]));
                //Device.NotePulseHighTime(dataLst[9]);
                //Device.NotePulseLowTime(dataLst[10]);
               
               
            }
        }
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            MksRfPlasmaGeneratorMessage response = msg as MksRfPlasmaGeneratorMessage;
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

}
