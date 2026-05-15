using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Communications;
using MECF.Framework.RT.Core.IoProviders;
using MECF.Framework.RT.Core.IoProviders.Mitsubishis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MECF.Framework.RT.Core.IoProviders.Melsec;
using MECF.Framework.RT.Core.IoProviders.Common;

namespace MECF.Framework.RT.Core.IoProviders.Mitsubishis
{
    public class HSLMCProtocolPlc : IoProvider, IConnection
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
        //private string _localIp = "127.0.0.1";
        private int _port = 6731;
        private int _socketId = 101;
        private int _stationId = 102;

        private byte[] _bufferIn;
        private byte[] _bufferOut;

        private MelsecMcNet melsec_net = null;

        //private int demandAiFrom;  //should be the same the offset in lstBuffers
        //private int demandAiSize;
        //private int demandAoFrom;  //should be the same the offset in lstBuffers
        //private int demandAoSize;
        //private bool inDemanding = false;

        private string aiStoragename = "D";
        private string aoStoragename = "D";
        private string diStoragename = "B";
        private string doStoragename = "B";
        private ushort perIoLength = 960;

        private Stopwatch stopwatchDi = new Stopwatch();
        private Stopwatch stopwatchDo = new Stopwatch();
        private Stopwatch stopwatchAi = new Stopwatch();
        private Stopwatch stopwatchAo = new Stopwatch();
        private int comunicationSpanDi;
        private int comunicationSpanDo;
        private int comunicationSpanAi;
        private int comunicationSpanAo;
        protected int comunicationSpanTotal;
        private Stopwatch stopwatchTotal = new Stopwatch();

        private int diStartAddress;
        private int doStartAddress;
        private int aiStartAddress;
        private int aoStartAddress;

        //private List<IoBlockItem> _blockSectionsDemand;

        private int plcCollectionInterval;

        private int connectionNumber;

        private bool isWriteRecipe;
        private int recipeOffset;
        private short[] recipeData = new short[50 * 15];

        public override void Initialize(string module, string name, List<IoBlockItem> lstBuffers, IIoBuffer buffer, XmlElement nodeParameter, string ioMappingPathFile, string ioModule)
        {
            Module = module;
            Name = name;

            _source = module + "." + name;

            _buffer = buffer;

            _nodeParameter = nodeParameter;

            _blockSections = lstBuffers;

            buffer.SetBufferBlock(_source, lstBuffers);
            buffer.SetIoMapByModule(_source, 0, ioMappingPathFile, ioModule);

            SetParameter(nodeParameter);

            State = IoProviderStateEnum.Uninitialized;

            plcCollectionInterval = SC.ContainsItem("System.PlcCollectionInterval") ? SC.GetValue<int>("System.PlcCollectionInterval") : 50;

            _thread = new PeriodicJob(plcCollectionInterval, OnTimer, name);

            ConnectionManager.Instance.Subscribe(Name, this);
            DATA.Subscribe($"{Module}.{Name}.IsConnected", () => melsec_net == null ? false : melsec_net.IsConnected);

            OP.Subscribe($"{Name}.Reconnect", (string cmd, object[] args) =>
            {
                Close();
                Open();
                return true;
            });
        }
        
