using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MECF.Framework.Common.Equipment;

namespace Aitex.Sorter.Common
{


    public enum Motion
    {
        [EnumMember]
        Pick = 0,            //Lower arm

        [EnumMember]
        Place = 1,             //Upper arm

        [EnumMember]
        Exchange = 2,

        [EnumMember]
        Alignment = 3,

    }

    public enum Hand
    {
        [EnumMember]
        Blade1 = 0,            //Lower arm

        [EnumMember]
        Blade2 = 1,             //Upper arm

        [EnumMember]
        Both = 2,
    }

    public enum Axis
    {
        //‘S’ : Rotation axis.
        //‘A’ : Extension axis.
        //‘H’ : Wrist axis 1.
        //‘I’ : Wrist axis 2.
        //‘Z’ : Elevation axis.
        [EnumMember]
        S=0,
        [EnumMember]
        A=1,
        [EnumMember]
        H=2,
        [EnumMember]
        I=3,
        [EnumMember]
        Z=4
    }


    public enum MoveType
    {
        Move,     //single move
        Swap,     //Single swap

        SPSP,     //double move seperate pick, seperate place
        SPDP,     //double move seperate pick, double place 
        DPSP,     //double move. double pick, seperate place
        DPDP,     //double move, double pick, double place
    }



    [Flags]
    public enum MoveOption
    {
        None = 0,       //0000
        Align = 1,      //0001
        ReadID = 2,     //0010
        ReadID2 = 4,    //0100
        Reader1 = 8,    //1000
        Reader2 = 16,   //0001 0000
        LoadLock1 = 32, //0010 0000
        LoadLock2 = 64, //0100 0000
        LoadLock3 = 128,//1000 0000
        LoadLock4 = 256,//0001 0000 0000
        Buffer = 512,   //0010 0000 0000
        Turnover = 1024, //0100 0000 0000 
        LoadLock5 = 2048, //1000 0000 0000 
        LoadLock6 = 4096, //0001 1000 0000 0000 
        LoadLock7 = 8192, //0010 1000 0000 0000 
        LoadLock8 = 16384, //0100 1000 0000 0000 
        CoolingBuffer1 = 32768,
        CoolingBuffer2 = 65536,
        Aligner1 = 131072,
        Aligner2=262144,
    }
    public enum LMReadOption
    {
        LMRead1 = 1,
        LMRead2 = 2
    }


    [DataContract]
    public class TransferInfo
    {
        /// <summary>
        /// 源Wafer
        /// </summary>
        [DataMember]
        public string WaferID
        {
            get; set;
        }

        /// <summary>
        /// 源
        /// </summary>
        [DataMember]
        public ModuleName Source
        {
            get;
            set;
        }

        /// <summary>
        /// 源槽位
        /// </summary>
        [DataMember]
        public int SourceSlot
        {
            get;
            set;
        }

        /// <summary>
        /// 目的地
        /// </summary>
        [DataMember]
        public ModuleName Station
        {
            get; set;
        }

        /// <summary>
        /// 目的地槽位
        /// </summary>
        [DataMember]
        public int Slot
        {
            get; set;
        }

        [DataMember]
        public MoveOption Option
        {
            get; set;
        }
        [DataMember]
        public bool PreAlign
        {
            get;
            set;
        }

        [DataMember]
        public double Angle
        {
            get;
            set;
        }

        [DataMember]
        public bool VerifyAny
        {
            get;
            set;
        }

        [DataMember]
        public bool VerifyLaserMaker
        {
            get;
            set;
        }
        [DataMember]
        public bool VerifyLM1Checksum
        {
            get;
            set;
        }



        [DataMember]
        public string LaserMaker
        {
            get;
            set;
        }



        [DataMember]
        public bool VerifyT7Code
        {
            get;
            set;
        }
        [DataMember]
        public bool VerifyLM2Checksum
        {
            get;
            set;
        }
        [DataMember]
        public string T7Code
        {
            get;
            set;
        }
        [DataMember]
        public List<string> LM1JobFile
        { get; set; }
        [DataMember]
        public List<string> LM2JobFile
        { get; set; }
        [DataMember]
        public LMReadOption LM1Reader
        { get; set; }
        [DataMember]
        public LMReadOption LM2Reader
        { get; set; }
        [DataMember]
        public bool PostAlign
        {
            get;
            set;
        }
        [DataMember]
        public double PostAlignAngle
        {
            get;
            set;
        }
    }

    [Serializable]
    [DataContract]
    public class ManualTransferTask
    {
        [DataMember]
        public ModuleName SourceModule { get; set; }
        [DataMember]
        public int SourceSlotIndex { get; set; }
        [DataMember]
        public ModuleName DestModule { get; set; }
        [DataMember]
        public int DestSlotIndex { get; set; }
    }

    [Serializable]
    [DataContract]
    public class CjTransferTask
    {
        [DataMember]
        public ModuleName SourceModule { get; set; }
        [DataMember]
        public int SourceSlot { get; set; }

        [DataMember]
        public string SourcePostion { get; set; }

        [DataMember]
        public ModuleName DestModule { get; set; }
        [DataMember]
        public int DestSlot { get; set; }
        [DataMember]
        public string DestPosition { get; set; }

        [DataMember]
        public string Status { get; set; }

    }

}
