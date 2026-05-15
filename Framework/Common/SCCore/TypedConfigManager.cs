using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Aitex.Common.Util;

namespace MECF.Framework.Common.SCCore
{
    /// <summary>
    /// 各种按类别区分的配置
    /// </summary>
    public class TypedConfigManager : Singleton<TypedConfigManager>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="typedXmlFile"></param>
        public void Initialize(Dictionary<string, string> typedXmlFile)
        {
            string fileDefault = string.Format("{0}\\config\\DataHistoryTypedConfig.default.xml", Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName));
            string fileCurrent = string.Format("{0}\\config\\DataHistoryTypedConfig.xml", Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName));

            if (!File.Exists(fileDefault) && !File.Exists(fileCurrent))
                throw new ApplicationException("没有找到DataHistoryTypedConfig配置文件.\r\n" + fileDefault + "\r\n或者" + fileCurrent);

            if (File.Exists(fileDefault))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(fileDefault);

                try
                {
                    //ParseNode(doc );
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }

            }

            if (File.Exists(fileCurrent))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(fileCurrent);

                try
                {
                    //ParseNode(doc );
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }
            }


        }


        public string GetTypedConfigContent(string type, string path)
        {
            if (type != "UserDefine")
            {
                return GetCustomConfigContent(type);
            }
            string fileCurrent = string.Format("{0}\\config\\DataHistoryTypedConfig.xml", Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName));
            if (!File.Exists(fileCurrent))
                return null;
            var doc = XDocument.Load(fileCurrent);
            var node = doc.Root.Elements().SingleOrDefault(x => x.Attribute("Type").Value == type);
            if (node == null)
                return null;

            List<string> content = new List<string>();
            foreach (var e in node.Elements().ToList())
            {
                content.Add(e.Attribute("Name").Value);
            }
            return string.Join(",", content.ToArray());
        }

        public void SetTypedConfigContent(string type, string path, string content)
        {
            if (type != "UserDefine")
            {
                SetCustomConfigContent(type, content);
                return;
            }

            string fileCurrent = string.Format("{0}\\config\\DataHistoryTypedConfig.xml", Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName));

            XmlDocument doc = new XmlDocument();

            if (File.Exists(fileCurrent))
            {
                doc.Load(fileCurrent);
            }
            else
            {
                doc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\" ?><TypedConfig></TypedConfig>");
            }

            var nodeRoot = doc.SelectSingleNode(string.Format("TypedConfig"));
            if (nodeRoot == null)
            {
                nodeRoot = doc.CreateElement("TypedConfig");
                doc.AppendChild(nodeRoot);
            }

            var node = doc.SelectSingleNode(string.Format("TypedConfig/Configs[@Type='{0}']", type)) as XmlElement;
            if (node == null)
            {
                node = doc.CreateElement("Configs");
                node.SetAttribute("Type", type);
                nodeRoot.AppendChild(node);
            }
            node.RemoveAll();
            node.SetAttribute("Type", type);

            var contentArrys = content.Split(',');
            foreach (var item in contentArrys)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    var newNode = doc.CreateElement("Config");
                    newNode.SetAttribute("Name", item);
                    node.AppendChild(newNode);
                }
            }

            doc.Save(fileCurrent);

        }


        #region DataGroup

        private bool IsXmlFileLoadable(string file)
        {
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(file);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return false;
            }

            return true;
        }



        public void InitCustomConfigFile(string type)
        {
            string typeConfigDefaultFile = PathManager.GetCfgDir() + $"{type}.xml";
            string typeConfigFile = PathManager.GetCfgDir() + $"_{type}.xml";

            try
            {
                if (!File.Exists(typeConfigFile))
                {
                    if (File.Exists(typeConfigDefaultFile))
                    {
                        File.Copy(typeConfigDefaultFile, typeConfigFile, true);
                    }
                    else
                    {
                        XmlDocument xml = new XmlDocument();

                        xml.LoadXml($"<?xml version=\"1.0\" encoding=\"utf-8\" ?><{type}Config></{type}Config>");

                        using (FileStream fsFileStream = new FileStream(typeConfigFile,
                            FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 1024, FileOptions.WriteThrough))
                        {
                            fsFileStream.SetLength(0);

                            XmlWriterSettings settings = new XmlWriterSettings();
                            settings.Indent = true;
                            settings.OmitXmlDeclaration = false;

                            using (XmlWriter xmlWrite = XmlWriter.Create(fsFileStream, settings))
                            {
                                xml.Save(xmlWrite);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }

        public string GetCustomConfigContent(string type)
        {
            InitCustomConfigFile(type);

            string typeConfigFile = PathManager.GetCfgDir() + $"_{type}.xml";

            if (!File.Exists(typeConfigFile))
                return "";

            StringBuilder s = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(typeConfigFile))
                {
                    while (!sr.EndOfStream)
                    {
                        s.Append(sr.ReadLine());
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return "";
            }
            return s.ToString();
        }

        public void SetCustomConfigContent(string type, string content)
        {

            try
            {
                string typeConfigFile = PathManager.GetCfgDir() + $"_{type}.xml";


                XmlDocument xml = new XmlDocument();

                xml.LoadXml(content);

                using (FileStream fsFileStream = new FileStream(typeConfigFile,
                    FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 1024, FileOptions.WriteThrough))
                {


                    fsFileStream.SetLength(0);

                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    settings.OmitXmlDeclaration = false;

                    using (XmlWriter xmlWrite = XmlWriter.Create(fsFileStream, settings))
                    {
                        xml.Save(xmlWrite);
                    }
                }

                LOG.Write($" {typeConfigFile} updated");
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

        }


        #endregion


    }
}