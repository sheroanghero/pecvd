using System.Collections.ObjectModel;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    public class RecipeSelectDialogViewModel : DialogViewModel<string>
    {
        public ObservableCollection<ProcessTypeFileItem> ProcessTypeFileList { get; set; }

        public FileNode CurrentFileNode { get; set; }

        public int ProcessTypeIndexSelection { get; set; }

        public ObservableCollection<FileNode> Files { get; set; }
        private FileNode currentFileNode;

        public void TreeSelectChanged(FileNode file)
        {
            this.currentFileNode = file;
        }

        public void TreeMouseDoubleClick(FileNode file)
        {
            this.currentFileNode = file;
            OK();
        }

        public void OK()
        {
            if (this.currentFileNode!=null)
            {
                if (this.currentFileNode.IsFile)
                {
                    this.DialogResult = currentFileNode.PrefixPath + "\\" + currentFileNode.FullPath;
                    IsCancel = false;
                    TryClose(true);
                }
                else
                {
                    DialogBox.ShowWarning("Recipe copy to this folder path");
                    this.DialogResult = currentFileNode.PrefixPath + "\\"+ currentFileNode.FullPath;
                    IsCancel = false;
                    TryClose(true);
                }
            }
        }

        public void Cancel()
        {
            IsCancel = true;
            TryClose(false);
        }

    }
}
