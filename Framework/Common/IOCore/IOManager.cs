using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.RT.Core.IoProviders;

namespace MECF.Framework.Common.IOCore
{
    public class IoManager : Singleton<IoManager>, IIoBuffer
    {
        private Dictionary<string, DIAccessor> _diMap = new Dictionary<string, DIAccessor>();
        private Dictionary<string, DOAccessor> _doMap = new Dictionary<string, DOAccessor>();
        private Dictionary<string, AIAccessor> _aiMap = new Dictionary<string, AIAccessor>();
        private Dictionary<string, AOAccessor> _aoMap = new Dictionary<string, AOAccessor>();

        private Dictionary<string, Dictionary<int, bool[]>> _diBuffer = new Dictionary<string, Dictionary<int, bool[]>>();
        private Dictionary<string, Dictionary<int, bool[]>> _doBuffer = new Dictionary<string, Dictionary<int, bool[]>>();
        private Dictionary<string, Dictionary<int, short[]>> _aiBufferShort = new Dictionary<string, Dictionary<int, short[]>>();
        private Dictionary<string, Dictionary<int, short[]>> _aoBufferShort = new Dictionary<string, Dictionary<int, short[]>>();
        private Dictionary<string, Dictionary<int, float[]>> _aiBufferFloat = new Dictionary<string, Dictionary<int, float[]>>();
        private Dictionary<string, Dictionary<int, float[]>> _aoBufferFloat = new Dictionary<string, Dictionary<int, float[]>>();


        private Dictionary<string, Dictionary<int, Type>> _aiBufferType = new Dictionary<string, Dictionary<int, Type>>();
        private Dictionary<string, Dictionary<int, Type>> _aoBufferType = new Dictionary<string, Dictionary<int, Type>>();


        private Dictionary<string, List<DIAccessor>> _diList = new Dictionary<string, List<DIAccessor>>();
        private Dictionary<string, List<DOAccessor>> _doList = new Dictionary<string, List<DOAccessor>>();
        private Dictionary<string, List<AIAccessor>> _aiList = new Dictionary<string, List<AIAccessor>>();
        private Dictionary<string, List<AOAccessor>> _aoList = new Dictionary<string, List<AOAccessor>>();

        private Dictionary<string, List<NotifiableIoItem>> _ioItemList = new Dictionary<string, List<NotifiableIoItem>>();

        private PeriodicJob _monitorThread;

