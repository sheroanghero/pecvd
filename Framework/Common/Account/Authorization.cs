using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Net;
using System.Timers;
using Aitex.Core.RT.Event;



namespace Aitex.Core.Account
{

    public enum AuthorizationStatusEnum
    {
        NoAuthorization,
        Authorizing,
        Granted,
        Rejected,
    }
    
    public static class Authorization
    {
        public static string Module { get; set; }
        static Authorization()
        {
            Module = "System";

            AuthorizedAccount = string.Empty;
            AuthorizedIP = string.Empty;
            AuthorizingAccount = string.Empty;
            AuthorizingIP = string.Empty;            

    //        Update();

            _timer = new Timer(3 * 60*1000);//3 minutes
            _timer.AutoReset = false;
            _timer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
        }

        static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_status == AuthorizationStatusEnum.Authorizing)
            {               
                Grant(true);
            }
        }        

        public static AuthorizationStatusEnum Status
        {
            get
            {
                return _status;
            }
        }

        public static bool IsAuthorizedAccount(string accountId, string ip)
        {
            if (string.IsNullOrWhiteSpace(AuthorizedAccount))
            {
                EV.PostMessage(Module, EventEnum.AccountWithoutAuthorization, accountId);
                return false;
            }
            if (ip ==  Network.LocalIP && AuthorizedIP ==  Network.LocalIP) return true;

            if (AuthorizedAccount != accountId)
            {
                EV.PostMessage(Module, EventEnum.AccountWithoutAuthorization, accountId);
                return false;
            }

            return true;
        }

        static bool CanAutoAuthorize(string accountId, string ip)
        {
            return string.IsNullOrWhiteSpace(AuthorizedAccount)     //未有授权用户
                    || (ip ==  Network.LocalIP)                             //申请IP来自服务器同一地址
                    || (accountId == AuthorizedAccount);            //已授权账户
                 //|| (AuthorizedIP == ServerIP && ip == ServerIP)  //本地IP已授权，且本地账户申请权限 
        }

        public static string AuthorizedAccount
        {
            get;
            private set;
        }

        public static string AuthorizedIP
        {
            get;
            private set;
        }

        public static string AuthorizingAccount
        {
            get;
            private set;
        }

        public static string AuthorizingIP
        {
            get;
            private set;
        }

        /// <summary>
        /// User applies operation & control authorization
        /// </summary>
        /// <param name="accountId">client user</param>
        /// <param name="ip">client ip</param>
        public static void Request(string accountId, string ip)
        {
            EV.PostMessage(Module,  EventEnum.OperationAuthorization, string.Format("{0} 在申请操控权", accountId));

            if (CanAutoAuthorize(accountId, ip))
            {
                AuthorizedAccount = accountId;
                AuthorizedIP = ip;

                EV.PostMessage(Module, EventEnum.OperationAuthorization, string.Format("{0} 获得操控权", AuthorizedAccount));
                _status = AuthorizationStatusEnum.Granted;

                 

                return;
            }

            AuthorizingAccount = accountId;
            AuthorizingIP = ip;
            _status = AuthorizationStatusEnum.Authorizing;

             

            _timer.Start();
        }

        /// <summary>
        /// Authorizing user aborts this authorization operation.
        /// </summary>
        public static void Abort()
        {
            _timer.Stop();

            _status = AuthorizationStatusEnum.NoAuthorization;
            AuthorizingAccount = string.Empty;
            AuthorizingIP = string.Empty;
 
        }

        /// <summary>
        /// Authorized user grants or rejects the authorization
        /// </summary>
        /// <param name="isGranted">True: granted   False: Rejected</param>
        public static void Grant(bool isGranted)
        {
            _timer.Stop();

            if (_status == AuthorizationStatusEnum.Granted) return;

            if (isGranted)
            {
                AuthorizedAccount = AuthorizingAccount;
                AuthorizedIP = AuthorizingIP;

                _status = AuthorizationStatusEnum.Granted;
                EV.PostMessage(Module, EventEnum.OperationAuthorization, string.Format("{0} 获得操控权", AuthorizedAccount));
            }
            else
            {
                _status = AuthorizationStatusEnum.Rejected;
                EV.PostMessage(Module, EventEnum.OperationAuthorization, string.Format("{0} 拒绝转交操控权", AuthorizedAccount));
            }

 
        }

        public static void Exit(string accountId)
        {            
            if (accountId == AuthorizedAccount)
            {
                AuthorizedAccount = string.Empty;
                AuthorizedIP = string.Empty;  
              
                Abort();            
            }
            else if (accountId == AuthorizingAccount)
            {
                Abort();
            }
        }

 
        static Timer _timer = new Timer();
        static AuthorizationStatusEnum _status = AuthorizationStatusEnum.NoAuthorization;

        public static class Network
        {
            const string IpPrefix = "192.18.";

            static string _localIP;
            public static string LocalIP
            {
                get
                {
                    if (string.IsNullOrWhiteSpace(_localIP))
                    {
                        string hostName = Dns.GetHostName();
                        IPHostEntry me = Dns.GetHostEntry(hostName);
                        IPAddress[] ips = me.AddressList;

                        Func<IPAddress, bool> predicateIPV4 = ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
                        Func<IPAddress, bool> predicate192 = ip => predicateIPV4(ip) && ip.ToString().StartsWith(IpPrefix);

                        _localIP = ips.Any(predicate192) ? ips.First(predicate192).ToString()
                            : ips.Any(predicateIPV4) ? ips.First(predicateIPV4).ToString()
                            : (ips.Length > 0 ? ips[0] : new IPAddress(0x0)).ToString();
                    }
                    return _localIP;
                }
            }
        }
    }
}