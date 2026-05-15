using System;
using System.Security.AccessControl;
using System.Xml;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoSensor : BaseDevice, IDevice
    {
        private DIAccessor _di = null;
        private DOAccessor _do = null;

        public DIAccessor SensorDI => _di;
        public DOAccessor SensorDO => _do;

        private R_TRIG _trigTextOut = new R_TRIG();

        private SCConfigItem _scGas1IsInstalled;
        private SCConfigItem _scGas2IsInstalled;
        private SCConfigItem _scGas3IsInstalled;
        private SCConfigItem _scGas4IsInstalled;

        private bool _textOutTrigValue;
		public bool AlarmTrigValue
		{
			get { return _textOutTrigValue&&!string.IsNullOrEmpty(_alarmText); }
		}

		private string _warningText;
        private string _alarmText;
        private string _infoText;

        public Action WarningAction
        {
            get;
            set;
        }

        public event Action<IoSensor, bool> OnSignalChanged;

        public bool Value  
        {
            get
            {
                if (_di != null)
                    return _di.Value;

                if (_do != null)
                    return _do.Value;

                return false;
            }
        }

        private AITSensorData DeviceData
        {
            get
            {
                AITSensorData data = new AITSensorData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,
                    Value = Value,
                };

                return data;
            }
        }

        private bool _previous;
 
        public IoSensor(string module, XmlElement node, string ioModule = "")
        {
            var attrModule = node.GetAttribute("module");
            base.Module = string.IsNullOrEmpty(attrModule) ? module : attrModule;
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            _di = ParseDiNode("di", node, ioModule);// IO.DI[node.GetAttribute("di")];
            //_do = ParseDoNode("do", node, ioModule);

            _infoText = node.GetAttribute("infoText");
            _warningText = node.GetAttribute("warningText");
            _alarmText = node.GetAttribute("alarmText");

            _scGas1IsInstalled = SC.GetConfigItem($"System.SetUp.{Module}.Gas1IsInstalled");
            _scGas2IsInstalled = SC.GetConfigItem($"System.SetUp.{Module}.Gas2IsInstalled");
            _scGas3IsInstalled = SC.GetConfigItem($"System.SetUp.{Module}.Gas3IsInstalled");
            _scGas4IsInstalled = SC.GetConfigItem($"System.SetUp.{Module}.Gas4IsInstalled");

            _textOutTrigValue = Convert.ToBoolean(string.IsNullOrEmpty(node.GetAttribute("textOutTrigValue")) ? "false" : node.GetAttribute("textOutTrigValue"));
        }

        public bool Initialize()
        {
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);
            DATA.Subscribe($"{Module}.{Name}.Value", () => Value);

            return true;
        }

        public void Terminate()
        {
        }

        public void Monitor()
        {
            try
            {
                if (Name.Contains("MFC"))
                {
                    if (Name.Contains("MFC1") && _scGas1IsInstalled!=null && !_scGas1IsInstalled.BoolValue)
                    {
                        return;
                    }

                    if (Name.Contains("MFC2") && _scGas2IsInstalled != null && !_scGas2IsInstalled.BoolValue)
                    {
                        return;
                    }

                    if (Name.Contains("MFC3") && _scGas3IsInstalled != null && !_scGas3IsInstalled.BoolValue)
                    {
                        return;
                    }

                    if (Name.Contains("MFC4") && _scGas4IsInstalled != null && !_scGas4IsInstalled.BoolValue)
                    {
                        return;
                    }
                }

                if (Name.Contains("BackPressure") && _scGas4IsInstalled != null && !_scGas4IsInstalled.BoolValue)
                {
                    return;
                }

                _trigTextOut.CLK = (Value == _textOutTrigValue) ;

                if (_trigTextOut.Q)
                {
                    if (WarningAction != null)
                    {
                        WarningAction();
                    }
                    else if (!string.IsNullOrEmpty(_warningText.Trim()))
                    {
                        EV.PostWarningLog(Module, _warningText);
                    }else if (!string.IsNullOrEmpty(_alarmText.Trim()))
                    {
                        EV.PostAlarmLog(Module,  _alarmText);
                    }else if (!string.IsNullOrEmpty(_infoText.Trim()))
                    {
                        EV.PostInfoLog(Module,  _infoText);
                    }
                }

                if (_previous != Value)
                {
                    if (OnSignalChanged != null)
                        OnSignalChanged(this, Value);
                    _previous = Value;
                }

            }
            catch (Exception ex)
            {
                LOG.Write(ex);

            }

        }

        public void Reset()
        {
            _trigTextOut.RST = true;
        }
    }
}
