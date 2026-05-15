using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;

namespace MECF.Framework.RT.Core.IoProviders
{
    public class MCProtocolPlc : IoProvider,IConnection
    {
        public string Address
        {
            get { return $"{_ip}:{_port}"; }
        }

        public bool IsConnected
        {
            get { return IsOpened; }
        }

        public bool Connect()
        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }

        private string _ip = "127.0.0.1";
        private string _localIp = "127.0.0.1";
        private int _port = 6731;
        private int _socketId = 101;
        private int _stationId = 102;

        private byte[] _bufferIn;
        private byte[] _bufferOut;
        private int _bufferSize = 2048;

        private MCProtocol.MC_COMMAND_HEADER _header;
        //private MCProtocol.MC_RESPONSE_HEADER _response;

        private MCProtocol.MC_BATCH_COMMAND _batchCommand;

        private MCSocket _socket;
        private int perIoLength = 960;

        protected override void SetParameter(XmlElement nodeParameter)
        {
            string strIp = nodeParameter.GetAttribute("ip");
            string strPort = nodeParameter.GetAttribute("port");
            string networkId = nodeParameter.GetAttribute("network_id");
            string stationId = nodeParameter.GetAttribute("station_id");
            if(!string.IsNullOrEmpty(nodeParameter.GetAttribute("buffer_size")))
            {
                int.TryParse(nodeParameter.GetAttribute("buffer_size"), out _bufferSize);
            }

            _port = int.Parse(strPort);
            _ip = strIp;
            _socketId = int.Parse(networkId);
            _stationId = int.Parse(stationId);

            ConnectionManager.Instance.Subscribe(Name, this);
            DATA.Subscribe($"{Module}.{Name}.IsConnected", () => _socket == null ? false : _socket.Connected);
        }

        protected override void Open()
        {
            _socket = new MCSocket();

            _header = new MCProtocol.MC_COMMAND_HEADER()
            {
                ProtocolID = MCProtocol.MC_SUBHEADER_COMMAND_MESSAGE,
                NetworkID = (byte)_socketId,
                StationID = (byte)_stationId,
                RequestIONumber = MCProtocol.MC_REQUEST_MODULE_IO_NUMBER,
                RequestStationNumber = MCProtocol.MC_REQUEST_MODULE_STATION_NUMBER,
                RequestDataLen = 0,
                CPUMonitorTimer = (ushort)(MCProtocol.MC_CPU_MONITOR_TIMER * 2),
            };


            _batchCommand = new MCProtocol.MC_BATCH_COMMAND()
            {
                Command = MCProtocol.MC_COMMAND_BATCH_READ,
                DeviceCode = MCProtocol.MC_DEVICE_CODE_DATA_REGISTER_WORD,
                DevicePoints = 0,
                HeadAddr = 0,
                Reserved = 0,
                SubCommand = MCProtocol.MC_SUBCOMMAND_WORD_UNITS,
            };

            _bufferOut = new byte[_bufferSize];
            _bufferIn = new byte[_bufferSize];

            if(_socket.Open(_ip, _port, _localIp))
                SetState(IoProviderStateEnum.Opened);
        }

        protected override void Close()
        {
            _socket.Close();
            SetState(IoProviderStateEnum.Closed);
        }

