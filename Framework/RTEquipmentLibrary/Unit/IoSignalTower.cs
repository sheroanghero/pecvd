using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Device.Unit;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.Util;
using MECF.Framework.Common.Device.Bases;

namespace Aitex.Core.RT.Device.Unit
{
    public class IoSignalTower : BaseDevice, IDevice
    {
        //device  
        private IoSignalLight _red = null;
        private IoSignalLight _yellow = null;
        private IoSignalLight _green = null;
        private IoSignalLight _blue = null;
        private IoSignalLight _white = null;

        private IoSignalLight _buzzer = null;

        private IoSignalLight _buzzer1 = null;
        private IoSignalLight _buzzer2 = null;
        private IoSignalLight _buzzer3 = null;
        private IoSignalLight _buzzer4 = null;
        private IoSignalLight _buzzer5 = null;


        public bool HostControl { get; set; }
        private bool _buzzerSwitchOff = false;
        private bool _buzzer1SwitchOff = false;
        private bool _buzzer2SwitchOff = false;
        private bool _buzzer3SwitchOff = false;
        private bool _buzzer4SwitchOff = false;
        private bool _buzzer5SwitchOff = false;

        public bool BuzzerSwithcOff => _buzzerSwitchOff;

        public virtual AITSignalTowerData DeviceData
        {
            get
            {
                AITSignalTowerData data = new AITSignalTowerData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,

                    IsGreenLightOn = _green != null && _green.Value,
                    IsRedLightOn = _red != null && _red.Value,
                    IsYellowLightOn = _yellow != null && _yellow.Value,
                    IsWhiteLightOn = _white != null && _white.Value,
                    IsBlueLightOn = _blue != null && _blue.Value,

                    IsBuzzerOn = _buzzer != null && _buzzer.Value,

                    IsBuzzer1On = _buzzer1 != null && _buzzer1.Value,
                    IsBuzzer2On = _buzzer2 != null && _buzzer2.Value,
                    IsBuzzer3On = _buzzer3 != null && _buzzer3.Value,
                    IsBuzzer4On = _buzzer4 != null && _buzzer4.Value,
                    IsBuzzer5On = _buzzer5 != null && _buzzer5.Value,
                 };

                return data;
            }
        }

        public Dictionary<IoSignalLight, TowerLightStatus> SignalTowerState
        {
            get { return dicState; }
        }

        public IoSignalLight Red
        {
            get { return _red; }
        }

        public IoSignalLight Yellow
        {
            get { return _yellow; }
        }

        public IoSignalLight Green
        {
            get { return _green; }
        }

        public IoSignalLight Blue
        {
            get { return _blue; }
        }

        public IoSignalLight White
        {
            get { return _white; }
        }

        public IoSignalLight Buzzer1
        {
            get { return _buzzer1; }
        }

        public IoSignalLight Buzzer2
        {
            get { return _buzzer2; }
        }

        public IoSignalLight Buzzer3
        {
            get { return _buzzer3; }
        }

        public IoSignalLight Buzzer4
        {
            get { return _buzzer4; }
        }

        public IoSignalLight Buzzer5
        {
            get { return _buzzer5; }
        }

        private object _locker = new object();
 
        private Dictionary<string, Dictionary<IoSignalLight, TowerLightStatus>> _config =
            new Dictionary<string, Dictionary<IoSignalLight, TowerLightStatus>>();

        private Dictionary<IoSignalLight, TowerLightStatus> _cmdSetPoint = new Dictionary<IoSignalLight, TowerLightStatus>();
        private Dictionary<IoSignalLight, TowerLightStatus> dicState = new Dictionary<IoSignalLight, TowerLightStatus>();

