using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MECF.Framework.UI.Client.ClientViews.Dialogs
{
    /// <summary>
    /// FullKeyboard.xaml 的交互逻辑
    /// </summary>
    public partial class FullKeyboard : Window
    {
        //public FullKeyboard()
        //{
        //    InitializeComponent();
        //}
        private String valueString;

        public int TextIndex { get; set; }
        public TextBox temptb { get; set; }
        public string Type { get; set; }
        public ObservableCollection<int> TempIndexList { get; set; } = new ObservableCollection<int>();
        public PasswordBox tempPasswordtb { get; set; }
        public string textType { get; set; }
        //public int CurrentTextIndex { get; set; }
        private int _currentTextIndex;
        public int CurrentTextIndex
        {
            get => _currentTextIndex;
            set
            {
                _currentTextIndex = value;
                //NotifyOfPropertyChange(nameof(CurrentTextIndex));
            }
        }

        internal String ValueString
        {
            get { return valueString; }
        }

        public FullKeyboard(String inputTitle, String inputvalue, object tbtext = null)
        {
            InitializeComponent();
            FullKeyboardTitle.Text = inputTitle;
            tbValue.Text = inputvalue;
            valueString = inputvalue;
            if (tbtext.GetType() == typeof(TextBox))
            {
                temptb = tbtext as TextBox;
                textType = "TextBox";
            }
            else
            {
                tempPasswordtb = tbtext as PasswordBox;
                textType = "PasswordBox";

            }
        }

        //通过判断按钮的content属性来做对应处理，以简化大量按钮的编程
        private void ButtonGrid_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)e.OriginalSource;    //获取click事件触发源，即按了的按钮
            if ((String)clickedButton.Content == "Clear")
            {
                tbValue.Text = "";
            }
            else if ((String)clickedButton.Content == "DEL")
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
            else if ((String)clickedButton.Content.ToString().ToLower() == "Space".ToLower())
            {
                tbValue.Text += " ";
            }
            else if ((String)clickedButton.Content == "OK")
            {
                if (tbValue.Text == "") { valueString = " "; }
                else
                {
                    valueString = tbValue.Text;
                }
                if (textType == "TextBox")
                    temptb.Text = valueString;
                else
                    tempPasswordtb.Password = valueString;
                this.Close();
            }
            else if ((String)clickedButton.Content == "Cancel")
            {
                this.Close();
            }
            else if ((String)clickedButton.Content == "A/a")
            {
                int count = ButtonGrid.Children.Count;
                for (int i = 0; i < count; i++)//add yujiao 2021/10/25 添加特殊字符
                {
                    Button buttonTemp = ButtonGrid.Children[i] as Button;
                    String contentTemp = buttonTemp.Content as String;
                    var a = contentTemp[0];
                    switch (i)
                    {
                        case 0:
                            buttonTemp.Content = contentTemp[0] > 47 ? "!" : "1";
                            break;
                        case 1:
                            buttonTemp.Content = contentTemp[0] < 57 ? "@" : "2";
                            break;
                        case 2:
                            buttonTemp.Content = contentTemp[0] > 47 ? "#" : "3";
                            break;
                        case 3:
                            buttonTemp.Content = contentTemp[0] > 47 ? "$" : "4";
                            break;
                        case 4:
                            buttonTemp.Content = contentTemp[0] > 47 ? "%" : "5";
                            break;
                        case 5:
                            buttonTemp.Content = contentTemp[0] < 57 ? "^" : "6";
                            break;
                        case 6:
                            buttonTemp.Content = contentTemp[0] > 47 ? "&" : "7";
                            break;
                        case 7:
                            buttonTemp.Content = contentTemp[0] > 47 ? "*" : "8";
                            break;
                        case 8:
                            buttonTemp.Content = contentTemp[0] < 58 ? "_" : "9";
                            break;
                        case 9:
                            buttonTemp.Content = contentTemp[0] > 47 ? "," : "0";
                            break;
                        case 29:
                            buttonTemp.Content = contentTemp[0] < 58 ? ":" : "-";
                            break;
                        case 37:
                            buttonTemp.Content = contentTemp[0] > 82 ? "." : "Space";
                            break;
                            //default:
                    }

                }
                for (int i = 10; i < count - 6; i++)
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

        private void tbValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox text = (TextBox)sender;
            TextIndex = text.CaretIndex;
        }
    }
}
