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

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot.MAG7
{
    public class Mag7RobotHandlerFactory : IRobotHandlerFactory
    {
        private TokenGenerator _tokener;

        private IDevice _device = null;
        public Mag7RobotHandlerFactory(IDevice device)
        {
            _device = device;

            if (SC.ContainsItem($"{device.Name}.RobotCommunicationToken"))
                _tokener = new TokenGenerator($"{device.Name}.RobotCommunicationToken");
            else
            {
                _tokener = new TokenGenerator($"TMRobot.RobotCommunicationToken");
            }
        }

        public IHandler Init()
        {
            throw new NotImplementedException();
        }
        public IHandler Home()
        {
            return new Mag7RobotHandlerBase<RbHomeHandler>(_device, ref _tokener);
        }
        public IHandler ArmHome()
        {
            throw new NotImplementedException();
        }

        public IHandler Aligner()
        {
            return new Mag7RobotHandlerBase<RbAlignerHandler>(_device, ref _tokener);
        }

        public IHandler AlignerHome()
        {
            return new Mag7RobotHandlerBase<RbHomeAGLNHandler>(_device, ref _tokener);
        }

        public IHandler RequestAlignerResult()
        {
            return new Mag7RobotHandlerBase<RRequestAlignerResultHandler>(_device, ref _tokener);
        }

        public IHandler RequestAlignerOffset(Hand hand)
        {
            return new Mag7RobotHandlerBase<RRequestAlignerOffsetHandler>(_device, ref _tokener, hand);
        }

        public IHandler Event()
        {
            return new Mag7RobotHandlerBase<RbEventHandler>(_device, ref _tokener,  true);
        }


        public IHandler QueryState()
        {
            return new Mag7RobotHandlerBase<RBQueryStateHandler>(_device, ref _tokener);
        }

        public IHandler QueryPosition()
        {          
            return new Mag7RobotHandlerBase<RBQueryPositionHandler>(_device, ref _tokener);
        }



        public IHandler Stop()
        {
            return new Mag7RobotHandlerBase<RbStopHandler>(_device, ref _tokener);
        }

        public IHandler SetSpeed(int speed)
        {
            return new Mag7RobotHandlerBase<RbSetSpeedHandler>(_device, ref _tokener, speed);
        }


        public IHandler Goto(ModuleName chamber, int slot,  Motion next, Hand hand, int x, int y, int z)
        {
            return new Mag7RobotHandlerBase<GotoHandler>(_device, ref _tokener, chamber, slot, next, hand, x, y , z);
        }


        public IHandler Pick(ModuleName chamber, int slot, Hand hand)
        {      
            return new Mag7RobotHandlerBase<PickHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler PickExtend(ModuleName chamber, int slot, Hand hand)
        {
            return new Mag7RobotHandlerBase<PickExtendHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler PickRetract(ModuleName chamber, int slot, Hand hand)
        {
            return new Mag7RobotHandlerBase<PickRetractHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler PickAligner(ModuleName chamber, int slot, Hand hand)
        {
            return new Mag7RobotHandlerBase<PickAlignerHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler PickReadyPosition(ModuleName chamber, int slot, Hand hand)
        {
            throw new NotImplementedException();
        }

        public IHandler Place(ModuleName chamber, int slot, Hand hand)
        {      
            return new Mag7RobotHandlerBase<PlaceHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler PlaceExtend(ModuleName chamber, int slot, Hand hand)
        {
            return new Mag7RobotHandlerBase<PlaceExtendHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler PlaceRetract(ModuleName chamber, int slot, Hand hand)
        {
            return new Mag7RobotHandlerBase<PlaceRetractHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler PlaceReadyPosition(ModuleName chamber, int slot, Hand hand)
        {
            throw new NotImplementedException();
        }

        public IHandler Exchange(ModuleName chamber, int slot, Hand hand)
        {    
            return new Mag7RobotHandlerBase<ExchangHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler PickEx(ModuleName chamber, int slot, Hand hand, int x, int y, int z)
        {
            return new Mag7RobotHandlerBase<PickExHandler>(_device, ref _tokener, chamber, slot, hand, x, y, z);
        }

        public IHandler PlaceEx(ModuleName chamber, int slot, Hand hand, int x, int y, int z)
        {
            return new Mag7RobotHandlerBase<PlaceHandler>(_device, ref _tokener, chamber, slot, hand, x, y, z);
        }

        public IHandler Grip(Hand hand)
        {
            throw new NotImplementedException();
        }

        public IHandler Release(Hand hand)
        {
            throw new NotImplementedException();
        }

        public IHandler Clear()
        {
            throw new NotImplementedException();
        }

        public IHandler WaferMapping(ModuleName loadport)
        {
            throw new NotImplementedException();
        }

        public IHandler QueryWaferMap(ModuleName loadport)
        {
            throw new NotImplementedException();
        }

        public IHandler SetCommunication( )
        {
            return new Mag7RobotHandlerBase<RbSetCommunicationHandler>(_device, ref _tokener);
        }

        public IHandler SetLoad(Hand hand, bool isOn)
        {
            return new Mag7RobotHandlerBase<RbSetLoadHandler>(_device, ref _tokener, hand,isOn);
        }

        public IHandler CheckLoad(ModuleName chamber, int slot)
        {
            return new Mag7RobotHandlerBase<RbCheckLoadHandler>(_device, ref _tokener, chamber, slot);
        }

        public IHandler RequestWaferPresent( )
        {
            return new Mag7RobotHandlerBase<RbRequestWaferPresentHandler>(_device, ref _tokener);
        }

        public IHandler RequestWaferPresentLoadA()
        {
            return new Mag7RobotHandlerBase<RbRequestWaferPresentLoadAHandler>(_device, ref _tokener);
        }

        public IHandler RequestWaferPresentLoadB()
        {
            return new Mag7RobotHandlerBase<RbRequestWaferPresentLoadBHandler>(_device, ref _tokener);
        }

        public IHandler RequestAWCData()
        {
            return new Mag7RobotHandlerBase<RRequestAWCDataHandler>(_device, ref _tokener);
        }

        public IHandler Extend(ModuleName chamber, int slot, Hand hand)
        {
            return new Mag7RobotHandlerBase<ExtendHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler Retract(ModuleName chamber, int slot, Hand hand)
        {
            return new Mag7RobotHandlerBase<RetractHandler>(_device, ref _tokener, chamber, slot, hand);
        }

        public IHandler Stop(bool isEmergency)
        {
            throw new NotImplementedException();
        }

        public IHandler Resume()
        {
            throw new NotImplementedException();
        }

        public IHandler MoveTo(ModuleName chamber, int slot, Hand hand, bool isPick, int x, int y, int z)
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
            throw new NotImplementedException();
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
    }
}
