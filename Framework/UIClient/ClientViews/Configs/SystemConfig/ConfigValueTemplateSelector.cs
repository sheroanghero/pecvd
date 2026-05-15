using System.Windows;
using System.Windows.Controls;

namespace MECF.Framework.UI.Client.CenterViews.Configs.SystemConfig
{
    public class ConfigValueTemplateSelector : DataTemplateSelector
    {
        public DataTemplate BoolTemplate { get; set; }
        public DataTemplate NumbericTemplate { get; set; }
        public DataTemplate StringTemplate { get; set; }
        public DataTemplate SelectionTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is ConfigItem))
                return null;

            ConfigItem configItem = item as ConfigItem;

            DataTemplate curTemplate = null;
            switch (configItem.Type)
            {
                case DataType.Bool:
                    curTemplate = BoolTemplate;
                    break;
                case DataType.Int:
                case DataType.Double:
                    curTemplate = StringTemplate;  // NumbericTemplate;
                    break;
                case DataType.String:
                    curTemplate = configItem.Tag=="ReadOnlySelection" ? SelectionTemplate: StringTemplate;
                    break;
            }

            return curTemplate;
        }
    }
}
