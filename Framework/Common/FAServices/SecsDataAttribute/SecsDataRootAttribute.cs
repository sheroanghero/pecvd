using System;

namespace MECF.Framework.Common.FAServices.SecsDataAttribute
{
    /// <summary>
    ///  用于DVID为List的情况，需对需要展示给Host端的类或者属性加上此特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
    public class SecsDataRootAttribute : Attribute
    {

    }
}
