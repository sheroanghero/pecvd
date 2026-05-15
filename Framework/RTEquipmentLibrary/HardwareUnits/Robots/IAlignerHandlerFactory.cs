using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Sorter.Common;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot
{
    public interface IAlignerHandlerFactory
    {
        IHandler Init();
        IHandler Home();

        IHandler Event();
        IHandler Grip(Hand hand);
        IHandler Release(Hand hand);

        IHandler LiftUp();
        IHandler LiftDown();

        IHandler MoveToReadyPostion();

        IHandler QueryState();
        IHandler Clear();
        IHandler Stop();
        IHandler Align(double anlge);

    }
}
