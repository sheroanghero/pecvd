using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.ThrottleValves.KITZ
{
    //PCH-C6
    public abstract class KITZHandler : HandlerBase
    {
        public KITZThrottleValve ThrottleValveDevice { get; }

        protected KITZHandler(KITZThrottleValve throttleValve, bool isQuery, string parameter, string data = "")
            : base(BuildMessage(isQuery, parameter, data))
        {
            ThrottleValveDevice = throttleValve;
        }

        private static string BuildMessage(bool isQuery, string parameter, string data)
        {
            string msg = string.Empty;
            if (isQuery)
                msg = parameter.ToLower() + ":";
            else
                msg = parameter.ToUpper() + ":" + data;

            return $"{msg}\r\n";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            ResponseMessage = msg;
            handled = true;
            return true;
        }
    }

    //remote switch
    public class RemoteHandler : KITZHandler
    {
        public RemoteHandler(KITZThrottleValve throttleValve)
            : base(throttleValve, false, "REMOTE")
        {
            Name = "Remote";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as KITZMessage;
            handled = false;
            if (!result.IsResponse) return true;

            if (result.IsError || result.IsFormatError || result.IsNak || result.Command.ToUpper() != Name.ToUpper())
            {
                ThrottleValveDevice.SetError(result.RawMessage);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    //Calibration
    public class CalibrationHandler : KITZHandler
    {
        public CalibrationHandler(KITZThrottleValve throttleValve)
            : base(throttleValve, false, "CAL")
        {
            Name = "Cal";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as KITZMessage;
            handled = false;
            if (!result.IsResponse) return true;

            if (result.IsError || result.IsFormatError || result.IsNak || result.Command.ToUpper() != Name.ToUpper())
            {
                ThrottleValveDevice.SetError(result.RawMessage);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    //Close
    public class CloseHandler : KITZHandler
    {
        public CloseHandler(KITZThrottleValve throttleValve)
            : base(throttleValve, false, "C")
        {
            Name = "C";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as KITZMessage;
            handled = false;
            if (!result.IsResponse) return true;

            if (result.IsError || result.IsFormatError || result.IsNak || result.Command.ToUpper() != Name.ToUpper())
            {
                ThrottleValveDevice.SetError(result.RawMessage);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    //Open
    public class OpenHandler : KITZHandler
    {
        public OpenHandler(KITZThrottleValve throttleValve)
            : base(throttleValve, false, "O")
        {
            Name = "O";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as KITZMessage;
            handled = false;
            if (!result.IsResponse) return true;

            if (result.IsError || result.IsFormatError || result.IsNak || result.Command.ToUpper() != Name.ToUpper())
            {
                ThrottleValveDevice.SetError(result.RawMessage);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    //Position control
    public class SetPositionHandler : KITZHandler
    {
        public SetPositionHandler(KITZThrottleValve throttleValve, float position)
            : base(throttleValve, false, "POS", position.ToString("f1"))
        {
            Name = "POS";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as KITZMessage;
            handled = false;
            if (!result.IsResponse) return true;

            if (result.IsError || result.IsFormatError || result.IsNak || result.Command.ToUpper() != Name.ToUpper())
            {
                ThrottleValveDevice.SetError(result.RawMessage);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    //Pressure control
    public class SetPressureHandler : KITZHandler
    {
        //PRS: (a), (b), (c), (d), (e)
        //a:Map No.
        //b:Table No.
        //c:DB No.
        //d:RAMP Mode No.
        //e:Target pressure
        public SetPressureHandler(KITZThrottleValve throttleValve, float pressure, int dbNo = 1)
            : base(throttleValve, false, "PRS", $"1,1,{dbNo},1," + pressure.ToString("f3"))
        {
            Name = "PRS";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as KITZMessage;
            handled = false;
            if (!result.IsResponse) return true;

            if (result.IsError || result.IsFormatError || result.IsNak || result.Command.ToUpper() != Name.ToUpper())
            {
                ThrottleValveDevice.SetError(result.RawMessage);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    //State
    public class QueryStateHandler : KITZHandler
    {
        public QueryStateHandler(KITZThrottleValve throttleValve)
            : base(throttleValve, true, "sts")
        {
            Name = "sts";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as KITZMessage;
            handled = false;
            if (!result.IsResponse) return true;
            if (result.IsError || result.IsFormatError || result.IsNak || result.Command.ToUpper() != Name.ToUpper() || string.IsNullOrEmpty(result.Parameter))
            {
                ThrottleValveDevice.SetError(result.RawMessage);
            }

            string[] parameters = Regex.Split(result.Parameter, ",");
            if (parameters.Length < 4)
            {
                ThrottleValveDevice.SetError(result.RawMessage);
                return false;
            }

            var accessMode = parameters[0];
            var controlMode = parameters[1];
            var warningInfo = parameters[2];
            var cycleCounter = parameters[3];

            if (controlMode.ToUpper() == "CLOSE")
            {
                ThrottleValveDevice.Mode = PressureCtrlMode.TVClose;
            }
            else if (controlMode.ToUpper() == "OPEN")
            {
                ThrottleValveDevice.Mode = PressureCtrlMode.TVOpen;
            }
            else if (controlMode.ToUpper() == "PRESS")
            {
                ThrottleValveDevice.Mode = PressureCtrlMode.TVPressureCtrl;
            }
            else if (controlMode.ToUpper() == "POS")
            {
                ThrottleValveDevice.Mode = PressureCtrlMode.TVPositionCtrl;
            }
            else if (controlMode.ToUpper() == "CALIB")
            {
                ThrottleValveDevice.Mode = PressureCtrlMode.TVCalib;
            }

            if (warningInfo.Contains("1") && ThrottleValveDevice.Mode != PressureCtrlMode.TVCalib)
            {
                ThrottleValveDevice.SetError(result.RawMessage);
                return false;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class QueryPressureHandler : KITZHandler
    {
        public QueryPressureHandler(KITZThrottleValve throttleValve)
            : base(throttleValve, true, "prs")
        {
            Name = "prs";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as KITZMessage;
            handled = false;
            if (!result.IsResponse) return true;
            if (result.IsError || result.IsFormatError || result.IsNak || result.Command.ToUpper() != Name.ToUpper() || string.IsNullOrEmpty(result.Parameter))
            {
                ThrottleValveDevice.SetError(result.RawMessage);
            }

            if (!float.TryParse(result.Parameter, out float pressure))
            {
                //ThrottleValveDevice.SetErrorCode(1);
            }
            else
            {
                ThrottleValveDevice.PressureFeedback = pressure;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class QueryPositionHandler : KITZHandler
    {
        public QueryPositionHandler(KITZThrottleValve throttleValve)
            : base(throttleValve, true, "pos")
        {
            Name = "pos";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as KITZMessage;
            handled = false;
            if (!result.IsResponse) return true;
            if (result.IsError || result.IsFormatError || result.IsNak || result.Command.ToUpper() != Name.ToUpper() || string.IsNullOrEmpty(result.Parameter))
            {
                ThrottleValveDevice.SetError(result.RawMessage);
            }

            if (!float.TryParse(result.Parameter, out float position))
            {
                //ThrottleValveDevice.SetErrorCode(1);
            }
            else
            {
                ThrottleValveDevice.PositionFeedback = position;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class CalbrationHandler : KITZHandler
    {
        public CalbrationHandler(KITZThrottleValve throttleValve)
            : base(throttleValve, false, "CAL")
        {
            Name = "CAL";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            var result = msg as KITZMessage;
            handled = false;
            if (!result.IsResponse) return true;

            if (result.IsError || result.IsFormatError || result.IsNak || result.Command.ToUpper() != Name.ToUpper())
            {
                ThrottleValveDevice.SetError(result.RawMessage);
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }
}
