using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aitex.Core.RT.Log;

namespace Aitex.Core.RT.DataCenter
{
    public class DataItem<T>
    {
        readonly Func<T> _getter;

        public DataItem(Func<T> getter)
        {
            if (getter == null)
                throw new ArgumentNullException("getter");
            _getter = getter;
        }

        public T Value
        {
            get
            {
                try
                {
                    return _getter.Invoke();
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }
                return default(T);
            }
        }
    }
}
