using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProjekatScada.Services
{
    public static class PasswordValidator
    {
        public static IEnumerable<string> Validate(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Lozinka je obavezna.");
                return errors;
            }

            if (password.Length < 15)
            {
                errors.Add("Lozinka mora imati najmanje 15 karaktera.");
            }

            if (!password.Any(char.IsUpper))
            {
                errors.Add("Lozinka mora sadržati bar jedno veliko slovo.");
            }

            if (!password.Any(char.IsLower))
            {
                errors.Add("Lozinka mora sadržati bar jedno malo slovo.");
            }

            if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
            {
                errors.Add("Lozinka mora sadržati bar jedan specijalni karakter.");
            }

            return errors;
        }

        public static bool IsPasswordUnique(string passwordHash, IEnumerable<string> existingPasswordHashes)
        {
            return existingPasswordHashes == null || !existingPasswordHashes.Any(existing => existing == passwordHash);
        }
    }
}
