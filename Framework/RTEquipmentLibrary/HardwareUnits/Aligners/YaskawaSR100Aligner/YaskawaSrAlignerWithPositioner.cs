using Aitex.Core.Common;
using Aitex.Core.RT.Device.Unit;
using MECF.Framework.Common.SubstrateTrackings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.YaskawaSR100Aligner
{
    public class YaskawaSrAlignerWithPositioner : YaskawaAligner.YaskawaAligner
    {
        public YaskawaSrAlignerWithPositioner(string module, string name, string scRoot, IoSensor[] dis, IoTrigger[] dos, int alignerType = 0)
            :base(module,name,scRoot,dis,dos,alignerType)
        {
            _diAlignerOnWS8 = dis[4];
            _diAlginerOnWS12 = dis[5];
            _diOcrOnWS8 = dis[6];
            _diOcrOnWS12 = dis[7];

            _doAlignerToWS8 = dos[1];
            _doAlignerToWS12 = dos[2];
            _doOcrToWS8 = dos[3];
            _doOcrToWS12 = dos[4];
        }
        
        private IoSensor _diAlignerOnWS8;
        private IoSensor _diAlginerOnWS12;

        private IoSensor _diOcrOnWS8;
        private IoSensor _diOcrOnWS12;

        private IoTrigger _doAlignerToWS8;
        private IoTrigger _doAlignerToWS12;

        private IoTrigger _doOcrToWS8;
        private IoTrigger _doOcrToWS12;

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
                            _doAlignerToWS12.SetTrigger(true, out _);
                            _doAlignerToWS8.SetTrigger(false, out _);

                            _doOcrToWS12.SetTrigger(true, out _);
                            _doOcrToWS8.SetTrigger(false, out _);
                            break;

                        case WaferSize.WS8:
                            _doAlignerToWS12.SetTrigger(false, out _);
                            _doAlignerToWS8.SetTrigger(true, out _);
                            _doOcrToWS12.SetTrigger(false, out _);
                            _doOcrToWS8.SetTrigger(true, out _);
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
            if (DateTime.Now - _dtActionStart > TimeSpan.FromSeconds(TimeLimitAlignerHome))
                OnError("Set parameter timeout");

            switch (_currentSetParameter)
            {
                case "WaferSize":
                    switch (_currentSetWaferSize)
                    {
                       
                        case WaferSize.WS8:
                            return !_diAlginerOnWS12.Value && _diAlignerOnWS8.Value &&
                                !_diOcrOnWS12.Value && _diOcrOnWS8.Value;
                            
                      
                        case WaferSize.WS12:
                            return _diAlginerOnWS12.Value && !_diAlignerOnWS8.Value &&
                                _diOcrOnWS12.Value && !_diOcrOnWS8.Value; 
                            
                        default:
                            return true;
                    }
                    
                default:
                    return true;
            }
        }
        public override bool IsNeedChangeWaferSize(WaferSize wz)
        {
            switch(wz)
            {
                case WaferSize.WS12:
                    return !(_diAlginerOnWS12.Value && !_diAlignerOnWS8.Value &&
                                _diOcrOnWS12.Value && !_diOcrOnWS8.Value && 
                                _doAlignerToWS12.Value && !_doAlignerToWS8.Value && 
                                _doOcrToWS12.Value && !_doOcrToWS8.Value);
                        
                case WaferSize.WS8:
                    return !(!_diAlginerOnWS12.Value && _diAlignerOnWS8.Value &&
                                !_diOcrOnWS12.Value && _diOcrOnWS8.Value &&
                                !_doOcrToWS12.Value && _doOcrToWS8.Value &&
                                !_doAlignerToWS12.Value && _doAlignerToWS8.Value);
                default:
                    return true;
            }
        }

        public override WaferSize GetCurrentWaferSize()
        {
            if (WaferManager.Instance.CheckHasWafer(RobotModuleName, 0))
                return WaferManager.Instance.GetWaferSize(RobotModuleName, 0);

            if (_diAlginerOnWS12.Value && !_diAlignerOnWS8.Value &&
                                _diOcrOnWS12.Value && !_diOcrOnWS8.Value &&
                                _doAlignerToWS12.Value && !_doAlignerToWS8.Value &&
                                _doOcrToWS12.Value && !_doOcrToWS8.Value)
                return WaferSize.WS12;
            if (!_diAlginerOnWS12.Value && _diAlignerOnWS8.Value &&
                                !_diOcrOnWS12.Value && _diOcrOnWS8.Value &&
                                !_doOcrToWS12.Value && _doOcrToWS8.Value &&
                                !_doAlignerToWS12.Value && _doAlignerToWS8.Value)
                return WaferSize.WS8;
            return WaferSize.WS0;
        }

    }
}
