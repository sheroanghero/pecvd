using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Aitex.Core.Util;

namespace Aitex.Core.Account
{
        [Serializable]
        public class Account
        {
            public string AccountId{get;set;}
            public string Role{get;set;}
            public string RealName{get;set;}
            public string Touxian{get;set;}
            public string Company{get;set;}
            public string Department{get;set;}
            public bool AccountStatus{get;set;} //enable or disable
            public string Email{get;set;}
            public string Telephone{get;set;}
            public string Description{get;set;}
            public string LastLoginTime{get;set;}
            public string LastAccountUpdateTime{get;set;}
            public string AccountCreationTime{get;set;}
            public SerializableDictionary<string, ViewPermission> Permission{get;set;}
            public string Md5Pwd { get; set; }
            public string LoginIP { get; set; }

            public string LoginId { get; set; }
        }

    [Serializable]
        public class LoginResult
        {
            public bool ActSucc{get;set;}

            public String SessionId{get;set;}

            public Account AccountInfo{get;set;}

            public String Description{get;set;}
        }

        [Serializable]
        public class CreateAccountResult
        {
            public bool ActSucc{get;set;}
            public String Description{set;get;}
        }
        [Serializable]
        public class DeleteAccountResult
        {
            public bool ActSucc{get;set;}
            public String Description{set;get;}
        }

        [Serializable]
        public class UpdateAccountResult
        {
            public bool ActSucc { get; set; }
            public String Description { set; get; }
        }
 
        [Serializable]
        public class ChangePwdResult
        {
            public bool ActSucc { get; set; }
            public String Description { set; get; }
        }
 
        [Serializable]
        public class GetAccountListResult
        {
            public bool ActSuccess{get;set;}
            public IEnumerable<Account> AccountList{get;set;}
            public String Description{get;set;}
        }

        [Serializable]
        public class GetAccountInfoResult
        {
            public bool ActSuccess{get;set;}
            public Account AccountInfo{get;set;}
            public String Description{get;set;}
        }

        [Serializable]
        [DataContract]
        public enum ViewPermission
        {
            [EnumMember]
            Invisiable = 0x1,

            [EnumMember]
            Readonly = 0x2,

            [EnumMember]
            PartlyControl = 0x3,

            [EnumMember]
            FullyControl = 0x4,

            /// <summary>
            /// 设定ProcessView界面中的OP权限
            /// </summary>
            [EnumMember]
            ProcessOPControl = 0x5,

        }
 
}
