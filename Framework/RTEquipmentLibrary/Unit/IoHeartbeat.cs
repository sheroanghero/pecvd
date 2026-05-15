using System;
using System.Xml;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device.Unit
{
    /// <summary>
    /// 心跳包机制
    /// 
    /// 功能：
    /// 1.C#送出给PLC，如果PLC检测到C#心跳信号停止，则判定C#程序运行异常，从而触发安全逻辑动作.
    /// 2.C#检测PLC返回的心跳包信号，如果检测到PLC心跳信号停止，则判定PLC程序运行异常，从而进行报警处理.
    /// </summary>
    public class IoHeartbeat : BaseDevice, IDevice
    {
        public float Feedback
        {
            get
            {
                return _isFloatAioType ? _ai.FloatValue : _ai.Value;
            }
        }

        public float SetPoint
        {
            get
            {
                return _isFloatAioType ? _ao.FloatValue : _ao.Value;
            }
            set
            {
                if (_isFloatAioType)
                    _ao.FloatValue = value;
                else
                {
                    _ao.Value = (short)value;
                }
            }
        }
        private int PLC_Heart_Beat_Timeout_ms = 1000* 120;
//IO
        private AIAccessor _ai = null;
        private AOAccessor _ao = null;
 
        private DeviceTimer _updateTimer = new DeviceTimer();  //更新 AO信号 给PLC
 
        private int MAX = 0x7FFF;
        private float _prevAiValue = 0;

        private DeviceTimer _connectTimer = new DeviceTimer();  
        private R_TRIG _trigConnectionLost = new R_TRIG();
        private bool _isFloatAioType = false;

        public IoHeartbeat(string module, XmlElement node, string ioModule = "")
        {
            base.Module = string.IsNullOrEmpty(node.GetAttribute("module")) ? module : node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _ai = ParseAiNode("ai", node, ioModule);
            _ao = ParseAoNode("ao", node, ioModule);


            _isFloatAioType = !string.IsNullOrEmpty(node.GetAttribute("aioType")) && (node.GetAttribute("aioType") == "float");

        }

        public bool Initialize()
        {
            _updateTimer.Start(500);
            _connectTimer.Start(PLC_Heart_Beat_Timeout_ms);
            return true;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            if (Math.Abs(_prevAiValue - Feedback) > 0.01)
            {
                _connectTimer.Start(PLC_Heart_Beat_Timeout_ms);
            }

            _trigConnectionLost.CLK = _connectTimer.IsTimeout();
            if (_trigConnectionLost.Q)
            {
                EV.PostAlarmLog(Module, $"PLC heartbeat check failed");
            }

            _prevAiValue = Feedback;

            //如果计时到达，则翻转心跳信号
            if (_updateTimer.IsTimeout())
            {
                int value = (int)SetPoint;
                value++;
                if (value >= MAX)
                {
                    value = 0;
                }

                SetPoint = value;

                _updateTimer.Start(500);       //500 ms 
            }
        }

        public void Reset()
        {
            _connectTimer.Start(PLC_Heart_Beat_Timeout_ms);
            _trigConnectionLost.RST = true;

        }
    }
}
