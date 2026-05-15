using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.Event;
using Aitex.Core.Util;
using Aitex.Core.WCF.Interface;
using System.Collections.Concurrent;

namespace Aitex.Core.WCF
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class EventService : IEventService
    {
        ConcurrentDictionary<Guid, IEventServiceCallback> _callbackClientList = new ConcurrentDictionary<Guid, IEventServiceCallback>();
        public event Action<EventItem> OnEvent;

        private int _counter;
        public EventService()
        {

        }

        public void FireEvent(EventItem obj)
        {
            //DeviceTimer _timer = new DeviceTimer();
            //_timer.Start(0);
            //double t1 = 0;
            //double t2 = 0;
            //double t3 = 0;
            
            //t1 = _timer.GetElapseTime();

            Guid[] ids = _callbackClientList.Keys.ToArray();
            for (int i = 0; i < ids.Length; i++)
            {
                try
                {
                    //t2 = _timer.GetElapseTime() - t1;
                    _callbackClientList[ids[i]].SendEvent(obj);

                    //t3 = _timer.GetElapseTime() - t2;
                }
                catch (Exception ex)
                {
                    LOG.Error(string.Format("给客户端{0}发送事件失败，客户端被删除.", ids[i]), ex);
                    IEventServiceCallback service;
                    _callbackClientList.TryRemove(ids[i], out service);
                }
            }

            if (OnEvent != null)
            {
                OnEvent(obj);
            }

            //System.Diagnostics.Trace.WriteLine(string.Format("[{0}]  -----   [{1}]     --------- [{2}]",  t1, t2, t3));
        }

        public bool Register(Guid id)
        {
            try
            {
                if (!_callbackClientList.ContainsKey(id))
                {
                    LOG.Info(string.Format("客户端{0}已连接", id.ToString()), true);
                }

                _callbackClientList[id] = OperationContext.Current.GetCallbackChannel<IEventServiceCallback>();
            }
            catch (Exception ex)
            {
                LOG.Error("监听事件发生错误", ex);

            }
            return true;
        }

        public void UnRegister(Guid id)
        {
            if (!_callbackClientList.ContainsKey(id))
            {
                LOG.Info(string.Format("客户端{0}断开连接", id.ToString()), true);
            }

            IEventServiceCallback service;
            _callbackClientList.TryRemove(id, out service);
        }

        public int Heartbeat()
        {
            return _counter++ % 100000;
        }
    }
}
