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
using MECF.Framework.Common.Event;

namespace Aitex.Core.RT.Device.Unit
{
    /// <summary>
    /// 支持Alarm机制
    ///
    /// DI open 
    /// DI close
    /// 
    /// DO open
    /// DO close
    /// 
    /// </summary>
    /// 
    public class IoLift2 : BaseDevice, IDevice
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
 
        private DOAccessor _doUp;
        private DOAccessor _doDown;

        private DeviceState _state = DeviceState.Idle;
        private DeviceTimer _timer = new DeviceTimer();

        private SCConfigItem _scTimeout;

        public bool IsUp { get { return !_diDown.Value && _diUp.Value; } }
        public bool IsDown { get { return _diDown.Value && !_diUp.Value; } }

        public AlarmEventItem AlarmMoveTimeout { get; set; }
        public AlarmEventItem AlarmSignalAbnormal { get; set; }

        private object _lockerState = new object();

        public IoLift2(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;

            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            string scBasePath = node.GetAttribute("scBasePath");
            if (string.IsNullOrEmpty(scBasePath))
                scBasePath = $"{Module}.{Name}";
            else
            {
                scBasePath = scBasePath.Replace("{module}", Module);
            }

            _diUp = ParseDiNode("diUp", node, ioModule);
            _diDown = ParseDiNode("diDown", node, ioModule);
 
            _doUp = ParseDoNode("doUp", node, ioModule);
            _doDown = ParseDoNode("doDown", node, ioModule);

            _scTimeout = ParseScNode("scTimeout", node, ioModule, $"{scBasePath}.{Name}.MoveTimeout");
        }

        public bool Initialize()
        {
            _state = DeviceState.Idle;

            AlarmMoveTimeout = SubscribeAlarm($"{Module}.{Name}.MoveTimeout", "", null);
            AlarmSignalAbnormal = SubscribeAlarm($"{Module}.{Name}.SignalAbnormal", $"{Name} Up&Down signal trigger at same time", ResetCheckSignalAbnormal);

            DATA.Subscribe($"{Module}.{Name}.State", () => State.ToString());

            DATA.Subscribe($"{Module}.{Name}.UpFeedback", ()=> _diUp.Value && !_diDown.Value);
            DATA.Subscribe($"{Module}.{Name}.DownFeedback", () => _diDown.Value && !_diUp.Value);
 
            OP.Subscribe($"{Module}.{Name}.MoveUp", (out string reason, int time, object[] param) =>
            {
                reason = "";
                return MoveUp(out reason);
            });

            OP.Subscribe($"{Module}.{Name}.MoveDown", (out string reason, int time, object[] param) =>
            {
                reason = "";
                return MoveDown(out reason);
            });

            return true;
        }


        public void Monitor()
        {
            lock (_lockerState)
            {
                switch (_state)
                {
                    case DeviceState.MovingUp:
                        if (IsUp)
                        {
                            _state = DeviceState.Idle;
                        }
                        else if (_timer.IsTimeout())
                        {
                            if (!_doUp.SetValue(false, out string reason))
                            {
                                LOG.Error($"{Module} reset DO failed, {reason}");
                            }

                            AlarmMoveTimeout.Description = $"Can not move {Name} up in {_scTimeout.IntValue} seconds";
                            AlarmMoveTimeout.Set();

                            _state = DeviceState.Error;
                        }

                        break;
                    case DeviceState.MovingDown:
                        if (IsDown)
                        {
                            _state = DeviceState.Idle;
                        }
                        else if (_timer.IsTimeout())
                        {
                            if (!_doUp.SetValue(true, out string reason))
                            {
                                LOG.Error($"{Module} reset DO failed, {reason}");
                            }

                            AlarmMoveTimeout.Description = $"Can not move {Name} down in {_scTimeout.IntValue} seconds";
                            AlarmMoveTimeout.Set();

                            _state = DeviceState.Error;
                        }

                        break;
                    default:
                        break;
                }

            }

            if (_diUp.Value && _diDown.Value)
            {
                AlarmSignalAbnormal.Set();
            }
        }

        public void Terminate()
        {

        }

        public bool Move(bool up, out string reason)
        {
            if (up)
                return MoveUp(out reason);

            return MoveDown(out reason);

        }

        public bool MoveUp(out string reason)
        {
            lock (_lockerState)
            {
                if (!_doDown.SetValue(false, out reason))
                {
                    return false;
                }

                if (!_doUp.SetValue(true, out reason))
                {
                    return false;
                }

                _timer.Start(_scTimeout.IntValue * 1000);

                _state = DeviceState.MovingUp;
            }

            return true;
        }


        public bool MoveDown(out string reason)
        {
            lock (_lockerState)
            {
                if (!_doUp.SetValue(false, out reason))
                {
                    return false;
                }

                if (!_doDown.SetValue(true, out reason))
                {
                    return false;
                }

                _timer.Start(_scTimeout.IntValue * 1000);

                _state = DeviceState.MovingDown;
            }

            return true;
        }


        public bool ResetCheckSignalAbnormal()
        {
            return !(_diUp.Value && _diDown.Value);
        }

        public void Reset()
        {
            AlarmMoveTimeout.Reset();
            AlarmSignalAbnormal.Reset();
        }
    }
}
