using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using Aitex.Core.RT.Log;
using MECF.Framework.UI.Client.CenterViews.Configs.SystemConfig;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    public class RecipeFormatBuilder
    {
        public ObservableCollection<EditorDataGridTemplateColumnBase> Columns
        {
            get;
            set;
        }

        public ObservableCollection<Param> Configs
        {
            get;
            set;
        }


        public ObservableCollection<Param> OesConfig
        {
            get;
            set;
        }


        public ObservableCollection<Param> VatConfig
        {
            get;
            set;
        }
        public ObservableCollection<Param> BrandConfig
        {
            get;
            set;
        }

        public ObservableCollection<Param> FineTuningConfig
        {
            get;
            set;
        }

        public ObservableCollection<Param> ProcessSelConfig
        {
            get;
            set;
        }

        public string RecipeChamberType
        {
            get;
            set;
        }

        public string RecipeVersion
        {
            get;
            set;
        }

        private RecipeProvider recipeProvider = new RecipeProvider();

        public ObservableCollection<EditorDataGridTemplateColumnBase> Build(string path)
        {
            var str = recipeProvider.GetRecipeFormatXml(path);
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.LoadXml(str);

                XmlNode nodeRoot = doc.SelectSingleNode("TableRecipeFormat");

                RecipeChamberType = nodeRoot.Attributes["RecipeChamberType"].Value;

                RecipeVersion = nodeRoot.Attributes["RecipeVersion"].Value;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return null;
            }

            var columns = new ObservableCollection<EditorDataGridTemplateColumnBase>();

            EditorDataGridTemplateColumnBase col = null;
            XmlNodeList nodes = doc.SelectNodes("TableRecipeFormat/Catalog/Group");
            foreach (XmlNode node in nodes)
            {
                columns.Add(new ExpanderColumn()
                {
                    DisplayName = node.Attributes["DisplayName"].Value,
                    StringCellTemplate = "TemplateExpander",
                    StringHeaderTemplate = "ParamExpander"
                });

                XmlNodeList childNodes = node.SelectNodes("Step");
                foreach (XmlNode step in childNodes)
                {
                    if (string.IsNullOrEmpty(step.Attributes["DisplayName"].Value))
                        continue;

                    //step number
                    if (step.Attributes["ControlName"].Value == "StepNo")
                    {
                        col = new StepColumn()
                        {
                            DisplayName = "Step",
                            ControlName = "StepNo",
                            StringCellTemplate = "TemplateStep",
                            StringHeaderTemplate = "ParamTemplate"
                        };
                        columns.Add(col);
                        continue;
                    }

                    switch (step.Attributes["InputType"].Value)
                    {
                        case "TextInput":
                            col = new TextBoxColumn()
                            {
                                ModuleName = step.Attributes["ModuleName"].Value,
                                ControlName = step.Attributes["ControlName"].Value,
                                DisplayName = step.Attributes["DisplayName"].Value,
                                StringCellTemplate = "TemplateText",
                                StringHeaderTemplate = "ParamTemplate",
                                EnableConfig = step.Attributes["EnableConfig"] != null && Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                EnableTolerance = step.Attributes["EnableTolerance"] != null && Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                            };
                            columns.Add(col);
                            break;
                        case "ReadOnly":
                            col = new TextBoxColumn()
                            {
                                ModuleName = step.Attributes["ModuleName"].Value,
                                ControlName = step.Attributes["ControlName"].Value,
                                DisplayName = step.Attributes["DisplayName"].Value,
                                StringCellTemplate = "TemplateText",
                                StringHeaderTemplate = "ParamTemplate",
                                IsReadOnly = true,
                                EnableConfig = step.Attributes["EnableConfig"] != null && Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                EnableTolerance = step.Attributes["EnableTolerance"] != null && Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                            };
                            columns.Add(col);
                            break;
                        case "NumInput":
                            col = new NumColumn()
                            {
                                ModuleName = step.Attributes["ModuleName"].Value,
                                ControlName = step.Attributes["ControlName"].Value,
                                DisplayName = step.Attributes["DisplayName"].Value,
                                InputMode = step.Attributes["InputMode"].Value,
                                Minimun = double.Parse(step.Attributes["Min"].Value),
                                Maximun = double.Parse(step.Attributes["Max"].Value),
                                StringCellTemplate = "TemplateNumber",
                                StringHeaderTemplate = "ParamTemplate",
                                EnableConfig = step.Attributes["EnableConfig"] != null && Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                EnableTolerance = step.Attributes["EnableTolerance"] != null && Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                            };
                            columns.Add(col);
                            break;
                        case "DoubleInput":
                            col = new DoubleColumn()
                            {
                                ModuleName = step.Attributes["ModuleName"].Value,
                                ControlName = step.Attributes["ControlName"].Value,
                                DisplayName = step.Attributes["DisplayName"].Value,
                                InputMode = step.Attributes["InputMode"].Value,
                                Minimun = double.Parse(step.Attributes["Min"].Value),
                                Maximun = double.Parse(step.Attributes["Max"].Value),
                                StringCellTemplate = "TemplateNumber",
                                StringHeaderTemplate = "ParamTemplate",
                                EnableConfig = step.Attributes["EnableConfig"] != null && Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                EnableTolerance = step.Attributes["EnableTolerance"] != null && Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                            };
                            columns.Add(col);
                            break;
                        case "EditableSelection":
                        case "ReadOnlySelection":
                            col = new ComboxColumn()
                            {
                                IsReadOnly = step.Attributes["InputType"].Value == "ReadOnlySelection",
                                ModuleName = step.Attributes["ModuleName"].Value,
                                Default = step.Attributes["Default"] != null ? step.Attributes["Default"].Value : "",
                                ControlName = step.Attributes["ControlName"].Value,
                                DisplayName = step.Attributes["DisplayName"].Value,
                                StringCellTemplate = "TemplateCombox",
                                StringHeaderTemplate = "ParamTemplate",
                                EnableConfig = step.Attributes["EnableConfig"] != null && Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                EnableTolerance = step.Attributes["EnableTolerance"] != null && Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                            };
                            XmlNodeList items = step.SelectNodes("Item");
                            foreach (XmlNode item in items)
                            {
                                ComboxColumn.Option opt = new ComboxColumn.Option();
                                opt.ControlName = item.Attributes["ControlName"].Value;
                                opt.DisplayName = item.Attributes["DisplayName"].Value;
                                ((ComboxColumn)col).Options.Add(opt);
                            }
                            columns.Add(col);
                            break;
                        case "LoopSelection":
                            col = new LoopComboxColumn()
                            {
                                IsReadOnly = false,
                                ModuleName = step.Attributes["ModuleName"].Value,
                                ControlName = step.Attributes["ControlName"].Value,
                                DisplayName = step.Attributes["DisplayName"].Value,
                                StringCellTemplate = "TemplateCombox",
                                StringHeaderTemplate = "ParamTemplate",
                                EnableConfig = step.Attributes["EnableConfig"] != null && Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                EnableTolerance = step.Attributes["EnableTolerance"] != null && Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                            };
                            XmlNodeList options = step.SelectNodes("Item");
                            foreach (XmlNode item in options)
                            {
                                LoopComboxColumn.Option opt = new LoopComboxColumn.Option();
                                opt.ControlName = item.Attributes["ControlName"].Value;
                                opt.DisplayName = item.Attributes["DisplayName"].Value;
                                ((LoopComboxColumn)col).Options.Add(opt);
                            }
                            columns.Add(col);
                            break;
                        case "PopSetting":
                            col = new PopSettingColumn()
                            {
                                ModuleName = step.Attributes["ModuleName"].Value,
                                ControlName = step.Attributes["ControlName"].Value,
                                DisplayName = step.Attributes["DisplayName"].Value,
                                StringCellTemplate = "TemplatePopSetting",
                                StringHeaderTemplate = "ParamTemplate",
                                EnableConfig = step.Attributes["EnableConfig"] != null && Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                EnableTolerance = step.Attributes["EnableTolerance"] != null && Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                            };
                            columns.Add(col);
                            break;
                    }
                    this.SetFeedback(col);
                }
            }

            Columns = columns;

            var configs = new ObservableCollection<Param>();
            nodes = doc.SelectNodes("TableRecipeFormat/ProcessConfig/Configs");
            foreach (XmlNode node in nodes)
            {
                XmlNodeList childNodes = node.SelectNodes("Config");
                foreach (XmlNode configNode in childNodes)
                {
                    switch (configNode.Attributes["InputType"].Value)
                    {

                        case "DoubleInput":
                            var config = new DoubleParam()
                            {
                                Name = configNode.Attributes["ControlName"].Value,
                                Value = configNode.Attributes["Default"].Value,
                                DisplayName = configNode.Attributes["DisplayName"].Value,
                                EnableConfig = configNode.Attributes["EnableConfig"] != null && Convert.ToBoolean(configNode.Attributes["EnableConfig"].Value),
                                EnableTolerance = configNode.Attributes["EnableTolerance"] != null && Convert.ToBoolean(configNode.Attributes["EnableTolerance"].Value),
                            };
                            if (double.TryParse(configNode.Attributes["Max"].Value, out double max))
                            {
                                (config as DoubleParam).Maximun = max;
                            }
                            if (double.TryParse(configNode.Attributes["Min"].Value, out double min))
                            {
                                (config as DoubleParam).Minimun = min;
                            }
                            configs.Add(config);
                            break;

                    }
                }
            }

            nodes = doc.SelectNodes("TableRecipeFormat/ProcessSelConfig/Configs");
            foreach (XmlNode node in nodes)
            {
                XmlNodeList childNodes = node.SelectNodes("Step");
                foreach (XmlNode configNode in childNodes)
                {
                    switch (configNode.Attributes["InputType"].Value)
                    {

                        case "ReadOnlySelection":

                            var cols = new ComboxParam()
                            {
                                Name = configNode.Attributes["ControlName"].Value,
                                DisplayName = configNode.Attributes["DisplayName"].Value,
                                Value = configNode.Attributes["Default"] != null ? configNode.Attributes["Default"].Value : "",
                                Options = new ObservableCollection<ComboxColumn.Option>(),
                                IsEditable = configNode.Attributes["InputType"].Value == "ReadOnlySelection",
                                EnableTolerance = configNode.Attributes["EnableTolerance"] != null && Convert.ToBoolean(configNode.Attributes["EnableTolerance"].Value),

                            };
                            XmlNodeList items = configNode.SelectNodes("Item");
                            foreach (XmlNode item in items)
                            {
                                ComboxColumn.Option opt = new ComboxColumn.Option();
                                opt.ControlName = item.Attributes["ControlName"].Value;
                                opt.DisplayName = item.Attributes["DisplayName"].Value;
                                cols.Options.Add(opt);
                            }
                            cols.Value = !string.IsNullOrEmpty(cols.Value) ? cols.Value : (cols.Options.Count > 0 ? cols.Options[0].ControlName : "");
                            configs.Add(cols);
                            break;

                    }
                }
            }


            Configs = configs;

            BrandConfig = GetConfig(doc.SelectNodes("TableRecipeFormat/BrandConfig/Configs"));
            OesConfig = GetConfig(doc.SelectNodes("TableRecipeFormat/OesConfig/Configs"));
            VatConfig = GetConfig(doc.SelectNodes("TableRecipeFormat/VatConfig/Configs"));
            FineTuningConfig = GetConfig(doc.SelectNodes("TableRecipeFormat/FineTuningConfig/Configs"));

            ProcessSelConfig = GetConfig(doc.SelectNodes("TableRecipeFormat/ProcessSelConfig/Configs"));

            return Columns;
        }

        private ObservableCollection<Param> GetConfig(XmlNodeList nodes)
        {
            var configs = new ObservableCollection<Param>();
            foreach (XmlNode node in nodes)
            {
                XmlNodeList childNodes = node.SelectNodes("Config");
                foreach (XmlNode configNode in childNodes)
                {
                    switch (configNode.Attributes["InputType"].Value)
                    {
                        case "TextInput":
                            var text = new StringParam()
                            {
                                Name = configNode.Attributes["ControlName"].Value,
                                Value = configNode.Attributes["Default"].Value,
                                DisplayName = configNode.Attributes["DisplayName"].Value,
                            };
                            configs.Add(text);
                            break;
                        case "DoubleInput":
                            var config = new DoubleParam()
                            {
                                Name = configNode.Attributes["ControlName"].Value,
                                Value = configNode.Attributes["Default"].Value,
                                DisplayName = configNode.Attributes["DisplayName"].Value,
                            };
                            if (double.TryParse(configNode.Attributes["Max"].Value, out double max))
                            {
                                (config as DoubleParam).Maximun = max;
                            }
                            if (double.TryParse(configNode.Attributes["Min"].Value, out double min))
                            {
                                (config as DoubleParam).Minimun = min;
                            }
                            configs.Add(config);
                            break;
                        case "ReadOnlySelection":

                            var col = new ComboxParam()
                            {
                                Name = configNode.Attributes["ControlName"].Value,
                                DisplayName = configNode.Attributes["DisplayName"].Value,
                                Value = configNode.Attributes["Default"] != null ? configNode.Attributes["Default"].Value : "",
                                Options = new ObservableCollection<ComboxColumn.Option>(),
                                IsEditable = configNode.Attributes["InputType"].Value == "ReadOnlySelection",
                                EnableTolerance = configNode.Attributes["EnableTolerance"] != null && Convert.ToBoolean(configNode.Attributes["EnableTolerance"].Value),

                            };
                            XmlNodeList items = configNode.SelectNodes("Item");
                            foreach (XmlNode item in items)
                            {
                                ComboxColumn.Option opt = new ComboxColumn.Option();
                                opt.ControlName = item.Attributes["ControlName"].Value;
                                opt.DisplayName = item.Attributes["DisplayName"].Value;
                                col.Options.Add(opt);
                            }
                            col.Value = !string.IsNullOrEmpty(col.Value) ? col.Value : (col.Options.Count > 0 ? col.Options[0].ControlName : "");
                            configs.Add(col);
                            break;

                    }
                }
            }

            return configs;
        }

        public static void ApplyTemplate(UserControl uc, ObservableCollection<EditorDataGridTemplateColumnBase> columns)
        {
            columns.ToList().ForEach(col =>
            {
                col.CellTemplate = (DataTemplate)uc.FindResource(col.StringCellTemplate);
                col.HeaderTemplate = (DataTemplate)uc.FindResource(col.StringHeaderTemplate);
            });
        }

        private void SetFeedback(EditorDataGridTemplateColumnBase col)
        {
            if (col.ControlName == "TSS")
            {
                col.Feedback = (p) =>
                {
                    ComboxParam nump = (ComboxParam)p;
                    #region 将所有参数框复原到可输入状态
                    foreach (Param param in nump.Parent)
                    {
                        param.IsEnabled = true;
                    }
                    #endregion
                    if (nump.Value == "Yes")
                    {
                        foreach (Param param in nump.Parent)
                        {
                            if (param.Name == "WaferGasPressure")
                            {
                                ((DoubleParam)param).Value = "0";
                                param.IsEnabled = false;
                            }

                            if (param.Name == "GateValve")
                            {
                                ((ComboxParam)param).Value = "Close";
                                param.IsEnabled = false;
                            }

                            if (param.Name == "DCPower.SetPowerForRecipe" || param.Name == "MksRfPlasmaGenerator.SetPowerForRecipe" || param.Name == "DxkdpDCPower.SetCurrentForRecipe")
                            {
                                ((DoubleParam)param).Value = "0";
                                param.IsEnabled = false;
                            }

                            if (param.Name == "Final1Valve.GVTurnValve")
                            {
                                ((ComboxParam)param).Value = "Open";
                                param.IsEnabled = false;
                            }
                        }
                    }
                };
            }
        }
    }
}
