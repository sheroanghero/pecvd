using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Communications
{
    /// <summary>
    /// 管理所有连接，通常在PLC之前
    /// </summary>
    public class ConnectionManager : Singleton<ConnectionManager>
    {
        public List<NotifiableConnectionItem> ConnectionList
        {
            get
            {
                foreach (var conn in _connections)
                {
                    conn.IsConnected = _dicConnections[conn.Name].IsConnected;
                }

                return _connections;
            }
        }

        private Dictionary<string, IConnection> _dicConnections = new Dictionary<string, IConnection>();

        private List<NotifiableConnectionItem> _connections = new List<NotifiableConnectionItem>();

        public void Initialize()
        {
            OP.Subscribe("Connection.Connect", (string cmd, object[] args) =>
            {
                Connect((string) args[0]);
                return true;
            });

            OP.Subscribe("Connection.Disconnect", (string cmd, object[] args) =>
            {
                Disconnect((string)args[0]);
                return true;
            });

            DATA.Subscribe("Connection.List", () => ConnectionList);
        }
 
        public void Terminate()
        {

        }

        public void Subscribe(string name, IConnection conn)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (conn == null)
            {
                return;
            }

            _connections.Add(new NotifiableConnectionItem(){Address = conn.Address, Name = name});
            _dicConnections[name] = conn;
        }

        public void Connect(string name)
        {
            if (_dicConnections.ContainsKey(name) && _dicConnections[name] != null)
            {
                _dicConnections[name].Connect();
            }
        }

        public void Disconnect(string name)
        {
            if (_dicConnections.ContainsKey(name) && _dicConnections[name] != null)
            {
                _dicConnections[name].Disconnect();
            }
        }


    }
}
