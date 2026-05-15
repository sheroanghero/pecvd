using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.Alarms
{
    public class AlarmDefine
    {
        public int Number { get; set; }
        public string Message { get; set; }
        public string Action { get; set; }
        public string Recovery { get; set; }
        public string Cause { get; set; }
        public int AlarmGroup { get; set; }
        public bool IsUse { get; set; }
    }
}
