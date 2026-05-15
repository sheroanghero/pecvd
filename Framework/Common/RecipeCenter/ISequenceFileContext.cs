using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MECF.Framework.Common.RecipeCenter
{

    public interface ISequenceFileContext
    {
        string GetConfigXml();
        bool Validation(string content);
    }
}