        protected override bool OnTimer()
        {
            if (State == IoProviderStateEnum.Uninitialized)
            {
                SetState(IoProviderStateEnum.Opening);
                Open();
            }

            if (State == IoProviderStateEnum.Opened)
            {
                try
                {
                    stopwatchTotal.Start();
                    foreach (var bufferSection in _blockSections)
                    {
                        if (bufferSection.Type == IoType.DI)
                        {
                            stopwatchDi.Start();
                            bool[] diBuffer = ReadDi(bufferSection.Offset, bufferSection.Size);
                            if (diBuffer != null)
                            {
                                _buffer.SetDiBuffer(_source, bufferSection.Offset, diBuffer);

                                //TraceArray(diBuffer);
                            }
                            stopwatchDi.Stop();
                        }
                        else if (bufferSection.Type == IoType.AI)
                        {
                            stopwatchAi.Start();
                            short[] aiBuffer = ReadAi(bufferSection.Offset, bufferSection.Size);
                            if (aiBuffer != null)
                            {
                                _buffer.SetAiBuffer(_source, bufferSection.Offset, aiBuffer);
                            }

                            stopwatchAi.Stop();
                        }
                        //else if (bufferSection.Type == IoType.DO)
                        //{
                        //    bool[] doBuffer = ReadDo(bufferSection.Offset, bufferSection.Size);
                        //    if (doBuffer != null)
                        //    {
                        //        _buffer.SetDoBuffer(_source, bufferSection.Offset, doBuffer);
                        //    }
                        //}
                        //else if (bufferSection.Type == IoType.AO)
                        //{
                        //    short[] aoBuffer = ReadAo(bufferSection.Offset, bufferSection.Size);
                        //    if (aoBuffer != null)
                        //    {
                        //        _buffer.SetAoBuffer(_source, bufferSection.Offset, aoBuffer);
                        //        if (bufferSection.AIOType == typeof(float))
                        //        {
                        //            _isAIOFloatType = true;
                        //            _buffer.SetAoBufferFloat(_source, bufferSection.Offset, Array.ConvertAll(aoBuffer, x => (float)x).ToArray());
                        //        }
                        //    }
                        //}
                    }

                    comunicationSpanDi = (int)stopwatchDi.ElapsedMilliseconds;
                    stopwatchDi.Reset();

                    comunicationSpanAi = (int)stopwatchAi.ElapsedMilliseconds;
                    stopwatchAi.Reset();

                    stopwatchAo.Start();
                    Dictionary<int, short[]> aos = _buffer.GetAoBuffer(_source);
                    if (aos != null)
                    {
                        foreach (var ao in aos)
                        {
                            WriteAo(aoStartAddress, ao.Value);
                        }
                    }
                    stopwatchAo.Stop();
                    comunicationSpanAo = (int)stopwatchAo.ElapsedMilliseconds;
                    stopwatchAo.Reset();

                    stopwatchDo.Start();
                    Dictionary<int, bool[]> dos = _buffer.GetDoBuffer(_source);
                    if (dos != null)
                    {
                        foreach (var doo in dos)
                        {
                            WriteDo(doStartAddress, doo.Value);
                        }
                    }

                    stopwatchDo.Stop();
                    comunicationSpanDo = (int)stopwatchDo.ElapsedMilliseconds;
                    stopwatchDo.Reset();

                    if (isWriteRecipe)
                    {
                        isWriteRecipe = false;
                        WriteRecipe(recipeOffset, recipeData);
                    }

                    stopwatchTotal.Stop();
                    comunicationSpanTotal = (int)stopwatchTotal.ElapsedMilliseconds;
                    stopwatchTotal.Reset();
                }
                catch (Exception ex)
                {
                    LOG.Error($"{Name} {ex}");
                    //SetState(IoProviderStateEnum.Error);
                    Thread.Sleep(1000);
                    Close();
                    Open();
                }
            }

            

            _trigError.CLK = State == IoProviderStateEnum.Error;
            if (_trigError.Q)
            {
                EV.PostAlarmLog(Module, $"{_source} PLC {_ip}:{_port} error");
            }

            _trigNotConnected.CLK = State != IoProviderStateEnum.Opened;
            if (_trigNotConnected.T)
            {
                EV.PostInfoLog(Module, $"{_source} connected");
            }

            if (_trigNotConnected.R)
            {
                EV.PostAlarmLog(Module, $"{_source} PLC {_ip}:{_port} not connected");
            }

            if (State != IoProviderStateEnum.Opened && connectionNumber < 3)
            {
                Thread.Sleep(1000);
                Close();
                Open();
                connectionNumber++;
                EV.PostInfoLog(Module, $"{_source} PLC {_ip}:{_port} reconnection {connectionNumber}");

                if (connectionNumber >=3)
                {
                    EV.PostAlarmLog(Module, $"{_source} PLC {_ip}:{_port} not connected");
                }
            }

            return true;
        }