        public IoSignalTower(string module, XmlElement node, string ioModule = "")
        {
            if (node == null)
                return;

            base.Module = node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            DOAccessor doRed = ParseDoNode("doRed", node, ioModule);
            if (doRed != null)
            {
                _red = new IoSignalLight(module, "SignalLightRed", "Red Light", "SignalLightRed", doRed);
            }

            DOAccessor doYellow = ParseDoNode("doYellow", node, ioModule);
            if (doYellow != null)
            {
                _yellow = new IoSignalLight(module, "SignalLightYellow", "Yellow Light", "SignalLightYellow", doYellow);
            }

            DOAccessor doGreen = ParseDoNode("doGreen", node, ioModule);
            if (doGreen != null)
            {
                _green = new IoSignalLight(module, "SignalLightGreen", "Green Light", "SignalLightGreen", doGreen);
            }

            DOAccessor doBlue = ParseDoNode("doBlue", node, ioModule);
            if (doBlue != null)
            {
                _blue = new IoSignalLight(module, "SignalLightBlue", "Blue Light", "SignalLightBlue", doBlue);
            }

            DOAccessor doWhite = ParseDoNode("doWhite", node, ioModule);
            if (doWhite != null)
            {
                _white = new IoSignalLight(module, "SignalLightWhite", "White Light", "SignalLightWhite", doWhite);
            }

            DOAccessor doBuzzer1 = ParseDoNode("doBuzzer1", node, ioModule);
            if (doBuzzer1 != null)
            {
                _buzzer1 = new IoSignalLight(module, "SignalLightBuzzer1", "Buzzer1 Light", "SignalLightBuzzer1", doBuzzer1);
            }

            DOAccessor doBuzzer2 = ParseDoNode("doBuzzer2", node, ioModule);
            if (doBuzzer2 != null)
            {
                _buzzer2 = new IoSignalLight(module, "SignalLightBuzzer2", "Buzzer2 Light", "SignalLightBuzzer2", doBuzzer2);
            }

            DOAccessor doBuzzer3 = ParseDoNode("doBuzzer3", node, ioModule);
            if (doBuzzer3 != null)
            {
                _buzzer3 = new IoSignalLight(module, "SignalLightBuzzer3", "Buzzer3 Light", "SignalLightBuzzer3", doBuzzer3);
            }

            DOAccessor doBuzzer4 = ParseDoNode("doBuzzer4", node, ioModule);
            if (doBuzzer4 != null)
            {
                _buzzer4 = new IoSignalLight(module, "SignalLightBuzzer4", "Buzzer4 Light", "SignalLightBuzzer4", doBuzzer4);
            }

            DOAccessor doBuzzer5 = ParseDoNode("doBuzzer5", node, ioModule);
            if (doBuzzer5 != null)
            {
                _buzzer5 = new IoSignalLight(module, "SignalLightBuzzer5", "Buzzer5 Light", "SignalLightBuzzer5", doBuzzer5);
            }

            IoSignalLight[] buzzers = new[] {_buzzer1, _buzzer2, _buzzer3, _buzzer4, _buzzer5};
            _buzzer = Array.Find(buzzers, x => x != null);
        }

        public virtual bool Initialize()
        {
            OP.Subscribe($"{Module}.{Name}.{AITSignalTowerOperation.SwitchOffBuzzer}", SwitchOffBuzzer);
            OP.Subscribe($"{Module}.{Name}.{AITSignalTowerOperation.SwitchOffBuzzer1}", SwitchOffBuzzer);
            OP.Subscribe($"{Module}.{Name}.{AITSignalTowerOperation.SwitchOffBuzzer2}", SwitchOffBuzzer);
            OP.Subscribe($"{Module}.{Name}.{AITSignalTowerOperation.SwitchOffBuzzer3}", SwitchOffBuzzer);
            OP.Subscribe($"{Module}.{Name}.{AITSignalTowerOperation.SwitchOffBuzzer4}", SwitchOffBuzzer);

			DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            return true;
        }

