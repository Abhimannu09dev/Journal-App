namespace Journal_App.Services
{
    public class StreakStatsDto
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }

        public int MissedDaysCount { get; set; }
        public List<string> MissedDays { get; set; } = new(); // yyyy-MM-dd (can be large)

        public string? LastEntryDate { get; set; } // yyyy-MM-dd
        public string? FirstEntryDate { get; set; } // yyyy-MM-dd
    }
}
