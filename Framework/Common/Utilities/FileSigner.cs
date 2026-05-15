using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using Aitex.Core.RT.Log;

namespace Aitex.Core.Util
{
    public static class FileSigner
    {
        public static bool IsValid(string fileName)
        {
            bool retVal = false;

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(fileName);

                Debug.Assert(doc != null && doc.DocumentElement != null);

                // Get root element
                XmlElement elemRoot = doc.DocumentElement;

                // Get signature element
                XmlElement elemSignature = elemRoot["Signature"];
                if (elemSignature == null)
                {
                    return false; // The file was not signed.
                }

                // Remove signature element from document
                elemRoot.RemoveChild(elemSignature);

                // Calculate hash code from file (after removing Signature element)
                UnicodeEncoding ue = new UnicodeEncoding();

                SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();

                byte[] signature = sha1.ComputeHash(ue.GetBytes(elemRoot.InnerXml));

                string strSignature = Convert.ToBase64String(signature);

                // Add signature back to document
                elemRoot.AppendChild(elemSignature);

                // Compare embedded signature to calculated value
                if (elemSignature.InnerText == strSignature)
                {
                    retVal = true;
                }
            }
            catch (System.Exception e)
            {
                retVal = false;
                LOG.Write(e);
                
            }
            finally
            {
            }

            return retVal;
        }
        public static void Sign(string fileName)
        {
            try
            {
                XmlDocument doc = new XmlDocument();

                bool writeable = true;
                if (File.Exists(fileName) && (File.GetAttributes(fileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    writeable = false;
                    File.SetAttributes(fileName, FileAttributes.Normal);
                }

                doc.Load(fileName);

                XmlElement elemRoot = doc.DocumentElement;

                // Remove any existing signature
                XmlElement elemSignature = elemRoot["Signature"];
                if (elemSignature != null)
                {
                    elemRoot.RemoveChild(elemSignature);
                }

                // Calculate hash code (after removing Signature element)
                UnicodeEncoding ue = new UnicodeEncoding();

                SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();

                byte[] nSignature = sha1.ComputeHash(ue.GetBytes(elemRoot.InnerXml));

                string strSignature = Convert.ToBase64String(nSignature);

                // Add signature to XML document
                elemSignature = doc.CreateElement("Signature");
                elemSignature.InnerText = strSignature;
                elemRoot.AppendChild(elemSignature);
                doc.Save(fileName);
                if (!writeable)
                {
                    File.SetAttributes(fileName, FileAttributes.ReadOnly);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                
            }
            finally
            {
            }
        }
    }
}
