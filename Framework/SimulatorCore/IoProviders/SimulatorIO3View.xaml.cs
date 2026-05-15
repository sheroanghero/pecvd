using System.Windows;
using System.Windows.Controls;
using Aitex.Common.Util;
using Aitex.Core.UI.MVVM;

namespace MECF.Framework.Simulator.Core.IoProviders
{
    /// <summary>
    /// IoView.xaml 的交互逻辑
    /// </summary>
    public partial class SimulatorIO3View : UserControl
    {
        public SimulatorIO3View()
        {
            InitializeComponent();
            DataContext = new IOViewModel(6733, "System.io3", PathManager.GetCfgDir() + "_ioDefine3.xml");

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
