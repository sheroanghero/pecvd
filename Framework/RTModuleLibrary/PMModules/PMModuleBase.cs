using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.LogicUnits;
using MECF.Framework.RT.ModuleLibrary.Commons;

namespace MECF.Framework.RT.ModuleLibrary.PMModules
{
    public abstract class PMModuleBase : ModuleFsmDevice, ITransferTarget, IModuleDevice
    {
        private int _slot = 1;
        public PMModuleBase(int slot)
        {
            _slot = slot;
        }

        public override bool Initialize()
        {
            WaferManager.Instance.SubscribeLocation(Module, _slot);

            return base.Initialize();
        }

        public abstract double ChamberPressure { get; }

        public abstract bool CheckAcked(int entityTaskToken);

        //IModuleDevice
        public abstract bool IsReady { get; }
        public abstract bool IsError { get; }
        public abstract bool IsInit { get; }
        public abstract bool IsIdle { get; }
        public abstract bool IsBusy { get; }
        public abstract bool Home(out string reason);

        //ITransferTarget
        public abstract bool PrepareTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool TransferHandoff(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool CheckHandoff(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool PostTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool CheckReadyForTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract void NoteTransferStart(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType);
        public abstract void NoteTransferStop(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType);

        //Process
        public abstract bool Process(string recipeName, bool isCleanRecipe, bool withWafer, out string reason);

        //TSS
        public virtual int InvokeTss()
        {
            return 0;
        }


        //Pump
        public abstract bool PreparePump(out string reason);
        public abstract bool CheckPreparePump();
        public abstract bool SlowPump(int tvPosition, out string reason);
        public abstract bool FastPump(int tvPosition, out string reason);
        public abstract bool TurnOnPump(out string reason);
        public abstract bool CheckPumpIsOn();
        public abstract bool ShutDownPump(out string reason);
        public abstract bool AbortPump();

        //Vent
        public abstract bool PrepareVent(out string reason);
        public abstract bool CheckPrepareVent();
        public abstract bool Vent(out string reason);
        public abstract bool StopVent(out string reason);

        //Leak check
        //public abstract bool PrepareLeakCheck(out string reason);
        //public abstract bool CheckPrepareLeakCheck();
        //public abstract bool AbortLeakCheck();

        //Slit Valve
        //public abstract bool OpenSlitValve(out string reason);
        //public abstract bool CheckSlitValveOpen();
        //public abstract bool CloseSlitValve(out string reason);
        public abstract bool CheckSlitValveClose();

        //Lid
        public abstract bool CheckLidClosed();
    }
}
