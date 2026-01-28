using Journal_App.Data;
using Journal_App.Entities;
using Journal_App.Security;
using Microsoft.EntityFrameworkCore;
using System;

namespace Journal_App.Services
{
    public enum PinAuthStatus
    {
        Success,
        InvalidPin,
        Locked
    }

    public class UserSettingsService
    {
        private const int SETTINGS_ID = 1;
        private const string PIN_SECRET_TYPE = "pin";

        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);

        private readonly AppDbContext _db;

        // Notify UI when settings change (e.g., username updated)
        public event Action? SettingsChanged;
        private void NotifySettingsChanged() => SettingsChanged?.Invoke();

        public UserSettingsService(AppDbContext db)
        {
            _db = db;
        }

        // ----------------------------------------------------
        // UserSettings (single row)
        // ----------------------------------------------------
        public async Task<UserSettings> GetSettingsAsync()
        {
            var settings = await _db.UserSettings.FirstOrDefaultAsync(s => s.Id == SETTINGS_ID);
            if (settings != null) return settings;

            settings = new UserSettings
            {
                Id = SETTINGS_ID,
                Username = "User",
                ThemeMode = "light",
                PinHint = "The PIN is 1234"
            };

            _db.UserSettings.Add(settings);
            await _db.SaveChangesAsync();

            // New settings row created (optional notify)
            NotifySettingsChanged();

            return settings;
        }

        // ----------------------------------------------------
        // Ensure PIN exists (first run)
        // ----------------------------------------------------
        public async Task EnsurePinSecretExistsAsync()
        {
            var pinSecret = await _db.AuthSecrets.FirstOrDefaultAsync(a => a.SecretType == PIN_SECRET_TYPE);
            if (pinSecret != null) return;

            var (hash, salt, iterations) = PinHasher.Hash("1234");

            _db.AuthSecrets.Add(new AuthSecret
            {
                SecretType = PIN_SECRET_TYPE,
                SecretHash = hash,
                Salt = salt,
                Iterations = iterations,
                FailedAttempts = 0,
                LockedUntil = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            // Default PIN created (optional notify)
            NotifySettingsChanged();
        }

        // ----------------------------------------------------
        // PIN verification (simple bool, no lockout changes)
        // ----------------------------------------------------
        public async Task<bool> VerifyPinAsync(string pinInput)
        {
            await EnsurePinSecretExistsAsync();

            var pinSecret = await _db.AuthSecrets
                .AsNoTracking()
                .FirstAsync(a => a.SecretType == PIN_SECRET_TYPE);

            return PinHasher.Verify(pinInput, pinSecret.SecretHash, pinSecret.Salt, pinSecret.Iterations);
        }

        // ----------------------------------------------------
        // PIN verification WITH lockout (use in Login.razor)
        // ----------------------------------------------------
        public async Task<(PinAuthStatus Status, int? SecondsRemaining)> VerifyPinWithLockoutAsync(string pinInput)
        {
            await EnsurePinSecretExistsAsync();

            var pinSecret = await _db.AuthSecrets.FirstAsync(a => a.SecretType == PIN_SECRET_TYPE);
            var now = DateTime.UtcNow;

            // Locked?
            if (pinSecret.LockedUntil.HasValue && pinSecret.LockedUntil.Value > now)
            {
                var remaining = (int)Math.Ceiling((pinSecret.LockedUntil.Value - now).TotalSeconds);
                return (PinAuthStatus.Locked, remaining);
            }

            // Lock expired -> clear
            if (pinSecret.LockedUntil.HasValue && pinSecret.LockedUntil.Value <= now)
            {
                pinSecret.LockedUntil = null;
                pinSecret.FailedAttempts = 0;
                pinSecret.UpdatedAt = now;
                await _db.SaveChangesAsync();
            }

            // Verify hash
            var ok = PinHasher.Verify(pinInput, pinSecret.SecretHash, pinSecret.Salt, pinSecret.Iterations);

            if (ok)
            {
                pinSecret.FailedAttempts = 0;
                pinSecret.LockedUntil = null;
                pinSecret.UpdatedAt = now;
                await _db.SaveChangesAsync();
                return (PinAuthStatus.Success, null);
            }

            // Failed attempt
            pinSecret.FailedAttempts += 1;

            if (pinSecret.FailedAttempts >= MaxFailedAttempts)
            {
                pinSecret.LockedUntil = now.Add(LockDuration);
            }

            pinSecret.UpdatedAt = now;
            await _db.SaveChangesAsync();

            if (pinSecret.LockedUntil.HasValue)
            {
                var remaining = (int)Math.Ceiling((pinSecret.LockedUntil.Value - now).TotalSeconds);
                return (PinAuthStatus.Locked, remaining);
            }

            return (PinAuthStatus.InvalidPin, null);
        }

        // ----------------------------------------------------
        // Update PIN + hint (settings page)
        // ----------------------------------------------------
        public async Task UpdatePinAsync(string currentPin, string newPin, string newHint)
        {
            var isValid = await VerifyPinAsync(currentPin);
            if (!isValid)
                throw new InvalidOperationException("Current PIN is incorrect.");

            var pinSecret = await _db.AuthSecrets.FirstAsync(a => a.SecretType == PIN_SECRET_TYPE);

            var (hash, salt, iterations) = PinHasher.Hash(newPin);

            pinSecret.SecretHash = hash;
            pinSecret.Salt = salt;
            pinSecret.Iterations = iterations;

            // Reset brute-force state after changing PIN
            pinSecret.FailedAttempts = 0;
            pinSecret.LockedUntil = null;

            pinSecret.UpdatedAt = DateTime.UtcNow;

            var settings = await GetSettingsAsync();
            settings.PinHint = (newHint ?? "").Trim();
            settings.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Notify UI (hint could be shown on login, etc.)
            NotifySettingsChanged();
        }

        // ----------------------------------------------------
        // Username update (requires current PIN)
        // ----------------------------------------------------
        public async Task UpdateUsernameAsync(string currentPin, string newUsername)
        {
            var isValid = await VerifyPinAsync(currentPin);
            if (!isValid)
                throw new InvalidOperationException("Current PIN is incorrect.");

            newUsername = (newUsername ?? "").Trim();

            if (newUsername.Length < 3 || newUsername.Length > 50)
                throw new InvalidOperationException("Username must be between 3 and 50 characters.");

            var settings = await GetSettingsAsync();
            settings.Username = newUsername;
            settings.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Notify UI so NavMenu updates immediately
            NotifySettingsChanged();
        }

        // ----------------------------------------------------
        // Theme handling
        // ----------------------------------------------------
        public async Task<string> GetThemeAsync()
        {
            var settings = await GetSettingsAsync();
            return settings.ThemeMode;
        }

        public async Task UpdateThemeAsync(string themeMode)
        {
            themeMode = (themeMode ?? "").Trim().ToLowerInvariant();

            if (themeMode != "light" && themeMode != "dark")
                throw new ArgumentException("Invalid theme mode.");

            var settings = await GetSettingsAsync();
            settings.ThemeMode = themeMode;
            settings.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Notify UI (optional, but useful if you show theme label somewhere)
            NotifySettingsChanged();
        }
    }
}
