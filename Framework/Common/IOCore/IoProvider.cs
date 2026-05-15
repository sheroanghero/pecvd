using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace MECF.Framework.RT.Core.IoProviders
{
    //
    public abstract class IoProvider : IIoProvider
    {
        public string Module { get; set; }
        public string Name { get; set; }

        public bool IsOpened
        {
            get { return State == IoProviderStateEnum.Opened; }
        }

        public IoProviderStateEnum State { get; set; }

 

        protected PeriodicJob _thread;

        protected IIoBuffer _buffer;

        protected R_TRIG _trigError = new R_TRIG();

        protected RD_TRIG _trigNotConnected = new RD_TRIG();

        protected string _source;

        protected XmlElement _nodeParameter;

        protected List<IoBlockItem> _blockSections;

        protected abstract void SetParameter(XmlElement nodeParameter);

        protected abstract void Open();
        protected abstract void Close();

        protected abstract bool[] ReadDi(int offset, int size);
        protected abstract short[] ReadAi(int offset, int size);

        protected abstract void WriteDo(int offset, bool[] buffer);
        protected abstract void WriteAo(int offset, short[] buffer);

        protected virtual float[] ReadAiFloat(int offset, int size)
        {
            return null;
        }

        protected virtual void WriteAoFloat(int offset, float[] buffer)
        {
        }

        /// <summary>
        /// 单腔体
        /// </summary>
        /// <param name="module"></param>
        /// <param name="name"></param>
        /// <param name="lstBuffers"></param>
        /// <param name="buffer"></param>
        /// <param name="nodeParameter"></param>
        /// <param name="ioMappingPathFile"></param>
        public virtual void Initialize(string module, string name,List<IoBlockItem> lstBuffers , IIoBuffer buffer, XmlElement nodeParameter, Dictionary<int, string> ioMappingPathFile)
        {
            Module = module;
            Name = name;

            _source = module + "." + name;

            _buffer = buffer;
 
            _nodeParameter = nodeParameter;

            _blockSections = lstBuffers;

            buffer.SetBufferBlock(_source, lstBuffers);
            buffer.SetIoMap(_source, ioMappingPathFile);

            SetParameter(nodeParameter);

            State = IoProviderStateEnum.Uninitialized;

            _thread = new PeriodicJob(50, OnTimer, name);
        }

        /// <summary>
        /// 多腔体
        /// </summary>
        /// <param name="module"></param>
        /// <param name="name"></param>
        /// <param name="lstBuffers"></param>
        /// <param name="buffer"></param>
        /// <param name="nodeParameter"></param>
        /// <param name="ioMappingPathFile"></param>
        /// <param name="ioModule"></param>
        public virtual void Initialize(string module, string name, List<IoBlockItem> lstBuffers, IIoBuffer buffer, XmlElement nodeParameter, string ioMappingPathFile, string ioModule )
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

            _thread = new PeriodicJob(50, OnTimer, name);
        }

        protected void SetState(IoProviderStateEnum newState)
        {
            State = newState;
        }

        public void TraceArray(bool[] data)
        {
            string[] values = new string[data.Length];
 
            for (int i = 0; i < data.Length; i++)
            {
                values[i++] = data[i] ? "1" : "0";
            }
            System.Diagnostics.Trace.WriteLine(string.Join(",", values));
        }

        protected virtual bool OnTimer()
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
                    bool isAIOFloatType = false;
                    foreach (var bufferSection in _blockSections)
                    {
                        if (bufferSection.Type == IoType.DI)
                        {
                            bool[] diBuffer = ReadDi(bufferSection.Offset, bufferSection.Size);
                            if (diBuffer != null)
                            {
                                _buffer.SetDiBuffer(_source, bufferSection.Offset, diBuffer);

                                //TraceArray(diBuffer);
                            }
                        }
                        else if (bufferSection.Type == IoType.AI)
                        {
                            short[] aiBuffer = ReadAi(bufferSection.Offset, bufferSection.Size);
                            if (aiBuffer != null)
                            {
                                _buffer.SetAiBuffer(_source, bufferSection.Offset, aiBuffer);
                                if (bufferSection.AIOType == typeof(float))
                                {
                                    isAIOFloatType = true;
                                    _buffer.SetAiBufferFloat(_source, bufferSection.Offset, Array.ConvertAll(aiBuffer, x => (float)x).ToArray());
                                }
                            }
                        }
                    }


                    Dictionary<int, short[]> aos = _buffer.GetAoBuffer(_source);
                    if (aos != null)
                    {
                        foreach (var ao in aos)
                        {
                            WriteAo(ao.Key, ao.Value);

                            if (isAIOFloatType)
                                WriteAoFloat(ao.Key, Array.ConvertAll(ao.Value, x => (float)x).ToArray());
                        }
                    }

                    Dictionary<int, bool[]> dos = _buffer.GetDoBuffer(_source);
                    if (dos != null)
                    {
                        foreach (var doo in dos)
                        {
                            WriteDo(doo.Key, doo.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                    SetState(IoProviderStateEnum.Error);
                    Close();
                }
            }

            _trigError.CLK = State == IoProviderStateEnum.Error;
            if (_trigError.Q)
            {
                EV.PostAlarmLog(Module, $"{_source} error");
            }

            _trigNotConnected.CLK = State != IoProviderStateEnum.Opened;
            if (_trigNotConnected.T)
            {
                EV.PostInfoLog(Module, $"{_source} connected");
            }

            if (_trigNotConnected.R)
            {
                EV.PostWarningLog(Module, $"{_source} not connected");
            }

            return true;
        }

        public virtual void Reset()
        {
            _trigError.RST = true;
            _trigNotConnected.RST = true;


        }
 
        public void Start()
        {
            if(_thread != null)
                _thread.Start();
        }

        public void Stop()
        {
            SetState(IoProviderStateEnum.Closing);
            Close();

            _thread.Stop();
        }

        public virtual bool SetValue(AOAccessor aoItem, short value)
        {
            return true;
        }

        public virtual bool SetValueFloat(AOAccessor aoItem, float value)
        {
            return true;
        }

        public virtual bool SetValue(DOAccessor doItem, bool value)
        {
            return true;
        }

    }
}
