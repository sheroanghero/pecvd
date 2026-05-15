using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.IOCore;

namespace MECF.Framework.RT.Core.IoProviders
{
    public class IoProviderManager : Singleton<IoProviderManager>
    {
        private List<IIoProvider> _providers = new List<IIoProvider>();
        private Dictionary<string, IIoProvider> _dicProviders = new Dictionary<string, IIoProvider>();
        public List<IIoProvider> Providers => _providers;

        public IIoProvider GetProvider(string name)
        {
            if (_dicProviders.ContainsKey(name))
                return _dicProviders[name];
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlConfigFile">配置文件名，绝对路径</param>
        /// <param name="ioMappingPathFile">需要激活的IO Provider名字</param>
        public void Initialize(string xmlConfigFile, Dictionary<string, Dictionary<int, string>> ioMappingPathFile)
        {
            XmlDocument xml = new XmlDocument();

            try
            {
                xml.Load(xmlConfigFile);

                XmlNodeList nodeProviders = xml.SelectNodes("IoProviders/IoProvider");
                foreach (var nodeProvider in nodeProviders)
                {
                    XmlElement element = nodeProvider as XmlElement;
                    if (element == null)
                        continue;

                    XmlElement nodeParameter = element.SelectSingleNode("Parameter") as XmlElement;
                    if (nodeParameter == null)
                        continue;

                    string module = element.GetAttribute("module").Trim();
                    string name = element.GetAttribute("name").Trim();
                    string className = element.GetAttribute("class").Trim();
                    string assemblyName = element.GetAttribute("assembly").Trim();
                    string loadCondition = element.GetAttribute("load_condition").Trim();

                    bool isSimulator = SC.GetConfigItem("System.IsSimulatorMode").BoolValue;

                    //0 : Simualtor, 1:real 2： both
                    if (isSimulator && loadCondition!="0" && loadCondition != "2")
                    {
                        continue;
                    }

                    if (!isSimulator && loadCondition != "1" && loadCondition != "2")
                    {
                        continue;
                    }

                    string source = module + "." + name;

                    Type t = Assembly.Load(assemblyName).GetType(className);

                    if (t == null)
                    {
                        throw new Exception(string.Format("ioProvider config file class and assembly not valid,"+ source));
                    }

                    IIoProvider provider;
                    try
                    {
                        provider = (IIoProvider)Activator.CreateInstance(t);
                        _providers.Add(provider);
                        _dicProviders[source] = provider;
                    }
                    catch (Exception ex)
                    {
                        LOG.Write(ex);
                        throw new Exception(string.Format("ioProvider can not be created," + source));
                    }

                    List<IoBlockItem> lstBuffers = new List<IoBlockItem>();

                    XmlNodeList nodeBlocks = element.SelectNodes("Blocks/Block");
                    foreach (var nodeBlock in nodeBlocks)
                    {
                        XmlElement blockElement = nodeBlock as XmlElement;
                        if (blockElement == null)
                            continue;
                        IoBlockItem section = new IoBlockItem();
                        string type = blockElement.GetAttribute("type");
                        string offset = blockElement.GetAttribute("offset");
                        string size = blockElement.GetAttribute("size");
                        string value_type = blockElement.GetAttribute("value_type");

                        int result;
                        if (!int.TryParse(offset, out result))
                        {
                            continue;
                        }

                        section.Offset = result;

                        if (!int.TryParse(size, out result))
                        {
                            continue;
                        }

                        section.Size = (ushort)result;

                        switch (type.ToLower())
                        {
                            case "ai":
                                section.Type = IoType.AI;
                                break;
                            case "ao":
                                section.Type = IoType.AO;
                                break;
                            case "di":
                                section.Type = IoType.DI;
                                break;
                            case "do":
                                section.Type = IoType.DO;
                                break;
                            default:
                                continue;
                        }

                        if (section.Type == IoType.AI || section.Type == IoType.AO)
                        {
                            section.AIOType = (string.IsNullOrEmpty(value_type) || value_type.ToLower()!="float") ?  typeof(short) : typeof(float);
                        }
                        
                        lstBuffers.Add(section);
                    }

                    if (ioMappingPathFile.ContainsKey(source))
                    {
                        provider.Initialize(module, name, lstBuffers, IoManager.Instance, nodeParameter, ioMappingPathFile[source]);
                    }
                    else
                    {
                        throw new Exception(string.Format("can not find io map config files," + source));
                    }
 
                    provider.Start();
                }

            }
            catch (Exception ex)
            {
                throw  new ApplicationException("IoProvider configuration not valid," + ex.Message);
            }

             
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlConfigFile">配置文件名，绝对路径</param>
        public void Initialize(string xmlConfigFile)
        {
            XmlDocument xml = new XmlDocument();

            try
            {
                xml.Load(xmlConfigFile);

                XmlNodeList nodeProviders = xml.SelectNodes("IoProviders/IoProvider");
                foreach (var nodeProvider in nodeProviders)
                {
                    XmlElement element = nodeProvider as XmlElement;
                    if (element == null)
                        continue;

                    XmlElement nodeParameter = element.SelectSingleNode("Parameter") as XmlElement;
                    if (nodeParameter == null)
                        continue;

                    string module = element.GetAttribute("module").Trim();
                    string name = element.GetAttribute("name").Trim();
                    string className = element.GetAttribute("class").Trim();
                    string assemblyName = element.GetAttribute("assembly").Trim();
                    string loadCondition = element.GetAttribute("load_condition").Trim();

                    bool isSimulator = SC.GetConfigItem("System.IsSimulatorMode").BoolValue;

                    //0 : Simualtor, 1:real 2： both
                    if (isSimulator && loadCondition != "0" && loadCondition != "2")
                    {
                        continue;
                    }

                    if (!isSimulator && loadCondition != "1" && loadCondition != "2")
                    {
                        continue;
                    }

                    string source = module + "." + name;

                    Type t = Assembly.Load(assemblyName).GetType(className);

                    if (t == null)
                    {
                        throw new Exception(string.Format("ioProvider config file class and assembly not valid," + source));
                    }

                    IIoProvider provider;
                    try
                    {
                        provider = (IIoProvider)Activator.CreateInstance(t);
                        _providers.Add(provider);
                        _dicProviders[source] = provider;
                    }
                    catch (Exception ex)
                    {
                        LOG.Write(ex);
                        throw new Exception(string.Format("ioProvider can not be created," + source));
                    }

                    List<IoBlockItem> lstBuffers = new List<IoBlockItem>();

                    XmlNodeList nodeBlocks = element.SelectNodes("Blocks/Block");
                    foreach (var nodeBlock in nodeBlocks)
                    {
                        XmlElement blockElement = nodeBlock as XmlElement;
                        if (blockElement == null)
                            continue;
                        IoBlockItem section = new IoBlockItem();
                        string type = blockElement.GetAttribute("type");
                        string offset = blockElement.GetAttribute("offset");
                        string size = blockElement.GetAttribute("size");
                        string value_type = blockElement.GetAttribute("value_type");

                        int result;
                        if (!int.TryParse(offset, out result))
                        {
                            continue;
                        }

                        section.Offset = result;

                        if (!int.TryParse(size, out result))
                        {
                            continue;
                        }

                        section.Size = (ushort)result;

                        switch (type.ToLower())
                        {
                            case "ai":
                                section.Type = IoType.AI;
                                break;
                            case "ao":
                                section.Type = IoType.AO;
                                break;
                            case "di":
                                section.Type = IoType.DI;
                                break;
                            case "do":
                                section.Type = IoType.DO;
                                break;
                            default:
                                continue;
                        }

                        if (section.Type == IoType.AI || section.Type == IoType.AO)
                        {
                            section.AIOType = (string.IsNullOrEmpty(value_type) || value_type.ToLower() != "float") ? typeof(short) : typeof(float);
                        }


                        lstBuffers.Add(section);
                    }

                    string mapModule = element.GetAttribute("map_module").Trim();
                    string mapFile = element.GetAttribute("map_file").Trim();

                    FileInfo file = new FileInfo(xmlConfigFile);
                    string fullPathName = file.Directory.FullName + "\\" + mapFile;
 
                    provider.Initialize(module, name, lstBuffers, IoManager.Instance, nodeParameter, fullPathName, mapModule);

                    provider.Start();
                }

            }
            catch (Exception ex)
            {
                throw new ApplicationException("IoProvider configuration not valid," + ex.Message);
            }


        }





        /// <summary>
        /// 模块化的版本
        /// </summary>
        /// <param name="xmlConfigFile"></param>
        /// <param name="module"></param>
        /// <param name="name"></param>
        public void Initialize(string xmlConfigFile, string module, string name, bool filterModule=false)
        {
            XmlDocument xml = new XmlDocument();

            try
            {
                xml.Load(xmlConfigFile);

                XmlNodeList nodeProviders = xml.SelectNodes("IoProviders/IoProvider");
                foreach (var nodeProvider in nodeProviders)
                {
                    XmlElement element = nodeProvider as XmlElement;
                    if (element == null)
                        continue;

                    XmlElement nodeParameter = element.SelectSingleNode("Parameter") as XmlElement;
                    if (nodeParameter == null)
                        continue;

                    string className = element.GetAttribute("class").Trim();
                    string assemblyName = element.GetAttribute("assembly").Trim();
                    string loadCondition = element.GetAttribute("load_condition").Trim();

                    string configModule = element.GetAttribute("module").Trim();

                    if (filterModule &&!string.IsNullOrEmpty(configModule) && configModule!=module)
                        continue;

                    bool isSimulator = SC.GetConfigItem("System.IsSimulatorMode").BoolValue;

                    //0 : Simualtor, 1:real 2： both
                    if (isSimulator && loadCondition != "0" && loadCondition != "2")
                    {
                        continue;
                    }

                    if (!isSimulator && loadCondition != "1" && loadCondition != "2")
                    {
                        continue;
                    }

                    string source = module + "." + name;

                    Type t = Assembly.Load(assemblyName).GetType(className);

                    if (t == null)
                    {
                        throw new Exception(string.Format("ioProvider config file class and assembly not valid," + source));
                    }

                    IIoProvider provider;
                    try
                    {
                        provider = (IIoProvider)Activator.CreateInstance(t);
                        _providers.Add(provider);
                        _dicProviders[source] = provider;
                    }
                    catch (Exception ex)
                    {
                        LOG.Write(ex);
                        throw new Exception(string.Format("ioProvider can not be created," + source));
                    }

                    List<IoBlockItem> lstBuffers = new List<IoBlockItem>();

                    XmlNodeList nodeBlocks = element.SelectNodes("Blocks/Block");
                    foreach (var nodeBlock in nodeBlocks)
                    {
                        XmlElement blockElement = nodeBlock as XmlElement;
                        if (blockElement == null)
                            continue;
                        IoBlockItem section = new IoBlockItem();
                        string type = blockElement.GetAttribute("type");
                        string offset = blockElement.GetAttribute("offset");
                        string size = blockElement.GetAttribute("size");
                        string value_type = blockElement.GetAttribute("value_type");

                        int result;
                        if (!int.TryParse(offset, out result))
                        {
                            continue;
                        }

                        section.Offset = result;

                        if (!int.TryParse(size, out result))
                        {
                            continue;
                        }

                        section.Size = (ushort)result;

                        switch (type.ToLower())
                        {
                            case "ai":
                                section.Type = IoType.AI;
                                break;
                            case "ao":
                                section.Type = IoType.AO;
                                break;
                            case "di":
                                section.Type = IoType.DI;
                                break;
                            case "do":
                                section.Type = IoType.DO;
                                break;
                            default:
                                continue;
                        }

                        if (section.Type == IoType.AI || section.Type == IoType.AO)
                        {
                            section.AIOType = (string.IsNullOrEmpty(value_type) || value_type.ToLower() != "float") ? typeof(short) : typeof(float);
                        }


                        lstBuffers.Add(section);
                    }

                    string mapFile = element.GetAttribute("map_file").Trim();

                    FileInfo file = new FileInfo(xmlConfigFile);
                    string fullPathName = file.Directory.FullName + "\\" + mapFile;

                    provider.Initialize(module, name, lstBuffers, IoManager.Instance, nodeParameter, fullPathName, module);

                    provider.Start();
                }

            }
            catch (Exception ex)
            {
                throw new ApplicationException("IoProvider configuration not valid," + ex.Message);
            }


        }

        public void Terminate()
        {
            try
            {
                foreach (var ioProvider in _providers)
                {
                    ioProvider.Stop();
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        public void Reset()
        {
            try
            {
                foreach (var ioProvider in _providers)
                {
                    ioProvider.Reset();
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }
    }
}
