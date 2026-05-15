using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net.Core;
using System.Reflection;

namespace Aitex.Core.RT.Log
{
    class LogWriter
    {
        private readonly static Type ThisDeclaringType = typeof(LogWriter);
        private readonly ILogger defaultLogger;

        public LogWriter()
        {
            defaultLogger = log4net.Core.LoggerManager.GetLogger(Assembly.GetExecutingAssembly(), "CommonLogger");
        }

        string FormatLogString(LogItem logItem)
        {
            string fileName = string.Empty;
            string result = logItem.msg;
            try
            {

                //fileName = logItem.sf.GetFileName().Substring(logItem.sf.GetFileName().LastIndexOf('\\') + 1);

                //2014-10-17 17:53:51.9   INFO   Log.cs   Line:50 - Config模块初始化成功.
                var temp = logItem.sf.GetFileName();
                if (temp != null)
                {
                    fileName = temp.Substring(temp.LastIndexOf('\\') + 1);
                }

            }
            catch (Exception)
            {
                fileName = string.Empty;
            }

            try
            {
                //result = string.Format("{0} LOG.{1} {2} Line:{3} {4}() - {5}", logItem.dt.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                //                                    logItem.lv.Name,
                //                                    fileName,
                //                                    logItem.sf.GetFileLineNumber().ToString(),
                //                                    logItem.sf.GetMethod().Name,
                //                                    logItem.msg);

                //result = string.Format("{0}\t{1}\t{2}\t{3}", logItem.dt.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                //    logItem.lv.Name,
                //    logItem.StackFile,
                //    logItem.msg);

                result = string.Format("{0}\t{1}\t{2} ", logItem.dt.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                    logItem.lv.Name,
                     
                    logItem.msg);
            }
            catch (Exception)
            {
            }

            return result;
        }

        public string Write(LogItem logItem)
        {
            string log = FormatLogString(logItem);
            if (defaultLogger.IsEnabledFor(logItem.lv))
            {
                defaultLogger.Log(typeof(LogWriter), logItem.lv, log, logItem.ex);
            }
            return log;
        }
    }
    public class TypedLogWriter
    {
        private readonly ILogger _logger;

        public TypedLogWriter(string type)
        {
            _logger = log4net.Core.LoggerManager.GetLogger(Assembly.GetExecutingAssembly(), type);
        }

        public void Write(string text)
        {
            _logger.Log(typeof(TypedLogWriter), Level.Info, text, null);
        }
    }
}
