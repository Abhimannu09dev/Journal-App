using System;
using System.Collections.Generic;

namespace Journal_App.Entities
{
    public class JournalEntry
    {
        public int Id { get; set; }
        // Core identity

        /// <summary>
        /// Date key stored as ISO string (yyyy-MM-dd).
        /// Enables "one entry per day" constraint and safe string range queries.
        /// </summary>
        public string EntryDate { get; set; } = string.Empty;

        // Content
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Markdown or rich text content
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Content format identifier ("markdown" or "richtext")
        /// </summary>
        public string ContentFormat { get; set; } = "markdown";

        /// <summary>
        /// System-generated word count (updated on create/update)
        /// </summary>
        public int WordCount { get; set; }

        /// <summary>
        /// UTC timestamp when the entry was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// UTC timestamp when the entry was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        // Navigation properties

        public ICollection<EntryMood> EntryMoods { get; set; } = new List<EntryMood>();

        public ICollection<EntryTag> EntryTags { get; set; } = new List<EntryTag>();

        // Helper (NOT mapped to DB)

        /// <summary>
        /// Parses EntryDate into DateTime safely.
        /// Used for display/export only.
        /// </summary>
        public DateTime? EntryDateAsDateTime
        {
            get
            {
                if (DateTime.TryParseExact(
                        EntryDate,
                        "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var dt))
                {
                    return dt;
                }

                return null;
            }
        }
    }
}
