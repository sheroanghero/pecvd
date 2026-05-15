using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace Aitex.Core.Util
{
	/// <summary>
	/// 下降沿信号检测类
	/// 
	/// 设计目的：
	/// 用于信号下降沿事件检测
	/// 
	/// 使用场合：
	/// 系统输入数字信号，1->0跳变检测
	/// </summary>
	public class F_TRIG
	{
		/// <summary>
		/// 构造函数
		/// </summary>
		public F_TRIG()
		{
			Q = false;
			M = true;
		}

		/// <summary>
		/// 检测信号输入
		/// </summary>
		public bool CLK
		{
			set
			{
				if (M != value && !value)
					Q = true;
				else
					Q = false;
				M = value;
			}
			get
			{
				return M;
			}
		}

        //clear 
        public bool RST
        {
            set
            {
                Q = false;
                M = true;
            }
        }
		/// <summary>
		/// 检测结果输出
		/// </summary>
        [XmlIgnore]
        public bool Q { get; private set; }

		/// <summary>
		/// 记录上一次输入信号值
		/// </summary>
        [XmlIgnore]
        public bool M { get; private set; }
	}

	/// <summary>
	/// 上升沿信号检测类
	/// 
	/// 设计目的：
	/// 用于信号上升沿事件检测
	/// 
	/// 使用场合：
	/// 系统输入数字信号，0->1跳变检测
	/// </summary>
	public class R_TRIG
	{
		/// <summary>
		/// 构造函数
		/// </summary>
		public R_TRIG()
		{
			Q = false;
			M = false;
		}

		/// <summary>
		/// 检测信号输入
		/// </summary>
		public bool CLK
		{
			set
			{
				if (M != value && value)
					Q = true;
				else
					Q = false;
				M = value;
			}
			get
			{
				return M;
			}
		}

        public bool RST
        {
            set
            {
                Q = false;
                M = false;
            }
        }
		/// <summary>
		/// 检测结果输出
		/// </summary>
        [XmlIgnore]
        public bool Q { get; private set; }

		/// <summary>
		/// 记录上一次输入信号值
		/// </summary>
        [XmlIgnore]
		public bool M { get; private set; }
	}

    /// <summary>
    /// 边沿信号检测类
    /// 
    /// 设计目的：
    /// 用于信号上升沿/下降沿事件检测
    /// 
    /// 使用场合：
    /// 初始值为0
    /// 系统输入数字信号，0->1跳变检测  Q 触发
    /// 1->0 R
    /// </summary>
    public class RD_TRIG
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public RD_TRIG()
        {
            R = false;    
            T = false;
            M = false;
        }

        /// <summary>
        /// 检测信号输入
        /// </summary>
        public bool CLK
        {
            set
            {
                if (M != value)
                {
                    R = value;
                    T = !value;
                }
                else
                {
                    R = false;
                    T = false;
                }

                M = value;
            }

            get
            {
                return M;
            }
        }

        public bool RST
        {
            set
            {
                R = false;
                T = false;
                M = false;
            }
        }
        /// <summary>
        /// 检测结果输出, 上升沿触发 rising edge
        /// </summary>
        [XmlIgnore]
        public bool R { get; private set; }

        /// <summary>
        /// 检测结果输出, 下降沿触发 trailing edge
        /// </summary>
        [XmlIgnore]
        public bool T { get; private set; }

        /// <summary>
        /// 记录上一次输入信号值
        /// </summary>
        [XmlIgnore]
        public bool M { get; private set; }
    }

}
