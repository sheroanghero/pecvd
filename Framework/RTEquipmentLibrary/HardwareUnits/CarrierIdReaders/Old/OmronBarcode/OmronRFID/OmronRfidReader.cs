using System;
using System.Text.RegularExpressions;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.LoadPorts;




namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.CarrierIdReaders.OmronRFID
{

    public class OmronRfidReader : CarrierIdReader, IConnection
    {
 
        public DeviceState State
        {
            get
            {
                if (!Initalized)
                {
                    return DeviceState.Unknown;
                }
                if (Error)
                {
                    return DeviceState.Error;
                }

                if (Busy)
                    return DeviceState.Busy;

                return DeviceState.Idle;
            }
        }

        public bool Initalized { get; set; }
        public bool Busy
        {
            get { return _foregroundHandler != null; }
        }

        public bool Error
        {
            get{return _commErr;}
        }

        public bool Communication
        {
            get
            {
                return _socket == null ? !_commErr : !_commErr && _socket.IsConnected;
            }
        }

        private string _foupId;

        public string FoupID
        {
            get
            {
                return _foupId;
            }
            set
            {
                //FAManager.Instance.ChangeFoupId(Name, value);
                _foupId = value;
            }
        }

        public string LoadPortName
        {
            get
            {
                return _loadPort;
            }
 
        }

        private static Object _locker = new Object();  
        private AsyncSocket _socket;

        private IHandler _foregroundHandler = null;  //current handler

        public const string delimiter = "\r";

        private bool _commErr = false;

        private string page;

        private string _addr;

        private string _loadPort ;

        public OmronRfidReader(string module, string name, string display, string deviceId, string address, string page, string loadport )
            : base(module, name, display, deviceId, address, page, loadport)
        {
            this.page = page;
            _addr = address;
            _loadPort = loadport ;

            _socket = new AsyncSocket(address);
            _socket.OnDataChanged += new AsyncSocket.MessageHandler(OnDataChanged);
            _socket.OnErrorHappened += new AsyncSocket.ErrorHandler(OnErrorHandler);

            Initalized = false;
        }

        public override bool Initialize()
        {
            ConnectionManager.Instance.Subscribe(Name, this);
            _socket.Connect(this._addr);

            DATA.Subscribe(Name, "RIDReaderBusy", () => Busy);
            DATA.Subscribe(Name, "FoupID", () => FoupID);
            DATA.Subscribe(Name, "RIDReaderState", () => State);
            DATA.Subscribe(Name, "CommunicationStatus", () => IsConnected);

            DATA.Subscribe(string.Format("Device.{0}.{1}", Module , Name), 
                () =>
                {
                    AITRfidReaderData data = new AITRfidReaderData()
                                        {
                                            DeviceName = Name,
                                            DeviceSchematicId = DeviceID,
                                            DisplayName = Display,
                                            IsBusy = Busy,
                                            IsError = Error,
                                            IsInitalized = Initalized,
                                            State = State.ToString(),
                    };

                    return data;
                }, SubscriptionAttribute.FLAG.IgnoreSaveDB);

            OP.Subscribe($"{Name}.Reconnect", (string cmd, object[] args) => Connect());
            Initalized = true;
            return true;
        }

 

        public override void Reset()
        {
            base.Reset();

            lock (_locker)
            {
                _foregroundHandler = null;
            }

            if (_commErr)
            {
                Connect();
            }

        }

        public string Address
        {
            get { return _addr; }
        }

        public bool IsConnected
        {
            get { return _socket.IsConnected; }
        }

        public override bool Connect()
        {
            _commErr = false;
            _socket.Connect(this._addr);
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }


        #region Command

        public override bool Read(out string reason)
        {
            reason = string.Empty;
            return execute(new handler<ReadHandler>(this.DeviceID, this.page), out reason);
        }

        public override bool Write(string id, out string reason)
        {
            reason = string.Empty;

            return execute(new handler<WriteHandler>(this.DeviceID, this.page,id), out reason);
        }

        #endregion


        private bool execute(IHandler handler, out string reason)
        {
            reason = string.Empty;
            lock (_locker)
            {
                if (_foregroundHandler != null)
                {
                    reason = string.Format("Busy,can not execute");
                    return false;
                }

                if(!handler.Execute(ref _socket))
                {
                    reason = "Communication error, please re-connect";
                    return false;
                }

                _foregroundHandler = handler;
            }
            return true;
        }
        
        private void OnDataChanged(string package)
        {
            try
            {
                package = package.ToUpper();
                string[] msgs = Regex.Split(package, delimiter);

                foreach (string msg in msgs)
                {
                    if (msg.Length > 0)
                    {
                        bool completed = false;
                        string resp = msg;

                        lock (_locker)
                        {
                            if (_foregroundHandler != null && _foregroundHandler.OnMessage(ref _socket, resp, out completed))
                            {
                                _foregroundHandler = null;
                            }
                        }
                    }
                }
            }
            catch (ExcuteFailedException ex)
            {
                EV.PostMessage(DeviceID, EventEnum.DefaultWarning, ex.Message);
                _foregroundHandler = null;
            }
            catch (InvalidPackageException ex)
            {
                EV.PostMessage(DeviceID, EventEnum.DefaultWarning, ex.Message);
                _foregroundHandler = null;
            }
            catch (System.Exception ex)
            {
                EV.PostMessage(Name, EventEnum.DefaultWarning, "【RFID】has exception：" + ex.ToString());
            }
        }


        private void OnErrorHandler(ErrorEventArgs args)
        {
            ReadFailed();

            _commErr = true;
            Initalized = false;
            EV.PostMessage(Module, EventEnum.CommunicationError, Display, args.Reason);
        }

        public void SetCarrierIdReadResult(string carrierId)
        {

            FoupID = carrierId;

            ReadOk( carrierId);
 
        }
    }
}

