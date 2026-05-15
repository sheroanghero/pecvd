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
    public class JelAlignerEG : JelAligner, IConnection
    {
        /* SAL3262HV model: for 2-inch to 6-inch wafer
           SAL3362HV model: for 3-inch to 6-inch wafer
           SAL3482HV model: for 4-inch to 8-inch wafer
           SAL38C3HV model: for 8-inch to 12-inch wafer*/
        public JelAlignerEG(string module, string name, string scRoot, IoSensor[] dis, IoTrigger[] dos, int alignerType = 0, string robotModel = "") : 
            base(module, name,scRoot,dis,dos,alignerType,robotModel)
        {
            isSimulatorMode = SC.ContainsItem("System.IsSimulatorMode") ? SC.GetValue<bool>("System.IsSimulatorMode") : false;
            _robotModel = robotModel;
            _scRoot = scRoot;
            if(dis!=null && dis.Length >0)
                _diWaferPresent = dis[0];
           

            //ResetPropertiesAndResponses();
            //RegisterSpecialData();
            //_enableLog = SC.GetValue<bool>($"{_scRoot}.{Name}.EnableLogMessage");
            //_bodyNumber = SC.GetValue<int>($"{_scRoot}.{Name}.BodyNumber");
            //PortName = SC.GetStringValue($"{_scRoot}.{Name}.PortName");
            //TimelimitAlginerHome = SC.GetValue<int>($"{_scRoot}.{Name}.TimeLimitAlignerHome");
            //TimelimitForAlignWafer = SC.GetValue<int>($"{_scRoot}.{Name}.TimeLimitForAlignWafer");

            //_connection = new JelAlignerConnection(PortName);
            //_connection.EnableLog(_enableLog);
            //if (_connection.Connect())
            //{

            //    EV.PostInfoLog(Module, $"{Module}.{Name} connected");
            //}
            //_thread = new PeriodicJob(100, OnTimer, $"{Module}.{Name} MonitorHandler", true);
        }


        private IoSensor _diWaferPresent;
        

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

        private void ResetPropertiesAndResponses()
        {

        }
        private void RegisterSpecialData()
        {
            DATA.Subscribe($"{Module}.{Name}.CurrentReadRoutine", () => CurrentReadRoutine);
            DATA.Subscribe($"{Module}.{Name}.CurrentReadSpeedData", () => CurrentReadSpeedData);
            DATA.Subscribe($"{Module}.{Name}.CurrentReadAData", () => CurrentReadAData);
            DATA.Subscribe($"{Module}.{Name}.IsLeftArmPressureSensorON", () => IsLeftArmPressureSensorON);
            DATA.Subscribe($"{Module}.{Name}.IsRightArmPressureSensorON", () => IsRightArmPressureSensorON);
            DATA.Subscribe($"{Module}.{Name}.YAxisAndThetaAxisStatus", () => YAxisAndThetaAxisStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.XAxisStatus", () => XAxisStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.CurrentCompoundCommandStatus", () => CurrentCompoundCommandStatus.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadCassetNumber", () => ReadCassetNumber.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadSlotNumber", () => ReadSlotNumber.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadBankNumber", () => ReadBankNumber.ToString());



            DATA.Subscribe($"{Module}.{Name}.ReadPosLeftArmPostion", () => ReadPosLeftArmPostion);
            DATA.Subscribe($"{Module}.{Name}.ReadPosRightArmPostion", () => ReadPosRightArmPostion.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadPosZAxisPostion", () => ReadPosZAxisPostion.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadPosThetaAxisPostion", () => ReadPosThetaAxisPostion.ToString());

            DATA.Subscribe($"{Module}.{Name}.ReadParameterMax", () => ReadParameterMax.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadParameterMin", () => ReadParameterMin.ToString());
            DATA.Subscribe($"{Module}.{Name}.ReadParameterValue", () => ReadParameterValue.ToString());

            DATA.Subscribe($"{Module}.{Name}.MappingFirstSlotPosition", () => MappingFirstSlotPosition.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingGateWidth", () => MappingGateWidth.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingMaxDetectWidth", () => MappingMaxDetectWidth.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingMinDetectWidth", () => MappingMinDetectWidth.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingSlotsNumber", () => MappingSlotsNumber.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingSpeed", () => MappingSpeed.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingStopPostion", () => MappingStopPostion.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingTopSlotPostion", () => MappingTopSlotPostion.ToString());
            DATA.Subscribe($"{Module}.{Name}.IsMappingSensorON", () => IsMappingSensorON.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingWaferResult", () => MappingWaferResult.ToString());
            DATA.Subscribe($"{Module}.{Name}.MappingWidthResult", () => MappingWidthResult.ToString());




        }

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

       







        #region ParseHandler
        
        private void ParseRobotStatus(string data)
        {
            if (data.Length < 2) return;
            YAxisAndThetaAxisStatus = (JelAxisStatus)Convert.ToInt16(data.ToArray()[0].ToString());
            XAxisStatus = (JelAxisStatus)Convert.ToInt16(data.ToArray()[1].ToString());
            if (YAxisAndThetaAxisStatus == JelAxisStatus.SensorError || YAxisAndThetaAxisStatus == JelAxisStatus.CommandError)
            {
                IsBusy = false;
                OnError($"YAxisAndThetaAxisStatus is {YAxisAndThetaAxisStatus}");
            }
            if (XAxisStatus == JelAxisStatus.SensorError || XAxisStatus == JelAxisStatus.CommandError)
            {
                IsBusy = false;
                OnError($"XAxisStatus is {XAxisStatus}");
            }


        }
        private void ParseRobotPostion(string axis, string data)
        {
            float _floatvalue;
            if (!float.TryParse(data, out _floatvalue)) return;
            if (axis == "1")
            {
                RightArmPostion = _floatvalue;
                PositionAxis1 = _floatvalue;
            }
            if (axis == "2")
            {
                ThetaAxisPostion = _floatvalue;
                PositionAxis2 = _floatvalue;
            }
            if (axis == "3")
            {
                LeftArmPostion = _floatvalue;
                PositionAxis3 = _floatvalue;
            }
            if (axis == "4")
            {
                ZAxisPostion = _floatvalue;
                PositionAxis4 = _floatvalue;
            }
        }

        #endregion



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
                //TimelimitForAlignWafer = SC.GetValue<int>($"{_scRoot}.{Name}.TimeLimitForAlignWafer");

                _connection = new JelAlignerConnection(PortName);
                
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
            }
            return true;
        }

        protected override bool fMonitorReset(object[] param)
        {
            _isAligned = false;
            return true;
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
                int alignspeed = AlginerSpeedSetPoint * 8191 / 100;
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "W0"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerSetHandler(this, "WSP", alignspeed.ToString()));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WU"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WUC"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WT"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
            }
            _isAligned = false;
            _isOnHomedPostion = false;
            _dtActionStart = DateTime.Now;
            return true;


        }
        public override WaferSize GetCurrentWaferSize()
        {
            return  WaferSize.WS12;
        }
        private DateTime _dtActionStart;
        protected override bool fMonitorInit(object[] param)
        {
            _isAligned = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds((double)TimelimitAlginerHome))
                OnError("Init timeout");
            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
                && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            {
                IsBusy = false;
                _isOnHomedPostion = true;
                if (IsWaferPresent(0) && WaferManager.Instance.CheckNoWafer(RobotModuleName, 0))
                {
                    WaferManager.Instance.CreateWafer(RobotModuleName, 0,
                         WaferStatus.Normal, GetCurrentWaferSize());
                }
                if (!IsWaferPresent(0) && WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                {
                    EV.PostAlarmLog("System", $"There's no phisical wafer on {RobotModuleName},but it has wafer information on.");
                    //WaferManager.Instance.DeleteWafer(RobotModuleName, 0);
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
                int alignspeed = AlginerSpeedSetPoint * 8191 / 100;
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "W0"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerSetHandler(this, "WSP", alignspeed.ToString()));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WU"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WUC"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WT"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));

            }
            _isAligned = false;

            _dtActionStart = DateTime.Now;
            return true;
        }

        protected override bool fMonitorHome(object[] param)
        {
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds((double)TimelimitAlginerHome))
                OnError("Home timeout");
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

                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WU"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
            }
            return true;
        }

        protected override bool fMonitorLiftup(object[] param)
        {
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitForAlignWafer))
                OnError("Aligner lift up timeout");

            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
                && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            {
                IsBusy = false;
                _isAligned = false;
                _isOnHomedPostion = false;
                return true;
            }
            if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelAlignerReadHandler(this, ""));

            return false;
        }

        protected override bool fStartLiftdown(object[] param)
        {
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WD"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
            }
            return true;
        }
        protected override bool fMonitorLiftdown(object[] param)
        {
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitForAlignWafer))
                OnError("Aligner lift down timeout");

            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
                && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            {
                IsBusy = false;
                _isAligned = true;
                _isOnHomedPostion = false;
                return true;
            }
            if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelAlignerReadHandler(this, ""));
            return false;
        }

        protected override bool fStartAlign(object[] param)
        {
            //WaferSize wz = WaferManager.Instance.GetWaferSize(RobotModuleName, 0);
            //if (wz != GetCurrentWaferSize())
            //{
            //    EV.PostAlarmLog("System", "Wafer size is not match, can't do alignment");
            //    return false;
            //}
            _isOnHomedPostion = false;

            double aligneangle = (double)param[0];

            while (aligneangle < 0 || aligneangle > 360)
            {
                if (aligneangle < 0)
                    aligneangle += 360;
                if (aligneangle > 360)
                    aligneangle -= 360;
            }
            //int speed = SC.GetValue<int>($"{_scRoot}.{Name}.AlignSpeed");

            int intangle = (int)(aligneangle * 76799 / 360);
            CurrentNotch = aligneangle;
            lock (_locker)
            {
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WC"));   //Close grip
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WD"));    //Align
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                //_lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WA"));   //Move down
                //_lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));                
                _lstMoveHandler.AddLast(new JelAlignerSetHandler(this, "WOP", intangle.ToString()));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WOF"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WU"));     //Move Up
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WUC"));     //Ungrip
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WT"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
            }
            _dtActionStart = DateTime.Now;
            return true;
        }
        protected override bool fMonitorAligning(object[] param)
        {
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimelimitForAlignWafer))
                OnError("Alignment timeout");
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
                    }
                    break;
            }
            return true;
        }
        private string _currentSetParameter;
        private WaferSize _currentSetWaferSize = WaferSize.WS0;
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
            if (DateTime.Now - _diStartUngrip > TimeSpan.FromSeconds(10))
                OnError("UnGrip timeout");
            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy && XAxisStatus == JelAxisStatus.NormalEnd
                && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
            {
                IsBusy = false;
                return true;
            }
            if (_lstMoveHandler.Count == 0 && !_connection.IsBusy)
                _connection.Execute(new JelAlignerReadHandler(this, ""));
            return false;
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
            if (DateTime.Now - _diStartUngrip > TimeSpan.FromSeconds(10))
                OnError("Grip timeout");
            if (_lstMoveHandler.Count == 0
                && !_connection.IsBusy)
            {
                if (XAxisStatus == JelAxisStatus.NormalEnd && YAxisAndThetaAxisStatus == JelAxisStatus.NormalEnd)
                {
                    IsBusy = false;
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
            get => !_isOnHomedPostion;
        }

        protected override bool fStartPrepareAccept(object[] param)
        {
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WU"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WUC"));
                _lstMoveHandler.AddLast(new JelAlignerReadHandler(this, ""));
                _lstMoveHandler.AddLast(new JelAlignerMoveHandler(this, "WT"));
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
