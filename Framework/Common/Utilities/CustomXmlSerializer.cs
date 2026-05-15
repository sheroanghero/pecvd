using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Soap;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace Aitex.Core.Util
{
        public class SerializeStatic
    {
        public static bool Save(Type static_class, string filename)
        {
            try
            {
                FieldInfo[] fields = static_class.GetFields(BindingFlags.Static | BindingFlags.Public);
                object[,] a = new object[fields.Length,2];
                int i = 0;
                foreach (FieldInfo field in fields)
                {
                    a[i, 0] = field.Name;
                    a[i, 1] = field.GetValue(null);
                    i++;
                };
                Stream f = File.Open(filename, FileMode.Create);
                SoapFormatter formatter = new SoapFormatter();                
                formatter.Serialize(f, a);
                f.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool Load(Type static_class, string filename)
        {
            try
            {
                FieldInfo[] fields = static_class.GetFields(BindingFlags.Static | BindingFlags.Public);                
                object[,] a;
                Stream f = File.Open(filename, FileMode.Open);
                SoapFormatter formatter = new SoapFormatter();
                a = formatter.Deserialize(f) as object[,];
                f.Close();
                if (a.GetLength(0) != fields.Length) return false;
                int i = 0;
                foreach (FieldInfo field in fields)
                {
                    if (field.Name == (a[i, 0] as string))
                    {
                        field.SetValue(null, a[i,1]);
                    }
                    i++;
                };                
                return true;
            }
            catch
            {
                return false;
            }
        }
    }


    public class CustomXmlSerializer
    {
        static public void Serialize<T>(T t, string fileName)
        {
            using (FileStream fileWriter = new FileStream(fileName, FileMode.Create))
            using (XmlWriter writer = XmlTextWriter.Create(fileWriter))
            {
                XmlSerializer serializer = new XmlSerializer(t.GetType());

                serializer.Serialize(writer, t);
                writer.Flush();
            }
        }
        static public string Serialize<T>(T t)
        {
            StringBuilder buffer = new StringBuilder();
            using (StringWriter writer = new StringWriter(buffer))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(writer, t);
            }

            return buffer.ToString();
        }

        static public T Deserialize<T>(string xmlString)
        {
            if (string.IsNullOrWhiteSpace(xmlString))
                throw new ArgumentNullException("xmlString");

            using (StringReader reader = new StringReader(xmlString))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(reader);
            }
        }

        static public T Deserialize<T>(Stream streamReader)
        {
            if (streamReader == null)
                throw new ArgumentNullException("streamReader");
            if (streamReader.Length <= 0)
                throw new EndOfStreamException("The stream is blank");

            using (XmlReader reader = XmlTextReader.Create(streamReader))
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(T));
                return (T)deserializer.Deserialize(reader);
            }
        }

        static public T Deserialize<T>(FileInfo fi)
        {
            if (fi == null)
                throw new ArgumentNullException("fi");
            if (!fi.Exists)
                throw new FileNotFoundException(fi.FullName);

            using (FileStream fileReader = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read))
                return Deserialize<T>(fileReader);
        }
    }
}