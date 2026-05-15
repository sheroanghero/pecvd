using System.Collections.Generic;

namespace MECF.Framework.UI.Client.ClientBase
{
    public static class ModuleManager
    {
        public static Dictionary<string, ModuleInfo> ModuleInfos { get; private set; }

        static ModuleManager()
        {
            ModuleInfos = new Dictionary<string, ModuleInfo>();
        }

        public static void Initialize(List<ModuleInfo> allModules)
        {
            ModuleInfos.Clear();

            foreach (var info in allModules)
            {
                ModuleInfos.Add(info.ModuleID, info);
            }
        }
    }
}
