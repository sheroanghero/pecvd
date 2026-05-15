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
    /// Interaction logic for GasFlowButton.xaml
    /// </summary>
    public partial class GasFlowButton : UserControl
    {
        public GasFlowButton()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty Flow2RightProperty = DependencyProperty.Register(
            "Flow2Right", typeof(bool), typeof(GasFlowButton),
            new FrameworkPropertyMetadata(true,FrameworkPropertyMetadataOptions.AffectsRender));

        public bool Flow2Right
        {
            get { return (bool)this.GetValue(Flow2RightProperty); }
            set { this.SetValue(Flow2RightProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (!Flow2Right)
            {
                Canvas.SetLeft(btnFlow, 54);
                Canvas.SetLeft(btnGas, 103);
                Canvas.SetLeft(btnCarrierGas, 182);
            }
           
        }
    }
}
