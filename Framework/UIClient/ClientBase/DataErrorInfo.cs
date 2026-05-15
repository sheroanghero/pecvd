using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace MECF.Framework.UI.Client.ClientBase
{
    public class DataErrorInfo<T>
       : IDataErrorInfo where T : class
    {
        private readonly T ImplementorInstance;
        private Dictionary<string, Func<T, object>> PropertyGetterMap;
        private Dictionary<string, ValidationAttribute[]> ValidatorMap;
        private bool HasInitialisedValidator;

        public DataErrorInfo(T implementator)
        {
            ImplementorInstance = implementator;
        }

        #region Implementation of IDataErrorInfo

        public string this[string propertyName]
        {
            get
            {
                if (!HasInitialisedValidator) InitialiseValidators();
                if (PropertyGetterMap.ContainsKey(propertyName))
                {
                    var propertyValue = PropertyGetterMap[propertyName](ImplementorInstance);
                    var errorMessages = ValidatorMap[propertyName]
                        .Where(v => !v.IsValid(propertyValue))
                        .Select(v => v.ErrorMessage).ToArray();

                    return string.Join(Environment.NewLine, errorMessages);
                }

                return string.Empty;
            }
        }

        public string Error
        {
            get
            {
                if (!HasInitialisedValidator) InitialiseValidators();
                var errors = from validator in ValidatorMap
                             from attribute in validator.Value
                             where !attribute.IsValid(PropertyGetterMap[validator.Key](ImplementorInstance))
                             select attribute.ErrorMessage;

                return string.Join(Environment.NewLine, errors.ToArray());
            }
        }

        private void InitialiseValidators()
        {
            HasInitialisedValidator = true;
            ValidatorMap = ImplementorInstance.GetType()
                .GetProperties()
                .Where(p => GetValidations(p).Length != 0)
                .ToDictionary(p => p.Name, GetValidations);

            PropertyGetterMap = ImplementorInstance.GetType()
                .GetProperties()
                .Where(p => GetValidations(p).Length != 0)
                .ToDictionary(p => p.Name, GetValueGetter);
        }

        private static ValidationAttribute[] GetValidations(PropertyInfo property)
        {
            return (ValidationAttribute[])property.GetCustomAttributes(typeof(ValidationAttribute), true);
        }

        private static Func<T, object> GetValueGetter(PropertyInfo property)
        {
            return viewmodel => property.GetValue(viewmodel, null);
        }

        #endregion Implementation of IDataErrorInfo
    }
}
