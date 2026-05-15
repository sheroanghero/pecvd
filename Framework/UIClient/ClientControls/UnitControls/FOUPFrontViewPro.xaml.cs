using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MECF.Framework.UI.Client.ClientBase;

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{


    /// <summary>
    /// Interaction logic for FOUPFrontViewPro.xaml
    /// </summary>
    public partial class FOUPFrontViewPro : UserControl
    {

     


        public FOUPFrontViewPro()
        {
            InitializeComponent();
           
        }


        #region UnitData (DependencyProperty)
        public ModuleInfo UnitData
        {
            get { return (ModuleInfo)GetValue(UnitDataProperty); }
            set { SetValue(UnitDataProperty, value); }
        }
        public static readonly DependencyProperty UnitDataProperty =
            DependencyProperty.Register("UnitData", typeof(ModuleInfo), typeof(FOUPFrontViewPro), new UIPropertyMetadata(null));

        public bool ShowTitle
        {
            get { return (bool)GetValue(ShowTitleProperty); }
            set { SetValue(ShowTitleProperty, value); }
        }
        public static readonly DependencyProperty ShowTitleProperty =
            DependencyProperty.Register("ShowTitle", typeof(bool), typeof(FOUPFrontViewPro), new UIPropertyMetadata(true));
        #endregion      
    }
}
