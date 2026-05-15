using System;
using System.Xml;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using CdioCs;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.Core.IoProviders;

namespace MECF.Framework.RT.Core.IoProviders
{
    public class ContecIoCard:IoProvider, IConnection
    {
        public string Address
        {
            get { return _device.Name; }
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

        private Cdio _dio = new Cdio();

        private ContecIoDevice _device;

        private int _inputsize = 0;
        private int _outputsize = 0;

        private Byte[] _inputDI;
        private Byte[] _ouputDO;

        //<Parameter name="System.dio000" in_offset="0" in_size="64" out_offset="0" out_size="64"></Parameter>
        protected override void SetParameter(XmlElement nodeParameter)
        {
            string name = nodeParameter.GetAttribute("name");
            int in_offset = int.Parse(nodeParameter.GetAttribute("in_offset"));
            int in_size = int.Parse(nodeParameter.GetAttribute("in_size"));
            int out_offset = int.Parse(nodeParameter.GetAttribute("out_offset"));
            int out_size = int.Parse(nodeParameter.GetAttribute("out_size"));

            _device =  new ContecIoDevice(_dio, name, in_offset, (short)in_size, out_offset, (short)out_size);

 
                _inputsize = Math.Max(_inputsize, _device.InputOffset + _device.InputSize);
                _outputsize = Math.Max(_inputsize, _device.OutputOffset + _device.OutputSize);
 
            _inputDI = new byte[_inputsize];
            _ouputDO = new byte[_outputsize];

            ConnectionManager.Instance.Subscribe(name, this);
        }

        protected override void Open()
        {
            if (!_device.Open())
            {
                EV.PostAlarmLog("System", "can not open io card");
                return;
            }

            SetState(IoProviderStateEnum.Opened);
        }

        protected override void Close()
        {
            try
            {
                _device.Reset();

                _device.Close();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        protected override bool[] ReadDi(int offset, int size)
        {
            try
            {
                _device.Read();
                for (short i = 0; i < _device.InputSize / 8; i++)
                {
                    for (short j = 0; j < 8; j++)
                        _inputDI[_device.InputOffset + i * 8 + j] = (byte)((_device.Input[i] >> j) & 0x01);
                }

                return Array.ConvertAll(_inputDI, x => x == 1);
            }
            catch (Exception e)
            {
                LOG.Write(e);
            }

            return null;
        }

        protected override short[] ReadAi(int offset, int size)
        {
            return null;
        }

        protected override void WriteDo(int offset, bool[] buffer)
        {
            try
            {
                _ouputDO = Array.ConvertAll(buffer, x => x ? (byte)1 : (byte)0);
                _device.Write(_ouputDO);
            }
            catch (Exception e)
            {
                LOG.Write(e);
            }

        }

        protected override void WriteAo(int offset, short[] buffer)
        {
            
        }
    }


    public class ContecIoDevice
    {
        public string Name { get; set; }

        public int InputOffset { get; set; }   // Input 起点
        public short InputSize { get; set; }    // 长度

        public int OutputOffset { get; set; }   // Outnput 起点
        public short OutputSize { get; set; }

        public byte[] Input { get { return _inputBuff; } }

        private Cdio _io = null;
        private short _id = 0;


        private short _inputSize;
        private byte[] _inputBuff = null;
        private short[] _inputPort = null;


        private short _outputSize;
        private byte[] _outputBuff = null;
        private short[] _outputPort = null;

        public ContecIoDevice(Cdio io, string name, int inoffset, short inlen, int outoffset, short outlen)
        {
            _io = io;
            Name = name;

            InputOffset = inoffset;
            InputSize = inlen;

            OutputOffset = outoffset;
            OutputSize = outlen;
        }

        public bool Open()
        {
            int ret = _io.Init(Name, out _id);
            if (ret != (int)CdioConst.DIO_ERR_SUCCESS)
            {
                string error;
                _io.GetErrorString(ret, out error);
                LOG.Error(string.Format("Init IO Card {0} failed,{1}", Name, error));
                return false;
            }

            _io.GetMaxPorts(_id, out _inputSize, out _outputSize);
            if (ret != (int)CdioConst.DIO_ERR_SUCCESS)
            {
                string error;
                _io.GetErrorString(ret, out error);
                LOG.Error(string.Format("Init IO Card {0} failed,{1}", Name, error));
                return false;
            }

            int len = InputSize / 8;
            _inputBuff = new byte[len];
            _inputPort = new short[len];
            for (short i = 1; i < len; i++)
                _inputPort[i] = i;

            len = OutputSize / 8;
            _outputBuff = new byte[len];
            _outputPort = new short[len];
            for (short i = 0; i < len; i++)
                _outputPort[i] = i;

            return true;
        }

        public bool Read()
        {
            short size = Math.Min(_inputSize, (short)(InputSize / 8));
            int ret = _io.InpMultiByte(_id, _inputPort, size, _inputBuff);
            if (ret != (int)CdioConst.DIO_ERR_SUCCESS)
            {
                string error;
                _io.GetErrorString(ret, out error);
                LOG.Error(string.Format("Read IO Card {0} failed,{1}", Name, error));
                return false;
            }
            return true;
        }

        public bool Write(byte[] buffer)
        {
            short size = Math.Min(_outputSize, (short)(OutputSize / 8));

            for (int i = 0; i < size; i++)
            {
                byte value = 0;
                for (int j = 0; j < 8; j++)
                {
                    value |= (byte)((buffer[OutputOffset + i * 8 + j] & 0x01) << j);

                }
                _outputBuff[i] = value;
            }


            int ret = _io.OutMultiByte(_id, _outputPort, size, _outputBuff);
            if (ret != (int)CdioConst.DIO_ERR_SUCCESS)
            {
                string error;
                _io.GetErrorString(ret, out error);
                LOG.Error(string.Format("Write IO Card {0} failed,{1}", Name, error));
                return false;
            }

            return true;
        }


        public bool Reset()
        {
            return _io.ResetDevice(_id) == (int)CdioConst.DIO_ERR_SUCCESS;
        }

        public bool Close()
        {
            return _io.Exit(_id) == (int)CdioConst.DIO_ERR_SUCCESS;
        }
    }

}
