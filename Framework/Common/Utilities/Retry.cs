using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Aitex.Core.Utilities
{
    /// <summary>
    /// 用于简化多次尝试执行动作，仅判断一次成功或者错误.类似于Trigger，但是增加了上升沿，下降沿两种逻辑
    /// </summary>
    public class Retry
    {
        /// <summary>
        /// 第一次触发成功
        /// </summary>
        public bool IsSucceeded { get; set; }

        /// <summary>
        /// 第一次触发失败
        /// </summary>
        public bool IsErrored { get; set; }

        bool _result = false;
        int _retryTime = -1;

        /// <summary>
        /// 当前执行结果
        /// </summary>
        public bool Result
        {
            set
            {
                if (value)
                {
                    IsSucceeded = !_result;
                    IsErrored = false;
                }
                else
                {
                    IsErrored = (_result || _retryTime == -1);
                    IsSucceeded = false;
                }

                _result = value;
                _retryTime = ++_retryTime % 100;
            }

            get
            {
                return _result;
            }
        }

        public Retry()
        {
        }

        public void Do(Func<bool> function, int time)
        {
            for (int i = 0; i < time; i++)
            {
                if (function())
                    Result = true;

                Thread.Sleep(10);
            }
            Result = false;
        }
    }
}
