using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.RT.Core.IoProviders;

namespace MECF.Framework.Simulator.Core.IoProviders
{
    public class PlcBuffer
    {
        public IoType Type;
        public int Offset;
        public int Size;
        public byte[] Buffer;
        public bool[] BoolValue;
        public ushort[] ShortValue;
        public float[] FloatValue;
    }

    public class MCProtocolPlcSimulator
    {
        public event Action<string> NotifyEvent;

        private PeriodicJob _threadSocket;
        private PeriodicJob _threadTimer;

        private Socket _socketServer;

        private string _ip;

 
        private bool _stopFlag;

        protected List<PlcBuffer> _buffers = new List<PlcBuffer>();
 
        private int _port = 6731;
        private int _socketId = 101;
        private int _stationId = 102;

        private byte[] _bufferIn;
        private byte[] _bufferOut;

 
        private MCProtocol.MC_RESPONSE_HEADER _response;
 
        public MCProtocolPlcSimulator(string ip, int port)
        {
            _ip = ip;

            _port = port;




            _response = new MCProtocol.MC_RESPONSE_HEADER()
            {
                ProtocolId = MCProtocol.MC_SUBHEADER_RESPONSE_MESSAGE,
                NetworkId = (byte)_socketId,
                StationId = (byte)_stationId,
                RequestIoNumber = MCProtocol.MC_REQUEST_MODULE_IO_NUMBER,
                RequestStationNumber = MCProtocol.MC_REQUEST_MODULE_STATION_NUMBER,
                ResponseDataLen = 0,
                CompleteCode = (ushort)(MCProtocol.MC_COMPLETE_CODE_SUCCESS  ),
            };

            _bufferOut = new byte[2048];
            _bufferIn = new byte[2048];

            _socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _threadSocket = new PeriodicJob(50, OnMonitor, "MCProtocolPlcSimulator",true);

            _threadTimer = new PeriodicJob(1000, OnTimer, "MCProtocolPlcSimulatorTimer", true);
        }

        protected virtual bool OnTimer()
        {
            return true;
        }

        private void PerformNotifyEvent(string msg)
        {
            if (NotifyEvent != null)
            {
                NotifyEvent(msg);
            }
        }

