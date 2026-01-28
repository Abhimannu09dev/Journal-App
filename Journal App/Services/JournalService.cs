using Journal_App.Data;
using Journal_App.Entities;
using Microsoft.EntityFrameworkCore;

namespace Journal_App.Services
{
    public class JournalService : IJournalService
    {
        private readonly AppDbContext _context;

        public JournalService(AppDbContext context)
        {
            _context = context;
        }

        // Entries
        public Task<List<JournalEntry>> GetEntriesAsync()
        {
            // include moods so preview/editor can show selected moods
            return _context.JournalEntries
                .Include(e => e.EntryMoods)
                    .ThenInclude(em => em.Mood)
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();
        }

        public Task<JournalEntry?> GetEntryByDateAsync(string entryDateKey)
        {
            return _context.JournalEntries
                .Include(e => e.EntryMoods)
                    .ThenInclude(em => em.Mood)
                .FirstOrDefaultAsync(e => e.EntryDate == entryDateKey);
        }

        public Task<JournalEntry?> GetEntryByDateAsync(DateTime date)
        {
            var key = date.Date.ToString("yyyy-MM-dd");
            return GetEntryByDateAsync(key);
        }

        public Task<bool> EntryExistsAsync(DateTime date)
        {
            var key = date.Date.ToString("yyyy-MM-dd");
            return _context.JournalEntries.AnyAsync(e => e.EntryDate == key);
        }

        public async Task<(bool ok, string? error)> CreateEntryAsync(DateTime date, string title, string content)
        {
            var key = date.Date.ToString("yyyy-MM-dd");

            if (await _context.JournalEntries.AnyAsync(e => e.EntryDate == key))
                return (false, "Entry already exists for this date.");

            title = (title ?? "").Trim();
            content = content ?? "";

            if (string.IsNullOrWhiteSpace(title)) return (false, "Title is required.");
            if (string.IsNullOrWhiteSpace(content)) return (false, "Content is required.");

            var now = DateTime.UtcNow;

            var entry = new JournalEntry
            {
                EntryDate = key,
                Title = title,
                Content = content,
                WordCount = CountWords(content),
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.JournalEntries.Add(entry);

            try
            {
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (DbUpdateException)
            {
                return (false, "Entry already exists for this date.");
            }
        }

        public async Task<(bool ok, string? error)> UpdateEntryAsync(int id, string title, string content)
        {
            var entry = await _context.JournalEntries.FirstOrDefaultAsync(e => e.Id == id);
            if (entry is null) return (false, "Entry not found.");

            title = (title ?? "").Trim();
            content = content ?? "";

            if (string.IsNullOrWhiteSpace(title)) return (false, "Title is required.");
            if (string.IsNullOrWhiteSpace(content)) return (false, "Content is required.");

            entry.Title = title;
            entry.Content = content;
            entry.WordCount = CountWords(content);
            entry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> DeleteEntryAsync(int id)
        {
            var entry = await _context.JournalEntries
                .Include(e => e.EntryMoods)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (entry is null) return false;
            entry.EntryMoods.Clear();

            _context.JournalEntries.Remove(entry);
            await _context.SaveChangesAsync();
            return true;
        }

        // Mood Tracking 
        public Task<List<Mood>> GetMoodsAsync()
        {
            return _context.Moods
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<(bool ok, string? error)> SetEntryMoodsAsync(
            int entryId,
            int primaryMoodId,
            List<int>? secondaryMoodIds)
        {
            if (entryId <= 0) return (false, "Invalid entry.");
            if (primaryMoodId <= 0) return (false, "Primary mood is required.");

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

            // Validate moods exist & active
            var moodIdsToCheck = new List<int> { primaryMoodId };
            moodIdsToCheck.AddRange(cleanSecondary);

            var existingMoodIds = await _context.Moods
                .Where(m => m.IsActive && moodIdsToCheck.Contains(m.Id))
                .Select(m => m.Id)
                .ToListAsync();

            if (!existingMoodIds.Contains(primaryMoodId))
                return (false, "Selected primary mood does not exist.");

            foreach (var id in cleanSecondary)
                if (!existingMoodIds.Contains(id))
                    return (false, "One or more selected secondary moods do not exist.");

            // Replace links
            entry.EntryMoods.Clear();

            entry.EntryMoods.Add(new EntryMood
            {
                EntryId = entry.Id,
                MoodId = primaryMoodId,
                MoodRole = "primary"
            });

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

        // Helpers
        private static int CountWords(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            return text.Split(new[] { ' ', '\n', '\r', '\t' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Length;
        }
    }
}
