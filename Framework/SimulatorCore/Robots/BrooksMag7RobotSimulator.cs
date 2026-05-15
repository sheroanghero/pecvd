using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Timers;
using MECF.Framework.Simulator.Core.SubstrateTrackings;

namespace MECF.Framework.Simulator.Core.Robots
{

    public class BrooksMag7RobotSimulator : RobotSimulator
    {
        protected Random _rd = new Random();
        public bool Failed { get; set; }
        public string ErrorCode { get; set; }

        public bool EventChecked { get; set; }
        public string EventCode { get; set; }

        public override Dictionary<string, double> MoveTimes
        {
            get { return moveTimes; }
            set { moveTimes = value; }
        }

        public override ReadOnlyCollection<string> Arms
        {
            get { return arms; }
        }

        //private static string source = "BrooksMag7";
        private static string msgDone = "_RDY";
        private static string msgError = "_ERR";
        private readonly string armAPan1 = "VTM.ArmA.Left";
        private readonly string armAPan2 = "VTM.ArmA.Right";
        private readonly string armBPan1 = "VTM.ArmB.Left";
        private readonly string armBPan2 = "VTM.ArmB.Right";
        private System.Timers.Timer timer;
        private string currentStation;
        private string newLocation;
        private string currentArm;
        private string newArm = "";
        private string lastMsg;
        private Dictionary<string, double> moveTimes;
        private ReadOnlyCollection<string> arms;


        public BrooksMag7RobotSimulator(int port = 1102) : base(port, 0, "\r", ' ', 5)
        {

            List<string> armsList = new List<string>();
            armsList.Add(armAPan1);
            armsList.Add(armAPan2);
            armsList.Add(armBPan1);
            armsList.Add(armBPan2);
            arms = new ReadOnlyCollection<string>(armsList);

            // create the message handling dictionary
            AddCommandHandler("HOME", HandleHome);
            AddCommandHandler("PICK", HandlePick);
            AddCommandHandler("PLACE", HandlePlace);
            AddCommandHandler("GOTO", HandleGoto);
            AddCommandHandler("SWAP", HandleExchange);
            AddCommandHandler("RQ", HandleRequest);
            AddCommandHandler("Unknown", HandleUnknown);

            //AddCommandHandler("SVON", HandleSVON);
            // AddCommandHandler("SVOFF", HandleSVOFF);


            timer = new System.Timers.Timer();
            timer.Enabled = false;
            timer.AutoReset = false;
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);

            // Default move times based on Brooks spreadsheet
            moveTimes = new Dictionary<string, double>();
            moveTimes["PickPlace"] = 2.8;
            moveTimes["Move90Degrees"] = 1.69;
            moveTimes["Move180Degrees"] = 2.11;
            moveTimes["HeightAdjust"] = 0.9;
            moveTimes["ExtendRetract"] = 1.3;
            moveTimes["WaferFactor"] = 1.0;
            moveTimes["SwapAtPM"] = 7.3;

            // Original default times based on Brooks log files
            //moveTimes["PickPlace"] = 3.2;
            //moveTimes["Move90Degrees"] = 1.5;
            //moveTimes["Move180Degrees"] = 1.9;
            //moveTimes["HeightAdjust"] = 0.9;
            //moveTimes["ExtendRetract"] = 1.2;
            //moveTimes["WaferFactor"] = 1.15;

            currentArm = "A";

            currentStation = "Unknown";


        }

        internal void HandleSVON(string msg)
        {

            if (ErrorMessage == "SVON Failed")
            {
                HandleError(ErrorMessage);
                return;
            }

            string[] cmdComponents = msg.Split(_msgDelimiter);
            if (cmdComponents.Length != 1)
            {

                HandleError("Invalid SVON command (arguments)");
                return;
            }
            HandleMove(RobotStateEnum.SVON, cmdComponents);

        }

        internal void HandleSVOFF(string msg)
        {

            if (ErrorMessage == "SVOFF Failed")
            {
                HandleError(ErrorMessage);
                return;
            }

            string[] cmdComponents = msg.Split(_msgDelimiter);
            if (cmdComponents.Length != 1)
            {

                HandleError("Invalid SVOFF command (arguments)");
                return;
            }
            HandleMove(RobotStateEnum.SVOFF, cmdComponents);

        }