        public override void Reset()
        {
            if (State != IoProviderStateEnum.Opened)
            {
                Close();
                Open();
                connectionNumber = 0;
            }
        }

        protected override void SetParameter(XmlElement nodeParameter)
        {
            string strIp = nodeParameter.GetAttribute("ip");
            //_localIp = nodeParameter.GetAttribute("localIp");
            string strPort = nodeParameter.GetAttribute("port");
            string networkId = nodeParameter.GetAttribute("network_id");
            string stationId = nodeParameter.GetAttribute("station_id");
            aiStoragename = nodeParameter.GetAttribute("aiStoragename");
            aoStoragename = nodeParameter.GetAttribute("aoStoragename");
            diStoragename = nodeParameter.GetAttribute("diStoragename");
            doStoragename = nodeParameter.GetAttribute("doStoragename");
            ushort.TryParse(nodeParameter.GetAttribute("perIoLength"), out perIoLength);
            perIoLength = perIoLength == 0 ? (ushort)960 : perIoLength;

            int.TryParse(nodeParameter.GetAttribute("doStartAddress"), out doStartAddress);
            int.TryParse(nodeParameter.GetAttribute("diStartAddress"), out diStartAddress);
            int.TryParse(nodeParameter.GetAttribute("aoStartAddress"), out aoStartAddress);
            int.TryParse(nodeParameter.GetAttribute("aiStartAddress"), out aiStartAddress);

            _port = int.Parse(strPort);
            _ip = strIp;
            _socketId = int.Parse(networkId);
            _stationId = int.Parse(stationId);
        }

        protected override void Open()
        {
            melsec_net = new MelsecMcNet(_ip, _port);

            _bufferOut = new byte[2048];
            _bufferIn = new byte[2048];

            melsec_net.ConnectClose();

            try
            {
                OperateResult connect = melsec_net.ConnectServer();
                if (connect.IsSuccess)
                {
                    SetState(IoProviderStateEnum.Opened);
                }
            }
            catch (Exception ex)
            {
                LOG.Error(ex.Message);
            }
        }

        protected override void Close()
        {
            melsec_net.ConnectClose();
            SetState(IoProviderStateEnum.Closed);
        }

        protected override bool[] ReadDi(int offset, int size)
        {
            bool[] buff = new bool[size];

            int count = size / perIoLength;
            if (count < 1)
            {
                bool[] dibuffer = DoReadDi(diStartAddress, (ushort)size);
                if (dibuffer != null)
                    Array.Copy(dibuffer, 0, buff, 0, dibuffer.Length);
                else
                    return null;
            }
            else
            {
                bool[] dibuffer;
                for (int i = 0; i < count; i++)
                {
                    dibuffer = DoReadDi(i * perIoLength + diStartAddress, perIoLength);
                    if (dibuffer != null)
                        Array.Copy(dibuffer, 0, buff, i * perIoLength, dibuffer.Length);
                    else
                        return null;
                }

                if (size % perIoLength != 0)
                {
                    dibuffer = DoReadDi(diStartAddress + perIoLength * count, (ushort)(size % perIoLength));
                    if (dibuffer != null)
                        Array.Copy(dibuffer, 0, buff, size - size % perIoLength, dibuffer.Length);
                    else
                        return null;
                }
            }

            return buff;
        }

        protected  bool[] ReadDo(int offset, int size)
        {
            bool[] buff = new bool[size];

            int count = size / perIoLength;
            if (count < 1)
            {
                bool[] dobuffer = DoReadDo(doStartAddress, (ushort)size);
                if (dobuffer != null)
                    Array.Copy(dobuffer, 0, buff, 0, dobuffer.Length);
                else
                    return null;
            }
            else
            {
                bool[] dobuffer;
                for (int i = 0; i < count; i++)
                {
                    dobuffer = DoReadDo(i * perIoLength + doStartAddress, perIoLength);
                    if (dobuffer != null)
                        Array.Copy(dobuffer, 0, buff, i * perIoLength, dobuffer.Length);
                    else
                        return null;
                }

                if (size % perIoLength != 0)
                {
                    dobuffer = DoReadDo(doStartAddress + perIoLength * count, (ushort)(size % perIoLength));
                    if (dobuffer != null)
                        Array.Copy(dobuffer, 0, buff, size - size % perIoLength, dobuffer.Length);
                    else
                        return null;
                }
            }

            return buff;
        }

