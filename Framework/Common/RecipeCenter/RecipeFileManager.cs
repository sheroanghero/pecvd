using Aitex.Common.Util;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using Aitex.Core.WCF;
using MECF.Framework.Common.Properties;
using MECF.Framework.Common.RecipeCenter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace Aitex.Core.RT.RecipeCenter
{
    public class RecipeFileManager : Singleton<RecipeFileManager>
    {
        //sequence文件 统一放在 Recipes/Sequence 文件夹下面
        public const string SequenceFolder = "Sequence";
        public const string SourceModule = "Recipe";
        public const string WaferFlowFolder = "WaferFlow";
        string _chamberId;

        private bool _recipeIsValid;

        private List<string> _validationErrors = new List<string>();

        private List<string> _validationWarnings = new List<string>();

        IRecipeFileContext _rcpContext;
        private ISequenceFileContext _seqContext;

        public RecipeFileManager()
        {
            _chamberId = SC.GetStringValue("System.Recipe.RecipeChamberType");
            if (_chamberId == null)
                _chamberId = "Furnace";
        }
        public void Initialize(IRecipeFileContext context)
        {
            Initialize(context, null, true);
        }
        public void Initialize(IRecipeFileContext context, bool enableService)
        {
            Initialize(context, null, enableService);

        }

        public void Initialize(IRecipeFileContext rcpContext, ISequenceFileContext seqContext)
        {
            Initialize(rcpContext, seqContext, true);
        }

        public void Initialize(IRecipeFileContext rcpContext, ISequenceFileContext seqContext, bool enableService)
        {
            _rcpContext = rcpContext == null ? new DefaultRecipeFileContext() : rcpContext;
            _seqContext = seqContext == null ? new DefaultSequenceFileContext() : seqContext;

            CultureSupported.UpdateCoreCultureResource(CultureSupported.English);

            if (enableService)
            {
                Singleton<WcfServiceManager>.Instance.Initialize(new Type[]
                {
                    typeof(RecipeService)
                });
            }

            var dir = string.Format("{0}{1}\\", PathManager.GetRecipeDir(), SequenceFolder);
            DirectoryInfo di = new DirectoryInfo(dir);
            if (!di.Exists)
            {
                di.Create();
            }
        }

        private void ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            switch (e.Severity)
            {
                case XmlSeverityType.Error:
                    _validationErrors.Add(e.Message);
                    _recipeIsValid = false;
                    break;
                case XmlSeverityType.Warning:
                    _validationWarnings.Add(e.Message);
                    break;
            }
        }

        /// <summary>
        /// XML schema checking
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="recipeName"></param>
        /// <param name="recipeContent"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public bool ValidateRecipe(string chamberId, string recipeName, string recipeContent, out List<string> reason)
        {
            try
            {
                XmlDocument document = new XmlDocument();

                document.LoadXml(recipeContent);

                MemoryStream schemaStream = new MemoryStream(ASCIIEncoding.ASCII.GetBytes(GetRecipeSchema(chamberId)));

                XmlReader xmlSchemaReader = XmlReader.Create(schemaStream);

                XmlSchema schema1 = XmlSchema.Read(xmlSchemaReader, ValidationEventHandler);

                document.Schemas.Add(schema1);

                document.LoadXml(recipeContent);

                ValidationEventHandler eventHandler = new ValidationEventHandler(ValidationEventHandler);

                _recipeIsValid = true;

                _validationErrors = new List<string>();

                _validationWarnings = new List<string>();

                // Validates recipe.
                document.Validate(eventHandler);
            }
            catch (Exception ex)
            {
                LOG.Write(ex.Message);
                _recipeIsValid = false;
            }
            if (!_recipeIsValid && _validationErrors.Count == 0)
            {
                _validationErrors.Add(Resources.RecipeFileManager_ValidateRecipe_XMLSchemaValidateFailed);
            }
            reason = _validationErrors;
            return _recipeIsValid;
        }

        /// <summary>
        /// Check recipe content
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="recipeContent"></param>
        /// <param name="reasons"></param>
        /// <returns></returns>
        public bool CheckRecipe(string chamberId, string recipeName, out List<string> reasons)
        {
            reasons = new List<string>();
            string chamberType = chamberId.Split('\\')[0];
            string processType = chamberId.Split('\\')[1];
            string recipeContent = LoadRecipe(chamberId, recipeName, false);
            var xmlRecipe = new XmlDocument();
            try
            {
                if (string.IsNullOrEmpty(recipeContent))
                    throw new Exception("invalid recipe file.");
                xmlRecipe.LoadXml(recipeContent);
                XmlNodeList nodeSteps = xmlRecipe.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{processType}']/Step");
                XmlNode nodeConfig = xmlRecipe.SelectNodes($"Aitex/TableRecipeData/Config")[0];
                switch (processType)
                {
                    case "WaferFlow":
                        CheckWaferFlowRecipe(nodeConfig, nodeSteps, out reasons);
                        break;
                    case "COT":
                        CheckSpinRecipe(nodeConfig, nodeSteps, true, out reasons);
                        break;
                    case "DEV":
                        CheckSpinRecipe(nodeConfig, nodeSteps, false, out reasons);
                        break;
                    case "ADH":
                        CheckADHRecipe(nodeSteps, out reasons);
                        break;
                    case "Oven":
                        CheckOvenRecipe(nodeSteps, out reasons);
                        break;
                }
            }
            catch (Exception ex)
            {
                reasons.Add(ex.Message);
                LOG.Write(ex);
                return false;
            }
            XmlElement nodeData = xmlRecipe.SelectSingleNode($"Aitex/TableRecipeData") as XmlElement;
            bool bResult = reasons.Count == 0;
            if (bResult)
            {
                nodeData.SetAttribute("CheckResult", "Correct");
            }
            else
            {
                nodeData.SetAttribute("CheckResult", "Error");
            }
            SaveRecipe(chamberId, recipeName, xmlRecipe.OuterXml, false, false);
            return bResult;
        }

        /// <summary>
        /// Check recipe content
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="recipeContent"></param>
        /// <param name="reasons"></param>
        /// <returns></returns>
        public bool CheckRestoreRecipe(string chamberId, string recipeName, out List<string> reasons)
        {
            reasons = new List<string>();
            string chamberType = chamberId.Split('\\')[0];
            string processType = chamberId.Split('\\')[1];
            string recipeContent = LoadRestoreRecipe(chamberId, recipeName, false);
            var xmlRecipe = new XmlDocument();
            try
            {
                if (string.IsNullOrEmpty(recipeContent))
                    throw new Exception("invalid recipe file.");
                xmlRecipe.LoadXml(recipeContent);
                XmlNodeList nodeSteps = xmlRecipe.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{processType}']/Step");
                XmlNode nodeConfig = xmlRecipe.SelectNodes($"Aitex/TableRecipeData/Config")[0];
                switch (processType)
                {
                    case "WaferFlow":
                        CheckWaferFlowRecipe(nodeConfig, nodeSteps, out reasons);
                        break;
                    case "COT":
                        CheckSpinRecipe(nodeConfig, nodeSteps, true, out reasons);
                        break;
                    case "DEV":
                        CheckSpinRecipe(nodeConfig, nodeSteps, false, out reasons);
                        break;
                    case "ADH":
                        CheckADHRecipe(nodeSteps, out reasons);
                        break;
                    case "Oven":
                        CheckOvenRecipe(nodeSteps, out reasons);
                        break;
                }
            }
            catch (Exception ex)
            {
                reasons.Add(ex.Message);
                LOG.Write(ex);
                return false;
            }
            XmlElement nodeData = xmlRecipe.SelectSingleNode($"Aitex/TableRecipeData") as XmlElement;
            bool bResult = reasons.Count == 0;
            if (bResult)
            {
                nodeData.SetAttribute("CheckResult", "Correct");
            }
            else
            {
                nodeData.SetAttribute("CheckResult", "Error");
            }
            SaveRestoreRecipe(chamberId, recipeName, xmlRecipe.OuterXml, false, false);
            return bResult;
        }
        void CheckWaferFlowRecipe(XmlNode nodeConfig, XmlNodeList nodeSteps, out List<string> reasons)
        {
            reasons = new List<string>();
            if (nodeSteps.Count <= 0)
            {
                reasons.Add("steps count is 0");
                return;
            }
            if (nodeSteps.Count < 5)
            {
                reasons.Add("steps count is less than 5");
                return;
            }
            int beginStepIndex = 0;
            int endStepIndex = 1;
            for (int i = 0; i < nodeSteps.Count; i++)
            {
                if (nodeSteps[i].Attributes["ModuleName"].Value.Contains("End UNC"))
                {
                    int.TryParse(nodeSteps[i].Attributes["StepNo"].Value, out endStepIndex);
                    break;
                }
            }
            endStepIndex--;
            if (endStepIndex <= 0)
            {
                reasons.Add($"Current recipe muste contains end step");
                return;
            }

            for (int i = beginStepIndex; i <= endStepIndex; i++)
            {
                int stepNo = i + 1;
                string strModuleName = nodeSteps[i].Attributes["ModuleName"].Value;
                if (!string.IsNullOrEmpty(strModuleName))
                    strModuleName = strModuleName.Split(',')[0].Split(' ')[1];
                else
                {
                    reasons.Add($"Step{stepNo} module name is empty");
                    return;
                }
                if (i == 0)//check step1
                {
                    string moduleName = "UNC";
                    if (!strModuleName.Equals(moduleName))
                    {
                        reasons.Add($"Step{stepNo} module muste be {moduleName}");
                    }
                    continue;
                }
                if (i == 1)//check step2
                {
                    string moduleName = "TRS,TCP";
                    if (!moduleName.Contains(strModuleName))
                    {
                        reasons.Add($"Step{stepNo} module muste be {moduleName} module");
                    }
                }
                if (i == endStepIndex - 1)//check last second step
                {
                    string moduleName = "TRS,TCP";
                    if (!moduleName.Contains(strModuleName))
                    {
                        reasons.Add($"Step{stepNo} module muste be {moduleName} module");
                    }
                    if (strModuleName == nodeSteps[1].Attributes["ModuleName"].Value)
                    {
                        reasons.Add($"Step{stepNo} module muste be different with step1 module");
                    }
                }
                if (i == endStepIndex)//check last step
                {
                    string moduleName = "UNC";
                    if (!strModuleName.Equals(moduleName))
                    {
                        reasons.Add($"Step{stepNo} module muste be {moduleName}");
                    }
                    continue;
                }

                //check linked recipe
                string unCheckModuleName = "SHU,TRS,SUB,EIS";
                if (!unCheckModuleName.Contains(strModuleName))
                {
                    string ovenModuleName = "CPL,HHP,LHP,CHP,TCP";
                    if (ovenModuleName.Contains(strModuleName))
                        strModuleName = "Oven";
                    string linkRecipeName = nodeSteps[i].Attributes["RecipeName"].Value;
                    if (string.IsNullOrEmpty(linkRecipeName))
                    {
                        reasons.Add($"Step{stepNo} link recipe is empty.");
                    }
                    else
                    {
                        string[] subRecipeNames = linkRecipeName.Split(',');
                        foreach (var item in subRecipeNames)
                        {
                            string subRecipeName = string.Empty;
                            string[] subRecipeNameStrings = item.Split(':');
                            if (subRecipeNameStrings.Length > 1)
                            {
                                subRecipeName = subRecipeNameStrings[1];
                            }
                            else
                            {
                                subRecipeName = subRecipeNameStrings[0];
                            }
                            if (!CheckRecipe($"{_chamberId}\\{strModuleName}", subRecipeName, out List<string> subReasons))
                            {
                                reasons.Add($"Step{stepNo} linked recipe check fail.");
                            }

                        }

                    }
                }
            }

            //check system reicpe
            string strSystemReicpeName = nodeConfig.Attributes["SystemRecipe"]?.Value;
            if (!string.IsNullOrEmpty(strSystemReicpeName))
            {
                if (!CheckRecipe($"{_chamberId}\\System", strSystemReicpeName, out List<string> subReasons))
                {
                    reasons.Add($"Linked system recipe check fail.");
                }
            }
            else
            {
                reasons.Add($"Must link system recipe.");

            }
        }

        public string LoadRecipeByFullPath(string fullPath)
        {
            string rcp = string.Empty;
            try
            {
                using (StreamReader fs = new StreamReader(fullPath))
                {
                    rcp = fs.ReadToEnd();
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    using (StreamReader fs = new StreamReader(fullPath))
                    {
                        rcp = fs.ReadToEnd();
                        fs.Close();
                    }
                }
                catch
                {
                    LOG.Write(ex, $"load recipe file failed, {fullPath}");
                    rcp = string.Empty;
                };
            }
            return rcp;
        }

        void CheckSpinRecipe(XmlNode nodeConfig, XmlNodeList nodeSteps, bool checkPumpRecipe, out List<string> reasons)
        {
            reasons = new List<string>();
            if (nodeSteps.Count <= 0)
            {
                reasons.Add("steps count is 0.");
                return;
            }

            List<int> loopStartStepsNo = new List<int>();
            List<int> loopEndStepsNo = new List<int>();

            bool hasDispense = false;

            List<bool> arm1HasMove = new List<bool>();
            List<bool> arm2HasMove = new List<bool>();
            for (int i = 0; i < nodeSteps.Count; i++)
            {
                int stepNo = i + 1;
                if (nodeSteps[i].Attributes["Loop"].Value.Contains("Start"))
                {
                    loopStartStepsNo.Add(stepNo);
                }
                if (nodeSteps[i].Attributes["Loop"].Value.Contains("End"))
                {
                    loopEndStepsNo.Add(stepNo);
                }

                string strDispense = nodeSteps[i].Attributes["Dispense"].Value;
                if (strDispense.Contains("Resist"))
                {
                    hasDispense = true;
                }
                //check dispens

                if (!string.IsNullOrEmpty(strDispense))
                {
                    bool bDispenseHasError = false;
                    string[] strDispenseValues = strDispense.Split(',');
                    string strFirstDispenseValue = strDispenseValues[0];
                    foreach (var item in strDispenseValues)
                    {
                        if (strFirstDispenseValue.Split(' ')[0] != item.Split(' ')[0])
                            bDispenseHasError = true;
                    }

                    if (bDispenseHasError)
                        reasons.Add($"Step{stepNo} dispense must select same module.");
                }
                if (i > 0)
                {
                    string arm1CurrentPosition = nodeSteps[i].Attributes["Arm1"].Value.Split(',')[2].Split(':')[1];
                    string arm1PreviousPosition = nodeSteps[i - 1].Attributes["Arm1"].Value.Split(',')[2].Split(':')[1];
                    if (!string.IsNullOrEmpty(arm1CurrentPosition))
                    {
                        if (arm1CurrentPosition.Equals(arm1PreviousPosition))
                            arm1HasMove.Add(false);
                        else
                            arm1HasMove.Add(true);
                    }


                    string arm2CurrentPosition = nodeSteps[i].Attributes["Arm2"].Value.Split(',')[2].Split(':')[1];
                    string arm2PreviousPosition = nodeSteps[i - 1].Attributes["Arm2"].Value.Split(',')[2].Split(':')[1];
                    if (!string.IsNullOrEmpty(arm2CurrentPosition))
                    {
                        if (arm2CurrentPosition.Equals(arm2PreviousPosition))
                            arm2HasMove.Add(false);
                        else
                            arm2HasMove.Add(true);
                    }


                }
            }

            //check loop
            if (loopStartStepsNo.Count != loopEndStepsNo.Count) //判断个数
            {
                reasons.Add("loop set is incorrect");
            }

            //check pump recipe
            if (checkPumpRecipe && hasDispense)
            {
                string pumpRecipeName = nodeConfig.Attributes["COTPumpRecipe"]?.Value;
                if (string.IsNullOrEmpty(pumpRecipeName))
                    reasons.Add("pump recipe is null");
                else
                {
                    if (!CheckRecipe($"{_chamberId}\\Pump", pumpRecipeName, out List<string> subReasons))
                    {
                        reasons.Add($"Linked pump recipe check fail.");
                    }
                }
            }

            //check arm move
            for (int i = 0; i < arm1HasMove.Count; i++)
            {
                if (arm1HasMove[i] && arm2HasMove[i])//同时移动
                    reasons.Add($"step{i + 2} arm1 and arm2 move at the same time");

            }
        }
        void CheckADHRecipe(XmlNodeList nodeSteps, out List<string> reasons)
        {
            reasons = new List<string>();
            if (nodeSteps.Count <= 0)
            {
                reasons.Add("steps count is 0.");
                return;
            }


            for (int i = 0; i < nodeSteps.Count; i++)
            {
                int stepNo = i + 1;

                //check dispense
                if (!nodeSteps[i].Attributes["ProcessPosition"].Value.Equals("Process") && !string.IsNullOrEmpty(nodeSteps[i].Attributes["Dispense"].Value))
                {
                    reasons.Add($"Step{stepNo} only position is process can select dispense.");
                }

                //check plate temp
                if (nodeSteps[i].Attributes["ProcessPosition"].Value.Equals("Process") && string.IsNullOrEmpty(nodeSteps[i].Attributes["PlateTemp"].Value))
                {
                    reasons.Add($"Step{stepNo} must set plate temp.");
                }

                //check alrm
                double.TryParse(nodeSteps[i].Attributes["AlarmMax"].Value, out double alarmMax);
                double.TryParse(nodeSteps[i].Attributes["AlarmMin"].Value, out double alarmMin);
                double.TryParse(nodeSteps[i].Attributes["WarnMax"].Value, out double warnMax);
                double.TryParse(nodeSteps[i].Attributes["WarnMin"].Value, out double warnMin);
                bool bRightRange = false;
                if (alarmMax == 0 && alarmMin == 0 && warnMax == 0 && warnMin == 0) continue;
                if (alarmMax == 0 && alarmMin == 0)
                {
                    bRightRange = warnMax > warnMin;
                    if (!bRightRange)
                    {
                        reasons.Add($"Step{stepNo} warnMax>warnMin?");
                    }
                }
                else if (warnMax == 0 && warnMax == 0)
                {
                    bRightRange = alarmMax > alarmMin;
                    if (!bRightRange)
                    {
                        reasons.Add($"Step{stepNo} alarmMax>alarmMin?");
                    }
                }
                else
                {
                    bRightRange = alarmMax > warnMax && warnMax > warnMin && warnMin > alarmMin;
                    if (!bRightRange)
                    {
                        reasons.Add($"Step{stepNo} alarmMax>warnMax>warnMin>alarmMin?");
                    }
                }

            }
        }
        void CheckOvenRecipe(XmlNodeList nodeSteps, out List<string> reasons)
        {
            reasons = new List<string>();
            if (nodeSteps.Count <= 0)
            {
                reasons.Add("steps count is 0.");
                return;
            }


            for (int i = 0; i < nodeSteps.Count; i++)
            {
                int stepNo = i + 1;

                //check plate temp
                if (nodeSteps[i].Attributes["ProcessPosition"].Value.Equals("Heat") && string.IsNullOrEmpty(nodeSteps[i].Attributes["PlateTemp"].Value))
                {
                    reasons.Add($"Step{stepNo} must set plate temp.");
                }

                //check alrm
                double.TryParse(nodeSteps[i].Attributes["AlarmMax"].Value, out double alarmMax);
                double.TryParse(nodeSteps[i].Attributes["AlarmMin"].Value, out double alarmMin);
                double.TryParse(nodeSteps[i].Attributes["WarnMax"].Value, out double warnMax);
                double.TryParse(nodeSteps[i].Attributes["WarnMin"].Value, out double warnMin);
                bool bRightRange = false;
                if (alarmMax == 0 && alarmMin == 0 && warnMax == 0 && warnMin == 0) continue;
                if (alarmMax == 0 && alarmMin == 0)
                {
                    bRightRange = warnMax > warnMin;
                    if (!bRightRange)
                    {
                        reasons.Add($"Step{stepNo} warnMax>warnMin?");
                    }
                }
                else if (warnMax == 0 && warnMax == 0)
                {
                    bRightRange = alarmMax > alarmMin;
                    if (!bRightRange)
                    {
                        reasons.Add($"Step{stepNo} alarmMax>alarmMin?");
                    }
                }
                else
                {
                    bRightRange = alarmMax > warnMax && warnMax > warnMin && warnMin > alarmMin;
                    if (!bRightRange)
                    {
                        reasons.Add($"Step{stepNo} alarmMax>warnMax>warnMin>alarmMin?");
                    }
                }

            }
        }

        public IEnumerable<string> GetRecipesPath(string chamberId, bool includingUsedRecipe)
        {
            return _rcpContext.GetRecipesPath(chamberId, includingUsedRecipe);
        }

        public string LoadRecipe(string chamberId, string recipeName, bool needValidation)
        {
            string rcp = string.Empty;
            try
            {
                using (StreamReader fs = new StreamReader(GenerateRecipeFilePath(chamberId, recipeName)))
                {
                    rcp = fs.ReadToEnd();
                    fs.Close();
                }
                //if (needValidation)
                //{
                //    List<string> reason;
                //    if (!ValidateRecipe(chamberId, recipeName, rcp, out reason))
                //    {
                //        rcp = string.Empty;

                //        LOG.Write("校验recipe file 出错, " + string.Join(",", reason.ToArray()));
                //    }
                //}
            }
            catch (Exception ex)
            {
                LOG.Write(ex, $"load recipe file failed, {recipeName}");
                rcp = string.Empty;
            }
            return rcp;
        }

        public string LoadRecipeNorcp(string chamberId, string recipeName, bool needValidation)
        {
            string rcp = string.Empty;
            try
            {
                using (StreamReader fs = new StreamReader(GenerateRecipeFilePathNorcp(chamberId, recipeName)))
                {
                    rcp = fs.ReadToEnd();
                    fs.Close();
                }
                //if (needValidation)
                //{
                //    List<string> reason;
                //    if (!ValidateRecipe(chamberId, recipeName, rcp, out reason))
                //    {
                //        rcp = string.Empty;

                //        LOG.Write("校验recipe file 出错, " + string.Join(",", reason.ToArray()));
                //    }
                //}
            }
            catch (Exception ex)
            {
                LOG.Write(ex, $"load recipe file failed, {recipeName}");
                rcp = string.Empty;
            }
            return rcp;
        }

        /// <summary>
        /// Get recipe list
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="includingUsedRecipe"></param>
        /// <returns></returns>
        public IEnumerable<string> GetRecipes(string chamberId, bool includingUsedRecipe)
        {
            return _rcpContext.GetRecipes(chamberId, includingUsedRecipe);
        }


        /// <summary>
        /// Get recipe list in xml format
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="includingUsedRecipe"></param>
        /// <returns></returns>
        public string GetXmlRecipeList(string chamberId, bool includingUsedRecipe)
        {
            XmlDocument doc = new XmlDocument();
            var baseFolderPath = getRecipeDirPath(chamberId);
            DirectoryInfo curFolderInfo = new DirectoryInfo(baseFolderPath);
            doc.AppendChild(GenerateRecipeList(chamberId, curFolderInfo, doc, includingUsedRecipe));
            return doc.OuterXml;
        }

        public void SaveRecipeHistory(string chamberId, string recipeName, string recipeContent, bool needSaveAs = true)
        {
            try
            {
                if (!string.IsNullOrEmpty(recipeName) && needSaveAs)
                {
                    string newRecipeName = string.Format("HistoryRecipe\\{0}\\{1}", DateTime.Now.ToString("yyyyMM"), recipeName);

                    SaveRecipe(chamberId, newRecipeName, recipeContent, true, false);

                    LOG.Write(string.Format("{0}通知TM保存工艺程序{1}", chamberId, recipeName));
                }

            }
            catch (Exception ex)
            {
                LOG.Write(ex, string.Format("保存{0}工艺程序{1}发生错误", chamberId, recipeName));
            }

        }
        /// <summary>
        /// generate recipe list information in current directory
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="currentDir"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        XmlElement GenerateRecipeList(string chamberId, DirectoryInfo currentDir, XmlDocument doc, bool includingUsedRecipe)
        {
            int trimLength = getRecipeDirPath(chamberId).Length;
            XmlElement folderEle = doc.CreateElement("Folder");
            folderEle.SetAttribute("Name", currentDir.FullName.Substring(trimLength));

            DirectoryInfo[] dirInfos = currentDir.GetDirectories();
            foreach (DirectoryInfo dirInfo in dirInfos)
            {
                if (!includingUsedRecipe && dirInfo.Name == "HistoryRecipe")
                    continue;
                folderEle.AppendChild(GenerateRecipeList(chamberId, dirInfo, doc, includingUsedRecipe));
            }

            FileInfo[] fileInfos = currentDir.GetFiles("*.rcp");
            foreach (FileInfo fileInfo in fileInfos)
            {
                XmlElement fileNd = doc.CreateElement("File");
                string fileStr = fileInfo.FullName.Substring(trimLength).TrimStart(new char[] { '\\' }); ;
                fileStr = fileStr.Substring(0, fileStr.LastIndexOf("."));
                fileNd.SetAttribute("Name", fileStr);
                folderEle.AppendChild(fileNd);
            }
            return folderEle;
        }

        XmlElement GeneratelRestoreRecipeList(string chamberId, DirectoryInfo currentDir, XmlDocument doc, bool includingUsedRecipe)
        {
            int trimLength = getRecipeBackupDirPath(chamberId).Length;
            XmlElement folderEle = doc.CreateElement("Folder");
            var name = currentDir.FullName.Substring(trimLength);
            folderEle.SetAttribute("Name", name);

            DirectoryInfo[] dirInfos = currentDir.GetDirectories();
            foreach (DirectoryInfo dirInfo in dirInfos)
            {
                if (!includingUsedRecipe && dirInfo.Name == "HistoryRecipe")
                    continue;
                folderEle.AppendChild(GeneratelRestoreRecipeList(chamberId, dirInfo, doc, includingUsedRecipe));
            }

            FileInfo[] fileInfos = currentDir.GetFiles("*.rcp");
            foreach (FileInfo fileInfo in fileInfos)
            {
                XmlElement fileNd = doc.CreateElement("File");
                string fileStr = fileInfo.FullName.Substring(trimLength).TrimStart(new char[] { '\\' }); ;
                fileStr = fileStr.Substring(0, fileStr.LastIndexOf("."));
                fileNd.SetAttribute("Name", fileStr);
                folderEle.AppendChild(fileNd);
            }
            return folderEle;
        }

        /// <summary>
        /// Delete a recipe by recipe name
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="recipeName"></param>
        /// <returns></returns>
        public bool DeleteRecipe(string chamberId, string recipeName)
        {
            try
            {
                File.Delete(GenerateRecipeFilePath(chamberId, recipeName));

                InfoDialog(string.Format(Resources.RecipeFileManager_DeleteRecipe_RecipeFile0DeleteSucceeded, recipeName));
            }
            catch (Exception ex)
            {
                LOG.Write(ex, "删除recipe file 出错");

                WarningDialog(string.Format(Resources.RecipeFileManager_DeleteRecipe_RecipeFile0DeleteFailed, recipeName));

                return false;
            }
            return true;
        }

        /// <summary>
        ///  Rename recipe
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public bool RenameRecipe(string chamId, string oldName, string newName)
        {
            try
            {
                if (File.Exists(GenerateRecipeFilePath(chamId, newName)))
                {
                    WarningDialog(string.Format(Resources.RecipeFileManager_RenameRecipe_RecipeFile0FileExisted, oldName));
                    return false;
                }
                else
                {
                    File.Move(GenerateRecipeFilePath(chamId, oldName), GenerateRecipeFilePath(chamId, newName));

                    InfoDialog(string.Format(Resources.RecipeFileManager_RenameRecipe_RecipeFile0Renamed, oldName, newName));
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex, "重命名recipe file 出错");

                WarningDialog(string.Format(Resources.RecipeFileManager_RenameRecipe_RecipeFile0RenameFailed, oldName, newName));

                return false;
            }
            return true;
        }


        public bool BackupRecipe(string fileOriginalPath, string fileDestinationPath, bool isSaveLinkRecipe, List<string> recipeNames)
        {
            try
            {
                string filePath = getRecipeBackupDirPath(fileOriginalPath);

                foreach (var item in recipeNames)
                {
                    string sourceFilePath = GenerateRecipeFilePath(fileOriginalPath, item);
                    string destFilePath = GenerateBackupRecipeFilePath(fileDestinationPath, item);

                    if (item.Contains("\\"))
                    {
                        DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(destFilePath));
                        if (!di.Exists) di.Create();
                    }
                    File.Copy(sourceFilePath, destFilePath, true);
                    if (isSaveLinkRecipe)
                    {
                        string recipeContent = LoadRecipe(fileOriginalPath, item, false);
                        var xmlRecipe = new XmlDocument();
                        try
                        {
                            string chamberType = fileOriginalPath.Split('\\')[0];
                            string processType = fileOriginalPath.Split('\\')[1];
                            if (string.IsNullOrEmpty(recipeContent))
                                continue;
                            xmlRecipe.LoadXml(recipeContent);
                            XmlNodeList nodeSteps = xmlRecipe.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{processType}']/Step");
                            XmlNode nodeConfig = xmlRecipe.SelectNodes($"Aitex/TableRecipeData/Config")[0];
                            switch (processType)
                            {
                                case "WaferFlow":
                                    string strSystemReicpeName = nodeConfig.Attributes["SystemRecipe"]?.Value;
                                    if (!string.IsNullOrEmpty(strSystemReicpeName))
                                    {
                                        if (CheckRecipe($"{_chamberId}\\System", strSystemReicpeName, out List<string> subReasons))
                                        {
                                            string subSourceFilePath = GenerateRecipeFilePath($"{_chamberId}\\System", strSystemReicpeName);
                                            string subDestFilePath = GenerateBackupRecipeFilePath($"{fileDestinationPath.Split('\\')[0]}\\System", strSystemReicpeName);
                                            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(subDestFilePath));
                                            if (!di.Exists) di.Create();
                                            File.Copy(subSourceFilePath, subDestFilePath, true);
                                        }
                                    }
                                    break;
                                case "COT":
                                case "DEV":
                                    string pumpRecipeName = nodeConfig.Attributes["COTPumpRecipe"]?.Value;
                                    if (!string.IsNullOrEmpty(pumpRecipeName))
                                    {
                                        if (CheckRecipe($"{_chamberId}\\Pump", pumpRecipeName, out List<string> subReasons))
                                        {
                                            string subSourceFilePath = GenerateRecipeFilePath($"{_chamberId}\\Pump", pumpRecipeName);
                                            string subDestFilePath = GenerateBackupRecipeFilePath($"{fileDestinationPath.Split('\\')[0]}\\Pump", pumpRecipeName);
                                            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(subDestFilePath));
                                            if (!di.Exists) di.Create();
                                            File.Copy(subSourceFilePath, subDestFilePath, true);
                                        }
                                    }
                                    break;
                                case "ADH":
                                case "Oven":
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            LOG.Write(ex);
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex, "Backup Recipe file error");
            }
            return true;
        }

        public bool CheckBackRecipeIsLinkRecipe(string fileOriginalPath, List<string> recipeNames)
        {
            string chamberType = fileOriginalPath.Split('\\')[0];
            string processType = fileOriginalPath.Split('\\')[1];
            foreach (var item in recipeNames)
            {
                string recipeContent = LoadRestoreRecipe(fileOriginalPath, item, false);
                var xmlRecipe = new XmlDocument();
                try
                {
                    if (string.IsNullOrEmpty(recipeContent))
                        continue;
                    xmlRecipe.LoadXml(recipeContent);
                    XmlNodeList nodeSteps = xmlRecipe.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{processType}']/Step");
                    XmlNode nodeConfig = xmlRecipe.SelectNodes($"Aitex/TableRecipeData/Config")[0];
                    switch (processType)
                    {
                        case "WaferFlow":
                            string strSystemReicpeName = nodeConfig.Attributes["SystemRecipe"]?.Value;
                            if (!string.IsNullOrEmpty(strSystemReicpeName))
                            {
                                //if (CheckRecipe($"{_chamberId}\\System", strSystemReicpeName, out List<string> subReasons))
                                //{
                                return true;
                                //}
                            }
                            break;
                        case "COT":
                        case "DEV":
                            string pumpRecipeName = nodeConfig.Attributes["COTPumpRecipe"]?.Value;
                            if (!string.IsNullOrEmpty(pumpRecipeName))
                            {
                                //if (CheckRecipe($"{_chamberId}\\Pump", pumpRecipeName, out List<string> subReasons))
                                //{
                                return true;
                                //}
                            }
                            break;
                        case "ADH":
                        case "Oven":
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LOG.Write(ex);
                    return false;
                }
            }
            return false;
        }

        public string GetXmlRestoreRecipeList(string chamberId, bool includingUsedRecipe)
        {
            XmlDocument doc = new XmlDocument();
            var baseFolderPath = getRecipeBackupDirPath(chamberId);
            DirectoryInfo curFolderInfo = new DirectoryInfo(baseFolderPath);
            doc.AppendChild(GeneratelRestoreRecipeList(chamberId, curFolderInfo, doc, includingUsedRecipe));
            return doc.OuterXml;
        }

        public List<string> RestoreRecipeFolderList()
        {
            List<string> folderList = new List<string>();
            var recipeBackupPath = PathManager.GetRecipeBackupDir();
            DirectoryInfo directoryInfo = new DirectoryInfo(recipeBackupPath);
            DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();
            foreach (var item in directoryInfos)
            {
                folderList.Add(item.Name);
            }
            return folderList;
        }

        public string LoadRestoreRecipe(string chamberId, string recipeName, bool needValidation)
        {
            string rcp = string.Empty;
            try
            {
                using (StreamReader fs = new StreamReader(GenerateBackupRecipeFilePath(chamberId, recipeName)))
                {
                    rcp = fs.ReadToEnd();
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex, $"load recipe file failed, {recipeName}");
                rcp = string.Empty;
            }
            return rcp;
        }

        public bool SigRestoreRecipe(string chamId, List<string> recipeNames)
        {
            try
            {
                string filePath = getRecipeBackupDirPath(chamId);
                foreach (var item in recipeNames)
                {
                    string strdest = chamId;
                    string destFilePath = GenerateRecipeFilePath(strdest, item);
                    string sourceFilePath = GenerateBackupRecipeFilePath(chamId, item);

                    if (item.Contains("\\"))
                    {
                        DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(destFilePath));
                        if (!di.Exists) di.Create();
                    }
                    File.Copy(sourceFilePath, destFilePath, true);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex, "Backup Recipe file error");
            }
            return true;
        }

        public bool RestoreRecipe(string chamId, bool isSaveLink, List<string> recipeNames)
        {
            try
            {
                string filePath = getRecipeBackupDirPath(chamId);
                foreach (var item in recipeNames)
                {
                    string strdest = chamId.Remove(5, 14);
                    string destFilePath = GenerateRecipeFilePath(strdest, item);
                    string sourceFilePath = GenerateBackupRecipeFilePath(chamId, item);

                    if (item.Contains("\\"))
                    {
                        DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(destFilePath));
                        if (!di.Exists) di.Create();
                    }
                    File.Copy(sourceFilePath, destFilePath, true);
                    if (isSaveLink)
                    {
                        string recipeContent = LoadRestoreRecipe(chamId, item, false);
                        var xmlRecipe = new XmlDocument();
                        try
                        {
                            string chamberType = chamId.Split('\\')[0];
                            string processType = chamId.Split('\\')[1];
                            if (string.IsNullOrEmpty(recipeContent))
                                continue;
                            xmlRecipe.LoadXml(recipeContent);
                            XmlNodeList nodeSteps = xmlRecipe.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{processType}']/Step");
                            XmlNode nodeConfig = xmlRecipe.SelectNodes($"Aitex/TableRecipeData/Config")[0];
                            switch (processType)
                            {
                                case "WaferFlow":
                                    string strSystemReicpeName = nodeConfig.Attributes["SystemRecipe"]?.Value;
                                    if (!string.IsNullOrEmpty(strSystemReicpeName))
                                    {
                                        if (CheckRestoreRecipe($"{ chamId.Split('\\')[0]}\\System", strSystemReicpeName, out List<string> subReasons))
                                        {
                                            string subSourceFilePath = GenerateBackupRecipeFilePath($"{chamId.Split('\\')[0]}\\System", strSystemReicpeName);
                                            string subDestFilePath = GenerateRecipeFilePath($"{_chamberId}\\System", strSystemReicpeName);
                                            if (item.Contains("\\"))
                                            {
                                                DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(subDestFilePath));
                                                if (!di.Exists) di.Create();
                                            }
                                            File.Copy(subSourceFilePath, subDestFilePath, true);
                                        }
                                    }
                                    break;
                                case "COT":
                                case "DEV":
                                    string pumpRecipeName = nodeConfig.Attributes["COTPumpRecipe"]?.Value;
                                    if (!string.IsNullOrEmpty(pumpRecipeName))
                                    {
                                        if (CheckRestoreRecipe($"{_chamberId}\\Pump", pumpRecipeName, out List<string> subReasons))
                                        {
                                            string subSourceFilePath = GenerateBackupRecipeFilePath($"{chamId.Split('\\')[0]}\\Pump", pumpRecipeName);
                                            string subDestFilePath = GenerateRecipeFilePath($"{_chamberId}\\Pump", pumpRecipeName);
                                            if (item.Contains("\\"))
                                            {
                                                DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(subDestFilePath));
                                                if (!di.Exists) di.Create();
                                            }
                                            File.Copy(subSourceFilePath, subDestFilePath, true);
                                        }
                                    }
                                    break;
                                case "ADH":
                                case "Oven":
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            LOG.Write(ex);
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex, "Backup Recipe file error");
            }
            return true;
        }

        //private void EventInfo(string message)
        //{
        //    _rcpContext.PostInfoEvent(message);
        //}

        //private void EventWarning(string message)
        //{
        //    _rcpContext.PostWarningEvent(message);
        //}

        //private void EventAlarm(string message)
        //{
        //    _rcpContext.PostAlarmEvent(message);
        //}

        private void InfoDialog(string message)
        {
            _rcpContext.PostInfoDialogMessage(message);
        }

        private void WarningDialog(string message)
        {
            _rcpContext.PostWarningDialogMessage(message);
        }

        //private void AlarmDialog(string message)
        //{
        //    _rcpContext.PostAlarmDialogMessage(message);
        //}

        private void EventDialog(string message, List<string> reason)
        {
            string msg = message;
            foreach (var r in reason)
            {
                msg += "\r\n" + r;
            }
            _rcpContext.PostDialogEvent(msg);
        }

        /// <summary>
        /// get recipe's file path
        /// </summary>
        /// <param name="recipeName"></param>
        /// <returns></returns>
        private string GenerateRecipeFilePath(string chamId, string recipeName)
        {
            return getRecipeDirPath(chamId) + recipeName + ".rcp";
        }

        private string GenerateRecipeFilePathNorcp(string chamId, string recipeName)
        {
            return getRecipeDirPath(chamId) + recipeName;
        }

        private string GenerateBackupRecipeFilePath(string chamId, string recipeName)
        {
            return getRecipeBackupDirPath(chamId) + recipeName + ".rcp";
        }

        private string GenerateSequenceFilePath(string chamId, string recipeName)
        {
            return getRecipeDirPath(chamId) + recipeName + ".seq";
        }
        /// <summary>
        /// get recipe's dir path
        /// </summary>
        /// <param name="recipeName"></param>
        /// <returns></returns>
        private string getRecipeDirPath(string chamId)
        {
            var dir = string.Format("{0}{1}\\", PathManager.GetRecipeDir(), chamId);
            DirectoryInfo di = new DirectoryInfo(dir);
            if (!di.Exists) di.Create();
            return dir;
        }

        /// <summary>
        /// get recipe's dir path
        /// </summary>
        /// <param name="recipeName"></param>
        /// <returns></returns>
        private string getRecipeBackupDirPath(string chamId)
        {
            var dir = string.Format("{0}{1}\\", PathManager.GetRecipeBackupDir(), chamId);
            DirectoryInfo di = new DirectoryInfo(dir);
            if (!di.Exists) di.Create();
            return dir;
        }

        /// <summary>
        /// delete a recipe folder
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public bool DeleteFolder(string chamId, string folderName)
        {
            try
            {
                Directory.Delete(getRecipeDirPath(chamId) + folderName, true);
                InfoDialog(string.Format(Resources.RecipeFileManager_DeleteFolder_RecipeFolder0DeleteSucceeded, folderName));
            }
            catch (Exception ex)
            {
                LOG.Write(ex, "删除recipe folder 出错");
                WarningDialog(string.Format("recipe folder  {0} delete failed", folderName));
                return false;
            }
            return true;
        }


        /// <summary>
        /// save as recipe content
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="recipeName"></param>
        /// <param name="recipeContent"></param>
        /// <returns></returns>
        public bool SaveAsRecipe(string chamId, string recipeName, string recipeContent)
        {
            var path = GenerateRecipeFilePath(chamId, recipeName);
            if (File.Exists(path))
            {
                WarningDialog(string.Format(Resources.RecipeFileManager_SaveAsRecipe_RecipeFile0savefailed, recipeName));
                return false;
            }
            return SaveRecipe(chamId, recipeName, recipeContent, true, true);
        }

        /// <summary>
        /// save recipe content
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="recipeName"></param>
        /// <param name="recipeContent"></param>
        /// <returns></returns>
        public bool SaveRecipe(string chamId, string recipeName, string recipeContent, bool clearBarcode, bool notifyUI)
        {
            //validate recipe format when saving a recipe file
            //var reasons1 = new List<string>();
            //var reasons2 = new List<string>();
            //ValidateRecipe(chamId, recipeName, recipeContent, out reasons1);
            //CheckRecipe(chamId, recipeContent, out reasons2);
            //reasons1.AddRange(reasons2);
            //if (reasons1.Count > 0)
            //{
            //    EventDialog(string.Format( Resources.RecipeFileManager_SaveRecipe_SaveRecipeContentError, recipeName), reasons1);
            //}

            bool ret = true;
            try
            {
                var path = GenerateRecipeFilePath(chamId, recipeName);
                FileInfo fi = new FileInfo(path);
                if (!fi.Directory.Exists)
                    fi.Directory.Create();

                XmlDocument xml = new XmlDocument();
                xml.LoadXml(recipeContent);

                XmlTextWriter writer = new XmlTextWriter(path, Encoding.UTF8);
                writer.Formatting = Formatting.Indented;
                xml.Save(writer);
                writer.Close();

                if (notifyUI)
                {
                    InfoDialog(string.Format(Resources.RecipeFileManager_SaveRecipe_RecipeFile0SaveCompleted, recipeName));
                }
                else
                {
                    EV.PostMessage("System", EventEnum.GeneralInfo, string.Format(Resources.RecipeFileManager_SaveRecipe_RecipeFile0SaveCompleted, recipeName));
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex, "保存recipe file 出错");

                if (notifyUI)
                {
                    WarningDialog(string.Format(Resources.RecipeFileManager_SaveRecipe_RecipeFile0SaveFailed, recipeName));
                }
                ret = false;
            }
            return ret;
        }


        /// <summary>
        /// save recipe content
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="recipeName"></param>
        /// <param name="recipeContent"></param>
        /// <returns></returns>
        public bool SaveRestoreRecipe(string chamId, string recipeName, string recipeContent, bool clearBarcode, bool notifyUI)
        {
            //validate recipe format when saving a recipe file
            //var reasons1 = new List<string>();
            //var reasons2 = new List<string>();
            //ValidateRecipe(chamId, recipeName, recipeContent, out reasons1);
            //CheckRecipe(chamId, recipeContent, out reasons2);
            //reasons1.AddRange(reasons2);
            //if (reasons1.Count > 0)
            //{
            //    EventDialog(string.Format( Resources.RecipeFileManager_SaveRecipe_SaveRecipeContentError, recipeName), reasons1);
            //}

            bool ret = true;
            try
            {
                var path = GenerateBackupRecipeFilePath(chamId, recipeName);
                FileInfo fi = new FileInfo(path);
                if (!fi.Directory.Exists)
                    fi.Directory.Create();

                XmlDocument xml = new XmlDocument();
                xml.LoadXml(recipeContent);

                XmlTextWriter writer = new XmlTextWriter(path, Encoding.UTF8);
                writer.Formatting = Formatting.Indented;
                xml.Save(writer);
                writer.Close();

                if (notifyUI)
                {
                    InfoDialog(string.Format(Resources.RecipeFileManager_SaveRecipe_RecipeFile0SaveCompleted, recipeName));
                }
                else
                {
                    EV.PostMessage("System", EventEnum.GeneralInfo, string.Format(Resources.RecipeFileManager_SaveRecipe_RecipeFile0SaveCompleted, recipeName));
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex, "保存recipe file 出错");

                if (notifyUI)
                {
                    WarningDialog(string.Format(Resources.RecipeFileManager_SaveRecipe_RecipeFile0SaveFailed, recipeName));
                }
                ret = false;
            }
            return ret;
        }

        /// <summary>
        /// create a new recipe folder
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public bool CreateFolder(string chamId, string folderName)
        {
            try
            {
                Directory.CreateDirectory(getRecipeDirPath(chamId) + folderName);
                InfoDialog(string.Format(Resources.RecipeFileManager_CreateFolder_RecipeFolder0Created, folderName));
            }
            catch (Exception ex)
            {
                LOG.Write(ex, "create recipe folder failed");
                WarningDialog(string.Format(Resources.RecipeFileManager_CreateFolder_RecipeFolder0CreateFailed, folderName));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Rename recipe folder name
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public bool RenameFolder(string chamId, string oldName, string newName)
        {
            try
            {
                string oldPath = getRecipeDirPath(chamId) + oldName;
                string newPath = getRecipeDirPath(chamId) + newName;
                Directory.Move(oldPath, newPath);
                InfoDialog(string.Format(Resources.RecipeFileManager_RenameFolder_RecipeFolder0renamed, oldName, newName));
            }
            catch (Exception ex)
            {
                LOG.Write(ex, "Rename recipe folder failed");
                WarningDialog(string.Format(Resources.RecipeFileManager_RenameFolder_RecipeFolder0RenameFailed, oldName, newName));
                return false;
            }
            return true;
        }

        public XmlDocument RecipeDom = new XmlDocument();

        private string GetRecipeBody(string chamberId, string nodePath)
        {
            if (_rcpContext == null)
                return string.Empty;

            string schema = _rcpContext.GetRecipeDefiniton(chamberId);

            RecipeDom = new XmlDocument();

            RecipeDom.LoadXml(schema);

            XmlNode node = RecipeDom.SelectSingleNode(nodePath);

            return node.OuterXml;
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

        public Dictionary<string, ObservableCollection<RecipeTemplateColumnBase>> GetGroupRecipeTemplate()
        {
            try
            {
                XmlNode nodeRoot = RecipeDom.SelectSingleNode("Aitex/TableRecipeFormat");

                RecipeChamberType = nodeRoot.Attributes["RecipeChamberType"].Value;

                RecipeVersion = nodeRoot.Attributes["RecipeVersion"].Value;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return null;
            }

            var columns = new Dictionary<string, ObservableCollection<RecipeTemplateColumnBase>>();

            RecipeTemplateColumnBase col = null;
            XmlNodeList nodes = RecipeDom.SelectNodes("Aitex/TableRecipeFormat/Catalog/Group");
            foreach (XmlNode node in nodes)
            {
                var sigcolumns = new ObservableCollection<RecipeTemplateColumnBase>();
                XmlNodeList childNodes = node.SelectNodes("Step");
                foreach (XmlNode step in childNodes)
                {
                    //step number
                    if (step.Attributes["ControlName"].Value == "StepNo")
                    {
                        col = new RecipeTemplateColumnBase()
                        {
                            DisplayName = "Step",
                            ControlName = "StepNo",
                        };
                        sigcolumns.Add(col);
                        continue;
                    }

                    switch (step.Attributes["InputType"].Value)
                    {
                        case "TextInput":
                            col = new RecipeTemplateColumnBase()
                            {
                                ValueType = "TextInput",
                                ModuleName = step.Attributes["ModuleName"].Value,
                                ControlName = step.Attributes["ControlName"].Value,
                                DisplayName = step.Attributes["DisplayName"].Value,
                                IsEnable = !bool.Parse(step.Attributes["ReadOnly"] != null ? step.Attributes["ReadOnly"].Value : "false"),
                                EnableConfig = step.Attributes["EnableConfig"] != null && Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                EnableTolerance = step.Attributes["EnableTolerance"] != null && Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                            };
                            sigcolumns.Add(col);
                            break;
                        case "NumInput":
                            col = new RecipeTemplateColumnBase()
                            {
                                ValueType = "NumInput",
                                ModuleName = step.Attributes["ModuleName"].Value,
                                ControlName = step.Attributes["ControlName"].Value,
                                DisplayName = step.Attributes["DisplayName"].Value,
                                InputMode = step.Attributes["InputMode"].Value,
                                Minimun = double.Parse(step.Attributes["Min"].Value),
                                Maximun = double.Parse(step.Attributes["Max"].Value),
                                IsEnable = !bool.Parse(step.Attributes["ReadOnly"] != null ? step.Attributes["ReadOnly"].Value : "false"),
                                EnableConfig = step.Attributes["EnableConfig"] != null && Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                EnableTolerance = step.Attributes["EnableTolerance"] != null && Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                            };
                            sigcolumns.Add(col);
                            break;
                        case "DoubleInput":
                            col = new RecipeTemplateColumnBase()
                            {
                                ValueType = "DoubleInput",
                                ModuleName = step.Attributes["ModuleName"].Value,
                                ControlName = step.Attributes["ControlName"].Value,
                                DisplayName = step.Attributes["DisplayName"].Value,
                                InputMode = step.Attributes["InputMode"].Value,
                                Minimun = double.Parse(step.Attributes["Min"].Value),
                                Maximun = double.Parse(step.Attributes["Max"].Value),
                                IsEnable = !bool.Parse(step.Attributes["ReadOnly"] != null ? step.Attributes["ReadOnly"].Value : "false"),
                                EnableConfig = step.Attributes["EnableConfig"] != null && Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                EnableTolerance = step.Attributes["EnableTolerance"] != null && Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                            };
                            sigcolumns.Add(col);
                            break;
                        case "EditableSelection":
                            col = new RecipeTemplateColumnBase()
                            {
                                ValueType = "EditableSelection",
                                ModuleName = step.Attributes["ModuleName"].Value,
                                Default = step.Attributes["Default"] != null ? step.Attributes["Default"].Value : "",
                                ControlName = step.Attributes["ControlName"].Value,
                                DisplayName = step.Attributes["DisplayName"].Value,
                                IsEnable = !bool.Parse(step.Attributes["ReadOnly"] != null ? step.Attributes["ReadOnly"].Value : "false"),
                                EnableConfig = step.Attributes["EnableConfig"] != null && Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                EnableTolerance = step.Attributes["EnableTolerance"] != null && Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                            };
                            XmlNodeList items = step.SelectNodes("Item");
                            foreach (XmlNode item in items)
                            {
                                Option opt = new Option();
                                opt.ControlName = item.Attributes["ControlName"].Value;
                                opt.DisplayName = item.Attributes["DisplayName"].Value;
                                ((RecipeTemplateColumnBase)col).Options.Add(opt);
                            }
                            sigcolumns.Add(col);
                            break;
                        case "ReadOnlySelection":
                        case "LoopSelection":
                            col = new RecipeTemplateColumnBase()
                            {
                                ValueType = "LoopSelection",
                                IsReadOnly = bool.Parse(step.Attributes["ReadOnly"] != null ? step.Attributes["ReadOnly"].Value : "false"),
                                ModuleName = step.Attributes["ModuleName"].Value,
                                ControlName = step.Attributes["ControlName"].Value,
                                DisplayName = step.Attributes["DisplayName"].Value,
                                EnableConfig = step.Attributes["EnableConfig"] != null && Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                EnableTolerance = step.Attributes["EnableTolerance"] != null && Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                            };
                            XmlNodeList options = step.SelectNodes("Item");
                            foreach (XmlNode item in options)
                            {
                                Option opt = new Option();
                                opt.ControlName = item.Attributes["ControlName"].Value;
                                opt.DisplayName = item.Attributes["DisplayName"].Value;
                                ((RecipeTemplateColumnBase)col).Options.Add(opt);
                            }
                            sigcolumns.Add(col);
                            break;
                        case "PopSetting":
                            col = new RecipeTemplateColumnBase()
                            {
                                ValueType = "LoopSelection",
                                ModuleName = step.Attributes["ModuleName"].Value,
                                ControlName = step.Attributes["ControlName"].Value,
                                DisplayName = step.Attributes["DisplayName"].Value,
                                EnableConfig = step.Attributes["EnableConfig"] != null && Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                EnableTolerance = step.Attributes["EnableTolerance"] != null && Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                            };
                            sigcolumns.Add(col);
                            break;

                    }
                }

                columns.Add(node.Attributes["DisplayName"].Value, sigcolumns);
            }

            return columns;

        }




        /// <summary>
        /// get reactor's recipe format define file
        /// </summary>
        /// <param name="chamId"></param>
        /// <returns></returns>
        public string GetRecipeFormatXml(string chamberId)
        {
            var rtn = GetRecipeBody(chamberId, "/Aitex/TableRecipeFormat");
            return rtn;
        }

        /// <summary>
        /// get reactor's template recipe file
        /// </summary>
        /// <param name="chamId"></param>
        /// <returns></returns>
        public string GetRecipeTemplate(string chamberId)
        {
            if (_rcpContext != null)
                return _rcpContext.GetRecipeTemplate(chamberId);

            return GetRecipeBody(chamberId, "/Aitex/TableRecipeData");
        }

        /// <summary>
        /// get reactor's template recipe file
        /// </summary>
        /// <param name="chamId"></param>
        /// <returns></returns>
        public string GetRecipeSchema(string chamberId)
        {
            if (_rcpContext == null)
                return string.Empty;

            string schema = _rcpContext.GetRecipeDefiniton(chamberId);

            XmlDocument dom = new XmlDocument();

            dom.LoadXml(schema);

            XmlNode node = dom.SelectSingleNode("/Aitex/TableRecipeSchema");
            return node.InnerXml;
        }

        public string GetRecipeByBarcode(string chamberId, string barcode)
        {
            try
            {
                string recipePath = PathManager.GetRecipeDir() + chamberId + "\\";
                var di = new DirectoryInfo(recipePath);
                var fis = di.GetFiles("*.rcp", SearchOption.AllDirectories);

                XmlDocument xml = new XmlDocument();
                foreach (var fi in fis)
                {
                    string str = fi.FullName.Substring(recipePath.Length);

                    if (!str.Contains("HistoryRecipe\\"))
                    {
                        xml.Load(fi.FullName);

                        if (xml.SelectSingleNode(string.Format("/TableRecipeData[@Barcode='{0}']", barcode)) != null)
                        {
                            return str.Substring(0, str.LastIndexOf('.'));
                        }
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return string.Empty;
            }
        }


        public List<string> ExpertLayoutRecipeParse(string chamberId, string recipeFile)
        {
            var layoutRecipe = new List<string>();

            string content = LoadRecipe(chamberId, recipeFile, false);

            if (string.IsNullOrEmpty(content))
            {
                //reason = $"{recipeFile} is not a valid recipe file";
                return layoutRecipe;
            }

            try
            {
                XmlDocument rcpDataDoc = new XmlDocument();
                rcpDataDoc.LoadXml(content);

                XmlNode nodeModule;

                nodeModule = rcpDataDoc.SelectSingleNode("/Aitex/TableRecipeData/Module/Step[@Name='Expert']");

                if (nodeModule == null)
                {
                    return layoutRecipe;
                }
                else
                {
                    Dictionary<string, string> recipeData = new Dictionary<string, string>();

                    XmlElement stepNode = nodeModule as XmlElement;

                    //遍历Step节点
                    foreach (XmlAttribute att in stepNode.Attributes)
                    {
                        if (att.Name != "Name")
                        {
                            if (att.Value.ToLower() == "xd")
                            {
                                layoutRecipe.Add("XD");
                            }
                            else if (att.Value.ToLower() == "t")
                            {
                                layoutRecipe.Add("T");
                            }
                            else
                            {
                                layoutRecipe.Add("");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                return layoutRecipe;
            }

            return layoutRecipe;
        }

        public List<string> LayoutExpertRecipeParse(/*string chamberId, */string recipeContent, string slotCount, string cassetteSlotCount)
        {
            var layoutRecipe = new List<string>();

            try
            {
                XmlDocument rcpDataDoc = new XmlDocument();
                rcpDataDoc.LoadXml(recipeContent);

                XmlNode nodeModule;

                nodeModule = rcpDataDoc.SelectSingleNode("/Aitex/TableRecipeData/Module/Step[@Name='Normal']");

                if (nodeModule == null)
                {
                    //reason = "Recipe file does not contains step content for Normal";
                    return layoutRecipe;
                }
                else
                {
                    Dictionary<string, string> recipeData = new Dictionary<string, string>();

                    XmlElement stepNode = nodeModule as XmlElement;

                    //遍历Step节点
                    foreach (XmlAttribute att in stepNode.Attributes)
                    {
                        if (att.Name != "Name")
                        {
                            recipeData[att.Name] = att.Value;
                        }
                    }

                    // 获取SlotCount
                    int iSlotCount = 0;
                    int.TryParse(slotCount, out iSlotCount);

                    // 获取CassetteSlotCount;
                    int iCassetteSlotCount = 0;
                    int.TryParse(cassetteSlotCount, out iCassetteSlotCount);

                    // 先往List里面添加SlotcCount数量的空值，后续再往里面insert具体的值
                    for (int i = 0; i < iSlotCount; i++)
                    {
                        layoutRecipe.Add(string.Empty);
                    }

                    // 获取DummyUpperSlot
                    int iDummyUpperSlot;
                    iDummyUpperSlot = 0;
                    if (recipeData.ContainsKey("DummyUpperSlot"))
                    {
                        int.TryParse(recipeData["DummyUpperSlot"], out iDummyUpperSlot);
                        if (iDummyUpperSlot == 0)
                        {
                            return layoutRecipe;
                        }
                    }
                    else
                    {
                        return layoutRecipe;
                    }

                    // insert Upper Dummy
                    for (int i = 0; i < iDummyUpperSlot; i++)
                    {
                        layoutRecipe[i] = "XD";
                    }

                    // 获取DummyLowerSlot
                    int iDummyLowerSlot;
                    iDummyLowerSlot = 0;
                    if (recipeData.ContainsKey("DummyLowerSlot"))
                    {
                        int.TryParse(recipeData["DummyLowerSlot"], out iDummyLowerSlot);
                        if (iDummyLowerSlot == 0)
                        {
                            return layoutRecipe;
                        }
                    }
                    else
                    {
                        return layoutRecipe;
                    }

                    // insert Lower Dummy
                    for (int i = iSlotCount - 1; i >= iSlotCount - iDummyLowerSlot; i--)
                    {
                        layoutRecipe[i] = "XD";
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                return layoutRecipe;
            }

            return layoutRecipe;
        }

        public List<string> LayoutRecipeParse(/*string chamberId, */string recipeContent, string slotCount, string cassetteSlotCount)
        {
            var layoutRecipe = new List<string>();

            //string content = loadrecipe(chamberid, recipefile, false);

            //if (string.isnullorempty(content))
            //{
            //    reason = $"{recipefile} is not a valid recipe file";
            //    return layoutrecipe;
            //}

            try
            {
                XmlDocument rcpDataDoc = new XmlDocument();
                rcpDataDoc.LoadXml(recipeContent);

                XmlNode nodeModule;

                nodeModule = rcpDataDoc.SelectSingleNode("/Aitex/TableRecipeData/Module/Step[@Name='Normal']");

                if (nodeModule == null)
                {
                    //reason = "Recipe file does not contains step content for Normal";
                    return layoutRecipe;
                }
                else
                {
                    Dictionary<string, string> recipeData = new Dictionary<string, string>();

                    XmlElement stepNode = nodeModule as XmlElement;

                    //遍历Step节点
                    foreach (XmlAttribute att in stepNode.Attributes)
                    {
                        if (att.Name != "Name")
                        {
                            recipeData[att.Name] = att.Value;
                        }
                    }

                    // 获取SlotCount
                    int iSlotCount = 0;
                    int.TryParse(slotCount, out iSlotCount);

                    // 获取CassetteSlotCount;
                    int iCassetteSlotCount = 0;
                    int.TryParse(cassetteSlotCount, out iCassetteSlotCount);

                    // 先往List里面添加SlotcCount数量的空值，后续再往里面insert具体的值
                    for (int i = 0; i < iSlotCount; i++)
                    {
                        layoutRecipe.Add(string.Empty);
                    }

                    // 获取DummyUpperSlot
                    int iDummyUpperSlot;
                    iDummyUpperSlot = 0;
                    if (recipeData.ContainsKey("DummyUpperSlot"))
                    {
                        int.TryParse(recipeData["DummyUpperSlot"], out iDummyUpperSlot);
                        if (iDummyUpperSlot == 0)
                        {
                            return layoutRecipe;
                        }
                    }
                    else
                    {
                        return layoutRecipe;
                    }

                    // insert Upper Dummy
                    for (int i = 0; i < iDummyUpperSlot; i++)
                    {
                        layoutRecipe[i] = "Dummy";
                    }

                    // 获取DummyLowerSlot
                    int iDummyLowerSlot;
                    iDummyLowerSlot = 0;
                    if (recipeData.ContainsKey("DummyLowerSlot"))
                    {
                        int.TryParse(recipeData["DummyLowerSlot"], out iDummyLowerSlot);
                        if (iDummyLowerSlot == 0)
                        {
                            return layoutRecipe;
                        }
                    }
                    else
                    {
                        return layoutRecipe;
                    }

                    // insert Lower Dummy
                    for (int i = iSlotCount - 1; i >= iSlotCount - iDummyLowerSlot; i--)
                    {
                        layoutRecipe[i] = "Dummy";
                    }

                    string strPitch, strProductPos, strProductSlotNo, strMonitorPos, strMonitorSlotNo, strMonitorBetweenCasseteNo;
                    string strPWaferShort, strSDRule, strCassetteInBatchShort, strWaferInCasseteShort, strEDShort, strBoatRule;

                    strPitch = string.Empty;
                    strProductPos = string.Empty;
                    strProductSlotNo = string.Empty;
                    strMonitorPos = string.Empty;
                    strMonitorSlotNo = string.Empty;
                    strMonitorBetweenCasseteNo = string.Empty;
                    strPWaferShort = string.Empty;
                    strSDRule = string.Empty;
                    strCassetteInBatchShort = string.Empty;
                    strWaferInCasseteShort = string.Empty;
                    strEDShort = string.Empty;
                    strBoatRule = string.Empty;

                    // 获取配置
                    if (recipeData.ContainsKey("Pitch"))
                    {
                        strPitch = recipeData["Pitch"];
                    }
                    if (recipeData.ContainsKey("ProductPosition"))
                    {
                        strProductPos = recipeData["ProductPosition"];
                    }
                    if (recipeData.ContainsKey("ProductSlotNo"))
                    {
                        strProductSlotNo = recipeData["ProductSlotNo"];
                    }
                    if (recipeData.ContainsKey("MonitorPosition"))
                    {
                        strMonitorPos = recipeData["MonitorPosition"];
                    }
                    if (recipeData.ContainsKey("MonitorSlotNo"))
                    {
                        strMonitorSlotNo = recipeData["MonitorSlotNo"];
                    }
                    if (recipeData.ContainsKey("MonitorBetweenCassetteNo"))
                    {
                        strMonitorBetweenCasseteNo = recipeData["MonitorBetweenCassetteNo"];
                    }
                    if (recipeData.ContainsKey("WhenPWaferShort"))
                    {
                        strPWaferShort = recipeData["WhenPWaferShort"];
                    }
                    if (recipeData.ContainsKey("SDRule"))
                    {
                        strSDRule = recipeData["SDRule"];
                    }
                    if (recipeData.ContainsKey("WhenCassetteInBatchAreShort"))
                    {
                        strCassetteInBatchShort = recipeData["WhenCassetteInBatchAreShort"];
                    }
                    if (recipeData.ContainsKey("WhenWaferInCassetteAreShort"))
                    {
                        strWaferInCasseteShort = recipeData["WhenWaferInCassetteAreShort"];
                    }
                    if (recipeData.ContainsKey("WhenEDAreShort"))
                    {
                        strEDShort = recipeData["WhenEDAreShort"];
                    }
                    if (recipeData.ContainsKey("RuleOfSpaceInBoat"))
                    {
                        strBoatRule = recipeData["RuleOfSpaceInBoat"];
                    }

                    // insert Production and Monitor and ExtraDummy
                    if (strProductPos == "Auto")
                    {
                        if (strMonitorPos == "BetweenCassette")
                        {
                            if (strPitch == "DoublePitch")
                            {
                                layoutRecipe = ProductAutoMonitorBetweenCassetteDoublePitch(iSlotCount, iCassetteSlotCount, iDummyUpperSlot, iDummyLowerSlot, strMonitorBetweenCasseteNo, strBoatRule);

                            }
                            else if (strPitch == "TriplePitch")
                            {
                                layoutRecipe = ProductAutoMonitorBetweenCassetteTriplePitch(iSlotCount, iCassetteSlotCount, iDummyUpperSlot, iDummyLowerSlot, strMonitorBetweenCasseteNo, strBoatRule);
                            }
                            else
                            {
                                layoutRecipe = ProductAutoMonitorBetweenCassetteStandardPitch(iSlotCount, iCassetteSlotCount, iDummyUpperSlot, iDummyLowerSlot, strMonitorBetweenCasseteNo, strBoatRule);
                            }
                        }
                        else if (strMonitorPos == "Slot")
                        {
                            if (strPitch == "DoublePitch")
                            {
                                layoutRecipe = ProductAutoMonitorSlotDoublePitch(iSlotCount, iCassetteSlotCount, iDummyUpperSlot, iDummyLowerSlot, strMonitorSlotNo, strBoatRule);
                            }
                            else if (strPitch == "TriplePitch")
                            {
                                layoutRecipe = ProductAutoMonitorSlotTriplePitch(iSlotCount, iCassetteSlotCount, iDummyUpperSlot, iDummyLowerSlot, strMonitorSlotNo, strBoatRule);
                            }
                            else
                            {
                                layoutRecipe = ProductAutoMonitorSlotStandardPitch(iSlotCount, iCassetteSlotCount, iDummyUpperSlot, iDummyLowerSlot, strMonitorSlotNo, strBoatRule);
                            }
                        }
                        else
                        {
                            //reason = "MonitorPosition is invalid";
                            return layoutRecipe;
                        }
                    }
                    else if (strProductPos == "Slot")
                    {
                        if (strMonitorPos == "BetweenCassette")
                        {
                            if (strPitch == "DoublePitch")
                            {
                                layoutRecipe = ProductSlotMonitorBetweenCassetteDoublePitch(iSlotCount, iCassetteSlotCount, iDummyUpperSlot, iDummyLowerSlot, strProductSlotNo, strMonitorBetweenCasseteNo, strBoatRule);
                            }
                            else if (strPitch == "TriplePitch")
                            {
                                layoutRecipe = ProductSlotMonitorBetweenCassetteTriplePitch(iSlotCount, iCassetteSlotCount, iDummyUpperSlot, iDummyLowerSlot, strProductSlotNo, strMonitorBetweenCasseteNo, strBoatRule);
                            }
                            else
                            {
                                layoutRecipe = ProductSlotMonitorBetweenCassetteStandardPitch(iSlotCount, iCassetteSlotCount, iDummyUpperSlot, iDummyLowerSlot, strProductSlotNo, strMonitorBetweenCasseteNo, strBoatRule);
                            }
                        }
                        else if (strMonitorPos == "Slot")
                        {
                            if (strPitch == "DoublePitch")
                            {
                                layoutRecipe = ProductSlotMonitorSlotDoublePitch(iSlotCount, iCassetteSlotCount, iDummyUpperSlot, iDummyLowerSlot, strProductSlotNo, strMonitorSlotNo, strBoatRule);
                            }
                            else if (strPitch == "TriplePitch")
                            {
                                layoutRecipe = ProductSlotMonitorSlotTriplePitch(iSlotCount, iCassetteSlotCount, iDummyUpperSlot, iDummyLowerSlot, strProductSlotNo, strMonitorSlotNo, strBoatRule);
                            }
                            else
                            {
                                layoutRecipe = ProductSlotMonitorSlotStandardPitch(iSlotCount, iCassetteSlotCount, iDummyUpperSlot, iDummyLowerSlot, strProductSlotNo, strMonitorSlotNo, strBoatRule);
                            }
                        }
                        else
                        {
                            //reason = "MonitorPosition is invalid";
                            return layoutRecipe;
                        }
                    }
                    else
                    {
                        //reason = "ProductPosition is invalid";
                        return layoutRecipe;
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                return layoutRecipe;
            }

            return layoutRecipe;
        }

        private List<string> ProductAutoMonitorBetweenCassetteStandardPitch(int iSlotCount, int iCassetteSlotCount, int iDummyUpperSlot, int iDummyLowerSlot, string strMonitorBetweenCasseteNo, string strBoatRule)
        {
            var layoutRecipe = new List<string>();

            for (int i = 0; i < iSlotCount; i++)
            {
                layoutRecipe.Add(string.Empty);
            }

            try
            {
                // insert Upper Dummy
                for (int i = 0; i < iDummyUpperSlot; i++)
                {
                    layoutRecipe[i] = "Dummy";
                }

                // insert Lower Dummy
                for (int i = iSlotCount - 1; i >= iSlotCount - iDummyLowerSlot; i--)
                {
                    layoutRecipe[i] = "Dummy";
                }

                var monitorBetweenCassetteNo = new List<string>();
                monitorBetweenCassetteNo = GetLayoutProductionMonitorSlot(strMonitorBetweenCasseteNo);

                // insert Monitor
                int iIndex, iMonitorCount = 7;
                iIndex = iDummyUpperSlot;
                for (int i = 0; i < iMonitorCount; i++)
                {
                    iIndex = iDummyUpperSlot + i * iCassetteSlotCount + i;
                    if (iIndex < iSlotCount - iDummyLowerSlot - 1)
                    {
                        if (monitorBetweenCassetteNo[i] == "ON")
                        {
                            layoutRecipe[iIndex] = "Monitor";
                        }
                    }
                    else
                    {
                        if (monitorBetweenCassetteNo[i] == "ON")
                        {
                            iIndex = iSlotCount - iDummyLowerSlot - 1;
                            layoutRecipe[iIndex] = "Monitor";
                        }
                    }
                }

                // insert Production
                int iFirstMonitorIndex = 0;

                for (int i = iDummyUpperSlot; i < iSlotCount; i++)
                {
                    if (layoutRecipe[i] == "Monitor")
                    {
                        iFirstMonitorIndex = i;
                        break;
                    }
                }

                // Search Up
                for (int i = iFirstMonitorIndex; i >= iDummyUpperSlot; i--)
                {
                    if (layoutRecipe[i] == "")
                    {
                        layoutRecipe[i] = "Production";
                    }
                    else
                    {
                        break;
                    }
                }

                // Search Down
                for (int i = iFirstMonitorIndex; i <= iSlotCount - iDummyLowerSlot - 1; i++)
                {
                    if (layoutRecipe[i] == "")
                    {
                        layoutRecipe[i] = "Production";
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                return layoutRecipe;
            }

            return layoutRecipe;
        }

        private List<string> ProductAutoMonitorBetweenCassetteDoublePitch(int iSlotCount, int iCassetteSlotCount, int iDummyUpperSlot, int iDummyLowerSlot, string strMonitorBetweenCasseteNo, string strBoatRule)
        {
            var layoutRecipe = new List<string>();

            for (int i = 0; i < iSlotCount; i++)
            {
                layoutRecipe.Add(string.Empty);
            }

            try
            {
                // insert Upper Dummy
                for (int i = 0; i < iDummyUpperSlot; i++)
                {
                    layoutRecipe[i] = "Dummy";
                }

                // insert Lower Dummy
                for (int i = iSlotCount - 1; i >= iSlotCount - iDummyLowerSlot; i--)
                {
                    layoutRecipe[i] = "Dummy";
                }

                var monitorBetweenCassetteNo = new List<string>();
                monitorBetweenCassetteNo = GetLayoutProductionMonitorSlot(strMonitorBetweenCasseteNo);

                // insert Monitor
                int iIndex, iMonitorCount = 7;
                iIndex = iDummyUpperSlot;
                for (int i = 0; i < iMonitorCount; i += 2)
                {
                    iIndex = iDummyUpperSlot + 1 + i * iCassetteSlotCount * 2 + i * 2;
                    if (iIndex < iSlotCount - iDummyLowerSlot - 2)
                    {
                        if (monitorBetweenCassetteNo[i] == "ON")
                        {
                            layoutRecipe[iIndex] = "Monitor";
                        }
                    }
                    else
                    {
                        if (monitorBetweenCassetteNo[i] == "ON")
                        {
                            iIndex = iSlotCount - iDummyLowerSlot - 2;
                            layoutRecipe[iIndex] = "Monitor";
                        }
                    }
                }

                // insert Production
                int iFirstMonitorIndex = 0;

                for (int i = iDummyUpperSlot; i < iSlotCount; i++)
                {
                    if (layoutRecipe[i] == "Monitor")
                    {
                        iFirstMonitorIndex = i;
                        break;
                    }
                }

                // search Up
                for (int i = iFirstMonitorIndex; i >= iDummyUpperSlot + 1; i -= 2)
                {
                    if (layoutRecipe[i] == "" && layoutRecipe[i - 1] == "")
                    {
                        layoutRecipe[i] = "Production";
                    }
                    else
                    {
                        break;
                    }
                }

                // serach Down
                for (int i = iFirstMonitorIndex; i <= iSlotCount - iDummyLowerSlot - 2; i += 2)
                {
                    if (layoutRecipe[i] == "" && layoutRecipe[i + 1] == "")
                    {
                        layoutRecipe[i] = "Production";
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                return layoutRecipe;
            }

            return layoutRecipe;
        }

        private List<string> ProductAutoMonitorBetweenCassetteTriplePitch(int iSlotCount, int iCassetteSlotCount, int iDummyUpperSlot, int iDummyLowerSlot, string strMonitorBetweenCasseteNo, string strBoatRule)
        {
            var layoutRecipe = new List<string>();

            for (int i = 0; i < iSlotCount; i++)
            {
                layoutRecipe.Add(string.Empty);
            }

            try
            {
                // insert Upper Dummy
                for (int i = 0; i < iDummyUpperSlot; i++)
                {
                    layoutRecipe[i] = "Dummy";
                }

                // insert Lower Dummy
                for (int i = iSlotCount - 1; i >= iSlotCount - iDummyLowerSlot; i--)
                {
                    layoutRecipe[i] = "Dummy";
                }

                var monitorBetweenCassetteNo = new List<string>();
                monitorBetweenCassetteNo = GetLayoutProductionMonitorSlot(strMonitorBetweenCasseteNo);

                // insert Monitor
                int iIndex, iMonitorCount = 7;
                iIndex = iDummyUpperSlot;
                for (int i = 0; i < iMonitorCount; i += 3)
                {
                    iIndex = iDummyUpperSlot + 2 + i * iCassetteSlotCount * 3 + i * 3;
                    if (iIndex < iSlotCount - iDummyLowerSlot - 3)
                    {
                        if (monitorBetweenCassetteNo[i] == "ON")
                        {
                            layoutRecipe[iIndex] = "Monitor";
                        }
                    }
                    else
                    {
                        if (monitorBetweenCassetteNo[i] == "ON")
                        {
                            iIndex = iSlotCount - iDummyLowerSlot - 3;
                            layoutRecipe[iIndex] = "Monitor";
                        }
                    }
                }

                // insert Production
                int iFirstMonitorIndex = 0;

                for (int i = iDummyUpperSlot; i < iSlotCount; i++)
                {
                    if (layoutRecipe[i] == "Monitor")
                    {
                        iFirstMonitorIndex = i;
                        break;
                    }
                }

                // search Up
                for (int i = iFirstMonitorIndex; i >= iDummyUpperSlot + 2; i -= 3)
                {
                    if (layoutRecipe[i] == "" && layoutRecipe[i - 1] == "" && layoutRecipe[i - 2] == "")
                    {
                        layoutRecipe[i] = "Production";
                    }
                    else
                    {
                        break;
                    }
                }

                // serach Down
                for (int i = iFirstMonitorIndex; i <= iSlotCount - iDummyLowerSlot - 3; i += 3)
                {
                    if (layoutRecipe[i] == "" && layoutRecipe[i + 1] == "" && layoutRecipe[i + 2] == "")
                    {
                        layoutRecipe[i] = "Production";
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                return layoutRecipe;
            }

            return layoutRecipe;
        }

        private List<string> ProductAutoMonitorSlotStandardPitch(int iSlotCount, int iCassetteSlotCount, int iDummyUpperSlot, int iDummyLowerSlot, string strMonitorSlotNo, string strBoatRule)
        {
            var layoutRecipe = new List<string>();

            for (int i = 0; i < iSlotCount; i++)
            {
                layoutRecipe.Add(string.Empty);
            }

            try
            {
                // insert Upper Dummy
                for (int i = 0; i < iDummyUpperSlot; i++)
                {
                    layoutRecipe[i] = "Dummy";
                }

                // insert Lower Dummy
                for (int i = iSlotCount - 1; i >= iSlotCount - iDummyLowerSlot; i--)
                {
                    layoutRecipe[i] = "Dummy";
                }

                var monitorSlotList = new List<string>();
                monitorSlotList = GetLayoutProductionMonitorSlot(strMonitorSlotNo);
                var monitorSlotNo = new List<int>();
                foreach (string str in monitorSlotList)
                {
                    int iPos = 0;
                    int.TryParse(str, out iPos);
                    iPos--;
                    if (!monitorSlotNo.Contains(iPos))
                    {
                        monitorSlotNo.Add(iPos);
                    }
                }

                monitorSlotNo.Sort();

                // insert Monitor
                List<int> monitorNo = new List<int>();
                foreach (int vm in monitorSlotNo)
                {
                    if (layoutRecipe[vm] != "Dummy")
                    {
                        layoutRecipe[vm] = "Monitor";
                    }
                }

                // insert Production
                int iFirstMonitorIndex = 0;

                for (int i = iDummyUpperSlot; i < iSlotCount; i++)
                {
                    if (layoutRecipe[i] == "Monitor")
                    {
                        iFirstMonitorIndex = i;
                        break;
                    }
                }

                for (int i = iDummyUpperSlot; i < iFirstMonitorIndex; i++)
                {
                    if (layoutRecipe[i] == "")
                    {
                        layoutRecipe[i] = "Production";
                    }
                    else
                    {
                        break;
                    }
                }

                for (int i = iFirstMonitorIndex; i <= iSlotCount - iDummyLowerSlot - 1; i++)
                {
                    if (layoutRecipe[i] == "")
                    {
                        layoutRecipe[i] = "Production";
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                return layoutRecipe;
            }

            return layoutRecipe;
        }

        private List<string> ProductAutoMonitorSlotDoublePitch(int iSlotCount, int iCassetteSlotCount, int iDummyUpperSlot, int iDummyLowerSlot, string strMonitorSlotNo, string strBoatRule)
        {
            var layoutRecipe = new List<string>();

            for (int i = 0; i < iSlotCount; i++)
            {
                layoutRecipe.Add(string.Empty);
            }

            try
            {
                // insert Upper Dummy
                for (int i = 0; i < iDummyUpperSlot; i++)
                {
                    layoutRecipe[i] = "Dummy";
                }

                // insert Lower Dummy
                for (int i = iSlotCount - 1; i >= iSlotCount - iDummyLowerSlot; i--)
                {
                    layoutRecipe[i] = "Dummy";
                }

                var monitorSlotList = new List<string>();
                monitorSlotList = GetLayoutProductionMonitorSlot(strMonitorSlotNo);
                var monitorSlotNo = new List<int>();
                foreach (string str in monitorSlotList)
                {
                    int iPos = 0;
                    int.TryParse(str, out iPos);
                    iPos--;
                    if (!monitorSlotNo.Contains(iPos))
                    {
                        monitorSlotNo.Add(iPos);
                    }
                }

                monitorSlotNo.Sort();

                // insert Monitor
                List<int> monitorNo = new List<int>();
                foreach (int vm in monitorSlotNo)
                {
                    if (layoutRecipe[vm] != "Dummy" && vm >= iDummyUpperSlot + 1 && vm <= iSlotCount - iDummyLowerSlot - 2)
                    {
                        layoutRecipe[vm] = "Monitor";
                    }
                }

                // insert Production
                int iFirstMonitorIndex = 0;

                for (int i = iDummyUpperSlot; i < iSlotCount; i++)
                {
                    if (layoutRecipe[i] == "Monitor")
                    {
                        iFirstMonitorIndex = i;
                        break;
                    }
                }

                for (int i = iDummyUpperSlot + 1; i < iFirstMonitorIndex; i += 2)
                {
                    if (layoutRecipe[i] == "" && layoutRecipe[i + 1] == "")
                    {
                        layoutRecipe[i] = "Production";
                    }
                    else
                    {
                        break;
                    }
                }

                for (int i = iFirstMonitorIndex; i <= iSlotCount - iDummyLowerSlot - 2; i += 2)
                {
                    if (layoutRecipe[i] == "" && layoutRecipe[i + 1] == "")
                    {
                        layoutRecipe[i] = "Production";
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                return layoutRecipe;
            }

            return layoutRecipe;
        }

        private List<string> ProductAutoMonitorSlotTriplePitch(int iSlotCount, int iCassetteSlotCount, int iDummyUpperSlot, int iDummyLowerSlot, string strMonitorSlotNo, string strBoatRule)
        {
            var layoutRecipe = new List<string>();

            for (int i = 0; i < iSlotCount; i++)
            {
                layoutRecipe.Add(string.Empty);
            }

            try
            {
                // insert Upper Dummy
                for (int i = 0; i < iDummyUpperSlot; i++)
                {
                    layoutRecipe[i] = "Dummy";
                }

                // insert Lower Dummy
                for (int i = iSlotCount - 1; i >= iSlotCount - iDummyLowerSlot; i--)
                {
                    layoutRecipe[i] = "Dummy";
                }

                var monitorSlotList = new List<string>();
                monitorSlotList = GetLayoutProductionMonitorSlot(strMonitorSlotNo);
                var monitorSlotNo = new List<int>();
                foreach (string str in monitorSlotList)
                {
                    int iPos = 0;
                    int.TryParse(str, out iPos);
                    iPos--;
                    if (!monitorSlotNo.Contains(iPos))
                    {
                        monitorSlotNo.Add(iPos);
                    }
                }

                monitorSlotNo.Sort();

                // insert Monitor
                List<int> monitorNo = new List<int>();
                foreach (int vm in monitorSlotNo)
                {
                    if (layoutRecipe[vm] != "Dummy" && vm >= iDummyUpperSlot + 2 && vm <= iSlotCount - iDummyLowerSlot - 3)
                    {
                        layoutRecipe[vm] = "Monitor";
                    }
                }

                // insert Production
                int iFirstMonitorIndex = 0;

                for (int i = iDummyUpperSlot; i < iSlotCount; i++)
                {
                    if (layoutRecipe[i] == "Monitor")
                    {
                        iFirstMonitorIndex = i;
                        break;
                    }
                }

                for (int i = iDummyUpperSlot + 2; i < iFirstMonitorIndex; i += 3)
                {
                    if (layoutRecipe[i] == "" && layoutRecipe[i + 1] == "" && layoutRecipe[i + 2] == "")
                    {
                        layoutRecipe[i] = "Production";
                    }
                    else
                    {
                        break;
                    }
                }

                for (int i = iFirstMonitorIndex; i <= iSlotCount - iDummyLowerSlot - 3; i += 3)
                {
                    if (layoutRecipe[i] == "" && layoutRecipe[i + 1] == "" && layoutRecipe[i + 2] == "")
                    {
                        layoutRecipe[i] = "Production";
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                return layoutRecipe;
            }

            return layoutRecipe;
        }

        private List<string> ProductSlotMonitorBetweenCassetteStandardPitch(int iSlotCount, int iCassetteSlotCount, int iDummyUpperSlot, int iDummyLowerSlot, string strProductSlotNo, string strMonitorBetweenCasseteNo, string strBoatRule)
        {
            var layoutRecipe = new List<string>();

            for (int i = 0; i < iSlotCount; i++)
            {
                layoutRecipe.Add(string.Empty);
            }

            try
            {
                // insert Upper Dummy
                for (int i = 0; i < iDummyUpperSlot; i++)
                {
                    layoutRecipe[i] = "Dummy";
                }

                // insert Lower Dummy
                for (int i = iSlotCount - 1; i >= iSlotCount - iDummyLowerSlot; i--)
                {
                    layoutRecipe[i] = "Dummy";
                }

                var productSlotList = new List<string>();
                productSlotList = GetLayoutProductionMonitorSlot(strProductSlotNo);
                var productSloNo = new List<int>();
                foreach (string str in productSlotList)
                {
                    int iPos = 0;
                    int.TryParse(str, out iPos);
                    iPos--;
                    if (!productSloNo.Contains(iPos))
                    {
                        productSloNo.Add(iPos);
                    }
                }

                productSloNo.Sort();

                var monitorBetweenCassetteNo = new List<string>();
                monitorBetweenCassetteNo = GetLayoutProductionMonitorSlot(strMonitorBetweenCasseteNo);

                // insert Monitor
                int iIndex, iMonitorCount = 7;
                iIndex = iDummyUpperSlot;
                for (int i = 0; i < iMonitorCount; i++)
                {
                    iIndex = iDummyUpperSlot + i * iCassetteSlotCount + i;
                    if (iIndex < iSlotCount - iDummyLowerSlot - 1)
                    {
                        if (monitorBetweenCassetteNo[i] == "ON")
                        {
                            layoutRecipe[iIndex] = "Monitor";
                        }
                    }
                    else
                    {
                        if (monitorBetweenCassetteNo[i] == "ON")
                        {
                            iIndex = iSlotCount - iDummyLowerSlot - 1;
                            layoutRecipe[iIndex] = "Monitor";
                        }
                    }
                }

                // insert Production
                foreach (int vm in productSloNo)
                {
                    if (layoutRecipe[vm] == "" && vm >= iDummyUpperSlot && vm <= iSlotCount - iDummyLowerSlot - 1)
                    {
                        layoutRecipe[vm] = "Production";

                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                return layoutRecipe;
            }

            return layoutRecipe;
        }

        private List<string> ProductSlotMonitorBetweenCassetteDoublePitch(int iSlotCount, int iCassetteSlotCount, int iDummyUpperSlot, int iDummyLowerSlot, string strProductSlotNo, string strMonitorBetweenCasseteNo, string strBoatRule)
        {
            var layoutRecipe = new List<string>();

            for (int i = 0; i < iSlotCount; i++)
            {
                layoutRecipe.Add(string.Empty);
            }

            try
            {
                // insert Upper Dummy
                for (int i = 0; i < iDummyUpperSlot; i++)
                {
                    layoutRecipe[i] = "Dummy";
                }

                // insert Lower Dummy
                for (int i = iSlotCount - 1; i >= iSlotCount - iDummyLowerSlot; i--)
                {
                    layoutRecipe[i] = "Dummy";
                }

                var productSlotList = new List<string>();
                productSlotList = GetLayoutProductionMonitorSlot(strProductSlotNo);
                var productSloNo = new List<int>();
                foreach (string str in productSlotList)
                {
                    int iPos = 0;
                    int.TryParse(str, out iPos);
                    iPos--;
                    if (!productSloNo.Contains(iPos))
                    {
                        productSloNo.Add(iPos);
                    }
                }

                productSloNo.Sort();

                var monitorBetweenCassetteNo = new List<string>();
                monitorBetweenCassetteNo = GetLayoutProductionMonitorSlot(strMonitorBetweenCasseteNo);

                // insert Monitor
                int iIndex, iMonitorCount = 7;
                iIndex = iDummyUpperSlot;
                for (int i = 0; i < iMonitorCount; i += 2)
                {
                    iIndex = iDummyUpperSlot + 1 + i * iCassetteSlotCount * 2 + i * 2;
                    if (iIndex < iSlotCount - iDummyLowerSlot - 2)
                    {
                        if (monitorBetweenCassetteNo[i] == "ON")
                        {
                            layoutRecipe[iIndex] = "Monitor";
                        }
                    }
                    else
                    {
                        if (monitorBetweenCassetteNo[i] == "ON")
                        {
                            iIndex = iSlotCount - iDummyLowerSlot - 2;
                            layoutRecipe[iIndex] = "Monitor";
                        }
                    }
                }

                // insert Production
                foreach (int vm in productSloNo)
                {
                    if (layoutRecipe[vm] == "" && layoutRecipe[vm - 1] == "" && layoutRecipe[vm + 1] == "" && vm >= iDummyUpperSlot + 1 && vm <= iSlotCount - iDummyLowerSlot - 2)
                    {
                        layoutRecipe[vm] = "Production";

                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                return layoutRecipe;
            }

            return layoutRecipe;
        }

        private List<string> ProductSlotMonitorBetweenCassetteTriplePitch(int iSlotCount, int iCassetteSlotCount, int iDummyUpperSlot, int iDummyLowerSlot, string strProductSlotNo, string strMonitorBetweenCasseteNo, string strBoatRule)
        {
            var layoutRecipe = new List<string>();

            for (int i = 0; i < iSlotCount; i++)
            {
                layoutRecipe.Add(string.Empty);
            }

            try
            {
                // insert Upper Dummy
                for (int i = 0; i < iDummyUpperSlot; i++)
                {
                    layoutRecipe[i] = "Dummy";
                }

                // insert Lower Dummy
                for (int i = iSlotCount - 1; i >= iSlotCount - iDummyLowerSlot; i--)
                {
                    layoutRecipe[i] = "Dummy";
                }

                var productSlotList = new List<string>();
                productSlotList = GetLayoutProductionMonitorSlot(strProductSlotNo);
                var productSloNo = new List<int>();
                foreach (string str in productSlotList)
                {
                    int iPos = 0;
                    int.TryParse(str, out iPos);
                    iPos--;
                    if (!productSloNo.Contains(iPos))
                    {
                        productSloNo.Add(iPos);
                    }
                }

                productSloNo.Sort();

                var monitorBetweenCassetteNo = new List<string>();
                monitorBetweenCassetteNo = GetLayoutProductionMonitorSlot(strMonitorBetweenCasseteNo);

                // insert Monitor
                int iIndex, iMonitorCount = 7;
                iIndex = iDummyUpperSlot;
                for (int i = 0; i < iMonitorCount; i += 3)
                {
                    iIndex = iDummyUpperSlot + 2 + i * iCassetteSlotCount * 3 + i * 3;
                    if (iIndex < iSlotCount - iDummyLowerSlot - 3)
                    {
                        if (monitorBetweenCassetteNo[i] == "ON")
                        {
                            layoutRecipe[iIndex] = "Monitor";
                        }
                    }
                    else
                    {
                        if (monitorBetweenCassetteNo[i] == "ON")
                        {
                            iIndex = iSlotCount - iDummyLowerSlot - 3;
                            layoutRecipe[iIndex] = "Monitor";
                        }
                    }
                }

                // insert Production
                foreach (int vm in productSloNo)
                {
                    if (layoutRecipe[vm] == "" && layoutRecipe[vm - 1] == "" && layoutRecipe[vm - 2] == "" &&
                        layoutRecipe[vm + 1] == "" && layoutRecipe[vm + 2] == "" && vm >= iDummyUpperSlot + 2 && vm <= iSlotCount - iDummyLowerSlot - 3)
                    {
                        layoutRecipe[vm] = "Production";

                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                return layoutRecipe;
            }

            return layoutRecipe;
        }

        private List<string> ProductSlotMonitorSlotStandardPitch(int iSlotCount, int iCassetteSlotCount, int iDummyUpperSlot, int iDummyLowerSlot, string strProductSlotNo, string strMonitorSlotNo, string strBoatRule)
        {
            var layoutRecipe = new List<string>();

            for (int i = 0; i < iSlotCount; i++)
            {
                layoutRecipe.Add(string.Empty);
            }

            try
            {
                // insert Upper Dummy
                for (int i = 0; i < iDummyUpperSlot; i++)
                {
                    layoutRecipe[i] = "Dummy";
                }

                // insert Lower Dummy
                for (int i = iSlotCount - 1; i >= iSlotCount - iDummyLowerSlot; i--)
                {
                    layoutRecipe[i] = "Dummy";
                }

                var productSlotNo = new List<string>();
                productSlotNo = GetLayoutProductionMonitorSlot(strProductSlotNo);

                var monitorSlotNo = new List<string>();
                monitorSlotNo = GetLayoutProductionMonitorSlot(strMonitorSlotNo);

                // insert Production
                foreach (string str in productSlotNo)
                {
                    int iPos = 0;
                    int.TryParse(str, out iPos);
                    if (layoutRecipe[iPos - 1] != "Dummy")
                    {
                        layoutRecipe[iPos - 1] = "Production";

                    }
                }

                // insert Monitor
                foreach (string str in monitorSlotNo)
                {
                    int iPos = 0;
                    int.TryParse(str, out iPos);
                    if (layoutRecipe[iPos - 1] != "Dummy")
                    {
                        layoutRecipe[iPos - 1] = "Monitor";
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                return layoutRecipe;
            }

            return layoutRecipe;
        }

        private List<string> ProductSlotMonitorSlotDoublePitch(int iSlotCount, int iCassetteSlotCount, int iDummyUpperSlot, int iDummyLowerSlot, string strProductSlotNo, string strMonitorSlotNo, string strBoatRule)
        {
            var layoutRecipe = new List<string>();

            for (int i = 0; i < iSlotCount; i++)
            {
                layoutRecipe.Add(string.Empty);
            }

            try
            {
                // insert Upper Dummy
                for (int i = 0; i < iDummyUpperSlot; i++)
                {
                    layoutRecipe[i] = "Dummy";
                }

                // insert Lower Dummy
                for (int i = iSlotCount - 1; i >= iSlotCount - iDummyLowerSlot; i--)
                {
                    layoutRecipe[i] = "Dummy";
                }

                var productSlotNo = new List<string>();
                productSlotNo = GetLayoutProductionMonitorSlot(strProductSlotNo);

                var monitorSlotNo = new List<string>();
                monitorSlotNo = GetLayoutProductionMonitorSlot(strMonitorSlotNo);

                // insert Production
                foreach (string str in productSlotNo)
                {
                    int iPos = 0;
                    int.TryParse(str, out iPos);
                    if (layoutRecipe[iPos - 2] == string.Empty && layoutRecipe[iPos - 1] != "Dummy" && layoutRecipe[iPos] == string.Empty)
                    {
                        layoutRecipe[iPos - 1] = "Production";

                    }
                }

                // insert Monitor
                foreach (string str in monitorSlotNo)
                {
                    int iPos = 0;
                    int.TryParse(str, out iPos);
                    if (layoutRecipe[iPos - 2] == string.Empty && layoutRecipe[iPos - 1] != "Dummy" && layoutRecipe[iPos] == string.Empty)
                    {
                        layoutRecipe[iPos - 1] = "Monitor";
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                return layoutRecipe;
            }

            return layoutRecipe;
        }

        private List<string> ProductSlotMonitorSlotTriplePitch(int iSlotCount, int iCassetteSlotCount, int iDummyUpperSlot, int iDummyLowerSlot, string strProductSlotNo, string strMonitorSlotNo, string strBoatRule)
        {
            var layoutRecipe = new List<string>();

            for (int i = 0; i < iSlotCount; i++)
            {
                layoutRecipe.Add(string.Empty);
            }

            try
            {
                // insert Upper Dummy
                for (int i = 0; i < iDummyUpperSlot; i++)
                {
                    layoutRecipe[i] = "Dummy";
                }

                // insert Lower Dummy
                for (int i = iSlotCount - 1; i >= iSlotCount - iDummyLowerSlot; i--)
                {
                    layoutRecipe[i] = "Dummy";
                }

                var productSlotNo = new List<string>();
                productSlotNo = GetLayoutProductionMonitorSlot(strProductSlotNo);

                var monitorSlotNo = new List<string>();
                monitorSlotNo = GetLayoutProductionMonitorSlot(strMonitorSlotNo);

                // insert Production
                foreach (string str in productSlotNo)
                {
                    int iPos = 0;
                    int.TryParse(str, out iPos);
                    if (layoutRecipe[iPos - 3] == string.Empty && layoutRecipe[iPos - 2] == string.Empty && layoutRecipe[iPos - 1] != "Dummy" &&
                        layoutRecipe[iPos] == string.Empty && layoutRecipe[iPos + 1] == string.Empty)
                    {
                        layoutRecipe[iPos - 1] = "Production";

                    }
                }

                // insert Monitor
                foreach (string str in monitorSlotNo)
                {
                    int iPos = 0;
                    int.TryParse(str, out iPos);
                    if (layoutRecipe[iPos - 3] == string.Empty && layoutRecipe[iPos - 2] == string.Empty && layoutRecipe[iPos - 1] != "Dummy" &&
                        layoutRecipe[iPos] == string.Empty && layoutRecipe[iPos + 1] == string.Empty)
                    {
                        layoutRecipe[iPos - 1] = "Monitor";
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                return layoutRecipe;
            }

            return layoutRecipe;
        }

        private List<string> GetLayoutProductionMonitorSlot(string strParam)
        {
            var slot = new List<string>();

            if (strParam == string.Empty)
            {
                return slot;
            }

            slot = strParam.Split(',').ToList();

            return slot;
        }

        #region Sequence 

        private string GetSequenceConfig(string nodePath)
        {
            if (_seqContext == null)
                return string.Empty;

            string schema = _seqContext.GetConfigXml();

            XmlDocument dom = new XmlDocument();

            dom.LoadXml(schema);

            XmlNode node = dom.SelectSingleNode(nodePath);

            return node.OuterXml;
        }

        public string GetWaferFlowRecipe(string recipeName, bool needValidation)
        {
            string seq = string.Empty;
            try
            {
                using (StreamReader fs = new StreamReader(GenerateRecipeFilePath($"{SC.GetStringValue("System.Recipe.RecipeChamberType")}\\{WaferFlowFolder}", recipeName)))
                {
                    seq = fs.ReadToEnd();
                    fs.Close();
                }
                if (needValidation && !_seqContext.Validation(seq))
                {
                    EV.PostWarningLog(SourceModule, $"Read {recipeName} failed, validation failed");
                    seq = string.Empty;
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                EV.PostWarningLog(SourceModule, $"Read {recipeName} failed, " + ex.Message);
                seq = string.Empty;
            }
            return seq;
        }
        public string GetSequence(string sequenceName, bool needValidation)
        {
            string seq = string.Empty;
            try
            {
                using (StreamReader fs = new StreamReader(GenerateSequenceFilePath(SequenceFolder, sequenceName)))
                {
                    seq = fs.ReadToEnd();
                    fs.Close();
                }
                if (needValidation && !_seqContext.Validation(seq))
                {
                    EV.PostWarningLog(SourceModule, $"Read {sequenceName} failed, validation failed");
                    seq = string.Empty;
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                EV.PostWarningLog(SourceModule, $"Read {sequenceName} failed, " + ex.Message);
                seq = string.Empty;
            }
            return seq;
        }

        public List<string> GetSequenceNameList()
        {
            var result = new List<string>();
            try
            {
                string recipePath = PathManager.GetRecipeDir() + SequenceFolder + "\\";
                var di = new DirectoryInfo(recipePath);
                var fis = di.GetFiles("*.seq", SearchOption.AllDirectories);

                foreach (var fi in fis)
                {
                    string str = fi.FullName.Substring(recipePath.Length);
                    str = str.Substring(0, str.LastIndexOf('.'));

                    result.Add(str);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                EV.PostWarningLog(SourceModule, "Get sequence list failed, " + ex.Message);
            }

            return result;
        }

        public bool DeleteSequence(string sequenceName)
        {
            try
            {
                File.Delete(GenerateSequenceFilePath(SequenceFolder, sequenceName));

                EV.PostInfoLog(SourceModule, $"sequence {sequenceName} deleted");
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                EV.PostWarningLog(SourceModule, $"delete {sequenceName} failed, " + ex.Message);

                return false;
            }
            return true;
        }

        public bool SaveSequence(string sequenceName, string sequenceContent, bool notifyUI)
        {
            bool ret = true;
            try
            {
                var path = GenerateSequenceFilePath(SequenceFolder, sequenceName);
                FileInfo fi = new FileInfo(path);
                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }

                XmlDocument xml = new XmlDocument();
                xml.LoadXml(sequenceContent);

                XmlTextWriter writer = new XmlTextWriter(path, null);
                writer.Formatting = Formatting.Indented;
                xml.Save(writer);
                writer.Close();

                if (notifyUI)
                {
                    EV.PostPopDialogMessage(EventLevel.Information, "Save Complete", $"Sequence {sequenceName} saved ");
                }
                else
                {
                    EV.PostInfoLog(SourceModule, $"Sequence {sequenceName} saved ");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                EV.PostWarningLog(SourceModule, $"save sequence {sequenceName} failed, " + ex.Message);

                if (notifyUI)
                {
                    EV.PostPopDialogMessage(EventLevel.Alarm, "Save Error", $"save sequence {sequenceName} failed, " + ex.Message);
                }
                ret = false;
            }
            return ret;
        }

        public bool SaveAsSequence(string sequenceName, string sequenceContent)
        {
            var path = GenerateSequenceFilePath(SequenceFolder, sequenceName);
            if (File.Exists(path))
            {
                EV.PostWarningLog(SourceModule, $"save sequence {sequenceName} failed, already exist");
                return false;
            }
            return SaveSequence(sequenceName, sequenceContent, false);
        }

        public bool RenameSequence(string oldName, string newName)
        {
            try
            {
                if (File.Exists(GenerateSequenceFilePath(SequenceFolder, newName)))
                {
                    EV.PostWarningLog(SourceModule, $"{newName} already exist, rename failed");
                    return false;
                }
                else
                {
                    File.Move(GenerateSequenceFilePath(SequenceFolder, oldName), GenerateSequenceFilePath(SequenceFolder, newName));

                    EV.PostInfoLog(SourceModule, $"sequence {oldName} renamed to {newName}");
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                EV.PostWarningLog(SourceModule, $"rename {oldName} failed, " + ex.Message);

                return false;
            }
            return true;
        }

        public string GetSequenceFormatXml()
        {
            return GetSequenceConfig("/Aitex/TableSequenceFormat");
        }



        internal bool DeleteSequenceFolder(string folderName)
        {
            try
            {
                Directory.Delete(PathManager.GetRecipeDir() + SequenceFolder + "\\" + folderName, true);

                EV.PostInfoLog(SourceModule, "Folder " + folderName + "deleted");
            }
            catch (Exception ex)
            {
                LOG.Write(ex, "delete sequence folder exception");
                EV.PostWarningLog(SourceModule, $"can not delete folder {folderName}, {ex.Message}");
                return false;
            }
            return true;
        }

        internal bool CreateSequenceFolder(string folderName)
        {
            try
            {
                Directory.CreateDirectory(PathManager.GetRecipeDir() + SequenceFolder + "\\" + folderName);

                EV.PostInfoLog(SourceModule, "Folder " + folderName + "created");
            }
            catch (Exception ex)
            {
                LOG.Write(ex, "sequence folder create exception");
                EV.PostWarningLog(SourceModule, $"can not create folder {folderName}, {ex.Message}");
                return false;
            }
            return true;
        }

        internal bool RenameSequenceFolder(string oldName, string newName)
        {
            try
            {
                string oldPath = PathManager.GetRecipeDir() + SequenceFolder + "\\" + oldName;
                string newPath = PathManager.GetRecipeDir() + SequenceFolder + "\\" + newName;
                Directory.Move(oldPath, newPath);

                EV.PostInfoLog(SourceModule, $"rename folder  from {oldName} to {newName}");
            }
            catch (Exception ex)
            {
                LOG.Write(ex, "rename sequence folder failed");
                EV.PostWarningLog(SourceModule, $"can not rename folder {oldName}, {ex.Message}");
                return false;
            }
            return true;
        }

        public string GetXmlSequenceList(string chamberId)
        {
            XmlDocument doc = new XmlDocument();

            DirectoryInfo curFolderInfo = new DirectoryInfo(PathManager.GetRecipeDir() + SequenceFolder + "\\");
            doc.AppendChild(GenerateSequenceList(chamberId, curFolderInfo, doc));
            return doc.OuterXml;
        }

        XmlElement GenerateSequenceList(string chamberId, DirectoryInfo currentDir, XmlDocument doc)
        {
            int trimLength = (PathManager.GetRecipeDir() + SequenceFolder + "\\").Length;
            XmlElement folderEle = doc.CreateElement("Folder");
            folderEle.SetAttribute("Name", currentDir.FullName.Substring(trimLength));

            DirectoryInfo[] dirInfos = currentDir.GetDirectories();
            foreach (DirectoryInfo dirInfo in dirInfos)
            {

                folderEle.AppendChild(GenerateSequenceList(chamberId, dirInfo, doc));
            }

            FileInfo[] fileInfos = currentDir.GetFiles("*.seq");
            foreach (FileInfo fileInfo in fileInfos)
            {
                XmlElement fileNd = doc.CreateElement("File");
                string fileStr = fileInfo.FullName.Substring(trimLength).TrimStart(new char[] { '\\' }); ;
                fileStr = fileStr.Substring(0, fileStr.LastIndexOf("."));
                fileNd.SetAttribute("Name", fileStr);
                folderEle.AppendChild(fileNd);
            }
            return folderEle;
        }
        #endregion

    }
}
