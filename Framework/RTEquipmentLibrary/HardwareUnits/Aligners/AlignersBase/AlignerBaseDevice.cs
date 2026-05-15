using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Event;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.AlignersBase
{
    public abstract class AlignerBaseDevice : Entity, IDevice, IEntity
    {
        protected bool IsSimulatorMode
        {
            get
            {
                if (SC.ContainsItem("System.IsSimulatorMode"))
                    return SC.GetValue<bool>("System.IsSimulatorMode");
                return false;
            }
        }
        private OCRReaderBaseDevice[] _ocrs;
        public OCRReaderBaseDevice[] OCRs 
        {
            get => _ocrs;
            set
            {
                _ocrs = value;
                foreach(var ocr in _ocrs)
                {
                    if(ocr!=null)
                        ocr.InstalledModule = RobotModuleName;
                }
            }
        }

        public event Action<bool> ActionDone;

        public event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;
        public bool HasAlarm { get; }

        public bool IsBusy { get; set; }

        public string ErrorCode { get; set; } = "";
        public virtual bool IsReady()
        {
            return AlignerState == AlignerStateEnum.Idle && !IsBusy;
        }
        public object[] CurrentParamter { get; private set; }

        public bool IsOnline { get; set; }
        public WaferSize Size
        {
            get; set;
        } = WaferSize.WS12;

        public virtual WaferAlignerTypeEnum CurrentWaferAlignmentType { get; set; }
        public virtual int AlginerSpeedSetPoint
        {
            get
            {
                if (SC.ContainsItem($"Aligner.{RobotModuleName}.AlignSpeed"))
                    return SC.GetValue<int>($"Aligner.{RobotModuleName}.AlignSpeed");
                return 100;
            }
            set
            {
                if (SC.ContainsItem($"Aligner.{RobotModuleName}.AlignSpeed"))
                    SC.SetItemValue($"Aligner.{RobotModuleName}.AlignSpeed", value);
            }
        }

        public virtual bool IsReversePreAlignDirection
        {
            get
            {
                if (SC.ContainsItem($"Aligner.{RobotModuleName}.ReversePreAlignDirection"))
                    return SC.GetValue<bool>($"Aligner.{RobotModuleName}.ReversePreAlignDirection");
                return false;
            }
        }

        public virtual bool IsReversePostAlignDirection
        {
            get
            {
                if (SC.ContainsItem($"Aligner.{RobotModuleName}.ReversePostAlignDirection"))
                    return SC.GetValue<bool>($"Aligner.{RobotModuleName}.ReversePostAlignDirection");
                return false;
            }
        }

        public virtual bool IsPreAlgin { get; set; }
        public virtual WaferSize GetCurrentWaferSize()
        {
            return Size;
        }

        public virtual double CompensationAngleValue 
        {
            get
            {
                if (SC.ContainsItem($"Aligner.{Name}.CompensationAngle"))
                    return SC.GetValue<double>($"Aligner.{Name}.CompensationAngle");
                return 0;
            }
        }
        public virtual double PostAlignCompensationAngleValue
        {
            get
            {
                double retValue = 0;
                if (SC.ContainsItem($"Aligner.{Name}.PostAlignCompensationAngle"))
                    retValue += SC.GetValue<double>($"Aligner.{Name}.PostAlignCompensationAngle");

                var wafer = WaferManager.Instance.GetWafer(RobotModuleName, 0);
                if (wafer.IsEmpty || !ModuleHelper.IsLoadPort((ModuleName)wafer.DestinationStation))
                    return retValue;
                ModuleName module = (ModuleName)wafer.DestinationStation;
                if(SC.ContainsItem($"LoadPort.{module}.PostAlignAngle"))
                    retValue += SC.GetValue<double>($"LoadPort.{module}.PostAlignAngle");

                return retValue;
            }
        }

        public virtual double PreDefineAngle { get; set; }

        public virtual bool IsGripperHoldWafer { get; set; }


        public bool IsIdle
        {
            get
            {
                return fsm.State == (int)AlignerStateEnum.Idle && fsm.CheckExecuted() && !IsBusy;
            }
        }

        public bool IsError
        {
            get
            {
                return fsm.State == (int)AlignerStateEnum.Error;
            }
        }

        public bool IsInit
        {
            get
            {
                return fsm.State == (int)AlignerStateEnum.Init;
            }
        }
        public double CurrentNotch { get; set; }
        public int SpeedAxis1 { set; get; }
        public int SpeedAxis2 { set; get; }
        public int SpeedAxis3 { set; get; }
        public int SpeedAxis4 { set; get; }
        public int SpeedAxis5 { set; get; }
        public int SpeedAxis6 { set; get; }

        public float PositionAxis1 { get; set; }
        public float PositionAxis2 { get; set; }
        public float PositionAxis3 { get; set; }
        public float PositionAxis4 { get; set; }
        public float PositionAxis5 { get; set; }
        public float PositionAxis6 { get; set; }
        public int AlignerCommandTimeout { get; private set; }
        public virtual float RadialOffset { set; get; }//extend
        public virtual float ThetaOffset { set; get; }//rotate
        public virtual bool IsAlignError { get; set; }
        public int RetryCounter { get; set; }
        public int MaxRetryCounter { get; set; }

        public virtual int TimeLimitAlignWafer
        {
            get
            {
                if (SC.ContainsItem($"Aligner.{Name}.TimeLimitForAlignWafer"))
                    return SC.GetValue<int>($"Aligner.{Name}.TimeLimitForAlignWafer");
                return 90;
            }
        }

        public virtual int TimeLimitAlignerHome
        {
            get
            {
                if (SC.ContainsItem($"Aligner.{Name}.TimeLimitAlignerHome"))
                    return SC.GetValue<int>($"Aligner.{Name}.TimeLimitAlignerHome");
                return 30;
            }
        }
        public virtual int TimeLimitReadOcr
        {
            get
            {
                if (SC.ContainsItem($"Aligner.{Name}.TimeLimitReadOCR"))
                    return SC.GetValue<int>($"Aligner.{Name}.TimeLimitReadOCR");
                return 180;
            }
        }

        public virtual int TimeLimitVerifyOcr
        {
            get
            {
                if (SC.ContainsItem($"Aligner.{Name}.TimeLimitVerifyOCR"))
                    return SC.GetValue<int>($"Aligner.{Name}.TimeLimitVerifyOCR");
                return 5;
            }
        }

        public enum AlignerMsg
        {
            Reset,
            Clear,
            ResetToReady,
            Stop,
            StartInit,
            InitComplete,
            StartHome,
            HomeComplete,
            ERROR,
            ActionDone,
            Abort,
            StartMaintenance,
            CompleteMaintenance,

            ReadData,

            SetParameters,
            SetParametersComplete,




            Grip,
            GripComplete,

            UnGrip,
            UnGripComplete,

            LiftUp,
            LiftUpComplete,

            LiftDown,
            LiftDownComplete,

            Align,
            AlignComplete,

            PrepareAccept,
            PrepareAcceptComplete,
            SetAlign,
        }
        protected AlignerBaseDevice(string module, string name)
            : base()
        {
            Module = module;
            Name = name;
            RobotModuleName = (ModuleName)Enum.Parse(typeof(ModuleName), Name);
            InitializeRobot();
        }
        public string EventAlignerStartAlign { get => RobotModuleName.ToString() + "StartAligning"; }
        public string EventAlignerCompleteAlign { get => RobotModuleName.ToString() + "CompleteAligning"; }
        public string AlarmAlignFailed { get => RobotModuleName.ToString() + "AligningFailed"; }
        public string AlarmAlignTimeout { get => RobotModuleName.ToString() + "AligningTimeout"; }
        public string AlarmVerifyLaserMarkFailed { get => RobotModuleName.ToString() + "VerifyLaserMarkFailed"; }
        public string AlarmVerifyLaserMarkSlotNoFailed { get => RobotModuleName.ToString() + "VerifyLaserMarkSlotNoFailed"; }
        public string AlarmVerifyLaserMarkLengthFailed { get => RobotModuleName.ToString() + "VerifyLaserMarkLengthFailed"; }
        public string AlarmVerifyLaserMarkWithHostFailed { get => RobotModuleName.ToString() + "VerifyLaserMarkWithHostFailed"; }
        public string AlarmVerifyLaserMarkChecksumFailed { get => RobotModuleName.ToString() + "VerifyLaserMarkChecksumFailed"; }

        public string AlarmSlotInLaserMarkIsOccuppied { get => RobotModuleName.ToString() + "SlotInLaserMarkIsOccuppied"; }

        public string AlarmVerifyLaserMarkTimeout { get => RobotModuleName.ToString() + "VerifyLaserMarkTimeout"; }
        public string AlarmWaferInformationError { get => RobotModuleName.ToString() + "WaferInformationError"; }

        public virtual bool IsNeedChangeWaferSize(WaferSize wz)
        {
            return false;
        }




        protected override bool Init()
        {
            return base.Init();
        }

        public void InitializeRobot()
        {
            BuildTransitionTable();
            SubscribeDataVariable();

            SubscribeOperation();
            SubscribeDeviceOperation();

            
            AlignerCommandTimeout = SC.ContainsItem("Aligner.TimeLimitAlignerCommand") ?
                SC.GetValue<int>("Aligner.TimeLimitAlignerCommand") : 120;
            WaferManager.Instance.SubscribeLocation(Name, 1);

            Running = true;
        }

        protected virtual void SubscribeOperation()
        {
            OP.Subscribe(String.Format("{0}.{1}", Name, "LiftUp"), (out string reason, int time, object[] param) =>
            {
                bool ret = LiftUp(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "LiftUp");
                    return true;
                }
                reason = "";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "LiftDown"), (out string reason, int time, object[] param) =>
            {
                bool ret = LiftDown(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "LiftDown");
                    return true;
                }
                reason = "";
                return false;
            });




            OP.Subscribe(String.Format("{0}.{1}", Name, "SetWaferSize"), (out string reason, int time, object[] param) =>
            {
                if(param.Length <2)
                {
                    reason = "Invalide parameter";
                    return false;
                }
                WaferSize size = (WaferSize)Enum.Parse(typeof(WaferSize), param[1].ToString());

                bool ret = SetWaferSize( size, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "SetWaferSize");
                    return true;
                }
                reason = "";
                return false;
            });



            OP.Subscribe(String.Format("{0}.{1}", Name, "Init"), (out string reason, int time, object[] param) =>
            {
                bool ret = Init(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Initializing");
                    return true;
                }
                reason = "";
                return false;
            });

            OP.Subscribe(String.Format("{0}.{1}", Name, "Abort"), (out string reason, int time, object[] param) =>
            {
                bool ret = Abort(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Abort");
                    return true;
                }
                reason = "";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "Home"), (out string reason, int time, object[] param) =>
            {
                bool ret = Home(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Homing");
                    return true;
                }
                reason = "";
                return false;
            });

            OP.Subscribe(String.Format("{0}.{1}", Name, "AlignerHome"), (out string reason, int time, object[] param) =>
            {
                bool ret = Home(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Homing");
                    return true;
                }
                reason = "";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "HomeModule"), (out string reason, int time, object[] param) =>
            {
                bool ret = HomeModule(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "ModuleHoming");
                    return true;
                }
                reason = "";
                return false;
            });

            OP.Subscribe(String.Format("{0}.{1}", Name, "AlignerHomeModule"), (out string reason, int time, object[] param) =>
            {
                bool ret = HomeModule(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "ModuleHoming");
                    return true;
                }
                reason = "";
                return false;
            });

            OP.Subscribe(String.Format("{0}.{1}", Name, "Reset"), (out string reason, int time, object[] param) =>
            {
                bool ret = AlignerReset( out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Resetting");
                    return true;
                }

                reason = "";
                return true;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "AlignerReset"), (out string reason, int time, object[] param) =>
            {
                bool ret = AlignerReset( out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Resetting");
                    return true;
                }

                reason = "";
                return true;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "AlignerAlign"), (out string reason, int time, object[] param) =>
            {
                bool ret = AlignerAlign(param, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Resetting");
                    return true;
                }

                reason = "";
                return true;
            });

            OP.Subscribe(String.Format("{0}.{1}", Name, "AlignerSetWaferSize"), (out string reason, int time, object[] param) =>
            {
                bool ret = AlignerSetWaferSize(param, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Resetting");
                    return true;
                }

                reason = "";
                return true;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "PrepareAccept"), (out string reason, int time, object[] param) =>
            {
                bool ret = PrepareAcceptWafer();
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "PrepareAccept");
                    return true;
                }

                reason = "";
                return true;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "AlignerHold"), (out string reason, int time, object[] param) =>
            {
                bool ret = Grip(0, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Hold Wafer");
                    return true;
                }

                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "AlignerRelease"), (out string reason, int time, object[] param) =>
            {
                bool ret = Release(0, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Release Wafer");
                    return true;
                }

                return false;
            });



        }

        
        protected virtual void SubscribeDeviceOperation()
        {

            DEVICE.Register(String.Format("{0}.{1}", Name, "AlignerInit"), (out string reason, int time, object[] param) =>
            {
                bool ret = Init(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Init");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "AlignerHome"), (out string reason, int time, object[] param) =>
            {
                bool ret = Home(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Home");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "AlignerHomeModule"), (out string reason, int time, object[] param) =>
            {
                bool ret = HomeModule(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "ModuleHome");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "AlignerReset"), (out string reason, int time, object[] param) =>
            {
                bool ret = AlignerReset(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Reset Error");
                    return true;
                }

                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "AlignerGrip"), (out string reason, int time, object[] param) =>
            {
                bool ret = Grip(0, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Hold Wafer");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "AlignerRelease"), (out string reason, int time, object[] param) =>
            {
                bool ret = Release(0, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Release Wafer");
                    return true;
                }

                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "AlignerLiftUp"), (out string reason, int time, object[] param) =>
            {
                bool ret = LiftUp(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Lifter Up");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "AlignerLiftDown"), (out string reason, int time, object[] param) =>
            {
                bool ret = LiftDown(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Lifter Down");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "AlignerStop"), (out string reason, int time, object[] param) =>
            {
                bool ret = Stop(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Stop Align");
                    return true;
                }
                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "AlignerAlign"), (out string reason, int time, object[] param) =>
            {
                double angle = double.Parse((string)param[0]);
                bool ret = Align(angle, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "PreAlign");
                    return true;
                }
                return false;
            });

            

        }
        public bool SetWaferSize(WaferSize size, out string reason)
        {
            reason = "";
            if(CheckToPostMessage((int)AlignerMsg.SetParameters, new object[] { "WaferSize", size }))
            {
                IsBusy = true;
                return true;
            }
            return false;
                
        }

        public bool AlignerReset(out string reason)
        {
            IsBusy = false;
            reason = "";
            if (CheckToPostMessage((int)AlignerMsg.Reset, null))
                return true;
            reason = $"{Name} is in {AlignerState}";
            return false;
        }
        public bool Abort(out string reason)
        {
            reason = "";
            if (CheckToPostMessage((int)AlignerMsg.Abort, null))
                return true;
            reason = $"{Name} is in {AlignerState}";
            return false;
        }

        public bool Stop(out string reason)
        {
            reason = "";
            if (CheckToPostMessage((int)AlignerMsg.Stop, null))
                return true;
            reason = $"{Name} is in {AlignerState}";
            return false;
        }
        public bool AlignerAlign(object[] param, out string reason)
        {
            reason = "";
            if (CheckToPostMessage((int)AlignerMsg.Align, param))
            {
                IsBusy = true;
                return true;
            }
            reason = $"{Name} is in {AlignerState}";
            return false;
        }

        public bool AlignerSetWaferSize(object[] param, out string reason)
        {
            reason = "";
            if (CheckToPostMessage((int)AlignerMsg.SetParameters, param))
            {
                IsBusy = true;
                return true;
            }
            reason = $"{Name} is in {AlignerState}";
            return false;
        }

        public bool AlignerSetWaferSize(WaferSize wz,out string reason)
        {
            reason = "";
            if (CheckToPostMessage((int)AlignerMsg.SetParameters, new object[] { "WaferSize",wz }))
            {
                IsBusy = true;
                return true;
            }
            reason = $"{Name} is in {AlignerState}";
            return false;
        }

        public bool Align(double angle, out string reason)
        {
            IsPreAlgin = true;
            reason = "";
            double anglevalue = angle + CompensationAngleValue;

            while(anglevalue >=360)
            {
                anglevalue -= 360;
            }
            while (anglevalue < 0)
            {
                anglevalue += 360;
            }

            if (CheckToPostMessage((int)AlignerMsg.Align, new object[] { anglevalue }))
            {
                IsBusy = true;
                return true;
            }
            reason = $"{Name} is in {AlignerState}";
            return false;
        }

        public bool SetAlign(double angle, out string reason)
        {
            reason = "";
            double anglevalue = angle + CompensationAngleValue;


            if (CheckToPostMessage((int)AlignerMsg.SetAlign, new object[] { anglevalue }))
            {
                IsBusy = true;
                return true;
            }
            reason = $"{Name} is in {AlignerState}";
            return false;
        }

        public bool PreAlign(double angle,out string reason)
        {
            IsPreAlgin = true;
            reason = "";
            double anglevalue = angle + CompensationAngleValue;

            while (anglevalue > 360)
            {
                anglevalue -= 360;
            }
            while (anglevalue < 0)
            {
                anglevalue += 360;
            }

            if (CheckToPostMessage((int)AlignerMsg.Align, new object[] { anglevalue }))
            {
                IsBusy = true;
                return true;
            }
            reason = $"{Name} is in {AlignerState}";
            return false;
        }

        public bool PostAlign(double angle, out string reason)
        {
            IsPreAlgin = false;
            reason = "";
            double anglevalue = angle + CompensationAngleValue;

            while (anglevalue > 360)
            {
                anglevalue -= 360;
            }
            while (anglevalue < 0)
            {
                anglevalue += 360;
            }

            if (CheckToPostMessage((int)AlignerMsg.Align, new object[] { anglevalue }))
            {
                IsBusy = true;
                return true;
            }
            reason = $"{Name} is in {AlignerState}";
            return false;
        }



        public bool LiftDown(out string reason)
        {
            reason = "";
            if (CheckToPostMessage((int)AlignerMsg.LiftDown, null))
            {
                IsBusy = true;
                return true;
            }
            reason = $"{Name} is in {AlignerState}";
            return false;
        }

        public bool LiftUp(out string reason)
        {
            reason = "";
            if (CheckToPostMessage((int)AlignerMsg.LiftUp, null))
            {
                IsBusy = true;
                return true;
            }
            reason = $"{Name} is in {AlignerState}";
            return false;
        }

        public bool Release(int v, out string reason)
        {
            reason = "";
            if (CheckToPostMessage((int)AlignerMsg.UnGrip, new object[] { v}))
            {
                IsBusy = true;
                return true;
            }
            reason = $"{Name} is in {AlignerState}";
            return false;
        }

        public bool Grip(int v, out string reason)
        {
            reason = "";
            if (CheckToPostMessage((int)AlignerMsg.Grip, new object[] { v }))
            {
                IsBusy = true;
                return true;
            }
            reason = $"{Name} is in {AlignerState}";
            return false;
        }

        public bool Clear(out string reason)
        {
            reason = "";
            if (CheckToPostMessage((int)AlignerMsg.Clear, null))
            {
                
                return true;
            }
            reason = $"{Name} is in {AlignerState}";
            return false;
        }

        public bool Init(out string reason)
        {
            reason = "";
            if (CheckToPostMessage((int)AlignerMsg.StartInit, null))
            {
                IsBusy = true;
                return true;
            }
            reason = $"{Name} is in {AlignerState}";
            return false;
        }
        public bool Home(out string reason)
        {
            reason = "";
            if (CheckToPostMessage((int)AlignerMsg.StartInit, null))
            {
                IsBusy = true;
                return true;
            }
            reason = $"{Name} is in {AlignerState}";
            return false;
        }
        public bool HomeModule(out string reason)
        {
            reason = "";
            if (CheckToPostMessage((int)AlignerMsg.StartHome, null))
            {
                IsBusy = true;
                return true;
            }
            reason = $"{Name} is in {AlignerState}";
            return false;
        }        
        public bool SetMaintenanceMode(bool ismaintain)
        {
            if (ismaintain)
                return CheckToPostMessage((int)AlignerMsg.StartMaintenance, null);
            else
                return CheckToPostMessage((int)AlignerMsg.CompleteMaintenance, null);
        }

        private void SubscribeDataVariable()
        {
            DATA.Subscribe($"{Name}.State", () => AlignerState.ToString());
            DATA.Subscribe($"{Name}.AlignerState", () => AlignerState.ToString());
            DATA.Subscribe($"{Name}.Busy", () => AlignerState != AlignerStateEnum.Idle && AlignerState != AlignerStateEnum.Init);
            DATA.Subscribe($"{Name}.ErrorCode", () => ErrorCode);
            DATA.Subscribe($"{Name}.Error", () => AlignerState == AlignerStateEnum.Error);
            DATA.Subscribe($"{Name}.Notch", () => CurrentNotch);
            DATA.Subscribe($"{Name}.WaferOnAligner", () => IsWaferPresent(0));
            DATA.Subscribe($"{Name}.WaferSize", () => GetCurrentWaferSize().ToString());

            DATA.Subscribe($"{Name}.WaferAlignmentType", () => CurrentWaferAlignmentType.ToString());
            //            string str = string.Empty;
            //       



            DATA.Subscribe($"{Module}.{Name}.State", () => AlignerState.ToString());

            DATA.Subscribe($"{Module}.{Name}.Axis1Postion", () => PositionAxis1.ToString());
            DATA.Subscribe($"{Module}.{Name}.Axis2Postion", () => PositionAxis2.ToString());
            DATA.Subscribe($"{Module}.{Name}.Axis3Postion", () => PositionAxis3.ToString());
            DATA.Subscribe($"{Module}.{Name}.Axis4Postion", () => PositionAxis4.ToString());
            DATA.Subscribe($"{Module}.{Name}.Axis5Postion", () => PositionAxis5.ToString());
            DATA.Subscribe($"{Module}.{Name}.AlignerErrorCode", () => ErrorCode ?? ErrorCode.ToString());

            //EV.Subscribe(new EventItem("Event", EventAlignerStartAlign, $"{RobotModuleName} start aligning wafer.", EventLevel.Information, Aitex.Core.RT.Event.EventType.EventUI_Notify));
            //EV.Subscribe(new EventItem("Event", EventAlignerCompleteAlign, $"{RobotModuleName} complete aligning wafer.", EventLevel.Information, Aitex.Core.RT.Event.EventType.EventUI_Notify));

            //EV.Subscribe(new EventItem("Alarm", AlarmAlignTimeout, "Aligner align wafer time out.", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.EventUI_Notify));
            //EV.Subscribe(new EventItem("Alarm", AlarmAlignFailed, "Aligner aligner wafer failed.", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.EventUI_Notify));
            //EV.Subscribe(new EventItem("Alarm", AlarmVerifyLaserMarkFailed, "Aligner verify laser mark failed.", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.EventUI_Notify));
            //EV.Subscribe(new EventItem("Alarm", AlarmVerifyLaserMarkSlotNoFailed, "Aligner verify laser mark slot no failed.", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.EventUI_Notify));
            //EV.Subscribe(new EventItem("Alarm", AlarmVerifyLaserMarkLengthFailed, "Aligner verify laser mark length failed.", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.EventUI_Notify));
            //EV.Subscribe(new EventItem("Alarm", AlarmVerifyLaserMarkWithHostFailed, "Aligner verify laser mark with host failed.", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.EventUI_Notify));
            //EV.Subscribe(new EventItem("Alarm", AlarmVerifyLaserMarkChecksumFailed, "Aligner verify laser mark check character failed.", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.EventUI_Notify));

            //EV.Subscribe(new EventItem("Alarm", AlarmVerifyLaserMarkTimeout, "Aligner wait ocr manual verification timeout.", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.EventUI_Notify));
            //EV.Subscribe(new EventItem("Alarm", AlarmSlotInLaserMarkIsOccuppied, "The slot in Laser Mark is occuppied.", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.EventUI_Notify));
            //EV.Subscribe(new EventItem("Alarm", AlarmWaferInformationError, "The wafer sensor status doesn't match wafer information.", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.EventUI_Notify));


        }

        private void BuildTransitionTable()
        {
            fsm = new StateMachine<AlignerBaseDevice>(Name + ".StateMachine", (int)AlignerStateEnum.Init, 50);

            //Init sequence
            AnyStateTransition(AlignerMsg.ERROR, fError, AlignerStateEnum.Error);
            AnyStateTransition(AlignerMsg.Reset, fReset, AlignerStateEnum.Resetting);
            AnyStateTransition(AlignerMsg.Stop, fStop, AlignerStateEnum.Stopped);
            AnyStateTransition(AlignerMsg.Abort, FsmAbort, AlignerStateEnum.Init);
            AnyStateTransition(AlignerMsg.StartMaintenance, fStartMaintenance, AlignerStateEnum.Maintenance);

            Transition(AlignerStateEnum.Maintenance, FSM_MSG.TIMER, fMonitorMaintenance, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.Maintenance, AlignerMsg.CompleteMaintenance, fCompleteMaintenance, AlignerStateEnum.Idle);



            Transition(AlignerStateEnum.Resetting, AlignerMsg.ActionDone, fResetComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.Resetting, FSM_MSG.TIMER, fMonitorReset, AlignerStateEnum.Idle);


            Transition(AlignerStateEnum.Init, AlignerMsg.StartInit, fStartInit, AlignerStateEnum.Initializing);
            Transition(AlignerStateEnum.Idle, AlignerMsg.StartInit, fStartInit, AlignerStateEnum.Initializing);
            Transition(AlignerStateEnum.Error, AlignerMsg.StartInit, fStartInit, AlignerStateEnum.Initializing);

            Transition(AlignerStateEnum.Init, AlignerMsg.InitComplete, fInitComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.Error, AlignerMsg.InitComplete, fInitComplete, AlignerStateEnum.Idle);

            Transition(AlignerStateEnum.Initializing, AlignerMsg.InitComplete, fInitComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.Initializing, AlignerMsg.ActionDone, fInitComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.Initializing, FSM_MSG.TIMER, fMonitorInit, AlignerStateEnum.Idle);

            Transition(AlignerStateEnum.Init, AlignerMsg.StartHome, fStartHome, AlignerStateEnum.Homing);
            Transition(AlignerStateEnum.Idle, AlignerMsg.StartHome, fStartHome, AlignerStateEnum.Homing);
            Transition(AlignerStateEnum.Error, AlignerMsg.StartHome, fStartHome, AlignerStateEnum.Homing);

            Transition(AlignerStateEnum.Init, AlignerMsg.HomeComplete, fHomeComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.Error, AlignerMsg.HomeComplete, fHomeComplete, AlignerStateEnum.Idle);

            Transition(AlignerStateEnum.Homing, AlignerMsg.HomeComplete, fHomeComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.Homing, AlignerMsg.ActionDone, fHomeComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.Homing, FSM_MSG.TIMER, fMonitorHome, AlignerStateEnum.Idle);

            Transition(AlignerStateEnum.Error, AlignerMsg.Clear, fClear, AlignerStateEnum.Init);
            Transition(AlignerStateEnum.Error, AlignerMsg.ResetToReady, fResetToReady, AlignerStateEnum.Idle);

            Transition(AlignerStateEnum.Idle, AlignerMsg.SetParameters, fStartSetParameters, AlignerStateEnum.SettingServos);
            Transition(AlignerStateEnum.SettingServos, AlignerMsg.SetParametersComplete, fSetComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.SettingServos, AlignerMsg.ActionDone, fSetComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.SettingServos, FSM_MSG.TIMER, fMonitorSetParamter, AlignerStateEnum.Idle);


            Transition(AlignerStateEnum.Idle, AlignerMsg.ReadData, fStartReadData, AlignerStateEnum.ReadingData);
            Transition(AlignerStateEnum.ReadingData, AlignerMsg.ActionDone, null, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.ReadingData, FSM_MSG.TIMER, fMonitorReadData, AlignerStateEnum.Idle);


            Transition(AlignerStateEnum.Idle, AlignerMsg.Grip, fStartGrip, AlignerStateEnum.Gripping);
            Transition(AlignerStateEnum.Gripping, AlignerMsg.GripComplete, fGripComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.Gripping, AlignerMsg.ActionDone, fGripComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.Gripping, FSM_MSG.TIMER, fMonitorGrip, AlignerStateEnum.Idle);


            Transition(AlignerStateEnum.Idle, AlignerMsg.UnGrip, fStartUnGrip, AlignerStateEnum.UnGripping);
            Transition(AlignerStateEnum.UnGripping, AlignerMsg.UnGripComplete, fUnGripComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.UnGripping, AlignerMsg.ActionDone, fUnGripComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.UnGripping, FSM_MSG.TIMER, fMonitorUnGrip, AlignerStateEnum.Idle);

            Transition(AlignerStateEnum.Idle, AlignerMsg.LiftUp, fStartLiftup, AlignerStateEnum.LiftingUp);
            Transition(AlignerStateEnum.LiftingUp, AlignerMsg.LiftUpComplete, fLiftupComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.LiftingUp, AlignerMsg.ActionDone, fLiftupComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.LiftingUp, FSM_MSG.TIMER, fMonitorLiftup, AlignerStateEnum.Idle);

            Transition(AlignerStateEnum.Idle, AlignerMsg.LiftDown, fStartLiftdown, AlignerStateEnum.LiftingDown);
            Transition(AlignerStateEnum.LiftingDown, AlignerMsg.LiftDownComplete, fLiftdownComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.LiftingDown, AlignerMsg.ActionDone, fLiftdownComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.LiftingDown, FSM_MSG.TIMER, fMonitorLiftdown, AlignerStateEnum.Idle);


            Transition(AlignerStateEnum.Idle, AlignerMsg.Align, fStartAlign, AlignerStateEnum.Aligning);
            Transition(AlignerStateEnum.Aligning, AlignerMsg.AlignComplete, fAlignComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.Aligning, AlignerMsg.ActionDone, fAlignComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.Aligning, FSM_MSG.TIMER, fMonitorAligning, AlignerStateEnum.Idle);

            Transition(AlignerStateEnum.Idle, AlignerMsg.SetAlign, fStartSetAlign, AlignerStateEnum.SetAligning);
            Transition(AlignerStateEnum.SetAligning, AlignerMsg.AlignComplete, fAlignComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.SetAligning, AlignerMsg.ActionDone, fAlignComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.SetAligning, FSM_MSG.TIMER, fMonitorAligning, AlignerStateEnum.Idle);

            Transition(AlignerStateEnum.Idle, AlignerMsg.PrepareAccept, fStartPrepareAccept, AlignerStateEnum.PrepareAccept);
            Transition(AlignerStateEnum.PrepareAccept, AlignerMsg.PrepareAcceptComplete, fPrepareAcceptComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.PrepareAccept, AlignerMsg.ActionDone, fPrepareAcceptComplete, AlignerStateEnum.Idle);
            Transition(AlignerStateEnum.PrepareAccept, FSM_MSG.TIMER, fMonitorPrepareAccept, AlignerStateEnum.Idle);

        }

        protected virtual bool fMonitorPrepareAccept(object[] param)
        {
            return false;
        }

        protected virtual bool fPrepareAcceptComplete(object[] param)
        {
            return true;
        }

        protected virtual bool fStartPrepareAccept(object[] param)
        {
            return true ;
        }

        protected virtual bool fMonitorHome(object[] param)
        {
            return false;
        }

        protected virtual bool fHomeComplete(object[] param)
        {
            return true; ;
        }

        protected virtual bool fStartHome(object[] param)
        {
            return true;
        }

        public virtual bool IsWaferPresent(int slotindex)
        {
            return WaferManager.Instance.CheckHasWafer(RobotModuleName, slotindex);
        }

        public virtual RobotArmWaferStateEnum GetWaferState(int slotindex=0)
        {
            if (WaferManager.Instance.CheckHasWafer(RobotModuleName, slotindex) != IsWaferPresent(slotindex))
            {
                return RobotArmWaferStateEnum.Error;
            }
            return IsWaferPresent(slotindex)? RobotArmWaferStateEnum.Present: RobotArmWaferStateEnum.Absent;
        }
        protected virtual bool fStartMaintenance(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorMaintenance(object[] param)
        {
            return false;
        }

        protected virtual bool fCompleteMaintenance(object[] param)
        {
            return true;
        }

        protected abstract bool fStartLiftup(object[] param);

        protected virtual bool fLiftupComplete(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorLiftup(object[] param)
        {
            return false;
        }

        protected abstract bool fStartLiftdown(object[] param);

        protected virtual bool fLiftdownComplete(object[] param)
        {
            return true ;
        }

        protected virtual bool fMonitorLiftdown(object[] param)
        {
            return false;
        }

        protected abstract bool fStartAlign(object[] param);

        protected virtual bool fStartSetAlign(object[] param)
        {
            return true;
        }

        private bool fAlignComplete(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorAligning(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorUnGrip(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorGrip(object[] param)
        {
            return false;
        }

        protected virtual bool fMoveComplete(object[] param)
        {
            return true;
        }

        protected virtual bool fStartMove(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorSetParamter(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorReadData(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorReset(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorGoTo(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorMap(object[] param)
        {
            return false;
        }
        protected virtual bool fMonitorPlace(object[] param)
        {
            return false;
        }
        protected virtual bool fMonitorSwap(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorPick(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorInit(object[] param)
        {
            return false;
        }

        protected abstract bool fStop(object[] param);
        

        protected abstract bool FsmAbort(object[] param);
       

        protected bool fResetComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }

        protected abstract bool fClear(object[] param);

        protected virtual bool fSetComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }
        protected abstract bool fStartReadData(object[] param);
        protected abstract bool fStartSetParameters(object[] param);
        
        protected abstract bool fStartUnGrip(object[] param);
        protected abstract bool fStartGrip(object[] param);        
        protected abstract bool fResetToReady(object[] param);
        protected abstract bool fReset(object[] param);
        protected abstract bool fStartInit(object[] param);
        protected abstract bool fError(object[] param);


        protected virtual bool fUnGripComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }

        protected virtual bool fGripComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }


        protected virtual bool fInitComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }


        public AlignerStateEnum AlignerState { get => (AlignerStateEnum)fsm.State; }
        public string Module { get; set; }
        public string Name { get; set; }

        public ModuleName RobotModuleName { get; private set; }
        public ModuleName CurrentInteractModule { get; private set; }


        public virtual bool IsNeedRelease
        {
            get; set;

        } = false;

        public virtual bool IsNeedPrepareBeforePlaceWafer()
        {
            return false;
        }


        public virtual void Monitor()
        {
            return;
        }

        public virtual void Reset()
        {

        }
        public virtual void OnError(string errortext)
        {
            EV.PostAlarmLog(Module, $"{Name} occurred error: {errortext}");
            IsBusy = false;
            CheckToPostMessage((int)AlignerMsg.ERROR, new object[] { errortext });
        }
        public virtual bool SetParameters(object[] param)
        {
            return CheckToPostMessage((int)AlignerMsg.SetParameters, param);
        }
        public virtual bool PrepareAcceptWafer()
        {
            if(CheckToPostMessage((int)AlignerMsg.PrepareAccept,null))
            {
                IsBusy = true;
                return true;
            }
            return false;
        }

        public virtual bool HomeAllAxes(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool QueryWaferMap(ModuleName module, out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool OnActionDone(object[] param)
        {
            IsBusy = false;
            return CheckToPostMessage((int)AlignerMsg.ActionDone, new object[] { "ActionDone" });
        }

        public bool CheckToPostMessage(int msg, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, msg))
            {
                if (msg != 7)
                    EV.PostWarningLog(Name, $"{Name} is in { (AlignerStateEnum)fsm.State} state，can not do {(AlignerMsg)msg}");
                return false;
            }
           
            
            CurrentParamter = args;
            fsm.PostMsg(msg, args);

            return true;
        }
        public bool CheckToPostMessage(AlignerMsg msg, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, (int)msg))
            {
                if ((int)msg != 7)
                    EV.PostWarningLog(Name, $"{Name} is in { (AlignerStateEnum)fsm.State} state，can not do {(AlignerMsg)msg}");
                return false;
            }
            CurrentParamter = args;
            fsm.PostMsg((int)msg, args);

            return true;
        }

        public bool Check(int msg, out string reason, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, msg))
            {
                reason = String.Format("{0} is in {1} state，can not do {2}", Name, (AlignerStateEnum)fsm.State, (AlignerMsg)msg);
                return false;
            }
            CurrentParamter = args;
            reason = "";

            return true;
        }

    }


    public enum WaferAlignerTypeEnum
    {
        Notch,
        Line,
    }
}
