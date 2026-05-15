using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.SCCore;
using Aitex.Core.RT.Tolerance;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoFlowMeter : BaseDevice, IDevice
    {
        public double Feedback
        {
            get
            {
                if (_aiFlowValue != null)
                {
                    ushort value = (ushort)_aiFlowValue.Value;

                    double e = value * 1.0 / 65536 * _factor;

                    return (e - _physicalMin) * (_rangeMax - _rangeMin) / (_physicalMax - _physicalMin) + _rangeMin;

                    //return (e-4) * 40 / 16;

                    //return Converter.Phy2Logic((ushort)_aiFlowValue.Value, 0, 40, 0, 0xFFFF/5 );
                }
                return 0;
            }
        }

        public double FilteredFeedback
        {
            get
            {
                return _queueData.ToList().Average();
            }
        }

        public bool IsWarning
        {
            get
            {
                return _checkWarning.Result;
            }
        }

        public bool IsError
        {
            get
            {
                return _checkAlarm.Result;
            }
        }

        private double _minSetPoint;

        public double MinSetPoint
        {
            get
            {
                return _minSetPoint;
            }
            set
            {
                _minSetPoint = value;
                //if (_aoMin != null)
                //    _aoMin.Value = (short)value;
            }
        }

        private double _maxSetPoint;

        public double MaxSetPoint
        {
            get
            {
                return _maxSetPoint;
            }
            set
            {
                _maxSetPoint = value;
                //if (_aoMax != null)
                //    _aoMax.Value = (short)value;
            }
        }

        public double MinFlow
        {
            get
            {
                return _scMinFlow == null ? 0 : _scMinFlow.DoubleValue;
            }
        }

        public double MaxFlow
        {
            get
            {
                return _scMaxFlow == null ? 0 : _scMaxFlow.DoubleValue;
            }
        }

        public double WarningTime
        {
            get
            {
                return _scWarningTime == null ? 0 : _scWarningTime.IntValue;
            }
        }

        public double AlarmTime
        {
            get
            {
                return _scAlarmTime == null ? 0 : _scAlarmTime.IntValue;
            }
        }

        public bool IsOutOfTolerance
        {
            get
            {
                if (MinFlow < 0.01 && MaxFlow < 0.01)
                    return false;

                return (Feedback < MinFlow) || (Feedback > MaxFlow);
            }
        }

        public bool EnableToleranceCheck { get; set; }


        private AITWaterFlowMeterData DeviceData
        {
            get
            {
                AITWaterFlowMeterData data = new AITWaterFlowMeterData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    FeedBack = Feedback,
                    IsWarning = IsWarning,
                    Unit = Unit,
                    IsOutOfTolerance = IsOutOfTolerance,
                };

                return data;
            }
        }

        public string Unit { get; set; }

        private AIAccessor _aiFlowValue = null;

        //private DIAccessor _diValve;

        //private DOAccessor _doWarning;
        //private DOAccessor _doAlarm;

        //private AOAccessor _aoMin;
        //private AOAccessor _aoMax;

        private SCConfigItem _scMinFlow;
        private SCConfigItem _scMaxFlow;
        private SCConfigItem _scWarningTime;
        private SCConfigItem _scAlarmTime;
        private SCConfigItem _scEnableTolerance;

        private ToleranceChecker _checkWarning = new ToleranceChecker();
        private ToleranceChecker _checkAlarm = new ToleranceChecker();

        private RD_TRIG _trigValveOpenClose = new RD_TRIG();

        private int _factor;
        private int _rangeMin;
        private int _rangeMax;
        private int _physicalMin;
        private int _physicalMax;

        FixSizeQueue<double> _queueData = new FixSizeQueue<double>(20);

        public IoFlowMeter(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            Name = node.GetAttribute("id");
            Display = node.GetAttribute("display");
            DeviceID = node.GetAttribute("schematicId");
            Unit = node.GetAttribute("unit");

            _aiFlowValue = ParseAiNode("aiFeedback", node, ioModule);


            int.TryParse(node.GetAttribute("factor"), out _factor);

            string[] range = node.GetAttribute("range").Split(',');
            int.TryParse(range[0], out _rangeMin);
            int.TryParse(range[1], out _rangeMax);

            string[] physical = node.GetAttribute("physical").Split(',');
            int.TryParse(physical[0], out _physicalMin);
            int.TryParse(physical[1], out _physicalMax);

            _scMinFlow = SC.GetConfigItem($"Modules.{Module}.{Name}.MinFlow");
            _scMaxFlow = SC.GetConfigItem($"Modules.{Module}.{Name}.MaxFlow");
            _scWarningTime = SC.GetConfigItem($"Modules.{Module}.{Name}.WarningTime");
            _scAlarmTime = SC.GetConfigItem($"Modules.{Module}.{Name}.AlarmTime");
            _scAlarmTime = SC.GetConfigItem($"Modules.{Module}.{Name}.AlarmTime");
            _scEnableTolerance = SC.GetConfigItem($"Modules.{Module}.{Name}.EnableTolerance");

            MinSetPoint = MinFlow;
            MaxSetPoint = MaxFlow;
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.Feedback", () => Feedback);


            return true;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            MinSetPoint = MinFlow;
            MaxSetPoint = MaxFlow;

            _queueData.Enqueue(Feedback);

            //_trigValveOpenClose.CLK = (_diValve == null || _diValve.Value);

            //关闭阀门
            if (_trigValveOpenClose.T)
            {
                _checkWarning.RST = true;
                _checkAlarm.RST = true;
            }

            //打开过程中，一直监控
            if (_trigValveOpenClose.M && _scEnableTolerance.BoolValue)
            {
                if (WarningTime > 0.1)
                {
                    _checkWarning.Monitor(Feedback, MinFlow, MaxFlow, WarningTime);

                    if (_checkWarning.Trig)
                    {
                        EV.PostWarningLog(Module, $"Flow meter {Display} feedback out of range");

                    }

                    //if (_doWarning != null)
                    //{
                    //    _doWarning.Value = _checkWarning.Result;
                    //}
                }

                if (AlarmTime > 0.1)
                {
                    _checkAlarm.Monitor(Feedback, MinFlow, MaxFlow, AlarmTime);
                    if (_checkAlarm.Trig)
                    {
                        EV.PostAlarmLog(Module, $"Flow meter {Display} feedback out of range");
                    }

                    //if (_doAlarm != null)
                    //{
                    //    _doAlarm.Value = _checkAlarm.Result;
                    //}
                }
            }
        }

        public void Reset()
        {
            _checkWarning.RST = true;
            _checkAlarm.RST = true;
        }
    }
}