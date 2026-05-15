using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OpenSEMI.ClientBase.Command
{
    public sealed class EventCommandTrigger : CommandTrigger
    {
        #region Dependency Properties

        #region RoutedEvent Property

        /// <value>Identifies the RoutedEvent dependency property</value>
        public static readonly DependencyProperty RoutedEventProperty =
            DependencyProperty.Register("RoutedEvent", typeof(RoutedEvent), typeof(EventCommandTrigger),
            new FrameworkPropertyMetadata(null));

        /// <value>description for RoutedEvent property</value>
        public RoutedEvent RoutedEvent
        {
            get { return (RoutedEvent)GetValue(RoutedEventProperty); }
            set { SetValue(RoutedEventProperty, value); }
        }

        #endregion

        #endregion

        protected override Freezable CreateInstanceCore()
        {
            return new EventCommandTrigger();
        }


        protected override void InitializeCore(FrameworkElement source)
        {
            source.AddHandler(RoutedEvent, (RoutedEventHandler)ExecuteCommand);
        }

        private void ExecuteCommand(object sender, RoutedEventArgs args)
        {
            CommandParameter<object> parameter = new EventCommandParameter<object, RoutedEventArgs>(
                CustomParameter, RoutedEvent, args);

            base.ExecuteCommand(parameter);
        }
    }
}
