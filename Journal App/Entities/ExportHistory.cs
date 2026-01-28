using System;

namespace Journal_App.Entities
{
    public class ExportHistory
    {
        public int Id { get; set; }
        public string ExportType { get; set; } = "json";

        public string FileName { get; set; } = string.Empty;

        public string? FilePath { get; set; }

        public string? FromDate { get; set; }
        public string? ToDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
