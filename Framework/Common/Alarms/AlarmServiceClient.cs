using Aitex.Core.RT.Event;
using Aitex.Core.Util;
using Aitex.Core.WCF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.Alarms
{
    public class AlarmClient : Singleton<AlarmClient>
    {

        private IAlarmDefineService _service;
        public IAlarmDefineService Service
        {
            get
            {
                if (_service == null)
                {
                    _service = new AlarmServiceClient();
                }
                return _service;
            }
        }
    }
    public class AlarmServiceClient : ServiceClientWrapper<IAlarmDefineService>, IAlarmDefineService
    {
        public AlarmServiceClient()
            : base("Client_IAlarmDefineService", "AlarmDefineService")
        {
        }

        public Dictionary<string, Dictionary<string, EventItem>> GetAlarmDefineTemplate()
        {
            Dictionary<string, Dictionary<string, EventItem>> result = null;
            Invoke(svc => { result = svc.GetAlarmDefineTemplate(); });
            return result;
        }

        public string GetStringAlarmDefineTemplate()
        {
            string result = null;
            Invoke(svc => { result = svc.GetStringAlarmDefineTemplate(); });
            return result;
        }

    }
}
