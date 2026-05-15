using System.Globalization;
using System.Threading;
using MECF.Framework.Common.Properties;

namespace Aitex.Core.Utilities
{
    public class CultureSupported
    {
        public const string Chinese = "zh-CN";
        public const string English = "en-US";

        public static void UpdateCoreCultureResource(string culture)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);

            Resources.Culture = new CultureInfo(culture);
        }
    }
}
