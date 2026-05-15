using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Aitex.Core.RT.Log;
using System.Windows;
using Aitex.Core.Util;

namespace Aitex.Core.WCF
{
    public class WcfServiceManager : Singleton<WcfServiceManager>
    {
        List<ServiceHost> _serviceHost = new List<ServiceHost>();

        public void Initialize(Type[] serviceType)
        {
            foreach (Type t in serviceType)
            {

                try
                {
                    ServiceHost host = new ServiceHost(t);

                    host.Open();

                    _serviceHost.Add(host);
                }
                catch (Exception ex)
                {
                    LOG.Error(string.Format("未能打开WCF服务'{0}'.\n请检查配置后重新启动程序.", ex.Message));
                    throw new ApplicationException(string.Format("初始化{0}服务失败，", t.ToString(), ex.Message));
                }


            }
        }

        public void Terminate()
        {
            foreach (var serviceHost in _serviceHost)
            {
                try
                {
                    serviceHost.Abort();
                    serviceHost.Close();
                }
                catch (Exception ex)
                {
                    LOG.Error(string.Format("关闭'{0}'服务失败", serviceHost.Description.Name), ex);
                }
            }
        }


    }
}
