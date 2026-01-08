using LibraryManagementBE.Model;
using Microsoft.EntityFrameworkCore;
 
namespace LibraryManagementBE.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Account> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique constraints for both users and admins

            modelBuilder.Entity<Account>()
                .HasIndex(a => a.email)
                .IsUnique();

            modelBuilder.Entity<Account>()
                .HasIndex(a => a.phoneNumber)
                .IsUnique();

            modelBuilder.Entity<Account>()
                .HasIndex(a => a.role);

            modelBuilder.Entity<Account>()
                .HasIndex(a => a.isActive);
        }
    }
}
