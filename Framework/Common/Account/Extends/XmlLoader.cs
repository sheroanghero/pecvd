using System.IO;
using System.Xml.Linq;

namespace MECF.Framework.Common.Account.Extends
{
    public abstract class XmlLoader
    {
        protected string m_strPath;
        protected XDocument m_xdoc;

        protected XmlLoader(string p_strPath)
        {
            this.m_strPath = p_strPath;
        }

        protected virtual void AnalyzeXml()
        {

        }

        public void Load()
        {
            if (File.Exists(this.m_strPath))
            {
                this.m_xdoc = XDocument.Load(this.m_strPath);
                this.AnalyzeXml();
            }
            else
                throw new FileNotFoundException("File " + this.m_strPath + " not be found");
        }

    }
}
