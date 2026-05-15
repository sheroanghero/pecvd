using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aitex.Core.Util
{
	[AttributeUsageAttribute(AttributeTargets.Property)]
	public class SubscriptionModuleAttribute : Attribute
	{
		public string Module { get; set; }

		public SubscriptionModuleAttribute(string module)
		{
			Module = module;
		}
	}
}
