using System;
using System.Collections.Generic;

namespace Journal_App.Entities
{
    public class Mood
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Emoji { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<EntryMood> EntryMoods { get; set; } = new List<EntryMood>();
    }
}
