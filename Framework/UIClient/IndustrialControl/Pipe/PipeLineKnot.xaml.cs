using MECF.Framework.UI.Client.IndustrialControl.Converters;
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
    /// PipeLineKnot.xaml 的交互逻辑
    /// </summary>
    public partial class PipeLineKnot : UserControl
    {
        #region Contructor

        /// <summary>
        /// 实例化一个管道对象
        /// </summary>
        public PipeLineKnot()
        {
            InitializeComponent();

            //Binding binding = new Binding();
            //binding.Source = grid1;
            //binding.Path = new PropertyPath("ActualHeight");
            //binding.Converter = new MultiplesValueConverter();
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
            DependencyProperty.Register("LeftDirection", typeof(HslPipeTurnDirection), typeof(PipeLineKnot),
                new PropertyMetadata(HslPipeTurnDirection.Left, new PropertyChangedCallback(LeftDirectionPropertyChangedCallback)));

        /// <summary>
        /// Description
        /// </summary>
        public static readonly DependencyProperty ThroughInnerColorProperty =
            DependencyProperty.Register("ThroughInnerColor",
                                        typeof(string),
                                        typeof(PipeLineKnot),
                                        new FrameworkPropertyMetadata("White"));

        /// <summary>
        /// A property wrapper for the <see cref="BalloonTextProperty"/>
        /// dependency property:<br/>
        /// Description
        /// </summary>
        public string ThroughInnerColor
        {
            get { return (string)GetValue(ThroughInnerColorProperty); }
            set { SetValue(ThroughInnerColorProperty, value); }
        }

        public static void LeftDirectionPropertyChangedCallback(System.Windows.DependencyObject dependency, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            PipeLine pipeLine = (PipeLine)dependency;
            pipeLine.UpdateLeftDirectionBinding();
        }

        public void UpdateLeftDirectionBinding()
        {
            if (LeftDirection == HslPipeTurnDirection.Left)
            {
                Binding binding = new Binding();
                binding.Source = grid1;
                binding.Path = new PropertyPath("ActualHeight");
                binding.Converter = new MultiplesValueConverter();
                binding.ConverterParameter = 0;
            }
            else if (LeftDirection == HslPipeTurnDirection.Right)
            {
                Binding binding = new Binding();
                binding.Source = grid1;
                binding.Path = new PropertyPath("ActualHeight");
                binding.Converter = new MultiplesValueConverter();
                binding.ConverterParameter = -1;
            }
            else
            {
            }
            UpdatePathData();
        }

        public void UpdatePath()
        {
            polygon1.Points = new PointCollection(new Point[]
            {
                new Point(0, ActualHeight*0.6),
                new Point(0, ActualHeight),
                new Point( ActualHeight*0.7, ActualHeight),
                new Point( ActualHeight *0.3, ActualHeight*0.6),

            });
            polygon2.Points = new PointCollection(new Point[]
           {
                new Point(ActualHeight*1.9, ActualHeight*0.6),
                new Point(ActualHeight*1.5, ActualHeight),
                new Point( ActualHeight*2.2, ActualHeight),
                new Point( ActualHeight*2.2 , ActualHeight*0.6),

           });
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
            DependencyProperty.Register("RightDirection", typeof(HslPipeTurnDirection), typeof(PipeLineKnot),
                new PropertyMetadata(HslPipeTurnDirection.Right, new PropertyChangedCallback(RightDirectionPropertyChangedCallback)));

        public static void RightDirectionPropertyChangedCallback(System.Windows.DependencyObject dependency, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            PipeLine pipeLine = (PipeLine)dependency;
            pipeLine.UpdateRightDirectionBinding();
        }

        public void UpdateRightDirectionBinding()
        {
            if (RightDirection == HslPipeTurnDirection.Left)
            {
                Binding binding = new Binding();
                binding.Source = grid1;
                binding.Path = new PropertyPath("ActualHeight");
                binding.Converter = new MultiplesValueConverter();
                binding.ConverterParameter = 0;
            }
            else if (RightDirection == HslPipeTurnDirection.Right)
            {
                Binding binding = new Binding();
                binding.Source = grid1;
                binding.Path = new PropertyPath("ActualHeight");
                binding.Converter = new MultiplesValueConverter();
                binding.ConverterParameter = -1;
            }
            else
            {
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
            DependencyProperty.Register("PipeLineActive", typeof(bool), typeof(PipeLineKnot), new PropertyMetadata(false));

        #endregion

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            UpdatePath();
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
            DependencyProperty.Register("LineOffect", typeof(double), typeof(PipeLineKnot), new PropertyMetadata(0d));


        public void UpdatePathData()
        {
            //Console.WriteLine("Size Changed");
            var g = new StreamGeometry();
            using (StreamGeometryContext context = g.Open())
            {
                context.BeginFigure(new Point(0, ActualHeight * 0.6), false, false);
                context.LineTo(new Point(ActualHeight * 0.3, ActualHeight * 0.6), true, false);
                context.ArcTo(new Point(ActualHeight * 1.2, ActualHeight * 0.6), new Size(ActualHeight / 5, ActualHeight / 5), 180, false, SweepDirection.Clockwise, true, false);
                context.LineTo(new Point(ActualHeight * 2.5, ActualHeight * 0.6), true, false);



                //if (LeftDirection == HslPipeTurnDirection.Left)
                //{
                //    context.BeginFigure( new Point( ActualHeight / 2, ActualHeight ), false, false );
                //    context.ArcTo( new Point( ActualHeight, ActualHeight / 2 ), new Size( ActualHeight / 2, ActualHeight / 2 ), 0, false, SweepDirection.Clockwise, true, false );
                //}
                //else if (LeftDirection == HslPipeTurnDirection.Right)
                //{
                //    context.BeginFigure( new Point( ActualHeight / 2, 0 ), false, false );
                //    context.ArcTo( new Point( ActualHeight, ActualHeight / 2 ), new Size( ActualHeight / 2, ActualHeight / 2 ), 0, false, SweepDirection.Counterclockwise, true, false );
                //}
                //else
                //{
                //    context.BeginFigure( new Point( 0, ActualHeight / 2 ), false, false );
                //    context.LineTo( new Point( ActualHeight, ActualHeight / 2 ), true, false );
                //}






                //context.LineTo( new Point( ActualWidth - ActualHeight, ActualHeight / 2 ), true, false );

                //if (RightDirection == HslPipeTurnDirection.Left)
                //{
                //    context.ArcTo( new Point( ActualWidth - ActualHeight / 2, ActualHeight ), new Size( ActualHeight / 2, ActualHeight / 2 ), 0, false, SweepDirection.Clockwise, true, false );
                //}
                //else if (RightDirection == HslPipeTurnDirection.Right)
                //{
                //    context.ArcTo( new Point( ActualWidth - ActualHeight / 2, 0 ), new Size( ActualHeight / 2, ActualHeight / 2 ), 0, false, SweepDirection.Counterclockwise, true, false );
                //}
                //else
                //{
                //    context.LineTo( new Point( ActualWidth, ActualHeight / 2 ), true, false );
                //}

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
            DependencyProperty.Register("MoveSpeed", typeof(double), typeof(PipeLineKnot), new PropertyMetadata(0.3d, new PropertyChangedCallback(MoveSpeedPropertyChangedCallback)));

        public static void MoveSpeedPropertyChangedCallback(DependencyObject dependency, DependencyPropertyChangedEventArgs e)
        {
            PipeLineKnot pipeLine = (PipeLineKnot)dependency;
            pipeLine.UpdateMoveSpeed();
        }

        private Storyboard storyboard = new Storyboard();

        public void UpdateMoveSpeed()
        {
            if (MoveSpeed > 0)
            {
                offectDoubleAnimation.From = 0d;
                offectDoubleAnimation.To = 10d;
                offectDoubleAnimation.Duration = TimeSpan.FromMilliseconds(300 / MoveSpeed);
                BeginAnimation(LineOffectProperty, offectDoubleAnimation);
            }
            else if (MoveSpeed < 0)
            {
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
            DependencyProperty.Register("CenterColor", typeof(Color), typeof(PipeLineKnot), new PropertyMetadata(Colors.LightGray));

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
            DependencyProperty.Register("PipeLineWidth", typeof(int), typeof(PipeLineKnot), new PropertyMetadata(2));

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
            DependencyProperty.Register("ActiveLineCenterColor", typeof(Color), typeof(PipeLineKnot), new PropertyMetadata(Colors.DodgerBlue));

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
            DependencyProperty.Register("EdgeColor", typeof(Color), typeof(PipeLineKnot), new PropertyMetadata(Colors.DimGray));

        public static readonly DependencyProperty KnotElliColorProperty =
          DependencyProperty.Register("KnotElliColor",
                                      typeof(string),
                                      typeof(PipeLineKnot),
                                      new FrameworkPropertyMetadata("Black"));

        /// <summary>
        /// A property wrapper for the <see cref="BalloonTextProperty"/>
        /// dependency property:<br/>
        /// Description
        /// </summary>
        public string KnotElliColor
        {
            get { return (string)GetValue(KnotElliColorProperty); }
            set { SetValue(KnotElliColorProperty, value); }
        }

        #endregion

        #endregion
    }
}
