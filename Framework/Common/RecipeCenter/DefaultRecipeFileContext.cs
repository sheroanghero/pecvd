using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Aitex.Common.Util;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.RecipeCenter;
using Aitex.Core.RT.SCCore;

namespace MECF.Framework.Common.RecipeCenter
{
    public class DefaultRecipeFileContext : IRecipeFileContext
    {
        public string GetRecipeDefiniton(string chamberType)
        {
            try
            {
                //string recipeSchema = PathManager.GetCfgDir() + $@"\Recipe\{chamberType}\RecipeFormat.xml";

                string pmName = chamberType.Substring(0, 3);
                string pmType = SC.GetStringValue($"System.SetUp.{pmName}.ChamberType");
                var supportedChamberType = SC.GetStringValue($"System.Recipe.SupportedChamberType").Split(',');

                string recipeSchema = PathManager.GetCfgDir() + $@"\Recipe\{chamberType}\{pmType}RecipeFormat.xml";

                XmlDocument xmlDom = new XmlDocument();
                xmlDom.Load(recipeSchema);

                return xmlDom.OuterXml;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return "";
            }
        }

        public IEnumerable<string> GetRecipes(string path, bool includingUsedRecipe)
        {
            try
            {
                var recipes = new List<string>();
                string recipePath = PathManager.GetRecipeDir() + path + "\\";

                if (!Directory.Exists(recipePath))
                    return recipes;

                var di = new DirectoryInfo(recipePath);
                var fis = di.GetFiles("*.rcp", SearchOption.AllDirectories);

                foreach (var fi in fis)
                {
                    string str = fi.FullName.Substring(di.FullName.Length);
                    str = str.Substring(0, str.LastIndexOf('.'));
                    if (includingUsedRecipe || (!includingUsedRecipe && !str.Contains("HistoryRecipe\\")))
                    {
                        recipes.Add(str);
                    }
                }
                return recipes;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return new List<string>();
            }
        }

        public IEnumerable<string> GetRecipesPath(string path, bool includingUsedRecipe)
        {
            try
            {
                var recipes = new List<string>();
                string recipePath = PathManager.GetRecipeDir() + path + "\\";

                if (!Directory.Exists(recipePath))
                    return recipes;

                var di = new DirectoryInfo(recipePath);
                var fis = di.GetFiles("*.rcp", SearchOption.AllDirectories);

                foreach (var fi in fis)
                {
                    string str = fi.FullName.Substring(di.FullName.Length);
                    //str = str.Substring(0, str.LastIndexOf('.'));
                    if (includingUsedRecipe || (!includingUsedRecipe && !str.Contains("HistoryRecipe\\")))
                    {
                        recipes.Add(str);
                    }
                }
                return recipes;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return new List<string>();
            }
        }

        public void PostInfoEvent(string message)
        {
            EV.PostMessage("System", EventEnum.GeneralInfo, message);
        }

        public void PostWarningEvent(string message)
        {
            EV.PostMessage("System", EventEnum.DefaultWarning, message);
        }

        public void PostAlarmEvent(string message)
        {
            EV.PostMessage("System", EventEnum.DefaultAlarm, message);
        }

        public void PostDialogEvent(string message)
        {
            EV.PostNotificationMessage(message);
        }

        public void PostInfoDialogMessage(string message)
        {
            EV.PostMessage("System", EventEnum.GeneralInfo, message);

            EV.PostPopDialogMessage(EventLevel.Information, "System Information", message);
        }

        public void PostWarningDialogMessage(string message)
        {
            EV.PostMessage("System", EventEnum.GeneralInfo, message);

            EV.PostPopDialogMessage(EventLevel.Warning, "System Warning", message);
        }

        public void PostAlarmDialogMessage(string message)
        {
            EV.PostMessage("System", EventEnum.GeneralInfo, message);

            EV.PostPopDialogMessage(EventLevel.Alarm, "System Alarm", message);
        }

        public string GetRecipeTemplate(string chamberId)
        {
            string schema = GetRecipeDefiniton(chamberId);

            XmlDocument dom = new XmlDocument();

            dom.LoadXml(schema);

            XmlNode nodeTemplate = dom.SelectSingleNode("/Aitex/TableRecipeData");


            return nodeTemplate.OuterXml;
        }
    }
}
