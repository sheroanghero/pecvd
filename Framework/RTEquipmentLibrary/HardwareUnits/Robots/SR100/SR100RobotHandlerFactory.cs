using Aitex.Core.RT.SCCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Aitex.Sorter.Common;
using MECF.Framework.Common.Equipment;
using Aitex.Core.RT.Device;
using Aitex.Core.Common;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.SR100
{
    public class SR100RobotHandlerFactory : IRobotHandlerFactory
    {

        private TokenGenerator _tokener;

        private IDevice _device = null;
        public SR100RobotHandlerFactory(IDevice device)
        {
            _device = device;

            if (SC.ContainsItem($"{device.Name}.RobotCommunicationToken"))
                _tokener = new TokenGenerator($"{device.Name}.RobotCommunicationToken");
            else
            {
                _tokener = new TokenGenerator($"Robot.RobotCommunicationToken");
            }
        }

        public IHandler SetCommunication()
        {
            throw new NotImplementedException();
        }

        public IHandler SetLoad(Hand hand, bool isOn)
        {
            throw new NotImplementedException();
        }

        public IHandler CheckLoad(ModuleName chamber, int slot)
        {
            throw new NotImplementedException();
        }

        public IHandler RequestWaferPresent()
        {
            throw new NotImplementedException();
        }

        public IHandler RequestWaferPresentLoadA()
        {
            throw new NotImplementedException();
        }

        public IHandler RequestWaferPresentLoadB()
        {
            throw new NotImplementedException();
        }

        public IHandler RequestAWCData()
        {
            throw new NotImplementedException();
        }

        public IHandler Init()
        {
            return new handler<RbInitHandler>(_device, ref _tokener);
        }
        public IHandler Home()
        {
            return new handler<RbHomeHandler>(_device, ref _tokener);
        }
        public IHandler ArmHome()
        {
            throw new NotImplementedException();
        }
        public IHandler Event()
        {
            return new handler<RbEventHandler>(_device, ref _tokener,  true);
        }

        public IHandler Grip(Hand hand)
        {
            return new handler<RbGripHandler>(_device, ref _tokener, hand, true);
        }

        public IHandler Release(Hand hand)
        {
            return new handler<RbGripHandler>(_device, ref _tokener, hand, false);
        }

        public IHandler QueryState()
        {
            return new handler<RBQueryStateHandler>(_device, ref _tokener);
        }

        public IHandler QueryPosition()
        {          
            return new handler<RBQueryPositionHandler>(_device, ref _tokener);
        }

        public IHandler Clear()
        {          
            return new handler<RbClearErrorHandler>(_device, ref _tokener);
        }


        public IHandler Stop(bool isEmergency)
        {
            return new handler<RbStopHandler>(_device, ref _tokener, isEmergency);
        }

        public IHandler Resume( )
        {
            return new handler<RbResumeHandler>(_device, ref _tokener);
        }
        public IHandler SetSpeed(int speed)
        {
            return new handler<RbSetSpeedHandler>(_device, ref _tokener, speed);
        }


        public IHandler Goto(ModuleName chamber, int slot,  Motion next, Hand hand, int x, int y, int z)
        {
            return new handler<GotoHandler>(_device, ref _tokener, chamber, slot, next, hand, x, y , z);
        }

        public IHandler MoveTo(ModuleName chamber, int slot, Hand hand, bool isPick, int x, int y, int z)
        {
            return new handler<MoveToHandler>(_device, ref _tokener, chamber, slot, hand, isPick, x, y, z);
        }

        public IHandler Pick(ModuleName chamber, int slot, Hand hand)
        {      
            return new handler<PickHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler PickExtend(ModuleName chamber, int slot, Hand hand)
        {
            return new handler<PickExtendHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler PickRetract(ModuleName chamber, int slot, Hand hand)
        {
            return new handler<PickRetractHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler PickReadyPosition(ModuleName chamber, int slot, Hand hand)
        {
            throw new NotImplementedException();
        }

        public IHandler Place(ModuleName chamber, int slot, Hand hand)
        {      
            return new handler<PlaceHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler PlaceExtend(ModuleName chamber, int slot, Hand hand)
        {
            return new handler<PlaceExtendHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler PlaceRetract(ModuleName chamber, int slot, Hand hand)
        {
            return new handler<PlaceRetractHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler PlaceReadyPosition(ModuleName chamber, int slot, Hand hand)
        {
            throw new NotImplementedException();
        }

        public IHandler Exchange(ModuleName chamber, int slot, Hand hand)
        {    
            return new handler<ExchangHandler>(_device, ref _tokener, chamber, slot, hand);
        }


        public IHandler WaferMapping(ModuleName chamber)
        {

            return new handler<RBWaferMappingHandler>(_device, ref _tokener, chamber);
        }
        public IHandler QueryWaferMap(ModuleName chamber)
        {
            return new handler<RBQueryWaferMapHandler>(_device, ref _tokener, chamber);
        }


        public IHandler PickEx(ModuleName chamber, int slot, Hand hand, int x, int y, int z)
        {
            return new handler<PickHandlerEx>(_device, ref _tokener, chamber, slot, hand, x, y, z);
        }

        public IHandler PlaceEx(ModuleName chamber, int slot, Hand hand, int x, int y, int z)
        {
            return new handler<PlaceHandlerEx>(_device, ref _tokener, chamber, slot, hand, x, y, z);
        }

        public IHandler Extend(ModuleName chamber, int slot, Hand hand)
        {
            throw new NotImplementedException();
        }

        public IHandler Retract(ModuleName chamber, int slot, Hand hand)
        {
            throw new NotImplementedException();
        }

        public IHandler ExchangeEx(ModuleName chamber, int slot, Hand hand, int x, int y, int z)
        {
            throw new NotImplementedException();
        }

        public IHandler ExchangeReadyPosition(ModuleName chamber, int slot, Hand pickHand, Hand placeHand)
        {
            throw new NotImplementedException();
        }

        public IHandler ExchangePickExtend(ModuleName chamber, int slot, Hand pickHand, Hand placeHand)
        {
            throw new NotImplementedException();
        }

        public IHandler ExchangePlaceExtend(ModuleName chamber, int slot, Hand pickHand, Hand placeHand)
        {
            throw new NotImplementedException();
        }

        public IHandler ExchangePlaceRetract(ModuleName chamber, int slot, Hand pickHand, Hand placeHand)
        {
            throw new NotImplementedException();
        }

        public IHandler SetWaferSize(int size)
        {
            throw new NotImplementedException();
        }

        public IHandler QueryWaferSize()
        {
            throw new NotImplementedException();
        }

        public IHandler ExchangeReadyExtend(ModuleName chamber, int slot, Hand pickHand, Hand placeHand)
        {
            throw new NotImplementedException();
        }

        public IHandler ExchangeAfterReadyExtend(ModuleName chamber, int slot, Hand pickHand, Hand placeHand)
        {
            throw new NotImplementedException();
        }

        public IHandler PositionAdjustment(Axis axis, Hand hand, int value)
        {
            return new handler<PositionAdjustmentHandler>(_device, ref _tokener, axis, hand, value);
        }

        public IHandler SetWaferSize(int cmd, WaferSize size)
        {
            throw new NotImplementedException();
        }

        public IHandler QueryParameter(int parameter, string parameterType)
        {
            throw new NotImplementedException();
        }

        public IHandler SetServoOnOff(bool trueForOnFalseForOff)
        {
            throw new NotImplementedException();
        }

        public IHandler Aligner()
        {
            throw new NotImplementedException();
        }

        public IHandler AlignerHome()
        {
            throw new NotImplementedException();
        }

        public IHandler RequestAlignerOffset(Hand hand)
        {
            throw new NotImplementedException();
        }

        public IHandler PickAligner(ModuleName chamber, int slot, Hand hand)
        {
            throw new NotImplementedException();
        }

        public IHandler RequestAlignerResult()
        {
            throw new NotImplementedException();
        }
    }
}