        public void SetLight(LightType lightType, LightState state, int blinkInterval = 500)
        {
            IoSignalLight light = null;
            switch (lightType)
            {
                case LightType.Red:
                    light = _red;
                    break;
                case LightType.Green:
                    light = _green;
                    break;
                case LightType.Yellow:
                    light = _yellow;
                    break;
                case LightType.White:
                    light = _white;
                    break;
                case LightType.Blue:
                    light = _blue;
                    break;
                case LightType.Buzzer:
                    light = _buzzer;
                    break;
                case LightType.Buzzer1:
                    light = _buzzer1;
                    break;
                case LightType.Buzzer2:
                    light = _buzzer2;
                    break;
                case LightType.Buzzer3:
                    light = _buzzer3;
                    break;
                case LightType.Buzzer4:
                    light = _buzzer4;
                    break;
                case LightType.Buzzer5:
                    light = _buzzer5;
                    break;
            }

            if (light == null)
            {
                LOG.Write($"Undefined light {lightType}");
                return;
            }

            switch (state)
            {
                case LightState.On:
                    _cmdSetPoint[light] = TowerLightStatus.On;
                    light.StateSetPoint = TowerLightStatus.On;
                    break;
                case LightState.Off:
                    _cmdSetPoint[light] = TowerLightStatus.Off;
                    light.StateSetPoint = TowerLightStatus.Off;
                    break;
                case LightState.Blink:
                    _cmdSetPoint[light] = TowerLightStatus.Blinking;
                    light.Interval = blinkInterval;
                    light.StateSetPoint = TowerLightStatus.Blinking;
                    break;
            }

        }

        public virtual bool CustomSignalTower(string configPathFile)
        {
            try
            {
                STEvents config = CustomXmlSerializer.Deserialize<STEvents>(new FileInfo(configPathFile));
                lock (_locker)
                {
                    foreach (STEvent e in config.Events)
                    {
                        if (!_config.ContainsKey(e.Name))
                        {
                            _config[e.Name] = new Dictionary<IoSignalLight, TowerLightStatus>();
                        }

                        if (_red != null)
                            _config[e.Name][_red] = ParseLight(e.Red);
                        if (_yellow != null)
                            _config[e.Name][_yellow] = ParseLight(e.Yellow);
                        if (_green != null)
                            _config[e.Name][_green] = ParseLight(e.Green);
                        if (_blue != null)
                            _config[e.Name][_blue] = ParseLight(e.Blue);
                        if (_white != null)
                            _config[e.Name][_white] = ParseLight(e.White);

                        if (_buzzer != null)
                            _config[e.Name][_buzzer] = ParseLight(e.Buzzer);

                        if (_buzzer1 != null)
                            _config[e.Name][_buzzer1] = ParseLight(e.Buzzer1);
                        if (_buzzer2 != null)
                            _config[e.Name][_buzzer2] = ParseLight(e.Buzzer2);
                        if (_buzzer3 != null)
                            _config[e.Name][_buzzer3] = ParseLight(e.Buzzer3);
                        if (_buzzer4 != null)
                            _config[e.Name][_buzzer4] = ParseLight(e.Buzzer4);
                        if (_buzzer5 != null)
                            _config[e.Name][_buzzer5] = ParseLight(e.Buzzer5);
                    }
                }
            }
            catch (Exception e)
            {
                EV.PostWarningLog(Module, $"Signal tower config file is invalid, {e.Message}");
                return false;
            }
            return true;
        }

