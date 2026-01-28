using System;

namespace Journal_App.Entities
{
    // Roles allowed: "primary" or "secondary"
    public class EntryMood
    {
        // Composite key will be configured in DbContext:
        // (EntryId, MoodId, MoodRole)

        public int EntryId { get; set; }
        public JournalEntry? Entry { get; set; }

        public int MoodId { get; set; }
        public Mood? Mood { get; set; }

        // Use only "primary" or "secondary"
        public string MoodRole { get; set; } = "primary";

        public DateTime CreatedAt { get; set; }
    }
}
