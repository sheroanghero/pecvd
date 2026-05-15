using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.Util
{
    public static class ArrayUtil
    {
        public static byte[] CombomBinaryArray(byte[] srcArray1, byte[] srcArray2)
        {
            //根据要合并的两个数组元素总数新建一个数组
            byte[] newArray = new byte[srcArray1.Length + srcArray2.Length];

            //把第一个数组复制到新建数组
            Array.Copy(srcArray1, 0, newArray, 0, srcArray1.Length);

            //把第二个数组复制到新建数组
            Array.Copy(srcArray2, 0, newArray, srcArray1.Length, srcArray2.Length);

            return newArray;
        }
    }
}