        /// <summary>
        /// 只有互锁用到
        /// </summary>
        /// <param name="interlockConfigFile"></param>
        public void Initialize(string interlockConfigFile,string module)
        {
            string reason = string.Empty;
            if (!InterlockManager.Instance.Initialize(interlockConfigFile, module, _doMap, _diMap, out reason))
            {
                throw new Exception(string.Format("interlock define file found error: \r\n {0}", reason));
            }

            _monitorThread = new PeriodicJob(200, OnTimer, "IO Monitor Thread", true);
        }
        private bool OnTimer()
        {
            try
            {
                InterlockManager.Instance.Monitor();
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }
        internal List<Tuple<int, int, string>> GetIONameList(string group, IOType ioType)
        {
            return null;
        }

        public bool CanSetDo(string doName, bool onOff, out string reason)
        {
            return InterlockManager.Instance.CanSetDo(doName, onOff, out reason);
        }

        public List<DIAccessor> GetDIList(string source)
        {
            return _diList.ContainsKey(source) ? _diList[source] : null;
        }

        public List<DOAccessor> GetDOList(string source)
        {
            return _doList.ContainsKey(source) ? _doList[source] : null;
        }

        public List<AIAccessor> GetAIList(string source)
        {
            return _aiList.ContainsKey(source) ? _aiList[source] : null;
        }

        public List<AOAccessor> GetAOList(string source)
        {
            return _aoList.ContainsKey(source) ? _aoList[source] : null;
        }

        public IoManager()
        {
            OP.Subscribe("System.SetDoValue", InvokeSetDo);
            OP.Subscribe("System.SetAoValue", InvokeSetAo);
            OP.Subscribe("System.SetAoValueFloat", InvokeSetAoFloat);


            OP.Subscribe("System.SetDoValueWithPrivoder", InvokeSetDoWithPrivoder);
            OP.Subscribe("System.SetAoValueWithPrivoder", InvokeSetAoWithPrivoder);

            OP.Subscribe("System.SetAiBuffer", InvokeSetAiBuffer);
            OP.Subscribe("System.SetDiBuffer", InvokeSetDiBuffer);

            
        }

        private bool InvokeSetDo(string arg1, object[] args)
        {
            string name = (string)args[0];
            bool setpoint = (bool)args[1];

            string reason;
            if (!CanSetDo(name, setpoint, out reason))
            {
                EV.PostWarningLog("System", $"Can not set DO {name} to {setpoint}, {reason}");
                return false;
            }

            DOAccessor do1 = GetIO<DOAccessor>(name);
            if (do1 == null)
            {
                EV.PostWarningLog("System", $"Can not set DO {name} to {setpoint}, not defined do");
                return false;
            }

            if (!do1.SetValue(setpoint, out reason))
            {
                EV.PostWarningLog("System", $"Can not set DO {name} to {setpoint}, {reason}");
                return false;
            }

            EV.PostInfoLog("System", $"Change DO {name} to {setpoint}");


            return true;
        }

        private bool InvokeSetAo(string arg1, object[] args)
        {
            string name = (string)args[0];
            short setpoint = (short)args[1];

            AOAccessor io = GetIO<AOAccessor>(name);
            if (io == null)
            {
                EV.PostWarningLog("System", $"Can not set AO {name} to {setpoint}, not defined do");
                return false;
            }

            io.Value = setpoint;

            EV.PostInfoLog("System", $"Change AO {name} to {setpoint}");


            return true;
        }

        private bool InvokeSetAoFloat(string arg1, object[] args)
        {
            string name = (string)args[0];
            float setpoint = (float)args[1];

            AOAccessor io = GetIO<AOAccessor>(name);
            if (io == null)
            {
                EV.PostWarningLog("System", $"Can not set AO {name} to {setpoint}, not defined");
                return false;
            }

            io.FloatValue = setpoint;

            EV.PostInfoLog("System", $"Change AO {name} to {setpoint}");

            return true;
        }

        private bool InvokeSetDoWithPrivoder(string arg1, object[] args)
        {
            string provider = (string)args[0];
            int offset = (int)args[1];
            string name = (string)args[2];
            bool setpoint = (bool)args[3];

            string reason;
            if (!CanSetDo(name, setpoint, out reason))
            {
                EV.PostWarningLog("System", $"Can not set DO {provider}.{name} to {setpoint}, {reason}");
                return false;
            }

            var doList = GetDOList(provider);
            if (doList == null)
            {
                EV.PostWarningLog("System", $"Can not set DO {provider}.{name} to {setpoint}, {reason}");
                return false;
            }
            var doAccessor = doList.FirstOrDefault(x => x.Name == name);
            if (doAccessor == default(DOAccessor))
            {
                EV.PostWarningLog("System", $"Can not set DO {provider}.{name} to {setpoint}, {reason}");
                return false;
            }

            if (!doAccessor.SetValue(setpoint, out reason))
            {
                EV.PostWarningLog("System", $"Can not set DO {provider}.{name} to {setpoint}, {reason}");
                return false;
            }

            EV.PostInfoLog("System", $"Change DO {provider}.{name} to {setpoint}");

            return true;
        }

        private bool InvokeSetAiBuffer(string arg1, object[] args)
        {
            string provider = (string)args[0];
            int offset = (int)args[1];
            short[] buffer = (short[])args[2];

            SetAiBuffer(provider, offset, buffer);

            return true;
        }

        private bool InvokeSetDiBuffer(string arg1, object[] args)
        {
            string provider = (string)args[0];
            int offset = (int)args[1];
            bool[] buffer = (bool[])args[2];

            SetDiBuffer(provider, offset, buffer);

            return true;
        }


        private bool InvokeSetAoWithPrivoder(string arg1, object[] args)
        {
            string provider = (string)args[0];
            int offset = (int)args[1];
            string name = (string)args[2];
            float setpoint = (float)args[3];

            string reason = "";
            var aoList = GetAOList(provider);
            if (aoList == null)
            {
                EV.PostWarningLog("System", $"Can not set AO {provider}.{name} to {setpoint}, {reason}");
                return false;
            }
            var aoAccessor = aoList.FirstOrDefault(x => x.Name == name);
            if (aoAccessor == default(AOAccessor))
            {
                EV.PostWarningLog("System", $"Can not set AO {provider}.{name} to {setpoint}, {reason}");
                return false;
            }

            aoAccessor.Value = (short)setpoint;

            EV.PostInfoLog("System", $"Change DO {provider}.{name} to {setpoint}");

            return true;
        }




        public T GetIO<T>(string name) where T : class
        {
            if (typeof(T) == typeof(DIAccessor) && _diMap.ContainsKey(name))
            {
                return _diMap[name] as T;
            }

            if (typeof(T) == typeof(DOAccessor) && _doMap.ContainsKey(name))
            {
                return _doMap[name] as T;
            }

            if (typeof(T) == typeof(AIAccessor) && _aiMap.ContainsKey(name))
            {
                return _aiMap[name] as T;
            }

            if (typeof(T) == typeof(AOAccessor) && _aoMap.ContainsKey(name))
            {
                return _aoMap[name] as T;
            }

            return null;
        }

        public Dictionary<int, bool[]> GetDiBuffer(string source)
        {
            if (_diBuffer.ContainsKey(source))
            {
                return _diBuffer[source];
            }

            return null;
        }

        public Dictionary<int, bool[]> GetDoBuffer(string source)
        {
            if (_doBuffer.ContainsKey(source))
            {
                return _doBuffer[source];
            }

            return null;
        }

        public Dictionary<int, short[]> GetAiBuffer(string source)
        {
            if (_aiBufferShort.ContainsKey(source))
            {
                return _aiBufferShort[source];
            }

            return null;
        }

        public Dictionary<int, float[]> GetAoBufferFloat(string source)
        {
            if (_aoBufferFloat.ContainsKey(source))
            {
                return _aoBufferFloat[source];
            }

            return null;
        }

        public Dictionary<int, float[]> GetAiBufferFloat(string source)
        {
            if (_aiBufferFloat.ContainsKey(source))
            {
                return _aiBufferFloat[source];
            }

            return null;
        }

        public Dictionary<int, short[]> GetAoBuffer(string source)
        {
            if (_aoBufferShort.ContainsKey(source))
            {
                return _aoBufferShort[source];
            }

            return null;
        }

        public void SetDiBuffer(string source, int offset, bool[] buffer)
        {
            if (_diBuffer.ContainsKey(source) && _diBuffer[source].ContainsKey(offset))
            {
                for (int i = 0; i < buffer.Length && i < _diBuffer[source][offset].Length; i++)
                {
                    _diBuffer[source][offset][i] = buffer[i];
                }
            }
        }

        public void SetAiBuffer(string source, int offset, short[] buffer, int skipSize = 0)
        {
            if (_aiBufferShort.ContainsKey(source) && _aiBufferShort[source].ContainsKey(offset))
            {
                for (int i = 0; i < buffer.Length && i < _aiBufferShort[source][offset].Length; i++)
                {
                    _aiBufferShort[source][offset][i + skipSize] = buffer[i];
                }
            }
        }

        public void SetAiBufferFloat(string source, int offset, float[] buffer, int skipSize = 0)
        {
            if (_aiBufferFloat.ContainsKey(source) && _aiBufferFloat[source].ContainsKey(offset))
            {
                for (int i = 0; i < buffer.Length && i < _aiBufferFloat[source][offset].Length; i++)
                {
                    _aiBufferFloat[source][offset][i + skipSize] = buffer[i];
                }
            }
        }

        public void SetDoBuffer(string source, int offset, bool[] buffer)
        {
            if (_doBuffer.ContainsKey(source) && _doBuffer[source].ContainsKey(offset))
            {
                for (int i = 0; i < buffer.Length && i < _doBuffer[source][offset].Length; i++)
                {
                    _doBuffer[source][offset][i] = buffer[i];
                }
            }
        }

        public void SetAoBuffer(string source, int offset, short[] buffer)
        {
            if (_aoBufferShort.ContainsKey(source) && _aoBufferShort[source].ContainsKey(offset))
            {
                for (int i = 0; i < buffer.Length && i < _aoBufferShort[source][offset].Length; i++)
                {
                    _aoBufferShort[source][offset][i] = buffer[i];
                }
            }
        }

        public void SetAoBufferFloat(string source, int offset, float[] buffer, int bufferStartIndex = 0)
        {
            if (_aoBufferFloat.ContainsKey(source) && _aoBufferFloat[source].ContainsKey(offset))
            {
                for (int i = 0; i < buffer.Length && i < _aoBufferFloat[source][offset].Length; i++)
                {
                    _aoBufferFloat[source][offset][i] = buffer[i];
                }
            }
        }
        //spin recipe set
        public void SetAoBuffer(string source, int offset, short[] buffer, int bufferStartIndex)
        {
            if (_aoBufferShort.ContainsKey(source) && _aoBufferShort[source].ContainsKey(offset) && _aoBufferShort[source][offset].Length > bufferStartIndex)
            {
                for (int i = 0; i < buffer.Length && bufferStartIndex + i < _aoBufferShort[source][offset].Length; i++)
                {
                    _aoBufferShort[source][offset][bufferStartIndex + i] = buffer[i];
                }
            }
        }

        public void SetBufferBlock(string provider, List<IoBlockItem> lstBlocks)
        {
            foreach (var ioBlockItem in lstBlocks)
            {
                switch (ioBlockItem.Type)
                {
                    case IoType.AI:
                        if (!_aiBufferShort.ContainsKey(provider))
                        {
                            _aiBufferShort[provider] = new Dictionary<int, short[]>();
                            _aiBufferFloat[provider] = new Dictionary<int, float[]>();
                            _aiBufferType[provider] = new Dictionary<int, Type>();
                        }

                        if (!_aiBufferShort[provider].ContainsKey(ioBlockItem.Offset))
                        {
                            _aiBufferShort[provider][ioBlockItem.Offset] = new short[ioBlockItem.Size];
                            _aiBufferFloat[provider][ioBlockItem.Offset] = new float[ioBlockItem.Size];

                            _aiBufferType[provider][ioBlockItem.Offset] = ioBlockItem.AIOType;
                        }
                        break;
                    case IoType.AO:
                        if (!_aoBufferShort.ContainsKey(provider))
                        {
                            _aoBufferShort[provider] = new Dictionary<int, short[]>();
                            _aoBufferFloat[provider] = new Dictionary<int, float[]>();
                            _aoBufferType[provider] = new Dictionary<int, Type>();
                        }

                        if (!_aoBufferShort[provider].ContainsKey(ioBlockItem.Offset))
                        {
                            _aoBufferShort[provider][ioBlockItem.Offset] = new short[ioBlockItem.Size];
                            _aoBufferFloat[provider][ioBlockItem.Offset] = new float[ioBlockItem.Size];

                            _aoBufferType[provider][ioBlockItem.Offset] = ioBlockItem.AIOType;
                        }
                        break;
                    case IoType.DI:
                        if (!_diBuffer.ContainsKey(provider))
                        {
                            _diBuffer[provider] = new Dictionary<int, bool[]>();
                        }

                        if (!_diBuffer[provider].ContainsKey(ioBlockItem.Offset))
                        {
                            _diBuffer[provider][ioBlockItem.Offset] = new bool[ioBlockItem.Size];
                        }
                        break;
                    case IoType.DO:
                        if (!_doBuffer.ContainsKey(provider))
                        {
                            _doBuffer[provider] = new Dictionary<int, bool[]>();
                        }

                        if (!_doBuffer[provider].ContainsKey(ioBlockItem.Offset))
                        {
                            _doBuffer[provider][ioBlockItem.Offset] = new bool[ioBlockItem.Size];
                        }
                        break;
                }
            }
        }


        List<NotifiableIoItem> SubscribeDiData()
        {
            List<NotifiableIoItem> result = new List<NotifiableIoItem>();
            foreach (var ioDefine in _diMap)
            {
                NotifiableIoItem di = new NotifiableIoItem()
                {
                    Address = ioDefine.Value.Addr,
                    Name = ioDefine.Value.Name,
                    Description = ioDefine.Value.Description,
                    Index = ioDefine.Value.Index,
                    BoolValue = ioDefine.Value.Value,
                    Provider = ioDefine.Value.Provider,
                    BlockOffset = ioDefine.Value.BlockOffset,
                    BlockIndex = ioDefine.Value.Index,
                };
                result.Add(di);
            }

            return result;
        }

        List<NotifiableIoItem> SubscribeDoData()
        {
            List<NotifiableIoItem> result = new List<NotifiableIoItem>();
            foreach (var ioDefine in _doMap)
            {
                NotifiableIoItem io = new NotifiableIoItem()
                {
                    Address = ioDefine.Value.Addr,
                    Name = ioDefine.Value.Name,
                    Description = ioDefine.Value.Description,
                    Index = ioDefine.Value.Index,
                    BoolValue = ioDefine.Value.Value,
                    Provider = ioDefine.Value.Provider,
                    BlockOffset = ioDefine.Value.BlockOffset,
                    BlockIndex = ioDefine.Value.Index,

                };
                result.Add(io);
            }

            return result;
        }
        List<NotifiableIoItem> SubscribeAiData()
        {
            List<NotifiableIoItem> result = new List<NotifiableIoItem>();
            foreach (var ioDefine in _aiMap)
            {
                NotifiableIoItem di = new NotifiableIoItem()
                {
                    Address = ioDefine.Value.Addr,
                    Name = ioDefine.Value.Name,
                    Description = ioDefine.Value.Description,
                    Index = ioDefine.Value.Index,
                    ShortValue = ioDefine.Value.Value,
                    FloatValue = ioDefine.Value.FloatValue,
                    Provider = ioDefine.Value.Provider,
                    BlockOffset = ioDefine.Value.BlockOffset,
                    BlockIndex = ioDefine.Value.Index,
                };
                result.Add(di);
            }

            return result;
        }

        List<NotifiableIoItem> SubscribeAoData()
        {
            List<NotifiableIoItem> result = new List<NotifiableIoItem>();
            foreach (var ioDefine in _aoMap)
            {
                NotifiableIoItem ao = new NotifiableIoItem()
                {
                    Address = ioDefine.Value.Addr,
                    Name = ioDefine.Value.Name,
                    Description = ioDefine.Value.Description,
                    Index = ioDefine.Value.Index,
                    ShortValue = ioDefine.Value.Value,
                    FloatValue = ioDefine.Value.FloatValue,
                    Provider = ioDefine.Value.Provider,
                    BlockOffset = ioDefine.Value.BlockOffset,
                    BlockIndex = ioDefine.Value.Index,
                };
                result.Add(ao);
            }

            return result;
        }

        public void SetIoMap(string provider, int blockOffset, List<DIAccessor> ioList)
        {
            SubscribeIoItemList(provider);
            var scConfig = SC.GetConfigItem("System.IsIgnoreSaveDB");
            var isIgnoreSaveDB = scConfig != null && scConfig.BoolValue;
            foreach (var accessor in ioList)
            {
                accessor.Provider = provider;
                accessor.BlockOffset = blockOffset;
                _diMap[accessor.Name] = accessor;
                if (!_diList.ContainsKey(provider))
                    _diList[provider] = new List<DIAccessor>();
                _diList[provider].Add(accessor);
                _ioItemList[$"{provider}.DIItemList"].Add(new NotifiableIoItem()
                {
                    Address = accessor.Addr,
                    Name = accessor.Name,
                    Description = accessor.Description,
                    Index = accessor.Index,
                    Provider = provider,
                    BlockOffset = blockOffset,
                    BlockIndex = accessor.BlockOffset,
                });

                if (!isIgnoreSaveDB)
                    DATA.Subscribe($"IO.{accessor.Name}", () => accessor.Value);
            }
        }

        public void SetIoMap(string provider, int blockOffset, List<DOAccessor> ioList)
        {
            SubscribeIoItemList(provider);
            var scConfig = SC.GetConfigItem("System.IsIgnoreSaveDB");
            var isIgnoreSaveDB = scConfig != null && scConfig.BoolValue;
            foreach (var accessor in ioList)
            {
                accessor.Provider = provider;
                accessor.BlockOffset = blockOffset;
                _doMap[accessor.Name] = accessor;
                if (!_doList.ContainsKey(provider))
                    _doList[provider] = new List<DOAccessor>();
                _doList[provider].Add(accessor);
                _ioItemList[$"{provider}.DOItemList"].Add(new NotifiableIoItem()
                {
                    Address = accessor.Addr,
                    Name = accessor.Name,
                    Description = accessor.Description,
                    Index = accessor.Index,
                    Provider = provider,
                    BlockOffset = blockOffset,
                    BlockIndex = accessor.BlockOffset,
                });

                if (!isIgnoreSaveDB)
                    DATA.Subscribe($"IO.{accessor.Name}", () => accessor.Value);
            }
        }

        public void SetIoMap(string provider, int blockOffset, List<AIAccessor> ioList)
        {
            SubscribeIoItemList(provider);
            var scConfig = SC.GetConfigItem("System.IsIgnoreSaveDB");
            var isIgnoreSaveDB = scConfig != null && scConfig.BoolValue;
            foreach (var accessor in ioList)
            {
                accessor.Provider = provider;
                accessor.BlockOffset = blockOffset;
                _aiMap[accessor.Name] = accessor;
                if (!_aiList.ContainsKey(provider))
                    _aiList[provider] = new List<AIAccessor>();
                _aiList[provider].Add(accessor);
                _ioItemList[$"{provider}.AIItemList"].Add(new NotifiableIoItem()
                {
                    Address = accessor.Addr,
                    Name = accessor.Name,
                    Description = accessor.Description,
                    Index = accessor.Index,
                    Provider = provider,
                    BlockOffset = blockOffset,
                    BlockIndex = accessor.BlockOffset,
                });

                if (!isIgnoreSaveDB)
                    DATA.Subscribe($"IO.{accessor.Name}", () => accessor.Value);
            }
        }

        public void SetIoMap(string provider, int blockOffset, List<AOAccessor> ioList)
        {
            SubscribeIoItemList(provider);
            var scConfig = SC.GetConfigItem("System.IsIgnoreSaveDB");
            var isIgnoreSaveDB = scConfig != null && scConfig.BoolValue;
            foreach (var accessor in ioList)
            {
                accessor.Provider = provider;
                accessor.BlockOffset = blockOffset;
                _aoMap[accessor.Name] = accessor;
                if (!_aoList.ContainsKey(provider))
                    _aoList[provider] = new List<AOAccessor>();
                _aoList[provider].Add(accessor);
                _ioItemList[$"{provider}.AOItemList"].Add(new NotifiableIoItem()
                {
                    Address = accessor.Addr,
                    Name = accessor.Name,
                    Description = accessor.Description,
                    Index = accessor.Index,
                    Provider = provider,
                    BlockOffset = blockOffset,
                    BlockIndex = accessor.BlockOffset,
                });

                if (!isIgnoreSaveDB)
                    DATA.Subscribe($"IO.{accessor.Name}", () => accessor.Value);
            }
        }

        public void SetIoMap(string provider, int blockOffset, string xmlPathFile, string module = "")
        {
            SubscribeIoItemList(provider);

            XmlDocument xml = new XmlDocument();
            xml.Load(xmlPathFile);

            XmlNodeList lstDi = xml.SelectNodes("IO_DEFINE/Dig_In/DI_ITEM");

            var scConfig = SC.GetConfigItem("System.IsIgnoreSaveDB");
            var isIgnoreSaveDB = scConfig == null ? false : scConfig.BoolValue;

            //<DI_ITEM Index="0" Name="" BufferOffset="0" Addr="0" Description=""/>

            List<DIAccessor> diList = new List<DIAccessor>();
            foreach (var diItem in lstDi)
            {
                XmlElement element = diItem as XmlElement;
                if (element == null)
                    continue;

                string index = element.GetAttribute("Index");
                string bufferOffset = element.GetAttribute("BufferOffset");
                if (string.IsNullOrEmpty(bufferOffset))
                    bufferOffset = index;
                string name = element.GetAttribute("Name");
                string address = element.GetAttribute("Addr");
                string description = element.GetAttribute("Description");
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(index) || string.IsNullOrEmpty(bufferOffset))
                    continue;

                name = name.Trim();
                index = index.Trim();
                bufferOffset = bufferOffset.Trim();
                string moduleName = string.IsNullOrEmpty(module) ? name : $"{module}.{name}";

                int intIndex;
                if (!int.TryParse(index, out intIndex))
                    continue;

                int intBufferOffset;
                if (!int.TryParse(bufferOffset, out intBufferOffset))
                    continue;

                if (!_diBuffer.ContainsKey(provider) )
                {
                    throw new Exception("Not defined DI buffer from IO provider, " + provider);
                }
                if(!_diBuffer[provider].ContainsKey(blockOffset))
                {
                    throw new Exception("Not defined DI buffer from IO provider, " + provider);
                }

                DIAccessor diAccessor = new DIAccessor(moduleName, intBufferOffset, _diBuffer[provider][blockOffset], _diBuffer[provider][blockOffset]);

                diAccessor.IoTableIndex = intIndex;
                diAccessor.Addr = address;
                diAccessor.Provider = provider;
                diAccessor.BlockOffset = blockOffset;
                diAccessor.Description = description;

                diList.Add(diAccessor);

                _diMap[moduleName] = diAccessor;

                if (!_diList.ContainsKey(provider))
                    _diList[provider] = new List<DIAccessor>();
                _diList[provider].Add(diAccessor);

                _ioItemList[$"{provider}.DIItemList"].Add(new NotifiableIoItem()
                {
                    Address = address,
                    Name = moduleName,
                    Description = description,
                    Index = intIndex,
                    Provider = provider,
                    BlockOffset = blockOffset,
                    BlockIndex = intIndex,
                });

                if (!isIgnoreSaveDB)
                {

                    DATA.Subscribe($"IO.{moduleName}", () => diAccessor.Value);
                }
            }

            XmlNodeList lstDo = xml.SelectNodes("IO_DEFINE/Dig_Out/DO_ITEM");
            //    < DO_ITEM Index = "0" BufferOffset="0" Name = "" Addr = "0" Description = "" />
            foreach (var doItem in lstDo)
            {
                XmlElement element = doItem as XmlElement;
                if (element == null)
                    continue;

                string index = element.GetAttribute("Index");
                string bufferOffset = element.GetAttribute("BufferOffset");
                if (string.IsNullOrEmpty(bufferOffset))
                    bufferOffset = index;
                string name = element.GetAttribute("Name");
                string address = element.GetAttribute("Addr");
                string description = element.GetAttribute("Description");
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(index) || string.IsNullOrEmpty(bufferOffset))
                    continue;

                name = name.Trim();
                index = index.Trim();
                bufferOffset = bufferOffset.Trim();
                string moduleName = string.IsNullOrEmpty(module) ? name : $"{module}.{name}";

                int intIndex;
                if (!int.TryParse(index, out intIndex))
                    continue;

                int intBufferOffset;
                if (!int.TryParse(bufferOffset, out intBufferOffset))
                    continue;

                if (!_doBuffer.ContainsKey(provider) || !_doBuffer[provider].ContainsKey(blockOffset))
                {
                    throw new Exception("Not defined DO buffer from IO provider, " + provider);
                }

                DOAccessor doAccessor = new DOAccessor(moduleName, intBufferOffset, _doBuffer[provider][blockOffset]);
                _doMap[moduleName] = doAccessor;
                doAccessor.IoTableIndex = intIndex;
                doAccessor.Addr = address;
                doAccessor.Provider = provider;
                doAccessor.BlockOffset = blockOffset;
                doAccessor.Description = description;

                if (!_doList.ContainsKey(provider))
                    _doList[provider] = new List<DOAccessor>();
                _doList[provider].Add(doAccessor);

                _ioItemList[$"{provider}.DOItemList"].Add(new NotifiableIoItem()
                {
                    Address = address,
                    Name = moduleName,
                    Description = description,
                    Index = intIndex,
                    Provider = provider,
                    BlockOffset = blockOffset,
                    BlockIndex = intIndex,
                });

                if (!isIgnoreSaveDB)
                    DATA.Subscribe($"IO.{moduleName}", () => doAccessor.Value);
            }

