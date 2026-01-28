using Android.Content;
using Journal_App.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Journal_App.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

         // Mood Tracking
        public DbSet<Mood> Moods => Set<Mood>();
        public DbSet<EntryMood> EntryMoods => Set<EntryMood>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // JournalEntry
            modelBuilder.Entity<JournalEntry>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.EntryDate).IsRequired().HasMaxLength(10); // yyyy-MM-dd
                entity.HasIndex(e => e.EntryDate).IsUnique(); // one entry per day

                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Content).IsRequired();

                entity.Property(e => e.WordCount).HasDefaultValue(0);

                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
            });

            // Mood
            modelBuilder.Entity<Mood>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.Property(m => m.Name).IsRequired().HasMaxLength(50);
                entity.HasIndex(m => m.Name).IsUnique();

                entity.Property(m => m.Emoji).HasMaxLength(10);
                entity.Property(m => m.IsActive).HasDefaultValue(true);

                // Seed moods
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

            // EntryMood
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

                // Prevent same mood linked twice for same entry (even across roles)
                entity.HasIndex(em => new { em.EntryId, em.MoodId }).IsUnique();

                // Enforce allowed roles at DB level
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_EntryMood_MoodRole",
                    "MoodRole IN ('primary','secondary')"
                ));
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
        }
    }
}
