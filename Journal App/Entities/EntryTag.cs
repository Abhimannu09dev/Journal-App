using System;

namespace Journal_App.Entities
{
    public class EntryTag
    {
        public int EntryId { get; set; }
        public JournalEntry? Entry { get; set; }

        public int TagId { get; set; }
        public Tag? Tag { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
