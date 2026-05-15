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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using MECF.Framework.UI.Client.IndustrialControl.Converters;

namespace MECF.Framework.UI.Client.IndustrialControl
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class PipeLine : UserControl
    {
        #region Contructor

        /// <summary>
        /// 实例化一个管道对象
        /// </summary>
        public PipeLine()
        {
            InitializeComponent();

            //Binding binding = new Binding( );
            //binding.Source = grid1;
            //binding.Path = new PropertyPath( "ActualHeight" );
            //binding.Converter = new MultiplesValueConverter( );
            //binding.ConverterParameter = -1;
            //ellipe1.SetBinding( Canvas.TopProperty, binding );

            offectDoubleAnimation = new DoubleAnimation(0, 10, TimeSpan.FromMilliseconds(1000));
            offectDoubleAnimation.RepeatBehavior = RepeatBehavior.Forever;

            BeginAnimation(LineOffectProperty, offectDoubleAnimation);
        }

        private DoubleAnimation offectDoubleAnimation = null;

        #endregion

        #region Property Dependency

        #region LeftDirection Property

        /// <summary>
        /// 设置左边的方向
        /// </summary>
        public HslPipeTurnDirection LeftDirection
        {
            get { return (HslPipeTurnDirection)GetValue(LeftDirectionProperty); }
            set { SetValue(LeftDirectionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LeftDirection.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LeftDirectionProperty =
            DependencyProperty.Register("LeftDirection", typeof(HslPipeTurnDirection), typeof(PipeLine),
                new PropertyMetadata(HslPipeTurnDirection.Left, new PropertyChangedCallback(LeftDirectionPropertyChangedCallback)));

        public static void LeftDirectionPropertyChangedCallback(System.Windows.DependencyObject dependency, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            PipeLine pipeLine = (PipeLine)dependency;
            pipeLine.UpdateLeftDirectionBinding();
        }

        public void UpdateLeftDirectionBinding()
        {
            BindingOperations.ClearBinding(ellipe1, Canvas.TopProperty);
            if (LeftDirection == HslPipeTurnDirection.Left)
            {
                canvas1.Visibility = Visibility.Visible;
                Binding binding = new Binding();
                binding.Source = grid1;
                binding.Path = new PropertyPath("ActualHeight");
                binding.Converter = new MultiplesValueConverter();
                binding.ConverterParameter = 0;
                ellipe1.SetBinding(Canvas.TopProperty, binding);
            }
            else if (LeftDirection == HslPipeTurnDirection.Right)
            {
                canvas1.Visibility = Visibility.Visible;
                Binding binding = new Binding();
                binding.Source = grid1;
                binding.Path = new PropertyPath("ActualHeight");
                binding.Converter = new MultiplesValueConverter();
                binding.ConverterParameter = -1;
                ellipe1.SetBinding(Canvas.TopProperty, binding);
            }
            else
            {
                canvas1.Visibility = Visibility.Collapsed;
            }
            UpdatePathData();
        }

        #endregion

        #region RightDirection Property

        /// <summary>
        /// 设置右边的方向
        /// </summary>
        public HslPipeTurnDirection RightDirection
        {
            get { return (HslPipeTurnDirection)GetValue(RightDirectionProperty); }
            set { SetValue(RightDirectionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LeftDirection.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RightDirectionProperty =
            DependencyProperty.Register("RightDirection", typeof(HslPipeTurnDirection), typeof(PipeLine),
                new PropertyMetadata(HslPipeTurnDirection.Right, new PropertyChangedCallback(RightDirectionPropertyChangedCallback)));

        public static void RightDirectionPropertyChangedCallback(System.Windows.DependencyObject dependency, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            PipeLine pipeLine = (PipeLine)dependency;
            pipeLine.UpdateRightDirectionBinding();
        }

        public void UpdateRightDirectionBinding()
        {
            BindingOperations.ClearBinding(ellipe2, Canvas.TopProperty);
            if (RightDirection == HslPipeTurnDirection.Left)
            {
                canvas2.Visibility = Visibility.Visible;
                Binding binding = new Binding();
                binding.Source = grid1;
                binding.Path = new PropertyPath("ActualHeight");
                binding.Converter = new MultiplesValueConverter();
                binding.ConverterParameter = 0;
                ellipe2.SetBinding(Canvas.TopProperty, binding);
            }
            else if (RightDirection == HslPipeTurnDirection.Right)
            {
                canvas2.Visibility = Visibility.Visible;
                Binding binding = new Binding();
                binding.Source = grid1;
                binding.Path = new PropertyPath("ActualHeight");
                binding.Converter = new MultiplesValueConverter();
                binding.ConverterParameter = -1;
                ellipe2.SetBinding(Canvas.TopProperty, binding);
            }
            else
            {
                canvas2.Visibility = Visibility.Collapsed;
            }
            UpdatePathData();
        }

        #endregion

        #region PipeLineActive Property

        public bool PipeLineActive
        {
            get { return (bool)GetValue(PipeLineActiveProperty); }
            set { SetValue(PipeLineActiveProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PipeLineActive.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PipeLineActiveProperty =
            DependencyProperty.Register("PipeLineActive", typeof(bool), typeof(PipeLine), new PropertyMetadata(false));

        #endregion

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            UpdatePathData();
            base.OnRenderSizeChanged(sizeInfo);
        }

        #region LineOffect Property

        public double LineOffect
        {
            get { return (double)GetValue(LineOffectProperty); }
            set { SetValue(LineOffectProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LineOffect.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LineOffectProperty =
            DependencyProperty.Register("LineOffect", typeof(double), typeof(PipeLine), new PropertyMetadata(0d));


        public void UpdatePathData()
        {
            //Console.WriteLine( "Size Changed" );
            var g = new StreamGeometry();
            using (StreamGeometryContext context = g.Open())
            {
                if (LeftDirection == HslPipeTurnDirection.Left)
                {
                    context.BeginFigure(new Point(ActualHeight / 2, ActualHeight), false, false);
                    context.ArcTo(new Point(ActualHeight, ActualHeight / 2), new Size(ActualHeight / 2, ActualHeight / 2), 0, false, SweepDirection.Clockwise, true, false);
                }
                else if (LeftDirection == HslPipeTurnDirection.Right)
                {
                    context.BeginFigure(new Point(ActualHeight / 2, 0), false, false);
                    context.ArcTo(new Point(ActualHeight, ActualHeight / 2), new Size(ActualHeight / 2, ActualHeight / 2), 0, false, SweepDirection.Counterclockwise, true, false);
                }
                else
                {
                    context.BeginFigure(new Point(0, ActualHeight / 2), false, false);
                    context.LineTo(new Point(ActualHeight, ActualHeight / 2), true, false);
                }

                context.LineTo(new Point(ActualWidth - ActualHeight, ActualHeight / 2), true, false);

                if (RightDirection == HslPipeTurnDirection.Left)
                {
                    context.ArcTo(new Point(ActualWidth - ActualHeight / 2, ActualHeight), new Size(ActualHeight / 2, ActualHeight / 2), 0, false, SweepDirection.Clockwise, true, false);
                }
                else if (RightDirection == HslPipeTurnDirection.Right)
                {
                    context.ArcTo(new Point(ActualWidth - ActualHeight / 2, 0), new Size(ActualHeight / 2, ActualHeight / 2), 0, false, SweepDirection.Counterclockwise, true, false);
                }
                else
                {
                    context.LineTo(new Point(ActualWidth, ActualHeight / 2), true, false);
                }

            }
            path1.Data = g;
        }

        #endregion

        #region MoveSpeed Property

        /// <summary>
        /// 获取或设置流动的速度
        /// </summary>
        public double MoveSpeed
        {
            get { return (double)GetValue(MoveSpeedProperty); }
            set { SetValue(MoveSpeedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MoveSpeed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MoveSpeedProperty =
            DependencyProperty.Register("MoveSpeed", typeof(double), typeof(PipeLine), new PropertyMetadata(0.3d, new PropertyChangedCallback(MoveSpeedPropertyChangedCallback)));

        public static void MoveSpeedPropertyChangedCallback(DependencyObject dependency, DependencyPropertyChangedEventArgs e)
        {
            PipeLine pipeLine = (PipeLine)dependency;
            pipeLine.UpdateMoveSpeed();
        }

        private Storyboard storyboard = new Storyboard();

        public void UpdateMoveSpeed()
        {
            if (MoveSpeed > 0)
            {
                path1.Visibility = Visibility.Visible;
                offectDoubleAnimation.From = 0d;
                offectDoubleAnimation.To = 10d;
                offectDoubleAnimation.Duration = TimeSpan.FromMilliseconds(300 / MoveSpeed);
                BeginAnimation(LineOffectProperty, offectDoubleAnimation);
            }
            else if (MoveSpeed < 0)
            {
                path1.Visibility = Visibility.Visible;
                offectDoubleAnimation.From = 0d;
                offectDoubleAnimation.To = -10d;
                offectDoubleAnimation.Duration = TimeSpan.FromMilliseconds(300 / Math.Abs(MoveSpeed));
                BeginAnimation(LineOffectProperty, offectDoubleAnimation);
            }
            else
            {
                offectDoubleAnimation.From = 0d;
                offectDoubleAnimation.To = 0d;
                BeginAnimation(LineOffectProperty, offectDoubleAnimation);
                path1.Visibility = Visibility.Hidden;
            }
        }

        #endregion

        #region CenterColor Property

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
            DependencyProperty.Register("CenterColor", typeof(Color), typeof(PipeLine), new PropertyMetadata(Colors.LightGray));

        #endregion

        #region PipeLineWidth Property

        /// <summary>
        /// 管道活动状态时的中心线的线条宽度
        /// </summary>
        public int PipeLineWidth
        {
            get { return (int)GetValue(PipeLineWidthProperty); }
            set { SetValue(PipeLineWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PipeLineWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PipeLineWidthProperty =
            DependencyProperty.Register("PipeLineWidth", typeof(int), typeof(PipeLine), new PropertyMetadata(2));

        #endregion

        #region ActiveLineCenterColor Property

        /// <summary>
        /// 管道活动状态时的中心线的颜色信息
        /// </summary>
        public Color ActiveLineCenterColor
        {
            get { return (Color)GetValue(ActiveLineCenterColorProperty); }
            set { SetValue(ActiveLineCenterColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActiveLineCenterColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveLineCenterColorProperty =
            DependencyProperty.Register("ActiveLineCenterColor", typeof(Color), typeof(PipeLine), new PropertyMetadata(Colors.DodgerBlue));

        #endregion

        #region MyRegion

        /// <summary>
        /// 管道控件的边缘颜色
        /// </summary>
        public Color EdgeColor
        {
            get { return (Color)GetValue(EdgeColorProperty); }
            set { SetValue(EdgeColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EdgeColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EdgeColorProperty =
            DependencyProperty.Register("EdgeColor", typeof(Color), typeof(PipeLine), new PropertyMetadata(Colors.DimGray));



        #endregion

        #endregion
    }

    public enum HslPipeTurnDirection
    {
        /// <summary>
        /// 向上
        /// </summary>
        Up = 1,
        /// <summary>
        /// 向下
        /// </summary>
        Down = 2,
        /// <summary>
        /// 向左
        /// </summary>
        Left = 3,
        /// <summary>
        /// 向右
        /// </summary>
        Right = 4,
        /// <summary>
        /// 无效果
        /// </summary>
        None = 5,
    }
}
