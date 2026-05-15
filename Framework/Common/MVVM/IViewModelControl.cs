using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.UI.MVVM
{
    public interface IViewModelControl
    {
        void InvokePropertyChanged();
    }
}
