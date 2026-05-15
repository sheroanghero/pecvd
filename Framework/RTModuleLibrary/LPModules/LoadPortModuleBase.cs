using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;
using MECF.Framework.RT.EquipmentLibrary.LogicUnits;
using MECF.Framework.RT.ModuleLibrary.Commons;

namespace MECF.Framework.RT.ModuleLibrary.LPModules
{
    public abstract class LoadPortModuleBase : ModuleFsmDevice, ITransferTarget, IModuleDevice
    {
        public LoadPortModuleBase(int slot)
        {

        }

        public override bool Initialize()
        {

            return base.Initialize();
        }

        //IModuleDevice
        public abstract bool IsReady { get; }
        public abstract bool IsError { get; }
        public abstract bool IsInit { get; }
        public abstract bool Home(out string reason);

        //Job Task
        public abstract void NoteJobStart();
        public abstract void NoteJobComplete();

        //Transfer
        public abstract bool PrepareTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool TransferHandoff(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool PostTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract bool CheckReadyForTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        public abstract void NoteTransferStart(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType);
        public abstract void NoteTransferStop(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType);
        public abstract bool CheckReadyForMap(ModuleName robot, Hand blade, out string reason);


        public virtual bool IsLoaded { get; }
        public virtual bool IsUnloaded { get; }
        public virtual bool Load()
        {
            return true;
        }
        public virtual bool Unload()
        {
            return true;
        }

        public virtual bool IsClamped { get; }
        public virtual bool IsUnclamped { get; }
        public virtual bool Clamp()
        {
            return true;
        }
        public virtual bool Unclamp()
        {
            return true;
        }

        public bool CheckHandoff(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason)
        {
            throw new System.NotImplementedException();
        }
    }
}
