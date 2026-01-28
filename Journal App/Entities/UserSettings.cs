using System;

namespace Journal_App.Entities
{
    public class UserSettings
    {
        public int Id { get; set; } = 1;

        public string Username { get; set; } = "User";

        // THEME: "light" | "dark"
        public string ThemeMode { get; set; } = "light";

        public string? AccentColorHex { get; set; }
        public string? CustomThemeJson { get; set; }

        // SECURITY (PIN-related UI hint)
        public string PinHint { get; set; } = "The PIN is 1234";

        // AUDIT
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
