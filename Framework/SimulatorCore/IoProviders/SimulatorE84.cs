using Aitex.Core.RT.IOCore;
using Aitex.Core.Util;

namespace MECF.Framework.Simulator.Core.IoProviders
{
    public enum E84Stage
    {
        WaitForStation,
        IDLE,
        TD0,    //when CS_0 wait for VALID
        TA1,    //CS_0 and VALID wait for L_REQ(U_REQ)
        TP1,    //when TA1 and TR_REQ is set
        TA2,    //
        TP2,
        TP3,
        TP4,
        TA3,
        TP5
    }
    
    public class SimulatorE84 
    {
        #region sequence

        /*
         * for loading sequence:
         * (1) Active set CS_0 *ON* then set VALID *ON* for valid confirm
         *     A:CS_0 *ON*    VALID *ON*
         * (2) Passive equipment set L_REQ *ON* if the load port is ready for loading operation / U_REQ for unload
         *     A:CS_0 *ON*    VALID *ON*
         *     P:L_REQ *ON \\ U_REQ *ON*    
         * (3) After active equipment receive the L_REQ/U_REQ *ON* set TR_REQ *ON*
         *     A:TR_TRQ *ON*    CS_0 *ON*    VALID *ON*
         *     P:L_REQ *ON \\ U_REQ *ON* 
         * (4) After passive equipment turn the READY *ON*, active equipment BUSY set to *ON* and proceed handoff operation
         *     A:BUSY *ON*    TR_TRQ *ON*    CS_0 *ON*    VALID *ON*
         *     P:READY *ON*    L_REQ *ON \\ U_REQ *ON* 
         * (5) When passive equipment turns the L_REQ(U_REQ) signal *OFF* when carrier is done processing
         *     A:BUSY *ON*    TR_TRQ *ON*    CS_0 *ON*    VALID *ON*
         *     P:READY *ON*
         * (6) After finish the operation and the active equipment is out of conflict area,
         *     active equipment turns the BUSY signal *OFF*(make sure after the L_REQ(R_REQ) was *OFF*)
         *     A:TR_TRQ *ON*    CS_0 *ON*    VALID *ON*
         *     P:READY *ON*
         * (7) After the BUSY *OFF*, active equipment set the TR_REQ signal *OFF*
         *     A:CS_0 *ON*    VALID *ON*
         *     P:READY *ON*
         * (8) When finish those above, active equipment set the COMPT *ON*
         *     A:COMPT *ON*    CS_0 *ON*    VALID *ON*
         *     P:READY *ON* 
         * (9) After COMPT *ON*, passive equipment should set the READY *OFF*
         *     A:COMPT *ON*    CS_0 *ON*    VALID *ON*
         *     P:
         * (10) After READY *OFF*, active set the signal all off
         *     A:
         *     P:
         */
        
        #endregion

        private readonly string _loadPortName;
        private PeriodicJob _thread;

        public readonly bool IsFloor;

        public bool IsLoading => Stage == E84Stage.TP2 && lReq;
        public bool IsUnLoading => Stage == E84Stage.TP2 && uReq;


        public string LoadPortName => _loadPortName;
        public bool LReq => lReq;
        public bool UReq => uReq;
        public bool Ready => ready;
        public bool HoAvbl => hoAvbl;
        public bool ES => es;
        public bool VA => va;
        public bool VS0 => vs0;
        public bool VS1 => vs1;
        
        //inputs
        private bool lReq;
        private bool uReq;
        private bool ready;
        private bool hoAvbl;
        private bool es;
        private bool va;
        private bool vs0;
        private bool vs1;
        
        private RD_TRIG _lReq = new RD_TRIG();
        private RD_TRIG _uReq = new RD_TRIG();
        private RD_TRIG _ready = new RD_TRIG();
        private RD_TRIG _hoAvbl = new RD_TRIG();
        private RD_TRIG _es = new RD_TRIG();
        private RD_TRIG _va = new RD_TRIG();
        private RD_TRIG _vs0 = new RD_TRIG();
        private RD_TRIG _vs1 = new RD_TRIG();
            
