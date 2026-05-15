using System.Windows;
using System.Windows.Controls;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;

namespace MECF.Framework.UI.Client.CenterViews.Editors
{
    public class FolderOrFileTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FolderTemplate
        {
            get;
            set;
        }


        public DataTemplate FileTemplate
        {
            get;
            set;
        }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FileNode file = (FileNode)item;
            if (!file.IsFile)
                return FolderTemplate;
            else 
                return FileTemplate;
        }
    }
}
