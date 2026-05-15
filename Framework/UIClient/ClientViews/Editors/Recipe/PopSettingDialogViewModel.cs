using OpenSEMI.ClientBase;
using RecipeEditorLib.RecipeModel.Params;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    public class TemplateSelector : DataTemplateSelector
    {
        private DataTemplate _textBoxTemplate = null;

        public DataTemplate TextBoxTemplate
        {
            get { return _textBoxTemplate; }
            set { _textBoxTemplate = value; }
        }

        private DataTemplate _textBoxTemplateString = null;

        public DataTemplate TextBoxTemplateString
        {
            get { return _textBoxTemplateString; }
            set { _textBoxTemplateString = value; }
        }

        private DataTemplate _comboBoxTemplate = null;
        public DataTemplate ComboBoxTemplate
        {
            get { return _comboBoxTemplate; }
            set { _comboBoxTemplate = value; }
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is Param)
            {
                return (item as Param).GetType().Name == "ComboxParam" ? _comboBoxTemplate : ((item as Param).GetType().Name == "StringParam" ? _textBoxTemplateString : _textBoxTemplate);
            }

            return base.SelectTemplate(item, container);
        }
    }

    public class BandParam : Param
    {
        public Param WavelengthDoubleParam
        {
            get;
            set;
        }

        public Param BandwidthDoubleParam
        {
            get;
            set;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public class PublicPopSettingDialogViewModel : DialogViewModel<ObservableCollection<Param>>
    {

        public ObservableCollection<Param> Parameters
        {
            get;
            set;
        }

        public ObservableCollection<Param> ControlParameters
        {
            get;
            set;
        }

        public ObservableCollection<BandParam> BandParameters
        {
            get;
            set;
        }

        public Visibility BandVisibility => BandParameters == null || BandParameters.Count == 0 ? Visibility.Collapsed : Visibility.Visible;


        public void OK()
        {
            this.DialogResult = Parameters;

            IsCancel = false;
            TryClose(true);
        }

        public void Cancel()
        {
            IsCancel = true;
            TryClose(false);
        }

    }
}