        //outputs
        public bool ON => IO.DI[$"DI_{_loadPortName}_ON"].Value;
        public bool VALID => IO.DI[$"DI_{_loadPortName}_VALID"].Value;
        public bool CS_0 => IO.DI[$"DI_{_loadPortName}_CS_0"].Value;
        public bool TR_REQ => IO.DI[$"DI_{_loadPortName}_TR_REQ"].Value;
        public bool BUSY => IO.DI[$"DI_{_loadPortName}_BUSY"].Value;
        public bool COMPT => IO.DI[$"DI_{_loadPortName}_COMPT"].Value;
        public bool CONT => IO.DI[$"DI_{_loadPortName}_CONT"].Value;
        public bool AM_AVBL => IO.DI[$"DI_{_loadPortName}_AM_AVBL"].Value;
        

        public E84Stage Stage;

        private readonly DeviceTimer _timerDelay = new DeviceTimer();

        public SimulatorE84(string loadPortName, bool isFloor)
        {
            _loadPortName = loadPortName;
            IsFloor = isFloor;
            _thread = new PeriodicJob(50, OnMonitor, _loadPortName, true);
            Stage = E84Stage.IDLE;
            if (isFloor)
                IO.DI[$"DI_{_loadPortName}_AM_AVBL"].Value = true;
            _thread.Start();
        }
        
