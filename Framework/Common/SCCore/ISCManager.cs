using System.Collections.Generic;
using Aitex.Core.RT.SCCore;

namespace MECF.Framework.Common.SCCore
{
    public interface ISCManager
    {
        T GetValue<T>(string name) where T : struct ;

        string GetStringValue(string name);

        void SetItemValue(string name, object value);
        void SetItemValueStringFormat(string name, string value);
        void SetItemValue(string name, bool value);
        void SetItemValue(string name, int value);
        void SetItemValue(string name, double value);
        void SetItemValue(string name, string value);
        void SetItemValueFromString(string name, string value);
        
        SCConfigItem GetConfigItem(string name);

        bool ContainsItem(string name);

        List<SCConfigItem> GetItemList();

        string GetFileContent();

        T SafeGetValue<T>(string name, T defaultValue) where T : struct;
        string SafeGetStringValue(string name, string defaultValue);
    }
}
