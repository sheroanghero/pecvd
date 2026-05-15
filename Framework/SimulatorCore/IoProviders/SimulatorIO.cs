using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aitex.Core.RT.IOCore;
using MECF.Framework.Common.IOCore;
using MECF.Framework.RT.Core.IoProviders;

namespace MECF.Framework.Simulator.Core.IoProviders
{
    public class SimulatorIO : MCProtocolPlcSimulator
    {
        private Random _rd = new Random();

        public ObservableCollection<NotifiableIoItem> DiItemList { get; set; }
        public ObservableCollection<NotifiableIoItem> DoItemList { get; set; }
        public ObservableCollection<NotifiableIoItem> AiItemList { get; set; }
        public ObservableCollection<NotifiableIoItem> AoItemList { get; set; }

        private string _source;

        public SimulatorIO(int port, string source, string ioMapPathFile, string module)
            : base("127.0.0.1", port)
        {
            _source = source;

            _buffers.Add(new PlcBuffer() { Buffer = new byte[640], Type = IoType.DI, Offset = 0, Size = 640, BoolValue = new bool[640] });
            _buffers.Add(new PlcBuffer() { Buffer = new byte[640], Type = IoType.DO, Offset = 0, Size = 640, BoolValue = new bool[640] });
            _buffers.Add(new PlcBuffer() { Buffer = new byte[640], Type = IoType.AI, Offset = 0, Size = 640, ShortValue = new ushort[640] });
            _buffers.Add(new PlcBuffer() { Buffer = new byte[640], Type = IoType.AO, Offset = 0, Size = 640, ShortValue = new ushort[640] });

            List<IoBlockItem> lstBuffers = new List<IoBlockItem>();
            lstBuffers.Add(new IoBlockItem() { Type = IoType.DI, Offset = 0, Size = 640 });
            lstBuffers.Add(new IoBlockItem() { Type = IoType.DO, Offset = 0, Size = 640 });
            lstBuffers.Add(new IoBlockItem() { Type = IoType.AI, Offset = 0, Size = 640 });
            lstBuffers.Add(new IoBlockItem() { Type = IoType.AO, Offset = 0, Size = 640 });

            IoManager.Instance.SetBufferBlock(source, lstBuffers);

            IoManager.Instance.SetIoMap(source, 0, ioMapPathFile, module);


            Init();


            SetDefaultValue();
            //SetScCommand = new DelegateCommand<string>(SetSc);
        }



        //private void SetSc(string obj)
        //{
        //    NotifiableSCConfigItem item = ScItemList.First(x => x.Name == obj);

        //    if (item.Type != SCConfigType.StringType.ToString() && string.IsNullOrEmpty(item.SetPoint))
        //        return;

        //    SC.SetItemValue(obj, item.SetPoint == null ? "" : item.SetPoint);

        //    item.StringValue = SC.GetItemValue(obj).ToString();
        //    item.InvokePropertyChanged(nameof(item.StringValue));
        //}

        void Init()
        {
            if (DiItemList == null)
            {
                List<DIAccessor> diItems = IO.GetDiList(_source);
                if (diItems != null)
                {
                    DiItemList = new ObservableCollection<NotifiableIoItem>();
                    foreach (var diItem in diItems)
                    {
                        NotifiableIoItem item = new NotifiableIoItem()
                        {
                            Name = diItem.Name,
                            Index = diItem.Index,
                            Description = diItem.Description,
                            BoolValue = diItem.Value,
                            Address = diItem.Addr,
                            BlockOffset = diItem.BlockOffset,
                            BlockIndex = diItem.Index,
                        };
                        DiItemList.Add(item);
                    }
                }
            }

            if (DoItemList == null)
            {
                List<DOAccessor> doItems = IO.GetDoList(_source);
                if (doItems != null)
                {
                    DoItemList = new ObservableCollection<NotifiableIoItem>();
                    foreach (var ioItem in doItems)
                    {
                        NotifiableIoItem item = new NotifiableIoItem()
                        {
                            Name = ioItem.Name,
                            Index = ioItem.Index,
                            Description = ioItem.Description,
                            BoolValue = ioItem.Value,
                            Address = ioItem.Addr,
                            BlockOffset = ioItem.BlockOffset,
                            BlockIndex = ioItem.Index,
                        };
                        DoItemList.Add(item);
                    }
                }
            }

            if (AiItemList == null)
            {
                List<AIAccessor> aiItems = IO.GetAiList(_source);
                if (aiItems != null)
                {
                    AiItemList = new ObservableCollection<NotifiableIoItem>();
                    foreach (var ioItem in aiItems)
                    {
                        NotifiableIoItem item = new NotifiableIoItem()
                        {
                            Name = ioItem.Name,
                            Index = ioItem.Index,
                            Description = ioItem.Description,
                            ShortValue = ioItem.Value,
                            Address = ioItem.Addr,
                            BlockOffset = ioItem.BlockOffset,
                            BlockIndex = ioItem.Index,
                        };
                        AiItemList.Add(item);
                    }
                }
            }

            if (AoItemList == null)
            {
                List<AOAccessor> aoItems = IO.GetAoList(_source);
                if (aoItems != null)
                {
                    AoItemList = new ObservableCollection<NotifiableIoItem>();
                    foreach (var ioItem in aoItems)
                    {
                        NotifiableIoItem item = new NotifiableIoItem()
                        {
                            Name = ioItem.Name,
                            Index = ioItem.Index,
                            Description = ioItem.Description,
                            ShortValue = ioItem.Value,
                            Address = ioItem.Addr,
                            BlockOffset = ioItem.BlockOffset,
                            BlockIndex = ioItem.Index,
                        };
                        AoItemList.Add(item);
                    }
                }
            }
        }

