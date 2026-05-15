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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RFMatchs.AE
{
    public class AeRfMatch : RfMatchBase
    {
 
        public override EnumRfMatchTuneMode TuneMode1
        {
            get
            {
                return _statusData.Status2.Net1AutoMode ? EnumRfMatchTuneMode.Auto : EnumRfMatchTuneMode.Manual;
            }
        }

        public override EnumRfMatchTuneMode TuneMode2
        {
            get
            {
                return _statusData.Status2.Net2AutoMode ? EnumRfMatchTuneMode.Auto : EnumRfMatchTuneMode.Manual;
            }
        }
        public override float LoadPosition1
        {
            get { return _statusData.LoadPosi1; }
        }

        public override float LoadPosition2
        {
            get { return _statusData.LoadPosi2; }
        }

        public override float TunePosition1
        {
            get { return _statusData.TunePosi1; }
        }

        public override float TunePosition2
        {
            get { return _statusData.TunePosi2; }
        }
        public override float BiasPeak
        {
            get { return _statusData.BiasPeak; }
        }

        public override float DCBias
        {
            get { return _statusData.DCBias; }
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

                    TunePosition1 = TunePosition1,
                    TunePosition2 = TunePosition2,

                    TuneMode1 = TuneMode1,
                    TuneMode2 = TuneMode2,

                    BiasPeak = BiasPeak,
                    DCBias = DCBias,
                };

                return data;
            }
        }

        public AeRfMatchConnection Connection
        {
            get { return _connection; }
        }

        private AeRfMatchConnection _connection;

        private byte _deviceAddress ;
 
        private EnumRfMatchTuneMode _tuneMode1;
        private EnumRfMatchTuneMode _tuneMode2;

        private RD_TRIG _trigRfOnOff = new RD_TRIG();

        private int _presetNumber;

        private R_TRIG _trigError = new R_TRIG();
 
        private R_TRIG _trigWarningMessage = new R_TRIG();
 
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        private PeriodicJob _thread;

        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private object _locker = new object();

        private bool _enableLog = true;
 
        private AEStatusData _statusData;

        public AeRfMatch(string module, string name) : base(module, name)
        {

        }

        public override bool Initialize()
        {
            base.Initialize();
 
            string portName = SC.GetStringValue($"{ScBasePath}.{Name}.Address");
            int bautRate = SC.GetValue<int>($"{ScBasePath}.{Name}.BaudRate");
            int dataBits = SC.GetValue<int>($"{ScBasePath}.{Name}.DataBits");
            Enum.TryParse(SC.GetStringValue($"{ScBasePath}.{Name}.Parity"), out Parity parity);
            Enum.TryParse(SC.GetStringValue($"{ScBasePath}.{Name}.StopBits"), out StopBits stopBits);

            _deviceAddress = (byte)SC.GetValue<int>($"{ScBasePath}.{Name}.DeviceAddress");
            _enableLog = SC.GetValue<bool>($"{ScBasePath}.{Name}.EnableLogMessage");

            _connection = new AeRfMatchConnection(portName, bautRate, dataBits, parity, stopBits);
            _connection.EnableLog(_enableLog);

            if (_connection.Connect())
            {
                EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            }

            _thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);

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
                            _lstHandler.AddLast(new AeRfMatchQueryStatusHandler(this, _deviceAddress));
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

        internal void NotePresetNumber(int presetNumber)
        {
            _presetNumber = presetNumber;
        }


        internal void NoteStatus(AEStatusData data)
        {
            _statusData = data;
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
 


        public override void SetPreSetsAndTrajectories1(Presets presets)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfMatchSetPresetHandler(this, _deviceAddress, 1, presets));
            }
        }

        public override void SetPreSetsAndTrajectories2(Presets presets)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfMatchSetPresetHandler(this, _deviceAddress, 2, presets));
            }
        }

        public override void SetActivePresetNo1(int presetNumber)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfMatchSetActivePresetHandler(this, _deviceAddress, 1, presetNumber));
            }
        }

        public override void SetActivePresetNo2(int presetNumber)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfMatchSetActivePresetHandler(this, _deviceAddress, 2, presetNumber));
            }
        }


        public override void EnablePreset1(bool enable)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfMatchEnablePresetHandler(this, _deviceAddress, 1, enable));
            }
        }

        public override void EnablePreset2(bool enable)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfMatchEnablePresetHandler(this, _deviceAddress, 2, enable));
            }
        }

        public override void EnableCapacitorMove1(bool enable)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfMatchEnableCapMoveHandler(this, _deviceAddress, 1, enable));
            }
        }

        public override void EnableCapacitorMove2(bool enable)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfMatchEnableCapMoveHandler(this, _deviceAddress, 2, enable));
            }
        }

        public override void SetTuneMode1(EnumRfMatchTuneMode mode)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfMatchSetControlModeHandler(this, _deviceAddress, 1, mode));
            }
        }

        public override void SetTuneMode2(EnumRfMatchTuneMode mode)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfMatchSetControlModeHandler(this, _deviceAddress, 2, mode));
            }
        }

        public override void SetLoad1(float position)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfMatchSetLoadPositionHandler(this, _deviceAddress, 1, position));
            }
        }

        public override void SetLoad2(float position)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfMatchSetLoadPositionHandler(this, _deviceAddress, 2, position));
            }
        }


        public override void SetTune1(float position)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfMatchSetTunePositionHandler(this, _deviceAddress, 1, position));
            }
        }


        public override void SetTune2(float position)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new AeRfMatchSetTunePositionHandler(this, _deviceAddress, 2, position));
            }
        }
    }




}
