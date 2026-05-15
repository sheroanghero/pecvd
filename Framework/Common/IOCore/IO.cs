using System;
using System.Collections.Generic;

using Aitex.Core.RT.IOCore;
using Aitex.Core.Util;
using MECF.Framework.Common.IOCore;

namespace Aitex.Core.RT.IOCore
{
    public class Index<T> where T : class
    {
        public T this[string name]
        {
            get
            {
                return IoManager.Instance.GetIO<T>(name);
            }
        }
    }

    public static class IO
    {
        public static Index<DIAccessor> DI = new Index<DIAccessor>();
        public static Index<DOAccessor> DO = new Index<DOAccessor>();
        public static Index<AIAccessor> AI = new Index<AIAccessor>();
        public static Index<AOAccessor> AO = new Index<AOAccessor>();


        public static bool CanSetDO(string doName, bool onOff, out string reason)
        {
            return IoManager.Instance.CanSetDo(doName, onOff, out reason);
        }
 

        public static List<Tuple<int, int, string>> GetIONameList(string group, IOType ioType)
        {
            return IoManager.Instance.GetIONameList(group, ioType);
        }

        public static List<DIAccessor> GetDiList(string source)
        {
            return IoManager.Instance.GetDIList(source);
        }

        public static List<DOAccessor> GetDoList(string source)
        {
            return IoManager.Instance.GetDOList(source);
        }

        public static List<AIAccessor> GetAiList(string source)
        {
            return IoManager.Instance.GetAIList(source);
        }

        public static List<AOAccessor> GetAoList(string source)
        {
            return IoManager.Instance.GetAOList(source);
        }
    }     

}
