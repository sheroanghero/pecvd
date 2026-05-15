using System.Windows;
using System.Windows.Controls;
using Aitex.Common.Util;
using Aitex.Core.UI.MVVM;

namespace MECF.Framework.Simulator.Core.IoProviders
{
    /// <summary>
    /// IoView.xaml 的交互逻辑
    /// </summary>
    public partial class SimulatorIO1View : UserControl
    {
        public SimulatorIO1View()
        {
            InitializeComponent();
            DataContext = new IOViewModel(6731, "System.io1", PathManager.GetCfgDir() + "_ioDefine1.xml");

            this.IsVisibleChanged += IOView_IsVisibleChanged;
        }

        private void IOView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.DataContext == null)
            {
            }
            (DataContext as TimerViewModelBase).EnableTimer(IsVisible);
        }
 
    }

 
}
