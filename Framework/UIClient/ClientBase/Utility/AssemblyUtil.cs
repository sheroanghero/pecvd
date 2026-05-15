using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenSEMI.ClientBase.Utility
{
    public class AssemblyUtil
    {
        public static Type GetType(string typeName)
        {
            Type type = Type.GetType(typeName);
            return type;
        }

        public static object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }

        public static Assembly LoadAssembly(string assemblyName)
        {
            return Assembly.Load(assemblyName);
        }

        public static object CreateInstance(Assembly assembly, string typeName)
        {
            return assembly.CreateInstance(typeName);
        }

        public static string GetExecutePath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}
