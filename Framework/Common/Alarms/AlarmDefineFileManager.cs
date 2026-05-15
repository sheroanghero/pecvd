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
    public class AlarmDefineFileManager : Singleton<AlarmDefineFileManager>
    {
        public void Initialize()
        {
            Singleton<WcfServiceManager>.Instance.Initialize(new Type[]
            {
                typeof(AlarmDefineService)
            });
        }
    }
}
