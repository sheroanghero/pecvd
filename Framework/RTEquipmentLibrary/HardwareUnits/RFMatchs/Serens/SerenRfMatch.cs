using System;
using System.Collections.Generic;
using System.IO.Ports;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.Serens
{
    public class SerenRfMatch : RfMatchBase
    {
 
        public override EnumRfMatchTuneMode TuneMode1
        {
            get;
            set;
        }

        public override EnumRfMatchTuneMode TuneMode2
        {
            get;
            set;
        }
        public EnumRfMatchTuneMode LoadMode1
        {
            get;
            set;
        }

        public EnumRfMatchTuneMode LoadMode2
        {
            get;
            set;
        }

        public override float LoadPosition1
        {
            get;
            set;
        }

        public override float LoadPosition2
        {
            get;
            set;
        }

        public override float TunePosition1
        {
            get;
            set;
        }

 
 
        public override AITRfMatchData DeviceData
        {
            get
            {
                AITRfMatchData data = new AITRfMatchData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
 
                    LoadPosition1 = LoadPosition1,
                    LoadPosition2 = LoadPosition2,
                    LoadPosition1SetPoint = LoadPosition1Setpoint,

                    TunePosition1 = TunePosition1,
                    TunePosition2 = TunePosition2,
                    TunePosition1SetPoint = TunePosition1Setpoint,

                    TuneMode1 = TuneMode1,
                    TuneMode2 = TuneMode2,

                    BiasPeak = BiasPeak,
                    DCBias = DCBias,
                };

                return data;
            }
        }

        public SerenRfMatchConnection Connection
        {
            get { return _connection; }
        }

        private SerenRfMatchConnection _connection;

 
        private EnumRfMatchTuneMode _tuneMode1;
        private EnumRfMatchTuneMode _tuneMode2;

        private RD_TRIG _trigRfOnOff = new RD_TRIG();
 
        private R_TRIG _trigError = new R_TRIG();
 
        private R_TRIG _trigWarningMessage = new R_TRIG();
 
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private object _locker = new object();

        private bool _enableLog = true;
 
        public int MagnitudeError { get; set; }

        public int PhaseError { get; set; }

        public int DCVoltage { get; set; }

        public int RFVoltage { get; set; }

        public string MatchType { get; set; } //ATS or MC2

        private string _scRoot;

        public SerenRfMatch(string module, string name, string scRoot, string type="ATS") : base(module, name)
        {
            MatchType = type;
            _scRoot = scRoot;
        }

        public bool Initialize(string portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
        {
            base.Initialize();
            _enableLog = true;
            _connection = new SerenRfMatchConnection(portName, baudRate, dataBits, parity, stopBits);
            _connection.EnableLog(_enableLog);
            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }
            _thread = new PeriodicJob(1000, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            return true;
        }

        public override bool Initialize()
        {
            base.Initialize();
 
            string portName = SC.GetStringValue($"{_scRoot}.Address");
            int bautRate = SC.GetValue<int>($"{_scRoot}.BaudRate");
            int dataBits = SC.GetValue<int>($"{_scRoot}.DataBits");
            Enum.TryParse(SC.GetStringValue($"{_scRoot}.Parity"), out Parity parity);
            Enum.TryParse(SC.GetStringValue($"{_scRoot}.StopBits"), out StopBits stopBits);
                                               
             _enableLog = SC.GetValue<bool>($"{_scRoot}.EnableLogMessage");

            _connection = new SerenRfMatchConnection(portName, bautRate, dataBits, parity, stopBits);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(300, OnTimer, $"{Module}.{Name} MonitorHandler", true);

            OP.Subscribe($"{Module}.{Name}.MatchMode1", (out string reason, int time, object[] args) =>
            {
                reason = "";

                if (!Enum.TryParse((string)args[0], out EnumRfMatchTuneMode mode))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not mode, {args[0]} is not a valid mode value");
                    return false;
                }

                if (!PerformMatchMode1(out reason, time, mode))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set match mode1 to {mode}");
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetMatchLoad1", (out string reason, int time, object[] args) =>
            {
                reason = "";
                float value = Convert.ToSingle((string)args[0]);

                if (!PerformSetMatchLoad1(out reason, time, value))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set match load1 to {value}");
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetMatchTune1", (out string reason, int time, object[] args) =>
            {
                reason = "";
                float value = Convert.ToSingle((string)args[0]);

                if (!PerformSetMatchTune1(out reason, time, value))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set match tune1 to {value}");
                return true;
            });

            OP.Subscribe($"{Module}.{Name}.MatchMode2", (out string reason, int time, object[] args) =>
            {
                reason = "";

                if (!Enum.TryParse((string)args[0], out EnumRfMatchTuneMode mode))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not mode, {args[0]} is not a valid mode value");
                    return false;
                }

                if (!PerformMatchMode2(out reason, time, mode))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set match mode2 to {mode}");
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetMatchLoad2", (out string reason, int time, object[] args) =>
            {
                reason = "";
                float value = Convert.ToSingle((string)args[0]);

                if (!PerformSetMatchLoad2(out reason, time, value))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set match load2 to {value}");
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.SetMatchTune2", (out string reason, int time, object[] args) =>
            {
                reason = "";
                float value = Convert.ToSingle((string)args[0]);

                if (!PerformSetMatchTune2(out reason, time, value))
                {
                    EV.PostAlarmLog(Module, $"{Module}.{Name} Can not set mode, {reason}");
                    return false;
                }

                EV.PostInfoLog(Module, $"{Module}.{Name} set match tune2 to {value}");
                return true;
            });
            return true;
        }

        private bool PerformSetMatchTune1(out string reason, int time, float value)
        {
            reason = string.Empty;
            return true;
        }

        private bool PerformSetMatchLoad1(out string reason, int time, float value)
        {
            reason = string.Empty;
            return true;
        }

        private bool PerformMatchMode1(out string reason, int time, EnumRfMatchTuneMode mode)
        {
            reason = string.Empty;
            return true;
        }

        private bool PerformMatchMode2(out string reason, int time, EnumRfMatchTuneMode mode)
        {
            reason = string.Empty;
            return true;
        }

        private bool PerformSetMatchLoad2(out string reason, int time, float value)
        {
            reason = string.Empty;
            return true;
        }

        private bool PerformSetMatchTune2(out string reason, int time, float value)
        {
            reason = string.Empty;
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
                        _connection.SetPortAddress(SC.GetStringValue($"{ScBasePath}.{Name}.Address"));
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
                            //_lstHandler.AddLast(new SerenRfMatchGetLoadControlModeHandler(this));
                            //_lstHandler.AddLast(new SerenRfMatchGetTuneControlModeHandler(this));
                            //_lstHandler.AddLast(new SerenRfMatchGetLoadPositionHandler(this));
                            //_lstHandler.AddLast(new SerenRfMatchGetTunePositionHandler(this));
                            //_lstHandler.AddLast(new SerenRfMatchGetMagnitudeErrorHandler(this));
                            //_lstHandler.AddLast(new SerenRfMatchGetPhaseErrorHandler(this));
                            //_lstHandler.AddLast(new SerenRfMatchGetDCVoltageHandler(this));
                            //_lstHandler.AddLast(new SerenRfMatchGetRFVoltageHandler(this));
                            _lstHandler.AddLast(new SerenRfMatchGetTunePositionHandler(this));
                            _lstHandler.AddLast(new SerenRfMatchGetLoadPositionHandler(this));
                        }

                        if (_lstHandler.Count > 0)
                        {
                            handler = _lstHandler.First.Value;

                            _lstHandler.RemoveFirst();
                        }
                    }

                    if (handler != null)
                    {
                        _connection.Execute(handler);
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        public override void Monitor()
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

 
 
        public override void Reset()
        {
            _trigError.RST = true;
             _trigWarningMessage.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

            _trigRetryConnect.RST = true;

            base.Reset();
        }
 
        internal void NoteError(string reason)
        {
            _trigWarningMessage.CLK = true;
            if (_trigWarningMessage.Q)
            {
                EV.PostWarningLog(Module, $"{Module}.{Name} error, {reason}");
            }
        }

        internal void NoteLoadControlMode(bool isAuto)
        {
            LoadMode1 = isAuto ? EnumRfMatchTuneMode.Auto : EnumRfMatchTuneMode.Manual;
        }

        internal void NoteTuneControlMode(bool isAuto)
        {
            TuneMode1 = isAuto ? EnumRfMatchTuneMode.Auto : EnumRfMatchTuneMode.Manual;
        }
        internal void NoteLoadPosition(float position)
        {
            LoadPosition1 = position;
        }
        internal void NoteTunePosition(float position)
        {
            TunePosition1 = position;
        }
        internal void NoteMagnitudeError(int error)
        {
            MagnitudeError = error;
        }
        internal void NotePhaseError(int error)
        {
            PhaseError = error;
        }

        internal void NoteDCVoltage(int voltage)
        {
            DCVoltage = voltage;
        }

        internal void NoteRFVoltage(int voltage)
        {
            RFVoltage = voltage;
        }

        public override void SetLoadMode1(EnumRfMatchTuneMode mode)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SerenRfMatchSetLoadControlModeHandler(this, mode==EnumRfMatchTuneMode.Auto));
            }
        }

        public override void SetTuneMode1(EnumRfMatchTuneMode mode)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SerenRfMatchSetTuneControlModeHandler(this, mode == EnumRfMatchTuneMode.Auto));
                _lstHandler.AddLast(new SerenRfMatchGetTuneControlModeHandler(this ));
            }
        }


        public override void SetLoad1(float position)
        {
            LoadPosition1Setpoint = position;
            lock (_locker)
            {
                if (MatchType == "ATS")
                {
                    _lstHandler.AddLast(new SerenRfMatchSetLoadControlModeHandler(this, false));

                    _lstHandler.AddLast(new SerenRfMatchSetLoadPositionHandler(this, position));

                }
                else
                {
                    _lstHandler.AddLast(new SerenRfMatchSetLoadControlModeHandler(this, false));

                    _lstHandler.AddLast(new SerenRfMatchSetLoadPreset1PositionHandler(this, position));

                    _lstHandler.AddLast(new SerenRfMatchGotoHandler(this));
                }
            }
        }

 

        public override void SetTune1(float position)
        {
            TunePosition1Setpoint = position;
            lock (_locker)
            {
                _lstHandler.AddLast(new SerenRfMatchSetTuneControlModeHandler(this, false));

                _lstHandler.AddLast(new SerenRfMatchSetTunePositionHandler(this,  position));

                _lstHandler.AddLast(new SerenRfMatchGotoHandler(this));
            }
        }

        public void GetTune1()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SerenRfMatchGetTunePositionHandler(this));
            }
        }

        public void GetLoad1()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SerenRfMatchGetLoadPositionHandler(this));
            }
        }

        public void GetLoad1Mode()
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new SerenRfMatchGetLoadControlModeHandler(this));
            }
        }

    }




}
