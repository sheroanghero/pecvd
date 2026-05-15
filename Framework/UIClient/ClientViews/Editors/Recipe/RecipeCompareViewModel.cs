using Aitex.Core.RT.Log;
using Caliburn.Micro;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.Editors.Recipe;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    public class CompareItem : NotifiableItem
    {
        public bool IsDiff { get; set; }
        public bool IsDiffName { get; set; }
        public bool IsExtra { get; set; }

        public bool IsHidden { get; set; }

        public string Background
        {
            get
            {
                if (IsDiff)
                    return "Tomato";

                if (IsExtra)
                    return "Gold";

                return "White";
            }
        }

        public Visibility CopyVisibility
        {
            get
            {
                return IsDiff || IsExtra ? Visibility.Visible : Visibility.Hidden;
            }
        }
    }

    public class StepDataItem : CompareItem
    {
        public string StepNumber { get; set; }
        public string StepName { get; set; }
    }

    public class ParamDataItem : CompareItem
    {
        public string ParamName { get; set; }
        public string ParamValue { get; set; }
    }

    public class LineDataItem : CompareItem
    {
        public string LineNumber { get; set; }
        public string LineText { get; set; }
    }

    public class RecipeCompareViewModel : ModuleUiViewModelBase
    {
        private string _module = "PM1";
        public ObservableCollection<ProcessTypeFileItem> ProcessTypeFileList { get; set; }

        public ObservableCollection<string> ChamberType { get; set; }
        public ObservableCollection<string> RecipeProcessType { get; set; }
        public int ChamberTypeIndexSelection { get; set; }
        public int _processTypeIndexSelection;


        bool _useChamberTypeName = false;

        public int ProcessTypeIndexSelection
        {
            get { return _processTypeIndexSelection; }
            set { _processTypeIndexSelection = value; NotifyOfPropertyChange("ProcessTypeIndexSelection"); }
        }

        public string CurrentChamberType
        {
            get
            {
                return ChamberType[ChamberTypeIndexSelection];
            }
        }

        private string _currentProcessType;
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
            set { _currentProcessType = CurrentProcessType; NotifyOfPropertyChange("CurrentProcessType"); }
        }

        //-------------------------A Properties
        public ObservableCollection<StepDataItem> StepListA { get; set; }
        public ObservableCollection<ParamDataItem> ParamListA { get; set; }
        public ObservableCollection<LineDataItem> WholeListA { get; set; }
        public string RecipeA { get; set; }
        private XmlDocument _domA = new XmlDocument();
        private string _pathPrefixA;
        private Dictionary<string, ObservableCollection<ParamDataItem>> _mapStepParamA = new Dictionary<string, ObservableCollection<ParamDataItem>>();
        private Dictionary<string, LineDataItem> _mapLineTextA = new Dictionary<string, LineDataItem>();

        private List<string> BackInnerXmlTextA = new List<string>();

        private bool _isChangedA { get; set; }

        public bool EnableButtonRemoveA
        {
            get { return !string.IsNullOrEmpty(RecipeA); }
        }
        public bool EnableButtonUndoA
        {
            get { return false; }
        }
        public bool EnableButtonSaveA
        {
            get { return false; }
        }

        private StepDataItem _stepSelectionA;
        public StepDataItem StepSelectionA
        {
            get
            {
                return _stepSelectionA;
            }
            set
            {
                SyncStepSelection(value, false);
                _stepSelectionA = value;
            }
        }

        private ParamDataItem _paramSelectionA;
        public ParamDataItem ParamSelectionA
        {
            get
            {
                return _paramSelectionA;
            }
            set
            {
                SyncParamSelection(value, false);
                _paramSelectionA = value;
            }
        }

        private LineDataItem _lineSelectionA;
        public LineDataItem LineSelectionA
        {
            get
            {
                return _lineSelectionA;
            }
            set
            {
                SyncLineSelection(value, false);
                _lineSelectionA = value;
            }
        }

        //-------------------------B Properties-------------------------------------------------------------
        public ObservableCollection<StepDataItem> StepListB { get; set; }
        public ObservableCollection<ParamDataItem> ParamListB { get; set; }
        public ObservableCollection<LineDataItem> WholeListB { get; set; }
        public string RecipeB { get; set; }
        private XmlDocument _domB = new XmlDocument();
        private string _pathPrefixB;
        private Dictionary<string, ObservableCollection<ParamDataItem>> _mapStepParamB = new Dictionary<string, ObservableCollection<ParamDataItem>>();
        private Dictionary<string, LineDataItem> _mapLineTextB = new Dictionary<string, LineDataItem>();

        private List<string> BackInnerXmlTextB = new List<string>();
        private string BaseTextB = "";

        private bool _isChangedB { get; set; }

        public bool EnableButtonRemoveB
        {
            get { return !string.IsNullOrEmpty(RecipeB); }
        }
        public bool EnableButtonUndoB
        {
            get { return false; }
        }
        public bool EnableButtonSaveB
        {
            get { return false; }
        }

        private StepDataItem _stepSelectionB;
        public StepDataItem StepSelectionB
        {
            get
            {
                return _stepSelectionB;
            }
            set
            {
                SyncStepSelection(value, true);
                _stepSelectionB = value;
            }
        }

        private ParamDataItem _paramSelectionB;
        public ParamDataItem ParamSelectionB
        {
            get
            {
                return _paramSelectionB;
            }
            set
            {
                SyncParamSelection(value, true);
                _paramSelectionB = value;
            }
        }

        private LineDataItem _lineSelectionB;
        public LineDataItem LineSelectionB
        {
            get
            {
                return _lineSelectionB;
            }
            set
            {
                SyncLineSelection(value, true);
                _lineSelectionB = value;
            }
        }

        public RecipeCompareViewModel()
        {
            StepListA = new ObservableCollection<StepDataItem>();
            ParamListA = new ObservableCollection<ParamDataItem>();
            WholeListA = new ObservableCollection<LineDataItem>();

            StepListB = new ObservableCollection<StepDataItem>();
            ParamListB = new ObservableCollection<ParamDataItem>();
            WholeListB = new ObservableCollection<LineDataItem>();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            var chamberType = _useChamberTypeName? QueryDataClient.Instance.Service.GetConfig("System.Recipe.SupportedChamberType") : QueryDataClient.Instance.Service.GetConfig("System.Recipe.ChamberModules");
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


            UpdateProcessTypeFileList();
        }

        protected override void OnActivate()
        {
            base.OnActivate();
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
        }

        public void UpdateProcessTypeFileList()
        {
            ProcessTypeFileList.Clear();
            for (int i = 0; i < RecipeProcessType.Count; i++)
            {
                var type = new ProcessTypeFileItem();
                type.ProcessType = RecipeProcessType[i];
                var prefix = $"{ChamberType[ChamberTypeIndexSelection]}\\{RecipeProcessType[i]}";

                ProcessTypeFileList.Add(type);
            }

            while (ProcessTypeFileList.Count > RecipeProcessType.Count)
            {
                ProcessTypeFileList.RemoveAt(0);
            }
        }

        public void SelectA()
        {
            SelectRecipe(true);
        }

        public void SelectB()
        {
            SelectRecipe(false);
        }

        private void SelectRecipe(bool isSelectA)
        {
            try
            {
                RecipeCompareSelectDialogViewModel dialog = new RecipeCompareSelectDialogViewModel();
                dialog.DisplayName = "Select Recipe";
                SystemName = _module;

                var recipeProvider = new RecipeProvider();
                var processType = QueryDataClient.Instance.Service.GetConfig("System.Recipe.SupportedProcessType");
                if (processType == null)
                {
                    processType = "Process";
                }

                var ProcessTypeFileList = new ObservableCollection<ProcessTypeFileItem>();
                string[] recipeProcessType = ((string)processType).Split(',');

                string[] partsType1 = new string[] { };
                string[] partsType2 = new string[] { };

                for (int i = 0; i < recipeProcessType.Length; i++)
                {
                    var type = new ProcessTypeFileItem();
                    type.ProcessType = recipeProcessType[i];
                    var prefix = $"{ChamberType[ChamberTypeIndexSelection]}\\{recipeProcessType[i]}";
                    var recipes = recipeProvider.GetXmlRecipeList(prefix);

                    string[] parts = Regex.Split(recipes, "<");
                    if (i == 0)
                    {
                        partsType1 = parts;
                    }
                    else if (i == 1)
                    {
                        partsType2 = parts;
                    }


                    string recipeChamber;
                    recipeChamber = "<" + parts[1];
                    foreach (string part in parts)
                    {
                        if (part.Contains($".{SystemName}"))
                        {

                            string temp = part.Replace($".{SystemName}", string.Empty);
                            recipeChamber += "<" + temp;
                        }
                    }

                    if (parts.Length > 2)
                    {
                        recipeChamber += "<" + parts[parts.Length - 1];
                    }

                    type.FileListByProcessType = RecipeSequenceTreeBuilder.BuildFileNode(prefix, "", false, recipeChamber)[0].Files;
                    ProcessTypeFileList.Add(type);
                }

                dialog.ProcessTypeFileList = ProcessTypeFileList;

                WindowManager wm = new WindowManager();
                bool? bret = wm.ShowDialog(dialog);
                if (!(bool)bret)
                    return;

                _module = dialog.SelectedChamber;
                string suffix = $".{_module}";

                string dialogResult = dialog.DialogResult;
                bool isFullPath = false;
                if (dialogResult.Contains(".rcp"))
                    isFullPath = true;
                string recipeName = dialogResult;
                if (isSelectA)
                {
                    RecipeA = recipeName;
                    NotifyOfPropertyChange(nameof(RecipeA));
                    NotifyOfPropertyChange(nameof(EnableButtonRemoveA));
                }
                else
                {
                    RecipeB = recipeName;
                    NotifyOfPropertyChange(nameof(RecipeB));
                    NotifyOfPropertyChange(nameof(EnableButtonRemoveB));
                }

                if (!string.IsNullOrEmpty(RecipeA) && !string.IsNullOrEmpty(RecipeB))
                {
                    string[] recipe1 = RecipeA.Split('\\'); string r1 = recipe1[0];
                    string[] recipe2 = RecipeB.Split('\\'); string r2 = recipe2[0];
                    if (r1 != r2)
                    {
                        if (MessageBoxResult.Yes != MessageBox.Show($"腔体类型不一致! \n 是否继续比较 ?", "", MessageBoxButton.YesNo, MessageBoxImage.Information))
                        {
                            RemoveSelectB();
                            return;
                        }
                    }
                }

                LoadData(recipeName, isSelectA, isFullPath);

                Recompare();

                SyncShowDiffSteps(IsShowDiffSteps, IsCompareBySName);
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public void LoadData(string selectedRecipePath, bool isSelectA, bool isFullPath)
        {
            var array = selectedRecipePath.Split(new char[] { '\\' });

            string recipeName = array[array.Length - 1];

            XmlDocument doc = isSelectA ? _domA : _domB;

            string prefixPath = (isSelectA ? RecipeA : RecipeB).Replace(recipeName, "");

            var _recipeProvider = new RecipeProvider();
            var recipeContent = isFullPath ? _recipeProvider.LoadRecipeByFullPath(selectedRecipePath) : _recipeProvider.LoadRecipe(prefixPath, recipeName);



            if (string.IsNullOrEmpty(recipeContent))
            {
                MessageBox.Show($"{prefixPath}\\{recipeName} is empty, please confirm the file is valid.");
                return;
            }

            if (isSelectA)
            {
                BackInnerXmlTextA.Clear();

                if (BackInnerXmlTextB.Count > 0)
                {
                    string dataXml = BackInnerXmlTextB[0];
                    BackInnerXmlTextB.Clear();
                    BackInnerXmlTextB.Add(dataXml);
                }

            }
            else
            {
                BackInnerXmlTextB.Clear();

                if (BackInnerXmlTextA.Count > 0)
                {
                    string dataXml = BackInnerXmlTextA[0];
                    BackInnerXmlTextA.Clear();
                    BackInnerXmlTextA.Add(dataXml);
                }
            }
            if (isSelectA)
            {
                _pathPrefixA = prefixPath;
                _mapLineTextA = new Dictionary<string, LineDataItem>();
                BackInnerXmlTextA.Add(recipeContent);
            }
            else
            {
                _pathPrefixB = prefixPath;
                _mapLineTextB = new Dictionary<string, LineDataItem>();
                BackInnerXmlTextB.Add(recipeContent);
            }



            LoadrecipeContentData(recipeContent, isSelectA);
        }


        public void StepGridSelectionChangedA()
        {
            if (StepSelectionA == null)
            {
                ParamListA = null;
                NotifyOfPropertyChange(nameof(ParamListA));
                return;
            }

            if (_mapStepParamA.ContainsKey(StepSelectionA.StepNumber))
            {
                ParamListA = _mapStepParamA[StepSelectionA.StepNumber];
                foreach (ParamDataItem item in ParamListA)
                {
                    item.IsHidden = (!item.IsDiff && !item.IsExtra && IsShowDiffParams) ? true : false;
                }
                NotifyOfPropertyChange(nameof(ParamListA));
                NotifyOfPropertyChange(nameof(StepSelectionA));
                StepSelectionA.InvokePropertyChanged();
            }
        }

        public void LoadDataByRecipeContent(string recipeContent, bool isSelectA)
        {
            LoadrecipeContentData(recipeContent, isSelectA);
        }

        private void LoadrecipeContentData(string recipeContent, bool isSelectA)
        {
            XmlDocument doc = isSelectA ? _domA : _domB;

            if (isSelectA)
            {
                _mapLineTextA = new Dictionary<string, LineDataItem>();
                //BackInnerXmlTextA.Add(recipeContent);
            }
            else
            {
                _mapLineTextB = new Dictionary<string, LineDataItem>();
                //BackInnerXmlTextB.Add(recipeContent);
            }

            string[] allText = recipeContent.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            int number = 0;
            ObservableCollection<LineDataItem> lineData = new ObservableCollection<LineDataItem>();
            foreach (string lineText in allText)
            {
                LineDataItem line = new LineDataItem();
                line.LineNumber = (++number).ToString();
                line.LineText = lineText;
                if (!string.IsNullOrEmpty(lineText))
                {
                    lineData.Add(line);
                    if (isSelectA)
                    {
                        _mapLineTextA[line.LineNumber] = line;
                    }
                    else
                    {
                        _mapLineTextB[line.LineNumber] = line;
                    }
                }
            }

            doc.LoadXml(recipeContent);

            ObservableCollection<StepDataItem> stepData = new ObservableCollection<StepDataItem>();

            XmlNodeList nodeSteps = doc.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{_module}']/Step");
            if (nodeSteps == null)
                nodeSteps = doc.SelectNodes($"Aitex/TableRecipeData/Step");


            foreach (XmlNode nodeStep in nodeSteps)
            {
                ObservableCollection<ParamDataItem> paramData = new ObservableCollection<ParamDataItem>();
                StepDataItem step = new StepDataItem();

                foreach (XmlAttribute attr in nodeStep.Attributes)
                {
                    if (attr.Name == "StepNo")
                        step.StepNumber = attr.Value;
                    else if (attr.Name == "Name")
                        step.StepName = attr.Value;
                    else
                    {
                        ParamDataItem param = new ParamDataItem();
                        param.ParamName = attr.Name;
                        param.ParamValue = attr.Value;
                        paramData.Add(param);
                    }
                }
                stepData.Add(step);

                if (isSelectA)
                {
                    _mapStepParamA[step.StepNumber] = paramData;
                }
                else
                {
                    _mapStepParamB[step.StepNumber] = paramData;
                }
            }

            if (isSelectA)
            {
                StepListA = stepData;
                NotifyOfPropertyChange(nameof(StepListA));
                WholeListA = lineData;
                NotifyOfPropertyChange(nameof(WholeListA));
            }
            else
            {
                StepListB = stepData;
                NotifyOfPropertyChange(nameof(StepListB));
                WholeListB = lineData;
                NotifyOfPropertyChange(nameof(WholeListB));
            }
        }

        public void StepGridSelectionChangedB()
        {
            if (StepSelectionB == null)
            {
                ParamListB = null;
                NotifyOfPropertyChange(nameof(ParamListB));
                return;
            }

            if (_mapStepParamB.ContainsKey(StepSelectionB.StepNumber))
            {
                ParamListB = _mapStepParamB[StepSelectionB.StepNumber];
                foreach (ParamDataItem item in ParamListB)
                {
                    item.IsHidden = (!item.IsDiff && !item.IsExtra && IsShowDiffParams) ? true : false;
                }
                NotifyOfPropertyChange(nameof(ParamListB));
                NotifyOfPropertyChange(nameof(StepSelectionB));
                StepSelectionB.InvokePropertyChanged();
            }
        }

        public void ParamGridSelectionChangedA()
        {
            if (ParamSelectionA == null)
            {
                return;
            }

            NotifyOfPropertyChange(nameof(ParamSelectionA));
            ParamSelectionA.InvokePropertyChanged();
        }

        public void ParamGridSelectionChangedB()
        {
            if (ParamSelectionB == null)
            {
                return;
            }

            NotifyOfPropertyChange(nameof(ParamSelectionB));
            ParamSelectionB.InvokePropertyChanged();
        }

        public void WholeGridSelectionChangedA()
        {
            if (LineSelectionA == null)
            {
                return;
            }

            NotifyOfPropertyChange(nameof(LineSelectionA));
            LineSelectionA.InvokePropertyChanged();
        }

        public void WholeGridSelectionChangedB()
        {
            if (LineSelectionB == null)
            {
                return;
            }

            NotifyOfPropertyChange(nameof(LineSelectionB));
            LineSelectionB.InvokePropertyChanged();
        }

        public void SaveLineA()
        {
            if (LineSelectionA == null)
                return;
            foreach (LineDataItem lineDataItem in WholeListA)
            {
                if (lineDataItem.LineNumber == LineSelectionA.LineNumber)
                {
                    lineDataItem.LineText = LineSelectionA.LineText;
                    break;
                }
            }

            string recipeContent = "";
            for (int i = 0; i < WholeListA.Count; i++)
            {
                recipeContent += WholeListA[i].LineText + ((i == WholeListB.Count - 1) ? "" : "\r\n");
            }

            CopyToInnerXml(recipeContent, true);
            LoadDataByRecipeContent(recipeContent, true);
            Recompare();
            SyncShowDiffSteps(IsShowDiffSteps, IsCompareBySName);
        }
        public void SaveLineB()
        {
            if (LineSelectionB == null)
                return;
            foreach (LineDataItem lineDataItem in WholeListB)
            {
                if (lineDataItem.LineNumber == LineSelectionB.LineNumber)
                {
                    lineDataItem.LineText = LineSelectionB.LineText;
                    break;
                }
            }
            string recipeContent = "";

            for (int i = 0; i < WholeListB.Count; i++)
            {
                recipeContent += WholeListB[i].LineText + ((i == WholeListB.Count - 1) ? "" : "\r\n");
            }


            CopyToInnerXml(recipeContent, false);
            LoadDataByRecipeContent(recipeContent, false);
            Recompare();
            SyncShowDiffSteps(IsShowDiffSteps, IsCompareBySName);
        }

        private void Recompare()
        {
            RecompareStep();
            RecompareWhole();
        }
        private void RecompareStep()
        {
            if (StepListA == null || StepListB == null)
                return;

            int i = 0;
            for (i = 0; i < StepListA.Count && i < StepListB.Count; i++)
            {
                if (StepListA[i].StepName == StepListB[i].StepName)
                    StepListA[i].IsDiffName = StepListB[i].IsDiffName = false;
                else
                    StepListA[i].IsDiffName = StepListB[i].IsDiffName = true;

                ObservableCollection<ParamDataItem> paramA = _mapStepParamA[StepListA[i].StepNumber];
                ObservableCollection<ParamDataItem> paramB = _mapStepParamB[StepListB[i].StepNumber];
                bool isDiff = false;
                bool IsExtra = true;
                foreach (var pa in paramA)
                {
                    pa.IsExtra = true;

                }
                foreach (var pb in paramB)
                {
                    pb.IsExtra = true;
                }
                foreach (var pa in paramA)
                {
                    foreach (var pb in paramB)
                    {
                        if (pb.ParamName == pa.ParamName)
                        {
                            pb.IsExtra = false;
                            pa.IsExtra = false;
                            pb.IsDiff = pa.IsDiff = (pa.ParamValue != pb.ParamValue);
                            if (pa.IsDiff)
                                isDiff = true;
                            break;
                        }
                    }
                }

                foreach (var pa in paramA)
                {
                    pa.InvokePropertyChanged();
                }

                foreach (var pb in paramB)
                {
                    pb.InvokePropertyChanged();
                }

                StepListA[i].IsDiff = StepListB[i].IsDiff = isDiff;
                StepListA[i].IsExtra = StepListB[i].IsExtra = false;

                StepListA[i].IsHidden = StepListB[i].IsHidden = !isDiff && IsShowDiffSteps ? true : false;

                StepListA[i].InvokePropertyChanged();
                StepListB[i].InvokePropertyChanged();
            }

            for (int j = i; j < StepListA.Count; j++)
            {
                StepListA[j].IsDiff = false;
                StepListA[j].IsExtra = true;
                StepListA[j].InvokePropertyChanged();
                foreach (var pa in _mapStepParamA[StepListA[j].StepNumber])
                {
                    pa.InvokePropertyChanged();
                }
            }

            for (int k = i; k < StepListB.Count; k++)
            {
                StepListB[k].IsDiff = false;
                StepListB[k].IsExtra = true;
                WholeListB[k].InvokePropertyChanged();
                _mapLineTextB[WholeListB[k].LineNumber].InvokePropertyChanged();
                foreach (var pb in _mapStepParamB[StepListB[k].StepNumber])
                {
                    pb.InvokePropertyChanged();
                }
            }
            NotifyOfPropertyChange(nameof(StepListA));
            NotifyOfPropertyChange(nameof(StepListB));
        }

        private void RecompareWhole()
        {
            if (WholeListA == null || WholeListB == null)
                return;

            int i = 0;
            for (i = 0; i < WholeListA.Count && i < WholeListB.Count; i++)
            {
                LineDataItem lineA = _mapLineTextA[WholeListA[i].LineNumber];
                LineDataItem lineB = _mapLineTextB[WholeListB[i].LineNumber];
                bool isDiff = false;

                lineB.IsDiff = lineA.IsDiff = (lineA.LineText != lineB.LineText);
                if (lineA.IsDiff)
                    isDiff = true;

                WholeListA[i].IsDiff = WholeListB[i].IsDiff = isDiff;
                WholeListA[i].IsExtra = WholeListB[i].IsExtra = false;

                _mapLineTextA[WholeListA[i].LineNumber].IsDiff = _mapLineTextB[WholeListB[i].LineNumber].IsDiff = isDiff;
                _mapLineTextA[WholeListA[i].LineNumber].IsExtra = _mapLineTextB[WholeListB[i].LineNumber].IsExtra = false;

                lineA.InvokePropertyChanged();
                lineB.InvokePropertyChanged();
                WholeListA[i].InvokePropertyChanged();
                WholeListB[i].InvokePropertyChanged();

            }

            for (int j = i; j < WholeListA.Count; j++)
            {
                WholeListA[j].IsDiff = false;
                WholeListA[j].IsExtra = true;

                _mapLineTextA[WholeListA[j].LineNumber].IsDiff = false;
                _mapLineTextA[WholeListA[j].LineNumber].IsExtra = true;

                if (!(_mapLineTextA[WholeListA[j].LineNumber].LineText.Contains("<Step StepNo") || _mapLineTextA[WholeListA[j].LineNumber].LineText.Contains("<Moudule Name")))
                {
                    WholeListA[j].IsDiff = false;
                    WholeListA[j].IsExtra = false;
                }

                WholeListA[j].InvokePropertyChanged();
                _mapLineTextA[WholeListA[j].LineNumber].InvokePropertyChanged();
            }

            for (int k = i; k < WholeListB.Count; k++)
            {
                WholeListB[k].IsDiff = false;
                WholeListB[k].IsExtra = true;

                _mapLineTextB[WholeListB[k].LineNumber].IsDiff = false;
                _mapLineTextB[WholeListB[k].LineNumber].IsExtra = true;

                if (!(_mapLineTextB[WholeListB[k].LineNumber].LineText.Contains("<Step StepNo") || _mapLineTextB[WholeListB[k].LineNumber].LineText.Contains("<Moudule Name")))
                {
                    WholeListB[k].IsDiff = false;
                    WholeListB[k].IsExtra = false;
                }

                WholeListB[k].InvokePropertyChanged();
                _mapLineTextB[WholeListB[k].LineNumber].InvokePropertyChanged();
            }
            NotifyOfPropertyChange(nameof(WholeListA));
            NotifyOfPropertyChange(nameof(WholeListB));
        }

        private void SyncStepSelection(StepDataItem stepData, bool isSelectA)
        {
            if (stepData == null)
                return;

            if (isSelectA)
            {
                foreach (var item in StepListA)
                {
                    if (item.StepNumber == stepData.StepNumber)
                    {
                        _stepSelectionA = item;
                        NotifyOfPropertyChange(nameof(StepSelectionA));
                        StepSelectionA.InvokePropertyChanged();
                    }
                }
            }
            else
            {
                foreach (var item in StepListB)
                {
                    if (item.StepNumber == stepData.StepNumber)
                    {
                        _stepSelectionB = item;
                        NotifyOfPropertyChange(nameof(StepSelectionB));
                        StepSelectionB.InvokePropertyChanged();
                    }
                }
            }
        }

        private void SyncParamSelection(ParamDataItem paramData, bool isSelectA)
        {
            try
            {
                if (paramData == null)
                    return;

                if (isSelectA)
                {
                    if (_mapStepParamA.ContainsKey(StepSelectionB.StepNumber))
                    {
                        ParamListA = _mapStepParamA[StepSelectionB.StepNumber];
                        NotifyOfPropertyChange(nameof(ParamListA));
                    }
                    else
                        return;
                    foreach (var item in ParamListA)
                    {
                        if (item.ParamName == paramData.ParamName)
                        {
                            _paramSelectionA = item;
                            NotifyOfPropertyChange(nameof(ParamSelectionA));
                            ParamSelectionA.InvokePropertyChanged();
                        }
                    }
                }
                else
                {
                    if (ParamListB == null)
                    {
                        if (_mapStepParamB.ContainsKey(StepSelectionA.StepNumber))
                        {
                            ParamListB = _mapStepParamB[StepSelectionA.StepNumber];
                            NotifyOfPropertyChange(nameof(ParamListB));
                        }
                        else
                            return;
                    }
                    foreach (var item in ParamListB)
                    {
                        if (item.ParamName == paramData.ParamName)
                        {
                            _paramSelectionB = item;
                            NotifyOfPropertyChange(nameof(ParamSelectionB));
                            ParamSelectionB.InvokePropertyChanged();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex.Message);
            }
        }

        private void SyncLineSelection(LineDataItem lineData, bool isSelectA)
        {
            if (lineData == null)
                return;

            if (isSelectA)
            {
                foreach (var item in WholeListA)
                {
                    if (item.LineNumber == lineData.LineNumber)
                    {
                        _lineSelectionA = item;
                        NotifyOfPropertyChange(nameof(LineSelectionA));
                        LineSelectionA.InvokePropertyChanged();
                    }
                }
            }
            else
            {
                foreach (var item in WholeListB)
                {
                    if (item.LineNumber == lineData.LineNumber)
                    {
                        _lineSelectionB = item;
                        NotifyOfPropertyChange(nameof(LineSelectionB));
                        LineSelectionB.InvokePropertyChanged();
                    }
                }
            }
        }

        public void RemoveA()
        {
            RemoveRecipe(true);
        }

        public void RemoveB()
        {
            RemoveRecipe(false);
        }

        private void RemoveSelectB()
        {
            StepListB?.Clear();
            ParamListB?.Clear();
            WholeListB?.Clear();
            RecipeB = string.Empty;
            _mapStepParamB?.Clear();
            _stepSelectionB = null;
            _paramSelectionB = null;

            NotifyOfPropertyChange(nameof(StepListB));
            NotifyOfPropertyChange(nameof(ParamListB));
            NotifyOfPropertyChange(nameof(RecipeB));
            NotifyOfPropertyChange(nameof(EnableButtonRemoveB));
            NotifyOfPropertyChange(nameof(StepSelectionB));
            NotifyOfPropertyChange(nameof(ParamSelectionB));
        }

        private void RemoveRecipe(bool isSelectA)
        {
            if (!DialogBox.Confirm($"Are you sure you want to remove the recipe? \r\n{RecipeB}"))
                return;

            if (isSelectA)
            {
                StepListA?.Clear();
                ParamListA?.Clear();
                WholeListA?.Clear();
                RecipeA = string.Empty;
                _mapStepParamA?.Clear();
                _stepSelectionA = null;
                _paramSelectionA = null;

                NotifyOfPropertyChange(nameof(StepListA));
                NotifyOfPropertyChange(nameof(ParamListA));
                NotifyOfPropertyChange(nameof(RecipeA));
                NotifyOfPropertyChange(nameof(EnableButtonRemoveA));
                NotifyOfPropertyChange(nameof(StepSelectionA));
                NotifyOfPropertyChange(nameof(ParamSelectionA));
            }
            else
            {
                StepListB?.Clear();
                ParamListB?.Clear();
                WholeListB?.Clear();
                RecipeB = string.Empty;
                _mapStepParamB?.Clear();
                _stepSelectionB = null;
                _paramSelectionB = null;

                NotifyOfPropertyChange(nameof(StepListB));
                NotifyOfPropertyChange(nameof(ParamListB));
                NotifyOfPropertyChange(nameof(RecipeB));
                NotifyOfPropertyChange(nameof(EnableButtonRemoveB));
                NotifyOfPropertyChange(nameof(StepSelectionB));
                NotifyOfPropertyChange(nameof(ParamSelectionB));
            }
        }

        public void StepCopyToRight(StepDataItem stepA)
        {
            string temp = "";
            if (StepSelectionA != null)
                temp = StepSelectionA.StepNumber;
            StepCopy(stepA, true);

            if (!string.IsNullOrEmpty(temp))
            {
                foreach (StepDataItem tempStep in StepListA)
                {
                    if (tempStep.StepNumber == temp)
                    {
                        StepSelectionA = tempStep;
                        break;
                    }
                }
            }
        }

        public void StepCopyToLeft(StepDataItem stepB)
        {
            string temp = "";
            if (StepSelectionB != null)
                temp = StepSelectionB.StepNumber;
            StepCopy(stepB, false);

            if (!string.IsNullOrEmpty(temp))
            {
                foreach (StepDataItem tempStep in StepListB)
                {
                    if (tempStep.StepNumber == temp)
                    {
                        StepSelectionB = tempStep;
                        break;
                    }
                }
            }
        }

        public void LeftDelete(StepDataItem step)
        {
            StepDelete(step, true);
        }

        public void RightDelete(StepDataItem step)
        {
            StepDelete(step, false);
        }

        private void StepDelete(StepDataItem step, bool isSelectA)
        {
            ObservableCollection<StepDataItem> stepList = isSelectA ? StepListA : StepListB;
            Dictionary<string, ObservableCollection<ParamDataItem>> _mapStepParam = isSelectA ? _mapStepParamA : _mapStepParamB;
            foreach (var stepTemp in stepList)
            {
                if (step.StepNumber != stepTemp.StepNumber)
                    continue;

                stepList.Remove(stepTemp);
                break;
            }

            for (int i = 0; i < stepList.Count; i++)
            {
                if (stepList[i].StepNumber != (i + 1).ToString())
                {
                    if (_mapStepParam.ContainsKey((i + 1).ToString()))
                    {
                        _mapStepParam[(i + 1).ToString()] = _mapStepParam[stepList[i].StepNumber];
                    }
                    stepList[i].StepNumber = (i + 1).ToString();
                }
            }

            DeleteInnerXml(isSelectA);
            Recompare();
            SyncShowDiffSteps(IsShowDiffSteps, IsCompareBySName);
        }

        private void DeleteInnerXml(bool isSelectA)
        {
            try
            {
                XmlDocument docTo = isSelectA ? _domA : _domB;
                //XmlNode nodeModule = docTo.SelectSingleNode($"Aitex/TableRecipeData/Module[@Name='{_module}']");

                XmlNodeList nodeSteps = docTo.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{_module}']/Step");
                if (nodeSteps == null)
                    nodeSteps = docTo.SelectNodes($"Aitex/TableRecipeData/Step");
                List<XmlNode> oldNodeSteps = new List<XmlNode>();
                foreach (XmlNode nodeTemp in nodeSteps)
                {
                    oldNodeSteps.Add(nodeTemp.Clone());
                }

                XmlNodeList backSteps = docTo.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{_module}']/BakeStep");
                if (backSteps == null)
                    backSteps = docTo.SelectNodes($"Aitex/TableRecipeData/BakeStep");
                List<XmlNode> oldBackNodeSteps = new List<XmlNode>();
                foreach (XmlNode nodeTemp in backSteps)
                {
                    oldBackNodeSteps.Add(nodeTemp.Clone());
                }

                XmlNode stepsNode = docTo.SelectSingleNode($"Aitex/TableRecipeData/Module[@Name='{_module}']/Step").ParentNode;

                stepsNode.RemoveAll();
                (stepsNode as XmlElement).SetAttribute("Name", _module);

                ObservableCollection<StepDataItem> stepListTo = isSelectA ? StepListA : StepListB;

                if (stepsNode == null)
                {
                    return;
                }

                int stepNoCount = 1;
                foreach (StepDataItem item in stepListTo)
                {
                    XmlElement DeviceTree = docTo.CreateElement("Step");
                    DeviceTree.SetAttribute("StepNo", (stepNoCount++).ToString());
                    DeviceTree.SetAttribute("Name", item.StepName);
                    stepsNode.AppendChild(DeviceTree);
                }

                foreach (XmlNode nodeStep in stepsNode)
                {
                    string stepNumber = nodeStep.Attributes["StepNo"].Value;

                    ObservableCollection<ParamDataItem> paramList =
                        isSelectA ? _mapStepParamA[stepNumber] : _mapStepParamB[stepNumber];
                    foreach (var param in paramList)
                    {
                        (nodeStep as XmlElement).SetAttribute(param.ParamName, param.ParamValue);
                    }
                }

                foreach (XmlNode nodeStep in backSteps)
                {
                    stepsNode.AppendChild(nodeStep);
                }

                string backText = getXmlText(isSelectA ? _domA : _domB);
                LoadDataByRecipeContent(backText, isSelectA);

                Recompare();

                SyncShowDiffSteps(IsShowDiffSteps, IsCompareBySName);
                if (isSelectA)
                {
                    BackInnerXmlTextA.Add(backText);
                }
                else
                {
                    BackInnerXmlTextB.Add(backText);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex.Message);
            }
        }

        private void StepCopy(StepDataItem stepData, bool isFromA)
        {
            StepDataItem stepFrom = stepData;
            ObservableCollection<StepDataItem> stepListTo = isFromA ? StepListB : StepListA;
            Dictionary<string, ObservableCollection<ParamDataItem>> mapFrom = isFromA ? _mapStepParamA : _mapStepParamB;
            Dictionary<string, ObservableCollection<ParamDataItem>> mapTo = isFromA ? _mapStepParamB : _mapStepParamA;

            bool isNotOverOf = false;
            foreach (var stepTo in stepListTo)
            {
                if (stepTo.StepNumber != stepFrom.StepNumber)
                    continue;

                isNotOverOf = true;
                stepTo.StepName = stepFrom.StepName;
                //stepTo.IsDiff = stepFrom.IsDiff = false;
                //stepTo.IsExtra = stepFrom.IsExtra = false;

                if (mapFrom.ContainsKey(stepFrom.StepNumber) &&
                    mapTo.ContainsKey(stepTo.StepNumber))
                {
                    foreach (var paramFrom in mapFrom[stepFrom.StepNumber])
                    {
                        foreach (var paramTo in mapTo[stepFrom.StepNumber])
                        {
                            if (paramTo.ParamName != paramFrom.ParamName)
                                continue;

                            paramTo.ParamValue = paramFrom.ParamValue;
                            break;
                        }
                    }
                }
                break;
            }
            if (!isNotOverOf)
            {
                StepDataItem stepDataItem = new StepDataItem();
                stepDataItem.StepName = stepFrom.StepName;
                stepDataItem.StepNumber = (stepListTo.Count + 1).ToString();
                stepListTo.Add(stepDataItem);
                ObservableCollection<ParamDataItem> paramDataItems = new ObservableCollection<ParamDataItem>();
                foreach (var paramFrom in mapFrom[stepFrom.StepNumber])
                {
                    ParamDataItem paramDataItem = new ParamDataItem();
                    paramDataItem.ParamName = paramFrom.ParamName;
                    paramDataItem.ParamValue = paramFrom.ParamValue;
                    paramDataItems.Add(paramDataItem);
                }
                if (mapTo.ContainsKey(stepDataItem.StepNumber))
                    mapTo[stepDataItem.StepNumber] = paramDataItems;
                else
                    mapTo.Add(stepDataItem.StepNumber, paramDataItems);
            }
            CopyToInnerXml(!isFromA);
            Recompare();
            SyncShowDiffSteps(IsShowDiffSteps, IsCompareBySName);

            SyncStepSelection(stepFrom, !isFromA);
        }


        private void CopyToInnerXml(bool isSelectA)
        {
            try
            {
                XmlDocument docTo = isSelectA ? _domA : _domB;
                XmlNode nodeModule = docTo.SelectSingleNode($"Aitex/TableRecipeData/Module[@Name='{_module}']");
                XmlNodeList nodeSteps = docTo.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{_module}']/Step");
                if (nodeSteps == null)
                    nodeSteps = docTo.SelectNodes($"Aitex/TableRecipeData/Step");
                List<XmlNode> oldNodeSteps = new List<XmlNode>();
                foreach (XmlNode nodeTemp in nodeSteps)
                {
                    oldNodeSteps.Add(nodeTemp.Clone());
                }

                XmlNodeList backSteps = docTo.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{_module}']/BakeStep");
                if (backSteps == null)
                    backSteps = docTo.SelectNodes($"Aitex/TableRecipeData/BakeStep");
                List<XmlNode> oldBackNodeSteps = new List<XmlNode>();
                foreach (XmlNode nodeTemp in backSteps)
                {
                    oldBackNodeSteps.Add(nodeTemp.Clone());
                }

                ObservableCollection<StepDataItem> stepListTo = isSelectA ? StepListA : StepListB;

                if (nodeModule == null)
                {
                    return;
                }

                foreach (StepDataItem item in stepListTo)
                {
                    bool isOverOf = true;
                    foreach (XmlNode xmlNode in oldNodeSteps)
                    {
                        string stepNumber = xmlNode.Attributes["StepNo"].Value;
                        if (item.StepNumber == stepNumber)
                        {
                            isOverOf = false;
                            break;
                        }
                    }

                    if (isOverOf)
                    {
                        XmlElement DeviceTree = docTo.CreateElement("Step");
                        DeviceTree.SetAttribute("StepNo", (oldNodeSteps.Count + 1).ToString());
                        DeviceTree.SetAttribute("Name", item.StepName);
                        nodeModule.AppendChild(DeviceTree);
                    }
                }

                nodeSteps = docTo.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{_module}']/Step");

                foreach (XmlNode nodeStep in nodeSteps)
                {
                    string stepNumber = nodeStep.Attributes["StepNo"].Value;

                    ObservableCollection<ParamDataItem> paramList =
                        isSelectA ? _mapStepParamA[stepNumber] : _mapStepParamB[stepNumber];
                    foreach (var param in paramList)
                    {
                        (nodeStep as XmlElement).SetAttribute(param.ParamName, param.ParamValue);
                    }
                }

                foreach (XmlNode nodeStep in backSteps)
                {
                    nodeModule.AppendChild(nodeStep);
                }

                string backText = getXmlText(isSelectA ? _domA : _domB);
                LoadDataByRecipeContent(backText, isSelectA);

                Recompare();

                SyncShowDiffSteps(IsShowDiffSteps, IsCompareBySName);
                if (isSelectA)
                {
                    BackInnerXmlTextA.Add(backText);
                }
                else
                {
                    BackInnerXmlTextB.Add(backText);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex.Message);
            }
        }

        private string getXmlText(XmlDocument xmlDocument)
        {
            (new RecipeProvider()).SaveRecipe("", "RecipeTemp", xmlDocument.InnerXml);

            var _recipeProvider = new RecipeProvider();
            var recipeContent = _recipeProvider.LoadRecipe("", "RecipeTemp");
            return recipeContent;
        }

        public void ParamCopyToRight(ParamDataItem paramData)
        {
            string temp = "";
            if (StepSelectionA != null)
                temp = StepSelectionA.StepNumber;
            ParamCopy(paramData, true);
            if (!string.IsNullOrEmpty(temp))
            {
                foreach (StepDataItem tempStep in StepListA)
                {
                    if (tempStep.StepNumber == temp)
                    {
                        StepSelectionA = tempStep;
                        break;
                    }
                }
            }
        }

        public void ParamCopyToLeft(ParamDataItem paramData)
        {
            string temp = "";
            if (StepSelectionB != null)
                temp = StepSelectionB.StepNumber;
            ParamCopy(paramData, false);
            if (!string.IsNullOrEmpty(temp))
            {
                foreach (StepDataItem tempStep in StepListB)
                {
                    if (tempStep.StepNumber == temp)
                    {
                        StepSelectionB = tempStep;
                        break;
                    }
                }
            }
        }

        private void ParamCopy(ParamDataItem paramData, bool isFromA)
        {
            ParamDataItem paramFrom = paramData;
            StepDataItem stepFrom = isFromA ? StepSelectionA : StepSelectionB;
            Dictionary<string, ObservableCollection<ParamDataItem>> mapFrom = isFromA ? _mapStepParamA : _mapStepParamB;
            Dictionary<string, ObservableCollection<ParamDataItem>> mapTo = isFromA ? _mapStepParamB : _mapStepParamA;


            if (mapTo.ContainsKey(stepFrom.StepNumber))
            {
                foreach (var paramTo in mapTo[stepFrom.StepNumber])
                {
                    if (paramTo.ParamName != paramFrom.ParamName)
                        continue;

                    paramTo.ParamValue = paramFrom.ParamValue;

                    paramTo.IsDiff = paramFrom.IsDiff = false;
                    paramTo.IsExtra = paramFrom.IsExtra = false;
                    paramFrom.InvokePropertyChanged();
                    paramTo.InvokePropertyChanged();
                    break;
                }
            }

            bool isDiff = false;
            foreach (var paramListFrom in mapFrom[stepFrom.StepNumber])
            {
                if (paramListFrom.IsDiff)
                {
                    isDiff = true;
                    break;
                }
            }
            stepFrom.IsDiff = isDiff;
            stepFrom.InvokePropertyChanged();

            ObservableCollection<StepDataItem> stepListTo = isFromA ? StepListB : StepListA;
            foreach (var stepTo in stepListTo)
            {
                if (stepTo.StepNumber == stepFrom.StepNumber)
                {
                    isDiff = false;
                    foreach (var paramListTo in mapTo[stepTo.StepNumber])
                    {
                        if (paramListTo.IsDiff)
                        {
                            isDiff = true;
                            break;
                        }
                    }
                    stepTo.IsDiff = isDiff;
                    stepTo.InvokePropertyChanged();
                    break;
                }
            }

            CopyToInnerXml(!isFromA);
        }

        public void LineCopyToLeft(LineDataItem lineData)
        {
            LineCopy(lineData, false);
        }

        public void LineCopyToRight(LineDataItem lineData)
        {
            LineCopy(lineData, true);
        }

        public void LineCopy(LineDataItem lineData, bool isFromA)
        {
            try
            {
                LineDataItem lineFrom = lineData;
                ObservableCollection<LineDataItem> lineListTo = isFromA ? WholeListB : WholeListA;

                Dictionary<string, LineDataItem> mapFrom = isFromA ? _mapLineTextA : _mapLineTextB;
                Dictionary<string, LineDataItem> mapTo = isFromA ? _mapLineTextB : _mapLineTextA;

                foreach (var lineTo in lineListTo)
                {
                    if (lineTo.LineNumber != lineFrom.LineNumber)
                        continue;

                    lineTo.LineText = lineFrom.LineText;
                    lineTo.IsDiff = lineFrom.IsDiff = false;
                    lineTo.IsExtra = lineFrom.IsExtra = false;
                    break;
                }

                string recipeContent = "";
                if (isFromA)
                {
                    for (int i = 0; i < WholeListB.Count; i++)
                    {
                        recipeContent += WholeListB[i].LineText + ((i == WholeListB.Count - 1) ? "" : "\r\n");
                    }
                }
                else
                {
                    for (int i = 0; i < WholeListA.Count; i++)
                    {
                        recipeContent += WholeListA[i].LineText + ((i == WholeListB.Count - 1) ? "" : "\r\n");
                    }
                }

                CopyToInnerXml(recipeContent, !isFromA);
                LoadDataByRecipeContent(recipeContent, !isFromA);
                Recompare();
                SyncShowDiffSteps(IsShowDiffSteps, IsCompareBySName);
            }
            catch (Exception ex)
            {
                LOG.Write(ex.Message);
            }
        }

        private void CopyToInnerXml(string recipeContent, bool isSelectA)
        {
            XmlDocument doc = isSelectA ? _domA : _domB;
            doc.InnerXml = recipeContent;
            if (isSelectA)
            {
                BackInnerXmlTextA.Add(recipeContent);
            }
            else
            {
                BackInnerXmlTextB.Add(recipeContent);
            }
        }

        public void SaveA()
        {
            Save(true);
        }

        public void SaveB()
        {
            Save(false);
        }

        private void Save(bool isSelectA)
        {
            XmlDocument doc = isSelectA ? _domA : _domB;
            XmlNodeList nodeSteps = doc.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{_module}']/Step");
            if (nodeSteps == null)
                nodeSteps = doc.SelectNodes($"Aitex/TableRecipeData/Step");

            if (isSelectA)
            {
                (new RecipeProvider()).SaveRecipe("", RecipeA, doc.InnerXml);
            }
            else
            {
                (new RecipeProvider()).SaveRecipe("", RecipeB, doc.InnerXml);
            }
        }

        public void UndoA()
        {
            string xmlData = "";
            string temp = "";
            if (StepSelectionA != null)
                temp = StepSelectionA.StepNumber;
            for (var i = BackInnerXmlTextA.Count - 1; i >= 0;)
            {
                if (i > 0)
                {
                    xmlData = BackInnerXmlTextA[i - 1];
                    BackInnerXmlTextA.RemoveAt(i);
                }
                else
                    xmlData = BackInnerXmlTextA[0];

                break;
            }
            //_domA.InnerXml = xmlData;
            LoadDataByRecipeContent(xmlData, true);
            Recompare();
            SyncShowDiffSteps(IsShowDiffSteps, IsCompareBySName);

            if (!string.IsNullOrEmpty(temp))
            {
                foreach (StepDataItem tempStep in StepListB)
                {
                    if (tempStep.StepNumber == temp)
                    {
                        StepSelectionB = tempStep;
                        break;
                    }
                }
            }
        }

        public void UndoB()
        {
            string xmlData = "";
            string temp = "";
            if (StepSelectionA != null)
                temp = StepSelectionA.StepNumber;
            for (var i = (BackInnerXmlTextB.Count - 1); i >= 0;)
            {
                if (i > 0)
                {
                    xmlData = BackInnerXmlTextB[i - 1];
                    BackInnerXmlTextB.RemoveAt(i);
                }
                else
                    xmlData = BackInnerXmlTextB[0];
                break;
            }
            //_domB.InnerXml = xmlData;
            LoadDataByRecipeContent(xmlData, false);
            Recompare();
            SyncShowDiffSteps(IsShowDiffSteps, IsCompareBySName);

            if (!string.IsNullOrEmpty(temp))
            {
                foreach (StepDataItem tempStep in StepListA)
                {
                    if (tempStep.StepNumber == temp)
                    {
                        StepSelectionA = tempStep;
                        break;
                    }
                }
            }
        }

        private bool _isStepModel = true;
        public bool IsStepModel
        {
            get { return _isStepModel; }
            set
            {
                _isStepModel = value;
                _isWholeModel = !_isStepModel;
                StepVisibility = _isStepModel ? Visibility.Visible : Visibility.Hidden;
                WholeVisibility = _isWholeModel ? Visibility.Visible : Visibility.Hidden;
                InvokePropertyChanged(nameof(IsStepModel));
                InvokePropertyChanged(nameof(IsWholeModel));

                InvokePropertyChanged(nameof(StepVisibility));
                InvokePropertyChanged(nameof(WholeVisibility));
            }
        }


        private bool _isWholeModel;
        public bool IsWholeModel
        {
            get { return _isWholeModel; }
            set
            {
                _isWholeModel = value;
                _isStepModel = !_isWholeModel;
                StepVisibility = _isStepModel ? Visibility.Visible : Visibility.Hidden;
                WholeVisibility = _isWholeModel ? Visibility.Visible : Visibility.Hidden;
                InvokePropertyChanged(nameof(IsWholeModel));
                InvokePropertyChanged(nameof(IsStepModel));

                InvokePropertyChanged(nameof(StepVisibility));
                InvokePropertyChanged(nameof(WholeVisibility));
            }
        }

        public Visibility StepVisibility
        {
            get; set;
        }
        public Visibility WholeVisibility
        {
            get; set;
        }

        private bool _isShowDiffSteps;
        public bool IsShowDiffSteps
        {
            get { return _isShowDiffSteps; }
            set
            {
                _isShowDiffSteps = value;
                _isShowAllSteps = !_isShowDiffSteps;

                SyncShowDiffSteps(_isShowDiffSteps, _isCompareBySName);

                InvokePropertyChanged(nameof(IsShowDiffSteps));
                InvokePropertyChanged(nameof(IsShowAllSteps));
            }
        }


        private bool _isShowAllSteps = true;
        public bool IsShowAllSteps
        {
            get { return _isShowAllSteps; }
            set
            {
                _isShowAllSteps = value;
                _isShowDiffSteps = !_isShowAllSteps;

                SyncShowDiffSteps(_isShowDiffSteps, _isCompareBySName);

                InvokePropertyChanged(nameof(IsShowAllSteps));
                InvokePropertyChanged(nameof(IsShowDiffSteps));
            }
        }


        private bool _isShowDiffParams;
        public bool IsShowDiffParams
        {
            get { return _isShowDiffParams; }
            set
            {
                _isShowDiffParams = value;
                SyncShowDiffParams(_isShowDiffParams);
                _isShowAllParams = !_isShowDiffParams;
                InvokePropertyChanged(nameof(IsShowDiffParams));
                InvokePropertyChanged(nameof(IsShowAllParams));
            }
        }


        private bool _isShowAllParams = true;
        public bool IsShowAllParams
        {
            get { return _isShowAllParams; }
            set
            {
                _isShowAllParams = value;
                _isShowDiffParams = !_isShowAllParams;
                SyncShowDiffParams(_isShowDiffParams);
                InvokePropertyChanged(nameof(IsShowDiffParams));
                InvokePropertyChanged(nameof(IsShowAllParams));
            }
        }


        private bool _isCompareByStep = true;
        public bool IsCompareByStep
        {
            get { return _isCompareByStep; }
            set
            {
                _isCompareByStep = value;
                _isCompareBySName = !_isCompareByStep;
                SyncShowDiffSteps(_isShowDiffSteps, _isCompareBySName);
                InvokePropertyChanged(nameof(IsCompareByStep));
                InvokePropertyChanged(nameof(IsCompareBySName));
            }
        }


        private bool _isCompareBySName;
        public bool IsCompareBySName
        {
            get { return _isCompareBySName; }
            set
            {
                _isCompareBySName = value;
                _isCompareByStep = !_isCompareBySName;
                SyncShowDiffSteps(_isShowDiffSteps, _isCompareBySName);
                InvokePropertyChanged(nameof(IsCompareByStep));
                InvokePropertyChanged(nameof(IsCompareBySName));
            }
        }

        private void SyncShowDiffSteps(bool isShowDiffSteps, bool isCompareBySName)
        {
            if (StepListA == null || StepListB == null)
                return;

            foreach (var item in StepListA)
            {
                item.IsHidden = (IsShowDiffSteps ? (item.IsDiff == false && item.IsExtra == false) : false) || (isCompareBySName ? (item.IsDiffName || item.IsExtra == true) : false);

                item.InvokePropertyChanged();
            }
            foreach (var item in StepListB)
            {
                item.IsHidden = (IsShowDiffSteps ? (item.IsDiff == false && item.IsExtra == false) : false) || (isCompareBySName ? (item.IsDiffName || item.IsExtra == true) : false);

                item.InvokePropertyChanged();
            }
            NotifyOfPropertyChange(nameof(StepListA));
            NotifyOfPropertyChange(nameof(StepListB));
        }

        private void SyncShowDiffParams(bool isShowDiffParams)
        {
            if (ParamListA == null || ParamListB == null)
                return;

            if (isShowDiffParams)
            {
                foreach (var item in ParamListA)
                {
                    if (item.IsDiff == false && item.IsExtra == false)
                    {
                        item.IsHidden = isShowDiffParams;
                        item.InvokePropertyChanged();
                    }
                }
                foreach (var item in ParamListB)
                {
                    if (item.IsDiff == false && item.IsExtra == false)
                    {
                        item.IsHidden = isShowDiffParams;
                        item.InvokePropertyChanged();
                    }
                }
            }
            else
            {
                foreach (var item in ParamListA)
                {
                    item.IsHidden = isShowDiffParams;
                    item.InvokePropertyChanged();
                }
                foreach (var item in ParamListB)
                {
                    item.IsHidden = isShowDiffParams;
                    item.InvokePropertyChanged();
                }
            }
            NotifyOfPropertyChange(nameof(ParamListA));
            NotifyOfPropertyChange(nameof(ParamListB));
        }
    }
}
