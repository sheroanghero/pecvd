using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Aitex.Common.Util;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;

namespace MECF.Framework.Common.RecipeCenter
{
    public class DefaultSequenceFileContext:ISequenceFileContext
    {
        public string GetConfigXml()
        {
                try
                {
                    string configContent = PathManager.GetCfgDir() + @"\SequenceFormat.xml";

                    XmlDocument xmlDom = new XmlDocument();
                    xmlDom.Load(configContent);

                    CustomSequenceItem(xmlDom);

                    return xmlDom.OuterXml;
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                    return "";
                }
 
        }

        public virtual bool Validation(string content)
        {
            try
            {
 
                XmlDocument xmlDom = new XmlDocument();
                xmlDom.LoadXml(content);

                CustomValidation(xmlDom);

                return CustomValidation(xmlDom);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                EV.PostWarningLog("Recipe", "sequence file not valid, "+ex.Message);
                return false;
            }
        }

        public virtual void CustomSequenceItem(XmlDocument xmlContent)
        {

        }

        public virtual bool CustomValidation(XmlDocument xmlContent)
        {
            return true;
        }
    }
}
