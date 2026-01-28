using Microsoft.Maui;

namespace Journal_App.Services
{
    /// <summary>
    /// Applies theme globally for the MAUI app.
    /// Persistence (saving to DB) should be handled by UserSettingsService.
    /// </summary>
    public class ThemeService
    {
        /// <summary>
        /// Raised whenever theme is applied (useful if Blazor UI wants to re-render).
        /// </summary>
        public event Action<string>? ThemeChanged;

        /// <summary>
        /// Last applied theme mode: "light" | "dark" | "system"
        /// </summary>
        public string CurrentMode { get; private set; } = "system";

        /// <summary>
        /// Apply theme globally.
        /// Accepted values: "light", "dark", "system" (case-insensitive).
        /// Anything else becomes "system".
        /// </summary>
        public void Apply(string? themeMode)
        {
            var mode = Normalize(themeMode);
            CurrentMode = mode;

            var appTheme = mode switch
            {
                "light" => AppTheme.Light,
                "dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified // "system"
            };

            // Apply globally
            if (Application.Current != null)
                Application.Current.UserAppTheme = appTheme;

            ThemeChanged?.Invoke(CurrentMode);
        }

        private static string Normalize(string? themeMode)
        {
            var t = (themeMode ?? "").Trim().ToLowerInvariant();
            return t is "light" or "dark" or "system" ? t : "system";
        }
    }
}
