using Aitex.Core.Common.DeviceData;
using MECF.Framework.Common.Communications;
using System;
using System.Linq;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.Serens
{
    public abstract class SerenRfMatchHandler : HandlerBase
    {
        public SerenRfMatch Device { get; }

        private string _command;

        protected SerenRfMatchHandler(SerenRfMatch device, string command)
            : base($"{command}\r")
        {
            Device = device;
            _command = command;
        }

        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            SerenRfMatchMessage response = msg as SerenRfMatchMessage;
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

        protected virtual void ParseData(SerenRfMatchMessage msg)
        {

        }
    }
    public class SerenRfMatchGotoHandler : SerenRfMatchHandler
    {
        public SerenRfMatchGotoHandler(SerenRfMatch device)
            : base(device, "GOTO")
        {
            Name = $"Goto the preset position";
        }

    }
    public class SerenRfMatchSetLoadControlModeHandler : SerenRfMatchHandler
    {
        public SerenRfMatchSetLoadControlModeHandler(SerenRfMatch device, bool isAuto)
            : base(device, isAuto ? "ALD" : "MLD")
        {
            if (isAuto)
            {
                Name = $"Set Load Control Mode to Auto";
            }
            else
            {
                Name = $"Set Load Control Mode to Manual";
            }
        }

    }

    public class SerenRfMatchGetLoadControlModeHandler : SerenRfMatchHandler
    {
        public SerenRfMatchGetLoadControlModeHandler(SerenRfMatch device)
            : base(device, "QAML")
        {

            Name = $"Get Load Control Mode";

        }
        protected override void ParseData(SerenRfMatchMessage msg)
        {
            if (msg.RawMessage.StartsWith( "A"))
                Device.NoteLoadControlMode(true);

            if (msg.RawMessage.StartsWith("M"))
                Device.NoteLoadControlMode(false);
        }
    }

    public class SerenRfMatchSetTuneControlModeHandler : SerenRfMatchHandler
    {
        public SerenRfMatchSetTuneControlModeHandler(SerenRfMatch device, bool isAuto)
            : base(device, isAuto ? "ATN" : "MTN")
        {
            if (isAuto)
            {
                Name = $"Set Tune Control Mode to Auto";
            }
            else
            {
                Name = $"Set Tune Control Mode to Manual";
            }
        }

    }
    public class SerenRfMatchGetTuneControlModeHandler : SerenRfMatchHandler
    {
        public SerenRfMatchGetTuneControlModeHandler(SerenRfMatch device)
            : base(device, "QAMT")
        {

            Name = $"Get Tune Control Mode";

        }
        protected override void ParseData(SerenRfMatchMessage msg)
        {
            if (msg.RawMessage.StartsWith("A"))
                Device.NoteTuneControlMode(true);

            if (msg.RawMessage.StartsWith("M"))
                Device.NoteTuneControlMode(false);
        }
    }

    //Reports the current Load capacitor position, in Percent x10.
    public class SerenRfMatchGetLoadPositionHandler : SerenRfMatchHandler
    {
        public SerenRfMatchGetLoadPositionHandler(SerenRfMatch device )
            : base(device, device.MatchType=="ATS" ? "CPLP" : "LPS?")
        {
            Name = "Get Load Position";
        }

        protected override void ParseData(SerenRfMatchMessage msg)
        {
            if (Device.MatchType == "ATS")
            {
                if (int.TryParse(msg.RawMessage, out int pos))
                    Device.NoteLoadPosition((float)(pos / 10.0));
            }
            else
            {
                if (int.TryParse(msg.RawMessage, out int pos))
                    Device.NoteLoadPosition((float)(pos));
            }

        }
    }

    //Reports the current Tune capacitor position, in Percent x10.
    public class SerenRfMatchGetTunePositionHandler : SerenRfMatchHandler
    {
        public SerenRfMatchGetTunePositionHandler(SerenRfMatch device)
            : base(device, device.MatchType=="ATS" ?  "CPTP" : "TPS?")
        {
            Name = "Get Tune Position";
        }

        protected override void ParseData(SerenRfMatchMessage msg)
        {
            if (Device.MatchType == "ATS")
            {
                if (int.TryParse(msg.RawMessage, out int pos))
                    Device.NoteTunePosition((float)(pos / 10.0));
            }
            else
            {
                if (int.TryParse(msg.RawMessage, out int pos))
                    Device.NoteTunePosition((float)(pos ));
            }

        }
    }
 
    //set load position
    public class SerenRfMatchSetLoadPositionHandler : SerenRfMatchHandler
    {
        public SerenRfMatchSetLoadPositionHandler(SerenRfMatch device, float position)
            : base(device, BuildMessage(position))
        {
            Name = "set load position";
        }


        private static string BuildMessage(float position)
        {
            position = Math.Max(0, Math.Min(100, position));
            int value = (int)position * 10 ;
            return $"{value} MVLD";
        }
    }

    //set load preset1 position
    //Command syntax: X<sp>MPLS<CR>
    //Where: “X” is the desired Load Capacitor Preset Position 1 value, in percent.
    //1 to 5 digits, range: 0.0 to 100

    public class SerenRfMatchSetLoadPreset1PositionHandler : SerenRfMatchHandler
    {
        public SerenRfMatchSetLoadPreset1PositionHandler(SerenRfMatch device, float position)
            : base(device, BuildMessage(device, position))
        {
            Name = "set load preset1 position";
        }

        private static string BuildMessage(SerenRfMatch device, float position)
        {
            if (device.MatchType == "ATS")
            {
                position = Math.Max(0, Math.Min(100, position));
                return $"{position:F1} MPLS";
            }
            else
            {
                int pos = (int)Math.Max(2, Math.Min(98, position));
                return $"{pos} MPL";
            }

        }
    }

    //set tune position
    public class SerenRfMatchSetTunePositionHandler : SerenRfMatchHandler
    {
        public SerenRfMatchSetTunePositionHandler(SerenRfMatch device, float position)
            : base(device, BuildMessage(device, position))
        {
            Name = "set tune position";
        }

        private static string BuildMessage(SerenRfMatch device, float position)
        {
            if (device.MatchType == "ATS")
            {
                position = Math.Max(0, Math.Min(100, position));
                int value = (int)position * 10;
                return $"{value} MVTN";
            }
            else
            {
                int pos = (int)Math.Max(2, Math.Min(98, position));
                return $"{pos} MPT";
            }
        }
    }

    public class SerenRfMatchSetTunePreset1PositionHandler : SerenRfMatchHandler
    {
        public SerenRfMatchSetTunePreset1PositionHandler(SerenRfMatch device, float position)
            : base(device, BuildMessage(device, position))
        {
            Name = "set tune position";
        }

        private static string BuildMessage(SerenRfMatch device, float position)
        {
            if (device.MatchType == "ATS")
            {
                position = Math.Max(0, Math.Min(100, position));
                return $"{position:F1} MPTS";
            }
            else
            {
                int pos = (int) Math.Max(2, Math.Min(98, position));
                return $"{pos} MPT";
            }
        }
    }
 
    public class SerenRfMatchGetMagnitudeErrorHandler : SerenRfMatchHandler
    {
        public SerenRfMatchGetMagnitudeErrorHandler(SerenRfMatch device)
            : base(device, "MAG")
        {
            Name = "Get Magnitude Error";
        }

        protected override void ParseData(SerenRfMatchMessage msg)
        {
            if (int.TryParse(msg.RawMessage, out int error))
                Device.NoteMagnitudeError(error);
        }
    }

    public class SerenRfMatchGetPhaseErrorHandler : SerenRfMatchHandler
    {
        public SerenRfMatchGetPhaseErrorHandler(SerenRfMatch device)
            : base(device, "PHS")
        {
            Name = "Get Phase Error";
        }

        protected override void ParseData(SerenRfMatchMessage msg)
        {
            if (int.TryParse(msg.RawMessage, out int error))
                Device.NotePhaseError(error);
        }
    }

    public class SerenRfMatchGetDCVoltageHandler : SerenRfMatchHandler
    {
        public SerenRfMatchGetDCVoltageHandler(SerenRfMatch device)
            : base(device, device.MatchType=="ATS" ? "QDP":"0?")
        {
            Name = "Get DC Voltage";
        }

        protected override void ParseData(SerenRfMatchMessage msg)
        {
            if (int.TryParse(msg.RawMessage, out int voltage))
                Device.NoteDCVoltage(voltage);
        }
    }

    public class SerenRfMatchGetRFVoltageHandler : SerenRfMatchHandler
    {
        public SerenRfMatchGetRFVoltageHandler(SerenRfMatch device)
            : base(device, device.MatchType == "ATS" ? "QRP":"0?")
        {
            Name = "Get RF Voltage";
        }

        protected override void ParseData(SerenRfMatchMessage msg)
        {
            if (int.TryParse(msg.RawMessage, out int voltage))
                Device.NoteRFVoltage(voltage);
        }
    }
}
