using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Aitex.Core.RT.Log;
using Caliburn.Micro;
using Caliburn.Micro.Core;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.Editors.Recipe;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.Command;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;
using SciChart.Core.Extensions;

namespace MECF.Framework.UI.Client.CenterViews.Editors.RecipeConfig
{
    public class RecipeConfigViewModel : BaseModel
    {
        public class RecipeConfigItem : NotifiableItem
        {
            public string ControlName { get; set; }

            public string DisplayName { get; set; }

            public float DefaultValue { get; set; }

            public float MinValue { get; set; }

            public float MaxValue { get; set; }

            public float WarnValue { get; set; }

            public float AlarmValue { get; set; }

            public bool EnableDefaultValue { get; set; }

            public bool EnableMinValue { get; set; }

            public bool EnableMaxValue { get; set; }

            public bool EnableWarnValue { get; set; }

            public bool EnableAlarmValue { get; set; }

            public RecipeConfigItem()
            {
                EnableDefaultValue = true;
                EnableMinValue = true;
                EnableMaxValue = true;
                EnableWarnValue = true;
                EnableAlarmValue = true;
            }
        }

        public bool IsPermission { get => this.Permission == 3; }//&& RtStatus != "AutoRunning";

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

        public bool EnableNew { get; set; }
        public bool EnableReName { get; set; }
        public bool EnableCopy { get; set; }
        public bool EnableDelete { get; set; }
        public bool EnableSave { get; set; }
        public bool EnableStep { get; set; }
        public bool EnableReload { get; set; }
        public bool EnableSaveToAll { get; set; }
        public bool EnableSaveTo { get; set; }

        private RecipeFormatBuilder _columnBuilder = new RecipeFormatBuilder();

        private EditMode editMode;

        private RecipeProvider _recipeProvider = new RecipeProvider();

        public ObservableCollection<RecipeConfigItem> StepParameters { get; set; }
        public ObservableCollection<RecipeConfigItem> OesParameters { get; set; }
        public ObservableCollection<RecipeConfigItem> VatParameters { get; set; }
        public ObservableCollection<RecipeConfigItem> FineTuningParameters { get; set; }


        public ObservableCollection<string> ChamberType { get; set; }
        public int ChamberTypeIndexSelection { get; set; }

        public string CurrentChamberType
        {
            get
            {
                return ChamberType[ChamberTypeIndexSelection];
            }
        }

        public Visibility OesVisibility { get; set; }
        public Visibility VatVisibility { get; set; }
        public Visibility FineTuningVisibility { get; set; }

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

            this.Columns = this._columnBuilder.Build(CurrentChamberType);

            OesVisibility = _columnBuilder.OesConfig.Count > 0 ? Visibility.Visible : Visibility.Hidden;
            VatVisibility = _columnBuilder.VatConfig.Count > 0 ? Visibility.Visible : Visibility.Hidden;
            FineTuningVisibility = _columnBuilder.FineTuningConfig.Count > 0 ? Visibility.Visible : Visibility.Hidden;

            this.CurrentRecipe = new RecipeData();
            CurrentRecipe.RecipeChamberType = _columnBuilder.RecipeChamberType;
            CurrentRecipe.RecipeVersion = _columnBuilder.RecipeVersion;

            this.editMode = EditMode.None;

            InitStepParameter();
            InitOesParameter();
            InitVatParameter();
            InitFineTuningParameter();
        }

        private void InitStepParameter()
        {
            //name1,default,min,max,warn,alarm; name2,default,min,max,warn,alarm;

            StepParameters = new ObservableCollection<RecipeConfigItem>();

            var stepParameterConfig = QueryDataClient.Instance.Service.GetConfig($"System.Recipe.StepParameter_{CurrentChamberType}");
            var stepParameterValues = new Dictionary<string, string[]>();
            if (stepParameterConfig != null)
            {
                string[] parameters = stepParameterConfig.ToString().Split(';');

                foreach (var parameter in parameters)
                {
                    string[] values = parameter.Split(',');
                    if (values.Length != 6)
                        continue;

                    stepParameterValues[values[0]] = values;
                }
            }

            foreach (var col in Columns)
            {
                if (col.ControlName.IsNullOrEmpty())
                    continue;

                if (!col.EnableConfig)
                    continue;

                var item = new RecipeConfigItem();
                item.ControlName = col.ControlName;
                item.DisplayName = col.DisplayName;

                if (stepParameterValues.ContainsKey(item.ControlName))
                {
                    string[] values = stepParameterValues[item.ControlName];
                    if (float.TryParse(values[1], out float defaultValue))
                        item.DefaultValue = defaultValue;
                    if (float.TryParse(values[1], out float minValue))
                        item.MinValue = minValue;
                    if (float.TryParse(values[1], out float maxValue))
                        item.MaxValue = maxValue;
                    if (float.TryParse(values[1], out float warnValue))
                        item.WarnValue = warnValue;
                    if (float.TryParse(values[1], out float alarmValue))
                        item.AlarmValue = alarmValue;
                }

                StepParameters.Add(item);
            }
        }

        private void InitOesParameter()
        {
            OesParameters = GetParameters(QueryDataClient.Instance.Service.GetConfig($"System.Recipe.OesParameter_{CurrentChamberType}"), _columnBuilder.OesConfig);
        }


        private void InitVatParameter()
        {
            VatParameters = GetParameters(QueryDataClient.Instance.Service.GetConfig($"System.Recipe.VatParameter_{CurrentChamberType}"), _columnBuilder.VatConfig); 
        }

        private void InitFineTuningParameter()
        {
            FineTuningParameters = GetParameters(QueryDataClient.Instance.Service.GetConfig($"System.Recipe.FineTuningParameter_{CurrentChamberType}"), _columnBuilder.FineTuningConfig);
        }

        private ObservableCollection<RecipeConfigItem> GetParameters(object config, ObservableCollection<Param> Configs)
        {
            //name1,default,min,max,warn,alarm; name2,default,min,max,warn,alarm;
            ObservableCollection<RecipeConfigItem> Parameters = new ObservableCollection<RecipeConfigItem>();
            var dicConfig = new Dictionary<string, string[]>();
            if (config != null)
            {
                string[] parameters = config.ToString().Split(';');

                foreach (var parameter in parameters)
                {
                    string[] values = parameter.Split(',');
                    if (values.Length != 6)
                        continue;

                    dicConfig[values[0]] = values;
                }
            }

            foreach (var col in Configs)
            {
                var item = new RecipeConfigItem();
                item.ControlName = col.Name;
                item.DisplayName = col.DisplayName;

                if (dicConfig.ContainsKey(item.ControlName))
                {
                    string[] values = dicConfig[item.ControlName];
                    if (float.TryParse(values[1], out float defaultValue))
                        item.DefaultValue = defaultValue;
                    if (float.TryParse(values[1], out float minValue))
                        item.MinValue = minValue;
                    if (float.TryParse(values[1], out float maxValue))
                        item.MaxValue = maxValue;
                    if (float.TryParse(values[1], out float warnValue))
                        item.WarnValue = warnValue;
                    if (float.TryParse(values[1], out float alarmValue))
                        item.AlarmValue = alarmValue;
                }

                Parameters.Add(item);
            }

            return Parameters;
        }

        protected override void OnActivate()
        {
            base.OnActivate();
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
        }


        private void ClearData()
        {
            this.editMode = EditMode.None;
            this.CurrentRecipe.Clear();
            this.CurrentRecipe.Name = string.Empty;
            this.CurrentRecipe.Description = string.Empty;
        }


    }
}
