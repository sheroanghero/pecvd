using Aitex.Core.Common;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TazmoAligners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.TazmoAlignerIIs
{
    public class TazmoAlignerWithOcrCylinder: TazmoAlignerII
    {
         public TazmoAlignerWithOcrCylinder(string module,string name, string scRoot,IoSensor[] dis,IoTrigger[] dos, WaferSize[] wsizes):base(module,name,scRoot)
        {
            switch (wsizes[0])
            {
                case WaferSize.WS4:
                    _diOcrOn4Inch = dis[0];
                    _doOcrTo4Inch = dos[0];
                    break;
                case WaferSize.WS6:
                    _diOcrOn6Inch = dis[0];
                    _doOcrTo6Inch = dos[0];
                    break;
                case WaferSize.WS8:
                    _diOcrOn8Inch = dis[0];
                    _doOcrTo8Inch = dos[0];
                    break;
                case WaferSize.WS12:
                    _diOcrOn12Inch = dis[0];
                    _doOcrTo12Inch = dos[0];
                    break;
                default:
                    break;
            }

            switch (wsizes[1])
            {
                case WaferSize.WS4:
                    _diOcrOn4Inch = dis[1];
                    _doOcrTo4Inch = dos[1];
                    break;
                case WaferSize.WS6:
                    _diOcrOn6Inch = dis[1];
                    _doOcrTo6Inch = dos[1];
                    break;
                case WaferSize.WS8:
                    _diOcrOn8Inch = dis[1];
                    _doOcrTo8Inch = dos[1];
                    break;
                case WaferSize.WS12:
                    _diOcrOn12Inch = dis[1];
                    _doOcrTo12Inch = dos[1];
                    break;
                default:
                    break;
            }
            if (dis.Length > 2)
                _diWaferPresent = dis[2];

        }

        private IoSensor _diOcrOn6Inch;
        private IoSensor _diOcrOn8Inch;
        private IoSensor _diOcrOn12Inch;
        private IoSensor _diOcrOn4Inch;
        private IoSensor _diWaferPresent;

        private IoTrigger _doOcrTo4Inch;
        private IoTrigger _doOcrTo6Inch;
        private IoTrigger _doOcrTo8Inch;
        private IoTrigger _doOcrTo12Inch;

        private DateTime _dtActionStart;
        protected override bool fStartSetParameters(object[] param)
        {
            _dtActionStart = DateTime.Now;
            _currentSetParameter = param[0].ToString();
            switch (_currentSetParameter)
            {
                case "WaferSize":
                    //if (GetWaferState() != RobotArmWaferStateEnum.Absent)
                    //{
                    //    EV.PostAlarmLog("System", "Can't set wafersize when wafer is not absent");
                    //    return false;
                    //}
                    _currentSetWaferSize = (WaferSize)param[1];

                    if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                    {
                        WaferManager.Instance.UpdateWaferSize(RobotModuleName, 0, _currentSetWaferSize);
                    }

                    switch (_currentSetWaferSize)
                    {
                        case WaferSize.WS12:
                            if (_doOcrTo8Inch != null && _doOcrTo8Inch.DoTrigger != null)
                                _doOcrTo8Inch.SetTrigger(false, out _);
                            if (_doOcrTo6Inch != null && _doOcrTo6Inch.DoTrigger != null)
                                _doOcrTo6Inch.SetTrigger(false, out _);
                            if (_doOcrTo12Inch != null && _doOcrTo12Inch.DoTrigger != null)
                                _doOcrTo12Inch.SetTrigger(true, out _);
                            if (_doOcrTo4Inch != null && _doOcrTo4Inch.DoTrigger != null)
                                _doOcrTo4Inch.SetTrigger(false, out _);
                          
                            break;

                        case WaferSize.WS8:
                            if (_doOcrTo8Inch != null && _doOcrTo8Inch.DoTrigger != null)
                                _doOcrTo8Inch.SetTrigger(true, out _);
                            if (_doOcrTo6Inch != null && _doOcrTo6Inch.DoTrigger != null)
                                _doOcrTo6Inch.SetTrigger(false, out _);
                            if (_doOcrTo12Inch != null && _doOcrTo12Inch.DoTrigger != null)
                                _doOcrTo12Inch.SetTrigger(false, out _);
                            if (_doOcrTo4Inch != null && _doOcrTo4Inch.DoTrigger != null)
                                _doOcrTo4Inch.SetTrigger(false, out _);

                           
                            break;
                        case WaferSize.WS2:
                        case WaferSize.WS3:
                        case WaferSize.WS4:
                            if (_doOcrTo8Inch != null && _doOcrTo8Inch.DoTrigger != null)
                                _doOcrTo8Inch.SetTrigger(false, out _);
                            if (_doOcrTo6Inch != null && _doOcrTo6Inch.DoTrigger != null)
                                _doOcrTo6Inch.SetTrigger(false, out _);
                            if (_doOcrTo12Inch != null && _doOcrTo12Inch.DoTrigger != null)
                                _doOcrTo12Inch.SetTrigger(false, out _);
                            if (_doOcrTo4Inch != null && _doOcrTo4Inch.DoTrigger != null)
                                _doOcrTo4Inch.SetTrigger(true, out _);
                           
                            break;

                        case WaferSize.WS5:
                        case WaferSize.WS6:
                        case WaferSize.WS7:
                            if (_doOcrTo4Inch != null && _doOcrTo4Inch.DoTrigger != null)
                                _doOcrTo4Inch.SetTrigger(false, out _);
                            if (_doOcrTo6Inch != null && _doOcrTo6Inch.DoTrigger != null)
                                _doOcrTo6Inch.SetTrigger(true, out _);
                            if (_doOcrTo8Inch != null && _doOcrTo8Inch.DoTrigger != null)
                                _doOcrTo8Inch.SetTrigger(false, out _);
                            if (_doOcrTo12Inch != null && _doOcrTo12Inch.DoTrigger != null)
                                _doOcrTo12Inch.SetTrigger(false, out _);
                           
                            break;
                        default:
                            break;
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
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitAlignWafer))
                OnError("Set parameter timeout");

            switch (_currentSetParameter)
            {
                case "WaferSize":
                    switch (_currentSetWaferSize)
                    {
                        case WaferSize.WS6:
                            if (_diOcrOn4Inch != null && _diOcrOn4Inch.SensorDI != null && _diOcrOn4Inch.Value)
                                return false;
                            if (_diOcrOn6Inch != null && _diOcrOn6Inch.SensorDI != null && !_diOcrOn6Inch.Value)
                                return false;
                            if (_diOcrOn8Inch != null && _diOcrOn8Inch.SensorDI != null && _diOcrOn8Inch.Value)
                                return false;
                            if (_diOcrOn12Inch != null && _diOcrOn12Inch.SensorDI != null && _diOcrOn12Inch.Value)
                                return false;
                            break;
                        case WaferSize.WS8:
                            if (_diOcrOn4Inch != null && _diOcrOn4Inch.SensorDI != null && _diOcrOn4Inch.Value)
                                return false;
                            if (_diOcrOn6Inch != null && _diOcrOn6Inch.SensorDI != null && _diOcrOn6Inch.Value)
                                return false;
                            if (_diOcrOn8Inch != null && _diOcrOn8Inch.SensorDI != null && !_diOcrOn8Inch.Value)
                                return false;
                            if (_diOcrOn12Inch != null && _diOcrOn12Inch.SensorDI != null && _diOcrOn12Inch.Value)
                                return false;
                            break;
                        case WaferSize.WS4:
                            if (_diOcrOn4Inch != null && _diOcrOn4Inch.SensorDI != null && !_diOcrOn4Inch.Value)
                                return false;
                            if (_diOcrOn6Inch != null && _diOcrOn6Inch.SensorDI != null && _diOcrOn6Inch.Value)
                                return false;
                            if (_diOcrOn8Inch != null && _diOcrOn8Inch.SensorDI != null && _diOcrOn8Inch.Value)
                                return false;
                            if (_diOcrOn12Inch != null && _diOcrOn12Inch.SensorDI != null && _diOcrOn12Inch.Value)
                                return false;
                            break;
                        case WaferSize.WS12:
                            if (_diOcrOn4Inch != null && _diOcrOn4Inch.SensorDI != null && _diOcrOn4Inch.Value)
                                return false;
                            if (_diOcrOn6Inch != null && _diOcrOn6Inch.SensorDI != null && _diOcrOn6Inch.Value)
                                return false;
                            if (_diOcrOn8Inch != null && _diOcrOn8Inch.SensorDI != null && _diOcrOn8Inch.Value)
                                return false;
                            if (_diOcrOn12Inch != null && _diOcrOn12Inch.SensorDI != null && !_diOcrOn12Inch.Value)
                                return false;
                            break;
                        default:

                            return true;
                    }
                    WaferManager.Instance.UpdateWaferSize(RobotModuleName, 0, _currentSetWaferSize);
                    return true;
                default:
                    return true;
            }
        }


        public override bool IsNeedChangeWaferSize(WaferSize wz)
        {
            switch(wz)
            {
                case WaferSize.WS12:
                    if (_diOcrOn12Inch != null && _diOcrOn12Inch.Value && _doOcrTo12Inch != null && _doOcrTo12Inch.Value)
                        return false;
                    break;
                case WaferSize.WS8:
                    if (_diOcrOn8Inch != null && _diOcrOn8Inch.Value && _doOcrTo8Inch != null && _doOcrTo8Inch.Value)
                        return false;
                    break;

            }
            return true;
        }

        public override WaferSize GetCurrentWaferSize()
        {
            if (_diOcrOn12Inch != null && _diOcrOn12Inch.Value && _doOcrTo12Inch != null && _doOcrTo12Inch.Value)
                return  WaferSize.WS12;
            if (_diOcrOn8Inch != null && _diOcrOn8Inch.Value && _doOcrTo8Inch != null && _doOcrTo8Inch.Value)
                return WaferSize.WS8;
            return WaferSize.WS0;
        }



        public override bool IsNeedPrepareBeforePlaceWafer()
        {
            if (AligneTimesNoHome >= IntervalToHome)
                return true;
            return false;
        }

        private int IntervalToHome
        {
            get
            {
                if (SC.ContainsItem($"Aligner.{Name}.IntervalToHome"))
                    return SC.GetValue<int>($"Aligner.{Name}.IntervalToHome");
                return 0;
            }
        }


        protected override bool fStartAlign(object[] param)
        {
            _dtActionStart = DateTime.Now;
            lock (_locker)
            {
                double aligneangle = (double)param[0];
                int anglevalue = (int)aligneangle * 10;
                IsNeedRelease = false;
                string para1 = $"1," + anglevalue.ToString("0000") + ",000";
                var wafer = WaferManager.Instance.GetWafer(RobotModuleName, 0);
                if (IsPreAlgin && !wafer.IsEmpty && wafer.Size == WaferSize.WS8)
                {
                    if (SC.ContainsItem($"Aligner.{RobotModuleName}.PreAlignDistanceWS8"))
                    {
                        int nDistance = SC.GetValue<int>($"Aligner.{RobotModuleName}.PreAlignDistanceWS8");
                        para1 = $"1," + anglevalue.ToString("0000") + $",{nDistance.ToString("D3")}";
                        IsNeedRelease = true;
                    }
                }
             

                
                if (_tazmotype == AlignerType.Mechnical)
                {
                    anglevalue = (int)aligneangle * 100;
                    para1 = $"1," + anglevalue.ToString("00000");
                }
                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.SetalignmentangleetcSet, para1));
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.SeriesofalignmentMotion, "1,5"));

                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.OpenalignerchuckMotion, null));


                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));

            }
            return true;
        }
        protected override bool fMonitorAligning(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitAlignerHome))
                OnError("Aligning timeout");

            if (_lstHandler.Count != 0 || _connection.IsBusy) return false;
            
            EV.PostInfoLog("Aligner", $"{RobotModuleName} aligning complete.");
            AligneTimesNoHome++;
            return true;
        }

        protected override bool fStartUnGrip(object[] param)
        {
            lock (_locker)
            {

                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.OpenalignerchuckMotion, null));
                _lstHandler.AddLast(new TwinTransactionHandler(this, TazmoCommand.MoveTheAlignerChuckToSpecifiedPosition, _defaultChuckPosition.ToString()));

                _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.RequeststatusStatus, null));
                if (_tazmotype == AlignerType.Mechnical)
                    _lstHandler.AddLast(new SingleTransactionHandler(this, TazmoCommand.Requeststatus2Status, null));
            }
            EV.PostInfoLog("Aligner", $"{RobotModuleName} start to ungrip wafer.");
            _dtActionStart = DateTime.Now;
            return true;
        }
        protected override bool fMonitorUnGrip(object[] param)
        {
            IsBusy = false;
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitAlignerHome))
                OnError("UnGrip timeout");

            if (_lstHandler.Count != 0 || _connection.IsBusy) return false;
            IsNeedRelease = false;
            EV.PostInfoLog("Aligner", $"{RobotModuleName} ungrip complete.");
            return true;
        }

    }
}
