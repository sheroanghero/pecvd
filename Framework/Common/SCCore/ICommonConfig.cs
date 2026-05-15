using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aitex.Core.RT.ConfigCenter
{
    public interface ICommonConfig
    {
        void Subscribe(string module, string key, Func<object> getter);

        object Poll(string key);
        Dictionary<string, object> PollConfig(IEnumerable<string> keys);

        string GetFileContent(string fileName);
        object GetConfig(string config);
        void SetConfig(string config, object value);
    }
}