        public virtual void Monitor()
        {
            //Dictionary<IoSignalLight, TowerLightStatus> dicState = new Dictionary<IoSignalLight, TowerLightStatus>();
            if (_red != null)
                dicState[_red] = TowerLightStatus.Off;
            if (_yellow != null)
                dicState[_yellow] = TowerLightStatus.Off;
            if (_green != null)
                dicState[_green] = TowerLightStatus.Off;
            if (_blue != null)
                dicState[_blue] = TowerLightStatus.Off;
            if (_white != null)
                dicState[_white] = TowerLightStatus.Off;

            if (_buzzer != null)
                dicState[_buzzer] = TowerLightStatus.Off;

            if (_buzzer1 != null)
                dicState[_buzzer1] = TowerLightStatus.Off;
            if (_buzzer2 != null)
	            dicState[_buzzer2] = TowerLightStatus.Off;
            if (_buzzer3 != null)
	            dicState[_buzzer3] = TowerLightStatus.Off;
            if (_buzzer4 != null)
	            dicState[_buzzer4] = TowerLightStatus.Off;
            if (_buzzer5 != null)
                dicState[_buzzer5] = TowerLightStatus.Off;

            foreach (var trigCondition in _config)
            {
                var conditionValue = DATA.Poll(trigCondition.Key);
                if (conditionValue == null)
                    continue;
                
                bool isTrig = (bool)conditionValue;
                if (isTrig)
                {
                    if (_red != null)
                    {
                        dicState[_red] = MergeCondition(dicState[_red], trigCondition.Value[_red]);
                    }

                    if (_yellow != null)
                    {
                        dicState[_yellow] = MergeCondition(dicState[_yellow], trigCondition.Value[_yellow]);
                    }

                    if (_green != null)
                    {
                        dicState[_green] = MergeCondition(dicState[_green], trigCondition.Value[_green]);
                    }

                    if (_white != null)
                    {
                        dicState[_white] = MergeCondition(dicState[_white], trigCondition.Value[_white]);
                    }

                    if (_blue != null)
                    {
                        dicState[_blue] = MergeCondition(dicState[_blue], trigCondition.Value[_blue]);
                    }

                    if (_buzzer != null)
                    {
                        dicState[_buzzer] = _buzzerSwitchOff ? TowerLightStatus.Off : MergeCondition(dicState[_buzzer], trigCondition.Value[_buzzer]);
                    }

                    if (_buzzer1 != null)
                    {
                        dicState[_buzzer1] = _buzzer1SwitchOff ? TowerLightStatus.Off : MergeCondition(dicState[_buzzer1], trigCondition.Value[_buzzer1]);
                    }
                    if (_buzzer2 != null)
                    {
	                    dicState[_buzzer2] = _buzzer2SwitchOff ? TowerLightStatus.Off : MergeCondition(dicState[_buzzer2], trigCondition.Value[_buzzer2]);
					}
					if (_buzzer3 != null)
					{
						dicState[_buzzer3] = _buzzer3SwitchOff ? TowerLightStatus.Off : MergeCondition(dicState[_buzzer3], trigCondition.Value[_buzzer3]);
					}
					if (_buzzer4 != null)
					{
						dicState[_buzzer4] = _buzzer4SwitchOff ? TowerLightStatus.Off : MergeCondition(dicState[_buzzer4], trigCondition.Value[_buzzer4]);
					}
                    if (_buzzer5 != null)
                    {
                        dicState[_buzzer5] = _buzzer5SwitchOff ? TowerLightStatus.Off : MergeCondition(dicState[_buzzer5], trigCondition.Value[_buzzer5]);
                    }
                }
            }

            if (_config.Count > 0 && !HostControl)
            {
                SetLight(_red, dicState);
                SetLight(_blue, dicState);
                SetLight(_yellow, dicState);
                SetLight(_green, dicState);
                SetLight(_white, dicState);

                SetLight(_buzzer, dicState);

                SetLight(_buzzer1, dicState);
                SetLight(_buzzer2, dicState);
                SetLight(_buzzer3, dicState);
                SetLight(_buzzer4, dicState);
                SetLight(_buzzer5, dicState);
            }


            MonitorLight(_red);
            MonitorLight(_blue);
            MonitorLight(_yellow);
            MonitorLight(_green);
            MonitorLight(_white);

            MonitorLight(_buzzer);

            MonitorLight(_buzzer1);
            MonitorLight(_buzzer2);
            MonitorLight(_buzzer3);
            MonitorLight(_buzzer4);
            MonitorLight(_buzzer5);
        }

        public virtual void Reset()
        {
            ResetLight(_red );
            ResetLight(_blue );
            ResetLight(_yellow );
            ResetLight(_green );
            ResetLight(_white);

            ResetLight(_buzzer );

            _buzzerSwitchOff = false;

            _buzzer1SwitchOff = false;
            _buzzer2SwitchOff = false;
            _buzzer3SwitchOff = false;
            _buzzer4SwitchOff = false;
            _buzzer5SwitchOff = false;
		}