        protected override short[] ReadAi(int offset, int size)
        {
            short[] buff = new short[size];

            int count = size / perIoLength;
            if (count < 1)
            {
                short[] aibuffer = DoReadAi(aiStartAddress, size);
                if (aibuffer != null)
                    Array.Copy(aibuffer, 0, buff, 0, aibuffer.Length);
            }
            else
            {
                short[] aibuffer;
                for (int i = 0; i < count; i++)
                {
                    aibuffer = DoReadAi(i * perIoLength + aiStartAddress, perIoLength);
                    if (aibuffer != null)
                        Array.Copy(aibuffer, 0, buff, i * perIoLength, aibuffer.Length);
                }

                if (size % perIoLength != 0)
                {
                    aibuffer = DoReadAi(aiStartAddress + perIoLength * count, (ushort)(size % perIoLength));
                    if (aibuffer != null)
                        Array.Copy(aibuffer, 0, buff, size - size % perIoLength, aibuffer.Length);
                }
            }

            return buff;
        }

        protected  short[] ReadAo(int offset, int size)
        {
            short[] buff = new short[size];

            int count = size / perIoLength;
            if (count < 1)
            {
                short[] aobuffer = DoReadAo(aoStartAddress, (ushort)size);
                if (aobuffer != null)
                    Array.Copy(aobuffer, 0, buff, 0, aobuffer.Length);
            }
            else
            {
                short[] aobuffer;
                for (int i = 0; i < count; i++)
                {
                    aobuffer = DoReadAo(i * perIoLength + aoStartAddress, perIoLength);
                    if (aobuffer != null)
                        Array.Copy(aobuffer, 0, buff, i * perIoLength, aobuffer.Length);
                }

                if (size % perIoLength != 0)
                {
                    aobuffer = DoReadAo(aoStartAddress + perIoLength * count, (ushort)(size % perIoLength));
                    if (aobuffer != null)
                        Array.Copy(aobuffer, 0, buff, size - size % perIoLength, aobuffer.Length);
                }
            }

            return buff;
        }

        protected override void WriteDo(int offset, bool[] data)
        {
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

        protected override void WriteAo(int offset, short[] data)
        {
            short[] databuffer = new short[perIoLength];

            int count = data.Length / perIoLength;
            if (count < 1)
            {
                Array.Copy(data, 0, databuffer, 0, data.Length);
                Array.Resize(ref databuffer, data.Length);

                DoWriteAo(offset, databuffer);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    Array.Copy(data, i * perIoLength, databuffer, 0, databuffer.Length);
                    DoWriteAo(offset + perIoLength * i, databuffer, data.Length, i);
                }

                if(data.Length % perIoLength != 0)
                {
                    Array.Copy(data, perIoLength * count, databuffer, 0, data.Length % perIoLength);
                    Array.Resize(ref databuffer, data.Length % perIoLength);
                    DoWriteAo(offset + perIoLength * count, databuffer, data.Length);
                }
               
            }
        }

        public void RecipeData(int offset, short[] data)
        {
            recipeOffset = offset;
            recipeData = data;
            isWriteRecipe = true;
        }

