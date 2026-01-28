namespace Journal_App.Services.Export
{
    public class ExportOptions
    {
        public bool IncludeTitle { get; set; } = true;
        public bool IncludeMoods { get; set; } = true;
        public bool IncludeTags { get; set; } = true;
        public bool IncludeContent { get; set; } = true;
        public bool PrivacySafeMode { get; set; } = false;
    }
}
