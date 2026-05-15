using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aitex.Core.UI.DeviceControl
{
    /// <summary>
    /// AITBarcodeTextBox.xaml 的交互逻辑
    /// </summary>
    public partial class AITBarcodeTextBox : UserControl
    {
        public static readonly DependencyProperty BarcodeTextProperty = DependencyProperty.Register(
"BarcodeText", typeof(string), typeof(AITBarcodeTextBox),
new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public string BarcodeText
        {
            get
            {
                return (string)this.GetValue(BarcodeTextProperty);
            }
            set
            {
                this.SetValue(BarcodeTextProperty, value);
            }
        }

                public static readonly DependencyProperty BarcodeLengthProperty = DependencyProperty.Register(
"BarcodeLength", typeof(int), typeof(AITBarcodeTextBox),
new FrameworkPropertyMetadata(6, FrameworkPropertyMetadataOptions.AffectsRender));

        public int BarcodeLength
        {
            get
            {
                return (int)this.GetValue(BarcodeLengthProperty);
            }
            set
            {
                this.SetValue(BarcodeLengthProperty, value);
            }
        }

                public static readonly DependencyProperty MaxScanCountProperty = DependencyProperty.Register(
"MaxScanCount", typeof(int), typeof(AITBarcodeTextBox),
new FrameworkPropertyMetadata(9999, FrameworkPropertyMetadataOptions.AffectsRender));

        public int MaxScanCount
        {
            get
            {
                return (int)this.GetValue(MaxScanCountProperty);
            }
            set
            {
                this.SetValue(MaxScanCountProperty, value);
            }
        }



                        public static readonly DependencyProperty BarcodeInputChangedCommandProperty = DependencyProperty.Register(
                        "BarcodeInputChangedCommand", typeof(ICommand), typeof(AITBarcodeTextBox),
                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
 

        public ICommand BarcodeInputChangedCommand
        {
            get
            {
                return (ICommand)this.GetValue(BarcodeInputChangedCommandProperty);
            }
            set
            {
                this.SetValue(BarcodeInputChangedCommandProperty, value);
            }
        }


        //private string _preInput;

        public AITBarcodeTextBox()
        {
            InitializeComponent();
        }

        private void UIElement_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            return true;
            //Regex regex = new Regex("[^0-9.]+"); //regex that matches disallowed text
            //return !regex.IsMatch(text);
        }

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox control = (TextBox)sender;

            string value = control.Text;
            string origin = value;
            if (value.Contains(Environment.NewLine))
            {
                //int pos = value.IndexOf("/");

                //string newInput = value.Substring(value.IndexOf("/") + 1, value.IndexOf(Environment.NewLine));


                 origin = value.Replace(Environment.NewLine, "");
                if (!origin.EndsWith("/"))
                    origin = origin + "/";


                if (!string.IsNullOrEmpty(origin))
                {
                    var elements = origin.Split('/');
                    //if (elements.Length > 0)
                    //    ;
                }


                
            }

            control.Text = origin;
            control.CaretIndex = control.Text.Length;

            if (BarcodeInputChangedCommand != null )
            {
                BarcodeInputChangedCommand.Execute(origin);
            }

            //string origin = value.Replace("\r\n", "/");
          //System.Diagnostics.Trace.WriteLine(value);

            //string origin = value.Replace("/", "");

            //while (origin.Length > MaxScanCount * BarcodeLength)
            //{
            //    origin = origin.Substring(BarcodeLength);
            //}

            //if (MaxScanCount > 1 && origin.Length > BarcodeLength)
            //{
            //    string split = "";
            //    while (origin.Length > BarcodeLength)
            //    {
            //        split += origin.Substring(0, BarcodeLength) + "/";
            //        origin = origin.Substring(BarcodeLength);
            //    }
            //    split += origin;

            //    origin = split;
            //}







        }
    }
}