            XmlNodeList lstAo = xml.SelectNodes("IO_DEFINE/Ana_Out/AO_ITEM");
            //    < AO_ITEM Index = "0" BufferOffset="0" Name = "" Addr = "0" Description = "" />
            foreach (var aoItem in lstAo)
            {
                XmlElement element = aoItem as XmlElement;
                if (element == null)
                    continue;

                string index = element.GetAttribute("Index");
                string bufferOffset = element.GetAttribute("BufferOffset");
                if (string.IsNullOrEmpty(bufferOffset))
                    bufferOffset = index;
                string name = element.GetAttribute("Name");
                string address = element.GetAttribute("Addr");
                string description = element.GetAttribute("Description");
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(index) || string.IsNullOrEmpty(bufferOffset))
                    continue;

                name = name.Trim();
                index = index.Trim();
                bufferOffset = bufferOffset.Trim();
                string moduleName = string.IsNullOrEmpty(module) ? name : $"{module}.{name}";

                int intIndex;
                if (!int.TryParse(index, out intIndex))
                    continue;

                int intBufferOffset;
                if (!int.TryParse(bufferOffset, out intBufferOffset))
                    continue;

                if (!_aoBufferShort.ContainsKey(provider) || !_aoBufferShort[provider].ContainsKey(blockOffset))
                {
                    throw new Exception("Not defined AO buffer from IO provider, " + provider);
                }

