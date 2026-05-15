using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems.Rorzes
{
    public interface IRorzeEfemController : IEfemController
    {
        void SetLoadPortCallback(ModuleName module, IEfemLoadPortCallback lpCallback);
        void SetRobotCallback(ModuleName module, IEfemRobotCallback robotCallback);
        void SetAlignerCallback(ModuleName module, IEfemAlignerCallback alignerCallback);
        void SetSignalTowerCallback(ModuleName module, IEfemSignalTowerCallback signalTowerCallback);
        void SetBufferCallback(ModuleName module, IEfemBufferCallback bufferCallback);
    }

    public class RorzeEfem : BaseDevice, IDevice, IConnectionContext, IRorzeEfemController
    {
        #region Connection Config
        public virtual bool IsEnabled => true;

        public int RetryConnectIntervalMs => 500;

        public int MaxRetryConnectCount => 3;

        public bool EnableCheckConnection => true;

        private string _address = "127.0.0.1:13000";
        public string Address => _address;

        public bool IsAscii => true;

        public string NewLine => "\r";

        public bool EnableLog => true;

        #endregion

        private R_TRIG _trigError = new R_TRIG();
        private R_TRIG _trigWarningMessage = new R_TRIG();

        protected LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        protected LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

        private object _locker = new object();

        private string _scRoot;

        protected IEfemAlignerCallback _aligner;
        protected IEfemRobotCallback _robot;
        protected IEfemSignalTowerCallback _signalTower;
        protected IEfemFfuCallback _ffu;
        protected IEfemSystemCallback _system;

        protected Dictionary<ModuleName, IEfemLoadPortCallback> _lpCallback = new Dictionary<ModuleName, IEfemLoadPortCallback>();

        public RorzeEfemConnection Connection
        {
            get
            {
                return _connection;
            }
        }

        private RorzeEfemConnection _connection;


        public event Action<string, EventLevel, string> AlarmGenerated;

        public bool IsInitialized { get; set; }

        public bool IsCommunicationReady { get; set; }

        public RorzeEfem(string module, string name, string scRoot) : base(module, name, name, name)
        {
            _scRoot = scRoot;            
        }


        public virtual bool Initialize()
        {
            if (!SC.GetValue<bool>("System.IsSimulatorMode"))
            {
                if (SC.ContainsItem($"{Name}.Address"))
                    _address = SC.GetStringValue($"{Name}.Address");
            }
            _connection = new RorzeEfemConnection(this);

            _connection.OnDisconnected += OnDisconnected;
            _connection.OnConnected += OnConnected;
            _connection.OnError += OnError;
            _connection.Name = $"{Module}.{Name}";

            _connection.Initialize();

            _connection.AddEventHandler(RorzeEfemBasicMessage.MAPDT, new RorzeEfemHandlerMapdt(this, ModuleName.System, true));
            _connection.AddEventHandler(RorzeEfemBasicMessage.SIGSTAT, new RorzeEfemHandlerSigStat(this, ModuleName.System));
            _connection.AddEventHandler(RorzeEfemBasicMessage.TRANSREQ, new RorzeEfemHandlerTransReq(this, ModuleName.System));
            _connection.AddEventHandler(RorzeEfemBasicMessage.READY, new RorzeEfemHandlerReady(this, ModuleName.System));

            DATA.Subscribe($"{Module}.{Name}.Status", () => _connection.StringFsmStatus);


            return true;
        }

        void IEfemController.Initialize()
        {
            Init();
        }

        public virtual void Init()
        {
            if (_connection == null || !_connection.IsConnected)
            {
                EV.PostWarningLog("System", $"EFEM can not do initialize, not connected");
                return;
            }

            IsInitialized = false;

            if (!_connection.Execute(new RorzeEfemHandlerInit(this, ModuleName.System), out string reason))
            {
                EV.PostWarningLog("System", $"EFEM can not do initialize, {reason}");
                return;
            }
        }

        public void ClearError()
        {
            if (_connection == null || !_connection.IsConnected)
            {
                EV.PostWarningLog("System", $"EFEM can not do clear error, not connected");
                return;
            }

            if (!_connection.Execute(new RorzeEfemHandlerError(this, false), out string reason))
            {
                EV.PostWarningLog("System", $"EFEM can not do clear error, {reason}");
                return;
            }
        }

        public void SetLoadPortCallback(ModuleName module, IEfemLoadPortCallback lpCallback)
        {
            lock (_locker)
            {
                _lpCallback[module] = lpCallback;
            }
        }
        public void SetRobotCallback(ModuleName module, IEfemRobotCallback robotCallback)
        {
            _robot = robotCallback;
        }
        public void SetAlignerCallback(ModuleName module, IEfemAlignerCallback alignerCallback)
        {
            _aligner = alignerCallback;
        }
        public void SetBufferCallback(ModuleName module, IEfemBufferCallback bufferCallback)
        {

        }
        public void SetSignalTowerCallback(ModuleName module, IEfemSignalTowerCallback signalTowerCallback)
        {
            _signalTower = signalTowerCallback;
        }
        public void SetSystemCallback(ModuleName module, IEfemSystemCallback systemCallback)
        {
            _system = systemCallback;
        }
        public void SetFfuCallback(ModuleName module, IEfemFfuCallback ffuCallback)
        {
            _ffu = ffuCallback;
        }

        protected virtual void OnError(string obj)
        {
            if (!string.IsNullOrEmpty(obj))
                EV.PostAlarmLog(Module, obj);
        }

        protected virtual void OnConnected()
        {
            EV.PostInfoLog(Module, $"{Name} connected.");

            
        }

        protected virtual void OnDisconnected()
        {
            EV.PostWarningLog(Module, $"{Name} disconnected.");
        }

        public virtual void Monitor()
        {

        }
 
        public void Terminate()
        {
            if (_connection != null)
            {
                _connection.Terminate();
            }

        }

        public bool AlarmIsTripped()
        {
            throw new NotImplementedException();
        }

        public bool IsOperable()
        {
            return false;
        }

        public bool Home(out string reason)
        {
            Init();

            reason = string.Empty;
            return true;
        }

        public bool QueryWaferPresence(out string reason)
        {
            throw new NotImplementedException();
        }

        public event Action<string> CarrierArrived;
        public event Action<string> CarrierRemoved;
        public event Action<string, string> CarrierPresenceStateError;
        public event Action<string, bool> CarrierPresenceStateChanged;
        public event Action<string> CarrierDoorClosed;
        public event Action<string> CarrierDoorOpened;
        public event Action<string> E84HandOffStart;
        public event Action<string> E84HandOffComplete;
        public virtual bool HomeLoadPort(string lp, out string reason)
        {
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerInit(this, ModuleHelper.Converter(lp)), out reason);
        }

        public bool LoadPortClearAlarm(string lp, out string reason)
        {
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerError(this, false), out reason);
        }

        public bool UnclampCarrier(string lp, out string reason)
        {
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerUnlock(this, ModuleHelper.Converter(lp)), out reason);
        }

        public bool ClampCarrier(string lp, out string reason)
        {
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerLock(this, ModuleHelper.Converter(lp)), out reason);
        }

        public bool MoveCarrierPort(string lp, string position, out string reason)
        {
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }

            if(position.ToUpper() == "DOCK")
                return _connection.Execute(new RorzeEfemHandlerDock(this, ModuleHelper.Converter(lp)), out reason);
            if (position.ToUpper() == "UNDOCK")
                return _connection.Execute(new RorzeEfemHandlerUndock(this, ModuleHelper.Converter(lp)), out reason);

            reason = "invalid parameter";
            return false;
        }

        public bool OpenCarrierDoor(string lp, out string reason)
        {
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerOpen(this, ModuleHelper.Converter(lp)), out reason);
        }

        public bool OpenDoorAndMapCarrier(string lp, out string slotMap, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool CloseCarrierDoor(string lp, out string reason)
        {
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerClose(this, ModuleHelper.Converter(lp)), out reason);
        }

        public bool GetLoadPortStatus(string lp, out LoadportCassetteState cassetteState, out FoupClampState clampState,
            out FoupDockState dockState, out FoupDoorState doorState, out string reason)
        {
            reason = string.Empty;
            cassetteState = LoadportCassetteState.Unknown;
            clampState = FoupClampState.Unknown;
            dockState = FoupDockState.Unknown;
            doorState = FoupDoorState.Unknown;
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerSigStat(this, ModuleHelper.Converter(lp)), out reason);
        }

        public virtual bool MapCarrier(string lp, out string slotMap, out string reason)
        {
            slotMap = string.Empty;
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerWafsh(this, ModuleHelper.Converter(lp), true), out reason);
        }

        public bool QueryMapResult(string lp, out string reason, bool mapByRobot = true)
        {
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerMapdt(this, ModuleHelper.Converter(lp), mapByRobot), out reason);

        }

        public virtual bool ReadCarrierId(string lp, out string carrierId, out string reason)
        {
            carrierId = string.Empty;
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerCstid(this, ModuleHelper.Converter(lp)), out reason);
        }

        public bool HomeWaferAligner(out string reason)
        {
            throw new NotImplementedException();
        }

        public bool AlignWafer(double angle, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool AlignerMapWaferPresence(out string slotMap, out string reason)
        {
            slotMap = string.Empty;
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerState(this, RorzeEfemStateType.TRACK), out reason);
        }

        public bool HomeAllAxes(out string reason)
        {
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerHome(this, ModuleName.EfemRobot), out reason);
        }

        public bool QueryRobotWaferPresence(out string slotMap, out string reason)
        {
            slotMap = string.Empty;
            if (_connection == null || !_connection.IsConnected || !IsInitialized)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerState(this, RorzeEfemStateType.TRACK), out reason);
        }

        public bool GetTwoWafers(ModuleName chamber, int slot, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool PutTwoWafers(ModuleName chamber, int slot, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool GetWafer(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            if (_connection == null || !_connection.IsConnected || !IsInitialized)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerLoad(this, chamber, slot, hand, WaferManager.Instance.GetWaferSize(chamber, slot)), out reason);
        }

        public bool PutWafer(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            if (_connection == null || !_connection.IsConnected || !IsInitialized)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerUnload(this, chamber, slot, hand, WaferManager.Instance.GetWaferSize(ModuleName.EfemRobot, (int)hand)), out reason);
        }

        public bool MoveToReadyGet(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            if (_connection == null || !_connection.IsConnected || !IsInitialized)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerGoto(this, chamber, slot, hand, true), out reason);
        }

        public bool MoveToReadyPut(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            if (_connection == null || !_connection.IsConnected || !IsInitialized)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerGoto(this, chamber, slot, hand,  false), out reason);
        }

        public bool SetSignalLight(LightType type, TowerLightStatus state, out string reason)
        {
            if (_connection == null || !_connection.IsConnected  || !IsCommunicationReady)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerSigout(this, ModuleName.System, type, state), out reason);
        }

        public bool SetLoadPortLight(ModuleName chamber, Indicator light, IndicatorState state)
        {
            throw new NotImplementedException();
        }

        public virtual bool SetLoadPortLight(ModuleName chamber, IndicatorType light, IndicatorState state, out string reason)
        {
            if (_connection == null || !_connection.IsConnected || !IsCommunicationReady)
            {
                reason = "not connected";
                return false;
            }

            return _connection.Execute(new RorzeEfemHandlerSigout(this, chamber, light, state), out reason);
        }

        public bool SetE84Available(string lp, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool SetE84Unavailable(string lp, out string reason)
        {
            throw new NotImplementedException();
        }

        public virtual void Reset()
        {
            if (_connection != null)
            {
                _connection.Reset();
            }

        }

        public void Connect()
        {
            if (_connection != null)
            {
                _connection.InvokeConnect();
            }
        }

        public void Disconnect()
        {
            if (_connection != null)
            {
                _connection.InvokeDisconnect();
            }

        }


        public bool CheckIsBusy(ModuleName module)
        {
            return _connection.IsBusy((int)module);
        }

        public bool Map(ModuleName module, out string reason, bool mapByRobot = true)
        {
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "Not connected";
                return false;
            }

            if (!_connection.Execute(new RorzeEfemHandlerMapdt(this, module, mapByRobot), out reason))
            {
                return false;
            }

            return true;
        }

        public void QueryState(ModuleName module)
        {
            if (_connection != null && _connection.IsConnected)
            {
                if (!_connection.Execute(new RorzeEfemHandlerSigStat(this, module), out string reason))
                {
                    EV.PostWarningLog(Module, reason);
                }
            }
        }

        public virtual void NoteStateEvent(ModuleName module, string data1, string data2)
        {
            if (ModuleHelper.IsLoadPort(module) && _lpCallback.ContainsKey(module))
                _lpCallback[module].NoteStatus(data1, data2);
        }

        internal void NoteFailed(ModuleName module, string error)
        {
            switch (module)
            {
                case ModuleName.EfemRobot:
                    _robot.NoteFailed(error);
                    break;
            }
        }

        internal void NoteCancel(ModuleName module, string error)
        {
            switch (module)
            {
                case ModuleName.EfemRobot:
                    _robot?.NoteCancel(error);
                    break;
                case ModuleName.System:
                {
                    _robot?.NoteCancel(error);
                    _aligner?.NoteCancel(error);
                    _lpCallback.Values.ToList().ForEach(x => x.NoteCancel(error));
                }
                    break;
                case ModuleName.LP1:
                case ModuleName.LP2:
                case ModuleName.LP3:
                    _lpCallback[module].NoteCancel(error);
                    break;
                case ModuleName.Aligner:
                    _aligner?.NoteCancel(error);
                    break;

            }
        }

        internal void NoteComplete(ModuleName module)
        {
            switch (module)
            {
                case ModuleName.EfemRobot:
                    _robot.NoteComplete();
                    break;
                case ModuleName.System:
                {
                    IsInitialized = true;
                    _robot?.NoteComplete();
                    _aligner?.NoteComplete();
                    _lpCallback.Values.ToList().ForEach(x => x.NoteComplete());
                }
                    break;
                case ModuleName.LP1:
                case ModuleName.LP2:
                case ModuleName.LP3:
                    _lpCallback[module].NoteComplete();
                    break;
                case ModuleName.Aligner:
                    _aligner?.NoteComplete();
                    break;
            }
        }

        internal void NoteWaferMapResult(ModuleName module, string slotMap)
        {
            if (ModuleHelper.IsLoadPort(module))
            {
                _lpCallback[module].NoteSlotMap(slotMap);
            }
        }
        internal void NoteCarrierIDReadResult(ModuleName module, string carrierID)
        {
            if (ModuleHelper.IsLoadPort(module))
            {
                _lpCallback[module].NoteCarrierID(carrierID);
            }
        }
        public void NoteInitialized()
        {
            IsInitialized = true;
        }

        public void NoteCommunicationReady()
        {
            IsCommunicationReady = true;
        }

        public virtual void NoteWaferTrack(string waferInfo)
        {

        }

        protected virtual void OnCarrierArrived(string obj)
        {
            CarrierArrived?.Invoke(obj);
        }
        public void AlarmStateChanged(string s, string arg)
        {
            AlarmGenerated?.Invoke(s, EventLevel.Alarm, arg);
        }
    }
}
