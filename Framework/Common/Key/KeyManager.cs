using System;
using System.Collections.Generic;
using System.Linq;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Microsoft.Win32;

namespace Aitex.Core.RT.Key
{
    public class KeyManager : Singleton<KeyManager>
    {
        public const string KeyTag = "Jet";
        public const string RegistryKey = "SOFTWARE\\Jet\\Keys";

        public string LocalMachineCode
        {
            get;  private set;
        }

        public bool IsExpired
        {
            get
            {
                return false;//ExpireDateTime < CurrentDateTime;
            }
        }

        public int LeftDays
        {
            get
            {
                if (IsExpired)
                    return 0;

                return (ExpireDateTime - CurrentDateTime).Days;
            }
        }

        public DateTime CurrentDateTime
        {
            get
            {
                //if (_plcDateTime != null)
                //    return _plcDateTime.CurrentDateTime;
                return DateTime.Now;
            }
        }

        public DateTime ExpireDateTime
        {
            get
            {
                return LastRegisterDateTime + new TimeSpan(LastRegisterDays, 0, 0, 0);
            }
        }

        public DateTime LastRegisterDateTime
        {
            get; set;
        }

        public int LastRegisterDays
        {
            get; set;
        }

 
        private PeriodicJob _thread;
        Dictionary<string, string> _historyKeys = new Dictionary<string, string>();

        //private IoPlcDateTime _plcDateTime;

        object _locker = new object();

        DeviceTimer _timer0Days = new DeviceTimer();
        R_TRIG _trig0Days = new R_TRIG();

        DeviceTimer _timer3Days = new DeviceTimer();
        R_TRIG _trig3Days = new R_TRIG();

        DeviceTimer _timer7Days = new DeviceTimer();
        R_TRIG _trig7Days = new R_TRIG();

        DeviceTimer _timer15Days = new DeviceTimer();
        R_TRIG _trig15Days = new R_TRIG();


        //public void Initialize(IoPlcDateTime plcDateTime)
        //{
        //    _plcDateTime = plcDateTime;

        //    LocalMachineCode = new MachineCoder().CreateCode();

        //    UpdateLicenseInformation();

        //    _thread = new PeriodicJob(1000, OnTimer, "Register Key Thread", true);
        //}

        public void Initialize( )
        {
 
            LocalMachineCode = new MachineCoder().CreateCode();

            UpdateLicenseInformation();

            _thread = new PeriodicJob(1000, OnTimer, "Register Key Thread", true);
        }

