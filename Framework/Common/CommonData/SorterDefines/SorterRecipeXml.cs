using Aitex.Core.Common;
using Aitex.Core.RT.Log;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using MECF.Framework.Common.Equipment;

namespace Aitex.Sorter.Common
{
    [DataContract]
    [Serializable]
    public class SorterRecipeXml : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string name;
        [DataMember]
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }
        [DataMember]
        public SorterRecipeType RecipeType
        {
            get { return _type; }
            set
            {
                _type = value;
                OnPropertyChanged("RecipeType");
            }
        }
        [DataMember]
        public ObservableCollection<ModuleName> Source
        {
            get { return _source; }
            set
            {
                _source = value;
                OnPropertyChanged("Source");
                OnPropertyChanged("SourceStation");
            }
        }

        public string StringListSource
        {
            get
            {
                if (RecipeType != SorterRecipeType.HostNToN && RecipeType != SorterRecipeType.TransferNToN)
                    return string.Join(",", SourceStation);
                return string.Empty;
            }
        }

        public ObservableCollection<string> SourceStation
        {
            get { return new ObservableCollection<string>(_source.Select(x => x.ToString())); }
            set
            {
                _source = new ObservableCollection<ModuleName>(value.Select(x => (ModuleName)Enum.Parse(typeof(ModuleName), x)));
                OnPropertyChanged("Source");
                OnPropertyChanged("SourceStation");
            }
        }

        [DataMember]
        public ObservableCollection<ModuleName> Destination
        {
            get { return _destination; }
            set
            {
                _destination = value;
                OnPropertyChanged("Destination");
                OnPropertyChanged("DestinationStation");
            }
        }
        public string StringListDestination
        {
            get
            {
                if (RecipeType == SorterRecipeType.Transfer1To1 || RecipeType == SorterRecipeType.TransferNTo1)
                    return string.Join(",", DestinationStation);
                return string.Empty;
            }
        }

        public ObservableCollection<string> DestinationStation
        {
            get { return new ObservableCollection<string>(_destination.Select(x => x.ToString())); }
            set
            {
                _destination = new ObservableCollection<ModuleName>(value.Select(x => (ModuleName)Enum.Parse(typeof(ModuleName), x)));
                OnPropertyChanged("Destination");
                OnPropertyChanged("DestinationStation");
            }
        }

        [DataMember]
        public bool IsReadLaserMarker
        {
            get { return _isReadLaserMarker; }
            set
            {
                _isReadLaserMarker = value;
                OnPropertyChanged("IsReadLaserMarker");
            }
        }
        [DataMember]
        public string LaserMark1Jobs
        {
            get { return _lasermark1jobs; }
            set
            {
                _lasermark1jobs = value;
                OnPropertyChanged("LaserMark1Jobs");
            }
        }
        [DataMember]
        public string ReadIDRecipe
        {
            get { return _readidrecipe; }
            set
            {
                _readidrecipe = value;
                OnPropertyChanged("ReadIDRecipe");
            }
        }
        [DataMember]
        public bool IsReadT7Code
        {
            get { return _isReadT7Code; }
            set
            {
                _isReadT7Code = value;
                OnPropertyChanged("IsReadT7Code");
            }
        }


        [DataMember]
        public string LaserMark2Jobs
        {
            get { return _lasermark2jobs; }
            set
            {
                _lasermark2jobs = value;
                OnPropertyChanged("LaserMark2Jobs");
            }
        }

        [DataMember]
        public bool IsTurnOver
        {
            get { return _isTurnOver; }
            set
            {
                _isTurnOver = value;
                OnPropertyChanged("IsTurnOver");
            }
        }

        [DataMember]
        public bool IsAlign
        {
            get { return _isAlign; }
            set
            {
                _isAlign = value;
                OnPropertyChanged("IsAlign");
            }
        }
        [DataMember]
        public bool IsPostAlign
        {
            get { return _isPostAlign; }
            set
            {
                _isPostAlign = value;
                OnPropertyChanged("IsPostAlign");
            }
        }

        private bool _isVerifyLaserMarker;
        [DataMember]
        public bool IsVerifyLaserMarker
        {
            get { return _isVerifyLaserMarker; }
            set
            {
                _isVerifyLaserMarker = value;
                OnPropertyChanged("IsVerifyLaserMarker");
            }
        }

        private bool _isVerifyT7Code;
        [DataMember]
        public bool IsVerifyT7Code
        {
            get { return _isVerifyT7Code; }
            set
            {
                _isVerifyT7Code = value;
                OnPropertyChanged("IsVerifyT7Code");
            }
        }

        private OrderByMode _orderBy;
        [DataMember]
        public OrderByMode OrderBy
        {
            get { return _orderBy; }
            set
            {
                _orderBy = value;
                OnPropertyChanged("OrderBy");
            }
        }

        [DataMember]
        public double AlignAngle
        {
            get { return _anlignAngle; }
            set
            {
                _anlignAngle = value;
                OnPropertyChanged("AlignAngle");
            }
        }
        [DataMember]
        public double PostAlignAngle
        {
            get { return _postAlignAngle; }
            set
            {
                _postAlignAngle = value;
                OnPropertyChanged("PostAlignAngle");
            }
        }
        [DataMember]
        public SorterRecipePlaceModeTransfer1To1 PlaceModeTransfer1To1
        {
            get { return _placeModeTransfer1To1; }
            set
            {
                _placeModeTransfer1To1 = value;
                OnPropertyChanged("PlaceModeTransfer1To1");
                OnPropertyChanged("PlaceMode");
            }
        }

        [DataMember]
        public SorterPickMode PickMode
        {
            get { return _pickMode; }
            set
            {
                _pickMode = value;
                OnPropertyChanged("PickMode");
            }
        }


        [DataMember]
        public SorterRecipePlaceModeOrder PlaceModeOrder
        {
            get { return _placeModeOrder; }
            set
            {
                _placeModeOrder = value;
                OnPropertyChanged("PlaceModeOrder");
                OnPropertyChanged("PlaceMode");
            }
        }
        [DataMember]
        public SorterRecipePlaceModePack PlaceModePack
        {
            get { return _placeModePack; }
            set
            {
                _placeModePack = value;
                OnPropertyChanged("PlaceModePack");
                OnPropertyChanged("PlaceMode");
            }
        }

        private int waferReaderIndex;
        [DataMember]
        public int WaferReaderIndex
        {
            get => waferReaderIndex;
            set
            {
                waferReaderIndex = value;
                OnPropertyChanged("WaferReaderIndex");
            }
        }
        private int waferReader2Index;
        [DataMember]
        public int WaferReader2Index
        {
            get => waferReader2Index;
            set
            {
                waferReader2Index = value;
                OnPropertyChanged("WaferReader2Index");
            }
        }

        [DataMember]
        public ObservableCollection<SorterRecipeTransferTableItem> TransferItems
        {
            get; set;
        }

        [DataMember]
        public ObservableCollection<SorterHostUsageRecipeTableItem> HostUsageItems
        {
            get; set;
        }

        [DataMember]
        public ObservableCollection<WaferInfo> TransferSourceLP1
        {
            get; set;
        }

        [DataMember]
        public ObservableCollection<WaferInfo> TransferSourceLP2
        {
            get; set;
        }
        [DataMember]
        public ObservableCollection<WaferInfo> TransferSourceLP3
        {
            get; set;
        }

        [DataMember]
        public ObservableCollection<WaferInfo> TransferDestinationLP1
        {
            get; set;
        }

        [DataMember]
        public ObservableCollection<WaferInfo> TransferDestinationLP2
        {
            get; set;
        }

        [DataMember]
        public ObservableCollection<WaferInfo> TransferDestinationLP3
        {
            get; set;
        }

        public string PlaceMode
        {
            get
            {
                var placeMode = "";
                switch (RecipeType)
                {
                    case SorterRecipeType.Transfer1To1:
                    case SorterRecipeType.TransferNTo1:
                        placeMode = PlaceModeTransfer1To1.ToString();
                        break;
                    case SorterRecipeType.TransferNToN:
                    case SorterRecipeType.HostNToN:
                        break;
                    case SorterRecipeType.Pack:
                        placeMode = PlaceModePack.ToString();
                        break;
                    case SorterRecipeType.Order:
                        placeMode = PlaceModeOrder.ToString();
                        break;
                    case SorterRecipeType.Align:
                        break;
                    case SorterRecipeType.ReadWaferId:
                        break;
                    default:
                        break;
                }
                return placeMode;
            }
        }

        public SlotTransferInfo[] TransferSlotInfoA
        {
            get
            {
                return GetTransferSlotInfo(ModuleName.LP1);
            }
        }

        public SlotTransferInfo[] TransferSlotInfoB
        {
            get
            {
                return GetTransferSlotInfo(ModuleName.LP2);
            }
        }

        public SlotTransferInfo[] TransferSlotInfoC
        {
            get
            {
                return GetTransferSlotInfo(ModuleName.LP3);
            }
        }

        private SlotTransferInfo[] GetTransferSlotInfo(ModuleName station)
        {
            var result = new SlotTransferInfo[25];
            for (int i = 0; i < 25; i++)
            {
                var slotInfo = new SlotTransferInfo();
                slotInfo.Station = station;
                slotInfo.Slot = i;
                var srcItem = TransferItems.FirstOrDefault(x => x.SourceStation == slotInfo.Station && x.SourceSlot == slotInfo.Slot);
                if (srcItem != null)
                {
                    slotInfo.DestinationStation = srcItem.DestinationStation;
                    slotInfo.DestinationSlot = srcItem.DestinationSlot;
                }

                var dstItem = TransferItems.FirstOrDefault(x => x.DestinationStation == slotInfo.Station && x.DestinationSlot == slotInfo.Slot);
                if (dstItem != null)
                {
                    slotInfo.SourceStation = dstItem.SourceStation;
                    slotInfo.SourceSlot = dstItem.SourceSlot;
                }
                result[i] = slotInfo;
            }
            return result;
        }

        private static string _template = @"<?xml version='1.0'?><AitexSorterRecipe Type='' Source='' Destination='' PlaceMode='' IsReadLaserMarker='' IsReadT7Code='' OrderBy='' IsAlign='' AlignAngle='' IsVerifyLaserMarker='' IsVerifyT7Code='' IsTurnOver=''><TransferTable></TransferTable></AitexSorterRecipe>";

        private XDocument doc = new XDocument();

        private SorterRecipeType _type = SorterRecipeType.Transfer1To1;
        private ObservableCollection<ModuleName> _source = new ObservableCollection<ModuleName>();
        private ObservableCollection<ModuleName> _destination = new ObservableCollection<ModuleName>();
        private SorterRecipePlaceModeTransfer1To1 _placeModeTransfer1To1 = SorterRecipePlaceModeTransfer1To1.FromBottom;

        private SorterRecipePlaceModePack _placeModePack = SorterRecipePlaceModePack.FromBottomInsert;
        private SorterRecipePlaceModeOrder _placeModeOrder = SorterRecipePlaceModeOrder.Forward;
        private bool _isAlign = false;

        private double _anlignAngle = 0;

        private bool _isReadLaserMarker = false;
        private bool _isReadT7Code = false;
        private bool _isTurnOver = false;

        private SorterPickMode _pickMode = SorterPickMode.FromBottom;
        private bool _isPostAlign = false;
        private double _postAlignAngle = 0;
        private string _lasermark1jobs = "";
        private string _lasermark2jobs = "";
        private string _readidrecipe = "";

        public SorterRecipeXml(string name = "") : this(_template, name)
        {
        }

        public SorterRecipeXml(string content, string name = "")
        {
            TransferItems = new ObservableCollection<SorterRecipeTransferTableItem>();
            HostUsageItems = new ObservableCollection<SorterHostUsageRecipeTableItem>();
            Name = name;
            SetContent(content);
        }
        public SorterRecipeXml(string content, bool IsRemoteRecipe, string name = "")
        {
            TransferItems = new ObservableCollection<SorterRecipeTransferTableItem>();
            HostUsageItems = new ObservableCollection<SorterHostUsageRecipeTableItem>();
            Name = name;
            try
            {
                doc = XDocument.Parse(content);

            }
            catch (Exception e)
            {
                LOG.Write(e);
            }

        }
        public bool SetContent(string content)
        {
            try
            {
                doc = XDocument.Parse(content);
                ParseContent();
            }
            catch (Exception e)
            {
                LOG.Write(e);
                return false;
            }

            return true;
        }

        public string GetContent()
        {
            return doc.ToString();
        }

        public void SaveContent()
        {
            var lst = new List<XElement>();
            foreach (var item in TransferItems)
            {
                lst.Add(new XElement("TransferItem"
                    , new XAttribute("SourceStation", item.SourceStation.ToString())
                    , new XAttribute("SourceSlot", item.SourceSlot.ToString())
                    , new XAttribute("DestinationStation", item.DestinationStation.ToString())
                    , new XAttribute("DestinationSlot", item.DestinationSlot.ToString())
                    , new XAttribute("IsReadLaserMarker", item.IsReadLaserMarker.ToString())
                    , new XAttribute("IsReadT7Code", item.IsReadT7Code.ToString())
                    , new XAttribute("IsAlign", item.IsAlign.ToString())
                    , new XAttribute("AlignAngle", item.AlignAngle.ToString())
                    , new XAttribute("IsVerifyLaserMarker", item.IsReadLaserMarker.ToString())
                    , new XAttribute("IsVerifyT7Code", item.IsReadT7Code.ToString())
                    , new XAttribute("OrderBy", item.OrderBy.ToString())
                    , new XAttribute("IsTurnOver", item.IsTurnOver.ToString())
                    ));
            }

            doc = new XDocument(new XElement("AitexSorterRecipe"
                , new XAttribute("Type", RecipeType.ToString())
                , new XAttribute("Source", StringListSource)
                , new XAttribute("Destination", StringListDestination)
                , new XAttribute("PlaceMode", PlaceMode)
                , new XAttribute("PickMode", PickMode)    //new1
                , new XAttribute("IsReadLaserMarker", IsReadLaserMarker.ToString())
                , new XAttribute("IsReadT7Code", IsReadT7Code.ToString())
                , new XAttribute("IsVerifyLaserMarker", IsReadLaserMarker.ToString())
                , new XAttribute("IsVerifyT7Code", IsReadT7Code.ToString())
                , new XAttribute("OrderBy", OrderBy.ToString())
                , new XAttribute("IsAlign", IsAlign.ToString())
                , new XAttribute("AlignAngle", AlignAngle.ToString())
                , new XAttribute("IsTurnOver", IsTurnOver.ToString())
                , new XAttribute("IsPostAlign", IsPostAlign.ToString())  //new2
                , new XAttribute("PostAlignAngle", PostAlignAngle.ToString())   //new3
                , new XAttribute("WaferReaderIndex", WaferReaderIndex.ToString())
                , new XAttribute("WaferReader2Index", WaferReader2Index.ToString())

                , new XAttribute("LaserMark1Jobs", LaserMark1Jobs.ToString())   //new4
                , new XAttribute("LaserMark2Jobs", LaserMark2Jobs.ToString())  //new5
                , new XAttribute("ReadIDRecipe", ReadIDRecipe.ToString())  //new6

                , new XElement("TransferTable", lst)
                ));
        }

        public void SaveHostUsageContent()
        {

        }

        private ObservableCollection<ModuleName> ParseModule(string value)
        {
            ObservableCollection<ModuleName> result = new ObservableCollection<ModuleName>();
            if (!string.IsNullOrEmpty(value))
            {
                string[] modules = value.Split(',');
                ModuleName source;
                foreach (var module in modules)
                {
                    if (Enum.TryParse(module, out source))
                        result.Add(source);
                }
            }

            return result;
        }
        private void ParseContent()
        {
            try
            {
                var nodeRoot = doc.Root;
                if (nodeRoot == null)
                {
                    LOG.Write(string.Format("recipe not valid"));
                    return;
                }
                string value = nodeRoot.Attribute("Type").Value;
                SorterRecipeType type;
                Enum.TryParse(value, out type);
                RecipeType = type;

                value = nodeRoot.Attribute("Source").Value;
                Source = ParseModule(value);

                value = nodeRoot.Attribute("Destination").Value;
                Destination = ParseModule(value);

                value = nodeRoot.Attribute("PlaceMode").Value;
                SorterRecipePlaceModePack pack;
                Enum.TryParse(value, out pack);
                PlaceModePack = pack;

                SorterRecipePlaceModeOrder order;
                Enum.TryParse(value, out order);
                PlaceModeOrder = order;

                SorterRecipePlaceModeTransfer1To1 placeModeTransfer1To1;
                Enum.TryParse(value, out placeModeTransfer1To1);
                PlaceModeTransfer1To1 = placeModeTransfer1To1;

                SorterPickMode pickMode;
                Enum.TryParse(value, out pickMode);
                PickMode = pickMode;

                value = nodeRoot.Attribute("IsReadLaserMarker").Value;
                bool isReadLaserMarker;
                bool.TryParse(value, out isReadLaserMarker);
                IsReadLaserMarker = isReadLaserMarker;

                value = nodeRoot.Attribute("IsReadT7Code").Value;
                bool isReadT7Code;
                bool.TryParse(value, out isReadT7Code);
                IsReadT7Code = isReadT7Code;

                value = nodeRoot.Attribute("IsVerifyLaserMarker").Value;
                bool isVerifyLaserMarker;
                bool.TryParse(value, out isVerifyLaserMarker);
                IsVerifyLaserMarker = isVerifyLaserMarker;

                value = nodeRoot.Attribute("IsVerifyT7Code").Value;
                bool isVerifyT7Code;
                bool.TryParse(value, out isVerifyT7Code);
                IsVerifyT7Code = isVerifyT7Code;

                value = nodeRoot.Attribute("OrderBy").Value;
                OrderByMode orderBy;
                Enum.TryParse(value, out orderBy);
                OrderBy = orderBy;

                value = nodeRoot.Attribute("IsAlign").Value;
                bool isAlign;
                bool.TryParse(value, out isAlign);
                IsAlign = isAlign;

                value = nodeRoot.Attribute("AlignAngle").Value;
                double anlignAngle;
                double.TryParse(value, out anlignAngle);
                AlignAngle = anlignAngle;

                value = nodeRoot.Attribute("IsTurnOver").Value;
                bool isTurnOver;
                bool.TryParse(value, out isTurnOver);
                IsTurnOver = isTurnOver;

                if (nodeRoot.Attribute("IsPostAlign") != null)
                {
                    value = nodeRoot.Attribute("IsPostAlign").Value;
                    bool isPostAlign;
                    bool.TryParse(value, out isPostAlign);
                    IsPostAlign = isPostAlign;
                }
                if (nodeRoot.Attribute("PostAlignAngle") != null)
                {
                    value = nodeRoot.Attribute("PostAlignAngle").Value;
                    double postalignAngle;
                    double.TryParse(value, out postalignAngle);
                    PostAlignAngle = postalignAngle;
                }
                if (nodeRoot.Attribute("PickMode") != null)
                {
                    value = nodeRoot.Attribute("PickMode").Value;
                    SorterPickMode pickmode;
                    Enum.TryParse(value, out pickmode);
                    PickMode = pickmode;
                }
                if (nodeRoot.Attribute("LaserMark1Jobs") != null)
                {
                    value = nodeRoot.Attribute("LaserMark1Jobs").Value;

                    LaserMark1Jobs = value;
                }
                if (nodeRoot.Attribute("ReadIDRecipe") != null)
                {
                    value = nodeRoot.Attribute("ReadIDRecipe").Value;

                    ReadIDRecipe = value;
                }
                if (nodeRoot.Attribute("LaserMark2Jobs") != null)
                {
                    value = nodeRoot.Attribute("LaserMark2Jobs").Value;

                    LaserMark2Jobs = value;
                }

                WaferReaderIndex = GetValue<int>(nodeRoot, "WaferReaderIndex");
                if (nodeRoot.Attribute("WaferReader2Index") != null)
                    WaferReader2Index = GetValue<int>(nodeRoot, "WaferReader2Index");

                var transferItems = nodeRoot.Element("TransferTable").Elements("TransferItem");
                if (transferItems.Any())
                {
                    foreach (var item in transferItems)
                    {
                        ModuleName sourceChamberSet;
                        Enum.TryParse(item.Attribute("SourceStation").Value, out sourceChamberSet);
                        int sourceSlot;
                        int.TryParse(item.Attribute("SourceSlot").Value, out sourceSlot);
                        ModuleName destChamberSet;
                        Enum.TryParse(item.Attribute("DestinationStation").Value, out destChamberSet);
                        int destSlot;
                        int.TryParse(item.Attribute("DestinationSlot").Value, out destSlot);
                        bool readLaserMarker;
                        bool.TryParse(item.Attribute("IsReadLaserMarker").Value, out readLaserMarker);
                        bool readT7Code;
                        bool.TryParse(item.Attribute("IsReadT7Code").Value, out readT7Code);
                        bool verifyLaserMarker;
                        bool.TryParse(item.Attribute("IsVerifyLaserMarker").Value, out verifyLaserMarker);
                        bool verifyT7Code;
                        bool.TryParse(item.Attribute("IsVerifyT7Code").Value, out verifyT7Code);
                        bool align;
                        bool.TryParse(item.Attribute("IsAlign").Value, out align);
                        bool turnOver;
                        bool.TryParse(item.Attribute("IsTurnOver").Value, out turnOver);
                        OrderByMode oBy;
                        Enum.TryParse(item.Attribute("OrderBy").Value, out oBy);
                        double angle;
                        double.TryParse(item.Attribute("AlignAngle").Value, out angle);

                        TransferItems.Add(new SorterRecipeTransferTableItem
                        {
                            SourceStation = sourceChamberSet,
                            SourceSlot = sourceSlot,
                            DestinationStation = destChamberSet,
                            DestinationSlot = destSlot,
                            IsReadLaserMarker = readLaserMarker,
                            IsReadT7Code = readT7Code,
                            IsVerifyLaserMarker = verifyLaserMarker,
                            IsVerifyT7Code = verifyT7Code,
                            IsAlign = align,
                            OrderBy = oBy,
                            AlignAngle = angle,
                            IsTurnOver = turnOver
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        private void ParseHostUsageContent()
        {
            try
            {
                var nodeRoot = doc.Root;
                if (nodeRoot == null)
                {
                    LOG.Write(string.Format("recipe not valid"));
                    return;
                }

                string value = nodeRoot.Attribute("Type").Value;
                SorterRecipeType type;
                Enum.TryParse(value, out type);
                RecipeType = type;

                value = nodeRoot.Attribute("Source").Value;
                Source = ParseModule(value);

                value = nodeRoot.Attribute("Destination").Value;
                Destination = ParseModule(value);

                value = nodeRoot.Attribute("PlaceMode").Value;
                SorterRecipePlaceModePack pack;
                Enum.TryParse(value, out pack);
                PlaceModePack = pack;

                SorterRecipePlaceModeOrder order;
                Enum.TryParse(value, out order);
                PlaceModeOrder = order;

                SorterRecipePlaceModeTransfer1To1 placeModeTransfer1To1;
                Enum.TryParse(value, out placeModeTransfer1To1);
                PlaceModeTransfer1To1 = placeModeTransfer1To1;

                SorterPickMode pickMode;
                Enum.TryParse(value, out pickMode);
                PickMode = pickMode;

                value = nodeRoot.Attribute("IsReadLaserMarker").Value;
                bool isReadLaserMarker;
                bool.TryParse(value, out isReadLaserMarker);
                IsReadLaserMarker = isReadLaserMarker;

                value = nodeRoot.Attribute("IsReadT7Code").Value;
                bool isReadT7Code;
                bool.TryParse(value, out isReadT7Code);
                IsReadT7Code = isReadT7Code;

                value = nodeRoot.Attribute("IsVerifyLaserMarker").Value;
                bool isVerifyLaserMarker;
                bool.TryParse(value, out isVerifyLaserMarker);
                IsVerifyLaserMarker = isVerifyLaserMarker;

                value = nodeRoot.Attribute("IsVerifyT7Code").Value;
                bool isVerifyT7Code;
                bool.TryParse(value, out isVerifyT7Code);
                IsVerifyT7Code = isVerifyT7Code;

                value = nodeRoot.Attribute("OrderBy").Value;
                OrderByMode orderBy;
                Enum.TryParse(value, out orderBy);
                OrderBy = orderBy;

                value = nodeRoot.Attribute("IsAlign").Value;
                bool isAlign;
                bool.TryParse(value, out isAlign);
                IsAlign = isAlign;

                value = nodeRoot.Attribute("AlignAngle").Value;
                double anlignAngle;
                double.TryParse(value, out anlignAngle);
                AlignAngle = anlignAngle;

                WaferReaderIndex = GetValue<int>(nodeRoot, "WaferReaderIndex");

                var transferItems = nodeRoot.Element("TransferTable").Elements("TransferItem");
                if (transferItems.Any())
                {
                    foreach (var item in transferItems)
                    {
                        ModuleName sourceChamberSet;
                        Enum.TryParse(item.Attribute("SourceStation").Value, out sourceChamberSet);
                        int sourceSlot;
                        int.TryParse(item.Attribute("SourceSlot").Value, out sourceSlot);
                        ModuleName destChamberSet;
                        Enum.TryParse(item.Attribute("DestinationStation").Value, out destChamberSet);
                        int destSlot;
                        int.TryParse(item.Attribute("DestinationSlot").Value, out destSlot);
                        bool readLaserMarker;
                        bool.TryParse(item.Attribute("IsReadLaserMarker").Value, out readLaserMarker);
                        bool readT7Code;
                        bool.TryParse(item.Attribute("IsReadT7Code").Value, out readT7Code);
                        bool verifyLaserMarker;
                        bool.TryParse(item.Attribute("IsVerifyLaserMarker").Value, out verifyLaserMarker);
                        bool verifyT7Code;
                        bool.TryParse(item.Attribute("IsVerifyT7Code").Value, out verifyT7Code);
                        bool align;
                        bool.TryParse(item.Attribute("IsAlign").Value, out align);
                        OrderByMode oBy;
                        Enum.TryParse(item.Attribute("OrderBy").Value, out oBy);

                        double angle;
                        double.TryParse(item.Attribute("AlignAngle").Value, out angle);

                        TransferItems.Add(new SorterRecipeTransferTableItem
                        {
                            SourceStation = sourceChamberSet,
                            SourceSlot = sourceSlot,
                            DestinationStation = destChamberSet,
                            DestinationSlot = destSlot,
                            IsReadLaserMarker = readLaserMarker,
                            IsReadT7Code = readT7Code,
                            IsVerifyLaserMarker = verifyLaserMarker,
                            IsVerifyT7Code = verifyT7Code,
                            IsAlign = align,
                            OrderBy = oBy,
                            AlignAngle = angle
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        private T GetValue<T>(XElement element, string Name)
        {
            var attr = element.Attribute(Name);
            if (attr != null)
            {
                return (T)Convert.ChangeType(attr.Value, typeof(T));
            }
            return default(T);
        }
    }
}