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

namespace Aitex.Core.UI.Dialog
{
    /// <summary>
    /// Interaction logic for RecipeAlarmedDialog.xaml
    /// </summary>
    public partial class RecipeAlarmedDialog : Window
    {
        public RecipeAlarmedDialog()
        {
            InitializeComponent();
 
            BannerColor = Brushes.Red;
        }
 
        public string MessageTitle
        {
            get
            {
                return _msgTitle.Text;
            }
            set
            {
                _msgTitle.Text = value;
            }
        }
 
        public string MessageContent
        {
            get
            {
                return _msgContent.Text;
            }
            set
            {
                _msgContent.Text = value;
            }
        }
 
        public Brush BannerColor
        {
            get
            {
                return topGrid.Background;
            }
            set
            {
                topGrid.Background = value;
            }
        }

        public Action ResumeRecipe;
        public Action AbortRecipe;


        private void buttonContinue_Click(object sender, RoutedEventArgs e)
        {
            if (ResumeRecipe != null)
                ResumeRecipe();
            //Close();

            Hide();
        }

        private void buttonAbort_Click(object sender, RoutedEventArgs e)
        {
            if (AbortRecipe != null)
                AbortRecipe();
            Hide();
        }
    }
}