        Random _rd = new Random();
        protected override bool[] ReadDi(int offset, int size)
        {
            if(_socket == null || !_socket.Connected)
            {
                State = IoProviderStateEnum.Error;
                return null;
            }
            bool[] buff = new bool[size];

            int count = size / perIoLength;
            if (count < 1)
            {
                bool[] dibuffer = DoReadDi(offset, size);
                if (dibuffer != null)
                    Array.Copy(dibuffer, 0, buff, 0, dibuffer.Length);
            }
            else
            {
                bool[] dibuffer;
                for (int i = 0; i < count; i++)
                {
                    dibuffer = DoReadDi(i * perIoLength + offset, perIoLength);
                    if (dibuffer != null)
                        Array.Copy(dibuffer, 0, buff, i * perIoLength, dibuffer.Length);
                }

                if (size % perIoLength != 0)
                {
                    dibuffer = DoReadDi(offset + perIoLength * count, size % perIoLength);
                    if (dibuffer != null)
                        Array.Copy(dibuffer, 0, buff, size - size % perIoLength, dibuffer.Length);
                }
            }

            return buff;
        }
        protected bool[] DoReadDi(int offset, int size)
        {

            _batchCommand.Command = MCProtocol.MC_COMMAND_BATCH_READ;
            _batchCommand.SubCommand = MCProtocol.MC_SUBCOMMAND_BIT_UNITS;

            _batchCommand.HeadAddr = (ushort)(offset & 0xFFFF);
            _batchCommand.Reserved = (byte)(offset >> 16);

            _batchCommand.DevicePoints = (ushort)size;

            _header.RequestDataLen = (ushort)(MCProtocol.MC_BATCH_COMMAND_SIZE + MCProtocol.JUNK_SIZE);

            byte[] buffer = MCProtocol.Struct2Bytes(_header);
            byte[] command = MCProtocol.Struct2Bytes(_batchCommand);

            Array.Copy(buffer, 0, _bufferOut, 0, buffer.Length);
            Array.Copy(command, 0, _bufferOut, MCProtocol.MC_QHEADER_COMMAND_SIZE, command.Length);

            if (!WriteData(_bufferOut, MCProtocol.MC_QHEADER_COMMAND_SIZE + MCProtocol.MC_BATCH_COMMAND_SIZE))
            {
                return null;
            }

            if (!ReceiveData(_bufferIn, MCProtocol.MC_QHEADER_RESPONSE_SIZE))
            {
                return null;
            }

            MCProtocol.MC_RESPONSE_HEADER responseHeader =
                MCProtocol.ToStruct<MCProtocol.MC_RESPONSE_HEADER>(_bufferIn);

            if (responseHeader.CompleteCode != MCProtocol.MC_COMPLETE_CODE_SUCCESS)
            {
                return null;
            }

            int dataLength = responseHeader.ResponseDataLen - MCProtocol.JUNK_SIZE;
            if (!ReceiveData(_bufferIn, dataLength))
            {
                return null;
            }

            byte[] result = new byte[dataLength * 2];
            for (int i = 0; i < dataLength; i++)
            {
                if ((_bufferIn[i] & 0x10) == 0x10)
                {
                    result[i * 2 + 0] = 0x01;
                }

                if ((_bufferIn[i] & 0x01) == 0x01)
                {
                    result[i * 2 + 1] = 0x01;
                }
            }

            return result.Select(x => x == 0x01).Take(size).ToArray();
        }

        protected override short[] ReadAi(int offset, int size)
        {
            if (_socket == null || !_socket.Connected)
            {
                State = IoProviderStateEnum.Error;
                return null;
            }
            short[] buff = new short[size];

            int count = size / perIoLength;
            if (count < 1)
            {
                short[] aibuffer = DoReadAi(offset, size);
                if (aibuffer != null)
                    Array.Copy(aibuffer, 0, buff, 0, aibuffer.Length);
            }
            else
            {
                short[] aibuffer;
                for (int i = 0; i < count; i++)
                {
                    aibuffer = DoReadAi(i * perIoLength + offset, perIoLength);
                    if (aibuffer != null)
                        Array.Copy(aibuffer, 0, buff, i * perIoLength, aibuffer.Length);
                }

                if(size % perIoLength != 0)
                {
                    aibuffer = DoReadAi(offset + perIoLength * count, size % perIoLength);
                    if (aibuffer != null)
                        Array.Copy(aibuffer, 0, buff, size - size % perIoLength, aibuffer.Length);
                }
            }

            return buff;
        }

