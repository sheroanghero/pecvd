using System;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device.Unit
{
    public enum LiftStateEnum
    {
        Unknown,
        Up,
        Down,
        Error,
    }

    public class IoLift : BaseDevice, IDevice
    {
        public LiftStateEnum State
        {
            get
            {
                if (_diUp.Value && _diDown.Value)
                    return LiftStateEnum.Error;
                if (_diUp.Value && !_diDown.Value)
                    return LiftStateEnum.Up;
                if (!_diUp.Value && _diDown.Value)
                    return LiftStateEnum.Down;
                return LiftStateEnum.Unknown;
            }
        }
        enum DeviceState
        {
            Idle,
            MovingUp,
            MovingDown,
            Error,
        }

        private DIAccessor _diUp;
        private DIAccessor _diDown;

        private DIAccessor _diMotionEnable;

        private DOAccessor _doUp;
        private DOAccessor _doDown;

        private DeviceState _state = DeviceState.Idle;
        private DeviceTimer _timer = new DeviceTimer();

        private SCConfigItem _scTimeout;

        public bool IsUp { get { return !_diDown.Value && _diUp.Value; } }
        public bool IsDown { get { return _diDown.Value && !_diUp.Value; } }

 
        public IoLift(string module, XmlElement node, string ioModule = "")
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _diUp = ParseDiNode("diUp", node, ioModule);
            _diDown = ParseDiNode("diDown", node, ioModule);
            _diMotionEnable = ParseDiNode("diMotionEnable", node, ioModule);

            _doUp = ParseDoNode("doUp", node, ioModule);
            _doDown = ParseDoNode("doDown", node, ioModule);

            _scTimeout = ParseScNode("scTimeout", node);
        }

        public bool Initialize()
        {
            _state = DeviceState.Idle;

            DATA.Subscribe($"{Module}.{Name}.UpFeedback", ()=>_diUp.Value);
            DATA.Subscribe($"{Module}.{Name}.DownFeedback", () => _diDown.Value);

            DATA.Subscribe($"{Module}.{Name}.UpEnable", () => _diMotionEnable != null ? _diMotionEnable.Value: false);
            DATA.Subscribe($"{Module}.{Name}.DownEnable", () => _diMotionEnable != null ? _diMotionEnable.Value : false);

            DATA.Subscribe($"{Module}.{Name}.LiftState", () => State.ToString());

            DEVICE.Register($"{Module}.{Name}.MoveUp", (out string reason, int time, object[] param) =>
            {
                reason = "";
                return MoveUp(out reason);
            });

            DEVICE.Register($"{Module}.{Name}.MoveDown", (out string reason, int time, object[] param) =>
            {
                reason = "";
                return MoveDown(out reason);
            });

            return true;
        }


        public void Monitor()
        {
            switch (_state)
            {
                case DeviceState.MovingUp:
                    if (IsUp)
                    {
                        //if (!_doUp.SetValue(false, out string reason))
                        //{
                        //    LOG.Error($"{Module} reset DO failed, {reason}");
                        //}

                        _state = DeviceState.Idle;
                    }
                    else if (_timer.IsTimeout())
                    {
                        if (!_doUp.SetValue(false, out string reason))
                        {
                            LOG.Error($"{Module} reset DO failed, {reason}");
                        }

                        EV.PostAlarmLog(Module, $"{Module} {Name} Can not move up in {_scTimeout.IntValue} seconds");

                        _state = DeviceState.Error;
                    }
                    break;
                case DeviceState.MovingDown:
                    if (IsDown)
                    {
                        //if (!_doDown.SetValue(false, out string reason))
                        //{
                        //    LOG.Error($"{Module} reset DO failed, {reason}");
                        //}

                        _state = DeviceState.Idle;
                    }
                    else if (_timer.IsTimeout())
                    {
                        if (!_doDown.SetValue(false, out string reason))
                        {
                            LOG.Error($"{Module} reset DO failed, {reason}");
                        }

                        EV.PostAlarmLog(Module, $"{Module} {Name} Can not move down in {_scTimeout.IntValue} seconds");

                        _state = DeviceState.Error;
                    }
                    break;
                default:
                    break;
            }
        }

        public void Terminate()
        {
            _doDown.SetValue(false, out _);
            _doUp.SetValue(false, out _);
        }

        public bool Move(bool up, out string reason)
        {
            if (up)
                return MoveUp(out reason);

            return MoveDown(out reason);

        }

        public bool MoveUp(out string reason)
        {
            if (_diMotionEnable != null && !_diMotionEnable.Value)
            {
                reason = "Motion enable interlock triggered";
                return false;
            }

            if (!_doDown.SetValue(false, out reason) || !_doUp.SetValue(true, out reason))
            {
                _doDown.SetValue(false, out _);
                _doUp.SetValue(false, out _);
                return false;
            }

            _timer.Start(_scTimeout.IntValue * 1000);

            _state = DeviceState.MovingUp;
            return true;
        }


        public bool MoveDown(out string reason)
        {
            if (_diMotionEnable != null && !_diMotionEnable.Value)
            {
                reason = "Motion enable interlock triggered";
                return false;
            }

            if (!_doDown.SetValue(true, out reason) || !_doUp.SetValue(false, out reason))
            {
                _doDown.SetValue(false, out _);
                _doUp.SetValue(false, out _);
                return false;
            }

            _timer.Start(_scTimeout.IntValue * 1000);

            _state = DeviceState.MovingDown;

            return true;
        }


        public void Reset()
        {
        }
    }
}
