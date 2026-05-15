using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.Utilities
{
    [AttributeUsageAttribute(AttributeTargets.Property | AttributeTargets.Field)]
    public class IgnorePropertyChangeAttribute : Attribute
    {
         
        public IgnorePropertyChangeAttribute( )
        {
             
        }

    }
}