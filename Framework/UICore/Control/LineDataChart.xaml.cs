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
using SciChart.Charting.Visuals.Axes;
using Aitex.Core.UI.View.Smart;
using Aitex.Core.UI.ControlDataContext;

namespace Aitex.Core.UI.Control
{
    /// <summary>
    /// Interaction logic for PocketProcessDataChart.xaml
    /// </summary>
    public partial class LineDataChart : UserControl
    {
        public LineDataChart()
        {
            InitializeComponent();
        }

        private void sciChart_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                //右键双击，恢复1:1的视图
                this.sciChart.ZoomExtents();
            }
        }

        private void NumericAxis_Pyro_MouseEnter(object sender, MouseEventArgs e)
        {
            pyroAxisTopDown.Visibility = System.Windows.Visibility.Visible;
            pyroAxisTopUp.Visibility = System.Windows.Visibility.Visible;
            pyroAxisBottomUp.Visibility = System.Windows.Visibility.Visible;
            pyroAxisBottomDown.Visibility = System.Windows.Visibility.Visible;
        }

        private void NumericAxis_Pyro_MouseLeave(object sender, MouseEventArgs e)
        {
            if (pyroAxisTopDown.IsMouseOver || pyroAxisTopUp.IsMouseOver ||
                pyroAxisBottomDown.IsMouseOver || pyroAxisBottomUp.IsMouseOver)
                return;
            pyroAxisTopDown.Visibility = System.Windows.Visibility.Hidden;
            pyroAxisTopUp.Visibility = System.Windows.Visibility.Hidden;
            pyroAxisBottomDown.Visibility = System.Windows.Visibility.Hidden;
            pyroAxisBottomUp.Visibility = System.Windows.Visibility.Hidden;
        }

        private void NumericAxis_Wrap_MouseEnter(object sender, MouseEventArgs e)
        {
            warpAxisTopDown.Visibility = System.Windows.Visibility.Visible;
            warpAxisTopUp.Visibility = System.Windows.Visibility.Visible;
            warpAxisBottomUp.Visibility = System.Windows.Visibility.Visible;
            warpAxisBottomDown.Visibility = System.Windows.Visibility.Visible;
        }

        private void NumericAxis_Wrap_MouseLeave(object sender, MouseEventArgs e)
        {
            if (warpAxisTopDown.IsMouseOver || warpAxisTopUp.IsMouseOver ||
                warpAxisBottomDown.IsMouseOver || warpAxisBottomUp.IsMouseOver)
                return;
            warpAxisTopDown.Visibility = System.Windows.Visibility.Hidden;
            warpAxisTopUp.Visibility = System.Windows.Visibility.Hidden;
            warpAxisBottomDown.Visibility = System.Windows.Visibility.Hidden;
            warpAxisBottomUp.Visibility = System.Windows.Visibility.Hidden;
        }


        private void NumericAxis_Reflect_MouseEnter(object sender, MouseEventArgs e)
        {
            ReflectAxisTopDown.Visibility = System.Windows.Visibility.Visible;
            ReflectAxisTopUp.Visibility = System.Windows.Visibility.Visible;
            ReflectAxisBottomUp.Visibility = System.Windows.Visibility.Visible;
            ReflectAxisBottomDown.Visibility = System.Windows.Visibility.Visible;
        }

        private void NumericAxis_Reflect_MouseLeave(object sender, MouseEventArgs e)
        {
            if (ReflectAxisTopDown.IsMouseOver || ReflectAxisTopUp.IsMouseOver ||
                ReflectAxisBottomDown.IsMouseOver || ReflectAxisBottomUp.IsMouseOver)
                return;
            ReflectAxisTopDown.Visibility = System.Windows.Visibility.Hidden;
            ReflectAxisTopUp.Visibility = System.Windows.Visibility.Hidden;
            ReflectAxisBottomDown.Visibility = System.Windows.Visibility.Hidden;
            ReflectAxisBottomUp.Visibility = System.Windows.Visibility.Hidden;
        }

        private void YAxisTopUp_Click(object sender, MouseButtonEventArgs e)
        {
            var l = sender as Label;
            NumericAxis axis = sciChart.YAxes[1 + int.Parse(l.Tag.ToString())] as NumericAxis;
            double min = (double)axis.VisibleRange.Min;
            double max = (double)axis.VisibleRange.Max;
            axis.VisibleRange.Max = max + 0.1 * (max - min);
        }

        private void YAxisTopDown_Click(object sender, MouseButtonEventArgs e)
        {
            var l = sender as Label;
            NumericAxis axis = sciChart.YAxes[1 + int.Parse(l.Tag.ToString())] as NumericAxis;
            double min = (double)axis.VisibleRange.Min;
            double max = (double)axis.VisibleRange.Max;
            axis.VisibleRange.Max = max - 0.1 * (max - min);
        }

        private void YAxisBottomUp_Click(object sender, MouseButtonEventArgs e)
        {
            var l = sender as Label;
            NumericAxis axis = sciChart.YAxes[1 + int.Parse(l.Tag.ToString())] as NumericAxis;
            double min = (double)axis.VisibleRange.Min;
            double max = (double)axis.VisibleRange.Max;
            axis.VisibleRange.Min = min + 0.1 * (max - min);
        }

        private void YAxisBottomDown_Click(object sender, MouseButtonEventArgs e)
        {
            var l = sender as Label;
            NumericAxis axis = sciChart.YAxes[1 + int.Parse(l.Tag.ToString())] as NumericAxis;
            double min = (double)axis.VisibleRange.Min;
            double max = (double)axis.VisibleRange.Max;
            axis.VisibleRange.Min = min - 0.1 * (max - min);
        }

        private void checkAutoRange_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox ck = sender as CheckBox;
            AutoRange value = ck.IsChecked.Value ? AutoRange.Always : AutoRange.Never;

            sciChart.XAxis.AutoRange = value;
            foreach (var yaxis in sciChart.YAxes)
                yaxis.AutoRange = value;
        }

        private void OnChangeLineColor(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            if (btn != null)
            {
                string dataId = (string)btn.Tag;
                var dlg = new System.Windows.Forms.ColorDialog();
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var newColor = new System.Windows.Media.Color() { A = dlg.Color.A, B = dlg.Color.B, G = dlg.Color.G, R = dlg.Color.R };
                    var vm = (ProcessDataChartDataItem)DataContext;
                    var line = vm.RenderableSeries.ToList().Find((o) => ((SmartDataLine)o).UniqueId == dataId);
                    if (line != null) line.Stroke = newColor;
                }
            }
        }

        private void OnChangeDrawingItemVisibility(object sender, RoutedEventArgs e)
        {
            var checkbox = (CheckBox)sender;
            if (checkbox != null && checkbox.IsChecked.HasValue)
            {
                string dataId = (string)checkbox.Tag;
                var vm = (ProcessDataChartDataItem)DataContext;
                var line = vm.RenderableSeries.ToList().Find((o) => ((SmartDataLine)o).UniqueId == dataId);
                if (line != null)
                {
                    line.IsVisible = checkbox.IsChecked.Value;
                    //vm.InvokePropertyChanged("ReflectionDataSeries");
                }
            }
        }

        private void checkConfigPanel_Checked(object sender, RoutedEventArgs e)
        {
            dataConfigPanelColumn.Width = new GridLength((sender as CheckBox).IsChecked.Value ? 300 : 0, GridUnitType.Pixel);

        }
    }
}