using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Xml;
using Aitex.Core.UI.View.Common;
using Caliburn.Micro.Core;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Sequence
{
    public class FileNode : PropertyChangedBase
    {

        public FileNode()
        {
            this.Files = new ObservableCollection<FileNode>();
            this.IsFile = false;
        }

        private string name = string.Empty;
        public string Name
        {
            get { return name; }
            set { name = value; NotifyOfPropertyChange("Name"); }
        }
        public string FullPath { get; set; }
        public FileNode Parent { get; set; }
        public ObservableCollection<FileNode> Files { get; set; }
        public bool IsFile { get; set; }
        public string PrefixPath { get; set; }
        public bool IsSelected { get; set; }
        public bool IsExpanded { get; set; }
    }
    
    public class RecipeSequenceTreeBuilder
    {
        public static List<FileNode> GetFiles(string prefixPath, List<string> filenames)
        {
            List<FileNode> folders = new List<FileNode>();
            FileNode root = new FileNode() { Name = "Files", IsFile = false, FullPath="", PrefixPath = prefixPath};
            folders.Add(root);

            foreach(string filename in filenames)
            {
                string[] filesplits = filename.Split('\\');

                FileNode parent = root;
                if (filesplits.Length>1)
                {
                    for(var index =0;index< filesplits.Length-1; index++)
                    {
                        bool found = false;
                        for(var j = 0; j < parent.Files.Count; j++)
                        {
                            if(parent.Files[j].Name == filesplits[index] && !parent.Files[j].IsFile)
                            {
                                found = true;
                                parent = parent.Files[j];
                                break;
                            }
                        }
                        if (!found)
                        {
                            FileNode folder = new FileNode() { Name = filesplits[index], IsFile = false, PrefixPath = prefixPath };
                            folder.FullPath = (parent.FullPath == string.Empty ? filesplits[index] : parent.FullPath + "\\" + filesplits[index]);
                            folder.Parent = parent;
                            parent.Files.Add(folder);
                            parent = folder;
                        }
                    }
                }
                FileNode file = new FileNode() { Name = filesplits[filesplits.Length - 1], IsFile = true, PrefixPath = prefixPath };
                file.FullPath = (parent.FullPath == string.Empty ? filesplits[filesplits.Length - 1] : parent.FullPath + "\\" + filesplits[filesplits.Length - 1]);
                file.Parent = parent;
                parent.Files.Add(file);
            }
            return folders;
        }

        public static void CreateTreeViewItems(XmlElement curElementNode, FileNode parent, string prefixPath, string selectionName, bool selectionIsFolder)
        {
            foreach (XmlElement ele in curElementNode.ChildNodes)
            {
                if (ele.Name == "File")
                {
                    string fileName = ele.Attributes["Name"].Value;
                    fileName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
                    string fullPath = string.IsNullOrEmpty(parent.FullPath)
                        ? fileName
                        : parent.FullPath + "\\" + fileName;
                    FileNode file = new FileNode()
                    {
                        Name = fileName,
                        IsFile = true,
                        PrefixPath = prefixPath,
                        Parent = parent,
                        FullPath = fullPath,
                    };
                    if (!selectionIsFolder && selectionName == file.FullPath)
                    {
                        file.IsSelected = true;
                        FileNode node = file.Parent;
                        while (node.FullPath.Contains("\\"))
                        {
                            node.IsExpanded = true;
                            node = node.Parent;
                        }
                    }
                    parent.Files.Add(file);
                }
                else if (ele.Name == "Folder")
                {
                    string folderName = ele.Attributes["Name"].Value;
                    folderName = folderName.Substring(folderName.LastIndexOf('\\') + 1);
                    string fullPath = string.IsNullOrEmpty(parent.FullPath)
                        ? folderName
                        : parent.FullPath + "\\" + folderName;
                    FileNode folder = new FileNode()
                    {
                        Name = folderName,
                        IsFile = false,
                        PrefixPath = prefixPath,
                        Parent = parent,
                        FullPath = fullPath,
                        IsExpanded = !fullPath.Contains("\\"),
                    };
                    parent.Files.Add(folder);
                    if (selectionIsFolder && selectionName == folder.FullPath)
                    {
                        folder.IsSelected = true;
                        FileNode node = folder;
                        while (node.FullPath.Contains("\\"))
                        {
                            node.IsExpanded = true;
                            node = node.Parent;
                        }
                    }
                    CreateTreeViewItems(ele, folder, prefixPath, selectionName, selectionIsFolder);
                }
            }
        }

        public static List<FileNode> BuildFileNode(string prefixPath, string selectionName, bool selectionIsFolder, string fileListInXml)
        {
            List<FileNode> folders = new List<FileNode>();
            FileNode root = new FileNode() { Name = "Files", IsFile = false, FullPath = "", PrefixPath = prefixPath };
            folders.Add(root);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(fileListInXml);
            CreateTreeViewItems(doc.DocumentElement, root, prefixPath, selectionName, selectionIsFolder);
 
            return folders;
        }
     }
}
