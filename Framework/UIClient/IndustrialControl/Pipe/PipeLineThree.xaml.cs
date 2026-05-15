using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MECF.Framework.UI.Client.IndustrialControl
{
	/// <summary>
	/// HslPipeLineThree.xaml 的交互逻辑
	/// </summary>
	public partial class PipeLineThree : UserControl
	{
		public PipeLineThree()
		{
			InitializeComponent();


			offect1DoubleAnimation = new DoubleAnimation(0, 10, TimeSpan.FromMilliseconds(1000));
			offect1DoubleAnimation.RepeatBehavior = RepeatBehavior.Forever;

			BeginAnimation(LineOffect1Property, offect1DoubleAnimation);



			offect2DoubleAnimation = new DoubleAnimation(0, 10, TimeSpan.FromMilliseconds(1000));
			offect2DoubleAnimation.RepeatBehavior = RepeatBehavior.Forever;

			BeginAnimation(LineOffect2Property, offect2DoubleAnimation);


			offect3DoubleAnimation = new DoubleAnimation(0, 10, TimeSpan.FromMilliseconds(1000));
			offect3DoubleAnimation.RepeatBehavior = RepeatBehavior.Forever;

			BeginAnimation(LineOffect3Property, offect3DoubleAnimation);
		}


		/// <summary>
		/// 获取或设置管道控件的边缘颜色
		/// </summary>
		public Color EdgeColor
		{
			get { return (Color)GetValue(EdgeColorProperty); }
			set { SetValue(EdgeColorProperty, value); }
		}

		// Using a DependencyProperty as the backing store for EdgeColor.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty EdgeColorProperty =
			DependencyProperty.Register("EdgeColor", typeof(Color), typeof(PipeLineThree), new PropertyMetadata(Colors.DimGray));

		/// <summary>
		/// 管道的中心颜色
		/// </summary>
		public Color CenterColor
		{
			get { return (Color)GetValue(CenterColorProperty); }
			set { SetValue(CenterColorProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CenterColor.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CenterColorProperty =
			DependencyProperty.Register("CenterColor", typeof(Color), typeof(PipeLineThree), new PropertyMetadata(Colors.LightGray));

		/// <summary>
		/// 获取或设置管道1号线是否激活液体显示
		/// </summary>
		public bool PipeLineActive1
		{
			get { return (bool)GetValue(PipeLineActive1Property); }
			set { SetValue(PipeLineActive1Property, value); }
		}

		// Using a DependencyProperty as the backing store for PipeLineActive1.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PipeLineActive1Property =
			DependencyProperty.Register("PipeLineActive1", typeof(bool), typeof(PipeLineThree), new PropertyMetadata(false));

		/// <summary>
		/// 获取或设置管道2号线是否激活液体显示
		/// </summary>
		public bool PipeLineActive2
		{
			get { return (bool)GetValue(PipeLineActive2Property); }
			set { SetValue(PipeLineActive2Property, value); }
		}

		// Using a DependencyProperty as the backing store for PipeLineActive2.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PipeLineActive2Property =
			DependencyProperty.Register("PipeLineActive2", typeof(bool), typeof(PipeLineThree), new PropertyMetadata(false));

		/// <summary>
		/// 获取或设置管道3号线是否激活液体显示
		/// </summary>
		public bool PipeLineActive3
		{
			get { return (bool)GetValue(PipeLineActive3Property); }
			set { SetValue(PipeLineActive3Property, value); }
		}

		// Using a DependencyProperty as the backing store for PipeLineActive3.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PipeLineActive3Property =
			DependencyProperty.Register("PipeLineActive3", typeof(bool), typeof(PipeLineThree), new PropertyMetadata(false));

		/// <summary>
		/// 获取或设置管道1号线液体流动的速度，0为静止，正数为正向流动，负数为反向流动
		/// </summary>
		public double MoveSpeed1
		{
			get { return (double)GetValue(MoveSpeed1Property); }
			set { SetValue(MoveSpeed1Property, value); }
		}

		// Using a DependencyProperty as the backing store for MoveSpeed1.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MoveSpeed1Property =
			DependencyProperty.Register("MoveSpeed1", typeof(double), typeof(PipeLineThree),
				new PropertyMetadata(0d, new PropertyChangedCallback(MoveSpeed1PropertyChangedCallback)));

		public static void MoveSpeed1PropertyChangedCallback(DependencyObject dependency, DependencyPropertyChangedEventArgs e)
		{
			PipeLineThree pipeLine = (PipeLineThree)dependency;
			pipeLine.UpdateMoveSpeed1();
		}

		private DoubleAnimation offect1DoubleAnimation = null;

		public void UpdateMoveSpeed1()
		{
			if (MoveSpeed1 > 0)
			{
				path1.Visibility = Visibility.Visible;
				offect1DoubleAnimation.From = 0d;
				offect1DoubleAnimation.To = 10d;
				offect1DoubleAnimation.Duration = TimeSpan.FromMilliseconds(300 / MoveSpeed1);
				BeginAnimation(LineOffect1Property, offect1DoubleAnimation);
			}
			else if (MoveSpeed1 < 0)
			{
				path1.Visibility = Visibility.Visible;
				offect1DoubleAnimation.From = 0d;
				offect1DoubleAnimation.To = -10d;
				offect1DoubleAnimation.Duration = TimeSpan.FromMilliseconds(300 / Math.Abs(MoveSpeed1));
				BeginAnimation(LineOffect1Property, offect1DoubleAnimation);
			}
			else
			{
				offect1DoubleAnimation.From = 0d;
				offect1DoubleAnimation.To = 0d;
				BeginAnimation(LineOffect1Property, offect1DoubleAnimation);
				path1.Visibility = Visibility.Hidden;
			}
		}

		/// <summary>
		/// 获取或设置管道2号线液体流动的速度，0为静止，正数为正向流动，负数为反向流动
		/// </summary>
		public double MoveSpeed2
		{
			get { return (double)GetValue(MoveSpeed2Property); }
			set { SetValue(MoveSpeed2Property, value); }
		}

		// Using a DependencyProperty as the backing store for MoveSpeed2.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MoveSpeed2Property =
			DependencyProperty.Register("MoveSpeed2", typeof(double), typeof(PipeLineThree),
				new PropertyMetadata(0d, new PropertyChangedCallback(MoveSpeed2PropertyChangedCallback)));

		public static void MoveSpeed2PropertyChangedCallback(DependencyObject dependency, DependencyPropertyChangedEventArgs e)
		{
			PipeLineThree pipeLine = (PipeLineThree)dependency;
			pipeLine.UpdateMoveSpeed2();
		}

		private DoubleAnimation offect2DoubleAnimation = null;

		public void UpdateMoveSpeed2()
		{
			if (MoveSpeed2 > 0)
			{
				path2.Visibility = Visibility.Visible;
				offect2DoubleAnimation.From = 0d;
				offect2DoubleAnimation.To = 10d;
				offect2DoubleAnimation.Duration = TimeSpan.FromMilliseconds(300 / MoveSpeed2);
				BeginAnimation(LineOffect2Property, offect2DoubleAnimation);
			}
			else if (MoveSpeed2 < 0)
			{
				path2.Visibility = Visibility.Visible;
				offect2DoubleAnimation.From = 0d;
				offect2DoubleAnimation.To = -10d;
				offect2DoubleAnimation.Duration = TimeSpan.FromMilliseconds(300 / Math.Abs(MoveSpeed2));
				BeginAnimation(LineOffect2Property, offect2DoubleAnimation);
			}
			else
			{
				offect2DoubleAnimation.From = 0d;
				offect2DoubleAnimation.To = 0d;
				BeginAnimation(LineOffect2Property, offect2DoubleAnimation);
				path2.Visibility = Visibility.Hidden;
			}
		}

		/// <summary>
		/// 获取或设置管道3号线液体流动的速度，0为静止，正数为正向流动，负数为反向流动
		/// </summary>
		public double MoveSpeed3
		{
			get { return (double)GetValue(MoveSpeed3Property); }
			set { SetValue(MoveSpeed3Property, value); }
		}

		// Using a DependencyProperty as the backing store for MoveSpeed3.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MoveSpeed3Property =
			DependencyProperty.Register("MoveSpeed3", typeof(double), typeof(PipeLineThree),
				new PropertyMetadata(0d, new PropertyChangedCallback(MoveSpeed3PropertyChangedCallback)));

		public static void MoveSpeed3PropertyChangedCallback(DependencyObject dependency, DependencyPropertyChangedEventArgs e)
		{
			PipeLineThree pipeLine = (PipeLineThree)dependency;
			pipeLine.UpdateMoveSpeed3();
		}

		private DoubleAnimation offect3DoubleAnimation = null;

		public void UpdateMoveSpeed3()
		{
			if (MoveSpeed3 > 0)
			{
				path3.Visibility = Visibility.Visible;
				offect3DoubleAnimation.From = 0d;
				offect3DoubleAnimation.To = 10d;
				offect3DoubleAnimation.Duration = TimeSpan.FromMilliseconds(300 / MoveSpeed3);
				BeginAnimation(LineOffect3Property, offect3DoubleAnimation);
			}
			else if (MoveSpeed3 < 0)
			{
				path3.Visibility = Visibility.Visible;
				offect3DoubleAnimation.From = 0d;
				offect3DoubleAnimation.To = -10d;
				offect3DoubleAnimation.Duration = TimeSpan.FromMilliseconds(300 / Math.Abs(MoveSpeed3));
				BeginAnimation(LineOffect3Property, offect3DoubleAnimation);
			}
			else
			{
				offect3DoubleAnimation.From = 0d;
				offect3DoubleAnimation.To = 0d;
				BeginAnimation(LineOffect3Property, offect3DoubleAnimation);
				path3.Visibility = Visibility.Hidden;
			}
		}

		/// <summary>
		/// 管道1的偏移
		/// </summary>
		public double LineOffect1
		{
			get { return (double)GetValue(LineOffect1Property); }
			set { SetValue(LineOffect1Property, value); }
		}

		// Using a DependencyProperty as the backing store for LineOffect1.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty LineOffect1Property =
			DependencyProperty.Register("LineOffect1", typeof(double), typeof(PipeLineThree), new PropertyMetadata(0d));

		/// <summary>
		/// 管道2的偏移
		/// </summary>
		public double LineOffect2
		{
			get { return (double)GetValue(LineOffect2Property); }
			set { SetValue(LineOffect2Property, value); }
		}

		// Using a DependencyProperty as the backing store for LineOffect2.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty LineOffect2Property =
			DependencyProperty.Register("LineOffect2", typeof(double), typeof(PipeLineThree), new PropertyMetadata(0d));

		/// <summary>
		/// 管道3的偏移
		/// </summary>
		public double LineOffect3
		{
			get { return (double)GetValue(LineOffect3Property); }
			set { SetValue(LineOffect3Property, value); }
		}

		// Using a DependencyProperty as the backing store for LineOffect3.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty LineOffect3Property =
			DependencyProperty.Register("LineOffect3", typeof(double), typeof(PipeLineThree), new PropertyMetadata(0d));



		/// <summary>
		/// 获取或设置中间管道线的宽度信息，默认为3
		/// </summary>
		public int PipeLineWidth
		{
			get { return (int)GetValue(PipeLineWidthProperty); }
			set { SetValue(PipeLineWidthProperty, value); }
		}

		// Using a DependencyProperty as the backing store for PipeLineWidth.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PipeLineWidthProperty =
			DependencyProperty.Register("PipeLineWidth", typeof(int), typeof(PipeLineThree), new PropertyMetadata(2));

		/// <summary>
		/// 获取或设置流动状态时管道控件的中心颜色
		/// </summary>
		public Color ActiveLineCenterColor
		{
			get { return (Color)GetValue(ActiveLineCenterColorProperty); }
			set { SetValue(ActiveLineCenterColorProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ActiveLineCenterColor.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ActiveLineCenterColorProperty =
			DependencyProperty.Register("ActiveLineCenterColor", typeof(Color), typeof(PipeLineThree), new PropertyMetadata(Colors.DodgerBlue));

		/// <summary>
		/// 获取或设置管道的宽度，默认为30
		/// </summary>
		public int PipeWidth
		{
			get { return (int)GetValue(PipeWidthProperty); }
			set { SetValue(PipeWidthProperty, value); }
		}

		// Using a DependencyProperty as the backing store for PipeWidth.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PipeWidthProperty =
			DependencyProperty.Register("PipeWidth", typeof(int), typeof(PipeLineThree),
				new PropertyMetadata(30, new PropertyChangedCallback(PipeWidthPropertyChangedCallback)));


		public static void PipeWidthPropertyChangedCallback(DependencyObject dependency, DependencyPropertyChangedEventArgs e)
		{
			PipeLineThree pipeLine = (PipeLineThree)dependency;
			pipeLine.PipeWidthUpdate();
		}

		public void PipeWidthUpdate()
		{
			UpdatePath();
		}


		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			UpdatePath();
			base.OnRenderSizeChanged(sizeInfo);
		}

		public void UpdatePath()
		{
			polygon1.Points = new PointCollection(new Point[]
			{
				new Point(ActualWidth / 2 - PipeWidth / 2d, PipeWidth),
				new Point(ActualWidth / 2, PipeWidth / 2d),
				new Point(ActualWidth / 2 + PipeWidth / 2d, PipeWidth),
				new Point(ActualWidth / 2 + PipeWidth / 2d, ActualHeight),
				new Point(ActualWidth / 2 - PipeWidth / 2d, ActualHeight),
				new Point(ActualWidth / 2 - PipeWidth / 2d, PipeWidth),
			});

			var g1 = new StreamGeometry();
			using (StreamGeometryContext context = g1.Open())
			{
				context.BeginFigure(new Point(0, PipeWidth / 2d), false, false);
				context.LineTo(new Point(ActualWidth, PipeWidth / 2d), true, false);
			}
			path1.Data = g1;

			var g2 = new StreamGeometry();
			using (StreamGeometryContext context = g2.Open())
			{
				context.BeginFigure(new Point(0, PipeWidth / 2d), false, false);
				context.LineTo(new Point(ActualWidth / 2 - PipeWidth / 2d, PipeWidth / 2d), true, false);
				context.ArcTo(new Point(ActualWidth / 2, PipeWidth), new Size(PipeWidth / 2d, PipeWidth / 2d), 0, false, SweepDirection.Clockwise, true, false);
				context.LineTo(new Point(ActualWidth / 2, ActualHeight), true, false);
			}
			path2.Data = g2;

			var g3 = new StreamGeometry();
			using (StreamGeometryContext context = g3.Open())
			{
				context.BeginFigure(new Point(ActualWidth, PipeWidth / 2d), false, false);
				context.LineTo(new Point(ActualWidth / 2 + PipeWidth / 2d, PipeWidth / 2d), true, false);
				context.ArcTo(new Point(ActualWidth / 2, PipeWidth), new Size(PipeWidth / 2d, PipeWidth / 2d), 0, false, SweepDirection.Counterclockwise, true, false);
				context.LineTo(new Point(ActualWidth / 2, ActualHeight), true, false);
			}
			path3.Data = g3;


		}
	}
}
