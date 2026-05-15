using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.Core.IoProviders.Mitsubishis
{

	public class MelsecMCBin
	{
		public MelsecMCBin()
		{
			m_nNetNo = 0x00;//本站
			m_nPLCNo = 0xFF;
			m_nIONo = 0xFF03;
			m_nStationNo = 0x00;
			m_nTimeOut = 0x1000;
			m_bytelist = new List<byte>();
			m_nDataLen = 12;//不包括软元件数据

		}

		/// <summary>
		/// 检查起始地址和读取长度
		/// </summary>
		/// <returns></returns>
		private bool check()
		{

			if (m_nElementDataLen < 0 || m_nElementDataLen > m_melsecElement.m_nLen)
			{
				return false;
			}

			if (m_nElementStartAddr < m_melsecElement.m_nStartAddr || m_nElementStartAddr > m_melsecElement.m_nEndAddr)
			{
				return false;
			}

			return true;
		}

		/*************读协议内容******************************
		 * 副标题(2)50 00|网络编号(1)00|PLC编号(1)FF
		 * IO编号(2)FF 03|站编号(1)00|请求数据长度(2)_12
		 * 应答超时(2)1000|命令(2)_|子命令(2)_|起始地址(3)_
		 * 请求软元件代码(1)|请求点数长度(2)
		 * ***************************************************/


		/// <summary>
		/// 构建读取字节
		/// </summary>
		public byte[] getReadBytes()
		{
			m_bytelist.Clear();

			if (check())
			{
				m_enumSubTitle = EnumsubTitle.Request;
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_enumSubTitle, 2));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nNetNo, 1));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nPLCNo, 1));

				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nIONo, 2));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nStationNo, 1));

				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nDataLen, 2, false));

				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nTimeOut, 2));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_enumCmd, 2, false));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_enumSubCmd, 2, false));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nElementStartAddr, 3, false));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_melsecElement.m_nBinCode, 1));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nElementDataLen, 2, false));

			}

			return m_bytelist.ToArray();
		}

		/****************写协议内容**********************************
		* 副标题(2)50 00|网络编号(1)00|PLC编号(1)FF
		* IO编号(2)FF 03|站编号(1)00|请求数据长度(2)_12+写入数据长度
		* 应答超时(2)1000|命令(2)_|子命令(2)_|起始地址(3)_
		* 请求软元件代码(1)|请求点数长度(2)|写入数据(分按位和按字)
		* ***********************************************************/

		/// <summary>
		/// 构建写字节
		/// </summary>
		/// <param name="nLen">写入数据长度</param>
		public byte[] getWriteBytes(int nLen)
		{
			m_bytelist.Clear();
			if (check())
			{
				m_enumSubTitle = EnumsubTitle.Request;
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_enumSubTitle, 2));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nNetNo, 1));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nPLCNo, 1));

				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nIONo, 2));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nStationNo, 1));

				m_nDataLen += nLen;
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nDataLen, 2, false));

				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nTimeOut, 2));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_enumCmd, 2, false));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_enumSubCmd, 2, false));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nElementStartAddr, 3, false));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_melsecElement.m_nBinCode, 1));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nElementDataLen, 2, false));
				m_bytelist.AddRange(StringHelper.ConverterToBytes(m_nElementData, nLen));

			}

			return m_bytelist.ToArray();
		}
		//MelsecMCBinSend: 500000FFFF03000E001000011400000000009D0100000F 按字写
		//MelsecMCBinRece: D00000FFFF030002000000

		/****************读应答正常协议内容**********************************
		* 副标题(2)D0 00|网络编号(1)00|PLC编号(1)FF
		* IO编号(2)FF 03|站编号(1)00|应答数据长度(2)_
		* 结束代码(2)00 00|应答数据部分
		* ***********************************************************/
		public byte[] getReadRespond(byte[] byteRespond)
		{
			if (byteRespond.Length > 11)
			{

				int nLen = StringHelper.ConverterToInt(byteRespond[7], byteRespond[8]) - 2;
				if (nLen == byteRespond.Length - 11)
				{
					for (int n = 0; n < nLen; n++)
					{
						byteRespond[n] = byteRespond[byteRespond.Length - n - 1];
					}
					Array.Resize(ref byteRespond, nLen);
				}

				return byteRespond;
			}
			return null;

		}

		/****************写应答正常协议内容**********************************
		* 副标题(2)D0 00|网络编号(1)00|PLC编号(1)FF
		* IO编号(2)FF 03|站编号(1)00|应答数据长度(2)02 00
		* 结束代码(2)00 00
		 * D0 00 |00 |FF |FF 03| 00| 02 00 |00 00
		* ***********************************************************/
		public bool getWriteRespond(byte[] byteRespond)
		{
			string strRespond = StringHelper.GetHexString(byteRespond, byteRespond.Length);
			string strTemp = "D00000FFFF030002000000";
			return string.Equals(strTemp, strRespond);
		}

		/****************应答异常协议内容**********************************
		* 副标题(2)D0 00|网络编号(1)00|PLC编号(1)FF
		* IO编号(2)FF 03|站编号(1)00|应答数据长度(2) 0B 00
		* 结束代码(2)51 C0
		*
		* 网络编号(1)00|PLC编号(1)FF
		* IO编号(2)FF 03|站编号(1)00|命令(2)|子命令(2)
		* ***********************************************************/
		public int getRespondCode(byte[] byteRespond)
		{
			if (byteRespond.Length > 10)
			{
				return StringHelper.ConverterToInt(byteRespond[9], byteRespond[10]);
			}
			return -1;
		}



		/*副标题*/
		public EnumsubTitle m_enumSubTitle { get; set; }

		/*网络编号*/
		public int m_nNetNo { get; set; }

		/*PLC编号*/
		public int m_nPLCNo { get; set; }

		/*IO编号*/
		public int m_nIONo { get; set; }

		/*站编号*/
		public int m_nStationNo { get; set; }

		/*数据长度*/
		public int m_nDataLen { get; set; }

		/*CPU监视定时器，命令输出到接收应答文件时间*/
		public int m_nTimeOut { get; set; }

		/*命令*/
		public EnumCmd m_enumCmd { get; set; }

		/*子命令*/
		public EnumSubCmd m_enumSubCmd { get; set; }


		/*软元件*/
		public MelsecElement m_melsecElement { get; set; }

		/*起始软元件地址*/
		public int m_nElementStartAddr { get; set; }

		/*软元件数据长度*/
		public int m_nElementDataLen { get; set; }

		/*软元件数据*/
		public int m_nElementData { get; set; }

		/*结束代码*/
		public EnumEndCode m_nEndCode { get; set; }

		/*临时存放字节数组*/
		private List<byte> m_bytelist;

		public enum EnumsubTitle
		{
			Request = 0x5000,//请求
			Respond = 0xD000,//应答
		}

		public enum EnumEndCode
		{
			Ok = 0x0000,//正常应答
			Err = 0x51C0,//异常应答
		}

		public enum EnumCmd
		{
			ReadBatch = 0x0401,//成批读
			WriteBatch = 0x1401,//成批写

		}

		/// <summary>
		/// 返回byte数组
		/// </summary>
		public enum EnumSubCmd
		{
			/*有存储扩展模块b7=0，b6=0：随机读出,监视数据注册用外*/
			Bit = 0x0001,//按位读写 
			Word = 0x0000,//按字读写


			/*有存储扩展模块b7=1，b6=0：随机读出,监视数据注册用外*/
			BitEx = 0x0081,//按位读写 
			WordEx = 0x0080,//按字读写
		}


	}

	public static class StringHelper
	{
		public static byte[] ConverterToBytes(object value, int length,bool reverse=true)
		{
			byte[] src = new byte[length];
			for (int i = 0; i < length; i++)
			{
				src[i] = (byte)(((byte)value >> (i*8)) & 0xFF);
			}
			return src;
		}

		public static int ConverterToInt(byte value1, byte value2)
		{
			int result = value1 << 8 + value2;

			return result;
		}

		public static string GetHexString(byte[] byteRespond, int length)
		{
			StringBuilder result = new StringBuilder();
			foreach (byte bytes in byteRespond)
			{
				result.AppendFormat("{0:x2}", bytes);
			}
			return result.ToString();
		}
	}
}
