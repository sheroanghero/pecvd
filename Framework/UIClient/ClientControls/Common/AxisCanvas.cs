using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MECF.Framework.UI.Client.ClientControls.Common
{
    public class AxisCanvas : Canvas
    {
        private Storyboard currentStoryboard;

        public bool ShowAxisPoint
        {
            get { return (bool)GetValue(ShowAxisPointProperty); }
            set { SetValue(ShowAxisPointProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowAxisPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowAxisPointProperty =
            DependencyProperty.Register("ShowAxisPoint", typeof(bool), typeof(AxisCanvas), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));



        public static readonly DependencyProperty AxisLeftProperty;

        static AxisCanvas()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AxisCanvas), new FrameworkPropertyMetadata(typeof(AxisCanvas)));

            AxisLeftProperty = DependencyProperty.Register("AxisLeft", typeof(int), typeof(AxisCanvas), new UIPropertyMetadata(8));
        }

        public int AxisLeft
        {
            get
            {
                return (int)GetValue(AxisLeftProperty);
            }
            set
            {
                SetValue(AxisLeftProperty, value);
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (ShowAxisPoint)
            {
                dc.DrawEllipse(Brushes.Black, new Pen(Brushes.White, 2), new Point(AxisLeft, Height / 2), 5, 5);
            }

            //	var text = new FormattedText(
            //CurrentAngle.ToString(),
            //CultureInfo.GetCultureInfo("en-us"),
            //FlowDirection.LeftToRight,
            //new Typeface("Verdana"),
            //14,
            //Brushes.Black);
            //	dc.DrawText(text, new Point(AxisLeft, 60));
        }

        public double CurrentAngle
        {
            get
            {
                if (RenderTransform is RotateTransform)
                {
                    return (RenderTransform as RotateTransform).Angle;
                }
                return 0;
            }
        }

        public void Rotate(int angle, bool absolute = true, int ms = 0, double accelerationRatio = 0, double decelerationRatio = 0, Action onComplete = null)
        {
            var storyboard = new Storyboard();
            storyboard.Completed += (s, e) => onComplete?.Invoke();
            if (Rotate(storyboard, angle, absolute, ms, accelerationRatio, decelerationRatio, onComplete))
            {
                storyboard.Begin();
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        public bool Rotate(Storyboard storyboard, int angle, bool absolute = true, int ms = 0, double accelerationRatio = 0, double decelerationRatio = 0, Action onComplete = null)
        {
            var oldAngle = 0d;
            if (RenderTransform is RotateTransform)
            {
                oldAngle = (RenderTransform as RotateTransform).Angle;
            }

            var newAngle = absolute ? angle : oldAngle + angle;

            if (newAngle == oldAngle)
            {
                return false;
            }

            var top = Height / 2;
            var leftPoint = new Point(AxisLeft, top);

            if (ms <= 0)
            {
                RenderTransform = new RotateTransform(newAngle, leftPoint.X, leftPoint.Y);

                return false;
                //InvalidateVisual();
            }
            else
            {
                var rotateTransform = new RotateTransform();
                rotateTransform.CenterX = leftPoint.X;
                rotateTransform.CenterY = leftPoint.Y;
                RenderTransform = rotateTransform;
                var animation = new DoubleAnimation(oldAngle, newAngle, TimeSpan.FromMilliseconds(ms));
                animation.AccelerationRatio = accelerationRatio;
                animation.DecelerationRatio = decelerationRatio;
                storyboard.Children.Add(animation);
                Storyboard.SetTarget(animation, this);
                Storyboard.SetTargetProperty(animation, new PropertyPath("RenderTransform.Angle"));
                return true;
            }
        }

        public void Stop()
        {
            if (currentStoryboard != null)
            {
                currentStoryboard.Stop(this);
                currentStoryboard = null;
            }
        }
    }
}