                AOAccessor aoAccessor = new AOAccessor(moduleName, intBufferOffset, _aoBufferShort[provider][blockOffset], _aoBufferFloat[provider][blockOffset]);
                _aoMap[moduleName] = aoAccessor;
                aoAccessor.IoTableIndex = intIndex;
                aoAccessor.Addr = address;
                aoAccessor.Provider = provider;
                aoAccessor.BlockOffset = blockOffset;
                aoAccessor.Description = description;

                if (!_aoList.ContainsKey(provider))
                    _aoList[provider] = new List<AOAccessor>();
                _aoList[provider].Add(aoAccessor);

                _ioItemList[$"{provider}.AOItemList"].Add(new NotifiableIoItem()
                {
                    Address = address,
                    Name = moduleName,
                    Description = description,
                    Index = intIndex,
                    Provider = provider,
                    BlockOffset = blockOffset,
                    BlockIndex = intIndex,
                });

                if (!isIgnoreSaveDB)
                {
                    if (_aoBufferType.ContainsKey(provider) && _aoBufferType[provider].ContainsKey(blockOffset) &&
                        _aoBufferType[provider][blockOffset] == typeof(float))
                    {
                        DATA.Subscribe($"IO.{moduleName}", () => aoAccessor.FloatValue);
                    }
                    else
                    {
                        DATA.Subscribe($"IO.{moduleName}", () => aoAccessor.Value);
                    }

                }
            }

