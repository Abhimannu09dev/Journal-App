using Journal_App.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Journal_App.Services
{
    public interface IDashboardService
    {
        Task<DashboardVm> GetDashboardAsync(int latestCount = 5, CancellationToken ct = default);
    }

    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _db;

        public DashboardService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardVm> GetDashboardAsync(int latestCount = 5, CancellationToken ct = default)
        {
            // 1) Total Entries
            var totalEntries = await _db.JournalEntries.CountAsync(ct);

            // Pull all dates once (fast enough for coursework scale)
            var dateStrings = await _db.JournalEntries
                .AsNoTracking()
                .Select(e => e.EntryDate)
                .ToListAsync(ct);

            var dates = dateStrings
                .Select(TryParseDateOnly)
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .Distinct()
                .ToList();

            // 2) Current Streak (STRICT: must have entry today)
            var currentStreak = CalculateCurrentStreakStrict(dates);

            // 3) Longest Streak
            var longestStreak = CalculateLongestStreak(dates);

            // 4) Latest Entries (top N) + Primary Mood (if exists)
            var latestEntries = await GetLatestEntriesAsync(latestCount, ct);

            // 5) Emotion Overview: Most selected PRIMARY mood
            var topPrimaryMood = await GetTopPrimaryMoodAsync(ct);

            return new DashboardVm
            {
                TotalEntries = totalEntries,
                CurrentStreak = currentStreak,
                LongestStreak = longestStreak,
                LatestEntries = latestEntries,
                TopPrimaryMood = topPrimaryMood
            };
        }

        private async Task<List<LatestEntryItemVm>> GetLatestEntriesAsync(int latestCount, CancellationToken ct)
        {
            // Get latest entries (by EntryDate, since you enforce 1/day)
            var latest = await _db.JournalEntries
                .AsNoTracking()
                .OrderByDescending(e => e.EntryDate)
                .Take(latestCount)
                .Select(e => new
                {
                    e.Id,
                    e.EntryDate,
                    e.Title,
                    e.Content
                })
                .ToListAsync(ct);

            if (latest.Count == 0)
                return new List<LatestEntryItemVm>();

            var ids = latest.Select(x => x.Id).ToList();

            // Primary moods for those entries
            var primaryMoods = await _db.EntryMoods
                .AsNoTracking()
                .Where(em => ids.Contains(em.EntryId) && em.MoodRole == "primary")
                .Select(em => new
                {
                    em.EntryId,
                    MoodName = em.Mood!.Name,
                    MoodEmoji = em.Mood!.Emoji
                })
                .ToListAsync(ct);

            var moodMap = primaryMoods
                .GroupBy(x => x.EntryId)
                .ToDictionary(g => g.Key, g => g.First());

            // Build VM
            var result = new List<LatestEntryItemVm>();
            foreach (var e in latest)
            {
                var snippet = MakeSnippet(e.Content, 120);

                moodMap.TryGetValue(e.Id, out var mood);

                result.Add(new LatestEntryItemVm
                {
                    EntryId = e.Id,
                    EntryDate = e.EntryDate,
                    Title = e.Title,
                    Snippet = snippet,
                    PrimaryMood = mood?.MoodName,
                    PrimaryMoodEmoji = mood?.MoodEmoji
                });
            }

            return result;
        }

        private async Task<TopMoodVm?> GetTopPrimaryMoodAsync(CancellationToken ct)
        {
            // Count primary moods
            var grouped = await _db.EntryMoods
                .AsNoTracking()
                .Where(em => em.MoodRole == "primary")
                .GroupBy(em => new { em.MoodId, em.Mood!.Name, em.Mood!.Emoji })
                .Select(g => new
                {
                    g.Key.MoodId,
                    g.Key.Name,
                    g.Key.Emoji,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(ct);

            if (grouped.Count == 0)
                return null;

            // Tie-break: choose the mood used most recently (by EntryDate)
            var topCount = grouped[0].Count;
            var tied = grouped.Where(x => x.Count == topCount).ToList();

            if (tied.Count == 1)
            {
                var only = tied[0];
                return new TopMoodVm { Name = only.Name, Emoji = only.Emoji, Count = only.Count };
            }

            var tiedMoodIds = tied.Select(x => x.MoodId).ToList();

            // Find latest EntryDate among tied moods
            var latestUse = await _db.EntryMoods
                .AsNoTracking()
                .Where(em => em.MoodRole == "primary" && tiedMoodIds.Contains(em.MoodId))
                .Select(em => new
                {
                    em.MoodId,
                    EntryDate = em.Entry!.EntryDate
                })
                .ToListAsync(ct);

            var latestByMood = latestUse
                .Select(x => new { x.MoodId, Date = TryParseDateOnly(x.EntryDate) })
                .Where(x => x.Date.HasValue)
                .GroupBy(x => x.MoodId)
                .Select(g => new { MoodId = g.Key, MaxDate = g.Max(z => z.Date!.Value) })
                .OrderByDescending(x => x.MaxDate)
                .ToList();

            var bestMoodId = latestByMood.FirstOrDefault()?.MoodId ?? tied[0].MoodId;
            var best = tied.First(x => x.MoodId == bestMoodId);

            return new TopMoodVm { Name = best.Name, Emoji = best.Emoji, Count = best.Count };
        }

        private static int CalculateCurrentStreakStrict(List<DateOnly> dates)
        {
            if (dates.Count == 0) return 0;

            var set = dates.ToHashSet();
            var today = DateOnly.FromDateTime(DateTime.Now);

            if (!set.Contains(today))
                return 0;

            int streak = 0;
            var cursor = today;

            while (set.Contains(cursor))
            {
                streak++;
                cursor = cursor.AddDays(-1);
            }

            return streak;
        }

        private static int CalculateLongestStreak(List<DateOnly> dates)
        {
            if (dates.Count == 0) return 0;

            var sorted = dates.Distinct().OrderBy(d => d).ToList();

            int best = 1;
            int run = 1;

            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i] == sorted[i - 1].AddDays(1))
                {
                    run++;
                }
                else
                {
                    run = 1;
                }

                if (run > best) best = run;
            }

            return best;
        }

        private static DateOnly? TryParseDateOnly(string? entryDate)
        {
            if (string.IsNullOrWhiteSpace(entryDate))
                return null;

            // Your storage format: yyyy-MM-dd
            if (DateOnly.TryParseExact(entryDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;

            return null;
        }

        private static string MakeSnippet(string? content, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "";

            var cleaned = content.Trim()
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\t", " ");

            if (cleaned.Length <= maxLen) return cleaned;

            return cleaned.Substring(0, maxLen).Trim() + "…";
        }
    }

    // ---------------------------
    // View Models
    // ---------------------------
    public class DashboardVm
    {
        public int TotalEntries { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public List<LatestEntryItemVm> LatestEntries { get; set; } = new();
        public TopMoodVm? TopPrimaryMood { get; set; }
    }

    public class LatestEntryItemVm
    {
        public int EntryId { get; set; }
        public string EntryDate { get; set; } = "";
        public string Title { get; set; } = "";
        public string Snippet { get; set; } = "";
        public string? PrimaryMood { get; set; }
        public string? PrimaryMoodEmoji { get; set; }
    }

    public class TopMoodVm
    {
        public string Name { get; set; } = "";
        public string? Emoji { get; set; }
        public int Count { get; set; }
    }
}
