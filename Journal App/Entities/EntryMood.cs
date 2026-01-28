using System;

namespace Journal_App.Entities
{
    public class EntryMood
    {
        public int EntryId { get; set; }
        public JournalEntry? Entry { get; set; }
        public int MoodId { get; set; }
        public Mood? Mood { get; set; }

        public string MoodRole { get; set; } = "primary";

        public DateTime CreatedAt { get; set; }
    }
}
