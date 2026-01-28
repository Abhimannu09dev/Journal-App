using System;
using System.Collections.Generic;

namespace Journal_App.Entities
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ColorHex { get; set; }
        public bool IsActive { get; set; } = true;
        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Navigation 
        public ICollection<EntryTag> EntryTags { get; set; } = new List<EntryTag>();
    }
}
