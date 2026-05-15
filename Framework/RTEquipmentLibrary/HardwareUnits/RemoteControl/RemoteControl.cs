using Aitex.Core.Common;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Event;
using MECF.Framework.Common.SubstrateTrackings;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Aligners.AlignersBase;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.YaskawaRobots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.RemoteControl
{
    public class RemoteControl : BaseDevice,IDevice, IConnection
    {
        public RemoteControl(string module,string name,string scRoot) 
        {
            _scRoot = scRoot;
            Module = module;
            Name = name;
            _scRoot = scRoot;
            _address = SC.GetStringValue($"{Module}.{Name}.Address");
            _enableLog = SC.GetValue<bool>($"{Module}.{Name}.EnableLogMessage");
            _senderID = SC.GetStringValue($"{Module}.{Name}.SenderID");
            _receiverID = SC.GetStringValue($"{Module}.{Name}.ReceiverID");
            _connection = new RemoteControlConnection(this,_address);
            _connection.EnableLog(_enableLog);

            AlarmEvent = SubscribeAlarm($"{Module}.{Name}.Warning", $"Alarm message received From {Address}", null);

            ConnectionManager.Instance.Subscribe($"{Name}", this);
        }

        private void ExecuteHandler(HandlerBase handler)
        {
            string commandstr = $"{UnitNumber}";
            commandstr += $"{handler.SendText}";

            handler.SendText = $"{commandstr}\r\n";
            _connection.Execute(handler);

        }
        public int UnitNumber
        {
            get; private set;
        }
        private string _scRoot;
        public string PortName;
        private string _address;
        private bool _enableLog;
        private string _senderID;
        private string _receiverID;
        private RemoteControlConnection _connection;
        private PeriodicJob _thread;
        private R_TRIG _trigCommunicationError = new R_TRIG();
        private R_TRIG _trigRetryConnect = new R_TRIG();
        public string Address => _address;
        private object _locker = new object();
        private LinkedList<HandlerBase> _lstHandlers = new LinkedList<HandlerBase>();

        public bool IsConnected => _connection.IsConnected;

        public bool Connect()
        {
            return _connection.Connect();
        }

        public bool Disconnect()
        {
            return _connection.Disconnect();
        }

        AlarmEventItem AlarmEvent { get; set; }

        public string EventID { get; set; }

        public int ActionCode { get; set; }
        public int RePlyCode { get; set; }
        public string ErrorDescription { get; set; }


        public string RunName { get; set; }


        public int NumberOfRecipe { get; set; }
        public List<string> RecipeNames { get; set; }


        public bool ParseStatus(RemoteControlMessage message)
        {
            try
            {
                EventID = message.MessagePart[5];
                EV.PostInfoLog($"{Module}", $"Get Event From RemodControl {EventID}.");
                //AlarmEvent.Description = $"Alarm message {EventID} received From {Address}";
                //AlarmEvent.Set();
                
                return true;

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }
        public bool ParseGetSensorStatus(int replyCode, string errorDescription)
        {
            try
            {
                RePlyCode = replyCode;
                ErrorDescription = errorDescription;
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        internal void OnError(string rawMessage)
        {
            EV.PostAlarmLog(Module, $"{Name} occurred error: {rawMessage}");
        }

        public bool ParseStartMeasurement(int replyCode,string errorDescription, string runName)
        {
            try
            {
                RePlyCode = replyCode;
                ErrorDescription = errorDescription;
                RunName = runName;
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        public bool ParseGetEPDRecipeList(int replyCode, string errorDescription,int numberOfRecipe,List<string> recipeNames)
        {
            try
            {
                RePlyCode = replyCode;
                ErrorDescription = errorDescription;
                NumberOfRecipe = numberOfRecipe;
                RecipeNames = recipeNames;
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        public bool ParseRunAction(int replyCode, string errorDescription)
        {
            try
            {
                RePlyCode = replyCode;
                ErrorDescription = errorDescription;
                return true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }
        }

        public AITDeviceData DeviceData
        {
            get
            {
                AITDeviceData data = new AITDeviceData()
                {
                    Module = Module,
                    DeviceName = Name,
                };
                data.AttrValue.Add("EventID", EventID==null?"":EventID);
                data.AttrValue.Add("RePlyCode", RePlyCode);
                data.AttrValue.Add("ErrorDescription", ErrorDescription == null ? "" : ErrorDescription);
                data.AttrValue.Add("RunName", RunName == null ? "" : RunName);
                data.AttrValue.Add("NumberOfRecipe", NumberOfRecipe);
                
                data.AttrValue.Add("RecipeNames", RecipeNames);
                return data;
            }
        }
        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            OP.Subscribe($"{Module}.{Name}.GetSensorStatus", (function, args) =>
            {
                GetSensorStatus();
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.StartMeasurement", (function, args) =>
            {
                string recipeName = args[0].ToString();
                string waferID = args[1].ToString();
                string lotId = args[2].ToString();
                string stepName = args[3].ToString();
                StartMeasurement(recipeName, waferID, lotId, stepName);
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.GetEPDRecipeList", (function, args) =>
            {
                

                GetEPDRecipeList();
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.RequestRunAction", (function, args) =>
            {
                RequestRunAction();
                return true;
            });
            OP.Subscribe($"{Module}.{Name}.Reset", (function, args) =>
            {
                Reset();
                return true;
            });
            return true;
        }

        private void RequestRunAction()
        {
            lock (_locker)
            {
                _lstHandlers.AddLast(new RemoteControlRequestRunActionHandler(this, _senderID, _receiverID));
            }
        }

        private void GetEPDRecipeList()
        {
            lock (_locker)
            {
                
                _lstHandlers.AddLast(new RemoteControlGetEPDRecipeListHandler(this, _senderID, _receiverID));
            }
        }

        private void StartMeasurement(string recipeName, string waferID, string lotID, string stepName)
        {
            lock (_locker)
            {
                List<string> paramList = new List<string>();
                paramList.Add(recipeName);
                paramList.Add(waferID);
                paramList.Add(lotID);
                paramList.Add(stepName);
                _lstHandlers.AddLast(new RemoteControlStartMeasurementHandler(this, paramList, _senderID, _receiverID));
            }
        }

        private void GetSensorStatus()
        {
            lock (_locker)
            {
                _lstHandlers.AddLast(new RemoteControlGetSensorStatusHandler(this, _senderID, _receiverID));
            }
        }

        public void Monitor()
        {
            try
            {
                if (!_connection.IsConnected || _connection.IsCommunicationError)
                {
                    lock (_locker)
                    {
                        _lstHandlers.Clear();
                    }

                    _trigRetryConnect.CLK = !_connection.IsConnected;
                    if (_trigRetryConnect.Q)
                    {
                        if (!_connection.Connect())
                        {
                            EV.PostAlarmLog(Module, $"Can not connect with {_connection.Address}, {Module}.{Name}");
                        }
                        else
                        {
                        }
                    }
                    return;
                }

                HandlerBase handler = null;

                lock (_locker)
                {
                    while (_lstHandlers.Count > 0 || _connection.IsBusy)
                    {
                        if (!_connection.IsBusy)
                        {
                            if (_lstHandlers.Count > 0)
                            {
                                handler = _lstHandlers.First.Value;
                                ExecuteHandler(handler);
                                _lstHandlers.RemoveFirst();
                            }
                        }
                        else
                        {
                            _connection.MonitorTimeout();

                            _trigCommunicationError.CLK = _connection.IsCommunicationError;
                            if (_trigCommunicationError.Q)
                            {
                                _lstHandlers.Clear();
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public void Terminate()
        {
        }

        public void Reset()
        {
            lock (_locker)
            {
                _lstHandlers.Clear();
                _connection.ForceClear();
            }
        }
    }


}