        private void SetDefaultValue()
        {
            //IO.DI[IoNameSorter6LP.DI_Maintenance].Value = true;
        }

        protected override bool OnTimer()
        {

            if (DiItemList != null)
            {
                foreach (var notifiableIoItem in DiItemList)
                {
                    if (notifiableIoItem.HoldValue)
                    {
                        IO.DI[notifiableIoItem.Name].Value = notifiableIoItem.BoolValue;
                    }
                    notifiableIoItem.BoolValue = IO.DI[notifiableIoItem.Name].Value;
                    notifiableIoItem.InvokePropertyChanged("BoolValue");
                }
            }

            if (DoItemList != null)
            {
                foreach (var notifiableIoItem in DoItemList)
                {
                    notifiableIoItem.BoolValue = IO.DO[notifiableIoItem.Name].Value;
                    notifiableIoItem.InvokePropertyChanged("BoolValue");
                }
            }

            if (AiItemList != null)
            {
                foreach (var notifiableIoItem in AiItemList)
                {
                    if (notifiableIoItem.HoldValue)
                    {
                        IO.AI[notifiableIoItem.Name].Value = notifiableIoItem.ShortValue;
                    }

                    notifiableIoItem.ShortValue = IO.AI[notifiableIoItem.Name].Value;
                    notifiableIoItem.InvokePropertyChanged("ShortValue");
                }
            }

            if (AoItemList != null)
            {
                foreach (var notifiableIoItem in AoItemList)
                {
                    notifiableIoItem.ShortValue = IO.AO[notifiableIoItem.Name].Value;
                    notifiableIoItem.InvokePropertyChanged("ShortValue");
                }
            }

            foreach (var plcBuffer in _buffers)
            {
                //IO修改 ---> PLC  
                if (plcBuffer.Type == IoType.DI)
                {
                    var ioBuffers = IoManager.Instance.GetDiBuffer(_source);
                    if (ioBuffers != null)
                    { foreach (var ioBuffer in ioBuffers)
                    {
                        if (plcBuffer.Offset == ioBuffer.Key)
                        {
                            plcBuffer.BoolValue = ioBuffer.Value;
                        }
                    }}
                }

                // PLC --> IO
                if (plcBuffer.Type == IoType.DO)
                {
                    var ioBuffers = IoManager.Instance.GetDoBuffer(_source);
                    if (ioBuffers!=null)
                    { foreach (var buffer in ioBuffers)
                    {
                        if (plcBuffer.Offset == buffer.Key)
                        {
                            //IoManager.Instance.SetDoBuffer(_source, plcBuffer.Offset, plcBuffer.BoolValue);
                        }
                    }}
                }

                //IO修改 ---> PLC  
                if (plcBuffer.Type == IoType.AI)
                {
                    var ioBuffers = IoManager.Instance.GetAiBuffer(_source);
                    if (ioBuffers != null)
                    {
                        foreach (var buffer in ioBuffers)
                    {
                        if (plcBuffer.Offset == buffer.Key)
                        {
                            plcBuffer.ShortValue = Array.ConvertAll<short, ushort>(buffer.Value,x=>(ushort)x);
                        }
                    }}
                }

                // PLC --> IO
                if (plcBuffer.Type == IoType.AO)
                {
                    var ioBuffers = IoManager.Instance.GetAoBuffer(_source);
                    if (ioBuffers != null)
                    {    foreach (var buffer in ioBuffers)
                    {
                        if (plcBuffer.Offset == buffer.Key)
                        {
                            //IoManager.Instance.SetAoBuffer(_source, plcBuffer.Offset, Array.ConvertAll<ushort, short>(plcBuffer.ShortValue, x => (short)x));
                        }
                    }}
                }
            }




            return true;
        }

 

    }
}