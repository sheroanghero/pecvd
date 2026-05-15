using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Aitex.Core.RT.IOCore;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.IOCore;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.IO;

namespace MECF.Framework.UI.Client.CenterViews.Maitenances.IO3
{
    /// <summary>
    /// 与IO2ViewModel的区别是，这个用Float类型的AI , AO 
    /// </summary>
    public class IO3ViewModel : UiViewModelBase, ISupportMultipleSystem
    {
        public bool IsPermission { get => this.Permission == 3; }

        public string SystemName { get; set; }

        public Visibility DIVisibility
        {
            get { return DIs.Count > 0 ? Visibility.Visible : Visibility.Collapsed; }
        }
        public Visibility DOVisibility
        {
            get { return DOs.Count > 0 ? Visibility.Visible : Visibility.Collapsed; }
        }
        public Visibility AIVisibility
        {
            get { return AIs.Count > 0 ? Visibility.Visible : Visibility.Collapsed; }
        }
        public Visibility AOVisibility
        {
            get { return AOs.Count > 0 ? Visibility.Visible : Visibility.Collapsed; }
        }

        public int DIWidth
        {
            get { return (DIs.Count / 31 + 1) * 355; }
        }
        public int DOWidth
        {
            get { return (DOs.Count / 31 + 1) * 405; }
        }
        public int AIWidth
        {
            get { return (AIs.Count / 31 + 1) * 370; }
        }
        public int AOWidth
        {
            get { return (AOs.Count / 31 + 1) * 370; }
        }

        public ObservableCollection<IOItem<float>> AIs { get; private set; }
        public ObservableCollection<AOItemFloat> AOs { get; private set; }
        public ObservableCollection<IOItem<bool>> DIs { get; private set; }
        public ObservableCollection<IOItem<bool>> DOs { get; private set; }

        private string _diKey;
        private string _doKey;
        private string _aiKey;
        private string _aoKey;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _diKey = $"{SystemName}.DIItemList";
            _doKey = $"{SystemName}.DOItemList";
            _aiKey = $"{SystemName}.AIItemList";
            _aoKey = $"{SystemName}.AOItemList";

            this.AIs = InitIOData<float>(IOType.AI, _aiKey);
            this.AOs = InitIOData(IOType.AO, _aoKey);
            this.DIs = InitIOData<bool>(IOType.DI, _diKey);
            this.DOs = InitIOData<bool>(IOType.DO, _doKey);


            _diKey = $"{SystemName}.DIList";
            _doKey = $"{SystemName}.DOList";
            _aiKey = $"{SystemName}.AIList";
            _aoKey = $"{SystemName}.AOList";
            Subscribe(_aiKey);
            Subscribe(_aoKey);
            Subscribe(_diKey);
            Subscribe(_doKey);
        }
        protected override void OnActivate()
        {
            base.OnActivate();
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
        }

        protected override void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {
            base.InvokeAfterUpdateProperty(data);

            if (data.ContainsKey(_aiKey) &&  data[_aiKey] != null)
            {
                List<NotifiableIoItem> lstData = (List<NotifiableIoItem>)data[_aiKey];
                Dictionary<string, float> dicValues = new Dictionary<string, float>();

                for (int i = 0; i < lstData.Count; i++)
                {
                    dicValues[lstData[i].Name] = lstData[i].FloatValue;
                }

                foreach (IOItem<float> item in AIs)
                {
                    if (dicValues.ContainsKey(item.Name))
                        item.Value = dicValues[item.Name];
                }
            }
            if (data.ContainsKey(_aoKey) && data[_aoKey] != null)
            {
                List<NotifiableIoItem> lstData = (List<NotifiableIoItem>)data[_aoKey];
                Dictionary<string, float> dicValues = new Dictionary<string, float>();

                for (int i = 0; i < lstData.Count; i++)
                {
                    dicValues[lstData[i].Name] = lstData[i].FloatValue;
                }

                foreach (IOItem<float> item in AOs)
                {
                    if (dicValues.ContainsKey(item.Name))
                        item.Value = dicValues[item.Name];
                }
            }

            if (data.ContainsKey(_diKey) && data[_diKey] != null)
            {
                List<NotifiableIoItem> lstData = (List<NotifiableIoItem>)data[_diKey];
                Dictionary<string, bool> dicValues = new Dictionary<string, bool>();

                for (int i = 0; i < lstData.Count; i++)
                {
                    dicValues[lstData[i].Name] = lstData[i].BoolValue;
                }

                foreach (IOItem<bool> item in DIs)
                {
                    if (dicValues.ContainsKey(item.Name))
                        item.Value = dicValues[item.Name];
                }
            }
            if (data.ContainsKey(_doKey) && data[_doKey] != null)
            {
                List<NotifiableIoItem> lstData = (List<NotifiableIoItem>)data[_doKey];
                Dictionary<string, bool> dicValues = new Dictionary<string, bool>();

                for (int i = 0; i < lstData.Count; i++)
                {
                    dicValues[lstData[i].Name] = lstData[i].BoolValue;
                }

                foreach (IOItem<bool> item in DOs)
                {
                    if (dicValues.ContainsKey(item.Name))
                        item.Value = dicValues[item.Name];
                }
            }
        }

