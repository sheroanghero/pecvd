using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Utilities
{
    public class PerformanceMonitor
    {
        private PeriodicJob _performanceThread;

        public void Initialize()
        {
            _performanceThread = new PeriodicJob(60 * 1000, TracePerformanceLog, "PerformanceMonitor", true);
        }

        public bool TracePerformanceLog()
        {
            int memoryUsedRate = (int)GetMemoryUsedRate();
            LOG.Write($"Total Memory Used Rate: {memoryUsedRate}%");

            if (memoryUsedRate > 90)
            {
                EV.PostAlarmLog("System", $"Memory Used Rate is more than 90% ，need release");
            }
            else if (memoryUsedRate > 80)
            {
                EV.PostWarningLog("System", $"Memory Used Rate is more than 80% ，need release");
            }

            if (SC.ContainsItem($"System.EnablePerformanceMonitor") && SC.GetValue<bool>($"System.EnablePerformanceMonitor"))
            {
                Action act = () =>
                {
                    try
                    {
                        Process[] processes = Process.GetProcesses();
                        processes = processes.OrderByDescending(m => m.PrivateMemorySize64).ToArray();

                        StringBuilder sb = new StringBuilder();

                        sb.Append("【System Performance Monitor Report】\r\n");

                        sb.Append(string.Format("{0,30}", "ProcessName"));

                        sb.Append(string.Format("{0,10}", "PID"));

                        sb.Append(string.Format("{0,10}", "Priority"));

                        sb.Append(string.Format("{0,10}", "Handle"));

                        sb.Append(string.Format("{0,10}", "Thread"));

                        sb.Append(string.Format("{0,15}", "PriMemory(MB)"));

                        sb.Append(string.Format("{0,15}", " WorkingSet(MB)\r\n"));

                        for (int i = 0; i < processes.Length - 1; i++)
                        {
                            var process = processes[i];
                            sb.Append(string.Format("{0,30}", process.ProcessName));
                            sb.Append(string.Format("{0,10}", Convert.ToString(process.Id.ToString("0000"))));
                            sb.Append(string.Format("{0,10}", process.BasePriority));
                            sb.Append(string.Format("{0,10}", process.HandleCount));
                            sb.Append(string.Format("{0,10}", process.Threads.Count));
                            sb.Append(string.Format("{0,15}", (process.PrivateMemorySize64 / 1024.0 / 1024.0).ToString("F1"))); ;
                            sb.Append(string.Format("{0,15}", (process.WorkingSet64 / 1024.0 / 1024.0).ToString("F1"))); sb.Append("\r\n");
                        }

                        LOG.Write(sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        LOG.Write(ex);
                    }
                };

                act.BeginInvoke(null, null);
            }

            return true;
        }

        #region 获取内存使用率

        internal static long? GetMemoryAvailable()
        {
            const int MbDiv = 1024 * 1024;
            long availablebytes = 0;
            var managementClassOs = new ManagementClass("Win32_OperatingSystem");
            foreach (var managementBaseObject in managementClassOs.GetInstances())
                if (managementBaseObject["FreePhysicalMemory"] != null)
                    availablebytes = 1024 * long.Parse(managementBaseObject["FreePhysicalMemory"].ToString());
            return availablebytes / MbDiv;
        }

        internal static double? GetMemoryUsed()
        {
            float? PhysicalMemory = GetPhysicalMemory();
            float? MemoryAvailable = GetMemoryAvailable();
            double? MemoryUsed = (double?)(PhysicalMemory - MemoryAvailable);
            double currentMemoryUsed = (double)MemoryUsed;
            return currentMemoryUsed;
        }

        private static long? GetPhysicalMemory()
        {
            //获得物理内存
            const int MbDiv = 1024 * 1024;
            var managementClass = new ManagementClass("Win32_ComputerSystem");
            var managementObjectCollection = managementClass.GetInstances();
            foreach (var managementBaseObject in managementObjectCollection)
                if (managementBaseObject["TotalPhysicalMemory"] != null)
                    return long.Parse(managementBaseObject["TotalPhysicalMemory"].ToString()) / MbDiv;
            return null;
        }

        public static double GetMemoryUsedRate()
        {
            float? PhysicalMemory = GetPhysicalMemory();
            float? MemoryAvailable = GetMemoryAvailable();
            double? MemoryUsedRate = (double?)(PhysicalMemory - MemoryAvailable) / PhysicalMemory;
            return MemoryUsedRate.HasValue ? Convert.ToDouble(MemoryUsedRate * 100) : 0;
        }

        #endregion
    }
}
