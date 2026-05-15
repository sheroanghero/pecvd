using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace Aitex.Common.Util
{
    public class PathManager
    {
        /// <summary>
        /// Application directory
        /// </summary>
        /// <returns></returns>
        public static string GetAppDir()
        {
            return string.IsNullOrWhiteSpace(_appPath) ? _appPath = GetAppStartupDirectory() : _appPath;
        }

        /// <summary>
        /// Application config directory is relative to the directory of the currently running application.
        /// end with the "/"
        /// </summary>
        /// <returns></returns>
        public static string GetCfgDir()
        {
            return GetDirectory("Config");
        }


        /// <summary>
        /// Application's log directory is relative to the directory of the currently running application.
        /// </summary>
        /// <returns></returns>
        public static string GetLogDir()
        {
            return GetDirectory("Logs");
        }


        /// <summary>
        /// Application's process recipe directory
        /// </summary>
        /// <returns></returns>
        public static string GetRecipeDir()
        {
            return GetDirectory("Recipes");
        }

        /// <summary>
        /// Application's process recipe directory
        /// </summary>
        /// <returns></returns>
        public static string GetRecipeBackupDir()
        {

            return @"D:\Backup\Recipes\";
        }

        /// <summary>
        /// Application's account file path
        /// </summary>
        /// <returns></returns>
        public static string GetAccountFilePath()
        {
            return GetDirectory("Account");    
        }

        /// <summary>
        /// Application start up directory
        /// </summary>
        /// <returns></returns>
        static string GetAppStartupDirectory()
        {
            var startupPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string dir = Path.GetDirectoryName(startupPath);
            return dir;
        }

        public static string GetDirectory(string directoryPath)
        {
            var ret = Path.Combine(GetAppDir(), directoryPath);
            if (!ret.EndsWith(Path.DirectorySeparatorChar.ToString()))
                ret = ret + Path.DirectorySeparatorChar;

            if (!Directory.Exists(ret))
                Directory.CreateDirectory(ret);

            return ret;
        }

        static string _appPath;
    }
}
