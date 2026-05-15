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
using System.Threading;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.HonghuAligners
{
    public class HonghuAligner : BaseDevice, IDevice
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
            get;set;

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


        public HonghuAlignerConnection Connection
        {
            get => _connection;
        }
        private HonghuAlignerConnection _connection;

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
        public HonghuAligner(string module, string name)
            : base()
        {
            Name = name;
            Module = module;

            WaferManager.Instance.SubscribeLocation(name, 1);
        }
        

        public bool Initialize()
        {
            string portName = SC.GetStringValue($"{Module}.{Name}.Address");
            int bautRate = SC.GetValue<int>($"{Module}.{Name}.BaudRate");
            int dataBits = SC.GetValue<int>($"{Module}.{Name}.DataBits");
            Enum.TryParse(SC.GetStringValue($"{Module}.{Name}.Parity"), out Parity parity);
            Enum.TryParse(SC.GetStringValue($"{Module}.{Name}.StopBits"), out StopBits stopBits);
            //_deviceAddress = SC.GetValue<int>($"{Name}.DeviceAddress");
            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");


            _tazmotype = (AlignerType)(SC.ContainsItem($"{Name}.AlignerType") ? SC.GetValue<int>($"{Name}.AlignerType") : 0);
            _defaultChuckPosition = SC.ContainsItem("Aligner.DefaultChuckPosition") ?
                SC.GetValue<int>("Aligner.DefaultChuckPosition") : 1;

            _connection = new HonghuAlignerConnection(portName, bautRate, dataBits, parity, stopBits);
            _connection.EnableLog(_enableLog);

            int count = SC.ContainsItem("System.ComPortRetryCount") ? SC.GetValue<int>("System.ComPortRetryCount") : 5;
            int sleep = SC.ContainsItem("System.ComPortRetryDelayTime") ? SC.GetValue<int>("System.ComPortRetryDelayTime") : 2;
            if (sleep <= 0 || sleep > 10)
                sleep = 2;

            int retry = 0;
            do
            {
                _connection.Disconnect();
                Thread.Sleep(sleep * 1000);
                if (_connection.Connect())
                {
                    EV.PostInfoLog(Module, $"{Module}.{Name} connected");
                    break;
                }

                if (count > 0 && retry++ > count)
                {
                    LOG.Write($"Retry connect {Module}.{Name} stop retry.");
                    EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
                    break;
                }

                Thread.Sleep(sleep * 1000);
                LOG.Write($"Retry connect {Module}.{Name} for the {retry + 1} time.");

            } while (true);


           
            
        

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
            _connection.Disconnect();
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

            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");

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
        public virtual bool Reset(out string reason)
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                _lstHandler.AddLast(new RequestHandler(this, HonghuAlignerCommand.Reset));

            }
            reason = "";
            return true;
        }
        public virtual bool Init(out string reason)
        {

            lock (_locker)
            {
                _lstHandler.Clear();
                _lstHandler.AddLast(new RequestHandler(this, HonghuAlignerCommand.Reset));               

            }
            reason = "";
            return true;
        }
        

        public virtual bool Home(out string reason)
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                _lstHandler.AddLast(new RequestHandler(this, HonghuAlignerCommand.Reset));

            }
            reason = "";
            return true;
        }


        

        public virtual bool Clear(out string reason)
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                _lstHandler.AddLast(new RequestHandler(this, HonghuAlignerCommand.Reset));

            }
            reason = "";
            return true;
        }

        public virtual bool Grip(out string reason)
        {

            lock (_locker)
            {
                _lstHandler.AddLast(new RequestHandler(this, HonghuAlignerCommand.RequestVacuumOn));
                

            }
            reason = "";
            return true;
        }

        public virtual bool Release(out string reason)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RequestHandler(this, HonghuAlignerCommand.RequestVacuumOff));


            }
            reason = "";
            return true;           
        }


        public virtual bool LiftUp(out string reason)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RequestHandler(this, HonghuAlignerCommand.RequestPlace));


            }
            reason = "";
            return true;
        }

        public virtual bool LiftDown(out string reason)
        {
            lock (_locker)
            {


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
                _lstHandler.AddLast(new RequestHandler(this, HonghuAlignerCommand.SetNotReadLM));
                _lstHandler.AddLast(new RequestHandler(this, $"B{angle.ToString()}"));
                _lstHandler.AddLast(new RequestHandler(this, HonghuAlignerCommand.SetVacuumOnAfterAln));
                _lstHandler.AddLast(new RequestHandler(this, HonghuAlignerCommand.RequestFinishPlace));
            }

            reason = "";
            return true;
        }
        public virtual bool RequestPlace(out string reason)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new RequestHandler(this, HonghuAlignerCommand.RequestPlace));

            }

            reason = "";
            return true;
        }


        public virtual bool QueryStatus(out string reason)
        {
            lock (_locker)
            {
               

            }
            reason = "";
            return true;
        }

        #endregion



    }
}

 
