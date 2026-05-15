using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Aitex.Core.UI.MVVM;

namespace Aitex.Core.Common.DeviceData
{
    [DataContract]
    [Serializable]
    public class AITVaporLineData : INotifyPropertyChanged, IDeviceData
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void InvokePropertyChanged()
        {
            PropertyInfo[] ps = this.GetType().GetProperties();
            foreach (PropertyInfo p in ps)
            {
                InvokePropertyChanged(p.Name);

                if (p.PropertyType == typeof (ICommand))
                {
                    DelegateCommand<string> cmd = p.GetValue(this, null) as DelegateCommand<string>;
                    if (cmd != null)
                        cmd.RaiseCanExecuteChanged();

                }
            }

            FieldInfo[] fi = this.GetType().GetFields();
            foreach (FieldInfo p in fi)
            {
                InvokePropertyChanged(p.Name);

                if (p.FieldType == typeof (ICommand))
                {
                    DelegateCommand<string> cmd = p.GetValue(this) as DelegateCommand<string>;
                    if (cmd != null)
                        cmd.RaiseCanExecuteChanged();

                }
            }
        }

        /// <summary>
        /// 设备的唯一名称，UI与RT交互的ID
        /// </summary>
        [DataMember]
        public string DeviceName { get; set; }

        /// <summary>
        /// 显示在界面上的名称
        /// </summary>
        [DataMember]
        public string DisplayName { get; set; }

        /// <summary>
        /// IO 表中定义的物理编号，物理追溯使用 比如: M122
        /// </summary>
        [DataMember]
        public string DeviceSchematicId { get; set; }

        [DataMember]
        public string Unit { get; set; }

        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// 量程
        /// </summary>
        [DataMember]
        public double Scale { get; set; }

        /// <summary>
        /// 设定值
        /// </summary>
        [DataMember]
        public double SetPoint { get; set; }

        [DataMember]
        public double FeedBack { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        [DataMember]
        public double DefaultValue { get; set; }

        /// <summary>
        /// 是否有报警
        /// </summary>
        [DataMember]
        public bool IsWarning { get; set; }

        /// <summary>
        /// alarm或是erro时显示的信息
        /// </summary>
        [DataMember]
        public string ErroMessage { get; set; }

        /// <summary>
        /// MFC,PC
        /// </summary>
        [DataMember]
        public string Type { get; set; }


        [DataMember]
        public double Factor { get; set; }

        private string _title;
        [DataMember]
        public string DisplayTitle
        {
            get
            {
                return string.Format("{0}({1})", DeviceSchematicId, DisplayName);
            }

            set
            {
                _title = value;
            }
        }

        [DataMember]
        public bool HasMFC
        {
            get;
            set;
        }

        public Visibility MFCVisibility
        {
            get
            {
                return HasMFC ? Visibility.Visible : Visibility.Hidden;
            }
        }

        [DataMember]
        public AITValveData SupplyValveData
        {
            get;
            set;
        }


        [DataMember]
        public AITValveData RunValveData
        {
            get;
            set;
        }

        [DataMember]
        public AITValveData BypassValveData
        {
            get;
            set;
        }


        [DataMember]
        public AITValveData FeedValveData
        {
            get;
            set;
        }

        [DataMember]
        public AITMfcData SupplyMFCData
        {
            get;
            set;
        }

        [DataMember]
        public AITPressureMeterData PressureMeterData
        {
            get;
            set;
        }

        [DataMember]
        public AITSensorData HighLevelSensordata
        {
            get;
            set;
        }

        [DataMember]
        public AITSensorData MiddleLevelSensordata
        {
            get;
            set;
        }

        [DataMember]
        public AITSensorData LowLevelSensordata
        {
            get;
            set;
        }

        [DataMember]
        public AITHeaterData HeaterData
        {
            get;
            set;
        }

        public AITVaporLineData()
        {
            DisplayName = "Undefined";
            Factor = 1.0;
            Unit = "";
            Type = "Vapor";
        }
        public void Update(IDeviceData data)
        {
            throw new NotImplementedException();
        }
    }

    public class AITVaporLineOperation
    {
        public const string Pump = "Pump";
    }
}
