using Microsoft.AspNetCore.Identity;

namespace LibraryManagementBE.Helpers
{
    public static class PasswordHelper
    {
        private static readonly PasswordHasher<string> hasher = new();

        public static string Hash(string password)
        {
            return hasher.HashPassword(null, password);
        }

        public static bool Verify(string hash, string input)
        {
            return hasher.VerifyHashedPassword(null, hash, input)
                   == PasswordVerificationResult.Success;
        }
    }
}
