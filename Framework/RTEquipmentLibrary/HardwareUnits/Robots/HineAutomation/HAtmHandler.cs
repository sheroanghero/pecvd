using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.HineAutomation
{
    public abstract class HAtmHandler : HandlerBase
    {
        protected ModuleName _target;
        protected int _slot;
        protected const string EOT = "\r\n";
        protected bool _isBackground;
        protected bool _isSimulator;
        public bool IsBackground { get { return _isBackground; } }

        protected bool _hasResponse;
        public bool HasResponse { get { return _hasResponse; } } 

        public HAtmRobot RobotDevice { get; }
        public ModuleName Target => _target;
        public int Slot => _slot;

        protected HAtmHandler(Robot robot)
            : base("")
        {
            RobotDevice = (HAtmRobot)robot;
            _isSimulator = SC.GetValue<bool>("System.IsSimulatorMode");
        }

        public virtual string Package(params object[] args)
        {
            return "";
        }

        public virtual void Update()
        {

        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            ResponseMessage = msg;
            handled = true;
            return true;
        }

        public string GetBladeTarget(ModuleName target, int slot)
        {
            string bladeTarget = "";
            var from = "";
            var level = "";
            {
                int cassette = 180;
                int cooling = 110;
                int pm = 55;
                
                if (Math.Abs(RobotDevice.RotateReqPos - cassette) < 10)
                {
                    from = $"{ModuleName.LoadLock}";
                }

                if (Math.Abs(RobotDevice.RotateReqPos - cooling) < 10)
                {
                    from = $"{ModuleName.Cooling}";
                }

                if (Math.Abs(RobotDevice.RotateReqPos - pm) < 10)
                {
                    from = $"{ModuleName.PM}";
                }

                if (Math.Abs(RobotDevice.RotateReqPos) < 10)
                {
                    from = $"{ModuleHelper.Converter(RobotDevice.Name)}";
                }
            }
            {
                //int pm = 1600;
                //int cool = 2628;
                if (RobotDevice.ZActPos < 15000)
                {
                    if(slot < 8)
                    {
                        level = "low,low";
                    }
                    else if(slot > 15)
                    {
                        level = "low,high";
                    }
                    else
                    {
                        level = "low,medium";
                    }
                }
                else if(RobotDevice.ZActPos > 30000)
                {
                    if (slot < 8)
                    {
                        level = "high,low";
                    }
                    else if (slot > 15)
                    {
                        level = "high,high";
                    }
                    else
                    {
                        level = "high,medium";
                    }
                }
                else
                {
                    if (slot < 8)
                    {
                        level = "medium,low";
                    }
                    else if (slot > 15)
                    {
                        level = "medium,high";
                    }
                    else
                    {
                        level = "medium,medium";
                    }
                }
            }

            bladeTarget = $"{from}.{level}";
            return bladeTarget;
        }
    }

    public class HomeHandler : HAtmHandler
    {
        private const string Category = "HM";
        public HomeHandler(Robot robot)
           : base(robot)
        {
            Name = "Home";
            _isBackground = false;
            _hasResponse = SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse");

            //if(_isSimulator)
                UpdateBefore();
        }

        public override string Package(params object[] args)
        {
            //A,HM<CR><LF>
            return string.Format($"A,HM{EOT}");
        }

        private void UpdateBefore()
        {
            RobotDevice.Blade1Target = ModuleHelper.Converter(RobotDevice.Name);
            //RobotDevice.CmdBladeTarget = $"{ModuleHelper.Converter(RobotDevice.Name)}.Retract";
			RobotDevice.CmdBlade1Extend = "0";
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            // X,HM<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class AbortHandler : HAtmHandler
    {
        public AbortHandler(Robot robot)
           : base(robot)
        {
            Name = "Abort";
            _isBackground = false;
            _hasResponse = false;
        }

        public override string Package(params object[] args)
        {
            return string.Format($"D{EOT}");
        }
    }

    public class ResetHandler : HAtmHandler
    {
        private const string Category = "ER";
        public ResetHandler(Robot robot)
           : base(robot)
        {
            Name = "Reset";
            _isBackground = false;
            _hasResponse = SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse");
        }

        public override string Package(params object[] args)
        {
            return string.Format($"S,ER{EOT}");
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            // X,ER<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class PickHandler : HAtmHandler
    {
        private int _thetaAxis;
        private int _zAxis;
        private Hand blade;
        private const string Category = "PK";

        public PickHandler(Robot robot, ModuleName target, int slot = 0)
           : base(robot)
        {
            Name = "Pick";
            _thetaAxis = RobotConvertor.Chamber2ThetaAxisPosition(target);
            _zAxis = RobotConvertor.ChamberSlot2ZAxisPosition(target, slot);
            _slot = slot;
            _target = target;
            blade = Hand.Blade1;

            _isBackground = false;
            _hasResponse = SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse");

            //if (_isSimulator)
                UpdateBefore();
        }

        public override string Package(params object[] args)
        {
            //A,PK[,tt,zz,at]<CR><LF>
            return string.Format("A,PK,{0:D2},{1:D2}{2}",
               _thetaAxis,
               _zAxis,
               EOT);
        }

        private void UpdateBefore()
        {
            RobotDevice.Blade1Target = _target;

            //RobotDevice.CmdBladeTarget = $"{GetBladeTarget(_target, _slot)}.Picking";
            RobotDevice.CmdBladeTarget = $"{_target}.Picking";
            RobotDevice.CmdBlade1Extend = "1";

            //if (!_isSimulator)
            //    WaferManager.Instance.WaferMoved(_target, _slot, ModuleHelper.Converter(RobotDevice.Name), (int)blade);
        }

        public override void Update()
        {
            //if (_isSimulator)
            //{
            //    WaferManager.Instance.WaferMoved(_target, _slot, ModuleHelper.Converter(RobotDevice.Name), (int)blade);
            //    RobotDevice.CmdBladeTarget = $"{_target}.Retract";
            //}
            //else
            {
                if(WaferManager.Instance.CheckNoWafer(ModuleHelper.Converter(RobotDevice.Name), (int)blade) && WaferManager.Instance.CheckHasWafer(_target, _slot))
                {
                    WaferManager.Instance.WaferMoved(_target, _slot, ModuleHelper.Converter(RobotDevice.Name), (int)blade);
                    LOG.Info("Wafer move in handler");
                }
            }
            RobotDevice.Blade1Target = ModuleHelper.Converter(RobotDevice.Name);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            // X,PK<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class GotoHandler : HAtmHandler
    {
        private int _thetaAxis;
        private int _zAxis;
        private Hand blade;
        private bool _isPick;
        private const string Category = "GO";
        private string _up;

        public GotoHandler(Robot robot, ModuleName target, int slot = 0, bool isPick = true, bool isUp = true)
           : base(robot)
        {
            Name = "goto";
            _thetaAxis = RobotConvertor.Chamber2ThetaAxisPosition(target);
            _zAxis = RobotConvertor.ChamberSlot2ZAxisPosition(target, slot);
            _slot = slot;
            _target = target;
            blade = Hand.Blade1;
            _isPick = isPick;
            _up = isUp ? "U" : "D";

            _isBackground = false;
            _hasResponse = SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse");

            //if (_isSimulator)
                UpdateBefore();
        }

        public override string Package(params object[] args)
        {
            //A,GO[,rr,tt,zzb]<CR><LF>
            return string.Format("A,GO,EX,{0:D2},{1:D2}{2}{3}",
               _thetaAxis,
               _zAxis,
               _up,
               EOT);
        }

        private void UpdateBefore()
        {
            RobotDevice.Blade1Target = _target;

            //RobotDevice.CmdBladeTarget = $"{_target}.{(_isPick ? "Picking" : "Placing")}";
            //RobotDevice.CmdBladeTarget = $"{GetBladeTarget(_target, _slot)}.Extend";
            RobotDevice.CmdBladeTarget = $"{_target}.Extend";
            RobotDevice.CmdBlade1Extend = "1";

            
        }

        public override void Update()
        {
            if (!_isPick)
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(RobotDevice.Name), (int)blade, _target, _slot);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            // X,GO<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class ZAxisMoveHandler : HAtmHandler
    {
        private int _zAxis;
        private Hand blade;
        private const string Category = "GZ";
        private string _up;
        private bool _isUp;

        public ZAxisMoveHandler(Robot robot, ModuleName target, int slot, bool isUp)
           : base(robot)
        {
            Name = "goto";
            _zAxis = RobotConvertor.ChamberSlot2ZAxisPosition(target, slot);
            _slot = slot;
            _target = target;
            blade = Hand.Blade1;
            _up = isUp ? "U" : "D";
            _isUp = isUp;

            _isBackground = false;
            _hasResponse = SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse");

            //if (_isSimulator)
            UpdateBefore();
        }

        public override string Package(params object[] args)
        {
            //A,GZ[,zzb]<CR><LF>
            return string.Format("A,GZ,{0:D2}{1}{2}",
               _zAxis,
               _up,
               EOT);
        }

        private void UpdateBefore()
        {

        }

        public override void Update()
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            // X,GZ<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class PlaceRetractHandler : HAtmHandler
    {
        private int _thetaAxis;
        private int _zAxis;
        //private Hand blade;
        private const string Category = "GO";

        public PlaceRetractHandler(Robot robot, ModuleName target, int slot = 0)
           : base(robot)
        {
            Name = "PlaceRetract";
            _thetaAxis = RobotConvertor.Chamber2ThetaAxisPosition(target);
            _zAxis = RobotConvertor.ChamberSlot2ZAxisPosition(target, slot);
            _slot = slot;
            _target = target;
            //blade = Hand.Blade1;

            _isBackground = false;
            _hasResponse = SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse");

            //if (_isSimulator)
                UpdateBefore();
        }

        public override string Package(params object[] args)
        {
            //A,GO[,rr,tt,zzb]<CR><LF>
            return string.Format("A,GO,RE,{0:D2},{1:D2}U{2}",
               _thetaAxis,
               _zAxis,
               EOT);
        }

        private void UpdateBefore()
        {
            RobotDevice.Blade1Target = ModuleHelper.Converter(RobotDevice.Name);

            RobotDevice.CmdBladeTarget = $"{_target}.PlaceRetract";
            RobotDevice.CmdBlade1Extend = "0";
        }

        public override void Update()
        {
            //WaferManager.Instance.WaferMoved(ModuleHelper.Converter(RobotDevice.Name), (int)blade, _target, _slot);

            //if (_isSimulator)
            {
                //RobotDevice.Blade1Target = ModuleHelper.Converter(RobotDevice.Name);

                //RobotDevice.CmdBladeTarget = $"{_target}.Retract";
                //RobotDevice.CmdBlade1Extend = "0";
            }
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            // X,GO<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class PickRetractHandler : HAtmHandler
    {
        private int _thetaAxis;
        private int _zAxis;
        private Hand blade;
        private const string Category = "GO";
        private bool _isNeedWaferMove;

        public PickRetractHandler(Robot robot, ModuleName target, int slot = 0, bool isNeedWaferMove = true)
           : base(robot)
        {
            Name = "PickRetract";
            _thetaAxis = RobotConvertor.Chamber2ThetaAxisPosition(target);
            _zAxis = RobotConvertor.ChamberSlot2ZAxisPosition(target, slot);
            _slot = slot;
            _target = target;
            blade = Hand.Blade1;

            _isNeedWaferMove = isNeedWaferMove;

            _isBackground = false;
            _hasResponse = SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse");

            //if (_isSimulator)
                UpdateBefore();
        }

        public override string Package(params object[] args)
        {
            //A,GO[,rr,tt,zzb]<CR><LF>
            return string.Format("A,GO,RE,{0:D2},{1:D2}U{2}",
               _thetaAxis,
               _zAxis,
               EOT);
        }

        private void UpdateBefore()
        {
            if(_isNeedWaferMove)
                WaferManager.Instance.WaferMoved(_target, _slot, ModuleHelper.Converter(RobotDevice.Name), (int)blade);
            RobotDevice.Blade1Target = ModuleHelper.Converter(RobotDevice.Name);

            RobotDevice.CmdBladeTarget = $"{_target}.PickRetract";
            RobotDevice.CmdBlade1Extend = "0";
        }

        public override void Update()
        {
            //WaferManager.Instance.WaferMoved(_target, _slot, ModuleHelper.Converter(RobotDevice.Name), (int)blade);

            //if (_isSimulator)
            {
                //RobotDevice.Blade1Target = ModuleHelper.Converter(RobotDevice.Name);

                //RobotDevice.CmdBladeTarget = $"{_target}.Retract";
                //RobotDevice.CmdBlade1Extend = "0";
            }
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            // X,GO<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class PlaceHandler : HAtmHandler
    {
        private int _thetaAxis;
        private int _zAxis;
        private Hand blade;
        private const string Category = "PL";

        public PlaceHandler(Robot robot, ModuleName target, int slot = 0)
           : base(robot)
        {
            Name = "Place";
            _thetaAxis = RobotConvertor.Chamber2ThetaAxisPosition(target);
            _zAxis = RobotConvertor.ChamberSlot2ZAxisPosition(target, slot);
            _slot = slot;
            _target = target;
            blade = Hand.Blade1;

            _isBackground = false;
            _hasResponse = SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse");

            //if (_isSimulator)
                UpdateBefore();
        }

        public override string Package(params object[] args)
        {
            //A,PL[,tt,zz,at]<CR><LF>
            return string.Format("A,PL,{0:D2},{1:D2}{2}",
               _thetaAxis,
               _zAxis,
               EOT);
        }

        private void UpdateBefore()
        {
            RobotDevice.Blade1Target = _target;

            //RobotDevice.CmdBladeTarget = $"{GetBladeTarget(_target, _slot)}.Placing";
            RobotDevice.CmdBladeTarget = $"{_target}.Placing";
            RobotDevice.CmdBlade1Extend = "1";
        }

        public override void Update()
        {
            if(WaferManager.Instance.CheckHasWafer(ModuleHelper.Converter(RobotDevice.Name), (int)blade) && WaferManager.Instance.CheckNoWafer(_target, _slot))
            {
                WaferManager.Instance.WaferMoved(ModuleHelper.Converter(RobotDevice.Name), (int)blade, _target, _slot);
                LOG.Info("Wafer move in handler");
            }
            
            //if (_isSimulator)
            //{
            //    RobotDevice.CmdBladeTarget = $"{_target}.Retract";
            //}

            RobotDevice.Blade1Target = ModuleHelper.Converter(RobotDevice.Name);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            // X,PL<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class VacuumOnHandler : HAtmHandler
    {
        private const string Category = "VY";
        public VacuumOnHandler(Robot robot)
           : base(robot)
        {
            Name = "vacuum on";
            _isBackground = false;
            _hasResponse = SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse");
        }

        public override string Package(params object[] args)
        {
            //A,VY<CR><LF>
            return string.Format("A,VY{0}",
               EOT);
        }

        private void UpdateBefore()
        {
        }

        public override void Update()
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            // X,VY<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class VacuumOffHandler : HAtmHandler
    {
        private const string Category = "VN";
        public VacuumOffHandler(Robot robot)
           : base(robot)
        {
            Name = "vacuum off";
            _isBackground = false;
            _hasResponse = SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse");
        }

        public override string Package(params object[] args)
        {
            //A,VN<CR><LF>
            return string.Format("A,VN{0}",
               EOT);
        }

        private void UpdateBefore()
        {
        }

        public override void Update()
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            // X,VN<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class SetThetaSpeedHandler : HAtmHandler
    {
        private int _speed;
        private int _acceleration;
        private const string Category = "MH";
        public SetThetaSpeedHandler(Robot robot, int speed, int acceleration)
           : base(robot)
        {
            Name = "set speed";
            _isBackground = false;
            _hasResponse = SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse");
            _speed = speed;
            _acceleration = acceleration;
        }

        public override string Package(params object[] args)
        {
            //S,MH,a,xx,yy<CR><LF>
            //xx = Percentage of maximum allowed acceleration.
            //yy = Percentage of maximum allowed speed
            //a T(Theta) or Z(Up/ Down), or R(Reach)
            var command = "S,MH,T," + _speed.ToString("D2") + "," + _acceleration.ToString("D2");
            return string.Format("{0}{1}", command,
               EOT);
        }

        private void UpdateBefore()
        {
        }

        public override void Update()
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            // X,MH<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class SetReachSpeedHandler : HAtmHandler
    {
        private int _speed;
        private int _acceleration;
        private const string Category = "MH";
        public SetReachSpeedHandler(Robot robot, int speed, int acceleration)
           : base(robot)
        {
            Name = "set speed";
            _isBackground = false;
            _hasResponse = SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse");
            _speed = speed;
            _acceleration = acceleration;
        }

        public override string Package(params object[] args)
        {
            //S,MH,a,xx,yy<CR><LF>
            //xx = Percentage of maximum allowed acceleration.
            //yy = Percentage of maximum allowed speed
            //a T(Theta) or Z(Up/ Down), or R(Reach)
            var command = "S,MH,R," + _speed.ToString("D2") + "," + _acceleration.ToString("D2");
            return string.Format("{0}{1}", command,
               EOT);
        }

        private void UpdateBefore()
        {
        }

        public override void Update()
        {
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            // X,MH<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class QueryAllAxisStatusHandler : HAtmHandler
    {
        private const string Retract = "RE";
        private const string Up = "U";
        private const string Category = "AA";
        //private bool _isUp;
        public QueryAllAxisStatusHandler(Robot robot)
           : base(robot)
        {
            Name = "QueryAllAxisStatus";

            _isBackground = false;
            _hasResponse = true;
        }

        public override string Package(params object[] args)
        {
            //R,AA <CR> <LF>
            return string.Format($"R,AA{EOT}");
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            //aabbccd     X,AA,RE0000d
            //aa = RE(Radial Axis Retract0 or EX(Extended)
            //bb = Theta Axis Address Position (01-15)
            //cc = Z Axis Address Position (01-99)
            //d = U(Up) or D(Dn)
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category || result.MessagePart.Length < 1 || result.MessagePart[0].Length != 7)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            string aa = result.MessagePart[0].Substring(0, 2);
            string bb = result.MessagePart[0].Substring(2, 2);
            string cc = result.MessagePart[0].Substring(4, 2);
            string d = result.MessagePart[0].Substring(6, 1);
            if (aa == Retract)
                RobotDevice.IsRetract = true;
            else
                RobotDevice.IsRetract = false;

            int.TryParse(bb, out int thetaAxisAddressPosition);
            RobotDevice.ThetaAxisAddressPosition = thetaAxisAddressPosition;
            int.TryParse(cc, out int zAxisAddressPosition);
            RobotDevice.ZAxisAddressPosition = zAxisAddressPosition;

            if (d.ToUpper() == Up)
                RobotDevice.IsUp = true;
            else
                RobotDevice.IsUp = false;

            handled = true;

            return true;
        }
    }

    public class QueryThetaAxisStatusHandler : HAtmHandler
    {
        private const string Retract = "RE";
        private const string Up = "U";
        private const string Category = "TA";
        private const string Busy = "B";
        private const string Referenced = "R";
        //private bool _isUp;
        public QueryThetaAxisStatusHandler(Robot robot)
           : base(robot)
        {
            Name = "QueryThetaAxisStatus";

            _isBackground = false;
            _hasResponse = true;
        }

        public override string Package(params object[] args)
        {
            //R,TA <CR> <LF>
            return string.Format($"R,TA{EOT}");
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            //X,TA,abccdde<CR><LF>
            //a = B (Busy) or R (Ready)
            //b = R (Referenced) or U (Unreferenced)
            //cc = Current Position
            //dd = Command Address Position
            //e = X (Overcurrent Latch Set) or (Not Set)
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category || result.MessagePart.Length < 1 || result.MessagePart[0].Length < 7)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            string a = result.MessagePart[0].Substring(0, 1);
            string b = result.MessagePart[0].Substring(1, 1);
            string cc = result.MessagePart[0].Substring(2, 2);
            string dd = result.MessagePart[0].Substring(4, 2);
            string e = result.MessagePart[0].Substring(6, 1);
            if (a == Busy)
                RobotDevice.IsThetaBusy = true;
            else
                RobotDevice.IsThetaBusy = false;

            if (RobotDevice.IsHomed)
            {
                RobotDevice.IsRotationAxisUnreferencedTrig.CLK = b.ToUpper() == "U";
                if (RobotDevice.IsRotationAxisUnreferencedTrig.Q)
                {
                    EV.PostAlarmLog(RobotDevice.Module, "Rotation axis is unreferenced, please repair it");
                }
            }

            handled = true;

            return true;
        }
    }

    public class QueryZAxisStatusHandler : HAtmHandler
    {
        private const string Retract = "RE";
        private const string Up = "U";
        private const string Category = "ZA";
        private const string Busy = "B";
        private const string Referenced = "R";
        //private bool _isUp;
        public QueryZAxisStatusHandler(Robot robot)
           : base(robot)
        {
            Name = "QueryZAxisStatus";

            _isBackground = false;
            _hasResponse = true;
        }

        public override string Package(params object[] args)
        {
            //R,ZA <CR> <LF>
            return string.Format($"R,ZA{EOT}");
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            //X,TA,abccdde<CR><LF>
            //X,ZA,abccdeefg<CR><LF>
            //a = B (Busy) or R (Ready)
            //b = R (Referenced) or U (Unreferenced)
            //cc = Current Position
            //dd = Command Address Position
            //e = X (Overcurrent Latch Set) or (Not Set)
            //f U(Up) or D(Down) Command
            //g = X (Overcurrent Latch Set) or (Space) (Not Set)
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category || result.MessagePart.Length < 1 || result.MessagePart[0].Length < 9)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            string a = result.MessagePart[0].Substring(0, 1);
            string b = result.MessagePart[0].Substring(1, 1);
            string cc = result.MessagePart[0].Substring(2, 2);
            string dd = result.MessagePart[0].Substring(4, 2);
            string e = result.MessagePart[0].Substring(6, 1);
            string f = result.MessagePart[0].Substring(7, 1);
            string g = result.MessagePart[0].Substring(8, 1);
            if (a == Busy)
                RobotDevice.IsZBusy = true;
            else
                RobotDevice.IsZBusy = false;

            if (RobotDevice.IsHomed)
            {
                RobotDevice.IsZAxisUnreferencedTrig.CLK = b.ToUpper() == "U";
                if (RobotDevice.IsZAxisUnreferencedTrig.Q)
                {
                    EV.PostAlarmLog(RobotDevice.Module, "Z axis is unreferenced, please repair it");
                }
            }

            handled = true;

            return true;
        }
    }

    public class QueryRadialAxisStatusHandler : HAtmHandler
    {
        private const string Retract = "RE";
        private const string Up = "U";
        private const string Category = "RA";
        private const string Busy = "B";
        private const string Referenced = "R";
        //private bool _isUp;
        public QueryRadialAxisStatusHandler(Robot robot)
           : base(robot)
        {
            Name = "QueryRadialAxisStatus";

            _isBackground = false;
            _hasResponse = true;
        }

        public override string Package(params object[] args)
        {
            //R,RA <CR> <LF>
            return string.Format($"R,RA{EOT}");
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            //X,RA,abccdde<CR><LF>
            //a = B (Busy) or R (Ready)
            //b = R (Referenced) or U (Unreferenced)
            //cc = Current Position(RE = retract, EX = extend)
            //dd = Command Address Position(RE = retract, EX = extend)
            //e = X (Overcurrent Latch Set) or (Not Set)
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category || result.MessagePart.Length < 1 || result.MessagePart[0].Length < 7)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            string a = result.MessagePart[0].Substring(0, 1);
            string b = result.MessagePart[0].Substring(1, 1);
            string cc = result.MessagePart[0].Substring(2, 2);
            string dd = result.MessagePart[0].Substring(4, 2);
            string e = result.MessagePart[0].Substring(6, 1);
            if (a == Busy)
                RobotDevice.IsRadialBusy = true;
            else
                RobotDevice.IsRadialBusy = false;

            if (cc == Retract)
                RobotDevice.IsRetract = true;
            else
                RobotDevice.IsRetract = false;

            if(RobotDevice.IsHomed)
            {
                RobotDevice.IsExtensionAxisUnreferencedTrig.CLK = b.ToUpper() == "U";
                if (RobotDevice.IsExtensionAxisUnreferencedTrig.Q)
                {
                    EV.PostAlarmLog(RobotDevice.Module, "Extension axis is unreferenced, please repair it");
                }
            }

            handled = true;

            return true;
        }
    }

    public class QueryErrorHandler : HAtmHandler
    {
        private const string Category = "ER";
        public QueryErrorHandler(Robot robot)
           : base(robot)
        {
            Name = "QueryError";

            _isBackground = false;
            _hasResponse = true;
        }

        public override string Package(params object[] args)
        {
            //R,ER <CR> <LF>
            return string.Format("R,ER{0}", EOT);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            //aa,bb,…..zz     X,ER,OK
            //aa = first error code
            //bb = second error code
            //etc… All current error codes will be returned.
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category || result.MessagePart.Length < 1)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            if(result.MessagePart[0] == "OK")
            {
                return true;
            }

            ResponseMessage = msg;
            string error = string.Join(",", result.MessagePart);

            EV.PostAlarmLog(RobotDevice.Name, $"{RobotDevice.Name} error, error code({error}):{(RobotDevice.ErrorCodeReference.ContainsKey(error) ? RobotDevice.ErrorCodeReference[error] : "")}");

            handled = true;

            return true;
        }
    }

    public class QueryOperationalStatusHandler : HAtmHandler
    {
        private const string Error = "E";
        private const string Busy = "B";
        private const string Referenced = "R";
        private const string DataValid = "V";
        private const string LastCommandSuccessful = "S";
        private const string WaferPresent = "Y";
        private const string OvercurrentShutdownPresent = "X";
        private const string CassettePortChange = "C";
        private const string Category = "OS";
        public QueryOperationalStatusHandler(Robot robot)
           : base(robot)
        {
            Name = "QueryOperationalStatus";

            _isBackground = false;
            _hasResponse = true;
        }

        public override string Package(params object[] args)
        {
            //R,OS <CR> <LF>
            return string.Format($"R,OS{EOT}");
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            //abcdeeffgghijkl   "X,OS, RRVHM    UN C"
            //a = E (Error Present) or space (No Error Present)
            //b = B (Busy) or R (Ready)
            //c = R (All Axes Referenced) or space (Not All Referenced)
            //d = V (Data Valid) or space (Data Not Valid)
            //ee = Last/Current Action Category
            //ff = Theta Address of High Level Command
            //gg = Z-Address of High Level Command
            //h = S (Last Command Successful) or U (Unsuccessful)
            //I = Y (Wafer Present on End Effector) or N (Not Present)
            //J = X (Overcurrent Shutdown Present) or space (Not Present)
            //K = C (Cassette Port Change) or space (no change)
            //L = Mapping sensor status

            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category || result.MessagePart.Length < 1 || 
                (result.MessagePart[0].Length != 14 && result.MessagePart[0].Length != 15))
            {
                RobotDevice.SetError(result.RawMessage );
                return true;
            }

            ResponseMessage = msg;
            string a = result.MessagePart[0].Substring(0, 1);
            string b = result.MessagePart[0].Substring(1, 1);
            string c = result.MessagePart[0].Substring(2, 1);
            string d = result.MessagePart[0].Substring(3, 1);
            string ee = result.MessagePart[0].Substring(4, 2);
            string ff = result.MessagePart[0].Substring(6, 2);
            string gg = result.MessagePart[0].Substring(8, 2);
            string h = result.MessagePart[0].Substring(10, 1);
            string i = result.MessagePart[0].Substring(11, 1);
            string j = result.MessagePart[0].Substring(12, 1);
            string k = result.MessagePart[0].Substring(13, 1);
            string l = string.Empty;
            if (result.MessagePart[0].Length > 14)
                l = result.MessagePart[0].Substring(14, 1);

            if (a == Error)
                RobotDevice.SetError($"{result.RawMessage}");

            if (b == Busy)
                RobotDevice.IsBusy = true;
            else
                RobotDevice.IsBusy = false; ;

            if (i == WaferPresent)
                RobotDevice.WaferPresentOnBlade1 = true;
            else
                RobotDevice.WaferPresentOnBlade1 = false;

            handled = true;

            return true;
        }
    }

    public class QueryPositionHandler : HAtmHandler
    {
        private const string Category = "GP";
        public QueryPositionHandler(Robot robot)
           : base(robot)
        {
            Name = "QueryPosition";

            _isBackground = false;
            _hasResponse = true;
        }

        public override string Package(params object[] args)
        {
            //R,GP <CR> <LF>
            return string.Format($"R,GP{EOT}");
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            //X,GP,0,11118,1149,0,11118,1149<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category || result.MessagePart.Length != 6)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            string rReqPos = result.MessagePart[0];//retract
            string tReqPos = result.MessagePart[1];//rotate
            string zReqPos = result.MessagePart[2];//z
            string rActPos = result.MessagePart[3];
            string tActPos = result.MessagePart[4];
            string zActPos = result.MessagePart[5];
            handled = true;
            int.TryParse(rReqPos, out int pos);
            RobotDevice.RetractReqPos = pos;

            int.TryParse(tReqPos, out pos);
            RobotDevice.RotateReqPos = pos/100;//deg

            int.TryParse(zReqPos, out pos);
            RobotDevice.ZReqPos = pos;

            return true;
        }
    }

    public class QuerySpeedHandler : HAtmHandler
    {
        private const string Category = "SS";
        public QuerySpeedHandler(Robot robot)
           : base(robot)
        {
            Name = "QuerySpeed";

            _isBackground = false;
            _hasResponse = true;
        }

        public override string Package(params object[] args)
        {
            //R,SS <CR> <LF>
            return string.Format($"R,SS{EOT}");
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            // X,SS,rAccel,rVel, tAccel, tVel, zAccel, zVel<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category || result.MessagePart.Length != 6)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            string rAccel = result.MessagePart[0];
            string rVel = result.MessagePart[1];
            string tAccel = result.MessagePart[2];
            string tVel = result.MessagePart[3];
            string zAccel = result.MessagePart[4];
            string zVel = result.MessagePart[5];
            handled = true;

            return true;
        }
    }

    public class SetCommandResponseHandler : HAtmHandler
    {
        private const string Category = "RR";
        public SetCommandResponseHandler(Robot robot)
           : base(robot)
        {
            Name = "SetCommandResponse";

            _isBackground = false;
            _hasResponse = SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse");
        }

        public override string Package(params object[] args)
        {
            //S,RR <CR> <LF>
            return string.Format($"S,{Category},Y{EOT}");
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            // X,RR<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class QueryWaferMapHandler : HAtmHandler
    {
        private const string Category = "MP";
        private int _thetaAxis;
        public QueryWaferMapHandler(Robot robot, ModuleName target)
           : base(robot)
        {
            Name = "QueryWaferMap";
            _thetaAxis = RobotConvertor.Chamber2ThetaAxisPosition(target);
            _isBackground = false;
            _hasResponse = true;
        }

        public override string Package(params object[] args)
        {
            //R,MP,tt<CR><LF>
            return string.Format("R,MP,{0:D2}{1}",
               _thetaAxis,
               EOT);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            //X,MP,tt,xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx<CR><LF>
            //0= No wafer
            //1= wafer present
            //2=double wafer
            //X=cross slotted wafer
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category || result.MessagePart == null && result.MessagePart.Length < 2)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            RobotDevice.SlotMap = string.Empty;
            foreach(var item in result.MessagePart[1])
            {
                RobotDevice.SlotMap += item;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }

    public class WaferMapHandler : HAtmHandler
    {
        private const string Category = "MP";
        private int _thetaAxis;
        public WaferMapHandler(Robot robot, ModuleName target)
           : base(robot)
        {
            Name = "WaferMap";
            _thetaAxis = RobotConvertor.Chamber2ThetaAxisPosition(target);
            _isBackground = false;
            _hasResponse = SC.GetValue<bool>("TMRobot.AllCommandsHaveResponse");
        }

        public override string Package(params object[] args)
        {
            //A,MP,tt<CR><LF>
            return string.Format("A,MP,{0:D2}{1}",
               _thetaAxis,
               EOT);
        }

        public override bool HandleMessage(MessageBase msg, out bool handled)
        {
            //X,MP,tt<CR><LF>
            handled = false;
            var result = msg as HAtmMessage;
            if (!result.IsResponse) return true;

            if (result.IsFormatError || result.Category != Category)
            {
                RobotDevice.SetError(result.Data);
                return true;
            }

            ResponseMessage = msg;
            handled = true;

            return true;
        }
    }
}
