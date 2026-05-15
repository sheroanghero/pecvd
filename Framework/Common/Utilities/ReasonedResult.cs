using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.Utilities
{
    public class ReasonedResult
    {
        public bool Result;
        public string Reason;

        public ReasonedResult(bool result, string reason)
        {
            Result = result;
            Reason = reason;
        }
    }

}
