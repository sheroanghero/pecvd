using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.OperationCenter;
using OpenSEMI.ClientBase;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Data.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using MECF.Framework.UI.Client.ClientBase;
using System.Windows;
using System.Windows.Media;

namespace MECF.Framework.UI.Client.CenterViews.Maitenances.MFCVerification
{
    public class MFCVerificationDataListItem : NotifiableItem
    {
        private float _Setpoint;
        public float Setpoint
        {
            get
            {
                return _Setpoint;
            }
            set
            {
                _Setpoint = value;
                InvokePropertyChanged("Setpoint");
            }
        }

        private float _CalculateValue;
        public float CalculateValue
        {
            get
            {
                return _CalculateValue;
            }
            set
            {
                _CalculateValue = value;
                InvokePropertyChanged("CalculateValue");
            }
        }

        private float _ElapseTime;
        public float ElapseTime
        {
            get
            {
                return _ElapseTime;
            }
            set
            {
                _ElapseTime = value;
                InvokePropertyChanged("ElapseTime");
            }
        }

        private float _pressure;
        public float Pressure
        {
            get
            {
                return _pressure;
            }
            set
            {
                _pressure = value;
                InvokePropertyChanged("Pressure");
            }
        }
    }

    public class MFCVerificationErrorRateListItem : NotifiableItem
    {
        public float Setpoint { get; set; }
        public float CalculateValue { get; set; }
        public float ErrorRate { get; set; }
    }

    public class MFCVerificationData : NotifiableItem
    {
        public string Module { get; set; }
        public string Name { get; set; }
        public string Time { get; set; }
        public float Percent10Setpoint { get; set; }
        public float Percent10Calculate { get; set; }
        public float Percent20Setpoint { get; set; }
        public float Percent20Calculate { get; set; }
        public float Percent30Setpoint { get; set; }
        public float Percent30Calculate { get; set; }
        public float Percent40Setpoint { get; set; }
        public float Percent40Calculate { get; set; }
        public float Percent50Setpoint { get; set; }
        public float Percent50Calculate { get; set; }
        public float Percent60Setpoint { get; set; }
        public float Percent60Calculate { get; set; }
        public float Percent70Setpoint { get; set; }
        public float Percent70Calculate { get; set; }
        public float Percent80Setpoint { get; set; }
        public float Percent80Calculate { get; set; }
        public float Percent90Setpoint { get; set; }
        public float Percent90Calculate { get; set; }
        public float Percent100Setpoint { get; set; }
        public float Percent100Calculate { get; set; }
        public float Setpoint { get; set; }
        public float Calculate { get; set; }
    }
    public class MFCVerificationViewModel : ModuleUiViewModelBase, ISupportMultipleSystem
    {
        public MFCVerificationViewModel()
        {
            var now = DateTime.Today;
            SearchBeginTime = now;
            SearchEndTime = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, 999);
        }
        #region MFCs

