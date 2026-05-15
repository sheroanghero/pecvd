using System;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.Util;

using Aitex.Sorter.Common;
using Aitex.Core.RT.Device;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.SubstrateTrackings;
using System.Collections.Generic;
using Aitex.Core.RT.SCCore;
using System.IO.Ports;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TazmoAligners
{
    public enum TazmoState1
    {
        StandbyNoWafer = 0x100,
        StandbyWithWafer,
        OperationEndNormaly,      // Normal
        Operating,
        Calibrationing = 0x106,
        Pause,
        Interlock,
        Initialization,
        StandbyDueToUninitialized = 0x110,

        WaferLostDuringAlignment,       
        AlignmentOperationTimeOver,
        CannotDeterminTheWaferSize = 0x114,
        WaferNotSetToAligner = 0x115,
        OverTheDetectinRangeOfTheLineSensor = 0x117,
        SamplingOperationNotProcessed,
        CannotExecuteTheAlignment,
        CannotPerformTheInitialization,
        ReceivingErrorOfDIO,
        StepOutError,
        CommandExecuteError,
        InputValueOfLineSensorAbnormal,
        CannotPerformAlignerInitializationStoppedByInterlock,
    }
    public enum TazmoStatus
    {
        PossibleToOperate =0,
        NeedInit,
        Abnormal = 9,
    }

    public enum LiftStatus
    {
        Up =0,
        Down,
        Busy,
        Unknown =9,
    }
    public enum NotchDetectionStatus
    {
        NotComplete=0,
        Complete =1,
        Error =9,
    }

    


    public class TazmoAligner : BaseDevice, IDevice
    {
        public enum AlignerType
        {
            Mechnical =0,
            Vaccum,
        }
        public string Address
        {
            get
            {
                return "";

            }
        }

        public virtual bool IsConnected
        {
            get { return true; }
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

        public int Notch { get; set; }

        public bool Initalized { get; set; }


        private AlignerType _tazmotype;
        public AlignerType TazmoType { get => _tazmotype; }







        public bool Communication
        {
            get
            {
                return !_commErr;
            }
        }        

        public virtual bool Error
        {
            get
            {
                return (int)TaAlignerStatus1 >= 0x111 || _commErr;
            }
        }

        public bool Busy { get { return _connection.IsBusy || _lstHandler.Count != 0; } }


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

        public TazmoState1 TaAlignerStatus1
        {
            get;set;
        }

        public TazmoStatus TaAlignerStatus2Status
        {
            get;set;
        }

        public LiftStatus TaAlignerStatus2Lift
        {
            get;set;
        }

        public NotchDetectionStatus TaAlignerStatus2Notch
        {
            get;set;
        }
        public int TaAlignerStatus2DeviceStatus
        { get; set; }

        public int TaAlignerStatus2ErrorCode
        {
            get;set;
        }

        public int TaAlignerStatus2LastErrorCode
        {
            get;set;
        }

        public bool TaExecuteSuccss
        {
            get;set;
        }      



        public virtual bool WaferOnAligner
        {
            get
            {
                return true;
            }
        }

        //private int _deviceAddress;


        public TazmoAlignerConnection Connection
        {
            get => _connection;
        }
        private TazmoAlignerConnection _connection;

        //private int _presetNumber;

        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigWarningMessage = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;
        private static Object _locker = new Object();

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();

        private bool _enableLog = true;

        private bool _commErr = false;

        private int _defaultChuckPosition;
        //private bool _exceuteErr = false;
        //private string _addr;


        private DeviceTimer _timerQuery = new DeviceTimer();
        

        
        private string AlarmMechanicalAlignmentError = "MechanicalAlignmentError";
        public TazmoAligner(string module, string name)
            : base()
        {
            Name = name;

            WaferManager.Instance.SubscribeLocation(name, 1);
        }

        public bool Initialize()
        {
            string portName = SC.GetStringValue($"{Name}.Address");
            int bautRate = SC.GetValue<int>($"{Name}.BaudRate");
            int dataBits = SC.GetValue<int>($"{Name}.DataBits");
            Enum.TryParse(SC.GetStringValue($"{Name}.Parity"), out Parity parity);
            Enum.TryParse(SC.GetStringValue($"{Name}.StopBits"), out StopBits stopBits);
            //_deviceAddress = SC.GetValue<int>($"{Name}.DeviceAddress");
            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");


            _tazmotype = (AlignerType)(SC.ContainsItem($"{Name}.AlignerType") ? SC.GetValue<int>($"{Name}.AlignerType") : 0);
            _defaultChuckPosition = SC.ContainsItem("Aligner.DefaultChuckPosition") ?
                SC.GetValue<int>("Aligner.DefaultChuckPosition") : 1;

            _connection = new TazmoAlignerConnection(portName, bautRate, dataBits, parity, stopBits);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }
            
        

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);

           

            DEVICE.Register(String.Format("{0}.{1}", Name, "Init"), (out string reason, int time, object[] param) =>
            {
                bool ret = Init(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Reset Error");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "AlignerInit"), (out string reason, int time, object[] param) =>
            {
                bool ret = Init(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Reset Error");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "Home"), (out string reason, int time, object[] param) =>
            {
                bool ret = Home(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Reset Error");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "AlignerHome"), (out string reason, int time, object[] param) =>
            {
                bool ret = Home(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Reset Error");
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
                bool ret = Grip(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Hold Wafer");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "AlignerGrip"), (out string reason, int time, object[] param) =>
            {
                bool ret = Grip(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Hold Wafer");
                    return true;
                }

                return false;
            });

            DEVICE.Register(String.Format("{0}.{1}", Name, "Release"), (out string reason, int time, object[] param) =>
            {
                bool ret = Release(out reason);
                if (ret)
                {
                    reason = string.Format("{0} {1}", Name, "Release Wafer");
                    return true;
                }

                return false;
            });
            DEVICE.Register(String.Format("{0}.{1}", Name, "AlignerRelease"), (out string reason, int time, object[] param) =>
            {
                bool ret = Release(out reason);
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
            EV.Subscribe(new EventItem(0, "Event", AlarmMechanicalAlignmentError, "Aligner error", EventLevel.Alarm, Aitex.Core.RT.Event.EventType.HostNotification));

            //            _timerQuery.Start(_queryPeriod);
            
            return true;
        }

        private bool OnTimer()
        { 
            try
            {
                _connection.MonitorTimeout();

                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        _connection.SetPortAddress(SC.GetStringValue($"{Name}.Address"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }

                    }
                    return true;
                }

                HandlerBase handler = null;
                if (!_connection.IsBusy)
                {
                    lock (_locker)
                    {
                        if (_lstHandler.Count == 0)
                        {
                            //_lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null)); 
                            //_lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null)); 
                        }

                        if (_lstHandler.Count > 0)
                        {
                            handler = _lstHandler.First.Value;
                            if (handler != null) _connection.Execute(handler);
                            _lstHandler.RemoveFirst();
                        }
                    }

                    
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        public void Terminate()
        {
            
        }

        public void Monitor()
        {
            try
            {
                _connection.EnableLog(_enableLog);

                _trigCommunicationError.CLK = _connection.IsCommunicationError;
                if (_trigCommunicationError.Q)
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public void Reset()
        {
            _trigError.RST = true;
            _trigWarningMessage.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;
            
        }

        
        internal void NoteError(string reason)
        {
         _trigWarningMessage.CLK = true;
   
            if (_trigWarningMessage.Q)
   
            {
       
                EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");
   
            }


         }


        #region Command
        public virtual bool ResetCPU(out string reason)
        {
            lock(_locker)
            {
                _lstHandler.Clear();
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.CPUresetMotion,null));


            }
            reason = "";
            return true;
        }
        public virtual bool Init(out string reason)
        {

            lock (_locker)
            {
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.InitializeMotion, null));
                if (_tazmotype == AlignerType.Vaccum)
                    _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MoveTheAlignerChuckToSpecifiedPosition, _defaultChuckPosition.ToString("0")));

                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if(_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
            reason = "";
            return true;
        }
        public virtual void Pause()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.PauseMotion, null));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical) 
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
        }

        public virtual void CancelPause()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.CancelthepauseMotion, null));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical) 
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
        }

        public virtual bool Home(out string reason)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MovealignertohomepositionMotion, null));
                if (_tazmotype == AlignerType.Mechnical)
                {
                    string para1 = $"A," + "0000";
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.SetalignmentangleetcSet, para1));
                    _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MovetopickpositionMotion, "A"));
                }

                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical) 
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
            reason = "";
            return true;
        }
        public virtual bool HomeForSwap(out string reason)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MovealignertohomepositionMotion, null));
                //if (_tazmotype == AlignerType.Mechnical)
                //{
                //    string para1 = $"A," + "0000";
                //    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.SetalignmentangleetcSet, para1));
                //    _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MovetopickpositionMotion, "A"));
                //}

                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
            reason = "";
            return true;
        }


        public virtual void MoveChuck(int position)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MoveTheAlignerChuckToSpecifiedPosition, position.ToString("0")));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
        }

        public virtual bool Clear(out string reason)
        {
            lock (_locker)
            {
                
                _lstHandler.Clear();
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.CancelerrorSet, null));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical) 
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));
            }
            reason = "";
            return true;
        }

        public virtual bool Grip(out string reason)
        {

            lock (_locker)
            {
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.ClosealignerchuckMotion, null));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical) 
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
            reason = "";
            return true;
        }

        public virtual bool Release(out string reason)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.OpenalignerchuckMotion, null));

                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical) 
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }

            reason = "";
            return true;
        }


        public virtual bool LiftUp(out string reason)
        {
            lock (_locker)
            {
                
                 string para1 = $"A," +"0000";
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.SetalignmentangleetcSet, para1));


                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MovetopickpositionMotion, "A"));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical) 
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
            reason = "";
            return true;
        }

        public virtual bool LiftDown(out string reason)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MovealignertohomepositionMotion, ""));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical) 
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }

            reason = "";
            return true;
        }
        public virtual bool MoveToReady(out string reason)
        {
            reason = "";
            return true;
        }


        public virtual bool Stop(out string reason)
        {
            reason = string.Empty;

            reason = "";
            return true;
        }

        public virtual bool Align(double angle, out string reason)
        {
            lock (_locker)
            {
                int anglevalue = (int)angle * 10;
                string para1 = $"1," + anglevalue.ToString("0000") + ",000";
                if (_tazmotype == AlignerType.Mechnical)
                {
                    anglevalue = (int)angle * 100;
                    para1 = $"1," + anglevalue.ToString("00000");
                }
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.SetalignmentangleetcSet, para1));
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.SeriesofalignmentMotion, "1,5"));
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical) 
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }

            reason = "";
            return true;
        }


        public virtual bool QueryStatus(out string reason)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical) 
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
            reason = "";
            return true;
        }

        #endregion



    }
}

 
