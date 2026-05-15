using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.UI.Client.ClientBase
{
    public enum WaferStatus
    {
        Empty = 0,
        Normal = 1,
        Crossed = 2,
        Double = 3,
        Unknown = 4,
        Dummy = 5
    }

    [Flags]
    public enum DialogButton
    {
        Yes = 1,
        OK = 2,
        Continue = 4,
        Transfer = 8,
        Restart = 16,
        No = 32,
        Cancel = 64
    }

    public enum DialogType
    {
        INFO,
        WARNING,
        ERROR,
        CONFIRM
    }

    public enum ServiceState
    {
        Normal,
        Shutdown,
        Fault,
        Disconnect
    }

    public enum UserMode
    {
        None,
        Normal,
        Lock,
        Logoff,
        Exit,
        Shutdown,
        Breakdown
    }

    public enum PageID
    {
       PAGE1,
       PAGE2,
       PAGE3,
       MAX_PAGE
    }

    public enum AuthorizeResult
    {
        None = -1,
        WrongPwd = 1,
        HasLogin = 2,
        NoMatchRole = 3,
        NoSession = 4,
        NoMatchUser = 5
    }

    public enum MenuPermissionEnum
    {
        MP_NONE = 1,
        MP_READ = 2,
        MP_READ_WRITE = 3
    }

    /// <summary>
    /// Process control mode
    /// </summary>
    public enum CtrlMode
    {
        VIEW,
        EDIT
    }
 
}
