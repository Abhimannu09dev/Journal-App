using System;

namespace Journal_App.Entities
{
    public class JournalEntry
    {
        public int Id { get; set; }

        // ISO date key: yyyy-MM-dd (one entry per day)
        public string EntryDate { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        // Store content as plain text for now (Markdown feature comes later)
        public string Content { get; set; } = string.Empty;

        public int WordCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
