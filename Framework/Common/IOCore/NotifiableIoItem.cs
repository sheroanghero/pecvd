using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.CommonData;

namespace MECF.Framework.Common.IOCore
{
    public class NotifiableIoItem : NotifiableItem
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public int Index { get; set; }

        [DataMember]
        public bool BoolValue { get; set; }

        [DataMember]
        public short ShortValue { get; set; }

        [DataMember]
        public float FloatValue { get; set; }

        [DataMember]
        public int IntValue { get; set; }

        [DataMember]
        public string StringValue { get; set; }

        [DataMember]
        public string Address { get; set; }

        [DataMember]
        public string Provider { get; set; }

        [DataMember]
        public int BlockOffset { get; set; }

        [DataMember]
        public int BlockIndex { get; set; }


        [DataMember]
        public bool HoldValue { get; set; }

        public NotifiableIoItem Clone()
        {
            return new NotifiableIoItem()
            {
                Address = Address,
                BlockIndex = BlockIndex,
                BlockOffset = BlockOffset,
                BoolValue = BoolValue,
                Description = Description,
                HoldValue = HoldValue,
                Index = Index,
                Name = Name,
                Provider = Provider,
                ShortValue = ShortValue,
                FloatValue = FloatValue,
                IntValue = IntValue,
                StringValue = StringValue,
            };
        }
    }
}