        //DO_LP1_L_REQ        Load Request
        //DO_LP1_U_REQ        Unload Request
        //DO_LP1_READY        Ready Signal
        //DO_LP1_HO_AVBL      HandOff Available  Always on
        //DO_LP1_ES           Emergency Stop     Always on
        //DO_LP1_VA           Vehicle Arrive
        //DO_LP1_VS_O         Carrier Stage 0 
        //DO_LP1_VS_1         Carrier Stage 1
        private bool OnMonitor()
        {
            //inputs
            lReq = IO.DO[$"DO_{_loadPortName}_L_REQ"].Value;
            uReq = IO.DO[$"DO_{_loadPortName}_U_REQ"].Value;
            ready = IO.DO[$"DO_{_loadPortName}_READY"].Value;
            hoAvbl = IO.DO[$"DO_{_loadPortName}_HO_AVBL"].Value;
            es = IO.DO[$"DO_{_loadPortName}_ES"].Value;
            va = IO.DO[$"DO_{_loadPortName}_VA"].Value;
            vs0 = IO.DO[$"DO_{_loadPortName}_VS_O"].Value;
            vs1 = IO.DO[$"DO_{_loadPortName}_VS_1"].Value;

            _lReq.CLK = lReq;
            _uReq.CLK = uReq;
            _ready.CLK = ready;
            _hoAvbl.CLK = hoAvbl;
            _es.CLK = es;
            _va.CLK = va;
            _vs0.CLK = vs0;
            _vs1.CLK = vs1;
            
            //outputs
            if (IsFloor)
            {
                if (_es.T || _hoAvbl.T)
                {
                    Stage = E84Stage.WaitForStation;
                    IO.DI[$"DI_{_loadPortName}_CS_0"].Value = false;
                    IO.DI[$"DI_{_loadPortName}_VALID"].Value = false;
                    IO.DI[$"DI_{_loadPortName}_TR_REQ"].Value = false;
                    IO.DI[$"DI_{_loadPortName}_BUSY"].Value = false;
                    IO.DI[$"DI_{_loadPortName}_COMPT"].Value = false;
                }
                else if (_es.R || _hoAvbl.R)
                {
                    Stage = E84Stage.IDLE;
                }
                
                switch(Stage)
                {
                    case E84Stage.IDLE:
                    {
                        if ((lReq || uReq) && (vs0 || vs1))
                            Stage = E84Stage.TD0;
                        break;
                    }
                    case E84Stage.TD0:
                    {
                        if (va)
                            Stage = E84Stage.TA1;
                        break;
                    }
                    case E84Stage.TA1:
                    {
                        IO.DI[$"DI_{_loadPortName}_TR_REQ"].Value = true;
                        Stage = E84Stage.TA2;
                        break;
                    }
                    case E84Stage.TA2:
                    {
                        if (ready)
                            Stage = E84Stage.TP2;
                        break;
                    }
                    case E84Stage.TP2:
                        IO.DI[$"DI_{_loadPortName}_BUSY"].Value = true;
                        Stage = E84Stage.TP3;
                        break;
                    case E84Stage.TP3:
                    {
                        if (!uReq || !lReq)
                            Stage = E84Stage.TP4;
                        break;
                    }
                    case E84Stage.TP4:
                        IO.DI[$"DI_{_loadPortName}_BUSY"].Value = false;
                        IO.DI[$"DI_{_loadPortName}_TR_REQ"].Value = false;
                        IO.DI[$"DI_{_loadPortName}_COMPT"].Value = true;
                        Stage = E84Stage.TA3;
                        break;
                    case E84Stage.TA3:
                    {
                        if (!ready)
                            Stage = E84Stage.TP5;
                        break;
                    }
                    case E84Stage.TP5:
                        IO.DI[$"DI_{_loadPortName}_CS_0"].Value = false;
                        IO.DI[$"DI_{_loadPortName}_VALID"].Value = false;
                        IO.DI[$"DI_{_loadPortName}_COMPT"].Value = false;
                        Stage = E84Stage.IDLE;
                        break;
                }
            }
            else
            {
                if (_es.T || _hoAvbl.T)
                {
                    Stage = E84Stage.WaitForStation;
                    IO.DI[$"DI_{_loadPortName}_CS_0"].Value = false;
                    IO.DI[$"DI_{_loadPortName}_VALID"].Value = false;
                    IO.DI[$"DI_{_loadPortName}_TR_REQ"].Value = false;
                    IO.DI[$"DI_{_loadPortName}_BUSY"].Value = false;
                    IO.DI[$"DI_{_loadPortName}_COMPT"].Value = false;
                }
                else if (_es.R || _hoAvbl.R)
                {
                    Stage = E84Stage.IDLE;
                }

                switch(Stage)
                {
                    case E84Stage.IDLE:
                    {
                        IO.DI[$"DI_{_loadPortName}_CS_0"].Value = true;
                        Stage = E84Stage.TD0;
                        break;
                    }
                    case E84Stage.TD0:
                    {
                        IO.DI[$"DI_{_loadPortName}_VALID"].Value = true;
                        Stage = E84Stage.TA1;
                        break;
                    }
                    case E84Stage.TA1:
                    {
                        if (lReq || uReq)
                            Stage = E84Stage.TP1;
                        break;
                    }
                    case E84Stage.TP1:
                        IO.DI[$"DI_{_loadPortName}_TR_REQ"].Value = true;
                        Stage = E84Stage.TA2;
                        break;
                    case E84Stage.TA2:
                    {
                        if (ready)
                            Stage = E84Stage.TP2;
                        break;
                    }
                    case E84Stage.TP2:
                        IO.DI[$"DI_{_loadPortName}_BUSY"].Value = true;
                        Stage = E84Stage.TP3;
                        break;
                    case E84Stage.TP3:
                    {
                        if (!uReq || !lReq)
                            Stage = E84Stage.TP4;
                        break;
                    }
                    case E84Stage.TP4:
                        IO.DI[$"DI_{_loadPortName}_BUSY"].Value = false;
                        IO.DI[$"DI_{_loadPortName}_TR_REQ"].Value = false;
                        IO.DI[$"DI_{_loadPortName}_COMPT"].Value = true;
                        Stage = E84Stage.TA3;
                        break;
                    case E84Stage.TA3:
                    {
                        if (!ready)
                            Stage = E84Stage.TP5;
                        break;
                    }
                    case E84Stage.TP5:
                        IO.DI[$"DI_{_loadPortName}_CS_0"].Value = false;
                        IO.DI[$"DI_{_loadPortName}_VALID"].Value = false;
                        IO.DI[$"DI_{_loadPortName}_COMPT"].Value = false;
                        Stage = E84Stage.IDLE;
                        break;
                }
            }
            
            return true;
        }
    }
}