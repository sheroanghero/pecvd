using System;
using System.Xml;
using Aitex.Core.Common;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoCoolBuffer : BaseDevice, IDevice
    {
        enum DeviceState
        {
            Idle,
            Homing,
            MovingUp,
            MovingDown,

            Error,
        }

        public enum LiftState
        {
            Unknown = 0,
            Down = 1,
            Up = 2,
            Error = 3,
        }

        private DIAccessor _diLiftUp;
        private DIAccessor _diLiftDown;
        private DIAccessor _di3InchExtend;
        private DIAccessor _di3InchRetract;

        private DIAccessor _di4InchExtend;
        private DIAccessor _di4InchRetract;

        private DOAccessor _doLiftUp;
        private DOAccessor _doLiftDown;
        private DOAccessor _do3InchExtend;
        private DOAccessor _do3InchRetract;

        private DOAccessor _do4InchExtend;
        private DOAccessor _do4InchRetract;


        private DeviceState _state = DeviceState.Idle;
        private DeviceTimer _timer = new DeviceTimer();

        private WaferSize _size = WaferSize.WS0;

        private SCConfigItem _scLiftUpTimeout;
        private SCConfigItem _scLiftDownTimeout;

        public bool Busy
        {
            get
            {
                return _state != DeviceState.Idle;
            }
        }

        public bool Moving
        {
            get
            {
                return _state != DeviceState.Idle && _state != DeviceState.Error;
            }
        }


        public bool Error
        {
            get
            {
                return _state == DeviceState.Error;
            }
        }


        //public bool IsUp
        //{
        //    get
        //    {
        //        return _diLiftUp.Value && !_diLiftDown.Value;
        //    }
        //}
        //public bool IsDown
        //{
        //    get
        //    {
        //        return !_diLiftUp.Value && _diLiftDown.Value;
        //    }
        //}

        public LiftState Feedback3Inch
        {
            get
            {
                
                if (_di3InchExtend.Value && !_di3InchRetract.Value)
                    return LiftState.Up;
                if (!_di3InchExtend.Value && _di3InchRetract.Value)
                    return LiftState.Down;
                return LiftState.Unknown;
            }
        }

        public LiftState Feedback4Inch
        {
            get
            {
                //if (_di4InchExtend.Value && _di4InchRetract.Value)
                //    return LiftState.Error;
                if (_di4InchExtend.Value && !_di4InchRetract.Value)
                    return LiftState.Up;
                if (!_di4InchExtend.Value && _di4InchRetract.Value)
                    return LiftState.Down;
                //if (!_di4InchExtend.Value && !_di4InchRetract.Value)
                return LiftState.Unknown;
            }
        }


        public LiftState FeedbackLift
        {
            get
            {
                //if (_diLiftUp.Value && _diLiftDown.Value)
                //    return LiftState.Error;
                if (_diLiftUp.Value && !_diLiftDown.Value)
                    return LiftState.Up;
                if (!_diLiftUp.Value && _diLiftDown.Value)
                    return LiftState.Down;
                //if (!_di3InchExtend.Value && !_di3InchRetract.Value)
                return LiftState.Unknown;
            }
        }
        public LiftState SetPoint3Inch
        {
            get
            {
                //if (_do3InchExtend.Value && _do3InchRetract.Value)
                //    return LiftState.Error;
                if (_do3InchExtend.Value && !_do3InchRetract.Value)
                    return LiftState.Up;
                if (!_do3InchExtend.Value && _do3InchRetract.Value)
                    return LiftState.Down;
                //if (!_do3InchExtend.Value && !_do3InchRetract.Value)
                return LiftState.Unknown;
            }
        }

        public LiftState SetPoint4Inch
        {
            get
            {
                //if (_do4InchExtend.Value && _do4InchRetract.Value)
                //    return LiftState.Error;
                if (_do4InchExtend.Value && !_do4InchRetract.Value)
                    return LiftState.Up;
                if (!_do4InchExtend.Value && _do4InchRetract.Value)
                    return LiftState.Down;
                //if (!_do4InchExtend.Value && !_do4InchRetract.Value)
                return LiftState.Unknown;
            }
        }


        public LiftState SetPointLift
        {
            get
            {
                //if (_doLiftUp.Value && _doLiftDown.Value)
                //    return LiftState.Error;
                if (_doLiftUp.Value && !_doLiftDown.Value)
                    return LiftState.Up;
                if (!_doLiftUp.Value && _doLiftDown.Value)
                    return LiftState.Down;
                //if (!_do3InchExtend.Value && !_do3InchRetract.Value)
                return LiftState.Unknown;
            }
        }

        public IoCoolBuffer(string module, XmlElement node, string ioModule = "")
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");


            _diLiftUp = ParseDiNode("diLiftUp", node, ioModule);
            _diLiftDown = ParseDiNode("diLiftDown", node, ioModule);

            _di3InchExtend = ParseDiNode("diAligner1Extend", node, ioModule);
            _di3InchRetract = ParseDiNode("diAligner1Retract", node, ioModule);

            _di4InchExtend = ParseDiNode("diAligner2Extend", node, ioModule);
            _di4InchRetract = ParseDiNode("diAligner2Retract", node, ioModule);

            _doLiftUp = ParseDoNode("doLiftUp", node, ioModule);
            _doLiftDown = ParseDoNode("doLiftDown", node, ioModule);

            _do3InchExtend = ParseDoNode("doAligner1Extend", node, ioModule);
            _do3InchRetract = ParseDoNode("doAligner1Retract", node, ioModule);

            _do4InchExtend = ParseDoNode("doAligner2Extend", node, ioModule);
            _do4InchRetract = ParseDoNode("doAligner2Retract", node, ioModule);


            _scLiftUpTimeout = ParseScNode("scUpTimeout", node);
            _scLiftDownTimeout = ParseScNode("scDownTimeout", node);
        }

        public bool Initialize()
        {
            _state = DeviceState.Idle;

            WaferManager.Instance.SubscribeLocation(Module, 1);

            DATA.Subscribe($"{Module}.{Name}.Feedback3Inch", () => Feedback3Inch.ToString());
            DATA.Subscribe($"{Module}.{Name}.Feedback4Inch", () => Feedback4Inch.ToString());
            DATA.Subscribe($"{Module}.{Name}.FeedbackLift", () => FeedbackLift.ToString());

            DATA.Subscribe($"{Module}.{Name}.SetPoint3Inch", () => SetPoint3Inch.ToString());
            DATA.Subscribe($"{Module}.{Name}.SetPoint4Inch", () => SetPoint4Inch.ToString());
            DATA.Subscribe($"{Module}.{Name}.SetPointLift", () => SetPointLift.ToString());

            DATA.Subscribe($"{Module}.{Name}.Status", () => _state.ToString());
            DATA.Subscribe($"{Module}.{Name}.WaferSizeByWafer", () => WaferManager.Instance.GetWafer(ModuleHelper.Converter(Module), 0).Size.ToString());
            DATA.Subscribe($"{Module}.{Name}.WaferSizeBySetPoint", () => _size.ToString());


            OP.Subscribe($"{Module}.{Name}.MoveUp3", (string reason, object[] param) =>
            {
                reason = "";
                return Move(WaferSize.WS3, true, out reason);
            });
            OP.Subscribe($"{Module}.{Name}.MoveUp4", (string reason, object[] param) =>
            {
                reason = "";
                return Move(WaferSize.WS4, true, out reason);
            });
            OP.Subscribe($"{Module}.{Name}.MoveUpLift", (string reason, object[] param) =>
            {
                reason = "";
                return Move(WaferSize.WS6, true, out reason);
            });


            OP.Subscribe($"{Module}.{Name}.MoveDown3", (string reason, object[] param) =>
            {
                reason = "";
                return Move(WaferSize.WS3, false, out reason);
            });

            OP.Subscribe($"{Module}.{Name}.MoveDown4", (string reason, object[] param) =>
            {
                reason = "";
                return Move(WaferSize.WS4, false, out reason);
            });

            OP.Subscribe($"{Module}.{Name}.MoveDownLift", (string reason, object[] param) =>
            {
                reason = "";
                return Move(WaferSize.WS6, false, out reason);
            });
            return true;
        }


        public void Monitor()
        {
            switch (_state)
            {
                case DeviceState.MovingUp:
                    if (CheckPinUp() /*&& Check3InchDown() && Check4InchDown()*/)
                    {
                        _state = DeviceState.Idle;
                    }
                    else
                    {
                        if (_timer.IsTimeout())
                        {
                            EV.PostAlarmLog(Module, $"{Module} {Name} Can not move up in {_scLiftUpTimeout.IntValue} seconds");
                            _state = DeviceState.Error;
                        }
                        else
                        {
                            if (CheckPinUp())
                            {
                                //Set3InchDown(out _);
                                //Set4InchDown(out _);
                            }
                        }
                    }
                    break;
                case DeviceState.MovingDown:
                    bool guided = CheckGuided(_size);
                    if (CheckPinDown() && guided)
                    {
                        _state = DeviceState.Idle;
                    }
                    else
                    {
                        if (_timer.IsTimeout())
                        {
                            EV.PostAlarmLog(Module, $"{Module} {Name} Can not move down in {_scLiftUpTimeout.IntValue} seconds");
                            _state = DeviceState.Error;
                        }
                        else
                        {
                            if (guided)
                            {
                                SetPinDown(out _);
                            }
                        }
                    }
                    break;

                case DeviceState.Homing:
                    if(Check3InchDown() && Check4InchDown() && CheckPinDown())
                        _state = DeviceState.Idle;
                    break;

                default:
                    break;
            }

        }

        public void Terminate()
        {
        }

        public bool Home(out string reason)
        {
            ModuleName _module;
            if (!Enum.TryParse<ModuleName>(Module, out _module))
            {
                reason = $"{Module} isn't exist";
                return false;
            }
            if (_state != DeviceState.Error && _state != DeviceState.Idle)
            {
                reason = $"{Module} is in {_state} state.";
                return false;
            }
            WaferInfo info = WaferManager.Instance.GetWafer(_module, 0);

            _do3InchExtend.SetValue(false, out reason);
            _do4InchExtend.SetValue(false, out reason);
            _doLiftUp.SetValue(false, out reason);

            _do3InchRetract.SetValue(true, out reason);
            _do4InchRetract.SetValue(true, out reason);
            _doLiftDown.SetValue(true, out reason);

            _state = DeviceState.Homing;
            return true;
        }

        public bool Move(WaferSize size, bool up, out string reason)
        {
            _size = size;
            if (up)
                return MoveUp(out reason);

            return MoveDown(out reason);
        }

        public bool Check3InchUp() { return Feedback3Inch == LiftState.Up; }
        public bool Check3InchDown() { return Feedback3Inch == LiftState.Down; }
        public bool Check4InchUp() { return Feedback4Inch == LiftState.Up; }
        public bool Check4InchDown() { return Feedback4Inch == LiftState.Down; }
        public bool CheckPinUp() { return FeedbackLift == LiftState.Up; }
        public bool CheckPinDown() { return FeedbackLift == LiftState.Down; }

        private bool Set3InchUp(out string reason)
        {
            if (SetPoint3Inch != LiftState.Up)
                EV.PostInfoLog(Module, $"{Module} move 3 inch up");
            return _do3InchExtend.SetValue(true, out reason) && _do3InchRetract.SetValue(false, out reason);
        }

        private bool Set3InchDown(out string reason)
        {
            if (SetPoint3Inch != LiftState.Down)
                EV.PostInfoLog(Module, $"{Module} move 3 inch down");
            return _do3InchExtend.SetValue(false, out reason) && _do3InchRetract.SetValue(true, out reason);
        }

        private bool Set4InchUp(out string reason)
        {
            if (SetPoint4Inch != LiftState.Up)
                EV.PostInfoLog(Module, $"{Module} move 4 inch up");
            return _do4InchExtend.SetValue(true, out reason) && _do4InchRetract.SetValue(false, out reason);
        }

        private bool Set4InchDown(out string reason)
        {
            if (SetPoint4Inch != LiftState.Down)
                EV.PostInfoLog(Module, $"{Module} move 4 inch down");
            return _do4InchExtend.SetValue(false, out reason) && _do4InchRetract.SetValue(true, out reason);
        }

        private bool SetPinUp(out string reason)
        {
            if (SetPointLift != LiftState.Up)
                EV.PostInfoLog(Module, $"{Module} move pin up");
            return _doLiftUp.SetValue(true, out reason) && _doLiftDown.SetValue(false, out reason);
        }

        private bool SetPinDown(out string reason)
        {
            if (SetPointLift != LiftState.Down)
                EV.PostInfoLog(Module, $"{Module} move pin down");
            return _doLiftUp.SetValue(false, out reason) && _doLiftDown.SetValue(true, out reason);
        }

        public bool CheckMovedUp()
        {
            // return CheckPinUp() && Check3InchDown() && Check4InchDown();
            return CheckPinUp();
        }

        public bool CheckMovedDown()
        {
            return CheckPinDown() && CheckGuided(_size);
        }

        private bool CheckGuided(WaferSize size)
        {
            bool guided = false;
            switch (_size)
            {
                case WaferSize.WS0:
                case WaferSize.WS8:
                case WaferSize.WS6:
                    {
                        guided = Check3InchDown() && Check4InchDown();
                    }
                    break;
                case WaferSize.WS3:
                    {
                        guided = Check3InchUp() && Check4InchDown();
                    }
                    break;
                case WaferSize.WS4:
                    {
                        guided = Check3InchDown() && Check4InchUp();
                    }
                    break;
            }

            return guided;
        }

        private bool MoveUp(out string reason)
        {
            reason = string.Empty;

            bool ret = true;
            switch (_size)
            {
                case WaferSize.WS0:
                case WaferSize.WS8:
                case WaferSize.WS6:
                    {
                        if (!Set3InchDown(out reason) || !Set4InchDown(out reason) || !SetPinUp(out reason))
                        {
                            ret = false;
                        }
                    }
                    break;
                case WaferSize.WS3:
                    {
                        if (!Set3InchUp(out reason) || !Set4InchDown(out reason) || !SetPinUp(out reason))
                        {
                            ret = false;
                        }
                    }
                    break;
                case WaferSize.WS4:
                    {
                        if (!Set3InchDown(out reason) || !Set4InchUp(out reason) || !SetPinUp(out reason))
                        {
                            ret = false;
                        }
                    }
                    break;
               
            }

            if (!ret)
                return false;

            _timer.Start(_scLiftUpTimeout.IntValue * 1000);

            _state = DeviceState.MovingUp;

            return true;
        }


        private bool MoveDown(out string reason)
        {
            reason = string.Empty;

            bool ret = true;
            switch (_size)
            {
                case WaferSize.WS0:
                case WaferSize.WS8:
                case WaferSize.WS6:
                    {
                        if (!Set3InchDown(out reason) || !Set4InchDown(out reason))
                        {
                            ret = false;
                        }
                    }
                    break;
                case WaferSize.WS3:
                    {
                        if (!Set3InchUp(out reason) || Set4InchDown(out reason))
                        {
                            ret = false;
                        }
                    }
                    break;
                case WaferSize.WS4:
                    {
                        if (!Set4InchUp(out reason)|| !Set3InchDown(out reason))
                        {
                            ret = false;
                        }
                    }
                    break;
            }


            if (!ret)
                return false;

            _timer.Start(_scLiftDownTimeout.IntValue * 1000);

            _state = DeviceState.MovingDown;

            return true;
        }


        public void Reset()
        {
        }


    }
}