            XmlNodeList lstAi = xml.SelectNodes("IO_DEFINE/Ana_In/AI_ITEM");
            //    < AO_ITEM Index = "0" BufferOffset="0" Name = "" Addr = "0" Description = "" />
            foreach (var aiItem in lstAi)
            {
                XmlElement element = aiItem as XmlElement;
                if (element == null)
                    continue;

                string index = element.GetAttribute("Index");
                string bufferOffset = element.GetAttribute("BufferOffset");
                if (string.IsNullOrEmpty(bufferOffset))
                    bufferOffset = index;
                string name = element.GetAttribute("Name");
                string address = element.GetAttribute("Addr");
                string description = element.GetAttribute("Description");
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(index) || string.IsNullOrEmpty(bufferOffset))
                    continue;

                name = name.Trim();
                index = index.Trim();
                bufferOffset = bufferOffset.Trim();
                string nameWithModule = string.IsNullOrEmpty(module) ? name : $"{module}.{name}";

                int intIndex;
                if (!int.TryParse(index, out intIndex))
                    continue;

                int intBufferOffset;
                if (!int.TryParse(bufferOffset, out intBufferOffset))
                    continue;

                if (!_aiBufferShort.ContainsKey(provider) || !_aiBufferShort[provider].ContainsKey(blockOffset))
                {
                    throw new Exception("Not defined AI buffer from IO provider, " + provider);
                }

