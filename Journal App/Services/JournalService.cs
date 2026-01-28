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

        public Task<List<JournalEntry>> GetEntriesAsync()
        {
            return _context.JournalEntries
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();
        }

        public Task<JournalEntry?> GetEntryByDateAsync(string entryDateKey)
        {
            return _context.JournalEntries
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
            var entry = await _context.JournalEntries.FirstOrDefaultAsync(e => e.Id == id);
            if (entry is null) return false;

            _context.JournalEntries.Remove(entry);
            await _context.SaveChangesAsync();
            return true;
        }

        private static int CountWords(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            return text.Split(new[] { ' ', '\n', '\r', '\t' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Length;
        }
    }
}
