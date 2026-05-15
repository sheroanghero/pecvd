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
using Aitex.Core.Util;

namespace Aitex.Core.UI.View.Common
{
    /// <summary>
    /// Interaction logic for EventView.xaml
    /// </summary>
    public partial class EventView : UserControl
    {
        public EventView()
        {
            InitializeComponent();

            IsVisibleChanged += new DependencyPropertyChangedEventHandler(MainWindow_IsVisibleChanged);
        }

        void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                var vm = DataContext as EventViewModel;
                if (vm != null)
                {
                    wfTimeFrom.Value = vm.SearchBeginTime;

                    wfTimeTo.Value = vm.SearchEndTime;
                    vm.Preload();
                }
            }
        }

        /// <summary>
        /// Query
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as EventViewModel;
            vm.SearchBeginTime = wfTimeFrom.Value;
            vm.SearchEndTime = wfTimeTo.Value;
            vm.SearchCommand.Execute(this);
        }
    }
}
