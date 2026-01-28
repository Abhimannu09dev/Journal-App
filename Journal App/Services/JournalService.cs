using Journal_App.Data;
using Journal_App.Entities;
using Microsoft.EntityFrameworkCore;
using System;
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

        public Task<JournalEntry?> GetEntryByDateAsync(string dateKey)
        {
            return _context.JournalEntries
                .Include(e => e.EntryTags)
                    .ThenInclude(et => et.Tag)
                .FirstOrDefaultAsync(e => e.EntryDate == dateKey);
        }

        public Task<List<JournalEntry>> GetEntriesAsync()
        {
            return _context.JournalEntries
                .Include(e => e.EntryTags)
                    .ThenInclude(et => et.Tag)
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();
        }

        // TAGS
        public async Task SetEntryTagsAsync(int entryId, List<string> tagNames)
        {
            var entry = await _context.JournalEntries
                .Include(e => e.EntryTags)
                .FirstOrDefaultAsync(e => e.Id == entryId);

            if (entry == null) return;

            entry.EntryTags.Clear();

            var clean = tagNames
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var name in clean)
            {
                var tag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());

                if (tag == null)
                {
                    tag = new Tag
                    {
                        Name = name,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Tags.Add(tag);
                    await _context.SaveChangesAsync();
                }

                entry.EntryTags.Add(new EntryTag
                {
                    EntryId = entry.Id,
                    TagId = tag.Id
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<(bool ok, string? error)> CreateEntryAsync(
            DateTime date,
            string title,
            string content,
            string contentFormat)
        {
            var key = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (await _context.JournalEntries.AnyAsync(e => e.EntryDate == key))
                return (false, "Entry already exists.");

            var entry = new JournalEntry
            {
                EntryDate = key,
                Title = title,
                Content = content,
                ContentFormat = contentFormat,
                WordCount = CountWords(content)
            };

            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool ok, string? error)> UpdateEntryAsync(
            int id,
            string title,
            string content,
            string contentFormat)
        {
            var entry = await _context.JournalEntries.FindAsync(id);
            if (entry == null) return (false, "Entry not found.");

            entry.Title = title;
            entry.Content = content;
            entry.ContentFormat = contentFormat;
            entry.WordCount = CountWords(content);

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> DeleteEntryAsync(int id)
        {
            var entry = await _context.JournalEntries
                .Include(e => e.EntryTags)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (entry == null) return false;

            _context.JournalEntries.Remove(entry);
            await _context.SaveChangesAsync();
            return true;
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}
