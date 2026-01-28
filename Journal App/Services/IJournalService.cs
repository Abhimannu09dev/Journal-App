using Journal_App.Entities;

namespace Journal_App.Services
{
    public interface IJournalService
    {
        Task<JournalEntry?> GetEntryByDateAsync(string dateKey);
        Task<List<JournalEntry>> GetEntriesAsync();

        // TAGS
        Task SetEntryTagsAsync(int entryId, List<string> tagNames);

        // CREATE / UPDATE
        Task<(bool ok, string? error)> CreateEntryAsync(
            DateTime date,
            string title,
            string content,
            string contentFormat
        );

        Task<(bool ok, string? error)> UpdateEntryAsync(
            int id,
            string title,
            string content,
            string contentFormat
        );

        // DELETE
        Task<bool> DeleteEntryAsync(int id);
    }
}
