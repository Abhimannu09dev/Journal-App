window.journalTheme = {
    setTheme: function (mode) {
        var resolved = mode;
        if (mode === "system") {
            resolved = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches
                ? "dark"
                : "light";
        }
        document.body.classList.remove("theme-light", "theme-dark");
        document.body.classList.add("theme-" + resolved);

        // Persist so self-apply on next load picks up the right theme
        try { localStorage.setItem("journal-theme", resolved); } catch (e) { }
    }
};

// Self-apply saved theme immediately on script load (before Blazor initialises)
// so the user never sees a flash of the wrong theme.
(function () {
    var saved = "light";
    try { saved = localStorage.getItem("journal-theme") || "light"; } catch (e) { }
    document.body.classList.remove("theme-light", "theme-dark");
    document.body.classList.add("theme-" + saved);
})();
