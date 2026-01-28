using Journal_App.Data;
using Journal_App.Entities;
using Journal_App.Services;
using Journal_App.Services.Export;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Journal_App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
#endif

            // Add DbContext (SQLite) - SAFE
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                // Windows-safe: doesn't depend on MAUI Essentials being ready
                var dbDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var dbPath = Path.Combine(dbDir, "journal.db");

                options.UseSqlite($"Filename={dbPath}");
            });

            // Add Services
            builder.Services.AddScoped<IJournalService, JournalService>();

            // Dashboard DI
            builder.Services.AddScoped<IDashboardService, DashboardService>();

            //  Export Service (PDF)
            builder.Services.AddScoped<IExportService, PdfExportService>();

            builder.Services.AddScoped<UserSettingsService>();
            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton<AuthStateService>();

            var app = builder.Build();

            // Ensure database is created + seed initial data
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                // Seed Tags (only if missing)
                var seedTags = new[]
                {
                    "Work","Study","Health","Family","Friends","Stress","Happy","Goal",
                    "Reflection","Productivity","Exercise","Sleep","Important","Gratitude","Travel"
                };

                var existingTagNames = db.Tags.Select(t => t.Name).ToList();
                var tagSet = new HashSet<string>(existingTagNames, StringComparer.OrdinalIgnoreCase);

                var newTags = seedTags
                    .Where(name => !tagSet.Contains(name))
                    .Select(name => new Tag
                    {
                        Name = name,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    })
                    .ToList();

                if (newTags.Count > 0)
                {
                    db.Tags.AddRange(newTags);
                    db.SaveChanges();
                }

                // Ensure default settings + default PIN exist
                var settingsSvc = scope.ServiceProvider.GetRequiredService<UserSettingsService>();
                settingsSvc.GetSettingsAsync().GetAwaiter().GetResult();
                settingsSvc.EnsurePinSecretExistsAsync().GetAwaiter().GetResult();
            }

            return app;
        }
    }
}
