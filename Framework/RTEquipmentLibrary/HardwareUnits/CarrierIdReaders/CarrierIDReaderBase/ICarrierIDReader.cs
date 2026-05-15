using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.CarrierIDReaderBase
{
    public interface ICarrierIDReader
    {
        bool ReadCarrierID();
        bool WriteCarrierID(string carrierID);
        bool ReadParameter(string parameter);
        bool SetParameter(string parameter, string value);
        bool ReadCarrierID(int offset, int length);
        bool WriteCarrierID(int offset, int length, string carrierID);
        void Reset();

    }
}