        public virtual bool SwitchOffBuzzer(string cmd, object[] objs)
        {
            if (cmd == $"{ Module}.{ Name}.{ AITSignalTowerOperation.SwitchOffBuzzer}" && _buzzer != null && _buzzer.StateSetPoint != TowerLightStatus.Off)
            {
                _buzzerSwitchOff = true;
                _buzzer1SwitchOff = true;
            }

            if (cmd == $"{ Module}.{ Name}.{ AITSignalTowerOperation.SwitchOffBuzzer1}"&&_buzzer1 != null && _buzzer1.StateSetPoint != TowerLightStatus.Off)
            {
                _buzzer1SwitchOff = true;
            }
	        if (cmd == $"{ Module}.{ Name}.{ AITSignalTowerOperation.SwitchOffBuzzer2}" && _buzzer2 != null && _buzzer2.StateSetPoint != TowerLightStatus.Off)
	        {
		        _buzzer2SwitchOff = true;
			}
			if (cmd == $"{ Module}.{ Name}.{ AITSignalTowerOperation.SwitchOffBuzzer3}" && _buzzer3 != null && _buzzer3.StateSetPoint != TowerLightStatus.Off)
			{
				_buzzer3SwitchOff = true;
			}
			if (cmd == $"{ Module}.{ Name}.{ AITSignalTowerOperation.SwitchOffBuzzer4}" && _buzzer4 != null && _buzzer4.StateSetPoint != TowerLightStatus.Off)
			{
				_buzzer4SwitchOff = true;
			}
			return true;
        }

        private void SetLight(IoSignalLight light, Dictionary<IoSignalLight, TowerLightStatus> state)
        {
            if (light != null)
            {
                light.StateSetPoint = state[light];
            }
        }
        private void ResetLight(IoSignalLight light )
        {
            if (light != null)
            {
                light.Reset();
            }
        }

        private void MonitorLight(IoSignalLight light)
        {
            if (light != null)
            {
                light.Monitor();
            }
        }

        public void Terminate()
        {
        }

        protected TowerLightStatus MergeCondition(TowerLightStatus newValue, TowerLightStatus oldValue)
        {
            if (newValue == TowerLightStatus.Blinking || oldValue == TowerLightStatus.Blinking)
                return TowerLightStatus.Blinking;

            if (newValue == TowerLightStatus.On || oldValue == TowerLightStatus.On)
                return TowerLightStatus.On;

            return TowerLightStatus.Off;
        }
 
