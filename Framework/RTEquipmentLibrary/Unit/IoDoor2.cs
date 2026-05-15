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
    public class IoDoor2 : BaseDevice, IDevice
    {
        public LidState State
        {
            get
            {
                if (_diOpen.Value && _diClose.Value)
                    return LidState.Error;
                if (_diOpen.Value && !_diClose.Value)
                    return LidState.Open;
                if (!_diOpen.Value && _diClose.Value)
                    return LidState.Close;
                return LidState.Unknown;
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
 
        private DOAccessor _doClose;
        private DOAccessor _doOpen;

        private DeviceState _state = DeviceState.Idle;
        private DeviceTimer _timer = new DeviceTimer();

        private object _lockerState = new object();

        private SCConfigItem _scTimeout;

        public bool IsOpen { get { return !_diClose.Value && _diOpen.Value; } }
        public bool IsClose { get { return _diClose.Value && !_diOpen.Value; } }

        public bool OpenFeedback
        {
            get { return _diOpen.Value; }
        }
        public bool CloseFeedback
        {
            get { return _diClose.Value; }
        }


        public AlarmEventItem AlarmMoveTimeout { get; set; }
        public AlarmEventItem AlarmSignalAbnormal { get; set; }

        public IoDoor2(string module, XmlElement node, string ioModule = "")
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

            _diOpen = ParseDiNode("diOpen", node, ioModule);
            _diClose = ParseDiNode("diClose", node, ioModule);
 
            _doClose = ParseDoNode("doClose", node, ioModule);
            _doOpen = ParseDoNode("doOpen", node, ioModule);

            _scTimeout = ParseScNode("scTimeout", node, ioModule, $"{scBasePath}.{Name}.MoveTimeout");
        }

        public bool Initialize()
        {
            _state = DeviceState.Idle;

            AlarmMoveTimeout = SubscribeAlarm($"{Module}.{Name}.MoveTimeout", "", null);
            AlarmSignalAbnormal = SubscribeAlarm($"{Module}.{Name}.SignalAbnormal", $"{Name} Open&Close signal trigger at same time", ResetCheckSignalAbnormal);

            DATA.Subscribe($"{Module}.{Name}.State", () => State.ToString());

            DATA.Subscribe($"{Module}.{Name}.OpenFeedback", ()=>_diOpen.Value);
 
            DATA.Subscribe($"{Module}.{Name}.CloseFeedback", () => _diClose.Value);
 
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
            lock (_lockerState)
            {
                switch (_state)
                {
                    case DeviceState.Opening:
                        if (IsOpen)
                        {
                            _state = DeviceState.Idle;
                        }
                        else if (_timer.IsTimeout())
                        {
                            if (!_doClose.SetValue(true, out string reason))
                            {
                                LOG.Error($"{Module} reset DO failed, {reason}");
                            }

                            AlarmMoveTimeout.Description = $"Can not open {Name} in {_scTimeout.IntValue} seconds";
                            AlarmMoveTimeout.Set();

                            _state = DeviceState.Error;
                        }
                        break;
                    case DeviceState.Closing:
                        if (IsClose)
                        {
                            _state = DeviceState.Idle;
                        }
                        else if (_timer.IsTimeout())
                        {
                            if (!_doClose.SetValue(false, out string reason))
                            {
                                LOG.Error($"{Module} reset DO failed, {reason}");
                            }

                            AlarmMoveTimeout.Description = $"Can not close {Name} in {_scTimeout.IntValue} seconds";
                            AlarmMoveTimeout.Set();

                            _state = DeviceState.Error;
                        }
                        break;
                    default:
                        break;
                }
            }

            if (_diOpen.Value && _diClose.Value)
            {
                AlarmSignalAbnormal.Set();
            }

        }

        public void Terminate()
        {
        }

        public bool SetDoor(bool open, out string reason)
        {
            if (open)
                return Open(out reason);

            return Close(out reason);
        }

        public bool Open(out string reason)
        {
            lock (_lockerState)
            {
                if (!_doClose.SetValue(false, out reason))
                {
                    return false;
                }

                if (!_doOpen.SetValue(true, out reason))
                {
                    return false;
                }

                _timer.Start(_scTimeout.IntValue * 1000);

                _state = DeviceState.Opening;
            }

            return true;
        }


        public bool Close(out string reason)
        {
            lock (_lockerState)
            {
                if (!_doClose.SetValue(true, out reason))
                {
                    _doClose.SetValue(false, out _);
                    return false;
                }

                if (!_doOpen.SetValue(false, out reason))
                {
                    _doClose.SetValue(false, out _);
                    _doOpen.SetValue(false, out _);
                    return false;
                }

                _timer.Start(_scTimeout.IntValue * 1000);

                _state = DeviceState.Closing;
            }


            return true;
        }

        public bool ResetCheckSignalAbnormal()
        {
            return !(_diOpen.Value && _diClose.Value);
        }

        public void Reset()
        {
           AlarmMoveTimeout.Reset();
           AlarmSignalAbnormal.Reset();
        }
    }
}