        private bool OnMonitor()
        {
            try
            {
                EndPoint ep = new IPEndPoint(IPAddress.Loopback, _port);
                _socketServer.Bind(ep);
                _socketServer.Listen(3);

                Socket s;
                while (!_stopFlag)
                {
                    PerformNotifyEvent("Waiting for connection ...");
                    s = _socketServer.Accept();
                    PerformNotifyEvent("Connected.");
                    
                    s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    try
                    {
                        while (!_stopFlag && OnTransmission(s))
                        {
                            PerformNotifyEvent("Waiting for another command ...");
                        }
                        PerformNotifyEvent("receive error buffer content and exit ...");
                    }
                    catch (Exception ex)
                    {
                        LOG.Write(ex);
                        return true;
                    }
                    PerformNotifyEvent("A client disconnected from port " + _port);
                    s = null;
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                try
                {
                    _socketServer.Close();
                }
                catch (Exception )
                {
                     
                }

                _socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                return true;
            }

            return true;
        }

        public void Start()
        {
            _threadSocket.Start();
        }

        public void Stop()
        {
            _stopFlag = true;
            _threadSocket.Stop();
        }

        protected bool OnTransmission(Socket s)
        {
            int size = s.Receive(_bufferIn, 0, MCProtocol.MC_QHEADER_COMMAND_SIZE, SocketFlags.None);
            if (size < MCProtocol.MC_QHEADER_COMMAND_SIZE)
            {
                return false;
            }

            MCProtocol.MC_COMMAND_HEADER header = MCProtocol.ToStruct<MCProtocol.MC_COMMAND_HEADER>(_bufferIn);
            int dataLength = header.RequestDataLen - MCProtocol.MC_BATCH_COMMAND_SIZE - MCProtocol.JUNK_SIZE;
            if (dataLength < 0)
            {
                return false;
            }

            size = s.Receive(_bufferIn, 0, MCProtocol.MC_BATCH_COMMAND_SIZE, SocketFlags.None);
            if (size < MCProtocol.MC_BATCH_COMMAND_SIZE)
            {
                return false;
            }

            MCProtocol.MC_BATCH_COMMAND cmd = MCProtocol.ToStruct<MCProtocol.MC_BATCH_COMMAND>(_bufferIn);
            int offset = cmd.Reserved * (0xFFFF + 1) + cmd.HeadAddr;
            if (dataLength > 0)
            {
                size = s.Receive(_bufferIn, 0, dataLength, SocketFlags.None);
                if (size < dataLength)
                {
                    return false;
                }
            }
 
            
            byte[] data = null;
            if (cmd.Command == MCProtocol.MC_COMMAND_BATCH_READ)
            {
                data = GetData(offset, cmd.DevicePoints, cmd.SubCommand == MCProtocol.MC_SUBCOMMAND_BIT_UNITS);
            }
            else
            {
                SetData(offset, _bufferIn, dataLength, cmd.SubCommand == MCProtocol.MC_SUBCOMMAND_BIT_UNITS);
            }

            _response.ResponseDataLen = (ushort)(MCProtocol.JUNK_SIZE + (data == null ? 0 : data.Length));

            byte[] response = MCProtocol.Struct2Bytes(_response);
            Array.Copy(response, 0, _bufferOut, 0, response.Length);
            s.Send(_bufferOut, response.Length, SocketFlags.None);
 

            if (data != null)
            {
                Array.Copy(data, 0, _bufferOut, 0, data.Length);

                s.Send(_bufferOut, data.Length, SocketFlags.None);
 
            }
            return true;
        }

 
        protected void SetData(int headAddr, byte[] data, int length, bool isBit)
        {
            IoType ioType = isBit ? IoType.DO : IoType.AO;

            if (ioType == IoType.DO)
            {
                bool[] boolData = MCProtocol.TransByteDataToBoolArray(data, 0, length);
                
                PlcBuffer buffer = _buffers.Find(x => x.Type == ioType);
                if (buffer != null&&boolData!=null&&boolData.Length>0&& buffer.BoolValue.Length >= headAddr + boolData.Length)
                {
                    Array.Copy(boolData, 0, buffer.BoolValue, headAddr, boolData.Length);
                }

            }
            else
            {
                ushort[] shortValue = MCProtocol.Byte2Ushort(data, 0, length);
                PlcBuffer buffer = _buffers.Find(x=>x.Type == ioType);
                if (buffer != null && shortValue != null && shortValue.Length > 0 && buffer.ShortValue.Length >= headAddr + shortValue.Length) 
                {
                    Array.Copy(shortValue, 0, buffer.ShortValue, headAddr, shortValue.Length);
                }
                 
            }
 
        }
 
        protected void SetDi(int headAddr, int offset, bool value)
        {
            PlcBuffer buffer = _buffers.Find(x =>x.Type ==  IoType.DI );
            if (buffer != null)
            {
                buffer.BoolValue[offset] = value;
            }
        }

        protected void SetAi(int headAddr, int offset, ushort value)
        {
            PlcBuffer buffer = _buffers.Find(x => x.Type ==   IoType.AI  );
            if (buffer != null)
            {
                buffer.ShortValue[offset] = value;
            }
        }

        protected virtual byte[] GetData(int headAddr, ushort length, bool isBit)
        {
            IoType ioType = isBit ? IoType.DI : IoType.AI;
            if(!isBit)
            {
                var tmp = _buffers.Find(x => x.Type == ioType);
                if(tmp != null && tmp.ShortValue != null)
                {
                    var remain = tmp.ShortValue.Length - headAddr;
                    var size = remain >= length ? length : (remain > 0 ? remain : 0);
                    if (size > 0)
                    {
                        ushort[] bf = new ushort[size];
                        Array.Copy(tmp.ShortValue, headAddr, bf, 0, bf.Length);
                        return MCProtocol.Ushort2Byte(bf);
                    }
                    else
                        return null;
                }

            }

            PlcBuffer buffer = _buffers.Find(x => x.Type == ioType);

            if (buffer == null)
            {
                return null;
            }

            byte[] result = null;
            if (ioType == IoType.DI)
            {
                bool[] value = buffer.BoolValue;
                int size = (length + 1) / 2;
                 result = new byte[size];

                for (int i = 0; i < size; i++)
                {
                    if (value[headAddr+i * 2 + 0])
                        result[i] += 0x10;
                    if ((i * 2 + 1) < value.Length)
                    {
                        if (value[headAddr + i * 2 + 1])
                            result[i] += 0x01;
                    }
                }

                //result = MCProtocol.TransBoolArrayToByteData(buffer.BoolValue);
            }
            else
            {
                result = MCProtocol.Ushort2Byte(buffer.ShortValue);
            }

            return result;
        }
 


    }
}
