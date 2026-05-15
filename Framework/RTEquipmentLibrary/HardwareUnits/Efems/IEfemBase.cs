using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Common;
using Aitex.Core.RT.Event;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Device.Bases;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems
{
    public interface IEfemBase
    {
    }

    public interface IEfemController
    {
        //efem
        event Action<string/*ModuleName*/, EventLevel, string/*message*/> AlarmGenerated;

        bool IsInitialized { get; }

        void Initialize();
        void Terminate();

        bool AlarmIsTripped();
        bool IsOperable();

        bool CheckIsBusy(ModuleName module);


        bool Home(out string reason);
        bool QueryWaferPresence(out string reason);

        //loadport
        event Action<string/*ModuleName*/> CarrierArrived;
        event Action<string/*ModuleName*/> CarrierRemoved;
        event Action<string/*ModuleName*/, string /*message*/> CarrierPresenceStateError;
        event Action<string/*ModuleName*/, bool/*present*/> CarrierPresenceStateChanged;
        event Action<string/*ModuleName*/> CarrierDoorClosed;
        event Action<string/*ModuleName*/> CarrierDoorOpened;
        event Action<string/*ModuleName*/> E84HandOffStart;
        event Action<string/*ModuleName*/> E84HandOffComplete;

        bool HomeLoadPort(string lp, out string reason);
        bool LoadPortClearAlarm(string lp, out string reason);
        bool UnclampCarrier(string lp, out string reason);
        bool ClampCarrier(string lp, out string reason);
        bool MoveCarrierPort(string lp, string position, out string reason);
        bool OpenCarrierDoor(string lp, out string reason);
        bool OpenDoorAndMapCarrier(string lp, out string slotMap, out string reason);
        bool CloseCarrierDoor(string lp, out string reason);
        bool GetLoadPortStatus(string lp, out LoadportCassetteState cassetteState, out FoupClampState clampState, out FoupDockState dockState, out FoupDoorState doorState, out string reason);
        bool MapCarrier(string lp, out string slotMap, out string reason);
        bool QueryMapResult(string lp, out string reason, bool mapByRobot = true);

        //carrier ID
        bool ReadCarrierId(string lp, out string carrierId, out string reason);

        //aligner
        bool HomeWaferAligner(out string reason);
        bool AlignWafer(double angle, out string reason);
        bool AlignerMapWaferPresence(out string slotMap, out string reason);

        //robot
        bool HomeAllAxes(out string reason);
        bool QueryRobotWaferPresence(out string slotMap, out string reason);
        bool GetTwoWafers(ModuleName chamber, int slot, out string reason);
        bool PutTwoWafers(ModuleName chamber, int slot, out string reason);
        bool GetWafer(ModuleName chamber, int slot, Hand hand, out string reason);
        bool PutWafer(ModuleName chamber, int slot, Hand hand, out string reason);
        bool MoveToReadyGet(ModuleName chamber, int slot, Hand hand, out string reason);
        bool MoveToReadyPut(ModuleName chamber, int slot, Hand hand, out string reason);

        //signal tower
        bool SetSignalLight(LightType type, TowerLightStatus state, out string reason);

        [Obsolete]
        bool SetLoadPortLight(ModuleName chamber, Indicator light, IndicatorState state);
        
        bool SetLoadPortLight(ModuleName chamber, IndicatorType lightType, IndicatorState state, out string reason);

        bool SetE84Available(string lp, out string reason);
        bool SetE84Unavailable(string lp, out string reason);

    }

}
