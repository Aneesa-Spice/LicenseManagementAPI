using System.Text.RegularExpressions;

namespace LicensingAPI.Services
{
    public class StringTypeDetector
    {
        private static readonly Regex EmailRegex =
       new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

        // true = Email, false = License Key
        public static bool IsEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            return EmailRegex.IsMatch(input.Trim());
        }
    }
}
