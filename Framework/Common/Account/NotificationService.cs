using System;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using System.ServiceModel.Activation;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Aitex.Core.RT.Log;



namespace Aitex.Core.Account
{
    /// <summary>
    /// NotificationService class
    /// </summary>
    public sealed class NotificationService 
    {
            /// 客户电脑名
        /// </summary>
        public static string ClientHostName
        {
            get
            {
                //return OperationContext.Current.IncomingMessageHeaders.GetHeader<string>("IdentityClientName", "ns");
                return null;   
            }
        }

        /// <summary>
        /// 客户用户名
        /// </summary>
        public static string ClientUserName
        {
            get
            {
                if (OperationContext.Current == null || OperationContext.Current.IncomingMessageHeaders == null)
                    return "";
                var res = OperationContext.Current.IncomingMessageHeaders.FindHeader("IdentityUserName", "ns");
                if (res == -1)
                    return "";
                return OperationContext.Current.IncomingMessageHeaders.GetHeader<string>("IdentityUserName", "ns");
            }
        }

        /// <summary>
        /// 客户端程序唯一的Guid编号
        /// </summary>
        public static Guid ClientGuid
        {
            get
            {
                //try
                //{
                //    var guidStr = OperationContext.Current.IncomingMessageHeaders.GetHeader<string>("ClientGuid", "ns");
                //    return Guid.Parse(guidStr);
                //}
                //catch (Exception ex)
                //{
                //    LOG.Write(ex);
                //}
                return Guid.Empty;
            }
        }


    }
}
