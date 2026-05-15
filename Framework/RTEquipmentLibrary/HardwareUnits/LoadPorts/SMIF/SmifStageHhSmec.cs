using Aitex.Core.Common;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.SMIF
{
    public class SmifStageHhSmec:BrooksSmifPort
    {
        public SmifStageHhSmec(string module, string name, string scRoot, RobotBaseDevice robot,IoSensor[] dis,IoTrigger[] dos) 
            : base(module, name, scRoot,robot)
        {
            _diSensorStageMode = dis[0];
            _diSmifError = dis[1];
            _diAdapterPlacementA = dis[2];
            _diAdapterPlacementB = dis[3];
            _diAdapterPresence = dis[4];
            _diWaferProtrude = dis[5];

            _doSmifReset = dos[0];

            _thread = new PeriodicJob(50, OnTimerMonitor, $"{Module}.{Name} MonitorHandler", true);

            _diSensorStageMode.OnSignalChanged += _diSensorStageMode_OnSignalChanged;



        }

        private void _diSensorStageMode_OnSignalChanged(IoSensor arg1, bool arg2)
        {
            _isLoaded = false;
            _isMapped = false;
            SetPresent(false);
            SetPlaced(false);
            _isLoaded = false;

            DockState = FoupDockState.Undocked;
            DoorState = FoupDoorState.Close;
            DoorPosition = FoupDoorPostionEnum.Up;
        }

        private IoSensor _diSensorStageMode;
        private IoSensor _diSmifError;
        private IoSensor _diAdapterPlacementA;
        private IoSensor _diAdapterPlacementB;
        private IoSensor _diAdapterPresence;
        private IoSensor _diWaferProtrude;

        private IoTrigger _doSmifReset;

        private PeriodicJob _thread;

        private bool _isStageMode => _diSensorStageMode.Value;


        public override int ValidEndSlotIndex
        {
            get
            {
                return ValidStartSlotIndex + ValidSlotsNumber - 1;
            }
        }

        private bool OnTimerMonitor()
        {
            if (!_isStageMode) 
                return true;
            if (CassetteState == LoadportCassetteState.Normal)
            {
                SetPresent(true);
                SetPlaced(true);

                DockState = FoupDockState.Docked;
                DoorState = FoupDoorState.Open;
                DoorPosition = FoupDoorPostionEnum.Down;


            }

            if (CassetteState == LoadportCassetteState.Absent)
            {
                SetPresent(false);
                SetPlaced(false);
                _isLoaded = false;

                DockState = FoupDockState.Undocked;
                DoorState = FoupDoorState.Close;
                DoorPosition = FoupDoorPostionEnum.Up;
            }

            return true;
        }

        public override bool IsMapWaferByLoadPort => !_isStageMode;

        public override WaferSize GetCurrentWaferSize()
        {
            if (!_isStageMode) return WaferSize.WS8;

            if (LPModuleName == ModuleName.LP1 || LPModuleName == ModuleName.LP2)
                return WaferSize.WS6;
            if (LPModuleName == ModuleName.LP3 || LPModuleName == ModuleName.LP4)
                return WaferSize.WS7;

            return base.GetCurrentWaferSize();
        }


        public override int InfoPadCarrierIndex 
        { 
            get
            {
                if (!_isStageMode) return 8;

                if (LPModuleName == ModuleName.LP1 || LPModuleName == ModuleName.LP2)
                    return 6;
                if (LPModuleName == ModuleName.LP3 || LPModuleName == ModuleName.LP4)
                    return 7;
                return base.InfoPadCarrierIndex;
            }

        }
        public override bool IsEnableMapWafer(out string reason)
        {
            reason = "";
            if(_isStageMode)
            {
                if(_diWaferProtrude.Value)
                {
                    reason = "WaferProtrude";
                    return false;
                }
                if(!_isLoaded)
                {
                    reason = "NotLoaded";
                    return false;
                }
                return true;
            }

            return base.IsEnableMapWafer(out reason);
        }

        public override bool IsEnableTransferWafer(out string reason)
        {
            reason = "";
            if (_isStageMode)
            {
                if (_diWaferProtrude.Value)
                {
                    reason = "WaferProtrude";
                    return false;
                }
            }

            return base.IsEnableTransferWafer(out reason);
        }

        public override LoadportCassetteState CassetteState
        {
            get
            {
                if(_isStageMode)
                {
                    if (_diAdapterPlacementA.Value && _diAdapterPlacementB.Value && _diAdapterPresence.Value)
                        return LoadportCassetteState.Normal;
                    if (!_diAdapterPlacementA.Value && !_diAdapterPlacementB.Value && !_diAdapterPresence.Value)
                        return LoadportCassetteState.Absent;
                    return LoadportCassetteState.Unknown;
                }
                else 
                    return base.CassetteState;
            }
        }

        public override bool ReadCarrierIDByIndex(int offset = 0, int length = 16, int index = 0)
        {
            if (_isStageMode &&  CIDReaders[0] != null)
            {
                return base.ReadCarrierIDByIndex(offset, length, index);
            }

            ReadSmifSmartTag();

            return true;
        }

        public override bool WriteCarrierIDByIndex(string cid, int offset = 0, int length = 16, int index = 0)
        {
            if (!_isStageMode)
            {
                return base.WriteCarrierIDByIndex(cid,offset, length, index);
            }
            if (CIDReaders == null || CIDReaders.Length <= index) 
                return false;

            _carrierIdToBeWrite = cid;
            return CIDReaders[index].WriteCarrierID(offset, length, cid);
        }

        protected override bool fStartInit(object[] param)
        {
            if(!_isStageMode)
                return base.fStartInit(param);
            return true;
        }
        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            if (!_isStageMode)
                return base.fStartInit(param);
            return true;
        }

        protected override bool fStartReset(object[] param)
        {
            if (!_isStageMode)
            {
                _doSmifReset.SetTrigger(true, out _);
                Thread.Sleep(500);
                _doSmifReset.SetTrigger(false, out _);
                return base.fStartInit(param);
            }

            return true;
        }

        protected override bool fMonitorReset(object[] param)
        {
            IsBusy = false;
            if (!_isStageMode)
                return base.fMonitorReset(param);
            return true;
        }

        protected override bool fStartHomet(object[] param)
        {
            if (!_isStageMode)
                return base.fStartHomet(param);
            return true;
        }
        protected override bool fMonitorHome(object[] param)
        {
            IsBusy = false;
            if (!_isStageMode)
                return base.fMonitorHome(param);
            return true;
        }

        protected override bool fStartLoad(object[] param)
        {
            if (!_isStageMode)
                return base.fStartLoad(param);
            return true;
        }

        protected override bool fMonitorLoad(object[] param)
        {
            IsBusy = false;
            if (!_isStageMode)
                return base.fMonitorLoad(param);
            _isLoaded = true;
            OnLoaded();
            return true;
        }

        protected override bool fStartUnload(object[] param)
        {
            if (!_isStageMode)
                return base.fStartUnload(param);
            
            return true;
        }

        protected override bool fMonitorUnload(object[] param)
        {
            IsBusy = false;
            if (!_isStageMode)
                return base.fMonitorUnload(param);
            _isLoaded = false;
            OnUnloaded();            
            return true;
        }

        public override bool MapWafer(out string reason)
        {
            if (_isStageMode)
            {
                if (MapRobot == null)
                {
                    reason = "No mapping tools";
                    return false;
                }
                if (!MapRobot.IsReady())
                {
                    reason = "Robot not ready";
                    return false;
                }
                _isLoaded = true;
                if (!IsEnableMapWafer(out reason))
                {
                    EV.PostAlarmLog("System", $"{LPModuleName} with carrier:{CarrierId} is not ready:{reason}.");
                    return false;
                }
                bool ret = MapRobot.WaferMapping(LPModuleName, out reason);
               
                return ret;

            }

            return base.MapWafer(out reason);

        }

    }
}
