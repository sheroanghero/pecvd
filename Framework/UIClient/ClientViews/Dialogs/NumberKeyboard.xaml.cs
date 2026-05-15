using OpenSEMI.ClientBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace MECF.Framework.UI.Client.ClientViews.Dialogs
{
    /// <summary>
    /// NumberKeyboard.xaml 的交互逻辑
    /// </summary>
    public partial class NumberKeyboard : Window
    {
        //public FullKeyboard()
        //{
        //    InitializeComponent();
        //}
        private NumberKeyboard view;
        private String valueString;
        public int TextIndex { get; set; }
        public string Type { get; set; }
        public ObservableCollection<int> TempIndexList { get; set; } = new ObservableCollection<int>();

        public string result { get; set; }
        public TextBox temptb { get; set; }

        public String ValueString
        {
            get
            {
                ValueHandler(ref valueString);
                if (valueString == "")
                { return "0"; }
                else
                {
                    return valueString;
                }
            }
        }


        public NumberKeyboard(String inputTitle, String inputvalue, object tbtext = null)
        {
            InitializeComponent();
            ValueHandler(ref inputvalue);
            tbValue.Text = inputvalue;
            valueString = inputvalue;
            temptb = tbtext as TextBox;
        }
        private void ValueHandler(ref string inputvalue)
        {
            if (!string.IsNullOrEmpty(inputvalue))
            {
                if (Regex.Matches(inputvalue, ":").Count == 0 && inputvalue.Length > 1)
                {
                    if (inputvalue.Split('.').Length <= 1)
                        inputvalue = inputvalue.TrimStart('0');
                }

            }
        }
        //通过判断按钮的content属性来做对应处理，以简化大量按钮的编程
        private void ButtonGrid_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)e.OriginalSource;    //获取click事件触发源，即按了的按钮
            if ((String)clickedButton.Content == "DEL")
            {
                Type = "DEL";
                if (tbValue.Text.Length > 0)
                {
                    if (TextIndex == 0 && tbValue.Text.Length >= 1)
                    {
                        tbValue.Text = tbValue.Text.Remove(tbValue.Text.Length - 1, 1);
                    }
                    else if (TextIndex > 0)
                    {
                        tbValue.Text = tbValue.Text.Remove(TextIndex - 1, 1);
                    }
                }
            }
            else if ((String)clickedButton.Content == "Clear")
            {
                tbValue.Text = "";
            }
            else if ((String)clickedButton.Content == "Cancel")
            {
                this.Close();
            }
            else if ((String)clickedButton.Content == "OK")
            {
                valueString = tbValue.Text;
                if (Regex.Matches(valueString, ":").Count > 0)
                {
                    var value = Regex.IsMatch(valueString, "[0-9][0-9]:[0-9][0-9]:[0-9][0-9]");
                    if (!value)
                    {
                        DialogBox.ShowWarning("输入格式不正确");
                        return;
                    }
                }
                temptb.Text = valueString;
                this.Close();
            }
            else if ((String)clickedButton.Content == "-")
            {
                if (tbValue.Text == "")
                {
                    tbValue.Text += (String)clickedButton.Content;
                }
                else
                {
                    if (tbValue.Text.Substring(0, 1) == "-")
                    {
                        tbValue.Text = tbValue.Text.Substring(1, tbValue.Text.Length - 1);
                    }
                    else
                    {
                        tbValue.Text = $"-{tbValue.Text }";
                    }
                }
            }
            else if ((String)clickedButton.Content == "A/a")
            {
                int count = ButtonGrid.Children.Count;
                for (int i = 10; i < count - 4; i++)
                {
                    Button buttonTemp = ButtonGrid.Children[i] as Button;
                    String contentTemp = buttonTemp.Content as String;
                    buttonTemp.Content = contentTemp[0] > 90 ? contentTemp.ToUpper() : contentTemp.ToLower();
                }
            }
            else
            {
                Type = "Normal";
                if (TextIndex == 0)
                    tbValue.Text += (String)clickedButton.Content;
                else
                    tbValue.Text = tbValue.Text.Insert(TextIndex, (String)clickedButton.Content);
            }
        }

        private void tbValue_SelectionChanged(object sender, RoutedEventArgs e)
        {
            TextBox text = (TextBox)sender;
            TextIndex = text.CaretIndex;
            if (TextIndex > 0)
                TempIndexList.Clear();
            if (TempIndexList.Count == 0 && TextIndex != 0)
            {
                TempIndexList.Add(TextIndex);
            }
            else if (TempIndexList.Count > 0 && TextIndex == 0)
            {
                if (Type == "DEL")
                {
                    TextIndex = TempIndexList[0] - 1 < 0 ? 0 : TempIndexList[0] - 1;
                    TempIndexList[0] = TempIndexList[0] - 1 < 0 ? 0 : TempIndexList[0] - 1;
                }
                else
                {
                    TextIndex = TempIndexList[0] + 1;
                    TempIndexList[0] = TempIndexList[0] + 1;
                }
            }
        }
    }
}
