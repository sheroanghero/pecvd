using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.DataCenter;

namespace MECF.Framework.UI.Core.View.Common
{
    public class PageSCValue
    {
        protected Dictionary<string, PropertyInfo> _fieldMap = new Dictionary<string, PropertyInfo>();

        public PageSCValue()
        {
        }

        public List<string> GetKeys()
        {
            return _fieldMap.Keys.ToList();
        }



        public void UpdateKeys(PropertyInfo[] property)
        {
            _fieldMap.Clear();
            foreach (PropertyInfo fiGroup in property)
            {
                _fieldMap[fiGroup.Name.Replace("_", ".")] = fiGroup;
            }
        }

        public void Update(Dictionary<string, object> result)
        {
            if (result == null)
                return;

            foreach (KeyValuePair<string, object> item in result)
            {
                if (_fieldMap.ContainsKey(item.Key))
                {
                    _fieldMap[item.Key].SetValue(this, item.Value, null);
                }
            }
        }

        public void Update(string key, string value)
        {

            if (!_fieldMap.ContainsKey(key))
                return;

            if (_fieldMap[key].PropertyType == typeof(double))
            {
                _fieldMap[key].SetValue(this, Convert.ToDouble(value), null);
            }
            else if (_fieldMap[key].PropertyType == typeof(int))
            {
                _fieldMap[key].SetValue(this, Convert.ToInt32(value), null);
            }
            else if (_fieldMap[key].PropertyType == typeof(string))
            {
                _fieldMap[key].SetValue(this, value, null);

            }
            else if (_fieldMap[key].PropertyType == typeof(bool))
            {
                _fieldMap[key].SetValue(this, Convert.ToBoolean(value), null);
            }

        }

        public Dictionary<string, object> GetValue()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (var item in _fieldMap)
            {
                result[item.Key] = item.Value.GetValue(this, null);

            }
            return result;
        }

    }
}
