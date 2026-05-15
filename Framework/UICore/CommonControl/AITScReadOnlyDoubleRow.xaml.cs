using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MECF.Framework.UI.Core.CommonControl
{
    /// <summary>
    /// AITScReadOnlyDoubleRow.xaml 的交互逻辑
    /// </summary>
    public partial class AITScReadOnlyDoubleRow : UserControl
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(ICommand), typeof(AITScReadOnlyDoubleRow),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
        public ICommand Command
        {
            get
            {
                return (ICommand)this.GetValue(CommandProperty);
            }
            set
            {
                this.SetValue(CommandProperty, value);
            }
        }



        public static readonly DependencyProperty NameWidthProperty = DependencyProperty.Register(
            "NameWidth", typeof(int), typeof(AITScReadOnlyDoubleRow),
            new FrameworkPropertyMetadata(180, FrameworkPropertyMetadataOptions.AffectsRender));
        public int NameWidth
        {
            get
            {
                return (int)this.GetValue(NameWidthProperty);
            }
            set
            {
                this.SetValue(NameWidthProperty, value);
            }
        }

        public static readonly DependencyProperty FeedbackWidthProperty = DependencyProperty.Register(
            "FeedbackWidth", typeof(int), typeof(AITScReadOnlyDoubleRow),
            new FrameworkPropertyMetadata(90, FrameworkPropertyMetadataOptions.AffectsRender));
        public int FeedbackWidth
        {
            get
            {
                return (int)this.GetValue(FeedbackWidthProperty);
            }
            set
            {
                this.SetValue(FeedbackWidthProperty, value);
            }
        }

        public static readonly DependencyProperty SetPointWidthProperty = DependencyProperty.Register(
            "SetPointWidth", typeof(int), typeof(AITScReadOnlyDoubleRow),
            new FrameworkPropertyMetadata(90, FrameworkPropertyMetadataOptions.AffectsRender));
        public int SetPointWidth
        {
            get
            {
                return (int)this.GetValue(SetPointWidthProperty);
            }
            set
            {
                this.SetValue(SetPointWidthProperty, value);
            }
        }

        public static readonly DependencyProperty CommandWidthProperty = DependencyProperty.Register(
            "CommandWidth", typeof(int), typeof(AITScReadOnlyDoubleRow),
            new FrameworkPropertyMetadata(90, FrameworkPropertyMetadataOptions.AffectsRender));
        public int CommandWidth
        {
            get
            {
                return (int)this.GetValue(CommandWidthProperty);
            }
            set
            {
                this.SetValue(CommandWidthProperty, value);
            }
        }

        public static readonly DependencyProperty ScIdProperty = DependencyProperty.Register(
            "ScId", typeof(string), typeof(AITScReadOnlyDoubleRow),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public string ScId
        {
            get
            {
                return (string)this.GetValue(ScIdProperty);
            }
            set
            {
                this.SetValue(ScIdProperty, value);
            }
        }

        public static readonly DependencyProperty ScNameProperty = DependencyProperty.Register(
            "ScName", typeof(string), typeof(AITScReadOnlyDoubleRow),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
        public string ScName
        {
            get
            {
                return (string)this.GetValue(ScNameProperty);
            }
            set
            {
                this.SetValue(ScNameProperty, value);
            }
        }

        public static readonly DependencyProperty ScFeedbackProperty = DependencyProperty.Register(
            "ScFeedback", typeof(double), typeof(AITScReadOnlyDoubleRow),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public double ScFeedback
        {
            get
            {
                return (double)this.GetValue(ScFeedbackProperty);
            }
            set
            {
                this.SetValue(ScFeedbackProperty, value);
            }
        }

        public static readonly DependencyProperty ScSetPointProperty = DependencyProperty.Register(
            "ScSetPoint", typeof(double), typeof(AITScReadOnlyDoubleRow),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
        public double ScSetPoint
        {
            get
            {
                return (double)this.GetValue(ScSetPointProperty);
            }
            set
            {
                this.SetValue(ScSetPointProperty, value);
            }
        }


        public AITScReadOnlyDoubleRow()
        {
            InitializeComponent();
        }
        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
             
        }
    }
}
