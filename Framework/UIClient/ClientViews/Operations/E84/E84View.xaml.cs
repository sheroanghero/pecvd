using System.Windows;
using System.Windows.Controls;

namespace MECF.Framework.UI.Client.CenterViews.Operations.E84
{
    public partial class E84View : UserControl
    {
        public E84ViewModel _e84ViewModel;

        public E84View()
        {
            InitializeComponent();
            
            _e84ViewModel = new E84ViewModel();
            DataContext = _e84ViewModel;

            IsVisibleChanged += E84View_IsVisibleChanged;
        }

        private void E84View_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            (DataContext as E84ViewModel)?.EnableTimer(IsVisible);
        }
    }
}