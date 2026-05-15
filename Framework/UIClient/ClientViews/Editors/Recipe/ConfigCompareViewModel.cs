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
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    public class DataCompareItem : NotifiableItem
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

    public class DataStepItem : DataCompareItem
    {
        public string StepNumber { get; set; }
        public string StepName { get; set; }
    }

    public class DataParamItem : DataCompareItem
    {
        public string ParamName { get; set; }
        public string ParamValue { get; set; }
    }

    public class DataLineItem : DataCompareItem
    {
        public string LineNumber { get; set; }
        public string LineText { get; set; }
    }

    public class ConfigCompareViewModel : ModuleUiViewModelBase
    {
        private string _module = "PM1";
        public ObservableCollection<ProcessTypeFileItem> ProcessTypeFileList { get; set; }

        public ObservableCollection<string> ChamberType { get; set; }
        public ObservableCollection<string> RecipeProcessType { get; set; }
        public int ChamberTypeIndexSelection { get; set; }
        public int _processTypeIndexSelection;
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
        public ObservableCollection<DataStepItem> StepListA { get; set; }
        public ObservableCollection<DataParamItem> ParamListA { get; set; }
        public ObservableCollection<DataLineItem> WholeListA { get; set; }
        public string RecipeA { get; set; }
        private XmlDocument _domA = new XmlDocument();
        private string _pathPrefixA;
        private Dictionary<string, ObservableCollection<DataParamItem>> _mapStepParamA = new Dictionary<string, ObservableCollection<DataParamItem>>();
        private Dictionary<string, DataLineItem> _mapLineTextA = new Dictionary<string, DataLineItem>();
        private Dictionary<string, DataLineItem> _mapLineKeyTextA = new Dictionary<string, DataLineItem>();
        private List<string> _listLineKeyTextA = new List<string>();
        private Dictionary<string, DataLineItem> _mapLineKeyTextB = new Dictionary<string, DataLineItem>();
        private List<string> _listLineKeyTextB = new List<string>();
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

        private DataStepItem _stepSelectionA;
        public DataStepItem StepSelectionA
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

        private DataParamItem _paramSelectionA;
        public DataParamItem ParamSelectionA
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

        private DataLineItem _lineSelectionA;
        public DataLineItem LineSelectionA
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
        public ObservableCollection<DataStepItem> StepListB { get; set; }
        public ObservableCollection<DataParamItem> ParamListB { get; set; }
        public ObservableCollection<DataLineItem> WholeListB { get; set; }
        public string RecipeB { get; set; }
        private XmlDocument _domB = new XmlDocument();
        private string _pathPrefixB;
        private Dictionary<string, ObservableCollection<DataParamItem>> _mapStepParamB = new Dictionary<string, ObservableCollection<DataParamItem>>();
        private Dictionary<string, DataLineItem> _mapLineTextB = new Dictionary<string, DataLineItem>();

        private List<string> BackInnerXmlTextB = new List<string>();
        private string BaseTextB = "";
        public bool CanCompare
        {

            get { return EnableButtonRemoveB && EnableButtonRemoveA; }
        }
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

        private DataStepItem _stepSelectionB;
        public DataStepItem StepSelectionB
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

        private DataParamItem _paramSelectionB;
        public DataParamItem ParamSelectionB
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

        private DataLineItem _lineSelectionB;
        public DataLineItem LineSelectionB
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

        public ConfigCompareViewModel()
        {
            StepListA = new ObservableCollection<DataStepItem>();
            ParamListA = new ObservableCollection<DataParamItem>();
            WholeListA = new ObservableCollection<DataLineItem>();

            StepListB = new ObservableCollection<DataStepItem>();
            ParamListB = new ObservableCollection<DataParamItem>();
            WholeListB = new ObservableCollection<DataLineItem>();
        }

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

            //var chamber = QueryDataClient.Instance.Service.GetConfig("System.Recipe.ChamberModules");


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
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Title = "Select config file";
                //openFileDialog.Filter = string.Format("data|*.{0}.data", SelectedChamber);
                openFileDialog.FileName = string.Empty;
                openFileDialog.DefaultExt = "xml";
                if (openFileDialog.ShowDialog() == false)
                {
                    return;
                }
                string xmlConfigPath = openFileDialog.FileName;
                //TryClose(true);
                if (isSelectA)
                {
                    RecipeA = xmlConfigPath;
                    NotifyOfPropertyChange(nameof(RecipeA));
                    NotifyOfPropertyChange(nameof(EnableButtonRemoveA));
                }
                else
                {
                    RecipeB = xmlConfigPath;
                    NotifyOfPropertyChange(nameof(RecipeB));
                    NotifyOfPropertyChange(nameof(EnableButtonRemoveB));
                }
                if (RecipeA != null || RecipeB != null)
                {
                    NotifyOfPropertyChange(nameof(CanCompare));
                }
                //_threadDeleteLogs = new PeriodicJob(1000 * 60 * 60 * 24, LoadData, "DeleteLog Thread", true);              
                LoadData(xmlConfigPath, isSelectA);

                //Recompare();

                //SyncShowDiffSteps(IsShowDiffSteps, IsCompareBySName);
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public void LoadData(string selectedRecipePath, bool isSelectA)
        {
            var array = selectedRecipePath.Split(new char[] { '\\' });

            string recipeName = array[array.Length - 1];

            XmlDocument doc = isSelectA ? _domA : _domB;

            string prefixPath = (isSelectA ? RecipeA : RecipeB).Replace(recipeName, "");


            var configContent = LoadConfigByFullPath(selectedRecipePath);



            if (string.IsNullOrEmpty(configContent))
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
                _mapLineTextA = new Dictionary<string, DataLineItem>();
                _mapLineKeyTextA = new Dictionary<string, DataLineItem>();
                _mapLineKeyTextA.Clear();
                BackInnerXmlTextA.Add(configContent);
            }
            else
            {
                _pathPrefixB = prefixPath;
                _mapLineTextB = new Dictionary<string, DataLineItem>();
                _mapLineKeyTextB.Clear();
                BackInnerXmlTextB.Add(configContent);
            }



            LoadrecipeContentData(configContent, isSelectA);
        }

        public string LoadConfigByFullPath(string fullPath)
        {
            string cfg = string.Empty;
            try
            {
                using (StreamReader fs = new StreamReader(fullPath))
                {
                    cfg = fs.ReadToEnd();
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    using (StreamReader fs = new StreamReader(fullPath))
                    {
                        cfg = fs.ReadToEnd();
                        fs.Close();
                    }
                }
                catch
                {
                    LOG.Write(ex, $"load config file failed, {fullPath}");
                    cfg = string.Empty;
                };
            }
            return cfg;
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
                foreach (DataParamItem item in ParamListA)
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
        List<string> left = new List<string>();
        List<string> right = new List<string>();
        private void LoadrecipeContentData(string recipeContent, bool isSelectA)
        {
            XmlDocument doc = isSelectA ? _domA : _domB;

            if (isSelectA)
            {
                _mapLineTextA = new Dictionary<string, DataLineItem>();
                _mapLineKeyTextA = new Dictionary<string, DataLineItem>();
                left.Clear();
                _listLineKeyTextA.Clear();
            }
            else
            {
                _mapLineTextB = new Dictionary<string, DataLineItem>();
                _mapLineKeyTextB = new Dictionary<string, DataLineItem>();
                right.Clear();
                _listLineKeyTextB.Clear();
            }
            string[] allText = recipeContent.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            int number = 0;
            ObservableCollection<DataLineItem> lineData = new ObservableCollection<DataLineItem>();
            foreach (string lineText in allText)
            {
                DataLineItem line = new DataLineItem();
                line.LineNumber = (++number).ToString();
                line.LineText = lineText;
                if (!string.IsNullOrEmpty(lineText))
                {
                    lineData.Add(line);
                    if (isSelectA)
                    {
                        _mapLineTextA[line.LineNumber] = line;
                        left.Add(lineText);
                    }
                    else
                    {
                        _mapLineTextB[line.LineNumber] = line;
                        right.Add(lineText);
                    }
                }
                if (lineText.Contains("<scdata name="))
                {
                    if (isSelectA)
                    {

                        int keySatrtPos = lineText.IndexOf("<scdata name=") + 14;
                        int keyEndPos = lineText.IndexOf("value=") - 2;
                        int valueSatrtPos = lineText.IndexOf("value=") + 7;
                        int valueEndPos = lineText.Length - 4;
                        string key = lineText.Substring(keySatrtPos, keyEndPos - keySatrtPos);
                        string Value = lineText.Substring(valueSatrtPos, valueEndPos - valueSatrtPos);
                        _mapLineKeyTextA.Add(key, line);
                        _listLineKeyTextA.Add(key);

                    }
                    else
                    {
                        int keySatrtPos = lineText.IndexOf("<scdata name=") + 14;
                        int keyEndPos = lineText.IndexOf("value=") - 2;
                        int valueSatrtPos = lineText.IndexOf("value=") + 7;
                        int valueEndPos = lineText.Length - 4;
                        string key = lineText.Substring(keySatrtPos, keyEndPos - keySatrtPos);
                        string Value = lineText.Substring(valueSatrtPos, valueEndPos - valueSatrtPos);
                        _mapLineKeyTextB.Add(key, line);
                        _listLineKeyTextB.Add(key);

                    }

                }
            }
            if (isSelectA)
            {
                WholeListA = lineData;
                NotifyOfPropertyChange(nameof(WholeListA));
            }
            else
            {
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
                foreach (DataParamItem item in ParamListB)
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
            foreach (DataLineItem DataLineItem in WholeListA)
            {
                if (DataLineItem.LineNumber == LineSelectionA.LineNumber)
                {
                    DataLineItem.LineText = LineSelectionA.LineText;
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
            foreach (DataLineItem DataLineItem in WholeListB)
            {
                if (DataLineItem.LineNumber == LineSelectionB.LineNumber)
                {
                    DataLineItem.LineText = LineSelectionB.LineText;
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

        int compare = 0;
        public void Compare()
        {
            compare++;
            Recompare();
            MessageBox.Show("Comparing,Please wait ...");
        }
        public void Recompare()
        {

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

                ObservableCollection<DataParamItem> paramA = _mapStepParamA[StepListA[i].StepNumber];
                ObservableCollection<DataParamItem> paramB = _mapStepParamB[StepListB[i].StepNumber];
                bool isDiff = false;

                foreach (var pa in paramA)
                {
                    foreach (var pb in paramB)
                    {
                        if (pb.ParamName == pa.ParamName)
                        {
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
        public string GetLineKey(string lineText)
        {
            if (lineText.Contains("<scdata name="))
            {
                int keySatrtPos = lineText.IndexOf("<scdata name=") + 14;
                int keyEndPos = lineText.IndexOf("value=") - 2;
                int valueSatrtPos = lineText.IndexOf("value=") + 7;
                int valueEndPos = lineText.Length - 4;
                string key = lineText.Substring(keySatrtPos, keyEndPos - keySatrtPos);
                string Value = lineText.Substring(valueSatrtPos, valueEndPos - valueSatrtPos);
                return key;
            }
            else
            {
                return lineText;
            }
        }

        private void RecompareWhole()
        {
            if (compare > 1)
            {
                return;
            }
            if (WholeListA == null || WholeListB == null)
                return;
            int spaceNumL = 1;
            int spaceNumR = 1;
            bool isEqua = true;

            if (right.Count == left.Count && !isCopy)
            {
                for (int c = 0; c < right.Count; c++)
                {
                    if (!left.Equals(right[c]))
                    {
                        isEqua = false;
                        continue;
                    }
                }
                if (!isEqua)
                {
                    for (int m = 0; m < right.Count; m++)
                    {
                        if (left[m] != right[m])
                        {
                            string keyL = GetLineKey(left[m]);
                            string keyR = GetLineKey(right[m]);
                            if (keyL != keyR)
                            {
                                if (_listLineKeyTextB.Contains(keyL))
                                {
                                    left.Insert(m, $"{spaceNumL}");
                                    spaceNumL++;
                                }
                                else
                                {
                                    right.Insert(m, $"{spaceNumR}");
                                    spaceNumR++;
                                }
                            }
                        }
                        else
                        {

                            string keyL = GetLineKey(left[m]);
                            string keyR = GetLineKey(right[m]);
                            if (keyL.Contains("   "))
                            {
                                left.Remove(left[m]);
                                right.Remove(right[m]);
                            }

                        }

                    }
                    int numberL = 0;
                    int numberR = 0;

                    _mapLineTextA.Clear();
                    _mapLineTextB.Clear();
                    ObservableCollection<DataLineItem> lineDataL = new ObservableCollection<DataLineItem>();
                    ObservableCollection<DataLineItem> lineDataR = new ObservableCollection<DataLineItem>();
                    foreach (string lineText in left)
                    {
                        DataLineItem line = new DataLineItem();
                        line.LineNumber = (++numberL).ToString();
                        line.LineText = lineText;
                        if (lineText != null)
                        {
                            lineDataL.Add(line);
                            _mapLineTextA[line.LineNumber] = line;
                        }
                    }
                    foreach (string lineText in right)
                    {
                        DataLineItem line = new DataLineItem();
                        line.LineNumber = (++numberR).ToString();
                        line.LineText = lineText;
                        if (lineText != null)
                        {
                            lineDataR.Add(line);
                            _mapLineTextB[line.LineNumber] = line;
                        }
                    }
                    WholeListA.Clear();
                    WholeListB.Clear();
                    WholeListA = lineDataL;
                    //  NotifyOfPropertyChange(nameof(WholeListA));
                    WholeListB = lineDataR;
                    // NotifyOfPropertyChange(nameof(WholeListB));
                    isEqua = true;
                }

            }

            if (right.Count > left.Count)
            {
                for (int m = 0; m < right.Count; m++)
                {
                    if (left[m] != right[m])
                    {
                        string keyL = GetLineKey(left[m]);
                        string keyR = GetLineKey(right[m]);
                        if (keyL != keyR)
                        {
                            if (_listLineKeyTextB.Contains(keyL))
                            {
                                left.Insert(m, $"{spaceNumL}");
                                spaceNumL++;
                            }
                            else
                            {
                                right.Insert(m, $"{spaceNumR}");
                                spaceNumR++;
                            }
                        }
                    }
                    else
                    {

                        string keyL = GetLineKey(left[m]);
                        string keyR = GetLineKey(right[m]);
                        if (keyL.Contains("   "))
                        {
                            left.Remove(left[m]);
                            right.Remove(right[m]);
                        }

                    }
                }
                int numberL = 0;
                int numberR = 0;

                _mapLineTextA.Clear();
                _mapLineTextB.Clear();
                ObservableCollection<DataLineItem> lineDataL = new ObservableCollection<DataLineItem>();
                ObservableCollection<DataLineItem> lineDataR = new ObservableCollection<DataLineItem>();
                foreach (string lineText in left)
                {
                    DataLineItem line = new DataLineItem();
                    line.LineNumber = (++numberL).ToString();
                    line.LineText = lineText;
                    if (lineText != null)
                    {
                        lineDataL.Add(line);
                        _mapLineTextA[line.LineNumber] = line;
                    }
                }
                foreach (string lineText in right)
                {
                    DataLineItem line = new DataLineItem();
                    line.LineNumber = (++numberR).ToString();
                    line.LineText = lineText;
                    if (lineText != null)
                    {
                        lineDataR.Add(line);
                        _mapLineTextB[line.LineNumber] = line;
                    }
                }
                WholeListA.Clear();
                WholeListB.Clear();
                WholeListA = lineDataL;
                //NotifyOfPropertyChange(nameof(WholeListA));
                WholeListB = lineDataR;
                //NotifyOfPropertyChange(nameof(WholeListB));
            }
            else if (right.Count < left.Count)
            {
                for (int m = 0; m < left.Count; m++)
                {
                    if (left[m] != right[m])
                    {
                        string keyL = GetLineKey(left[m]);
                        string keyR = GetLineKey(right[m]);
                        if (keyL != keyR)
                        {
                            if (_listLineKeyTextB.Contains(keyL) && keyR != "</root>")
                            {
                                left.Insert(m, $"{spaceNumL}");
                                spaceNumL++;

                            }
                            else
                            {
                                right.Insert(m, $"{spaceNumR}");
                                spaceNumR++;
                            }
                        }
                    }
                    else
                    {

                        string keyL = GetLineKey(left[m]);
                        string keyR = GetLineKey(right[m]);
                        if (keyL.Contains(" "))
                        {
                            left.Remove(left[m]);
                            right.Remove(right[m]);
                        }

                    }
                }
                int numberL = 0;
                int numberR = 0;
                ObservableCollection<DataLineItem> lineDataL = new ObservableCollection<DataLineItem>();
                ObservableCollection<DataLineItem> lineDataR = new ObservableCollection<DataLineItem>();
                foreach (string lineText in left)
                {
                    DataLineItem line = new DataLineItem();
                    line.LineNumber = (++numberL).ToString();
                    line.LineText = lineText;
                    if (lineText != null)
                    {
                        lineDataL.Add(line);
                        _mapLineTextA[line.LineNumber] = line;
                    }
                }
                foreach (string lineText in right)
                {
                    DataLineItem line = new DataLineItem();
                    line.LineNumber = (++numberR).ToString();
                    line.LineText = lineText;
                    if (lineText != null)
                    {
                        lineDataR.Add(line);
                        _mapLineTextB[line.LineNumber] = line;
                    }
                }

                WholeListA.Clear();
                WholeListB.Clear();
                WholeListA = lineDataL;

                WholeListB = lineDataR;



            }

            int i = 0;

            for (i = 0; i < WholeListA.Count && i < WholeListB.Count; i++)
            {
                DataLineItem lineA = _mapLineTextA[WholeListA[i].LineNumber];
                DataLineItem lineB = _mapLineTextB[WholeListB[i].LineNumber];
                bool isDiff = false;
                bool IsExtra = false;
                lineB.IsDiff = lineA.IsDiff = (lineA.LineText != lineB.LineText);
                if (lineA.IsDiff)
                    isDiff = true;
                if (lineA.IsDiff)
                {
                    if (_mapLineKeyTextB.ContainsValue(lineB))
                    {
                        IsExtra = false;
                    }
                }
                WholeListA[i].IsDiff = WholeListB[i].IsDiff = isDiff;
                WholeListA[i].IsExtra = WholeListB[i].IsExtra = IsExtra;

                _mapLineTextA[WholeListA[i].LineNumber].IsDiff = _mapLineTextB[WholeListB[i].LineNumber].IsDiff = isDiff;
                _mapLineTextA[WholeListA[i].LineNumber].IsExtra = _mapLineTextB[WholeListB[i].LineNumber].IsExtra = IsExtra;

                lineA.InvokePropertyChanged();
                lineB.InvokePropertyChanged();
                WholeListA[i].InvokePropertyChanged();
                WholeListB[i].InvokePropertyChanged();


            }
            string speaceL = "  ";
            string speaceR = "  ";
            foreach (var item in WholeListA)
            {

                for (int p = 1; p < 100; p++)
                {
                    speaceL += " ";
                    if (item.LineText == p.ToString())
                    {
                        item.LineText = speaceL;
                    }
                }
                speaceL = "  ";
            }
            foreach (var item in WholeListB)
            {
                for (int p = 1; p < 100; p++)
                {
                    speaceR += " ";
                    if (item.LineText == p.ToString())
                    {
                        item.LineText = speaceR;
                    }
                }
                speaceR = "  ";
            }
            spaceNumL = 1;
            spaceNumR = 1;


            NotifyOfPropertyChange(nameof(WholeListA));
            NotifyOfPropertyChange(nameof(WholeListB));

        }

        private void SyncStepSelection(DataStepItem stepData, bool isSelectA)
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

        private void SyncParamSelection(DataParamItem paramData, bool isSelectA)
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

        private void SyncLineSelection(DataLineItem lineData, bool isSelectA)
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

        private void RemoveRecipe(bool isSelectA)
        {

            if (!DialogBox.Confirm($"Are you sure you want to remove the config data? \r\n{RecipeB}"))
                return;
            compare = 0;
            if (isSelectA)
            {
                StepListA?.Clear();
                ParamListA?.Clear();
                WholeListA?.Clear();
                RecipeA = string.Empty;
                _mapStepParamA.Clear();
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
                StepListB.Clear();
                ParamListB.Clear();
                WholeListB?.Clear();
                RecipeB = string.Empty;
                _mapStepParamB.Clear();
                _stepSelectionB = null;
                _paramSelectionB = null;

                NotifyOfPropertyChange(nameof(StepListB));
                NotifyOfPropertyChange(nameof(ParamListB));
                NotifyOfPropertyChange(nameof(RecipeB));
                NotifyOfPropertyChange(nameof(EnableButtonRemoveB));
                NotifyOfPropertyChange(nameof(StepSelectionB));
                NotifyOfPropertyChange(nameof(ParamSelectionB));

            }
            NotifyOfPropertyChange(nameof(CanCompare));
        }

        public void StepCopyToRight(DataStepItem stepA)
        {
            StepCopy(stepA, true);
        }

        public void StepCopyToLeft(DataStepItem stepB)
        {
            StepCopy(stepB, false);
        }

        public void LeftDelete(DataStepItem step)
        {
            StepDelete(step, true);
        }

        public void RightDelete(DataStepItem step)
        {
            StepDelete(step, false);
        }

        private void StepDelete(DataStepItem step, bool isSelectA)
        {
            ObservableCollection<DataStepItem> stepList = isSelectA ? StepListA : StepListB;
            Dictionary<string, ObservableCollection<DataParamItem>> _mapStepParam = isSelectA ? _mapStepParamA : _mapStepParamB;
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

            ObservableCollection<DataStepItem> stepListTo = isSelectA ? StepListA : StepListB;

            if (stepsNode == null)
            {
                return;
            }

            int stepNoCount = 1;
            foreach (DataStepItem item in stepListTo)
            {
                XmlElement DeviceTree = docTo.CreateElement("Step");
                DeviceTree.SetAttribute("StepNo", (stepNoCount++).ToString());
                DeviceTree.SetAttribute("Name", item.StepName);
                stepsNode.AppendChild(DeviceTree);
            }

            foreach (XmlNode nodeStep in stepsNode)
            {
                string stepNumber = nodeStep.Attributes["StepNo"].Value;

                ObservableCollection<DataParamItem> paramList =
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

        private void StepCopy(DataStepItem stepData, bool isFromA)
        {
            DataStepItem stepFrom = stepData;
            ObservableCollection<DataStepItem> stepListTo = isFromA ? StepListB : StepListA;
            Dictionary<string, ObservableCollection<DataParamItem>> mapFrom = isFromA ? _mapStepParamA : _mapStepParamB;
            Dictionary<string, ObservableCollection<DataParamItem>> mapTo = isFromA ? _mapStepParamB : _mapStepParamA;

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
                DataStepItem DataStepItem = new DataStepItem();
                DataStepItem.StepName = stepFrom.StepName;
                DataStepItem.StepNumber = (stepListTo.Count + 1).ToString();
                stepListTo.Add(DataStepItem);
                ObservableCollection<DataParamItem> DataParamItems = new ObservableCollection<DataParamItem>();
                foreach (var paramFrom in mapFrom[stepFrom.StepNumber])
                {
                    DataParamItem DataParamItem = new DataParamItem();
                    DataParamItem.ParamName = paramFrom.ParamName;
                    DataParamItem.ParamValue = paramFrom.ParamValue;
                    DataParamItems.Add(DataParamItem);
                }
                if (mapTo.ContainsKey(DataStepItem.StepNumber))
                    mapTo[DataStepItem.StepNumber] = DataParamItems;
                else
                    mapTo.Add(DataStepItem.StepNumber, DataParamItems);
            }
            CopyToInnerXml(!isFromA);
            Recompare();
            SyncShowDiffSteps(IsShowDiffSteps, IsCompareBySName);

            SyncStepSelection(stepFrom, !isFromA);
        }

        private void CopyToInnerXml(bool isSelectA)
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

            ObservableCollection<DataStepItem> stepListTo = isSelectA ? StepListA : StepListB;

            if (nodeModule == null)
            {
                return;
            }

            foreach (DataStepItem item in stepListTo)
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

                ObservableCollection<DataParamItem> paramList =
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

        private string getXmlText(XmlDocument xmlDocument)
        {
            (new RecipeProvider()).SaveRecipe("", "RecipeTemp", xmlDocument.InnerXml);

            var _recipeProvider = new RecipeProvider();
            var recipeContent = _recipeProvider.LoadRecipe("", "RecipeTemp");
            return recipeContent;
        }

        public void ParamCopyToRight(DataParamItem paramData)
        {
            ParamCopy(paramData, true);
        }

        public void ParamCopyToLeft(DataParamItem paramData)
        {
            ParamCopy(paramData, false);
        }

        private void ParamCopy(DataParamItem paramData, bool isFromA)
        {
            DataParamItem paramFrom = paramData;
            DataStepItem stepFrom = isFromA ? StepSelectionA : StepSelectionB;
            Dictionary<string, ObservableCollection<DataParamItem>> mapFrom = isFromA ? _mapStepParamA : _mapStepParamB;
            Dictionary<string, ObservableCollection<DataParamItem>> mapTo = isFromA ? _mapStepParamB : _mapStepParamA;
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

            ObservableCollection<DataStepItem> stepListTo = isFromA ? StepListB : StepListA;
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

        public void LineCopyToLeft(DataLineItem lineData)
        {
            LineCopy(lineData, false);
            MessageBox.Show($"Line Copy To Left Success ");
        }

        public void LineCopyToRight(DataLineItem lineData)
        {
            LineCopy(lineData, true);
            MessageBox.Show($"Line Copy To Right Success");
        }
        bool isCopy = false;
        public void LineCopy(DataLineItem lineData, bool isFromA)
        {
            try
            {
                isCopy = true;
                DataLineItem lineFrom = lineData;
                ObservableCollection<DataLineItem> lineListTo = isFromA ? WholeListB : WholeListA;

                Dictionary<string, DataLineItem> mapFrom = isFromA ? _mapLineTextA : _mapLineTextB;
                Dictionary<string, DataLineItem> mapTo = isFromA ? _mapLineTextB : _mapLineTextA;

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
                //LoadDataByRecipeContent(recipeContent, isFromA);
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
            if (isSelectA)
            {
                SaveConfigData(RecipeA, doc.InnerXml);
            }
            else
            {
                SaveConfigData(RecipeB, doc.InnerXml);
            }
        }
        public bool SaveConfigData(string path, string recipeContent)
        {

            bool ret = true;
            try
            {

                FileInfo fi = new FileInfo(path);
                if (!fi.Directory.Exists)
                    fi.Directory.Create();

                XmlDocument xml = new XmlDocument();
                xml.LoadXml(recipeContent);

                XmlTextWriter writer = new XmlTextWriter(path, Encoding.UTF8);
                writer.Formatting = Formatting.Indented;
                xml.Save(writer);
                writer.Close();
                MessageBox.Show($"{path} Save Success");
                LOG.Write($"{path}保存成功");

            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex}  Save Config file Error");
                LOG.Write(ex, "保存Config file 出错");


                ret = false;
            }
            return ret;
        }
        public void UndoA()
        {
            string xmlData = "";
            for (var i = (BackInnerXmlTextA.Count - 1); i >= 0;)
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
        }

        public void UndoB()
        {
            string xmlData = "";
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
