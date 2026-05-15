using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class SystemExtension
    {
        public static string ToSingleString(this IEnumerable<string> strList, char seperator = ';')
        {
            if (strList == null || strList.Count() == 0) return string.Empty;
            bool isFirst = true;
            string strRet = string.Empty;
            foreach (string str in strList)
            {
                if (isFirst)
                {
                    strRet += string.IsNullOrEmpty(str) ? string.Empty : str;
                    isFirst = false;
                    continue;
                }
                strRet += seperator + (string.IsNullOrEmpty(str) ? string.Empty : str);
            }
            return strRet;
        }

        #region double
        public static bool AreEqual(this double value1, double value2, double torrence = 1E-6)
        {
            return (value1 == value2)
                || Math.Abs(value1 - value2) < torrence;
        }

        public static bool GreaterThan(this double value1, double value2, double torrence = 1E-6)
        {
            return ((value1 > value2) && !AreEqual(value1, value2, torrence));
        }

        public static bool GreaterThanOrEqual(this double value1, double value2, double torrence = 1E-6)
        {
            return (value1 > value2) || AreEqual(value1, value2, torrence);
        }

        public static bool IsZero(this double value, double torrence = 1E-6)
        {
            return (Math.Abs(value) < torrence);
        }

        public static bool LessThan(this double value1, double value2, double torrence = 1E-6)
        {
            return ((value1 < value2) && !AreEqual(value1, value2, torrence));
        }

        public static bool LessThanOrEqual(this double value1, double value2, double torrence = 1E-6)
        {
            return (value1 < value2) || AreEqual(value1, value2, torrence);
        }

        /// <summary>
        /// 四舍五入
        /// </summary>
        /// <param name="target"></param>
        /// <param name="multiple">... 0.01,0.1,1,10,100,1000 ...</param>
        /// <returns></returns>
        public static double HalfAdjust(this double target, double multiple = 1)
        {
            double temp = target / multiple;

            double intPart = temp < 0 ? Math.Ceiling(temp) : Math.Floor(temp);

            double decimalPart = Math.Abs(temp - intPart);

            if (decimalPart >= 0.5)
            {
                if (temp < 0)
                    intPart--;
                else intPart++;
            }

            return intPart * multiple;
        }
        #endregion double

        #region float
        public static bool AreEqual(this float value1, float value2, double torrence = 1E-6)
        {
            return (value1 == value2)
                || Math.Abs(value1 - value2) < torrence;
        }

        public static bool GreaterThan(this float value1, float value2, double torrence = 1E-6)
        {
            return ((value1 > value2) && !AreEqual(value1, value2, torrence));
        }

        public static bool GreaterThanOrEqual(this float value1, float value2, double torrence = 1E-6)
        {
            return (value1 > value2) || AreEqual(value1, value2, torrence);
        }

        public static bool IsZero(this float value, double torrence = 1E-6)
        {
            return (Math.Abs(value) < torrence);
        }

        public static bool LessThan(this float value1, float value2, double torrence = 1E-6)
        {
            return ((value1 < value2) && !AreEqual(value1, value2, torrence));
        }

        public static bool LessThanOrEqual(this float value1, float value2, double torrence = 1E-6)
        {
            return (value1 < value2) || AreEqual(value1, value2, torrence);
        }

        /// <summary>
        /// 四舍五入
        /// </summary>
        /// <param name="target"></param>
        /// <param name="multiple">... 0.01,0.1,1,10,100,1000 ...</param>
        /// <returns></returns>
        public static float HalfAdjust(this float target, float multiple = 1)
        {
            float temp = target / multiple;

            float intPart = temp < 0 ? (float)Math.Ceiling(temp) : (float)Math.Floor(temp);

            float decimalPart = Math.Abs(temp - intPart);

            if (decimalPart >= 0.5)
            {
                if (temp < 0)
                    intPart--;
                else intPart++;
            }

            return intPart * multiple;
        }
        #endregion float
    }
}
