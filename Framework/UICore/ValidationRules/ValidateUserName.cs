using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace Aitex.Core.UI.ValidationRules
{
    /// <summary>
    /// Validate user name
    /// </summary>
    public class ValidateUserName : ValidationRule
    {
        /// <summary>
        /// Implement the rule that validates user name.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var userName = (string)value;

            // Unicode编码中的汉字范围是/^[\u2E80-\u9FFF]+$/
            var pattern = @"[a-zA-Z_\u2E80-\u9FFF]{1}[\w\u2E80-\u9FFF]{0,7}";

            var match = Regex.Match(userName, pattern);

            if (match.Value.Equals(userName))
            {
                return new ValidationResult(true, null);
            }

            return new ValidationResult(false, "Invalid user name");
        }
    }
}
