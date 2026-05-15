using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Aitex.Core.RT.Log;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using MECF.Framework.Common.Communications.Tcp.Socket.Server.APM.EventArgs;
using MECF.Framework.Simulator.Core.Commons;
using MECF.Framework.Simulator.Core.Driver;
using MECF.Framework.Simulator.Core.LoadPorts;
using MECF.Framework.Simulator.Core.LoadPorts.Hirata;
using MECF.Framework.Simulator.Core.Robots;

namespace MECF.Framework.Simulator.Core.SiasunEfem
{
    /// <summary>
    /// SiasunEfem.xaml 的交互逻辑
    /// </summary>
    public partial class SiasunEfemView : UserControl
    {
        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
            "Port", typeof(int), typeof(SiasunEfemView),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public void Initialize(string v1, string v2, int port)
        {
            Port = port;
        }

        public int Port
        {
            get
            {
                return (int)this.GetValue(PortProperty);
            }
            set
            {
                this.SetValue(PortProperty, value);
            }
        }
        public SiasunEfemView()
        {
            InitializeComponent();
            this.Loaded += OnViewLoaded;
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext == null)
            {
                //Port = 13000;
                DataContext = new SiasunEfemViewModel(Port,0);
                (DataContext as TimerViewModelBase).Start();
            }
        }
    }

    class SiasunEfemViewModel : SocketDeviceViewModel
    {
        public string Title
        {
            get { return "Efem Simulator"; }
        }

        public string LP1WaferMap
        {
            get { return _sim.LP1SlotMap; }
        }
        public string LP2WaferMap
        {
            get { return _sim.LP2SlotMap; }
        }
        public string LP3WaferMap
        {
            get { return _sim.LP3SlotMap; }
        }

        public string LP1InfoPadStatus
        {
            get { return _sim.LP1InforPadState; }
        }

        public string LP2InfoPadStatus
        {
            get { return _sim.LP2InforPadState; }
        }
        public string LP3InfoPadStatus
        {
            get { return _sim.LP3InforPadState; }
        }

        [IgnorePropertyChange]
        public string InfoPadSet { get; set; }

        public ObservableCollection<WaferItem> WaferList { get; set; }

        public ICommand PlaceCommand { get; set; }
        public ICommand RemoveCommand { get; set; }

        public ICommand ClearCommand { get; set; }
        public ICommand SetAllCommand { get; set; }
        public ICommand RandomCommand { get; set; }

        public ICommand SetInfoPadCommand { get; set; }

        private EfemSimulator _sim;

        public SiasunEfemViewModel(int port, int index) : base("SiasunEfemViewModel")
        {
            PlaceCommand = new DelegateCommand<string>(Place);
            RemoveCommand = new DelegateCommand<string>(Remove);

            ClearCommand = new DelegateCommand<string>(Clear);
            SetAllCommand = new DelegateCommand<string>(SetAll);
            RandomCommand = new DelegateCommand<string>(RandomGenerateWafer);
            SetInfoPadCommand = new DelegateCommand<string>(SetInfoPadStatus);

            _sim = new EfemSimulator(port);
            Init(_sim);
            WaferList = new ObservableCollection<WaferItem>()
            {
                new WaferItem {Display = "1", Index = 2, State = 3}
            };
            if (index == 1)
            {
                _sim.SetUpWafer("LP1");
                _sim.SetUpWafer("LP2");
                _sim.SetUpWafer("LP3");
            }
            else
            {
                _sim.SetLowWafer("LP1");
                _sim.SetUpWafer("LP2");
                _sim.SetLowWafer("LP3");
            }
        }

        private void SetInfoPadStatus(string obj)
        {
            if (obj == "LP1")
            {
                _sim.LP1InforPadState = InfoPadSet;
            }
            if (obj == "LP2")
            {
                _sim.LP2InforPadState = InfoPadSet;
            }
            if (obj == "LP3")
            {
                _sim.LP3InforPadState = InfoPadSet;
            }
        }

        private void RandomGenerateWafer(string obj)
        {
            _sim.RandomWafer(obj);
        }

        private void SetAll(string obj)
        {
            _sim.SetAllWafer(obj);
        }

        private void Clear(string obj)
        {
            _sim.ClearWafer(obj);
        }

        private void Remove(string obj)
        {
            _sim.RemoveCarrier(obj);
        }

        private void Place(string obj)
        {
            _sim.PlaceCarrier(obj);
        }
    }

    public class EfemSimulator : SocketDeviceSimulator
    {
        public string LP1SlotMap
        {
            get { return string.Join("", _lp1slotMap); }
        }
        public string LP2SlotMap
        {
            get { return string.Join("", _lp2slotMap); }
        }
        public string LP3SlotMap
        {
            get { return string.Join("", _lp3slotMap); }
        }
        //public event Action<string> WriteDeviceEvent;

        private bool[] _lp1SigStateData1 = new bool[32];
        private bool[] _lp1SigStateData2 = new bool[32];

        private bool[] _lp2SigStateData1 = new bool[32];
        private bool[] _lp2SigStateData2 = new bool[32];

        private bool[] _lp3SigStateData1 = new bool[32];
        private bool[] _lp3SigStateData2 = new bool[32];

        private string[] _lp1slotMap = new string[25];
        private string[] _lp2slotMap = new string[25];
        private string[] _lp3slotMap = new string[25];

        private string[] _stationslotMap = new string[25];

        public string LP1InforPadState { get; set; } = "0";
        public string LP2InforPadState { get; set; } = "0";
        public string LP3InforPadState { get; set; } = "0";

        //private LpState _lp1State;
        //private E84State _lp1E84State;
        //private SystemState _systemState;
        //private bool _isPlaced;
        //private bool _isPresent;

        //private int _moveTime = 5000; //


        private PeriodicJob _HwThread;
        private bool _bCommReady;


        public EfemSimulator(int tcpPort)
            : base(tcpPort, -1, "\r", ' ')
        {
            for (int i = 0; i < _lp1slotMap.Length; i++)
                _lp1slotMap[i] = "0";
            for (int i = 0; i < _lp2slotMap.Length; i++)
                _lp2slotMap[i] = "0";
            for (int i = 0; i < _lp3slotMap.Length; i++)
                _lp3slotMap[i] = "0";
            for (int i = 0; i < _stationslotMap.Length; i++)
                _stationslotMap[i] = "0";

            for (int i = 0; i < _lp1SigStateData1.Length; i++)
                _lp1SigStateData1[i] = false;
            for (int i = 0; i < _lp1SigStateData2.Length; i++)
                _lp1SigStateData2[i] = false;

            for (int i = 0; i < _lp2SigStateData1.Length; i++)
                _lp2SigStateData1[i] = false;
            for (int i = 0; i < _lp2SigStateData2.Length; i++)
                _lp2SigStateData2[i] = false;

            for (int i = 0; i < _lp3SigStateData1.Length; i++)
                _lp3SigStateData1[i] = false;
            for (int i = 0; i < _lp3SigStateData2.Length; i++)
                _lp3SigStateData2[i] = false;

            _lp1SigStateData1[0] = false; //LP Placement
            _lp1SigStateData1[1] = false; //Pod Present
            _lp1SigStateData1[2] = false; //Access SW
            _lp1SigStateData1[3] = false; //Pod lock
            _lp1SigStateData1[4] = true; //Cover close

            _lp1SigStateData1[5] = false; // Cover lock
            _lp1SigStateData1[6] = false;
            _lp1SigStateData1[7] = false;
            _lp1SigStateData1[8] = false; //Info pad A
            _lp1SigStateData1[9] = false; //Info pad B

            _lp1SigStateData1[10] = false; //Info pad C
            _lp1SigStateData1[11] = false; //Info pad D

            _lp1SigStateData1[24] = false; //VALID (E84)
            _lp1SigStateData1[25] = false; //CS_0 (E84)
            _lp1SigStateData1[26] = false; //CS_1 (E84)
            _lp1SigStateData1[27] = false; //Valid
            _lp1SigStateData1[28] = false; //TR_REQ (E84)
            _lp1SigStateData1[29] = false; //BUSY (E84)
            _lp1SigStateData1[30] = false; //COMPT (E84)
            _lp1SigStateData1[31] = false; //CONT (E84)

            _lp1SigStateData2[0] = false; //PRESENCE
            _lp1SigStateData2[1] = false; //PLACEMENT
            _lp1SigStateData2[2] = false; //LOAD
            _lp1SigStateData2[3] = false; //UNLOAD
            _lp1SigStateData2[4] = false; //MANUAL MODE

            _lp1SigStateData2[5] = false; //ERROR
            _lp1SigStateData2[6] = false; //CLAMP / UNCLAMP
            _lp1SigStateData2[7] = false; //DOCK/UNDOCK
            _lp1SigStateData2[8] = false; //ACCESS SW

            //A000A 41010 10001 01000
            _lp2SigStateData1[0] = false; //LP Placement
            _lp2SigStateData1[1] = false; //Pod Present
            _lp2SigStateData1[2] = false; //Access SW
            _lp2SigStateData1[3] = false; //Pod lock
            _lp2SigStateData1[4] = true; //Cover close

            _lp2SigStateData1[5] = false; // Cover lock
            _lp2SigStateData1[6] = false;
            _lp2SigStateData1[7] = false;
            _lp2SigStateData1[8] = false; //Info pad A
            _lp2SigStateData1[9] = false; //Info pad B

            _lp2SigStateData1[10] = false; //Info pad C
            _lp2SigStateData1[11] = false; //Info pad D

            _lp2SigStateData1[24] = false; //VALID (E84)
            _lp2SigStateData1[25] = false; //CS_0 (E84)
            _lp2SigStateData1[26] = false; //CS_1 (E84)
            _lp2SigStateData1[27] = false; //Valid
            _lp2SigStateData1[28] = false; //TR_REQ (E84)
            _lp2SigStateData1[29] = false; //BUSY (E84)
            _lp2SigStateData1[30] = false; //COMPT (E84)
            _lp2SigStateData1[31] = false; //CONT (E84)

            _lp2SigStateData2[0] = false; //PRESENCE
            _lp2SigStateData2[1] = false; //PLACEMENT
            _lp2SigStateData2[2] = false; //LOAD
            _lp2SigStateData2[3] = false; //UNLOAD
            _lp2SigStateData2[4] = false; //MANUAL MODE

            _lp2SigStateData2[5] = false; //ERROR
            _lp2SigStateData2[6] = false; //CLAMP / UNCLAMP
            _lp2SigStateData2[7] = false; //DOCK/UNDOCK
            _lp2SigStateData2[8] = false; //ACCESS SW

            //A000A 41010 10001 01000
            _lp3SigStateData1[0] = false; //LP Placement
            _lp3SigStateData1[1] = false; //Pod Present
            _lp3SigStateData1[2] = false; //Access SW
            _lp3SigStateData1[3] = false; //Pod lock
            _lp3SigStateData1[4] = true; //Cover close

            _lp3SigStateData1[5] = false; // Cover lock
            _lp3SigStateData1[6] = false;
            _lp3SigStateData1[7] = false;
            _lp3SigStateData1[8] = false; //Info pad A
            _lp3SigStateData1[9] = false; //Info pad B

            _lp3SigStateData1[10] = false; //Info pad C
            _lp3SigStateData1[11] = false; //Info pad D

            _lp3SigStateData1[24] = false; //VALID (E84)
            _lp3SigStateData1[25] = false; //CS_0 (E84)
            _lp3SigStateData1[26] = false; //CS_1 (E84)
            _lp3SigStateData1[27] = false; //Valid
            _lp3SigStateData1[28] = false; //TR_REQ (E84)
            _lp3SigStateData1[29] = false; //BUSY (E84)
            _lp3SigStateData1[30] = false; //COMPT (E84)
            _lp3SigStateData1[31] = false; //CONT (E84)

            _lp3SigStateData2[0] = false; //PRESENCE
            _lp3SigStateData2[1] = false; //PLACEMENT
            _lp3SigStateData2[2] = false; //LOAD
            _lp3SigStateData2[3] = false; //UNLOAD
            _lp3SigStateData2[4] = false; //MANUAL MODE

            _lp3SigStateData2[5] = false; //ERROR
            _lp3SigStateData2[6] = false; //CLAMP / UNCLAMP
            _lp3SigStateData2[7] = false; //DOCK/UNDOCK
            _lp3SigStateData2[8] = false; //ACCESS SW

            BinaryDataMode = false;
        }

        //public void ChangeSlotMap(SlotMapChangedEventArgs obj)
        //{
        //    string slotMap = obj.SlotMap.Replace("'", "");
        //    for (int i = 0; i < slotMap.Length; i++)
        //        _slotMap[i] = slotMap.Substring(i, 1);
        //}

        public override void ClientConnected(object sender, TcpClientConnectedEventArgs e)
        {
            base.ClientConnected(sender, e);
            Thread.Sleep(10);
            OnWriteMessage("INF:READY/COMM;");
            LOG.Info("Send message " + "INF:READY/COMM;");



            _HwThread = new PeriodicJob(5000, OnSendEvent, "EfemHardware", true);
        }

        private bool OnSendEvent()
        {
            if (!_bCommReady)
            {
                OnWriteMessage("INF:READY/COMM;");
            }
            return true;
        }

        protected override void ProcessUnsplitMessage(string message)
        {
            LOG.Info("Received message " + message);
            string[] msg = message.Split(':');
            if (msg.Length < 2)
                return;

            if (!_bCommReady && message.Contains("ACK:READY/COMM"))
            {
                _bCommReady = true;
            }

            string type = msg[0];
            string cmd = msg[1];
            switch (type)
            {
                case "SET":
                    ReceiveSetCommand(cmd);
                    break;
                case "MOD":
                    ReceiveModCommand(cmd);
                    break;
                case "GET":
                    ReceiveGetCommand(cmd);
                    break;
                case "FIN":
                    ReceiveFinCommand(cmd);
                    break;
                case "MOV":
                    ReceiveMovCommand(cmd);
                    break;
                case "EVT":
                    ReceiveEvtCommand(cmd);
                    break;
                case "TCH":
                    ReceiveTchCommand(cmd);
                    break;
                default:
                    ReceiveUnknownCommand(message);
                    break;
            }
        }

        private void ReceiveMovCommand(string cmd)
        {
            bool needINF = true;
            string[] data = cmd.Split('/');
            bool needSendMap = false;
            switch (data[0])
            {
                case "INIT":

                    break;
                case "ORGSH": //back to initial

                    if (data[1] == "ALL")
                    {
                    }
                    if (data[1] == "P1")
                    {
                    }
                    if (data[1] == "P2")
                    {
                    }

                    break;
                case "ABORG": //Force back to initial

                    break;
                case "UNLOCK": //FOUP clamp: Close
                    ReceiveCommandUNLOCK(data[1]);
                    break;
                case "LOCK": //FOUP clamp: open
                    ReceiveCommandLOCK(data[1]);
                    break;
                case "DOCK": //FOUP dock:
                    ReceiveCommandDOCK(data[1]);
                    break;
                case "UNDOCK": //FOUP undock
                    ReceiveCommandUNDOCK(data[1]);
                    break;
                case "OPEN": //Door open
                    ReceiveCommandOPEN(data[1]);
                    break;
                case "CLOSE": //Door Close
                    ReceiveCommandCLOSE(data[1]);
                    break;
                case "WAFSH": //Map
                    needSendMap = true;
                    break;
                case "GOTO": //Maps and loads the FOUP.

                    break;

                case "CSTID":
                    break;
                default:
                    break;
            }

            SendAck(cmd);

            Thread.Sleep(500);
            if (needINF)
            {
                if (needSendMap)
                {
                    Thread.Sleep(200);
                    SendMapEvent(data[1]);
                }

                if (data[0] == "CSTID")
                    FeedbackCarrierID(cmd);
                else
                    SendInf(cmd);
            }

        }

        private void ReceiveCommandCLOSE(string module)
        {
            module = module.Replace(";", "").Replace("\r", "");
            if (module == "P1")
            {
                _lp1SigStateData1[4] = true;
                //_lp1SigStateData1[5] = false;
            }
            if (module == "P2")
            {
                _lp2SigStateData1[4] = true;
                //_lp2SigStateData1[5] = false;
            }
            if (module == "P3")
            {
                _lp3SigStateData1[4] = true;
                //_lp3SigStateData1[5] = false;
            }
        }

        private void ReceiveCommandOPEN(string module)
        {
            module = module.Replace(";", "").Replace("\r", "");
            if (module == "P1")
            {
                _lp1SigStateData1[4] = false;
                //_lp1SigStateData1[5] = true;
            }

            if (module == "P2")
            {
                _lp2SigStateData1[4] = false;
                //_lp2SigStateData1[5] = true;
            }
            if (module == "P3")
            {
                _lp3SigStateData1[4] = false;
                //_lp3SigStateData1[5] = true;
            }
        }

        private void ReceiveCommandDOCK(string module)
        {
            module = module.Replace(";", "").Replace("\r", "");
            if (module == "P1")
                _lp1SigStateData1[5] = true;
            if (module == "P2")
                _lp2SigStateData1[5] = true;
            if (module == "P3")
                _lp3SigStateData1[5] = true;
        }

        private void ReceiveCommandUNDOCK(string module)
        {
            module = module.Replace(";", "").Replace("\r", "");
            if (module == "P1")
                _lp1SigStateData1[5] = false;
            if (module == "P2")
                _lp2SigStateData1[5] = false;
            if (module == "P3")
                _lp3SigStateData1[5] = false;
        }

        private void ReceiveCommandLOCK(string module)
        {
            module = module.Replace(";", "").Replace("\r", "");
            if (module == "P1")
                _lp1SigStateData1[3] = true;
            if (module == "P2")
                _lp2SigStateData1[3] = true;
            if (module == "P3")
                _lp3SigStateData1[3] = true;
        }

        private void ReceiveCommandUNLOCK(string module)
        {
            module = module.Replace(";", "").Replace("\r", "");
            if (module == "P1")
                _lp1SigStateData1[3] = false;
            if (module == "P2")
                _lp2SigStateData1[3] = false;
            if (module == "P3")
                _lp3SigStateData1[3] = false;
        }

        private void ReceiveSetCommand(string cmd)
        {
            string[] data = cmd.Split('/');
            switch (data[0])
            {
                case "SIGOUT":
                    if (data[1] == "P1")
                    {
                        if (data[2] == "LOAD")
                        {
                            if (data[3] == "ON")
                            {
                                _lp1SigStateData2[2] = true;
                            }
                            else _lp1SigStateData2[2] = false;
                        }
                        if (data[2] == "UNLOAD")
                        {
                            if (data[3] == "ON")
                            {
                                _lp1SigStateData2[3] = true;
                            }
                            else _lp1SigStateData2[3] = false;
                        }
                        if (data[2] == "MANUAL MODE")
                        {
                            if (data[3] == "ON")
                            {
                                _lp1SigStateData2[4] = true;
                            }
                            else _lp1SigStateData2[4] = false;
                        }
                    }
                    if (data[1] == "P2")
                    {
                        if (data[2] == "LOAD")
                        {
                            if (data[3] == "ON")
                            {
                                _lp2SigStateData2[2] = true;
                            }
                            else _lp2SigStateData2[2] = false;
                        }
                        if (data[2] == "UNLOAD")
                        {
                            if (data[3] == "ON")
                            {
                                _lp2SigStateData2[3] = true;
                            }
                            else _lp2SigStateData2[3] = false;
                        }
                        if (data[2] == "MANUAL MODE")
                        {
                            if (data[3] == "ON")
                            {
                                _lp2SigStateData2[4] = true;
                            }
                            else _lp2SigStateData2[4] = false;
                        }
                    }

                    break;

                case "SIZE":
                    if (data[1] == "ARM1")
                        robotArm1Wafersize = data[2];
                    if (data[1] == "ARM2")
                        robotArm2Wafersize = data[2];
                    if (data[1] == "ALIGN1")
                        alignerwafersize = data[2];
                    break;
            }
            SendAck(cmd);
            if (cmd.Contains("FSB")) return;

            if (cmd.Contains("ALIGN")) return;

            if (cmd.Contains("ERROR")) return;
            SendInf(cmd);
        }

        private void ReceiveModCommand(string cmd)
        {
            SendAck(cmd);
        }

        #region GET Command

        private void ReceiveGetCommand(string cmdGet)
        {
            string message = $"ACK:{cmdGet}";
            OnWriteMessage(message);
            string[] data = cmdGet.Split('/');

            switch (data[0])
            {
                case "MAPDT":
                    FeedbackGetWaferMapDescendingOrder(cmdGet);
                    break;
                case "ERROR":
                    break;
                case "CLAMP":
                    break;
                case "STATE":
                    FeedbackGetStatus(cmdGet);
                    break;
                case "MODE":
                    break;
                case "TRANSREQ":
                    break;
                case "SIGSTAT":
                    FeedbackGetSigStatus(cmdGet);
                    break;
                case "EVENT":
                    break;
                case "CSTID":
                    FeedbackCarrierID(cmdGet);
                    break;
                case "SIZE":
                    FeedbackWaferSize(cmdGet);
                    break;
                case "ADPLOCK":
                    break;
                case "ADPUNLOCK":
                    break;
            }
        }

        private void FeedbackWaferSize(string cmdGet)
        {
            //string message = string.Format("s00ACK:{0}/{1};\r", cmd, string.Join("", _state));
            string[] data = cmdGet.Replace(";", "").Split('/');

            string message = $"INF:{cmdGet.Replace(";", "")}/{robotArm1Wafersize};";

            if (message.Contains("BF1") || message.Contains("BF2") || message.Contains("BF3"))
                message = message.Replace("300", "NONE");

            if (message.Contains("STATION"))
                message = message.Replace("300", "NONE");

            OnWriteMessage(message);
        }

        private string robotArm1Wafersize = "300";
        private string robotArm2Wafersize = "300";
        private string alignerwafersize = "300";

        private int _cid = 0;

        private void FeedbackCarrierID(string cmd)
        {
            _cid++;
            if (_cid > 1)
                _cid = 1;

            string message = $"INF:{cmd.Replace(";", "")}/{_cid};\r";
            OnWriteMessage(message);
        }

        private void FeedbackGetStatus(string cmd)
        {
            //string message = string.Format("s00ACK:{0}/{1};\r", cmd, string.Join("", _state));
            string[] data = cmd.Split('/');
            string reply = "";
            if (data[1] == "TRACK;")
            {
                reply = "00000";
            }
            if (data[1] == "VER;")
            {
                reply = "01.000(2021-03-11)";
            }
            if (data[1] == "INF1;")
            {
                reply = "0.06";
            }
            string message = $"INF:{cmd.Replace(";", "")}/{reply};\r";
            OnWriteMessage(message);
        }

        private void FeedbackGetSigStatus(string cmd)
        {
            string message = "";
            string[] data = cmd.Split('/');
            if (data[1] == "SYSTEM;")
            {
                message = "FFFF/FFFF";
            }
            if (data[1] == "P1;")
            {
                message = $"{ConvertBinToHexString(_lp1SigStateData1)}/{ConvertBinToHexString(_lp1SigStateData2)}";
            }
            if (data[1] == "P2;")
            {
                message = $"{ConvertBinToHexString(_lp2SigStateData1)}/{ConvertBinToHexString(_lp2SigStateData2)}";
            }
            if (data[1] == "P3;")
            {
                message = $"{ConvertBinToHexString(_lp3SigStateData1)}/{ConvertBinToHexString(_lp3SigStateData2)}";
            }
            string datamsg = $"INF:{cmd.Replace(";", "")}/{message};\r";
            OnWriteMessage(datamsg);
        }

        private void FeedbackGetVersion()
        {
        }

        private void SendMapEvent(string module)
        {
            module = module.Replace(";", "").Replace("\r", "");
            var sm = _lp1slotMap.Reverse();

            if (module.Contains("P1"))
                sm = _lp1slotMap.Reverse();

            if (module.Contains("P2"))
                sm = _lp2slotMap.Reverse();

            if (module.Contains("P3"))
                sm = _lp3slotMap.Reverse();

            if (module.Contains("STATION"))
                sm = _stationslotMap.Reverse();

            if (module.Contains("LL"))
                sm = new string[] { "00000000" };

            string message = $"EVT:MAPDT/{module}/{string.Join("", sm)};\r";
            OnWriteMessage(message);
        }

        //25 - 1
        private void FeedbackGetWaferMapDescendingOrder(string cmd)
        {
            var sm = _lp1slotMap.Reverse();

            if (cmd.Contains("P1"))
                sm = _lp1slotMap.Reverse();

            if (cmd.Contains("P2"))
                sm = _lp2slotMap.Reverse();

            if (cmd.Contains("P3"))
                sm = _lp3slotMap.Reverse();

            if (cmd.Contains("STATION"))
                sm = _stationslotMap.Reverse();

            if (cmd.Contains("LL"))
                sm = new string[] { "00000000" };

            string message = $"INF:{cmd.Replace(";", "")}/{string.Join("", sm)};\r";
            OnWriteMessage(message);
        }

        //1-25

        private void FeedbackGetWaferCount()
        {
        }

        #endregion GET Command

        private void ReceiveFinCommand(string cmd)
        {
            SendAck(cmd);
        }

        private void ReceiveEvtCommand(string cmd)
        {
            SendAck(cmd);
        }

        private void ReceiveTchCommand(string cmd)
        {
            SendAck(cmd);
        }

        private void ReceiveUnknownCommand(string message)
        {
            //LOG.Write("LoadPort" + _loadPortNumber + " Receive Unknown message," + message);
        }

        private void SendInf(string cmd)
        {
            Thread.Sleep(1500);

            //string msg = _isPlaced ? "s00INF:PODON;\r" : "s00INF:PODOF;\r";

            string message = Encoding.ASCII.GetString(BuildMesage($"INF:{cmd}"));
            OnWriteMessage("INF:" + cmd);
            LOG.Info("Send message " + "INF:" + cmd);
        }

        private void SendAck(string cmd)
        {
            string ack = Encoding.ASCII.GetString(BuildMesage($"ACK:{cmd}"));

            OnWriteMessage("ACK:" + cmd);
            LOG.Info("Send message " + "ACK:" + cmd);
        }

        public static byte[] BuildMesage(string data)
        {
            List<byte> ret = new List<byte>();

            ret.Add(0x1);

            List<byte> cmd = new List<byte>();
            foreach (char c in data)
            {
                cmd.Add((byte)c);
            }
            //cmd.Add((byte)(':'));    //3A

            cmd.Add((byte)(';'));  //3B

            int length = cmd.Count + 4;

            int checksum = length;
            foreach (byte bvalue in cmd)
            {
                checksum += bvalue;
            }

            byte[] byteschecksum = Encoding.ASCII.GetBytes(Convert.ToString((int)((byte)(checksum & 0xFF)), 16));
            byte[] blength = BitConverter.GetBytes((short)length);
            ret.Add(blength[1]);
            ret.Add(blength[0]);
            ret.AddRange(new byte[] { 0, 0 });
            ret.AddRange(cmd);
            ret.AddRange(byteschecksum);
            ret.Add(0xD);
            return ret.ToArray();
        }

        public void PlaceCarrier(string lp)
        {
            string message = "";
            if (lp == "P1")
            {
                _lp1SigStateData1[0] = true; //LP Placement
                _lp1SigStateData1[1] = false; //Pod Present.
                _lp1SigStateData1[2] = false; //ACCESS SW, Pressed
                _lp1SigStateData1[3] = false; //PodLock, Unlocked
                _lp1SigStateData1[5] = false; //Dock, UnDock
                //_lp1SigStateData1[6] = true; //wafer size 8.
                //_lp1SigStateData1[7] = false; //wafer size 12.

                message = $"EVT:SIGSTAT/{lp}/{ConvertBinToHexString(_lp1SigStateData1)}" +
                    $"/{ConvertBinToHexString(_lp1SigStateData2)}";
            }
            if (lp == "P2")
            {
                _lp2SigStateData1[0] = true; //LP Placement
                _lp2SigStateData1[1] = false; //Pod Present
                _lp2SigStateData1[2] = false; //ACCESS SW, Pressed
                _lp2SigStateData1[3] = false; //PodLock, Unlocked
                _lp2SigStateData1[5] = false; //Dock, UnDock
                //_lp2SigStateData1[6] = true; //wafer size 8.
                //_lp2SigStateData1[7] = false; //wafer size 12.
                message = $"EVT:SIGSTAT/{lp}/{ConvertBinToHexString(_lp2SigStateData1)}" +
                    $"/{ConvertBinToHexString(_lp2SigStateData2)}";
            }
            if (lp == "P3")
            {
                _lp3SigStateData1[0] = true; //LP Placement
                _lp3SigStateData1[1] = false; //Pod Present
                _lp3SigStateData1[6] = false; //wafer size 8.
                _lp3SigStateData1[7] = true; //wafer size 12.
                _lp3SigStateData1[5] = false; //Dock, UnDock
                message = $"EVT:SIGSTAT/{lp}/{ConvertBinToHexString(_lp3SigStateData1)}" +
                    $"/{ConvertBinToHexString(_lp3SigStateData2)}";
            }
            OnWriteMessage(message + ";\r");
        }

        private string ConvertBinToHexString(bool[] data)
        {
            uint intdata = 0;

            for (int i = 0; i < data.Length; i++)
            {
                intdata += data[i] ? (uint)Math.Pow(2, i) : 0;
            }
            return intdata.ToString("X8");
        }

        public void RemoveCarrier(string lp)
        {
            string message = "";
            if (lp == "P1")
            {
                _lp1SigStateData1[0] = false; //LP Placement
                _lp1SigStateData1[1] = true; //Pod Present.

                message = $"EVT:SIGSTAT/{lp}/{ConvertBinToHexString(_lp1SigStateData1)}" +
                    $"/{ConvertBinToHexString(_lp1SigStateData2)}";
            }
            if (lp == "P2")
            {
                _lp2SigStateData1[0] = false; //LP Placement
                _lp2SigStateData1[1] = true; //Pod Present
                message = $"EVT:SIGSTAT/{lp}/{ConvertBinToHexString(_lp2SigStateData1)}" +
                    $"/{ConvertBinToHexString(_lp2SigStateData2)}";
            }
            if (lp == "P3")
            {
                _lp3SigStateData1[0] = false; //LP Placement
                _lp3SigStateData1[1] = true; //Pod Present
                message = $"EVT:SIGSTAT/{lp}/{ConvertBinToHexString(_lp3SigStateData1)}" +
                    $"/{ConvertBinToHexString(_lp3SigStateData2)}";
            }
            OnWriteMessage(message + ";\r");
        }

        public void ClearWafer(string lp)
        {
            if (lp == "LP1")
            {
                for (int i = 0; i < _lp1slotMap.Length; i++)
                {
                    _lp1slotMap[i] = "0";
                }
            }
            if (lp == "LP2")
            {
                for (int i = 0; i < _lp2slotMap.Length; i++)
                {
                    _lp2slotMap[i] = "0";
                }
            }
            if (lp == "LP3")
            {
                for (int i = 0; i < _lp3slotMap.Length; i++)
                {
                    _lp3slotMap[i] = "0";
                }
            }
        }

        public void SetAllWafer(string lp)
        {
            if (lp == "LP1")
            {
                for (int i = 0; i < _lp1slotMap.Length; i++)
                {
                    _lp1slotMap[i] = "1";
                }
            }
            if (lp == "LP2")
            {
                for (int i = 0; i < _lp2slotMap.Length; i++)
                {
                    _lp2slotMap[i] = "1";
                }
            }
            if (lp == "LP3")
            {
                for (int i = 0; i < _lp3slotMap.Length; i++)
                {
                    _lp3slotMap[i] = "1";
                }
            }
        }

        public void SetUpWafer(string lp)
        {
            if (lp == "LP1")
            {
                for (int i = 0; i < _lp1slotMap.Length; i++)
                {
                    _lp1slotMap[i] = i > 15 ? "1" : "0";
                }
            }
            if (lp == "LP2")
            {
                for (int i = 0; i < _lp2slotMap.Length; i++)
                {
                    _lp2slotMap[i] = i > 15 ? "1" : "0";
                }
            }
            if (lp == "LP3")
            {
                for (int i = 0; i < _lp3slotMap.Length; i++)
                {
                    _lp3slotMap[i] = i > 15 ? "1" : "0";
                }
            }
        }

        public void SetLowWafer(string lp)
        {
            if (lp == "LP1")
            {
                for (int i = 0; i < _lp1slotMap.Length; i++)
                {
                    _lp1slotMap[i] = i < 10 ? "1" : "0";
                }
            }
            if (lp == "LP2")
            {
                for (int i = 0; i < _lp2slotMap.Length; i++)
                {
                    _lp2slotMap[i] = i < 10 ? "1" : "0";
                }
            }
            if (lp == "LP3")
            {
                for (int i = 0; i < _lp3slotMap.Length; i++)
                {
                    _lp3slotMap[i] = i < 10 ? "1" : "0";
                }
            }
        }

        public void RandomWafer(string lp)
        {
            Random _rd = new Random();
            if (lp == "LP1")
            {
                for (int i = 0; i < _lp1slotMap.Length; i++)
                {
                    _lp1slotMap[i] = _rd.Next(0, 10) < 6 ? "0" : "1";
                }
            }
            if (lp == "LP2")
            {
                for (int i = 0; i < _lp2slotMap.Length; i++)
                {
                    _lp2slotMap[i] = _rd.Next(0, 10) < 6 ? "0" : "1";
                }
            }
            if (lp == "LP3")
            {
                for (int i = 0; i < _lp3slotMap.Length; i++)
                {
                    _lp3slotMap[i] = _rd.Next(0, 10) < 6 ? "0" : "1";
                }
            }
        }

        public class LpState
        {
            public bool IsPlacement { get; set; }
            public bool IsPresent { get; set; }
            public bool IsAccessSwPressed { get; set; }
            public bool ClampState { get; set; }//close=1
            public bool DoorState { get; set; }//open=1
            public bool DockState { get; set; }//docked=1

            public bool IndicatiorPresence { get; set; }//ON=1
            public bool IndicatiorPlacement { get; set; }//ON=1
            public bool IndicatiorLoad { get; set; }//ON=1
            public bool IndicatiorUnload { get; set; }//ON=1
            public bool IndicatiorManualMode { get; set; }//ON=1
            public bool IndicatorAlarm { get; set; }//ON=1
            public bool IndicatiorClampUnclamp { get; set; }//ON=1
            public bool IndicatiorDockUndock { get; set; }//ON=1
            public bool IndicatiorOpAccess { get; set; }//ON=1
        }

        public class E84State
        {
            public bool DiValid { get; set; }//ON=1
            public bool DiCS0 { get; set; }//ON=1
            public bool DiCS1 { get; set; }//ON=1
            public bool DiTrReq { get; set; }//ON=1
            public bool DiBusy { get; set; }//ON=1
            public bool DiCompt { get; set; }//ON=1
            public bool DiCont { get; set; }//ON=1

            public bool DoLoadReq { get; set; }//ON=1
            public bool DoUnloadReq { get; set; }//ON=1
            public bool DoReady { get; set; }//ON=1
            public bool DoHOAvbl { get; set; }//ON=1
            public bool DoES { get; set; }//ON=1
        }

        public class SystemState
        {
            public bool VacuumsSourcePressure1 { get; set; }//Normal=1
            public bool VacuumsSourcePressure2 { get; set; }//Normal=1
            public bool CompressedAirPressure1 { get; set; }//Normal=1
            public bool CompressedAirPressure2 { get; set; }//Normal=1
            public bool DifferentialPressureSensorSetting1 { get; set; }//Normal=1
            public bool DifferentialPressureSensorSetting2 { get; set; }//Normal=1
            public bool FFUAlarm { get; set; }//Normal=1
            public bool IonizerAlarm { get; set; }//Normal=1
            public bool ModeSwitch { get; set; }//RUN=1
            public bool DriverPower { get; set; }//Normal=1
            public bool DoorOpenCloseStatus { get; set; }//close=1
            public bool AreaSensorBarInterlock { get; set; }//Normal=1

            public bool LightRed { get; set; }//ON=1
            public bool LightYellow { get; set; }//ON=1
            public bool LightGreen { get; set; }//ON=1
            public bool LightBlue { get; set; }//ON=1
            public bool LightWhite { get; set; }//ON=1
            public bool FlashRed { get; set; }//Flash=1
            public bool FlashYellow { get; set; }//Flash=1
            public bool FlashGreen { get; set; }//Flash=1
            public bool FlashBlue { get; set; }//Flash=1
            public bool FlashWhite { get; set; }//Flash=1
            public bool Buzzer1 { get; set; }//ON=1
            public bool Buzzer2 { get; set; }//ON=1
        }
    }
}

