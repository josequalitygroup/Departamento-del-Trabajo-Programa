using System.Security.Cryptography;
using System.Text;

namespace EmployeeFixedWidthGenerator.App;

internal static class AuthService
{
    private const string ValidUsername = "Kiri";
    private const int Iterations = 120000;

    // PBKDF2(SHA256) hash for initial password configured by owner.
    private static readonly byte[] Salt = Convert.FromBase64String("+3pxaMn7WPARr4f6DBq+Fw==");
    private static readonly byte[] PasswordHash = Convert.FromBase64String("x5TuFRrMZsCOI4amPQhuQ3oaYIz9f/KI8Mg+QOxHCZg=");

    public static bool Validate(string username, string password)
    {
        if (!string.Equals(username, ValidUsername, StringComparison.Ordinal))
        {
            return false;
        }

        byte[] supplied;
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, Salt, Iterations, HashAlgorithmName.SHA256))
        {
            supplied = pbkdf2.GetBytes(32);
        }

        return CryptographicOperations.FixedTimeEquals(supplied, PasswordHash);
    }
}
