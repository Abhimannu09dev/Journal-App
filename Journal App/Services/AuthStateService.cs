using System;

namespace Journal_App.Services
{
    /// <summary>
    /// Simple in-app auth state (PIN gate).
    /// - Does NOT verify PIN (that belongs in UserSettingsService).
    /// - Only tracks whether the user is currently "logged in".
    /// </summary>
    public class AuthStateService
    {
        public bool IsLoggedIn { get; private set; }
        public bool IsAuthenticated => IsLoggedIn;

        /// <summary>
        /// Fired whenever login/logout state changes.
        /// Use this to refresh UI (e.g., show/hide nav, redirect, etc.)
        /// </summary>
        public event Action? StateChanged;

        public void Login()
        {
            if (IsLoggedIn) return;

            IsLoggedIn = true;
            StateChanged?.Invoke();
        }

        public void Logout()
        {
            if (!IsLoggedIn) return;

            IsLoggedIn = false;
            StateChanged?.Invoke();
        }

        /// <summary>
        /// Useful if you want to force logout on app start or after timeout later.
        /// </summary>
        public void Reset()
        {
            IsLoggedIn = false;
            StateChanged?.Invoke();
        }
    }
}
