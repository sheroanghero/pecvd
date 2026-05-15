using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.CarrierIDReaderBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.LoadPortBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDK;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.RobotBase;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts.TDKB
{
    public class TDKBLoadPort : LoadPortBaseDevice, IConnection
    {
        public TDKBLoadPort(string module, string name, string scRoot, IoTrigger[] dos = null, IoSensor[] dis = null, RobotBaseDevice robot = null, bool IsTCPconnection = false, IE84CallBack e84 =null) : base(module, name, robot,e84)
        {
            _scRoot = scRoot;
            _isTcpConnection = IsTCPconnection;
            LoadPortType = "TDKLoadPort";
            if (dos != null && dos.Length >= 1)
            {
                _doLoadPortOK = dos[0];

            }
            if (dis != null && dis.Length >= 1)
            {
                _diInfoPadA = dis[0];
                _diInfoPadA.OnSignalChanged += _diInfoPad_OnSignalChanged;
            }
            if (dis != null && dis.Length >= 2)
            {
                _diInfoPadB = dis[1];
                _diInfoPadB.OnSignalChanged += _diInfoPad_OnSignalChanged;
            }
            if (dis != null && dis.Length >= 3)
            {
                _diInfoPadC = dis[2];
                _diInfoPadC.OnSignalChanged += _diInfoPad_OnSignalChanged;
            }
            if (dis != null && dis.Length >= 4)
            {
                _diInfoPadD = dis[3];
                _diInfoPadD.OnSignalChanged += _diInfoPad_OnSignalChanged;
            }
            InitializeLP();
            SubscribeLPData();
            //SubscribeLPAlarm();
        }

        
        private void SubscribeLPData()
        {   
            if (Module != "")
            {
                DATA.Subscribe($"{Module}.{Name}.SystemStatus", () => SystemStatus.ToString());
                DATA.Subscribe($"{Module}.{Name}.Mode", () => Mode.ToString());
                DATA.Subscribe($"{Module}.{Name}.InitPosMovement", () => InitPosMovement.ToString());
                DATA.Subscribe($"{Module}.{Name}.OperationStatus", () => OperationStatus.ToString());
                DATA.Subscribe($"{Module}.{Name}.ErrorCode", () => ErrorCode.ToString());
                DATA.Subscribe($"{Module}.{Name}.ContainerStatus", () => ContainerStatus.ToString());
                DATA.Subscribe($"{Module}.{Name}.ClampPosition", () => ClampPosition.ToString());
                DATA.Subscribe($"{Module}.{Name}.LPDoorLatchPosition", () => LPDoorLatchPosition.ToString());
                DATA.Subscribe($"{Module}.{Name}.VacuumStatus", () => VacuumStatus.ToString());
                DATA.Subscribe($"{Module}.{Name}.LPDoorState", () => LPDoorState.ToString());
                DATA.Subscribe($"{Module}.{Name}.WaferProtrusion", () => WaferProtrusion.ToString());
                DATA.Subscribe($"{Module}.{Name}.ElevatorAxisPosition", () => ElevatorAxisPosition.ToString());
                DATA.Subscribe($"{Module}.{Name}.MapperPostion", () => MapperPostion.ToString());
                DATA.Subscribe($"{Module}.{Name}.MappingStatus", () => MappingStatus.ToString());
                DATA.Subscribe($"{Module}.{Name}.Model", () => Model.ToString());
                DATA.Subscribe($"{Module}.{Name}.IsFosbModeActual", () => IsFosbModeActual.ToString());
                DATA.Subscribe($"{Module}.{Name}.DockPosition", () => DockPosition.ToString());
            }
            else
            {
                DATA.Subscribe($"{Name}.SystemStatus", () => SystemStatus.ToString());
                DATA.Subscribe($"{Name}.Mode", () => Mode.ToString());
                DATA.Subscribe($"{Name}.InitPosMovement", () => InitPosMovement.ToString());
                DATA.Subscribe($"{Name}.OperationStatus", () => OperationStatus.ToString());
                DATA.Subscribe($"{Name}.ErrorCode", () => ErrorCode.ToString());
                DATA.Subscribe($"{Name}.ContainerStatus", () => ContainerStatus.ToString());
                DATA.Subscribe($"{Name}.ClampPosition", () => ClampPosition.ToString());
                DATA.Subscribe($"{Name}.LPDoorLatchPosition", () => LPDoorLatchPosition.ToString());
                DATA.Subscribe($"{Name}.VacuumStatus", () => VacuumStatus.ToString());
                DATA.Subscribe($"{Name}.LPDoorState", () => LPDoorState.ToString());
                DATA.Subscribe($"{Name}.WaferProtrusion", () => WaferProtrusion.ToString());
                DATA.Subscribe($"{Name}.ElevatorAxisPosition", () => ElevatorAxisPosition.ToString());
                DATA.Subscribe($"{Name}.MapperPostion", () => MapperPostion.ToString());
                DATA.Subscribe($"{Name}.MappingStatus", () => MappingStatus.ToString());
                DATA.Subscribe($"{Name}.Model", () => Model.ToString());
                DATA.Subscribe($"{Name}.IsFosbModeActual", () => IsFosbModeActual.ToString());
                DATA.Subscribe($"{Name}.DockPosition", () => DockPosition.ToString());
            }
        }


        private void SubscribeLPAlarm()
        {
            EV.Subscribe(new EventItem("Alarm", AlarmTdkZLMIT, $"Load Port {Name} Z-axis position: NG", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkYLMIT, $"Load Port {Name} Y-axis position: NG", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkPROTS, $"Load Port {Name} Wafer protrusion", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkDLMIT, $"Load Port {Name} Door forward/backward position: NG", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkMPBAR, $"Load Port {Name} Mapper arm position: NG", EventLevel.Alarm, EventType.EventUI_Notify));
            
            
            EV.Subscribe(new EventItem("Alarm", AlarmTdkMPSTP, $"Load Port {Name} Mapper stopper position: NG", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkMPEDL, $"Load Port {Name} Mapping end position: NG", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkCLOPS, $"Load Port {Name} FOUP clamp open error", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmTdkCLCLS, $"Load Port {Name} FOUP clamp close error", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkDROPS, $"Load Port {Name} Latch key open error", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkDRCLS, $"Load Port {Name} Latch key close error", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmTdkVACCS, $"Load Port {Name} Vacuum on error", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkVACOS, $"Load Port {Name} Vacuum off error", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkAIRSN, $"Load Port {Name} Main air error", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmTdkINTOP, $"Load Port {Name} Normal position error at FOUP open", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkINTCL, $"Load Port {Name} Normal position error at FOUP close", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkINTMP, $"Load Port {Name} Mapper storage error when Z-axis lowered", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmTdkINTPI, $"Load Port {Name} Parallel signal error from upper machine", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkSAFTY, $"Load Port {Name} Interlock relay failure", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkFANST, $"Load Port {Name} Fan operation error", EventLevel.Alarm, EventType.EventUI_Notify));

            EV.Subscribe(new EventItem("Alarm", AlarmTdkMPDOG, $"Load Port {Name} Mapping mechanical(Adjustment) error", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkDRDKE, $"Load Port {Name} Door detection error during dock.", EventLevel.Alarm, EventType.EventUI_Notify));
            EV.Subscribe(new EventItem("Alarm", AlarmTdkDRSWE, $"Load Port {Name} Door detection error except dock", EventLevel.Alarm, EventType.EventUI_Notify));
            //EV.Subscribe(new EventItem("Alarm", AlarmTdkNoOperation, $"Load Port {Name} No action when foup is present", EventLevel.Alarm, EventType.EventUI_Notify));-->


        }

        internal void OnE84Unload(string evtcontent)
        {
            
        }

        internal void OnE84Load(string evtcontent)
        {
            
        }

        private string AlarmTdkZLMIT { get => LPModuleName.ToString() + "ZLMIT"; }
        private string AlarmTdkYLMIT { get => LPModuleName.ToString() + "YLMIT"; }
        private string AlarmTdkPROTS { get => LPModuleName.ToString() + "PROTS"; }
        private string AlarmTdkDLMIT { get => LPModuleName.ToString() + "DLMIT"; }
        private string AlarmTdkMPBAR { get => LPModuleName.ToString() + "MPBAR"; }
        private string AlarmTdkMPSTP { get => LPModuleName.ToString() + "MPSTP"; }
        private string AlarmTdkMPEDL { get => LPModuleName.ToString() + "MPEDL"; }
        private string AlarmTdkCLOPS { get => LPModuleName.ToString() + "CLOPS"; }
        private string AlarmTdkCLCLS { get => LPModuleName.ToString() + "CLCLS"; }
        private string AlarmTdkDROPS { get => LPModuleName.ToString() + "DROPS"; }
        private string AlarmTdkDRCLS { get => LPModuleName.ToString() + "DRCLS"; }
        private string AlarmTdkVACCS { get => LPModuleName.ToString() + "VACCS"; }
        private string AlarmTdkVACOS { get => LPModuleName.ToString() + "VACOS"; }
        private string AlarmTdkAIRSN { get => LPModuleName.ToString() + "AIRSN"; }
        private string AlarmTdkINTOP { get => LPModuleName.ToString() + "INTOP"; }
        private string AlarmTdkINTCL { get => LPModuleName.ToString() + "INTCL"; }
        private string AlarmTdkINTMP { get => LPModuleName.ToString() + "INTMP"; }
        private string AlarmTdkINTPI { get => LPModuleName.ToString() + "INTPI"; }
        private string AlarmTdkSAFTY { get => LPModuleName.ToString() + "SAFTY"; }
        private string AlarmTdkFANST { get => LPModuleName.ToString() + "FANST"; }
        private string AlarmTdkMPDOG { get => LPModuleName.ToString() + "MPDOG"; }
        private string AlarmTdkDRDKE { get => LPModuleName.ToString() + "DRDKE"; }
        private string AlarmTdkDRSWE { get => LPModuleName.ToString() + "DRSWE"; }
        private string AlarmTdkNoOperation { get => LPModuleName.ToString() + "NoOperation"; }

        public bool IsHandlerBusy
        {
            get
            {
                return _lstHandler.Count != 0 || _connection.IsBusy;
            }
        }

        public E84DeviceTypeEnum E84DeviceType
        {
            get
            {
                if (LPE84Callback != null) 
                    return  E84DeviceTypeEnum.External;

                if (SC.ContainsItem($"LoadPort.{Name}.E84DeviceType"))
                    return (E84DeviceTypeEnum)SC.GetValue<int>($"LoadPort.{Name}.E84DeviceType");

                return E84DeviceTypeEnum.None;
            }
        }

        public override bool SetE84Available(out string reason)
        {
            if(E84DeviceType == E84DeviceTypeEnum.External)
                return base.SetE84Available(out reason);

            if(E84DeviceType == E84DeviceTypeEnum.Internal)
            {
                lock(_locker)
                {
                    _lstHandler.AddLast(new TDKBSetHandler(this, "E84EN/", "ON"));
                }
            }
            if (SC.ContainsItem($"LoadPort.{Name}.AccessMode"))
                SC.SetItemValue($"LoadPort.{Name}.AccessMode", true);

            IsAccessAuto = true;
            reason = "";
            return true;


        }



        public override bool ChangeAccessMode(bool auto, out string reason)
        {
            if(auto)
            {
                if (E84DeviceType == E84DeviceTypeEnum.Internal)
                {
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new TDKBSetHandler(this, "E84RS", null));
                    }
                }
            }
            else
            {
                if (E84DeviceType == E84DeviceTypeEnum.Internal)
                {
                    lock (_locker)
                    {
                        _lstHandler.AddLast(new TDKBSetHandler(this, "E84EN/", "OFF"));
                    }
                }
                if (SC.ContainsItem($"LoadPort.{Name}.AccessMode"))
                    SC.SetItemValue($"LoadPort.{Name}.AccessMode", false);
            }
            return base.ChangeAccessMode(auto, out reason);
        }
        public override bool SetE84Unavailable(out string reason)
        {
            if (E84DeviceType == E84DeviceTypeEnum.External)
                return base.SetE84Unavailable(out reason);

            if (E84DeviceType == E84DeviceTypeEnum.Internal)
            {
                lock (_locker)
                {
                    _lstHandler.AddLast(new TDKBSetHandler(this, "E84EN/", "OFF"));
                }
            }
            if (SC.ContainsItem($"LoadPort.{Name}.AccessMode"))
                SC.SetItemValue($"LoadPort.{Name}.AccessMode", false);

            IsAccessAuto = false;
            reason = "";
            return true;
        }

        public override bool LoadPortE84Complete(object[] param, out string reason)
        {
            if (E84DeviceType == E84DeviceTypeEnum.External)
                return base.LoadPortE84Complete(param, out reason);
            if (E84DeviceType == E84DeviceTypeEnum.Internal)
            {
                lock (_locker)
                {
                    _lstHandler.AddLast(new TDKBSetHandler(this, "E84RS", null));
                }
            }

            reason = "";
            return true;
        }

        public override bool LoadPortE84Retry(object[] param, out string reason)
        {
            if (E84DeviceType == E84DeviceTypeEnum.External)
                return base.LoadPortE84Complete(param, out reason);
            if (E84DeviceType == E84DeviceTypeEnum.Internal)
            {
                lock (_locker)
                {
                    _lstHandler.AddLast(new TDKBSetHandler(this, "E84RS", null));
                }
            }

            reason = "";
            return true;
        }


        internal void OnE84Error(string evtcontent)
        {
            
        }
        private bool _isE84Onload;

        private bool _isUndockOCBeforeHome
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{Name}.UndockOCBeforeHome"))
                    return SC.GetValue<bool>($"LoadPort.{Name}.UndockOCBeforeHome");
                return false;
            }
                
        }
        internal void OnE84IOEvent(string evtcontent)
        {            
            string startstr = "NF:E84IO/";
            string endstr = ";";
            Regex rg = new Regex("(?<=(" + startstr + "))[.\\s\\S]*?(?=(" + endstr + "))", RegexOptions.Multiline | RegexOptions.Singleline);
            string[] items =  rg.Match(evtcontent).Value.Split('/');
            int intputValue = Convert.ToInt32(items[0], 16);
            char[] inputs = Convert.ToString(Convert.ToByte(items[1], 16), 2).PadLeft(8, '0').ToCharArray();
            Array.Reverse(inputs);

            LPE84SigState.VALID = inputs[0] != '0';
            LPE84SigState.CS_0 = inputs[1] != '0';
            LPE84SigState.CS_1 = inputs[2] != '0';
            LPE84SigState.AM_AVBL = inputs[3] != '0';
            LPE84SigState.TR_REQ = inputs[4] != '0';
            LPE84SigState.BUSY = inputs[5] != '0';
            
            if(LPE84SigState.COMPT && inputs[6] == '0')
            {
                OnE84HandOffComplete(_isE84Onload);
            }
            LPE84SigState.COMPT = inputs[6] != '0';


            LPE84SigState.CONT = inputs[7] != '0';

            inputs = Convert.ToString(Convert.ToByte(items[2], 16), 2).PadLeft(8, '0').ToCharArray();
            Array.Reverse(inputs);

            if(!LPE84SigState.L_REQ && inputs[0] != '0')
            {
                OnE84HandOffStart(true);
                _isE84Onload = true;
            }
            LPE84SigState.L_REQ = inputs[0] != '0';

            if (!LPE84SigState.U_REQ && inputs[1] != '0')
            {
                OnE84HandOffStart(false);
                _isE84Onload = false;
            }

            LPE84SigState.U_REQ = inputs[1] != '0';
            LPE84SigState.VA = inputs[2] != '0';
            LPE84SigState.READY = inputs[3] != '0';
            LPE84SigState.VS_0 = inputs[4] != '0';
            LPE84SigState.VS_1 = inputs[5] != '0';
            LPE84SigState.HO_AVBL = inputs[6] != '0';
            LPE84SigState.ES = inputs[7] != '0';
        }

        internal void OnCommandFailed(string moveCommand)
        {
            switch(moveCommand)
            {
                case "PODCL":
                    break;
                case "PODOP":
                    break;
                case "YDOOR":
                    break;
                case "YWAIT":
                    break;
                default:
                    break;
            }
            EV.PostWarningLog("LoadPort", $"{LPModuleName} execute command:{moveCommand} failed");
        }

        private void _diInfoPad_OnSignalChanged(IoSensor arg1, bool arg2)
        {
         
        }

        internal void OnCommandSuccess(string moveCommand)
        {
            var dvid = new SerializableDictionary<string, object>
            {
                ["CarrierID"] = _carrierId ?? "",
                ["CAR_ID"] = _carrierId ?? "",
                ["PORT_ID"] = PortID,
                ["PortID"] = PortID,
                ["PORT_CTGRY"] = SpecPortName,
                ["CarrierType"] = SpecCarrierType,
                ["CarrierIndex"] = InfoPadCarrierIndex,
                ["InfoPadSensorIndex"] = InfoPadSensorIndex,

            };

            EV.PostInfoLog("LoadPort", $"{LPModuleName} execute command:{moveCommand} with carrier:{CarrierId}," +
                $" type:{SpecCarrierType ?? ""} successfully");


            switch (moveCommand)
            {
                case "PODCL":
                    EV.Notify(EventCarrierClamped, dvid);
                    break;
                case "PODOP":
                    EV.Notify(EventCarrierUnclamped, dvid);
                    break;
                case "YDOOR":
                    EV.Notify(EventCarrierDocked, dvid);
                    break;
                case "YWAIT":
                    EV.Notify(EventCarrierUndocked, dvid);
                    break;
                default:
                    break;
            }
          
        }

        public override bool IsKeepClampAfterUnload
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.KeepClampedAfterUnloadCarrier{InfoPadCarrierIndex}"))
                    return SC.GetValue<bool>($"CarrierInfo.KeepClampedAfterUnloadCarrier{InfoPadCarrierIndex}");
                return base.IsKeepClampAfterUnload;
            }
        }

        public override int InfoPadCarrierIndex 
        {
            get { return base.InfoPadCarrierIndex; }
            set 
            {
                if (base.InfoPadCarrierIndex != value)
                {
                    base.InfoPadCarrierIndex = value;
                    EV.PostInfoLog("LoadPort", $"{LPModuleName} infopad index change to {value}");
                    //if (CIDReaders != null && CIDReaders.Length > 1)
                    //{
                    //    int cidindex = SC.GetValue<int>($"CarrierInfo.{LPModuleName}CIDReaderIndex{InfoPadCarrierIndex}");
                    //    if (CIDReaders.Length <= cidindex)
                    //    {
                    //        EV.PostAlarmLog("System", $"The carrier info configuration for CIDReaderIndex{cidindex} is invalid.");
                    //    }
                    //    else
                    //    {
                    //        CarrierIDReaderCallBack = CIDReaders[cidindex];
                    //    }
                    //}
                }
            }
        }

        public override bool IsEnableDualTransfer(out string reason)
        {
            reason = "";
            if(SC.ContainsItem($"CarrierInfo.EnableDualTransfer{InfoPadCarrierIndex}") && 
                SC.GetValue<bool>($"CarrierInfo.EnableDualTransfer{InfoPadCarrierIndex}"))
            {
                
                return true;
            }
            return base.IsEnableDualTransfer(out reason);
        }

        public override CIDReaderBaseDevice[] CIDReaders
        {
            get { return base.CIDReaders; }
            set
            {
                base.CIDReaders = value;
               
            }
        }


        private void InitializeLP()
        {
            
            if (_doLoadPortOK != null)
                _doLoadPortOK.SetTrigger(true, out _);


            //_deviceAddress = SC.GetValue<int>($"{Name}.DeviceAddress");
             //InfoPadType,0=TDK,1=Ext,2=FixedbySC


            if (_isTcpConnection)
            {
                Address = SC.GetStringValue($"LoadPort.{Name}.Address");
                _tcpConnection = new TDKBLoadPortTCPConnection(this, Address);

                _tcpConnection.EnableLog(_enableLog);
                if (_tcpConnection.Connect())
                {
                    //LOG.Write($"Connected with {Module}.{Name} .");
                    EV.PostInfoLog(Module, $"Connected with {Module}.{Name} .");
                }
                else
                {
                    EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
                }
            }
            else
            {
                string portName = SC.GetStringValue($"LoadPort.{Name}.PortName");
                int bautRate = SC.GetValue<int>($"LoadPort.{Name}.BaudRate");
                int dataBits = SC.GetValue<int>($"LoadPort.{Name}.DataBits");
                Enum.TryParse(SC.GetStringValue($"LoadPort.{Name}.Parity"), out Parity parity);
                Enum.TryParse(SC.GetStringValue($"LoadPort.{Name}.StopBits"), out StopBits stopBits);
                Address = portName;
                _connection = new TDKBLoadPortConnection(this, portName, bautRate, dataBits, parity, stopBits);
                _connection.EnableLog(_enableLog);
                int count = SC.ContainsItem("System.ComPortRetryCount") ? SC.GetValue<int>("System.ComPortRetryCount") : 5;
                int sleep = SC.ContainsItem("System.ComPortRetryDelayTime") ? SC.GetValue<int>("System.ComPortRetryDelayTime") : 2;
                if (sleep <= 0 || sleep > 10)
                    sleep = 2;

                int retry = 0;
                do
                {
                    _connection.Disconnect();
                    Thread.Sleep(sleep * 1000);
                    if (_connection.Connect())
                    {
                        //LOG.Write($"Connected with {Module}.{Name} .");
                        EV.PostInfoLog(Module, $"Connected with {Module}.{Name} .");
                        break;
                    }
                    if (count > 0 && retry++ > count)
                    {
                        EV.PostAlarmLog(Module, $"Can't connect to {Module}.{Name}.");
                        break;
                    }
                } while (true);
            }
            
            ConnectionManager.Instance.Subscribe($"{Name}", this);

            _thread = new PeriodicJob(50, OnTimer, $"{Module}.{Name} MonitorHandler", true);




        }
        private bool _isEnableIdleMonitorState
        {
            get
            {
                if (SC.ContainsItem($"LoadPort.{Name}.EnableIdleMonitorState"))
                    return SC.GetValue<bool>($"LoadPort.{Name}.EnableIdleMonitorState");
                return false;
            }
        }

        private bool OnTimer()
        {
            try
            {
                MonitorFoupState();


                if (_isTcpConnection)
                {
                    _tcpConnection.EnableLog(_enableLog);

                    _trigCommunicationError.CLK = _tcpConnection.IsCommunicationError;
                    if (_trigCommunicationError.Q)
                    {
                        EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_tcpConnection.LastCommunicationError}");
                        OnError("Communicartion Error");
                    }
                }
                else
                {
                    _connection.EnableLog(_enableLog);

                    _trigCommunicationError.CLK = _connection.IsCommunicationError;
                    if (_trigCommunicationError.Q)
                    {
                        EV.PostAlarmLog(Module, $"{Module}.{Name} communication error, {_connection.LastCommunicationError}");
                        OnError("Communicartion Error");
                    }
                }





                if (_isTcpConnection)
                {
                    _tcpConnection.MonitorTimeout();
                    if (!_tcpConnection.IsConnected || _tcpConnection.IsCommunicationError)
                    {
                        lock (_locker)
                        {
                            _lstHandler.Clear();
                        }
                        _trigRetryConnect.CLK = !_tcpConnection.IsConnected;
                        if (_trigRetryConnect.Q)
                        {

                            Address = SC.GetStringValue($"LoadPort.{Name}.Address");
                            _tcpConnection = new TDKBLoadPortTCPConnection(this, Address);
                            _tcpConnection.EnableLog(_enableLog);
                            if (!_tcpConnection.Connect())
                            {
                                EV.PostAlarmLog(Module, $"Can not connect with {_tcpConnection.Address}, {Module}.{Name}");
                            }
                        }
                        return true;
                    }
                    //_trigActionDone.CLK = (_lstHandler.Count == 0 && !_tcpConnection.IsBusy);
                    //if (_trigActionDone.Q)
                    //    OnActionDone(null);

                    HandlerBase handler = null;
                    if (!_tcpConnection.IsBusy)
                    {
                        lock (_locker)
                        {
                            if (_lstHandler.Count == 0)
                            {
                            }

                            if (_lstHandler.Count > 0)
                            {
                                handler = _lstHandler.First.Value;
                                if (handler != null) _tcpConnection.Execute(handler);
                                _lstHandler.RemoveFirst();
                            }
                        }


                    }

                }
                else
                {

                    _connection.MonitorTimeout();

                    if (!_connection.IsConnected || _connection.IsCommunicationError)
                    {
                        lock (_locker)
                        {
                            _lstHandler.Clear();
                        }
                        _trigRetryConnect.CLK = !_connection.IsConnected;
                        if (_trigRetryConnect.Q)
                        {
                            _connection.SetPortAddress(SC.GetStringValue($"{Name}.Address"));
                            if (!_connection.Connect())
                            {
                                EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                            }
                        }
                        return true;
                    }                  

                    HandlerBase handler = null;
                    if (!_connection.IsBusy)
                    {
                        lock (_locker)
                        {
                            if (_lstHandler.Count == 0)
                            {
                                if(CurrentState == LoadPortStateEnum.TransferBlock)
                                {
                                    _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                                }

                                if(IsReady() && _isEnableIdleMonitorState)
                                {
                                    _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                                }
                            }

                            if (_lstHandler.Count > 0)
                            {
                                handler = _lstHandler.First.Value;
                                if (handler != null) _connection.Execute(handler);
                                _lstHandler.RemoveFirst();
                            }
                        }
                    }
                }
                if (_infoPadType == 1)   //Extenal
                {
                    InfoPadSensorIndex = (_diInfoPadA == null || !_diInfoPadA.Value ? 0 : 1) +
                    (_diInfoPadB == null || !_diInfoPadB.Value ? 0 : 2) +
                    (_diInfoPadC == null || !_diInfoPadC.Value ? 0 : 4) +
                    (_diInfoPadD == null || !_diInfoPadD.Value ? 0 : 8);
                }

                if (IsAutoDetectCarrierType)
                {  
                    if (_infoPadType == 2)  //Fixed by SC
                    {
                        InfoPadCarrierIndex = SC.GetValue<int>($"LoadPort.{Name}.CarrierIndex");
                    }                    
                    if(_infoPadType == 1)  //External
                    {
                        InfoPadCarrierIndex = InfoPadSensorIndex;
                    }
                    if(_infoPadType == 0)  //Internal
                    {

                    }
                }
                
                else //Fixed by SC
                {
                    InfoPadCarrierIndex = SC.GetValue<int>($"LoadPort.{Name}.CarrierIndex");
                }
              
                
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;

        }
        public override bool IsRequestFOSBMode 
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.CarrierFosbMode{InfoPadCarrierIndex}"))
                    return SC.GetValue<int>($"CarrierInfo.CarrierFosbMode{InfoPadCarrierIndex}") == 1;

                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierFosbMode"))
                    return SC.GetValue<bool>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierFosbMode");

                return false;
            }
            
            set => base.IsRequestFOSBMode = value;

        }

        public override bool IsMapWaferByLoadPort
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.MappedByRobot"))
                    return !SC.GetValue<bool>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.MappedByRobot");

                return !IsRequestFOSBMode;
            }
        }


        public void LPSetInfoPadSensorIndex(int index)
        {
            if(_infoPadType ==0)
            {
                InfoPadSensorIndex = index;
                if(IsAutoDetectCarrierType)
                    InfoPadCarrierIndex = InfoPadSensorIndex;
            }
        }
        public override void Monitor()
        {
            base.Monitor();
            
            try
            {
               

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        private void MonitorFoupState()
        {
            if (IsPlacement)
            {
                int currentfoupstatecode = (ClampPosition == TDKPosition.Close ? 0 : 1) +
                    (DockPosition == TDKDockPosition.Dock ? 0 : 2) +
                    (DoorState == FoupDoorState.Close ? 0 : 4) +
                    (DoorPosition == FoupDoorPostionEnum.Down ? 0 : 8);

                if(currentfoupstatecode!= _foupstatecode)
                {
                    _dtFoupStateStart = DateTime.Now;
                    _foupstatecode = currentfoupstatecode;
                }

                if (DateTime.Now - _dtFoupStateStart > TimeSpan.FromSeconds(TimeLimitNoOperation) && TimeLimitNoOperation > 0)
                {
                    SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>();
                    dvid["AlarmDescription"] = $"carrier:{_carrierId} on {LPModuleName}  no operation time over limit";
                    EV.Notify(AlarmTdkNoOperation, dvid);
                    _dtFoupStateStart = DateTime.Now;
                }
            }
            else
            {
                _dtFoupStateStart = DateTime.Now;
            }
        }

        private DateTime _dtFoupStateStart = DateTime.Now;
        private int _foupstatecode = -1;

        private R_TRIG _trigActionDone = new R_TRIG();
        private string _scRoot;
        private bool _isTcpConnection;
        private TDKBLoadPortConnection _connection;
        private TDKBLoadPortTCPConnection _tcpConnection;
        private IoTrigger _doLoadPortOK;
        private IoSensor _diInfoPadA;
        private IoSensor _diInfoPadB;
        private IoSensor _diInfoPadC;
        private IoSensor _diInfoPadD;

        private IoSensor _diIronCassetteDoorOpenLeft;
        public IoSensor DiIronCassetteDoorOpenLeft
        {
            get => _diIronCassetteDoorOpenLeft;
            set
            {
                _diIronCassetteDoorOpenLeft = value;
                _diIronCassetteDoorOpenLeft.OnSignalChanged += _diIronCassetteDoorOpen_OnSignalChanged;
            }
        }

        

        private IoSensor _diIronCassetteDoorOpenRight;
        public IoSensor DiIronCassetteDoorOpenRight
        {
            get => _diIronCassetteDoorOpenRight;
            set
            {
                _diIronCassetteDoorOpenRight = value;
                _diIronCassetteDoorOpenRight.OnSignalChanged += _diIronCassetteDoorOpen_OnSignalChanged; ;
            }
        }
        private object _lockerIronCst = new object();
        private void _diIronCassetteDoorOpen_OnSignalChanged(IoSensor arg1, bool arg2)
        {
            if(!_isNeedCheckIronStickDoor) return;                     
            
            lock (_lockerIronCst)
            {

                if (!_diIronCassetteDoorOpenLeft.Value && !_diIronCassetteDoorOpenRight.Value)
                {
                    if (!IsPlacement || DockState != FoupDockState.Undocked) return;
                    if (!IsReady()) return;
                    if (ClampState == FoupClampState.Close)
                        Unclamp(out _);
                }
                if (_diIronCassetteDoorOpenLeft.Value && _diIronCassetteDoorOpenRight.Value)
                {
                    if (!IsPlacement || DockState != FoupDockState.Undocked) return;
                    if (!IsReady()) return;
                    if (ClampState == FoupClampState.Open)
                        Clamp(out _);
                }

            }
        }
        private bool _isNeedCheckIronStickDoor
        {
            get
            {
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.NeedCheckIronDoorCarrier"))
                    return SC.GetValue<bool>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.NeedCheckIronDoorCarrier");
                return false;
            }
        }
        public override bool IsEnableLoad(out string reason)
        {
            if(_isNeedCheckIronStickDoor)
            {
                if (_diIronCassetteDoorOpenRight != null && !_diIronCassetteDoorOpenRight.Value)
                {
                    reason = "IronCstRightProtectionClose";
                    return false;
                }
                if (_diIronCassetteDoorOpenLeft != null && !_diIronCassetteDoorOpenLeft.Value)
                {
                    reason = "IronCstLeftProtectionClose";
                    return false;
                }
            }
            return base.IsEnableLoad(out reason);
        }

        


        private int _infoPadType 
        {
            get => SC.ContainsItem($"LoadPort.{Name}.InfoPadType") ? SC.GetValue<int>($"LoadPort.{Name}.InfoPadType") : 2;
        }
        public int InfoPadType => _infoPadType;

        public TDKBLoadPortConnection Connection
        {
            get => _connection;
        }
        public TDKBLoadPortTCPConnection TCPConnection => _tcpConnection;

        private PeriodicJob _thread;
        private static Object _locker = new Object();
        private LinkedList<HandlerBase> _lstHandler = new LinkedList<HandlerBase>();
        private bool _enableLog => SC.GetValue<bool>($"LoadPort.{Name}.EnableLogMessage");
        //private bool _commErr = false;
        private R_TRIG _trigError = new R_TRIG();

        private R_TRIG _trigWarningMessage = new R_TRIG();

        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();

        public TDKSystemStatus SystemStatus { get; set; }
        public TDKMode Mode { get; set; }
        public TDKInitPosMovement InitPosMovement { get; set; }
        public TDKOperationStatus OperationStatus { get; set; }
        public TDKContainerStatus ContainerStatus { get; set; }
        public TDKPosition ClampPosition { get; set; }
        public TDKPosition LPDoorLatchPosition { get; set; }
        public TDKVacummStatus VacuumStatus { get; set; }
        public TDKPosition LPDoorState { get; set; }
        public TDKWaferProtrusion WaferProtrusion { get; set; }
        public TDKElevatorAxisPosition ElevatorAxisPosition { get; set; }
        public TDKDockPosition DockPosition { get; set; }
        public TDKMapPosition MapperPostion { get; set; }
        public TDKMappingStatus MappingStatus { get; set; }

        public TDKModel Model { get; set; }
        public string Address { get; set; }

        public bool IsConnected => _connection.IsConnected;




        public override bool IsEnableTransferWafer(out string reason)
        {
            if(LPDoorState != TDKPosition.Open)
            {
                reason = "Door is not open";
                return false;
            }
            if(DockPosition != TDKDockPosition.Dock)
            {
                reason = "Foup is not dock";
                return false;
            }
            if (IsWaferProtrude)
            {
                int iCount = 0;
                while(true)
                {
                    Thread.Sleep(100);
                    iCount++;
                    if (!IsWaferProtrude)
                        break;
                    if (iCount >15)
                    {
                        reason = "Wafer Protrude";
                        return false;
                    }
                }
            }

            if (_isNeedCheckIronStickDoor)
            {
                if(_diIronCassetteDoorOpenRight != null && !_diIronCassetteDoorOpenRight.Value)
                {
                    reason = "IronCstRightProtectionClose";
                    return false;
                }
                if (_diIronCassetteDoorOpenLeft != null && !_diIronCassetteDoorOpenLeft.Value)
                {
                    reason = "IronCstLeftProtectionClose";
                    return false;
                }
            }
            if (IsVerifyPreDefineWaferCount && WaferCount != PreDefineWaferCount)
            {
                reason = "Mapping Error:WaferCount not matched";
                return false;
            }

            return base.IsEnableTransferWafer(out reason);
        }
        public override bool IsEnableMapWafer(out string reason)
        {
            if (IsWaferProtrude)
            {
                int iCount = 0;
                while (true)
                {
                    Thread.Sleep(100);
                    iCount++;
                    if (!IsWaferProtrude)
                        break;
                    if (iCount > 15)
                    {
                        reason = "Wafer Protrude";
                        return false;
                    }
                }
            }

            if (_isNeedCheckIronStickDoor)
            {
                if (_diIronCassetteDoorOpenRight != null && !_diIronCassetteDoorOpenRight.Value)
                {
                    reason = "IronCstRightProtectionClose";
                    return false;
                }
                if (_diIronCassetteDoorOpenLeft != null && !_diIronCassetteDoorOpenLeft.Value)
                {
                    reason = "IronCstLeftProtectionClose";
                    return false;
                }
            }




            return base.IsEnableMapWafer(out reason);
        }
        public bool Disconnect()
        {
            if (_isTcpConnection)
                return _tcpConnection.Disconnect();
            return _connection.Disconnect();
        }
        public void OnCarrierNotPlaced()
        {
            _isPlaced = false;
            ConfirmRemoveCarrier();
        }


        public void OnCarrierNotPresent()
        {
            _isPresent = false;
            //ConfirmRemoveCarrier();
        }

        public void OnCarrierPlaced()
        {
            _isPlaced = true;
            ConfirmAddCarrier();

        }


        public void OnCarrierPresent()
        {
            _isPresent = true;
            //ConfirmAddCarrier();
        }

        public void OnSwitchKey1()
        {
            _isAccessSwPressed = true;
        }

        public void OnSwitchKey2()
        {

        }

        public void OffSwitchKey1()
        {
            _isAccessSwPressed = false;
        }

        public void OffSwitchKey2()
        {

        }
        public bool OnEvent(out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                _lstHandler.AddLast(new TDKBGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new TDKBGetHandler(this, "LEDST", null));
            }
            return true;
        }
        private LoadportCassetteState _cassetteState = LoadportCassetteState.None;
        public override LoadportCassetteState CassetteState
        {
            get { return _cassetteState; }
            set
            {
                _cassetteState = value;
            }
        }
        public void SetCassetteState(LoadportCassetteState state)
        {
            _cassetteState = state;

            if (state == LoadportCassetteState.Normal)
            {
                OnCarrierPresent();
                OnCarrierPlaced();

            }
            if (state == LoadportCassetteState.Absent)
            {
                OnCarrierNotPlaced();
                OnCarrierNotPresent();
            }
        }
        public override WaferSize GetCurrentWaferSize()
        {
            int intwz = 0;
            
            if(SC.ContainsItem($"CarrierInfo.CarrierWaferSize{InfoPadCarrierIndex}"))
                intwz = SC.GetValue<int>($"CarrierInfo.CarrierWaferSize{InfoPadCarrierIndex}");
            if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierWaferSize"))
                intwz = SC.GetValue<int>($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierWaferSize");



            switch (intwz)
            {
                case 0:
                    return WaferSize.WS0;
                case 1:
                    return WaferSize.WS0;
                case 2:
                    return WaferSize.WS2;
                case 3:
                    return WaferSize.WS3;
                case 4:
                    return WaferSize.WS4;
                case 5:
                    return WaferSize.WS5;
                case 6:
                    return WaferSize.WS6;
                case 7:
                case 8:
                    return WaferSize.WS8;
                case 12:
                    return WaferSize.WS12;
                default:
                    return WaferSize.WS0;
            }
        }

        public override string SpecCarrierType
        {
            get
            {
                if(SC.ContainsItem($"CarrierInfo.CarrierName{InfoPadCarrierIndex}"))
                    return SC.GetStringValue($"CarrierInfo.CarrierName{InfoPadCarrierIndex}");
                if (SC.ContainsItem($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierName"))
                    return SC.GetStringValue($"CarrierInfo.Carrier{InfoPadCarrierIndex}.CarrierName");
                return "";
            }
            set => base.SpecCarrierType = value;
        }

        public override string SpecCarrierInformation
        {
            get
            {
                if (_isPresent)
                    return "Index:" + InfoPadCarrierIndex.ToString()
                        + $"\r\nInfoPad:{InfoPadSensorIndex}"
                        + $"\r\nType:{SpecCarrierType}"
                        + $"\r\nSize:{GetCurrentWaferSize()}"
                        + (IsVerifyPreDefineWaferCount ? ("\r\n" + "Pre-Count:" + PreDefineWaferCount.ToString()) : "")
                        + $"\r\n{(IsMapped ? "Mapped" : "Not Mapped")}"
                        + $"\r\nClamp:{ClampPosition}"
                        + $"\r\nDocked:{DockState}";

                return "";
            }
        }

        protected override bool fStartWrite(object[] param)
        {
            return true;
        }

        protected override bool fStartRead(object[] param)
        {
            return true;
        }

        protected override bool fStartExecute(object[] param)
        {
            try
            {

                switch (param[0].ToString())
                {
                    case "SetIndicator":
                        Indicator light = (Indicator)param[1];
                        int lightIndex = ParseLightIndex(light);
                        IndicatorState state = (IndicatorState)param[2];
                        string[] statestr = new string[] { "", "LON", "LBL", "LOF" };
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKBSetHandler(this, statestr[(int)state]+ $"{lightIndex:D2}",null));
                            _lstHandler.AddLast(new TDKBGetHandler(this, "LEDST", null));
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                            IsBusy = false;
                        }
                        break;
                        
                    case "QueryIndicator":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKBGetHandler(this, "LEDST", null));
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                        }
                        break;
                    case "QueryState":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                        }
                        break;
                    case "Undock":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKBMoveHandler(this, "YWAIT", null));
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                        }
                        break;
                    case "Dock":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKBMoveHandler(this, "YDOOR", null));
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                        }
                        break;
                    case "CloseDoor":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKBMoveHandler(this, "DORFW", null));
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                        }
                        break;
                    case "OpenDoor":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKBMoveHandler(this, "DORBK", null));
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                           
                        }
                        break;
                    case "Unclamp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKBMoveHandler(this, "PODOP", null));
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                           
                        }
                        break;
                    case "Clamp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKBMoveHandler(this, "PODCL", null));
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                            
                        }
                        break;
                    case "DoorUp":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKBMoveHandler(this, "ZDRUP", null));
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                            
                        }
                        break;
                    case "DoorDown":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKBMoveHandler(this, "ZDRDW", null));
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                            
                        }
                        break;


                    case "OpenDoorNoMap":
                        lock (_locker)
                        {
                            //_lstHandler.AddLast(new TDKMoveHandler(this, "YDOOR", null));
                        }
                        break;
                    case "OpenDoorAndMap":
                        lock (_locker)
                        {
                            //_lstHandler.AddLast(new TDKMoveHandler(this, "YDOOR", null));
                        }
                        break;
                    case "MapWafer":
                        lock (_locker)
                        {
                            if (!IsMapWaferByLoadPort)
                            {
                                if (MapRobot != null)
                                    return MapRobot.WaferMapping(LPModuleName, out _);
                                return false;
                            }
                            if (DockPosition != TDKDockPosition.Dock)
                                _lstHandler.AddLast(new TDKBMoveHandler(this, "YDOOR", null));
                            if (DoorState != FoupDoorState.Open)
                                _lstHandler.AddLast(new TDKBMoveHandler(this, "DORBK", null));
                            _lstHandler.AddLast(new TDKBMoveHandler(this, "MAPDO", null));
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));                            
                            _lstHandler.AddLast(new TDKBGetHandler(this, "MAPRD", null));
                        }
                        break;
                    case "DoorUpAndClose":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKBMoveHandler(this, "CUMDK", null));

                            //_lstHandler.AddLast(new SELP8MoveHandler(this, "ZDRUP", null));
                            //_lstHandler.AddLast(new SELP8MoveHandler(this, "DORFW", null));
                          
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));                            
                        }
                        
                        break;
                    case "OpenDoorAndDown":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKBMoveHandler(this, "DORBK", null));
                            _lstHandler.AddLast(new TDKBMoveHandler(this, "ZDRDW", null));
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));                            
                        }
                        break;
                    case "Move":
                        lock (_locker)
                        {
                            if(IsRequestFOSBMode && _isUndockOCBeforeHome && (param[1].ToString() == "ABORG" ||
                                param[1].ToString() == "ORGSH"))
                            {
                                _lstHandler.AddLast(new TDKBMoveHandler(this, "YWAIT", null));
                            }


                            _lstHandler.AddLast(new TDKBMoveHandler(this, param[1].ToString(), null));
                            _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                        }
                        break;
                    case "Set":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKBSetHandler(this, param[1].ToString(), null));
                        }
                        break;
                    case "Get":
                        lock (_locker)
                        {
                            _lstHandler.AddLast(new TDKBGetHandler(this, param[1].ToString(), null));
                        }
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                EV.PostAlarmLog(Name, $"Parameter invalid");
                return false;

            }
        }

        protected override bool fMonitorExecuting(object[] param)
        {
            if (_lstHandler.Count == 0 && !_connection.IsBusy)
            {
                OnActionDone(null);
            }
            return false;
        }
        private int ParseLightIndex(Indicator light)
        {
            if (SC.ContainsItem($"LoadPort.{LPModuleName}.{light}_IndicatorIndex"))
                return SC.GetValue<int>($"LoadPort.{LPModuleName}.{light}_IndicatorIndex");
            return (int)light;
        }

        protected override bool fStartUnload(object[] param)
        {
            if (!_isPlaced)
            {
                EV.PostAlarmLog(Name, $"No carrier on {Name},can't unload.");
                return false;
            }
            if (!_isDocked)
            {
                EV.PostAlarmLog(Name, $"Carrier is not docked on {Name},can't unload.");
                return false;
            }
            if (IsRequestFOSBMode)
            {
                lock (_locker)
                {
                    //_lstHandler.AddLast(new TDKBSetHandler(this, "FSB", "ON"));
                    //_lstHandler.AddLast(new TDKBGetHandler(this, "FSBxx", null));
                    _lstHandler.AddLast(new TDKBMoveHandler(this, "YWAIT", null));
                    _lstHandler.AddLast(new TDKBMoveHandler(this, "ZDRUP", null));
                    _lstHandler.AddLast(new TDKBMoveHandler(this, "DORFW", null));
                    
                    if (!IsKeepClampAfterUnload)
                        _lstHandler.AddLast(new TDKBMoveHandler(this, "PODOP", null));

                    _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                    _lstHandler.AddLast(new TDKBGetHandler(this, "FSBxx", null));
                    _lstHandler.AddLast(new TDKBGetHandler(this, "LEDST", null));
                }
            }
            else
            {
                if (DoorPosition == FoupDoorPostionEnum.Up && DoorState == FoupDoorState.Close)
                {
                    lock (_locker)
                    {                        
                        _lstHandler.AddLast(new TDKBMoveHandler(this, "CUDCL", null));
                    }
                }
                else
                {
                    if(IsNeedMapOnUnload)
                        _lstHandler.AddLast(new TDKBMoveHandler(this, "CUMFC", null));
                    else
                    {
                        _lstHandler.AddLast(new TDKBMoveHandler(this, "CULFC", null));
                    }
                }

                if (!IsKeepClampAfterUnload)
                   _lstHandler.AddLast(new TDKBMoveHandler(this, "PODOP", null));

                _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));

                if (IsNeedMapOnUnload)
                    _lstHandler.AddLast(new TDKBGetHandler(this, "MAPRD", null));



            }
            return true;



        }



        protected override bool fMonitorUnload(object[] param)
        {
            if (_lstHandler.Count == 0 && !_connection.IsBusy) 
                OnActionDone(null);
            return false;
        }

        protected override bool fStartLoad(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
            }

            int waitCount = 0;
            while(_lstHandler.Count != 0|| _connection.IsBusy)
            {
                waitCount++;
                Thread.Sleep(50);
                if(waitCount > 200)
                {
                    OnError("Query State timeout before load");
                    return false;
                }
            }





            if (!_isPlaced)
            {
                EV.PostAlarmLog(Name, $"No carrier on {Name},can't load.");
                return false;
            }
            if (_isDocked)
            {
                EV.PostAlarmLog(Name, $"Carrier is docked on {Name},can't load.");
                return false;
            }
            if (IsRequestFOSBMode)
            {
                lock (_locker)
                {
                    if (!IsFosbModeActual)
                    {
                        _lstHandler.AddLast(new TDKBSetHandler(this, "FSB", "ON"));
                        _lstHandler.AddLast(new TDKBGetHandler(this, "FSBxx", null));
                    }
                    if (ClampPosition == TDKPosition.Open)
                    {
                        _lstHandler.AddLast(new TDKBMoveHandler(this, "PODCL", null));
                    }
                    _lstHandler.AddLast(new TDKBMoveHandler(this, "DORBK", null));
                    

                    if (IsMapWaferByLoadPort)
                    {
                        _lstHandler.AddLast(new TDKBMoveHandler(this, "YDOOR", null));
                        _lstHandler.AddLast(new TDKBMoveHandler(this, "CLMPO", null));
                        _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                        _lstHandler.AddLast(new TDKBGetHandler(this, "MAPRD", null));
                    }
                    else
                    {
                        _lstHandler.AddLast(new TDKBMoveHandler(this, "ZDRDW", null));
                        _lstHandler.AddLast(new TDKBMoveHandler(this, "YDOOR", null));                        
                        _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                    }
                    

                }
            }
            else
            {
                lock (_locker)
                {
                    if (IsFosbModeActual)
                    {
                        _lstHandler.AddLast(new TDKBSetHandler(this, "FSB", "OF"));
                        _lstHandler.AddLast(new TDKBGetHandler(this, "FSBxx", null));
                    }

                    if (ClampPosition == TDKPosition.Open)
                    {
                        _lstHandler.AddLast(new TDKBMoveHandler(this, "PODCL", null));
                    }
                    _lstHandler.AddLast(new TDKBMoveHandler(this, "YDOOR", null));
                    _lstHandler.AddLast(new TDKBMoveHandler(this, "VACON", null));
                    _lstHandler.AddLast(new TDKBMoveHandler(this, "DOROP", null));


                    if (IsMapWaferByLoadPort)
                    {
                        
                        _lstHandler.AddLast(new TDKBMoveHandler(this, "CLMPO", null));
                        _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                        _lstHandler.AddLast(new TDKBGetHandler(this, "MAPRD", null));                        
                    }
                    else
                    {
                        _lstHandler.AddLast(new TDKBMoveHandler(this, "CLDOP", null));
                        _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));                        
                    }
                }
            }
            return true;
        }
        protected override bool fMonitorLoad(object[] param)
        {
            if (_lstHandler.Count == 0 && !_connection.IsBusy)
                OnActionDone(null);
            return false;
        }

        protected override bool fStartInit(object[] param)
        {
            lock (_locker)
            {
                _lstHandler.AddLast(new TDKBModHandler(this, "ONMGV", null));

                if(IsRequestFOSBMode && _isUndockOCBeforeHome)
                {
                    _lstHandler.AddLast(new TDKBMoveHandler(this, "YWAIT", null));
                }


                if (param.Length >= 1 && param[0].ToString() == "ForceHome")
                    _lstHandler.AddLast(new TDKBMoveHandler(this, "ABORG", null));
                else
                    _lstHandler.AddLast(new TDKBMoveHandler(this, "ORGSH", null));
                
                if(E84DeviceType == E84DeviceTypeEnum.Internal)
                {
                    _lstHandler.AddLast(new TDKBSetHandler(this, "E84EN/", "OFF"));
                    _lstHandler.AddLast(new TDKBSetHandler(this, "e84rv", "1"));
                    _lstHandler.AddLast(new TDKBSetHandler(this, "e84ce/", "1"));

                    if (SC.ContainsItem($"LoadPort.{Name}.AccessMode") && SC.GetValue<bool>($"LoadPort.{Name}.AccessMode"))
                    {
                        _lstHandler.AddLast(new TDKBSetHandler(this, "E84EN/", "ON"));
                        IsAccessAuto = true;
                    }
                }               
                
                _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                _lstHandler.AddLast(new TDKBGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new TDKBGetHandler(this, "LEDST", null));
            }
            return true;
        }
        protected override bool fMonitorInit(object[] param)
        {
            IsBusy = false;
            if (_lstHandler.Count == 0 && !_connection.IsBusy)
                OnActionDone(null);
            return false;
        }
        public override bool SetIndicator(Indicator light, IndicatorState state, out string reason)
        {
            reason = "";
            return fStartExecute(new object[] { "SetIndicator", light, state });
        }

        protected override bool fStartReset(object[] param)
        {
            _lstHandler.Clear();
            if (_isTcpConnection)
            {
                if (!_tcpConnection.IsConnected)
                    _tcpConnection.Connect();
                _tcpConnection.ForceClear();
            }
            else
            {
                if (!_connection.IsConnected)
                    _connection.Connect();
                _connection.ForceClear();
            }

            lock (_locker)
            {
                _lstHandler.AddLast(new TDKBModHandler(this, "ONMGV", null));
                _lstHandler.AddLast(new TDKBSetHandler(this, "RESET", null));

                if (E84DeviceType == E84DeviceTypeEnum.Internal)
                {
                    _lstHandler.AddLast(new TDKBSetHandler(this, "E84EN/", "OFF"));
                    _lstHandler.AddLast(new TDKBSetHandler(this, "e84rv", "1"));
                    if (SC.ContainsItem($"LoadPort.{Name}.AccessMode") && SC.GetValue<bool>($"LoadPort.{Name}.AccessMode"))
                    {
                        _lstHandler.AddLast(new TDKBSetHandler(this, "E84EN/", "ON"));
                        IsAccessAuto = true;
                    }
                }

                _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                _lstHandler.AddLast(new TDKBGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new TDKBGetHandler(this, "LEDST", null));
                
            }
            return true;
        }
        protected override bool fMonitorReset(object[] param)
        {
            if (_lstHandler.Count == 0 && !_connection.IsBusy)
                OnActionDone(null);
            return false;
        }
        public override void OnError(string error = "")
        {
            lock (_locker)
            {
                _lstHandler.Clear();
                if (_isTcpConnection)
                    _tcpConnection.ForceClear();
                else
                    _connection.ForceClear();
                _lstHandler.AddLast(new TDKBGetHandler(this, "STATE", null));
                _lstHandler.AddLast(new TDKBGetHandler(this, "FSBxx", null));
                _lstHandler.AddLast(new TDKBGetHandler(this, "LEDST", null));
            }

            base.OnError(error);
        }

        public void OnAbs(string absMsg)
        {
            try
            {
                string absContext = absMsg.Split('/')[1].Replace(";", "").Replace("\r","");
                EV.Notify($"{Name}{absContext}");

            }
            catch(Exception ex)
            {
                LOG.Write(ex);
            }
            OnError("Recieve ABS Message:" + absMsg);



        }
        public void OnNak(string absMsg)
        {
            try
            {
                string absContext = absMsg.Split('/')[1].Replace(";", "").Replace("\r", "");
                EV.Notify($"{Name}{absContext}");

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            OnError("Recieve Nak Message:" + absMsg);



        }
        public override void Terminate()
        {
            _thread.Stop();
            Thread.Sleep(100);
            if (!SC.ContainsItem($"{_scRoot}.{Name}.CloseConnectionOnShutDown") || SC.GetValue<bool>($"{_scRoot}.{Name}.CloseConnectionOnShutDown"))
            {
                LOG.Write($"Close {Address} for connection of {LPModuleName}");
                _connection.Disconnect();
                _connection.TerminateCom();
                
            }
            base.Terminate();
        }
        public override bool IsForbidAccessSlotAboveWafer()
        {
            if (SC.ContainsItem($"CarrierInfo.ForbidAccessAboveWaferCarrier{InfoPadCarrierIndex}"))
                return SC.GetValue<bool>($"CarrierInfo.ForbidAccessAboveWaferCarrier{InfoPadCarrierIndex}");
            return base.IsForbidAccessSlotAboveWafer();
        }

        public override void OnSlotMapRead(string _slotMap)
        {
            if(IsVerifyPreDefineWaferCount)
            {
                int wcount = 0;
                foreach(var ch in _slotMap)
                {
                    if(ch != '0')
                    {
                        wcount++;
                    }
                }
                WaferCount = wcount;

                if (WaferCount != PreDefineWaferCount)
                {
                    EV.PostAlarmLog("LoadPort", $"{LPModuleName} mapping error,predefine count is {PreDefineWaferCount}, " +
                        $"Mapping result is {WaferCount}.");
                    OnError("Mapping Error");
                }
            }

            base.OnSlotMapRead(_slotMap);
        }



    }
   
}
