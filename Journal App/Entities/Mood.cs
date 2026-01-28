using System.Collections.Generic;

namespace Journal_App.Entities
{
    public class Mood
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;   // e.g., "Happy"
        public string? Emoji { get; set; }                 // optional
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<EntryMood> EntryMoods { get; set; } = new List<EntryMood>();
    }
}
