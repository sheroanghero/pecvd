using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Aitex.Core.Util
{
    public static class Utils
    {
        public static StringBuilder PrintLevel(IEnumerable<string> strList, char seperator = '.', string strDataType = "")
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine();
            string strdata = string.IsNullOrEmpty(strDataType) ? "Data" : strDataType;
            builder.Append($"{strdata}: ");

            var dict = strList.Select(o => o.Split(seperator)).GroupBy(o => o[0], v => v.ToArray()).ToDictionary(o => o.Key, v => v.ToList());
            _PrintLevel(0, dict, builder);
            return builder;
        }

        static void _PrintLevel(int level, Dictionary<string, List<string[]>> dict, StringBuilder builder)
        {
            foreach (string key in dict.Keys.OrderBy(o => o))
            {
                builder.AppendLine();
                builder.Append('\t', level + 1);
                builder.Append(key);

                var subDict = dict[key].Where(o => o.Length > level + 1).GroupBy(o => o[level + 1], v => v.ToArray()).ToDictionary(o => o.Key, v => v.ToList());
                _PrintLevel(level + 1, subDict, builder);
            }
        }

        public static T DeepClone<T>(T inputObj) where T : class
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, inputObj);
            ms.Seek(0, 0);
            object obj = bf.Deserialize(ms);
            ms.Close();

            return obj as T;
        }
    }
}
