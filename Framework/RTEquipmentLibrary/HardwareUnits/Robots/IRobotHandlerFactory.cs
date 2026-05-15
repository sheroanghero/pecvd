using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Aitex.Core.RT.Log;
using Aitex.Sorter.Common;
using Aitex.Core.RT.SCCore;
using MECF.Framework.Common.Equipment;
using Aitex.Core.RT.Device;
using Aitex.Core.Common;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robot
{

    public interface ITransferMsg
    {
        IDevice Robot { set; }
        bool background { get; }
        bool evt { get; }
        string package(params object[] args);
        /// </summary>
        /// return value, completed
        /// <param name="type"></param>
        /// <param name="cmd"></param>
        /// <returns></returns>
        bool unpackage(string type, string[] cmd);


    }

    public class TokenGenerator
    {
        private int _last = 0;

        List<int> _pool = new List<int>();

        SCConfigItem scToken = null;
        public TokenGenerator(string scName) 
        {
            scToken = SC.GetConfigItem(scName);
            if (scToken == null)

            _last = scToken.IntValue;

            Random r = new Random();
            _last = r.Next() % 20;
        }
 

        public int create()
        {
            int first = _last;
            int token = first;

            do
            {
                token = (token + 1) % 100;

                if (_pool.Contains(token))
                    continue;

                _pool.Add(token);
                _last = token;

                scToken.IntValue = _last;
                return _last;
            } while (token != first);

            throw (new ExcuteFailedException("Get token failed,pool is full"));
        }


        public void release(int token)
        {
            _pool.Remove(token);
        }


        public void release()
        {
            _last = 0;
            _pool.Clear();
        }
    }

    public interface IRobotHandlerFactory
    {
        IHandler Init();
        IHandler Home();
        IHandler ArmHome();

        IHandler Aligner();
        IHandler AlignerHome();
        IHandler RequestAlignerResult();
        IHandler RequestAlignerOffset(Hand hand);

        IHandler Event();
        IHandler Grip(Hand hand);

        IHandler Release(Hand hand);


        IHandler QueryState();

        IHandler QueryPosition();
        IHandler Clear();

        IHandler Stop(bool isEmergency);
        IHandler Resume();

        IHandler SetSpeed(int speed);

        IHandler Pick(ModuleName chamber, int slot, Hand hand);
        IHandler PickExtend(ModuleName chamber, int slot, Hand hand);
        IHandler PickRetract(ModuleName chamber, int slot, Hand hand);
        IHandler PickAligner(ModuleName chamber, int slot, Hand hand);
        IHandler PickReadyPosition(ModuleName chamber, int slot, Hand hand);
        IHandler Place(ModuleName chamber, int slot, Hand hand);
        IHandler PlaceExtend(ModuleName chamber, int slot, Hand hand);
        IHandler PlaceRetract(ModuleName chamber, int slot, Hand hand);
        IHandler PlaceReadyPosition(ModuleName chamber, int slot, Hand hand);
        
        IHandler Extend(ModuleName chamber, int slot, Hand hand);
        IHandler Retract(ModuleName chamber, int slot, Hand hand);

        IHandler Exchange(ModuleName chamber, int slot, Hand hand);
        IHandler ExchangeReadyPosition(ModuleName chamber, int slot, Hand pickHand, Hand placeHand);
        IHandler ExchangePickExtend(ModuleName chamber, int slot, Hand pickHand, Hand placeHand);
        IHandler ExchangePlaceExtend(ModuleName chamber, int slot, Hand pickHand, Hand placeHand);
        IHandler ExchangePlaceRetract(ModuleName chamber, int slot, Hand pickHand, Hand placeHand);

        IHandler ExchangeReadyExtend(ModuleName chamber, int slot, Hand pickHand, Hand placeHand);
        IHandler ExchangeAfterReadyExtend(ModuleName chamber, int slot, Hand pickHand, Hand placeHand);

        IHandler Goto(ModuleName chamber, int slot, Motion next, Hand hand, int x,int y, int z);
        
        IHandler MoveTo(ModuleName chamber, int slot,Hand hand, bool isPick, int x,int y, int z);
    
        IHandler WaferMapping(ModuleName loadport);
        IHandler QueryWaferMap(ModuleName loadport);

        IHandler PickEx(ModuleName chamber, int slot, Hand hand, int x, int y, int z);
        IHandler PlaceEx(ModuleName chamber, int slot, Hand hand, int x, int y, int z);
        
        IHandler ExchangeEx(ModuleName chamber, int slot, Hand hand, int x, int y, int z);

        IHandler PositionAdjustment(Axis axis, Hand hand, int value);

        IHandler SetCommunication();

        IHandler SetLoad(Hand hand,bool isOn);

        IHandler CheckLoad(ModuleName chamber, int slot);

        IHandler RequestWaferPresent();
        IHandler RequestWaferPresentLoadA();
        IHandler RequestWaferPresentLoadB();

        IHandler RequestAWCData();

        IHandler SetWaferSize(int size);
        IHandler QueryWaferSize();
        IHandler SetWaferSize(int cmd, WaferSize size);
        IHandler QueryParameter(int parameter, string parameterType);
        IHandler SetServoOnOff(bool trueForOnFalseForOff);
    }




}