        public void SetDO(IOItem<bool> doItem)
        {
            if (!DialogBox.Confirm(
                $"Please be attention, direct control DO is generally forbidden, Are you sure you want to do the operation?\r\n {doItem.Name} = {!doItem.Value}",
                "Warning"))
                return;

            InvokeClient.Instance.Service.DoOperation("System.SetDoValue", doItem.Name, !doItem.Value);
        }

        public void SetAO(AOItemFloat AOItemFloat)
        {
            if (!DialogBox.Confirm(
                $"Please be attention, direct control AO is generally forbidden, Are you sure you want to do the operation?\r\n {AOItemFloat.Name} = {AOItemFloat.NewValue}",
                "Warning" ))
                return;

            InvokeClient.Instance.Service.DoOperation("System.SetAoValueFloat", AOItemFloat.Name, AOItemFloat.NewValue);

            AOItemFloat.TextSaved = true;
        }

        public ObservableCollection<IOItem<T>> InitIOData<T>(IOType type, string dataName)
        {
            //get the whole informations
            ObservableCollection<IOItem<T>> da = new ObservableCollection<IOItem<T>>();

            if (type == IOType.DI)
            {
                var diList = QueryDataClient.Instance.Service.GetData(dataName);

                if (diList != null)
                {
                    List<NotifiableIoItem> item = (List<NotifiableIoItem>)diList;
                    for (int i = 0; i < item.Count; i++)
                    {
                        bool value = true;
                        if (value is T)
                        {
                            da.Add(new IOItem<T>()
                            {
                                Index = item[i].Index,
                                Name = item[i].Name,
                                DisplayName = item[i].Name.Substring(item[i].Name.IndexOf('.') + 1),
                                Value = (T)(object)item[i].BoolValue,
                                Address = item[i].Address
                            });
                        }
                    }
                }
            }

            if (type == IOType.DO)
            {
                var diList = QueryDataClient.Instance.Service.GetData(dataName);

                if (diList != null)
                {
                    List<NotifiableIoItem> item = (List<NotifiableIoItem>)diList;
                    for (int i = 0; i < item.Count; i++)
                    {
                        bool value = true;
                        if (value is T)
                        {
                            da.Add(new IOItem<T>()
                            {
                                Index = item[i].Index,
                                Name = item[i].Name,
                                DisplayName = item[i].Name.Substring(item[i].Name.IndexOf('.') + 1),
                                Value = (T)(object)item[i].BoolValue,
                                Address = item[i].Address
                            });
                        }
                    }
                }
            }


            if (type == IOType.AI)
            {
                var diList = QueryDataClient.Instance.Service.GetData(dataName);

                if (diList != null)
                {
                    List<NotifiableIoItem> item = (List<NotifiableIoItem>)diList;
                    for (int i = 0; i < item.Count; i++)
                    {
                        da.Add(new IOItem<T>()
                        {
                            Index = item[i].Index,
                            Name = item[i].Name,
                            DisplayName = item[i].Name.Substring(item[i].Name.IndexOf('.') + 1),
                            Value = (T)(object)item[i].FloatValue,
                            Address = item[i].Address
                        });

                    }
                }
            }


            return da;
        }

        public ObservableCollection<AOItemFloat> InitIOData(IOType type, string dataName)
        {
            //get the whole informations
            ObservableCollection<AOItemFloat> da = new ObservableCollection<AOItemFloat>();

            if (type == IOType.AO)
            {
                var diList = QueryDataClient.Instance.Service.GetData(dataName);

                if (diList != null)
                {
                    List<NotifiableIoItem> item = (List<NotifiableIoItem>)diList;
                    for (int i = 0; i < item.Count; i++)
                    {
                        {
                            da.Add(new AOItemFloat()
                            {
                                Index = item[i].Index,
                                Name = item[i].Name,
                                DisplayName = item[i].Name.Substring(item[i].Name.IndexOf('.')+1),
                                Value = item[i].FloatValue,
                                Address = item[i].Address
                            });
                        }
                    }
                }
            }
            return da;
        }

    }
}
