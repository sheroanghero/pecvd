using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Aitex.Core.Common;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using TSC = Aitex.Sorter.Common;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using Aitex.Core.RT.OperationCenter;
using MECF.Framework.Common.CommonData;
using System.IO.Ports;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot
{
    public enum RobotType
    {
        SR100,
        NX100,
        MAG7,
        DAIHEN,
        HAtm,
        HIRATA_R4
    }

    public class Robot : BaseDevice, IDevice, IConnection
    {
        public event Action<ModuleName, string> OnSlotMapRead;

        public event Action<bool> ActionDone;

        public string Address
        {
            get { return _addr; }
        }

        public virtual bool IsConnected
        {
            get 
            {
                if (_socket != null)
                    return _socket.IsConnected;
                else if (_port != null)
                    return _port.IsOpen();
                else
                    return false;
            }
        }

        public bool Disconnect()
        {
            _commErr = true;
            if (_socket != null)
                _socket.Dispose();
            else if (_port != null)
                _port.Close();
            return true;
        }

        public bool IsOnline
        {
            get { return _isOnline; }
        }

        public const string delimiter = "\r";

        private bool _isOnline;

        public int LastErrorCode { get; set; }
        public int Status { get; set; }
        public int ErrorCode { get; set; }

        public int ElapseTime { get; set; }
        public int Speed { get; set; }

        public int Rotation { get;  set; }
        public int Extension { get;  set; }
        public int Wrist1 { get;  set; }
        public int Wrist2 { get;  set; }
        public int Evevation { get;  set; }

        public bool StateWaferOnBlade1 { get; set; }
        public bool StateWaferOnBlade2 { get; set; }
        public bool StateBlade1Gripped { get; set; }
        public bool StateBlade2Gripped { get; set; }

        public bool StateInterlock1 { get; set; }
        public bool StateInterlock2 { get; set; }
        public bool StateInterlock3 { get; set; }
        public bool StateInterlock4 { get; set; }
        public bool StateInterlock5 { get; set; }
        public bool StateInterlock6 { get; set; }
        public bool StateInterlock7 { get; set; }
        public bool StateInterlock8 { get; set; }

        public bool Swap { get; set; }
        public Hand PlaceBalde { get; set; }

        public ModuleName Blade1Target
        {
            get;
            set;
        }
        public ModuleName Blade2Target { get; set; }

        public RobotMoveInfo MoveInfo { get; set; }

        //naming follow PM1.A LL1.A PM2.B
        public string CmdBladeTarget { get; set; }

        //naming follow 0 1
        public string CmdBlade1Extend { get; set; }

        //naming follow 0 1
        public string CmdBlade2Extend { get; set; }

        public bool BladeAEnable
        {
            get
            {
                if (SC.ContainsItem($"{Name}.BladeAEnable"))
                {
                    return SC.GetValue<bool>($"{Name}.BladeAEnable");
                }

                return SC.GetValue<bool>($"TMRobot.BladeAEnable");
            }
            set
            {
                if (SC.ContainsItem($"{Name}.BladeAEnable"))
                {
                    SC.SetItemValue($"{Name}.BladeAEnable", value);
                }else if (SC.ContainsItem($"Robot.BladeAEnable"))
                {
                    SC.SetItemValue($"Robot.BladeAEnable", value);
                }
            }
        }
        public bool BladeBEnable
        {
            get
            {
                if (SC.ContainsItem($"{Name}.BladeBEnable"))
                {
                    return SC.GetValue<bool>($"{Name}.BladeBEnable");
                }

                return SC.GetValue<bool>($"TMRobot.BladeBEnable");
            }
            set
            {
                if (SC.ContainsItem($"{Name}.BladeBEnable"))
                {
                    SC.SetItemValue($"{Name}.BladeBEnable", value);
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
                if (SC.ContainsItem($"{Name}.Blade2Slots"))
                {
                    return Math.Max(SC.GetValue<int>($"{Name}.Blade2Slots"), 1);
                }

                if (SC.ContainsItem($"Robot.Blade2Slots"))
                {
                    return Math.Max(SC.GetValue<int>($"Robot.Blade2Slots"), 1);
                }

                return 1;                 
            }
        }

        public bool Initalized { get; set; }

        public bool Communication
        {
            get
            {
                return !_commErr;
            }
        }


        public virtual bool Busy
        {
            get { return _backgroundHandler != null || _foregroundHandler != null; }
        }

        public virtual bool Moving
        {
            get { return _backgroundHandler != null; }
        }

        public virtual bool Error
        {
            get
            {
                return ErrorCode > 0 || _commErr || _exceuteErr;
            }
        }

        public DeviceState State
        {
            get
            {
                if (!Initalized)
                {
                    return DeviceState.Unknown;
                }
                if (Error)
                {
                    return DeviceState.Error;
                }

                if (Busy)
                    return DeviceState.Busy;

                return DeviceState.Idle;
            }
        }

        //public string ErrorMessage
        //{
        //    get
        //    {
        //        return _factory.GetError(LastErrorCode);
        //    }
        //}


        public virtual bool WaferOnBlade1 { get { return (Status & (int)StateBit.WaferOnBlade1) > 0; } }
        public virtual bool WaferOnBlade2 { get { return (Status & (int)StateBit.WaferOnBlade2) > 0; } }

        public bool WaferPresentOnBlade1 { get; set; }
        public bool WaferPresentOnBlade2 { get; set; }

        public Hand TransferBlade { 
            get 
            {
                if (!BladeAEnable) return Hand.Blade2;
                if (!BladeBEnable) return Hand.Blade1;
                return Hand.Both;
            }
        }
        

        protected static Object _locker = new Object();
        protected AsyncSocket _socket;
        private AsyncSerialPort _port;

        protected IHandler _eventHandler = null;
        protected IHandler _backgroundHandler = null;  //moving
        protected IHandler _foregroundHandler = null;  //current handler

        private IRobotHandlerFactory _factory = null;

        private DeviceTimer _timerQuery = new DeviceTimer();
        //private int _queryPeriod = 5000;    //ms
        //private bool _queryState = true;
        protected bool _exceuteErr = false;
        protected bool _commErr = false;

        protected string _addr;
        protected RobotType _type = RobotType.SR100;
        private string AlarmRobotError = "RobotMotionError";

        //Active wafer Center finder Data R
        public int CenterOffsetR { get; private set; }

        //Active wafer Center finder Data T
        public int CenterOffsetT { get; private set; }

        // Aligner Offset RO and TO
        public int AlignerOffsetR { get; private set; }
        public int AlignerOffsetT { get; private set; }

        public string AlignerA1 { get; private set; }
        public string AlignerA2 { get; private set; }
        public string AlignerA3 { get; private set; }
        public string AlignerA4 { get; private set; }

        public int RetryCounter { get; set; }
        public int MaxRetryCounter { get; set; }
        public bool AlignFinished { get; set; }

        private int _bufferMaxSlot = 6;
        public int BufferMaxSlot
        {
            set
            {
                _bufferMaxSlot = value;
            }
            get
            {
                return _bufferMaxSlot;
            }
        }

        public Robot(string module, string name, string display, string deviceId, string address, RobotType type = RobotType.SR100)
            : base(module, name, display, deviceId)
        {
            _addr = address;
            if (!string.IsNullOrEmpty(_addr))
            {
                _socket = new AsyncSocket(address);
                _socket.OnDataChanged += new AsyncSocket.MessageHandler(OnDataChanged);
                _socket.OnErrorHappened += new AsyncSocket.ErrorHandler(OnErrorHandler);
            }
            _type = type;

            Initalized = false;

            MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.Both,
                BladeTarget = "ArmA.System",
            };

            WaferManager.Instance.SubscribeLocation(name, 1+ Blade2Slots);
        }

        public Robot(string module, string name, string display, string deviceId, string port, RobotType type = RobotType.SR100, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One, string newline = "\r", bool isAsciiMode = true)
            : base(module, name, display, deviceId)
        {
            _addr = port;
            if (!string.IsNullOrEmpty(_addr))
            {
                //_isAsciiMode = isAsciiMode;
                //_newLine = newline;
                _port = new AsyncSerialPort(port, baudRate, dataBits, parity, stopBits, newline, isAsciiMode);

                _port.OnDataChanged += OnDataChanged;
                _port.OnErrorHappened += _port_OnErrorHappened;
            }
            _type = type;

            Initalized = false;

            MoveInfo = new RobotMoveInfo()
            {
                Action = RobotAction.Moving,
                ArmTarget = RobotArm.Both,
                BladeTarget = "ArmA.System",
            };

            if (name != "Aligner")
            {
                WaferManager.Instance.SubscribeLocation(name, 1 + Blade2Slots);
            }

        }

        public Robot(string module, string name,  string address, RobotType type = RobotType.SR100, int bufferMaxSlot = 6)
            : this(module, name, name, name, address, type)
        {
        }

        public virtual bool Initialize()
        {
            switch (_type)
            {
                case RobotType.NX100:
                    _factory = new NX100.NX100RobotHandlerFactory(this);
                    break;
                case RobotType.SR100:
                    _factory = new SR100.SR100RobotHandlerFactory(this);
                    break;
                case RobotType.MAG7:
                    _factory = new MAG7.Mag7RobotHandlerFactory(this);
                    break;
                //case RobotType.DAIHEN:
                //    _factory = new Daihen.DaihenRobotHandlerFactory(this);
                //    break;
            }
            

            ConnectionManager.Instance.Subscribe(Name, this);

            if (!string.IsNullOrEmpty(_addr))
                Connect();

            DEVICE.Register(String.Format("{0}.{1}", ModuleName.Robot.ToString(), "Init"), (out string reason, int time, object[] param) =>
            {
                bool ret = Init(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Initializing");
                    return true;
                }

                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", ModuleName.Robot.ToString(), "Home"), (out string reason, int time, object[] param) =>
            {
                bool ret = Home(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Homing");
                    return true;
                }

                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", ModuleName.Robot.ToString(), "RobotHome"), (out string reason, int time, object[] param) =>
            {
                bool ret = Home(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Homing");
                    return true;
                }

                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "Reset"), (out string reason, int time, object[] param) =>
            {
                bool ret = Clear(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Reset");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotReset"), (out string reason, int time, object[] param) =>
            {
                bool ret = Clear(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Reset");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "Grip"), (out string reason, int time, object[] param) =>
            {
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[0], true);
                bool ret = Grip(hand,out reason);
                //bool ret = WaferMapping(ModuleName.LP1, out reason);

                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Grip");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotGrip"), (out string reason, int time, object[] param) =>
            {
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[0], true);
                bool ret = Grip(hand, out reason);
                //bool ret = WaferMapping(ModuleName.LP1, out reason);

                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Grip");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "Release"), (out string reason, int time, object[] param) =>
            {
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[0], true);
                bool ret = Release(hand, out reason);
                //bool ret = QueryWaferMap(ModuleName.LP1, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Release");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotRelease"), (out string reason, int time, object[] param) =>
            {
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[0], true);
                bool ret = Release(hand, out reason);
                //bool ret = QueryWaferMap(ModuleName.LP1, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Release");
                    return true;
                }

                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "Stop"), (out string reason, int time, object[] param) =>
            {
                bool ret = Stop(false, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Stopping");
                    return true;
                }
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotStop"), (out string reason, int time, object[] param) =>
            {
                bool ret = Stop(false, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Stopping");
                    return true;
                }
                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "Pick"), (out string reason, int time, object[] param) =>
            {
                ModuleName chamber = (ModuleName)Enum.Parse(typeof(ModuleName), (string)param[0], true);
                int slot = int.Parse((string)param[1]);
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[2], true);
                bool ret = Pick(chamber, slot, hand, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Pick wafer");
                    return true;
                }
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotPick"), (out string reason, int time, object[] param) =>
            {
                ModuleName chamber = (ModuleName)Enum.Parse(typeof(ModuleName), (string)param[0], true);
                int slot = int.Parse((string)param[1]);
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[2], true);
                bool ret = Pick(chamber, slot, hand, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Pick wafer");
                    return true;
                }
                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "Place"), (out string reason, int time, object[] param) =>
            {
                ModuleName chamber = (ModuleName)Enum.Parse(typeof(ModuleName), (string)param[0], true);
                int slot = int.Parse((string)param[1]);
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[2], true);
                bool ret = Place(chamber, slot, hand, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Place wafer");
                    return true;
                }
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotPlace"), (out string reason, int time, object[] param) =>
            {
                ModuleName chamber = (ModuleName)Enum.Parse(typeof(ModuleName), (string)param[0], true);
                int slot = int.Parse((string)param[1]);
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[2], true);
                bool ret = Place(chamber, slot, hand, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Place wafer");
                    return true;
                }
                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "Exchange"), (out string reason, int time, object[] param) =>
            {
                ModuleName chamber = (ModuleName)Enum.Parse(typeof(ModuleName), (string)param[0], true);
                int slot = int.Parse((string)param[1]);
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[2], true);
                bool ret = Exchange(chamber, slot, hand, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Swap wafer");
                    return true;
                }
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotExchange"), (out string reason, int time, object[] param) =>
            {
                ModuleName chamber = (ModuleName)Enum.Parse(typeof(ModuleName), (string)param[0], true);
                int slot = int.Parse((string)param[1]);
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[2], true);
                bool ret = Exchange(chamber, slot, hand, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Swap wafer");
                    return true;
                }
                return false;
            });


            DEVICE.Register(String.Format("{0}.{1}", Name, "Goto"), (out string reason, int time, object[] param) =>
            {
                ModuleName chamber = (ModuleName)Enum.Parse(typeof(ModuleName), (string)param[0], true);
                int slot = int.Parse((string)param[1]);
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[2], true);
                Motion next = (Motion)Enum.Parse(typeof(Motion), (string)param[3], true);

                int x = int.Parse((string)param[4]);
                int y = int.Parse((string)param[5]);
                int z = int.Parse((string)param[6]);

                bool ret = Goto(chamber, slot, next, hand, x,y,z, out reason);
 
               
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "Goto");
                    return true;
                }
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotGoto"), (out string reason, int time, object[] param) =>
            {
                ModuleName chamber = (ModuleName)Enum.Parse(typeof(ModuleName), (string)param[0], true);
                int slot = int.Parse((string)param[1]);
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[2], true);
                Motion next = (Motion)Enum.Parse(typeof(Motion), (string)param[3], true);

                int x = int.Parse((string)param[4]);
                int y = int.Parse((string)param[5]);
                int z = int.Parse((string)param[6]);

                bool ret = Goto(chamber, slot, next, hand, x, y, z, out reason);


                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "Goto");
                    return true;
                }
                return false;
            });


            DEVICE.Register(String.Format("{0}.{1}", Name, "BladeEnable"), (out string reason, int time, object[] param) =>
            {    
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[0], true);
                bool bEnable = bool.Parse((string)param[1]);
                bool ret = Enable(hand, bEnable, out reason);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "Disable robot arm");
                    return true;
                }
                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "RobotBladeEnable"), (out string reason, int time, object[] param) =>
            {
                Hand hand = (Hand)Enum.Parse(typeof(Hand), (string)param[0], true);
                bool bEnable = bool.Parse((string)param[1]);
                bool ret = Enable(hand, bEnable, out reason);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, "Disable robot arm");
                    return true;
                }
                return false;
            });

         
            OP.Subscribe($"{Name}.Reconnect", (string cmd, object[] args) =>
            {
                return Connect();
            });

            OP.Subscribe($"{Name}.TestRobotPosition", (string cmd, object[] param) =>
            {
                //int setValue = 0;
                Hand _hand = Hand.Blade1;
                Axis axis;
                if (param == null || param.Length < 2) return false;
                int moveValue = (int)(double.Parse(param[1].ToString()));
                switch (param[0].ToString())
                {
                    case "TestRobotUp":
                        axis = Axis.Z;
                        break;
                    case "TestRobotDown":
                        moveValue = -moveValue;
                        axis = Axis.Z;
                        break;
                    case "TestRobotArmLeft":
                        moveValue = -moveValue;
                        axis = Axis.S;// Axis.H;//H1
                        break;
                    case "TestRobotArmRight":
                        axis = Axis.S;
                        break;
                    case "TestRobotArmUp":
                        axis =  Axis.A;// Axis.I;//H2
                        break;
                    case "TestRobotArmDown":
                        moveValue = -moveValue;
                        axis = Axis.A;
                        break;
                    case "TestRobotBlade1Left":
                        _hand = Hand.Blade1;
                        axis = Axis.H;
                        break;
                    case "TestRobotBlade1Right":
                        moveValue = -moveValue;
                        _hand = Hand.Blade1;
                        axis = Axis.H;//TH
                        break;
                    case "TestRobotBlade2Left":
                        _hand = Hand.Blade2;
                        axis = Axis.I;
                        break;
                    case "TestRobotBlade2Right":
                        moveValue = -moveValue;
                        _hand = Hand.Blade2;
                        axis = Axis.I;
                        break;
                    default:
                        axis = Axis.A;
                        break;
                }
                string reason;
                bool ret = PositionAdjustment(axis,_hand, moveValue, out reason);
                if (ret)
                {
                    reason = string.Format("{0}{1}", Name, reason);
                    return true;
                }
                return false;
            });

            OP.Subscribe($"{Name}.QueryPosition", (string cmd, object[] args) =>
            {
                string reason;
                bool ret = QueryPosition(out reason);
                if (ret)
                {
                    reason = string.Format("{0}.QueryPosition,{1}", Name, reason);
                    return true;
                }
                return false;
            });

            DATA.Subscribe($"{Name}.State", () => State);

            DATA.Subscribe($"{Name}.Busy", () => Busy);
            DATA.Subscribe($"{Name}.ErrorCode", () => ErrorCode);

            DATA.Subscribe($"{Name}.Blade1Target", () => Blade1Target);
            DATA.Subscribe($"{Name}.Blade2Target", () => Blade2Target);
            
            DATA.Subscribe($"{Name}.BladeAEnable", () => BladeAEnable);
            DATA.Subscribe($"{Name}.BladeBEnable", () => BladeBEnable);

            DATA.Subscribe($"{Name}.Swap", () => Swap);
            DATA.Subscribe($"{Name}.PlaceBalde", () => PlaceBalde);

            
            DATA.Subscribe($"{Name}.Speed", () => Speed);
            DATA.Subscribe($"{Name}.RobotSpeed", () => Speed.ToString());
            DATA.Subscribe($"{Name}.RobotState", () => State.ToString());

            DATA.Subscribe($"{Name}.Rotation", () => Rotation);
            DATA.Subscribe($"{Name}.Extension", () => Extension);
            DATA.Subscribe($"{Name}.Wrist1", () => Wrist1);
            DATA.Subscribe($"{Name}.Wrist2", () => Wrist2);
            DATA.Subscribe($"{Name}.Evevation", () => Evevation);

            DATA.Subscribe($"{Name}.CmdBladeTarget", () => CmdBladeTarget);
            DATA.Subscribe($"{Name}.CmdBlade1Extend", () => CmdBlade1Extend);
            DATA.Subscribe($"{Name}.CmdBlade2Extend", () => CmdBlade2Extend);

            DATA.Subscribe($"{Name}.RobotMoveInfo", () => MoveInfo);


            DATA.Subscribe($"{Name}.RobotBusy", () => Busy);
            DATA.Subscribe($"{Name}.RobotError", () => ErrorCode);
            DATA.Subscribe($"{Name}.RobotBlade1Traget", () => Blade1Target);
            DATA.Subscribe($"{Name}.RobotBlade2Traget", () => Blade2Target);
            DATA.Subscribe($"{Name}.RobotBladeAEnabled", () => BladeAEnable);
            DATA.Subscribe($"{Name}.RobotBladeBEnabled", () => BladeBEnable);
            DATA.Subscribe($"{Name}.RobotSwap", () => Swap);
            DATA.Subscribe($"{Name}.RobotSwapPlaceBlade", () => PlaceBalde);
 
            DATA.Subscribe($"{Name}.RobotPosRotationAxis", () => Rotation);
            DATA.Subscribe($"{Name}.RobotPosExtensionAxis", () => Extension);
            DATA.Subscribe($"{Name}.RobotPosWrist1Axis", () => Wrist1);
            DATA.Subscribe($"{Name}.RobotPosWrist2Axis", () => Wrist2);
            DATA.Subscribe($"{Name}.RobotPosEvevationAxis", () => Evevation);

            DATA.Subscribe($"{Name}.CenterOffsetR", () => CenterOffsetR);
            DATA.Subscribe($"{Name}.CenterOffsetT", () => CenterOffsetT);

            DATA.Subscribe($"{Name}.CommunicationStatus", () => IsConnected);

            //DATA.Subscribe($"{Name}.IsOnline", () => _isOnline);

            EV.Subscribe(new EventItem(60014,"Event", AlarmRobotError, "Robot error", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));

            Reset();
            string str = string.Empty;

            if(_factory != null)
                _eventHandler = _factory.Event();
            return true;
        }

        public virtual void Terminate()
        {
            if (SC.ContainsItem("Robot.CloseSocketOnShutDown") && SC.GetValue<bool>("Robot.CloseSocketOnShutDown"))
            {
                LOG.Write($"Robot {Name} termination process: close socket");
                if (_socket != null)
                    _socket.Dispose();
                else if (_port != null)
                    _port.Dispose();
            }
        }

        public virtual void Monitor()
        {
        }

        public virtual void Reset()
        {

            if (Error)
            {
                lock (_locker)
                {
                    _foregroundHandler = null;
                    _backgroundHandler = null;
                }
            }
             
            _exceuteErr = false;
            if (_commErr)
            {               
                Connect();
            }
            Swap = false;
        }

        public void SetOnline(bool online)
        {
            _isOnline = online;
        }

        public bool Enable(Hand hand, bool bEnable, out string reason)
        {
            reason = string.Empty;

            if (hand == Hand.Blade1)
            {
                BladeBEnable = bEnable;
            }
            else if (hand == Hand.Blade2)
            {
                BladeBEnable = bEnable;
            }
            else
            {
                reason = "Can't disable all blade";
                return false;
            }

            return true;
        }

        public virtual bool Connect()
        {
            _commErr = false;

            if (_socket != null)
                _socket.Connect(this._addr);
            else if (_port != null)
                _port.Open();

            return true;
        }


        #region Command

        public virtual bool Init(out string reason)
        {
            lock (_locker)
            {
                _foregroundHandler = null;
                _backgroundHandler = null;
            }
            reason = string.Empty;
            return execute(_factory.Init(), out reason);
        }


        public virtual bool Home(out string reason)
        {          
            lock (_locker)
            {
                _foregroundHandler = null;
                _backgroundHandler = null;
            }
            reason = string.Empty;
            return execute(_factory.Home(), out reason);
        }

        public virtual bool ArmHome(out string reason)
        {
            lock (_locker)
            {
                _foregroundHandler = null;
                _backgroundHandler = null;
            }
            reason = string.Empty;
            return execute(_factory.ArmHome(), out reason);
        }

        public bool AlignerHome(out string reason)
        {
            lock (_locker)
            {
                _foregroundHandler = null;
                _backgroundHandler = null;
            }
            reason = string.Empty;
            return execute(_factory.AlignerHome(), out reason);
        }
        public bool Aligner(out string reason)
        {
            lock (_locker)
            {
                _foregroundHandler = null;
                _backgroundHandler = null;
            }
            reason = string.Empty;
            return execute(_factory.Aligner(), out reason);
        }
        public bool RequestAlignerResult(out string reason)
        {
            lock (_locker)
            {
                _foregroundHandler = null;
                _backgroundHandler = null;
            }
            reason = string.Empty;
            return execute(_factory.RequestAlignerResult(), out reason);
        }
        public bool RequestAlignerOffset(Hand hand, out string reason)
        {
            lock (_locker)
            {
                _foregroundHandler = null;
                _backgroundHandler = null;
            }
            reason = string.Empty;
            return execute(_factory.RequestAlignerOffset(hand), out reason);
        }

        public virtual bool Grip(Hand hand,out string reason)
        {
            reason = string.Empty;
            return execute(_factory.Grip(hand), out reason);
        }

        public virtual bool Release(Hand hand, out string reason)
        {
            reason = string.Empty;
            return execute(_factory.Release(hand), out reason);
        }

        public virtual bool WaferMapping(ModuleName loadport, out string reason)
        {
            reason = string.Empty;
            return execute(_factory.WaferMapping(loadport), out reason);
        }


        public virtual bool QueryWaferMap(ModuleName loadport, out string reason)
        {
            reason = string.Empty;
            return execute(_factory.QueryWaferMap(loadport), out reason);
        }

        public virtual bool QueryState(out string reason)
        {  
            reason = string.Empty;
            return execute(_factory.QueryState(), out reason);
        }

        public virtual bool QueryPosition(out string reason)
        {
            reason = string.Empty;

            return execute(_factory.QueryPosition(), out reason);
        }

        public virtual bool Clear(out string reason)
        {
            reason = string.Empty;
            return execute(_factory.Clear(), out reason);
        }


        public virtual bool Stop(bool isEmergency, out string reason)
        {
            reason = string.Empty;

            lock (_locker)
            {
                _foregroundHandler = null;
                _backgroundHandler = null;
            }
            return execute(_factory.Stop(isEmergency), out reason);
        }
        public virtual bool Resume( out string reason)
        {
            reason = string.Empty;
 
            return execute(_factory.Resume(), out reason);
        }

        public virtual bool SetSpeed(int speed, out string reason)
        {
            reason = string.Empty;
            return execute(_factory.SetSpeed(speed), out reason);
        }

        public virtual bool SetCommunication( out string reason)
        {

            lock (_locker)
            {
                _foregroundHandler = null;
                _backgroundHandler = null;
            }

            reason = string.Empty;
            return execute(_factory.SetCommunication(), out reason);
        }


        public virtual bool SetLoad(Hand hand,bool isOn ,out string reason)
        {

            lock (_locker)
            {
                _foregroundHandler = null;
                _backgroundHandler = null;
            }

            reason = string.Empty;
            return execute(_factory.SetLoad(hand, isOn), out reason);
        }


        public virtual bool CheckLoad(ModuleName chamber, int slot, out string reason)
        {
            reason = string.Empty;
            return execute(_factory.CheckLoad(chamber, slot), out reason);
        }

        public virtual bool RequestWaferAWCData(out string reason)
        {
            reason = string.Empty;
            return execute(_factory.RequestAWCData(), out reason);
        }

        public virtual bool RequestWaferPresent( out string reason)
        {
            reason = string.Empty;
            return execute(_factory.RequestWaferPresent(), out reason);
        }

        public virtual bool RequestWaferPresentLoadA(out string reason)
        {
            reason = string.Empty;
            return execute(_factory.RequestWaferPresentLoadA(), out reason);
        }

        public virtual bool RequestWaferPresentLoadB(out string reason)
        {
            reason = string.Empty;
            return execute(_factory.RequestWaferPresentLoadB(), out reason);
        }

        public virtual bool Goto(ModuleName chamber, int slot, Motion motion,Hand hand, int x, int y, int z,out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("Pick invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (hand == Hand.Both)
                {
                    if (!checkslot(0, 25 - Blade2Slots, slot))   //0-20 | 20,21,22,23,24
                    {
                        reason = string.Format("Pick invalid parameter,double hand, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else if(hand == Hand.Blade2)
                {
                    if (!checkslot(0, 25 - Blade2Slots + 1, slot))   //0 - 21| 21,22,23,24
                    {
                        reason = string.Format("Pick invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else
                {                    
                    if (!checkslot(0, 25, slot))
                    {
                        reason = string.Format("Pick invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
            }
            else
            {
                if (hand == Hand.Both)
                {
                    reason = string.Format("Pick invalid parameter, double pick,chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }

            return execute(_factory.Goto( chamber, slot, motion, hand, x,y,z), out reason);
        }

        public virtual bool MoveTo(ModuleName chamber, int slot, Hand hand, bool isPick, int x, int y, int z,out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("Pick invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (hand == Hand.Both)
                {
                    if (!checkslot(0, 25 - Blade2Slots, slot))   //0-20 | 20,21,22,23,24
                    {
                        reason = string.Format("Pick invalid parameter,double hand, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else if(hand == Hand.Blade2)
                {
                    if (!checkslot(0, 25 - Blade2Slots + 1, slot))   //0 - 21| 21,22,23,24
                    {
                        reason = string.Format("Pick invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else
                {                    
                    if (!checkslot(0, 25, slot))
                    {
                        reason = string.Format("Pick invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
            }
            else
            {
                if (hand == Hand.Both)
                {
                    reason = string.Format("Pick invalid parameter, double pick,chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }

            return execute(_factory.MoveTo( chamber, slot, hand, isPick, x,y,z), out reason);
        }

        public virtual bool Extend(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("Extend invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (hand == Hand.Both)
                {
                    if (!checkslot(0, 25 - Blade2Slots, slot))   //0-20 | 20,21,22,23,24
                    {
                        reason = string.Format("Extend invalid parameter,double hand, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else if (hand == Hand.Blade2)
                {
                    if (!checkslot(0, 25 - Blade2Slots + 1, slot))   //0 - 21| 21,22,23,24
                    {
                        reason = string.Format("Extend invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else
                {
                    if (!checkslot(0, 25, slot))
                    {
                        reason = string.Format("Extend invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
            }
            else
            {
                if (hand == Hand.Both)
                {
                    reason = string.Format("Extend invalid parameter, double pick,chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }

            return execute(_factory.Extend(chamber, slot, hand), out reason);

        }

        public virtual bool Retract(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("Retract invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (hand == Hand.Both)
                {
                    if (!checkslot(0, 25 - Blade2Slots, slot))   //0-20 | 20,21,22,23,24
                    {
                        reason = string.Format("Retract invalid parameter,double hand, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else if (hand == Hand.Blade2)
                {
                    if (!checkslot(0, 25 - Blade2Slots + 1, slot))   //0 - 21| 21,22,23,24
                    {
                        reason = string.Format("Retract invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else
                {
                    if (!checkslot(0, 25, slot))
                    {
                        reason = string.Format("Retract invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
            }
            else
            {
                if (hand == Hand.Both)
                {
                    reason = string.Format("Retract invalid parameter, double pick,chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }

            return execute(_factory.Retract(chamber, slot, hand), out reason);

        }

        public virtual bool PickExtend(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            reason = string.Empty;
            if (chamber.ToString() == Name || chamber == ModuleName.System || ModuleHelper.IsLoadPort(chamber) || ModuleHelper.IsBuffer(chamber))
            {
                reason = $"Pick extend target not support, chamber is {chamber.ToString()}, slot is {slot}";
                return false;
            }

            if (hand == Hand.Both)
            {
                reason = string.Format("Pick extend invalid parameter, do not support double arm extend ,chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            return execute(_factory.PickExtend(chamber, slot, hand), out reason);
        }

        public virtual bool PickRetract(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            reason = string.Empty;
            if (chamber.ToString() == Name || chamber == ModuleName.System || ModuleHelper.IsLoadPort(chamber) || ModuleHelper.IsBuffer(chamber))
            {
                reason = $"Pick retract target not support, chamber is {chamber.ToString()}, slot is {slot}";
                return false;
            }

            if (hand == Hand.Both)
            {
                reason = string.Format("Pick retract invalid parameter, do not support double arm retract ,chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            return execute(_factory.PickRetract(chamber, slot, hand), out reason);
        }

        public virtual bool Pick(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("Pick invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (hand == Hand.Both)
                {
                    if (!checkslot(0, 25 - Blade2Slots, slot))   //0-20 | 20,21,22,23,24
                    {
                        reason = string.Format("Pick invalid parameter,double hand, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else if (hand == Hand.Blade2)
                {
                    if (!checkslot(0, 25 - Blade2Slots + 1, slot))   //0 - 21| 21,22,23,24
                    {
                        reason = string.Format("Pick invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else
                {
                    if (!checkslot(0, 25, slot))
                    {
                        reason = string.Format("Pick invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
            }
            else if(ModuleHelper.IsBuffer(chamber))
            {
                if (hand == Hand.Both)
                {
                    if (!checkslot(0, _bufferMaxSlot - Blade2Slots, slot))   //0-20 | 20,21,22,23,24
                    {
                        reason = string.Format("Pick invalid parameter,double hand, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else if (hand == Hand.Blade2)
                {
                    if (!checkslot(0, _bufferMaxSlot - Blade2Slots + 1, slot))   //0 - 21| 21,22,23,24
                    {
                        reason = string.Format("Pick invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else
                {
                    if (!checkslot(0, _bufferMaxSlot, slot))
                    {
                        reason = string.Format("Pick invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
            }
            else
            {
                if (hand == Hand.Both)
                {
                    reason = string.Format("Pick invalid parameter, double pick,chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }

             return execute(_factory.Pick(chamber, slot, hand), out reason);

        }

        public virtual bool PickAligner(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("Pick aligner invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (hand == Hand.Both)
                {
                    if (!checkslot(0, 25 - Blade2Slots, slot))   //0-20 | 20,21,22,23,24
                    {
                        reason = string.Format("Pick aligner invalid parameter,double hand, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else if (hand == Hand.Blade2)
                {
                    if (!checkslot(0, 25 - Blade2Slots + 1, slot))   //0 - 21| 21,22,23,24
                    {
                        reason = string.Format("Pick aligner invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else
                {
                    if (!checkslot(0, 25, slot))
                    {
                        reason = string.Format("Pick aligner invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
            }
            else if (ModuleHelper.IsBuffer(chamber))
            {
                if (hand == Hand.Both)
                {
                    if (!checkslot(0, _bufferMaxSlot - Blade2Slots, slot))   //0-20 | 20,21,22,23,24
                    {
                        reason = string.Format("Pick aligner invalid parameter,double hand, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else if (hand == Hand.Blade2)
                {
                    if (!checkslot(0, _bufferMaxSlot - Blade2Slots + 1, slot))   //0 - 21| 21,22,23,24
                    {
                        reason = string.Format("Pick aligner invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else
                {
                    if (!checkslot(0, _bufferMaxSlot, slot))
                    {
                        reason = string.Format("Pick aligner invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
            }
            else
            {
                if (hand == Hand.Both)
                {
                    reason = string.Format("Pick aligner invalid parameter, double pick,chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }

            return execute(_factory.PickAligner(chamber, slot, hand), out reason);

        }

        public virtual bool PickReadyPosition(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("Pick invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }
            
            return execute(_factory.PickReadyPosition(chamber, slot, hand), out reason);

        }
        
        public virtual bool Place(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("Place invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (hand == Hand.Both)
                {
                    if (!checkslot(0, 25 - Blade2Slots, slot))   //0-20 | 20,21,22,23,24
                    {
                        reason = string.Format("Place invalid parameter,double hand, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else if (hand == Hand.Blade2)
                {
                    if (!checkslot(0, 25 - Blade2Slots + 1, slot))   //0 - 21| 21,22,23,24
                    {
                        reason = string.Format("Pick invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else
                {
                    if (!checkslot(0, 25, slot))
                    {
                        reason = string.Format("Place invalid parameter,place, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
            }
            else if (ModuleHelper.IsBuffer(chamber))
            {
                if (hand == Hand.Both)
                {
                    if (!checkslot(0, _bufferMaxSlot - Blade2Slots, slot))   //0-6
                    {
                        reason = string.Format("Place invalid parameter,double hand, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else if (hand == Hand.Blade2)
                {
                    if (!checkslot(0, _bufferMaxSlot - Blade2Slots + 1, slot))   //0 - 6
                    {
                        reason = string.Format("Pick invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else
                {
                    if (!checkslot(0, _bufferMaxSlot, slot))
                    {
                        reason = string.Format("Place invalid parameter,place, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
            }
            else
            {
                if (hand == Hand.Both)
                {
                    reason = string.Format("Place invalid parameter, double place,chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }

            return execute(_factory.Place(chamber, slot, hand), out reason);
        }

        public virtual bool PlaceReadyPosition(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("Place invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }
            
            return execute(_factory.PlaceReadyPosition(chamber, slot, hand), out reason);

        }

        public virtual bool PlaceExtend(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            reason = string.Empty;
            if (chamber.ToString() == Name || chamber == ModuleName.System || ModuleHelper.IsLoadPort(chamber) || ModuleHelper.IsBuffer(chamber))
            {
                reason = $"Place extend target not support, chamber is {chamber.ToString()}, slot is {slot}";
                return false;
            }

            if (hand == Hand.Both)
            {
                reason =
                    $"Place extend invalid parameter, do not support double arm extend ,chamber is {chamber.ToString()}, slot is {slot}";
                return false;
            }

            return execute(_factory.PlaceExtend(chamber, slot, hand), out reason);
        }

        public virtual bool PlaceRetract(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            reason = string.Empty;
            if (chamber.ToString() == Name || chamber == ModuleName.System || ModuleHelper.IsLoadPort(chamber) ||ModuleHelper.IsBuffer(chamber) )
            {
                reason = $"Place retract target not support, chamber is {chamber.ToString()}, slot is {slot}";
                return false;
            }

            if (hand == Hand.Both)
            {
                reason =
                    $"Place retract invalid parameter, do not support double arm extend ,chamber is {chamber.ToString()}, slot is {slot}";
                return false;
            }

            return execute(_factory.PlaceRetract(chamber, slot, hand), out reason);
        }

        public virtual bool Exchange(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("Exchange invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (Blade2Slots > 1)
            {
                reason = string.Format("this robot don't support exchange operation.");
                return false;
            }

            if (hand == Hand.Both)
            {
                reason = string.Format("Exchange invalid parameter,double hand");
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (!checkslot(0, 25, slot))
                {
                    reason = string.Format("Exchange invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }
            return execute(_factory.Exchange(chamber, slot, hand), out reason);
        }

        public virtual bool ExchangeReadyPosition(ModuleName chamber, int slot, Hand pickHand, Hand placeHand, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("ExchangeReadyPosition invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (Blade2Slots > 1)
            {
                reason = string.Format("this robot don't support exchange operation.");
                return false;
            }

            if (pickHand == Hand.Both || placeHand == Hand.Both)
            {
                reason = string.Format("ExchangeReadyPosition invalid parameter,double hand");
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (!checkslot(0, 25, slot))
                {
                    reason = string.Format("ExchangeReadyPosition invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }
            return execute(_factory.ExchangeReadyPosition(chamber, slot, pickHand, placeHand), out reason);
        }

        public virtual bool ExchangeReadyPositionExtend(ModuleName chamber, int slot, Hand pickHand, Hand placeHand, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("ExchangeReadyPosition invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (Blade2Slots > 1)
            {
                reason = string.Format("this robot don't support exchange operation.");
                return false;
            }

            if (pickHand == Hand.Both || placeHand == Hand.Both)
            {
                reason = string.Format("ExchangeReadyPosition invalid parameter,double hand");
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (!checkslot(0, 25, slot))
                {
                    reason = string.Format("ExchangeReadyPosition invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }
            return execute(_factory.ExchangeReadyExtend(chamber, slot, pickHand, placeHand), out reason);
        }

        public virtual bool ExchangeAfterReadyPositionExtend(ModuleName chamber, int slot, Hand pickHand, Hand placeHand, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("ExchangeReadyPosition invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (Blade2Slots > 1)
            {
                reason = string.Format("this robot don't support exchange operation.");
                return false;
            }

            if (pickHand == Hand.Both || placeHand == Hand.Both)
            {
                reason = string.Format("ExchangeReadyPosition invalid parameter,double hand");
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (!checkslot(0, 25, slot))
                {
                    reason = string.Format("ExchangeReadyPosition invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }
            return execute(_factory.ExchangeAfterReadyExtend(chamber, slot, pickHand, placeHand), out reason);
        }

        public virtual bool ExchangePickExtend(ModuleName chamber, int slot, Hand pickHand, Hand placeHand, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("ExchangePickExtend invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (Blade2Slots > 1)
            {
                reason = string.Format("this robot don't support exchange operation.");
                return false;
            }

            if (pickHand == Hand.Both || placeHand == Hand.Both)
            {
                reason = string.Format("ExchangePickExtend invalid parameter,double hand");
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (!checkslot(0, 25, slot))
                {
                    reason = string.Format("ExchangePickExtend invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }
            return execute(_factory.ExchangePickExtend(chamber, slot, pickHand, placeHand), out reason);
        }

        public virtual bool ExchangePlaceExtend(ModuleName chamber, int slot, Hand pickHand, Hand placeHand, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("ExchangePlaceExtend invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (Blade2Slots > 1)
            {
                reason = string.Format("this robot don't support exchange operation.");
                return false;
            }

            if (pickHand == Hand.Both || placeHand == Hand.Both)
            {
                reason = string.Format("ExchangePlaceExtend invalid parameter,double hand");
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (!checkslot(0, 25, slot))
                {
                    reason = string.Format("ExchangePlaceExtend invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }
            return execute(_factory.ExchangePlaceExtend(chamber, slot, pickHand, placeHand), out reason);
        }

        public virtual bool ExchangePlaceRetract(ModuleName chamber, int slot, Hand pickHand, Hand placeHand, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("ExchangePlaceRetract invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (Blade2Slots > 1)
            {
                reason = string.Format("this robot don't support exchange operation.");
                return false;
            }

            if (pickHand == Hand.Both || placeHand == Hand.Both)
            {
                reason = string.Format("ExchangePlaceRetract invalid parameter,double hand");
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (!checkslot(0, 25, slot))
                {
                    reason = string.Format("ExchangePlaceRetract invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }
            return execute(_factory.ExchangePlaceRetract(chamber, slot, pickHand, placeHand), out reason);
        }
        public virtual bool SetWaferSize(int cmd, WaferSize size, out string reason)
        {
            reason = string.Empty;
            return execute(_factory.SetWaferSize(cmd, size), out reason);
        }

        public virtual bool QueryParameter(int parameter, string parameterType, out string reason)
        {
            reason = string.Empty;
            return execute(_factory.QueryParameter(parameter, parameterType), out reason);
        }
        public virtual bool SetServoOnOff(bool trueForOnFalseForOff, out string reason)
        {
            reason = string.Empty;
            return execute(_factory.SetServoOnOff(trueForOnFalseForOff), out reason);
        }
        public virtual bool PickEx(ModuleName chamber, int slot, Hand hand, int x, int y, int z, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("Pick invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (hand == Hand.Both)
                {
                    if (!checkslot(0, 25 - Blade2Slots, slot))   //0-20 | 20,21,22,23,24
                    {
                        reason = string.Format("Pick invalid parameter,double hand, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else if (hand == Hand.Blade2)
                {
                    if (!checkslot(0, 25 - Blade2Slots + 1, slot))   //0 - 21| 21,22,23,24
                    {
                        reason = string.Format("Pick invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else
                {
                    if (!checkslot(0, 25, slot))
                    {
                        reason = string.Format("Pick invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
            }
            else
            {
                if (hand == Hand.Both)
                {
                    reason = string.Format("Pick invalid parameter, double pick,chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }

            return execute(_factory.PickEx(chamber, slot, hand, x,y,z), out reason);

        }

        public virtual bool PlaceEx(ModuleName chamber, int slot, Hand hand, int x, int y, int z, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("Place invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (hand == Hand.Both)
                {
                    if (!checkslot(0, 25 - Blade2Slots, slot))   //0-20 | 20,21,22,23,24
                    {
                        reason = string.Format("Place invalid parameter,double hand, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else if (hand == Hand.Blade2)
                {
                    if (!checkslot(0, 25 - Blade2Slots + 1, slot))   //0 - 21| 21,22,23,24
                    {
                        reason = string.Format("Pick invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
                else
                {
                    if (!checkslot(0, 25, slot))
                    {
                        reason = string.Format("Place invalid parameter,place, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                        return false;
                    }
                }
            }
            else
            {
                if (hand == Hand.Both)
                {
                    reason = string.Format("Place invalid parameter, double place,chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }

            return execute(_factory.PlaceEx(chamber, slot, hand,x,y,z), out reason);
        }

        public virtual bool ExchangeEx(ModuleName chamber, int slot, Hand hand, int x, int y, int z, out string reason)
        {
            reason = string.Empty;
            if (chamber == ModuleName.Robot)
            {
                reason = string.Format("Exchange invalid parameter, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                return false;
            }

            if (Blade2Slots > 1)
            {
                reason = string.Format("this robot don't support exchange operation.");
                return false;
            }

            if (hand == Hand.Both)
            {
                reason = string.Format("Exchange invalid parameter,double hand");
                return false;
            }

            if (ModuleHelper.IsLoadPort(chamber))
            {
                if (!checkslot(0, 25, slot))
                {
                    reason = string.Format("Exchange invalid parameter,pick, chamber is {0}, slot is {1}", chamber.ToString(), slot);
                    return false;
                }
            }
            return execute(_factory.ExchangeEx(chamber, slot, hand, x, y, z), out reason);
        }
        #endregion

        public virtual bool PositionAdjustment( Axis axis, Hand hand, int value, out string reason)
        {
            reason = string.Empty;
            return execute(_factory.PositionAdjustment( axis,hand,value), out reason);
        }

        public virtual bool execute(IHandler handler, out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                if (_foregroundHandler != null)
                {
                    reason = "System busy, please wait or reset system.";
                    EV.PostMessage(Name, EventEnum.DefaultWarning, string.Format("{0} {1} {2}", this.DeviceID, handler.ToString(), reason));

                    return false;
                }

                if (_backgroundHandler != null && handler.IsBackground)
                {
                    reason = "System busy，one background command is running, please wait or reset system.";
                    EV.PostMessage(Name, EventEnum.DefaultWarning, string.Format("{0} {1} {2}", this.DeviceID, handler.ToString(), reason));
                    //reason = "系统忙，后台命令正在处理,暂时不能处理新的后台命令";
                    return false;
                }

                handler.Unit = (int)Unit.Robot;

                if (_socket != null)
                {
                    if (!handler.Execute(ref _socket))
                    {
                        reason = "Communication error,please check it.";
                        OnError(reason);
                        return false;
                    }
                }
                else if (_port != null)
                {
                    if (!handler.Execute(ref _port))
                    {
                        reason = "Communication error,please check it.";
                        OnError(reason);
                        return false;
                    }
                }

                if (handler.IsBackground)
                    _backgroundHandler = handler;
                else
                    _foregroundHandler = handler;
            }

            return true;
        }

        public virtual void OnDataChanged(string package)
        {
            try
            {
                if (!package.Contains("Gb") && !package.Contains("Pb"))
                    package = package.ToUpper();
                string[] msgs = Regex.Split(package, delimiter);

                foreach (string msg in msgs)
                {
                    if (msg.Length > 0)
                    {
                        bool completed = false;
                        string resp = msg;

                        lock (_locker)
                        {
                            if (_socket != null)
                            {
                                if (_foregroundHandler != null && _foregroundHandler.OnMessage(ref _socket, resp, out completed))
                                {
                                    _foregroundHandler = null;
                                }
                                else if (_backgroundHandler != null && _backgroundHandler.OnMessage(ref _socket, resp, out completed))
                                {
                                    if (completed)
                                    {
                                        //string reason = string.Empty;
                                        //QueryState(out reason);
                                        _backgroundHandler = null;

                                        if (ActionDone != null)
                                            ActionDone(true);
                                    }
                                }
                                else
                                {
                                    if (_eventHandler != null)
                                    {
                                        if (_eventHandler.OnMessage(ref _socket, resp, out completed))
                                        {
                                            if (completed)
                                            {
                                                EV.PostMessage("Robot", EventEnum.DefaultWarning, string.Format(" has error. {0:X}", ErrorCode));
                                                OnError(string.Format("has error. {0:X}", ErrorCode));
                                                _exceuteErr = true;
                                            }
                                        }
                                    }
                                }
                            }
                            else if (_port != null)
                            {
                                if (_foregroundHandler != null && _foregroundHandler.OnMessage(ref _port, resp, out completed))
                                {
                                    _foregroundHandler = null;
                                }
                                else if (_backgroundHandler != null && _backgroundHandler.OnMessage(ref _port, resp, out completed))
                                {
                                    if (completed)
                                    {
                                        //string reason = string.Empty;
                                        //QueryState(out reason);
                                        _backgroundHandler = null;

                                        if (ActionDone != null)
                                            ActionDone(true);
                                    }
                                }
                                else
                                {
                                    if (_eventHandler != null)
                                    {
                                        if (_eventHandler.OnMessage(ref _port, resp, out completed))
                                        {
                                            if (completed)
                                            {
                                                EV.PostMessage("Robot", EventEnum.DefaultWarning, string.Format(" has error. {0:X}", ErrorCode));
                                                OnError(string.Format("has error. {0:X}", ErrorCode));
                                                _exceuteErr = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (ExcuteFailedException e)
            {
                EV.PostMessage("Robot", EventEnum.DefaultWarning, string.Format("executed failed. {0}",e.Message));
                OnError(string.Format("executed failed. {0}", e.Message));
                _exceuteErr = false;
            }
            catch (InvalidPackageException e)
            {
                EV.PostMessage("Robot", EventEnum.DefaultWarning, string.Format("receive invalid package. {0}", e.Message));
                OnError(string.Format("receive invalid package. {0}", e.Message));
            }
            catch (System.Exception ex)
            {
                _commErr = true;
                LOG.Write("Robot failed：" + ex.ToString());
            }
        }

        private void OnErrorHandler(ErrorEventArgs args)
        {
            _commErr = true;
            Initalized = false;
            EV.PostMessage(Module, EventEnum.CommunicationError, Display, args.Reason);

            OnError("CommunicationError");
        }

        private void _port_OnErrorHappened(string obj)
        {
            LOG.Error(obj);
        }

        private bool checkslot(int min, int max, int slot)
        {
            return slot >= min && slot < max;
        }

        public void NotifySlotMapResult(ModuleName module, string slotMap)
        {
            if (OnSlotMapRead != null)
            {
                OnSlotMapRead(module, slotMap);
            }
        }
        public void OnError(string errorcode)
        {
            EV.Notify(AlarmRobotError,new SerializableDictionary<string, object> {
                {
                    "AlarmText",errorcode
                }
            });

            if (ActionDone != null)
                ActionDone(false);
        }

        public void NotifyWaferPresent(Hand hand, WaferStatus status)
        {
            if (status == WaferStatus.Empty)
            {
                if (hand == Hand.Blade1)
                {
                    WaferPresentOnBlade1 = status == WaferStatus.Empty;
                }

                if (hand == Hand.Blade2)
                {
                    WaferPresentOnBlade2 = status == WaferStatus.Empty;
                }
            }
            if (status == WaferStatus.Unknown)
            {
                EV.PostWarningLog(Module, "Wafer present unknown");
                return;
            }

            if (hand == Hand.Blade1)
            {
                WaferPresentOnBlade1 = status == WaferStatus.Normal;
            }

            if (hand == Hand.Blade2)
            {
                WaferPresentOnBlade2 = status == WaferStatus.Normal;
            }
        }

        //Active wafer Center finder Data
        public void NotifyAWCData(int rOffset, int tOffset)
        {
            CenterOffsetR = rOffset;
            CenterOffsetT = tOffset;

            EV.PostInfoLog(Module, $"AWC CenterOffsetR: {CenterOffsetR} , CenterOffsetT: {CenterOffsetT}");
        }

        //Notify Aligner Offset Data
        public void NotifyAlignerOffsetData(int rOffset, int tOffset)
        {
            AlignerOffsetR = rOffset;
            AlignerOffsetT = tOffset;
        }

        //Notify Aligner Result Data
        public void NotifyAlignerResultData(string a1, string a2, string a3, string a4)
        {
            AlignerA1 = a1;
            AlignerA2 = a2;
            AlignerA3 = a3;
            AlignerA4 = a4;
        }
    }
}

  