        internal void HandleHome(string msg)
        {
            if (ErrorMessage == "Home Failed")
            {
                HandleError(ErrorMessage);
                return;
            }

            string[] cmdComponents = msg.Split(_msgDelimiter);
            if (cmdComponents.Length != 2)
            {

                HandleError("Invalid homing command (arguments)");
                return;
            }
            HandleMove(RobotStateEnum.Homing, cmdComponents);

        }


        internal void HandlePick(string msg)
        {

            msg = msg.Trim();
            if (ErrorMessage == "Pick Failed")
            {
                HandleError(ErrorMessage);
                return;
            }

            if (Failed && !string.IsNullOrEmpty(ErrorCode))
            {
                HandleError(ErrorMessage);
                return;
            }
            if (EventChecked && !string.IsNullOrEmpty(EventCode))
            {
                HandleEvent(EventCode);
                return;
            }
            string[] cmdComponents = msg.Split(_msgDelimiter);
            if (cmdComponents.Length != 5 && cmdComponents.Length != 6 && cmdComponents.Length != 8 && cmdComponents.Length != 10)
            {
                HandleError("Invalid pick command (arguments)");
                return;
            }

            RobotStateEnum rs = RobotStateEnum.Picking;
            if (cmdComponents.Length > 6 && cmdComponents[6] == "ENRT")
                rs = RobotStateEnum.Extending;
            else if (cmdComponents.Length > 6 && cmdComponents[6] == "STRT")
                rs = RobotStateEnum.Retracting;

            lastMsg = msg;

            HandleMove(rs, cmdComponents);

        }

        internal void HandlePlace(string msg)
        {

            msg = msg.Trim();
            if (ErrorMessage == "Place Failed")
            {
                HandleError(ErrorMessage);
                return;
            }


            if (Failed && !string.IsNullOrEmpty(ErrorCode))
            {
                HandleError(ErrorMessage);
                return;
            }
            if (EventChecked && !string.IsNullOrEmpty(EventCode))
            {
                HandleEvent(EventCode);
                return;
            }
            string[] cmdComponents = msg.Split(_msgDelimiter);
            if (cmdComponents.Length != 6 && cmdComponents.Length != 8 && cmdComponents.Length != 10)
            {

                HandleError("Invalid place command (arguments)");
                return;
            }

            RobotStateEnum rs = RobotStateEnum.Placing;
            if (cmdComponents.Length > 6 && cmdComponents[6] == "ENRT")
                rs = RobotStateEnum.Extending;
            else if (cmdComponents.Length > 6 && cmdComponents[6] == "STRT")
                rs = RobotStateEnum.Retracting;

            lastMsg = msg;

            HandleMove(rs, cmdComponents);

        }

        internal void HandleExchange(string msg)
        {

            if (ErrorMessage == "Swap Failed" || ErrorMessage == "Place Failed" || ErrorMessage == "Pick Failed")
            {
                HandleError(ErrorMessage);
                return;
            }

            string[] cmdComponents = msg.Split(_msgDelimiter);
            if (cmdComponents.Length != 4)
            {

                HandleError("Invalid swap command (arguments)");
                return;
            }
            lastMsg = msg;


            HandleMove(RobotStateEnum.Exchanging, cmdComponents);

        }


        internal void HandleRequest(string msg)
        {
            string[] components = msg.Split(_msgDelimiter);
            string reply = string.Empty;
            if (components[1] == "WAFER" && components[2] == "PRESENT")
                reply = components[1] + " " + components[2] + GetArmStates();
            else if (components[1] == "ALGN" && components[3] == "OFFSETS")
                reply = "RO " + "11 " + "TO " + "22";
            else if (components[1] == "ERRMSG")
                reply = LookupError(components[2]);


            OnWriteMessage(reply);

            OnWriteMessage(msgDone);
        }


        internal void HandleUnknown(string msg)
        {

            OnWriteMessage(msgDone);
        }


        internal void HandleGoto(string msg)
        {

            string[] cmdComponents = msg.Split(_msgDelimiter);
            if (cmdComponents.Length != 11)
            {

                HandleError("Invalid move command (arguments)");
                return;
            }
            HandleMove(RobotStateEnum.Approaching, cmdComponents);

        }

