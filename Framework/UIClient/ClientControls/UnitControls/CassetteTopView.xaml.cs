using MECF.Framework.UI.Client.Annotations;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MECF.Framework.Common.SubstrateTrackings;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    /// <summary>
    /// CassetteTopView.xaml 的交互逻辑
    /// </summary>
    public partial class CassetteTopView : UserControl, INotifyPropertyChanged
    {
        public string CarrierModule
        {
            get { return (string)GetValue(CarrierModuleProperty); }
            set
            {
                SetValue(CarrierModuleProperty, value);
            }
        }

        public static readonly DependencyProperty CarrierModuleProperty =
            DependencyProperty.Register("CarrierModule", typeof(string), typeof(CassetteTopView), new PropertyMetadata("System"));

        public int CarrierSlot
        {
            get { return (int)GetValue(CarrierSlotProperty); }
            set
            {
                SetValue(CarrierSlotProperty, value);
            }
        }

        public static readonly DependencyProperty CarrierSlotProperty =
            DependencyProperty.Register("CarrierSlot", typeof(int), typeof(CassetteTopView), new PropertyMetadata(0));


        public CarrierInfo CarrierData
        {
            get { return (CarrierInfo)GetValue(CarrierDataProperty); }
            set
            {
                SetValue(CarrierDataProperty, value);
            }
        }

        public static readonly DependencyProperty CarrierDataProperty =
            DependencyProperty.Register("CarrierData", typeof(CarrierInfo), typeof(CassetteTopView), new PropertyMetadata(new CarrierInfo(1)));

        public CassetteTopView()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
 
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
        }

    }


}
