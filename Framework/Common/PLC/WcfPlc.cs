using Aitex.Core.RT.Device;
using MECF.Framework.Common.Communications;
using MECF.Framework.Common.Event;
using System;
using System.Xml;

namespace MECF.Framework.Common.PLC
{
    public class WcfPlc : BaseDevice, IConnection, IConnectable, IPlc
    {
        private WcfPlcServiceClient _plcClient;

        public AlarmEventItem AlarmConnectFailed { get; set; }
        public AlarmEventItem AlarmCommunicationError { get; set; }
        public event Action OnConnected;
        public event Action OnDisconnected;

        private FsmConnection _connection;

        private int _heartbeat = 1;
        private string _wcfConfigName;

        public WcfPlc(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;

            Name = node.GetAttribute("id");

            _wcfConfigName = node.GetAttribute("wcf");
        }

        public WcfPlc(string module,  string name, string wcfConfigName)
        {
            Module = module;

            Name = name;

            _wcfConfigName = wcfConfigName;
        }

        public bool Initialize()
        {
            if (string.IsNullOrEmpty(_wcfConfigName))
            {
                _plcClient = new WcfPlcServiceClient();
            }
            else
            {
                _plcClient = new WcfPlcServiceClient(_wcfConfigName);
            }

            AlarmConnectFailed = SubscribeAlarm($"{Module}.{Name}.ConnectionError", $"Can not connect with {Address}", null);
            AlarmCommunicationError = SubscribeAlarm($"{Module}.{Name}.CommunicationError", $"Can not Communication {Address}", null);


            _connection = new FsmConnection();
            IConnectionContext contex = new StaticConnectionContext()
            {
                IsEnabled = true,
                Address = "WCF PLC",
                EnableCheckConnection = false,
                EnableLog = false,
                MaxRetryConnectCount = 3,
                IsAscii = false,
                NewLine = string.Empty,
                RetryConnectIntervalMs = 1000,
            };
            _connection.OnConnected += _connection_OnConnected;
            _connection.OnDisconnected += _connection_OnDisconnected;
            _connection.OnError += _connection_OnError;
            _connection.Initialize(50, this, contex);


            return true;
        }

        private void _connection_OnError(string error)
        {
            AlarmConnectFailed.Set(error);
        }

        private void _connection_OnDisconnected()
        {
            if (OnDisconnected != null)
                OnDisconnected();
        }

        private void _connection_OnConnected()
        {
            if (OnConnected != null)
                OnConnected();
        }

        public void Monitor()
        {

        }

        public void Terminate()
        {

        }

        public void Reset()
        {
            ResetAlarm();

            _connection.InvokeReset();
        }

        #region IConnection
        public string Address { get; }
        public bool IsConnected { get; set; }
        public bool Connect()
        {
            _connection.InvokeConnect();
            return true;
        }

        public bool Disconnect()
        {
            _connection.InvokeDisconnect();
            return true;
        }


        #endregion


