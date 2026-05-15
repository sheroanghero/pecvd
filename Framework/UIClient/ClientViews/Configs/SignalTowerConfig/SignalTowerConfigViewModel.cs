using Aitex.Core.RT.Log;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.ClientBase;
using SciChart.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml;

namespace MECF.Framework.UI.Client.CenterViews.Configs.SignalTowerConfig
{
    public class SignalTowerConfigViewModel : ModuleUiViewModelBase, ISupportMultipleSystem
    {
        public class SignalTowerItem : NotifiableItem
        {
            private string _name;
            public string Name
            {
                get { return _name; }
                set { _name = value; InvokePropertyChanged(nameof(Name)); }
            }

            private bool _isRed;
            public bool IsRed
            {
                get { return _isRed; }
                set
                {
                    _isRed = value;
                    if (value) IsRedBlinking = false; InvokePropertyChanged(nameof(IsRed));
                }
            }

            private bool _isRedBlinking;
            public bool IsRedBlinking
            {
                get { return _isRedBlinking; }
                set
                {
                    _isRedBlinking = value;
                    if (value) IsRed = false; InvokePropertyChanged(nameof(IsRedBlinking));
                }
            }

            private bool _isGreen;
            public bool IsGreen
            {
                get { return _isGreen; }
                set
                {
                    _isGreen = value;
                    if (value) IsGreenBlinking = false; InvokePropertyChanged(nameof(IsGreen));
                }
            }

            private bool _isGreenBlinking;
            public bool IsGreenBlinking
            {
                get { return _isGreenBlinking; }
                set
                {
                    _isGreenBlinking = value;
                    if (value) IsGreen = false; InvokePropertyChanged(nameof(IsGreenBlinking));
                }
            }

            private bool _isBlue;
            public bool IsBlue
            {
                get { return _isBlue; }
                set
                {
                    _isBlue = value;
                    if (value) IsBlueBlinking = false; InvokePropertyChanged(nameof(IsBlue));
                }
            }
            private bool _isBlueBlinking;
            public bool IsBlueBlinking
            {
                get { return _isBlueBlinking; }
                set
                {
                    _isBlueBlinking = value;
                    if (value) IsBlue = false; InvokePropertyChanged(nameof(IsBlueBlinking));
                }
            }

            private bool _isYellow;
            public bool IsYellow
            {
                get { return _isYellow; }
                set
                {
                    _isYellow = value;
                    if (value) IsYellowBlinking = false; InvokePropertyChanged(nameof(IsYellow));
                }
            }
            private bool _isYellowBlinking;
            public bool IsYellowBlinking
            {
                get { return _isYellowBlinking; }
                set
                {
                    _isYellowBlinking = value;
                    if (value) IsYellow = false; InvokePropertyChanged(nameof(IsYellowBlinking));
                }
            }

            private bool _isBuzzer;
            public bool IsBuzzer
            {
                get { return _isBuzzer; }
                set
                {
                    _isBuzzer = value;
                    if (value) IsBuzzerBlinking = false; InvokePropertyChanged(nameof(IsBuzzer));
                }
            }

            private bool _isBuzzerBlinking;
            public bool IsBuzzerBlinking
            {
                get { return _isBuzzerBlinking; }
                set
                {
                    _isBuzzerBlinking = value;
                    if (value) IsBuzzer = false; InvokePropertyChanged(nameof(IsBuzzerBlinking));
                }
            }
        }

        #region Properties

        public ObservableCollection<SignalTowerItem> SignalTowerData { get; set; }

        private string _contentDataGroup;

        private XmlDocument _xmlSignalTower;

        private List<string> _lstItems = new List<string>();

        #endregion

        #region Functions
        public SignalTowerConfigViewModel()
        {
            this.DisplayName = "SignalTower Config ";

            SignalTowerData = new ObservableCollection<SignalTowerItem>();

        }

        protected override void OnInitialize()
        {
            base.OnInitialize();


        }

        protected override void OnActivate()
        {
            base.OnActivate();

            UpdateData();
        }

