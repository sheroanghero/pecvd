using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Aitex.Common.Util;

namespace MECF.Framework.Common.Utilities
{
    public class BinarySerializer<T> where T : new()
    {
        private static string pathName = PathManager.GetDirectory("Objects");

        private static Object thisLock = new Object();
 
        public static void ToStream(T stuff)
        {
            lock (thisLock)
            {
                if (!Directory.Exists(pathName))
                {
                    Directory.CreateDirectory(pathName);
                }
                var fileName = typeof(T).FullName;

                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(pathName + @"\" + fileName + @".obj", FileMode.Create);
                try
                {
                    using (stream)
                    {
                        formatter.Serialize(stream, stuff);
                        stream.Close();
                    }
                }
                catch (SerializationException e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }

                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }
        }

 
        public static void ToStream(T stuff, string fileName)
        {
            lock (thisLock)
            {
                if (!Directory.Exists(pathName))
                {
                    Directory.CreateDirectory(pathName);
                }

                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(pathName + @"\" + fileName + @".obj", FileMode.Create);
                try
                {
                    using (stream)
                    {
                        formatter.Serialize(stream, stuff);
                        stream.Close();
                    }
                }
                catch (SerializationException e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }

                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }
        }
 
        public static T FromStream()
        {
            lock (thisLock)
            {
                if (!Directory.Exists(pathName))
                {
                    Directory.CreateDirectory(pathName);
                }

                T obj = default(T);

                var fileName = typeof(T).FullName;

                var fullName = pathName + @"\" + fileName + @".obj";

                if (!File.Exists(fullName))
                {
                    return new T();
                }

                IFormatter formatter = new BinaryFormatter();

                Stream stream = new FileStream(fullName, FileMode.Open, FileAccess.Read);

                try
                {
                    using (stream)
                    {
                        obj = (T)formatter.Deserialize(stream);
                        stream.Close();
                    }

                    //ISubscriber subscriber = obj as ISubscriber;
                    //if (subscriber != null)
                    //{
                    //    subscriber.Subscribe();
                    //}
                }
                catch (SerializationException e)
                {
                    Console.WriteLine(e.Message);
                    //throw;
                }

                return obj;
            }
        }
 
        public static T FromStream(string fileName)
        {
            lock (thisLock)
            {
                if (!Directory.Exists(pathName))
                {
                    Directory.CreateDirectory(pathName);
                }

                T obj = default(T);

                var fullName = pathName + @"\" + fileName + @".obj";

                if (!File.Exists(fullName))
                {
                    return new T();
                }

                IFormatter formatter = new BinaryFormatter();

                Stream stream = new FileStream(fullName, FileMode.Open, FileAccess.Read);


                try
                {
                    using (stream)
                    {
                        obj = (T)formatter.Deserialize(stream);
                        stream.Close();
                    }
                }
                catch (SerializationException e)
                {
                    Console.WriteLine(e.Message);
                    //throw;
                }

                return obj;
            }
        }
    }
}
