using System.Collections.ObjectModel;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Operations.WaferAssociation
{
    public class SequenceDialogViewModel : DialogViewModel<string>
    {
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
            if (this.currentFileNode != null)
            {
                if (this.currentFileNode.IsFile)
                {
                    this.DialogResult = this.currentFileNode.FullPath;
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

        public ObservableCollection<FileNode> Files { get; set; }
        private FileNode currentFileNode;
    }
}
