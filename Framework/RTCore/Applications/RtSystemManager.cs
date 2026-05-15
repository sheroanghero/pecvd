using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Aitex.Core.Util;

namespace MECF.Framework.RT.Core.Applications
{
    public class RtSystemManager : Singleton<RtSystemManager>
    {
        public void AddCustomBackend(string title, UserControl uc)
        {
             RtApplication.MainView.AddItem(title, uc);
        }

 
    }
}
