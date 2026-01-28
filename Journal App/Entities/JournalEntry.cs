using System;
using System.Collections.Generic;

namespace Journal_App.Entities
{
    public class JournalEntry
    {
        public int Id { get; set; }

        public string EntryDate { get; set; } = string.Empty; // yyyy-MM-dd
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public int WordCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<EntryMood> EntryMoods { get; set; } = new List<EntryMood>();
    }
}
