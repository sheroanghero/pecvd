using Aitex.Common.Util;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using System;
using System.Diagnostics;

namespace MECF.Framework.Common.DataCenter
{
    public class DiskManager
    {
        PeriodicJob _threadMonitorDiskSpace;

        Stopwatch _sw = new Stopwatch();

        public DiskManager()
        {

            if (SC.ContainsItem("System.IsEnableMonitorDiskSpaceFunc"))
            {
                IsEnableMonitorDiskSpaceFunc = SC.GetValue<bool>("System.IsEnableMonitorDiskSpaceFunc");
            }

        }

        public bool IsEnableMonitorDiskSpaceFunc { get; set; }

        public void Run()
        {
            //30分钟运行一次，删除一个月之前的 log文件
            _threadMonitorDiskSpace = new PeriodicJob(1000 * 60 * 30, MonitorDiskSpace, "MonitorDiskSpace Thread", false);

            _sw.Start();

            //防止UI没启动，就进行EV.Post
            //Task.Delay(10000).ContinueWith((a) => _threadMonitorDiskSpace.Start()) ;
        }

        bool MonitorDiskSpace()
        {
            if (_sw.IsRunning && _sw.ElapsedMilliseconds < 10000)
                return true;

            _sw.Stop();

            try
            {
                long freeSpace = new long();
                long totalSpace = new long();
                string hardDiskName = PathManager.GetAppDir().Substring(0, 3);
                System.IO.DriveInfo[] drives = System.IO.DriveInfo.GetDrives();
                foreach (System.IO.DriveInfo drive in drives)
                {
                    if (drive.Name == hardDiskName)
                    {
                        freeSpace = drive.TotalFreeSpace;
                        totalSpace = drive.TotalSize;
                    }
                }

                if (freeSpace < totalSpace * 0.05)
                {
                    EV.PostAlarmLog("System", $"{hardDiskName} Hard disk Free Space is less than 5% ，need release");
                }
                else if (freeSpace < totalSpace * 0.1)
                {
                    EV.PostWarningLog("System", $"{hardDiskName} Hard disk Free Space is less than 10%，need release");
                }

            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;

        }

        public void Stop()
        {
            _threadMonitorDiskSpace.Stop();
        }

        ~DiskManager()
        {
            Stop();
        }

    }
}
