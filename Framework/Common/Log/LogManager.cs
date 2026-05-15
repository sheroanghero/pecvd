using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.Util;
using log4net.Core;
using System.Diagnostics;
using System.IO;
using Aitex.Common.Util;
using Aitex.Core.RT.SCCore;

namespace Aitex.Core.RT.Log
{
    public class LogManager : ICommonLog
    {
        //PeriodicJob _threadWphLogs;
        PeriodicJob _loggingJob;
        FixSizeQueue<LogItem> _logQueue;
        LogWriter _writer;

        public LogManager()
        {

        }

        public void Initialize()
        {
            _logQueue = new FixSizeQueue<LogItem>(1000);
            _loggingJob = new PeriodicJob(300, this.PeriodicRun, "Save Log Job", true);
            _writer = new LogWriter();

            LOG.InnerLogger = this;


            //string s1 = System.Diagnostics.FileVersionInfo.GetVersionInfo(Path.Combine(PathManager.GetAppDir(), "EfemDualRT.exe")).ProductVersion;

            //LOG.Write($"RT {s1} launch ...");
        }
        private TypedLogWriter _logger;
        public void Initialize(string Type)
        {
            _logger = new TypedLogWriter(Type);
            //_threadWphLogs = new PeriodicJob(1000, OnWphLog, "WphLog", true);          
        }

        public bool LogWhplog(string textlog)
        {
            try
            {
                if(_logger !=null)
                    _logger.Write(textlog);
            }

            catch(Exception ex)
            {
                LOG.Write(ex);
            }
            return true;
        }




        //public bool OnWphLog()
        //{
        //    try
        //    {
        //        if (DateTime.Now.Hour < 23)
        //        {
        //            return true;
        //        }
        //        if (DateTime.Now.Minute < 59)
        //        {
        //            return true;
        //        }
        //        if (DateTime.Now.Second < 58)
        //        {
        //            return true;
        //        }
        //        int RunTime = SC.GetValue<int>("System.SystemAutoRunTime");
        //        int IdleTime = SC.GetValue<int>("System.SystemIdleTime");
        //        int AlarmTime = SC.GetValue<int>("System.SystemAlarmTime");
        //        int StrWph = SC.GetValue<int>("System.DayThroughput");
        //        int TimeNow = DateTime.Now.Second + (DateTime.Now.Hour * 60 + DateTime.Now.Minute) * 60;
        //        int StrUptime = (TimeNow - AlarmTime) * 100 / TimeNow;
        //        //int StrUtility = RunTime * 100 / (RunTime + IdleTime);
        //        int StrUtility = RunTime * 100 / TimeNow;
        //        _logger.Write($"System Uptime is {StrUptime}%, Throughput is {StrWph},Utility is {StrUtility}%");





        //    }
        //    catch (Exception ex)
        //    {
        //        LOG.Write(ex);
        //    }

        //    return true;
        //}
        public void Terminate()
        {
            try
            {
                if (_loggingJob != null)
                {
                    _loggingJob.Stop();
                    _loggingJob = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
        }

        public void Info(string message, bool isTraceOn, string stackFile)
        {
            CacheLog(message, Level.Info, null, isTraceOn, stackFile);
        }

        public void Warning(string message, string stackFile)
        {
            CacheLog(message, Level.Warn, null, true, stackFile);
        }

        public void Error(string message, string stackFile)
        {
            CacheLog(message, Level.Error, null, true, stackFile);
        }

        public void Warning(string message, Exception ex, string stackFile)
        {
            CacheLog(message, Level.Warn, ex, true, stackFile);
        }

        public void Error(string message, Exception ex, string stackFile)
        {
            CacheLog(message, Level.Error, ex, true, stackFile);
        }


        bool PeriodicRun()
        {
            LogItem item;
            while (_logQueue.TryDequeue(out item))
            {
                
                string log = _writer.Write(item);

            }

            return true;
        }

        void CacheLog(string message, Level level, Exception exception, bool isTraceOn, string stackFile)
        {
            //if (isTraceOn)
            //{
            //    System.Diagnostics.Trace.WriteLine(message + (exception == null ? "" : exception.Message));
            //}

            _logQueue.Enqueue(new LogItem(message, new StackTrace(true).GetFrame(5), level, exception, stackFile));
        }
    }
}
