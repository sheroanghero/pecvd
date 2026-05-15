using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.RecipeCenter;
using OpenSEMI.ClientBase.ServiceProvider;
using RecipeEditorLib.DGExtension.CustomColumn;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Sequence
{
    public class SequenceDataProvider : IProvider
    {
        private static SequenceDataProvider _Instance = null;
        public static SequenceDataProvider Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new SequenceDataProvider();

                return _Instance;
            }
        }
        public void Create()
        {

        }

        public bool CreateSequenceFolder(string foldername)
        {
            return RecipeClient.Instance.Service.CreateSequenceFolder(foldername);
        }

        public bool DeleteSequenceFolder(string foldername)
        {
            return RecipeClient.Instance.Service.DeleteSequenceFolder(foldername);
        }

        public bool Save(SequenceData seq)
        {
            return RecipeClient.Instance.Service.SaveSequence(seq.Name, seq.ToXml());
        }

        public List<string> GetSequences()
        {
            return RecipeClient.Instance.Service.GetSequenceNameList();
        }

        public List<string> GetRecipes(ModuleName modulename)
        {
            return RecipeClient.Instance.Service.GetRecipes(modulename, true).ToList();
        }

        public SequenceData GetSequenceByName(ObservableCollection<EditorDataGridTemplateColumnBase> _columns, string name)
        {
            string content = RecipeClient.Instance.Service.GetSequence(name);
            SequenceData seq = new SequenceData();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);
            seq.Name = name;
            seq.Load(_columns, doc);
            return seq;
        }

        public bool Rename(string beforename, string currentname)
        {
            return RecipeClient.Instance.Service.RenameSequence(beforename, currentname);
        }

        public bool Delete(string name)
        {
            return RecipeClient.Instance.Service.DeleteSequence(name);
        }

        public bool SaveAs(string name, SequenceData seq)
        {
            return RecipeClient.Instance.Service.SaveAsSequence(name, seq.ToXml());
        }

        public string GetSequenceFromXml()
        {
            string template = RecipeClient.Instance.Service.GetSequenceFormatXml();
            return template;
        }
    }
}
