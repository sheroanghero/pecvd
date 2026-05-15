using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Aitex.Core.RT.Log;
using System.Windows;
using Aitex.Core.Utilities;
using System.Threading;

namespace Aitex.Core.WCF
{
    public class ServiceClientWrapper<T>
    {
        ChannelFactory<T> _factory;
        T _proxy;
        bool _isInError = false;
        string _serviceName;

        Retry _retryConnect = new Retry();

        public bool ActionFailed
        {
            get
            {
                return _isInError;
            }
        }

        public ServiceClientWrapper(string endpointConfigurationName, string name)
        {
            _serviceName = name;
            try
            {
                _factory = new ChannelFactory<T>(endpointConfigurationName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("不能创建Service " + name + "，检查配置：" + endpointConfigurationName);

                LOG.Error("不能创建Service "+name+"，检查配置："+endpointConfigurationName, ex);
            }
        }

        public ServiceClientWrapper(string endpointConfigurationName, string name, EndpointAddress address)
        {
            _serviceName = name;
            try
            {
                _factory = new ChannelFactory<T>(endpointConfigurationName, address);
            }
            catch (Exception ex)
            {
                MessageBox.Show("不能创建Service " + name + "，检查配置：" + endpointConfigurationName);

                LOG.Error("不能创建Service " + name + "，检查配置：" + endpointConfigurationName, ex);
            }
        }

        public void Invoke(Action<T> action)
        {
            if (_factory == null)
                return;

            if (_proxy == null)
            {
                _proxy = _factory.CreateChannel();
            }
            
            for (int i = 0; i < 2; i++)
            {
                if (Do(action))
                    break;

                Thread.Sleep(10);
            }

        }

        bool Do(Action<T> action)
        {
            if (_proxy != null && ((IClientChannel)_proxy).State == CommunicationState.Faulted)
            {
                ((IClientChannel)_proxy).Abort();
                _proxy = _factory.CreateChannel();
            }

            try
            {
                action(_proxy);

                if (_isInError)
                {
                    _isInError = false;
                    LOG.Info(_serviceName + " 服务恢复");
                }

                return true;
            }
            catch (EndpointNotFoundException ex)
            {
                if (!_isInError)
                {
                    _isInError = true;
                    LOG.Error(_serviceName + " 连接已经断开.", ex);
                }
            }
            catch (ProtocolException ex)
            {
                if (!_isInError)
                {
                    _isInError = true;
                    LOG.Error(_serviceName + " 服务程序异常.", ex);
                }
            }
            catch (Exception ex)
            {
                if (!_isInError)
                {
                    _isInError = true;
                    LOG.Error(_serviceName + " 服务异常", ex);
                }
            }

            ((IClientChannel)_proxy).Abort();
            _proxy = _factory.CreateChannel();

            return false;
        }




        private Binding CreateBinding(string binding)
        {
            Binding bindinginstance = null;
            if (binding.ToLower() == "basichttpbinding")
            {
                BasicHttpBinding ws = new BasicHttpBinding();
                ws.MaxBufferSize = 2147483647;
                ws.MaxBufferPoolSize = 2147483647;
                ws.MaxReceivedMessageSize = 2147483647;
                ws.ReaderQuotas.MaxStringContentLength = 2147483647;
                ws.CloseTimeout = new TimeSpan(0, 10, 0);
                ws.OpenTimeout = new TimeSpan(0, 10, 0);
                ws.ReceiveTimeout = new TimeSpan(0, 10, 0);
                ws.SendTimeout = new TimeSpan(0, 10, 0);

                bindinginstance = ws;
            }
            else if (binding.ToLower() == "netnamedpipebinding")
            {
                NetNamedPipeBinding ws = new NetNamedPipeBinding();
                ws.MaxReceivedMessageSize = 65535000;
                bindinginstance = ws;
            }
            else if (binding.ToLower() == "nettcpbinding")
            {
                NetTcpBinding ws = new NetTcpBinding();
                ws.MaxReceivedMessageSize = 65535000;
                ws.Security.Mode = SecurityMode.None;
                bindinginstance = ws;
            }
            else if (binding.ToLower() == "wsdualhttpbinding")
            {
                WSDualHttpBinding ws = new WSDualHttpBinding();
                ws.MaxReceivedMessageSize = 65535000;

                bindinginstance = ws;
            }
            else if (binding.ToLower() == "webhttpbinding")
            {
                //WebHttpBinding ws = new WebHttpBinding();
                //ws.MaxReceivedMessageSize = 65535000;
                //bindinginstance = ws;
            }
            else if (binding.ToLower() == "wsfederationhttpbinding")
            {
                WSFederationHttpBinding ws = new WSFederationHttpBinding();
                ws.MaxReceivedMessageSize = 65535000;
                bindinginstance = ws;
            }
            else if (binding.ToLower() == "wshttpbinding")
            {
                WSHttpBinding ws = new WSHttpBinding(SecurityMode.None);
                ws.MaxReceivedMessageSize = 65535000;
                ws.Security.Message.ClientCredentialType = System.ServiceModel.MessageCredentialType.Windows;
                ws.Security.Transport.ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Windows;
                bindinginstance = ws;
            }

            return bindinginstance;

        }
    }
}
