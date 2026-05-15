using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Aitex.Core.RT.IOCore
{
	[Serializable]
	public class DI_ITEM
	{
		[XmlAttribute]
		public int Index;
 
		[XmlAttribute]
		public string Name = " ";

        [XmlAttribute]
        public string Addr;		//物理地址
 
        [XmlAttribute]
        public string Description = "";
	}
	/// <summary>
	/// 数字量输出节点定义
	/// </summary>
	[Serializable]
	public class DO_ITEM
	{
		[XmlAttribute]
		public int Index;


        [XmlAttribute]
        public string Addr;		//物理地址

        [XmlAttribute]
        public string Name = " ";

        [XmlAttribute]
        public string Description = "";
	}

	/// <summary>
	/// 模拟量输出节点定义
	/// </summary>
	[Serializable]
	public class AO_ITEM
	{
		[XmlAttribute]
		public int Index;
		[XmlAttribute]
		public string Name = " ";
        
        [XmlAttribute]
        public string Addr;		//物理地址

        [XmlAttribute]
        public string Description = "";
	}

	/// <summary>
	/// 数字量输入节点定义
	/// </summary>
	[Serializable]
	public class AI_ITEM
	{
		[XmlAttribute]
		public int Index;
		[XmlAttribute]
		public string Name = " ";

        [XmlAttribute]
        public string Addr;		//物理地址

        [XmlAttribute]
        public string Description = "";
	}

  
	/// <summary>
	/// IO数据表
	/// </summary>
	[Serializable]
	public class IO_DEFINE
	{
		public IO_DEFINE()
		{
			Dig_In = new DI_ITEM[64];
			Dig_Out = new DO_ITEM[64];
			Ana_In = new AI_ITEM[64];
			Ana_Out = new AO_ITEM[64];

			for (int i = 0; i < 64; i++)
			{
				Dig_In[i] = new DI_ITEM() { Index = i };
				Dig_Out[i] = new DO_ITEM() { Index = i };
 
			}

		    for (int i = 0; i < 64; i++)
		    {
 
		        Ana_In[i] = new AI_ITEM() { Index = i };
		        Ana_Out[i] = new AO_ITEM() { Index = i };
		    }
           
		}
		public DI_ITEM[] Dig_In;
		public DO_ITEM[] Dig_Out;
		public AI_ITEM[] Ana_In;
		public AO_ITEM[] Ana_Out;
	}


}
