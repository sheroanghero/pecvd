using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aitex.Core.UI.Control
{
    /// <summary>
    /// Interaction logic for HandValve.xaml
    /// </summary>
    public partial class HandValve : UserControl
    {
        public HandValve()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty HandValveDirectionProperty = DependencyProperty.Register(
        "HandValveDirection", typeof(ValveDirection), typeof(HandValve),
         new FrameworkPropertyMetadata(ValveDirection.ToBottom, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// direction of the valve
        /// </summary>
        public ValveDirection HandValveDirection
        {
            get
            {
                return (ValveDirection)GetValue(HandValveDirectionProperty);
            }
            set
            {
                SetValue(HandValveDirectionProperty, value);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            try
            {
                base.OnRender(drawingContext);
                switch (HandValveDirection)
                {
                    case ValveDirection.ToBottom:
                        rotateTransform.Angle = 90; 
                        break;
                    case ValveDirection.ToTop:
                        rotateTransform.Angle = -90; 
                        break;
                    case ValveDirection.ToLeft:
                        rotateTransform.Angle = 180; 
                        break;
                    case ValveDirection.ToRight:
                        rotateTransform.Angle = 0; 
                        break; 
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                //LOG.Write(ex);
                throw ex;
            }
        }
    }
}
