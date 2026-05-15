using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Shapes;

namespace MECF.Framework.RT.Core.Backend
{
    /// <summary>
    /// PassWord.xaml 的交互逻辑
    /// </summary>
    public partial class PassWordView : Window
    {
        public bool VerificationResult { get; set; }
        public string Password { get; set; }

        public PassWordView(string password = "")
        {
            InitializeComponent();

            this.Closing += PassWordView_Closing;

            this.Loaded += PassWordView_Loaded;

            this.IsVisibleChanged += PassWordView_IsVisibleChanged;

            Password = password;
        }

        private void PassWordView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
                PasswordBox.Focus();
        }

        private void PassWordView_Loaded(object sender, RoutedEventArgs e)
        {
            PasswordBox.Focus();
        }

        private void PassWordView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void EnsureButton_Click(object sender, RoutedEventArgs e)
        {
            
            if (!string.IsNullOrEmpty(Password)?
                PasswordBox.Password == Password:
                (PasswordBox.Password == "123456" || PasswordBox.Password == "admin"))
            {
                VerificationResult = true;

                Hide();
            }
            else
            {
                PasswordBox.Foreground = new SolidColorBrush(Colors.Red);
                VerificationResult = false;
            }
        }

        public void Reset()
        {
            PasswordBox.Password = "";
            VerificationResult = false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void PasswordBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void PasswordBox_OnTextInput(object sender, TextCompositionEventArgs e)
        {

        }
    }
}



