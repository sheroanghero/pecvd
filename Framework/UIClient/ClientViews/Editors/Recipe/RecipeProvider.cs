using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.RecipeCenter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    public class RecipeProvider
    {
        public List<string> GetRecipes(string path)
        {
            return RecipeClient.Instance.Service.GetRecipesByPath(path, false).ToList();
        }

        public string GetXmlRecipeList(string path)
        {
            return RecipeClient.Instance.Service.GetXmlRecipeListByPath(path, false);
        }

        public List<string> GetRecipeLists()
        {
            return RecipeClient.Instance.Service.GetRecipes(ModuleName.System, true).ToList();
        }

        public string GetRecipeFormatXml(string path)
        {
            return RecipeClient.Instance.Service.GetRecipeFormatXmlByPath(path);
        }

        public bool SaveRecipe(string recipeName, string recipeContent)
        {
            return RecipeClient.Instance.Service.SaveRecipe(ModuleName.System, recipeName, recipeContent);
        }

        public bool SaveRecipe(string prefix, string recipeName, string recipeContent)
        {
            return RecipeClient.Instance.Service.SaveRecipeByPath(prefix, recipeName, recipeContent);
        }


        public string LoadRecipe(string prefix, string recipeName)
        {
            return RecipeClient.Instance.Service.LoadRecipeByPath(prefix, recipeName);
        }

        public string LoadRecipeByFullPath(string fullPath)
        {
            return RecipeClient.Instance.Service.LoadRecipeByFullPath(fullPath);
        }

        public bool CreateRecipeFolder(string prefix, string foldername)
        {
            return RecipeClient.Instance.Service.CreateFolderByPath(prefix, foldername);
        }

        public bool DeleteRecipe(string prefix, string name)
        {
            return RecipeClient.Instance.Service.DeleteRecipeByPath(prefix, name);
        }

        public bool DeleteRecipeFolder(string prefix, string foldername)
        {
            return RecipeClient.Instance.Service.DeleteFolderByPath(prefix, foldername);
        }

        public bool SaveAsRecipe(string prefix, string recipeName, string recipeContent)
        {
            return RecipeClient.Instance.Service.SaveAsRecipeByPath(prefix, recipeName, recipeContent);
        }

        public bool RenameRecipe(string prefix, string beforename, string currentname)
        {
            return RecipeClient.Instance.Service.RenameRecipeByPath(prefix, beforename, currentname);
        }

        public bool BackupRecipe(string fileOriginalPath, string fileDestinationPath, bool isSaveLinkRecipe, List<string> recipeNames)
        {
            return RecipeClient.Instance.Service.BackupRecipe(fileOriginalPath, fileDestinationPath, isSaveLinkRecipe, recipeNames);
        }

        public bool CheckBackRecipeIsLinkRecipe(string fileOriginalPath, List<string> recipeNames)
        {
            return RecipeClient.Instance.Service.CheckBackRecipeIsLinkRecipe(fileOriginalPath, recipeNames);
        }

        public List<string> RestoreRecipeFolderList()
        {
            return RecipeClient.Instance.Service.RestoreRecipeFolderList();
        }

        public string GetXmlRestoreRecipeList(string fileType)
        {
            return RecipeClient.Instance.Service.GetXmlRestoreRecipeList(fileType, false);
        }

        public bool RestoreRecipe(string fileType, bool isSaveLink, List<string> recipeNames)
        {
            return RecipeClient.Instance.Service.RestoreRecipe(fileType, isSaveLink, recipeNames);
        }

        public bool SigRestoreRecipe(string fileType, List<string> recipeNames)
        {
            return RecipeClient.Instance.Service.SigRestoreRecipe(fileType, recipeNames);
        }

        public string LoadRestoreRecipe(string prefix, string recipeName)
        {
            return RecipeClient.Instance.Service.LoadRestoreRecipe(prefix, recipeName);
        }

        public bool RenameFolder(string prefix, string beforename, string currentname)
        {
            return RecipeClient.Instance.Service.RenameFolderByPath(prefix, beforename, currentname);
        }

        public string GetRecipeFormatXml(ModuleName chamId)
        {
            return RecipeClient.Instance.Service.GetRecipeFormatXml(chamId);
        }

        public Dictionary<string, ObservableCollection<RecipeTemplateColumnBase>> GetGroupRecipeTemplate()
        {
            return RecipeClient.Instance.Service.GetGroupRecipeTemplate();
        }

        public List<string> CheckRecipe(string prefix, string recipeName)
        {
            return RecipeClient.Instance.Service.CheckRecipe(prefix, recipeName);
        }
    }
}
