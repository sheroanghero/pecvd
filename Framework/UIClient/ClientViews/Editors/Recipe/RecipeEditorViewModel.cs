using Aitex.Core.RT.Log;
using Caliburn.Micro;
using Caliburn.Micro.Core;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.Command;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    public class ProcessTypeFileItem : NotifiableItem
    {
        public string ProcessType { get; set; }
        public ObservableCollection<FileNode> FileListByProcessType { get; set; }

        public ProcessTypeFileItem()
        {
            FileListByProcessType = new ObservableCollection<FileNode>();
        }
    }

    public class ChamberTypeItem : NotifiableItem
    {
        public string ChamberType { get; set; }
        public ObservableCollection<ProcessTypeFileItem> FileListByChamberType { get; set; }

        public ChamberTypeItem()
        {
            FileListByChamberType = new ObservableCollection<ProcessTypeFileItem>();
        }
    }

    public class RecipeEditorViewModel : BaseModel
    {
        //public bool IsPermission { get => this.Permission == 3 && !IsFreeze; }//&& RtStatus != "AutoRunning";
        public bool IsPermission { get => this.Permission == 3; }
        private ICommand _RenameFolderCommand;
        public ICommand RenameFolderCommand
        {
            get
            {
                if (this._RenameFolderCommand == null)
                    this._RenameFolderCommand = new BaseCommand(() => this.RenameFolder());
                return this._RenameFolderCommand;
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
        private ICommand _NewFolderRootCommand;
        public ICommand NewFolderRootCommand
        {
            get
            {
                if (this._NewFolderRootCommand == null)
                    this._NewFolderRootCommand = new BaseCommand(() => this.NewFolderRoot());
                return this._NewFolderRootCommand;
            }
        }

        private ICommand _NewRecipeCommand;
        public ICommand NewRecipeCommand
        {
            get
            {
                if (this._NewRecipeCommand == null)
                    this._NewRecipeCommand = new BaseCommand(() => this.NewRecipe());
                return this._NewRecipeCommand;
            }
        }
        private ICommand _CopyRecipeCommand;
        public ICommand CopyRecipeCommand
        {
            get
            {
                if (this._CopyRecipeCommand == null)
                    this._CopyRecipeCommand = new BaseCommand(() => this.CopyAsRecipe());
                return this._CopyRecipeCommand;
            }
        }
        private ICommand _NewRecipeRootCommand;
        public ICommand NewRecipeRootCommand
        {
            get
            {
                if (this._NewRecipeRootCommand == null)
                    this._NewRecipeRootCommand = new BaseCommand(() => this.NewRecipeRoot());
                return this._NewRecipeRootCommand;
            }
        }
        private ICommand _RenameRecipeCommand;
        public ICommand RenameRecipeCommand
        {
            get
            {
                if (this._RenameRecipeCommand == null)
                    this._RenameRecipeCommand = new BaseCommand(() => this.RenameRecipe());
                return this._RenameRecipeCommand;
            }
        }
        private ICommand _DeleteRecipeCommand;
        public ICommand DeleteRecipeCommand
        {
            get
            {
                if (this._DeleteRecipeCommand == null)
                    this._DeleteRecipeCommand = new BaseCommand(() => this.DeleteRecipe());
                return this._DeleteRecipeCommand;
            }
        }
        private ICommand _SaveAsRecipeCommand;
        public ICommand SaveAsRecipeCommand
        {
            get
            {
                if (this._SaveAsRecipeCommand == null)
                    this._SaveAsRecipeCommand = new BaseCommand(() => this.SaveAsRecipe());
                return this._SaveAsRecipeCommand;
            }
        }


        public ObservableCollection<ProcessTypeFileItem> ProcessTypeFileList { get; set; }

        public RecipeData CurrentRecipe { get; private set; }

        public FileNode CurrentFileNode { get; set; }

        private bool IsChanged
        {
            get
            {
                return editMode == EditMode.Edit || CurrentRecipe.IsChanged;
            }
        }

        public ObservableCollection<EditorDataGridTemplateColumnBase> Columns { get; set; }
        public Dictionary<string, ObservableCollection<Param>> PopSettingColumns { get; set; }
        public ObservableCollection<EditorDataGridTemplateColumnBase> ColumnsTolerance { get; set; }

        public bool EnableNew { get; set; }
        public bool EnableReName { get; set; }
        public bool EnableCopy { get; set; }
        public bool EnableDelete { get; set; }
        public bool EnableSave { get; set; }
        public bool EnableStep { get; set; }
        public bool EnableReload { get; set; }
        public bool EnableSaveToAll { get; set; }
        public bool EnableSaveTo { get; set; }
        public bool EnableFreeze { get; set; }
        public bool IsFreeze { get; set; }
        public bool DgvEnable { get; set; }
        RecipeEditorView u;

        private RecipeFormatBuilder _columnBuilder = new RecipeFormatBuilder();

        private EditMode editMode;

        private RecipeProvider _recipeProvider = new RecipeProvider();

        public ObservableCollection<string> ChamberType { get; set; }
        public ObservableCollection<string> RecipeProcessType { get; set; }
        public int ChamberTypeIndexSelection { get; set; }

        public int _processTypeIndexSelection;

        public int ProcessTypeIndexSelection
        {
            get { return this._processTypeIndexSelection; }
            set { this._processTypeIndexSelection = value; this.NotifyOfPropertyChange("ProcessTypeIndexSelection"); }
        }

        public string CurrentChamberType
        {
            get
            {
                return ChamberType[ChamberTypeIndexSelection];
            }
        }

        public string CurrentProcessType
        {
            get
            {
                if (ProcessTypeIndexSelection < 0)
                    ProcessTypeIndexSelection = 0;

                if (ProcessTypeFileList.Count == 0)
                    return "Process";

                return ProcessTypeFileList[ProcessTypeIndexSelection].ProcessType;
            }
        }

        public Visibility MultiChamberVisibility
        {
            get;
            set;
        }

        public Visibility ToleranceVisibility
        {
            get;
            set;
        }

        public ObservableCollection<string> Chambers { get; set; }

        public string SelectedChamber { get; set; }

        public object View { get; set; }

        public Dictionary<string, string> DicPMChamberType { get; set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            var chamberType = QueryDataClient.Instance.Service.GetConfig("System.Recipe.SupportedChamberType");
            if (chamberType == null)
            {
                ChamberType = new ObservableCollection<string>() { "Default" };
            }
            else
            {
                ChamberType = new ObservableCollection<string>(((string)(chamberType)).Split(','));
            }
            ChamberTypeIndexSelection = 0;

            //Etch:Process,Clean,Chuck,Dechuck;CVD:Process,Clean;
            var processType = QueryDataClient.Instance.Service.GetConfig("System.Recipe.SupportedProcessType");
            if (processType == null)
            {
                RecipeProcessType = new ObservableCollection<string>() { "Process" };
            }
            else
            {
                RecipeProcessType = new ObservableCollection<string>(((string)processType).Split(','));
            }

            ProcessTypeFileList = new ObservableCollection<ProcessTypeFileItem>();

            var chamber = QueryDataClient.Instance.Service.GetConfig("System.Recipe.ChamberModules");

            Chambers = new ObservableCollection<string>(((string)chamber).Split(','));
            SelectedChamber = Chambers[0];

            MultiChamberVisibility = Chambers.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

            DicPMChamberType = new Dictionary<string, string>();

            foreach (var pm in Chambers)
            {
                var type = QueryDataClient.Instance.Service.GetConfig($"System.SetUp.{pm}.ChamberType");
                if (type == null)
                {
                    DicPMChamberType[pm] = ChamberType[0];
                }
                else
                {
                    DicPMChamberType[pm] = (string)type;
                }
            }

            for (int i = 0; i < ChamberType.Count; i++)
            {
                if (ChamberType[i] == DicPMChamberType[SelectedChamber])
                {
                    ChamberTypeIndexSelection = i; // 修改当前腔体类型
                }
            }

            UpdateProcessTypeFileList();
            UpdateRecipeFormat();
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            TreeSelectChanged(CurrentFileNode);
            UpdateView();
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);

            if (this.IsChanged)
            {
                if (DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.CONFIRM, $"Recipe {CurrentRecipe.Name} content is changed, do you want to save it?") == DialogButton.Yes)
                {
                    this.SaveRecipe();
                }
            }
        }

        protected override void OnViewLoaded(object view)
        {
            View = view;
            base.OnViewLoaded(view);
            RecipeFormatBuilder.ApplyTemplate((UserControl)view, this.Columns);
            u = (RecipeEditorView)view;
            u.dgCustom.Columns.Clear();
            u.ToleranceGrid.Columns.Clear();

            this.Columns.Apply((c) =>
            {
                c.Header = c;
                u.dgCustom.Columns.Add(c);

            });

            RecipeFormatBuilder.ApplyTemplate((UserControl)view, this.ColumnsTolerance);
            ColumnsTolerance.Apply((c) =>
            {
                c.Header = c;
                u.ToleranceGrid.Columns.Add(c);
            });

            u.dgCustom.ItemsSource = this.CurrentRecipe.Steps;
            u.ToleranceGrid.ItemsSource = this.CurrentRecipe.StepTolerances;
        }

        public void TabSelectionChanged()
        {
            UpdateRecipeFormat();
            TreeSelectChanged(CurrentFileNode);
            OnViewLoaded(View);
        }
        public void FreezeClick()
        {
            this.CurrentRecipe.Freeze = true;
            this.Save(this.CurrentRecipe, false);
        }

        public void UnFreezeClick()
        {
            this.CurrentRecipe.Freeze = false;
            this.Save(this.CurrentRecipe, false);
        }

        public void CmbSelectionChanged()
        {
            bool DopantChoice = false;
            //bool RecipeMixOn = false;
            foreach (var param in CurrentRecipe.ConfigSelItems)
            {
                if (param.DisplayName == "Dopant_Choice" && ((RecipeEditorLib.RecipeModel.Params.ComboxParam)param).Value.ToString() == "Close")
                {
                    DopantChoice = true;
                }

                if (DopantChoice == true)
                {
                    if (param.DisplayName == "RecipeMixOn")
                    {
                        foreach (var param2 in CurrentRecipe.ConfigItems)
                        {
                            if (((RecipeEditorLib.RecipeModel.Params.ComboxParam)param).Value.ToString() == "Yes")
                            {
                                if (param2.DisplayName == "DopantRatio")
                                {
                                    param2.IsEnabled = false;
                                    break;
                                }
                            }
                            else
                            {
                                if (param2.DisplayName == "DopantRatio")
                                {
                                    param2.IsEnabled = true;
                                    break;
                                }
                            }
                        }
                    }

                }
                else
                {
                    foreach (var param2 in CurrentRecipe.ConfigItems)
                    {
                        if (param2.DisplayName == "DopantRatio")
                        {
                            param2.IsEnabled = true;
                            break;
                        }
                    }
                }

            }


        }

        public void UpdateRecipeFormat()
        {
            this.Columns = this._columnBuilder.Build($"{SelectedChamber}\\{CurrentProcessType}");

            int indexOthers = this.Columns.IndexOf(this.Columns.Where(x => x.DisplayName == "Others").FirstOrDefault());
            ColumnsTolerance = new ObservableCollection<EditorDataGridTemplateColumnBase>(
                this.Columns.Take(indexOthers));

            this.CurrentRecipe = new RecipeData();
            CurrentRecipe.RecipeChamberType = _columnBuilder.RecipeChamberType;
            CurrentRecipe.RecipeVersion = _columnBuilder.RecipeVersion;

            this.PopSettingColumns = new Dictionary<string, ObservableCollection<Param>>();
            this.PopSettingColumns.Add("Oes", _columnBuilder.OesConfig);
            this.PopSettingColumns.Add("Vat", _columnBuilder.VatConfig);
            this.PopSettingColumns.Add("FineTuning", _columnBuilder.FineTuningConfig);

            CurrentRecipe.PopEnable.Add("Oes", _columnBuilder.OesConfig.Count > 0);
            CurrentRecipe.PopEnable.Add("Vat", _columnBuilder.VatConfig.Count > 0);
            CurrentRecipe.PopEnable.Add("FineTuning", _columnBuilder.FineTuningConfig.Count > 0);

            CurrentRecipe.ToleranceEnable = Columns.FirstOrDefault(x => x.EnableTolerance) != null;

            this.editMode = EditMode.None;

            ToleranceVisibility = CurrentRecipe.ToleranceEnable ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateProcessTypeFileList()
        {
            for (int i = 0; i < RecipeProcessType.Count; i++)
            {
                if (ChamberTypeIndexSelection != 1 && i == 1) // PVD存在Process和Clean，其他腔体只有Process
                    continue;

                var type = new ProcessTypeFileItem();
                type.ProcessType = RecipeProcessType[i];
                var prefix = $"{SelectedChamber}\\{RecipeProcessType[i]}";
                var recipes = _recipeProvider.GetXmlRecipeList(prefix);
                type.FileListByProcessType = RecipeSequenceTreeBuilder.BuildFileNode(prefix, "", false, recipes)[0].Files;
                ProcessTypeFileList.Insert(i, type);
            }

            int remainCount = ChamberTypeIndexSelection == 1 ? 2 : 1; // PVD存在Process和Clean，其他腔体只有Process

            while (ProcessTypeFileList.Count > remainCount)
            {
                ProcessTypeFileList.RemoveAt(ProcessTypeFileList.Count - 1);
            }

            ProcessTypeIndexSelection = 0;
        }

        public void ChamberSelectionChanged()
        {
            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No,
                    DialogType.CONFIRM,
                    $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");

                if (selection == DialogButton.Yes)
                {
                    this.CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    this.CurrentRecipe.ReviseTime = DateTime.Now;
                    this.Save(this.CurrentRecipe, false);
                }
            }

            for (int i = 0; i < ChamberType.Count; i++)
            {
                if (ChamberType[i] == DicPMChamberType[SelectedChamber])
                {
                    ChamberTypeIndexSelection = i; // 修改当前腔体类型
                }
            }

            UpdateProcessTypeFileList();

            CurrentRecipe.ChangeChamber(Columns, PopSettingColumns
                , _columnBuilder.Configs, SelectedChamber);
        }

        public void TreeSelectChanged(FileNode node)
        {
            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No,
                    DialogType.CONFIRM,
                    $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");

                if (selection == DialogButton.Yes)
                {
                    this.CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    this.CurrentRecipe.ReviseTime = DateTime.Now;
                    this.Save(this.CurrentRecipe, false);
                }
            }

            CurrentFileNode = node;

            if (node != null && node.IsFile)
            {
                this.LoadData(node.PrefixPath, node.FullPath);
            }
            else
            {
                this.ClearData();
                this.editMode = EditMode.None;
            }
            if (CurrentRecipe != null)
            {
                //this.CurrentRecipe.EnableFreeze = !this.CurrentRecipe.Freeze && BaseApp.Instance.UserContext.LoginName == this.CurrentRecipe.Revisor;
                //this.CurrentRecipe.EnableNotFreeze = this.CurrentRecipe.Freeze && BaseApp.Instance.UserContext.LoginName == this.CurrentRecipe.Revisor;
                this.CurrentRecipe.EnableFreeze = !this.CurrentRecipe.Freeze;
                this.CurrentRecipe.EnableNotFreeze = this.CurrentRecipe.Freeze;

                //if (BaseApp.Instance.UserContext.LoginName != this.CurrentRecipe.Revisor)
                //{
                    IsFreeze = this.CurrentRecipe.Freeze;
                //}
                //else
                //{
                //    IsFreeze = false;
                //}
            }
            else
            {
                IsFreeze = false;
            }
            this.UpdateView();
        }

        #region folder
        public void NewFolder()
        {
            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No | DialogButton.Cancel, DialogType.CONFIRM, $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");
                if (selection == DialogButton.Cancel)
                    return;

                if (selection == DialogButton.Yes)
                {
                    this.CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    this.CurrentRecipe.ReviseTime = DateTime.Now;
                    this.Save(this.CurrentRecipe, false);
                }
            }

            InputFileNameDialogViewModel dialog = new InputFileNameDialogViewModel("Input New Folder Name");
            dialog.FileName = "new folder";
            WindowManager wm = new WindowManager();
            bool? dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            string name = dialog.FileName.Trim();
            if (string.IsNullOrEmpty(name))
            {
                DialogBox.ShowWarning("Folder name should not be empty");
                return;
            }

            string prefix = SelectedChamber + "\\" + ProcessTypeFileList[ProcessTypeIndexSelection].ProcessType;
            string processType = string.Empty;
            string newFolder = string.Empty;
            if (CurrentFileNode != null)
            {
                prefix = CurrentFileNode.PrefixPath;
                string folder = CurrentFileNode.FullPath;
                if (CurrentFileNode.IsFile)
                {
                    folder = folder.Substring(0, folder.LastIndexOf("\\") + 1);
                    if (!string.IsNullOrEmpty(folder))
                        newFolder = folder;
                }
                else
                {
                    newFolder = folder + "\\";
                }

            }

            newFolder = newFolder + name;

            if (IsExist(newFolder, false))
            {
                DialogBox.ShowWarning($"Can not create folder {newFolder}, Folder with the same name already exist.");
                return;
            }

            if (newFolder.Length > 200)
            {
                DialogBox.ShowWarning($"Can not create folder {newFolder}, Folder name too long, should be less 200.");
                return;
            }

            _recipeProvider.CreateRecipeFolder(prefix, newFolder);

            ReloadRecipeFileList(SelectedChamber, CurrentProcessType, newFolder, true);
        }

        public void NewFolderRoot()
        {
            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No | DialogButton.Cancel, DialogType.CONFIRM, $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");
                if (selection == DialogButton.Cancel)
                    return;

                if (selection == DialogButton.Yes)
                {
                    this.CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    this.CurrentRecipe.ReviseTime = DateTime.Now;
                    this.Save(this.CurrentRecipe, false);
                }
            }

            InputFileNameDialogViewModel dialog = new InputFileNameDialogViewModel("Input New Folder Name");
            dialog.FileName = "new folder";
            WindowManager wm = new WindowManager();
            bool? dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            string name = dialog.FileName.Trim();
            if (string.IsNullOrEmpty(name))
            {
                DialogBox.ShowWarning("Folder name should not be empty");
                return;
            }


            if (IsExist(name, false))
            {
                DialogBox.ShowWarning($"Can not create folder {name}, Folder with the same name already exist.");
                return;
            }

            if (name.Length > 200)
            {
                DialogBox.ShowWarning($"Can not create folder {name}, Folder name too long, should be less 200.");
                return;
            }

            string prefix = SelectedChamber + "\\" + ProcessTypeFileList[ProcessTypeIndexSelection].ProcessType;

            _recipeProvider.CreateRecipeFolder(prefix, name);

            ReloadRecipeFileList(SelectedChamber, CurrentProcessType, name, true);
        }

        public void DeleteFolder()
        {
            if (CurrentFileNode == null || CurrentFileNode.IsFile)
                return;

            if (CurrentFileNode.Files.Count > 0)
            {
                DialogBox.ShowWarning($"Can not delete non-empty folder, Remove the files or folders under \r\n{CurrentFileNode.FullPath}.");
                return;
            }

            var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.CONFIRM,
                $"Are you sure you want to delete \r\n {CurrentFileNode.FullPath}?");
            if (selection == DialogButton.No)
                return;

            string nextFocus = CurrentFileNode.Parent.FullPath;
            bool isFolder = true;
            if (CurrentFileNode.Parent.Files.Count > 1)
            {
                for (int i = 0; i < CurrentFileNode.Parent.Files.Count; i++)
                {
                    if (CurrentFileNode.Parent.Files[i] == CurrentFileNode)
                    {
                        if (i == 0)
                        {
                            nextFocus = CurrentFileNode.Parent.Files[i + 1].FullPath;
                            isFolder = !CurrentFileNode.Parent.Files[i + 1].IsFile;
                        }
                        else
                        {
                            nextFocus = CurrentFileNode.Parent.Files[i - 1].FullPath;
                            isFolder = !CurrentFileNode.Parent.Files[i - 1].IsFile;
                        }
                    }
                }
            }

            _recipeProvider.DeleteRecipeFolder(CurrentFileNode.PrefixPath, CurrentFileNode.FullPath);

            ReloadRecipeFileList(SelectedChamber, CurrentProcessType, nextFocus, isFolder);
        }

        public void RenameFolder()
        {
            if (CurrentFileNode == null || CurrentFileNode.IsFile)
                return;

            InputFileNameDialogViewModel dialog = new InputFileNameDialogViewModel("Input New Folder Name");
            dialog.FileName = CurrentFileNode.Name;
            WindowManager wm = new WindowManager();
            bool? dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            string name = dialog.FileName.Trim();
            if (string.IsNullOrEmpty(name))
                return;

            string newFolder = CurrentFileNode.FullPath.Substring(0, CurrentFileNode.FullPath.LastIndexOf("\\") + 1);
            if (!string.IsNullOrEmpty(newFolder))
                newFolder = newFolder + name;
            else
                newFolder = name;

            if (newFolder == CurrentFileNode.FullPath)
                return;

            if (IsExist(newFolder, false))
            {
                DialogBox.ShowWarning($"Can not rename to {newFolder}, Folder with the same name already exist.");
                return;
            }

            if (newFolder.Length > 200)
            {
                DialogBox.ShowWarning($"Can not create folder {newFolder}, Folder name too long, should be less 200.");
                return;
            }

            _recipeProvider.RenameFolder(CurrentFileNode.PrefixPath, CurrentFileNode.FullPath, newFolder);

            ReloadRecipeFileList(SelectedChamber, CurrentProcessType, newFolder, true);
        }

        #endregion

        #region recipe
        public void NewRecipe()
        {
            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No | DialogButton.Cancel, DialogType.CONFIRM, $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");
                if (selection == DialogButton.Cancel)
                    return;

                if (selection == DialogButton.Yes)
                {
                    this.CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    this.CurrentRecipe.ReviseTime = DateTime.Now;
                    this.Save(this.CurrentRecipe, false);
                }
            }

            InputFileNameDialogViewModel dialog = new InputFileNameDialogViewModel("Input New Recipe Name");
            dialog.FileName = "new recipe";
            WindowManager wm = new WindowManager();
            bool? dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            string recipeName = dialog.FileName.Trim();
            if (string.IsNullOrEmpty(dialog.FileName))
            {
                DialogBox.ShowWarning("Recipe file name should not be empty");
                return;
            }

            string prefix = SelectedChamber + "\\" + CurrentProcessType;
            string processType = string.Empty;
            if (CurrentFileNode != null)
            {
                string folder = CurrentFileNode.FullPath;


                if (CurrentFileNode.IsFile)
                {
                    folder = folder.Substring(0, folder.LastIndexOf("\\") + 1);
                    //if (!string.IsNullOrEmpty(folder))
                    //    folder = folder;
                }
                else
                {
                    folder = folder + "\\";
                }
                recipeName = folder + recipeName;
            }

            if (IsExist(recipeName, true))
            {
                DialogBox.ShowWarning($"Can not create {recipeName}, Recipe with the same name already exist.");
                return;
            }


            if (recipeName.Length > 200)
            {
                DialogBox.ShowWarning($"Can not create folder {recipeName}, Folder name too long, should be less 200.");
                return;
            }

            RecipeData recipe = new RecipeData();
            recipe.Name = recipeName;
            recipe.PrefixPath = prefix;
            recipe.Creator = BaseApp.Instance.UserContext.LoginName;
            recipe.CreateTime = DateTime.Now;
            recipe.Revisor = BaseApp.Instance.UserContext.LoginName;
            recipe.ReviseTime = DateTime.Now;
            recipe.Description = string.Empty;
            recipe.Freeze = false;
            if (!Save(recipe, true))
                return;

            var types = prefix.Split('\\');

            ReloadRecipeFileList(types[0], types[1], recipeName, false);

        }
        public void CopyAsRecipe()
        {
            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No | DialogButton.Cancel, DialogType.CONFIRM, $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");
                if (selection == DialogButton.Cancel)
                    return;
                if (selection == DialogButton.Yes)
                {
                    this.CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    this.CurrentRecipe.ReviseTime = DateTime.Now;
                    this.Save(this.CurrentRecipe, false);
                }
            }
            RecipeSelectDialogViewModel dialog = new RecipeSelectDialogViewModel();
            dialog.DisplayName = "Copy Recipe ";
            dialog.ProcessTypeFileList = ProcessTypeFileList;
            WindowManager wm = new WindowManager();
            string FloderName = "";
            bool? bret = wm.ShowDialog(dialog);
            if ((bool)bret)
            {
                var DialogResult = dialog.DialogResult;
                FloderName = dialog.DialogResult; 
            }
            string Floder = FloderName.Replace(SelectedChamber + "\\" + CurrentProcessType + "\\", "");          
            string prefix =  FloderName;
            string processType = string.Empty;
            string newName;
            int starIndex = this.CurrentRecipe.Name.LastIndexOf("\\");
            if (starIndex < 1)
                newName = this.CurrentRecipe.Name;
            else
                newName = this.CurrentRecipe.Name.Substring(starIndex + 1);
            string deletePath = SelectedChamber + "\\" + CurrentProcessType + "\\" + this.CurrentRecipe.Name.Substring(0, this.CurrentRecipe.Name.LastIndexOf("\\"));
            if (prefix == deletePath)
            {
                DialogBox.ShowError($"Can not copy to {newName}, The file is exist.");
                return;
            }
            if (!IsExist(Floder, false))
            {
                DialogBox.ShowError($"Can not copy to {newName}, The floder not exist.");
                return;
            }
            if (IsExist(Floder + "\\"+newName, true))
            {
                DialogBox.ShowError($"Can not copy to {newName}, The file  exist.");
                return;
            }
            this.CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
            this.CurrentRecipe.ReviseTime = DateTime.Now;
            bool result = this._recipeProvider.SaveRecipe(prefix, newName, this.CurrentRecipe.GetXmlString());
            if (result)
            {
                DialogBox.ShowInfo("Recipe file copied successfully");
            }
            else
            {
                DialogBox.ShowError($"Can not copy to {newName}, Please confim.");
            }
            ReloadRecipeFileList(SelectedChamber, CurrentProcessType, prefix, false);
        }
        public void NewRecipeRoot()
        {
            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No | DialogButton.Cancel, DialogType.CONFIRM, $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");
                if (selection == DialogButton.Cancel)
                    return;

                if (selection == DialogButton.Yes)
                {
                    this.CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    this.CurrentRecipe.ReviseTime = DateTime.Now;
                    this.Save(this.CurrentRecipe, false);
                }
            }

            InputFileNameDialogViewModel dialog = new InputFileNameDialogViewModel("Input New Recipe Name");
            dialog.FileName = "new recipe";
            WindowManager wm = new WindowManager();
            bool? dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            string recipeName = dialog.FileName.Trim();
            if (string.IsNullOrEmpty(dialog.FileName))
            {
                DialogBox.ShowWarning("Recipe file name should not be empty");
                return;
            }

            if (IsExist(recipeName, true))
            {
                DialogBox.ShowWarning($"Can not create {recipeName}, Recipe with the same name already exist.");
                return;
            }


            if (recipeName.Length > 200)
            {
                DialogBox.ShowWarning($"Can not create folder {recipeName}, Folder name too long, should be less 200.");
                return;
            }

            RecipeData recipe = new RecipeData();
            recipe.Name = recipeName;
            recipe.PrefixPath = SelectedChamber + "\\" + CurrentProcessType;
            recipe.Creator = BaseApp.Instance.UserContext.LoginName;
            recipe.CreateTime = DateTime.Now;
            recipe.Revisor = BaseApp.Instance.UserContext.LoginName;
            recipe.ReviseTime = DateTime.Now;
            recipe.Description = string.Empty;
            recipe.Freeze = false;
            if (!Save(recipe, true))
                return;


            ReloadRecipeFileList(SelectedChamber, CurrentProcessType, recipeName, false);
        }

        private void ReloadRecipeFileList(string chamberType, string processType, string selectedFile, bool selectionIsFolder)
        {
            ProcessTypeFileItem item = ProcessTypeFileList.FirstOrDefault(x => x.ProcessType == processType);
            if (item == null)
            {
                LOG.Write("error reload recipe file list, type = " + processType);
            }

            var prefix = $"{SelectedChamber}\\{item.ProcessType}";
            var recipes = _recipeProvider.GetXmlRecipeList(prefix);

            item.FileListByProcessType = RecipeSequenceTreeBuilder.BuildFileNode(prefix, selectedFile, selectionIsFolder, recipes)[0].Files;

            item.InvokePropertyChanged();
        }

        private bool IsExist(string fullPath, bool isFile)
        {
            for (int i = 0; i < ProcessTypeFileList.Count; i++)
            {
                if (ProcessTypeFileList[i].ProcessType == CurrentProcessType)
                {
                    if (ProcessTypeFileList[i].FileListByProcessType.Count == 0)
                        return false;

                    return FindFile(fullPath, ProcessTypeFileList[i].FileListByProcessType[0].Parent, isFile);
                }
            }

            return true;
        }
        private bool FindFile(string path, FileNode root, bool isFile)
        {
            if (root.FullPath == path && !isFile)
            {
                return true;
            }

            foreach (var node in root.Files)
            {
                if (isFile && node.IsFile && node.FullPath == path)
                    return true;

                if (!node.IsFile && FindFile(path, node, isFile))
                    return true;
            }
            return false;
        }

        public void SaveAsRecipe()
        {
            if (CurrentFileNode == null || !CurrentFileNode.IsFile)
                return;

            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No | DialogButton.Cancel, DialogType.CONFIRM, $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");
                if (selection == DialogButton.Cancel)
                    return;

                if (selection == DialogButton.Yes)
                {
                    this.CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    this.CurrentRecipe.ReviseTime = DateTime.Now;
                    this.Save(this.CurrentRecipe, false);
                }
            }

            InputFileNameDialogViewModel dialog = new InputFileNameDialogViewModel("Input New Recipe Name");
            dialog.FileName = CurrentFileNode.Name;
            WindowManager wm = new WindowManager();
            bool? dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            string recipeName = dialog.FileName.Trim();
            if (string.IsNullOrEmpty(dialog.FileName))
            {
                DialogBox.ShowWarning("Recipe file name should not be empty");
                return;
            }

            string prefix = SelectedChamber + "\\" + CurrentProcessType;
            string processType = string.Empty;

            string folder = CurrentFileNode.FullPath;
            if (CurrentFileNode.IsFile)
            {
                folder = folder.Substring(0, folder.LastIndexOf("\\") + 1);
            }
            if (!string.IsNullOrEmpty(folder))
                recipeName = folder + "\\" + recipeName;

            if (CurrentFileNode.FullPath == recipeName)
                return;

            if (IsExist(recipeName, true))
            {
                DialogBox.ShowWarning($"Can not copy to {recipeName}, Recipe with the same name already exist.");
                return;
            }


            if (recipeName.Length > 200)
            {
                DialogBox.ShowWarning($"Can not create folder {recipeName}, Folder name too long, should be less 200.");
                return;
            }

            CurrentRecipe.Creator = BaseApp.Instance.UserContext.LoginName;
            CurrentRecipe.CreateTime = DateTime.Now;
            CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
            CurrentRecipe.ReviseTime = DateTime.Now;
            CurrentRecipe.Description = CurrentRecipe.Description + ". Renamed from " + CurrentFileNode.Name;

            _recipeProvider.SaveAsRecipe(prefix, recipeName, CurrentRecipe.GetXmlString());

            ReloadRecipeFileList(SelectedChamber, CurrentProcessType, recipeName, false);
        }


        public void RenameRecipe()
        {
            if (CurrentFileNode == null || !CurrentFileNode.IsFile)
                return;

            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No | DialogButton.Cancel, DialogType.CONFIRM, $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");
                if (selection == DialogButton.Cancel)
                    return;

                if (selection == DialogButton.Yes)
                {
                    this.CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    this.CurrentRecipe.ReviseTime = DateTime.Now;
                    this.Save(this.CurrentRecipe, false);
                }
            }

            InputFileNameDialogViewModel dialog = new InputFileNameDialogViewModel("Input New Recipe Name");
            dialog.FileName = CurrentFileNode.Name;
            WindowManager wm = new WindowManager();
            bool? dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            string recipeName = dialog.FileName.Trim();
            if (string.IsNullOrEmpty(dialog.FileName))
            {
                DialogBox.ShowWarning("Recipe file name should not be empty");
                return;
            }

            string prefix = SelectedChamber + "\\" + CurrentProcessType;
            string processType = string.Empty;

            string newName = CurrentFileNode.FullPath.Substring(0, CurrentFileNode.FullPath.LastIndexOf("\\") + 1);
            if (!string.IsNullOrEmpty(newName))
                newName = newName + recipeName;
            else
                newName = recipeName;

            if (newName == CurrentFileNode.FullPath)
                return;

            if (IsExist(newName, true))
            {
                DialogBox.ShowWarning($"Can not rename to {newName}, Recipe with the same name already exist.");
                return;
            }


            if (newName.Length > 200)
            {
                DialogBox.ShowWarning($"Can not create folder {newName}, Folder name too long, should be less 200.");
                return;
            }

            _recipeProvider.RenameRecipe(prefix, CurrentFileNode.FullPath, newName);

            ReloadRecipeFileList(SelectedChamber, CurrentProcessType, newName, false);
        }


        public void DeleteRecipe()
        {
            if (CurrentFileNode == null || !CurrentFileNode.IsFile)
                return;

            var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.CONFIRM,
                $"Are you sure you want to delete \r\n {CurrentFileNode.FullPath}?");
            if (selection == DialogButton.No)
                return;

            string nextFocus = CurrentFileNode.Parent.FullPath;
            bool isFolder = true;
            if (CurrentFileNode.Parent.Files.Count > 1)
            {
                for (int i = 0; i < CurrentFileNode.Parent.Files.Count; i++)
                {
                    if (CurrentFileNode.Parent.Files[i] == CurrentFileNode)
                    {
                        if (i == 0)
                        {
                            nextFocus = CurrentFileNode.Parent.Files[i + 1].FullPath;
                            isFolder = !CurrentFileNode.Parent.Files[i + 1].IsFile;
                        }
                        else
                        {
                            nextFocus = CurrentFileNode.Parent.Files[i - 1].FullPath;
                            isFolder = !CurrentFileNode.Parent.Files[i - 1].IsFile;
                        }
                    }
                }
            }

            _recipeProvider.DeleteRecipe(CurrentFileNode.PrefixPath, CurrentFileNode.FullPath);

            ReloadRecipeFileList(SelectedChamber, CurrentProcessType, nextFocus, isFolder);
        }

        public void ReloadRecipe()
        {
            if (this.editMode == EditMode.Normal || this.editMode == EditMode.Edit)
            {
                this.LoadData(CurrentRecipe.PrefixPath, CurrentRecipe.Name);
                this.UpdateView();
            }
        }

        public void SaveToAll()
        {
            if (!CurrentRecipe.IsCompatibleWithCurrentFormat)
            {
                DialogBox.ShowWarning($"Save failed, {CurrentRecipe.Name} is not a valid recipe file");
                return;
            }

            var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No,
                DialogType.CONFIRM, $"Do you want to save to all? \r\n This will replace all the other chamber recipe content");
            if (selection == DialogButton.No)
                return;

            List<string> moduleList = new List<string>();
            var selectedChamberType = DicPMChamberType[SelectedChamber];

            foreach (var pm in DicPMChamberType)
            {
                if (pm.Value == selectedChamberType)
                    moduleList.Add(pm.Key);
            }

            foreach (var chamber in moduleList)
            {
                var prefix = $"{chamber}\\{CurrentProcessType}";
                var cbRecipes = _recipeProvider.GetRecipes(prefix);
                if (cbRecipes.Contains(CurrentRecipe.Name))
                {
                    var result = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No | DialogButton.Cancel, DialogType.CONFIRM, $"Recipe '{CurrentRecipe.Name}' with the same name already exist in {chamber}.\r\nAre you sure you want to overwrite?");
                    if (result == DialogButton.Cancel)
                        return;
                    if (result == DialogButton.No)
                        continue;
                }

                string temp = CurrentRecipe.GetXmlString();
                string recipeContent = temp.Replace($"Name=\"{SelectedChamber}\"", $"Name=\"{chamber}\"");

                _recipeProvider.SaveRecipe(prefix, CurrentRecipe.Name, recipeContent);
            }

            Save(this.CurrentRecipe, false);
        }

        public void SaveTo()
        {
            if (!CurrentRecipe.IsCompatibleWithCurrentFormat)
            {
                DialogBox.ShowWarning($"Save failed, {CurrentRecipe.Name} is not a valid recipe file");
                return;
            }

            List<string> moduleList = new List<string>();
            var selectedChamberType = DicPMChamberType[SelectedChamber];

            foreach (var pm in DicPMChamberType)
            {
                if (pm.Value == selectedChamberType)
                    moduleList.Add(pm.Key);
            }

            SaveToDialogViewModel dialog = new SaveToDialogViewModel("Select which chamber to copy to", SelectedChamber, moduleList.ToList());

            WindowManager wm = new WindowManager();
            bool? dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            List<string> chambers = new List<string>();
            foreach (var dialogChamber in dialog.Chambers)
            {
                if (dialogChamber.IsEnabled && dialogChamber.IsChecked)
                    chambers.Add(dialogChamber.Name);
            }

            if (chambers.Count == 0)
                return;

            foreach (var chamber in chambers)
            {
                if (chamber == SelectedChamber)
                    continue;
                var prefix = $"{chamber}\\{CurrentProcessType}";
                var cbRecipes = _recipeProvider.GetRecipes(prefix);
                if (cbRecipes.Contains(CurrentRecipe.Name))
                {
                    var result = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No | DialogButton.Cancel, DialogType.CONFIRM, $"Recipe '{CurrentRecipe.Name}' with the same name already exist in {chamber}.\r\nAre you sure you want to overwrite?");
                    if (result == DialogButton.Cancel)
                        return;
                    if (result == DialogButton.No)
                        continue;
                }

                string temp = CurrentRecipe.GetXmlString();
                string recipeContent = temp.Replace($"Name=\"{SelectedChamber}\"", $"Name=\"{chamber}\"");

                _recipeProvider.SaveRecipe(prefix, CurrentRecipe.Name, recipeContent);

            }

            Save(this.CurrentRecipe, false);
        }
        #endregion

        #region Steps

        public void ParamsExpanded(ExpanderColumn col)
        {
            int index = this.Columns.IndexOf(col);
            for (var i = index + 1; i < this.Columns.Count; i++)
            {
                if (this.Columns[i] is ExpanderColumn)
                    break;
                this.Columns[i].Visibility = Visibility.Visible;
            }
        }

        public void ParamsCollapsed(ExpanderColumn col)
        {
            int index = this.Columns.IndexOf(col);
            for (var i = index + 1; i < this.Columns.Count; i++)
            {
                if (this.Columns[i] is ExpanderColumn)
                    break;
                this.Columns[i].Visibility = Visibility.Collapsed;
            }
        }
        public void SaveRecipe()
        {
            if (this.IsChanged)
            {
                this.Save(this.CurrentRecipe, false);
            }
        }

        public void PopSetting(string controlName, Param paramData)
        {
            int stepNum = Convert.ToInt32(((StepParam)paramData.Parent[1]).Value);

            PublicPopSettingDialogViewModel dialog = new PublicPopSettingDialogViewModel();
            dialog.DisplayName = paramData.DisplayName;
            ObservableCollection<Param> Parameters = new ObservableCollection<Param>();

            Parameters = this.CurrentRecipe.PopSettingSteps[controlName][stepNum - 1];

            ObservableCollection<Param> ControlParameters = new ObservableCollection<Param>();
            ObservableCollection<BandParam> BrandParameters = new ObservableCollection<BandParam>();
            foreach (var item in Parameters)
            {
                if (item.Name.Contains("Band"))
                {
                    string name = item.Name.Replace("Wavelength", "").Replace("Bandwidth", "");
                    string displayName = item.DisplayName.Replace("Wavelength", "").Replace("Bandwidth", "");

                    if (BrandParameters.Where(x => x.Name == name).Count() == 0)
                    {
                        BrandParameters.Add(new BandParam()
                        {
                            Name = name,
                            DisplayName = displayName
                        });
                    }

                    if (item.Name.Contains("Wavelength"))
                    {
                        BrandParameters.First(x => x.Name == name).WavelengthDoubleParam = item;
                    }
                    else if (item.Name.Contains("Bandwidth"))
                    {
                        BrandParameters.First(x => x.Name == name).BandwidthDoubleParam = item;
                    }
                }
                else
                    ControlParameters.Add(item);
            }

            dialog.Parameters = Parameters;
            dialog.ControlParameters = ControlParameters;
            dialog.BandParameters = BrandParameters;

            WindowManager wm = new WindowManager();
            bool? bret = wm.ShowDialog(dialog);
            if ((bool)bret)
            {
                this.CurrentRecipe.PopSettingSteps[controlName][stepNum - 1] = dialog.Parameters;
            }
        }

        public bool Save(RecipeData recipe, bool createNew)
        {
            bool result = false;
            if (string.IsNullOrEmpty(recipe.Name))
            {
                MessageBox.Show("Recipe name can't be empty");
                return false;
            }

            recipe.Revisor = BaseApp.Instance.UserContext.LoginName;
            recipe.ReviseTime = DateTime.Now;

            result = this._recipeProvider.SaveRecipe(recipe.PrefixPath, recipe.Name, recipe.GetXmlString());

            if (result)
            {
                recipe.DataSaved();

                this.editMode = EditMode.Normal;

                this.UpdateView();

                CurrentRecipe.ChangeChamber(Columns, PopSettingColumns, _columnBuilder.Configs, SelectedChamber);
            }
            else
            {
                MessageBox.Show("Save failed!");
            }

            return result;
        }

        public void AddStep()
        {
            this.CurrentRecipe.Steps.Add(this.CurrentRecipe.CreateStep(this.Columns));
            foreach (string key in PopSettingColumns.Keys)
            {
                if (!this.CurrentRecipe.PopSettingSteps.ContainsKey(key))
                {
                    this.CurrentRecipe.PopSettingSteps.Add(key, new ObservableCollection<ObservableCollection<Param>>());
                }
                this.CurrentRecipe.PopSettingSteps[key].Add(this.PopSettingColumns[key]);
            }

            if (this.editMode != EditMode.New && this.editMode != EditMode.ReName)
                this.editMode = EditMode.Edit;

            int index = 1;
            foreach (ObservableCollection<Param> parameters in this.CurrentRecipe.Steps)
            {
                (parameters[1] as StepParam).Value = index.ToString();
                index++;
            }
            this.UpdateView();
        }

        public void AppendStep()
        {
            int index = -1;
            bool found = false;
            for (var i = 0; i < this.CurrentRecipe.Steps.Count; i++)
            {
                if (this.CurrentRecipe.Steps[i][1] is StepParam && ((StepParam)this.CurrentRecipe.Steps[i][1]).Checked)
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

                this.CurrentRecipe.Steps.Insert(index, this.CurrentRecipe.CreateStep(this.Columns));
                if (this.CurrentRecipe.PopSettingSteps.Count != 0)
                {
                    foreach (string key in PopSettingColumns.Keys)
                    {
                        this.CurrentRecipe.PopSettingSteps[key].Insert(index, this.PopSettingColumns[key]);
                    }
                }

                index = 1;
                foreach (ObservableCollection<Param> parameters in this.CurrentRecipe.Steps)
                {
                    (parameters[1] as StepParam).Value = index.ToString();
                    index++;
                }
            }
        }

        private ObservableCollection<ObservableCollection<Param>> copySteps = new ObservableCollection<ObservableCollection<Param>>();
        private Dictionary<string, ObservableCollection<ObservableCollection<Param>>> popCopySteps = new Dictionary<string, ObservableCollection<ObservableCollection<Param>>>();

        public void CopyStep()
        {
            this.copySteps.Clear();
            this.popCopySteps.Clear();

            for (var i = 0; i < this.CurrentRecipe.Steps.Count; i++)
            {
                if (this.CurrentRecipe.Steps[i][1] is StepParam && ((StepParam)this.CurrentRecipe.Steps[i][1]).Checked)
                {
                    this.copySteps.Add(this.CurrentRecipe.CloneStep(this.Columns, this.CurrentRecipe.Steps[i]));
                    foreach (string key in PopSettingColumns.Keys)
                    {
                        if (!this.popCopySteps.ContainsKey(key))
                        {
                            this.popCopySteps.Add(key, new ObservableCollection<ObservableCollection<Param>>());
                        }
                        if (this.CurrentRecipe.PopSettingSteps.Count != 0)
                        {
                            this.popCopySteps[key].Add(this.CurrentRecipe.PopSettingSteps[key][i]);
                        }
                    }
                }
            }
            CurrentRecipe.ValidLoopData();
        }

        public void PasteStep()
        {
            if (this.copySteps.Count > 0)
            {
                if (this.editMode != EditMode.New && this.editMode != EditMode.ReName)
                    this.editMode = EditMode.Edit;
                for (var i = 0; i < this.CurrentRecipe.Steps.Count; i++)
                {
                    if (this.CurrentRecipe.Steps[i][1] is StepParam && ((StepParam)this.CurrentRecipe.Steps[i][1]).Checked)
                    {
                        for (var copyindex = 0; copyindex < this.copySteps.Count; copyindex++)
                        {
                            this.CurrentRecipe.Steps.Insert(i, this.CurrentRecipe.CloneStep(this.Columns, this.copySteps[copyindex]));
                            if (this.CurrentRecipe.PopSettingSteps.Count != 0)
                            {
                                foreach (string key in PopSettingColumns.Keys)
                                {
                                    this.CurrentRecipe.PopSettingSteps[key].Insert(i, this.popCopySteps[key][copyindex]);
                                }
                            }
                            i++;
                        }
                        break;
                    }
                }
                int index = 1;
                foreach (ObservableCollection<Param> parameters in this.CurrentRecipe.Steps)
                {
                    (parameters[1] as StepParam).Value = index.ToString();
                    index++;
                }
                CurrentRecipe.ValidLoopData();
                this.UpdateView();
            }
        }

        public void DeleteStep()
        {
            if (this.editMode != EditMode.New && this.editMode != EditMode.ReName)
                this.editMode = EditMode.Edit;

            List<ObservableCollection<Param>> steps = this.CurrentRecipe.Steps.ToList();

            for (var i = 0; i < steps.Count; i++)
            {
                if (steps[i][1] is StepParam && ((StepParam)steps[i][1]).Checked)
                {
                    this.CurrentRecipe.Steps.Remove(steps[i]);

                    foreach (string key in PopSettingColumns.Keys)
                    {
                        if (CurrentRecipe.PopSettingSteps.ContainsKey(key) && CurrentRecipe.PopSettingSteps[key].Count > i)
                            CurrentRecipe.PopSettingSteps[key].Remove(this.CurrentRecipe.PopSettingSteps[key][i]);
                    }
                }
            }
            int index = 1;
            foreach (ObservableCollection<Param> parameters in this.CurrentRecipe.Steps)
            {
                (parameters[1] as StepParam).Value = index.ToString();
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
            };

        }


        private TreeViewItem GetParentObjectEx<TreeViewItem>(DependencyObject obj) where TreeViewItem : FrameworkElement
        {
            if (!DgvEnable)
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
        private void ClearData()
        {
            this.editMode = EditMode.None;
            this.CurrentRecipe.Clear();
            this.CurrentRecipe.Name = string.Empty;
            this.CurrentRecipe.Description = string.Empty;
            this.CurrentRecipe.Freeze = false;
        }

        private void LoadData(string prefixPath, string recipeName)
        {
            CurrentRecipe.Clear();

            var recipeContent = _recipeProvider.LoadRecipe(prefixPath, recipeName);

            if (string.IsNullOrEmpty(recipeContent))
            {
                MessageBox.Show($"{prefixPath}\\{recipeName} is empty, please confirm the file is valid.");
                return;
            }

            CurrentRecipe.RecipeChamberType = _columnBuilder.RecipeChamberType;
            CurrentRecipe.RecipeVersion = _columnBuilder.RecipeVersion;
            CurrentRecipe.InitData(prefixPath, recipeName, recipeContent, Columns, PopSettingColumns, _columnBuilder.Configs, SelectedChamber);

            this.editMode = EditMode.Normal;
        }

        private void UpdateView()
        {
            bool isFileSelected = CurrentFileNode != null && CurrentFileNode.IsFile;

            this.EnableNew = isFileSelected && !this.CurrentRecipe.Freeze;
            this.EnableReName = isFileSelected && !this.CurrentRecipe.Freeze;
            this.EnableCopy = isFileSelected && !this.CurrentRecipe.Freeze;
            this.EnableDelete = isFileSelected && !this.CurrentRecipe.Freeze;
            this.EnableSave = isFileSelected && !this.CurrentRecipe.Freeze;
            this.EnableStep = isFileSelected && !this.CurrentRecipe.Freeze;
            //this.CurrentRecipe.EnableFreeze = !this.CurrentRecipe.Freeze && BaseApp.Instance.UserContext.LoginName == this.CurrentRecipe.Revisor;
            //this.CurrentRecipe.EnableNotFreeze = this.CurrentRecipe.Freeze && BaseApp.Instance.UserContext.LoginName == this.CurrentRecipe.Revisor;
            this.CurrentRecipe.EnableFreeze = !this.CurrentRecipe.Freeze;
            this.CurrentRecipe.EnableNotFreeze = this.CurrentRecipe.Freeze;

            EnableSaveTo = isFileSelected && !this.CurrentRecipe.Freeze;
            EnableSaveToAll = isFileSelected && !this.CurrentRecipe.Freeze;
            EnableReload = isFileSelected && !this.CurrentRecipe.Freeze;
            DgvEnable = IsPermission && !this.CurrentRecipe.Freeze;
            if (this.editMode == EditMode.None)
            {
                this.EnableNew = true;
                this.EnableReName = false;
                this.EnableCopy = false;
                this.EnableDelete = false;
                this.EnableStep = false;
                this.EnableSave = false;

            }
            this.NotifyOfPropertyChange("EnableFreeze");
            this.NotifyOfPropertyChange("EnableNotFreeze");
            this.NotifyOfPropertyChange("EnableNew");
            this.NotifyOfPropertyChange("EnableReName");
            this.NotifyOfPropertyChange("EnableCopy");
            this.NotifyOfPropertyChange("EnableDelete");
            this.NotifyOfPropertyChange("EnableSave");
            this.NotifyOfPropertyChange("EnableStep");
            this.NotifyOfPropertyChange("EnableSaveTo");
            this.NotifyOfPropertyChange("EnableSaveToAll");
            this.NotifyOfPropertyChange("EnableReload");
            this.NotifyOfPropertyChange("DgvEnable");
            this.NotifyOfPropertyChange("CurrentRecipe");
        }


    }
}
