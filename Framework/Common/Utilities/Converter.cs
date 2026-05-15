using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Aitex.Core.Util
{
    public static class Converter
    {
        ///// <summary>
        ///// Unit converter from physical to logical in linear formula
        ///// </summary>
        ///// <returns></returns>
        public static double Phy2Logic(double physicalValue, double logicalMin, double logicalMax, double physicalMin, double physicalMax)
        {
            var ret = (logicalMax - logicalMin) * (physicalValue - physicalMin) / (physicalMax - physicalMin) + logicalMin;
            //if (ret < logicalMin) ret = logicalMin;
            //if (ret > logicalMax) ret = logicalMax;
            if (double.IsNaN(ret)) throw new DivideByZeroException("Phy2Logic除0操作");
            return ret;
        }

        ///// <summary>
        ///// Unit converter from logical to physical in linear formula
        ///// </summary>
        ///// <returns></returns>
        public static double Logic2Phy(double logicalValue, double logicalMin, double logicalMax, double physicalMin, double physicalMax)
        {
            var ret = (physicalMax - physicalMin) * (logicalValue - logicalMin) / (logicalMax - logicalMin) + physicalMin;
            if (ret < physicalMin) ret = physicalMin;
            if (ret > physicalMax) ret = physicalMax;
            if (double.IsNaN(ret)) 
                throw new DivideByZeroException("Logic2Phy除0操作");
            return ret;
        }

        /// <summary>
        /// 只按占用的字节转换，不检查范围
        /// </summary>
        /// <param name="high"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        public static float TwoUInt16ToFloat(UInt16 high, UInt16 low)
        {
            Int32 sum = (high << 16) + (low & 0xFFFF);
            byte[] bs = BitConverter.GetBytes(sum);
            return BitConverter.ToSingle(bs, 0);
        }

        public static int TwoInt16ToInt32(Int16 high, Int16 low)
        {
            UInt32 tmp = (ushort)low + (UInt32)(((ushort)high) << 16);
            return tmp > Int32.MaxValue ? (int)((~tmp + 1) * -1) : (int)tmp;////unsigned to signed
        }
 

        public static float TwoInt16ToFloat(Int16 high, Int16 low)
        {
            Int32 sum = (high << 16) + (low & 0xFFFF);
            byte[] bs = BitConverter.GetBytes(sum);
            return BitConverter.ToSingle(bs, 0);
        }

 
        public static byte SetBits(byte raw, byte value, int offset)
        {
            byte mask_1 = (byte)(value << offset);
            byte mask_2 = (byte)(~mask_1 & raw);
            return (byte) (mask_1 | mask_2);
        }

        public static byte SetBit(byte rawValue, int bitNumber, bool setValue)
        {
            if (bitNumber > 7 || bitNumber < 0)
                throw new ArgumentOutOfRangeException("bitNumber", "Must be 0 - 7");

            return setValue ?
                (byte)(rawValue | (1 << bitNumber)) :
                (byte)(rawValue & ~(1 << bitNumber));
        }

        public static ushort SetBit(ushort rawValue, int bitNumber, bool setValue)
        {
            if (bitNumber < 0 || bitNumber > 15)
                throw new ArgumentOutOfRangeException("bitNumber", "Must be 0 - 15");

            return setValue ?
                (ushort)(rawValue | (1 << bitNumber)) :
                (ushort)(rawValue & ~(1 << bitNumber));
        }

        public static bool GetBit(ushort rawValue, int bitNumber)
        {
            if (bitNumber < 0 || bitNumber > 15)
                throw new ArgumentOutOfRangeException("bitNumber", "Must be 0 - 15");

            return (rawValue & (1 << bitNumber)) > 0;
        }

        public static bool GetBit(byte rawValue, int bitNumber)
        {
            if (bitNumber < 0 || bitNumber > 7)
                throw new ArgumentOutOfRangeException("bitNumber", "Must be 0 - 7");

            return (rawValue & (1 << bitNumber)) > 0;
        }

    }
}
