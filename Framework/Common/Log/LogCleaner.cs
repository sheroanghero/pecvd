using Aitex.Common.Util;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.Log
{
    public class LogCleaner
    {
        PeriodicJob _threadDeleteLogs;

        /// <summary>
        /// LogCleaner 构造函数
        /// 启用SC参数
        /// "System.LogsSaveDays"
        /// "System.IsEnableCompressLogFileFunc"
        /// "System.SingleLogFileMaxSize"
        /// </summary>
        public LogCleaner()
        {
            if (SC.ContainsItem("System.LogsSaveDays"))
            {
                LogsSaveDays = SC.GetValue<int>("System.LogsSaveDays");
            }

            if (SC.ContainsItem("System.IsEnableCompressLogFileFunc"))
            {
                IsEnableCompressLogFileFunc = SC.GetValue<bool>("System.IsEnableCompressLogFileFunc");
            }

            if (SC.ContainsItem("System.SingleLogFileMaxSize"))
            {
                SingleLogFileMaxSize = SC.GetValue<int>("System.SingleLogFileMaxSize");
            }
        }

        /// <summary>
        /// LogCleaner 构造函数
        /// </summary>
        /// <param name="logsSaveDays">文件保存时间, 需设置≥7天</param>
        /// <param name="isEnableCompressLogFileFunc" >是否启用文件压缩功能</param>
        /// <param name="singleLogFileMaxSize">超过此大小的文件将会被压缩(以兆字节为单位), 需设置≥1兆</param>
        public LogCleaner(int logsSaveDays, bool isEnableCompressLogFileFunc, int singleLogFileMaxSize)
        {
            LogsSaveDays = logsSaveDays;
            IsEnableCompressLogFileFunc = isEnableCompressLogFileFunc;
            SingleLogFileMaxSize = singleLogFileMaxSize;
        }

        public LogCleaner(int logsSaveDays)
        {
            LogsSaveDays = logsSaveDays;
        }

        public bool IsEnableCompressLogFileFunc { get; set; }

        private int _logsSaveDays = 60;
        public int LogsSaveDays
        {
            get { return _logsSaveDays < 7 ? 7 : _logsSaveDays; }
            set { _logsSaveDays = value; }
        }

        private int _singleFileMaxSize = 100;
        public int SingleLogFileMaxSize
        {
            get { return _singleFileMaxSize < 1 ? 1 : _singleFileMaxSize; }
            set { _singleFileMaxSize = value; }
        }


        public void Run()
        {
            //1天运行一次，删除一个月之前的 log文件
            _threadDeleteLogs = new PeriodicJob(1000 * 60 * 60 * 24, OnDeleteLog, "DeleteLog Thread", true);
        }

        bool OnDeleteLog()
        {
            try
            {

                string path = PathManager.GetLogDir();
                FileInfo[] fileInfos;
                DirectoryInfo curFolderInfo = new DirectoryInfo(path);
                fileInfos = curFolderInfo.GetFiles();
                foreach (FileInfo info in fileInfos)
                {
                    if (info.Name.Contains("Log")||info.Name.Contains("log") || info.Extension == ".log")
                    {
                        DateTime lastWriteTime = DateTime.Parse(info.LastWriteTime.ToShortDateString());
                        DateTime intervalTime = DateTime.Now.AddDays(-LogsSaveDays);// DateTime.Parse(DateTime.Now.AddMonths(-1).ToShortDateString());

                        if (lastWriteTime < intervalTime)
                        {
                            File.Delete(info.FullName);
                            LOG.Write(string.Format("delete log successfully，logName:{0}", info.Name));
                        }
                        else if (IsEnableCompressLogFileFunc && info.Length > SingleLogFileMaxSize * 1024 * 1024 && lastWriteTime < DateTime.Now.AddDays(-1) && info.Extension != ".zip")
                        {
                            if (CompressFile(info.FullName, info.FullName + ".zip"))
                            {
                                File.Delete(info.FullName);
                                LOG.Write(string.Format("delete log successfully，logName:{0}", info.Name));
                            }
                        }
                    }
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
            _threadDeleteLogs?.Stop();
        }


        /// <summary>
        /// 压缩文件/文件夹
        /// </summary>
        /// <param name="filePath">需要压缩的文件/文件夹路径</param>
        /// <param name="zipPath">压缩文件路径（zip后缀）</param>
        /// <param name="password">密码</param>
        /// <param name="filterExtenList">需要过滤的文件后缀名</param>
        private bool CompressFile(string filePath, string zipPath, string password = "", List<string> filterExtenList = null)
        {
            try
            {
                using (ZipFile zip = new ZipFile(Encoding.UTF8))
                {
                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        zip.Password = password;
                    }
                    if (Directory.Exists(filePath))
                    {
                        if (filterExtenList == null)
                            zip.AddDirectory(filePath);
                        else
                            AddDirectory(zip, filePath, filePath, filterExtenList);
                    }
                    else if (File.Exists(filePath))
                    {
                        zip.AddFile(filePath, "");
                    }
                    zip.Save(zipPath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            return false;
        }

        /// <summary>
        /// 添加文件夹
        /// </summary>
        /// <param name="zip">ZipFile对象</param>
        /// <param name="dirPath">需要压缩的文件夹路径</param>
        /// <param name="rootPath">根目录路径</param>
        /// <param name="filterExtenList">需要过滤的文件后缀名</param>
        private void AddDirectory(ZipFile zip, string dirPath, string rootPath, List<string> filterExtenList)
        {
            var files = Directory.GetFiles(dirPath);
            for (int i = 0; i < files.Length; i++)
            {
                if (filterExtenList == null || (filterExtenList != null && !filterExtenList.Any(d => Path.GetExtension(files[i]).ToLower() == d.ToLower())))
                {
                    string relativePath = Path.GetFullPath(dirPath).Replace(Path.GetFullPath(rootPath), "");
                    zip.AddFile(files[i], relativePath);
                }
            }
            var dirs = Directory.GetDirectories(dirPath);
            for (int i = 0; i < dirs.Length; i++)
            {
                AddDirectory(zip, dirs[i], rootPath, filterExtenList);
            }
        }


        ~LogCleaner()
        {
            Stop();
        }
    }
}
