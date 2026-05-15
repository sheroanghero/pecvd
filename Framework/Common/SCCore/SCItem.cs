using System;
using System.Diagnostics;
using System.Xml.Serialization;
using Aitex.Core.RT.Log;

namespace Aitex.Core.RT.SCCore
{
    public class SCConfigItem
    {
        public string Name { get; set; }
        public string Path { get; set; }

        public string Default { get; set; }
        public string Min { get; set; }
        public string Max { get; set; }
        public string Unit { get; set; }
        public string Type { get; set; }
        public string Tag { get; set; }
        public string Parameter { get; set; }
        public string Description { get; set; }

        public object Value
        {
            get
            {
                switch (Type)
                {
                    case "Bool": 
                        return BoolValue;
                    case "Double":
                        return DoubleValue;
                    case "String":
                        return StringValue;
                    case "Integer":
                        return IntValue;
                    default:
                        return null;
                }
            }
        }

        public Type Typeof
        {
            get
            {
                switch (Type)
                {
                    case "Bool":
                        return typeof(bool);
                    case "Double":
                        return typeof(double);
                    case "String":
                        return typeof(string);
                    case "Integer":
                        return typeof(int);
                    default:
                        return null;
                }
            }
        }

        public string PathName
        {
            get { return string.IsNullOrEmpty(Path) ? Name : (Path + "." + Name); }
        }

        public int IntValue { get; set; }

        public double DoubleValue { get; set; }
 
        public bool BoolValue { get; set; }

        public string StringValue { get; set; }
 
        public SCConfigItem Clone()
        {
            return new SCConfigItem()
            {
                Name = this.Name,
                Path = this.Path,
                Default = this.Default,
                Min = this.Min,
                Max = this.Max,
                Unit = this.Unit,
                Type = this.Type,
                Tag = this.Tag,
                Parameter = this.Parameter,
                Description = this.Description,
                StringValue = this.StringValue,
                IntValue = this.IntValue,
                DoubleValue = this.DoubleValue,
                BoolValue = this.BoolValue,
            };
        }
    }

    public enum SCConfigType
    {
        String,
        Integer,
        Bool,
        Double,
        List,
    }

}
