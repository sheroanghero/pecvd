using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.SCCore;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems.Rorzes;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems.Siasuns
{
    public class SiasunsEfem : BaseDevice, IDevice, IConnectionContext, IRorzeEfemController
    {
        public bool IsEnabled => throw new NotImplementedException();

        public int RetryConnectIntervalMs => throw new NotImplementedException();

        public int MaxRetryConnectCount => throw new NotImplementedException();

        public bool EnableCheckConnection => throw new NotImplementedException();

        public string Address => throw new NotImplementedException();

        public bool IsAscii => throw new NotImplementedException();

        public string NewLine => throw new NotImplementedException();

        public bool EnableLog => throw new NotImplementedException();

        public bool IsInitialized { get; set; }

        public SiasunsEfemConnection Connection
        {
            get
            {
                return _connection;
            }
        }

        private SiasunsEfemConnection _connection;

        private object _locker = new object();

        public event Action<string, EventLevel, string> AlarmGenerated;
        public event Action<string> CarrierArrived;
        public event Action<string> CarrierRemoved;
        public event Action<string, string> CarrierPresenceStateError;
        public event Action<string, bool> CarrierPresenceStateChanged;
        public event Action<string> CarrierDoorClosed;
        public event Action<string> CarrierDoorOpened;
        public event Action<string> E84HandOffStart;
        public event Action<string> E84HandOffComplete;

        protected IEfemAlignerCallback _aligner;
        protected IEfemRobotCallback _robot;
        protected IEfemSystemCallback _system;

        protected Dictionary<ModuleName, IEfemLoadPortCallback> _lpCallback = new Dictionary<ModuleName, IEfemLoadPortCallback>();

        public LinkedList<HandlerBase> _lstHandlers = new LinkedList<HandlerBase>();

        public LinkedList<HandlerBase> _lstMonitorHandler = new LinkedList<HandlerBase>();

        private string _scRoot;

        public SiasunsEfem(string module, string name, string scRoot) : base(module, name, name, name)
        {
            _scRoot = scRoot;

            //string scBasePath = node.GetAttribute("scBasePath");
            //if (string.IsNullOrEmpty(scBasePath))
            //    scBasePath = $"{Module}.{Name}";
            //else
            //{
            //    scBasePath = scBasePath.Replace("{module}", Module);
            //}
        }

        public bool AlarmIsTripped()
        {
            throw new NotImplementedException();
        }

        public bool AlignerMapWaferPresence(out string slotMap, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool AlignWafer(double angle, out string reason)
        {
            reason = string.Empty;
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }
            _lstHandlers.AddLast(new SiasunsEfemMOVHandler(this, ModuleName.Aligner, SiasunsMovCmd.ALIGN,$"ALIGN1/{angle}"));
            return true;
        }


        public bool CheckIsBusy(ModuleName module)
        {
            return _connection.IsBusy;
        }

        public bool ClampCarrier(string lp, out string reason)
        {
            reason = string.Empty;
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }
            _lstHandlers.AddLast(new SiasunsEfemSetHandler(this, ModuleName.Robot, SiasunsSetCmd.CLAMP, "ALIGN1"));
            _lstHandlers.AddLast(new SiasunsEfemGetHandler(this, ModuleName.Robot, SiasunsGetCmd.CLAMP, "ALIGN1"));
            return true;
        }


        public bool LoadLP(string lp, out string reason)
        {
            reason = string.Empty;
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }
            _lstHandlers.AddLast(new SiasunsEfemMOVHandler(this,ModuleName.LP1, SiasunsMovCmd.LOAD,""));
            return true;
        }

        public bool UnLoadLP(string lp, out string reason)
        {
            reason = string.Empty;
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }
            _lstHandlers.AddLast(new SiasunsEfemMOVHandler(this, ModuleName.LP1, SiasunsMovCmd.UNLOAD, ""));
            return true;
        }

        public bool CloseCarrierDoor(string lp, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool GetLoadPortStatus(string lp, out LoadportCassetteState cassetteState, out FoupClampState clampState, out FoupDockState dockState, out FoupDoorState doorState, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool GetTwoWafers(ModuleName chamber, int slot, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool GetWafer(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool Home(out string reason)
        {
            throw new NotImplementedException();
        }

        public bool HomeAllAxes(out string reason)
        {
            throw new NotImplementedException();
        }

        public bool HomeLoadPort(string lp, out string reason)
        {
            reason = string.Empty;
            if (Connection == null || !Connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }
            string param = "P1";
            if(lp == ModuleName.LP1.ToString())
            {
                param = "P1";
            }
            else if (lp == ModuleName.LP2.ToString())
            {
                param = "P2";
            }
            else if (lp == ModuleName.LP3.ToString())
            {
                param = "P3";
            }
            else if (lp == ModuleName.LP4.ToString())
            {
                param = "P4";
            }
            else if (lp == ModuleName.LP5.ToString())
            {
                param = "P5";
            }
            _lstHandlers.AddLast(new SiasunsEfemMOVHandler(this, ModuleName.EfemRobot, SiasunsMovCmd.HOME, param));
            return true;
        }

        public bool HomeWaferAligner(out string reason)
        {
            throw new NotImplementedException();
        }

        private string _address = "127.0.0.1:13000";

        public virtual bool Initialize()
        {
            if (SC.ContainsItem($"{Name}.Address"))
                _address = SC.GetStringValue($"{Name}.Address");

            _connection = new SiasunsEfemConnection(_address);

            //_connection.AddEventHandler(RorzeEfemBasicMessage.MAPDT, new RorzeEfemHandlerMapdt(this, ModuleName.System, true));
            //_connection.AddEventHandler(RorzeEfemBasicMessage.SIGSTAT, new RorzeEfemHandlerSigStat(this, ModuleName.System));
            //_connection.AddEventHandler(RorzeEfemBasicMessage.TRANSREQ, new RorzeEfemHandlerTransReq(this, ModuleName.System));
            //_connection.AddEventHandler(RorzeEfemBasicMessage.READY, new RorzeEfemHandlerReady(this, ModuleName.System));

            //DATA.Subscribe($"{Module}.{Name}.Status", () => _connection.);

            return true;
        }

        public bool IsOperable()
        {
            return false;
        }

        public bool LoadPortClearAlarm(string lp, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool MapCarrier(string lp, out string slotMap, out string reason)
        {
            throw new NotImplementedException();
        }

        public void Monitor()
        {
            //throw new NotImplementedException();
        }

        public bool MoveCarrierPort(string lp, string position, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool MoveToReadyGet(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool MoveToReadyPut(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool OpenCarrierDoor(string lp, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool OpenDoorAndMapCarrier(string lp, out string slotMap, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool PutTwoWafers(ModuleName chamber, int slot, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool PutWafer(ModuleName chamber, int slot, Hand hand, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool QueryMapResult(string lp, out string reason, bool mapByRobot = true)
        {
            reason = string.Empty;
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }
            string lpStr = "";
            switch (lp)
            {
                case "LP1":
                    lpStr = "P1";
                    break;
                case "LP2":
                    lpStr = "P2";
                    break;
                case "LP3":
                    lpStr = "P3";
                    break;
            }
            _lstHandlers.AddLast(new SiasunsEfemGetHandler(this, ModuleName.LP1, SiasunsGetCmd.MAPDT, $"{lpStr}"));
            return true;
        }

        public bool QueryRobotWaferPresence(out string slotMap, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool QueryWaferPresence(out string reason)
        {
            throw new NotImplementedException();
        }

        public bool ReadCarrierId(string lp, out string carrierId, out string reason)
        {
            throw new NotImplementedException();
        }

        public virtual void Reset()
        {
            if (_connection != null)
            {
                _connection.ForceClear();
            }
        }

        public void SetAlignerCallback(ModuleName module, IEfemAlignerCallback alignerCallback)
        {
            _aligner = alignerCallback;
        }

        public void SetBufferCallback(ModuleName module, IEfemBufferCallback bufferCallback)
        {
            throw new NotImplementedException();
        }

        public bool SetE84Available(string lp, out string reason)
        {
            throw new NotImplementedException();
        }

        public bool SetE84Unavailable(string lp, out string reason)
        {
            throw new NotImplementedException();
        }

        public void SetLoadPortCallback(ModuleName module, IEfemLoadPortCallback lpCallback)
        {
            lock (_locker)
            {
                _lpCallback[module] = lpCallback;
            }
        }

        public bool SetLoadPortLight(ModuleName chamber, Indicator light, IndicatorState state)
        {
            throw new NotImplementedException();
        }

        public bool SetLoadPortLight(ModuleName chamber, IndicatorType lightType, IndicatorState state, out string reason)
        {
            throw new NotImplementedException();
        }

        public void SetRobotCallback(ModuleName module, IEfemRobotCallback robotCallback)
        {
            _robot = robotCallback;
        }

        public bool SetSignalLight(LightType type, TowerLightStatus state, out string reason)
        {
            reason = string.Empty;
            if (_connection == null || !_connection.IsConnected)
            {
                reason = "not connected";
                return false;
            }
            string typeStr = "";
            switch(type)
            {
                case LightType.Blue:
                    typeStr = "BLUE";
                    break;
                case LightType.Yellow:
                    typeStr = "YELLOW";
                    break;
                case LightType.Green:
                    typeStr = "GREEN";
                    break;
                case LightType.White:
                    typeStr = "WHITE";
                    break;
                case LightType.Red:
                    typeStr = "RED";
                    break;
            }
            string stateStr = "";
            switch (state)
            {
                case TowerLightStatus.Off:
                    stateStr = "OFF";
                    break;
                case TowerLightStatus.On:
                    stateStr = "ON";
                    break;
                case TowerLightStatus.Blinking:
                    stateStr = "BLINK";
                    break;
            }
            _lstHandlers.AddLast(new SiasunsEfemSetHandler(this, ModuleName.LP1, SiasunsSetCmd.SIGOUT, $"STOWER/{typeStr}/{stateStr}"));
            return true;
        }

        public void SetSignalTowerCallback(ModuleName module, IEfemSignalTowerCallback signalTowerCallback)
        {
            throw new NotImplementedException();
        }

        public void Terminate()
        {
            throw new NotImplementedException();
        }

        public bool UnclampCarrier(string lp, out string reason)
        {
            throw new NotImplementedException();
        }

        void IEfemController.Initialize()
        {
            Init();
        }
        public void Init()
        {
            if (_connection == null || !_connection.IsConnected)
            {
                EV.PostWarningLog("System", $"EFEM can not do initialize, not connected");
                return;
            }

            IsInitialized = false;

            //if (!_connection.Execute(new RorzeEfemHandlerInit(this, ModuleName.System), out string reason))
            //{
            //    EV.PostWarningLog("System", $"EFEM can not do initialize, {reason}");
            //    return;
            //}
        }

        public virtual bool ParseData(string command, string parameter, string data)
        {
            return true;
        }

        public virtual bool NoteInitComplete(string rawMessage)
        {
            return true;
        }

        public virtual bool NoteHomeComplete(string rawMessage)
        {
            return true;
        }


        public virtual bool NoteAlign(string v)
        {
            return true;
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

    }
}
