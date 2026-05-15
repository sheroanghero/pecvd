using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MECF.Framework.UI.Core.CommonControl
{
    /// <summary>
    /// AITSCRow.xaml 的交互逻辑
    /// </summary>
    public partial class AITScStringRow : UserControl
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(ICommand), typeof(AITScStringRow),
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
            "NameWidth", typeof(int), typeof(AITScStringRow),
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
            "FeedbackWidth", typeof(int), typeof(AITScStringRow),
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
            "SetPointWidth", typeof(int), typeof(AITScStringRow),
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
            "CommandWidth", typeof(int), typeof(AITScStringRow),
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
            "ScId", typeof(string), typeof(AITScStringRow),
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
            "ScName", typeof(string), typeof(AITScStringRow),
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
            "ScFeedback", typeof(string), typeof(AITScStringRow),
            new FrameworkPropertyMetadata("127.0.0.1", FrameworkPropertyMetadataOptions.AffectsRender));
        public string ScFeedback
        {
            get
            {
                return (string)this.GetValue(ScFeedbackProperty);
            }
            set
            {
                this.SetValue(ScFeedbackProperty, value);
            }
        }

        public static readonly DependencyProperty ScSetPointProperty = DependencyProperty.Register(
            "ScSetPoint", typeof(string), typeof(AITScStringRow),
            new FrameworkPropertyMetadata("127.0.0.1", FrameworkPropertyMetadataOptions.AffectsRender));
        public string ScSetPoint
        {
            get
            {
                return (string)this.GetValue(ScSetPointProperty);
            }
            set
            {
                this.SetValue(ScSetPointProperty, value);
            }
        }


        public AITScStringRow()
        {
            InitializeComponent();
        }
 
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            RectangleSetPoint.Fill = ScSetPoint == ScFeedback
                ? new SolidColorBrush(Color.FromRgb(0x95, 0xB3, 0xD7))
                : new SolidColorBrush(Colors.Yellow);
            
            InputSetPoint.Background = ScSetPoint == ScFeedback
                ? new SolidColorBrush(Color.FromRgb(0xDC, 0xF7, 0xF6)) 
                : new SolidColorBrush(Colors.Yellow);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Command.Execute(new object[] { ScId, ScSetPoint.ToString()});
        }
    }
}
