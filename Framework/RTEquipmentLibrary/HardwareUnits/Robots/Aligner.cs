using System;
using System.Text.RegularExpressions;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.Util;

using Aitex.Sorter.Common;
using TSC = Aitex.Sorter.Common;
using Aitex.Core.RT.Device;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.NX100.AL;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.SR100.AL;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot
{
  
    public class Aligner : BaseDevice, IDevice,IConnection
    {
        public string Address
        {
            get { return _addr; }
        }

        public virtual bool IsConnected
        {
            get { return _socket.IsConnected; }
        }

        public virtual bool Disconnect()
        {
            return true;
        }
        public virtual bool Connect()
        {
            return true;
        }
        public const string delimiter = "\r";

        public int LastErrorCode { get; set; }
        public int Status { get; set; }
        public int ErrorCode { get; set; }


        public int ElapseTime { get; set; }

        public int Notch { get;  set; }

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
                return ErrorCode >= Convert.ToInt32("2EB0",16)|| _commErr;
            }
        }

        //public string ErrorMessage
        //{
        //    get
        //    {
        //        return _factory.GetError(LastErrorCode);
        //    }
        //}

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

        public virtual bool WaferOnAligner
        {   
            get
            {
                return (Status & ((int)StateBit.WaferOnGrip | (int)StateBit.WaferOnCCD)) > 0; 
            } 
        }

        public bool StateWaferHold { get; set; }

        private static Object _locker = new Object();
        private AsyncSocket _socket;


        private IHandler _eventHandler = null;
        private IHandler _backgroundHandler = null;  //moving
        private IHandler _foregroundHandler = null;  //current handler

        private IAlignerHandlerFactory _factory = null;

        private bool _commErr = false;
        //private bool _exceuteErr = false;
        private string _addr;


        private DeviceTimer _timerQuery = new DeviceTimer();
        //private int _queryPeriod = 5000;    //ms
        //private bool _queryState = true;

        private RobotType _type = RobotType.SR100;

        private string AlarmAlignerMotionError = "AlignerMotionError";
        public Aligner(string module, string name, string display, string deviceId, string address, RobotType type = RobotType.SR100)
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
            _commErr = false;

            WaferManager.Instance.SubscribeLocation(name, 1);
        }

        public virtual bool Initialize()
        {
            switch (_type)
            {
                case RobotType.NX100:
                    _factory = new NX100AlignerHandlerFactory(this);
                    break;
                default:
                    _factory = new SR100AlignerHandlerFactory(this);
                    break;
            }
            _eventHandler = _factory.Event();

            ConnectionManager.Instance.Subscribe(Name, this);

            if(!string.IsNullOrEmpty(_addr))
                _socket.Connect(this._addr);

            DEVICE.Register(String.Format("{0}.{1}", Name, "Init"), (out string reason, int time, object[] param) =>
            {
                bool ret = Init(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Init");
                    return true;
                }

                return false;
            });
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

            DEVICE.Register(String.Format("{0}.{1}", Name, "Home"), (out string reason, int time, object[] param) =>
            {
                bool ret = Home(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Home");
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
            DEVICE.Register(String.Format("{0}.{1}", Name, "Reset"), (out string reason, int time, object[] param) =>
            {
                bool ret = Clear(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Reset Error");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "AlignerReset"), (out string reason, int time, object[] param) =>
            {
                bool ret = Clear(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Reset Error");
                    return true;
                }

                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "Grip"), (out string reason, int time, object[] param) =>
            {
                bool ret = Grip(0,out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Hold Wafer");
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

            DEVICE.Register(String.Format("{0}.{1}", Name, "Release"), (out string reason, int time, object[] param) =>
            {
                bool ret = Release(0, out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Release Wafer");
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



            DEVICE.Register(String.Format("{0}.{1}", Name, "LiftUp"), (out string reason, int time, object[] param) =>
            {
                bool ret = LiftUp(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Lifter Up");
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
            DEVICE.Register(String.Format("{0}.{1}", Name, "LiftDown"), (out string reason, int time, object[] param) =>
            {
                bool ret = LiftDown(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Lifter Down");
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


            DEVICE.Register(String.Format("{0}.{1}", Name, "Stop"), (out string reason, int time, object[] param) =>
            {
                bool ret = Stop(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Stop Align");
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


            DEVICE.Register(String.Format("{0}.{1}", Name, "Align"), (out string reason, int time, object[] param) =>
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

            DATA.Subscribe($"{Name}.State", () => State);
            DATA.Subscribe($"{Name}.AlignerState", () => State.ToString());

            DATA.Subscribe($"{Name}.Busy", () => Busy);

            DATA.Subscribe($"{Name}.ErrorCode", () => ErrorCode);

            DATA.Subscribe($"{Name}.Error", () => Error);

            DATA.Subscribe($"{Name}.ElapseTime", () => ElapseTime);

            DATA.Subscribe($"{Name}.Notch", () => Notch);

            DATA.Subscribe($"{Name}.WaferOnAligner", () => WaferOnAligner);

//            string str = string.Empty;
            EV.Subscribe(new EventItem(0,"Event", AlarmAlignerMotionError, "Aligner error", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));

//            _timerQuery.Start(_queryPeriod);

            return true;
        }

        public virtual void Terminate()
        {
            _socket.Dispose();
        }

        public virtual void Monitor()
        {         

        }

        public virtual void Reset()
        {
            lock (_locker)
            {
                _foregroundHandler = null;
                _backgroundHandler = null;
            }
            
            //_exceuteErr = false;
            if (_commErr)
            {
                _commErr = false;
                _socket.Connect(this._addr);
            }
        }


        #region Command
        public virtual bool Init(out string reason)
        {
            reason = string.Empty;
            return execute(_factory.Init(), out reason);
        }
        public virtual bool Home(out string reason)
        {
            reason = string.Empty;
            return execute(_factory.Home(), out reason);
        }

        public virtual bool Clear(out string reason)
        {
            reason = string.Empty;
            return execute(_factory.Clear(), out reason);
        }

        public virtual bool Grip(Hand hand, out string reason)
        {
            reason = string.Empty;
            return execute(_factory.Grip(hand), out reason);
        }

        public virtual bool Release(Hand hand, out string reason)
        {
            reason = string.Empty;
            return execute(_factory.Release(hand), out reason);
        }


        public virtual bool LiftUp(out string reason)
        {
            reason = string.Empty;
            return execute(_factory.LiftUp(), out reason);
        }

        public virtual bool LiftDown(out string reason)
        {
            reason = string.Empty;
            return execute(_factory.LiftDown(), out reason);
        }
        public virtual bool MoveToReady(out string reason)
        {
            reason = string.Empty;
            return execute(_factory.MoveToReadyPostion(), out reason);
        }


        public virtual bool Stop(out string reason)
        {
            reason = string.Empty;

            lock (_locker)
            {
                _foregroundHandler = null;
                _backgroundHandler = null;
            }
            return execute(_factory.Stop(), out reason);
        }

        public virtual bool Align(double angle, out string reason)
        {
            reason = string.Empty;
            return execute(_factory.Align(angle), out reason);
        }


        public virtual bool QueryState(out string reason)
        {
            reason = string.Empty;
            return execute(_factory.QueryState(), out reason);
        }

        #endregion


        private bool execute(IHandler handler, out string reason)
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
                    return false;
                }
                handler.Unit = (int)Unit.Aligner;
                if(!handler.Execute(ref _socket))
                {
                    reason = "Communication error,please check it.";
                    return false;
                }

                if (handler.IsBackground)
                    _backgroundHandler = handler;
                else
                    _foregroundHandler = handler;
            }
            return true;
        }
        
        private void OnDataChanged(string package)
        {
            try
            {
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
                            if (_foregroundHandler != null && _foregroundHandler.OnMessage(ref _socket, resp, out completed))
                            {
                                 _foregroundHandler = null;
                            }
                            else if (_backgroundHandler != null && _backgroundHandler.OnMessage(ref _socket, resp, out completed))
                            {
                                if (completed)
                                {
                                    string reason = string.Empty;
                                    QueryState(out reason);
                                    _backgroundHandler = null;
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
                                            EV.PostMessage("Aligner", EventEnum.DefaultWarning, string.Format(" has error. {0:X}", ErrorCode));
                                            OnError(string.Format(" has error. {0:X}", ErrorCode));//_exceuteErr = true;
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
                EV.PostMessage("Aligner", EventEnum.DefaultWarning, string.Format("executed failed. {0}", e.Message));
                OnError(string.Format("executed failed. {0}", e.Message));
            }
            catch (InvalidPackageException e)
            {
                EV.PostMessage("Aligner", EventEnum.DefaultWarning, string.Format("receive invalid package. {0}", e.Message));
                OnError(string.Format("receive invalid package. {0}", e.Message));
            }
            catch (System.Exception ex)
            {
                _commErr = true;
                LOG.Write("Aligner failed：" + ex.ToString());
            }
        }

        private void OnErrorHandler(ErrorEventArgs args)
        {
            Initalized = false;
            _commErr = true;

            EV.PostMessage(Module, EventEnum.CommunicationError, Display, args.Reason);

            OnError("Communciation Error");

            //LOG.Error(string.Format("{0} Communication failed, {1}", Name, args.Reason));
        }

        //private bool checkslot(int min, int max, int slot)
        //{
        //    return slot >= min && slot < max;
        //}

        private void OnError(string errortext)
        {
            EV.Notify(AlarmAlignerMotionError,new SerializableDictionary<string, object> {
                {"AlarmText",errortext }
            });
        }
    }
}

  