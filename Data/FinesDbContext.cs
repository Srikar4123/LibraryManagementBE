using LibraryManagementBE.Model;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementBE.Data
{
        public class FinesDbContext : DbContext
        {
            public FinesDbContext(DbContextOptions<FinesDbContext> options) : base(options) { }

            public DbSet<Fines> Fines { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Indexes to speed up common queries
            modelBuilder.Entity<Fines>()
                .HasIndex(f => new { f.UserId, f.ReturnDate }); // active loans per user

            modelBuilder.Entity<Fines>()
                .HasIndex(f => new { f.BookId, f.ReturnDate }); // active loans per book

            // Ensure decimal precision
            modelBuilder.Entity<Fines>()
                .Property(f => f.fineAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Fines>()
                .HasIndex(f => new { f.UserId, f.BookId })
                .HasFilter("[ReturnDate] IS NULL")
                .IsUnique();
        }
    }
}


