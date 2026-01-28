using System;
using System.Collections.Generic;

namespace Journal_App.Entities
{
    public class JournalEntry
    {
        public int Id { get; set; }

        // yyyy-MM-dd 
        public string EntryDate { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        // markdown only 
        public string ContentFormat { get; set; } = "markdown";

        public int WordCount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public ICollection<EntryMood> EntryMoods { get; set; } = new List<EntryMood>();
        public ICollection<EntryTag> EntryTags { get; set; } = new List<EntryTag>();
    }
}
