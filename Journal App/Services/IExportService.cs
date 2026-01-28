using Journal_App.Entities;

namespace Journal_App.Services.Export
{
    public interface IExportService
    {
        // Returns a suggested filename + the PDF bytes (UI will ask user where to save)
        Task<(string FileName, byte[] Bytes)> ExportSingleEntryPdfAsync(
            JournalEntry entry,
            ExportOptions options);

        // Returns a suggested filename + the PDF bytes (UI will ask user where to save)
        Task<(string FileName, byte[] Bytes)> ExportEntriesPdfAsync(
            List<JournalEntry> entries,
            string startDateKey,
            string endDateKey,
            ExportOptions options);
    }
}
