using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.STeam
{
    public class STeamRFMatchHanlder : HandlerBase
    {
        private STeamRFMatch _device = null;
        public STeamRFMatchHanlder(STeamRFMatch device, string command, string parameter, string command1) : base(BuildMessage(ToByteArray(command), StringToByteArray(parameter), ToByteArray(command1)))
        {
            _device = device;
        }

        public STeamRFMatch Device
        {
            get { return _device; }
            set { _device = value; }
        }

        public string _command;
        protected string _parameter;
        public override bool HandleMessage(MessageBase msg, out bool transactionComplete)
        {
            STeamRFMatchMessage msg1 = msg as STeamRFMatchMessage;



            transactionComplete = true;
            return true;
        }

        private static byte _address = 0x01;

        protected static byte[] BuildMessage(byte[] commandArray, byte[] argumentArray1 = null, byte[] argumentArray2 = null)
        {
            List<byte> buffer = new List<byte>();

            //buffer.Add(_address);

            if (commandArray != null && commandArray.Length > 0)
            {
                buffer.AddRange(commandArray);
            }
            if (argumentArray1 != null && argumentArray1.Length > 0)
            {
                buffer.AddRange(argumentArray1);
            }

            if (argumentArray2 != null && argumentArray2.Length > 0)
            {
                buffer.AddRange(argumentArray2);
            }

            return buffer.ToArray();
        }

        protected bool HandleMessage(MessageBase msg)
        {
            STeamRFMatchMessage response = msg as STeamRFMatchMessage;
            if (!response.IsComplete)
                return false;
            return true;
        }


        protected static byte[] ToByteArray(string parameter)
        {
            if (parameter == null)
                return new byte[] { };

            return parameter.Split(',').Select(para => Convert.ToByte(para, 16)).ToArray();
        }

        protected static byte[] StringToByteArray(string parameter)
        {
            if (parameter == null)
                return new byte[] { };
            return System.Text.Encoding.Default.GetBytes(parameter);
        }
    }



    public class STeamRFMatchStartMesurementHandler : STeamRFMatchHanlder
    {
        public STeamRFMatchStartMesurementHandler(STeamRFMatch device)
            : base(device, "80,11", "", null)
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleMessage(msg))
            {
                var result = msg as STeamRFMatchMessage;

                //Byte Name Meaning Note
                //0 HST STeam Status Byte(Section 4.3) A
                //1 HER STeam Error Byte(Section 4.4) H
                //2 PH Coded incident power – MSB H
                //3 PL Coded incident power – LSB H
                //4 PE Coded incident power – exponent H
                //5 TL Internal temperature in units of 0.1 Celsius – LSB H
                //6 TH Internal temperature in units of 0.1 Celsius – MSB H
                //7 RE Coded reflected power – exponent H, S
                //8 XL Coded real part of input(measured) reflection coefficient – LSB H
                //9 XH Coded real part of input(measured) reflection coefficient – MSB H
                //10 YL Coded imaginary part input(measured) of reflection coefficient – LSB H
                //11 YH Coded imaginary part input(measured) of reflection coefficient – MSB H
                //12 F0 Frequency in units of 10 Hz – Byte 0(LSB) H,F
                //13 F1 Frequency in units of 10 Hz – Byte 1 H,F
                //14 F2 Frequency in units of 10 Hz – Byte 2 H,F
                //15 F3 Frequency in units of 10 Hz – Byte 3(MSB) H,F
                //16 DXL Coded real part of load reflection coefficient – LSB H
                //17 DXH Coded real part of load reflection coefficient – MSB H
                //18 DYL Coded imaginary part of load reflection coefficient – LSB H
                //19 DYH Coded imaginary part of load reflection coefficient – MSB H
                //20 SRL Sample serial number/ Coded reflected power – LSB H, S
                //21 SRH Sample serial number/ Coded reflected power – MSB H, S
                //22 M1L Motor 1 position – LSB M
                //23 M1H Motor 1 position – MSB M
                //24 M2L Motor 2 position – LSB M
                //25 M2H Motor 2 position – MSB M
                //26 M3L Motor 3 position – LSB M
                //27 M3H Motor 3 position – MSB M
                //28 MS1 Motor Status, Part 1(Section 4.5) M
                //29 MS2 Motor Status, Part 2(Section 4.6) M
                //30 CS Checksum byte A
                if (result.Data != null)
                {





                }

                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }



    public class STeamRFMatchSetAutotuningParameterHandler : STeamRFMatchHanlder
    {
        public STeamRFMatchSetAutotuningParameterHandler(STeamRFMatch device, int toler, int skip, bool waitTol, bool isOnWaitRF, int tager)
            : base(device, "80,1C", $"ATP {toler} {skip} {(waitTol ? "Y" : "N")} {(isOnWaitRF ? "Y" : "N")} {tager}", "0D,0,80,49")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleMessage(msg))
            {
                var result = msg as STeamRFMatchMessage;
                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }

    public class STeamRFMatchSetHysteresisParameterHandler : STeamRFMatchHanlder
    {
        public STeamRFMatchSetHysteresisParameterHandler(STeamRFMatch device, int hysterval)
            : base(device, "80,1C", $"TSO 1 {hysterval}", "0D,0A,80,60")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleMessage(msg))
            {
                var result = msg as STeamRFMatchMessage;
                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }

    public class STeamRFMatchSetContinuousAutotuningHandler : STeamRFMatchHanlder
    {
        public STeamRFMatchSetContinuousAutotuningHandler(STeamRFMatch device, AutotuningEnum autotuning)
            : base(device, "80,1C", $"ATC {((int)autotuning)}", "0D,0A,80,48")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleMessage(msg))
            {
                var result = msg as STeamRFMatchMessage;
                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }

    public class STeamRFMatchSingleAutotuningStepHandler : STeamRFMatchHanlder
    {
        public STeamRFMatchSingleAutotuningStepHandler(STeamRFMatch device)
            : base(device, "80,1C", $"ATC S", "0D,0A,80,48")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleMessage(msg))
            {
                var result = msg as STeamRFMatchMessage;
                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }

    public class STeamRFMatchSendTunePosOnorOffHandler : STeamRFMatchHanlder
    {
        public STeamRFMatchSendTunePosOnorOffHandler(STeamRFMatch device, bool isOn)
            : base(device, "80,1C", $"ATC {(isOn ? "F" : "T")}", "0D,0A,80,48")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleMessage(msg))
            {
                var result = msg as STeamRFMatchMessage;
                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }

    public class STeamRFMatchSetAlarmParameterHandler : STeamRFMatchHanlder
    {
        /// <summary>
        /// Alarm参数
        /// </summary>
        /// <param name="device"></param>
        /// <param name="magLimit">0-100</param>
        /// <param name="insLimit">0-100</param>
        /// <param name="eval">0-4</param>
        /// <param name="isOnMotMove"></param>
        public STeamRFMatchSetAlarmParameterHandler(STeamRFMatch device, int magLimit, int insLimit, int eval, bool isOnMotMove)
            : base(device, "80,1C", $"SSA {magLimit} {insLimit} {eval} {(isOnMotMove ? "1" : "0")}", "0D,0A,80,5C")
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            if (HandleMessage(msg))
            {
                var result = msg as STeamRFMatchMessage;
                handled = true;
                return true;
            }

            handled = false;
            return false;
        }
    }


    public enum WaveFormEnum
    {
        CW = 0,
        Rectified,
        Pulused
    }

    public enum AutotuningEnum
    {
        OFF = 0,
        ON,
        QueryAutotunningState
    }
}