        #region IConnectable
#pragma warning disable 0067
        public event Action<string> OnCommunicationError;
        public event Action<string> OnAsciiDataReceived;
        public event Action<byte[]> OnBinaryDataReceived;
#pragma warning restore 0067
        public bool Connect(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public bool Disconnect(out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public bool CheckIsConnected()
        {
            if (_plcClient == null)
                return false;

            _heartbeat++;

            if(_plcClient.Heartbeat(_heartbeat)>0 && !_plcClient.ActionFailed)
                return true;

            return false;
        }

        public bool ReadBool(string address, out bool[] data, int length, out string reason)
        {
            if (!_connection.IsConnected)
            {
                reason = "Not connected with PLC";
                data = null;
                return false;
            }

            if (HasAlarm)
            {
                reason = "Has alarm";
                data = null;
                return false;
            }

            data = _plcClient.ReadDi(0,  length, out reason);

            if (data == null || _plcClient.ActionFailed)
            {
                AlarmCommunicationError.Description = "Failed read PLC data";
                AlarmCommunicationError.Set();
                return false;
            }

            return true;
        }

        public bool ReadInt16(string address, out short[] data, int length, out string reason)
        {
            if (!_connection.IsConnected)
            {
                reason = "Not connected with PLC";
                data = null;
                return false;
            }

            if (HasAlarm)
            {
                reason = "Has alarm";
                data = null;
                return false;
            }

            data = _plcClient.ReadAiInt16(0, length, out reason);

            if (data == null || _plcClient.ActionFailed)
            {
                AlarmCommunicationError.Description = "Failed read PLC data";
                AlarmCommunicationError.Set();
                return false;
            }

            return true;
        }

        public bool WriteBool(string address, bool[] data,  out string reason)
        {
            if (!_connection.IsConnected)
            {
                reason = "Not connected with PLC";
                data = null;
                return false;
            }

            if (HasAlarm)
            {
                reason = "Has alarm";
                data = null;
                return false;
            }
             
            bool result = _plcClient.WriteDo(0, data, out reason);

            if (!result || _plcClient.ActionFailed)
            {
                AlarmCommunicationError.Description = "Failed write PLC data";
                AlarmCommunicationError.Set();
                return false;
            }

            return true;
        }

        public bool WriteInt16(string address, short[] data , out string reason)
        {
            if (!_connection.IsConnected)
            {
                reason = "Not connected with PLC";
                data = null;
                return false;
            }

            if (HasAlarm)
            {
                reason = "Has alarm";
                data = null;
                return false;
            }

            bool result = _plcClient.WriteAoInt16(0, data, out reason);

            if (!result || _plcClient.ActionFailed)
            {
                AlarmCommunicationError.Description = "Failed write PLC data";
                AlarmCommunicationError.Set();
                return false;
            }

            return true;
        }

        public bool SendBinaryData(byte[] data)
        {
            return true;
        }

        public bool SendAsciiData(string data)
        {
            return true;
        }


        #endregion


        public bool Read(string variable, out object data, string type, int length, out string reason)
        {
            if (!_connection.IsConnected)
            {
                reason = "Not connected with PLC";
                data = null;
                return false;
            }

            if (HasAlarm)
            {
                reason = "Has alarm";
                data = null;
                return false;
            }

            bool result= _plcClient.Read(variable, out data, type, length, out reason);

            if (data == null || _plcClient.ActionFailed)
            {
                AlarmCommunicationError.Description = "Failed read PLC data";
                AlarmCommunicationError.Set();
                return false;
            }

            return result;
        }

        public bool WriteArrayElement(string variable, int index, object value, out string reason)
        {
            if (!_connection.IsConnected)
            {
                reason = "Not connected with PLC";
                return false;
            }

            if (HasAlarm)
            {
                reason = "Has alarm";
                return false;
            }

            bool result = _plcClient.WriteArrayElement(variable, index, value, out reason);

            if (_plcClient.ActionFailed)
            {
                AlarmCommunicationError.Description = "Failed read PLC data";
                AlarmCommunicationError.Set();
                return false;
            }

            return result;
        }

        public bool ReadFloat(string address, out float[] data, int length, out string reason)
        {
            if (!_connection.IsConnected)
            {
                reason = "Not connected with PLC";
                data = null;
                return false;
            }

            if (HasAlarm)
            {
                reason = "Has alarm";
                data = null;
                return false;
            }

            data = _plcClient.ReadAiFloat(0, length, out reason);

            if (data == null || _plcClient.ActionFailed)
            {
                AlarmCommunicationError.Description = "Failed read PLC data";
                AlarmCommunicationError.Set();
                return false;
            }

            return true;
        }

        public bool WriteFloat(string address, float[] data, out string reason)
        {
            if (!_connection.IsConnected)
            {
                reason = "Not connected with PLC";
                data = null;
                return false;
            }

            if (HasAlarm)
            {
                reason = "Has alarm";
                data = null;
                return false;
            }

            bool result = _plcClient.WriteAoFloat(0, data, out reason);

            if (!result || _plcClient.ActionFailed)
            {
                AlarmCommunicationError.Description = "Failed write PLC data";
                AlarmCommunicationError.Set();
                return false;
            }

            return true;
        }

        public bool ReadInt32(string address, out int[] data, int length, out string reason)
        {
            throw new NotImplementedException();
        }
    }

}
