namespace EcommerceApp.Helpers
{
    public static class PasswordHelper
    {
        public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
                return false;

            if (storedHash.StartsWith("$2"))
                return BCrypt.Net.BCrypt.Verify(password, storedHash);

            return storedHash == password;
        }
    }
}
