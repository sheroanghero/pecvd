using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.RT.Log
{
    public class CounterLogger
    {
        public string RecoverMessage { get; set; }
        public string ErroMessage { get; set; }

        bool _isError;
        public void Error(string error)
        {
            if (!_isError)
            {
                _isError = true;
                LOG.Error(ErroMessage + error);
            }
        }

        public void Recover()
        {
            if (_isError)
            {
                _isError = false;
                LOG.Info(RecoverMessage, true);
            }
        }
    }
}
