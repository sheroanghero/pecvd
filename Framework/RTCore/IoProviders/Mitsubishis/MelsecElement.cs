using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.RT.Core.IoProviders.Mitsubishis
{
	public class MelsecElement
	{
		public static MelsecElement relaySpecial =
			new MelsecElement("特殊继电器", "SM", 0x91, 0x000000, 0x002047, MelsecElement.EnumType.Bit);

		public static MelsecElement storageSpecial =
			new MelsecElement("特殊寄存器", "SD", 0xA9, 0x000000, 0x002047, MelsecElement.EnumType.Word, 960);

		public static MelsecElement relayInput =
			new MelsecElement("输入继电器", "X", 0x9C, 0x000000, 0x001FFF, MelsecElement.EnumType.Bit);

		public static MelsecElement relayOutPut =
			new MelsecElement("输出继电器", "Y", 0x9D, 0x000000, 0x001FFF, MelsecElement.EnumType.Bit, 7168);

		public static MelsecElement relayInner =
			new MelsecElement("内部继电器", "M", 0x90, 0x000000, 0x008191, MelsecElement.EnumType.Bit, 7904);

		public static MelsecElement relayLock =
			new MelsecElement("锁存继电器", "L", 0x92, 0x000000, 0x008191, MelsecElement.EnumType.Bit);

		public static MelsecElement relayAlarm =
			new MelsecElement("报警继电器", "F", 0x93, 0x000000, 0x002047, MelsecElement.EnumType.Bit);

		public static MelsecElement relayEdge =
			new MelsecElement("边沿继电器", "V", 0x94, 0x000000, 0x002047, MelsecElement.EnumType.Bit);

		public static MelsecElement relayLink =
			new MelsecElement("链接继电器", "B", 0xA0, 0x000000, 0x001FFF, MelsecElement.EnumType.Bit);

		public static MelsecElement storageData =
			new MelsecElement("数据寄存器", "D", 0xA8, 0x000000, 0x012287, MelsecElement.EnumType.Word, 960);

		public static MelsecElement storageLink =
			new MelsecElement("链接寄存器", "W", 0xB4, 0x000000, 0x001FFFF, MelsecElement.EnumType.Word, 960);

		public static MelsecElement relayTS =
		   new MelsecElement("定时器触点", "TS", 0xC1, 0x000000, 0x002047, MelsecElement.EnumType.Bit);

		public static MelsecElement relayTC =
			new MelsecElement("定时器线圈", "TN", 0xC0, 0x000000, 0x002047, MelsecElement.EnumType.Bit);

		public static MelsecElement storageTN =
			new MelsecElement("定时器当前值", "TC", 0xC2, 0x000000, 0x002047, MelsecElement.EnumType.Word, 960);

		public static MelsecElement relaySS =
		 new MelsecElement("累计定时器触点", "SS", 0xC7, 0x000000, 0x002047, MelsecElement.EnumType.Bit);

		public static MelsecElement relaySC =
			new MelsecElement("累计定时器线圈", "SC", 0xC6, 0x000000, 0x002047, MelsecElement.EnumType.Bit);

		public static MelsecElement storageSN =
			new MelsecElement("累计定时器当前值", "SN", 0xC8, 0x000000, 0x002047, MelsecElement.EnumType.Word);

		public static MelsecElement relayCS =
			new MelsecElement("计数器触点", "CS", 0xC4, 0x000000, 0x001023, MelsecElement.EnumType.Bit);

		public static MelsecElement relayCC =
			new MelsecElement("计数器线圈", "CC", 0xC3, 0x000000, 0x001023, MelsecElement.EnumType.Bit);

		public static MelsecElement storageCN =
			new MelsecElement("计数器当前值", "CN", 0xC5, 0x000000, 0x001023, MelsecElement.EnumType.Word, 960);

		public static MelsecElement relaySpecialLink =
		 new MelsecElement("链接特殊继电器", "SB", 0xA1, 0x000000, 0x0007FF, MelsecElement.EnumType.Bit);

		public static MelsecElement storageSpecialLink =
			new MelsecElement("链接特殊寄存器", "SW", 0xB5, 0x000000, 0x0007FF, MelsecElement.EnumType.Word, 960);

		public static MelsecElement relayStep =
			new MelsecElement("步进继电器", "S", 0x98, 0x000000, 0x008191, MelsecElement.EnumType.Bit);

		public static MelsecElement relayInputDir =
		   new MelsecElement("直接输入继电器", "DX", 0xA2, 0x000000, 0x001FFF, MelsecElement.EnumType.Bit);

		public static MelsecElement relayOutPutDir =
			new MelsecElement("直接输出继电器", "DY", 0xA3, 0x000000, 0x001FFF, MelsecElement.EnumType.Bit);

		public static MelsecElement storageAddr =
			 new MelsecElement("变址寄存器", "Z", 0xCC, 0x000000, 0x000015, MelsecElement.EnumType.Word, 960);

		public static MelsecElement storageFile =
			  new MelsecElement("文件寄存器", "R", 0xAF, 0x032767, 0x000015, MelsecElement.EnumType.Word, 960);

		public static MelsecElement storageFileZ =
			  new MelsecElement("文件寄存器Z", "RZ", 0xB0, 0x0FE7FF, 0x000015, MelsecElement.EnumType.Word, 960);

		public MelsecElement() { }

		public MelsecElement(string strName, string strAscCode, int nBinCode,
			int nStartAddr, int nEndAddr, EnumType enumType, int nLen = 3584)
		{
			m_strName = strName;
			m_strAscCode = strAscCode;
			m_nBinCode = nBinCode;
			m_nStartAddr = nStartAddr;
			m_nEndAddr = nEndAddr;
			m_enumType = enumType;
			m_nLen = nLen;
		}

		public static MelsecElement ChooseMelsecElement(string registername)
		{
            if (registername == storageFile.m_strName || registername == storageFile.m_strAscCode)
                return storageFile;
            if (registername == relayLink.m_strName || registername == relayLink.m_strAscCode)
                return relayLink;
            if (registername == relayInner.m_strName || registername == relayInner.m_strAscCode)
                return relayInner;
            if (registername == storageData.m_strName || registername == storageData.m_strAscCode)
                return storageData;
            if (registername == storageFileZ.m_strName || registername == "ZR" || registername == storageFileZ.m_strAscCode)
                return storageFileZ;
            if (registername== relaySpecial.m_strName ||registername==relaySpecial.m_strAscCode)
				return relaySpecial;
			if (registername == storageSpecial.m_strName || registername == storageSpecial.m_strAscCode)
				return storageSpecial;
			if (registername == relayInput.m_strName || registername == relayInput.m_strAscCode)
				return relayInput;
			if (registername == relayOutPut.m_strName || registername == relayOutPut.m_strAscCode)
				return relayOutPut;
			if (registername == relayLock.m_strName || registername == relayLock.m_strAscCode)
				return relayLock;
			if (registername == relayAlarm.m_strName || registername == relayAlarm.m_strAscCode)
				return relayAlarm;
			if (registername == relayEdge.m_strName || registername == relayEdge.m_strAscCode)
				return relayEdge;
			if (registername == storageLink.m_strName || registername == storageLink.m_strAscCode)
				return storageLink;
			if (registername == relayTS.m_strName || registername == relayTS.m_strAscCode)
				return relayTS;
			if (registername == relayTC.m_strName || registername == relayTC.m_strAscCode)
				return relayTC;
			if (registername == storageTN.m_strName || registername == storageTN.m_strAscCode)
				return storageTN;
			if (registername == relaySS.m_strName || registername == relaySS.m_strAscCode)
				return relaySS;
			if (registername == relaySC.m_strName || registername == relaySC.m_strAscCode)
				return relaySC;
			if (registername == storageSN.m_strName || registername == storageSN.m_strAscCode)
				return storageSN;
			if (registername == relayCS.m_strName || registername == relayCS.m_strAscCode)
				return relayCS;
			if (registername == relayCC.m_strName || registername == relayCC.m_strAscCode)
				return relayCC;
			if (registername == storageCN.m_strName || registername == storageCN.m_strAscCode)
				return storageCN;
			if (registername == relaySpecialLink.m_strName || registername == relaySpecialLink.m_strAscCode)
				return relaySpecialLink;
			if (registername == storageSpecialLink.m_strName || registername == storageSpecialLink.m_strAscCode)
				return storageSpecialLink;
			if (registername == relayStep.m_strName || registername == relayStep.m_strAscCode)
				return relayStep;
			if (registername == relayInputDir.m_strName || registername == relayInputDir.m_strAscCode)
				return relayInputDir;
			if (registername == relayOutPutDir.m_strName || registername == relayOutPutDir.m_strAscCode)
				return relayOutPutDir;
			if (registername == storageAddr.m_strName || registername == storageAddr.m_strAscCode)
				return storageAddr;
			return null;
		}

		public enum EnumType
		{
			Bit = 0,//位类型
			Word = 1,//字类型
		}

		public string m_strName { get; set; }
		public string m_strAscCode { get; set; }
		public int m_nBinCode { get; set; }
		public int m_nStartAddr { get; set; }
		public int m_nEndAddr { get; set; }
		public EnumType m_enumType { get; set; }
		public int m_nLen { get; set; }//最大处理长度

	}
}
