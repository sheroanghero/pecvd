using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.Annotations;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    /// <summary>
    /// Door.xaml 的交互逻辑
    /// </summary>
    public partial class FoupTopView : UserControl, INotifyPropertyChanged
    {
        public double CanvasYPosition { get; set; }

        public bool _isFoupLoaded;

        public bool IsFoupLoaded
        {
            get { return (bool)GetValue(IsFoupLoadedProperty); }
            set
            {
                SetValue(IsFoupLoadedProperty, value);

                CanvasYPosition = value ? -20 : 0;
                OnPropertyChanged(nameof(CanvasYPosition));
            }
        }

        public static readonly DependencyProperty IsFoupLoadedProperty =
            DependencyProperty.Register("IsFoupLoaded", typeof(bool), typeof(FoupTopView),
                new PropertyMetadata(false, PropertyChangedCallback));

        public string ModuleName
        {
            get { return (string)GetValue(ModuleNameProperty); }
            set { SetValue(ModuleNameProperty, value); }
        }

        public static readonly DependencyProperty ModuleNameProperty =
            DependencyProperty.Register("ModuleName", typeof(string), typeof(FoupTopView),
                new PropertyMetadata("LP1", PropertyChangedCallback));

        public Visibility MenuVisibility
        {
            get { return (Visibility)this.GetValue(MenuVisibilityProperty); }
            set { this.SetValue(MenuVisibilityProperty, value); }
        }

        public static readonly DependencyProperty MenuVisibilityProperty =
            DependencyProperty.Register("MenuVisibility", typeof(Visibility), typeof(FoupTopView),
                new FrameworkPropertyMetadata(Visibility.Visible, FrameworkPropertyMetadataOptions.AffectsRender));

        public LPMenuEnable DeviceData
        {
            get
            {
                return (LPMenuEnable)this.GetValue(DeviceDataProperty);
            }
            set
            {
                this.SetValue(DeviceDataProperty, value);
            }
        }

        public static readonly DependencyProperty DeviceDataProperty = DependencyProperty.Register(
            "DeviceData", typeof(LPMenuEnable), typeof(FoupTopView),
                new FrameworkPropertyMetadata(new LPMenuEnable(), FrameworkPropertyMetadataOptions.AffectsRender));

        public FoupTopView()
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
            var self = (FoupTopView)d;
            switch (e.Property.Name)
            {
                case "IsFoupLoaded":
                    self.CanvasYPosition = (bool)e.NewValue ? -30 : 0;
                    self.OnPropertyChanged(nameof(CanvasYPosition));
                    break;
                default:
                    self.OnPropertyChanged(nameof(e.Property.Name));
                    break;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            canvas.ContextMenu.Visibility = MenuVisibility;

            if (DeviceData == null)
                return;

            if (MenuVisibility == Visibility.Visible && canvas.ContextMenu.Items != null && canvas.ContextMenu.Items.Count == 13)
            {
                ((MenuItem)canvas.ContextMenu.Items[1]).IsEnabled = DeviceData.IsResetEnable;
                ((MenuItem)canvas.ContextMenu.Items[2]).IsEnabled = DeviceData.IsAbortEnable;             
                ((MenuItem)canvas.ContextMenu.Items[3]).IsEnabled = DeviceData.IsLoadEnable;
                ((MenuItem)canvas.ContextMenu.Items[4]).IsEnabled = DeviceData.IsUnloadEnable;
                ((MenuItem)canvas.ContextMenu.Items[5]).IsEnabled = DeviceData.IsReadCarrierIDEnable;
                ((MenuItem)canvas.ContextMenu.Items[6]).IsEnabled = DeviceData.IsClampEnable;
                ((MenuItem)canvas.ContextMenu.Items[7]).IsEnabled = DeviceData.IsUnclampEnable;
                ((MenuItem)canvas.ContextMenu.Items[8]).IsEnabled = DeviceData.IsDockEnable;
                ((MenuItem)canvas.ContextMenu.Items[9]).IsEnabled = DeviceData.IsUndockEnable;
                ((MenuItem)canvas.ContextMenu.Items[10]).IsEnabled = DeviceData.IsOpenEnable;
                ((MenuItem)canvas.ContextMenu.Items[11]).IsEnabled = DeviceData.IsCloseEnable;
                ((MenuItem)canvas.ContextMenu.Items[12]).IsEnabled = DeviceData.IsMapEnable;
            }
        }

        private void Home(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"{ModuleName}.Home");
        }

        private void Reset(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"{ModuleName}.Reset");
        }

        private void Abort(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"{ModuleName}.Abort");
        }

        private void ReadCarrierID(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"{ModuleName}.ReadCarrierId");
        }

        private void Load(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"{ModuleName}.Load");
        }

        private void Unload(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"{ModuleName}.Unload");
        }

        private void Clamp(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"{ModuleName}.Clamp");
        }

        private void Unclamp(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"{ModuleName}.Unclamp");
        }

        private void Dock(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"{ModuleName}.Dock");
        }

        private void Undock(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"{ModuleName}.Undock");
        }

        private void OpenDoor(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"{ModuleName}.OpenDoor");
        }

        private void CloseDoor(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"{ModuleName}.CloseDoor");
        }

        private void Map(object sender, RoutedEventArgs e)
        {
            InvokeClient.Instance.Service.DoOperation($"{ModuleName}.Map");
        }
    }

    public class LPMenuEnable : NotifiableItem
    {
        public bool IsResetEnable { get; set; }
        public bool IsAbortEnable { get; set; }
        public bool IsReadCarrierIDEnable { get; set; }
        public bool IsLoadEnable { get; set; }
        public bool IsUnloadEnable { get; set; }
        public bool IsClampEnable { get; set; }
        public bool IsUnclampEnable { get; set; }
        public bool IsDockEnable { get; set; }
        public bool IsUndockEnable { get; set; }
        public bool IsOpenEnable { get; set; }
        public bool IsCloseEnable { get; set; }
        public bool IsMapEnable { get; set; }
    }
}
