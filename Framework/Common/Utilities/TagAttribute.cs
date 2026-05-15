using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.Utilities
{
    [AttributeUsageAttribute(AttributeTargets.Property | AttributeTargets.Field)]
    public class TagAttribute : Attribute
    {
        public readonly string Tag;
        public TagAttribute(string tag)
        {
            Tag = tag;
        }

    }
}