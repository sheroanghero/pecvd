using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Aitex.Core.RT.IOCore;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoPlcDateTime : BaseDevice, IDevice
    {

        private AIAccessor _aiYear;
        private AIAccessor _aiMonth;
        private AIAccessor _aiDay;
        private AIAccessor _aiHour;
        private AIAccessor _aiMinute;
        private AIAccessor _aiSecond;
 
        public DateTime CurrentDateTime
        {
            get
            {
 
                if (Year <1000 && Month == 0 && Day == 0)
                    return DateTime.Now;
 
                return new DateTime(Year, Month, Day, Hour, Minute, Second);
            }
        }

        public int Year
        {
            get
            {
                return _aiYear == null ? DateTime.Now.Year : (int)_aiYear.Value;
            }
        }

        public int Month
        {
            get
            {
                return _aiMonth == null ? DateTime.Now.Month : (int)_aiMonth.Value;
            }
        }

        public int Day
        {
            get
            {
                return _aiDay == null ? DateTime.Now.Day : (int)_aiDay.Value;
            }
        }
        public int Hour
        {
            get
            {
                return _aiHour == null ? DateTime.Now.Hour : (int)_aiHour.Value;
            }
        }
        public int Minute
        {
            get
            {
                return _aiMinute == null ? DateTime.Now.Minute : (int)_aiMinute.Value;
            }
        }
        public int Second
        {
            get
            {
                return _aiSecond == null ? DateTime.Now.Second : (int)_aiSecond.Value;
            }
        }

        public IoPlcDateTime(string module, XmlElement node, string ioModule = "")
        {
            base.Module = module;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

             _aiYear = ParseAiNode("aiYear", node, ioModule);
            _aiMonth = ParseAiNode("aiMonth", node, ioModule);
            _aiDay = ParseAiNode("aiDay", node, ioModule);
            _aiHour = ParseAiNode("aiHour", node, ioModule);
            _aiMinute = ParseAiNode("aiMinute", node, ioModule);
            _aiSecond = ParseAiNode("aiSecond", node, ioModule);

        }

        public bool Initialize()
        {
            return true;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
           
        }



        public void Reset()
        {
            
        }

    }
}