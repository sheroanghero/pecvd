using Aitex.Common.Util;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MECF.Framework.Common.Alarms
{
    class AlarmDefineService : IAlarmDefineService
    {
        public Dictionary<string, Dictionary<string, EventItem>> GetAlarmDefineTemplate()
        {
            Dictionary<string, Dictionary<string, EventItem>> result = new Dictionary<string, Dictionary<string, EventItem>>();
            foreach (var key in Singleton<EventManager>.Instance.AlarmDic.Keys)
            {
                Dictionary<string, EventItem> temp = new Dictionary<string, EventItem>();
                foreach (var name in Singleton<EventManager>.Instance.AlarmDic[key].Keys)
                {
                    var item = Singleton<EventManager>.Instance.AlarmDic[key][name].Clone();
                    temp[name] = item;
                }
                result[key] = temp;
            }
            return result;
        }

        private AlarmDefine CreateAlarmDefine(XmlNode alarm)
        {
            AlarmDefine alarmDefine = new AlarmDefine();
            if (alarm != null)
            {
                if (alarm.Attributes["Number"] != null)
                {
                    int numberValue = 0;
                    int.TryParse(alarm.Attributes["Number"].Value, out numberValue);
                    alarmDefine.Number = numberValue;
                }
                if (alarm.Attributes["Message"] != null)
                    alarmDefine.Message = alarm.Attributes["Message"].Value;
                if (alarm.Attributes["Action"] != null)
                    alarmDefine.Action = alarm.Attributes["Action"].Value;
                if (alarm.Attributes["Recovery"] != null)
                    alarmDefine.Recovery = alarm.Attributes["Recovery"].Value;
                if (alarm.Attributes["Cause"] != null)
                    alarmDefine.Cause = alarm.Attributes["Cause"].Value;
                return alarmDefine;
            }
            return null;
        }

        public string GetStringAlarmDefineTemplate()
        {
            try
            {
                string recipeSchema = PathManager.GetCfgDir() + @"\AlarmDefine.xml";
                XmlDocument xmlDom = new XmlDocument();
                xmlDom.Load(recipeSchema);
                return xmlDom.OuterXml;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return "";
            }
        }
    }
}
