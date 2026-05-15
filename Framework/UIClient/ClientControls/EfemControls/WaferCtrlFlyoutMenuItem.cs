using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.UI.Client.ClientControls.EfemControls
{
    public class WaferCtrlFlyoutMenuItem
    {
        public WaferCtrlFlyoutMenuItem()
        {
            TargetType = typeof(WaferCtrlFlyoutMenuItem);
        }
        public int Id { get; set; }
        public string Title { get; set; }

        public Type TargetType { get; set; }
    }
}