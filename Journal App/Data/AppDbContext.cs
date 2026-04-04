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
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
        public DbSet<Mood> Moods => Set<Mood>();
        public DbSet<EntryMood> EntryMoods => Set<EntryMood>();

        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<EntryTag> EntryTags => Set<EntryTag>();

        public DbSet<AuthSecret> AuthSecrets => Set<AuthSecret>();
        public DbSet<UserSettings> UserSettings => Set<UserSettings>();
        public DbSet<ExportHistory> ExportHistory => Set<ExportHistory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // JournalEntry
            modelBuilder.Entity<JournalEntry>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.EntryDate).IsRequired().HasMaxLength(10);
                e.HasIndex(x => x.EntryDate).IsUnique();
            });

            // Tag
            modelBuilder.Entity<Tag>(t =>
            {
                t.HasKey(x => x.Id);
                t.Property(x => x.Name).IsRequired().HasMaxLength(50);
                t.HasIndex(x => x.Name).IsUnique();
            });

            // EntryTag (many-to-many)
            modelBuilder.Entity<EntryTag>(et =>
            {
                et.HasKey(x => new { x.EntryId, x.TagId });

                et.HasOne(x => x.Entry)
                  .WithMany(e => e.EntryTags)
                  .HasForeignKey(x => x.EntryId)
                  .OnDelete(DeleteBehavior.Cascade);

                et.HasOne(x => x.Tag)
                  .WithMany(t => t.EntryTags)
                  .HasForeignKey(x => x.TagId)
                  .OnDelete(DeleteBehavior.Restrict);
            });

            // Mood
            modelBuilder.Entity<Mood>(m =>
            {
                m.HasKey(x => x.Id);
                m.Property(x => x.Name).IsRequired().HasMaxLength(50);
                m.HasIndex(x => x.Name).IsUnique();
            });

            // EntryMood
            modelBuilder.Entity<EntryMood>(em =>
            {
                em.HasKey(x => new { x.EntryId, x.MoodId, x.MoodRole });
            });

            // AuthSecret
            modelBuilder.Entity<AuthSecret>(a =>
            {
                a.HasKey(x => x.Id);
                a.Property(x => x.SecretType).IsRequired().HasMaxLength(50);
                a.Property(x => x.SecretHash).IsRequired();
                a.Property(x => x.Salt).IsRequired();
            });

            // UserSettings (single-row table)
            modelBuilder.Entity<UserSettings>(s =>
            {
                s.HasKey(x => x.Id);
                s.Property(x => x.Username).IsRequired().HasMaxLength(50);
                s.Property(x => x.ThemeMode).IsRequired().HasMaxLength(20);
            });

            // ExportHistory
            modelBuilder.Entity<ExportHistory>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.ExportType).IsRequired().HasMaxLength(20);
                e.Property(x => x.FileName).IsRequired().HasMaxLength(260);
            });
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            foreach (var e in ChangeTracker.Entries<JournalEntry>())
            {
                if (e.State == EntityState.Added)
                {
                    e.Entity.CreatedAt = now;
                    e.Entity.UpdatedAt = now;
                }
                else if (e.State == EntityState.Modified)
                {
                    e.Property(x => x.CreatedAt).IsModified = false;
                    e.Entity.UpdatedAt = now;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
