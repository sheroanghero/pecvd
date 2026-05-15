using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.Core.IoProviders
{
    public partial class MCProtocol
    {
        ////
        ///
        ///
        /// Melsec Protocol QnA compatible 3E frame Constant and Message Format Definition
        ///
        ///
        ///
        /// 

        public const int CMD_BATCH_READ = 0x0401;
        public const int CMD_BATCH_WRITE = 0x1401;
        public const int CMD_RANDOM_READ = 0x0403;
        public const int CMD_RANDOM_WRITE = 0x1402;
        public const int SUB_CMD_WORD_UNITS = 0;
        public const int SUB_CMD_BYTE_UNITS = 1;

        public static readonly int JUNK_SIZE = Marshal.SizeOf(typeof(ushort)); // RESERVED

        static public readonly int PLCDataSize = Marshal.SizeOf(typeof(ushort));
        static public readonly int PLCDataBits = PLCDataSize * 8;
        static public readonly int BufDataSize = Marshal.SizeOf(typeof(byte));
        static public readonly int BufDataBits = BufDataSize * 8;
        static public readonly int IntSize = Marshal.SizeOf(typeof(int));
        static public readonly int IntBits = IntSize * 8;

        static public readonly int ShortSize = Marshal.SizeOf(typeof(ushort));

        // List of Commands and Functions for the QnA Compatible 3E Frame Device Memory Read/Write
        // refer to manual 3-62
        public const ushort MC_COMMAND_MULTIPLE_BATCH_READ = 0x0406;
        public const ushort MC_COMMAND_MULTIPLE_BATCH_WRITE = 0x1406;
        public const ushort MC_COMMAND_BATCH_READ = 0x0401;
        public const ushort MC_COMMAND_BATCH_WRITE = 0x1401;
        public const ushort MC_COMMAND_RANDOM_READ = 0x0403;
        public const ushort MC_COMMAND_RANDOM_WRITE = 0x1402;
        public const ushort MC_COMMAND_MONITOR_DATA_REGISTRATION = 0x0801;
        public const ushort MC_COMMAND_MONITOR = 0x0802;

        public const ushort MC_SUBCOMMAND_BIT_UNITS = 0x0001;
        public const ushort MC_SUBCOMMAND_WORD_UNITS = 0x0000;

        // List of Device code for QnACPU
        // refer to manual 3-70
        public const byte MC_DEVICE_CODE_INPUT_BIT = 0x9C;
        public const byte MC_DEVICE_CODE_OUTPUT_BIT = 0x0D;
        public const byte MC_DEVICE_CODE_INTERNAL_RELAY_BIT = 0x90;
        public const byte MC_DEVICE_CODE_LATCH_RELAY_BIT = 0x92;
        public const byte MC_DEVICE_CODE_ANNUNCIATOR_BIT = 0x93;
        public const byte MC_DEVICE_CODE_EDGE_RELAY_BIT = 0x94;
        public const byte MC_DEVICE_CODE_LINK_RELAY_BIT = 0xA0;
        public const byte MC_DEVICE_CODE_DATA_REGISTER_WORD = 0xA8;
        public const byte MC_DEVICE_CODE_LINK_REGISTER_WORD = 0xB4;

        // refer to manual 3-4
        public const ushort MC_SUBHEADER_COMMAND_MESSAGE = 0x0050;
        public const ushort MC_SUBHEADER_RESPONSE_MESSAGE = 0x00D0;

        // refer to manual 3-14
        public const ushort MC_REQUEST_MODULE_IO_NUMBER = 0x03FF;
        public const byte MC_REQUEST_MODULE_STATION_NUMBER = 0x00;
        public static readonly ushort MC_CPU_MONITOR_TIMER = 0x0008; // Wait time (Unit: 250ms)
        public const ushort MC_COMPLETE_CODE_SUCCESS = 0x0000;

        // User Defined Constant
        public static readonly ushort MC_NONE_DESIGNATE_DATA_LENGTH = 0x0000;
        public static readonly int MC_SUBHEADER_SIZE = Marshal.SizeOf(MC_SUBHEADER_COMMAND_MESSAGE);
        public static readonly int MC_QHEADER_COMMAND_SIZE = Marshal.SizeOf(typeof(MC_COMMAND_HEADER));
        public static readonly int MC_QHEADER_RESPONSE_SIZE = Marshal.SizeOf(typeof(MC_RESPONSE_HEADER));
        public static readonly int MC_BATCH_COMMAND_SIZE = Marshal.SizeOf(typeof(MC_BATCH_COMMAND));
        //public static readonly int MC_POINT_BITS = sizeof(ushort) * 8;
        //public const int MC_CA_WRITE_SIZE = 1024;
        //public const int MC_COMMAND_DATA_SIZE = 1024;
        //public const int MC_IO_BUFFER_LENGTH = 1024;
        // Command Message Format
        //[Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        //public struct MC_DATA_FORMAT
        //{
        //    public MC_HEADER mc_Header;
        //    public MC_APPLICATION_DATA mc_ApplicationData;
        //};

        //[Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        //public struct MC_HEADER // Ethernet module adds and deletes the header
        //{
        //};

        //[Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        //public struct MC_APPLICATION_DATA
        //{
        //    public MC_SUBHEADER mc_SubHeader;
        //    public MC_TEXT mc_Text;
        //};

        //[Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        //public struct MC_SUBHEADER  // 2 bytes (3E frame)
        //{
        //    public enum SUBHEADER_TYPE : ushort
        //    {
        //        MC_COMMAND_MESSAGE = 0x0050,
        //        MC_RESPONSE_MESSAGE = 0x00D0
        //    };

        //    public SUBHEADER_TYPE mc_SubHeaderType;
        //}

        //[Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        //public struct MC_TEXT   // Text Command or Text Response
        //{
        //}

        //public const byte MC_LOCAL_STATION_MCSocket_NUMBER = 0x00;
        //public const byte MC_LOCAL_STATION_PC_NUMBER = 0xFF;

        // refer to manual 3-7
        //[Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        //public struct MC_QHEADER_COMMAND
        //{
        //    public byte nMCSocketNo;
        //    public byte nStationNo;
        //    public ushort nRequestIONumber;
        //    public byte nRequestStationNumber;
        //    public ushort nRequestDataLength;
        //    public ushort nCPUMonitorTimer;
        //}

        [Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MC_COMMAND_HEADER
        {
            public ushort ProtocolID; // MC_COMMAND_MESSAGE or MC_RESPONSE_MESSAGE
            public byte NetworkID;
            public byte StationID;
            public ushort RequestIONumber;
            public byte RequestStationNumber;
            public ushort RequestDataLen;
            public ushort CPUMonitorTimer;
        };// MC_COMMAND_HEADER;

        [Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MC_RESPONSE_HEADER
        {
            public ushort ProtocolId; //nSubheader
            public byte NetworkId;  //SocketNumber
            public byte StationId;
            public ushort RequestIoNumber;
            public byte RequestStationNumber;
            public ushort ResponseDataLen;
            public ushort CompleteCode;
        };

        //[Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        //public struct MC_QHEADER_RESPONSE
        //{
        //    public ushort nSubHeader;
        //    public byte nMCSocketNo;
        //    public byte nStationNo;
        //    public ushort nRequestIONumber;
        //    public byte nRequestStationNumber;
        //    public ushort nResponseDataLength;
        //    public ushort nCompleteCode;
        //}

        [Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MC_BATCH_COMMAND
        {
            public ushort Command;
            public ushort SubCommand;
            public ushort HeadAddr;
            public byte Reserved;
            public byte DeviceCode;
            public ushort DevicePoints;
        }

        //[Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        //public struct MC_HEAD_DEVICE
        //{
        //    public byte bLow;
        //    public byte bMid;
        //    public byte bHig;

        //    public int HeadDevice
        //    {
        //        get
        //        {
        //            return (bLow | bMid << sizeof(byte) | bHig << sizeof(byte) * 2);
        //        }
        //        set
        //        {
        //            bLow = (byte)(0x00 | value);
        //            bMid = (byte)(0x00 | value >> sizeof(byte));
        //            bHig = (byte)(0x00 | value >> sizeof(byte) * 2);
        //        }
        //    }
        //}

        //[Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        //public struct MC_CA_RANDOM_WRITE_BIT
        //{
        //    public ushort nCommand;
        //    public ushort nSubCommand;
        //    public byte nBitPoints;

        //    public DATA_RANDOM_WRITE_BIT[] data;

        //    [Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
        //    public struct DATA_RANDOM_WRITE_BIT
        //    {
        //        public ushort nHeadDeviceAddr;
        //        public byte nReserved;
        //        public byte nDeviceCode;
        //        public byte nSet;
        //    }

        //    byte[] ToByteArray()    // can serialize with pointer type?
        //    {
        //        byte[] buffer = new byte[0];

        //        return buffer;
        //    }
        //};



        public static byte[] TransBoolArrayToByteData(bool[] value)
        {
            int length = (value.Length + 1) / 2;
            byte[] buffer = new byte[length];

            for (int i = 0; i < length; i++)
            {
                if (value[i * 2 + 0])
                    buffer[i] += 0x10;
                if ((i * 2 + 1) < value.Length)
                {
                    if (value[i * 2 + 1])
                        buffer[i] += 0x01;
                }
            }

            return buffer;
        }

        public static bool[] TransByteDataToBoolArray(byte[] value, int offset, int size)
        {
            int count = size * 2;
            bool[] result = new bool[count];
            for (int i = 0; i < size; i++)
            {
                if ((value[i] & 0x10) == 0x10)
                {
                    result[i * 2 + 0] = true;
                }

                if ((value[i] & 0x01) == 0x01)
                {
                    result[i * 2 + 1] = true;
                }
            }

            return result;
        }

        public static T ToStruct<T>(byte[] by) where T : struct
        {
            int objectSize = Marshal.SizeOf(typeof(T));
            if (objectSize > by.Length) return default(T);

            // Allocate some unmanaged memory.
            IntPtr buffer = Marshal.AllocHGlobal(objectSize);

            // Copy the read byte array (byte[]) into the unmanaged memory block.
            Marshal.Copy(by, 0, buffer, objectSize);

            // Push the memory into a new struct of type (T).
            T returnStruct = (T)Marshal.PtrToStructure(buffer, typeof(T));

            // Free the unmanaged memory block.
            Marshal.FreeHGlobal(buffer);

            return returnStruct;
        }

        public static object ToStruct(byte[] buffer, Type t)
        {
            int objectSize = Marshal.SizeOf(t);
            if (objectSize > buffer.Length) return null;

            // Allocate some unmanaged memory.
            IntPtr buf = Marshal.AllocHGlobal(objectSize);

            // Copy the read byte array (byte[]) into the unmanaged memory block.
            Marshal.Copy(buffer, 0, buf, objectSize);

            // Push the memory into a new struct of type (T).
            object result = Marshal.PtrToStructure(buf, t);

            // Free the unmanaged memory block.
            Marshal.FreeHGlobal(buf);

            return result;
        }

        public static byte[] Struct2Bytes(object o)
        {
            // create a new byte buffer the size of your struct
            byte[] buffer = new byte[Marshal.SizeOf(o)];
            // pin the buffer so we can copy data into it w/o GC collecting it
            GCHandle bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            // copy the struct data into the buffer
            Marshal.StructureToPtr(o, bufferHandle.AddrOfPinnedObject(), false);
            // free the GC handle
            bufferHandle.Free();

            return buffer;
        }

        public static byte[] Ushort2Byte(ushort[] data)
        {
            int sizeofT1 = Marshal.SizeOf(typeof(ushort));
            int sizeofT2 = Marshal.SizeOf(typeof(byte));

            byte[] by = new byte[sizeofT1 * data.Length / sizeofT2];
            for (int i = data.Length; i-- > 0;)
            {
                BitConverter.GetBytes(data[i]).CopyTo(by, i * 2);
            }
            return by;
        }
        public static byte[] Ushort2Byte(ushort[] data, int offset, int length)
        {
            int sizeofT1 = Marshal.SizeOf(typeof(ushort));
            int sizeofT2 = Marshal.SizeOf(typeof(byte));

            byte[] by = new byte[sizeofT1 * length / sizeofT2];
            
            for (int i = offset+length; i-- > offset;)
            {
                var value = BitConverter.GetBytes(data[i]);
                value.CopyTo(by, (i-offset) * 2);
            }
            return by;
        }
        public static ushort[] Byte2Ushort(byte[] data)
        {
            return Byte2Ushort(data, 0, data.Length);
        }

        public static ushort[] Byte2Ushort(byte[] data, int offset, int length)
        {
            System.Diagnostics.Debug.Assert(data.Length % 2 == 0);

            int sizeofT1 = Marshal.SizeOf(typeof(byte));
            int sizeofT2 = Marshal.SizeOf(typeof(ushort));

            ushort[] us = new ushort[sizeofT1 * length / sizeofT2];
            for (int i = 0; i < us.Length; i++)
            {
                us[i] = BitConverter.ToUInt16(data, offset + i * 2);
            }
            return us;
        }


    }



}
