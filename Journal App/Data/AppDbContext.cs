using Android.Content;
using Journal_App.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Journal_App.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
        }
    }
}
