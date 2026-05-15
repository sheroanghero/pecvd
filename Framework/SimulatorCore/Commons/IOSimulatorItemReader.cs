using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Simulator.Core.Commons
{
    public class IOSimulatorItemReader
    {

        private static Dictionary<string, IOSimulatorItemViewModelConfig> _commandModelConfigDict = new Dictionary<string, IOSimulatorItemViewModelConfig>();

        public static IOSimulatorItemViewModelConfig GetCommandModelConfig(string hardwarePath, string hardwareName)
        {
            IOSimulatorItemViewModelConfig commandModelConfig = null;
            if (_commandModelConfigDict.Keys.Contains(hardwareName))
            {
                commandModelConfig = _commandModelConfigDict[hardwareName];
            }
            else
            {
                commandModelConfig = JsonConvert.DeserializeObject<IOSimulatorItemViewModelConfig>(File.ReadAllText(ConfigPath.AssemblyPath + $"\\{hardwarePath}\\{hardwareName}\\IOSimulatorItems.json"));
                _commandModelConfigDict.Add(hardwareName, commandModelConfig);
            }
            return commandModelConfig;
        }
        public static IOSimulatorItemViewModelConfig GetCommandModelConfigV2(string hardwareName)
        {
            IOSimulatorItemViewModelConfig commandModelConfig = null;
            if (_commandModelConfigDict.Keys.Contains(hardwareName))
            {
                commandModelConfig = _commandModelConfigDict[hardwareName];
            }
            else
            {
                commandModelConfig = JsonConvert.DeserializeObject<IOSimulatorItemViewModelConfig>(File.ReadAllText(ConfigPath.AssemblyPath + $"\\SimulatorItems\\{hardwareName}_IOSimulatorItems.json"));
                _commandModelConfigDict.Add(hardwareName, commandModelConfig);
            }
            return commandModelConfig;
        }
    }

    public class ConfigPath
    {
        public static string AssemblyPath
        {
            get
            {
                string path = Assembly.GetExecutingAssembly().Location;
                FileInfo finfo = new FileInfo(path);
                return finfo.DirectoryName;
            }
        }
    }
}
