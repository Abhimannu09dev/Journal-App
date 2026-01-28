using Journal_App.Entities;

namespace Journal_App.Services
{
    public interface IJournalService
    {
        // Entries
        Task<List<JournalEntry>> GetEntriesAsync();
        Task<JournalEntry?> GetEntryByDateAsync(string entryDateKey); // yyyy-MM-dd
        Task<JournalEntry?> GetEntryByDateAsync(DateTime date);

        Task<bool> EntryExistsAsync(DateTime date);

        Task<(bool ok, string? error)> CreateEntryAsync(DateTime date, string title, string content);
        Task<(bool ok, string? error)> UpdateEntryAsync(int id, string title, string content);

        Task<bool> DeleteEntryAsync(int id);

        // Mood Tracking
        Task<List<Mood>> GetMoodsAsync();

        Task<(bool ok, string? error)> SetEntryMoodsAsync(
            int entryId,
            int primaryMoodId,
            List<int>? secondaryMoodIds
        );
    }
}
