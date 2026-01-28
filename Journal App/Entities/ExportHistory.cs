using System;

namespace Journal_App.Entities
{
    public class ExportHistory
    {
        public int Id { get; set; }

        // "pdf" | "json" | "csv"
        public string ExportType { get; set; } = "json";

        public string FileName { get; set; } = string.Empty;

        // Optional: store path if you save locally
        public string? FilePath { get; set; }

        public string? FromDate { get; set; } // YYYY-MM-DD
        public string? ToDate { get; set; }   // YYYY-MM-DD

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