                AIAccessor aiAccessor = new AIAccessor(nameWithModule, intBufferOffset, _aiBufferShort[provider][blockOffset], _aiBufferFloat[provider][blockOffset]);
                _aiMap[nameWithModule] = aiAccessor;
                aiAccessor.IoTableIndex = intIndex;
                aiAccessor.Addr = address;
                aiAccessor.Provider = provider;
                aiAccessor.BlockOffset = blockOffset;
                aiAccessor.Description = description;

                if (!_aiList.ContainsKey(provider))
                    _aiList[provider] = new List<AIAccessor>();
                _aiList[provider].Add(aiAccessor);


                _ioItemList[$"{provider}.AIItemList"].Add(new NotifiableIoItem()
                {
                    Address = address,
                    Name = nameWithModule,
                    Description = description,
                    Index = intIndex,
                    Provider = provider,
                    BlockOffset = blockOffset,
                    BlockIndex = intIndex,
                });

                if (!isIgnoreSaveDB)
                {
                    if (_aiBufferType.ContainsKey(provider) && _aiBufferType[provider].ContainsKey(blockOffset) &&
                         _aiBufferType[provider][blockOffset] == typeof(float))
                    {
                        DATA.Subscribe($"IO.{nameWithModule}", () => aiAccessor.FloatValue);
                    }
                    else
                    {
                        DATA.Subscribe($"IO.{nameWithModule}", () => aiAccessor.Value);
                    }
                }
            }
        }

        public void SetIoMap(string provider, Dictionary<int, string> ioMappingPathFile)
        {
            foreach (var map in ioMappingPathFile)
            {
                SetIoMap(provider, map.Key, map.Value);
            }

            DATA.Subscribe(provider, "DIList", SubscribeDiData);
            DATA.Subscribe(provider, "DOList", SubscribeDoData);
            DATA.Subscribe(provider, "AIList", SubscribeAiData);
            DATA.Subscribe(provider, "AOList", SubscribeAoData);
        }


        public void SetIoMapByModule(string provider, int offset, string ioMappingPathFile, string module)
        {
            SetIoMap(provider, offset, ioMappingPathFile, module);

            DATA.Subscribe(provider, "DIList", SubscribeDiData);
            DATA.Subscribe(provider, "DOList", SubscribeDoData);
            DATA.Subscribe(provider, "AIList", SubscribeAiData);
            DATA.Subscribe(provider, "AOList", SubscribeAoData);
        }

        private void SubscribeIoItemList(string provider)
        {
            string diKey = $"{provider}.DIItemList";
            if (!_ioItemList.ContainsKey(diKey))
            {
                _ioItemList[diKey] = new List<NotifiableIoItem>();
                DATA.Subscribe(diKey, () => _ioItemList[diKey]);
            }

            string doKey = $"{provider}.DOItemList";
            if (!_ioItemList.ContainsKey(doKey))
            {
                _ioItemList[doKey] = new List<NotifiableIoItem>();
                DATA.Subscribe(doKey, () => _ioItemList[doKey]);
            }

            string aiKey = $"{provider}.AIItemList";
            if (!_ioItemList.ContainsKey(aiKey))
            {
                _ioItemList[aiKey] = new List<NotifiableIoItem>();
                DATA.Subscribe(aiKey, () => _ioItemList[aiKey]);
            }

            string aoKey = $"{provider}.AOItemList";
            if (!_ioItemList.ContainsKey(aoKey))
            {
                _ioItemList[aoKey] = new List<NotifiableIoItem>();
                DATA.Subscribe(aoKey, () => _ioItemList[aoKey]);
            }
        }
    }
}
