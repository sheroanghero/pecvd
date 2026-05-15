using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using Newtonsoft.Json;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using System.Threading;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using System.Text.RegularExpressions;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.AlignersBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.JEL;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.JelAligner
{
    public class JelAlignerWithLiftNcd : JelAligner
    {
        /* SAL3262HV model: for 2-inch to 6-inch wafer
           SAL3362HV model: for 3-inch to 6-inch wafer
           SAL3482HV model: for 4-inch to 8-inch wafer
           SAL38C3HV model: for 8-inch to 12-inch wafer*/
        public JelAlignerWithLiftNcd(string module, string name, string scRoot, IoSensor[] dis, IoTrigger[] dos, int alignerType = 0, string robotModel = "") :
            base(module, name, scRoot, null, null, alignerType, robotModel)
        {
            _scRoot = scRoot;
            if (dis != null)
            {
                _diWaferPresent = dis[0];
                _diLifterPinError = dis[1];
                _diLifterPinStatus = dis[2];
                _diLifterPinUp = dis[3];
                _diLifterPinHome = dis[4];

                _diLifterPinError.OnSignalChanged += _diLifterPinError_OnSignalChanged;

            }
            if (dos != null)
            {
                _doLifterPinHome = dos[0];
                _doLifterPinUp = dos[1];
                _doLifterPinReset = dos[2];
            }




        }

        private void _diLifterPinError_OnSignalChanged(IoSensor arg1, bool arg2)
        {
            if (arg2)
            {
                OnError("MotionError");
            }
        }

        private IoSensor _diWaferPresent;
        private IoSensor _diLifterPinError;
        private IoSensor _diLifterPinStatus;
        private IoSensor _diLifterPinUp;
        private IoSensor _diLifterPinHome;

        private IoTrigger _doLifterPinHome;
        private IoTrigger _doLifterPinUp;
        private IoTrigger _doLifterPinReset;


        private int _bodyNumber;
        private string _robotModel;    //T-00902H
        //public int BodyNumber { get => _bodyNumber; }
        private string _address = "";
        //public string Address { get => _address; }
        private bool isSimulatorMode;
        private string _scRoot;
        private PeriodicJob _thread;
        //protected JelAlignerConnection _connection;
        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigCommunicationError = new R_TRIG();


        private R_TRIG _trigRetryConnect = new R_TRIG();
        private R_TRIG _trigActionDone = new R_TRIG();
        private LinkedList<HandlerBase> _lstMoveHandler = new LinkedList<HandlerBase>();
        private LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();
        private bool _isAligned;
        private bool _isOnHomedPostion;
        //public int TimelimitAlginerHome { get; private set; }

        //public int TimelimitForAlignWafer { get; private set; }
        private object _locker = new object();

        private bool _enableLog;

        public override bool OnTimer()
        {
            try
            {
                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstMoveHandler.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        _connection.SetPortAddress(SC.GetStringValue($"{_scRoot}.{Name}.PortName"));
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                        }
                    }
                    return true;
                }

                HandlerBase handler = null;
                DateTime dtstart = DateTime.Now;

                lock (_locker)
                {
                    while (_lstMoveHandler.Count > 0 && _lstMonitorHandler.Count == 0)
                    {
                        if (!_connection.IsBusy)
                        {
                            if (YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd &&
                                XAxisStatus == JelAxisStatus.NormalEnd)
                            {
                                handler = _lstMoveHandler.First.Value;
                                _connection.Execute(handler);
                                _lstMoveHandler.RemoveFirst();
                            }
                            else
                            {
                                _connection.Execute(new JelAlignerReadHandler(this, ""));
                            }
                        }
                        else
                        {
                            _connection.MonitorTimeout();

                            _trigCommunicationError.CLK = _connection.IsCommunicationError;
                            if (_trigCommunicationError.Q)
                            {
                                _lstMoveHandler.Clear();
                                OnError($"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                            }
                        }
                        if (DateTime.Now - dtstart > TimeSpan.FromSeconds(150))
                        {
                            _lstMonitorHandler.Clear();
                            _lstMoveHandler.Clear();
                            OnError("Robot action timeout");
                        }
                    }

                    while (_lstMonitorHandler.Count > 0)
                    {
                        if (!_connection.IsBusy)
                        {
                            handler = _lstMonitorHandler.First.Value;
                            _connection.Execute(handler);
                            _lstMonitorHandler.RemoveFirst();
                        }
                        if (DateTime.Now - dtstart > TimeSpan.FromSeconds(150))
                        {
                            _lstMonitorHandler.Clear();
                            _lstMoveHandler.Clear();
                            OnError("Robot action timeout");
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



        public override bool IsNeedChangeWaferSize(WaferSize wz)
        {
            return false;
        }


        protected override bool fReset(object[] param)
        {
            if (!_connection.IsConnected)
            {
                _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
                _bodyNumber = SC.GetValue<int>($"{_scRoot}.{Name}.BodyNumber");
                PortName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");

                //TimelimitAlginerHome = SC.GetValue<int>($"{_scRoot}.{Name}.TimeLimitAlignerHome");

                _connection = new JelAlignerConnection(PortName);
                _connection.EnableLog(_enableLog);
                if (_connection.Connect())
                {

                    EV.PostInfoLog(Module, $"{Module}.{Name} connected");
                }
            }
            _trigError.RST = true;

            _connection.SetCommunicationError(false, "");
            _trigCommunicationError.RST = true;

            _enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            _connection.EnableLog(_enableLog);

            _trigRetryConnect.RST = true;

            _lstMoveHandler.Clear();
            _lstMonitorHandler.Clear();
            _connection.ForceClear();
            lock (_locker)
            {
                _lstMonitorHandler.AddLast(new JelAlignerRawCommandHandler(this, $"${BodyNumber}RD\r"));
                _lstMonitorHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMonitorHandler.AddLast(new JelAlignerReadHandler(this, "WIS1"));
                _lstMonitorHandler.AddLast(new JelAlignerReadHandler(this, "WAS"));
            }

            _doLifterPinReset.SetTrigger(true, out _);
            _dtActionStart = DateTime.Now;
            return true;
        }

        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds((double)TimelimitAlginerHome))
            {
                _doLifterPinReset.SetTrigger(false, out _);
                OnError("Reset timeout");
            }

            if (_diLifterPinError.Value) return false;

            _isAligned = false;
            if (_lstMonitorHandler.Count == 0 && !_connection.IsBusy)
                return true;

            return false;
            //if (XAxisStatus == JelAxisStatus.NormalEnd
            //    && (YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            //    && _lstMoveHandler.Count == 0
            //    && _lstMonitorHandler.Count == 0
            //    && !_connection.IsBusy)
            //{
            //    IsBusy = false;
            //    return true;
            //}
            //if (_lstMonitorHandler.Count == 0 && _lstMoveHandler.Count == 0 && !_connection.IsBusy)
            //    _connection.Execute(new JelAlignerReadHandler(this, ""));
            //return false;
        }

        protected override bool fStartInit(object[] param)
        {
            lock (_locker)
            {
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "W0"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, "WIS1"));


                if (SC.ContainsItem($"{_scRoot}.{Name}.JelWaferType"))
                {
                    int wafertype = SC.GetValue<int>($"{_scRoot}.{Name}.JelWaferType");
                    _lstMoveHandler.AddLast(new JelAlignerSetHandler(this, "WAT", wafertype.ToString()));

                }
                //    if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                //    {

                //        string strpara;
                //        switch (WaferManager.Instance.GetWaferSize(RobotModuleName, 0))
                //        {
                //            case WaferSize.WS2:
                //            case WaferSize.WS3:
                //            case WaferSize.WS4:
                //            case WaferSize.WS5:
                //            case WaferSize.WS6:
                //            case WaferSize.WS8:
                //                strpara = _currentSetWaferSize.ToString().Replace("WS", "");

                //                break;
                //            case WaferSize.WS12:
                //                strpara = "9";

                //                break;
                //            default:
                //                return false;
                //        }
                //        _lstMonitorHandler.AddLast(new JelAlignerMoveHandler(this, "WAS", strpara));
                //    }
                //    _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WMC"));
                //    _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, "WAS"));


                //}
                string strpara = _currentSetWaferSize.ToString().Replace("WS", "");
                _lstMonitorHandler.AddLast(new JelAlignerMoveHandler(this, "WAS", strpara));
            }
            _doLifterPinUp.SetTrigger(false, out _);
            _doLifterPinHome.SetTrigger(true, out _);

            _isAligned = false;
            _isOnHomedPostion = false;
            _dtActionStart = DateTime.Now;
            return true;


        }
        private DateTime _dtActionStart;
        protected override bool fMonitorInit(object[] param)
        {
            _isAligned = false;
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds((double)TimelimitAlginerHome))
            {
                _doLifterPinHome.SetTrigger(false, out _);
                OnError("Init timeout");
            }

            if (!_diLifterPinHome.Value || _diLifterPinUp.Value) return false;
            _doLifterPinHome.SetTrigger(false, out _);

            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
                && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            {

                if (IsWaferPresent(0) && WaferManager.Instance.CheckNoWafer(RobotModuleName, 0))
                {
                    WaferManager.Instance.CreateWafer(RobotModuleName, 0,
                         WaferStatus.Normal, GetCurrentWaferSize());
                }
                if (!IsWaferPresent(0) && WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                {
                    WaferManager.Instance.DeleteWafer(RobotModuleName, 0);
                }
                return true;
            }
            if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelAlignerReadHandler(this, ""));

            return false;
        }

        protected override bool fStartHome(object[] param)
        {
            lock (_locker)
            {
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "W0"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, "WIS1"));

                if (SC.ContainsItem($"{_scRoot}.{Name}.JelWaferType"))
                {
                    int wafertype = SC.GetValue<int>($"{_scRoot}.{Name}.JelWaferType");
                    _lstMoveHandler.AddLast(new JelAlignerSetHandler(this, "WAT", wafertype.ToString()));

                }
                //if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                //{

                //    string strpara;
                //    switch (WaferManager.Instance.GetWaferSize(RobotModuleName, 0))
                //    {
                //        case WaferSize.WS2:
                //        case WaferSize.WS3:
                //        case WaferSize.WS4:
                //        case WaferSize.WS5:
                //        case WaferSize.WS6:
                //        case WaferSize.WS8:
                //            strpara = _currentSetWaferSize.ToString().Replace("WS", "");

                //            break;
                //        case WaferSize.WS12:
                //            strpara = "9";

                //            break;
                //        default:
                //            return false;
                //    }
                //    _lstMonitorHandler.AddLast(new JelAlignerMoveHandler(this, "WAS", strpara));
                //}
                string strpara = _currentSetWaferSize.ToString().Replace("WS", "");
                _lstMonitorHandler.AddLast(new JelAlignerMoveHandler(this, "WAS", strpara));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WMC"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, "WAS"));

            }
            _doLifterPinHome.SetTrigger(true, out _);
            _isAligned = false;

            _dtActionStart = DateTime.Now;
            return true;
        }

        protected override bool fMonitorHome(object[] param)
        {
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds((double)TimelimitAlginerHome))
            {
                OnError("Home timeout");
                _doLifterPinHome.SetTrigger(false, out _);
            }

            if (!_diLifterPinHome.Value || _diLifterPinUp.Value) return false;
            _doLifterPinHome.SetTrigger(false, out _);




            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
                && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            {
                IsBusy = false;
                _isOnHomedPostion = true;
                return true;
            }
            if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelAlignerReadHandler(this, ""));
            return false;
        }


        public override bool IsReady()
        {

            return AlignerState == AlignerStateEnum.Idle && !IsBusy;
        }

        public override bool IsWaferPresent(int slotindex)
        {
            if (_diWaferPresent != null)
                return _diWaferPresent.Value;
            return WaferManager.Instance.CheckHasWafer(RobotModuleName, slotindex);
        }
        public override bool IsNeedPrepareBeforePlaceWafer()
        {
            return !_isOnHomedPostion;
        }
        protected override bool fStartLiftup(object[] param)   //Ungrip
        {
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {
                _doLifterPinHome.SetTrigger(false, out _);
                _doLifterPinUp.SetTrigger(true, out _);
                //_lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WU"));
                //_lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
            }



            return true;
        }

        protected override bool fMonitorLiftup(object[] param)
        {
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitForAlignWafer))
                OnError("Liftup timeout");

            if (!_diLifterPinHome.Value && _diLifterPinUp.Value)
            {
                IsBusy = false;
                _isAligned = false;
                _isOnHomedPostion = false;
                return true;
            }
            //if (_lstMoveHandler.Count == 0
            //    && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
            //    && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            //{
            //    IsBusy = false;
            //    _isAligned = false;
            //    _isOnHomedPostion = false;
            //    return true;
            //}
            //if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
            //    _connection.Execute(new JelAlignerReadHandler(this, ""));    

            return false;
        }

        protected override bool fStartLiftdown(object[] param)
        {
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {
                _doLifterPinUp.SetTrigger(false, out _);
                _doLifterPinHome.SetTrigger(true, out _);
                //_lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WD"));
                //_lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
            }
            return true;
        }
        protected override bool fMonitorLiftdown(object[] param)
        {
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitForAlignWafer))
                OnError("Liftdown timeout");
            if (_diLifterPinHome.Value && !_diLifterPinUp.Value)
            {
                IsBusy = false;
                _isAligned = false;
                _isOnHomedPostion = false;
                return true;
            }
            //if (_lstMoveHandler.Count == 0
            //    && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
            //    && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            //{
            //    IsBusy = false;
            //    _isAligned = true;
            //    _isOnHomedPostion = false;
            //    return true;
            //}
            //if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
            //    _connection.Execute(new JelAlignerReadHandler(this, ""));
            return false;
        }

        protected override bool fStartAlign(object[] param)
        {
            _dtActionStart = DateTime.Now;
            _doLifterPinUp.SetTrigger(false, out _);
            _doLifterPinHome.SetTrigger(true, out _);

            while (!_diLifterPinHome.Value || _diLifterPinUp.Value)
            {
                if (_diLifterPinError.Value)
                {
                    OnError("MotionError");
                    return false;
                }


                if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitForAlignWafer))
                {
                    OnError("Alignment timeout");
                    return false;
                }
            }

            WaferSize wz = WaferManager.Instance.GetWaferSize(RobotModuleName, 0);
            if (wz != GetCurrentWaferSize())
            {
                EV.PostAlarmLog("System", "Wafer size is not match, can't do alignment");
                return false;
            }

            double aligneangle = (double)param[0];
            while (aligneangle < 0 || aligneangle > 360)
            {
                if (aligneangle < 0)
                    aligneangle += 360;
                if (aligneangle > 360)
                    aligneangle -= 360;
            }
            //int speed = SC.GetValue<int>($"{_scRoot}.{Name}.AlignSpeed");

            int intangle = (int)(aligneangle * 20000 / 360);
            CurrentNotch = aligneangle;
            lock (_locker)
            {
                _lstMoveHandler.AddLast(new JelAlignerRawCommandHandler(this, $"${BodyNumber}RD\r"));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WUC"));   //Close grip
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                //_lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WA"));   //Move down
                //_lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));                
                _lstMoveHandler.AddLast(new JelAlignerSetHandler(this, "WOP", intangle.ToString()));
                _lstMoveHandler.AddLast(new JelAlignerRawCommandHandler(this, $"${BodyNumber}RD\r"));
                if (_isAligned)
                {
                    _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "G15"));    //Align to angle compaund command

                }
                else
                    _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WA"));    //Align
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WDF"));     //Ungrip
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, "WIS1"));
            }

            return true;
        }
        protected override bool fMonitorAligning(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitForAlignWafer))
                OnError("Alignment timeout");
            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
                && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            {

                _isAligned = true;
                _isOnHomedPostion = false;
                return true;
            }
            if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelAlignerReadHandler(this, ""));
            return false;
        }

        protected override bool fStop(object[] param)
        {
            return true;
        }

        protected override bool FsmAbort(object[] param)
        {
            return true;
        }
        protected override bool fClear(object[] param)
        {
            return true;
        }
        protected override bool fStartReadData(object[] param)
        {
            return true;
        }
        protected override bool fStartSetParameters(object[] param)
        {
            _dtActionStart = DateTime.Now;
            _currentSetParameter = param[0].ToString();
            switch (_currentSetParameter)
            {
                case "WaferSize":
                    if (GetWaferState() != RobotArmWaferStateEnum.Absent)
                    {
                        EV.PostAlarmLog("System", "Can't set wafersize when wafer is not absent");
                        return false;
                    }
                    _currentSetWaferSize = (WaferSize)param[1];

                    if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                    {
                        WaferManager.Instance.UpdateWaferSize(RobotModuleName, 0, _currentSetWaferSize);
                    }
                    string strpara;
                    switch (_currentSetWaferSize)
                    {
                        case WaferSize.WS2:
                        case WaferSize.WS3:
                        case WaferSize.WS4:
                        case WaferSize.WS5:
                        case WaferSize.WS6:
                        case WaferSize.WS8:
                            strpara = _currentSetWaferSize.ToString().Replace("WS", "");

                            break;
                        case WaferSize.WS12:
                            strpara = "9";

                            break;
                        default:
                            return false;
                    }
                    lock (_locker)
                    {
                        _lstMonitorHandler.AddLast(new JelAlignerMoveHandler(this, "WAS", strpara));
                        _lstMonitorHandler.AddLast(new JelAlignerReadHandler(this, "WAS"));
                        _lstMonitorHandler.AddLast(new JelAlignerMoveHandler(this, "WMC"));
                    }
                    break;
            }
            return true;
        }
        private string _currentSetParameter;

        public override WaferSize GetCurrentWaferSize()
        {
            return _currentSetWaferSize;
        }
        private WaferSize _currentSetWaferSize = WaferSize.WS8;
        protected override bool fMonitorSetParamter(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitForAlignWafer))
                OnError("Set parameter timeout");
            switch (_currentSetParameter)
            {
                case "WaferSize":
                    if (_lstMonitorHandler.Count == 0 && _lstMoveHandler.Count == 0 && !_connection.IsBusy)
                    {
                        if (_currentSetWaferSize != Size)
                        {
                            OnError($"Fail to set wafer size,set:{_currentSetWaferSize},return:{Size}");
                        }
                        if (_currentSetWaferSize == WaferSize.WS12)
                        {


                            return true;
                        }
                        if (_currentSetWaferSize == WaferSize.WS8)
                        {

                            return true;
                        }
                        return true;
                    }
                    break;
            }
            return false;
        }
        protected override bool fStartUnGrip(object[] param)
        {
            lock (_locker)
            {
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WDF"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
            }
            _diStartUngrip = DateTime.Now;
            return true;
        }
        private DateTime _diStartUngrip;
        protected override bool fMonitorUnGrip(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _diStartUngrip > TimeSpan.FromSeconds(10))
                OnError("UnGrip timeout");

            if (_lstMoveHandler.Count != 0 || _connection.IsBusy || XAxisStatus != JelAxisStatus.NormalEnd
                || YAxisAndThetaAxisStatus != JelAxisStatus.NormalEnd)
            {
                if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
                    _connection.Execute(new JelAlignerReadHandler(this, ""));
                return false;
            }

            _doLifterPinUp.SetTrigger(true, out _);
            _doLifterPinHome.SetTrigger(false, out _);

            if (_diLifterPinHome.Value || !_diLifterPinUp.Value)
                return false;

            return true;
        }
        protected override bool fStartGrip(object[] param)
        {
            lock (_locker)
            {
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WUC"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
            }
            _diStartUngrip = DateTime.Now;
            return true;
        }
        protected override bool fMonitorGrip(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _diStartUngrip > TimeSpan.FromSeconds(10))
                OnError("Grip timeout");

            _doLifterPinUp.SetTrigger(false, out _);
            _doLifterPinHome.SetTrigger(true, out _);

            if (!_diLifterPinHome.Value || _diLifterPinUp.Value)
                return false;

            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy)
            {
                if (XAxisStatus == JelAxisStatus.NormalEnd && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
                {

                    return true;
                }
                else
                {
                    _connection.Execute(new JelAlignerReadHandler(this, ""));
                }
            }
            return false;
        }
        protected override bool fResetToReady(object[] param)
        {
            return true;
        }

        protected override bool fError(object[] param)
        {
            return true;
        }

        public override void OnError(string errortext)
        {
            _isAligned = false;
            _isOnHomedPostion = false;
            _lstMonitorHandler.Clear();
            _lstMoveHandler.Clear();
            base.OnError(errortext);
        }

        public override bool IsNeedRelease
        {
            get
            {
                return true;
            }
        }

        protected override bool fStartPrepareAccept(object[] param)
        {
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {
                _lstMonitorHandler.AddLast(new JelAlignerRawCommandHandler(this, $"${BodyNumber}RD\r"));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WMC"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
            }
            return true;
        }


        protected override bool fMonitorPrepareAccept(object[] param)
        {
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds((double)TimelimitAlginerHome))
                OnError("Home timeout");
            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
                && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            {
                IsBusy = false;
                _isAligned = false;
                _isOnHomedPostion = true;
                return true;
            }
            if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelAlignerReadHandler(this, ""));

            return false;
        }




    }
}
