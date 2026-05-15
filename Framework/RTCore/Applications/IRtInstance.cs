using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace MECF.Framework.RT.Core.Applications
{
    public interface IRtInstance
    {
        string SystemName { get; }

        string DatabaseName { get; }

        bool EnableNotifyIcon { get; }

        bool KeepRunningAfterUnknownException { get; }

        ImageSource TrayIcon { get; }

        bool DefaultShowBackendWindow { get; }

        IRtLoader Loader { get;  }
    }
}
