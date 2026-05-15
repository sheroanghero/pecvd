using Aitex.Core.RT.SCCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using Aitex.Core.RT.Device;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.NX100.AL
{
    public class NX100AlignerHandlerFactory : IAlignerHandlerFactory
    {

        private TokenGenerator _tokener = new TokenGenerator("Aligner.AlignerCommunicationToken");


        private IDevice _device = null;
        public NX100AlignerHandlerFactory(IDevice device)
        {
            _device = device;
        }
        public IHandler Init()
        {
            return new handler<AlInitHandler>(_device, ref _tokener);
        }
        public IHandler Home()
        {
            return new handler<AlHomeHandler>(_device, ref _tokener);
        }
        public IHandler Event()
        {
            return new handler<ALEventHandler>(_device, ref _tokener, true);
        }

        public IHandler Grip(Hand hand)
        {
            return new handler<AlGripHandler>(_device, ref _tokener, hand, true);
        }

        public IHandler Release(Hand hand)
        {
            return new handler<AlGripHandler>(_device, ref _tokener, hand, false);
        }

        public IHandler QueryState()
        {
            return new handler<ALQueryStateHandler>(_device, ref _tokener);
        }

        public IHandler Clear()
        {
            return new handler<AlClearErrorHandler>(_device, ref _tokener);
        }


        public IHandler Stop()
        {
            return new handler<AlStopHandler>(_device, ref _tokener);
        }


        public IHandler Align(double angle)
        {
            return new handler<AlAlignHandler>(_device, ref _tokener, angle);
        }

        public IHandler LiftUp()
        {
            return new handler<ALLiftHandler>(_device, ref _tokener, true);
        }

        public IHandler LiftDown()
        {
            return new handler<ALLiftHandler>(_device, ref _tokener, false);
        }
 
        public IHandler MoveToReadyPostion()
        {
            return new handler<ALMoveToReadyPositionHandler>(_device, ref _tokener);
        }
    }
}
