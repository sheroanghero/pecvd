using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
using Caliburn.Micro.Core;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Sequence
{
    public class SequenceData : PropertyChangedBase
    {
        public SequenceData()
        {
            this.Steps = new ObservableCollection<ObservableCollection<Param>>();
        }

        public ObservableCollection<ObservableCollection<Param>> Steps { get; set; }

        public ObservableCollection<Param> CreateStep(ObservableCollection<EditorDataGridTemplateColumnBase> _columns, XmlNode step = null)
        {
            PositionParam posParam = null;
            string posValue = string.Empty;

            ObservableCollection<Param> Params = new ObservableCollection<Param>();
            foreach (EditorDataGridTemplateColumnBase col in _columns)
            {
                Param parm = null;
                string value = string.Empty;
                if (!(col is ExpanderColumn) && step != null && !(col is StepColumn))
                {
                    if (step.Attributes[col.ControlName] != null)
                        value = step.Attributes[col.ControlName].Value;
                }

                if (col is StepColumn)
                {
                    parm = new StepParam()
                    {
                        Name = col.ControlName
                    };
                }
                else if (col is TextBoxColumn)
                {
                    parm = new StringParam()
                    {
                        Name = col.ControlName,
                        Value = value,
                        IsEnabled = !col.IsReadOnly
                    };
                }
                else if (col is NumColumn)
                {
                    if (!string.IsNullOrEmpty(value))
                        parm = new IntParam() { Name = col.ControlName, Value = int.Parse(value), IsEnabled = !col.IsReadOnly };
                    else
                        parm = new IntParam() { Name = col.ControlName, Value = 0, IsEnabled = !col.IsReadOnly };
                }
                else if (col is ComboxColumn)
                {
                    parm = new ComboxParam() { Name = col.ControlName,
                        Value = value, Options = ((ComboxColumn)col).Options, IsEnabled = !col.IsReadOnly };
                }
                else if (col is PositionColumn)
                {
                    posParam = new PositionParam() { Name = col.ControlName, Options = ((PositionColumn)col).Options, IsEnabled = !col.IsReadOnly };
                    parm = posParam;
                    posValue = value;
                }
                else if (col is ExpanderColumn)
                {
                    parm = new ExpanderParam() { };
                }
                else if (col is RecipeSelectColumn)
                {
                    string path = ((RecipeSelectColumn) col).RecipeProcessType;
                    if (!string.IsNullOrEmpty(path) && value.StartsWith(path))
                    {
                        value = value.Substring(path.Length + 1);
                    }

                    parm = new PathFileParam()
                    {
                        Name = col.ControlName,
                        Value = value,
                        IsEnabled = !col.IsReadOnly,
                        PrefixPath = ((RecipeSelectColumn)col).RecipeProcessType
                    };
                }
                else if (col is MultipleSelectColumn)
                {
                    parm = new MultipleSelectParam((MultipleSelectColumn)col) { Name = col.ControlName, IsEnabled = !col.IsReadOnly };
                    string[] opts = value.Split(',');
                    ((MultipleSelectParam)parm).Options.Apply(opt => {
                        for(var index = 0; index < opts.Length; index++)
                        {
                            if (opt.ControlName == opts[index])
                            {
                                opt.IsChecked = true;
                                break;
                            }
                        }
                    });
                    parm.IsSaved = true;
                }

                parm.Parent = Params;

                Params.Add(parm);
            }

            posParam.Value = posValue;

            return Params;
        }

        public ObservableCollection<Param> CloneStep(ObservableCollection<EditorDataGridTemplateColumnBase> _columns, ObservableCollection<Param> _sourceParams)
        {
            ObservableCollection<Param> targetParams = this.CreateStep(_columns);

            PositionParam posParam = null;
            string posValue = string.Empty;
            for (var index = 0; index < _sourceParams.Count; index++)
            {
                if (_sourceParams[index] is PositionParam)
                {
                    posParam = (PositionParam)targetParams[index];
                    posValue = ((PositionParam)_sourceParams[index]).Value;
                }
                else if (_sourceParams[index] is StringParam)
                {
                    ((StringParam)targetParams[index]).Value = ((StringParam)_sourceParams[index]).Value;
                }
                else if (_sourceParams[index] is PathFileParam)
                {
                    ((PathFileParam)targetParams[index]).Value = ((PathFileParam)_sourceParams[index]).Value;
                    ((PathFileParam)targetParams[index]).PrefixPath = ((PathFileParam)_sourceParams[index]).PrefixPath;
                }
                else if (_sourceParams[index] is IntParam)
                {
                    ((IntParam)targetParams[index]).Value = ((IntParam)_sourceParams[index]).Value;
                }
                else if (_sourceParams[index] is ComboxParam)
                {
                    ((ComboxParam)targetParams[index]).Value = ((ComboxParam)_sourceParams[index]).Value;
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
            posParam.Value = posValue;

            return targetParams;
        }

        public void LoadSteps(ObservableCollection<EditorDataGridTemplateColumnBase> _columns, XmlNodeList _steps)
        {
            foreach (XmlNode _step in _steps)
            {
                this.Steps.Add(this.CreateStep(_columns, _step));
            }
        }

        public void Load(ObservableCollection<EditorDataGridTemplateColumnBase> _columns, XmlDocument doc)
        {
            XmlNode node = doc.SelectSingleNode("Aitex/TableSequenceData");
            if (node.Attributes["CreatedBy"] != null)
                this.Creator = node.Attributes["CreatedBy"].Value;
            if (node.Attributes["CreationTime"] != null)
                this.CreateTime = DateTime.Parse(node.Attributes["CreationTime"].Value);
            if (node.Attributes["LastRevisedBy"] != null)
                this.Revisor = node.Attributes["LastRevisedBy"].Value;
            if (node.Attributes["LastRevisionTime"] != null)
                this.ReviseTime = DateTime.Parse(node.Attributes["LastRevisionTime"].Value);
            if (node.Attributes["Description"] != null)
                this.Description = node.Attributes["Description"].Value;
            if (node.Attributes["Freeze"] != null)
                this.Freeze = Convert.ToBoolean(node.Attributes["Freeze"].Value);
            foreach (XmlNode _step in doc.SelectNodes("Aitex/TableSequenceData/Step"))
            {
                this.Steps.Add(this.CreateStep(_columns, _step));
            }
        }

        public string ToXml()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            builder.Append(string.Format("<Aitex><TableSequenceData CreatedBy=\"{0}\" CreationTime=\"{1}\" LastRevisedBy=\"{2}\" LastRevisionTime=\"{3}\" Description=\"{4}\" Freeze=\"{5}\" >", this.Creator, this.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"), this.Revisor, this.ReviseTime.ToString("yyyy-MM-dd HH:mm:ss"), this.Description, this.Freeze));
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
                        else if (parameter is PathFileParam)
                            builder.Append(parameter.Name + "=\"" + ((PathFileParam)parameter).Value + "\" ");
                        else if (parameter is ComboxParam)
                            builder.Append(parameter.Name + "=\"" + ((ComboxParam)parameter).Value + "\" ");
                        else if (parameter is PositionParam)
                            builder.Append(parameter.Name + "=\"" + ((PositionParam)parameter).Value + "\" ");
                        else if (parameter is BoolParam)
                            builder.Append(parameter.Name + "=\"" + ((BoolParam)parameter).Value + "\" ");
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
            builder.Append("</TableSequenceData></Aitex>");
            return builder.ToString();
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

    }
}
