using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Aitex.Core.UI.View.Common
{
    public class TreeViewFileItem : TreeViewItem
    {
        public TreeViewFileItem(string fileName)
        {
            FileName = fileName;

            Image image = new Image();
            image.Stretch = Stretch.Fill;
            image.Source = new BitmapImage(new Uri(@"/MECF.Framework.Common;component/Resources/RecipeFile.png", UriKind.Relative));

            TextBlock txtBNode = new TextBlock();
            int lastIndex = FileName.LastIndexOf('\\');
            if (lastIndex >= 0)
                txtBNode.Text = fileName.Substring(lastIndex + 1);
            else
                txtBNode.Text = fileName;

            StackPanel panel = new StackPanel();
            panel.Children.Add(image);
            panel.Children.Add(txtBNode);

            panel.Orientation = Orientation.Horizontal;
            this.Header = panel;
        }

        public string FileName
        {
            get;
            set;
        }
    }

    public class TreeViewFolderItem : TreeViewItem
    {
        public string FolderName
        {
            set;
            get;
        }

        public TreeViewFolderItem(string folderName)
        {
            FolderName = folderName;

            Image image = new Image();
            image.Stretch = Stretch.Fill;
            image.Source = new BitmapImage(new Uri(@"/MECF.Framework.Common;component/Resources/RecipeFolder.png", UriKind.Relative));

            TextBlock txtBNode = new TextBlock();
            txtBNode.Text = folderName;

            StackPanel panel = new StackPanel();
            panel.Children.Add(image);
            panel.Children.Add(txtBNode);

            panel.Orientation = Orientation.Horizontal;
            this.Header = panel;
        }
    }
}
