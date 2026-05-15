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
    /// Validate email address.
    /// </summary>
    public class ValidateEmailAddress : ValidationRule
    {
        /// <summary>
        /// Implement the rule that validates phone number.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var emailAddress = (string)value;

            var pattern = @"\b[A-Z0-9._%-]+@[A-Z0-9.-]+\.[A-Z]{2,4}\b";

            var match = Regex.Match(emailAddress, pattern, RegexOptions.IgnoreCase);

            if (match.Value.Equals(emailAddress))
            {
                return new ValidationResult(true, null);
            }

            return new ValidationResult(false, "Invalid email address.");
        }
    }
}
