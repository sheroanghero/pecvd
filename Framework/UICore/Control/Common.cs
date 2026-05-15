using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.UI.Control
{
    public enum ValveDirection
    {
        ToLeft = 1,
        ToRight = 2,
        ToTop = 3,
        ToBottom = 4
    }

    public enum LineOrientation
    {
        Horizontal=1,
        Vertical,
    }

    /// <summary>
    /// which valve should be turn on
    /// </summary>
    public enum SwithType
    {
        Left = 1,
        Right = 2
    }

    public enum CoupleValveInOutDirection
    {
        ToOut = 1,
        ToInner = 2
    }

    public enum MessageType
    {
        Info,
        /// <summary>
        /// notice for update or insert result
        /// </summary>
        Success,
        Warning,
        Erro
    }
}