        public void WriteRecipe(int offset, short[] data)
        {
            short[] databuffer = new short[perIoLength];

            int count = data.Length / perIoLength;
            if (count < 1)
            {
                Array.Copy(data, 0, databuffer, 0, data.Length);
                Array.Resize(ref databuffer, data.Length);

                DoWriteAo(offset, databuffer);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    Array.Copy(data, i * perIoLength, databuffer, 0, databuffer.Length);
                    DoWriteAo(offset + perIoLength * i, databuffer, data.Length, i);
                }

                if (data.Length % perIoLength != 0)
                {
                    Array.Copy(data, perIoLength * count, databuffer, 0, data.Length % perIoLength);
                    Array.Resize(ref databuffer, data.Length % perIoLength);
                    DoWriteAo(offset + perIoLength * count, databuffer, data.Length);
                }

            }
        }

        private short[] DoReadAi(int offset, int size)
        {
           OperateResult<short[]> result = melsec_net.ReadInt16($"{aiStoragename}{offset}",(ushort)size);
           if (!result.IsSuccess)
           {
               LOG.Error(result.Message);
               SetState(IoProviderStateEnum.Error);
                return null;
           }
           return result.Content;
        }

        private short[] DoReadAo(int offset, ushort size)
        {
            OperateResult<short[]> result = melsec_net.ReadInt16($"{aoStoragename}{offset}", size);
            if (!result.IsSuccess)
            {
                LOG.Error(result.Message);
                SetState(IoProviderStateEnum.Error);
                return null;
            }
            return result.Content;
        }

        private bool DoWriteAo(int offset, short[] data, int total = 0, int count = 100)
        {
            OperateResult result = melsec_net.Write($"{aoStoragename}{offset}", data);
            if (!result.IsSuccess)
            {
                LOG.Error(result.Message);
                SetState(IoProviderStateEnum.Error);
            }
            return result.IsSuccess;
        }

        private bool[] DoReadDi(int offset, ushort size)
        {
            OperateResult<bool[]> result = melsec_net.ReadBool($"{diStoragename}{offset.ToString("X")}", size);
            if (!result.IsSuccess)
            {
                LOG.Error(result.Message);
                SetState(IoProviderStateEnum.Error);
                return null;
            }
            return result.Content;
        }

        private bool[] DoReadDo(int offset, ushort size)
        {
            OperateResult<bool[]> result = melsec_net.ReadBool($"{doStoragename}{offset.ToString("X")}", size);
            if (!result.IsSuccess)
            {
                LOG.Error(result.Message);
                SetState(IoProviderStateEnum.Error);
                return null;
            }
            return result.Content;
        }

        private bool DoWriteDo(int offset, bool[] data)
        {
            OperateResult result = melsec_net.Write($"{doStoragename}{offset.ToString("X")}", data);
            if (!result.IsSuccess)
            {
                LOG.Error(result.Message);
                SetState(IoProviderStateEnum.Error);
            }
            return result.IsSuccess;
        }

        public override bool SetValue(AOAccessor aoItem, short value)
        {
            if (State != IoProviderStateEnum.Opened)
                return false;

            //WriteAo(aoStartAddress + aoItem.Index, new short[] { value });

            return true;
        }

        public override bool SetValueFloat(AOAccessor aoItem, float value)
        {
            if (State != IoProviderStateEnum.Opened)
                return false;

            return true;
        }

        public override bool SetValue(DOAccessor doItem, bool value)
        {
            //if (State != IoProviderStateEnum.Opened)
            //    return false;

            //if (!IO.CanSetDO(doItem.Name, value, out string reason))
            //{
            //    LOG.Error(reason);
            //    return false;
            //}

            ////foreach (var bufferSection in _blockSections)
            ////{
            ////    if (bufferSection.Type == IoType.DO)
            ////    {
            ////        bool[] doBuffer = ReadDo(bufferSection.Offset, bufferSection.Size);
            ////        if (doBuffer != null)
            ////        {
            ////            _buffer.SetDoBuffer(_source, bufferSection.Offset, doBuffer);
            ////        }
            ////    } 
            ////}

            //Dictionary<int, bool[]> dos = _buffer.GetDoBuffer(_source);
            //if (dos != null)
            //{
            //    foreach (var doo in dos)
            //    {
            //        //doo.Value[doItem.Index] = value;
            //        //WriteDo(doStartAddress, doo.Value);

            //        WriteDo(doStartAddress + doItem.Index, new bool[] { value });
            //    }
            //}

            return true;
        }
    }
    
}
