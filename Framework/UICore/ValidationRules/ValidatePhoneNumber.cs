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
    /// Validate phone number.
    /// </summary>
    public class ValidatePhoneNumber : ValidationRule
    {
        /// <summary>
        /// Implement the rule that validates phone number.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var phoneNumber = (string)value;

            var pattern = @"\d{8,13}";

            var match = Regex.Match(phoneNumber, pattern);

            if (match.Value.Equals(phoneNumber))
            {
                return new ValidationResult(true, null);
            }

            return new ValidationResult(false, "Invalid phone number. Phone number must be 8-13 digits.");
        }
    }
}
