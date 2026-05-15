using Aitex.Core.RT.Log;
using MECF.Framework.UI.Client.ClientBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Aitex.Core.RT.SCCore;
using Aitex.Core.UI.MVVM;
using Aitex.Core.Util;
using DocumentFormat.OpenXml.Drawing.Charts;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.OperationCenter;
using SciChart.Core.Extensions;

namespace MECF.Framework.UI.Client.CenterViews.Maitenances.CalibrationTable
{

    public interface ICalibrationTableViewModelParameter
    {
        List<CalibrationTableItem> Items { get; set; }
    }

    public class CalibrationTableItem
    {
        public string DisplayName { get; set; }
        public string ItemTableScName { get; set; }
        public string ItemEnableScName { get; set; }
    }

    public class CalibrationTableViewModel : UiViewModelBase, ISupportMultipleSystem
    {
        public ICalibrationTableViewModelParameter CustomParameter { get; set; }

        public string SystemName { get; set; }

        public class NotifiableCalibrationTableItem : NotifiableItem
        {
            public string DisplayName { get; set; }

            public float FeedbackValue { get; set; }

            public float CalibrationValue { get; set; }
        }

        public ObservableCollection<CalibrationTableItem> CalibrationItems { get; set; }

        public ObservableCollection<NotifiableCalibrationTableItem> TableData { get; set; }

        public string FeedbackValue { get; set; }

        public string CalibrationValue { get; set; }

        private CalibrationTableItem _currentSelection;

        public CalibrationTableItem CurrentSelection
        {
            get { return _currentSelection; }
            set
            {
                _currentSelection = value;
                ChangeSelection(_currentSelection);

                NotifyOfPropertyChange(nameof(CurrentSelection));
            }
        }
 
        public CalibrationTableViewModel()
        {
            DisplayName = "Calibration Table ";
 
            CalibrationItems = new ObservableCollection<CalibrationTableItem>();
            TableData = new ObservableCollection<NotifiableCalibrationTableItem>();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            if (CustomParameter != null && CustomParameter.Items != null && CalibrationItems.IsEmpty())
            {
                foreach (var item in CustomParameter.Items)
                {
                    CalibrationItems.Add(new CalibrationTableItem()
                    {
                        DisplayName = item.DisplayName,
                        ItemEnableScName = item.ItemEnableScName,
                        ItemTableScName = item.ItemTableScName,
                    });
                }
            }

        }


        protected void ChangeSelection(CalibrationTableItem item)
        {
            if (item == null)
            {
                TableData.Clear();
                return;
            }

            var tableValues = QueryDataClient.Instance.Service.GetConfig(item.ItemTableScName);
            if (tableValues == null)
                return;

            var scValue = (string) tableValues;

            string[] items = scValue.Split(';');


            var tableData = new ObservableCollection<NotifiableCalibrationTableItem>();

            for (int i = 0; i < items.Length; i++)
            {
                if (items.Length > i)
                {
                    string itemValue = items[i];
                    if (!string.IsNullOrEmpty(itemValue))
                    {
                        string[] pairValue = itemValue.Split('#');
                        if (pairValue.Length == 2)
                        {
                            if (float.TryParse(pairValue[0], out float rangeItem1)
                                && float.TryParse(pairValue[1], out float rangeItem2))
                            {
                                tableData.Add(new NotifiableCalibrationTableItem()
                                {
                                    FeedbackValue = rangeItem1,
                                    CalibrationValue = rangeItem2,
                                });
                            }
                        }
                    }
                }
            }


            TableData = new ObservableCollection<NotifiableCalibrationTableItem>( tableData.OrderBy(x => x.FeedbackValue).ToList());

            NotifyOfPropertyChange(nameof(TableData));
        }


        public void Save()
        {
            if (CurrentSelection == null)
                return;

            string data = "";
            foreach (var item in TableData)
            {
                data += $"{item.FeedbackValue}#{item.CalibrationValue};";
            }

            InvokeClient.Instance.Service.DoOperation("System.SetConfig", CurrentSelection.ItemTableScName, data);

            Reload();
        }


        public void Cancel()
        {
            Reload();
        }

        private void Reload()
        {
            ChangeSelection(CurrentSelection);
        }

        public void Add()
        {
            if (string.IsNullOrEmpty(FeedbackValue) || string.IsNullOrEmpty(CalibrationValue))
            {
                MessageBox.Show("Input value is empty");
                return;
            }
            if (!float.TryParse(FeedbackValue, out float feedback) ||
                !float.TryParse(CalibrationValue, out float calibrationValue))
            {
                MessageBox.Show("Input value not valid");
                return;
            }

            FeedbackValue = "";
            CalibrationValue = "";
            NotifyOfPropertyChange(nameof(FeedbackValue));
            NotifyOfPropertyChange(nameof(CalibrationValue));

            TableData.Add(new NotifiableCalibrationTableItem()
            {
                DisplayName = CurrentSelection.DisplayName,
                FeedbackValue = feedback,
                CalibrationValue = calibrationValue,
            });
        }

        public void DeleteItem(NotifiableCalibrationTableItem item)
        {
            TableData.Remove(item);
        }
 
    }

}
