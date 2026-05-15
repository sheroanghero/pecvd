using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using Aitex.Common.Util;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Device;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Util;

namespace MECF.Framework.Common.Device.Bases
{
    [Serializable]
    [DataContract]
    public enum TowerLightStatus
    {
        [EnumMember]
        Off,

        [EnumMember]
        On,

        [EnumMember]
        Blinking,

        [EnumMember]
        Unknown,
    }

    [Serializable]
    [DataContract]
    public enum LightState
    {
        [EnumMember]
        Off,

        [EnumMember]
        On,

        [EnumMember]
        Blink,

        [EnumMember]
        Unknown,
    }

    public enum LightType
    {
        Red,
        Yellow,
        Green,
        White,
        Blue,
        Buzzer,
        Buzzer1,
        Buzzer2,
        Buzzer3,
        Buzzer4,
        Buzzer5,
    }

    public class STEvents
    {
        [XmlElement(ElementName = "STEvent")]
        public List<STEvent> Events;
    }

    public class STEvent
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute]
        public string Red { get; set; }

        [XmlAttribute]
        public string Yellow { get; set; }

        [XmlAttribute]
        public string Green { get; set; }

        [XmlAttribute]
        public string Blue { get; set; }

        [XmlAttribute]
        public string Buzzer { get; set; }

        [XmlAttribute]
        public string White { get; set; }

        [XmlAttribute]
        public string Buzzer1 { get; set; }

        [XmlAttribute]
        public string Buzzer2 { get; set; }

        [XmlAttribute]
        public string Buzzer3 { get; set; }

        [XmlAttribute]
        public string Buzzer4 { get; set; }

        [XmlAttribute]
        public string Buzzer5 { get; set; }
    }

    /*
    <STEvents>
        <STEvent name = "System.IsAlarm"                  Red="On" Yellow="Off" Green="Off" Blue="Off" White=""  Buzzer="Off"/>
    </STEvents>

    */
    public abstract class SignalTowerBase : BaseDevice, IDevice
    {
        public bool IsAutoSetLight { get; set; }

        public AITSignalTowerData DeviceData
        {
            get
            {
                AITSignalTowerData data = new AITSignalTowerData()
                {
                    DeviceName = Name,
                    DeviceSchematicId = DeviceID,
                    DisplayName = Display,

                    IsGreenLightOn = _lights.ContainsKey(LightType.Green) && _lights[LightType.Green] != null && _lights[LightType.Green].Value,
                    IsRedLightOn = _lights.ContainsKey(LightType.Red) && _lights[LightType.Red] != null && _lights[LightType.Red].Value,
                    IsYellowLightOn = _lights.ContainsKey(LightType.Yellow) && _lights[LightType.Yellow] != null && _lights[LightType.Yellow].Value,
                    IsWhiteLightOn = _lights.ContainsKey(LightType.White) && _lights[LightType.White] != null && _lights[LightType.White].Value,
                    IsBlueLightOn = _lights.ContainsKey(LightType.Blue) && _lights[LightType.Blue] != null && _lights[LightType.Blue].Value,

                    IsBuzzerOn = _lights.ContainsKey(LightType.Buzzer) && _lights[LightType.Buzzer] != null && _lights[LightType.Buzzer].Value,

                    IsBuzzer1On = _lights.ContainsKey(LightType.Buzzer1) && _lights[LightType.Buzzer1] != null && _lights[LightType.Buzzer1].Value,
                    IsBuzzer2On = _lights.ContainsKey(LightType.Buzzer2) && _lights[LightType.Buzzer2] != null && _lights[LightType.Buzzer2].Value,
                    IsBuzzer3On = _lights.ContainsKey(LightType.Buzzer3) && _lights[LightType.Buzzer3] != null && _lights[LightType.Buzzer3].Value,
                    IsBuzzer4On = _lights.ContainsKey(LightType.Buzzer4) && _lights[LightType.Buzzer4] != null && _lights[LightType.Buzzer4].Value,
                    IsBuzzer5On = _lights.ContainsKey(LightType.Buzzer5) && _lights[LightType.Buzzer5] != null && _lights[LightType.Buzzer5].Value,
                };

                return data;
            }
        }

        private Dictionary<string, Dictionary<LightType, TowerLightStatus>> _config =
            new Dictionary<string, Dictionary<LightType, TowerLightStatus>>();

        protected Dictionary<LightType, SignalLightBase> _lights = new Dictionary<LightType, SignalLightBase>();

        private bool _isBuzzerOff;

        public SignalTowerBase( ) : base( )
        {
            IsAutoSetLight = true;
        }

        public SignalTowerBase(string module, string name):base(module, name, name, name)
        {
            IsAutoSetLight = true;
        }

        public virtual bool Initialize()
        {
            OP.Subscribe($"{Module}.{Name}.{AITSignalTowerOperation.SwitchOffBuzzer}", SwitchOffBuzzer);              
            DATA.Subscribe($"{Module}.{Name}.DeviceData", () => DeviceData);

            CustomSignalTower();

            return true;
        }

        public void SwitchOffBuzzer(bool isOff)
        {
            _isBuzzerOff = isOff;
        }

        private bool SwitchOffBuzzer(string arg1, object[] arg2)
        {
            _isBuzzerOff = true;
            return true;
        }

        public void Monitor()
        {
            try
            {
                if (IsAutoSetLight)
                {
                    MonitorSignalTowerAuto();
                }

                foreach (var signalLightBase in _lights)
                {
                    if (signalLightBase.Value != null)
                    {
                        signalLightBase.Value.Monitor();
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }



        public void Terminate()
        {

        }

        public void Reset()
        {
            foreach (var signalLightBase in _lights)
            {
                if (signalLightBase.Value != null)
                {
                    signalLightBase.Value.Reset();
                }
            }

            _isBuzzerOff = false;
        }

        public abstract SignalLightBase CreateLight(LightType type);

        public bool CustomSignalTower()
        {
            string pathFile = PathManager.GetCfgDir() + "_SignalTower.xml";
            if (!File.Exists(pathFile))
            {
                pathFile = PathManager.GetCfgDir() + "SignalTower.xml";
            }
            return CustomSignalTower(pathFile);
        }

        public bool CustomSignalTower(string configPathFile)
        {
            try
            {
                if (!File.Exists(configPathFile))
                {
                    MessageBox.Show($"Signal tower config file not exist, " + configPathFile, "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                foreach (int light in Enum.GetValues(typeof(LightType)))
                {
                    _lights[(LightType)light] = CreateLight((LightType)light);
                }

                STEvents config = CustomXmlSerializer.Deserialize<STEvents>(new FileInfo(configPathFile));

                List<LightType> configTypes = _lights.Keys.ToList();

                foreach (STEvent e in config.Events)
                {
                    if (!_config.ContainsKey(e.Name))
                    {
                        _config[e.Name] = new Dictionary<LightType, TowerLightStatus>();
                    }

                    foreach (LightType lightType in _lights.Keys)
                    {
                        PropertyInfo[] ps = e.GetType().GetProperties();
                        foreach (PropertyInfo p in ps)
                        {
                            if (p.Name.ToLower() == lightType.ToString().ToLower())
                            {

                                string value = (string)p.GetValue(e);
                                if (!string.IsNullOrEmpty(value))
                                {
                                    value = value.ToLower();
                                    if (value == "on")
                                        _config[e.Name][lightType] = TowerLightStatus.On;
                                    else if (value == "off")
                                        _config[e.Name][lightType] = TowerLightStatus.Off;
                                    if (value.Contains("blink")) //bink, blinking,
                                        _config[e.Name][lightType] = TowerLightStatus.Blinking;
                                    if (configTypes.Contains(lightType))
                                        configTypes.Remove(lightType);
                                }
 
                            }
                        }
                    }
                }

                foreach (var light in configTypes)
                {
                    _lights.Remove(light);
                }
            }
            catch (Exception e)
            {
                EV.PostWarningLog(Module, $"Signal tower config file invalid, {e.Message}");
                return false;
            }


            return true;
        }

        public void MonitorSignalTowerAuto()
        {
            Dictionary<LightType, TowerLightStatus> lightStateValue = new Dictionary<LightType, TowerLightStatus>();
            foreach (int light in Enum.GetValues(typeof(LightType)))
            {
                lightStateValue[(LightType)light] = TowerLightStatus.Off;
            }

            foreach (var trigCondition in _config)
            {
                var conditionValue = DATA.Poll(trigCondition.Key);
                if (conditionValue == null)
                    continue;

                if (!(conditionValue is bool))
                    continue;

                bool isTrig = (bool)conditionValue;
                if (isTrig)
                {
                    foreach (LightType lightType in trigCondition.Value.Keys)
                    {
                        if (IsBuzzer(lightType) && _isBuzzerOff)
                        {
                            lightStateValue[lightType] = TowerLightStatus.Off;
                        }
                        else
                        {
                            lightStateValue[lightType] =
                                MergeCondition(lightStateValue[lightType], trigCondition.Value[lightType]);
                        }
                    }

                    if(lightStateValue.ContainsKey(LightType.Red) && (lightStateValue[LightType.Red] == TowerLightStatus.Blinking || lightStateValue[LightType.Red] == TowerLightStatus.On))
                    {
                        if (lightStateValue.ContainsKey(LightType.Green))
                        {
                            lightStateValue[LightType.Green] = TowerLightStatus.Off;
                        }

                        if (lightStateValue.ContainsKey(LightType.Yellow))
                        {
                            lightStateValue[LightType.Yellow] = TowerLightStatus.Off;
                        }

                        if (lightStateValue.ContainsKey(LightType.Blue))
                        {
                            lightStateValue[LightType.Blue] = TowerLightStatus.Off;
                        }
                    }
                }
            }

            foreach (var light in _lights)
            {
                if (light.Value != null)
                {
                    light.Value.StateSetPoint = lightStateValue[light.Key];
                }
            }
        }

        private bool IsBuzzer(LightType type)
        {
            return type == LightType.Buzzer || type == LightType.Buzzer1 || type == LightType.Buzzer2 ||
                   type == LightType.Buzzer3 ||
                   type == LightType.Buzzer4 || type == LightType.Buzzer5;
        }

        private TowerLightStatus MergeCondition(TowerLightStatus newValue, TowerLightStatus oldValue)
        {
            if (newValue == TowerLightStatus.Blinking || oldValue == TowerLightStatus.Blinking)
                return TowerLightStatus.Blinking;

            if (newValue == TowerLightStatus.On || oldValue == TowerLightStatus.On)
                return TowerLightStatus.On;

            return TowerLightStatus.Off;
        }

    }
}
