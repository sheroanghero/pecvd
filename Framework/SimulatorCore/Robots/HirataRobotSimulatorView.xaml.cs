using Aitex.Core.UI.MVVM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Aitex.Core.Utilities;
using MECF.Framework.Simulator.Core.Commons;

namespace MECF.Framework.Simulator.Core.Robots
{
    /// <summary>
    /// HirataRobotSimulatorView.xaml 的交互逻辑
    /// </summary>
    public partial class HirataRobotSimulatorView : UserControl
    {
 
 
        public HirataRobotSimulatorView()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            commonView.Initialize("Robots", "HirataR4", "COM28");

            this.Loaded += HirataRobotSimulatorView_Loaded;
 
        }

        private void HirataRobotSimulatorView_Loaded(object sender, RoutedEventArgs e)
        {

            
        }

 
    }

 

}
