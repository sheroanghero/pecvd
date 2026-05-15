using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using Aitex.Core.RT.Log;
using Caliburn.Micro.Core;
using MECF.Framework.Common.DataCenter;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    public class RecipeData : PropertyChangedBase
    {
        public bool IsChanged
        {
            get
            {
                bool changed = !IsSavedDesc;

                if (!changed)
                {
                    changed = ChkChanged(Steps) || ChkChanged(PopSettingSteps) || ChkChanged(StepTolerances);
                }

                if (!changed)
                {
                    foreach (Param config in this.ConfigItems)
                    {
                        if (!config.IsSaved)
                        {
                            changed = true;
                            break;
                        }
                    }

                    foreach (Param config in this.ConfigSelItems)
                    {
                        if (!config.IsSaved)
                        {
                            changed = true;
                            break;
                        }
                    }
                }
                return changed;
            }
        }
        private bool _freeze;
        public bool Freeze
        {
            get { return this._freeze; }
            set
            {
                this._freeze = value;
                this.NotifyOfPropertyChange("Freeze");
            }
        }

        private bool enableFreeze;
        public bool EnableFreeze
        {
            get { return this.enableFreeze; }
            set
            {
                this.enableFreeze = value;
                this.NotifyOfPropertyChange("EnableFreeze");
            }
        }
        private bool enablenotFreeze;
        public bool EnableNotFreeze
        {
            get { return this.enablenotFreeze; }
            set
            {
                this.enablenotFreeze = value;
                this.NotifyOfPropertyChange("EnableNotFreeze");
            }
        }

        private bool _isSavedDesc;
        public bool IsSavedDesc
        {
            get { return _isSavedDesc; }
            set
            {
                this._isSavedDesc = value;
                this.NotifyOfPropertyChange("IsSavedDesc");
            }
        }

        private string name;
        public string Name
        {
            get { return this.name; }
            set
            {
                this.name = value;
                this.NotifyOfPropertyChange("Name");
            }
        }

        private string _chamberType;
        public string RecipeChamberType
        {
            get { return this._chamberType; }
            set
            {
                this._chamberType = value;
                this.NotifyOfPropertyChange("RecipeChamberType");
            }
        }

        private string _recipeVersion;
        public string RecipeVersion
        {
            get { return this._recipeVersion; }
            set
            {
                this._recipeVersion = value;
                this.NotifyOfPropertyChange("RecipeVersion");
            }
        }

        private string _prefixPath;
        public string PrefixPath
        {
            get { return _prefixPath; }
            set
            {
                _prefixPath = value;
                this.NotifyOfPropertyChange("PrefixPath");
            }
        }

        private string creator;
        public string Creator
        {
            get { return this.creator; }
            set
            {
                this.creator = value;
                this.NotifyOfPropertyChange("Creator");
            }
        }

        private DateTime createTime;
        public DateTime CreateTime
        {
            get { return this.createTime; }
            set
            {
                this.createTime = value;
                this.NotifyOfPropertyChange("CreateTime");
            }
        }

        private string description;
        public string Description
        {
            get { return this.description; }
            set
            {
                this.description = value;
                this.NotifyOfPropertyChange("Description");
            }
        }

        private string devisor;
        public string Revisor
        {
            get { return this.devisor; }
            set
            {
                this.devisor = value;
                this.NotifyOfPropertyChange("Revisor");
            }
        }

        private DateTime deviseTime;
        public DateTime ReviseTime
        {
            get { return this.deviseTime; }
            set
            {
                this.deviseTime = value;
                this.NotifyOfPropertyChange("ReviseTime");
            }
        }
        public ObservableCollection<ObservableCollection<Param>> Steps { get; private set; }

        public Dictionary<string, ObservableCollection<ObservableCollection<Param>>> PopSettingSteps { get; private set; }

        public ObservableCollection<ObservableCollection<Param>> StepTolerances { get; private set; }

        public ObservableCollection<Param> ConfigItems { get; private set; }

        public ObservableCollection<Param> ConfigSelItems { get; private set; }

        private XmlDocument _doc;
        private string _module;

        public bool ToleranceEnable { get; set; }
        public Dictionary<string, bool> PopEnable { get; set; }

        public bool IsCompatibleWithCurrentFormat { get; set; }

        public bool ChkChanged(ObservableCollection<ObservableCollection<Param>> Steps)
        {
            foreach (ObservableCollection<Param> parameters in Steps)
            {
                if (parameters.Where(param => param.IsSaved == false).Count() > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ChkChanged(Dictionary<string, ObservableCollection<ObservableCollection<Param>>> PopSteps)
        {
            foreach (ObservableCollection<ObservableCollection<Param>> parameters in PopSteps.Values)
            {
                if (ChkChanged(parameters))
                {
                    return true;
                }
            }
            return false;
        }

        public RecipeData()
        {
            this.Steps = new ObservableCollection<ObservableCollection<Param>>();
            StepTolerances = new ObservableCollection<ObservableCollection<Param>>();
            this.PopSettingSteps = new Dictionary<string, ObservableCollection<ObservableCollection<Param>>>();
            this.PopEnable = new Dictionary<string, bool>();
            ConfigItems = new ObservableCollection<Param>();
            ConfigSelItems = new ObservableCollection<Param>();
            IsSavedDesc = true;

            _doc = new XmlDocument();
            XmlElement node = _doc.CreateElement("Aitex");
            _doc.AppendChild(node);
            node.AppendChild(_doc.CreateElement("TableRecipeData"));
        }

        public void Clear()
        {
            Steps.Clear();
            PopSettingSteps.Clear();
            StepTolerances.Clear();
            ConfigItems.Clear();
            ConfigSelItems.Clear();

            RecipeChamberType = "";
            RecipeVersion = "";
            IsSavedDesc = true;
            _module = "";
        }

        public void DataSaved()
        {
            foreach (ObservableCollection<Param> parameters in Steps)
            {
                parameters.Apply(param => param.IsSaved = true);
            }

            foreach (ObservableCollection<ObservableCollection<Param>> popParameters in PopSettingSteps.Values)
            {
                foreach (ObservableCollection<Param> parameters in popParameters)
                {
                    parameters.Apply(param => param.IsSaved = true);
                }
            }

            foreach (ObservableCollection<Param> parameters in StepTolerances)
            {
                parameters.Apply(param => param.IsSaved = true);
            }

            ConfigItems.Apply(config => config.IsSaved = true);
            ConfigSelItems.Apply(config => config.IsSaved = true);

            IsSavedDesc = true;
        }

        public void InitData(string prefixPath, string recipeName, string recipeContent,
            ObservableCollection<EditorDataGridTemplateColumnBase> columnDefine, Dictionary<string, ObservableCollection<Param>> popSettingColumnsDefine, ObservableCollection<Param> configDefine, string module)
        {
            IsCompatibleWithCurrentFormat = false;

            Name = recipeName;
            PrefixPath = prefixPath;
            _module = module;
            try
            {
                _doc = new XmlDocument();
                _doc.LoadXml(recipeContent);

                if (!LoadHeader(_doc.SelectSingleNode("Aitex/TableRecipeData")))
                    return;

                XmlNodeList nodeSteps = _doc.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{module}']/Step");
                if (nodeSteps == null)
                    nodeSteps = _doc.SelectNodes($"Aitex/TableRecipeData/Step");

                LoadSteps(columnDefine, popSettingColumnsDefine, nodeSteps);

                var index = 1;
                foreach (ObservableCollection<Param> parameters in Steps)
                {
                    (parameters[1] as StepParam).Value = index.ToString();
                    index++;
                }

                ValidLoopData();

                XmlNode nodeConfig =
                    _doc.SelectSingleNode($"Aitex/TableRecipeData/Module[@Name='{module}']/Config");
                if (nodeSteps == null)
                    nodeConfig = _doc.SelectSingleNode($"Aitex/TableRecipeData/Config");

                LoadConfigs(configDefine, nodeConfig);

                IsCompatibleWithCurrentFormat = true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public void ChangeChamber(ObservableCollection<EditorDataGridTemplateColumnBase> columnDefine, Dictionary<string, ObservableCollection<Param>> PopSettingColumns
            , ObservableCollection<Param> configDefine, string module)
        {
            _module = module;
            try
            {
                XmlNodeList nodeSteps = _doc.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{module}']/Step");
                if (nodeSteps == null)
                    nodeSteps = _doc.SelectNodes($"Aitex/TableRecipeData/Step");

                LoadSteps(columnDefine, PopSettingColumns, nodeSteps);

                var index = 1;
                foreach (ObservableCollection<Param> parameters in Steps)
                {
                    (parameters[1] as StepParam).Value = index.ToString();
                    index++;
                }

                ValidLoopData();

                XmlNode nodeConfig =
                    _doc.SelectSingleNode($"Aitex/TableRecipeData/Module[@Name='{module}']/Config");
                if (nodeSteps == null)
                    nodeConfig = _doc.SelectSingleNode($"Aitex/TableRecipeData/Config");

                LoadConfigs(configDefine, nodeConfig);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public void SaveTo(string[] moduleList)
        {
            GetXmlString();

            XmlNode nodeModule = _doc.SelectSingleNode($"Aitex/TableRecipeData/Module[@Name='{_module}']");
            if (nodeModule == null)
            {
                LOG.Write("recipe not find modules," + Name);
                return;
            }

            XmlNode nodeData = nodeModule.ParentNode;

            foreach (var module in moduleList)
            {
                if (module == _module)
                {
                    continue;
                }

                var child = _doc.SelectSingleNode($"Aitex/TableRecipeData/Module[@Name='{module}']");
                if (child != null)
                    nodeData.RemoveChild(child);

                XmlElement node = nodeModule.Clone() as XmlElement;
                node.SetAttribute("Name", module);
                nodeData.AppendChild(node);
            }
        }

        public ObservableCollection<Param> CreateStep(ObservableCollection<EditorDataGridTemplateColumnBase> columns, XmlNode stepNode = null)
        {
            ObservableCollection<Param> rows = new ObservableCollection<Param>();

            foreach (EditorDataGridTemplateColumnBase col in columns)
            {
                string value = string.Empty;
                if (!(col is ExpanderColumn) && stepNode != null && !(col is StepColumn) && !(col is PopSettingColumn))
                {
                    if (string.IsNullOrEmpty(col.ModuleName) && stepNode.Attributes[col.ControlName] != null)
                    {
                        value = stepNode.Attributes[col.ControlName].Value;
                    }
                    else
                    {
                        if (stepNode.Attributes[col.ControlName] != null && stepNode.SelectSingleNode(col.ModuleName) != null && stepNode.SelectSingleNode(col.ModuleName).Attributes[col.ControlName] != null)
                            value = stepNode.SelectSingleNode(col.ModuleName).Attributes[col.ControlName].Value;
                    }
                }

                Param cell = null;

                if (col is StepColumn)
                {
                    cell = new StepParam()
                    {
                        Name = col.ControlName,
                        EnableTolerance = col.EnableTolerance,
                    };
                }
                else if (col is TextBoxColumn)
                {
                    cell = new StringParam()
                    {
                        Name = col.ControlName,
                        Value = value,
                        IsEnabled = col.IsEnable,
                        EnableTolerance = col.EnableTolerance,
                    };
                }
                else if (col is NumColumn)
                {

                    cell = new IntParam()
                    {
                        Name = col.ControlName,
                        Value = string.IsNullOrEmpty(value) ? (string.IsNullOrEmpty(col.Default) ? 0 : int.Parse(col.Default)) : int.Parse(value),
                        IsEnabled = col.IsEnable,
                        Minimun = (int)((NumColumn)col).Minimun,
                        Maximun = (int)((NumColumn)col).Maximun,
                        EnableTolerance = col.EnableTolerance,
                    };

                }
                else if (col is DoubleColumn)
                {

                    cell = new DoubleParam()
                    {
                        Name = col.ControlName,
                        Value = string.IsNullOrEmpty(value) ? (string.IsNullOrEmpty(col.Default) ? "0" : col.Default) : value,
                        Minimun = (int)((DoubleColumn)col).Minimun,
                        Maximun = (int)((DoubleColumn)col).Maximun,
                        IsEnabled = col.IsEnable,
                        Resolution = ((DoubleColumn)col).Resolution,
                        EnableTolerance = col.EnableTolerance,
                    };

                }
                else if (col is ComboxColumn)
                {
                    string displayValue;
                    if (!string.IsNullOrEmpty(value) &&
                            ((ComboxColumn)col).Options.FirstOrDefault(x => x.ControlName == value) != null)
                    {
                        displayValue = ((ComboxColumn)col).Options.First(x => x.ControlName == value).DisplayName;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(col.Default) && ((ComboxColumn)col).Options.Any(x => x.DisplayName == col.Default))
                        {
                            displayValue = col.Default;
                        }
                        else
                        {
                            int selIndex = 0;
                            displayValue = displayValue = ((ComboxColumn)col).Options[selIndex].DisplayName;
                        }
                    }

                    cell = new ComboxParam()
                    {
                        Name = col.ControlName,
                        Value = displayValue,
                        Options = ((ComboxColumn)col).Options,
                        IsEditable = !col.IsReadOnly,
                        EnableTolerance = col.EnableTolerance,
                    };
                }
                else if (col is LoopComboxColumn)
                {
                    int selIndex = 0;
                    cell = new LoopComboxParam()
                    {
                        Name = col.ControlName,
                        Value = string.IsNullOrEmpty(value) ? ((LoopComboxColumn)col).Options[selIndex].DisplayName : value,
                        Options = ((LoopComboxColumn)col).Options,
                        IsEditable = !col.IsReadOnly,
                        IsLoopStep = false,
                        EnableTolerance = col.EnableTolerance,
                    };
                }
                else if (col is ExpanderColumn)
                {
                    cell = new ExpanderParam();
                }
                else if (col is PopSettingColumn)
                {
                    cell = new PopSettingParam()
                    {
                        Name = col.ControlName,
                        DisplayName = col.DisplayName,
                        EnableTolerance = col.EnableTolerance,
                    };
                }
                cell.Feedback = col.Feedback;
                cell.Parent = rows;

                if (col is LoopComboxColumn)
                {
                    cell.Feedback = LoopCellFeedback;
                }
                rows.Add(cell);
            }
            foreach (Param cell in rows)
            {
                if (cell.Feedback != null)
                    cell.Feedback(cell);
            }
            return rows;
        }

        public bool CreateStepTolerance(ObservableCollection<EditorDataGridTemplateColumnBase> columns,
            Dictionary<string, ObservableCollection<Param>> popSettingColumns, XmlNode stepNode, out ObservableCollection<Param> step, out ObservableCollection<Param> warning, out ObservableCollection<Param> alarm,
            out Dictionary<string, ObservableCollection<Param>> popSettingStep)
        {
            step = new ObservableCollection<Param>();
            warning = new ObservableCollection<Param>();
            alarm = new ObservableCollection<Param>();
            popSettingStep = new Dictionary<string, ObservableCollection<Param>>();

            foreach (EditorDataGridTemplateColumnBase col in columns)
            {
                string warningValue = string.Empty;
                string alarmValue = string.Empty;
                string stepValue = string.Empty;
                Dictionary<string, Dictionary<string, string>> popValues = new Dictionary<string, Dictionary<string, string>>();

                if (!(col is ExpanderColumn) && stepNode != null && !(col is StepColumn) && !(col is PopSettingColumn))
                {
                    XmlNode warningNode = stepNode.SelectSingleNode("Warning");
                    if (warningNode != null && warningNode.Attributes[col.ControlName] != null)
                    {
                        warningValue = warningNode.Attributes[col.ControlName].Value;
                    }
                    XmlNode alarmNode = stepNode.SelectSingleNode("Alarm");
                    if (alarmNode != null && alarmNode.Attributes[col.ControlName] != null)
                    {
                        alarmValue = alarmNode.Attributes[col.ControlName].Value;
                    }

                    if (string.IsNullOrEmpty(col.ModuleName) && stepNode.Attributes[col.ControlName] != null)
                    {
                        stepValue = stepNode.Attributes[col.ControlName].Value;
                    }
                    else
                    {
                        if (stepNode.Attributes[col.ControlName] != null && stepNode.SelectSingleNode(col.ModuleName) != null && stepNode.SelectSingleNode(col.ModuleName).Attributes[col.ControlName] != null)
                            stepValue = stepNode.SelectSingleNode(col.ModuleName).Attributes[col.ControlName].Value;
                    }
                }

                if (col is PopSettingColumn)
                {
                    foreach (string key in popSettingColumns.Keys)
                    {
                        XmlNode popNode = stepNode.SelectSingleNode(key);
                        if (popNode != null)
                        {
                            Dictionary<string, string> Values = new Dictionary<string, string>();
                            foreach (Param item in popSettingColumns[key])
                            {
                                if (popNode.Attributes[item.Name] != null)
                                    Values.Add(item.Name, popNode.Attributes[item.Name].Value);
                            };

                            popValues.Add(key, Values);
                        }
                    }
                }
                Param stepCell = new DoubleParam()
                {
                    Name = col.ControlName,
                    DisplayName = col.DisplayName,
                    Value = stepValue,
                    IsEnabled = false,
                    StepCheckVisibility = Visibility.Hidden,
                };

                stepCell.Parent = step;
                step.Add(stepCell);

                if (col is PopSettingColumn)
                {
                    for (int i = 0; i < popSettingColumns[col.ControlName].Count; i++)
                    {
                        string name = popSettingColumns[col.ControlName][i].Name;
                        string value = popValues[col.ControlName].Where(x => x.Key == name).Count() > 0 ?
                            popValues[col.ControlName].First(x => x.Key == name).Value : "";
                        if (popSettingColumns[col.ControlName][i] is DoubleParam)
                        {
                            stepCell = new DoubleParam()
                            {
                                Name = name,
                                DisplayName = popSettingColumns[col.ControlName][i].DisplayName,
                                Value = value,
                                IsEnabled = false,
                                StepCheckVisibility = Visibility.Hidden,
                            };
                        }
                        if (popSettingColumns[col.ControlName][i] is StringParam)
                        {
                            stepCell = new StringParam()
                            {
                                Name = name,
                                DisplayName = popSettingColumns[col.ControlName][i].DisplayName,
                                Value = value,
                                IsEnabled = false,
                                StepCheckVisibility = Visibility.Hidden,
                            };
                        }
                        if (popSettingColumns[col.ControlName][i] is ComboxParam)
                        {
                            stepCell = new ComboxParam()
                            {
                                Name = name,
                                DisplayName = popSettingColumns[col.ControlName][i].DisplayName,
                                Value = value,
                                Options = ((ComboxParam)popSettingColumns[col.ControlName][i]).Options,
                                IsEditable = !col.IsReadOnly,
                                EnableTolerance = col.EnableTolerance,
                            };
                        }

                        if (!popSettingStep.ContainsKey(col.ControlName))
                        {
                            popSettingStep.Add(col.ControlName, new ObservableCollection<Param>());
                        }
                        stepCell.Parent = popSettingStep[col.ControlName];
                        popSettingStep[col.ControlName].Add(stepCell);
                    }
                }
                Param warningCell = new DoubleParam()
                {
                    Name = col.ControlName,
                    DisplayName = col.DisplayName,
                    Value = col.EnableTolerance ? (string.IsNullOrEmpty(warningValue) ? "0" : warningValue) : "*",
                    IsEnabled = col.EnableTolerance && stepValue != "0",
                    StepCheckVisibility = Visibility.Collapsed,
                };


                warningCell.Feedback = col.Feedback;
                warningCell.Parent = warning;
                warning.Add(warningCell);

                Param alarmCell = new DoubleParam()
                {
                    Name = col.ControlName,
                    DisplayName = col.DisplayName,
                    Value = col.EnableTolerance ? (string.IsNullOrEmpty(alarmValue) ? "0" : alarmValue) : "*",
                    IsEnabled = col.EnableTolerance && stepValue != "0",
                    StepCheckVisibility = Visibility.Collapsed,
                };


                alarmCell.Feedback = col.Feedback;
                alarmCell.Parent = alarm;
                alarm.Add(alarmCell);
            }

            return true;
        }

        public void ValidLoopData()
        {
            if (Steps.Count == 0)
                return;

            for (int j = 0; j < Steps[0].Count; j++)
            {
                if (Steps[0][j] is LoopComboxParam)
                {
                    LoopCellFeedback(Steps[0][j]);
                }
            }
        }

        private void LoopCellFeedback(Param cell)
        {
            var loopCell = cell as LoopComboxParam;
            int rowIndex = -1;
            int colIndex = -1;

            for (int i = 0; i < Steps.Count; i++)
            {
                for (int j = 0; j < Steps[i].Count; j++)
                {
                    if (Steps[i][j] == loopCell)
                    {
                        rowIndex = i;
                        colIndex = j;
                    }
                }
            }

            if (rowIndex < 0 || colIndex < 0)
                return;

            for (int i = 0; i < Steps.Count; i++)
            {
                loopCell = Steps[i][colIndex] as LoopComboxParam;
                string loopStr = loopCell.Value;

                bool isLoopStart = Regex.IsMatch(loopStr, @"^Loop\x20x\d+$");
                bool isLoopEnd = Regex.IsMatch(loopStr, @"^Loop End$");
                bool isNullOrEmpty = string.IsNullOrWhiteSpace(loopStr);

                if (!isLoopEnd && !isLoopStart && !isNullOrEmpty)
                {
                    loopCell.IsLoopStep = true;
                    loopCell.IsValidLoop = false;
                    continue;
                }

                if (isLoopEnd)
                {
                    loopCell.IsLoopStep = true;
                    loopCell.IsValidLoop = false;
                    continue;
                }

                if (isLoopStart)
                {
                    if (i + 1 == Steps.Count)
                    {
                        loopCell.IsLoopStep = true;
                        loopCell.IsValidLoop = true;
                    }

                    for (int j = i + 1; j < Steps.Count; j++)
                    {
                        var loopCell2 = Steps[j][colIndex] as LoopComboxParam;
                        string loopStr2 = loopCell2.Value;
                        bool isLoopStart2 = Regex.IsMatch(loopStr2, @"^Loop\x20x\d+$");
                        bool isLoopEnd2 = Regex.IsMatch(loopStr2, @"^Loop End$");
                        bool isNullOrEmpty2 = string.IsNullOrWhiteSpace(loopStr2);

                        if (!isLoopEnd2 && !isLoopStart2 && !isNullOrEmpty2)
                        {
                            for (int k = i; k < j + 1; k++)
                            {
                                (Steps[k][colIndex] as LoopComboxParam).IsLoopStep = true;
                                (Steps[k][colIndex] as LoopComboxParam).IsValidLoop = false;
                            }
                            i = j;
                            break;
                        }
                        if (isLoopStart2)
                        {
                            loopCell.IsLoopStep = true;
                            loopCell.IsValidLoop = true;
                            i = j - 1;
                            break;
                        }

                        if (isLoopEnd2)
                        {
                            for (int k = i; k < j + 1; k++)
                            {
                                (Steps[k][colIndex] as LoopComboxParam).IsLoopStep = true;
                                (Steps[k][colIndex] as LoopComboxParam).IsValidLoop = true;
                            }
                            i = j;
                            break;
                        }

                        if (j == Steps.Count - 1)
                        {
                            loopCell.IsLoopStep = true;
                            loopCell.IsValidLoop = true;
                            i = j;
                            break;
                        }

                    }
                    continue;
                }

                loopCell.IsLoopStep = false;
                loopCell.IsValidLoop = false;
            }

        }

        private bool LoadHeader(XmlNode nodeHeader)
        {
            if (nodeHeader == null)
                return false;

            if (nodeHeader.Attributes["CreatedBy"] != null)
                this.Creator = nodeHeader.Attributes["CreatedBy"].Value;
            if (nodeHeader.Attributes["CreationTime"] != null)
                this.CreateTime = DateTime.Parse(nodeHeader.Attributes["CreationTime"].Value);
            if (nodeHeader.Attributes["LastRevisedBy"] != null)
                this.Revisor = nodeHeader.Attributes["LastRevisedBy"].Value;
            if (nodeHeader.Attributes["LastRevisionTime"] != null)
                this.ReviseTime = DateTime.Parse(nodeHeader.Attributes["LastRevisionTime"].Value);
            if (nodeHeader.Attributes["Description"] != null)
                this.Description = nodeHeader.Attributes["Description"].Value;
            string chamberType = string.Empty;
            if (nodeHeader.Attributes["RecipeChamberType"] != null)
                chamberType = nodeHeader.Attributes["RecipeChamberType"].Value;
            if (nodeHeader.Attributes["Freeze"] != null)
                this.Freeze = Convert.ToBoolean(nodeHeader.Attributes["Freeze"].Value);
            if (!string.IsNullOrEmpty(chamberType) && chamberType != RecipeChamberType)
            {
                LOG.Write($"{chamberType} is not accordance with {RecipeChamberType}");
                return false;
            }

            string version = string.Empty;
            if (nodeHeader.Attributes["RecipeVersion"] != null)
                version = nodeHeader.Attributes["RecipeVersion"].Value;
            if (!string.IsNullOrEmpty(version) && version != RecipeVersion)
            {
                LOG.Write($"{version} is not accordance with {RecipeVersion}");
                return false;
            }

            return true;
        }

        private void LoadSteps(ObservableCollection<EditorDataGridTemplateColumnBase> columns, Dictionary<string, ObservableCollection<Param>> popSettingColumns, XmlNodeList steps)
        {
            Steps.Clear();
            PopSettingSteps.Clear();
            StepTolerances.Clear();

            int index = 1;
            foreach (XmlNode nodeStep in steps)
            {
                ObservableCollection<Param> rows = this.CreateStep(columns, nodeStep);

                (rows[1] as StepParam).Value = index.ToString();

                CreateStepTolerance(columns, popSettingColumns, nodeStep, out ObservableCollection<Param> step, out ObservableCollection<Param> warning,
                    out ObservableCollection<Param> alarm, out Dictionary<string, ObservableCollection<Param>> popSettingStep
                    );

                StepTolerances.Add(step);
                (step[1] as DoubleParam).Value = index.ToString();

                StepTolerances.Add(warning);
                (warning[1] as DoubleParam).Value = "Warn(%)";

                StepTolerances.Add(alarm);
                (alarm[1] as DoubleParam).Value = "Alarm(%)";

                Steps.Add(rows);

                foreach (string key in popSettingStep.Keys)
                {
                    if (!PopSettingSteps.ContainsKey(key))
                    {
                        PopSettingSteps.Add(key, new ObservableCollection<ObservableCollection<Param>>());
                    }
                    PopSettingSteps[key].Add(popSettingStep[key]);
                }

                index++;
            }
        }

        private void LoadConfigs(ObservableCollection<Param> configDefine, XmlNode configNode)
        {
            ConfigItems.Clear();
            ConfigSelItems.Clear();

            foreach (var param in configDefine)
            {
                if (param is DoubleParam param1)
                {
                    var config = new DoubleParam()
                    {
                        Name = param.Name,
                        Value = param1.Value,
                        DisplayName = param.DisplayName,
                        Minimun = param1.Minimun,
                        Maximun = param1.Maximun,
                        Resolution = param1.Resolution
                    };

                    if (configNode != null && configNode.Attributes[param1.Name] != null)
                        config.Value = configNode.Attributes[param1.Name].Value;

                    ConfigItems.Add(config);
                }

                if (param is StringParam paramString)
                {
                    var config = new StringParam()
                    {
                        Name = param.Name,
                        Value = paramString.Value,
                        DisplayName = param.DisplayName,
                    };

                    if (configNode != null && configNode.Attributes[paramString.Name] != null)
                        config.Value = configNode.Attributes[paramString.Name].Value;

                    ConfigItems.Add(config);
                }

                if (param.GetType().Name == "ComboxParam")
                {
                    string Value = "";
                    if (configNode == null)
                    {
                        Value = ((RecipeEditorLib.RecipeModel.Params.ComboxParam)param).Value;
                    }
                    else
                    {
                        Value = configNode.Attributes[param.Name].Value;
                    }
                    var config = new ComboxParam()
                    {
                        Name = param.Name,
                        Options = ((RecipeEditorLib.RecipeModel.Params.ComboxParam)param).Options,
                        Value = Value,
                        DisplayName = param.DisplayName,
                    };
                    ConfigSelItems.Add(config);
                }
            }
        }

        public ObservableCollection<Param> CloneStep(ObservableCollection<EditorDataGridTemplateColumnBase> _columns, ObservableCollection<Param> _sourceParams)
        {
            ObservableCollection<Param> targetParams = this.CreateStep(_columns);

            for (var index = 0; index < _sourceParams.Count; index++)
            {
                if (_sourceParams[index] is StringParam)
                {
                    ((StringParam)targetParams[index]).Value = ((StringParam)_sourceParams[index]).Value;
                }
                else if (_sourceParams[index] is IntParam)
                {
                    ((IntParam)targetParams[index]).Value = ((IntParam)_sourceParams[index]).Value;
                }
                else if (_sourceParams[index] is ComboxParam)
                {
                    ((ComboxParam)targetParams[index]).Value = ((ComboxParam)_sourceParams[index]).Value;
                }
                else if (_sourceParams[index] is LoopComboxParam)
                {
                    ((LoopComboxParam)targetParams[index]).Value = ((LoopComboxParam)_sourceParams[index]).Value;
                }
                else if (_sourceParams[index] is BoolParam)
                {
                    ((BoolParam)targetParams[index]).Value = ((BoolParam)_sourceParams[index]).Value;
                }
                else if (_sourceParams[index] is IntParam)
                {
                    ((IntParam)targetParams[index]).Value = ((IntParam)_sourceParams[index]).Value;
                }
                else if (_sourceParams[index] is DoubleParam)
                {
                    ((DoubleParam)targetParams[index]).Value = ((DoubleParam)_sourceParams[index]).Value;
                }
                targetParams[index].Parent = targetParams;
            }

            return targetParams;
        }

        XmlElement nodeConfig;
        public string GetXmlString()
        {
            nodeConfig = _doc.CreateElement("Config");
            XmlElement nodeData = _doc.SelectSingleNode($"Aitex/TableRecipeData") as XmlElement;

            nodeData.SetAttribute("CreatedBy", Creator);
            nodeData.SetAttribute("CreationTime", CreateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            nodeData.SetAttribute("LastRevisedBy", Revisor);
            nodeData.SetAttribute("LastRevisionTime", ReviseTime.ToString("yyyy-MM-dd HH:mm:ss"));
            nodeData.SetAttribute("Description", Description);
            nodeData.SetAttribute("RecipeChamberType", RecipeChamberType);
            nodeData.SetAttribute("Freeze", Freeze.ToString());
            nodeData.SetAttribute("RecipeVersion", RecipeVersion);

            XmlNode nodeModule = _doc.SelectSingleNode($"Aitex/TableRecipeData/Module[@Name='{_module}']");
            if (nodeModule == null)
            {
                nodeModule = _doc.CreateElement("Module");
                nodeData.AppendChild(nodeModule);
            }
            nodeModule.RemoveAll();
            (nodeModule as XmlElement).SetAttribute("Name", _module);
            int i = 0;
            foreach (ObservableCollection<Param> parameters in Steps)
            {
                XmlElement nodeWarning = _doc.CreateElement("Warning");
                XmlElement nodeAlarm = _doc.CreateElement("Alarm");
                Dictionary<string, XmlElement> nodePop = new Dictionary<string, XmlElement>();
                foreach (string key in PopEnable.Keys)
                {
                    nodePop.Add(key, _doc.CreateElement(key));
                }

                XmlElement nodeStep = _doc.CreateElement("Step");
                int stepNo = -1;
                foreach (Param parameter in parameters)
                {
                    if (parameter is StepParam)
                    {
                        int.TryParse(((StepParam)parameter).Value.ToString(), out stepNo);
                        break;
                    }
                }
                foreach (Param parameter in parameters)
                {
                    if (parameter.Visible != System.Windows.Visibility.Visible)
                        continue;

                    if (ToleranceEnable)
                    {
                        if (parameter.EnableTolerance && StepTolerances.Count > (stepNo - 1) * 3 + 2)
                        {
                            var warning = StepTolerances[(stepNo - 1) * 3 + 1].SingleOrDefault(x => x.Name == parameter.Name);
                            var alarm = StepTolerances[(stepNo - 1) * 3 + 2].SingleOrDefault(x => x.Name == parameter.Name);

                            if (warning != null && warning is DoubleParam)
                                nodeWarning.SetAttribute(parameter.Name, (warning as DoubleParam).Value);
                            if (alarm != null && alarm is DoubleParam)
                                nodeAlarm.SetAttribute(parameter.Name, (alarm as DoubleParam).Value);
                        }
                    }

                    if (parameter is IntParam)
                        nodeStep.SetAttribute(parameter.Name, ((IntParam)parameter).Value.ToString());
                    else if (parameter is DoubleParam)
                        nodeStep.SetAttribute(parameter.Name, ((DoubleParam)parameter).Value.ToString());
                    else if (parameter is StringParam)
                        nodeStep.SetAttribute(parameter.Name, ((StringParam)parameter).Value.ToString());
                    else if (parameter is ComboxParam)
                        nodeStep.SetAttribute(parameter.Name, ((ComboxParam)parameter).Options.First(x => x.DisplayName == ((ComboxParam)parameter).Value.ToString()).ControlName);
                    else if (parameter is LoopComboxParam)
                        nodeStep.SetAttribute(parameter.Name, ((LoopComboxParam)parameter).Value.ToString());
                    else if (parameter is PositionParam)
                        nodeStep.SetAttribute(parameter.Name, ((PositionParam)parameter).Value.ToString());
                    else if (parameter is BoolParam)
                        nodeStep.SetAttribute(parameter.Name, ((BoolParam)parameter).Value.ToString());
                    else if (parameter is StepParam)
                        nodeStep.SetAttribute(parameter.Name, ((StepParam)parameter).Value.ToString());
                    else if (parameter is MultipleSelectParam)
                    {
                        List<string> selected = new List<string>();
                        ((MultipleSelectParam)parameter).Options.Apply(
                             opt =>
                             {
                                 if (opt.IsChecked)
                                     selected.Add(opt.ControlName);
                             }
                                                    );
                        nodeStep.SetAttribute(parameter.Name, string.Join(",", selected));
                    }
                    else if (parameter is PopSettingParam)
                    {
                        SetAttribute(PopSettingSteps[parameter.Name][i], nodePop[parameter.Name]);
                    }
                }

                if (ToleranceEnable)
                {
                    nodeStep.AppendChild(nodeWarning);
                    nodeStep.AppendChild(nodeAlarm);
                }

                foreach (string key in PopEnable.Keys)
                {
                    if (PopEnable[key])
                    {
                        nodeStep.AppendChild(nodePop[key]);
                    }
                }

                nodeModule.AppendChild(nodeStep);

                i++;
            }


            foreach (Param parameter in ConfigSelItems)
            {
                ConfigSelItemXml(parameter, i);
            }
            nodeModule.AppendChild(nodeConfig);


            foreach (Param parameter in ConfigItems)
            {
                ConfigItemXml(parameter);
            }
            nodeModule.AppendChild(nodeConfig);

            return _doc.OuterXml;
        }

        public XmlElement ConfigSelItemXml(Param parameter, int i)
        {
            Dictionary<string, XmlElement> nodePop = new Dictionary<string, XmlElement>();
            foreach (string key in PopEnable.Keys)
            {
                nodePop.Add(key, _doc.CreateElement(key));
            }
            if (parameter is IntParam)
                nodeConfig.SetAttribute(parameter.Name, ((IntParam)parameter).Value.ToString());
            else if (parameter is DoubleParam)
                nodeConfig.SetAttribute(parameter.Name, ((DoubleParam)parameter).Value.ToString());
            else if (parameter is StringParam)
                nodeConfig.SetAttribute(parameter.Name, ((StringParam)parameter).Value.ToString());
            else if (parameter is ComboxParam)
                if (parameter.Name == "ParWok")
                {
                    nodeConfig.SetAttribute(parameter.Name, "Yes");

                }
                else
                {
                    nodeConfig.SetAttribute(parameter.Name, ((ComboxParam)parameter).Options.First(x => x.DisplayName == ((ComboxParam)parameter).Value.ToString()).ControlName);

                }
            else if (parameter is LoopComboxParam)
                nodeConfig.SetAttribute(parameter.Name, ((LoopComboxParam)parameter).Value.ToString());
            else if (parameter is PositionParam)
                nodeConfig.SetAttribute(parameter.Name, ((PositionParam)parameter).Value.ToString());
            else if (parameter is BoolParam)
                nodeConfig.SetAttribute(parameter.Name, ((BoolParam)parameter).Value.ToString());
            else if (parameter is StepParam)
                nodeConfig.SetAttribute(parameter.Name, ((StepParam)parameter).Value.ToString());
            else if (parameter is MultipleSelectParam)
            {
                List<string> selected = new List<string>();
                ((MultipleSelectParam)parameter).Options.Apply(
                     opt =>
                     {
                         if (opt.IsChecked)
                             selected.Add(opt.ControlName);
                     }
                                            );
                nodeConfig.SetAttribute(parameter.Name, string.Join(",", selected));
            }
            else if (parameter is PopSettingParam)
            {
                SetAttribute(PopSettingSteps[parameter.Name][i], nodePop[parameter.Name]);
            }
            return nodeConfig;
        }
        public XmlElement ConfigItemXml(Param parameter)
        {
            if (parameter.Visible == System.Windows.Visibility.Visible)
            {
                if (parameter is IntParam)
                    nodeConfig.SetAttribute(parameter.Name, ((IntParam)parameter).Value.ToString());
                else if (parameter is DoubleParam)
                {
                    string strValue = ((DoubleParam)parameter).Value;
                    bool succed = double.TryParse(strValue, out double dValue);

                    if (!succed)
                    {
                        MessageBox.Show($"The set value of {parameter.DisplayName} is {strValue}, not a valid value");
                        return null;
                    }

                    var config = ConfigItems.Where(m => m.Name == parameter.Name).FirstOrDefault();

                    if (config is DoubleParam param1)
                    {
                        if (param1.Minimun == 0 && param1.Maximun == 0)
                        {
                            //没有设定范围
                        }
                        else if (dValue > param1.Maximun || dValue < param1.Minimun)
                        {
                            MessageBox.Show($"The set value of {parameter.DisplayName} is {dValue}, out of the range {param1.Minimun}~{param1.Maximun}");
                            return null;
                        }
                    }
                    if (parameter.Name.Contains("Flush") && !parameter.Name.Contains("FlushTime"))
                    {

                        nodeConfig.SetAttribute(parameter.Name, String.Format("{0:F}", Convert.ToDouble(((DoubleParam)parameter).Value)));
                    }
                    else
                    {
                        nodeConfig.SetAttribute(parameter.Name, ((DoubleParam)parameter).Value.ToString());
                    }
                    //nodeConfig.SetAttribute(parameter.Name, ((DoubleParam)parameter).Value.ToString());
                }
                else if (parameter is StringParam)
                    nodeConfig.SetAttribute(parameter.Name, ((StringParam)parameter).Value.ToString());
                else if (parameter is ComboxParam)
                    nodeConfig.SetAttribute(parameter.Name, ((ComboxParam)parameter).Value.ToString());
                else if (parameter is LoopComboxParam)
                    nodeConfig.SetAttribute(parameter.Name, ((LoopComboxParam)parameter).Value.ToString());
                else if (parameter is PositionParam)
                    nodeConfig.SetAttribute(parameter.Name, ((PositionParam)parameter).Value.ToString());
                else if (parameter is BoolParam)
                    nodeConfig.SetAttribute(parameter.Name, ((BoolParam)parameter).Value.ToString());
                else if (parameter is StepParam)
                    nodeConfig.SetAttribute(parameter.Name, ((StepParam)parameter).Value.ToString());
                else if (parameter is MultipleSelectParam)
                {
                    List<string> selected = new List<string>();
                    ((MultipleSelectParam)parameter).Options.Apply(
                         opt =>
                         {
                             if (opt.IsChecked)
                                 selected.Add(opt.ControlName);
                         }
                                                );
                    nodeConfig.SetAttribute(parameter.Name, string.Join(",", selected));
                }
            }
            return nodeConfig;
        }

        private void SetAttribute(ObservableCollection<Param> Parameters, XmlElement PopSettingStep)
        {
            if (Parameters != null)

                foreach (Param parameter1 in Parameters)
                {
                    if (parameter1.Visible != System.Windows.Visibility.Visible)
                        continue;

                    if (parameter1 is IntParam)
                        PopSettingStep.SetAttribute(parameter1.Name, ((IntParam)parameter1).Value.ToString());
                    else if (parameter1 is DoubleParam)
                        PopSettingStep.SetAttribute(parameter1.Name, ((DoubleParam)parameter1).Value.ToString());
                    else if (parameter1 is StringParam)
                        PopSettingStep.SetAttribute(parameter1.Name, ((StringParam)parameter1).Value.ToString());
                    else if (parameter1 is ComboxParam)
                        PopSettingStep.SetAttribute(parameter1.Name, ((ComboxParam)parameter1).Value.ToString());
                    else if (parameter1 is LoopComboxParam)
                        PopSettingStep.SetAttribute(parameter1.Name, ((LoopComboxParam)parameter1).Value.ToString());
                    else if (parameter1 is PositionParam)
                        PopSettingStep.SetAttribute(parameter1.Name, ((PositionParam)parameter1).Value.ToString());
                    else if (parameter1 is BoolParam)
                        PopSettingStep.SetAttribute(parameter1.Name, ((BoolParam)parameter1).Value.ToString());
                    else if (parameter1 is StepParam)
                        PopSettingStep.SetAttribute(parameter1.Name, ((StepParam)parameter1).Value.ToString());
                    else if (parameter1 is MultipleSelectParam)
                    {
                        List<string> selected1 = new List<string>();
                        ((MultipleSelectParam)parameter1).Options.Apply(
                            opt =>
                            {
                                if (opt.IsChecked)
                                    selected1.Add(opt.ControlName);
                            }
                            );
                        PopSettingStep.SetAttribute(parameter1.Name, string.Join(",", selected1));
                    }
                }
        }

        public string ToXmlString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            builder.Append(string.Format("<Aitex><TableRecipeData CreatedBy=\"{0}\" CreationTime=\"{1}\" LastRevisedBy=\"{2}\" LastRevisionTime=\"{3}\" Description=\"{4}\"  RecipeChamberType=\"{5}\" RecipeVersion=\"{6}\" Freeze=\"{7}\">", this.Creator, this.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"), this.Revisor, this.ReviseTime.ToString("yyyy-MM-dd HH:mm:ss"), this.Description, RecipeChamberType, RecipeVersion,Freeze.ToString()));
            foreach (ObservableCollection<Param> parameters in Steps)
            {
                builder.Append("<Step ");
                foreach (Param parameter in parameters)
                {
                    if (parameter.Visible == System.Windows.Visibility.Visible)
                    {
                        if (parameter is IntParam)
                            builder.Append(parameter.Name + "=\"" + ((IntParam)parameter).Value + "\" ");
                        else if (parameter is DoubleParam)
                            builder.Append(parameter.Name + "=\"" + ((DoubleParam)parameter).Value + "\" ");
                        else if (parameter is StringParam)
                            builder.Append(parameter.Name + "=\"" + ((StringParam)parameter).Value + "\" ");
                        else if (parameter is ComboxParam)
                            builder.Append(parameter.Name + "=\"" + ((ComboxParam)parameter).Value + "\" ");
                        else if (parameter is LoopComboxParam)
                            builder.Append(parameter.Name + "=\"" + ((LoopComboxParam)parameter).Value + "\" ");
                        else if (parameter is PositionParam)
                            builder.Append(parameter.Name + "=\"" + ((PositionParam)parameter).Value + "\" ");
                        else if (parameter is BoolParam)
                            builder.Append(parameter.Name + "=\"" + ((BoolParam)parameter).Value + "\" ");
                        else if (parameter is StepParam)
                            builder.Append(parameter.Name + "=\"" + ((StepParam)parameter).Value + "\" ");
                        else if (parameter is MultipleSelectParam)
                        {
                            List<string> selected = new List<string>();
                            ((MultipleSelectParam)parameter).Options.Apply(
                                opt =>
                                {
                                    if (opt.IsChecked)
                                        selected.Add(opt.ControlName);
                                }
                                );
                            builder.Append(parameter.Name + "=\"" + string.Join(",", selected) + "\" ");
                        }
                    }
                }
                builder.Append("/>");
            }

            builder.Append("<Config ");
            foreach (Param parameter in ConfigItems)
            {
                if (parameter.Visible == System.Windows.Visibility.Visible)
                {
                    if (parameter is IntParam)
                        builder.Append(parameter.Name + "=\"" + ((IntParam)parameter).Value + "\" ");
                    else if (parameter is DoubleParam)
                        builder.Append(parameter.Name + "=\"" + ((DoubleParam)parameter).Value + "\" ");
                    else if (parameter is StringParam)
                        builder.Append(parameter.Name + "=\"" + ((StringParam)parameter).Value + "\" ");
                    else if (parameter is ComboxParam)
                        builder.Append(parameter.Name + "=\"" + ((ComboxParam)parameter).Value + "\" ");
                    else if (parameter is LoopComboxParam)
                        builder.Append(parameter.Name + "=\"" + ((LoopComboxParam)parameter).Value + "\" ");
                    else if (parameter is PositionParam)
                        builder.Append(parameter.Name + "=\"" + ((PositionParam)parameter).Value + "\" ");
                    else if (parameter is BoolParam)
                        builder.Append(parameter.Name + "=\"" + ((BoolParam)parameter).Value + "\" ");
                    else if (parameter is StepParam)
                        builder.Append(parameter.Name + "=\"" + ((StepParam)parameter).Value + "\" ");
                    else if (parameter is MultipleSelectParam)
                    {
                        List<string> selected = new List<string>();
                        ((MultipleSelectParam)parameter).Options.Apply(
                            opt =>
                            {
                                if (opt.IsChecked)
                                    selected.Add(opt.ControlName);
                            }
                            );
                        builder.Append(parameter.Name + "=\"" + string.Join(",", selected) + "\" ");
                    }
                }


            }
            builder.Append("/>");

            builder.Append("</TableRecipeData></Aitex>");
            return builder.ToString();
        }

    }
}
