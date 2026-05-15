using MECF.Framework.Simulator.Core.Commons;
using MECF.Framework.Simulator.Core.Driver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Simulator.Core.Pumps.SkySGM
{
    public class SkySGMPumpSimulator:SocketDeviceSimulator
    {
        //private string robotType;
        private string errorMessage;
        private Dictionary<string, double> moveTimes;
        private ReadOnlyCollection<string> arms;
        protected Dictionary<string, string> errorLookup;
        protected int lastError;



        public string ErrorMessage
        {
            get { return errorMessage; }
            set { errorMessage = value; }
        }



        public SkySGMPumpSimulator(int port, int commandIndex, string lineDelimiter, char msgDelimiter, int cmdMaxLength = 4)
            : base(port, commandIndex, lineDelimiter, msgDelimiter,cmdMaxLength)
        {
            //this.robotType = parms["RobotType"].Value;
            this.BinaryDataMode = false;

            AddCommandHandler("@00CON_FDP_ON", HandleCONFDPON);
            AddCommandHandler("@00CON_FDP_OFF", HandleCONFDPOFF);
            AddCommandHandler("@00READ_RUN_PARA", HandleReadRunParameter);

            //AddCommandHandler("CON_FDP_ON", HandleCONFDPON);
            //AddCommandHandler("CON_FDP_OFF", HandleCONFDPOFF);
            //AddCommandHandler("READ_RUN_PARA", HandleReadRunParameter); 

            moveTimes = new Dictionary<string, double>();
            moveTimes["Realistic Delay"] = 1.0;

            arms = new ReadOnlyCollection<string>(new List<string>());
            errorLookup = new Dictionary<string, string>();
            lastError = 0;
        }

        internal void HandleCONFDPON(string msg)
        {

            //if (ErrorMessage == "Home Failed")
            //{
            //    HandleError(ErrorMessage);
            //    return;   
            //}

            //string[] cmdComponents = msg.Split(_msgDelimiter);
            //if (cmdComponents.Length != 2)
            //{

            //    HandleError("Invalid homing command (arguments)");
            //    return;
            //}

            OnWriteMessage("@00FDP_ONOK\0");

        }

        internal void HandleCONFDPOFF(string msg)
        {


            OnWriteMessage("@00FDP_OFFOK\0");

        }

        internal void HandleReadRunParameter(string msg)
        {


            OnWriteMessage("@00RUN_PARA%2ld%02ld%03ld%03ld%02ld%04ld%05ld%c%c%c%c%c%c%\0");

        }

        private void HandleError(string msg)
        {
            string errorCode = string.Format("0x{0}", lastError.ToString("x8"));
            lastError++;
            errorLookup[errorCode] = msg;

            //OnWriteMessage(msgError + " " + ErrorCode);

            //OnWriteMessage(msgDone);
        }


    }
}
