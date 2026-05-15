using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    public class RecipeCompareSelectDialogViewModel : DialogViewModel<string>
    {
        bool _useChamberTypeName = false;
        public RecipeCompareSelectDialogViewModel()
        {
            var chamber = _useChamberTypeName ? QueryDataClient.Instance.Service.GetConfig("System.Recipe.SupportedChamberType") : QueryDataClient.Instance.Service.GetConfig("System.Recipe.ChamberModules");
            Chambers = new ObservableCollection<string>(((string)chamber).Split(','));
            SelectedChamber = Chambers[0];
            var pmSupportedProcessAndCLeanType = QueryDataClient.Instance.Service.GetConfig("System.Recipe.SupportedProcessAndCLeanType");

            if (pmSupportedProcessAndCLeanType != null)
            {
                string[] pmSPAndCTypeList = pmSupportedProcessAndCLeanType.ToString().Split(',');
                if (RecipeProcessTypeDic == null)
                    RecipeProcessTypeDic = new Dictionary<string, ObservableCollection<string>>();
                foreach (string pm in Chambers)
                {
                    if (pmSPAndCTypeList.Contains(pm))
                    {
                        RecipeProcessTypeDic.Add(pm, new ObservableCollection<string>() { "Process", "Clean" });
                    }
                    else
                    {
                        RecipeProcessTypeDic.Add(pm, new ObservableCollection<string>() { "Process" });
                    }
                }
            }
            else
            {
                var processType = QueryDataClient.Instance.Service.GetConfig("System.Recipe.SupportedProcessType");
                Processes = new ObservableCollection<string>(((string)processType).Split(','));
                if (RecipeProcessTypeDic == null)
                    RecipeProcessTypeDic = new Dictionary<string, ObservableCollection<string>>();
                foreach (string pm in Chambers)
                {
                    ObservableCollection<string> recipeTypes = new ObservableCollection<string>();
                    foreach (string recipeType in Processes)
                    {
                        recipeTypes.Add(recipeType);
                    }
                    RecipeProcessTypeDic.Add(pm, recipeTypes);
                }
            }

            DicPMChamberType = new Dictionary<string, string>();
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

            foreach (var pm in Chambers)
            {
                var type = QueryDataClient.Instance.Service.GetConfig($"{pm}.ChamberType");
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
        }

        public void ChamberSelectionChanged()
        {
            for (int i = 0; i < ChamberType.Count; i++)
            {
                if (ChamberType[i] == DicPMChamberType[SelectedChamber])
                {
                    ChamberTypeIndexSelection = i; // 修改当前腔体类型
                    break;
                }
            }
            for(int i=0;i<DicPMChamberType.Count;i++)
            {
                if (DicPMChamberType.Keys.ToArray()[i] == SelectedChamber)
                {
                    ChamberNameIndexSelection = i;
                    break;
                }
            }

            UpdateProcessTypeFileList(SelectedChamber);
            ProcessTypeIndexSelection = 0;
            NotifyOfPropertyChange(nameof(ProcessTypeFileList));
        }

        public void UpdateProcessTypeFileList(string selectedChamber)
        {
            //ProcessTypeFileList.Clear();
            for (int i = 0; i < RecipeProcessTypeDic[SelectedChamber].Count; i++)
            {
                var type = new ProcessTypeFileItem();
                type.ProcessType = RecipeProcessTypeDic[SelectedChamber][i];
                var prefix = $"{Chambers[ChamberNameIndexSelection]}\\{RecipeProcessTypeDic[SelectedChamber][i]}";
                var recipes = _recipeProvider.GetXmlRecipeList(prefix);

                string[] parts = Regex.Split(recipes, "<");

                //string recipeChamber;
                //recipeChamber = "<" + parts[1];
                //foreach (string part in parts)
                //{
                //    if (CurrentChamberType == ChamberType[0] && (Chambers.Count > 1 && part.Contains($".{Chambers[1]}")) && (Chambers.Count > 2 && part.Contains($".{Chambers[2]}")))
                //    {
                //        string temp = part.Replace($".{Chambers[1]}", string.Empty);
                //        temp = temp.Replace($".{Chambers[2]}", string.Empty);
                //        recipeChamber += "<" + temp;
                //    }
                //    else if (part.Contains(_selectedChamberSuffix))
                //    {

                //        string temp = part.Replace(_selectedChamberSuffix, string.Empty);
                //        recipeChamber += "<" + temp;
                //    }
                //}

                //if (parts.Length > 2)
                //{
                //    recipeChamber += "<" + parts[parts.Length - 1];
                //}

                var recipesChamber = _recipeProvider.GetXmlRecipeList(prefix);
                type.FileListByProcessType = RecipeSequenceTreeBuilder.BuildFileNode(prefix, "", false, recipesChamber)[0].Files;
                ProcessTypeFileList.Add(type);
            }

            while (ProcessTypeFileList.Count > RecipeProcessTypeDic[SelectedChamber].Count)
            {
                ProcessTypeFileList.RemoveAt(0);
            }

            //for (int i = 0; i < ProcessTypeFileList.Count; i++)
            //{
            //    for (int j = ProcessTypeFileList[i].FileListByProcessType.Count - 1; j >= 0; j--)
            //    {
            //        if (!ProcessTypeFileList[i].FileListByProcessType[j].Name.Contains(selectedChamber))
            //            ProcessTypeFileList[i].FileListByProcessType.RemoveAt(j);
            //    }
            //}
        }

        public string _selectedChamberSuffix => $".{SelectedChamber}";
        public string CurrentChamberType
        {
            get
            {
                return ChamberType[ChamberTypeIndexSelection];
            }
        }
        public int ChamberTypeIndexSelection { get; set; }
        public int ChamberNameIndexSelection { get; set; }
        private RecipeProvider _recipeProvider = new RecipeProvider();
        public ObservableCollection<string> ChamberType { get; set; }
        public Dictionary<string, string> DicPMChamberType { get; set; }
        public Dictionary<string, ObservableCollection<string>> RecipeProcessTypeDic { get; set; }
        public ObservableCollection<string> Chambers { get; set; }
        public ObservableCollection<string> Processes { get; set; }
        public string _selectedChamber;
        public string SelectedChamber
        {
            get { return _selectedChamber; }
            set { _selectedChamber = value; NotifyOfPropertyChange("SelectedChamber"); }
        }
        public string _chamberSuffix => Chambers.Count > 2 ? $".{Chambers[1]}" + $".{Chambers[2]}" : string.Empty;
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
            if (this.currentFileNode != null)
            {
                if (this.currentFileNode.IsFile)
                {
                    this.DialogResult = currentFileNode.PrefixPath + "\\" + currentFileNode.FullPath;
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


        public void Browser()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Select source file";
            openFileDialog.Filter = string.Format("rcp|*.rcp");
            openFileDialog.FileName = string.Empty;
            openFileDialog.DefaultExt = "xml";
            if (openFileDialog.ShowDialog() == false)
            {
                return;
            }
            string xmlFile = openFileDialog.FileName;
            this.DialogResult = xmlFile;
            TryClose(true);
        }
    }
}
