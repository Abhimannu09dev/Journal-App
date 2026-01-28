using Journal_App.Entities;

namespace Journal_App.Services
{
    public interface IJournalService
    {
        // READ
        // DB key format: "yyyy-MM-dd"
        Task<JournalEntry?> GetEntryByDateAsync(string entryDate);
        Task<JournalEntry?> GetEntryByDateAsync(DateTime date);

        // Used for exporting a single entry with full navigation data 
        Task<JournalEntry?> GetEntryByDateWithDetailsAsync(string entryDate);

        // Used for exporting multiple entries in a date range (bulk export)
        Task<List<JournalEntry>> GetEntriesByDateRangeAsync(string startDateKey, string endDateKey);

        Task<StreakStatsDto> GetStreakStatsAsync();

        Task<List<JournalEntry>> GetEntriesAsync();

        // Calendar helper: which days in the month have entries (for highlighting)
        Task<HashSet<string>> GetEntryDatesForMonthAsync(int year, int month);

        // Quick check (optional but useful)
        Task<bool> EntryExistsAsync(DateTime date);

        // MOODS
        Task<List<Mood>> GetMoodsAsync();

        Task<(bool ok, string? error)> SetEntryMoodsAsync(
            int entryId,
            int primaryMoodId,
            List<int>? secondaryMoodIds
        );

        // TAGS
        Task SetEntryTagsAsync(int entryId, List<string> tagNames);

        // CREATE
        Task<(bool ok, string? error)> CreateEntryAsync(
            DateTime date,
            string title,
            string content,
            string contentFormat
        );

        Task<(bool ok, string? error, int entryId)> CreateEntryWithMoodsAsync(
            DateTime date,
            string title,
            string content,
            string contentFormat,
            int primaryMoodId,
            List<int>? secondaryMoodIds
        );

        // UPDATE
        Task<(bool ok, string? error)> UpdateEntryAsync(
            int id,
            string title,
            string content,
            string contentFormat
        );

        Task<(bool ok, string? error)> UpdateEntryWithMoodsAsync(
            int id,
            string title,
            string content,
            string contentFormat,
            int primaryMoodId,
            List<int>? secondaryMoodIds
        );

        // DELETE
        Task<bool> DeleteEntryAsync(int id);
    }
}
