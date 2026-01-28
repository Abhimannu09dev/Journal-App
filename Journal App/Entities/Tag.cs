using System;
using System.Collections.Generic;

namespace Journal_App.Entities
{
    public class Tag
    {
        public int Id { get; set; }

        // Tag display name (unique, case-insensitive by DB constraint)
        public string Name { get; set; } = string.Empty;

        // Optional UI color (e.g. "#FFAA00")
        public string? ColorHex { get; set; }

        // Soft-delete / disable instead of hard delete
        public bool IsActive { get; set; } = true;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation (many-to-many)
        public ICollection<EntryTag> EntryTags { get; set; } = new List<EntryTag>();
    }
}
