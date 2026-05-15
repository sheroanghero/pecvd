using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ServiceModel;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Key;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.RecipeCenter;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.RecipeCenter
{
    class RecipeService : IRecipeService
    {
        /// <summary>
        ///  Load a recipe from server to client for editing.
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="recipeName"></param>
        /// <returns></returns>
        public string LoadRecipe(ModuleName chamId, string recipeName)
        {
            EV.PostInfoLog(chamId.ToString(), string.Format("Read {0} recipe {1}.", chamId, recipeName));

            return RecipeFileManager.Instance.LoadRecipe(chamId.ToString(), recipeName, false);
        }

        /// <summary>
        /// Delete a recipe by recipe name
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="recipeName"></param>
        /// <returns></returns>
        public bool DeleteRecipe(ModuleName chamId, string recipeName)
        {
            if (KeyManager.Instance.IsExpired)
            {
                EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                return false;
            }
            //if (!ChamberOperationService.IsSuperEnter()) return false;
            EV.PostInfoLog(chamId.ToString(), string.Format("Delete {0} recipe {1}.", chamId, recipeName));
            return RecipeFileManager.Instance.DeleteRecipe(chamId.ToString(), recipeName);
        }


        /// <summary>
        /// Get recipe list in xml format
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="includingUsedRecipe"></param>
        /// <returns></returns>
        public string GetXmlRecipeList(ModuleName chamId, bool includingUsedRecipe)
        {
            return RecipeFileManager.Instance.GetXmlRecipeList(chamId.ToString(), includingUsedRecipe);
        }


        /// <summary>
        /// Get recipe data by recipe name
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="includingUsedRecipe"></param>
        /// <returns></returns>
        public IEnumerable<string> GetRecipes(ModuleName chamId, bool includingUsedRecipe)
        {
            return RecipeFileManager.Instance.GetRecipes(chamId.ToString(), includingUsedRecipe);
        }

        /// <summary>
        /// Rename recipe
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public bool RenameRecipe(ModuleName chamId, string oldName, string newName)
        {
            if (KeyManager.Instance.IsExpired)
            {
                EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                return false;
            }
            EV.PostInfoLog(chamId.ToString(), string.Format("Rename {0} recipe {1} to {2}.", chamId, oldName, newName));
            //if (!ChamberOperationService.IsSuperEnter()) return false;
            return RecipeFileManager.Instance.RenameRecipe(chamId.ToString(), oldName, newName);
        }


        public bool BackupRecipe(string fileOriginalPath, string fileDestinationPath, bool isSaveLinkRecipe, List<string> recipeNames)
        {
            if (KeyManager.Instance.IsExpired)
            {
                EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                return false;
            }
            EV.PostInfoLog(fileOriginalPath.ToString(), string.Format("Backup {0} recipe .", fileOriginalPath));
            //if (!ChamberOperationService.IsSuperEnter()) return false;
            return RecipeFileManager.Instance.BackupRecipe(fileOriginalPath, fileDestinationPath, isSaveLinkRecipe, recipeNames);
        }

      public  bool CheckBackRecipeIsLinkRecipe(string fileOriginalPath, List<string> recipeNames)
        {
            if (KeyManager.Instance.IsExpired)
            {
                EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                return false;
            }
            EV.PostInfoLog(fileOriginalPath.ToString(), string.Format("Backup IsLinkRecipe {0} recipe .", fileOriginalPath));
            return RecipeFileManager.Instance.CheckBackRecipeIsLinkRecipe(fileOriginalPath,recipeNames);
        }

        public List<string> RestoreRecipeFolderList()
        {
            return RecipeFileManager.Instance.RestoreRecipeFolderList();
        }

        public string GetXmlRestoreRecipeList(string fileType, bool includingUsedRecipe)
        {

            return RecipeFileManager.Instance.GetXmlRestoreRecipeList(fileType, includingUsedRecipe);
        }

        public string LoadRestoreRecipe(string pathName, string recipeName)
        {
            EV.PostInfoLog(pathName, string.Format("Read {0} recipe {1}.", pathName, recipeName));

            return RecipeFileManager.Instance.LoadRestoreRecipe(pathName, recipeName, false);
        }


        public bool RestoreRecipe(string chamId,bool isSaveLink, List<string> recipeNames)
        {
            if (KeyManager.Instance.IsExpired)
            {
                EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                return false;
            }
            EV.PostInfoLog(chamId.ToString(), string.Format("Restore {0} recipe .", chamId));
            return RecipeFileManager.Instance.RestoreRecipe(chamId.ToString(), isSaveLink,recipeNames);
        }

        public bool SigRestoreRecipe(string chamId, List<string> recipeNames)
        {
            if (KeyManager.Instance.IsExpired)
            {
                EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                return false;
            }
            EV.PostInfoLog(chamId.ToString(), string.Format("SigRestoreRecipe {0} recipe .", chamId));
            return RecipeFileManager.Instance.SigRestoreRecipe(chamId.ToString(), recipeNames);
        }
        /// <summary>
        /// delete a recipe folder
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public bool DeleteFolder(ModuleName chamId, string folderName)
        {
            if (KeyManager.Instance.IsExpired)
            {
                EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                return false;
            }
            //if (!ChamberOperationService.IsSuperEnter()) return false;
            EV.PostInfoLog(chamId.ToString(), string.Format("Delete {0} recipe folder {1}.", chamId, folderName));
            return RecipeFileManager.Instance.DeleteFolder(chamId.ToString(), folderName);
        }

        /// <summary>
        /// save recipe content
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="recipeName"></param>
        /// <param name="recipeContent"></param>
        /// <returns></returns>
        public bool SaveRecipe(ModuleName chamId, string recipeName, string recipeContent)
        {
            if (KeyManager.Instance.IsExpired)
            {
                EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                return false;
            }
            EV.PostInfoLog(chamId.ToString(), string.Format("Modify and save {0} recipe {1}.", chamId, recipeName));
            return RecipeFileManager.Instance.SaveRecipe(chamId.ToString(), recipeName, recipeContent, false, false);
        }

        /// <summary>
        /// save recipe content
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="recipeName"></param>
        /// <param name="recipeContent"></param>
        /// <returns></returns>
        public bool SaveAsRecipe(ModuleName chamId, string recipeName, string recipeContent)
        {
            if (KeyManager.Instance.IsExpired)
            {
                EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                return false;
            }
            EV.PostInfoLog(chamId.ToString(), string.Format("Modify and save as {0} recipe {1}.", chamId, recipeName));
            return RecipeFileManager.Instance.SaveAsRecipe(chamId.ToString(), recipeName, recipeContent);
        }

        /// <summary>
        /// create a new recipe folder
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public bool CreateFolder(ModuleName chamId, string folderName)
        {
            if (KeyManager.Instance.IsExpired)
            {
                EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                return false;
            }
            EV.PostInfoLog(chamId.ToString(), string.Format("Create {0} recipe foler {1}.", chamId, folderName));
            return RecipeFileManager.Instance.CreateFolder(chamId.ToString(), folderName);
        }

        /// <summary>
        /// Rename recipe folder name
        /// </summary>
        /// <param name="chamId"></param>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        public bool RenameFolder(ModuleName chamId, string oldName, string newName)
        {
            if (KeyManager.Instance.IsExpired)
            {
                EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
                return false;
            }
            EV.PostInfoLog(chamId.ToString(), string.Format("Rename {0} recipe folder {1} to {2}.", chamId, oldName, newName));
            return RecipeFileManager.Instance.RenameFolder(chamId.ToString(), oldName, newName);
        }


        /// <summary>
        /// get reactor's recipe format define file
        /// </summary>
        /// <param name="chamId"></param>
        /// <returns></returns>
        public string GetRecipeFormatXml(ModuleName chamId)
        {
            return RecipeFileManager.Instance.GetRecipeFormatXml(chamId.ToString());
        }

        public Dictionary<string, ObservableCollection<RecipeTemplateColumnBase>> GetGroupRecipeTemplate()
        {
            return RecipeFileManager.Instance.GetGroupRecipeTemplate();
        }

        /// <summary>
        /// get reactor's template recipe file
        /// </summary>
        /// <param name="chamId"></param>
        /// <returns></returns>
        public string GetRecipeTemplate(ModuleName chamId)
        {
            return RecipeFileManager.Instance.GetRecipeTemplate(chamId.ToString());
        }

        /// <summary>
        /// 获取当前反应腔正在运行的Recipe信息
        /// </summary>
        /// <param name="chamId"></param>
        /// <returns>recipeName + recipeContent</returns>
        public Tuple<string, string> LoadRunTimeRecipeInfo(ModuleName chamId)
        {
            return null;
            //var pm = PmManager.GetReactor(chamId);
            //if (pm != null) return pm.LoadRunTimeRecipeInfo();
            //return Singleton<PMEntity>.Instance.GetRecipeInfo();
        }

        public string GetRecipeByBarcode(ModuleName chamId, string barcode)
        {
            return "";
            //return RecipeFileManager.Instance.GetRecipeByBarcode(chamId.ToString(), barcode);
        }


        public string GetXmlSequenceList(ModuleName chamId)
        {
            return RecipeFileManager.Instance.GetXmlSequenceList(chamId.ToString());
        }

        public string GetSequence(string sequenceName)
        {
            return RecipeFileManager.Instance.GetSequence(sequenceName, true);
        }

        public List<string> GetSequenceNameList()
        {
            return RecipeFileManager.Instance.GetSequenceNameList();
        }

        public bool DeleteSequence(string sequenceName)
        {
            return RecipeFileManager.Instance.DeleteSequence(sequenceName);
        }

        public bool SaveSequence(string sequenceName, string sequenceContent)
        {
            return RecipeFileManager.Instance.SaveSequence(sequenceName, sequenceContent, false);
        }

        public bool SaveAsSequence(string sequenceName, string sequenceContent)
        {
            return RecipeFileManager.Instance.SaveAsSequence(sequenceName, sequenceContent);
        }

        public bool RenameSequence(string oldName, string newName)
        {
            return RecipeFileManager.Instance.RenameSequence(oldName, newName);
        }

        public string GetSequenceFormatXml()
        {
            return RecipeFileManager.Instance.GetSequenceFormatXml();
        }

        public bool RenameSequenceFolder(string oldName, string newName)
        {
            return RecipeFileManager.Instance.RenameSequenceFolder(oldName, newName);
        }

        public bool CreateSequenceFolder(string folderName)
        {
            return RecipeFileManager.Instance.CreateSequenceFolder(folderName);
        }

        public bool DeleteSequenceFolder(string folderName)
        {
            return RecipeFileManager.Instance.DeleteSequenceFolder(folderName);
        }

        #region extended recipe interface

        public string LoadRecipeByPath(string pathName, string recipeName)
        {
            return RecipeFileManager.Instance.LoadRecipe(pathName, recipeName, false);
        }

        public string LoadRecipeByFullPath(string fullPath)
        {
            return RecipeFileManager.Instance.LoadRecipeByFullPath(fullPath);
        }

        public bool DeleteRecipeByPath(string pathName, string recipeName)
        {
            return RecipeFileManager.Instance.DeleteRecipe(pathName, recipeName);
        }
        public string GetXmlRecipeListByPath(string pathName, bool includingUsedRecipe)
        {
            return RecipeFileManager.Instance.GetXmlRecipeList(pathName, includingUsedRecipe);
        }
        public IEnumerable<string> GetRecipesByPath(string pathName, bool includingUsedRecipe)
        {
            return RecipeFileManager.Instance.GetRecipes(pathName, includingUsedRecipe);
        }
        public bool RenameRecipeByPath(string pathName, string oldName, string newName)
        {
            return RecipeFileManager.Instance.RenameRecipe(pathName, oldName, newName);
        }
        public bool DeleteFolderByPath(string pathName, string folderName)
        {
            return RecipeFileManager.Instance.DeleteFolder(pathName, folderName);
        }
        public bool SaveRecipeByPath(string pathName, string recipeName, string recipeContent)
        {
            return RecipeFileManager.Instance.SaveRecipe(pathName, recipeName, recipeContent, false, false);
        }
        public bool SaveAsRecipeByPath(string pathName, string recipeName, string recipeContent)
        {
            return RecipeFileManager.Instance.SaveAsRecipe(pathName, recipeName, recipeContent);
        }
        public bool CreateFolderByPath(string pathName, string folderName)
        {
            return RecipeFileManager.Instance.CreateFolder(pathName, folderName);
        }
        public bool RenameFolderByPath(string pathName, string oldName, string newName)
        {
            return RecipeFileManager.Instance.RenameFolder(pathName, oldName, newName);
        }
        public string GetRecipeFormatXmlByPath(string pathName)
        {
            return RecipeFileManager.Instance.GetRecipeFormatXml(pathName);
        }
        public string GetRecipeTemplateByPath(string pathName)
        {
            return RecipeFileManager.Instance.GetRecipeTemplate(pathName);
        }
        public Tuple<string, string> LoadRunTimeRecipeInfoByPath(string pathName)
        {
            return null;
        }
        public string GetRecipeByBarcodeByPath(string pathName, string barcode)
        {
            return "";
        }

        public List<string> CheckRecipe(string prefix, string recipeName)
        {
            RecipeFileManager.Instance.CheckRecipe(prefix, recipeName, out List<string> reasons);
            return reasons;
        }

        public bool CheckRecipe(string prefix, string recipeName, out List<string> reasons)
        {
            throw new NotImplementedException();
        }




        #endregion
    }
}