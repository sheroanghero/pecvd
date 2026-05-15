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


namespace Aitex.Core.UI.View.Frame
{
    public partial class StandardFrameWindow : Window
    {
        public UserControl TopView { set; private get; }
        public UserControl CenterView { set; private get; }
        public UserControl BottomView { set; private get; }

        public StandardFrameWindow()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(StandardFrameWindow_Loaded);

            IsVisibleChanged += new DependencyPropertyChangedEventHandler(MainWindow_IsVisibleChanged);
        }

        void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }

        void StandardFrameWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (TopView != null)
                TopPanel.Children.Add(TopView);
            if (CenterView != null)
                CenterPanel.Children.Add(CenterView);
            if (BottomView != null)
                BottomPanel.Children.Add(BottomView);
        }
    }
}
