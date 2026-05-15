using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Efems.Rorzes
{
    public interface IEfemFfuCallback
    {
    }

    public interface IEfemAlignerCallback
    {
        void NoteComplete();
        void NoteCancel(string error);
    }

    public interface IEfemBufferCallback
    {
    }

    public interface IEfemLoadPortCallback
    {
        void NoteStatus(string data1, string data2);
        void NoteSlotMap(string slotMap);
        void NoteComplete();
        void NoteCancel(string error);
        void NoteCarrierID(string carrierID);
    }

    public interface IEfemRobotCallback
    {
        void NoteFailed(string error);
        void NoteComplete();
        void NoteCancel(string error);
    }

    public interface IEfemSignalTowerCallback
    {
    }

    public interface IEfemSystemCallback
    {
        void NoteStatus(string data1, string data2);
    }
}
