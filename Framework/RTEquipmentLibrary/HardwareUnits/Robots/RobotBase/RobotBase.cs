using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Fsm;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Common;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase
{
    public abstract class RobotBaseDevice : Entity, IDevice, IRobot, IEntity
    {
        public event Action<ModuleName, string> OnSlotMapRead;

        public event Action<bool> ActionDone;

        public event Action<string, AlarmEventItem> OnDeviceAlarmStateChanged;
        public bool HasAlarm { get; }
        private bool _isBusy;
        public bool IsBusy 
        {
            get { return _isBusy; }
            set
            {
                if(_isBusy !=value)
                    LOG.Write($"Set {RobotModuleName} IsBusy flag to {value}");
                _isBusy = value;
                
            }
        }

        public string ErrorCode { get; set; } = "";
        public virtual bool IsReady()
        {
            return RobotState == RobotStateEnum.Idle && !IsBusy && fsm.CheckExecuted();
        }
        public object[] CurrentParamter { get; private set; }

        public bool IsOnline { get; set; }
        public ModuleName BladeTarget { get; set; }
        public ModuleName Blade1Target { get; set; }
        public ModuleName Blade2Target { get; set; }

        //public BladePostureEnum Blade1Posture { get; set; } = BladePostureEnum.Degree0;

        public BladePostureEnum Blade1ActionPosture { get; set; }

        public virtual bool IsWaferPresenceOnBlade1 { get; set; }
        public virtual bool IsWaferPresenceOnBlade2 { get; set; }    
        
        public virtual bool IsEnableMapWafer
        {
            get
            {
                return true;
            }
        }
        public bool IsIdle
        {
            get
            {
                return fsm.State == (int)RobotStateEnum.Idle && fsm.CheckExecuted() && !IsBusy;
            }
        }

        public bool IsError
        {
            get
            {
                return fsm.State == (int)RobotStateEnum.Error || fsm.State == (int)RobotStateEnum.Stopped;
            }
        }

        public bool IsInit
        {
            get
            {
                return fsm.State == (int)RobotStateEnum.Init;
            }
        }

        public virtual bool IsOnReadyPosition
        {
            get; set;
        } = true;

        public virtual bool BladeAEnable
        {
            get
            {
                if (BladeCount == 1) return true;

                if (SC.ContainsItem($"{ScRoot}.{Name}.BladeAEnable"))
                {
                    return SC.GetValue<bool>($"{ScRoot}.{Name}.BladeAEnable");
                }

                return SC.GetValue<bool>($"Robot.BladeAEnable");
            }
            set
            {
                if (SC.ContainsItem($"{ScRoot}.{Name}.BladeAEnable"))
                {
                    SC.SetItemValue($"{ScRoot}.{Name}.BladeAEnable", value);
                }
                else if (SC.ContainsItem($"Robot.BladeAEnable"))
                {
                    SC.SetItemValue($"Robot.BladeAEnable", value);
                }
            }
        }
        public virtual bool BladeBEnable
        {
            get
            {
                if (BladeCount == 1) return false;

                if (SC.ContainsItem($"{ScRoot}.{Name}.BladeBEnable"))
                {
                    return SC.GetValue<bool>($"{ScRoot}.{Name}.BladeBEnable");
                }

                return SC.GetValue<bool>($"Robot.BladeBEnable");
            }
            set
            {
                if (SC.ContainsItem($"{ScRoot}.{Name}.BladeBEnable"))
                {
                    SC.SetItemValue($"{ScRoot}.{Name}.BladeBEnable", value);
                }
                else if (SC.ContainsItem($"Robot.BladeBEnable"))
                {
                    SC.SetItemValue($"Robot.BladeBEnable", value);
                }
            }
        }
        public int Blade2Slots
        {
            get
            {
                if (SC.ContainsItem($"{ScRoot}.{Name}.Blade2Slots"))
                {
                    return Math.Max(SC.GetValue<int>($"{ScRoot}.{Name}.Blade2Slots"), 1);
                }

                if (SC.ContainsItem($"Robot.Blade2Slots"))
                {
                    return Math.Max(SC.GetValue<int>($"Robot.Blade2Slots"), 1);
                }
                return 1;
            }
        }
        public WaferSize Size
        {
            get; set;
        } = WaferSize.WS12;

        public virtual WaferSize GetCurrentWaferSize()
        {
            return Size;
        }

        public virtual LinkedList<string> CommandMessages
        {
            get; set;
        } = new LinkedList<string>();

        private List<string> _commandMsgs
        {
            get
            {
                List<string> ret = new List<string>();
                foreach(var msg in CommandMessages)
                {
                    ret.Add(msg);
                }
                return ret;
            }
        }

        public RobotMoveInfo MoveInfo { get; set; }

        public RobotArmEnum CmdRobotArm { get; set; }
        public ModuleName CmdTarget { get; set; }
        public int CmdTargetSlot { get; set; }

        public int SpeedAxis1 { set; get; }
        public int SpeedAxis2 { set; get; }
        public int SpeedAxis3 { set; get; }
        public int SpeedAxis4 { set; get; }
        public int SpeedAxis5 { set; get; }
        public int SpeedAxis6 { set; get; }
        public int Speed { set; get; }

        public float PositionAxis1 { get; set; }
        public float PositionAxis2 { get; set; }
        public float PositionAxis3 { get; set; }
        public float PositionAxis4 { get; set; }
        public float PositionAxis5 { get; set; }
        public float PositionAxis6 { get; set; }

        public string[] ReadMappingUpData { get; protected set; }
        public string[] ReadMappingDownData { get; protected set; }
        public virtual int RobotCommandTimeout 
        {
            get
            {
                if (SC.ContainsItem($"Robot.{RobotModuleName}.TimeLimitRobotCommand"))
                    return SC.GetValue<int>($"Robot.{RobotModuleName}.TimeLimitRobotCommand");
                return 90;
            }
           
           
        }
        public virtual int BladeCount { get; set; }

        public enum RobotMsg
        {
            Reset,
            Clear,
            ResetToReady,
            Stop,
            StartInit,
            InitComplete,

            

            ERROR,
            ActionDone=7,
            Abort,
            StartHome,
            HomeComplete,
            StartMaintenance,
            CompleteMaintenance,

            ReadData,
            ReadDataComplete,


            SetParameters,
            SetParametersComplete,

            Move,
            MoveComplete,

            PickWafer,
            PickComplete,

            PlaceWafer,
            PlaceComplete,

            SwapWafer,
            SwapComplete,

            MapWafer,
            MapComplete,

            GoToPosition,
            ArrivedPosition,

            Grip,
            GripComplete,

            UnGrip,
            UnGripComplete,

            ExtendForPlace,
            ExtendForPick,
            ExtendComplete,

            RetractFromPlace,
            RetractFromPick,
            RetractComplete,

            TransferWafer,
            TransferComplete,

            PickCassette,
            PickCassetteComplete,

            PlaceCassette,
            PlaceCassetteComplete,

            ExecuteCommand,
        }
        protected RobotBaseDevice(string module, string name,string scRoot = "Robot",int bladeCount=2)
            : base()
        {
            Module = module;
            Name = name;
            ScRoot = scRoot;
            RobotModuleName = (ModuleName)Enum.Parse(typeof(ModuleName), Name);
            BladeCount = bladeCount;
            InitializeRobot();
        }

        protected override bool Init()
        {
            return base.Init();
        }

        public virtual void InitializeRobot()
        {
            BuildTransitionTable();
            SubscribeDataVariable();

            SubscribeOperation();
            SubscribeDeviceOperation();

            MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.Both,
                BladeTarget = "ArmA.System",
            };
            
            SubscribeWaferLocation();
            Running = true;
        }

        protected virtual void SubscribeWaferLocation()
        {
            WaferManager.Instance.SubscribeLocation(Name, BladeCount);
        }


        protected virtual void SubscribeOperation()
        {
            OP.Subscribe(String.Format("{0}.{1}", Name, "MapWafer"), (out string reason, int time, object[] param) =>
            {
                ModuleName chamber = (ModuleName)Enum.Parse(typeof(ModuleName), param[0].ToString(), true);
                
                bool ret = WaferMapping(chamber);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Map wafer succesfully");
                    return true;
                }
                reason = $"{Name} map wafer failed";
                return false;
            });

            OP.Subscribe(String.Format("{0}.{1}", Name, "ExecuteCommand"), (out string reason, int time, object[] param) =>
            {
                bool ret = ExecuteCommand(param);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Execute command succesfully");
                    return true;
                }
                reason = $"{Name} execute failed";
                return false;
            });

            OP.Subscribe(String.Format("{0}.{1}", Name, "RobotFlippe"), (out string reason, int time, object[] param) =>
            {
                bool isto0 = Convert.ToBoolean(param[0]);
                bool ret = RobotFlippe(isto0);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, $"Flippe to {(isto0?"0 degree":"180 Degree")} command succesfully");
                    return true;
                }
                reason = $"{Name} Flippe failed";
                return false;
            });



            OP.Subscribe(String.Format("{0}.{1}", Name, "SetWaferSize"), (out string reason, int time, object[] param) =>
            {
                RobotArmEnum arm = (RobotArmEnum)Enum.Parse(typeof(RobotArmEnum), param[0].ToString());
                WaferSize size = (WaferSize)Enum.Parse(typeof(WaferSize), param[1].ToString());
                if(WaferManager.Instance.CheckHasWafer(RobotModuleName,(int)arm))
                {
                    WaferManager.Instance.GetWafer(RobotModuleName, (int)arm).Size = size;
                }
                bool ret = SetWaferSize(arm, size);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Set Wafer size");
                    return true;
                }
                reason = $"{Name} not ready";
                return false;
            });


            OP.Subscribe(String.Format("{0}.{1}", Name, "PickCassette"), (out string reason, int time, object[] param) =>
            {

                bool ret = PickCassette( RobotArmEnum.Blade1,param[0].ToString(),0);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Pick cassette");
                    return true;
                }
                reason = $"{Name} not ready";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "PlaceCassette"), (out string reason, int time, object[] param) =>
            {

                bool ret = PlaceCassette(RobotArmEnum.Blade1, param[0].ToString(), 0);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Pick cassette");
                    return true;
                }
                reason = $"{Name} not ready";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "Init"), (out string reason, int time, object[] param) =>
            {
                bool ret = Home(param);
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
                bool ret = Abort();
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
                bool ret = Home(param);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Homing");
                    return true;
                }
                reason = "";
                return false;
            });

            OP.Subscribe(String.Format("{0}.{1}", Name, "RobotHome"), (out string reason, int time, object[] param) =>
            {
                bool ret = Home(param);
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
                bool ret = HomeModule(param);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "ModuleHoming");
                    return true;
                }
                reason = "";
                return false;
            });

            OP.Subscribe(String.Format("{0}.{1}", Name, "RobotHomeModule"), (out string reason, int time, object[] param) =>
            {
                bool ret = HomeModule(param);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "ModuleHomingHoming");
                    return true;
                }
                reason = "";
                return false;
            });

            OP.Subscribe(String.Format("{0}.{1}", Name, "Reset"), (out string reason, int time, object[] param) =>
            {

                RobotReset();
                reason = "";

                return true;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "RobotReset"), (out string reason, int time, object[] param) =>
            {
                RobotReset();

                reason = "";
                return true;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "Grip"), (out string reason, int time, object[] param) =>
            {
                Hand hand = (Hand)Enum.Parse(typeof(Hand), param[0].ToString(), true);

                RobotArmEnum arm = (RobotArmEnum)((int)hand);
                bool ret = Grip(arm);
                

                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Grip");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "RobotGrip"), (out string reason, int time, object[] param) =>
            {
                //Hand hand = (RobotArmEnum)Enum.Parse(typeof(RobotArmEnum), (string)param[0], );
                string strarm = Regex.Replace(param[0].ToString().Replace("（", "(").Replace("）", ")"), @"\([^\(]*\)", "");
                RobotArmEnum arm = (RobotArmEnum)Enum.Parse(typeof(RobotArmEnum), strarm);
                bool ret = Grip(arm);


                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Grip");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "Release"), (out string reason, int time, object[] param) =>
            {
                Hand hand = (Hand)Enum.Parse(typeof(Hand), param[0].ToString(), true);

                RobotArmEnum arm = (RobotArmEnum)((int)hand);
                bool ret = Release(arm);
                
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Release");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "RobotRelease"), (out string reason, int time, object[] param) =>
            {
                //Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[0], true);

                string strarm = Regex.Replace(param[0].ToString().Replace("（", "(").Replace("）", ")"), @"\([^\(]*\)", "");
                RobotArmEnum arm = (RobotArmEnum)Enum.Parse(typeof(RobotArmEnum), strarm);
                bool ret = Release(arm);
                
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Release");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "Stop"), (out string reason, int time, object[] param) =>
            {
                bool ret = Stop();
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Stopping");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "RobotStop"), (out string reason, int time, object[] param) =>
            {
                bool ret = Stop();
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Stopping");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "Pick"), (out string reason, int time, object[] param) =>
            {
                //ModuleName chamber = (ModuleName)Enum.Parse(typeof(ModuleName), (string)param[0], true);

                string station = (string)param[0];
                int slot = int.Parse(param[1].ToString());
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[2], true);

                RobotArmEnum arm = (RobotArmEnum)((int)hand);

                bool ret = Pick(arm, station, slot);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Pick wafer");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });

            OP.Subscribe(String.Format("{0}.{1}", Name, "RobotPick"), (out string reason, int time, object[] param) =>
            {
                string station = (string)param[0];
                int slot = int.Parse((string)param[1]);
                string strarm = Regex.Replace(param[2].ToString().Replace("（", "(").Replace("）", ")"), @"\([^\(]*\)", "");
                RobotArmEnum arm = (RobotArmEnum)Enum.Parse(typeof(RobotArmEnum), strarm);

                bool ret = Pick(arm, station, slot);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Pick wafer");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "Place"), (out string reason, int time, object[] param) =>
            {
                string station = (string)param[0];
                int slot = int.Parse(param[1].ToString());
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[2], true);

                RobotArmEnum arm = (RobotArmEnum)((int)hand);


                bool ret = Place(arm, station, slot);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Place wafer");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });

            OP.Subscribe(String.Format("{0}.{1}", Name, "RobotPlace"), (out string reason, int time, object[] param) =>
            {
                string station = (string)param[0];
                int slot = int.Parse((string)param[1]);
                string strarm = Regex.Replace(param[2].ToString().Replace("（", "(").Replace("）", ")"), @"\([^\(]*\)", "");
                RobotArmEnum arm = (RobotArmEnum)Enum.Parse(typeof(RobotArmEnum), strarm);


                bool ret = Place(arm, station, slot);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Place wafer");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "Exchange"), (out string reason, int time, object[] param) =>
            {
                string station = (string)param[0];
                int slot = int.Parse((string)param[1]);
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[2], true);

                RobotArmEnum arm = (RobotArmEnum)((int)hand);
                bool ret = Swap(arm, station, slot);//, slot, hand, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Swap wafer");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "RobotExchange"), (out string reason, int time, object[] param) =>
            {
                string station = (string)param[0];
                int slot = int.Parse((string)param[1]);
                string strarm = Regex.Replace(param[2].ToString().Replace("（", "(").Replace("）", ")"), @"\([^\(]*\)", "");
                RobotArmEnum arm = (RobotArmEnum)Enum.Parse(typeof(RobotArmEnum), strarm);
                bool ret = Swap(arm, station, slot);//, slot, hand, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Swap wafer");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "Goto"), (out string reason, int time, object[] param) =>
            {
                bool ret = GoTo(param);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "Goto");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "RobotGoto"), (out string reason, int time, object[] param) =>
            {
                bool ret = GoTo(param);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "Goto");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });
            

            OP.Subscribe(String.Format("{0}.{1}", Name, "SetSpeed"), (out string reason, int time, object[] param) =>
            {
                bool ret = SetSpeed(param);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "SetSpeed");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });
            
            OP.Subscribe(String.Format("{0}.{1}", Name, "SetParameter"), (out string reason, int time, object[] param) =>
            {
                bool ret = SetParameter(param);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "SetParameter");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });

            OP.Subscribe(String.Format("{0}.{1}", Name, "ReadParameter"), (out string reason, int time, object[] param) =>
            {
                bool ret = ReadParameter(param);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "ReadData");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });
            OP.Subscribe(String.Format("{0}.{1}", Name, "RobotMove"), (out string reason, int time, object[] param) =>
            {
                bool ret = Move(param);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "MovePostion");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });



        }

        

        protected virtual void SubscribeDeviceOperation()
        {
            DEVICE.Register(String.Format("{0}.{1}", Name, "Init"), (out string reason, int time, object[] param) =>
            {
                bool ret = Home(param);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Initializing");
                    return true;
                }
                reason = "";
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "Home"), (out string reason, int time, object[] param) =>
            {
                bool ret = Home(param);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Homing");
                    return true;
                }
                reason = "";
                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotHome"), (out string reason, int time, object[] param) =>
            {
                bool ret = Home(param);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Homing");
                    return true;
                }
                reason = "";
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "HomeModule"), (out string reason, int time, object[] param) =>
            {
                bool ret = HomeModule(param);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Homing");
                    return true;
                }
                reason = "";
                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotHomeModule"), (out string reason, int time, object[] param) =>
            {
                bool ret = HomeModule(param);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Homing");
                    return true;
                }
                reason = "";
                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "Reset"), (out string reason, int time, object[] param) =>
            {
                RobotReset();

                reason = "";
                return true;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotReset"), (out string reason, int time, object[] param) =>
            {
                RobotReset();

                reason = "";
                return true;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "Grip"), (out string reason, int time, object[] param) =>
            {
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[0], true);

                RobotArmEnum arm = (RobotArmEnum)((int)hand);
                bool ret = Grip(arm);
                

                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Grip");
                    return true;
                }
                reason = "";
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotGrip"), (out string reason, int time, object[] param) =>
            {
                //Hand hand = (RobotArmEnum)Enum.Parse(typeof(RobotArmEnum), (string)param[0], );
                string strarm = Regex.Replace(param[0].ToString().Replace("（", "(").Replace("）", ")"), @"\([^\(]*\)", "");
                RobotArmEnum arm = (RobotArmEnum)Enum.Parse(typeof(RobotArmEnum), strarm);
                bool ret = Grip(arm);


                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Grip");
                    return true;
                }
                reason = "";
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "Release"), (out string reason, int time, object[] param) =>
            {
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[0], true);

                RobotArmEnum arm = (RobotArmEnum)((int)hand);
                bool ret = Release(arm);
                
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Release");
                    return true;
                }
                reason = "";
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotRelease"), (out string reason, int time, object[] param) =>
            {
                //Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[0], true);

                string strarm = Regex.Replace(param[0].ToString().Replace("（", "(").Replace("）", ")"), @"\([^\(]*\)", "");
                RobotArmEnum arm = (RobotArmEnum)Enum.Parse(typeof(RobotArmEnum), strarm);
                bool ret = Release(arm);
                //bool ret = QueryWaferMap(ModuleName.LP1, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Release");
                    return true;
                }
                reason = "";
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "Stop"), (out string reason, int time, object[] param) =>
            {
                bool ret = Stop();
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Stopping");
                    return true;
                }
                reason = "";
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotStop"), (out string reason, int time, object[] param) =>
            {
                bool ret = Stop();
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Stopping");
                    return true;
                }
                reason = "";
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "Pick"), (out string reason, int time, object[] param) =>
            {
                //ModuleName chamber = (ModuleName)Enum.Parse(typeof(ModuleName), (string)param[0], true);

                string station = (string)param[0];
                int slot = int.Parse((string)param[1]);
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[2], true);

                RobotArmEnum arm = (RobotArmEnum)((int)hand);

                bool ret = Pick(arm, station, slot);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Pick wafer");
                    return true;
                }
                reason = "";
                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotPick"), (out string reason, int time, object[] param) =>
            {
                string station = (string)param[0];
                int slot = int.Parse((string)param[1]);
                string strarm = Regex.Replace(param[2].ToString().Replace("（", "(").Replace("）", ")"), @"\([^\(]*\)", "");
                RobotArmEnum arm = (RobotArmEnum)Enum.Parse(typeof(RobotArmEnum), strarm);

                bool ret = Pick(arm, station, slot);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Pick wafer");
                    return true;
                }
                reason = "";
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "Place"), (out string reason, int time, object[] param) =>
            {
                string station = (string)param[0];
                int slot = int.Parse((string)param[1]);
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[2], true);

                RobotArmEnum arm = (RobotArmEnum)((int)hand);


                bool ret = Place(arm, station, slot);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Place wafer");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotPlace"), (out string reason, int time, object[] param) =>
            {
                string station = (string)param[0];
                int slot = int.Parse((string)param[1]);
                string strarm = Regex.Replace(param[2].ToString().Replace("（", "(").Replace("）", ")"), @"\([^\(]*\)", "");
                RobotArmEnum arm = (RobotArmEnum)Enum.Parse(typeof(RobotArmEnum), strarm);


                bool ret = Place(arm, station, slot);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Place wafer");
                    return true;
                }
                reason = $"{Name} busy";
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "Exchange"), (out string reason, int time, object[] param) =>
            {
                string station = (string)param[0];
                int slot = int.Parse((string)param[1]);
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[2], true);

                RobotArmEnum arm = (RobotArmEnum)((int)hand);
                bool ret = Swap(arm, station, slot);//, slot, hand, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Swap wafer");
                    return true;
                }
                reason = "";
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotExchange"), (out string reason, int time, object[] param) =>
            {
                string station = (string)param[0];
                int slot = int.Parse((string)param[1]);
                string strarm = Regex.Replace(param[2].ToString().Replace("（", "(").Replace("）", ")"), @"\([^\(]*\)", "");
                RobotArmEnum arm = (RobotArmEnum)Enum.Parse(typeof(RobotArmEnum), strarm);
                bool ret = Swap(arm, station, slot);//, slot, hand, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Swap wafer");
                    return true;
                }
                reason = "";
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "Goto"), (out string reason, int time, object[] param) =>
            {
                bool ret = GoTo(param);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "Goto");
                    return true;
                }
                reason = "";
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotGoto"), (out string reason, int time, object[] param) =>
            {
                bool ret = GoTo(param);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "Goto");
                    return true;
                }
                reason = "";
                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "HirataRobotGoto"), (out string reason, int time, object[] param) =>
            {
                RobotArmEnum arm = (RobotArmEnum)Enum.Parse(typeof(RobotArmEnum), param[0].ToString());
                string station = param[1].ToString();
                int slot = Convert.ToInt32(param[2]);
                RobotPostionEnum pos = (RobotPostionEnum)Enum.Parse(typeof(RobotPostionEnum), param[3].ToString());
                bool ret = GoTo(arm, station, slot, pos);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "HirataRobotGoto");
                    return true;
                }
                reason = "";
                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "SetSpeed"), (out string reason, int time, object[] param) =>
            {
                bool ret = SetSpeed(param);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "SetSpeed");
                    return true;
                }
                reason = "";
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "SetParameter"), (out string reason, int time, object[] param) =>
            {
                bool ret = SetParameter(param);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "SetParameter");
                    return true;
                }
                reason = "";
                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "ReadParameter"), (out string reason, int time, object[] param) =>
            {
                bool ret = ReadParameter(param);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "ReadData");
                    return true;
                }
                reason = "";
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotMove"), (out string reason, int time, object[] param) =>
            {
                bool ret = Move(param);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "MovePostion");
                    return true;
                }
                reason = "";
                return false;
            });
        }

        public virtual bool Move(object[] param)
        {
            return CheckToPostMessage((int)RobotMsg.Move, param); ;
        }

        public virtual bool ReadParameter(object[] param)
        {
            return CheckToPostMessage((int)RobotMsg.ReadData, param);
        }
        public virtual bool SetParameter(object[] arg2)
        {
            return CheckToPostMessage((int)RobotMsg.SetParameters, arg2);
        }
        public virtual bool SetWaferSize(RobotArmEnum arm, WaferSize size)
        {
            return CheckToPostMessage((int)RobotMsg.SetParameters, new object[] { "WaferSize", arm, size });
        }

        public bool InvokeSetSpeed(string arg1, object[] arg2)
        {
            var speed = 3;
            //string reason;
            if (int.TryParse((string)arg2[0], out speed))
            {
                if (!(speed <= 3 || speed >= 1))
                    EV.PostWarningLog(ModuleName.Robot.ToString(), "Error Parameter,speed should be range in 1, 3");

                return SetSpeed(new object[] { speed });
            }
            EV.PostWarningLog(ModuleName.Robot.ToString(), $"invalid parameters. {arg2[0]}");
            return false;
        }
        public virtual bool SetSpeed(object[] arg2)
        {
            if (IsBusy) return false;
            List<object> paras = new List<object>() { "TransferSpeedLevel" };
            foreach (var value in arg2) paras.Add(value);
            return CheckToPostMessage((int)RobotMsg.SetParameters, paras.ToArray());
        }
        public virtual bool SetSpeed(string speedtype, int value)
        {
            return CheckToPostMessage((int)RobotMsg.SetParameters, new object[] { speedtype, value });
        }
        public virtual bool InvokeRobotGoTo(string arg1, object[] arg2)
        {
            return CheckToPostMessage((int)RobotMsg.GoToPosition, arg2);
        }

        private void SubscribeDataVariable()
        {
            DATA.Subscribe($"{Name}.State", () => RobotState.ToString());
            DATA.Subscribe($"{Name}.RobotState", () => RobotState.ToString());
            DATA.Subscribe($"{Module}.{Name}.State", () => RobotState.ToString());

            DATA.Subscribe($"{Module}.{Name}.Axis1Postion", () => PositionAxis1.ToString());
            DATA.Subscribe($"{Module}.{Name}.Axis2Postion", () => PositionAxis2.ToString());
            DATA.Subscribe($"{Module}.{Name}.Axis3Postion", () => PositionAxis3.ToString());
            DATA.Subscribe($"{Module}.{Name}.Axis4Postion", () => PositionAxis4.ToString());
            DATA.Subscribe($"{Module}.{Name}.Axis5Postion", () => PositionAxis5.ToString());
            DATA.Subscribe($"{Module}.{Name}.RobotErrorCode", () => ErrorCode ?? ErrorCode.ToString());

            DATA.Subscribe($"{Name}.RobotMoveInfo", () => MoveInfo);

            DATA.Subscribe($"{Name}.RobotBladeTraget", () => BladeTarget);
            DATA.Subscribe($"{Name}.RobotBlade1Traget", () => Blade1Target);
            DATA.Subscribe($"{Name}.RobotBlade2Traget", () => Blade2Target);

            DATA.Subscribe($"{Name}.WaferSize", () => GetCurrentWaferSize().ToString());
            DATA.Subscribe($"{Name}.Blade1WaferSize", () => GetBladeWaferSize(0).ToString());
            DATA.Subscribe($"{Name}.Blade2WaferSize", () => GetBladeWaferSize(1).ToString());

            DATA.Subscribe($"{Name}.CommandMessages", () => _commandMsgs);

        }

        private WaferSize GetBladeWaferSize(int slotIndex)
        {
            if(WaferManager.Instance.CheckHasWafer(RobotModuleName, slotIndex))
                return WaferManager.Instance.GetWaferSize(RobotModuleName, slotIndex);
            return Size;
        }

        private int _SmInterval
        {
            get
            {
                if (SC.ContainsItem($"Robot.{RobotModuleName}.SMInterval"))
                    return SC.GetValue<int>($"Robot.{RobotModuleName}.SMInterval");
                return 5;
            }
        }

        private void BuildTransitionTable()
        {
            fsm = new StateMachine<RobotBaseDevice>(Name + ".StateMachine", (int)RobotStateEnum.Init, _SmInterval);

            //Init sequence
            AnyStateTransition(RobotMsg.ERROR, fError, RobotStateEnum.Error);
            AnyStateTransition(RobotMsg.Reset, fReset, RobotStateEnum.Resetting);
            AnyStateTransition(RobotMsg.Stop, fStop, RobotStateEnum.Stopped);
            AnyStateTransition(RobotMsg.Abort, fAbort, RobotStateEnum.Init);
            AnyStateTransition(RobotMsg.StartMaintenance, fStartMaitenance, RobotStateEnum.Maintenance);

            Transition(RobotStateEnum.Maintenance, RobotMsg.CompleteMaintenance, fCompleteMaintenance, RobotStateEnum.Init);
            Transition(RobotStateEnum.Maintenance, FSM_MSG.TIMER, fMonitorMaintenance, RobotStateEnum.Init);


            Transition(RobotStateEnum.Resetting, RobotMsg.ActionDone, fResetComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Resetting, FSM_MSG.TIMER, fMonitorReset, RobotStateEnum.Idle);


            Transition(RobotStateEnum.Init, RobotMsg.StartInit, fStartInit, RobotStateEnum.Initializing);
            Transition(RobotStateEnum.Idle, RobotMsg.StartInit, fStartInit, RobotStateEnum.Initializing);
            Transition(RobotStateEnum.Error, RobotMsg.StartInit, fStartInit, RobotStateEnum.Initializing);

            Transition(RobotStateEnum.Error, RobotMsg.InitComplete, fInitComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Init, RobotMsg.InitComplete, fInitComplete, RobotStateEnum.Idle);

            Transition(RobotStateEnum.Initializing, RobotMsg.InitComplete, fInitComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Initializing, RobotMsg.ActionDone, fInitComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Initializing, FSM_MSG.TIMER, fMonitorInit, RobotStateEnum.Idle);

            Transition(RobotStateEnum.Init, RobotMsg.StartHome, fStartHome, RobotStateEnum.Homing);
            Transition(RobotStateEnum.Idle, RobotMsg.StartHome, fStartHome, RobotStateEnum.Homing);
            Transition(RobotStateEnum.Error, RobotMsg.StartHome, fStartHome, RobotStateEnum.Homing);

            Transition(RobotStateEnum.Init, RobotMsg.HomeComplete, fHomeComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Error, RobotMsg.HomeComplete, fHomeComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Homing, RobotMsg.HomeComplete, fHomeComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Homing, RobotMsg.ActionDone, fHomeComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Homing, FSM_MSG.TIMER, fMonitorHome, RobotStateEnum.Idle);

            Transition(RobotStateEnum.Error, RobotMsg.Clear, fClear, RobotStateEnum.Init);
            Transition(RobotStateEnum.Error, RobotMsg.ResetToReady, fResetToReady, RobotStateEnum.Idle);

            Transition(RobotStateEnum.Idle, RobotMsg.SetParameters, fStartSetParameters, RobotStateEnum.SettingServos);
            Transition(RobotStateEnum.SettingServos, RobotMsg.SetParametersComplete, fSetComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.SettingServos, RobotMsg.ActionDone, fSetComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.SettingServos, FSM_MSG.TIMER, fMonitorSetParamter, RobotStateEnum.Idle);


            Transition(RobotStateEnum.Idle, RobotMsg.ReadData, fStartReadData, RobotStateEnum.ReadingData);
            Transition(RobotStateEnum.ReadingData, RobotMsg.ActionDone, null, RobotStateEnum.Idle);
            Transition(RobotStateEnum.ReadingData, RobotMsg.ReadDataComplete, null, RobotStateEnum.Idle);
            Transition(RobotStateEnum.ReadingData, FSM_MSG.TIMER, fMonitorReadData, RobotStateEnum.Idle);




            Transition(RobotStateEnum.Idle, RobotMsg.PickWafer, fStartPickWafer, RobotStateEnum.Picking);
            Transition(RobotStateEnum.Picking, RobotMsg.PickComplete, fPickComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Picking, RobotMsg.ActionDone, fPickComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Picking, FSM_MSG.TIMER, fMonitorPick, RobotStateEnum.Idle);


            Transition(RobotStateEnum.Idle, RobotMsg.PlaceWafer, fStartPlaceWafer, RobotStateEnum.Placing);
            Transition(RobotStateEnum.Placing, RobotMsg.PlaceComplete, fPlaceComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Placing, RobotMsg.ActionDone, fPlaceComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Placing, FSM_MSG.TIMER, fMonitorPlace, RobotStateEnum.Idle);


            Transition(RobotStateEnum.Idle, RobotMsg.SwapWafer, fStartSwapWafer, RobotStateEnum.Swapping);
            Transition(RobotStateEnum.Swapping, RobotMsg.SwapComplete, fSwapComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Swapping, RobotMsg.ActionDone, fSwapComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Swapping, FSM_MSG.TIMER, fMonitorSwap, RobotStateEnum.Idle);


            Transition(RobotStateEnum.Idle, RobotMsg.MapWafer, fStartMapWafer, RobotStateEnum.Mapping);
            Transition(RobotStateEnum.Mapping, RobotMsg.MapComplete, fMapComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Mapping, RobotMsg.ActionDone, fMapComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Mapping, FSM_MSG.TIMER, fMonitorMap, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Mapping, RobotMsg.ReadData, fStartReadData, RobotStateEnum.ReadingData);


            Transition(RobotStateEnum.Idle, RobotMsg.GoToPosition, fStartGoTo, RobotStateEnum.GoingToPosition);
            Transition(RobotStateEnum.Idle, RobotMsg.Move, fStartMove, RobotStateEnum.Moving);
            Transition(RobotStateEnum.Moving, RobotMsg.MoveComplete, fMoveComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Moving, RobotMsg.ActionDone, fMoveComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Moving, FSM_MSG.TIMER, fMonitorMoving, RobotStateEnum.Idle);


            Transition(RobotStateEnum.GoingToPosition, RobotMsg.ArrivedPosition, fGoToComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.GoingToPosition, RobotMsg.MoveComplete, fMoveComplete, RobotStateEnum.Idle);

            Transition(RobotStateEnum.GoingToPosition, RobotMsg.ActionDone, fGoToComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.GoingToPosition, FSM_MSG.TIMER, fMonitorGoTo, RobotStateEnum.Idle);


            Transition(RobotStateEnum.Idle, RobotMsg.Grip, fStartGrip, RobotStateEnum.Gripping);
            Transition(RobotStateEnum.Gripping, RobotMsg.GripComplete, fGripComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Gripping, RobotMsg.ActionDone, fGripComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Gripping, FSM_MSG.TIMER, fMonitorGrip, RobotStateEnum.Idle);


            Transition(RobotStateEnum.Idle, RobotMsg.UnGrip, fStartUnGrip, RobotStateEnum.UnGripping);
            Transition(RobotStateEnum.UnGripping, RobotMsg.UnGripComplete, fUnGripComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.UnGripping, RobotMsg.ActionDone, fUnGripComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.UnGripping, FSM_MSG.TIMER, fMonitorUnGrip, RobotStateEnum.Idle);


            Transition(RobotStateEnum.Idle, RobotMsg.ExtendForPick, fStartExtendForPick, RobotStateEnum.ExtendingForPick);
            Transition(RobotStateEnum.ExtendingForPick, RobotMsg.ExtendComplete, fExtendForPickComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.ExtendingForPick, RobotMsg.ActionDone, fExtendForPickComplete, RobotStateEnum.Idle);


            Transition(RobotStateEnum.Idle, RobotMsg.ExtendForPlace, fStartExtendForPlace, RobotStateEnum.ExtendingForPlace);
            Transition(RobotStateEnum.ExtendingForPlace, RobotMsg.ExtendComplete, fExtendForPlaceComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.ExtendingForPlace, RobotMsg.ActionDone, fExtendForPlaceComplete, RobotStateEnum.Idle);


            Transition(RobotStateEnum.Idle, RobotMsg.RetractFromPick, fStartRetractFromPick, RobotStateEnum.RetractingFromPick);
            Transition(RobotStateEnum.RetractingFromPick, RobotMsg.RetractComplete, fRetractFromPickComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.RetractingFromPick, RobotMsg.ActionDone, fRetractFromPickComplete, RobotStateEnum.Idle);


            Transition(RobotStateEnum.Idle, RobotMsg.RetractFromPlace, fStartRetractFromPlace, RobotStateEnum.RetractingFromPlace);
            Transition(RobotStateEnum.RetractingFromPlace, RobotMsg.RetractComplete, fRetractFromPlaceComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.RetractingFromPlace, RobotMsg.ActionDone, fRetractFromPlaceComplete, RobotStateEnum.Idle);

            Transition(RobotStateEnum.Idle, RobotMsg.TransferWafer, fStartTransferWafer, RobotStateEnum.Transferring);
            Transition(RobotStateEnum.Transferring, RobotMsg.TransferComplete, fTransferPlaceComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Transferring, RobotMsg.ActionDone, fTransferPlaceComplete, RobotStateEnum.Idle);

            Transition(RobotStateEnum.Idle, RobotMsg.PickCassette, fStartPickCassette, RobotStateEnum.PickingCassette);
            Transition(RobotStateEnum.PickingCassette, RobotMsg.PickCassetteComplete, fPickCassetteComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.PickingCassette, RobotMsg.ActionDone, fPickCassetteComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.PickingCassette, FSM_MSG.TIMER, fMonitorPickingCassette, RobotStateEnum.Idle);

            Transition(RobotStateEnum.Idle, RobotMsg.PlaceCassette, fStartPlaceCassette, RobotStateEnum.PlacingCassette);
            Transition(RobotStateEnum.PlacingCassette, RobotMsg.PlaceCassetteComplete, fPlaceCassetteComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.PlacingCassette, RobotMsg.ActionDone, fPlaceCassetteComplete, RobotStateEnum.Idle);
            Transition(RobotStateEnum.PlacingCassette, FSM_MSG.TIMER, fMonitorPlacingCassette, RobotStateEnum.Idle);

            Transition(RobotStateEnum.Idle, RobotMsg.ExecuteCommand, fStartExecuteCommand, RobotStateEnum.Executing);
            Transition(RobotStateEnum.Executing, FSM_MSG.TIMER, fMonitorExecuting, RobotStateEnum.Idle);
            Transition(RobotStateEnum.Executing, RobotMsg.ActionDone, null, RobotStateEnum.Idle);
        }

        protected virtual bool fMonitorExecuting(object[] param)
        {
            IsBusy = false;
            return false;
        }

        protected virtual bool fStartExecuteCommand(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorHome(object[] param)
        {
            return false;
        }

        protected virtual bool fHomeComplete(object[] param)
        {
            return true;
        }

        protected virtual bool fStartHome(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorPlacingCassette(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorPickingCassette(object[] param)
        {
            return false;
        }

        protected virtual bool fPlaceCassetteComplete(object[] param)
        {
            return true;
        }

        protected virtual bool fStartPlaceCassette(object[] param)
        {
            return true;
        }

        protected virtual bool fPickCassetteComplete(object[] param)
        {
            return true;
        }

        protected virtual bool fStartPickCassette(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorMoving(object[] param)
        {
            return false;
        }

        protected virtual bool fMonitorMaintenance(object[] param)
        {
            return false;
        }

        protected virtual bool fCompleteMaintenance(object[] param)
        {
            return true;
        }

        protected virtual bool fStartMaitenance(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorUnGrip(object[] param)
        {
            return true;
        }

        protected virtual bool fMonitorGrip(object[] param)
        {
            return true;
        }

        protected virtual bool fMoveComplete(object[] param)
        {
            IsBusy = false;
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

        protected virtual bool fStop(object[] param)
        {
            return true;
        }

        protected virtual bool fAbort(object[] param)
        {
            return true;
        }

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
        protected abstract bool fStartTransferWafer(object[] param);
        protected abstract bool fStartUnGrip(object[] param);
        protected abstract bool fStartGrip(object[] param);
        protected abstract bool fStartGoTo(object[] param);
        protected abstract bool fStartMapWafer(object[] param);
        protected abstract bool fStartSwapWafer(object[] param);
        protected abstract bool fStartPlaceWafer(object[] param);
        protected abstract bool fStartPickWafer(object[] param);
        protected abstract bool fResetToReady(object[] param);
        protected abstract bool fReset(object[] param);
        protected abstract bool fStartInit(object[] param);
        protected abstract bool fError(object[] param);

        protected abstract bool fStartExtendForPick(object[] param);
        protected abstract bool fStartExtendForPlace(object[] param);
        protected abstract bool fStartRetractFromPick(object[] param);
        protected abstract bool fStartRetractFromPlace(object[] param);
        protected virtual bool fTransferPlaceComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }
        protected virtual bool fGoToComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }
        protected virtual bool fExtendForPickComplete(object[] param)
        {
            return true;
        }
        protected virtual bool fExtendForPlaceComplete(object[] param)
        {
            return true;
        }
        protected virtual bool fRetractFromPickComplete(object[] param)
        {
            return true;
        }
        protected virtual bool fRetractFromPlaceComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }

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

        protected virtual bool fMapComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }


        protected virtual bool fSwapComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }



        protected virtual bool fPlaceComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }


        protected virtual bool fPickComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }






        protected virtual bool fInitFail(object[] param)
        {
            IsBusy = false;
            return true;
        }

        protected virtual bool fInitComplete(object[] param)
        {
            IsBusy = false;
            return true;
        }


        public RobotStateEnum RobotState { get => (RobotStateEnum)fsm.State; }
        public string Module { get; set; }
        public string Name { get; set; }
        public string ScRoot { get; set; }

        public ModuleName RobotModuleName { get; private set; }
        public ModuleName CurrentInteractModule { get; private set; }
        public virtual bool ApproachForPick(RobotArmEnum arm, string station, int slot)
        {
            return true;
        }

        public virtual bool ApproachForPlace(RobotArmEnum arm, string station, int slot)
        {
            return true;
        }

        public virtual bool DualPick(string station, int lowerSlot)
        {
            return true;
        }

        public virtual bool DualPlace(string station, int lowerSlot)
        {
            return true;
        }

        public virtual bool DualSwap(string station, int lowerSlotPlaceTo, int lowerSlotPickFrom)
        {
            return true;
        }

        public virtual bool DualTransfer(string sourceStation, int sourceLowerSlot, string destStation, int destLowerSlot)
        {
            return true;
        }

        public virtual string[] GetStationsInUse()
        {
            return null;
        }

        public abstract RobotArmWaferStateEnum GetWaferState(RobotArmEnum arm);

        #region Action
        public virtual bool Home(object[] param)
        {
            return CheckToPostMessage((int)RobotMsg.StartInit, param);
        }
        public virtual bool Init(object[] param)
        {
            return CheckToPostMessage((int)RobotMsg.StartInit, param);
        }
        public virtual bool HomeModule(object[] param)
        {
            return CheckToPostMessage((int)RobotMsg.StartHome, param);
        }
        public virtual bool Abort()
        {
            return CheckToPostMessage((int)RobotMsg.Abort);
        }
        public virtual bool Grip(RobotArmEnum arm)
        {
            return CheckToPostMessage((int)RobotMsg.Grip, new object[] { (int)arm });
        }
        public virtual bool Release(RobotArmEnum arm)
        {
            return CheckToPostMessage((int)RobotMsg.UnGrip, new object[] { (int)arm });
        }
        public virtual bool Stop()
        {
            return CheckToPostMessage((int)RobotMsg.Stop, null);
        }

        public virtual bool GoTo(object[] param)
        {
            if (!IsReady())
                return false;
            if (PostMessageWithoutCheck((int)RobotMsg.GoToPosition, param))
            {
                string log = "";
                foreach (var obj in param)
                {
                    log += obj.ToString() + ",";
                }
                LOG.Write($"{RobotModuleName} start go to {log}");
                return true;
            }
            return false;
        }
        public virtual bool GoTo(RobotArmEnum arm, string station, int slot, RobotPostionEnum pos, float xoffset = 0, float yoffset = 0,
           float zoffset = 0, float woffset = 0, bool isFromOriginal = true, bool isJumpToNextMotion = false)
        {
            return GoTo(new object[] { arm, station, slot, pos, xoffset, yoffset, zoffset, woffset, isFromOriginal, isJumpToNextMotion });
        }
 

        public virtual bool RobotFlippe(bool to0degree)
        {
            return false;
        }
        #endregion



        public virtual void Monitor()
        {
            return;
        }

        public virtual bool Pick(RobotArmEnum arm, string station, int slot)
        {
            if (!IsReady())
            {
                
                return false;
            }

            IsBusy = true;

            LOG.Write($"{RobotModuleName} {arm} start post message to pick wafer from station:{station},slot:{slot + 1}.");
            if (PostMessageWithoutCheck((int)RobotMsg.PickWafer, new object[] { arm, station, slot }))
            {
                LOG.Write($"{RobotModuleName} {arm} start pick wafer from station:{station},slot:{slot+1}.");
                
                return true;
            }

            return false;
        }

        public virtual bool PickCassette(RobotArmEnum arm,string station,int slot)
        {
            if (!IsReady())
                return false;
            IsBusy = true;

            LOG.Write($"{RobotModuleName} {arm} start post message to pick cassette from {station}:{slot}");
            if (PostMessageWithoutCheck((int)RobotMsg.PickCassette, new object[] { station, arm, slot }))
            {
                LOG.Write($"{RobotModuleName} {arm} start pick cassette from {station}:{slot}");
                
                return true;
            }
            return false;
        }


        public virtual bool PickEx(RobotArmEnum arm, string station, int slot, float xoffset = 0, float yoffset = 0, float zoffset = 0, float woffset = 0)
        {
            if (!IsReady())
                return false;

            IsBusy = true;

            if (PostMessageWithoutCheck((int)RobotMsg.PickWafer, new object[] { arm, station, slot, xoffset, yoffset, zoffset, woffset }))
            {
                IsBusy = true;
                return true;
            }
            return false;
        }
        public virtual bool PickEx(RobotArmEnum arm, string station, int slot, float radialOffset = 0, float thetaOffset = 0)
        {
            if (!IsReady())
                return false;
            IsBusy = true;
            if (PostMessageWithoutCheck((int)RobotMsg.PickWafer, new object[] { arm, station, slot, radialOffset, thetaOffset }))
            {
                IsBusy = true;
                return true;
            }
            return false;
        }
        public virtual bool Place(RobotArmEnum arm, string station, int slot)
        {
            if (!IsReady())
                return false;

            IsBusy = true;
            LOG.Write($"{RobotModuleName} {arm} start post message to place wafer to station:{station},slot:{slot + 1}.");
            if (PostMessageWithoutCheck((int)RobotMsg.PlaceWafer, new object[] { arm, station, slot }))
            {
                LOG.Write($"{RobotModuleName} {arm} start place wafer to  station:{station},slot:{slot + 1}.");
                IsBusy = true;
                return true;
            }
            return false;
        }

        public virtual bool PlaceCassette(RobotArmEnum arm, string station, int slot)
        {
            if (!IsReady())
                return false;

            IsBusy = true;

            if (PostMessageWithoutCheck((int)RobotMsg.PlaceCassette, new object[] { station, arm,slot }))
            {
                LOG.Write($"{RobotModuleName} {arm} start place cassette to {station}:{slot}");
                IsBusy = true;
                return true;
            }
            return false;
        }
        public virtual bool PlaceEx(RobotArmEnum arm, string station, int slot, float xoffset = 0, float yoffset = 0, float zoffset = 0, float thetaoffset = 0)
        {
            if (!IsReady())
                return false;

            IsBusy = true;

            if (PostMessageWithoutCheck((int)RobotMsg.PlaceWafer, new object[] { arm, station, slot, xoffset, yoffset, zoffset, thetaoffset }))
            {
                IsBusy = true;
                return true;
            }
            return false;
        }
        public virtual bool PlaceEx(RobotArmEnum arm, string station, int slot, float radialOffset = 0, float thetaOffset = 0)
        {
            if (!IsReady())
                return false;

            IsBusy = true;

            if (PostMessageWithoutCheck((int)RobotMsg.PlaceWafer, new object[] { arm, station, slot, radialOffset, thetaOffset }))
            {
                IsBusy = true;
                return true;
            }
            return false;
        }


        public virtual bool Swap(RobotArmEnum pickArm, string station, int slot)
        {
            if (!IsReady())
                return false;

            IsBusy = true;

            if (PostMessageWithoutCheck((int)RobotMsg.SwapWafer, new object[] { pickArm, station, slot }))
            {
                LOG.Write($"{RobotModuleName} {pickArm} start swap wafer from station:{station},slot:{slot+1}.");
                IsBusy = true;
                return true;
            }
            return false;

        }
        public virtual bool SwapEx(RobotArmEnum pickArm, string station, int slot, float xoffset = 0, float yoffset = 0, float zoffset = 0, float thetaoffset = 0)
        {
            if (!IsReady())
                return false;

            IsBusy = true;

            if (PostMessageWithoutCheck((int)RobotMsg.SwapWafer, new object[] { pickArm, station, slot, xoffset, yoffset, zoffset, thetaoffset }))
            {
                IsBusy = true;
                return true;
            }
            return false;

        }
        public virtual bool Transfer(RobotArmEnum arm, string sourceStation, int sourceSlot, string destStation, int destSlot)
        {
            if (CheckToPostMessage((int)RobotMsg.TransferWafer, new object[] { arm, sourceStation, sourceSlot, destStation, destSlot }))
            {
                IsBusy = true;
                return true;
            }
            return false;
        }

        public virtual bool RobotReset()
        {
            if (CheckToPostMessage((int)RobotMsg.Reset, new object[] { "Reset" }))
            {
                IsBusy = true;
                return true;
            }
            return false;
        }
        public virtual void Reset()
        {

        }
        public virtual void OnError(string errortext)
        {
            EV.PostAlarmLog(Module, $"{Name} occurred error: {errortext}");
            IsBusy = false;
            CheckToPostMessage((int)RobotMsg.ERROR, new object[] { errortext });
        }
        public virtual bool SetParameters(object[] param)
        {
            return CheckToPostMessage((int)RobotMsg.SetParameters, param);
        }

        public void NotifySlotMapResult(ModuleName ModuleName, string slotMap)
        {
            if (OnSlotMapRead != null)
            {
                OnSlotMapRead(ModuleName, slotMap);
            }
        }

        public virtual bool HomeAllAxes(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool WaferMapping(ModuleName module, out string reason)
        {
            reason = "";
            if (!IsEnableMapWafer)
            {
                reason = "RobotIsNotEnableMapWafer";
                EV.PostAlarmLog("Robot", $"Robot is not enable to map wafer");
                return false;
            }
            if (CheckToPostMessage((int)RobotMsg.MapWafer, new object[] { module }))
            {
                CurrentInteractModule = module;
                IsBusy = true;
                return true;
            }
            return false;
        }
        public virtual bool WaferMapping(ModuleName module, int thicknessIndex=0,WaferSize size = WaferSize.WS12)
        { 
            if(!IsEnableMapWafer)
            {
                EV.PostAlarmLog("Robot", $"Robot is not enable to map wafer");
                return false;
            }
            if (CheckToPostMessage((int)RobotMsg.MapWafer, new object[] { module, thicknessIndex, size }))
            {
                CurrentInteractModule = module;
                IsBusy = true;
                return true;
            }
            return false;
        }

        public virtual bool QueryWaferMap(ModuleName module, out string reason)
        {
            reason = "";
            return true;
        }
        public virtual bool SetMaintenanceMode(bool value)
        {
            if (value)
                return CheckToPostMessage((int)RobotMsg.StartMaintenance, null);

            return CheckToPostMessage((int)RobotMsg.CompleteMaintenance, null);
        }

        public virtual bool OnActionDone(object[] param)
        {
            IsBusy = false;
            if(CheckToPostMessage((int)RobotMsg.ActionDone, new object[] { "ActionDone" }))
            {
                LOG.Write($"{RobotModuleName} action done");
            }
            return true;
        }
        public virtual bool ExecuteCommand(object[] paras)
        {
            IsBusy = true;
            if (CheckToPostMessage((int)RobotMsg.ExecuteCommand, paras))
            {
                LOG.Write($"{RobotModuleName} excute command:{paras[0].ToString()}");
            }
            return true;
        }

        public bool CheckToPostMessage(int msg, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, msg))
            {
                if (msg != 7)
                    EV.PostWarningLog(Name, $"{Name} is in { (RobotStateEnum)fsm.State} state，can not do {(RobotMsg)msg}");
                return false;
            }
            if ((RobotMsg)msg == RobotMsg.GoToPosition || (RobotMsg)msg == RobotMsg.ExtendForPick
                || (RobotMsg)msg == RobotMsg.ExtendForPlace || (RobotMsg)msg == RobotMsg.Grip
                || (RobotMsg)msg == RobotMsg.MapWafer || (RobotMsg)msg == RobotMsg.Move
                || (RobotMsg)msg == RobotMsg.PickWafer || (RobotMsg)msg == RobotMsg.PlaceWafer
                || (RobotMsg)msg == RobotMsg.RetractFromPick || (RobotMsg)msg == RobotMsg.StartInit
                || (RobotMsg)msg == RobotMsg.SwapWafer || (RobotMsg)msg == RobotMsg.TransferWafer
                || (RobotMsg)msg == RobotMsg.UnGrip || (RobotMsg)msg == RobotMsg.SetParameters
                || (RobotMsg)msg == RobotMsg.StartHome || (RobotMsg)msg == RobotMsg.ReadData)
                IsBusy = true;
            if ((RobotMsg)msg != RobotMsg.ActionDone && !((RobotMsg)msg).ToString().Contains("Complete"))
                CurrentParamter = args;
            fsm.PostMsg(msg, args);

            return true;
        }
        public bool PostMessageWithoutCheck(int msg, params object[] args)
        {
            if ((RobotMsg)msg == RobotMsg.GoToPosition || (RobotMsg)msg == RobotMsg.ExtendForPick
                || (RobotMsg)msg == RobotMsg.ExtendForPlace || (RobotMsg)msg == RobotMsg.Grip
                || (RobotMsg)msg == RobotMsg.MapWafer || (RobotMsg)msg == RobotMsg.Move
                || (RobotMsg)msg == RobotMsg.PickWafer || (RobotMsg)msg == RobotMsg.PlaceWafer
                || (RobotMsg)msg == RobotMsg.RetractFromPick || (RobotMsg)msg == RobotMsg.StartInit
                || (RobotMsg)msg == RobotMsg.SwapWafer || (RobotMsg)msg == RobotMsg.TransferWafer
                || (RobotMsg)msg == RobotMsg.UnGrip || (RobotMsg)msg == RobotMsg.SetParameters
                || (RobotMsg)msg == RobotMsg.StartHome)
                IsBusy = true;
            if ((RobotMsg)msg != RobotMsg.ActionDone && !((RobotMsg)msg).ToString().Contains("Complete"))
                CurrentParamter = args;
            fsm.PostMsgWithoutLock(msg, args);

            return true;
        }
        public bool CheckToPostMessage(RobotMsg msg, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, (int)msg))
            {
                if ((int)msg != 7)
                    EV.PostWarningLog(Name, $"{Name} is in { (RobotStateEnum)fsm.State} state，can not do {(RobotMsg)msg}");
                return false;
            }
            if (msg == RobotMsg.GoToPosition || msg == RobotMsg.ExtendForPick
                || msg == RobotMsg.ExtendForPlace || msg == RobotMsg.Grip
                || msg == RobotMsg.MapWafer || msg == RobotMsg.Move
                || msg == RobotMsg.PickWafer || msg == RobotMsg.PlaceWafer
                || msg == RobotMsg.RetractFromPick || msg == RobotMsg.StartInit
                || msg == RobotMsg.SwapWafer || msg == RobotMsg.TransferWafer
                || msg == RobotMsg.UnGrip || (RobotMsg)msg == RobotMsg.SetParameters
                || msg == RobotMsg.ExecuteCommand)
                IsBusy = true;
            if (msg != RobotMsg.ActionDone && !(msg).ToString().Contains("Complete"))
                CurrentParamter = args;
            fsm.PostMsgWithoutLock((int)msg, args);

            return true;
        }

        public bool Check(int msg, out string reason, params object[] args)
        {
            if (!fsm.FindTransition(fsm.State, msg))
            {
                reason = String.Format("{0} is in {1} state，can not do {2}", Name, (RobotStateEnum)fsm.State, (RobotMsg)msg);
                return false;
            }
            //CurrentParamter = args;
            reason = "";

            return true;
        }

    }
    public enum RobotMoveDirectionEnum
    {
        Fwd,
        Rev
    }
}
