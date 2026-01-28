using System;
using System.Linq;
using Journal_App.Data;
using Journal_App.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Journal_App.Services
{
    public class JournalService : IJournalService
    {
        private readonly AppDbContext _context;

        public JournalService(AppDbContext context)
        {
            _context = context;
        }

        // -----------------------------
        // READ
        // -----------------------------
        public Task<JournalEntry?> GetEntryByDateAsync(string entryDate)
        {
            // Include tags + moods so UI can render everything
            return _context.JournalEntries
                .Include(e => e.EntryMoods)
                    .ThenInclude(em => em.Mood)
                .Include(e => e.EntryTags)
                    .ThenInclude(et => et.Tag)
                .FirstOrDefaultAsync(e => e.EntryDate == entryDate);
        }

        // NEW: Explicit "with details" method for export (same includes, clear intent)
        public Task<JournalEntry?> GetEntryByDateWithDetailsAsync(string entryDate)
        {
            return _context.JournalEntries
                .AsNoTracking()
                .Include(e => e.EntryMoods)
                    .ThenInclude(em => em.Mood)
                .Include(e => e.EntryTags)
                    .ThenInclude(et => et.Tag)
                .FirstOrDefaultAsync(e => e.EntryDate == entryDate);
        }

        // Overload for calendar/page usage
        public Task<JournalEntry?> GetEntryByDateAsync(DateTime date)
        {
            var entryDate = date.Date.ToString("yyyy-MM-dd");
            return GetEntryByDateAsync(entryDate);
        }

        public Task<List<JournalEntry>> GetEntriesAsync()
        {
            // Include tags + moods so list views can filter/show
            return _context.JournalEntries
                .Include(e => e.EntryMoods)
                    .ThenInclude(em => em.Mood)
                .Include(e => e.EntryTags)
                    .ThenInclude(et => et.Tag)
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();
        }

        // NEW: Export helper - returns all entries in an inclusive date range
        // Since EntryDate is "yyyy-MM-dd", string comparison works correctly.
        public async Task<List<JournalEntry>> GetEntriesByDateRangeAsync(string startDateKey, string endDateKey)
        {
            // Basic validation (service-side guard)
            if (string.IsNullOrWhiteSpace(startDateKey) || string.IsNullOrWhiteSpace(endDateKey))
                return new List<JournalEntry>();

            // Ensure ISO format is valid; if invalid, return empty (privacy-safe)
            if (!DateOnly.TryParseExact(startDateKey, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return new List<JournalEntry>();

            if (!DateOnly.TryParseExact(endDateKey, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                return new List<JournalEntry>();

            // Swap if user accidentally reversed (optional; could also return error)
            if (string.CompareOrdinal(startDateKey, endDateKey) > 0)
            {
                var temp = startDateKey;
                startDateKey = endDateKey;
                endDateKey = temp;
            }

            return await _context.JournalEntries
                .AsNoTracking()
                .Include(e => e.EntryMoods)
                    .ThenInclude(em => em.Mood)
                .Include(e => e.EntryTags)
                    .ThenInclude(et => et.Tag)
                .Where(e =>
                    e.EntryDate.CompareTo(startDateKey) >= 0 &&
                    e.EntryDate.CompareTo(endDateKey) <= 0
                )
                .OrderBy(e => e.EntryDate)
                .ToListAsync();
        }

        // Calendar helper: return all date keys in that month that have entries
        public async Task<HashSet<string>> GetEntryDatesForMonthAsync(int year, int month)
        {
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1);

            var startKey = start.ToString("yyyy-MM-dd");
            var endKey = end.ToString("yyyy-MM-dd"); // exclusive

            var dates = await _context.JournalEntries
                .AsNoTracking()
                .Where(e =>
                    e.EntryDate.CompareTo(startKey) >= 0 &&
                    e.EntryDate.CompareTo(endKey) < 0
                )
                .Select(e => e.EntryDate)
                .Distinct()
                .ToListAsync();

            return dates.ToHashSet(StringComparer.Ordinal);
        }

        public Task<bool> EntryExistsAsync(DateTime date)
        {
            var entryDate = date.Date.ToString("yyyy-MM-dd");
            return _context.JournalEntries.AnyAsync(e => e.EntryDate == entryDate);
        }

        public Task<List<Mood>> GetMoodsAsync()
        {
            return _context.Moods
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        // -----------------------------
        // MOODS (Entry <-> Mood)
        // -----------------------------
        public async Task<(bool ok, string? error)> SetEntryMoodsAsync(
            int entryId,
            int primaryMoodId,
            List<int>? secondaryMoodIds)
        {
            if (entryId <= 0) return (false, "Invalid entry.");
            if (primaryMoodId <= 0) return (false, "Primary mood is required.");

            // Clean + de-dupe secondary
            var cleanSecondary = (secondaryMoodIds ?? new List<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (cleanSecondary.Count > 2)
                return (false, "You can select up to 2 secondary moods.");

            if (cleanSecondary.Contains(primaryMoodId))
                return (false, "Primary mood cannot be selected as a secondary mood.");

            // Load entry with existing mood links
            var entry = await _context.JournalEntries
                .Include(e => e.EntryMoods)
                .FirstOrDefaultAsync(e => e.Id == entryId);

            if (entry == null) return (false, "Entry not found.");

            // Ensure selected moods exist + active
            var moodIdsToCheck = new List<int> { primaryMoodId };
            moodIdsToCheck.AddRange(cleanSecondary);

            var existingMoodIds = await _context.Moods
                .Where(m => m.IsActive && moodIdsToCheck.Contains(m.Id))
                .Select(m => m.Id)
                .ToListAsync();

            if (!existingMoodIds.Contains(primaryMoodId))
                return (false, "Selected primary mood does not exist.");

            foreach (var id in cleanSecondary)
            {
                if (!existingMoodIds.Contains(id))
                    return (false, "One or more selected secondary moods do not exist.");
            }

            // Replace links
            entry.EntryMoods.Clear();

            // Add Primary
            entry.EntryMoods.Add(new EntryMood
            {
                EntryId = entry.Id,
                MoodId = primaryMoodId,
                MoodRole = "primary"
            });

            // Add Secondary (0–2)
            foreach (var moodId in cleanSecondary)
            {
                entry.EntryMoods.Add(new EntryMood
                {
                    EntryId = entry.Id,
                    MoodId = moodId,
                    MoodRole = "secondary"
                });
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        // -----------------------------
        // TAGS (Entry <-> Tag)
        // -----------------------------
        public async Task SetEntryTagsAsync(int entryId, List<string> tagNames)
        {
            if (entryId <= 0) return;

            // Normalize + de-dupe
            var cleanNames = (tagNames ?? new List<string>())
                .Select(n => (n ?? "").Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Load entry with existing links
            var entry = await _context.JournalEntries
                .Include(e => e.EntryTags)
                    .ThenInclude(et => et.Tag)
                .FirstOrDefaultAsync(e => e.Id == entryId);

            if (entry == null) return;

            // If none selected, clear and save
            if (cleanNames.Count == 0)
            {
                entry.EntryTags.Clear();
                await _context.SaveChangesAsync();
                return;
            }

            // SQLite case-insensitive matching reliably via ToLower
            var lowerNames = cleanNames.Select(n => n.ToLower()).ToList();

            // Only active tags should be used
            var existingTags = await _context.Tags
                .Where(t => t.IsActive && lowerNames.Contains(t.Name.ToLower()))
                .ToListAsync();

            // Create missing Tag rows
            foreach (var name in cleanNames)
            {
                var exists = existingTags.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    var newTag = new Tag
                    {
                        Name = name,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Tags.Add(newTag);
                    existingTags.Add(newTag);
                }
            }

            // Save so new tags get IDs
            await _context.SaveChangesAsync();

            // Replace links (composite key prevents duplicates)
            entry.EntryTags.Clear();

            foreach (var name in cleanNames)
            {
                var tag = existingTags.First(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

                entry.EntryTags.Add(new EntryTag
                {
                    EntryId = entry.Id,
                    TagId = tag.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }

        // -----------------------------
        // CREATE
        // -----------------------------
        public async Task<(bool ok, string? error)> CreateEntryAsync(
            DateTime date,
            string title,
            string content,
            string contentFormat)
        {
            var entryDate = date.Date.ToString("yyyy-MM-dd");

            // Friendly UX check
            var exists = await _context.JournalEntries.AnyAsync(e => e.EntryDate == entryDate);
            if (exists) return (false, "Entry already exists for this date.");

            var now = DateTime.UtcNow;

            var entry = new JournalEntry
            {
                EntryDate = entryDate,
                Title = (title ?? string.Empty).Trim(),
                Content = content ?? string.Empty,
                ContentFormat = string.IsNullOrWhiteSpace(contentFormat) ? "markdown" : contentFormat.Trim(),
                WordCount = CountWords(content),
                CreatedAt = now,
                UpdatedAt = now,
            };

            _context.JournalEntries.Add(entry);

            try
            {
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (DbUpdateException)
            {
                // Backup guard for race conditions; UNIQUE(EntryDate) should trigger here
                return (false, "Entry already exists for this date.");
            }
        }

        public async Task<(bool ok, string? error, int entryId)> CreateEntryWithMoodsAsync(
            DateTime date,
            string title,
            string content,
            string contentFormat,
            int primaryMoodId,
            List<int>? secondaryMoodIds)
        {
            var (ok, error) = await CreateEntryAsync(date, title, content, contentFormat);
            if (!ok) return (false, error, 0);

            var entryDate = date.Date.ToString("yyyy-MM-dd");
            var entry = await _context.JournalEntries.FirstAsync(e => e.EntryDate == entryDate);

            var moodResult = await SetEntryMoodsAsync(entry.Id, primaryMoodId, secondaryMoodIds);
            if (!moodResult.ok) return (false, moodResult.error, entry.Id);

            return (true, null, entry.Id);
        }

        // -----------------------------
        // UPDATE
        // -----------------------------
        public async Task<(bool ok, string? error)> UpdateEntryAsync(
            int id,
            string title,
            string content,
            string contentFormat)
        {
            var existing = await _context.JournalEntries
                .FirstOrDefaultAsync(e => e.Id == id);

            if (existing == null) return (false, "Entry not found.");

            existing.Title = (title ?? string.Empty).Trim();
            existing.Content = content ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(contentFormat))
                existing.ContentFormat = contentFormat.Trim();

            existing.WordCount = CountWords(existing.Content);
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool ok, string? error)> UpdateEntryWithMoodsAsync(
            int id,
            string title,
            string content,
            string contentFormat,
            int primaryMoodId,
            List<int>? secondaryMoodIds)
        {
            var (ok, error) = await UpdateEntryAsync(id, title, content, contentFormat);
            if (!ok) return (false, error);

            var moodResult = await SetEntryMoodsAsync(id, primaryMoodId, secondaryMoodIds);
            if (!moodResult.ok) return (false, moodResult.error);

            return (true, null);
        }

        // -----------------------------
        // DELETE
        // -----------------------------
        public async Task<bool> DeleteEntryAsync(int id)
        {
            var existing = await _context.JournalEntries
                .Include(e => e.EntryTags)
                .Include(e => e.EntryMoods)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (existing == null) return false;

            // Safe if cascade is configured; avoids FK issues if not
            existing.EntryTags.Clear();
            existing.EntryMoods.Clear();

            _context.JournalEntries.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        // -----------------------------
        // Helpers
        // -----------------------------
        private static int CountWords(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            return text.Split(new[] { ' ', '\n', '\r', '\t' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Length;
        }

        //-----------------------------
        // STREAK STATS
        //-----------------------------
        public async Task<StreakStatsDto> GetStreakStatsAsync()
        {
            // Pull all EntryDate keys (string yyyy-MM-dd)
            var keys = await _context.JournalEntries
                .AsNoTracking()
                .Select(e => e.EntryDate)
                .Where(k => k != null && k != "")
                .Distinct()
                .ToListAsync();

            var stats = new StreakStatsDto();

            if (keys.Count == 0)
                return stats;

            // Parse to DateOnly (strict)
            var dates = new List<DateOnly>(keys.Count);

            foreach (var k in keys)
            {
                if (DateOnly.TryParseExact(
                        k,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var d))
                {
                    dates.Add(d);
                }
            }

            if (dates.Count == 0)
                return stats;

            dates.Sort();
            var dateSet = new HashSet<DateOnly>(dates);

            var today = DateOnly.FromDateTime(DateTime.Today);

            var first = dates[0];
            var last = dates[^1];

            stats.FirstEntryDate = first.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            stats.LastEntryDate = last.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // -----------------------
            // Longest streak (scan forward)
            // -----------------------
            int longest = 1;
            int run = 1;

            for (int i = 1; i < dates.Count; i++)
            {
                if (dates[i] == dates[i - 1].AddDays(1))
                    run++;
                else
                    run = 1;

                if (run > longest) longest = run;
            }

            stats.LongestStreak = longest;

            // -----------------------
            // Current streak (alive if today exists, else if yesterday exists)
            // -----------------------
            int current = 0;
            var start = today;

            // If no entry today but there is one yesterday, streak is still "alive"
            if (!dateSet.Contains(today) && dateSet.Contains(today.AddDays(-1)))
                start = today.AddDays(-1);

            var cursor = start;
            while (dateSet.Contains(cursor))
            {
                current++;
                cursor = cursor.AddDays(-1);
            }

            stats.CurrentStreak = current;

            // -----------------------
            // Missed days detection (between first entry and today)
            // -----------------------
            var missed = new List<string>();

            for (var d = first; d <= today; d = d.AddDays(1))
            {
                if (!dateSet.Contains(d))
                    missed.Add(d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            stats.MissedDaysCount = missed.Count;
            stats.MissedDays = missed;

            return stats;
        }
    }
}
