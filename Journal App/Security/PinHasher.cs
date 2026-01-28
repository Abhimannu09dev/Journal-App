using System.Security.Cryptography;

namespace Journal_App.Security
{
    public static class PinHasher
    {
        public static (string hashB64, string saltB64, int iterations) Hash(string pin, int iterations = 100_000)
        {
            if (string.IsNullOrWhiteSpace(pin) || pin.Length != 4 || !pin.All(char.IsDigit))
                throw new ArgumentException("PIN must be exactly 4 digits.");

            byte[] salt = RandomNumberGenerator.GetBytes(16);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password: pin,
                salt: salt,
                iterations: iterations,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: 32
            );

            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt), iterations);
        }

        public static bool Verify(string pin, string storedHashB64, string storedSaltB64, int iterations)
        {
            if (string.IsNullOrWhiteSpace(pin)) return false;

            byte[] salt = Convert.FromBase64String(storedSaltB64);
            byte[] expectedHash = Convert.FromBase64String(storedHashB64);

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password: pin,
                salt: salt,
                iterations: iterations,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: expectedHash.Length
            );

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}
