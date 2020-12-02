using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Controls;

namespace ScreenShotApp.Utils
{
	public class TextboxDoubleValidationRules: ValidationRule
	{
        public double Min { get; set; }
        public double Max { get; set; }

        public TextboxDoubleValidationRules()
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double result = 0;

            if(!double.TryParse(value as string, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
			{
                return new ValidationResult(false, "Incorrect format.");
			}

            if((result < Min) || (result > Max))
            {
                return new ValidationResult(false,
                  $"Please enter a value in the range: {Min:0.##}-{Max:0.##}.");
            }
            return ValidationResult.ValidResult;
        }
    }
}
