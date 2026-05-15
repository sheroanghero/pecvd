using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Aitex.Core.RT.Log;

namespace Aitex.Core.RT.Log
{
    public static class LOG
    {
        public static ICommonLog InnerLogger { set; private get; }

        public static void Info(string message, bool isTraceOn=false, int traceLevel=2, [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            if (InnerLogger != null)
                InnerLogger.Info(message, isTraceOn, GetFormatStackFrameInfo(traceLevel));
        }

        static string GetFormatStackFrameInfo(int traceLevel)
        {
            StackFrame sf = new StackTrace(true).GetFrame(traceLevel+1);
            string pathFile = sf.GetFileName();
            string file = string.IsNullOrEmpty(pathFile) ? "" : pathFile.Substring(pathFile.LastIndexOf('\\') + 1);
            return $"{file}\tLine {sf.GetFileLineNumber()}\t{sf.GetMethod().Name}()";
        }

        public static void Warning(string message, int traceLevel = 2, [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            if (InnerLogger != null)
                InnerLogger.Warning(message,  GetFormatStackFrameInfo(traceLevel));
        }

        public static void Warning(string message, int traceLevel = 2, [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0, params object[] args)
        {
            if (InnerLogger != null)
                InnerLogger.Warning(string.Format(message, args), GetFormatStackFrameInfo(traceLevel));
        }

        public static void Error(string message, int traceLevel = 2, [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            if (InnerLogger != null)
                InnerLogger.Error(message, GetFormatStackFrameInfo(traceLevel));
        }

        public static void Warning(string message, Exception ex, int traceLevel = 2, [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            if (InnerLogger != null)
                InnerLogger.Warning(message, ex, GetFormatStackFrameInfo(traceLevel));
        }

        public static void Error(string message, Exception ex, int traceLevel = 2, [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            
            if (InnerLogger != null)
                InnerLogger.Error(message, ex, GetFormatStackFrameInfo(traceLevel));
        }

        public static void Write(Exception ex, int traceLevel = 2, [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0)
        {
            Error("", ex, traceLevel+1);
        }

        public static void Write(Exception ex, string message, int traceLevel = 2, [CallerFilePath] string file = "",[CallerMemberName] string member = "",[CallerLineNumber] int line = 0)
        {
            Error(message, ex, traceLevel + 1);
        }

        public static void Write(string message, int traceLevel = 2, [CallerFilePath] string file = "",[CallerMemberName] string member = "",[CallerLineNumber] int line = 0)
        {
            Info(message, false, traceLevel + 1);
        }
    }
}
