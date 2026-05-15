using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Aitex.Core.RT.IOCore;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.IOCore;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.RT.Core.IoProviders;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.IO;

namespace MECF.Framework.UI.Client.CenterViews.Maitenances.IO1
{
    public class IO1ViewModel : UiViewModelBase, ISupportMultipleSystem
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

        public ObservableCollection<IOItem<short>> AIs { get; private set; }
        public ObservableCollection<AOItem> AOs { get; private set; }
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

            this.AIs = InitIOData<short>(IOType.AI, _aiKey);
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

            if (data[_aiKey] != null)
            {
                List<NotifiableIoItem> lstData = (List<NotifiableIoItem>)data[_aiKey];
                Dictionary<string, short> dicValues = new Dictionary<string, short>();

                for (int i = 0; i < lstData.Count; i++)
                {
                    dicValues[lstData[i].Name] = lstData[i].ShortValue;
                }

                foreach (IOItem<short> item in AIs)
                {
                    if (dicValues.ContainsKey(item.Name))
                        item.Value = dicValues[item.Name];
                }
            }
            if (data[_aoKey] != null)
            {
                List<NotifiableIoItem> lstData = (List<NotifiableIoItem>)data[_aoKey];
                Dictionary<string, short> dicValues = new Dictionary<string, short>();

                for (int i = 0; i < lstData.Count; i++)
                {
                    dicValues[lstData[i].Name] = lstData[i].ShortValue;
                }

                foreach (IOItem<short> item in AOs)
                {
                    if (dicValues.ContainsKey(item.Name))
                        item.Value = dicValues[item.Name];
                }
            }

            if (data[_diKey] != null)
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
            if (data[_doKey] != null)
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
            if (MessageBox.Show(
                $"Please be attention, direct control DO is generally forbidden, Are you sure you want to do the operation?\r\n {doItem.Name} = {!doItem.Value}",
                "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            InvokeClient.Instance.Service.DoOperation("System.SetDoValue", doItem.Name, !doItem.Value);
        }

        public void SetAO(AOItem aoItem)
        {
            if (MessageBox.Show(
                $"Please be attention, direct control AO is generally forbidden, Are you sure you want to do the operation?\r\n {aoItem.Name} = {aoItem.NewValue}",
                "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            InvokeClient.Instance.Service.DoOperation("System.SetAoValue", aoItem.Name, aoItem.NewValue);

            aoItem.TextSaved = true;
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
                    List<NotifiableIoItem> di = (List<NotifiableIoItem>)diList;
                    for (int i = 0; i < di.Count; i++)
                    {
                        bool value = true;
                        if (value is T)
                        {
                            da.Add(new IOItem<T>()
                            {
                                Index = di[i].Index,
                                Name = di[i].Name ,
                                DisplayName = di[i].Name.Substring(di[i].Name.IndexOf('.') + 1),
                                Value = (T)(object)di[i].BoolValue,
                                Address = di[i].Address
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
                                Name = item[i].Name ,
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
                            Name = item[i].Name ,
                            DisplayName = item[i].Name.Substring(item[i].Name.IndexOf('.') + 1),
                            Value = (T)(object)item[i].ShortValue,
                            Address = item[i].Address
                        });

                    }
                }
            }


            return da;
        }

        public ObservableCollection<AOItem> InitIOData(IOType type, string dataName)
        {
            //get the whole informations
            ObservableCollection<AOItem> da = new ObservableCollection<AOItem>();

            if (type == IOType.AO)
            {
                var diList = QueryDataClient.Instance.Service.GetData(dataName);

                if (diList != null)
                {
                    List<NotifiableIoItem> item = (List<NotifiableIoItem>)diList;
                    for (int i = 0; i < item.Count; i++)
                    {
                        {
                            da.Add(new AOItem()
                            {
                                Index = item[i].Index,
                                Name = item[i].Name ,
                                DisplayName = item[i].Name.Substring(item[i].Name.IndexOf('.') + 1),
                                Value = item[i].ShortValue,
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