        protected override void WriteDo(int offset, bool[] data)
        {
            if (_socket == null || !_socket.Connected)
            {
                State = IoProviderStateEnum.Error;
                return;
            }
            bool[] databuffer = new bool[perIoLength];
            int count = data.Length / perIoLength;
            if (count < 1)
            {
                Array.Copy(data, 0, databuffer, 0, data.Length);
                Array.Resize(ref databuffer, data.Length);

                DoWriteDo(offset, databuffer);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    Array.Copy(data, i * perIoLength, databuffer, 0, databuffer.Length);
                    DoWriteDo(offset + perIoLength * i, databuffer);
                }

                if (data.Length % perIoLength != 0)
                {
                    Array.Copy(data, perIoLength * count, databuffer, 0, data.Length % perIoLength);
                    Array.Resize(ref databuffer, data.Length % perIoLength);
                    DoWriteDo(offset + perIoLength * count, databuffer);
                }
            }
        }

        protected short[] DoReadAi(int offset, int size)
        {
            _batchCommand.Command = MCProtocol.MC_COMMAND_BATCH_READ;
            _batchCommand.SubCommand = MCProtocol.MC_SUBCOMMAND_WORD_UNITS;

            _batchCommand.HeadAddr = (ushort)(offset & 0xFFFF);
            _batchCommand.Reserved = (byte)(offset >> 16);

            _batchCommand.DevicePoints = (ushort)size;

            _header.RequestDataLen = (ushort)(MCProtocol.MC_BATCH_COMMAND_SIZE + MCProtocol.JUNK_SIZE);

            byte[] buffer = MCProtocol.Struct2Bytes(_header);
            byte[] command = MCProtocol.Struct2Bytes(_batchCommand);

            Array.Copy(buffer, 0, _bufferOut, 0, buffer.Length);
            Array.Copy(command, 0, _bufferOut, MCProtocol.MC_QHEADER_COMMAND_SIZE, command.Length);

            if (!WriteData(_bufferOut, MCProtocol.MC_QHEADER_COMMAND_SIZE + MCProtocol.MC_BATCH_COMMAND_SIZE))
            {
                return null;
            }


            if (!ReceiveData(_bufferIn, MCProtocol.MC_QHEADER_RESPONSE_SIZE))
            {
                return null;
            }

            MCProtocol.MC_RESPONSE_HEADER responseHeader =
                MCProtocol.ToStruct<MCProtocol.MC_RESPONSE_HEADER>(_bufferIn);

            if (responseHeader.CompleteCode != MCProtocol.MC_COMPLETE_CODE_SUCCESS)
            {
                return null;
            }

            int dataLength = responseHeader.ResponseDataLen - MCProtocol.JUNK_SIZE;
            if (!ReceiveData(_bufferIn, dataLength))
            {
                return null;
            }

            int sizeofT1 = Marshal.SizeOf(typeof(byte));
            int sizeofT2 = Marshal.SizeOf(typeof(ushort));

            short[] result = new short[sizeofT1 * 2 / sizeofT2];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (short)BitConverter.ToUInt16(_bufferIn, i * 2);
            }

            return Array.ConvertAll(MCProtocol.Byte2Ushort(_bufferIn, 0, dataLength), x => (short)x);
        }

        protected void DoWriteDo(int offset, bool[] data)
        {
            if (_socket == null || !_socket.Connected)
            {
                State = IoProviderStateEnum.Error;
                return;
            }
            //    bool[] boolData = new bool[data.Length];
            //    boolData = data.Select(x => x==(byte)1).ToArray();
            //    WriteDo(offset, boolData);
            //}

            //protected void WriteDo(int offset, bool[] data)
            //{
            _batchCommand.Command = MCProtocol.MC_COMMAND_BATCH_WRITE;
            _batchCommand.SubCommand = MCProtocol.MC_SUBCOMMAND_BIT_UNITS;

            byte[] byteData = MCProtocol.TransBoolArrayToByteData(data);

            _batchCommand.HeadAddr = (ushort)(offset & 0xFFFF);
            _batchCommand.Reserved = (byte)(offset >> 16);

            _batchCommand.DevicePoints = (ushort)data.Length;

            _header.RequestDataLen = (ushort)(MCProtocol.MC_BATCH_COMMAND_SIZE + MCProtocol.JUNK_SIZE + byteData.Length);

            byte[] header = MCProtocol.Struct2Bytes(_header);
            byte[] command = MCProtocol.Struct2Bytes(_batchCommand);

            Array.Copy(header, 0, _bufferOut, 0, header.Length);
            Array.Copy(command, 0, _bufferOut, MCProtocol.MC_QHEADER_COMMAND_SIZE, command.Length);
            Array.Copy(byteData, 0, _bufferOut, MCProtocol.MC_QHEADER_COMMAND_SIZE + MCProtocol.MC_BATCH_COMMAND_SIZE, byteData.Length);

            if (!WriteData(_bufferOut, MCProtocol.MC_QHEADER_COMMAND_SIZE + MCProtocol.MC_BATCH_COMMAND_SIZE + byteData.Length))
            {
                return;
            }

            if (!ReceiveData(_bufferIn, MCProtocol.MC_QHEADER_RESPONSE_SIZE))
            {
                return;
            }

            MCProtocol.MC_RESPONSE_HEADER responseHeader =
                MCProtocol.ToStruct<MCProtocol.MC_RESPONSE_HEADER>(_bufferIn);

            if (responseHeader.CompleteCode != MCProtocol.MC_COMPLETE_CODE_SUCCESS)
            {
                LOG.Write("Write PLC failed with code," + responseHeader.CompleteCode);
                return;
            }

            int dataLength = responseHeader.ResponseDataLen - MCProtocol.JUNK_SIZE;
            if (dataLength > 0 && !ReceiveData(_bufferIn, dataLength))
            {
                return;
            }
        }

        protected override void WriteAo(int offset, short[] data)
        {
            short[] databuffer = new short[perIoLength];

            int count = data.Length / perIoLength;
            if (count < 1)
            {
                Array.Copy(data, 0, databuffer, 0, data.Length);
                Array.Resize(ref databuffer, data.Length);

                DoWriteAo(offset, databuffer.Select(x => (ushort)x).ToArray());
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    Array.Copy(data, i * perIoLength, databuffer, 0, databuffer.Length);
                    DoWriteAo(offset + perIoLength * i, databuffer.Select(x => (ushort)x).ToArray());
                }

                if(data.Length % perIoLength != 0)
                {
                    Array.Copy(data, perIoLength * count, databuffer, 0, data.Length % perIoLength);
                    Array.Resize(ref databuffer, data.Length % perIoLength);
                    DoWriteAo(offset + perIoLength * count, databuffer.Select(x => (ushort)x).ToArray());
                }
            }
        }

        //protected override void WriteAo(int offset, short[] data)
        //{
        //    ushort[] uData = new ushort[data.Length];
        //    uData = data.Select(x => (ushort)x).ToArray();
        //    WriteAo(offset, uData);
        //}
        protected void DoWriteAo(int offset, ushort[] data)
        {
            _batchCommand.Command = MCProtocol.MC_COMMAND_BATCH_WRITE;
            _batchCommand.SubCommand = MCProtocol.MC_SUBCOMMAND_WORD_UNITS;

            _batchCommand.HeadAddr = (ushort)(offset & 0xFFFF);
            _batchCommand.Reserved = (byte)(offset >> 16);

            _batchCommand.DevicePoints = (ushort)data.Length;

            _header.RequestDataLen = (ushort)(MCProtocol.MC_BATCH_COMMAND_SIZE + MCProtocol.JUNK_SIZE + MCProtocol.ShortSize * data.Length);

            byte[] header = MCProtocol.Struct2Bytes(_header);
            byte[] command = MCProtocol.Struct2Bytes(_batchCommand);
            byte[] byteData = MCProtocol.Ushort2Byte(data);

            Array.Copy(header, 0, _bufferOut, 0, header.Length);
            Array.Copy(command, 0, _bufferOut, MCProtocol.MC_QHEADER_COMMAND_SIZE, command.Length);
            Array.Copy(byteData, 0, _bufferOut, MCProtocol.MC_QHEADER_COMMAND_SIZE + MCProtocol.MC_BATCH_COMMAND_SIZE, byteData.Length);

            if (!WriteData(_bufferOut, MCProtocol.MC_QHEADER_COMMAND_SIZE + MCProtocol.MC_BATCH_COMMAND_SIZE + MCProtocol.ShortSize * data.Length))
            {
                return;
            }

            if (!ReceiveData(_bufferIn, MCProtocol.MC_QHEADER_RESPONSE_SIZE))
            {
                return;
            }

            MCProtocol.MC_RESPONSE_HEADER responseHeader =
                MCProtocol.ToStruct<MCProtocol.MC_RESPONSE_HEADER>(_bufferIn);

            if (responseHeader.CompleteCode != MCProtocol.MC_COMPLETE_CODE_SUCCESS)
            {
                LOG.Write("Write PLC failed with code," + responseHeader.CompleteCode);
                return;
            }

            int dataLength = responseHeader.ResponseDataLen - MCProtocol.JUNK_SIZE;
            if (dataLength > 0 && !ReceiveData(_bufferIn, dataLength))
            {
                return;
            }
        }

        private bool WriteData(byte[] data, int length)
        {
            return _socket.Write(data, length);
        }

        private bool ReceiveData(byte[] data, int length)
        {
            return _socket.Read(data, length);
        }
    }
}
