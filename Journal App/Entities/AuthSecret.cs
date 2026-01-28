using System;

namespace Journal_App.Entities
{
    // One row per secret type (e.g., "pin")
    public class AuthSecret
    {
        public int Id { get; set; } = 1;

        // "pin" | future: "password", "biometric"
        public string SecretType { get; set; } = "pin";

        // PBKDF2
        public string SecretHash { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public int Iterations { get; set; } = 100_000;

        // Brute-force protection
        public int FailedAttempts { get; set; } = 0;
        public DateTime? LockedUntil { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
