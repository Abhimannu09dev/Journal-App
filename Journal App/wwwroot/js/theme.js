window.journalTheme = {
    setTheme: function (mode) {
        let resolved = mode;
        if (mode === "system") {
            resolved = window.matchMedia &&
                window.matchMedia("(prefers-color-scheme: dark)").matches
                ? "dark"
                : "light";
        }
        document.body.classList.remove("theme-light", "theme-dark");
        document.body.classList.add("theme-" + resolved);
    }
};