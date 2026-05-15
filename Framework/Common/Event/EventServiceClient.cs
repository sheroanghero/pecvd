using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.WCF;
using Aitex.Core.RT.Event;
using System.ServiceModel;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using Aitex.Core.WCF.Interface;
using MECF.Framework.Common.Properties;

namespace Aitex.Core.WCF
{
    public class EventClient : Singleton<EventClient>
    {
        public event Action<EventItem> OnEvent;
        public event Action OnDisconnectedWithRT;

        public bool InProcess { get; set; }

        private IEventService _service;
        public IEventService Service
        {
            get
            {
                if (_service == null)
                {
                    if (InProcess)
                    {
                        _service = Singleton<EventManager>.Instance.Service;
                    }
                    else
                    {
                        _service = new EventServiceClient(new EventServiceCallback());
                    }
                    _service.OnEvent += _service_OnEvent;

                }

                return _service;
            }
        }

        public bool IsConnectedWithRT
        {
            get { return _retryConnectRT.Result; }
        }

         
        Retry _retryConnectRT = new Retry();

        FixSizeQueue<EventItem> _queue = new FixSizeQueue<EventItem>(500);
        PeriodicJob _fireJob;
        Guid _guid = Guid.NewGuid();
        private R_TRIG _trigDisconnect = new R_TRIG();
        private DeviceTimer _timer = new DeviceTimer();

        public bool ConnectRT()
        {
            _retryConnectRT.Result = InProcess || Service.Register(_guid);

            return _retryConnectRT.IsSucceeded;
        }

        public void Start()
        {
            if (_fireJob != null)
                return;

            _fireJob = new PeriodicJob(500, this.FireEvent, "UIEvent", true);
        }

        public void Stop()
        {
            if (!InProcess)
            {
                if (_retryConnectRT.IsSucceeded)
                {
                    Service.UnRegister(_guid);
                }
            }

            if (_fireJob != null)
            {
                _fireJob.Stop();
            }
        }

        bool FireEvent()
        {
            _retryConnectRT.Result = InProcess || Service.Register(_guid);

            if (_retryConnectRT.IsSucceeded)
            {
                _service_OnEvent(new EventItem()
                {
                    OccuringTime = DateTime.Now,
                    Description = "Connected with RT",
                    Id = 0,
                    Level = EventLevel.Information,
                    Type = EventType.EventUI_Notify,
                });
            }

            if (_retryConnectRT.IsErrored)
            {
                _service_OnEvent(new EventItem()
                {
                    OccuringTime = DateTime.Now,
                    Description = "Disconnect from RT",
                    Id = 0,
                    Level = EventLevel.Information,
                    Type = EventType.EventUI_Notify,
                });

                _timer.Start(0);
            }

            if (_timer.GetElapseTime() > 2000 && !_retryConnectRT.Result && OnDisconnectedWithRT!=null)
            {
                OnDisconnectedWithRT();
            }


            if (OnEvent != null)
            {
                EventItem ev;
                while (_queue.TryDequeue(out ev))
                {
                    //if (ev.Description.Equals("PM1 Process  Abort") || ev.Description.Equals("PM2 Process  Abort") || ev.Description.Equals("PM3 Process  Abort"))
                    //{
                    //    Console.WriteLine("EventServiceClient(QueeeD)：" + ev.Description);
                    //}
                    OnEvent(ev);
                }
            }

            return true;
        }


        private void _service_OnEvent(EventItem ev)
        {
            _queue.Enqueue(ev);
            //if (ev.Description.Equals("PM1 Process  Abort") || ev.Description.Equals("PM2 Process  Abort") || ev.Description.Equals("PM3 Process  Abort"))
            //{
            //    Console.WriteLine("EventServiceClient(QueeeE)：" + ev.Description);
            //}
        }
    }

    public class EventServiceClient : DuplexChannelServiceClientWrapper<IEventService>, IEventService
    {
        public event Action<EventItem> OnEvent;

        public EventServiceClient(EventServiceCallback callbackInstance)
            : base(new InstanceContext(callbackInstance), "Client_IEventService", "EventService")
        {
            callbackInstance.FireEvent += new Action<EventItem>(callbackInstance_FireEvent);
        }

        void callbackInstance_FireEvent(EventItem obj)
        {
            if (OnEvent != null)
            {
                OnEvent(obj);
            }
        }     

        public bool Register(Guid id)
        {
            bool ret = false;
            Invoke(svc => { ret = svc.Register(id); });
            return ret;
        }

        public void UnRegister(Guid id)
        {
            Invoke(svc => { svc.UnRegister(id); });
        }

        public int Heartbeat()
        {
            int result = -1;
            Invoke(svc => { result = svc.Heartbeat(); });
            return result;
        }
    }
}
