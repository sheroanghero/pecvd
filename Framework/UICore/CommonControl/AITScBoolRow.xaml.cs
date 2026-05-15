using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MECF.Framework.UI.Core.CommonControl
{
    /// <summary>
    /// AITScBoolRow.xaml 的交互逻辑
    /// </summary>
    public partial class AITScBoolRow : UserControl
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(ICommand), typeof(AITScBoolRow),
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
            "NameWidth", typeof(int), typeof(AITScBoolRow),
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
            "FeedbackWidth", typeof(int), typeof(AITScBoolRow),
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
            "SetPointWidth", typeof(int), typeof(AITScBoolRow),
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
            "CommandWidth", typeof(int), typeof(AITScBoolRow),
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
            "ScId", typeof(string), typeof(AITScBoolRow),
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
            "ScName", typeof(string), typeof(AITScBoolRow),
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
            "ScFeedback", typeof(bool), typeof(AITScBoolRow),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
        public bool ScFeedback
        {
            get
            {
                return (bool)this.GetValue(ScFeedbackProperty);
            }
            set
            {
                this.SetValue(ScFeedbackProperty, value);
            }
        }

        public static readonly DependencyProperty ScSetPointProperty = DependencyProperty.Register(
            "ScSetPoint", typeof(bool), typeof(AITScBoolRow),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
        public bool ScSetPoint
        {
            get
            {
                return (bool)this.GetValue(ScSetPointProperty);
            }
            set
            {
                this.SetValue(ScSetPointProperty, value);
            }
        }
 
        public AITScBoolRow()
        {
            InitializeComponent();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            RectangleSetPoint.Fill = ScSetPoint == ScFeedback
                ? new SolidColorBrush(Color.FromRgb(0x95, 0xB3, 0xD7))
                : new SolidColorBrush(Colors.Yellow);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Command.Execute(new object[] { ScId, ScSetPoint.ToString() });
        }
    }
}