using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.Event
{
    /// <summary>
    /// EventView.xaml 的交互逻辑
    /// </summary>
    public delegate void ItemSelectionChange(bool changeFalg);

    public  class ItemSelectionData
    {
        public bool _IsSelcet;
        public string _SelectItem;
        public bool IsSelect { get { return _IsSelcet; } set { _IsSelcet = value; } }
        public  string SelectItem { get { return _SelectItem; } set { _SelectItem = value; } }
        public ItemSelectionData(bool IsSelcet,string SelectItem)
        {
            this.IsSelect = IsSelcet;
            this.SelectItem = SelectItem;
        }
    }
    public partial class EventView : UserControl
    {
        public EventView()
        {
            InitializeComponent();
            tbLoadPort1ToolTipList = new List<string>();
        }
        public List<string> tbLoadPort1ToolTipList { get; set; }
        public static readonly DependencyProperty tbLoadPort1ToolTipValueProperty = DependencyProperty.Register(
                   "tbLoadPort1ToolTipValueData", typeof(ItemSelectionData), typeof(EventView));

        public ItemSelectionData tbLoadPort1ToolTipValueData
        {
            get { return (ItemSelectionData)this.GetValue(tbLoadPort1ToolTipValueProperty); }
            set { SetValue(tbLoadPort1ToolTipValueProperty, value); }
        }

        private void tbLoadPort1_ItemSelectionChanged(object sender, Xceed.Wpf.Toolkit.Primitives.ItemSelectionChangedEventArgs e)
        {
            if (e.Item != null)
            {    
                //if(e.Item.ToString()=="ALL")
                tbLoadPort1ToolTipValueData= new ItemSelectionData(e.IsSelected,e.Item.ToString());
            }
            else
            {
                tbLoadPort1ToolTipValueData = null;
            }
        }
    }
	[TemplateVisualState(Name = "Large", GroupName = "SizeStates")]
	[TemplateVisualState(Name = "Small", GroupName = "SizeStates")]
	[TemplateVisualState(Name = "Inactive", GroupName = "ActiveStates")]
	[TemplateVisualState(Name = "Active", GroupName = "ActiveStates")]
	public partial class CirclePointRingLoading : Control
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		// (set) Token: 0x06000002 RID: 2 RVA: 0x00002072 File Offset: 0x00000272
		public double BindableWidth
		{
			get
			{
				return (double)base.GetValue(CirclePointRingLoading.BindableWidthProperty);
			}
			private set
			{
				base.SetValue(CirclePointRingLoading.BindableWidthProperty, value);
			}
		}

		// Token: 0x06000003 RID: 3 RVA: 0x00002088 File Offset: 0x00000288
		private static void BindableWidthCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			CirclePointRingLoading circlePointRingLoading = dependencyObject as CirclePointRingLoading;
			bool flag = circlePointRingLoading == null;
			if (!flag)
			{
				circlePointRingLoading.SetEllipseDiameter((double)e.NewValue);
				circlePointRingLoading.SetEllipseOffset((double)e.NewValue);
				circlePointRingLoading.SetMaxSideLength((double)e.NewValue);
			}
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000020E0 File Offset: 0x000002E0
		private void SetEllipseDiameter(double width)
		{
			this.EllipseDiameter = width / 8.0;
		}

		// Token: 0x06000005 RID: 5 RVA: 0x000020F5 File Offset: 0x000002F5
		private void SetEllipseOffset(double width)
		{
			this.EllipseOffset = new Thickness(0.0, width / 2.0, 0.0, 0.0);
		}

		// Token: 0x06000006 RID: 6 RVA: 0x0000212A File Offset: 0x0000032A
		private void SetMaxSideLength(double width)
		{
			this.MaxSideLength = ((width <= 20.0) ? 20.0 : width);
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000007 RID: 7 RVA: 0x0000214C File Offset: 0x0000034C
		// (set) Token: 0x06000008 RID: 8 RVA: 0x0000216E File Offset: 0x0000036E
		public bool IsActive
		{
			get
			{
				return (bool)base.GetValue(CirclePointRingLoading.IsActiveProperty);
			}
			set
			{
				base.SetValue(CirclePointRingLoading.IsActiveProperty, value);
			}
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002184 File Offset: 0x00000384
		private static void IsActiveChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			CirclePointRingLoading circlePointRingLoading = sender as CirclePointRingLoading;
			bool flag = circlePointRingLoading == null;
			if (!flag)
			{
				circlePointRingLoading.UpdateActiveState();
			}
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000021AC File Offset: 0x000003AC
		private void UpdateActiveState()
		{
			bool isActive = this.IsActive;
			if (isActive)
			{
				VisualStateManager.GoToState(this, this.StateActive, true);
			}
			else
			{
				VisualStateManager.GoToState(this, this.StateInActive, true);
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600000B RID: 11 RVA: 0x000021E4 File Offset: 0x000003E4
		// (set) Token: 0x0600000C RID: 12 RVA: 0x00002206 File Offset: 0x00000406
		public bool IsLarge
		{
			get
			{
				return (bool)base.GetValue(CirclePointRingLoading.IsLargeProperty);
			}
			set
			{
				base.SetValue(CirclePointRingLoading.IsLargeProperty, value);
			}
		}

		// Token: 0x0600000D RID: 13 RVA: 0x0000221C File Offset: 0x0000041C
		private static void IsLargeChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			CirclePointRingLoading circlePointRingLoading = sender as CirclePointRingLoading;
			bool flag = circlePointRingLoading == null;
			if (!flag)
			{
				circlePointRingLoading.UpdateLargeState();
			}
		}

		// Token: 0x0600000E RID: 14 RVA: 0x00002244 File Offset: 0x00000444
		private void UpdateLargeState()
		{
			bool isLarge = this.IsLarge;
			if (isLarge)
			{
				VisualStateManager.GoToState(this, this.StateLarge, true);
			}
			else
			{
				VisualStateManager.GoToState(this, this.StateSmall, true);
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600000F RID: 15 RVA: 0x0000227C File Offset: 0x0000047C
		// (set) Token: 0x06000010 RID: 16 RVA: 0x0000229E File Offset: 0x0000049E
		public double MaxSideLength
		{
			get
			{
				return (double)base.GetValue(CirclePointRingLoading.MaxSideLengthProperty);
			}
			set
			{
				base.SetValue(CirclePointRingLoading.MaxSideLengthProperty, value);
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000011 RID: 17 RVA: 0x000022B4 File Offset: 0x000004B4
		// (set) Token: 0x06000012 RID: 18 RVA: 0x000022D6 File Offset: 0x000004D6
		public double EllipseDiameter
		{
			get
			{
				return (double)base.GetValue(CirclePointRingLoading.EllipseDiameterProperty);
			}
			set
			{
				base.SetValue(CirclePointRingLoading.EllipseDiameterProperty, value);
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000013 RID: 19 RVA: 0x000022EC File Offset: 0x000004EC
		// (set) Token: 0x06000014 RID: 20 RVA: 0x0000230E File Offset: 0x0000050E
		public Thickness EllipseOffset
		{
			get
			{
				return (Thickness)base.GetValue(CirclePointRingLoading.EllipseOffsetProperty);
			}
			set
			{
				base.SetValue(CirclePointRingLoading.EllipseOffsetProperty, value);
			}
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002324 File Offset: 0x00000524
		public CirclePointRingLoading()
		{


			base.SizeChanged += new SizeChangedEventHandler((object argument0, SizeChangedEventArgs argument1) => this.BindableWidth = base.ActualWidth);
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002378 File Offset: 0x00000578
		static CirclePointRingLoading()
		{
			FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(CirclePointRingLoading), new FrameworkPropertyMetadata(typeof(CirclePointRingLoading)));
			UIElement.VisibilityProperty.OverrideMetadata(typeof(CirclePointRingLoading), new FrameworkPropertyMetadata(delegate (DependencyObject ringObject, DependencyPropertyChangedEventArgs e)
			{
				bool flag = e.NewValue != e.OldValue;
				if (flag)
				{
					CirclePointRingLoading circlePointRingLoading = (CirclePointRingLoading)ringObject;
					bool flag2 = (Visibility)e.NewValue > Visibility.Visible;
					if (flag2)
					{
						circlePointRingLoading.SetCurrentValue(CirclePointRingLoading.IsActiveProperty, false);
					}
					else
					{
						circlePointRingLoading.IsActive = true;
					}
				}
			}));
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002531 File Offset: 0x00000731
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			this.UpdateLargeState();
			this.UpdateActiveState();
		}

		// Token: 0x04000001 RID: 1
		private string StateActive = "Active";

		// Token: 0x04000002 RID: 2
		private string StateInActive = "InActive";

		// Token: 0x04000003 RID: 3
		private string StateLarge = "Large";

		// Token: 0x04000004 RID: 4
		private string StateSmall = "Small";

		// Token: 0x04000005 RID: 5
		public static readonly DependencyProperty BindableWidthProperty = DependencyProperty.Register("BindableWidth", typeof(double), typeof(CirclePointRingLoading), new PropertyMetadata(0.0, new PropertyChangedCallback(CirclePointRingLoading.BindableWidthCallback)));

		// Token: 0x04000006 RID: 6
		public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register("IsActive", typeof(bool), typeof(CirclePointRingLoading), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(CirclePointRingLoading.IsActiveChanged)));

		// Token: 0x04000007 RID: 7
		public static readonly DependencyProperty IsLargeProperty = DependencyProperty.Register("IsLarge", typeof(bool), typeof(CirclePointRingLoading), new PropertyMetadata(true, new PropertyChangedCallback(CirclePointRingLoading.IsLargeChangedCallback)));

		// Token: 0x04000008 RID: 8
		public static readonly DependencyProperty MaxSideLengthProperty = DependencyProperty.Register("MaxSideLength", typeof(double), typeof(CirclePointRingLoading), new PropertyMetadata(0.0));

		// Token: 0x04000009 RID: 9
		public static readonly DependencyProperty EllipseDiameterProperty = DependencyProperty.Register("EllipseDiameter", typeof(double), typeof(CirclePointRingLoading), new PropertyMetadata(0.0));

		// Token: 0x0400000A RID: 10
		public static readonly DependencyProperty EllipseOffsetProperty = DependencyProperty.Register("EllipseOffset", typeof(Thickness), typeof(CirclePointRingLoading), new PropertyMetadata(default(Thickness)));
	}
}
