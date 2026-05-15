using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MECF.Framework.UI.Client.ClientControls.Common
{
    public class AnimationHelper
    {
        public static void TranslateX(TranslateTransform translation, int targetX, int ms, Action actionComplete = null)
        {
            var time = TimeSpan.FromMilliseconds(ms);
            var oldValue = translation.X;
            var animation = new DoubleAnimation(oldValue, targetX, time);
            animation.Completed += (s, e) =>
            {
                actionComplete?.Invoke();
            };
            translation.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        public static void TranslateX(UIElement uIElement, int startX, int targetX, int ms, Action actionComplete = null)
        {
            var transform = new TranslateTransform();
            uIElement.RenderTransform = transform;

            var time = TimeSpan.FromMilliseconds(ms);
            var animation = new DoubleAnimation(startX, targetX, time);
            animation.AutoReverse = false;
            animation.Completed += (s, e) =>
            {
                actionComplete?.Invoke();
            };
            transform.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        public static void TranslateY(TranslateTransform translation, int targetY, int ms, Action actionComplete = null)
        {
            var time = TimeSpan.FromMilliseconds(ms);
            var animation = new DoubleAnimation(targetY, time);
            animation.Completed += (s, e) =>
            {
                actionComplete?.Invoke();
            };
            translation.BeginAnimation(TranslateTransform.YProperty, animation);
        }

        public static void TranslateYRelative(TranslateTransform translation, int y, int ms, Action actionComplete = null)
        {
            var currentY = translation.Y;

            var time = TimeSpan.FromMilliseconds(ms);
            var animation = new DoubleAnimation(currentY + y, time);
            animation.Completed += (s, e) =>
            {
                actionComplete?.Invoke();
            };
            translation.BeginAnimation(TranslateTransform.YProperty, animation);
        }
    }
}
