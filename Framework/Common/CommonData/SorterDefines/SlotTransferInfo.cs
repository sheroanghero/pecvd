using System;
using System.Runtime.Serialization;
using MECF.Framework.Common.Equipment;

namespace Aitex.Sorter.Common
{
	[DataContract]
	[Serializable]
	public class SlotTransferInfo
	{
		[DataMember]
		public ModuleName Station { get; set; }

		[DataMember]
		public int Slot { get; set; }

		[DataMember]
		public ModuleName SourceStation { get; set; }

		[DataMember]
		public int SourceSlot { get; set; }

		[DataMember]
		public ModuleName DestinationStation { get; set; }

		[DataMember]
		public int DestinationSlot { get; set; }
	}
}
