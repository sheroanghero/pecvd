using Aitex.Common.Util;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MECF.Framework.Common.Alarms
{
    public class AlarmDefineFileContext : IAlarmDefineService
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
        public Dictionary<int, AlarmDefine> GetAlarmDefine()
        {
            string recipeSchema = PathManager.GetCfgDir() + @"\AlarmDefine.xml";
            XmlDocument xmlDom = new XmlDocument();
            xmlDom.Load(recipeSchema);

            XmlNodeList AlarmGroup = xmlDom.SelectNodes($"Alarms/AlarmGroup");
            Dictionary<int, AlarmDefine> alarmList = new Dictionary<int, AlarmDefine>();
            foreach (XmlNode item in AlarmGroup)
            {
                foreach (XmlNode alarm in item.ChildNodes)
                {
                    AlarmDefine alarmDefine = CreateAlarmDefine(alarm);
                    if (alarmDefine != null)
                    {
                        try
                        {
                            alarmList.Add(alarmDefine.Number, alarmDefine);
                        }
                        catch (Exception ex)
                        {
                            LOG.Write($"Alarm mumber={alarmDefine.Number} {ex}");
                        }
                    }
                }
            }

            return alarmList;
        }

        private AlarmDefine CreateAlarmDefine(XmlNode alarm)
        {
            AlarmDefine alarmDefine = new AlarmDefine();
            if (alarm != null && alarm.Attributes != null)
            {
                if (alarm.Attributes["Number"] != null)
                {
                    if (alarm.Attributes["Number"].Value.ToLower().Contains("0x"))
                    {
                        alarmDefine.Number = int.Parse(alarm.Attributes["Number"].Value.Substring(2), NumberStyles.HexNumber);
                    }
                    else
                    {
                        alarmDefine.Number = int.Parse(alarm.Attributes["Number"].Value, NumberStyles.Integer);
                    }
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
