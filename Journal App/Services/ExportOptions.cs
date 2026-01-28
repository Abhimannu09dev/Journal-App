namespace Journal_App.Services.Export
{
    public class ExportOptions
    {
        public bool IncludeTitle { get; set; } = true;
        public bool IncludeMoods { get; set; } = true;
        public bool IncludeTags { get; set; } = true;
        public bool IncludeContent { get; set; } = true;

        // If true → overrides the others (privacy-safe export)
        public bool PrivacySafeMode { get; set; } = false;
    }
}
