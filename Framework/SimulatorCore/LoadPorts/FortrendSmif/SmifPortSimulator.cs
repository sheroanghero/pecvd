using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aitex.Core.RT.Log;
using Brooks.WinSECS;
using MECF.Framework.Simulator.Core.Driver;

namespace MECF.Framework.Simulator.Core.LoadPorts.SecsSmif
{
    public class SmifPortSimulator
    {
        private WinSECS m_winsecs;
        private SmifAgent m_agent;
        public string SlotMap
        {
            get { return string.Join("", _slotMap); }
        }

        private string[] _slotMap = new string[25];
        private string[] _state = new string[20];
        private string[] _led = new string[13];

        public string InforPadState { get; set; } = "0";


        private string _comPort;
        public SmifPortSimulator(string portName)
        {
            _comPort = portName;
            InitializeSimulator();

            for (int i = 0; i < _slotMap.Length; i++)
                _slotMap[i] = "0";
            for (int i = 0; i < _state.Length; i++)
                _state[i] = "0";
            for (int i = 0; i < _led.Length; i++)
                _led[i] = "0";


            //00110 01?10 10001 01100

            //A000A 41010 10001 01000
            _state[0] = "0"; //Equipment status 0 = Normal A = Recoverable error E = Fatal error
            _state[1] = "0"; //Mode 0 = Online                      1 = Teaching
            _state[2] = "0"; //Initial position 0 = Unexecuted       1 = Executed
            _state[3] = "0"; //Operation status 0 = Stopped           1 = Operating
            _state[4] = "0"; //Error code Error code (upper)

            _state[5] = "0"; // Error code Error code (lower)
            _state[6] = "0"; //Cassette presence 0 = None            1 = Normal position        2 = Error load
            _state[7] = "0"; //FOUP clamp status 0 = Open            1 = Close                  ? = Not defined
            _state[8] = "0"; //Latch key status 0 = Open             1 = Close                 ? = Not defined
            _state[9] = "0"; //Vacuum 0 = OFF 1 = ON

            _state[10] = "1"; //Door position 0 = Open position      1 = Close position         ? = Not defined
            _state[11] = "0"; //Wafer protrusion sensor 0 = Blocked. 1 = Unblocked.
            _state[12] = "0";
            _state[13] = "0";   //Dock Position 0 = Undock, 1 = dock;
            _state[14] = "0";

            _state[15] = "0";
            _state[16] = "0";
            _state[17] = "0";
            _state[18] = "0";
            _state[19] = InforPadState;

            //A000A 41010 10001 01000
            _led[0] = "0"; //LOAD
            _led[1] = "0"; //UNLOAD
            _led[2] = "0"; //OP.ACCESS
            _led[3] = "0"; //PRESENCE
            _led[4] = "0"; //PLACEMENT

            _led[5] = "0"; // ALARM
            _led[6] = "0"; //STATUS1
            _led[7] = "0"; //STATUS2
            _led[8] = "0"; // 
            _led[9] = "0"; // 

            _led[10] = "0"; // 
            _led[11] = "0"; // 
            _led[12] = "0";
        }

        public void InitializeSimulator()
        {
            m_winsecs = new WinSECS();

            m_winsecs.PortType = SECS_PORT_TYPE.SECS1_SERIAL;
            m_winsecs.AutoDevice = true;
            m_winsecs.DefaultDeviceID = 0;


            m_winsecs.Secs1.IgnoreSytemBytes = false;
            m_winsecs.MultipleOpen = true;
            //Properties that apply to all SECS1 port types, which includes RS232
            m_winsecs.Secs1.AcceptDupBlock = false;
            m_winsecs.Secs1.Interleave = true;
            m_winsecs.Secs1.RetryCount = 10;
            m_winsecs.Secs1.SecsHost = true;
            m_winsecs.Secs1.T1 = 2;
            m_winsecs.Secs1.T2 = 10;
            m_winsecs.Secs1.T3 = 30;
            m_winsecs.Secs1.T4 = 10;
            m_winsecs.Secs1.PortName = _comPort;
            m_winsecs.Secs1.BaudRate = 9600;


            m_agent = new SmifAgent(this);
            m_agent.WinsecsHost = m_winsecs;
            var ret = m_winsecs.OpenPort(m_agent);
        }

        public void ReportError(int code)
        {
            SECSTransactionBuilder.BuildS5F1(code,_ErrorDict[code.ToString()]).Send(m_winsecs);
        }


        public void PlaceCarrier()
        {
            SECSTransactionBuilder.BuildS6F3(2).Send(m_winsecs);
        }

        public void RemoveCarrier()
        {
            SECSTransactionBuilder.BuildS6F3(1).Send(m_winsecs);
        }

        public void ClearWafer()
        {
            for (int i = 0; i < _slotMap.Length; i++)
            {
                _slotMap[i] = "0";
            }
        }

        public void SetAllWafer()
        {
            for (int i = 0; i < _slotMap.Length; i++)
            {
                _slotMap[i] = "1";
            }
        }


        public void SetUpWafer()
        {
            for (int i = 0; i < _slotMap.Length; i++)
            {
                _slotMap[i] = i > 15 ? "1" : "0";
            }
        }


        public void SetLowWafer()
        {
            for (int i = 0; i < _slotMap.Length; i++)
            {
                _slotMap[i] = i < 10 ? "1" : "0";
            }
        }

        public void RandomWafer()
        {
            Random _rd = new Random();
            for (int i = 0; i < _slotMap.Length; i++)
            {
                var rtn = _rd.Next(0, 10) % 4;
                switch (rtn)
                {
                    case 0:
                        _slotMap[i] = "0";
                        break;
                    case 1:
                        _slotMap[i] = "1";
                        break;
                    case 2:
                        _slotMap[i] = "2";
                        break;
                    case 3:
                        _slotMap[i] = "W";
                        break;
                    default:
                        break;
                }
            }
        }
        private static Dictionary<string, string> _ErrorDict = new Dictionary<string, string>()
        {
                {"1","PLM Macro Error, Error Code 52" },
                {"2","Pod not present" },
                {"38","Latch error" },
                {"39","Pod Remove" },
                {"40","Mask Position Incorrect" },
                {"41","Inhibit Detected while Elevator going Down" },
                {"43","Protrusion Detected" },

                {"44","END EFFECTOR PROTRUSION" },
                {"45","PELLICLE ON TOP" },
                {"49","PLM not Ready" },
         };

    }

}
