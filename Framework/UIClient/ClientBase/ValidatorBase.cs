using System.ComponentModel;
using OpenSEMI.ClientBase;


namespace MECF.Framework.UI.Client.ClientBase
{
    public abstract class ValidatorBase
        : IDataErrorInfo
    {
        private DataErrorInfo<ValidatorBase> DataErrorValidator;

        #region Implementation of IDataErrorInfo

        public string this[string propertyName]
        {
            get { return GetDataErrorInfo()[propertyName]; }
        }

        public string Error
        {
            get { return GetDataErrorInfo().Error; }
        }

        #endregion

        public bool IsValid
        {
            get { return string.IsNullOrEmpty(Error); }
        }

        private DataErrorInfo<ValidatorBase> GetDataErrorInfo()
        {
            if (DataErrorValidator != null) return DataErrorValidator;
            DataErrorValidator = new DataErrorInfo<ValidatorBase>(this);
            return DataErrorValidator;
        }
    }
}
