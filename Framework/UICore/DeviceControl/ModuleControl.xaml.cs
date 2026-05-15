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
using Aitex.Core.Common.DeviceData;
using Aitex.Core.UI.DeviceControl;

namespace MECF.Framework.UI.Core.DeviceControl
{
    /// <summary>
    /// ModuleControl.xaml 的交互逻辑
    /// </summary>
    public partial class ModuleControl : UserControl
    {
        public static readonly DependencyProperty ModuleNameProperty = DependencyProperty.Register(
            "ModuleName", typeof(string), typeof(ModuleControl),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

        public string ModuleName
        {
            get
            {
                return (string)this.GetValue(ModuleNameProperty);
            }
            set
            {
                this.SetValue(ModuleNameProperty, value);
            }
        }

        public bool HasWafer
        {
            get { return (bool)GetValue(HasWaferProperty); }
            set { SetValue(HasWaferProperty, value); }
        }

 
        public static readonly DependencyProperty HasWaferProperty =
            DependencyProperty.Register("HasWafer", typeof(bool), typeof(ModuleControl), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public ModuleControl()
        {
            InitializeComponent();
        }
    }
}
