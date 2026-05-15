using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.LogicUnits;
using MECF.Framework.RT.ModuleLibrary.Commons;

namespace MECF.Framework.RT.ModuleLibrary.CoolerModules
{
    public abstract class CoolerModuleBase : ModuleFsmDevice, ITransferTarget, IModuleDevice
    {
        private int _slot = 1;

        public CoolerModuleBase(int slot)
        {
            _slot = slot;
        }

        public override bool Initialize()
        {
            WaferManager.Instance.SubscribeLocation(Module, _slot);

            return base.Initialize();
        }

        //IModuleDevice
        public abstract bool IsReady { get; }
        public abstract bool IsError { get; }
        public abstract bool IsInit { get; }
        public abstract bool IsBusy { get; }
        public abstract bool Home(out string reason);

        //Transfer
        public abstract bool PrepareTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool TransferHandoff(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool PostTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool CheckReadyForTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract void NoteTransferStart(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType);
        public abstract void NoteTransferStop(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType);

        public abstract void Invoke(string function, params object[] args);

        public bool CheckHandoff(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason)
        {
            throw new System.NotImplementedException();
        }
    }
}
