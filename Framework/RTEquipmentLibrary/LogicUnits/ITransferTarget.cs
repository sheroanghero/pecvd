using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Schedulers;

namespace MECF.Framework.RT.EquipmentLibrary.LogicUnits
{
    public interface ITransferTarget
    {
        bool PrepareTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        bool TransferHandoff(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        bool PostTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);

        bool CheckReadyForTransfer(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
        void NoteTransferStart(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType);
        void NoteTransferStop(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType);

        bool CheckHandoff(ModuleName robot, Hand blade, int targetSlot, EnumTransferType transferType, out string reason);
    }

    public interface ITransferRobot
    {
        bool Pick(ModuleName target, Hand blade, int targetSlot, out string reason);
        bool Place(ModuleName target, Hand blade, int targetSlot, out string reason);

        bool PickAndPlace(ModuleName pickTarget, Hand pickHand, int pickSlot, ModuleName placeTarget, Hand placeHand, int placeSlot, out string reason);
        bool Goto(ModuleName target, Hand blade, int targetSlot, out string reason);

    }
}
