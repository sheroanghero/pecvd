using MECF.Framework.UI.Client.ClientBase;
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

namespace MECF.Framework.UI.Client.Ctrlib.UnitControls
{
    /// <summary>
    /// FOUPFrontViewBig.xaml 的交互逻辑
    /// </summary>
    public partial class FOUPFrontViewBig : UserControl
    {




        public FOUPFrontViewBig()
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
            DependencyProperty.Register("UnitData", typeof(ModuleInfo), typeof(FOUPFrontViewBig), new UIPropertyMetadata(null));

        public bool ShowTitle
        {
            get { return (bool)GetValue(ShowTitleProperty); }
            set { SetValue(ShowTitleProperty, value); }
        }
        public static readonly DependencyProperty ShowTitleProperty =
            DependencyProperty.Register("ShowTitle", typeof(bool), typeof(FOUPFrontViewBig), new UIPropertyMetadata(true));
        #endregion      
    }
}