        #region MFCData
        private AITMfcData _mfc1DeviceData;
        [Subscription("MFC1.DeviceData")]
        public AITMfcData MFC1DeviceData
        {
            get
            {
                return _mfc1DeviceData;
            }
            set
            {
                if (_mfc1DeviceData == null || _mfc1DeviceData.Scale != value.Scale)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        int delta = (int)value.Scale / 10;
                        if (MFC1VerificationPoints != null)
                            MFC1VerificationPoints.Clear();
                        else
                            MFC1VerificationPoints = new ObservableCollection<int>();

                        //ten points: 10% 20% 30% 40% 50% 60% 70% 80% 90% 100% of range
                        for (int i = 0; i < 10; i++)
                        {
                            MFC1VerificationPoints.Add(delta + delta * i);
                        }
                    }));
                }

                _mfc1DeviceData = value;
            }
        }

        private AITMfcData _mfc2DeviceData;
        [Subscription("MFC2.DeviceData")]
        public AITMfcData MFC2DeviceData
        {
            get
            {
                return _mfc2DeviceData;
            }
            set
            {
                if (_mfc2DeviceData == null || _mfc2DeviceData.Scale != value.Scale)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        int delta = (int)value.Scale / 10;
                        if (MFC2VerificationPoints != null)
                            MFC2VerificationPoints.Clear();
                        else
                            MFC2VerificationPoints = new ObservableCollection<int>();

                        //ten points: 10% 20% 30% 40% 50% 60% 70% 80% 90% 100% of range
                        for (int i = 0; i < 10; i++)
                        {
                            MFC2VerificationPoints.Add(delta + delta * i);
                        }
                    }));
                }

                _mfc2DeviceData = value;
            }
        }

        private AITMfcData _mfc3DeviceData;
        [Subscription("MFC3.DeviceData")]
        public AITMfcData MFC3DeviceData
        {
            get
            {
                return _mfc3DeviceData;
            }
            set
            {
                if (_mfc3DeviceData == null || _mfc3DeviceData.Scale != value.Scale)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        int delta = (int)value.Scale / 10;
                        if (MFC3VerificationPoints != null)
                            MFC3VerificationPoints.Clear();
                        else
                            MFC3VerificationPoints = new ObservableCollection<int>();

                        //ten points: 10% 20% 30% 40% 50% 60% 70% 80% 90% 100% of range
                        for (int i = 0; i < 10; i++)
                        {
                            MFC3VerificationPoints.Add(delta + delta * i);
                        }
                    }));
                }

                _mfc3DeviceData = value;
            }
        }

        private AITMfcData _mfc4DeviceData;
        [Subscription("MFC4.DeviceData")]
        public AITMfcData MFC4DeviceData
        {
            get
            {
                return _mfc4DeviceData;
            }
            set
            {
                if (_mfc4DeviceData == null || _mfc4DeviceData.Scale != value.Scale)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        int delta = (int)value.Scale / 10;
                        if (MFC4VerificationPoints != null)
                            MFC4VerificationPoints.Clear();
                        else
                            MFC4VerificationPoints = new ObservableCollection<int>();

                        //ten points: 10% 20% 30% 40% 50% 60% 70% 80% 90% 100% of range
                        for (int i = 0; i < 10; i++)
                        {
                            MFC4VerificationPoints.Add(delta + delta * i);
                        }
                    }));
                }

                _mfc4DeviceData = value;
            }
        }

        private AITMfcData _mfc5DeviceData;
        [Subscription("MFC5.DeviceData")]
        public AITMfcData MFC5DeviceData
        {
            get
            {
                return _mfc5DeviceData;
            }
            set
            {
                if (_mfc5DeviceData == null || _mfc5DeviceData.Scale != value.Scale)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        int delta = (int)value.Scale / 10;
                        if (MFC5VerificationPoints != null)
                            MFC5VerificationPoints.Clear();
                        else
                            MFC5VerificationPoints = new ObservableCollection<int>();

                        //ten points: 10% 20% 30% 40% 50% 60% 70% 80% 90% 100% of range
                        for (int i = 0; i < 10; i++)
                        {
                            MFC5VerificationPoints.Add(delta + delta * i);
                        }
                    }));
                }

                _mfc5DeviceData = value;
            }
        }

        private AITMfcData _mfc6DeviceData;
        [Subscription("MFC6.DeviceData")]
        public AITMfcData MFC6DeviceData
        {
            get
            {
                return _mfc6DeviceData;
            }
            set
            {
                if (_mfc6DeviceData == null || _mfc6DeviceData.Scale != value.Scale)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        int delta = (int)value.Scale / 10;
                        if (MFC6VerificationPoints != null)
                            MFC6VerificationPoints.Clear();
                        else
                            MFC6VerificationPoints = new ObservableCollection<int>();

                        //ten points: 10% 20% 30% 40% 50% 60% 70% 80% 90% 100% of range
                        for (int i = 0; i < 10; i++)
                        {
                            MFC6VerificationPoints.Add(delta + delta * i);
                        }
                    }));
                }

                _mfc6DeviceData = value;
            }
        }

        private AITMfcData _mfc7DeviceData;
        [Subscription("MFC7.DeviceData")]
        public AITMfcData MFC7DeviceData
        {
            get
            {
                return _mfc7DeviceData;
            }
            set
            {
                if (_mfc7DeviceData == null || _mfc7DeviceData.Scale != value.Scale)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        int delta = (int)value.Scale / 10;
                        if (MFC7VerificationPoints != null)
                            MFC7VerificationPoints.Clear();
                        else
                            MFC7VerificationPoints = new ObservableCollection<int>();

                        //ten points: 10% 20% 30% 40% 50% 60% 70% 80% 90% 100% of range
                        for (int i = 0; i < 10; i++)
                        {
                            MFC7VerificationPoints.Add(delta + delta * i);
                        }
                    }));
                }

                _mfc7DeviceData = value;
            }
        }

        private AITMfcData _mfc8DeviceData;
        [Subscription("MFC8.DeviceData")]
        public AITMfcData MFC8DeviceData
        {
            get
            {
                return _mfc8DeviceData;
            }
            set
            {
                if (_mfc8DeviceData == null || _mfc8DeviceData.Scale != value.Scale)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        int delta = (int)value.Scale / 10;
                        if (MFC8VerificationPoints != null)
                            MFC8VerificationPoints.Clear();
                        else
                            MFC8VerificationPoints = new ObservableCollection<int>();

                        //ten points: 10% 20% 30% 40% 50% 60% 70% 80% 90% 100% of range
                        for (int i = 0; i < 10; i++)
                        {
                            MFC8VerificationPoints.Add(delta + delta * i);
                        }
                    }));
                }

                _mfc8DeviceData = value;
            }
        }
        private AITMfcData _mfc9DeviceData;
        [Subscription("MFC9.DeviceData")]
        public AITMfcData MFC9DeviceData
        {
            get
            {
                return _mfc9DeviceData;
            }
            set
            {
                if (_mfc9DeviceData == null || _mfc9DeviceData.Scale != value.Scale)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        int delta = (int)value.Scale / 10;
                        if (MFC9VerificationPoints != null)
                            MFC9VerificationPoints.Clear();
                        else
                            MFC9VerificationPoints = new ObservableCollection<int>();

                        //ten points: 10% 20% 30% 40% 50% 60% 70% 80% 90% 100% of range
                        for (int i = 0; i < 10; i++)
                        {
                            MFC9VerificationPoints.Add(delta + delta * i);
                        }
                    }));
                }

                _mfc9DeviceData = value;
            }
        }

        private AITMfcData _mfc10DeviceData;
        [Subscription("MFC10.DeviceData")]
        public AITMfcData MFC10DeviceData
        {
            get
            {
                return _mfc10DeviceData;
            }
            set
            {
                if (_mfc10DeviceData == null || _mfc10DeviceData.Scale != value.Scale)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        int delta = (int)value.Scale / 10;
                        if (MFC10VerificationPoints != null)
                            MFC10VerificationPoints.Clear();
                        else
                            MFC10VerificationPoints = new ObservableCollection<int>();

                        //ten points: 10% 20% 30% 40% 50% 60% 70% 80% 90% 100% of range
                        for (int i = 0; i < 10; i++)
                        {
                            MFC10VerificationPoints.Add(delta + delta * i);
                        }
                    }));
                }

                _mfc10DeviceData = value;
            }
        }

        private AITMfcData _mfc11DeviceData;
        [Subscription("MFC11.DeviceData")]
        public AITMfcData MFC11DeviceData
        {
            get
            {
                return _mfc11DeviceData;
            }
            set
            {
                if (_mfc11DeviceData == null || _mfc11DeviceData.Scale != value.Scale)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        int delta = (int)value.Scale / 10;
                        if (MFC11VerificationPoints != null)
                            MFC11VerificationPoints.Clear();
                        else
                            MFC11VerificationPoints = new ObservableCollection<int>();

                        //ten points: 10% 20% 30% 40% 50% 60% 70% 80% 90% 100% of range
                        for (int i = 0; i < 10; i++)
                        {
                            MFC11VerificationPoints.Add(delta + delta * i);
                        }
                    }));
                }

                _mfc11DeviceData = value;
            }
        }

        private AITMfcData _mfc12DeviceData;
        [Subscription("MFC12.DeviceData")]
        public AITMfcData MFC12DeviceData
        {
            get
            {
                return _mfc12DeviceData;
            }
            set
            {
                if (_mfc12DeviceData == null || _mfc12DeviceData.Scale != value.Scale)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        int delta = (int)value.Scale / 10;
                        if (MFC12VerificationPoints != null)
                            MFC12VerificationPoints.Clear();
                        else
                            MFC12VerificationPoints = new ObservableCollection<int>();

                        //ten points: 10% 20% 30% 40% 50% 60% 70% 80% 90% 100% of range
                        for (int i = 0; i < 10; i++)
                        {
                            MFC12VerificationPoints.Add(delta + delta * i);
                        }
                    }));
                }

                _mfc12DeviceData = value;
            }
        }
        #endregion

        #region MFCVerificationResult
        private string _MFC1VerificationResult;
        [Subscription("MfcGas1.VerificationResult")]
        public string MFC1VerificationResult
        {
            get
            {
                return _MFC1VerificationResult;
            }
            set
            {
                if (_MFC1VerificationResult != value)
                {
                    var results = value.Split(';');
                    if (results != null && results.Length > 0)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MFC1VerificationData.Clear();
                            foreach (var item in results)
                            {
                                var onePoint = item.Split(',');
                                if (onePoint != null && onePoint.Length >= 2)
                                {
                                    MFC1VerificationData.Add(new MFCVerificationDataListItem()
                                    {
                                        Setpoint = Convert.ToSingle(onePoint[0]),
                                        CalculateValue = Convert.ToSingle(onePoint[1]),
                                    });
                                }
                            }
                        }));
                    }
                }
                _MFC1VerificationResult = value;
            }
        }

        private string _MFC2VerificationResult;
        [Subscription("MfcGas2.VerificationResult")]
        public string MFC2VerificationResult
        {
            get
            {
                return _MFC2VerificationResult;
            }
            set
            {
                if (_MFC2VerificationResult != value)
                {
                    var results = value.Split(';');
                    if (results != null && results.Length > 0)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MFC2VerificationData.Clear();
                            foreach (var item in results)
                            {
                                var onePoint = item.Split(',');
                                if (onePoint != null && onePoint.Length >= 2)
                                {
                                    MFC2VerificationData.Add(new MFCVerificationDataListItem()
                                    {
                                        Setpoint = Convert.ToSingle(onePoint[0]),
                                        CalculateValue = Convert.ToSingle(onePoint[1]),
                                    });
                                }
                            }
                        }));
                    }
                }
                _MFC2VerificationResult = value;

            }
        }

        private string _MFC3VerificationResult;
        [Subscription("MfcGas3.VerificationResult")]
        public string MFC3VerificationResult
        {
            get
            {
                return _MFC3VerificationResult;
            }
            set
            {
                if (_MFC3VerificationResult != value)
                {
                    var results = value.Split(';');
                    if (results != null && results.Length > 0)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MFC3VerificationData.Clear();
                            foreach (var item in results)
                            {
                                var onePoint = item.Split(',');
                                if (onePoint != null && onePoint.Length >= 2)
                                {
                                    MFC3VerificationData.Add(new MFCVerificationDataListItem()
                                    {
                                        Setpoint = Convert.ToSingle(onePoint[0]),
                                        CalculateValue = Convert.ToSingle(onePoint[1]),
                                    });
                                }
                            }
                        }));
                    }
                }
                _MFC3VerificationResult = value;
            }
        }

        private string _MFC4VerificationResult;
        [Subscription("MfcGas4.VerificationResult")]
        public string MFC4VerificationResult
        {
            get
            {
                return _MFC4VerificationResult;
            }
            set
            {
                if (_MFC4VerificationResult != value)
                {
                    var results = value.Split(';');
                    if (results != null && results.Length > 0)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MFC4VerificationData.Clear();
                            foreach (var item in results)
                            {
                                var onePoint = item.Split(',');
                                if (onePoint != null && onePoint.Length >= 2)
                                {
                                    MFC4VerificationData.Add(new MFCVerificationDataListItem()
                                    {
                                        Setpoint = Convert.ToSingle(onePoint[0]),
                                        CalculateValue = Convert.ToSingle(onePoint[1]),
                                    });
                                }
                            }
                        }));
                    }
                }
                _MFC4VerificationResult = value;
            }
        }

        private string _MFC5VerificationResult;
        [Subscription("MfcGas5.VerificationResult")]
        public string MFC5VerificationResult
        {
            get
            {
                return _MFC5VerificationResult;
            }
            set
            {
                if (_MFC5VerificationResult != value)
                {
                    var results = value.Split(';');
                    if (results != null && results.Length > 0)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MFC5VerificationData.Clear();
                            foreach (var item in results)
                            {
                                var onePoint = item.Split(',');
                                if (onePoint != null && onePoint.Length >= 2)
                                {
                                    MFC5VerificationData.Add(new MFCVerificationDataListItem()
                                    {
                                        Setpoint = Convert.ToSingle(onePoint[0]),
                                        CalculateValue = Convert.ToSingle(onePoint[1]),
                                    });
                                }
                            }
                        }));
                    }
                }
                _MFC5VerificationResult = value;
            }
        }

        private string _MFC6VerificationResult;
        [Subscription("MfcGas6.VerificationResult")]
        public string MFC6VerificationResult
        {
            get
            {
                return _MFC6VerificationResult;
            }
            set
            {
                if (_MFC6VerificationResult != value)
                {
                    var results = value.Split(';');
                    if (results != null && results.Length > 0)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MFC6VerificationData.Clear();
                            foreach (var item in results)
                            {
                                var onePoint = item.Split(',');
                                if (onePoint != null && onePoint.Length >= 2)
                                {
                                    MFC6VerificationData.Add(new MFCVerificationDataListItem()
                                    {
                                        Setpoint = Convert.ToSingle(onePoint[0]),
                                        CalculateValue = Convert.ToSingle(onePoint[1]),
                                    });
                                }
                            }
                        }));
                    }
                }
                _MFC6VerificationResult = value;
            }
        }

        private string _MFC7VerificationResult;
        [Subscription("MfcGas7.VerificationResult")]
        public string MFC7VerificationResult
        {
            get
            {
                return _MFC7VerificationResult;
            }
            set
            {
                if (_MFC7VerificationResult != value)
                {
                    var results = value.Split(';');
                    if (results != null && results.Length > 0)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MFC7VerificationData.Clear();
                            foreach (var item in results)
                            {
                                var onePoint = item.Split(',');
                                if (onePoint != null && onePoint.Length >= 2)
                                {
                                    MFC7VerificationData.Add(new MFCVerificationDataListItem()
                                    {
                                        Setpoint = Convert.ToSingle(onePoint[0]),
                                        CalculateValue = Convert.ToSingle(onePoint[1]),
                                    });
                                }
                            }
                        }));
                    }
                }
                _MFC7VerificationResult = value;
            }
        }

        private string _MFC8VerificationResult;
        [Subscription("MfcGas8.VerificationResult")]
        public string MFC8VerificationResult
        {
            get
            {
                return _MFC8VerificationResult;
            }
            set
            {
                if (_MFC8VerificationResult != value)
                {
                    var results = value.Split(';');
                    if (results != null && results.Length > 0)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MFC8VerificationData.Clear();
                            foreach (var item in results)
                            {
                                var onePoint = item.Split(',');
                                if (onePoint != null && onePoint.Length >= 2)
                                {
                                    MFC8VerificationData.Add(new MFCVerificationDataListItem()
                                    {
                                        Setpoint = Convert.ToSingle(onePoint[0]),
                                        CalculateValue = Convert.ToSingle(onePoint[1]),
                                    });
                                }
                            }
                        }));
                    }
                }
                _MFC8VerificationResult = value;
            }
        }

        private string _MFC9VerificationResult;
        [Subscription("MfcGas9.VerificationResult")]
        public string MFC9VerificationResult
        {
            get
            {
                return _MFC9VerificationResult;
            }
            set
            {
                if (_MFC9VerificationResult != value)
                {
                    var results = value.Split(';');
                    if (results != null && results.Length > 0)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MFC9VerificationData.Clear();
                            foreach (var item in results)
                            {
                                var onePoint = item.Split(',');
                                if (onePoint != null && onePoint.Length >= 2)
                                {
                                    MFC9VerificationData.Add(new MFCVerificationDataListItem()
                                    {
                                        Setpoint = Convert.ToSingle(onePoint[0]),
                                        CalculateValue = Convert.ToSingle(onePoint[1]),
                                    });
                                }
                            }
                        }));
                    }
                }
                _MFC9VerificationResult = value;
            }
        }

        private string _MFC10VerificationResult;
        [Subscription("MfcGas10.VerificationResult")]
        public string MFC10VerificationResult
        {
            get
            {
                return _MFC10VerificationResult;
            }
            set
            {
                if (_MFC10VerificationResult != value)
                {
                    var results = value.Split(';');
                    if (results != null && results.Length > 0)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MFC10VerificationData.Clear();
                            foreach (var item in results)
                            {
                                var onePoint = item.Split(',');
                                if (onePoint != null && onePoint.Length >= 2)
                                {
                                    MFC10VerificationData.Add(new MFCVerificationDataListItem()
                                    {
                                        Setpoint = Convert.ToSingle(onePoint[0]),
                                        CalculateValue = Convert.ToSingle(onePoint[1]),
                                    });
                                }
                            }
                        }));
                    }
                }
                _MFC10VerificationResult = value;
            }
        }

        private string _MFC11VerificationResult;
        [Subscription("MfcGas11.VerificationResult")]
        public string MFC11VerificationResult
        {
            get
            {
                return _MFC11VerificationResult;
            }
            set
            {
                if (_MFC11VerificationResult != value)
                {
                    var results = value.Split(';');
                    if (results != null && results.Length > 0)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MFC11VerificationData.Clear();
                            foreach (var item in results)
                            {
                                var onePoint = item.Split(',');
                                if (onePoint != null && onePoint.Length >= 2)
                                {
                                    MFC11VerificationData.Add(new MFCVerificationDataListItem()
                                    {
                                        Setpoint = Convert.ToSingle(onePoint[0]),
                                        CalculateValue = Convert.ToSingle(onePoint[1]),
                                    });
                                }
                            }
                        }));
                    }
                }
                _MFC11VerificationResult = value;
            }
        }

        private string _MFC12VerificationResult;
        [Subscription("MfcGas12.VerificationResult")]
        public string MFC12VerificationResult
        {
            get
            {
                return _MFC12VerificationResult;
            }
            set
            {
                if (_MFC12VerificationResult != value)
                {
                    var results = value.Split(';');
                    if (results != null && results.Length > 0)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MFC12VerificationData.Clear();
                            foreach (var item in results)
                            {
                                var onePoint = item.Split(',');
                                if (onePoint != null && onePoint.Length >= 2)
                                {
                                    MFC12VerificationData.Add(new MFCVerificationDataListItem()
                                    {
                                        Setpoint = Convert.ToSingle(onePoint[0]),
                                        CalculateValue = Convert.ToSingle(onePoint[1]),
                                    });
                                }
                            }
                        }));
                    }
                }
                _MFC12VerificationResult = value;
            }
        }

        #endregion
        #endregion

        private string _pmStatus;
        [Subscription("Status")]
        public string PMStatus
        {
            get
            {
                return _pmStatus;
            }
            set
            {
                if (_pmStatus != value)
                    Query();
                _pmStatus = value;
            }
        }

        [Subscription("IsOnline")]
        public bool IsOnline { get; set; }

        public string Setpoint { get; set; }

        public bool IsVerificationButtonEnable => PMStatus == "Idle" && !IsOnline;

        public bool IsPermission { get => this.Permission == 3; }

        public Visibility MFC1Visibility => !string.IsNullOrEmpty(MFC1VerificationResult) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MFC2Visibility => !string.IsNullOrEmpty(MFC2VerificationResult) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MFC3Visibility => !string.IsNullOrEmpty(MFC3VerificationResult) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MFC4Visibility => !string.IsNullOrEmpty(MFC4VerificationResult) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MFC5Visibility => !string.IsNullOrEmpty(MFC5VerificationResult) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MFC6Visibility => !string.IsNullOrEmpty(MFC6VerificationResult) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MFC7Visibility => !string.IsNullOrEmpty(MFC7VerificationResult) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MFC8Visibility => !string.IsNullOrEmpty(MFC8VerificationResult) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MFC9Visibility => !string.IsNullOrEmpty(MFC9VerificationResult) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MFC10Visibility => !string.IsNullOrEmpty(MFC10VerificationResult) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MFC11Visibility => !string.IsNullOrEmpty(MFC11VerificationResult) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MFC12Visibility => !string.IsNullOrEmpty(MFC12VerificationResult) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility MFCQueryVisibility
        {
            get; set;
        }

        public ObservableCollection<IRenderableSeries> SelectedData { get; set; }

        private IRange _xValueRange;
        public IRange XValueRange
        {
            get { return _xValueRange; }
            set { _xValueRange = value; NotifyOfPropertyChange("XValueRange"); }
        }

        private IRange _yValueRange;
        public IRange YValueRange
        {
            get { return _yValueRange; }
            set { _yValueRange = value; NotifyOfPropertyChange("YValueRange"); }
        }

        public AutoRange ChartAutoRange
        {
            get { return EnableAutoZoom ? AutoRange.Always : AutoRange.Never; }
        }

        private bool _enableAutoZoom = true;
        public bool EnableAutoZoom
        {
            get { return _enableAutoZoom; }

            set
            {
                _enableAutoZoom = value;
                NotifyOfPropertyChange(nameof(EnableAutoZoom));
                NotifyOfPropertyChange(nameof(ChartAutoRange));
            }
        }

        private AutoRange _autoRangeX;
        public AutoRange AutoRangeX
        {
            get => _autoRangeX;
            set { _autoRangeX = value; NotifyOfPropertyChange(nameof(AutoRangeX)); }
        }

        private AutoRange _autoRangeY;
        public AutoRange AutoRangeY
        {
            get => _autoRangeY;
            set { _autoRangeY = value; NotifyOfPropertyChange(nameof(AutoRangeY)); }
        }

        public ObservableCollection<MFCVerificationDataListItem> MFC1VerificationData { get; set; }
        public ObservableCollection<MFCVerificationDataListItem> MFC2VerificationData { get; set; }
        public ObservableCollection<MFCVerificationDataListItem> MFC3VerificationData { get; set; }
        public ObservableCollection<MFCVerificationDataListItem> MFC4VerificationData { get; set; }
        public ObservableCollection<MFCVerificationDataListItem> MFC5VerificationData { get; set; }
        public ObservableCollection<MFCVerificationDataListItem> MFC6VerificationData { get; set; }
        public ObservableCollection<MFCVerificationDataListItem> MFC7VerificationData { get; set; }
        public ObservableCollection<MFCVerificationDataListItem> MFC8VerificationData { get; set; }
        public ObservableCollection<MFCVerificationDataListItem> MFC9VerificationData { get; set; }
        public ObservableCollection<MFCVerificationDataListItem> MFC10VerificationData { get; set; }
        public ObservableCollection<MFCVerificationDataListItem> MFC11VerificationData { get; set; }
        public ObservableCollection<MFCVerificationDataListItem> MFC12VerificationData { get; set; }
        public ObservableCollection<MFCVerificationErrorRateListItem> VerificationErrorRate { get; set; }
        public ObservableCollection<int> MFC1VerificationPoints { get; set; }
        public ObservableCollection<int> MFC2VerificationPoints { get; set; }
        public ObservableCollection<int> MFC3VerificationPoints { get; set; }
        public ObservableCollection<int> MFC4VerificationPoints { get; set; }
        public ObservableCollection<int> MFC5VerificationPoints { get; set; }
        public ObservableCollection<int> MFC6VerificationPoints { get; set; }
        public ObservableCollection<int> MFC7VerificationPoints { get; set; }
        public ObservableCollection<int> MFC8VerificationPoints { get; set; }
        public ObservableCollection<int> MFC9VerificationPoints { get; set; }
        public ObservableCollection<int> MFC10VerificationPoints { get; set; }
        public ObservableCollection<int> MFC11VerificationPoints { get; set; }
        public ObservableCollection<int> MFC12VerificationPoints { get; set; }
        public ObservableCollection<MFCVerificationData> VerificationDataRecords { get; set; }//read form db
        private MFCVerificationData selectedVerificationData;
        public MFCVerificationData SelectedVerificationData
        {
            get { return this.selectedVerificationData; }
            set
            {
                this.selectedVerificationData = value;
            }
        }

        public DateTime SearchBeginTime { get; set; }
        public DateTime SearchEndTime { get; set; }

        private MFCVerificationView view;

        protected override void OnInitialize()
        {
            MFC1VerificationData = new ObservableCollection<MFCVerificationDataListItem>();
            MFC2VerificationData = new ObservableCollection<MFCVerificationDataListItem>();
            MFC3VerificationData = new ObservableCollection<MFCVerificationDataListItem>();
            MFC4VerificationData = new ObservableCollection<MFCVerificationDataListItem>();
            MFC5VerificationData = new ObservableCollection<MFCVerificationDataListItem>();
            MFC6VerificationData = new ObservableCollection<MFCVerificationDataListItem>();
            MFC7VerificationData = new ObservableCollection<MFCVerificationDataListItem>();
            MFC8VerificationData = new ObservableCollection<MFCVerificationDataListItem>();
            MFC9VerificationData = new ObservableCollection<MFCVerificationDataListItem>();
            MFC10VerificationData = new ObservableCollection<MFCVerificationDataListItem>();
            MFC11VerificationData = new ObservableCollection<MFCVerificationDataListItem>();
            MFC12VerificationData = new ObservableCollection<MFCVerificationDataListItem>();
            MFC1VerificationPoints = new ObservableCollection<int>();
            MFC2VerificationPoints = new ObservableCollection<int>();
            MFC3VerificationPoints = new ObservableCollection<int>();
            MFC4VerificationPoints = new ObservableCollection<int>();
            MFC5VerificationPoints = new ObservableCollection<int>();
            MFC6VerificationPoints = new ObservableCollection<int>();
            MFC7VerificationPoints = new ObservableCollection<int>();
            MFC8VerificationPoints = new ObservableCollection<int>();
            MFC9VerificationPoints = new ObservableCollection<int>();
            MFC10VerificationPoints = new ObservableCollection<int>();
            MFC11VerificationPoints = new ObservableCollection<int>();
            MFC12VerificationPoints = new ObservableCollection<int>();
            MFCQueryVisibility = Visibility.Collapsed;
            base.OnInitialize();

            YValueRange = new DoubleRange(0, 100);
            SelectedData = new ObservableCollection<IRenderableSeries>();

            AutoRangeX = AutoRange.Always;
            AutoRangeY = AutoRange.Always;//(bool)QueryDataClient.Instance.Service.GetConfig("System.IsCycleMode");
        }
        protected override void OnActivate()
        {
            XValueRange = new DoubleRange(0, MFC1DeviceData != null ? MFC1DeviceData.Scale : 100);
            base.OnActivate();
            Query();
        }

        protected override void OnViewLoaded(object _view)
        {
            base.OnViewLoaded(_view);
            this.view = (MFCVerificationView)_view;
            this.view.wfTimeFrom.Value = this.SearchBeginTime;
            this.view.wfTimeTo.Value = this.SearchEndTime;
            Query();
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
        }

        public void StartVerification(object oj1, object oj2, object oj3)
        {
            string gasName = oj1.ToString();
            float.TryParse(oj2.ToString(), out float flow);
            int.TryParse(oj3.ToString(), out int flowCount);
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.MFCVerification", gasName, flow, flowCount);
        }

        public void AcceptAll(object o)
        {
            string gasName = o.ToString();
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{gasName}.AcceptAllMFCVerification", gasName);
        }

        public void Back()
        {
            MFCQueryVisibility = Visibility.Collapsed;
            NotifyOfPropertyChange("RFCalVisibility");
            NotifyOfPropertyChange("MFCQueryVisibility");
        }

        public void SelectHistory()
        {
            if (SelectedVerificationData != null)
            {
                try
                {
                    if (SelectedVerificationData.Name.Contains("Gas1"))
                        XValueRange = new DoubleRange(0, MFC1DeviceData.Scale);
                    else if (SelectedVerificationData.Name.Contains("Gas2"))
                        XValueRange = new DoubleRange(0, MFC2DeviceData.Scale);
                    else if (SelectedVerificationData.Name.Contains("Gas3"))
                        XValueRange = new DoubleRange(0, MFC3DeviceData.Scale);
                    else
                        XValueRange = new DoubleRange(0, MFC4DeviceData.Scale);

                    MFCQueryVisibility = Visibility.Visible;

                    NotifyOfPropertyChange("MFCQueryVisibility");

                    VerificationErrorRate = new ObservableCollection<MFCVerificationErrorRateListItem>();

                    MFCVerificationErrorRateListItem errorRate = new MFCVerificationErrorRateListItem()
                    {
                        Setpoint = SelectedVerificationData.Percent10Setpoint,
                        CalculateValue = SelectedVerificationData.Percent10Calculate,
                        ErrorRate = Math.Abs((float)Math.Round((SelectedVerificationData.Percent10Setpoint - SelectedVerificationData.Percent10Calculate) / SelectedVerificationData.Percent10Setpoint * 100, 3))
                    };
                    VerificationErrorRate.Add(errorRate);

                    errorRate = new MFCVerificationErrorRateListItem()
                    {
                        Setpoint = SelectedVerificationData.Percent20Setpoint,
                        CalculateValue = SelectedVerificationData.Percent20Calculate,
                        ErrorRate = Math.Abs((float)Math.Round((SelectedVerificationData.Percent20Setpoint - SelectedVerificationData.Percent20Calculate) / SelectedVerificationData.Percent20Setpoint * 100, 3))
                    };
                    VerificationErrorRate.Add(errorRate);

                    errorRate = new MFCVerificationErrorRateListItem()
                    {
                        Setpoint = SelectedVerificationData.Percent30Setpoint,
                        CalculateValue = SelectedVerificationData.Percent30Calculate,
                        ErrorRate = Math.Abs((float)Math.Round((SelectedVerificationData.Percent30Setpoint - SelectedVerificationData.Percent30Calculate) / SelectedVerificationData.Percent30Setpoint * 100, 3))
                    };
                    VerificationErrorRate.Add(errorRate);

                    errorRate = new MFCVerificationErrorRateListItem()
                    {
                        Setpoint = SelectedVerificationData.Percent40Setpoint,
                        CalculateValue = SelectedVerificationData.Percent40Calculate,
                        ErrorRate = Math.Abs((float)Math.Round((SelectedVerificationData.Percent40Setpoint - SelectedVerificationData.Percent40Calculate) / SelectedVerificationData.Percent40Setpoint * 100, 3))
                    };
                    VerificationErrorRate.Add(errorRate);

                    errorRate = new MFCVerificationErrorRateListItem()
                    {
                        Setpoint = SelectedVerificationData.Percent50Setpoint,
                        CalculateValue = SelectedVerificationData.Percent50Calculate,
                        ErrorRate = Math.Abs((float)Math.Round((SelectedVerificationData.Percent50Setpoint - SelectedVerificationData.Percent50Calculate) / SelectedVerificationData.Percent50Setpoint * 100, 3))
                    };
                    VerificationErrorRate.Add(errorRate);

                    errorRate = new MFCVerificationErrorRateListItem()
                    {
                        Setpoint = SelectedVerificationData.Percent60Setpoint,
                        CalculateValue = SelectedVerificationData.Percent60Calculate,
                        ErrorRate = Math.Abs((float)Math.Round((SelectedVerificationData.Percent60Setpoint - SelectedVerificationData.Percent60Calculate) / SelectedVerificationData.Percent60Setpoint * 100, 3))
                    };
                    VerificationErrorRate.Add(errorRate);

                    errorRate = new MFCVerificationErrorRateListItem()
                    {
                        Setpoint = SelectedVerificationData.Percent70Setpoint,
                        CalculateValue = SelectedVerificationData.Percent70Calculate,
                        ErrorRate = Math.Abs((float)Math.Round((SelectedVerificationData.Percent70Setpoint - SelectedVerificationData.Percent70Calculate) / SelectedVerificationData.Percent70Setpoint * 100, 3))
                    };
                    VerificationErrorRate.Add(errorRate);

                    errorRate = new MFCVerificationErrorRateListItem()
                    {
                        Setpoint = SelectedVerificationData.Percent80Setpoint,
                        CalculateValue = SelectedVerificationData.Percent80Calculate,
                        ErrorRate = Math.Abs((float)Math.Round((SelectedVerificationData.Percent80Setpoint - SelectedVerificationData.Percent80Calculate) / SelectedVerificationData.Percent80Setpoint * 100, 3))
                    };
                    VerificationErrorRate.Add(errorRate);

                    errorRate = new MFCVerificationErrorRateListItem()
                    {
                        Setpoint = SelectedVerificationData.Percent90Setpoint,
                        CalculateValue = SelectedVerificationData.Percent90Calculate,
                        ErrorRate = Math.Abs((float)Math.Round((SelectedVerificationData.Percent90Setpoint - SelectedVerificationData.Percent90Calculate) / SelectedVerificationData.Percent90Setpoint * 100, 3))
                    };
                    VerificationErrorRate.Add(errorRate);

                    errorRate = new MFCVerificationErrorRateListItem()
                    {
                        Setpoint = SelectedVerificationData.Percent100Setpoint,
                        CalculateValue = SelectedVerificationData.Percent100Calculate,
                        ErrorRate = Math.Abs((float)Math.Round((SelectedVerificationData.Percent100Setpoint - SelectedVerificationData.Percent100Calculate) / SelectedVerificationData.Percent100Setpoint * 100, 3))
                    };
                    VerificationErrorRate.Add(errorRate);//SelectedData
                    ChartDataLine line = new ChartDataLine("MFCCalibration");
                    VerificationErrorRate.ToList().ForEach(x => line.Append(x.Setpoint, x.ErrorRate));
                    //foreach(var item in VerificationErrorRate)
                    //{
                    //    line.Append(item.Setpoint,item.ErrorRate);
                    //}
                    SelectedData.Clear();
                    SelectedData.Add(line);
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                }
            }
        }

        /// <summary>
        /// 查询
        /// </summary>
        public void Query()
        {
            if (view == null || view.wfTimeFrom == null || view.wfTimeFrom == null)
                return;
            try
            {
                this.SearchBeginTime = (System.DateTime)this.view.wfTimeFrom.Value;
                this.SearchEndTime = (System.DateTime)this.view.wfTimeTo.Value;

                if (SearchBeginTime > SearchEndTime)
                {
                    MessageBox.Show("Time range invalid, start time should be early than end time");
                    return;
                }

                string sql = string.Format(
                "SELECT * FROM \"mfc_verification_data\" where \"operate_time\" >= '{0}' and \"operate_time\" <= '{1}' and \"module\" = '{2}' order by \"operate_time\" ASC;",
                SearchBeginTime.ToString("yyyyMMdd HHmmss"), SearchEndTime.ToString("yyyyMMdd HHmmss"), SystemName);

                var result = QueryDataClient.Instance.Service.QueryData(sql);

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    VerificationDataRecords = new ObservableCollection<MFCVerificationData>();
                    for (int i = 0; i < result.Rows.Count; i++)
                    {
                        MFCVerificationData data = new MFCVerificationData()
                        {
                            Module = result.Rows[i]["module"].ToString(),
                            Name = result.Rows[i]["name"].ToString(),
                            Time = ((DateTime)result.Rows[i]["operate_time"]).ToString("yyyy/MM/dd HH:mm:ss.fff"),
                            Percent10Setpoint = float.Parse(result.Rows[i]["percent10_setpoint"].ToString()),
                            Percent10Calculate = float.Parse(result.Rows[i]["percent10_calculate"].ToString()),
                            Percent20Setpoint = float.Parse(result.Rows[i]["percent20_setpoint"].ToString()),
                            Percent20Calculate = float.Parse(result.Rows[i]["percent20_calculate"].ToString()),
                            Percent30Setpoint = float.Parse(result.Rows[i]["percent30_setpoint"].ToString()),
                            Percent30Calculate = float.Parse(result.Rows[i]["percent30_calculate"].ToString()),
                            Percent40Setpoint = float.Parse(result.Rows[i]["percent40_setpoint"].ToString()),
                            Percent40Calculate = float.Parse(result.Rows[i]["percent40_calculate"].ToString()),
                            Percent50Setpoint = float.Parse(result.Rows[i]["percent50_setpoint"].ToString()),
                            Percent50Calculate = float.Parse(result.Rows[i]["percent50_calculate"].ToString()),
                            Percent60Setpoint = float.Parse(result.Rows[i]["percent60_setpoint"].ToString()),
                            Percent60Calculate = float.Parse(result.Rows[i]["percent60_calculate"].ToString()),
                            Percent70Setpoint = float.Parse(result.Rows[i]["percent70_setpoint"].ToString()),
                            Percent70Calculate = float.Parse(result.Rows[i]["percent70_calculate"].ToString()),
                            Percent80Setpoint = float.Parse(result.Rows[i]["percent80_setpoint"].ToString()),
                            Percent80Calculate = float.Parse(result.Rows[i]["percent80_calculate"].ToString()),
                            Percent90Setpoint = float.Parse(result.Rows[i]["percent90_setpoint"].ToString()),
                            Percent90Calculate = float.Parse(result.Rows[i]["percent90_calculate"].ToString()),
                            Percent100Setpoint = float.Parse(result.Rows[i]["percent100_setpoint"].ToString()),
                            Percent100Calculate = float.Parse(result.Rows[i]["percent100_calculate"].ToString()),
                            Setpoint = float.Parse(result.Rows[i]["setpoint"].ToString()),
                            Calculate = float.Parse(result.Rows[i]["calculate"].ToString()),
                        };

                        VerificationDataRecords.Add(data);
                    }

                    NotifyOfPropertyChange("VerificationDataRecords");
                }));
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public void SetAsCurrent(object o)
        {
            int.TryParse(o.ToString(), out int selectIndex);
            if (selectIndex < 0 || selectIndex >= VerificationDataRecords.Count)
            {
                MessageBox.Show("Please select a row to set.", "Calibration", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InvokeClient.Instance.Service.DoOperation($"{SystemName}.{VerificationDataRecords[selectIndex].Name}.UpdateCalibrationInfo",
                VerificationDataRecords[selectIndex].Percent10Calculate,
                VerificationDataRecords[selectIndex].Percent20Calculate,
                VerificationDataRecords[selectIndex].Percent30Calculate,
                VerificationDataRecords[selectIndex].Percent40Calculate,
                VerificationDataRecords[selectIndex].Percent50Calculate,
                VerificationDataRecords[selectIndex].Percent60Calculate,
                VerificationDataRecords[selectIndex].Percent70Calculate,
                VerificationDataRecords[selectIndex].Percent80Calculate,
                VerificationDataRecords[selectIndex].Percent90Calculate,
                VerificationDataRecords[selectIndex].Percent100Calculate);
        }

        public void Abort()
        {
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.Abort");
        }
    }

    public class ChartDataLine : FastLineRenderableSeries, INotifyPropertyChanged
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void InvokePropertyChanged(string propertyName)
        {
            PropertyChangedEventArgs eventArgs = new PropertyChangedEventArgs(propertyName);
            PropertyChangedEventHandler changed = PropertyChanged;
            if (changed != null)
            {
                changed(this, eventArgs);
            }
        }
        public void InvokePropertyChanged()
        {
            Type t = this.GetType();
            var ps = t.GetProperties();
            foreach (var p in ps)
            {
                InvokePropertyChanged(p.Name);
            }
        }
        #endregion

        public string UniqueId { get; set; }

        public List<Tuple<double, double>> Points
        {
            get
            {
                lock (_lockData)
                {
                    return _queueRawData.ToList();
                }
            }
        }

        public string DataSource { get; set; }
        public string DataName { get; set; }

        public string DisplayName
        {
            get
            {
                return DataSeries.SeriesName;
            }
            set
            {
                DataSeries.SeriesName = value;
            }
        }
        public double LineThickness
        {
            get
            {
                return StrokeThickness;
            }
            set
            {
                var i = Convert.ToInt32(value);
                if (i < 1) i = 1;
                if (i > 100) i = 100;
                StrokeThickness = i;
                InvokePropertyChanged("LineThickness");
            }
        }

        private double _dataFactor = 1.0;
        public double DataFactor
        {
            get => _dataFactor;
            set
            {
                if (Math.Abs(_dataFactor - value) > 0.001)
                {
                    _dataFactor = value;
                    InvokePropertyChanged(nameof(DataFactor));
                    UpdateChartSeriesValue();
                }
            }
        }

        private double _dataOffset = 0;
        public double DataOffset
        {
            get => _dataOffset;
            set
            {
                if (Math.Abs(_dataOffset - value) > 0.001)
                {
                    _dataOffset = value;
                    InvokePropertyChanged(nameof(DataFactor));
                    UpdateChartSeriesValue();
                }
            }
        }

        private int _capacity = 10000;

        public int Capacity
        {
            get { return _capacity; }
            set
            {
                _capacity = value;
                DataSeries.FifoCapacity = value;
            }
        }

        private object _lockData = new object();

        private FixSizeQueue<Tuple<double, double>> _queueRawData;

        public ChartDataLine(string dataName)
        : this(dataName, dataName, Colors.Blue)
        {
        }

        public ChartDataLine(string dataName, string displayName, Color seriesColor)
        {
            UniqueId = Guid.NewGuid().ToString();

            _queueRawData = new FixSizeQueue<Tuple<double, double>>(_capacity);

            XAxisId = "DefaultAxisId";
            YAxisId = "DefaultAxisId";
            DataSeries = new XyDataSeries<double, double>(_capacity);

            DisplayName = displayName;

            DataName = dataName;

            Stroke = seriesColor;

            IsVisible = true;
            DataOffset = 0;
            LineThickness = 1;
            DataFactor = 1;
        }

        public void Append(double dt, double value)
        {
            lock (_lockData)
            {
                _queueRawData.Enqueue(Tuple.Create(dt, value));
                (DataSeries as XyDataSeries<double, double>)?.Append(dt, value * _dataFactor + _dataOffset);
            }

        }

        public void UpdateChartSeriesValue()
        {
            lock (_lockData)
            {
                using (DataSeries.SuspendUpdates())
                {
                    XyDataSeries<double, double> s = DataSeries as XyDataSeries<double, double>;
                    for (int i = 0; i < s.Count; i++)
                    {
                        s.Update(i, _queueRawData.ElementAt(i).Item2 * _dataFactor + _dataOffset);
                    }
                }
            }
        }

        public void ClearData()
        {
            lock (_lockData)
            {
                _queueRawData.Clear();
                (DataSeries as XyDataSeries<double, double>)?.Clear();
            }
        }

    }
}