        void UpdateLicenseInformation()
        {
            try
            {
                RegistryKey regRoot = Registry.CurrentUser;

                RegistryKey regKeys = regRoot.OpenSubKey(RegistryKey, true);
                if (regKeys == null)
                {
                    regKeys = regRoot.CreateSubKey(RegistryKey);
                }

                if (regKeys == null)
                {
                    throw new ApplicationException("注册表操作失败，无法进行软件授权操作.\r\n请确保用管理员权限运行程序.");
                }

                foreach (var date in regKeys.GetSubKeyNames())
                {
                    RegistryKey sub = regKeys.OpenSubKey(date);
                    _historyKeys.Add(date, (string)sub.GetValue("Key"));
                }

                if (_historyKeys.Count == 0)
                {
                    string reason;
                    if (!Register(7, "---", out reason))
                        throw  new ApplicationException(reason);
                }
                else
                {
                    List<string> date = _historyKeys.Keys.ToList();
                    date.Sort();
                    string last = date.Last();

                    RegistryKey sub = regKeys.OpenSubKey(last);

                    LastRegisterDateTime = new DateTime(int.Parse(last.Substring(0,4)),int.Parse(last.Substring(4,2)),int.Parse(last.Substring(6,2)),
                        int.Parse(last.Substring(8,2)),int.Parse(last.Substring(10,2)),int.Parse(last.Substring(12,2)));
                    LastRegisterDays = int.Parse(sub.GetValue("Days").ToString());
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }

        }

        bool OnTimer()
        {
            try
            {
                _trig0Days.CLK = IsExpired;
                _trig3Days.CLK = LeftDays <= 3 && !IsExpired;
                _trig7Days.CLK = LeftDays <= 7 && LeftDays > 3;
                _trig15Days.CLK = LeftDays <= 15 && LeftDays > 7;

                if (_trig0Days.M) //到期之后，每1个小时提醒一次，预警形式
                {
                    if (_trig0Days.Q)
                    {
                        EV.PostMessage("System", EventEnum.DefaultWarning, string.Format("Software expired at {0} ",  ExpireDateTime.ToString()));
                        _timer0Days.Start(1000 * 60 * 60 * 1);
                    }

                    if (_timer0Days.IsTimeout())
                    {
                        EV.PostMessage("System", EventEnum.DefaultWarning, string.Format("Software expired at {0} ",  ExpireDateTime.ToString()));
                        _timer0Days.Start(1000 * 60 * 60 * 1);
                    }

                }

                if (_trig3Days.M) //3天内到期，每3个小时提醒一次，预警形式
                {
                    if (_trig3Days.Q)
                    {
                        EV.PostMessage("System", EventEnum.DefaultWarning, string.Format("Software will be expired in {0} days, at {1} ", LeftDays,ExpireDateTime.ToString()));
                        _timer3Days.Start(1000 * 60 * 60 * 3);
                    }

                    if (_timer3Days.IsTimeout())
                    {
                        EV.PostMessage("System", EventEnum.DefaultWarning, string.Format("Software will be expired in {0} days, at {1}", LeftDays,ExpireDateTime.ToString()));
                        _timer3Days.Start(1000 * 60 * 60 * 3);
                    }

                }
                if (_trig7Days.M)//7天内到期，每8个小时提醒一次，预警形式
                {
                    if (_trig7Days.Q)
                    {
                        EV.PostMessage("System", EventEnum.DefaultWarning, string.Format("Software will be expired in {0} days, at {1} ", LeftDays,ExpireDateTime.ToString()));
                        _timer7Days.Start(1000 * 60 * 60 * 8);
                    }

                    if (_timer7Days.IsTimeout())
                    {
                        EV.PostMessage("System", EventEnum.DefaultWarning, string.Format("Software will be expired in {0} days, at {1}", LeftDays,ExpireDateTime.ToString()));
                        _timer7Days.Start(1000 * 60 * 60 * 8);
                    }                
                }
                
                if (_trig15Days.M)//15天内到期，每8个小时提醒一次，信息形式
                {
                    if (_trig15Days.Q)
                    {
                        EV.PostMessage("System", EventEnum.GeneralInfo, string.Format("Software will be expired in {0} days, at {1} ", LeftDays,ExpireDateTime.ToString()));
                        _timer15Days.Start(1000 * 60 * 60 * 8);
                    }

                    if (_timer15Days.IsTimeout())
                    {
                        EV.PostMessage("System", EventEnum.GeneralInfo, string.Format("Software will be expired in {0} days, at {1}", LeftDays,ExpireDateTime.ToString()));
                        _timer15Days.Start(1000 * 60 * 60 * 8);
                    }                  
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            return true;
        }

        bool Register(int days, string key, out string reason)
        {
            try
            {
                if (_historyKeys.ContainsValue(key))
                {
                    reason = "注册码已经使用，" + key; 
                    LOG.Write(reason);
                    return false;
                }

                if (days < 0)
                {
                    reason = "授权天数无效，" + days; 
                    LOG.Write(reason);
                    return false;                
                }

                RegistryKey regRoot = Registry.CurrentUser;

                RegistryKey regKeys = regRoot.OpenSubKey(RegistryKey, true);
                if (regKeys == null)
                {
                    regKeys = regRoot.CreateSubKey(RegistryKey);
                }

                if (regKeys == null)
                {
                    reason = "注册表操作失败，无法进行软件授权操作.\r\n请确保用管理员权限运行程序."; 
                    LOG.Write(reason);
                    return false;
                }

                DateTime now = CurrentDateTime;
                    string itemName = now.ToString("yyyyMMddHHmmss");
 
                    RegistryKey rkItem = regKeys.CreateSubKey(itemName);
                    rkItem.SetValue("Date", itemName);
                    rkItem.SetValue("Key", key);
                    rkItem.SetValue("Days", days.ToString());
                regKeys.Close();

                _historyKeys.Add(itemName, key);

                LastRegisterDateTime = now;
                LastRegisterDays = days;
            }
            catch (Exception ex)
            {
                    reason = "注册码无效，" + ex; 
                    LOG.Write(reason);
                    return false; 
            }
            reason = "注册成功";
            return true;
        }

        public bool Register(string key, out string reason)
        {
            reason = string.Empty;
            RsaCryption rsa = new RsaCryption();
            string decry = string.Empty;
            try
            {
                decry = rsa.RSADecrypt(/*JetKey.PrivateKey*/"", key);
            }
            catch (Exception ex)
            {
                reason = "注册码无效";
                LOG.Write(ex);
                return false;
            }

            string[] text = decry.Split(',');

            int days = 0;
            if (text.Length ==3 && text[0]==KeyTag && text[1]==LocalMachineCode && int.TryParse(text[2], out days))
            {
                return Register(days, key, out reason);
            } 

            LOG.Error("注册码无效，" + decry);
            reason = "注册码无效";
            return false;
        }
    }
}
