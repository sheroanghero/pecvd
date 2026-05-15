using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aitex.Core.Util;
using Aitex.Core.WCF;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.RecipeCenter
{
    public class RecipeClient : Singleton<RecipeClient>
    {
        public bool InProcess { get; set; }

        private IRecipeService _service;
        public IRecipeService Service
        {
            get
            {
                if (_service == null)
                {
                    if (InProcess)
                        _service = new RecipeService();
                    else
                        _service = new RecipeServiceClient();
                }

                return _service;
            }
        }
    }
    public class RecipeServiceClient : ServiceClientWrapper<IRecipeService>, IRecipeService
    {
        public RecipeServiceClient()
            : base("Client_IRecipeService", "RecipeService")
        {
        }

        public string LoadRecipe(ModuleName chamId, string recipeName)
        {
            string result = null;
            Invoke(svc => { result = svc.LoadRecipe(chamId, recipeName); });
            return result;   
        }

        public Tuple<string, string> LoadRunTimeRecipeInfo(ModuleName chamId)
        {
            Tuple<string, string> result = null;
            Invoke(svc => { result = svc.LoadRunTimeRecipeInfo(chamId); });
            return result;   
        }

        public IEnumerable<string> GetRecipes(ModuleName chamId, bool includeUsedRecipe)
        {
            IEnumerable<string> result = null;
            Invoke(svc => { result = svc.GetRecipes(chamId, includeUsedRecipe); });
            return result;   
        }

        public string GetXmlRecipeList(ModuleName chamId, bool includeUsedRecipe)
        {
            string result = null;
            Invoke(svc => { result = svc.GetXmlRecipeList(chamId, includeUsedRecipe); });
            return result;   
        }

        public bool DeleteRecipe(ModuleName chamId, string recipeName)
        {
            bool result = false;
            Invoke(svc => { result = svc.DeleteRecipe(chamId, recipeName); });
            return result;   
        }

        public bool DeleteFolder(ModuleName chamId, string folderName)
        {
            bool result = false;
            Invoke(svc => { result = svc.DeleteFolder(chamId, folderName); });
            return result;   
        }

        public bool SaveAsRecipe(ModuleName chamId, string recipeName, string recipeContent)
        {
            bool result = false;
            Invoke(svc => { result = svc.SaveAsRecipe(chamId, recipeName, recipeContent); });
            return result;   
        }

        public bool SaveRecipe(ModuleName chamId, string recipeName, string recipeContent)
        {
            bool result = false;
            Invoke(svc => { result = svc.SaveRecipe(chamId, recipeName, recipeContent); });
            return result;   
        }

        public bool CreateFolder(ModuleName chamId, string folderName)
        {
            bool result = false;
            Invoke(svc => { result = svc.CreateFolder(chamId, folderName); });
            return result;   
        }

        public bool RenameRecipe(ModuleName chamId, string oldName, string newName)
        {
            bool result = false;
            Invoke(svc => { result = svc.RenameRecipe(chamId, oldName, newName); });
            return result;   
        }

        public bool BackupRecipe(string fileOriginalPath, string fileDestinationPath, bool isSaveLinkRecipe, List<string> recipeNames)
        {
            bool result = false;
            Invoke(svc => { result = svc.BackupRecipe(fileOriginalPath, fileDestinationPath, isSaveLinkRecipe,recipeNames); });
            return result;
        }

        public bool CheckBackRecipeIsLinkRecipe(string fileOriginalPath, List<string> recipeNames)
        {
            bool result = false;
            Invoke(svc => { result = svc.CheckBackRecipeIsLinkRecipe(fileOriginalPath, recipeNames); });
            return result;
        }
     
        public List<string> RestoreRecipeFolderList()
        {
            List<string> result = new List<string>();
            Invoke(svc => { result = svc.RestoreRecipeFolderList(); });
            return result;
        }

        public string GetXmlRestoreRecipeList(string fileType, bool includeUsedRecipe)
        {
            string result = null;
            Invoke(svc => { result = svc.GetXmlRestoreRecipeList(fileType, includeUsedRecipe); });
            return result;
        }

        public string LoadRestoreRecipe(string pathName, string recipeName)
        {
            string result = null;
            Invoke(svc => { result = svc.LoadRestoreRecipe(pathName, recipeName); });
            return result;
        }

        public bool RestoreRecipe(string chamId,bool isSaveLink, List<string> recipeNames)
        {
            bool result = false;
            Invoke(svc => { result = svc.RestoreRecipe(chamId, isSaveLink,recipeNames); });
            return result;
        }

        public bool SigRestoreRecipe(string chamId, List<string> recipeNames)
        {
            bool result = false;
            Invoke(svc => { result = svc.SigRestoreRecipe(chamId, recipeNames); });
            return result;
        }

        public bool RenameFolder(ModuleName chamId, string oldName, string newName)
        {
            bool result = false;
            Invoke(svc => { result = svc.RenameFolder(chamId, oldName, newName); });
            return result;   
        }

        public string GetRecipeFormatXml(ModuleName chamId)
        {
            string result = null;
            Invoke(svc => { result = svc.GetRecipeFormatXml(chamId); });
            return result;   
        }

        public Dictionary<string, ObservableCollection<RecipeTemplateColumnBase>> GetGroupRecipeTemplate()
        {
            Dictionary<string, ObservableCollection<RecipeTemplateColumnBase>> result = null;
            Invoke(svc => { result = svc.GetGroupRecipeTemplate(); });
            return result;
        }

        public string GetRecipeTemplate(ModuleName chamId)
        {
            string result = string.Empty;
            Invoke(svc => { result = svc.GetRecipeTemplate(chamId); });
            return result;   
        }
        public string GetRecipeByBarcode(ModuleName chamId, string barcode)
        {
            string result = string.Empty;
            Invoke(svc => { result = svc.GetRecipeByBarcode(chamId, barcode); });
            return result;   
        }


        public string GetXmlSequenceList(ModuleName chamId)
        {
            string result = null;
            Invoke(svc => { result = svc.GetXmlSequenceList(chamId); });
            return result;
        }
        public string GetSequence(string sequenceName)
        {
            string result = string.Empty;
            Invoke(svc => { result = svc.GetSequence(sequenceName); });
            return result;
        }

        public List<string> GetSequenceNameList()
        {
            List<string> result = null;
            Invoke(svc => { result = svc.GetSequenceNameList(); });
            return result;
        }

        public bool DeleteSequence(string sequenceName)
        {
            bool result = false;
            Invoke(svc => { result = svc.DeleteSequence(sequenceName); });
            return result;
        }

        public bool SaveSequence(string sequenceName, string sequenceContent)
        {
            bool result = false;
            Invoke(svc => { result = svc.SaveSequence(sequenceName, sequenceContent); });
            return result;
        }

        public bool SaveAsSequence(string sequenceName, string sequenceContent)
        {
            bool result = false;
            Invoke(svc => { result = svc.SaveAsSequence(sequenceName, sequenceContent); });
            return result;
        }

        public bool RenameSequence(string oldName, string newName)
        {
            bool result = false;
            Invoke(svc => { result = svc.RenameSequence(oldName, newName); });
            return result;
        }

        public string GetSequenceFormatXml()
        {
            string result = string.Empty;
            Invoke(svc => { result = svc.GetSequenceFormatXml(); });
            return result;
        }

        public bool RenameSequenceFolder(string oldName, string newName)
        {
            bool result = false;
            Invoke(svc => { result = svc.RenameSequenceFolder(oldName, newName); });
            return result;
        }

        public bool CreateSequenceFolder(string folderName)
        {
            bool result = false;
            Invoke(svc => { result = svc.CreateSequenceFolder(folderName); });
            return result;
        }

        public bool DeleteSequenceFolder(string folderName)
        {
            bool result = false;
            Invoke(svc => { result = svc.DeleteSequenceFolder(folderName); });
            return result;
        }
        #region extended recipe interface
        public string LoadRecipeByPath(string pathName, string recipeName)
        {
            string result = null;
            Invoke(svc => { result = svc.LoadRecipeByPath(pathName, recipeName); });
            return result;
        }

        public string LoadRecipeByFullPath(string fullPath)
        {
            string result = null;
            Invoke(svc => { result = svc.LoadRecipeByFullPath(fullPath); });
            return result;
        }
        public Tuple<string, string> LoadRunTimeRecipeInfoByPath(string pathName)
        {
            Tuple<string, string> result = null;
            Invoke(svc => { result = svc.LoadRunTimeRecipeInfoByPath(pathName); });
            return result;
        }

        public IEnumerable<string> GetRecipesByPath(string pathName, bool includeUsedRecipe)
        {
            IEnumerable<string> result = null;
            Invoke(svc => { result = svc.GetRecipesByPath(pathName, includeUsedRecipe); });
            return result;
        }

        public string GetXmlRecipeListByPath(string pathName, bool includeUsedRecipe)
        {
            string result = null;
            Invoke(svc => { result = svc.GetXmlRecipeListByPath(pathName, includeUsedRecipe); });
            return result;
        }

        public bool DeleteRecipeByPath(string pathName, string recipeName)
        {
            bool result = false;
            Invoke(svc => { result = svc.DeleteRecipeByPath(pathName, recipeName); });
            return result;
        }

        public bool DeleteFolderByPath(string pathName, string folderName)
        {
            bool result = false;
            Invoke(svc => { result = svc.DeleteFolderByPath(pathName, folderName); });
            return result;
        }

        public bool SaveAsRecipeByPath(string pathName, string recipeName, string recipeContent)
        {
            bool result = false;
            Invoke(svc => { result = svc.SaveAsRecipeByPath(pathName, recipeName, recipeContent); });
            return result;
        }

        public bool SaveRecipeByPath(string pathName, string recipeName, string recipeContent)
        {
            bool result = false;
            Invoke(svc => { result = svc.SaveRecipeByPath(pathName, recipeName, recipeContent); });
            return result;
        }

        public bool CreateFolderByPath(string pathName, string folderName)
        {
            bool result = false;
            Invoke(svc => { result = svc.CreateFolderByPath(pathName, folderName); });
            return result;
        }

        public bool RenameRecipeByPath(string pathName, string oldName, string newName)
        {
            bool result = false;
            Invoke(svc => { result = svc.RenameRecipeByPath(pathName, oldName, newName); });
            return result;
        }

        public bool RenameFolderByPath(string pathName, string oldName, string newName)
        {
            bool result = false;
            Invoke(svc => { result = svc.RenameFolderByPath(pathName, oldName, newName); });
            return result;
        }

        public string GetRecipeFormatXmlByPath(string pathName)
        {
            string result = null;
            Invoke(svc => { result = svc.GetRecipeFormatXmlByPath(pathName); });
            return result;
        }

        public string GetRecipeTemplateByPath(string pathName)
        {
            string result = string.Empty;
            Invoke(svc => { result = svc.GetRecipeTemplateByPath(pathName); });
            return result;
        }
        public string GetRecipeByBarcodeByPath(string pathName, string barcode)
        {
            string result = string.Empty;
            Invoke(svc => { result = svc.GetRecipeByBarcodeByPath(pathName, barcode); });
            return result;
        }

        public List<string> CheckRecipe(string prefix, string recipeName)
        {
            List<string> result = null;
            Invoke(svc => { result = svc.CheckRecipe(prefix, recipeName); });
            return result;
        }
        #endregion
    }
}