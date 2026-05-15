using System;

namespace MECF.Framework.Common.FAServices.SecsDataAttribute
{
    /// <summary>
    ///  用于DVID为List的情况，需对需要展示给Host端的属性加上此特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SecsDataElementAttribute : Attribute
    {
        /// <summary>
        /// 如果使用无参构造方法则默认为属性名
        /// </summary>
        public string Name { get; }

        public SecsDataElementAttribute(string name)
        {
            Name = name;
        }

        public SecsDataElementAttribute()
        {

        }
    }

}
