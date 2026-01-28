using Journal_App.Entities;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Globalization;
using System.Text;

namespace Journal_App.Services.Export
{
    public class PdfExportService : IExportService
    {
        public Task<(string FileName, byte[] Bytes)> ExportSingleEntryPdfAsync(JournalEntry entry, ExportOptions options)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var list = new List<JournalEntry> { entry };

            // EntryDate is stored as a string key "yyyy-MM-dd"
            var key = string.IsNullOrWhiteSpace(entry.EntryDate)
                ? DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : entry.EntryDate;

            return ExportEntriesPdfAsync(list, key, key, options);
        }

        public Task<(string FileName, byte[] Bytes)> ExportEntriesPdfAsync(
            List<JournalEntry> entries,
            string startDateKey,
            string endDateKey,
            ExportOptions options)
        {
            entries ??= new List<JournalEntry>();
            options ??= new ExportOptions();

            // Privacy-safe preset
            if (options.PrivacySafeMode)
            {
                options.IncludeContent = false;
                options.IncludeMoods = false;
                options.IncludeTags = false;
                options.IncludeTitle = false;
            }

            // Filter nulls first, then sort (EntryDate is yyyy-MM-dd so ordinal sort works)
            entries = entries
                .Where(e => e != null)
                .OrderBy(e => e.EntryDate ?? "")
                .ToList();

            var doc = new PdfDocument();
            doc.Info.Title = $"Journal Export {startDateKey} to {endDateKey}";

            var page = doc.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // More portable than Arial
            var fontTitle = new XFont("Helvetica", 16, XFontStyle.Bold);
            var fontHeading = new XFont("Helvetica", 12, XFontStyle.Bold);
            var fontBody = new XFont("Helvetica", 11, XFontStyle.Regular);

            double margin = 40;
            double y = margin;

            void NewPage()
            {
                page = doc.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                y = margin;
            }

            void DrawLine(string text, XFont font)
            {
                if (y > page.Height - margin)
                    NewPage();

                gfx.DrawString(text ?? "", font, XBrushes.Black,
                    new XRect(margin, y, page.Width - margin * 2, page.Height),
                    XStringFormats.TopLeft);

                y += 18;
            }

            // Wrap one line (space-based)
            void DrawWrappedLine(string text, XFont font)
            {
                if (string.IsNullOrEmpty(text))
                {
                    DrawLine("", font);
                    return;
                }

                var maxWidth = page.Width - margin * 2;
                var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var line = new StringBuilder();
                foreach (var w in words)
                {
                    var test = (line.Length == 0) ? w : line + " " + w;
                    var size = gfx.MeasureString(test, font);

                    if (size.Width > maxWidth)
                    {
                        DrawLine(line.ToString(), font);
                        line.Clear();
                        line.Append(w);
                    }
                    else
                    {
                        if (line.Length > 0) line.Append(' ');
                        line.Append(w);
                    }
                }

                if (line.Length > 0)
                    DrawLine(line.ToString(), font);
            }

            // Wrap multi-line text (preserves newlines)
            void DrawWrappedText(string text, XFont font)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    DrawLine("—", font);
                    return;
                }

                var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
                var lines = normalized.Split('\n');

                foreach (var ln in lines)
                {
                    // blank line spacing
                    if (string.IsNullOrWhiteSpace(ln))
                    {
                        DrawLine("", font);
                        continue;
                    }

                    DrawWrappedLine(ln, font);
                }
            }

            static string ToReadableDate(string? dateKey)
            {
                if (string.IsNullOrWhiteSpace(dateKey))
                    return "—";

                if (DateTime.TryParseExact(
                        dateKey,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var dt))
                {
                    return dt.ToString("dddd, MMMM dd, yyyy", CultureInfo.InvariantCulture);
                }

                return dateKey;
            }

            // Header
            DrawLine("Journal Export", fontTitle);
            DrawLine($"Date Range: {ToReadableDate(startDateKey)} to {ToReadableDate(endDateKey)}", fontBody);
            DrawLine($"Entries: {entries.Count}", fontBody);
            y += 10;

            foreach (var e in entries)
            {
                DrawLine("------------------------------------------------------------", fontBody);

                DrawLine($"Date: {ToReadableDate(e.EntryDate)}", fontHeading);

                if (options.IncludeTitle)
                {
                    var t = string.IsNullOrWhiteSpace(e.Title) ? "Untitled Entry" : e.Title.Trim();
                    DrawWrappedLine($"Title: {t}", fontBody);
                }

                if (options.IncludeMoods)
                {
                    var primary = e.EntryMoods?.FirstOrDefault(x => x.MoodRole == "primary")?.Mood;
                    var primaryText = primary == null ? "—" : $"{primary.Emoji} {primary.Name}";

                    var secondary = e.EntryMoods?
                        .Where(x => x.MoodRole == "secondary")
                        .Select(x => x.Mood)
                        .Where(m => m != null)
                        .Select(m => $"{m!.Emoji} {m!.Name}")
                        .ToList() ?? new List<string>();

                    var secondaryText = secondary.Count == 0 ? "—" : string.Join(", ", secondary);

                    DrawWrappedLine($"Mood (Primary): {primaryText}", fontBody);
                    DrawWrappedLine($"Mood (Secondary): {secondaryText}", fontBody);
                }

                if (options.IncludeTags)
                {
                    var tagNames = e.EntryTags?
                        .Select(et => et.Tag?.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Select(n => n!.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(n => n)
                        .ToList() ?? new List<string>();

                    var tagText = tagNames.Count == 0 ? "—" : string.Join(", ", tagNames);
                    DrawWrappedLine($"Tags: {tagText}", fontBody);
                }

                if (options.IncludeContent)
                {
                    DrawLine("", fontBody);
                    DrawLine("Entry:", fontHeading);

                    // Export markdown as plain text (privacy-safe + reliable)
                    var content = string.IsNullOrWhiteSpace(e.Content) ? "—" : e.Content;
                    DrawWrappedText(content, fontBody);
                }

                y += 8;
            }

            // Build filename (UI will choose the location)
            var safeStart = string.IsNullOrWhiteSpace(startDateKey) ? "start" : startDateKey;
            var safeEnd = string.IsNullOrWhiteSpace(endDateKey) ? "end" : endDateKey;

            var fileName = $"JournalExport_{safeStart}_to_{safeEnd}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            // Save to memory (bytes)
            using var ms = new MemoryStream();
            doc.Save(ms, false);

            return Task.FromResult((fileName, ms.ToArray()));
        }
    }
}