        private bool HandleMove(RobotStateEnum action, string[] cmdComponents)
        {

            if (robotStateArgs.State != RobotStateEnum.Idle &&
                (action == RobotStateEnum.Homing && robotStateArgs.State != RobotStateEnum.Errored))  // allow homes when in error, but not other moves
            {

                HandleError("Already moving");
                return false;
            }

            newLocation = "Unknown";
            switch (action)
            {
                case RobotStateEnum.Picking:
                case RobotStateEnum.Placing:
                case RobotStateEnum.Approaching:
                case RobotStateEnum.Extending:
                case RobotStateEnum.Retracting:
                case RobotStateEnum.Exchanging:
                case RobotStateEnum.SVON:
                case RobotStateEnum.SVOFF:
                    break;

                default:

                    break;
            }


            if (ErrorMessage != "Pick Failed" && ErrorMessage != "Place Failed" && ErrorMessage != "Home Failed" &&
                ErrorMessage != "Swap Failed" && !string.IsNullOrEmpty(ErrorMessage))
            {
                HandleError(ErrorMessage);
                return false;
            }

            double delay = GetMoveTime(action);


            timer.Interval = delay * 1000;
            timer.Enabled = true;


            return true;

        }


        private double GetMoveTime(RobotStateEnum action)
        {


            double rotationTime = 0;
            double zMoveTime = 0;
            if (newLocation != currentStation)
            {
                if (currentStation == "LL1" || currentStation == "LL2")
                    rotationTime = newLocation == "PM2" ? moveTimes["Move180Degrees"] : moveTimes["Move90Degrees"];
                else
                    rotationTime = currentStation == "PM2" ? moveTimes["Move180Degrees"] : moveTimes["Move90Degrees"];
                double factor = 1.0;
                if (WaferTrack.Instance.IsOccupied(armAPan1) || WaferTrack.Instance.IsOccupied(armAPan2) ||
                    WaferTrack.Instance.IsOccupied(armBPan1) || WaferTrack.Instance.IsOccupied(armBPan2))
                {
                    factor = moveTimes["WaferFactor"];
                }

                rotationTime *= factor;
            }
            else if (newArm != currentArm)
                zMoveTime = moveTimes["HeightAdjust"];

            switch (action)
            {
                case RobotStateEnum.Approaching:
                    if (newLocation == currentStation)
                        return moveTimes["HeightAdjust"];

                    return rotationTime;

                case RobotStateEnum.Extending:
                case RobotStateEnum.Retracting:
                    return moveTimes["ExtendRetract"] + rotationTime + zMoveTime;

                case RobotStateEnum.Picking:
                    return moveTimes["PickPlace"] + zMoveTime;

                case RobotStateEnum.Placing:
                    if (newLocation.StartsWith("PM"))
                        return moveTimes["PickPlace"] + zMoveTime;
                    else
                        return moveTimes["ExtendRetract"] + rotationTime + zMoveTime;

                case RobotStateEnum.Exchanging:
                    return moveTimes["SwapAtPM"];

                default:
                    return moveTimes["Move90Degrees"];
            }
        }


        private void HandleError(string msg)
        {
            string errorCode = string.Format("0x{0}", lastError.ToString("x8"));
            lastError++;
            errorLookup[errorCode] = msg;

            OnWriteMessage(msgError + " " + ErrorCode);

            OnWriteMessage(msgDone);
        }
        private void HandleEvent(string msg)
        {
           // string errorCode = string.Format("0x{0}", lastError.ToString("x8"));
            //lastError++;
            //errorLookup[errorCode] = msg;

            OnWriteMessage(msg);

            OnWriteMessage(msgDone);
        }

        private string GetArmStates()
        {
            return " N N";


        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            currentStation = newLocation;
            currentArm = newArm;

            if (robotStateArgs.State == RobotStateEnum.Picking || robotStateArgs.State == RobotStateEnum.Placing ||
                      robotStateArgs.State == RobotStateEnum.Extending || robotStateArgs.State == RobotStateEnum.Exchanging)
            {
                lastMsg = "";
            }
            SetRobotState(RobotStateEnum.Idle);
            timer.Enabled = false;
            OnWriteMessage(msgDone);

        }



    }
}
