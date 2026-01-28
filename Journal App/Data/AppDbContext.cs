using Journal_App.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Journal_App.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
        public DbSet<Mood> Moods => Set<Mood>();
        public DbSet<EntryMood> EntryMoods => Set<EntryMood>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<EntryTag> EntryTags => Set<EntryTag>();
        public DbSet<UserSettings> UserSettings => Set<UserSettings>();
        public DbSet<AuthSecret> AuthSecrets => Set<AuthSecret>();
        public DbSet<ExportHistory> ExportHistories => Set<ExportHistory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -------------------------
            // JournalEntry
            // -------------------------
            modelBuilder.Entity<JournalEntry>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.EntryDate).IsRequired().HasMaxLength(10); // yyyy-MM-dd
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.ContentFormat).IsRequired().HasMaxLength(20);

                entity.Property(e => e.WordCount).HasDefaultValue(0);

                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasIndex(e => e.EntryDate).IsUnique(); // one entry per day
            });

            // -------------------------
            // Mood (lookup)
            // -------------------------
            modelBuilder.Entity<Mood>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.Property(m => m.Name).IsRequired().HasMaxLength(50);
                entity.HasIndex(m => m.Name).IsUnique();

                entity.Property(m => m.Emoji).HasMaxLength(10);
                entity.Property(m => m.IsActive).HasDefaultValue(true);

                entity.HasData(
                    new Mood { Id = 1, Name = "Happy", Emoji = "😀", IsActive = true },
                    new Mood { Id = 2, Name = "Sad", Emoji = "😢", IsActive = true },
                    new Mood { Id = 3, Name = "Angry", Emoji = "😡", IsActive = true },
                    new Mood { Id = 4, Name = "Calm", Emoji = "😌", IsActive = true },
                    new Mood { Id = 5, Name = "Anxious", Emoji = "😰", IsActive = true },
                    new Mood { Id = 6, Name = "Excited", Emoji = "🤩", IsActive = true },
                    new Mood { Id = 7, Name = "Tired", Emoji = "😴", IsActive = true },
                    new Mood { Id = 8, Name = "Stressed", Emoji = "😣", IsActive = true }
                );
            });

            // -------------------------
            // EntryMood
            // -------------------------
            modelBuilder.Entity<EntryMood>(entity =>
            {
                entity.HasKey(em => new { em.EntryId, em.MoodId, em.MoodRole });

                entity.Property(em => em.MoodRole).IsRequired().HasMaxLength(20);
                entity.Property(em => em.CreatedAt).IsRequired();

                entity.HasOne(em => em.Entry)
                      .WithMany(e => e.EntryMoods)
                      .HasForeignKey(em => em.EntryId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(em => em.Mood)
                      .WithMany(m => m.EntryMoods)
                      .HasForeignKey(em => em.MoodId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Prevent same MoodId being linked twice for the same EntryId (even across roles)
                entity.HasIndex(em => new { em.EntryId, em.MoodId }).IsUnique();

                // NEW: helps dashboard "Top primary mood" queries
                entity.HasIndex(em => new { em.MoodRole, em.MoodId });

                // Enforce allowed roles at DB level
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_EntryMood_MoodRole",
                    "MoodRole IN ('primary','secondary')"
                ));
            });

            // -------------------------
            // Tag
            // -------------------------
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Name).IsRequired().HasMaxLength(50);
                entity.HasIndex(t => t.Name).IsUnique();

                entity.Property(t => t.ColorHex).HasMaxLength(10);
                entity.Property(t => t.IsActive).HasDefaultValue(true);
            });

            // -------------------------
            // EntryTag
            // -------------------------
            modelBuilder.Entity<EntryTag>(entity =>
            {
                entity.HasKey(et => new { et.EntryId, et.TagId });

                entity.HasOne(et => et.Entry)
                      .WithMany(e => e.EntryTags)
                      .HasForeignKey(et => et.EntryId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(et => et.Tag)
                      .WithMany(t => t.EntryTags)
                      .HasForeignKey(et => et.TagId)
                      .OnDelete(DeleteBehavior.Restrict);

                // NEW: helps tag-based analytics queries (if you add them later)
                entity.HasIndex(et => et.TagId);
            });

            // -------------------------
            // UserSettings (single row)
            // -------------------------
            modelBuilder.Entity<UserSettings>(entity =>
            {
                entity.HasKey(us => us.Id);

                entity.Property(us => us.Username).IsRequired().HasMaxLength(50);

                entity.Property(us => us.ThemeMode).IsRequired().HasMaxLength(10);
                entity.Property(us => us.AccentColorHex).HasMaxLength(10);
                entity.Property(us => us.CustomThemeJson);

                entity.Property(us => us.PinHint).IsRequired().HasMaxLength(200);

                entity.Property(us => us.CreatedAt).IsRequired();
                entity.Property(us => us.UpdatedAt).IsRequired();
            });

            // -------------------------
            // AuthSecret (one row per secret type)
            // -------------------------
            modelBuilder.Entity<AuthSecret>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.SecretType).IsRequired().HasMaxLength(20);
                entity.HasIndex(a => a.SecretType).IsUnique();

                entity.Property(a => a.SecretHash).IsRequired();
                entity.Property(a => a.Salt).IsRequired().HasMaxLength(200);

                entity.Property(a => a.Iterations).IsRequired().HasDefaultValue(100_000);
                entity.Property(a => a.FailedAttempts).IsRequired().HasDefaultValue(0);

                entity.Property(a => a.LockedUntil);

                entity.Property(a => a.CreatedAt).IsRequired();
                entity.Property(a => a.UpdatedAt).IsRequired();
            });

            // -------------------------
            // ExportHistory
            // -------------------------
            modelBuilder.Entity<ExportHistory>(entity =>
            {
                entity.HasKey(eh => eh.Id);

                entity.Property(eh => eh.ExportType).IsRequired().HasMaxLength(10);
                entity.Property(eh => eh.FileName).IsRequired().HasMaxLength(255);

                entity.Property(eh => eh.FilePath).HasMaxLength(500);
                entity.Property(eh => eh.FromDate).HasMaxLength(10);
                entity.Property(eh => eh.ToDate).HasMaxLength(10);

                entity.HasIndex(eh => eh.CreatedAt);
            });
        }

        public override int SaveChanges()
        {
            ApplyRules();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyRules();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyRules()
        {
            var now = DateTime.UtcNow;

            // JournalEntry timestamps + immutables
            foreach (var entry in ChangeTracker.Entries<JournalEntry>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;

                    if (!string.IsNullOrWhiteSpace(entry.Entity.EntryDate) && entry.Entity.EntryDate.Length > 10)
                        entry.Entity.EntryDate = entry.Entity.EntryDate.Substring(0, 10);
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.EntryDate).IsModified = false;

                    entry.Entity.UpdatedAt = now;
                }
            }

            // EntryMood CreatedAt + normalization
            foreach (var em in ChangeTracker.Entries<EntryMood>())
            {
                if (em.State == EntityState.Added)
                {
                    em.Entity.CreatedAt = now;
                    em.Entity.MoodRole = (em.Entity.MoodRole ?? "").Trim().ToLowerInvariant();
                }
                else if (em.State == EntityState.Modified)
                {
                    em.Property(x => x.CreatedAt).IsModified = false;
                    em.Entity.MoodRole = (em.Entity.MoodRole ?? "").Trim().ToLowerInvariant();
                }
            }

            // UserSettings timestamps + normalization
            foreach (var us in ChangeTracker.Entries<UserSettings>())
            {
                if (us.State == EntityState.Added)
                {
                    us.Entity.CreatedAt = now;
                    us.Entity.UpdatedAt = now;

                    us.Entity.Username = (us.Entity.Username ?? "User").Trim();
                    us.Entity.ThemeMode = (us.Entity.ThemeMode ?? "light").Trim().ToLowerInvariant();
                    us.Entity.PinHint = (us.Entity.PinHint ?? "").Trim();
                }
                else if (us.State == EntityState.Modified)
                {
                    us.Property(x => x.CreatedAt).IsModified = false;
                    us.Entity.UpdatedAt = now;

                    us.Entity.Username = (us.Entity.Username ?? "User").Trim();
                    us.Entity.ThemeMode = (us.Entity.ThemeMode ?? "light").Trim().ToLowerInvariant();
                    us.Entity.PinHint = (us.Entity.PinHint ?? "").Trim();
                }
            }

            // AuthSecret timestamps + normalization
            foreach (var sec in ChangeTracker.Entries<AuthSecret>())
            {
                if (sec.State == EntityState.Added)
                {
                    sec.Entity.SecretType = (sec.Entity.SecretType ?? "").Trim().ToLowerInvariant();
                    if (string.IsNullOrWhiteSpace(sec.Entity.SecretType))
                        throw new InvalidOperationException("AuthSecret.SecretType cannot be empty.");

                    sec.Entity.CreatedAt = now;
                    sec.Entity.UpdatedAt = now;
                }
                else if (sec.State == EntityState.Modified)
                {
                    sec.Property(x => x.SecretType).IsModified = false;
                    sec.Property(x => x.CreatedAt).IsModified = false;

                    sec.Entity.UpdatedAt = now;
                }
            }
        }
    }
}
