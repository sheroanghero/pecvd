using Aitex.Core.RT.Device;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.LogicUnits;
using MECF.Framework.RT.ModuleLibrary.Commons;

namespace MECF.Framework.RT.ModuleLibrary.EfemModules
{
    public abstract class EfemModuleBase : ModuleFsmDevice, ITransferRobot, IModuleDevice
    {
        private int _slot = 2;

        public EfemModuleBase(int slot)
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
        public abstract bool Home(out string reason);

        //ITransferRobot
        public abstract bool Pick(ModuleName target, Hand blade, int targetSlot, out string reason);
        public abstract bool Place(ModuleName target, Hand blade, int targetSlot, out string reason);
        public abstract bool PickAndPlace(ModuleName pickTarget, Hand pickHand, int pickSlot, ModuleName placeTarget, Hand placeHand, int placeSlot, out string reason);
        public abstract bool Goto(ModuleName target, Hand blade, int targetSlot, out string reason);

        public abstract bool Map(ModuleName target, out string reason);
    }
}