        private void UpdateData()
        {


            if (_xmlSignalTower == null)
            {
                string contentSignal = QueryDataClient.Instance.Service.GetTypedConfigContent("SignalTower", "");

                try
                {
                    _xmlSignalTower = new XmlDocument();
                    _xmlSignalTower.LoadXml(contentSignal);

                    var items = _xmlSignalTower.SelectNodes($"STEvents/STEvent");

                    foreach (var item in items)
                    {
                        XmlElement element = item as XmlElement;

                        SignalTowerItem stItem = new SignalTowerItem();

                        stItem.Name = element.GetAttribute("name");
                        stItem.IsRed = string.Compare(element.GetAttribute("Red"), "on", StringComparison.CurrentCultureIgnoreCase) == 0;
                        stItem.IsRedBlinking = string.Compare(element.GetAttribute("Red"), "blinking", StringComparison.CurrentCultureIgnoreCase) == 0;

                        stItem.IsYellow = string.Compare(element.GetAttribute("Yellow"), "on", StringComparison.CurrentCultureIgnoreCase) == 0;
                        stItem.IsYellowBlinking = string.Compare(element.GetAttribute("Yellow"), "blinking", StringComparison.CurrentCultureIgnoreCase) == 0;

                        stItem.IsGreen = string.Compare(element.GetAttribute("Green"), "on", StringComparison.CurrentCultureIgnoreCase) == 0;
                        stItem.IsGreenBlinking = string.Compare(element.GetAttribute("Green"), "blinking", StringComparison.CurrentCultureIgnoreCase) == 0;

                        stItem.IsBlue = string.Compare(element.GetAttribute("Blue"), "on", StringComparison.CurrentCultureIgnoreCase) == 0;
                        stItem.IsBlueBlinking = string.Compare(element.GetAttribute("Blue"), "blinking", StringComparison.CurrentCultureIgnoreCase) == 0;

                        stItem.IsBuzzer = string.Compare(element.GetAttribute("Buzzer1"), "on", StringComparison.CurrentCultureIgnoreCase) == 0;
                        stItem.IsBuzzerBlinking = string.Compare(element.GetAttribute("Buzzer1"), "blinking", StringComparison.CurrentCultureIgnoreCase) == 0;

                        SignalTowerData.Add(stItem);

                        stItem.InvokePropertyChanged();
                    }
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }
            }

            string content = QueryDataClient.Instance.Service.GetTypedConfigContent("DataGroup", "");

            if (_contentDataGroup == content)
                return;
            try
            {
                XmlDocument xmlContent = new XmlDocument();
                xmlContent.LoadXml(content);

                _contentDataGroup = content;

                var groups = xmlContent.SelectNodes($"DataGroupConfig/DataGroup[@name='SignalTower']/DataItem");

                List<string> names = new List<string>();
                foreach (var item in groups)
                {
                    XmlElement element = item as XmlElement;

                    string name = element.GetAttribute("name");

                    names.Add(name);

                    if (SignalTowerData.FirstOrDefault(x => x.Name == name) == null)
                    {
                        SignalTowerData.Add(new SignalTowerItem() { Name = name, });
                    }
                }

                SignalTowerData.RemoveWhere(x => !names.Contains(x.Name));
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public void SaveSelection()
        {
            var nodeGroup = _xmlSignalTower.SelectSingleNode($"STEvents");

            var nodeItem = _xmlSignalTower.SelectNodes($"STEvents/STEvent");
            foreach (var nodeGroupChildNode in nodeItem)
            {
                var node = nodeGroupChildNode as XmlElement;
                nodeGroup.RemoveChild(node);
            }

            foreach (var item in SignalTowerData)
            {
                var node = _xmlSignalTower.CreateElement("STEvent");
                node.SetAttribute("name", item.Name);
                node.SetAttribute("Red", item.IsRed ? "on" : item.IsRedBlinking ? "blinking" : "off");
                node.SetAttribute("Yellow", item.IsYellow ? "on" : item.IsYellowBlinking ? "blinking" : "off");
                node.SetAttribute("Blue", item.IsBlue ? "on" : item.IsBlueBlinking ? "blinking" : "off");
                node.SetAttribute("Green", item.IsGreen ? "on" : item.IsGreenBlinking ? "blinking" : "off");
                node.SetAttribute("Buzzer1", item.IsBuzzer ? "on" : item.IsBuzzerBlinking ? "blinking" : "off");
                nodeGroup.AppendChild(node);
            }

            QueryDataClient.Instance.Service.SetTypedConfigContent("SignalTower", "", _xmlSignalTower.InnerXml);
        }

        public void CancelSelection()
        {
            try
            {
                var items = _xmlSignalTower.SelectNodes($"STEvents/STEvent");

                foreach (var item in items)
                {
                    XmlElement element = item as XmlElement;

                    SignalTowerItem stItem = SignalTowerData.FirstOrDefault(x => x.Name == element.GetAttribute("name"));

                    if (stItem == null)
                        continue;

                    stItem.IsRed = string.Compare(element.GetAttribute("Red"), "on", StringComparison.CurrentCultureIgnoreCase) == 0;
                    stItem.IsRedBlinking = string.Compare(element.GetAttribute("Red"), "blinking", StringComparison.CurrentCultureIgnoreCase) == 0;

                    stItem.IsYellow = string.Compare(element.GetAttribute("Yellow"), "on", StringComparison.CurrentCultureIgnoreCase) == 0;
                    stItem.IsYellowBlinking = string.Compare(element.GetAttribute("Yellow"), "blinking", StringComparison.CurrentCultureIgnoreCase) == 0;

                    stItem.IsGreen = string.Compare(element.GetAttribute("Green"), "on", StringComparison.CurrentCultureIgnoreCase) == 0;
                    stItem.IsGreenBlinking = string.Compare(element.GetAttribute("Green"), "blinking", StringComparison.CurrentCultureIgnoreCase) == 0;

                    stItem.IsBlue = string.Compare(element.GetAttribute("Blue"), "on", StringComparison.CurrentCultureIgnoreCase) == 0;
                    stItem.IsBlueBlinking = string.Compare(element.GetAttribute("Blue"), "blinking", StringComparison.CurrentCultureIgnoreCase) == 0;

                    stItem.IsBuzzer = string.Compare(element.GetAttribute("Buzzer"), "on", StringComparison.CurrentCultureIgnoreCase) == 0;
                    stItem.IsBuzzerBlinking = string.Compare(element.GetAttribute("Buzzer"), "blinking", StringComparison.CurrentCultureIgnoreCase) == 0;

                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }


        #endregion
    }
}