        protected TowerLightStatus ParseLight(string light)
        {
            if (string.IsNullOrEmpty(light))
                return TowerLightStatus.Off;

            TowerLightStatus result = TowerLightStatus.Unknown;
            light = light.Trim().ToLower();
            switch (light)
            {
                case "on":
                    result = TowerLightStatus.On;
                    break;
                case "off":
                    result = TowerLightStatus.Off;
                    break;
                case "blinking":
                    result = TowerLightStatus.Blinking;
                    break;
                default:
                    LOG.Write("signal tower config file has invalid set, " + light);
                    break;
            }

            return result;
        }
    }

    public class IoLoadPortSignal : BaseDevice, IDevice
    {
        //device  

        private IoSignalLight _lightCassette1 = null;
        private IoSignalLight _lightCassette2 = null;
        private IoSignalLight _lightCassette3 = null;
        private IoSignalLight _lightCassette4 = null;
        private IoSignalLight _lightCassettePlacement = null;
        private IoSignalLight _lightAlarm = null;


       


        public bool HostControl { get; set; }
        

        public virtual AITSignalTowerData DeviceData
        {
            get
            {
                AITSignalTowerData data = new AITSignalTowerData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,

                };

                return data;
            }
        }

        public Dictionary<IoSignalLight, TowerLightStatus> SignalTowerState
        {
            get { return dicState; }
        }

        public IoSignalLight LightCassette1
        {
            get { return _lightCassette1; }
        }
        public IoSignalLight LightCassette2
        {
            get { return _lightCassette2; }
        }
        public IoSignalLight LightCassette3
        {
            get { return _lightCassette3; }
        }
        public IoSignalLight LightCassette4
        {
            get { return _lightCassette4; }
        }
        public IoSignalLight LightCassettePlacement
        {
            get { return _lightCassettePlacement; }
        }
        public IoSignalLight LightAlarm
        {
            get { return _lightAlarm; }
        }




        private object _locker = new object();

        private Dictionary<string, Dictionary<IoSignalLight, TowerLightStatus>> _config =
            new Dictionary<string, Dictionary<IoSignalLight, TowerLightStatus>>();

        private Dictionary<IoSignalLight, TowerLightStatus> _cmdSetPoint = new Dictionary<IoSignalLight, TowerLightStatus>();
        private Dictionary<IoSignalLight, TowerLightStatus> dicState = new Dictionary<IoSignalLight, TowerLightStatus>();

        public IoLoadPortSignal(string module, XmlElement node, string ioModule = "")
        {
            if (node == null)
                return;

            base.Module = node.GetAttribute("module");
            base.Name = node.GetAttribute("id");
            base.Display = node.GetAttribute("display");
            base.DeviceID = node.GetAttribute("schematicId");

            DOAccessor doCS1 = ParseDoNode("DO_LP1_CS1", node, ioModule);
            if (doCS1 != null)
            {
                _lightCassette1 = new IoSignalLight(module, "SignalLightCS1", "CS1 Light", "SignalLightCS1", doCS1);
            }

            DOAccessor doCS2 = ParseDoNode("DO_LP1_CS2", node, ioModule);
            if (doCS2 != null)
            {
                _lightCassette2 = new IoSignalLight(module, "SignalLightCS2", "CS2 Light", "SignalLightCS2", doCS2);
            }

            DOAccessor doCS3 = ParseDoNode("DO_LP1_CS3", node, ioModule);
            if (doCS3 != null)
            {
                _lightCassette3 = new IoSignalLight(module, "SignalLightCS3", "CS1 Light", "SignalLightCS3", doCS3);
            }

            DOAccessor doCS4 = ParseDoNode("DO_LP1_CS4", node, ioModule);
            if (doCS4 != null)
            {
                _lightCassette4 = new IoSignalLight(module, "SignalLightCS4", "CS4 Light", "SignalLightCS4", doCS4);
            }
        }

        public virtual bool Initialize()
        {
            OP.Subscribe($"{Module}.{Name}.{AITSignalTowerOperation.SwitchOffBuzzer}", SwitchOffBuzzer);
            

            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            return true;
        }

        public void SetLight(LoadPortLightType lightType, LightState state, int blinkInterval = 500)
        {
            IoSignalLight light = null;
            switch (lightType)
            {
                case LoadPortLightType.CassetteNO1:
                    light = _lightCassette1;
                    break;
                case LoadPortLightType.CassetteNO2:
                    light = _lightCassette2;
                    break;
                case LoadPortLightType.CassetteNO3:
                    light = _lightCassette3;
                    break;
                case LoadPortLightType.CassetteNO4:
                    light = _lightCassette4; ;
                    break;
                case LoadPortLightType.CasseettePlacement:
                    light = _lightCassettePlacement; ;
                    break;
                case LoadPortLightType.LoadPortAlarm:
                    light = _lightAlarm; ;
                    break;               
            }

            if (light == null)
            {
                LOG.Write($"Undefined light {lightType}");
                return;
            }

            switch (state)
            {
                case LightState.On:
                    _cmdSetPoint[light] = TowerLightStatus.On;
                    light.StateSetPoint = TowerLightStatus.On;
                    break;
                case LightState.Off:
                    _cmdSetPoint[light] = TowerLightStatus.Off;
                    light.StateSetPoint = TowerLightStatus.Off;
                    break;
                case LightState.Blink:
                    _cmdSetPoint[light] = TowerLightStatus.Blinking;
                    light.Interval = blinkInterval;
                    light.StateSetPoint = TowerLightStatus.Blinking;
                    break;
            }

        }

        public virtual bool CustomSignalTower(string configPathFile)
        {
            try
            {
                STEvents config = CustomXmlSerializer.Deserialize<STEvents>(new FileInfo(configPathFile));
                lock (_locker)
                {
                    foreach (STEvent e in config.Events)
                    {
                        if (!_config.ContainsKey(e.Name))
                        {
                            _config[e.Name] = new Dictionary<IoSignalLight, TowerLightStatus>();
                        }

                        
                    }
                }
            }
            catch (Exception e)
            {
                EV.PostWarningLog(Module, $"Signal tower config file is invalid, {e.Message}");
                return false;
            }
            return true;
        }

        public virtual void Monitor()
        {
            //Dictionary<IoSignalLight, TowerLightStatus> dicState = new Dictionary<IoSignalLight, TowerLightStatus>();
            if (_lightCassette1 != null)
                dicState[_lightCassette1] = TowerLightStatus.Off;
            if (_lightCassette2 != null)
                dicState[_lightCassette2] = TowerLightStatus.Off;
            if (_lightCassette3 != null)
                dicState[_lightCassette3] = TowerLightStatus.Off;
            if (_lightCassette4 != null)
                dicState[_lightCassette4] = TowerLightStatus.Off;
            if (_lightCassettePlacement != null)
                dicState[_lightCassettePlacement] = TowerLightStatus.Off;
            if (_lightAlarm != null)
                dicState[_lightAlarm] = TowerLightStatus.Off;
            


            MonitorLight(_lightCassette1);
            MonitorLight(_lightCassette2);
            MonitorLight(_lightCassette3);
            MonitorLight(_lightCassette4);
            MonitorLight(_lightCassettePlacement);

            MonitorLight(_lightAlarm);

        }

        public virtual void Reset()
        {
            ResetLight(_lightCassette1);
            ResetLight(_lightCassette2);
            ResetLight(_lightCassette3);
            ResetLight(_lightCassette4);
            ResetLight(_lightCassettePlacement);

            ResetLight(_lightAlarm);

          
        }

        public virtual bool SwitchOffBuzzer(string cmd, object[] objs)
        {

            return true;
        }

        private void SetLight(IoSignalLight light, Dictionary<IoSignalLight, TowerLightStatus> state)
        {
            if (light != null)
            {
                light.StateSetPoint = state[light];
            }
        }
        private void ResetLight(IoSignalLight light)
        {
            if (light != null)
            {
                light.Reset();
            }
        }

        private void MonitorLight(IoSignalLight light)
        {
            if (light != null)
            {
                light.Monitor();
            }
        }

        public void Terminate()
        {
        }

        protected TowerLightStatus MergeCondition(TowerLightStatus newValue, TowerLightStatus oldValue)
        {
            if (newValue == TowerLightStatus.Blinking || oldValue == TowerLightStatus.Blinking)
                return TowerLightStatus.Blinking;

            if (newValue == TowerLightStatus.On || oldValue == TowerLightStatus.On)
                return TowerLightStatus.On;

            return TowerLightStatus.Off;
        }

        protected TowerLightStatus ParseLight(string light)
        {
            if (string.IsNullOrEmpty(light))
                return TowerLightStatus.Off;

            TowerLightStatus result = TowerLightStatus.Unknown;
            light = light.Trim().ToLower();
            switch (light)
            {
                case "on":
                    result = TowerLightStatus.On;
                    break;
                case "off":
                    result = TowerLightStatus.Off;
                    break;
                case "blinking":
                    result = TowerLightStatus.Blinking;
                    break;
                default:
                    LOG.Write("signal tower config file has invalid set, " + light);
                    break;
            }

            return result;
        }
    }

    public enum LoadPortLightType
    {
        CassetteNO1,
        CassetteNO2,
        CassetteNO3,
        CassetteNO4,
        CasseettePlacement,
        LoadPortAlarm,
    }
}

