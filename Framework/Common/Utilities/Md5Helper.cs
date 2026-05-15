using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Aitex.Core.RT.Log;

namespace Aitex.Core.Utilities
{
    public class Md5Helper
    {

        /// <summary>
        /// md5 string compare
        /// </summary>
        /// <param name="input"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static bool VerifyMd5Hash(string input, string hash)
        {
            string hashOfInput = GetMd5Hash(input);
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            if (0 == comparer.Compare(hashOfInput, hash))
                return true;
            return false;
        }

        public static string GetMd5Hash(string input)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        public static string GenerateDynamicPassword(string serialNum)
        {
            try
            {
                string genString = "aitex" + serialNum + DateTime.Now.ToString("yyyyMMdd");
                string hash = Md5Helper.GetMd5Hash(genString);
                return hash.Substring(0, 8);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return "";
            }
        }

    }
}
