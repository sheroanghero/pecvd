using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Common
{
    public class IOCommandReader
    {

        private static Dictionary<string, CommandModelConfig> _commandModelConfigDict = new Dictionary<string, CommandModelConfig>();

        public static CommandModelConfig GetCommandModelConfig(string hardwarePath, string hardwareName)
        {
            CommandModelConfig commandModelConfig = null;
            if (_commandModelConfigDict.Keys.Contains(hardwareName))
            {
                commandModelConfig = _commandModelConfigDict[hardwareName];
            }
            else
            {
                commandModelConfig = JsonConvert.DeserializeObject<CommandModelConfig>(File.ReadAllText(ConfigPath.HardwareUnitsPath + $"{hardwarePath}\\{hardwareName}\\IOCommands.json"));
                _commandModelConfigDict.Add(hardwareName, commandModelConfig);
            }
            return commandModelConfig;

        }
    }

    public class ConfigPath
    {
        public static string HardwareUnitsPath
        { 
            get
            {
                string path = Assembly.GetExecutingAssembly().Location;
                FileInfo finfo = new FileInfo(path);
                return finfo.DirectoryName + @"\\HardwareUnits\";
            }
        }
    }
}
