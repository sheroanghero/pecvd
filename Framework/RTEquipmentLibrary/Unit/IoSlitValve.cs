using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device.Unit
{
    public enum SlitValveStateEnum
    {
        Unknown,
        Up,
        Down,
        Error,
    }

    public class IoSlitValve : BaseDevice, IDevice
    {
        public SlitValveStateEnum State
        {
            get
            {
                if (_diOpen.Value && _diClose.Value)
                    return SlitValveStateEnum.Error;
                if (_diOpen.Value && !_diClose.Value)
                    return SlitValveStateEnum.Up;
                if (!_diOpen.Value && _diClose.Value)
                    return SlitValveStateEnum.Down;
                return SlitValveStateEnum.Unknown;
            }
        }
        enum DeviceState
        {
            Idle,
            Opening,
            Closing,
            Error,
        }

        private DIAccessor _diOpen;
        private DIAccessor _diClose;

        private DIAccessor _diOpenEnable;
        private DIAccessor _diCloseEnable;

        private DOAccessor _doOpen;
        private DOAccessor _doClose;

        private DeviceState _state = DeviceState.Idle;
        private DeviceTimer _timer = new DeviceTimer();

        private SCConfigItem _scTimeout;

        public bool OpenFeedback => _diOpen.Value;
        public bool CloseFeedback => _diClose.Value;

        public bool OpenSetPoint
        {
            get { return _doOpen.Value; }
            set { _doOpen.Value = value; }
        }
        public bool CloseSetPoint
        {
            get { return _doClose.Value; }
            set { _doClose.Value = value; }
        }

        public bool IsOpen { get { return !_diClose.Value && _diOpen.Value; } }
        public bool IsClose { get { return _diClose.Value && !_diOpen.Value; } }

        public bool EnableOpenInterlock { get { return _diOpenEnable != null ? _diOpenEnable.Value : false; } }
        public bool EnableCloseInterlock { get { return _diCloseEnable != null ? _diCloseEnable.Value : false; } }

        //在开&关完成之后，是否保持输出，默认不保持
        private bool _keepSignalOut = false;
        private DeviceTimer _mutexSignalTimer = new DeviceTimer();
        private R_TRIG _mutexSignalTrigger = new R_TRIG();

        public IoSlitValve(string module, XmlElement node, string ioModule = "")
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _keepSignalOut = string.IsNullOrEmpty(node.GetAttribute("keepSignalOut")) ? false : Convert.ToBoolean(node.GetAttribute("keepSignalOut"));
            _diOpen = ParseDiNode("diOpen", node, ioModule);
            _diClose = ParseDiNode("diClose", node, ioModule);

            _diOpenEnable = ParseDiNode("diOpenEnable", node, ioModule);
            _diCloseEnable = ParseDiNode("diCloseEnable", node, ioModule);

            _doOpen = ParseDoNode("doOpen", node, ioModule);
            _doClose = ParseDoNode("doClose", node, ioModule);

            _scTimeout = ParseScNode("scTimeout", node, ioModule);
        }

        public bool Initialize()
        {
            _state = DeviceState.Idle;

            DATA.Subscribe($"{Module}.{Name}.OpenFeedback", () => _diOpen.Value);
            DATA.Subscribe($"{Module}.{Name}.OpenEnable", () => _diOpenEnable != null ? _diOpenEnable.Value : false);
            DATA.Subscribe($"{Module}.{Name}.OpenSetpoint", () => _doOpen.Value);

            DATA.Subscribe($"{Module}.{Name}.CloseEnable", () => _diCloseEnable != null ? _diCloseEnable.Value : false);
            DATA.Subscribe($"{Module}.{Name}.CloseFeedback", () => _diClose.Value);
            DATA.Subscribe($"{Module}.{Name}.CloseSetpoint", () => _doClose.Value);

            DATA.Subscribe($"{Module}.{Name}.State", () => State.ToString());

            OP.Subscribe($"{Module}.{Name}.Open", (out string reason, int time, object[] param) =>
            {
                reason = "";
                return Open(out reason);
            });

            OP.Subscribe($"{Module}.{Name}.Close", (out string reason, int time, object[] param) =>
            {
                reason = "";
                return Close(out reason);
            });

            return true;
        }


        public void Monitor()
        {
            if (_diOpen != null && _diClose != null)
            {
                if (State != SlitValveStateEnum.Error)
                {
                    _mutexSignalTimer.Start(2000);
                }

                _mutexSignalTrigger.CLK = _mutexSignalTimer.IsTimeout();

                if (_mutexSignalTrigger.Q)
                {
                    EV.PostWarningLog(Module, $"Valve {Name} was abnormal，Reason：diOpen's value is {_diOpen.Value} and diClose's value is {_diClose.Value} too.");
                }
            }

            switch (_state)
            {
                case DeviceState.Opening:
                    if (IsOpen)
                    {
                        if (!_keepSignalOut)
                        {
                            if (!_doOpen.SetValue(false, out string reason))
                            {
                                LOG.Error($"{Module} reset DO failed, {reason}");
                            }
                        }

                        _state = DeviceState.Idle;
                    }
                    else if (_timer.IsTimeout())
                    {
                        if (!_doOpen.SetValue(false, out string reason))
                        {
                            LOG.Error($"{Module} reset DO failed, {reason}");
                        }

                        if (_keepSignalOut)
                        {
                            _doClose.SetValue(true, out _);
                        }

                        EV.PostAlarmLog(Module, $"{Module} {Name} Can not open in {_scTimeout.IntValue} seconds");

                        _state = DeviceState.Error;
                    }
                    break;
                case DeviceState.Closing:
                    if (IsClose)
                    {
                        if (!_keepSignalOut)
                        {
                            if (!_doClose.SetValue(false, out string reason))
                            {
                                LOG.Error($"{Module} reset DO failed, {reason}");
                            }
                        }

                        _state = DeviceState.Idle;
                    }
                    else if (_timer.IsTimeout())
                    {
                        if (!_doClose.SetValue(false, out string reason))
                        {
                            LOG.Error($"{Module} reset DO failed, {reason}");
                        }

                        if (_keepSignalOut)
                        {
                            _doOpen.SetValue(true, out _);
                        }

                        EV.PostAlarmLog(Module, $"{Module} {Name} Can not close in {_scTimeout.IntValue} seconds");

                        _state = DeviceState.Error;
                    }
                    break;
                default:
                    break;
            }
        }

        public void Terminate()
        {
            if (!_keepSignalOut)
            {
                _doOpen.SetValue(false, out _);
                _doClose.SetValue(false, out _);
            }
        }

        public bool CheckInterlockEnable(bool open)
        {
            return open ? (_diOpenEnable == null || _diOpenEnable.Value) : (_diCloseEnable == null || _diCloseEnable.Value);
        }

        public bool SetSlitValve(bool open, out string reason)
        {
            if (open)
                return Open(out reason);

            return Close(out reason);

        }

        public bool Open(out string reason)
        {
            if (_diOpenEnable != null && !_diOpenEnable.Value)
            {
                reason = "interlock blocked open";
                return false;
            }

            if (!_doClose.SetValue(false, out reason) || !_doOpen.SetValue(true, out reason))
            {
                _doOpen.SetValue(false, out _);

                if (_keepSignalOut)
                {
                    _doClose.SetValue(true, out _);
                }
                else
                {
                    _doClose.SetValue(false, out _);
                }
                return false;
            }

            _timer.Start(_scTimeout.IntValue * 1000);

            _state = DeviceState.Opening;
            return true;
        }


        public bool Close(out string reason)
        {
            if (_diCloseEnable != null && !_diCloseEnable.Value)
            {
                reason = "interlock blocked close";
                return false;
            }

            if (!_doClose.SetValue(true, out reason) || !_doOpen.SetValue(false, out reason))
            {
                _doClose.SetValue(false, out _);

                if (_keepSignalOut)
                {
                    _doOpen.SetValue(true, out _);
                }
                else
                {
                    _doOpen.SetValue(false, out _);
                }
                return false;
            }

            _timer.Start(_scTimeout.IntValue * 1000);

            _state = DeviceState.Closing;

            return true;
        }


        public void Reset()
        {
        }
    }
}

