using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.LogicUnits;
using MECF.Framework.RT.ModuleLibrary.Commons;

namespace MECF.Framework.RT.ModuleLibrary.VceModules
{
    public abstract class VceModuleBase : ModuleFsmDevice, ITransferTarget, IModuleDevice
    {
        private int _slot = 25;
        //private bool isJobDone;

        public VceModuleBase(int slot)
        {
            _slot = slot;
        }

        public override bool Initialize()
        {
            WaferManager.Instance.SubscribeLocation(Module, _slot);
            CarrierManager.Instance.SubscribeLocation(Module);

            return base.Initialize();
        }

        public virtual void NoteJobStart()
        {
            //isJobDone = false;
        }

        public virtual void NoteJobComplete()
        {
            //isJobDone = true;
        }
        
        //IModuleDevice
        public abstract bool IsReady { get; }
        public abstract bool IsError { get; }
        public abstract bool IsInit { get; }
        public abstract bool IsBusy { get; }
        public abstract bool IsMapped { get; }
        public abstract bool Home(out string reason);

        //Transfer
        public abstract bool PrepareTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool TransferHandoff(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool PostTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool CheckReadyForTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract void NoteTransferStart(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType);
        public abstract void NoteTransferStop(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType);

        public abstract bool InvokeCloseDoor();

        public abstract bool CheckIsPumping();

        public bool CheckHandoff(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason)
        {
            throw new System.NotImplementedException();
        }
        public abstract bool ExecuteLoad(out string reason);
        public abstract bool ExecuteUnload(out string reason);

        public abstract bool ExecutePlatformIn(out string reason);
        public abstract bool ExecutePlatformOut(out string reason);
    }
}
