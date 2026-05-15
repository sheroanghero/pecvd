using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using Caliburn.Micro.Core;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.RecipeCenter;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.Command;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Sequence
{
    public class SequenceViewModel : BaseModel
    {
        public bool IsPermission { get => this.Permission == 3; }//&& RtStatus != "AutoRunning";
        SequenceView u;
        public ObservableCollection<FileNode> Files { get; set; }
        protected override void OnInitialize()
        {
            base.OnInitialize();
            this.CurrentSequence = new SequenceData();
            this.Columns = this.columnBuilder.Build();

            this.editMode = EditMode.None;
            this.IsSavedDesc = true;

            List<string> names = this.provider.GetSequences();
            this.Files = new ObservableCollection<FileNode>(RecipeSequenceTreeBuilder.GetFiles("", names));
            this.CurrentFileNode = this.Files[0];

            this.SelectDefault(this.CurrentFileNode);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            if (this.IsChanged)
            {
                if (MessageBox.Show("This sequence is changed,do you want to save it?", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    this.SaveSequence();
                this.LoadData(this.CurrentSequence.Name);
            }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            SequenceColumnBuilder.ApplyTemplate((UserControl)view, this.Columns);
            u = (SequenceView)view;

            this.Columns.Apply((c) =>
            {
                c.Header = c;
                u.dgCustom.Columns.Add(c);
            });

            u.dgCustom.ItemsSource = CurrentSequence.Steps;

            this.UpdateView();
        }

        #region Sequence selection
        public bool EnableSaveTo { get; set; }
        public bool EnableFreeze { get; set; }
        public bool IsFreeze { get; set; }
        public void TreeSelectChanged(FileNode file)
        {
            if (file != null && file.IsFile)
            {
                if (this.IsChanged)
                    if (MessageBox.Show("This sequence is changed,do you want to save it?", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        this.Save(this.CurrentSequence);
                this.LoadData(file.FullPath);
                this.CurrentFileNode = file;
            }
            else
            {
                this.ClearData();
                this.editMode = EditMode.None;
                this.CurrentSequence.Steps.Clear();
            }
            if (CurrentSequence != null)
            {
                //this.CurrentRecipe.EnableFreeze = !this.CurrentRecipe.Freeze && BaseApp.Instance.UserContext.LoginName == this.CurrentRecipe.Revisor;
                //this.CurrentRecipe.EnableNotFreeze = this.CurrentRecipe.Freeze && BaseApp.Instance.UserContext.LoginName == this.CurrentRecipe.Revisor;
                this.CurrentSequence.EnableFreeze = !this.CurrentSequence.Freeze;
                this.CurrentSequence.EnableNotFreeze = this.CurrentSequence.Freeze;


                IsFreeze = this.CurrentSequence.Freeze;

            }
            else
            {
                IsFreeze = false;
            }
            this.UpdateView();
        }

        private TreeViewItem GetParentObjectEx<TreeViewItem>(DependencyObject obj) where TreeViewItem : FrameworkElement
        {
            if (!EnableStep|| obj.ToString() == "System.Windows.Documents.Run")
                return null;
            DependencyObject parent = VisualTreeHelper.GetParent(obj);
            while (parent != null)
            {
                if (parent is TreeViewItem)
                {
                    return (TreeViewItem)parent;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
        public void TreeRightMouseDown(MouseButtonEventArgs e)
        {
            var item = GetParentObjectEx<TreeViewItem>(e.OriginalSource as DependencyObject) as TreeViewItem;
            if (item != null)
            {
                item.Focus();
            }
        }

        #endregion

        private ICommand _NotImplementCommand;
        public ICommand NotImplementCommand
        {
            get
            {
                if (this._NotImplementCommand == null)
                    this._NotImplementCommand = new BaseCommand(() => this.NotImplement());
                return this._NotImplementCommand;
            }
        }

        public void NotImplement()
        {
            MessageBox.Show("Not Implement");
        }

        #region Sequence operation

        private ICommand _NewFolderCommand;
        public ICommand NewFolderCommand
        {
            get
            {
                if (this._NewFolderCommand == null)
                    this._NewFolderCommand = new BaseCommand(() => this.NewFolder());
                return this._NewFolderCommand;
            }
        }
        public void NewFolder()
        {
            if (!this.CurrentFileNode.IsFile)
            {
                InputFileNameDialogViewModel dialog = new InputFileNameDialogViewModel("Input Folder Name");
                WindowManager wm = new WindowManager();
                bool? bret = wm.ShowDialog(dialog);
                if (!(bool)bret)
                    return;
                string fullpath = (this.CurrentFileNode.FullPath == "" ? dialog.DialogResult : this.CurrentFileNode.FullPath + "\\" + dialog.DialogResult);
                if (this.provider.CreateSequenceFolder(fullpath))
                {
                    FileNode folder = new FileNode();
                    folder.Name = dialog.DialogResult;
                    folder.FullPath = fullpath;
                    folder.Parent = this.CurrentFileNode;
                    this.CurrentFileNode.Files.Add(folder);
                }
                else
                    MessageBox.Show("Create folder failed!");
            }
        }

        private ICommand _NewFolderInParentCommand;
        public ICommand NewFolderInParentCommand
        {
            get
            {
                if (this._NewFolderInParentCommand == null)
                    this._NewFolderInParentCommand = new BaseCommand(() => this.NewFolderInParent());
                return this._NewFolderInParentCommand;
            }
        }
        public void NewFolderInParent()
        {
            InputFileNameDialogViewModel dialog = new InputFileNameDialogViewModel("Input Folder Name");
            WindowManager wm = new WindowManager();
            bool? bret = wm.ShowDialog(dialog);
            if (!(bool)bret)
                return;
            string fullpath = dialog.DialogResult;
            if (this.provider.CreateSequenceFolder(fullpath))
            {
                FileNode folder = new FileNode();
                folder.Name = dialog.DialogResult;
                folder.FullPath = fullpath;
                folder.Parent = this.Files[0];
                this.Files[0].Files.Add(folder);
            }
            else
                MessageBox.Show("Create folder failed!");
        }

        private ICommand _NewSequenceInParentCommand;
        public ICommand NewSequenceInParentCommand
        {
            get
            {
                if (this._NewSequenceInParentCommand == null)
                    this._NewSequenceInParentCommand = new BaseCommand(() => this.NewSequenceInParent());
                return this._NewSequenceInParentCommand;
            }
        }
        public void FreezeClick()
        {
            this.CurrentSequence.Freeze = true;
            this.Save(this.CurrentSequence);
        }
        public void UnFreezeClick()
        {
            this.CurrentSequence.Freeze = false;
            this.Save(this.CurrentSequence);
        }
        public void NewSequenceInParent()
        {
            InputFileNameDialogViewModel dialog = new InputFileNameDialogViewModel("Input New Sequence Name");
            WindowManager wm = new WindowManager();
            bool? bret = wm.ShowDialog(dialog);
            if ((bool)bret)
            {
                if (this.IsChanged)
                {
                    if (MessageBox.Show("This sequence is changed,do you want to save it?", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        this.CurrentSequence.Revisor = BaseApp.Instance.UserContext.LoginName;
                        this.CurrentSequence.ReviseTime = DateTime.Now;
                        this.Save(this.CurrentSequence);
                    }
                }
                else
                {
                    string fullpath = dialog.DialogResult;
                    if (string.IsNullOrEmpty(fullpath))
                    {
                        MessageBox.Show("SequenceName cannot be null or empty!");
                        return;
                    }
                    if (this.IsExist(fullpath))
                    {
                        MessageBox.Show("Sequence already existed!");
                    }
                    else
                    {
                        SequenceData sequence = new SequenceData();
                        sequence.Name = fullpath;
                        sequence.Creator = BaseApp.Instance.UserContext.LoginName;
                        sequence.CreateTime = DateTime.Now;
                        sequence.Revisor = BaseApp.Instance.UserContext.LoginName;
                        sequence.ReviseTime = DateTime.Now;
                        sequence.Description = string.Empty;
                        sequence.Freeze = false;
                        if (this.Save(sequence))
                        {
                            this.CurrentSequence.Name = sequence.Name;
                            this.CurrentSequence.Creator = sequence.Creator;
                            this.CurrentSequence.CreateTime = sequence.CreateTime;
                            this.CurrentSequence.Revisor = sequence.Revisor;
                            this.CurrentSequence.ReviseTime = sequence.ReviseTime;
                            this.CurrentSequence.Description = sequence.Description;
                            this.CurrentSequence.Steps.Clear();

                            FileNode file = new FileNode();
                            file.Name = dialog.DialogResult;
                            file.FullPath = this.CurrentSequence.Name;
                            file.IsFile = true;
                            file.Parent = this.Files[0];
                            this.Files[0].Files.Insert(this.findInsertPosition(this.Files[0].Files,file), file);
                            this.editMode = EditMode.Normal;
                            this.UpdateView();
                        }
                    }
                }
            }
        }

        public int findInsertPosition(ObservableCollection<FileNode> files, FileNode file)
        {
            int pos = -1;
            if (files.Count == 0)
                pos = 0;
            else
            {
                bool foundfolder = false;
                for (var index = 0; index < files.Count; index++)
                {
                    if (!files[index].IsFile)
                    {
                        foundfolder = true;
                        pos = index;
                        break;
                    }
                }

                for (var index = 0; index < files.Count; index++)
                {
                    if (string.Compare(files[index].Name , file.Name)  == 1)
                    {
                        foundfolder = true;
                        pos = index;
                        break;
                    }
                }

                if (!foundfolder)
                    pos = files.Count;
            }
            return pos;
        }

        private ICommand _SaveAsCommand;
        public ICommand SaveAsCommand
        {
            get
            {
                if (this._SaveAsCommand == null)
                    this._SaveAsCommand = new BaseCommand(() => this.SaveAsSequence());
                return this._SaveAsCommand;
            }
        }

        public void SaveAsSequence()
        {
            if (this.CurrentFileNode.IsFile)
            {
                InputFileNameDialogViewModel dialog = new InputFileNameDialogViewModel("SaveAs Sequence");
                WindowManager wm = new WindowManager();
                bool? bret = wm.ShowDialog(dialog);
                if (!(bool)bret)
                    return;
                string fullpath = (this.CurrentFileNode.Parent.FullPath == "" ? dialog.DialogResult : this.CurrentFileNode.Parent.FullPath + "\\" + dialog.DialogResult);

                if (string.IsNullOrEmpty(fullpath))
                {
                    MessageBox.Show("SequenceName cannot be null or empty!");
                    return;
                }
                if (this.IsExist(fullpath))
                {
                    MessageBox.Show("Sequence already existed!");
                }
                else
                {
                    if (this.IsChanged)
                    {
                        if (MessageBox.Show("This sequence is changed,do you want to save it?", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        {
                            this.Save(this.CurrentSequence);
                        }
                    }

                    this.CurrentSequence.Revisor = BaseApp.Instance.UserContext.LoginName;
                    this.CurrentSequence.ReviseTime = DateTime.Now;
                    string tempname = this.CurrentSequence.Name;
                    this.CurrentSequence.Name = fullpath;
                    if (this.provider.SaveAs(fullpath, this.CurrentSequence))
                    {
                       
                        FileNode node = new FileNode();
                        node.Parent = this.CurrentFileNode.Parent;
                        node.Name = dialog.DialogResult;
                        node.FullPath = fullpath;
                        node.IsFile = true;
                        this.CurrentFileNode.Parent.Files.Insert(this.findInsertPosition(this.CurrentFileNode.Parent.Files, node), node);
                        //this.CurrentFileNode.Parent.Files.Add(node);
                    }
                    else
                    {
                        this.CurrentSequence.Name = tempname;
                        MessageBox.Show("SaveAs failed!");
                    }
                }
            }
        }
        private ICommand _DeleteFolderCommand;
        public ICommand DeleteFolderCommand
        {
            get
            {
                if (this._DeleteFolderCommand == null)
                    this._DeleteFolderCommand = new BaseCommand(() => this.DeleteFolder());
                return this._DeleteFolderCommand;
            }
        }
        public void DeleteFolder()
        {
            if (!this.CurrentFileNode.IsFile && this.CurrentFileNode.FullPath != "")
            {
                if (DialogBox.Confirm("Do you want to delete this folder? this operation will delete all files in this folder."))
                {
                    if (this.provider.DeleteSequenceFolder(this.CurrentFileNode.FullPath))
                    {
                        this.CurrentFileNode.Parent.Files.Remove(this.CurrentFileNode);
                    }
                    else
                        DialogBox.ShowInfo("Delete folder failed!");
                }
            }
        }

        private ICommand _NewSequenceCommand;
        public ICommand NewSequenceCommand
        {
            get
            {
                if (this._NewSequenceCommand == null)
                    this._NewSequenceCommand = new BaseCommand(() => this.NewSequence());
                return this._NewSequenceCommand;
            }
        }
        public void NewSequence()
        {
            InputFileNameDialogViewModel dialog = new InputFileNameDialogViewModel("Input New Sequence Name");
            WindowManager wm = new WindowManager();
            bool? bret = wm.ShowDialog(dialog);
            if ((bool)bret)
            {
                if (this.IsChanged)
                {
                    if (MessageBox.Show("This sequence is changed,do you want to save it?", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        this.CurrentSequence.Revisor = BaseApp.Instance.UserContext.LoginName;
                        this.CurrentSequence.ReviseTime = DateTime.Now;
                        this.Save(this.CurrentSequence);
                    }
                }
                else
                {
                    string fullpath = dialog.DialogResult;
                    if (this.CurrentFileNode.IsFile)
                        fullpath = (string.IsNullOrEmpty(this.CurrentFileNode.Parent.FullPath) ? dialog.DialogResult : this.CurrentFileNode.Parent.FullPath + "\\" + dialog.DialogResult);
                    else
                        fullpath = (string.IsNullOrEmpty(this.CurrentFileNode.FullPath) ? dialog.DialogResult : this.CurrentFileNode.FullPath + "\\" + dialog.DialogResult);

                    if (string.IsNullOrEmpty(fullpath))
                    {
                        MessageBox.Show("SequenceName cannot be null or empty!");
                        return;
                    }
                    if (this.IsExist(fullpath))
                    {
                        MessageBox.Show("Sequence already existed!");
                    }
                    else
                    {
                        SequenceData sequence = new SequenceData();
                        sequence.Name = fullpath;
                        sequence.Creator = BaseApp.Instance.UserContext.LoginName;
                        sequence.CreateTime = DateTime.Now;
                        sequence.Revisor = BaseApp.Instance.UserContext.LoginName;
                        sequence.ReviseTime = DateTime.Now;
                        sequence.Description = string.Empty;

                        if (this.Save(sequence))
                        {
                            this.CurrentSequence.Name = sequence.Name;
                            this.CurrentSequence.Creator = sequence.Creator;
                            this.CurrentSequence.CreateTime = sequence.CreateTime;
                            this.CurrentSequence.Revisor = sequence.Revisor;
                            this.CurrentSequence.ReviseTime = sequence.ReviseTime;
                            this.CurrentSequence.Description = sequence.Description;
                            this.CurrentSequence.Steps.Clear();

                            FileNode file = new FileNode();
                            file.Name = dialog.DialogResult;
                            file.FullPath = this.CurrentSequence.Name;
                            file.IsFile = true;
                            file.Parent = this.CurrentFileNode.IsFile ? this.CurrentFileNode.Parent : this.CurrentFileNode;
                            if (this.CurrentFileNode.IsFile)
                                this.CurrentFileNode.Parent.Files.Insert(this.findInsertPosition(this.CurrentFileNode.Parent.Files,file), file);
                            else
                                this.CurrentFileNode.Files.Insert(this.findInsertPosition(this.CurrentFileNode.Files,file), file);
                            this.editMode = EditMode.Normal;
                            this.UpdateView();
                        }
                    }
                }
            }
        }

        private ICommand _RenameCommand;
        public ICommand RenameCommand
        {
            get
            {
                if (this._RenameCommand == null)
                    this._RenameCommand = new BaseCommand(() => this.RenameSequence());
                return this._RenameCommand;
            }
        }
        public void RenameSequence()
        {
            if (this.CurrentFileNode.IsFile)
            {
                InputFileNameDialogViewModel dialog = new InputFileNameDialogViewModel("Rename Sequence");
                WindowManager wm = new WindowManager();
                dialog.FileName = this.CurrentFileNode.Name;
                bool? bret = wm.ShowDialog(dialog);
                if (!(bool)bret)
                    return;
                string fullpath = this.CurrentFileNode.Parent.FullPath == "" ? dialog.DialogResult : this.CurrentFileNode.Parent.FullPath + "\\" + dialog.DialogResult;

                if (string.IsNullOrEmpty(fullpath))
                {
                    MessageBox.Show("SequenceName cannot be null or empty!");
                    return;
                }
                if (this.IsExist(fullpath))
                {
                    MessageBox.Show("Sequence already existed!");
                }
                else
                {
                    if (this.IsChanged)
                    {
                        if (MessageBox.Show("This sequence is changed,do you want to save it?", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        {
                            this.Save(this.CurrentSequence);
                        }
                    }
                    this.CurrentSequence.Revisor = BaseApp.Instance.UserContext.LoginName;
                    this.CurrentSequence.ReviseTime = DateTime.Now;
                    if (this.provider.Rename(this.CurrentSequence.Name, fullpath))
                    {
                        this.CurrentFileNode.Name = dialog.DialogResult;
                        this.CurrentFileNode.FullPath = fullpath;
                        this.CurrentSequence.Name = fullpath;

                        FileNode file = new FileNode();
                        file.Name = this.CurrentSequence.Name;
                        file.FullPath = fullpath;
                        file.IsFile = true;
                        file.Parent = this.CurrentFileNode.IsFile ? this.CurrentFileNode.Parent : this.CurrentFileNode;
                        if (this.CurrentFileNode.IsFile)
                        {
                            this.CurrentFileNode.Parent.Files.Remove(CurrentFileNode);
                            this.CurrentFileNode.Parent.Files.Insert(this.findInsertPosition(this.CurrentFileNode.Parent.Files,file), file);
                        }
                        else
                            this.CurrentFileNode.Files.Insert(this.findInsertPosition(this.CurrentFileNode.Files,file), file);
                    }
                    else
                        MessageBox.Show("Rename failed!");
                }
            }
        }

        public void SaveSequence()
        {
            if (this.IsChanged)
            {
                this.Save(this.CurrentSequence);
            }
        }

        private ICommand _DeleteCommand;
        public ICommand DeleteSequenceCommand
        {
            get
            {
                if (this._DeleteCommand == null)
                    this._DeleteCommand = new BaseCommand(() => this.DeleteSequence());
                return this._DeleteCommand;
            }
        }
        public void DeleteSequence()
        {
            if (this.CurrentFileNode.IsFile)
            {
                if (DialogBox.Confirm("Do you want to delete this sequence?"))
                {
                    if (this.provider.Delete(this.CurrentSequence.Name))
                    {
                        this.CurrentFileNode.Parent.Files.Remove(this.CurrentFileNode);
                    }
                }
            }
        }

        public void ReloadSequence()
        {
            if (this.editMode == EditMode.Normal || this.editMode == EditMode.Edit)
            {
                this.LoadData(this.CurrentSequence.Name);
                this.UpdateView();
            }
        }

        private bool Save(SequenceData seq)
        {
            bool result = false;
            if (string.IsNullOrEmpty(seq.Name))
            {
                MessageBox.Show("Sequence name can't be empty");
                return false;
            }

            string ruleCheckMessage = null;
            if (!ValidateSequenceRules(seq, ref ruleCheckMessage))
            {
                MessageBox.Show(string.Format($"{seq.Name} rules check failed, don't allow to save: {ruleCheckMessage}"));
                return false;
            }

            seq.Revisor = BaseApp.Instance.UserContext.LoginName;
            seq.ReviseTime = DateTime.Now;
            result = this.provider.Save(seq);

            if (result)
            {
                foreach (ObservableCollection<Param> parameters in seq.Steps)
                {
                    parameters.Apply(param => param.IsSaved = true);
                }
                this.editMode = EditMode.Normal;
                this.IsSavedDesc = true;
                this.NotifyOfPropertyChange("IsSavedDesc");
                this.CurrentSequence.EnableFreeze = !this.CurrentSequence.Freeze;
                this.CurrentSequence.EnableNotFreeze = this.CurrentSequence.Freeze;
                this.UpdateView();
            }
            else
                MessageBox.Show("Save failed!");
            return result;
        }

        private bool IsExist(string sequencename)
        {
            bool existed = false;
            FileNode filenode = this.CurrentFileNode.IsFile ? this.CurrentFileNode.Parent : this.CurrentFileNode;
            for (var index = 0; index < filenode.Files.Count; index++)
            {
                if (filenode.Files[index].FullPath.ToLower() == sequencename.ToLower())
                {
                    existed = true;
                    break;
                }
            }
            return existed;
        }

        private bool ValidateSequenceRules(SequenceData currentSequence, ref string ruleCheckMessage)
        {
            foreach (var stepInfo in currentSequence.Steps)
            {
                if (string.IsNullOrEmpty((stepInfo[1] as PositionParam).Value))
                {
                    ruleCheckMessage = $"Step{(stepInfo[0] as StepParam).Value}, Position Is Empty";

                    return false;
                }

                //if (((stepInfo[4] as PathFileParam).Visible == Visibility.Visible && string.IsNullOrEmpty((stepInfo[4] as PathFileParam).Value)) ||
                //    ((stepInfo[5] as PathFileParam).Visible == Visibility.Visible && string.IsNullOrEmpty((stepInfo[5] as PathFileParam).Value)))
                //{
                //    ruleCheckMessage = $"Step{(stepInfo[0] as StepParam).Value}, Recipe Is Empty";

                //    return false;
                //}
            }
            return true;
        }

        #endregion

        #region Steps operation

        public void AddStep()
        {
            this.CurrentSequence.Steps.Add(CurrentSequence.CreateStep(this.Columns));

            if (this.editMode != EditMode.New && this.editMode != EditMode.ReName)
                this.editMode = EditMode.Edit;

            int index = 1;
            foreach (ObservableCollection<Param> parameters in this.CurrentSequence.Steps)
            {
                (parameters[0] as StepParam).Value = index.ToString();
                index++;
            }
            this.UpdateView();
        }

        public void AppendStep()
        {
            int index = -1;
            bool found = false;
            for (var i = 0; i < this.CurrentSequence.Steps.Count; i++)
            {
                if (this.CurrentSequence.Steps[i][0] is StepParam && ((StepParam)this.CurrentSequence.Steps[i][0]).Checked)
                {
                    index = i;
                    found = true;
                    break;
                }
            }
            if (found)
            {
                if (this.editMode != EditMode.New && this.editMode != EditMode.ReName)
                    this.editMode = EditMode.Edit;

                this.CurrentSequence.Steps.Insert(index, this.CurrentSequence.CreateStep(this.Columns));
                index = 1;
                foreach (ObservableCollection<Param> parameters in this.CurrentSequence.Steps)
                {
                    (parameters[0] as StepParam).Value = index.ToString();
                    index++;
                }
            }
        }

        private ObservableCollection<ObservableCollection<Param>> copySteps = new ObservableCollection<ObservableCollection<Param>>();
        public void CopyStep()
        {
            this.copySteps.Clear();

            for (var i = 0; i < this.CurrentSequence.Steps.Count; i++)
            {
                if (this.CurrentSequence.Steps[i][0] is StepParam && ((StepParam)this.CurrentSequence.Steps[i][0]).Checked)
                {
                    this.copySteps.Add(this.CurrentSequence.CloneStep(this.Columns, this.CurrentSequence.Steps[i]));
                }
            }
        }

        public void PasteStep()
        {
            if (this.copySteps.Count > 0)
            {
                if (this.editMode != EditMode.New && this.editMode != EditMode.ReName)
                    this.editMode = EditMode.Edit;
                for (var i = 0; i < this.CurrentSequence.Steps.Count; i++)
                {
                    if (this.CurrentSequence.Steps[i][0] is StepParam && ((StepParam)this.CurrentSequence.Steps[i][0]).Checked)
                    {
                        for (var copyindex = 0; copyindex < this.copySteps.Count; copyindex++)
                        {
                            this.CurrentSequence.Steps.Insert(i, this.CurrentSequence.CloneStep(this.Columns, this.copySteps[copyindex]));
                            i++;
                        }
                        break;
                    }
                }
                int index = 1;
                foreach (ObservableCollection<Param> parameters in this.CurrentSequence.Steps)
                {
                    (parameters[0] as StepParam).Value = index.ToString();
                    index++;
                }
                this.UpdateView();
            }
        }

        public void DeleteStep()
        {
            if (this.editMode != EditMode.New && this.editMode != EditMode.ReName)
                this.editMode = EditMode.Edit;

            List<ObservableCollection<Param>> steps = this.CurrentSequence.Steps.ToList();

            for (var i = 0; i < steps.Count; i++)
            {
                if (steps[i][0] is StepParam && ((StepParam)steps[i][0]).Checked)
                {
                    this.CurrentSequence.Steps.Remove(steps[i]);
                }
            }
            int index = 1;
            foreach (ObservableCollection<Param> parameters in this.CurrentSequence.Steps)
            {
                (parameters[0] as StepParam).Value = index.ToString();
                index++;
            }
        }

        public void FullScreen()
        {
            RecipeFullScreenEditor recipeFullScreen = new RecipeFullScreenEditor();
            var count = u.StepsGrid.Children.Count;
            u.FullScreenButton.Visibility = Visibility.Hidden;
            for (int i = 0; i < count; i++)
            {
                UIElement item = u.StepsGrid.Children[0];
                u.StepsGrid.Children.Remove(item);
                recipeFullScreen.RootGrid.Children.Add(item);
            }
            if ((bool)recipeFullScreen.ShowDialog())
            {
                u.FullScreenButton.Visibility = Visibility.Visible;
                count = recipeFullScreen.RootGrid.Children.Count;
                for (int i = 0; i < count; i++)
                {
                    UIElement item = recipeFullScreen.RootGrid.Children[0];
                    recipeFullScreen.RootGrid.Children.Remove(item);
                    u.StepsGrid.Children.Add(item);
                }
            }
        }

        public void SelectRecipe(PathFileParam param)
        {
            RecipeSequenceSelectDialogViewModel dialog = new RecipeSequenceSelectDialogViewModel();
            dialog.DisplayName = "Select Recipe";
            ObservableCollection<Param> parameters = param.Parent;
            PositionParam posParam = null;
            for (var index = 0; index < parameters.Count; index++)
            {
                if (parameters[index] is PositionParam)
                {
                    posParam = parameters[index] as PositionParam;
                    break;
                }
            }

            string _chamberName = string.Empty;
            var supportedChamberType = (string)QueryDataClient.Instance.Service.GetConfig($"System.Recipe.SupportedChamberType");

            if (supportedChamberType.Contains(posParam.Value))
            {
                _chamberName = param.Name.Substring(0, 3);
            }

            List<string> lstFiles = RecipeClient.Instance.Service.GetRecipesByPath($"{_chamberName}\\{param.PrefixPath}", false).ToList();

            dialog.Files = new ObservableCollection<FileNode>(RecipeSequenceTreeBuilder.GetFiles("", lstFiles));

            WindowManager wm = new WindowManager();
            bool? bret = wm.ShowDialog(dialog);
            if ((bool)bret)
            {
                param.Value = $"{_chamberName}\\{param.PrefixPath}\\" + dialog.DialogResult;

                string path = param.Value;
                int index = path.LastIndexOf("\\");
                if (index > -1)
                {
                    param.FileName = path.Substring(index + 1);
                }
                else
                {
                    param.FileName = path;
                }

                param.IsSaved = false;
            }
        }

        #endregion

        private void SelectDefault(FileNode node)
        {
            if (!node.IsFile)
            {
                if (node.Files.Count > 0)
                {
                    foreach (FileNode file in node.Files)
                    {
                        if (file.IsFile)
                        {
                            this.TreeSelectChanged(file);
                            //break;
                        }
                    }
                }
            }
        }

        private void LoadData(string newSeqName)
        {
            SequenceData Sequence = this.provider.GetSequenceByName(this.Columns, newSeqName);

            this.CurrentSequence.Name = Sequence.Name;
            this.CurrentSequence.Creator = Sequence.Creator;
            this.CurrentSequence.CreateTime = Sequence.CreateTime;
            this.CurrentSequence.Revisor = Sequence.Revisor;
            this.CurrentSequence.ReviseTime = Sequence.ReviseTime;
            this.CurrentSequence.Description = Sequence.Description;
            this.CurrentSequence.Freeze = Sequence.Freeze;

            this.CurrentSequence.Steps.Clear();
            Sequence.Steps.ToList().ForEach(step =>
                this.CurrentSequence.Steps.Add(step));
            
            this.CurrentSequence.EnableFreeze = !this.CurrentSequence.Freeze;
            this.CurrentSequence.EnableNotFreeze = this.CurrentSequence.Freeze;
            //int index = 1;
            //foreach (ObservableCollection<Param> parameters in this.CurrentSequence.Steps)
            //{
            //    (parameters[0] as StepParam).Value = index.ToString();
            //    index++;

            //    foreach (var para in parameters)
            //    {
            //        var pathFile = para as PathFileParam;
            //        if (pathFile != null)
            //        {
            //            pathFile.Value = pathFile.Value.Replace($"{pathFile.PrefixPath}\\", "");
            //        }
            //    }
            //}

            this.IsSavedDesc = true;
            this.NotifyOfPropertyChange("IsSavedDesc");

            this.editMode = EditMode.Normal;
        }

        private void ClearData()
        {
            this.editMode = EditMode.None;
            this.CurrentSequence.Steps.Clear();
            this.CurrentSequence.Name = string.Empty;
            this.CurrentSequence.Description = string.Empty;

            this.IsSavedDesc = true;

            this.NotifyOfPropertyChange("IsSavedGroup");
            this.NotifyOfPropertyChange("IsSavedDesc");
        }

        public void UpdateView()
        {
            this.EnableSequenceName = false;
            this.NotifyOfPropertyChange("EnableSequenceName");

            this.EnableNew = true && !this.CurrentSequence.Freeze;
            this.EnableReName = true && !this.CurrentSequence.Freeze;
            this.EnableCopy = true && !this.CurrentSequence.Freeze;
            this.EnableDelete = true && !this.CurrentSequence.Freeze;
            this.EnableSave = true && !this.CurrentSequence.Freeze;
            this.EnableStep = IsPermission && !this.CurrentSequence.Freeze;

            this.NotifyOfPropertyChange("EnableNew");
            this.NotifyOfPropertyChange("EnableReName");
            this.NotifyOfPropertyChange("EnableCopy");
            this.NotifyOfPropertyChange("EnableDelete");
            this.NotifyOfPropertyChange("EnableSave");
            this.NotifyOfPropertyChange("EnableStep");

            if (this.editMode == EditMode.None)
            {
                this.EnableNew = true;
                this.EnableReName = false;
                this.EnableCopy = false;
                this.EnableDelete = false;
                this.EnableStep = false;
                this.EnableSave = false;

                this.NotifyOfPropertyChange("EnableNew");
                this.NotifyOfPropertyChange("EnableReName");
                this.NotifyOfPropertyChange("EnableCopy");
                this.NotifyOfPropertyChange("EnableDelete");
                this.NotifyOfPropertyChange("EnableSave");
                this.NotifyOfPropertyChange("EnableStep");
            }
        }

        private bool IsChanged
        {
            get
            {
                bool changed = false;
                if (!this.IsSavedDesc || this.editMode == EditMode.Edit)
                    changed = true;
                else
                {
                    foreach (ObservableCollection<Param> parameters in this.CurrentSequence.Steps)
                    {
                        if (parameters.Where(param => param.IsSaved == false && param.Name != "Step").Count() > 0)
                        {
                            changed = true;
                            break;
                        }
                    }
                }
                return changed;
            }
        }

        public bool EnableNew { get; set; }
        public bool EnableReName { get; set; }
        public bool EnableCopy { get; set; }
        public bool EnableDelete { get; set; }
        public bool EnableSave { get; set; }
        public bool EnableStep { get; set; }
        public bool IsSavedDesc { get; set; }

       
        public FileNode CurrentFileNode { get; private set; }
        public SequenceData CurrentSequence { get; private set; }
        public ObservableCollection<EditorDataGridTemplateColumnBase> Columns { get; set; }

        public bool EnableSequenceName { get; set; }

        private string selectedSequenceName = string.Empty;
        private string SequenceNameBeforeRename = string.Empty;
        private string lastSequenceName = string.Empty;

        private SequenceColumnBuilder columnBuilder = new SequenceColumnBuilder();
        private EditMode editMode;
        private SequenceDataProvider provider = new SequenceDataProvider();
    }
}
