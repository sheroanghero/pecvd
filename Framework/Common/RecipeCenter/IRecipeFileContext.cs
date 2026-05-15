using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.RT.RecipeCenter
{
    public interface IRecipeFileContext
    {
        string GetRecipeDefiniton(string chamberType);

        IEnumerable<string> GetRecipes(string chamberId, bool includingUsedRecipe);

        IEnumerable<string> GetRecipesPath(string chamberId, bool includingUsedRecipe);

        void PostInfoEvent(string message);

        void PostWarningEvent(string message);

        void PostAlarmEvent(string message);

        void PostDialogEvent(string message);

        void PostInfoDialogMessage(string message);

        void PostWarningDialogMessage(string message);

        void PostAlarmDialogMessage(string message);

        string GetRecipeTemplate(string chamberId);
    }

}
